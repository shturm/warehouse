//
// BindColumnList.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   03/28/2007
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Warehouse.Data.DataBinding
{
    public class BindColumnList<T>
    {
        private readonly BindManager<T> manager;
        private readonly PropertyDescriptorCollection typedListProps;
        private readonly Dictionary<string, int> columnNames;
        private readonly Type [] columnTypes;
        private readonly List<string> captions;
        private readonly List<string> resolvedNames;
        private readonly int columnCount;

        #region Public properties

        public int Count
        {
            get { return columnCount; }
        }

        public bool NamedColumns
        {
            get
            {
                return (columnNames != null && columnNames.Count != 0);
            }
        }

        public BindColumn this [string columnName]
        {
            get
            {
                if (columnNames == null || columnNames.Count == 0)
                    throw new Exception ("The columns are not named.");

                if (!columnNames.ContainsKey (columnName))
                    throw new Exception (string.Format ("No column named {0} found.", columnName));

                int index = columnNames [columnName];
                return new BindColumn (index, columnName, columnTypes [index], captions [index], resolvedNames != null ? resolvedNames [index] : null);
            }
        }

        public BindColumn this [int index]
        {
            get
            {
                if (columnNames == null || columnNames.Count == 0)
                    throw new Exception ("The columns are not named.");

                foreach (string name in columnNames.Keys) {
                    if (columnNames [name] == index)
                        return new BindColumn (index, name, columnTypes [index], captions [index], resolvedNames != null ? resolvedNames [index] : null);
                }

                throw new Exception (string.Format ("No column with index {0} found.", index));
            }
        }

        #endregion

        internal BindColumnList (BindManager<T> manager, ITypedList list)
        {
            typedListProps = list.GetItemProperties (null);
            if (typedListProps != null && typedListProps.Count != 0) {
                columnNames = new Dictionary<string, int> ();
                columnTypes = new Type [typedListProps.Count];
                captions = new List<string> (typedListProps.Count);

                for (int i = 0; i < typedListProps.Count; i++) {
                    columnNames.Add (typedListProps [i].Name, i);
                    columnTypes [i] = typedListProps [i].PropertyType;
                    captions.Add (typedListProps [i].Name);
                }

                columnCount = columnNames.Count;

                DataTable dataTable = null;
                if (list is DataView)
                    dataTable = ((DataView) list).Table;
                if (list is DataTable)
                    dataTable = (DataTable) list;
                if (dataTable != null)
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                        captions [i] = dataTable.Columns [i].Caption;
            }
            this.manager = manager;
        }

        internal BindColumnList (BindManager<T> manager, IList rowList)
        {
            if (rowList == null)
                return;

            columnCount = rowList.Count;
            columnTypes = new Type [rowList.Count];

            for (int i = 0; i < rowList.Count; i++)
                columnTypes [i] = rowList [i].GetType ();

            this.manager = manager;
        }

        internal BindColumnList (BindManager<T> manager, Type type)
        {
            int i, j;

            if (type == null)
                throw new ArgumentNullException ("type");

            MemberInfo [] objMembers = type.GetMembers ();
            List<Type> list = new List<Type> ();
            columnNames = new Dictionary<string, int> ();
            captions = new List<string> ();
            resolvedNames = new List<string> ();

            for (i = 0, j = 0; i < objMembers.Length; i++) {
                MemberInfo memberInfo = objMembers [i];
                string columnName = manager.ColumnNameResolver != null ?
                    manager.ColumnNameResolver (memberInfo) :
                    memberInfo.Name;

                PropertyInfo property = memberInfo as PropertyInfo;
                if (property != null) {
                    if (!property.CanRead && !property.CanWrite)
                        continue;

                    columnNames.Add (property.Name, j++);
                    list.Add (property.PropertyType);
                    captions.Add (property.Name);
                    resolvedNames.Add (columnName);
                    continue;
                }

                FieldInfo field = memberInfo as FieldInfo;
                if (field != null) {
                    columnNames.Add (field.Name, j++);
                    list.Add (field.FieldType);
                    captions.Add (field.Name);
                    resolvedNames.Add (columnName);
                }
            }

            columnTypes = list.ToArray ();
            columnCount = columnNames.Count;

            this.manager = manager;
        }

        internal BindColumnList (BindManager<T> manager)
        {
            columnTypes = new Type [0];
            columnCount = 0;

            this.manager = manager;
        }

        public Type [] GetColumnTypes ()
        {
            Type [] ret = new Type [columnCount];
            columnTypes.CopyTo (ret, 0);

            return ret;
        }

        public object [] GetRowValues (object rowObject)
        {
            if (rowObject == null)
                return new object [0];

            rowObject = manager.ResolveDataSource (rowObject);

            if (typedListProps != null) {
                List<object> retList = new List<object> ();
                for (int i = 0; i < typedListProps.Count; i++)
                    retList.Add (typedListProps [i].GetValue (rowObject));

                return retList.ToArray ();
            }

            IList list = rowObject as IList;
            if (list != null)
                return list.Cast<object> ().ToArray ();

            if (columnNames == null || columnNames.Count == 0)
                return new object [0];

            object [] retArray = new object [columnNames.Count];
            MemberInfo [] members = rowObject.GetType ().GetMembers ();
            foreach (MemberInfo memberInfo in members) {
                if (!columnNames.ContainsKey (memberInfo.Name))
                    continue;

                switch (memberInfo.MemberType) {
                    case MemberTypes.Field:
                        FieldInfo fi = (FieldInfo) memberInfo;
                        try {
                            retArray [columnNames [memberInfo.Name]] = fi.GetValue (rowObject);
                        } catch (Exception) {
                            retArray [columnNames [memberInfo.Name]] = null;
                        }
                        break;

                    case MemberTypes.Property:
                        PropertyInfo pi = (PropertyInfo) memberInfo;
                        try {
                            retArray [columnNames [memberInfo.Name]] = pi.CanRead ? pi.GetValue (rowObject, null) : null;
                        } catch (Exception) {
                            retArray [columnNames [memberInfo.Name]] = null;
                        }
                        break;
                }
            }

            return retArray;
        }

        public object GetRowValue (object rowObject, int index)
        {
            if (rowObject == null)
                return null;

            rowObject = manager.ResolveDataSource (rowObject);

            if (typedListProps != null) {
                return typedListProps [index].GetValue (rowObject);
            }

            IList list = rowObject as IList;
            if (list != null) {
                return list [index];
            }

            if (columnNames == null || columnNames.Count == 0)
                return null;

            string colName = null;
            foreach (string name in columnNames.Keys) {
                if (columnNames [name] == index)
                    colName = name;
            }

            if (string.IsNullOrEmpty (colName))
                return null;
            
            MemberInfo [] members = rowObject.GetType ().GetMembers ();
            foreach (MemberInfo memberInfo in members) {
                if (memberInfo.Name != colName)
                    continue;

                switch (memberInfo.MemberType) {
                    case MemberTypes.Field:
                        FieldInfo fi = (FieldInfo) memberInfo;
                        try {
                            return fi.GetValue (rowObject);
                        } catch (Exception) {
                            return null;
                        }

                    case MemberTypes.Property:
                        PropertyInfo pi = (PropertyInfo) memberInfo;
                        try {
                            return pi.CanRead ? pi.GetValue (rowObject, null) : null;
                        } catch (Exception) {
                            return null;
                        }
                }
            }

            return null;
        }

        public void SetRowValue (object rowObject, int index, object value)
        {
            if (rowObject == null)
                return;

            rowObject = manager.ResolveDataSource (rowObject);

            if (typedListProps != null) {
                typedListProps [index].SetValue (rowObject, value);
                return;
            }

            IList list = rowObject as IList;
            if (list != null) {
                list [index] = value;
            }

            MemberInfo [] members = rowObject.GetType ().GetMembers ();
            foreach (MemberInfo memberInfo in members) {
                if (!columnNames.ContainsKey (memberInfo.Name))
                    continue;

                if (columnNames [memberInfo.Name] != index)
                    continue;

                switch (memberInfo.MemberType) {
                    case MemberTypes.Field:
                        FieldInfo fi = (FieldInfo) memberInfo;
                        fi.SetValue (rowObject, value);
                        return;

                    case MemberTypes.Property:
                        PropertyInfo pi = (PropertyInfo) memberInfo;
                        if (pi.CanWrite) {
                            pi.SetValue (rowObject, value, null);
                            return;
                        }
                        throw new Exception (string.Format ("Unable to set value for property {0}. The property is read only.", pi.Name));
                }
            }
        }
    }
}
