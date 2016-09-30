//
// LazyTableColumnManager.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   02/12/2009
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

namespace Warehouse.Data.Model
{
    public class LazyTableColumnManager : DbColumnManager
    {
        private readonly int columnIndex;
        private readonly DbField dbField;
        private Type type;

        public override DbField DbField
        {
            get { return dbField; }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override Type MemberType
        {
            get { return type ?? typeof (object); }
        }

        public LazyTableColumnManager (int columnIndex, DbField dbField)
        {
            this.columnIndex = columnIndex;
            this.dbField = dbField;
            DbPosition = columnIndex;
        }

        public override void SetValue (object obj, object value)
        {
            LazyTableDataRow ldr = (LazyTableDataRow) obj;
            ldr [columnIndex] = value;
            if (type == null && value != null)
                type = value.GetType ();
        }

        public override object GetValue (object obj, bool applyRestrictions = false)
        {
            LazyTableDataRow ldr = (LazyTableDataRow) obj;
            object ret = ldr [columnIndex];
            if (type == null && ret != null)
                type = ret.GetType ();

            return ret;
        }
    }
}
