//
// ConfigurationProvider.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   06/24/2006
//
// 2006-2015 (C) Microinvest, http://www.microinvest.net
//

// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Warehouse.Business.Entities;
using Warehouse.Data;

namespace Warehouse.Business
{
    public abstract class ConfigurationProvider : INotifyPropertyChanged
    {
        private readonly bool exeConfig;
        protected string configFileName;
        private static readonly Dictionary<string, string> customDefaults = new Dictionary<string, string> ();

        public static Dictionary<string, string> CustomDefaults
        {
            get { return customDefaults; }
        }

        protected ConfigurationProvider ()
            : this (true)
        {
        }

        protected ConfigurationProvider (bool exeConfig)
        {
            this.exeConfig = exeConfig;
        }

        #region Configuration load

        public void Load ()
        {
            LoadConfigSettings ();
            Load (true);
        }

        protected abstract void LoadConfigSettings ();

        public virtual void Load (bool loadLocal)
        {
            Load (this, loadLocal);
        }

        protected void Load (object configHolder, bool loadLocal)
        {
            bool saveNeeded = false;
            bool loadConfigFromDb = BusinessDomain.DataAccessProvider != null;

            foreach (MemberInfo member in configHolder.GetType ().GetMembers (BindingFlags.Public | BindingFlags.Instance)) {
                // Get the data type and skip everything that is not a field or property
                Type memberType;
                switch (member.MemberType) {
                    case MemberTypes.Field:
                        memberType = ((FieldInfo) member).FieldType;
                        break;
                    case MemberTypes.Property:
                        PropertyInfo property = (PropertyInfo) member;
                        if (!property.CanWrite)
                            continue;
                        memberType = property.PropertyType;
                        break;
                    default:
                        continue; // Skip methods, events etc.
                }

                // Check if this is a configuration property and get the extended attributes
                ConfigurationMemberAttribute attrib = null;
                foreach (object customAttrib in member.GetCustomAttributes (true)) {
                    attrib = customAttrib as ConfigurationMemberAttribute;
                    if (attrib != null)
                        break;
                }

                // If the field is not marked with the appropriate attribute or 
                // only user-specific settings are required and it is not user-specific, skip it
                if (attrib == null)
                    continue;

                // The default value cannot be null
                if (attrib.DefaultValue == null)
                    continue;

                if (string.IsNullOrEmpty (attrib.DbKey) != loadLocal)
                    continue;

                string value = null;
                ConfigurationMemberAttribute saveToDbAttrib = null;
                if (string.IsNullOrEmpty (attrib.DbKey)) {
                    if (!TryGetSetting (member.Name, out value))
                        saveNeeded = true;
                } else if (loadConfigFromDb) {
                    if (attrib.MigrateToDb && TryGetSetting (member.Name, out value)) {
                        DeleteSetting (member.Name);
                        saveNeeded = true;
                    }

                    ConfigEntry ent = ConfigEntry.GetByName (attrib.DbKey);
                    if (ent == null)
                        saveToDbAttrib = attrib;
                    else
                        value = ent.Value;
                }

                if (value == null) {
                    if (!customDefaults.TryGetValue (member.Name, out value))
                        value = attrib.DefaultValue;
                    attrib = null;
                }

                try {
                    // Set the property with the data from the configuration file
                    SetMember (configHolder, member, memberType, value, attrib);
                } catch {
                    // If the data is corrupted set the default value
                    SetMember (configHolder, member, memberType, value, null);
                }

                if (saveToDbAttrib != null) {
                    string valueString = GetMemberConfigValue (saveToDbAttrib, value);
                    SaveMember (member, valueString, true, saveToDbAttrib.DbKey);
                }
            }

            if (!saveNeeded)
                return;

            SetSettings (configHolder, false);
            SaveSettings ();
        }

        protected static void SetMember (object configHolder, MemberInfo member, Type type, string value, ConfigurationMemberAttribute attrib)
        {
            if (value == null)
                throw new ArgumentNullException ("value");

            if (attrib != null && attrib.Encrypted) {
                if (!string.IsNullOrEmpty (value)) {
                    string keyFile = PlatformHelper.IsWindows ?
                        attrib.KeyFile :
                        (string.IsNullOrWhiteSpace (attrib.UnixKeyFile) ? attrib.KeyFile : attrib.UnixKeyFile);

                    if (string.IsNullOrEmpty (keyFile))
                        value = Encryption.DecryptConfigurationValue (value);
                    else
                        try {
                            value = Encryption.DecryptConfigurationValue (value, keyFile);
                        } catch (Exception ex) {
                            ErrorHandling.LogException (ex);
                            value = Encryption.DecryptConfigurationValue (value);
                        }
                }
            }

            string memberTypeName = type.Name.ToLowerInvariant ();
            switch (memberTypeName) {
                case "string":
                    SetMemberValue (configHolder, member, value);
                    break;
                case "boolean":
                    SetMemberValue (configHolder, member, Convert.ToBoolean (value));
                    break;
                case "datetime":
                    DateTime dateTime;
                    try {
                        dateTime = Convert.ToDateTime (value);
                    } catch (FormatException) {
                        dateTime = DateTime.MinValue;
                    }

                    SetMemberValue (configHolder, member, dateTime);
                    break;
                case "decimal":
                    SetMemberValue (configHolder, member, Convert.ToDecimal (value));
                    break;
                case "single":
                    SetMemberValue (configHolder, member, Convert.ToSingle (value));
                    break;
                case "double":
                    SetMemberValue (configHolder, member, Convert.ToDouble (value));
                    break;
                default:
                    if (memberTypeName.StartsWith ("int32")) {
                        SetMemberValue (configHolder, member, Convert.ToInt32 (value));
                    } else if (memberTypeName.StartsWith ("int")) {
                        SetMemberValue (configHolder, member, Convert.ToInt64 (value));
                    } else if (type.IsEnum) {
                        SetMemberValue (configHolder, member, Enum.Parse (type, value));
                    }
                    break;
            }
        }

