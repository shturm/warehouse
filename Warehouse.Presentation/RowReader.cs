//
// RowReader.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   10.06.2011
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

using System.Collections.Generic;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Presentation
{
    public class RowReader
    {
        private readonly LazyTableDataRow row;
        private readonly Dictionary<DbField, int> indexes = new Dictionary<DbField, int> ();

        public object this [DbField field]
        {
            get
            {
                int index;
                return indexes.TryGetValue (field, out index) ? row [index] : null;
            }
        }

        public RowReader (LazyTableDataRow row, IList<ColumnInfo> columns)
        {
            this.row = row;
            for (int i = 0; i < columns.Count; i++)
                indexes [columns [i].Field] = i;
        }
    }
}
