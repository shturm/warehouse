//
// DbField.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/23/2008
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

namespace Warehouse.Data
{
    [Serializable]
    public class DbField : ICloneable
    {
        private DataField strongField = DataField.NotSet;
        private string field;

        public DataField StrongField
        {
            get { return strongField; }
            set
            {
                strongField = value;
                field = value.ToString ();
            }
        }

        public string Field
        {
            get { return field; }
            set
            {
                field = value;

                try {
                    if (Enum.IsDefined (typeof (DataField), value))
                        strongField = (DataField) Enum.Parse (typeof (DataField), value);
                    else
                        strongField = DataField.NotSet;
                } catch (ArgumentException) {
                    strongField = DataField.NotSet;
                }
            }
        }

        public DbField ()
        {
        }

        public DbField (DataField field)
        {
            StrongField = field;
        }

        public DbField (string field)
        {
            Field = field;
        }

        public static bool operator == (DbField dbField, DataField dataField)
        {
            return dbField != null && dbField.StrongField == dataField;
        }

        public static bool operator != (DbField dbField, DataField dataField)
        {
            return !(dbField == dataField);
        }

        public static bool operator == (DataField dataField, DbField dbField)
        {
            return dbField == dataField;
        }

        public static bool operator != (DataField dataField, DbField dbField)
        {
            return !(dbField == dataField);
        }

        public static bool operator == (DbField dbField1, DbField dbField2)
        {
            return Equals (dbField1, dbField2);
        }

        public static bool operator != (DbField dbField1, DbField dbField2)
        {
            return !Equals (dbField1, dbField2);
        }

        public static implicit operator DbField (DataField field)
        {
            return new DbField (field);
        }

        public static implicit operator DbField (string field)
        {
            return new DbField (field);
        }

        protected bool Equals (DbField dbField)
        {
            if (dbField == null)
                return false;
            return Equals (strongField, dbField.strongField) && Equals (field, dbField.field);
        }

        public override bool Equals (object obj)
        {
            if (ReferenceEquals (this, obj))
                return true;
            return Equals (obj as DbField);
        }

        public override int GetHashCode ()
        {
            return field != null ? field.GetHashCode () : strongField.GetHashCode ();
        }

        public override string ToString ()
        {
            return field;
        }

        #region ICloneable Members

        public object Clone ()
        {
            return new DbField (field);
        }

        #endregion
    }
}
