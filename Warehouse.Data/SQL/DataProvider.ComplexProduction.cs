//
// DataProvider.ComplexProduction.cs
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

        public override LazyListModel<T> GetAllComplexProductions<T> (DataQuery dataQuery)
        {
            string query = string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, users1.Name as {3}, users1.Name2 as {4}, users2.Name as {5}, users2.Name2 as {6},
                  objects.Code AS {7}
                FROM (((operations INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.OperType = 24 AND operations.PartnerID <> 0
                GROUP BY Acct", ComplexProductionDefaultAliases (),
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2),
                fieldsTable.GetFieldAlias (DataField.LocationCode));

            return ExecuteDataQuery<T> (dataQuery, query);
        }

        public override LazyListModel<T> GetComplexProductionMatDetailsById<T> (long id, DataField operPriceGroup)
        {
            string query = string.Format (@"
                SELECT {0}, goods.Measure1 as {1}, goods.Name as {2}, goods.Name2 as {3}, operations.PriceIn * operations.Qtty as {4}, {5} as {6},
                  lots.SerialNo as {7}, lots.EndDate as {8}, lots.ProductionDate as {9}, lots.Location as {10}
                FROM operations INNER JOIN goods ON operations.GoodID = goods.ID LEFT JOIN lots on operations.LotID = lots.ID
                WHERE operations.OperType = 24 AND Acct = @ComplexProductionId", ComplexProductionDefaultAliases (),
                fieldsTable.GetFieldAlias (DataField.ItemMeasUnit),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.ItemName2),
                fieldsTable.GetFieldAlias (DataField.OperationDetailTotal),
                fieldsTable.GetFieldFullName (operPriceGroup),
                fieldsTable.GetFieldAlias (DataField.OperationDetailPriceOut),
                fieldsTable.GetFieldAlias (DataField.LotSerialNumber),
                fieldsTable.GetFieldAlias (DataField.LotExpirationDate),
                fieldsTable.GetFieldAlias (DataField.LotProductionDate),
                fieldsTable.GetFieldAlias (DataField.LotLocation));

            return ExecuteLazyModel<T> (query, new DbParam ("ComplexProductionId", id));
        }

        public override LazyListModel<T> GetComplexProductionProdDetailsById<T> (long id, DataField operPriceGroup)
        {
            string query = string.Format (@"
                SELECT {0}, goods.Measure1 as {1}, goods.Name as {2}, goods.Name2 as {3}, operations.PriceIn * operations.Qtty as {4}, {5} as {6},
                  lots.SerialNo as {7}, lots.EndDate as {8}, lots.ProductionDate as {9}, lots.Location as {10}
                FROM operations INNER JOIN goods ON operations.GoodID = goods.ID LEFT JOIN lots on operations.LotID = lots.ID
                WHERE operations.OperType = 25 AND Acct = @ComplexProductionId", ComplexProductionDefaultAliases (),
                fieldsTable.GetFieldAlias (DataField.ItemMeasUnit),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.ItemName2),
                fieldsTable.GetFieldAlias (DataField.OperationDetailTotal),
                fieldsTable.GetFieldFullName (operPriceGroup),
                fieldsTable.GetFieldAlias (DataField.OperationDetailPriceOut),
                fieldsTable.GetFieldAlias (DataField.LotSerialNumber),
                fieldsTable.GetFieldAlias (DataField.LotExpirationDate),
                fieldsTable.GetFieldAlias (DataField.LotProductionDate),
                fieldsTable.GetFieldAlias (DataField.LotLocation));

            return ExecuteLazyModel<T> (query, new DbParam ("ComplexProductionId", id));
        }

        #endregion

        #region Save

        public override void AddUpdateComplexProductionMat (object obj, object [] detailObj, bool allowNegativeQty)
        {
            ItemsManagementType type = GetItemsManagementType ();
            foreach (object detail in detailObj)
                AddUpdateDetail (obj, detail, allowNegativeQty, type);
        }

        public override void AddUpdateComplexProductionProd (object obj, object [] detailObj, bool allowNegativeQty)
        {
            ItemsManagementType type = GetItemsManagementType ();
            foreach (object detail in detailObj)
                AddUpdateDetail (obj, detail, allowNegativeQty, type);
        }

        #endregion

        #region Reports

        public override DataQueryResult ReportComplexProductions (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT operations.Acct as {0}, operations.Date as {1}, goods.Code as {2}, goods.Name as {3},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.name as {4}, objects.Name as {5}, objectsgroups.Name AS {6}, 
                  users1.Name as {7}, usersgroups1.Name AS {8},{13}
                  operations.Qtty as {9}, goods.Measure1, operations.PriceIn as {10},
                  (operations.Qtty * operations.PriceIn) as {11},
                  operations.OperType as {12},
                  operations.Note as {15}
                FROM ((((((((operations LEFT JOIN goods ON operations.GoodID=goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN operationtype ON operations.OperType = operationtype.ID){14}
                WHERE (operations.OperType = 24 OR operations.OperType = 25) AND operations.PartnerID <> 0 AND operations.Acct > 0",
                fieldsTable.GetFieldAlias (DataField.OperationNumber),
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.ItemsGroupName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.LocationsGroupsName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorsGroupsName),
                fieldsTable.GetFieldAlias (DataField.OperationDetailQuantity),
                fieldsTable.GetFieldAlias (DataField.OperationDetailPriceIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumIn),
                fieldsTable.GetFieldAlias (DataField.OperationType),
                GetReportLotSelect (querySet),
                GetReportLotJoin (querySet),
                fieldsTable.GetFieldAlias (DataField.OperationDetailNote));

            querySet.SetComplexId (DbTable.Operations, DataField.OperationNumber, DataField.OperationType, OperationType.ComplexProductionMaterial);

            return ExecuteDataQuery (querySet, query);
        }

        #endregion

        private string ComplexProductionDefaultAliases ()
        {
            return GetAliasesString (DataField.OperationType,
                DataField.OperationNumber,
                DataField.OperationLocationId,
                DataField.OperationPartnerId,
                DataField.OperationOperatorId,
                DataField.OperationUserId,
                DataField.OperationTimeStamp,
                DataField.OperationDate,
                DataField.OperationDetailId,
                DataField.OperationDetailItemId,
                DataField.OperationDetailQuantity,
                DataField.OperationDetailPriceIn,
                /*DataField.OperationDetailPriceOut,*/
                DataField.OperationDetailDiscount,
                DataField.OperationDetailCurrencyId,
                DataField.OperationDetailCurrencyRate,
                DataField.OperationDetailReferenceId,
                DataField.OperationDetailLot,
                DataField.OperationDetailNote,
                DataField.OperationDetailVatIn,
                DataField.OperationDetailVatOut,
                DataField.OperationDetailSign,
                DataField.OperationDetailLotId);
        }
    }
}
