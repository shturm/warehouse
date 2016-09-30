//
// DataProvider.Purchase.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
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

using System.Collections.Generic;
using System.Data;
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetAllPurchases<T> (DataQuery dataQuery, bool onlyUninvoiced)
        {
            fieldsTable.UseAltNames = true;
            string query = string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, partners.Company as {3}, partners.Company2 as {4}, 
                  users1.Name as {5}, users1.Name2 as {6}, users2.Name as {7}, users2.Name2 as {8},
                  {10} AS {11}, {12} AS {13}, objects.Code AS {14}, partners.Code AS {15}
                FROM (((((operations INNER JOIN partners ON operations.PartnerID = partners.ID) INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                  LEFT JOIN documents ON operations.Acct = documents.Acct AND operations.OperType = documents.OperType)
                WHERE {9}operations.OperType = 1 AND operations.PartnerID <> 0
                GROUP BY operations.Acct", OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2),
                onlyUninvoiced ? "documents.InvoiceNumber IS NULL AND " : string.Empty,
                fieldsTable.GetFieldFullName (DataField.OperationTotal),
                fieldsTable.GetFieldAlias (DataField.OperationTotal),
                fieldsTable.GetFieldFullName (DataField.OperationVatSum),
                fieldsTable.GetFieldAlias (DataField.OperationVatSum),
                fieldsTable.GetFieldAlias (DataField.LocationCode),
                fieldsTable.GetFieldAlias (DataField.PartnerCode));
            fieldsTable.UseAltNames = false;

            return ExecuteDataQuery<T> (dataQuery, query);
        }

        public override Dictionary<long, long> GetAllPurchaseIdsWithInvoices ()
        {
            return ExecuteDictionary<long, long> (@"
                SELECT operations.Acct, MAX(CASE WHEN InvoiceNumber REGEXP '^[0-9]+$' THEN CAST(InvoiceNumber AS UNSIGNED) ELSE -1 END) as InvNumber
                FROM (operations INNER JOIN documents ON documents.OperType = operations.OperType AND documents.Acct = operations.Acct)
                WHERE operations.OperType = 1
                GROUP BY operations.Acct");
        }

        #endregion

        #region Save

        public override void AddUpdatePurchase (object purchaseObject, object [] purchaseDetailObjects, bool allowNegativeQty, DataField priceOutField)
        {
            ItemsManagementType type = GetItemsManagementType ();
            foreach (object detail in purchaseDetailObjects)
                AddUpdateDetail (purchaseObject, detail, allowNegativeQty, type, priceOutField);
        }

        #endregion

        #region Reports

        public override DataQueryResult ReportPurchases (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT operations.Acct as {0}, operations.Date as {1}, goods.Code as {2}, goods.Name as {3}, goods.BarCode1, goods.BarCode2, goods.BarCode3,
                  goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name, 
                  partners.Company, partnersgroups.Name, objects.Name as {4}, objectsgroups.Name, 
                  users1.Name as {5}, usersgroups1.Name,{12}
                  operations.Qtty as {6}, operations.PriceIn as {7}, operations.VATIn as {8},
                  (operations.Qtty * operations.PriceIn) as {9},
                  (operations.Qtty * operations.VATIn) as {10},
                  (operations.Qtty * (operations.PriceIn{14})) as {11},
                  operations.Note as {15}
                FROM ((((((((operations INNER JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                  INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN goods ON operations.GoodID = goods.ID){13}
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                WHERE operations.OperType = 1 AND operations.PartnerID <> 0 AND operations.Acct > 0",
                fieldsTable.GetFieldAlias (DataField.OperationNumber),
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationDetailQuantity),
                fieldsTable.GetFieldAlias (DataField.OperationDetailPriceIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailVatIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumVatIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailTotalIn),
                GetReportLotSelect (querySet), GetReportLotJoin (querySet),
                querySet.VATIsIncluded ? string.Empty : " + operations.VATIn",
                fieldsTable.GetFieldAlias (DataField.OperationDetailNote));

            querySet.SetComplexId (DbTable.Operations, DataField.OperationNumber, DataField.OperationType, OperationType.Purchase);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportPurchasesByItems (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT goods.ID, operations.Date as {0}, goods.Code as {1}, goods.Name as {2},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.Name,{8} objects.Name as {3}, objectsgroups.Name, 
                  users1.Name as {4}, usersgroups1.Name, partners.Company, partnersgroups.Name,
                  SUM(operations.Qtty) as {5},
                  SUM(operations.Qtty * operations.PriceIn) as {6},
                  SUM(operations.Qtty * operations.VatIn) as {7}
                FROM ((((((((operations LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID){9}
                WHERE operations.OperType = 1 AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY goods.Code, operations.Date, goods.ID, goods.Name,
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name,
                  objects.Name, objectsgroups.Name, users1.Name, usersgroups1.Name, partners.Company, partnersgroups.Name, operations.OperType{10}",
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationQuantitySum),
                fieldsTable.GetFieldAlias (DataField.OperationSum),
                fieldsTable.GetFieldAlias (DataField.OperationVatSum),
                GetReportLotSelect (querySet), GetReportLotJoin (querySet), GetReportLotGroup (querySet));

            querySet.SetSimpleId (DbTable.Items, DataField.ItemId, 0);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportPurchasesByPartners (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT partners.ID, operations.Date as {0}, goods.Code as {1}, goods.Name as {2},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.Name,{8} objects.Name as {3}, objectsgroups.Name, 
                  users1.Name as {4}, usersgroups1.Name, partners.Company, partnersgroups.Name,
                  SUM(operations.Qtty) as {5},
                  SUM(operations.Qtty * operations.PriceIn) as {6},
                  SUM(operations.Qtty * operations.VatIn) as {7}
                FROM ((((((((operations LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID){9}
                WHERE operations.OperType = 1 AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY goods.Code, operations.Date, goods.Name,
                 goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name,
                 objects.Name, objectsgroups.Name, users1.Name, usersgroups1.Name, partners.ID, partners.Company, partnersgroups.Name, operations.OperType{10}",
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationQuantitySum),
                fieldsTable.GetFieldAlias (DataField.OperationSum),
                fieldsTable.GetFieldAlias (DataField.OperationVatSum),
                GetReportLotSelect (querySet), GetReportLotJoin (querySet), GetReportLotGroup (querySet));

            querySet.SetSimpleId (DbTable.Partners, DataField.PartnerId, 0);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportPurchasesByLocations (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT objects.ID, operations.Date as {0}, goods.Code as {1}, goods.Name as {2},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.Name,{8}
                  objects.Name as {3}, objectsgroups.Name, users1.Name as {4}, usersgroups1.Name, partners.Company, partnersgroups.Name,
                  SUM(operations.Qtty) as {5},
                  SUM(operations.Qtty * operations.PriceIn) as {6},
                  SUM(operations.Qtty * operations.VatIn) as {7}
                FROM ((((((((operations LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID){9}
                WHERE operations.OperType = 1 AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY goods.Code, operations.Date, goods.Name,
                 goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name,
                 objects.ID, objects.Name, objectsgroups.Name, users1.Name, usersgroups1.Name, partners.Company, partnersgroups.Name, operations.OperType{10}",
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationQuantitySum),
                fieldsTable.GetFieldAlias (DataField.OperationSum),
                fieldsTable.GetFieldAlias (DataField.OperationVatSum),
                GetReportLotSelect (querySet), GetReportLotJoin (querySet), GetReportLotGroup (querySet));

            querySet.SetSimpleId (DbTable.Objects, DataField.LocationId, 0);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportPurchasesByTotal (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT operations.Acct as {0}, operations.Date as {1}, goods.Code as {2}, goods.Name as {3},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.Name,{8}
                  objects.Name as {4}, objectsgroups.Name, partners.Company, partnersgroups.Name, 
                  users1.Name as {5}, usersgroups1.Name,
                  SUM(operations.Qtty * operations.PriceIn) as {6},
                  SUM(operations.Qtty * operations.VatIn) as {7},
                  SUM(operations.Qtty * (operations.PriceIn{11})) as {12}
                FROM ((((((((operations LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                  LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID){9}
                WHERE operations.OperType = 1 AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY operations.Acct, operations.Date, goods.Code, goods.Name, goods.BarCode1, goods.BarCode2, goods.BarCode3, 
                  goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name, 
                  objects.Name, objectsgroups.Name, users1.Name, usersgroups1.Name, partners.Company, partnersgroups.Name{10}",
                fieldsTable.GetFieldAlias (DataField.OperationNumber),
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.PurchaseSum),
                fieldsTable.GetFieldAlias (DataField.OperationVatSum),
                GetReportLotSelect (querySet), GetReportLotJoin (querySet), GetReportLotGroup (querySet),
                querySet.VATIsIncluded ? string.Empty : " + operations.VATIn",
                fieldsTable.GetFieldAlias (DataField.PurchaseTotal));

            querySet.SetComplexId (DbTable.Operations, DataField.OperationNumber, DataField.OperationType, OperationType.Purchase);

            return ExecuteDataQuery (querySet, query);
        }

        #endregion
    }
}
