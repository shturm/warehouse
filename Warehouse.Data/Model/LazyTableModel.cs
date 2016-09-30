//
// LazyTableModel.cs
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

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Warehouse.Data.SQL;

namespace Warehouse.Data.Model
{
    public class LazyTableModel : LazyListModel<LazyTableDataRow>, ITypedList
    {
        private readonly List<string> columns = new List<string> ();

        public IList<string> Columns
        {
            get { return columns; }
        }

        internal LazyTableModel ()
        {
        }

        internal LazyTableModel (DataProvider dp, bool autoStart, string command, params DbParam [] parameters)
            : base (dp, autoStart, command, parameters)
        {
        }

        protected override void InitializeColumnManagers ()
        {
            FieldTranslationCollection fTrans = provider.FieldsTable;
            SelectBuilder sBuilder = new SelectBuilder (fTrans, commandText);

            colManagers = new DbColumnManager [sBuilder.SelectClause.Count];
            foreach (SelectColumnInfo columnInfo in sBuilder.SelectClause)
                columns.Add (fTrans.StripQuotationChars (columnInfo.ColumnName));

            DbField [] fields = fTrans.TranslateToDbField (columns.ToArray ());
            for (int i = 0; i < columns.Count; i++) {
                LazyTableColumnManager lcm = new LazyTableColumnManager (i, fields [i]);
                string columnName = columns [i];

                colManagers [i] = lcm;
                dbPropManagers [columnName] = lcm;
                allPropManagers [columnName] = lcm;
            }

            translateManagers = false;
        }

        public string GetListName (PropertyDescriptor [] listAccessors)
        {
            return typeof (LazyTableDataRow).Name;
        }

        public PropertyDescriptorCollection GetItemProperties (PropertyDescriptor [] listAccessors)
        {
            return new PropertyDescriptorCollection (colManagers
                .Cast<LazyTableColumnManager> ()
                .Select (manager => new LazyTableColumnDescriptor (manager))
                .Cast<PropertyDescriptor> ().ToArray ());
        }
    }
}
