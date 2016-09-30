//
// ConfigEntry.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   06/30/2006
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

using System.Globalization;
using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public class ConfigEntry
    {
        #region Private fields

        private long id = -1;
        private string key = string.Empty;
        private string value = string.Empty;
        private long userId = -1;

        #endregion

        #region Public properties

        [DbColumn (DataField.ConfigEntryId)]
        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        [DbColumn (DataField.ConfigEntryKey, 50)]
        public string Key
        {
            get { return key; }
            set { key = value; }
        }

        [DbColumn (DataField.ConfigEntryUserId)]
        public long UserId
        {
            get { return userId; }
            set { userId = value; }
        }

        [DbColumn (DataField.ConfigEntryValue, 50)]
        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }

        private int? intValue;
        public int? IntValue
        {
            get
            {
                if (intValue == null) {
                    int val;
                    if (int.TryParse (value, out val))
                        intValue = val;
                }

                return intValue;
            }
            set
            {
                if (value == intValue)
                    return;

                this.value = value != null ? value.Value.ToString (CultureInfo.InvariantCulture) : string.Empty;
                intValue = value;
            }
        }

        private long? longValue;
        public long? LongValue
        {
            get
            {
                if (longValue == null) {
                    long val;
                    if (long.TryParse (value, out val))
                        longValue = val;
                }

                return longValue;
            }
            set
            {
                if (value == longValue)
                    return;

                this.value = value != null ? value.Value.ToString (CultureInfo.InvariantCulture) : string.Empty;
                longValue = value;
            }
        }

        private bool? boolValue;
        public bool? BoolValue
        {
            get { return boolValue ?? (boolValue = (IntValue ?? 0) != 0); }
            set
            {
                if (value == boolValue)
                    return;

                IntValue = value != null ? (value.Value ? -1 : 0) : (int?) null;
                boolValue = value;
            }
        }

        #endregion

        public ConfigEntry CommitChanges ()
        {
            BusinessDomain.DataAccessProvider.AddUpdateConfiguration (this);

            return this;
        }

        public static void Delete (string key, long? userId)
        {
            BusinessDomain.DataAccessProvider.DeleteConfiguration (key, userId);
        }

        public static void SaveValue (string key, string value, long userId)
        {
            ConfigEntry configEntry = GetByName (key, userId) ?? new ConfigEntry { Key = key, UserId = userId };
            configEntry.Value = value;
            configEntry.CommitChanges ();
        }

        public static void SaveValue (string key, int value, long userId)
        {
            SaveValue (key, value.ToString (CultureInfo.InvariantCulture), userId);
        }

        public static void SaveValue (string key, long value, long userId)
        {
            SaveValue (key, value.ToString (CultureInfo.InvariantCulture), userId);
        }

        public static void SaveValue (string key, bool value, long userId)
        {
            SaveValue (key, value ? "-1" : "0", userId);
        }

        public static ConfigEntry GetByName (string keyName, long? userId = null)
        {
            return BusinessDomain.DataAccessProvider.GetConfigurationByKey<ConfigEntry> (keyName, userId);
        }

        public static long GetLongValue (string keyName, long? userId, long defaultValue = -1)
        {
            ConfigEntry configEntry = GetByName (keyName, userId);
            if (configEntry == null)
                return defaultValue;

            return configEntry.LongValue ?? defaultValue;
        }

        public static int GetIntegerValue (string keyName, long? userId, int defaultValue = -1)
        {
            ConfigEntry configEntry = GetByName (keyName, userId);
            if (configEntry == null)
                return defaultValue;

            return configEntry.IntValue ?? defaultValue;
        }

        public static bool GetBoolValue (string keyName, long? userId, bool defaultValue = false)
        {
            ConfigEntry configEntry = GetByName (keyName, userId);
            if (configEntry == null)
                return defaultValue;

            return configEntry.BoolValue ?? defaultValue;
        }

        public override string ToString ()
        {
            return string.Format ("ID: {0}, UserID: {1}, Key: {2}, Value: {3}",
                id, userId, key, value);
        }
    }
}
