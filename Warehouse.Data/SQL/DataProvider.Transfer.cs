//
// DataProvider.Transfer.cs
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetAllTransfers<T> (DataQuery dataQuery)
        {
            string query = string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, objects.Code AS {7},
                  users1.Name as {3}, users1.Name2 as {4}, users2.Name as {5}, users2.Name2 as {6}
                FROM (((operations INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.OperType = 8 AND operations.PartnerID <> 0
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

        public override void AddUpdateTransferOut (object transferObject, object [] transferDetailObjects, bool allowNegativeQty)
        {
            ItemsManagementType type = GetItemsManagementType ();
            foreach (object detail in transferDetailObjects)
                AddUpdateDetail (transferObject, detail, allowNegativeQty, type);
        }

        public override void AddUpdateTransferIn (object transferObject, object [] transferDetailObjects, bool allowNegativeQty, bool increaseStoreAvailability)
        {
            ItemsManagementType type = GetItemsManagementType ();

            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (transferObject);

            long id = (long) helper.GetObjectValue (DataField.OperationNumber);
            if (id < 0)
                foreach (object detail in transferDetailObjects)
                    AddUpdateDetail (transferObject, detail, allowNegativeQty, type);
            else
                foreach (object detail in transferDetailObjects)
                    AddUpdateTransferInDetail (transferObject, detail, allowNegativeQty, type, increaseStoreAvailability);
        }

        private void AddUpdateTransferInDetail (object transferObject, object transferDetailObject, bool allowNegativeQty, ItemsManagementType imt, bool increaseStoreAvailability)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (transferObject);
            helper.AddObject (transferDetailObject);

            bool usesLots = imt != ItemsManagementType.AveragePrice &&
                imt != ItemsManagementType.QuickAveragePrice &&
                imt != ItemsManagementType.LastPurchasePrice;

            // Check if we already have that detail
            long result;
            var oldInfo = ExecuteObject<ObjectsContainer<int, long, long, double>> ("SELECT Sign as Value1, ObjectID as Value2, GoodID as Value3, Qtty as Value4 FROM operations WHERE ID = @ID", helper.Parameters);
            // Get the quantity from the new detail
            double newGoodsQty = (double) helper.GetObjectValue (DataField.OperationDetailQuantity);

            // We are updating detail information
            if (oldInfo != null) {
                long oldLocationId = oldInfo.Value2;
                long oldGoodsId = oldInfo.Value3;
                double oldGoodsQty = oldInfo.Value4;
                // Get the store id from the new detail
                long newLocationId = (long) helper.GetObjectValue (DataField.OperationLocationId);
                // Get the item id from the new detail
                long newGoodsId = (long) helper.GetObjectValue (DataField.OperationDetailItemId);

                long newLotId;
                string newLot;
                double newPrice;
                long oldLotId;
                string oldLot;
                double oldPrice;
                GetOperationDetailLotInfo (helper, usesLots, newGoodsQty, out newLotId, out newLot, out newPrice, out oldLotId, out oldLot, out oldPrice);

                if (newGoodsQty.IsZero ()) {
                    // Update the operation row with the changes
                    result = ExecuteNonQuery ("DELETE FROM operations WHERE ID = @ID", helper.Parameters);
                } else {
                    // Update the operation row with the changes
                    result = ExecuteNonQuery (string.Format ("UPDATE operations {0} WHERE ID = @ID",
                        helper.GetSetStatement (OperationNonDBFields.Union (new [] { DataField.OperationDetailId }).ToArray ())), helper.Parameters);
                }

                if (result != 1)
                    throw new Exception ("Unable to update operation detail.");

                if (oldInfo.Value1 == 0 && increaseStoreAvailability) {
                    ApplyTransferInInsertToStore (helper, usesLots);
                    return;
                }

                helper.AddParameters (new DbParam ("oldGoodsQty", oldGoodsQty));
                helper.AddParameters (new DbParam ("oldLocationId", oldLocationId));
                helper.AddParameters (new DbParam ("oldGoodsId", oldGoodsId));

                if (usesLots) {
                    if (newLocationId != oldLocationId || newGoodsId != oldGoodsId || newGoodsQty != oldGoodsQty ||
                        newPrice != oldPrice || newLot != oldLot || newLotId != oldLotId) {
                        helper.AddParameters (new DbParam ("oldPrice", oldPrice));
                        helper.AddParameters (new DbParam ("oldLot", oldLot));
                        helper.AddParameters (new DbParam ("oldLotId", oldLotId));

                        // Set the new quantity in the store
                        if (!newGoodsQty.IsZero ()) {
                            result = ExecuteNonQuery (string.Format (@"UPDATE store 
                                SET Qtty = Qtty + @Qtty
                                WHERE ObjectID = @ObjectID AND GoodID = @GoodID AND Lot = @Lot AND LotID = @LotID AND ABS(Price - @PriceIn) < {0}",
                                PurchaseCurrencyPrecisionString), helper.Parameters);

                            if (result == 0) {
                                result = ExecuteScalar<long> (@"SELECT IFNULL(MAX(LotOrder), 0)
                                    FROM store
                                    WHERE ObjectID = @ObjectID AND GoodID = @GoodID", helper.Parameters);
                                helper.AddParameters (new DbParam ("newLotOrder", result + 1));
                                ExecuteNonQuery (@"INSERT INTO store (ObjectID, GoodID, Lot, LotID, Price, Qtty, LotOrder)
                                    VALUES(@ObjectID, @GoodID, @Lot, @LotID, @PriceIn, @Qtty, @newLotOrder)",
                                    helper.Parameters);
                            }
                        }

                        // Revert the old quantity in the store
                        result = ExecuteNonQuery (@"UPDATE store
                            SET Qtty = Qtty - @oldGoodsQty
                            WHERE ObjectID = @oldLocationId AND GoodID = @oldGoodsId AND Lot = @oldLot AND LotID = @oldLotId AND Price = @oldPrice AND " +
                                GetQuantityCondition ("oldGoodsId", "oldGoodsQty", "oldLocationId", helper, -1),
                            helper.Parameters);

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
                    if (newLocationId != oldLocationId || newGoodsId != oldGoodsId || newGoodsQty != oldGoodsQty) {
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
            } // We are creating new detail information
            else {
                if (newGoodsQty.IsZero ())
                    return;

                // Insert the operation row
                result = ExecuteNonQuery (string.Format ("INSERT INTO operations {0}",
                    helper.GetColumnsAndValuesStatement (OperationNonDBFields.Union (new [] { DataField.OperationDetailId }).ToArray ())), helper.Parameters);

                if (result != 1)
                    throw new Exception ("Unable to insert operation detail.");

                result = GetLastAutoId ();
                helper.SetObjectValue (DataField.OperationDetailId, result);

                if (increaseStoreAvailability)
                    ApplyTransferInInsertToStore (helper, usesLots);
            }
        }

        public override void ApplyTransferOutDeleteToStore (SqlHelper helper, bool usesLots)
        {
            object [] oldOperation = null;
            using (IDataReader reader = ExecuteReader ("SELECT ObjectID, GoodID, Qtty FROM operations WHERE ID = @ID", helper.Parameters))
                while (reader.Read ()) {
                    oldOperation = new [] { reader.GetValue (0), reader.GetValue (1), reader.GetValue (2) };
                    break;
                }

            if (oldOperation == null)
                return;

            helper.AddParameters (new DbParam ("oldLocationId", oldOperation [0]));
            helper.AddParameters (new DbParam ("oldGoodsId", oldOperation [1]));
            helper.AddParameters (new DbParam ("oldGoodsQty", oldOperation [2]));

            if (!usesLots) {
                // Revert the old quantity in the store
                ExecuteNonQuery ("UPDATE store SET Qtty = Qtty + @oldGoodsQty WHERE ObjectID = @oldLocationId AND GoodID = @oldGoodsId", helper.Parameters);
                return;
            }

            long newLotId;
            string newLot;
            double newPrice;
            long oldLotId;
            string oldLot;
            double oldPrice;
            GetOperationDetailLotInfo (helper, true, 0, out newLotId, out newLot, out newPrice, out oldLotId, out oldLot, out oldPrice);

            helper.AddParameters (new DbParam ("oldPrice", oldPrice));
            helper.AddParameters (new DbParam ("oldLot", oldLot));
            helper.AddParameters (new DbParam ("oldLotId", oldLotId));

            // Set the old quantity in the store
            long result = ExecuteNonQuery (@"UPDATE store
                SET Qtty = Qtty + @oldGoodsQty
                WHERE ObjectID = @oldLocationId AND GoodID = @oldGoodsId AND Lot = @oldLot AND LotID = @oldLotId AND Price = @oldPrice", helper.Parameters);

            if (result != 0)
                return;

            result = ExecuteScalar<long> (@"SELECT IFNULL(MAX(LotOrder), 0)
                FROM store
                WHERE ObjectID = @oldLocationId AND GoodID = @oldGoodsId", helper.Parameters);

            helper.AddParameters (new DbParam ("newLotOrder", result + 1));
            ExecuteNonQuery (@"INSERT INTO store (ObjectID, GoodID, Lot, LotID, Price, Qtty, LotOrder)
                VALUES(@oldLocationId, @oldGoodsId, @oldLot, @oldLotId, @oldPrice, @oldGoodsQty, @newLotOrder)", helper.Parameters);
        }

        public override void ApplyTransferInInsertToStore (SqlHelper helper, bool usesLots)
        {
            if (!usesLots) {
                ExecuteNonQuery (@"UPDATE store SET Qtty = Qtty + @Qtty WHERE ObjectID = @ObjectID AND GoodID = @GoodID", helper.Parameters);
                return;
            }

            // Set the new quantity in the store
            long result = ExecuteNonQuery (string.Format (@"UPDATE store
                SET Qtty = Qtty + @Qtty
                WHERE ObjectID = @ObjectID AND GoodID = @GoodID AND Lot = @Lot AND LotID = @LotID AND ABS(Price - @PriceIn) < {0}", PurchaseCurrencyPrecisionString),
                helper.Parameters);

            if (result != 0)
                return;

            result = ExecuteScalar<long> (@"SELECT IFNULL(MAX(LotOrder), 0)
                FROM store
                WHERE ObjectID = @ObjectID AND GoodID = @GoodID", helper.Parameters);

            helper.AddParameters (new DbParam ("newLotOrder", result + 1));
            ExecuteNonQuery (@"INSERT INTO store (ObjectID, GoodID, Lot, LotID, Price, Qtty, LotOrder)
                VALUES (@ObjectID, @GoodID, @Lot, @LotID, @PriceIn, @Qtty, @newLotOrder)", helper.Parameters);
        }

        #endregion

        #region Reports

        public override DataQueryResult ReportTransfers (DataQuery querySet, bool usePriceIn)
        {
            string priceColumns = usePriceIn ?
                GetOperationPriceInColumns (querySet, false) :
                GetOperationPriceOutColumns (querySet, false);

            string query = string.Format (@"
                SELECT operations.Acct as {0}, operations.Date as {1}, objFrom.Name as {2},
                  (SELECT DISTINCT objTo.Name
                  FROM operations operTo, objects as objTo
                  WHERE operTo.OperType = 7 AND operTo.Acct = operations.Acct AND objTo.ID = operTo.ObjectID) as {3}, 
                  goods.Code as {4}, goods.Name as {5}, goods.BarCode1, goods.BarCode2, goods.BarCode3, 
                  goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name,
                  goods.Measure1, operations.Qtty as {6}, {7}, users1.Name as {8}, usersgroups1.Name,{10} operations.Note as {9}
                FROM (((((operations INNER JOIN objects as objFrom ON operations.ObjectID = objFrom.ID)
                  INNER JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  INNER JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID){11}
                WHERE operations.OperType = 8 AND operations.PartnerID <> 0 AND operations.Acct > 0",
                fieldsTable.GetFieldAlias (DataField.OperationNumber),
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.SourceLocationName),
                fieldsTable.GetFieldAlias (DataField.TargetLocationName),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.OperationDetailQuantity),
                priceColumns,
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationDetailNote),
                GetReportLotSelect (querySet), GetReportLotJoin (querySet));

            querySet.SetComplexId (DbTable.Operations, DataField.OperationNumber, DataField.OperationType, OperationType.TransferIn);

            return ExecuteDataQuery (querySet, query);
        }

        #endregion
    }
}
