//
// DataProvider.StockTaking.cs
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

using System;
using System.Linq;
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetAllStockTakings<T> (DataQuery dataQuery)
        {
            string query = string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, partners.Company as {3}, partners.Company2 as {4}, 
                  users1.Name as {5}, users1.Name2 as {6}, users2.Name as {7}, users2.Name2 as {8}, objects.Code AS {9}
                FROM ((((operations INNER JOIN partners ON operations.PartnerID = partners.ID)
                  INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.OperType = 4 AND operations.PartnerID <> 0
                GROUP BY Acct", OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2),
                fieldsTable.GetFieldAlias (DataField.LocationCode));

            return ExecuteDataQuery<T> (dataQuery, query);
        }

        #endregion

        #region Save

        public override void AddUpdateStockTaking (object stockTakingObject, object [] stockTakingDetailObjects, bool allowNegativeQty, DataField priceOutField, bool annul)
        {
            ItemsManagementType type = GetItemsManagementType ();
            foreach (object detail in stockTakingDetailObjects)
                AddUpdateStockTakingDetail (stockTakingObject, detail, allowNegativeQty, priceOutField, type, annul);
        }

        private void AddUpdateStockTakingDetail (object stockTakingObject, object stockTakingDetailObjects, 
            bool allowNegativeQty, DataField priceOutField, ItemsManagementType imt, bool annul)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (stockTakingObject);
            helper.AddObject (stockTakingDetailObjects);

            bool usesLots = imt != ItemsManagementType.AveragePrice &&
                imt != ItemsManagementType.QuickAveragePrice &&
                imt != ItemsManagementType.LastPurchasePrice;

            // Check if we already have that detail
            var oldInfo = ExecuteObject<ObjectsContainer<int, long, long, double>> ("SELECT Sign as Value1, ObjectID as Value2, GoodID as Value3, Qtty as Value4 FROM operations WHERE ID = @ID", helper.Parameters);
            // Get the quantity from the new detail
            double newGoodsQty = (double) helper.GetObjectValue (DataField.OperationDetailQuantity);
            
            long result;

            // We are updating detail information
            if (oldInfo != null) {
                int oldSign = oldInfo.Value1;
                long oldLocationId = oldInfo.Value2;
                long oldGoodsId = oldInfo.Value3;
                double oldGoodsQty = oldInfo.Value4;

                long newLotId;
                string newLot;
                double newPrice;
                long oldLotId;
                string oldLot;
                double oldPrice;
                GetOperationDetailLotInfo (helper, usesLots, newGoodsQty, out newLotId, out newLot, out newPrice, out oldLotId, out oldLot, out oldPrice);

                // unlike other operations, stock-takings are allowed to have zero quantity
                if (annul) {
                    // Update the operation row with the changes
                    result = ExecuteNonQuery ("DELETE FROM operations WHERE ID = @ID", helper.Parameters);
                } else {
                    // Update the operation row with the changes
                    result = ExecuteNonQuery (string.Format ("UPDATE operations {0} WHERE ID = @ID",
                        helper.GetSetStatement (OperationNonDBFields.Union (new [] { DataField.OperationDetailId }).ToArray ())), helper.Parameters);
                }

                if (result != 1)
                    throw new Exception ("Unable to update operation detail.");

                int sign = (int) helper.GetObjectValue (DataField.OperationDetailSign);
                if (sign == 0)
                    return;

                if (oldSign == 0)
                    oldGoodsQty = 0;

                helper.AddParameters (new DbParam ("oldGoodsQty", oldGoodsQty));
                helper.AddParameters (new DbParam ("oldLocationId", oldLocationId));
                helper.AddParameters (new DbParam ("oldGoodsId", oldGoodsId));

                ExecuteNonQuery (string.Format ("UPDATE goods SET PriceIn = @PriceIn, {0} = @PriceOut WHERE ID = @GoodID", fieldsTable.GetFieldName (priceOutField)),
                    helper.Parameters);

                #region Update location items availability on hand

                if (usesLots) {
                    if (!newGoodsQty.IsEqualTo (oldGoodsQty) ||
                        !newPrice.IsEqualTo (oldPrice) || newLot != oldLot || newLotId != oldLotId || annul) {
                        // Set the new quantity in the store
                        if (!newGoodsQty.IsZero ()) {
                            result = ExecuteNonQuery (string.Format (@"UPDATE store
                                SET Qtty = Qtty + @Qtty
                                WHERE ObjectID = @ObjectID AND GoodID = @GoodID AND Lot = @Lot AND LotID = @LotID AND ABS(Price - @PriceIn) < {0}", PurchaseCurrencyPrecisionString), helper.Parameters);

                            if (result == 0) {
                                result = ExecuteScalar<long> (@"SELECT IFNULL(MAX(LotOrder), 0)
                                    FROM store
                                    WHERE ObjectID = @ObjectID AND GoodID = @GoodID", helper.Parameters);
                                helper.AddParameters (new DbParam ("newLotOrder", result + 1));
                                if ((double) helper.GetObjectValue (DataField.OperationDetailQuantity) > 0)
                                    ExecuteNonQuery (@"INSERT INTO store (ObjectID, GoodID, Lot, LotID, Price, Qtty, LotOrder)
                                        VALUES(@ObjectID, @GoodID, @Lot, @LotID, @PriceIn, @Qtty, @newLotOrder)", helper.Parameters);   
                            }
                        }

                        helper.AddParameters (new DbParam ("oldPrice", oldPrice));
                        helper.AddParameters (new DbParam ("oldLot", oldLot));
                        helper.AddParameters (new DbParam ("oldLotId", oldLotId));

                        // a stock-taking may have deleted a lot by nullifying the quantity; check for the lot and restore it if needed
                        object lot = ExecuteScalar (@"SELECT ID FROM store WHERE ObjectID = @oldLocationId AND GoodID = @oldGoodsId AND Lot = @oldLot 
                            AND LotID = @oldLotId AND Price = @oldPrice", helper.Parameters);
                        if (lot != null) {
                            result = ExecuteNonQuery (@"UPDATE store
                                SET Qtty = Qtty - @oldGoodsQty
                                WHERE ObjectID = @oldLocationId AND GoodID = @oldGoodsId AND Lot = @oldLot AND LotID = @oldLotId AND 
                                      Price = @oldPrice AND " + GetQuantityCondition ("oldGoodsId", "oldGoodsQty", "oldLocationId", helper, -1), helper.Parameters);
                        } else {
                            if (oldGoodsQty <= 0 || allowNegativeQty) {
                                result = ExecuteScalar<long> (@"SELECT IFNULL(MAX(LotOrder), 0)
                                    FROM store
                                    WHERE ObjectID = @oldLocationId AND GoodID = @oldGoodsId", helper.Parameters);
                                helper.AddParameters (new DbParam ("newLotOrder", result + 1));
                                result = ExecuteNonQuery (@"INSERT INTO store (ObjectID, GoodID, Lot, LotID, Price, Qtty, LotOrder)
                                    VALUES(@oldLocationId, @oldGoodsId, @oldLot, @oldLotId, @oldPrice, @oldGoodsQty, @newLotOrder)", helper.Parameters);
                            } else
                                result = 0;
                        }

                        if (result != 1) {
                            string goodsName = ExecuteScalar<string> ("SELECT Name FROM goods WHERE ID = @oldGoodsId",
                                helper.Parameters);
                            throw new InsufficientItemAvailabilityException (goodsName);
                        }

                        // Delete the row if it has become with 0 quantity
                        ExecuteNonQuery (@"DELETE FROM store
                            WHERE ObjectID = @oldLocationId AND GoodID = @oldGoodsId AND Lot = @oldLot AND LotID = @oldLotId AND Price = @oldPrice AND ABS(Qtty) < 0.0000001", helper.Parameters);
                        DeleteLot (oldLotId);
                    }
                } else {
                    if (!newGoodsQty.IsEqualTo (oldGoodsQty) || annul) {
                        // Set the new price in store
                        ExecuteNonQuery ("UPDATE store SET Price = @PriceIn WHERE GoodID = @GoodID", helper.Parameters);

                        // Set the new quantity in the store
                        if (!newGoodsQty.IsZero ()) {
                            ExecuteNonQuery ("UPDATE store SET Qtty = Qtty + @Qtty WHERE ObjectID = @ObjectID AND GoodID = @GoodID",
                                helper.Parameters);
                        }

                        // Revert the old quantity in the store
                        if (allowNegativeQty) {
                            ExecuteNonQuery ("UPDATE store SET Qtty = Qtty - @oldGoodsQty WHERE ObjectID = @oldLocationId AND GoodID = @oldGoodsId",
                                helper.Parameters);
                        } else {
                            result = ExecuteNonQuery (@"UPDATE store SET Qtty = Qtty - @oldGoodsQty 
                                WHERE ObjectID = @oldLocationId AND GoodID = @oldGoodsId AND " +
                                GetQuantityCondition ("oldGoodsId", "oldGoodsQty", "oldLocationId", helper, -1),
                                helper.Parameters);

                            if (result != 1) {
                                string goodsName = ExecuteScalar<string> ("SELECT Name FROM goods WHERE ID = @oldGoodsId",
                                    helper.Parameters);
                                throw new InsufficientItemAvailabilityException (goodsName);
                            }
                        }
                    }
                }

                #endregion

            } // We are creating new detail information
            else if (!annul) {
                if (usesLots) {
                    long lotId = GetCreateLotId ((string) helper.GetObjectValue (DataField.LotSerialNumber),
                        (DateTime?) helper.GetObjectValue (DataField.LotExpirationDate),
                        (DateTime?) helper.GetObjectValue (DataField.LotProductionDate),
                        (string) helper.GetObjectValue (DataField.LotLocation), null);

                    helper.SetObjectValue (DataField.OperationDetailLotId, lotId);
                    helper.SetParameterValue (DataField.OperationDetailLotId, lotId);
                }

                // Insert the operation row
                result = ExecuteNonQuery (string.Format ("INSERT INTO operations {0}",
                    helper.GetColumnsAndValuesStatement (OperationNonDBFields.Union (new [] { DataField.OperationDetailId }).ToArray ())), helper.Parameters);

                if (result != 1)
                    throw new Exception ("Unable to insert operation detail.");

                result = GetLastAutoId ();
                helper.SetObjectValue (DataField.OperationDetailId, result);

                int sign = (int) helper.GetObjectValue (DataField.OperationDetailSign);
                if (sign == 0)
                    return;

                ExecuteNonQuery (string.Format ("UPDATE goods SET PriceIn = @PriceIn, {0} = @PriceOut WHERE ID = @GoodID", fieldsTable.GetFieldName (priceOutField)),
                    helper.Parameters);

                #region Update location items availability on hand

                if (usesLots) {
                    result = ExecuteNonQuery (string.Format (@"UPDATE store
                        SET Qtty = Qtty + @Qtty
                        WHERE ObjectID = @ObjectID AND GoodID = @GoodID AND Lot = @Lot AND LotID = @LotID AND ABS(Price - @PriceIn) < {0} AND Qtty + @Qtty + 0.0000001 > 0", PurchaseCurrencyPrecisionString), helper.Parameters);

                    if (result == 0) {
                        newGoodsQty = ExecuteScalar<double> (string.Format (@"SELECT IFNULL(MAX({0} + @Qtty), 0)
                            FROM store
                            WHERE ObjectID = @ObjectID AND GoodID = @GoodID AND Lot = @Lot AND LotID = @LotID AND ABS(Price - @PriceIn) < {1}", GetQuantity ("GoodID", "ObjectID", helper, -1), PurchaseCurrencyPrecisionString), helper.Parameters);

                        if (newGoodsQty < 0)
                            throw new InsufficientItemAvailabilityException ((string) helper.GetObjectValue (DataField.ItemName));

                        result = ExecuteScalar<long> (@"SELECT IFNULL(MAX(LotOrder), 0)
                            FROM store
                            WHERE ObjectID = @ObjectID AND GoodID = @GoodID", helper.Parameters);
                        helper.AddParameters (new DbParam ("newLotOrder", result + 1));

                        if ((double) helper.GetObjectValue (DataField.OperationDetailQuantity) > 0)
                            ExecuteNonQuery (@"INSERT INTO store (ObjectID, GoodID, Lot, LotID, Price, Qtty, LotOrder)
                                VALUES(@ObjectID, @GoodID, @Lot, @LotID, @PriceIn, @Qtty, @newLotOrder)", helper.Parameters);
                    } else {
                        // Delete the row if it has become with 0 quantity
                        ExecuteNonQuery (string.Format (@"DELETE FROM store
                            WHERE ObjectID = @ObjectID AND GoodID = @GoodID AND Lot = @Lot AND LotID = @LotID AND ABS(Price - @PriceIn) < {0} AND ABS(Qtty) < 0.0000001", PurchaseCurrencyPrecisionString), helper.Parameters);
                    }
                } else {
                    // Set the new quantity in the store
                    ExecuteNonQuery ("UPDATE store SET Price = @PriceIn WHERE GoodID = @GoodID", helper.Parameters);
                    if (allowNegativeQty) {
                        ExecuteNonQuery ("UPDATE store SET Qtty = Qtty + @Qtty WHERE GoodID = @GoodID AND ObjectID = @ObjectID",
                            helper.Parameters);
                    } else {
                        result = ExecuteNonQuery (string.Format (@"UPDATE store SET Qtty = Qtty + @Qtty 
							WHERE GoodID = @GoodID AND ObjectID = @ObjectID AND {0} + @Qtty + 0.0000001 > 0", GetQuantity ("GoodID", "ObjectID", helper, -1)),
                            helper.Parameters);

                        if (result != 1) {
                            string goodsName = ExecuteScalar<string> ("SELECT Name FROM goods WHERE ID = @GoodID", helper.Parameters);
                            throw new InsufficientItemAvailabilityException (goodsName);
                        }
                    }
                }

                #endregion
            } else
                throw new Exception ("Wrong number of operation details found with the given Id.");
        }

        #endregion

        #region Reports

        public override DataQueryResult ReportStockTakings (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT operations.Acct as {0}, operations.Date as {1}, 
                  goods.Code as {2}, goods.Name as {3}, goods.BarCode1, goods.BarCode2, goods.BarCode3, 
                  goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name,
                  objects.Name as {4}, objectsgroups.Name, users1.Name as {5}, usersgroups1.Name,{14}
                  CASE operations.SrcDocID WHEN 0 THEN NULL ELSE -(
                    SELECT Qtty FROM operations o2 
                    WHERE o2.OperType = operations.OperType AND o2.Acct = operations.Acct AND operations.LotID = o2.LotID AND
                     operations.GoodID = o2.GoodID AND operations.SrcDocID = 2 AND o2.SrcDocID = 1
                    LIMIT 1) END AS {6}, 
                  CASE operations.SrcDocID WHEN 0 THEN NULL ELSE operations.Qtty END AS {7}, 
                  CASE operations.SrcDocID WHEN 0 THEN operations.Qtty ELSE (
                    SELECT Qtty FROM operations o2 
                    WHERE o2.OperType = operations.OperType AND o2.Acct = operations.Acct AND operations.GoodID = o2.GoodID AND 
                     operations.LotID = o2.LotID AND operations.SrcDocID = 2 AND o2.SrcDocID = 1
                    LIMIT 1) - (-operations.Qtty) END AS {8},
                  operations.PriceOut as {9},
                  operations.VatOut as {10},
                  CASE operations.SrcDocID WHEN 0 THEN operations.Qtty ELSE (
                    SELECT Qtty FROM operations o2 
                    WHERE o2.OperType = operations.OperType AND o2.Acct = operations.Acct AND operations.GoodID = o2.GoodID AND 
                     operations.LotID = o2.LotID AND operations.SrcDocID = 2 AND o2.SrcDocID = 1
                    LIMIT 1) - (-operations.Qtty) END * operations.PriceOut AS {11},
                  CASE operations.SrcDocID WHEN 0 THEN operations.Qtty ELSE (
                    SELECT Qtty FROM operations o2 
                    WHERE o2.OperType = operations.OperType AND o2.Acct = operations.Acct AND operations.GoodID = o2.GoodID AND 
                     operations.LotID = o2.LotID AND operations.SrcDocID = 2 AND o2.SrcDocID = 1
                    LIMIT 1) - (-operations.Qtty) END * operations.VatOut AS {12},
                  CASE operations.SrcDocID WHEN 0 THEN operations.Qtty ELSE (
                    SELECT Qtty FROM operations o2 
                    WHERE o2.OperType = operations.OperType AND o2.Acct = operations.Acct AND operations.GoodID = o2.GoodID AND 
                     operations.LotID = o2.LotID AND operations.SrcDocID = 2 AND o2.SrcDocID = 1
                    LIMIT 1) - (-operations.Qtty) END * (operations.PriceOut + operations.VatOut) AS {13},
                  operations.Note as {16}
                FROM ((((((operations LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID){15}
                WHERE operations.OperType = 4 AND (operations.SrcDocID = 0 OR operations.SrcDocID = 2) AND operations.PartnerID <> 0 AND operations.Acct > 0",
                fieldsTable.GetFieldAlias (DataField.OperationNumber),
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationDetailAvailableQuantity),
                fieldsTable.GetFieldAlias (DataField.OperationDetailQuantity),
                fieldsTable.GetFieldAlias (DataField.OperationDetailDifference),
                fieldsTable.GetFieldAlias (DataField.OperationDetailPriceOut),
                fieldsTable.GetFieldAlias (DataField.OperationDetailVatOut),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumOut),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumVatOut),
                fieldsTable.GetFieldAlias (DataField.OperationDetailTotalOut),
                GetReportLotSelect (querySet), GetReportLotJoin (querySet),
                fieldsTable.GetFieldAlias (DataField.OperationDetailNote));

            querySet.SetComplexId (DbTable.Operations, DataField.OperationNumber, DataField.OperationType, OperationType.StockTaking);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportStockTakingsByTotal (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT operations.Acct as {0}, operations.Date as {1}, 
                  goods.Code as {2}, goods.Name as {3}, goods.BarCode1, goods.BarCode2, goods.BarCode3, 
                  goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name,
                  objects.Name as {4}, objectsgroups.Name, users1.Name as {5}, usersgroups1.Name,{8}
                  SUM(operations.Qtty * operations.PriceOut) as {6},
                  SUM(operations.Qtty * operations.VatOut) as {7}
                FROM ((((((operations LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID){9}
                WHERE operations.OperType = 4 AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY operations.Acct, operations.Date,
                 goods.Code, goods.Name, goods.BarCode1, goods.BarCode2, goods.BarCode3,
                 goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name,
                 objects.Name, objectsgroups.Name, users1.Name, usersgroups1.Name",
                fieldsTable.GetFieldAlias (DataField.OperationNumber),
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.StockTakingSum),
                fieldsTable.GetFieldAlias (DataField.OperationVatSum),
                GetReportLotSelect (querySet), GetReportLotJoin (querySet));

            querySet.SetComplexId (DbTable.Operations, DataField.OperationNumber, DataField.OperationType, OperationType.StockTaking);

            return ExecuteDataQuery (querySet, query);
        }

        #endregion
    }
}
