//
// DataProvider.MeasUnit.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   10.13.2007
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

using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetAllUnits<T> ()
        {
            string query = string.Format (@"
                SELECT DISTINCT t.{0} FROM
                (SELECT DISTINCT {1} as {0} FROM goods
                UNION ALL
                SELECT DISTINCT {2} as {0} FROM goods) as t",
                fieldsTable.GetFieldAlias (DataField.ItemMeasUnit),
                fieldsTable.GetFieldName (DataField.ItemMeasUnit),
                fieldsTable.GetFieldName (DataField.ItemMeasUnit2));

            return ExecuteLazyModel<T> (query);
        }

        public override ObjectsContainer<string, string> GetUnitsForItem (long itemId)
        {
            string query = string.Format ("SELECT {0} as Value1, {1} as Value2 FROM goods WHERE ID = @ID",
                fieldsTable.GetFieldName (DataField.ItemMeasUnit),
                fieldsTable.GetFieldName (DataField.ItemMeasUnit2));

            return ExecuteObject<ObjectsContainer<string, string>> (query, new DbParam ("ID", itemId));
        }

        #endregion
    }
}
