//
// SourceItemId.cs
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

namespace Warehouse.Presentation
{
    public class SourceItemId : Dictionary<DbField, object>
    {
        private readonly DbTable? table;

        public DbTable? Table
        {
            get { return table; }
        }

        public SourceItemId (RowReader row, DbTable? table, params DataField [] fields)
        {
            this.table = table;
            if (fields != null)
                foreach (DbField field in fields)
                    this [field] = row [field];
        }

        public void AddConstants (IEnumerable<KeyValuePair<DataField, object>> consts)
        {
            foreach (KeyValuePair<DataField, object> pair in consts)
                this [pair.Key] = pair.Value;
        }
    }
}
