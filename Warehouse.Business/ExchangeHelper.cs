//
// ExchangeHelper.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   08.16.2011
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
using System.IO;
using System.Reflection;
using Warehouse.Business.Licensing;
using Warehouse.Data;

namespace Warehouse.Business
{
    public delegate void ImporterCallback<T> (T obj, long? locationId, int current, int total, out bool cancelled);

    public class ExchangePropertyInfo
    {
        public string Name { get; set; }
        public bool IsRequired { get; set; }
        public string DefaultMapping { get; set; }
        public int MappedColumn { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
    }

    public class PropertyMap : Dictionary<int, List<PropertyInfo>>
    {
    }

    public static class ExchangeHelper
    {
        public static IList<T> GetExchangeInfo<T> (Type type) where T : ExchangePropertyInfo, new ()
        {
            List<T> props = new List<T> ();
            foreach (PropertyInfo property in type.GetProperties ()) {
                DbColumnAttribute boAttribute = null;
                ExchangePropertyAttribute exAttribute = null;
                foreach (object attrib in property.GetCustomAttributes (true)) {
                    if (boAttribute == null && attrib is DbColumnAttribute)
                        boAttribute = (DbColumnAttribute) attrib;

                    if (exAttribute == null && attrib is ExchangePropertyAttribute)
                        exAttribute = (ExchangePropertyAttribute) attrib;

                    if (boAttribute != null && exAttribute != null)
                        break;
                }

                if (exAttribute == null)
                    continue;

                string exchangeName;
                if (boAttribute != null)
                    exchangeName = Translator.GetExchangeFieldName (boAttribute.DbField);
                else if (exAttribute.FieldName != DataField.NotSet)
                    exchangeName = Translator.GetExchangeFieldName (exAttribute.FieldName);
                else
                    exchangeName = exAttribute.DefaultExchangeName;

                props.Add (new T
                    {
                        Name = exchangeName,
                        IsRequired = exAttribute.Required,
                        DefaultMapping = exAttribute.DefaultExchangeName,
                        PropertyInfo = property
                    });
            }

            props.Sort ((x, y) => string.Compare (x.Name, y.Name));
            return props;
        }

        public static PropertyMap GeneratePropertyMap<T> (IEnumerable<T> exchangeInfo) where T : ExchangePropertyInfo
        {
            PropertyMap propertyMap = new PropertyMap ();
            foreach (T property in exchangeInfo) {
                int columnIndex = property.MappedColumn;

                if (propertyMap.ContainsKey (columnIndex))
                    propertyMap [columnIndex].Add (property.PropertyInfo);
                else
                    propertyMap.Add (columnIndex, new List<PropertyInfo> (new [] { property.PropertyInfo }));
            }

            return propertyMap;
        }

        public static T CreateImportedObject<T> (PropertyMap propertyMap, params string [] values)
        {
            T ret = (T) DataProvider.CreateObject (typeof (T));
            for (int i = 0; i < values.Length; i++) {
                List<PropertyInfo> properties;
                if (!propertyMap.TryGetValue (i, out properties))
                    continue;

                string value = values [i];
                foreach (PropertyInfo info in properties)
                    info.SetValue (ret, BusinessDomain.GetObjectValue (info.PropertyType, value), null);
            }

            return ret;
        }

        public static void ImportObjects<T> (IDataImporter importer, string file, PropertyMap propertyMap, long? locationId, ImporterCallback<T> callback)
        {
            string [] [] data = new string [0] [];
            LicenseLimitationException llex = null;

            try {
                importer.Import (file, out data);
            } catch (LicenseLimitationException ex) {
                llex = ex;
            }

            for (int i = 0; i < data.Length; i++) {
                T obj = CreateImportedObject<T> (propertyMap, data [i]);
                bool cancel;
                callback (obj, locationId, i, data.Length, out cancel);
                if (cancel)
                    break;
            }

            if (llex != null)
                throw llex;
        }
    }
}
