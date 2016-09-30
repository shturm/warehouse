//
// DataProvider.Sale.cs
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

using System.Collections.Generic;
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetAllSales<T> (DataQuery dataQuery, OperationType saleType = OperationType.Any)
        {
            string query = string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, partners.Company as {3}, 
                  partners.Company2 as {4}, users1.Name as {5}, users1.Name2 as {6}, users2.Name as {7}, users2.Name2 as {8},
                  objects.Code AS {9}, partners.Code AS {10}, SUM({12}) as {13}
                FROM ((((operations INNER JOIN partners ON operations.PartnerID = partners.ID) INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE ({11}) AND operations.PartnerID <> 0
                GROUP BY Acct", OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2),
                fieldsTable.GetFieldAlias (DataField.LocationCode),
                fieldsTable.GetFieldAlias (DataField.PartnerCode),
                saleType == OperationType.Any ? "operations.OperType = 2 OR operations.OperType = 16" :
                    string.Format ("operations.OperType = {0}", (int) saleType),
                GetOperationDetailTotalOut (dataQuery),
                fieldsTable.GetFieldAlias (DataField.OperationTotal));

            return ExecuteDataQuery<T> (dataQuery, query);
        }

        public override LazyListModel<T> GetAllUninvoicedSales<T> (DataQuery dataQuery, bool fullyPaid = false)
        {
            fieldsTable.UseAltNames = true;
            string query = string.Format (@"
                {0}, SUM(operations.PriceIn * operations.Qtty) AS {1}, SUM(operations.VatIn * operations.Qtty) AS {2}
                FROM (operations INNER JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN documents ON operations.Acct = documents.Acct AND operations.OperType = documents.OperType
                WHERE (operations.OperType = 2 OR operations.OperType = 16) AND documents.InvoiceNumber IS NULL AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY operations.Acct
                {3}",
                GetSelect (new [] { DataField.OperationNumber, DataField.OperationType, DataField.OperationPartnerId,
                    DataField.OperationDate, DataField.OperationDetailNote, DataField.OperationTotal, DataField.OperationVatSum }),
			    fieldsTable.GetFieldAlias (DataField.PurchaseTotal),
			    fieldsTable.GetFieldAlias (DataField.PurchaseVATSum),
                fullyPaid ? 
                string.Format (@"HAVING (
                  SELECT ABS(SUM(payments.Qtty * payments.Mode))
                  FROM payments 
                  WHERE payments.Acct = {0} AND payments.OperType = {1}) < 0.0000001", 
                      fieldsTable.GetFieldAlias (DataField.OperationNumber), fieldsTable.GetFieldAlias (DataField.OperationType)) : string.Empty);
            fieldsTable.UseAltNames = false;

            return ExecuteDataQuery<T> (dataQuery, query);
        }

        public override T GetCashReport<T> (DataQuery dataQuery)
        {
            string query = string.Format (@"
                SELECT SUM(payments.Qtty) AS {0}
                FROM (payments INNER JOIN paymenttypes ON payments.Type = paymenttypes.ID) 
                  LEFT JOIN documents ON payments.Acct = documents.Acct AND payments.OperType = documents.OperType
                WHERE payments.OperType IN (2, 16) AND payments.Mode = 1 AND paymenttypes.PaymentMethod = 1
                  AND documents.InvoiceNumber IS NULL",
                fieldsTable.GetFieldAlias (DataField.OperationTotal));

            LazyListModel<T> model = ExecuteDataQuery<T> (dataQuery, query);
            return model.Count > 0 ? model [0] : null;
        }

        public override Dictionary<long, long> GetAllSaleIdsWithInvoices ()
        {
            return ExecuteDictionary<long, long> (@"
                SELECT Acct, MAX(CASE WHEN InvoiceNumber REGEXP '^[0-9]+$' THEN CAST(InvoiceNumber AS UNSIGNED) ELSE -1 END) as InvNumber
                FROM documents
                WHERE OperType = 2 OR OperType = 16
                GROUP BY Acct");
        }

        public override T GetSaleById<T> (long saleId)
        {
            string query = string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, 
                  partners.Company as {3}, partners.Company2 as {4}, 
                  users1.Name as {5}, users1.Name2 as {6}, users2.Name as {7}, users2.Name2 as {8},
                  partners.Code AS {9}
                FROM ((((operations INNER JOIN partners ON operations.PartnerID = partners.ID) INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE (operations.OperType = 2 OR operations.OperType = 16) AND Acct = @saleId
                GROUP BY Acct", OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2),
                fieldsTable.GetFieldAlias (DataField.PartnerCode));

            return ExecuteObject<T> (query, new DbParam ("saleId", saleId));
        }

        public override T GetSaleByNote<T> (string note)
        {
            string query = string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, 
                  partners.Company as {3}, partners.Company2 as {4}, 
                  users1.Name as {5}, users1.Name2 as {6}, users2.Name as {7}, users2.Name2 as {8},
                  partners.Code AS {9}
                FROM ((((operations INNER JOIN partners ON operations.PartnerID = partners.ID) INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE (operations.OperType = 2 OR operations.OperType = 16) AND Note = @note
                GROUP BY Acct", OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2),
                fieldsTable.GetFieldAlias (DataField.PartnerCode));

            return ExecuteObject<T> (query, new DbParam ("note", note));
        }

        public override T GetPendingSale<T> (long partnerId, long locationId)
        {
            string query = string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, 
                  partners.Company as {3}, partners.Company2 as {4}, 
                  users1.Name as {5}, users1.Name2 as {6}, users2.Name as {7}, users2.Name2 as {8},
                  partners.Code AS {9}
                FROM ((((operations INNER JOIN partners ON operations.PartnerID = partners.ID) INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE (operations.OperType = 2 OR operations.OperType = 16) AND Acct = @operationId AND ObjectID = @locationId AND PartnerID = @partnerId
                GROUP BY Acct", OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2),
                fieldsTable.GetFieldAlias (DataField.PartnerCode));

            return ExecuteObject<T> (query,
                new DbParam ("operationId", (int) OperationState.Pending),
                new DbParam ("locationId", locationId),
                new DbParam ("partnerId", partnerId));
        }

        #endregion

        #region Save

        public override void AddUpdateSale (object saleObject, object [] saleDetailObjects, bool allowNegativeQty, long childLocationId)
        {
            ItemsManagementType type = GetItemsManagementType ();
            foreach (object detail in saleDetailObjects)
                AddUpdateDetail (saleObject, detail, allowNegativeQty, type, childLocationId: childLocationId);
        }

        #endregion

        #region Reports

        public override DataQueryResult ReportSales (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT operations.Acct as {0}, operations.Date as {1}, 
                  goods.Code as {2}, goods.Name as {3}, goods.BarCode1, goods.BarCode2, goods.BarCode3,
                  goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name, 
                  partners.Company, partnersgroups.Name, objects.Name as {4}, objectsgroups.Name, 
                  users1.Name as {5}, usersgroups1.Name,{12}
                  operations.Qtty as {6}, operations.PriceOut as {7}, operations.VATOut as {8},
                  (operations.Qtty * operations.PriceOut) as {9},
                  ({15}) as {10},
                  ({14}) as {11},
                  operations.Note as {16}
                FROM (((((((((operations INNER JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                  INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  {13})
                WHERE (operations.OperType = 2 OR operations.OperType = 16) AND operations.PartnerID <> 0 AND operations.Acct > 0",
                fieldsTable.GetFieldAlias (DataField.OperationNumber),
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationDetailQuantity),
                fieldsTable.GetFieldAlias (DataField.OperationDetailPriceOut),
                fieldsTable.GetFieldAlias (DataField.OperationDetailVatOut),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumOut),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumVatOut),
                fieldsTable.GetFieldAlias (DataField.OperationDetailTotalOut),
                GetReportLotSelect (querySet),
                GetReportLotJoin (querySet),
                GetOperationDetailTotalOut (querySet),
                GetOperationDetailVatOutSum (querySet),
                fieldsTable.GetFieldAlias (DataField.OperationDetailNote));

            querySet.SetComplexId (DbTable.Operations, DataField.OperationNumber, DataField.OperationType, OperationType.Sale);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportSalesByItems (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT goods.ID, operations.Date as {0}, goods.Code as {1}, goods.Name as {2},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.Name, {8} objects.Name as {3}, objectsgroups.Name, 
                  users1.Name as {4}, usersgroups1.Name, partners.Company, partnersgroups.Name,
                  SUM(operations.Qtty) as {5},
                  SUM(operations.Qtty * operations.PriceOut) as {6},
                  SUM(operations.Qtty * operations.VatOUT) as {7}
                FROM ((((((((operations LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID) {9}
                WHERE (operations.OperType = 2 OR operations.OperType = 16) AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY operations.Date, goods.ID, goods.Code, goods.Name,
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

        public override DataQueryResult ReportSalesByPartners (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT partners.ID, operations.Date as {0}, goods.Code as {1}, goods.Name as {2},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.Name,{8} objects.Name as {3}, objectsgroups.Name,
                  users1.Name as {4}, usersgroups1.Name, partners.Company, partnersgroups.Name,
                  SUM(operations.Qtty) as {5},
                  SUM(operations.Qtty * operations.PriceOUT) as {6},
                  SUM(operations.Qtty * operations.VatOUT) as {7}
                FROM ((((((((operations LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID) {9}
                WHERE (operations.OperType = 2 OR operations.OperType = 16) AND operations.PartnerID <> 0 AND operations.Acct > 0
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

        public override DataQueryResult ReportSalesByLocations (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT objects.ID, operations.Date as {0}, goods.Code as {1}, goods.Name as {2},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.Name,{8} objects.Name as {3}, objectsgroups.Name,
                  users1.Name as {4}, partners.Company, partnersgroups.Name,
                  SUM(operations.Qtty) as {5},
                  SUM(operations.Qtty * operations.PriceOut) as {6},
                  SUM(operations.Qtty * operations.VatOut) as {7}
                FROM (((((((operations LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID) {9}
                WHERE (operations.OperType = 2 OR operations.OperType = 16) AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY goods.Code, operations.Date, goods.Name,
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name,
                  objects.ID, objects.Name, objectsgroups.Name, users1.Name, partners.Company, partnersgroups.Name, operations.OperType{10}",
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

        public override DataQueryResult ReportSalesByTotal (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT operations.Acct as {0}, operations.Date as {1}, goods.Code as {2}, goods.Name as {3},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.Name,{8} objects.Name as {4}, objectsgroups.Name, 
                  partners.Company, partnersgroups.Name, users1.Name as {5}, usersgroups1.Name,
                  SUM(operations.Qtty * operations.PriceOut) as {6},
                  SUM(operations.Qtty * operations.VatOut) as {7},
                  SUM({12}) as {11}
                FROM ((((((((operations LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                  LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID) {9}
                WHERE (operations.OperType = 2 OR operations.OperType = 16) AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY operations.Acct, operations.Date, partners.Company, partnersgroups.Name, 
                  goods.Code, goods.Name, goods.BarCode1, goods.BarCode2, goods.BarCode3, 
                  goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name{10}",
                fieldsTable.GetFieldAlias (DataField.OperationNumber),
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.SaleSum),
                fieldsTable.GetFieldAlias (DataField.OperationVatSum),
                GetReportLotSelect (querySet), GetReportLotJoin (querySet), GetReportLotGroup (querySet),
                fieldsTable.GetFieldAlias (DataField.SaleTotal),
                GetOperationDetailTotalOut (querySet));

            querySet.SetComplexId (DbTable.Operations, DataField.OperationNumber, DataField.OperationType, OperationType.Sale);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportSalesByUser (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT objects.Name as {0}, IFNULL(objects.Name2, objects.Name) as {1},
                  partners.Company as {2}, IFNULL(partners.Company2, partners.Company) as {3},
                  users1.Name as {4}, IFNULL(users1.Name2, users1.Name) as {5},
                  users2.Name as {6}, IFNULL(users2.Name2, users2.Name) as {7},
                  SUM({11}) as {8},
                  operations.Acct as {9}, operations.UserRealTime as {10}
                FROM ((((operations INNER JOIN partners ON operations.PartnerID = partners.ID) INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE (operations.OperType = 2 OR operations.OperType = 16) AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY operations.Acct, objects.Name, objects.Name2, partners.Company, partners.Company2",
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2),
                fieldsTable.GetFieldAlias (DataField.OperationTotal),
                fieldsTable.GetFieldAlias (DataField.OperationNumber),
                fieldsTable.GetFieldAlias (DataField.OperationTimeStamp),
                GetOperationDetailTotalOut (querySet));

            querySet.SetComplexId (DbTable.Operations, DataField.OperationNumber, DataField.OperationType, OperationType.Sale);

            return ExecuteDataQuery (querySet, query);
        }

        #endregion
    }
}
