//
// DataProvider.Waste.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   10.12.2007
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

        public override LazyListModel<T> GetAllWastes<T> (DataQuery dataQuery)
        {
            string query = string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, objects.Code AS {7},
                  users1.Name as {3}, users1.Name2 as {4}, users2.Name as {5}, users2.Name2 as {6}
                FROM (((operations INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.OperType = 3 AND operations.PartnerID <> 0
                GROUP BY Acct", OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2),
                fieldsTable.GetFieldAlias (DataField.LocationCode));

            return ExecuteDataQuery<T> (dataQuery, query);
        }

        #endregion

        #region Save

        public override void AddUpdateWaste (object wasteObject, object [] wasteDetailObjects, bool allowNegativeQty)
        {
            ItemsManagementType type = GetItemsManagementType ();
            foreach (object detail in wasteDetailObjects)
                AddUpdateDetail (wasteObject, detail, allowNegativeQty, type);
        }

        #endregion

        #region Reports

        public override DataQueryResult ReportWastes (DataQuery querySet, bool usePriceIn)
        {
            string priceColumns = usePriceIn ?
                GetOperationPriceInColumns (querySet) :
                GetOperationPriceOutColumns (querySet);

            string priceGroups = usePriceIn ?
                "operations.PriceIn, operations.VATIn" :
                "operations.PriceOut, operations.VATOut";

            string query = string.Format (@"
                SELECT operations.Acct as {0}, operations.Date as {1}, 
                  goods.Code as {2}, goods.Name as {3}, goods.BarCode1, goods.BarCode2, goods.BarCode3, 
                  goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name,
                  objects.Name as {4}, objectsgroups.Name, users1.Name as {5},{9} goods.Measure1, 
                  operations.Qtty as {6}, {7},
                  operations.Note as {8}
                FROM ((((((operations LEFT JOIN goods ON operations.GoodID=goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)                 
                  LEFT JOIN objects ON operations.ObjectID=objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups ON ABS(users1.GroupID) = usersgroups.ID){10}
                WHERE operations.OperType = 3 AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY operations.ID, operations.Acct, operations.Date, goods.Code, goods.Name,
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  objects.Name, users1.Name, goods.Measure1, operations.Qtty, {12},
                  operations.Note, objects.Code, users1.Code{11}",
                fieldsTable.GetFieldAlias (DataField.OperationNumber),
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationDetailQuantity),
                priceColumns,
                fieldsTable.GetFieldAlias (DataField.OperationDetailNote),
                GetReportLotSelect (querySet),
                GetReportLotJoin (querySet),
                GetReportLotGroup (querySet),
                priceGroups);

            querySet.SetComplexId (DbTable.Operations, DataField.OperationNumber, DataField.OperationType, OperationType.Waste);

            return ExecuteDataQuery (querySet, query);
        }

        #endregion
    }
}