        private static void SetMemberValue (object configHolder, MemberInfo member, object value)
        {
            PropertyInfo propertyInfo = member as PropertyInfo;
            if (propertyInfo != null) {
                propertyInfo.SetValue (configHolder, value, null);
                return;
            }

            FieldInfo fieldInfo = member as FieldInfo;
            if (fieldInfo != null)
                fieldInfo.SetValue (configHolder, value);
        }

        #endregion

        public virtual void Save (bool saveToDb)
        {
            SetSettings (this, saveToDb);
            SaveSettings ();
        }

        protected void SetSettings (object configHolder, bool saveToDb)
        {
            if (!exeConfig)
                return;

            bool saveConfigToDb = BusinessDomain.DataAccessProvider != null && saveToDb;

            foreach (MemberInfo member in configHolder.GetType ().GetMembers (BindingFlags.Public | BindingFlags.Instance)) {
                ConfigurationMemberAttribute attrib = member.GetCustomAttributes (true).OfType<ConfigurationMemberAttribute> ().FirstOrDefault ();
                if (attrib == null)
                    continue;

                object value;
                switch (member.MemberType) {
                    case MemberTypes.Field:
                        value = ((FieldInfo) member).GetValue (configHolder);
                        break;
                    case MemberTypes.Property:
                        PropertyInfo property = (PropertyInfo) member;
                        if (!property.CanRead)
                            continue;
                        value = property.GetValue (configHolder, null);
                        break;
                    default:
                        continue; // not a property or field
                }

                string valueString = GetMemberConfigValue (attrib, value);
                SaveMember (member, valueString, saveConfigToDb, attrib.DbKey);
            }
        }

        public string GeConfigurationValue (string name)
        {
            foreach (MemberInfo member in GetType ().GetMember (name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance)) {
                object value;
                switch (member.MemberType) {
                    case MemberTypes.Field:
                        value = ((FieldInfo) member).GetValue (this);
                        break;
                    case MemberTypes.Property:
                        PropertyInfo property = (PropertyInfo) member;
                        if (!property.CanRead)
                            continue;
                        value = property.GetValue (this, null);
                        break;
                    default:
                        continue; // not a property or field
                }

                ConfigurationMemberAttribute attrib = member.GetCustomAttributes (true).OfType<ConfigurationMemberAttribute> ().FirstOrDefault ();
                if (attrib != null)
                    return GetMemberConfigValue (attrib, value);
            }

            return null;
        }

        private string GetMemberConfigValue (ConfigurationMemberAttribute attrib, object value)
        {
            string valueString;
            if (value == null)
                valueString = string.Empty;
            else if (value is DateTime)
                valueString = ((DateTime) value).ToString ("s");
            else
                valueString = value.ToString ();

            if (!attrib.Encrypted)
                return valueString;

            if (string.IsNullOrEmpty (attrib.KeyFile))
                return Encryption.EncryptConfigurationValue (valueString);

            try {
                return Encryption.EncryptConfigurationValue (valueString, attrib.KeyFile);
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                return Encryption.EncryptConfigurationValue (valueString);
            }
        }

        protected virtual void SaveMember (MemberInfo member, string valueString, bool saveConfigToDb, string dbKey)
        {
            if (string.IsNullOrEmpty (dbKey)) {
                SetSetting (member.Name, valueString);
            } else if (saveConfigToDb) {
                ConfigEntry.SaveValue (dbKey, valueString, -1);
            }
        }

        protected abstract bool TryGetSetting (string key, out string value);

        protected abstract void SetSetting (string key, string valueString);

        protected abstract void DeleteSetting (string key);

        protected abstract void SaveSettings ();

        protected bool SetValueConfig<T> (Expression<Func<T>> prop, ref T field, T value) where T : struct
        {
            if (field.Equals (value))
                return false;

            field = value;
            OnPropertyChanged (((MemberExpression) prop.Body).Member.Name);
            return true;
        }

        protected bool SetClassConfig<T> (Expression<Func<T>> prop, ref T field, T value) where T : class
        {
            if (field != null && field.Equals (value))
                return false;

            field = value;
            OnPropertyChanged (((MemberExpression) prop.Body).Member.Name);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged (string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler (this, new PropertyChangedEventArgs (propertyName));
        }
    }
}
