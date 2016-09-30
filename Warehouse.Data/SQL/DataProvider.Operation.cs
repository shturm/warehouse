//
// DataProvider.Operation.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   01.12.2010
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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        public override T GetOperationById<T> (OperationType operationType, long operationId)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, users1.Name as {3}, users1.Name2 as {4}, users2.Name as {5}, users2.Name2 as {6}
                FROM (((operations INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.OperType = @operationType AND Acct = @operationId",
                OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2)),
                new DbParam ("operationType", (int) operationType),
                new DbParam ("operationId", operationId));
        }

        public override T GetOperationWithPartnerById<T> (OperationType operationType, long operationId)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, partners.Company as {3}, partners.Company2 as {4}, users1.Name as {5}, users1.Name2 as {6}, users2.Name as {7}, users2.Name2 as {8}
                FROM ((((operations INNER JOIN partners ON operations.PartnerID = partners.ID) INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.OperType = @operationType AND Acct = @operationId",
                OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2)),
                new DbParam ("operationType", (int) operationType),
                new DbParam ("operationId", operationId));
        }

        public override T GetFirstOperation<T> ()
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, partners.Company as {3}, partners.Company2 as {4}, users1.Name as {5}, users1.Name2 as {6}, users2.Name as {7}, users2.Name2 as {8}
                FROM ((((operations INNER JOIN partners ON operations.PartnerID = partners.ID) INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                ORDER BY operations.Date
                LIMIT 1",
                OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2)));
        }

        public override T [] GetAllAfter<T> (DateTime operationDate)
        {
            return ExecuteArray<T> (string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, users1.Name as {3}, users1.Name2 as {4}, users2.Name as {5}, users2.Name2 as {6}
                FROM (((operations INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.PartnerID <> 0 AND operations.Date >= @operationDate",
                OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2)),
                new DbParam ("operationDate", operationDate));
        }

        public override T GetPendingOperation<T> (OperationType operationType, long partnerId, long locationId)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, users1.Name as {3}, users1.Name2 as {4}, users2.Name as {5}, users2.Name2 as {6}
                FROM (((operations INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE OperType = @operationType AND Acct = @operationId AND ObjectID = @locationId AND PartnerID = @partnerId",
                OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2)),
                new DbParam ("operationType", (int) operationType),
                new DbParam ("operationId", (int) OperationState.Pending),
                new DbParam ("locationId", locationId),
                new DbParam ("partnerId", partnerId));
        }

        public override T GetPendingOperationWithPartner<T> (OperationType operationType, long partnerId, long locationId)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, partners.Company as {3}, partners.Company2 as {4}, users1.Name as {5}, users1.Name2 as {6}, users2.Name as {7}, users2.Name2 as {8}
                FROM ((((operations INNER JOIN partners ON operations.PartnerID = partners.ID) INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE OperType = @operationType AND Acct = @operationId AND ObjectID = @locationId AND PartnerID = @partnerId",
                OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2)),
                new DbParam ("operationType", (int) operationType),
                new DbParam ("operationId", (int) OperationState.Pending),
                new DbParam ("locationId", locationId),
                new DbParam ("partnerId", partnerId));
        }

        public override ObjectsContainer<OperationType, long, long> [] GetAllPendingOperations ()
        {
            return ExecuteArray<ObjectsContainer<OperationType, long, long>> (@"
                SELECT OperType as Value1, PartnerID as Value2, ObjectID as Value3
                FROM operations
                WHERE Acct = @operationId AND OperType <> @transferIn",
                new DbParam ("operationId", (int) OperationState.Pending),
                new DbParam ("transferIn", (int) OperationType.TransferIn));
        }

        public override LazyListModel<T> GetOperationDetailsById<T> (OperationType operationType, long operationId, long? partnerId, long? locationId, DateTime? date, long? userId, long loggedUserId, int currencyPrecission, bool roundPrices, bool usePriceIn)
        {
            DataQuery dQuery = new DataQuery { VATIsIncluded = true, CurrencyPrecission = currencyPrecission, RoundPrices = roundPrices };
            string total = usePriceIn ? GetOperationDetailTotalIn (dQuery) : GetOperationDetailTotalOut (dQuery);
            bool isPending = operationId == (int) OperationState.Pending;

            string query = string.Format (@"
                SELECT {0}, goods.Measure1 as {1}, goods.Name as {2}, goods.Name2 as {3}, goods.Code as {10}, {4} as {5},
                  lots.SerialNo as {6}, lots.EndDate as {7}, lots.ProductionDate as {8}, lots.Location as {9}
                FROM operations LEFT JOIN goods ON operations.GoodID = goods.ID
                  LEFT JOIN lots on operations.LotID = lots.ID
                WHERE operations.OperType = @operationType AND Acct = @operationId{11}",
                OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.ItemMeasUnit),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.ItemName2),
                total,
                fieldsTable.GetFieldAlias (DataField.OperationDetailTotal),
                fieldsTable.GetFieldAlias (DataField.LotSerialNumber),
                fieldsTable.GetFieldAlias (DataField.LotExpirationDate),
                fieldsTable.GetFieldAlias (DataField.LotProductionDate),
                fieldsTable.GetFieldAlias (DataField.LotLocation),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                isPending ?
                    string.Format ("{0}{1}{2}{3} AND operations.UserID = @loggedUserId",
                        partnerId != null ? " AND operations.PartnerID = @partnerId" : string.Empty,
                        locationId != null ? " AND operations.ObjectID = @locationId" : string.Empty,
                        date != null ? " AND operations.Date = @date" : string.Empty,
                        userId != null ? " AND operations.OperatorID = @userId" : string.Empty) :
                    string.Empty);

            List<DbParam> pars = new List<DbParam>
                {
                    new DbParam ("operationId", operationId),
                    new DbParam ("operationType", (int) operationType),
                    new DbParam ("loggedUserId", loggedUserId)
                };
            if (partnerId != null)
                pars.Add (new DbParam ("partnerId", partnerId.Value));
            if (locationId != null)
                pars.Add (new DbParam ("locationId", locationId.Value));
            if (date != null)
                pars.Add (new DbParam ("date", date.Value));
            if (userId != null)
                pars.Add (new DbParam ("userId", userId.Value));

            return ExecuteLazyModel<T> (query, pars.ToArray ());
        }

        public override LazyListModel<long> GetOperationLocationIds (DataQuery dataQuery)
        {
            string sql = string.Format ("{0} FROM operations GROUP BY operations.ObjectID",
                GetSelect (new [] { DataField.OperationLocationId }));
            return ExecuteDataQuery<long> (dataQuery, sql);
        }

        public override void UpdateOperationUser (OperationType operationType, long operationId, long userId)
        {
            ExecuteNonQuery (@"UPDATE operations SET OperatorID = @newUser WHERE OperType = @operType AND Acct = @operNumber",
                new DbParam ("operType", (int) operationType),
                new DbParam ("operNumber", operationId),
                new DbParam ("newUser", userId));
        }

        public override void AddUpdateStoreNeutralDetail (SqlHelper helper)
        {
            // Check if we already have that detail
            long result = ExecuteScalar<long> ("SELECT count(*) FROM operations WHERE ID = @ID", helper.Parameters);

            // We are updating detail information
            if (result == 1) {
                // Get the quantity from the new detail
                double newItemQty = (double) helper.GetObjectValue (DataField.OperationDetailQuantity);

                if (newItemQty.IsZero ()) {
                    // Update the operation row with the changes
                    result = ExecuteNonQuery ("DELETE FROM operations WHERE ID = @ID", helper.Parameters);
                } else {
                    // Update the operation row with the changes
                    result = ExecuteNonQuery (string.Format ("UPDATE operations {0} WHERE ID = @ID",
                        helper.GetSetStatement (OperationNonDBFields.Union (new [] { DataField.OperationDetailId }).ToArray ())), helper.Parameters);
                }

                if (result != 1)
                    throw new DataException ("Unable to update operation detail.");
            } // We are creating new detail information
            else if (result == 0) {
                // Get the quantity from the new detail
                if (!((double) helper.GetObjectValue (DataField.OperationDetailQuantity)).IsZero ()) {
                    // Insert the operation row
                    result = ExecuteNonQuery (string.Format ("INSERT INTO operations {0}",
                        helper.GetColumnsAndValuesStatement (OperationNonDBFields.Union (new [] { DataField.OperationDetailId }).ToArray ())), helper.Parameters);

                    if (result != 1)
                        throw new DataException ("Unable to insert operation detail.");

                    result = GetLastAutoId ();
                    helper.SetObjectValue (DataField.OperationDetailId, result);
                }
            } else
                throw new DataException ("Wrong number of operation details found with the given Id.");
        }

        public override void AddUpdateDetailOut (SqlHelper helper, bool allowNegativeQty, ItemsManagementType imt, long childLocationId, string updateClause = "", string deleteClause = "")
        {
            bool usesLots = imt != ItemsManagementType.AveragePrice &&
                imt != ItemsManagementType.QuickAveragePrice &&
                imt != ItemsManagementType.LastPurchasePrice;

            // Check if we already have that detail
            var oldInfo = ExecuteObject<ObjectsContainer<int, long, long, double>> ("SELECT Sign as Value1, ObjectID as Value2, GoodID as Value3, Qtty as Value4 FROM operations WHERE ID = @ID", helper.Parameters);
            // Get the quantity from the new detail
            double newItemQty = (double) helper.GetObjectValue (DataField.OperationDetailQuantity);

            if (string.IsNullOrEmpty (updateClause))
                updateClause = "UPDATE operations {0} WHERE ID = @ID";
            if (string.IsNullOrEmpty (deleteClause))
                deleteClause = "DELETE FROM operations WHERE ID = @ID";

            long result;

            // We are updating detail information
            if (oldInfo != null) {
                int oldSign = oldInfo.Value1;
                long oldLocationId = oldInfo.Value2;
                long oldGoodsId = oldInfo.Value3;
                double oldGoodsQty = oldInfo.Value4;

                // Get the store id from the new detail
                long newLocationId = (long) helper.GetObjectValue (DataField.OperationLocationId);
                // Get the item id from the new detail
                long newItemId = (long) helper.GetObjectValue (DataField.OperationDetailItemId);

                long newLotId;
                string newLot;
                double newPrice;
                long oldLotId;
                string oldLot;
                double oldPrice;
                GetOperationDetailLotInfo (helper, usesLots, newItemQty, out newLotId, out newLot, out newPrice, out oldLotId, out oldLot, out oldPrice);

                if (newItemQty.IsZero ()) {
                    // Update the operation row with the changes
                    result = ExecuteNonQuery (deleteClause, helper.Parameters);
                } else {
                    // Update the operation row with the changes
                    result = ExecuteNonQuery (string.Format (updateClause,
                        helper.GetSetStatement (OperationNonDBFields.Union (new [] { DataField.OperationDetailId }).ToArray ())), helper.Parameters);
                }

                if (result != 1)
                    throw new DataException (string.Format ("Unable to update operation detail. Item: {0}.", helper.GetObjectValue (DataField.ItemName)));

                int sign = (int) helper.GetObjectValue (DataField.OperationDetailSign);
                if (sign == 0)
                    return;

                if (oldSign == 0)
                    oldGoodsQty = 0;

                helper.AddParameters (new DbParam ("oldGoodsQty", oldGoodsQty));
                helper.AddParameters (new DbParam ("oldLocationId", oldLocationId));
                helper.AddParameters (new DbParam ("oldGoodsId", oldGoodsId));

                if (usesLots) {
                    if (newLocationId != oldLocationId || newItemId != oldGoodsId || !newItemQty.IsEqualTo (oldGoodsQty) ||
                        !newPrice.IsEqualTo (oldPrice) || newLot != oldLot || newLotId != oldLotId) {
                        helper.AddParameters (new DbParam ("oldPrice", oldPrice));
                        helper.AddParameters (new DbParam ("oldLot", oldLot));
                        helper.AddParameters (new DbParam ("oldLotId", oldLotId));

                        // Set the old quantity in the store
                        result = ExecuteNonQuery (@"UPDATE store
                            SET Qtty = Qtty + @oldGoodsQty
                            WHERE ObjectID = @oldLocationId AND GoodID = @oldGoodsId AND Lot = @oldLot AND LotID = @oldLotId AND Price = @oldPrice", helper.Parameters);

                        if (result == 0) {
                            result = ExecuteScalar<long> (@"SELECT IFNULL(MAX(LotOrder), 0)
                                FROM store
                                WHERE ObjectID = @oldLocationId AND GoodID = @oldGoodsId", helper.Parameters);
                            helper.AddParameters (new DbParam ("newLotOrder", result + 1));
                            ExecuteNonQuery (@"INSERT INTO store (ObjectID, GoodID, Lot, LotID, Price, Qtty, LotOrder)
                                VALUES(@oldLocationId, @oldGoodsId, @oldLot, @oldLotId, @oldPrice, @oldGoodsQty, @newLotOrder)",
                                helper.Parameters);
                        }

                        // Set the new quantity in the store
                        result = ExecuteNonQuery (string.Format (@"UPDATE store
                            SET Qtty = Qtty - @Qtty
                            WHERE ObjectID = @ObjectID AND GoodID = @GoodID AND Lot = @Lot AND LotID = @LotID AND 
                                ABS(Price - @OperationDetailStorePriceIn) < {0} AND {1}",
                            PurchaseCurrencyPrecisionString,
                            GetQuantityCondition ("GoodID", "Qtty", "ObjectID", helper, childLocationId)),
                            helper.Parameters);

                        if (result != 1)
                            throw new InsufficientItemAvailabilityException ((string) helper.GetObjectValue (DataField.ItemName));

                        // Delete the row if it has become with 0 quantity
                        ExecuteNonQuery (string.Format (@"DELETE FROM store
                            WHERE ObjectID = @ObjectID AND GoodID = @GoodID AND Lot = @Lot AND LotID = @LotID AND ABS(Price - @OperationDetailStorePriceIn) < {0} AND ABS(Qtty) < 0.0000001",
                            PurchaseCurrencyPrecisionString), helper.Parameters);
                    }
                } else {
                    if (newLocationId != oldLocationId || newItemId != oldGoodsId || !newItemQty.IsEqualTo (oldGoodsQty)) {
                        // Revert the old quantity in the store
                        ExecuteNonQuery ("UPDATE store SET Qtty = Qtty + @oldGoodsQty WHERE ObjectID = @oldLocationId AND GoodID = @oldGoodsId",
                            helper.Parameters);

                        // Set the new quantity in the store
                        if (!newItemQty.IsZero ()) {
                            if (allowNegativeQty) {
                                ExecuteNonQuery ("UPDATE store SET Qtty = Qtty - @Qtty WHERE ObjectID = @ObjectID AND GoodID = @GoodID",
                                    helper.Parameters);
                            } else {
                                result = ExecuteNonQuery (@"UPDATE store SET Qtty = Qtty - @Qtty 
                                    WHERE ObjectID = @ObjectID AND GoodID = @GoodID AND " +
                                    GetQuantityCondition ("GoodID", "Qtty", "ObjectID", helper, childLocationId),
                                    helper.Parameters);

                                if (result != 1)
                                    throw new InsufficientItemAvailabilityException ((string) helper.GetObjectValue (DataField.ItemName));
                            }
                        }
                    }
                }
            } // We are creating new detail information
            else {
                if (newItemQty.IsZero ())
                    return;

                // Insert the operation row
                result = ExecuteNonQuery (string.Format ("INSERT INTO operations {0}",
                    helper.GetColumnsAndValuesStatement (OperationNonDBFields.Union (new [] { DataField.OperationDetailId }).ToArray ())), helper.Parameters);

                if (result != 1)
                    throw new DataException ("Unable to insert operation detail.");

                result = GetLastAutoId ();
                helper.SetObjectValue (DataField.OperationDetailId, result);

                int sign = (int) helper.GetObjectValue (DataField.OperationDetailSign);
                if (sign == 0)
                    return;

                if (usesLots) {
                    result = ExecuteNonQuery (string.Format (@"UPDATE store
                        SET Qtty = Qtty - @Qtty
                        WHERE ObjectID = @ObjectID AND GoodID = @GoodID AND Lot = @Lot AND LotID = @LotID AND 
                            ABS(Price - @OperationDetailStorePriceIn) < {0} AND {1}",
                        PurchaseCurrencyPrecisionString,
                        GetQuantityCondition ("GoodID", "Qtty", "ObjectID", helper, childLocationId)),
                        helper.Parameters);

                    if (result != 1)
                        throw new InsufficientItemAvailabilityException ((string) helper.GetObjectValue (DataField.ItemName));

                    // Delete the row if it has become with 0 quantity
                    ExecuteNonQuery (string.Format (@"DELETE FROM store
                        WHERE ObjectID = @ObjectID AND GoodID = @GoodID AND Lot = @Lot AND LotID = @LotID AND ABS(Price - @OperationDetailStorePriceIn) < {0} AND ABS(Qtty) < 0.0000001",
                        PurchaseCurrencyPrecisionString),
                        helper.Parameters);
                } else {
                    // Set the new quantity in the store
                    if (allowNegativeQty) {
                        ExecuteNonQuery ("UPDATE store SET Qtty = Qtty - @Qtty WHERE ObjectID = @ObjectID AND GoodID = @GoodID",
                            helper.Parameters);
                    } else {
                        result = ExecuteNonQuery (@"UPDATE store SET Qtty = Qtty - @Qtty 
                            WHERE ObjectID = @ObjectID AND GoodID = @GoodID AND " +
                            GetQuantityCondition ("GoodID", "Qtty", "ObjectID", helper, childLocationId),
                            helper.Parameters);

                        if (result != 1)
                            throw new InsufficientItemAvailabilityException ((string) helper.GetObjectValue (DataField.ItemName));
                    }
                }
            }
        }

        public override void AddUpdateDetailIn (SqlHelper helper, bool allowNegativeQty, DataField priceOutField, ItemsManagementType imt)
        {
            bool usesLots = imt != ItemsManagementType.AveragePrice &&
                imt != ItemsManagementType.QuickAveragePrice &&
                imt != ItemsManagementType.LastPurchasePrice;

            // Check if we already have that detail
            var oldInfo = ExecuteObject<ObjectsContainer<int, long, long, double>> ("SELECT Sign as Value1, ObjectID as Value2, GoodID as Value3, Qtty as Value4 FROM operations WHERE ID = @ID", helper.Parameters);
            // Get the quantity from the new detail
            double newItemQty = (double) helper.GetObjectValue (DataField.OperationDetailQuantity);

            long result;

            // We are updating detail information
            if (oldInfo != null) {
                int oldSign = oldInfo.Value1;
                long oldLocationId = oldInfo.Value2;
                long oldItemId = oldInfo.Value3;
                double oldItemQty = oldInfo.Value4;

                // Get the store id from the new detail
                long newLocationId = (long) helper.GetObjectValue (DataField.OperationLocationId);
                // Get the item id from the new detail
                long newItemId = (long) helper.GetObjectValue (DataField.OperationDetailItemId);

                long newLotId;
                string newLot;
                double newPrice;
                long oldLotId;
                string oldLot;
                double oldPrice;
                GetOperationDetailLotInfo (helper, usesLots, newItemQty, out newLotId, out newLot, out newPrice, out oldLotId, out oldLot, out oldPrice);

                if (newItemQty.IsZero ()) {
                    // Update the operation row with the changes
                    result = ExecuteNonQuery ("DELETE FROM operations WHERE ID = @ID", helper.Parameters);
                } else {
                    // Update the operation row with the changes
                    result = ExecuteNonQuery (string.Format ("UPDATE operations {0} WHERE ID = @ID",
                        helper.GetSetStatement (OperationNonDBFields.Union (new [] { DataField.OperationDetailId }).ToArray ())), helper.Parameters);
                }

                if (result != 1)
                    throw new DataException ("Unable to update operation detail.");

                int sign = (int) helper.GetObjectValue (DataField.OperationDetailSign);
                if (sign == 0)
                    return;

                if (oldSign == 0)
                    oldItemQty = 0;

                helper.AddParameters (new DbParam ("oldGoodsQty", oldItemQty));
                helper.AddParameters (new DbParam ("oldLocationId", oldLocationId));
                helper.AddParameters (new DbParam ("oldGoodsId", oldItemId));

                if (usesLots) {
                    if (newLocationId != oldLocationId || newItemId != oldItemId || !newItemQty.IsEqualTo (oldItemQty) ||
                        !newPrice.IsEqualTo (oldPrice) || newLot != oldLot || newLotId != oldLotId) {
                        helper.AddParameters (new DbParam ("oldPrice", oldPrice));
                        helper.AddParameters (new DbParam ("oldLot", oldLot));
                        helper.AddParameters (new DbParam ("oldLotId", oldLotId));

                        // Set the new quantity in the store
                        if (!newItemQty.IsZero ()) {
                            result = ExecuteNonQuery (string.Format (@"UPDATE store
                                SET Qtty = Qtty + @Qtty
                                WHERE ObjectID = @ObjectID AND GoodID = @GoodID AND Lot = @Lot AND LotID = @LotID AND ABS(Price - @OperationDetailStorePriceIn) < {0}",
                                PurchaseCurrencyPrecisionString), helper.Parameters);

                            if (result == 0) {
                                result = ExecuteScalar<long> (@"SELECT IFNULL(MAX(LotOrder), 0)
                                    FROM store
                                    WHERE ObjectID = @ObjectID AND GoodID = @GoodID", helper.Parameters);
                                helper.AddParameters (new DbParam ("newLotOrder", result + 1));
                                ExecuteNonQuery (@"INSERT INTO store (ObjectID, GoodID, Lot, LotID, Price, Qtty, LotOrder)
                                    VALUES(@ObjectID, @GoodID, @Lot, @LotID, @OperationDetailStorePriceIn, @Qtty, @newLotOrder)",
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
                    if (newLocationId != oldLocationId || newItemId != oldItemId || !newItemQty.IsEqualTo (oldItemQty)) {
                        // Set the new quantity in the store
                        if (!newItemQty.IsZero ()) {
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

                    if (newItemId != oldItemId) {
                        SetItemPriceIn (priceOutField, imt, oldItemId, oldPrice, oldItemQty, helper, true);
                        // Set the old price in the store
                        ExecuteNonQuery ("UPDATE store SET Price = @newPriceIn WHERE GoodID = @itemId", helper.Parameters);
                    }

                    if (newItemId != oldItemId || !newItemQty.IsEqualTo (oldItemQty) || !newPrice.IsEqualTo (oldPrice)) {
                        SetItemPriceIn (priceOutField, imt, newItemId, newPrice, newItemQty, helper, newItemQty.IsZero ());
                        // Set the new price in the store
                        ExecuteNonQuery ("UPDATE store SET Price = @newPriceIn WHERE GoodID = @itemId", helper.Parameters);
                    }
                }
            } // We are creating new detail information
            else {
                if (newItemQty.IsZero ())
                    return;

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
                    throw new DataException ("Unable to insert operation detail.");

                result = GetLastAutoId ();
                helper.SetObjectValue (DataField.OperationDetailId, result);

                int sign = (int) helper.GetObjectValue (DataField.OperationDetailSign);
                if (sign == 0)
                    return;

                #region Update item prices

                SetItemPriceIn (priceOutField, imt,
                    (long) helper.GetObjectValue (DataField.OperationDetailItemId),
                    (double) helper.GetObjectValue (DataField.OperationDetailStorePriceIn), newItemQty, helper);

                #endregion

                if (usesLots) {
                    result = ExecuteNonQuery (@"UPDATE store
                        SET Qtty = Qtty + @Qtty
                        WHERE ObjectID = @ObjectID AND GoodID = @GoodID AND Lot = @Lot AND LotID = @LotID AND Price = @newPriceIn", helper.Parameters);

                    if (result == 0) {
                        result = ExecuteScalar<long> (@"SELECT IFNULL(MAX(LotOrder), 0)
                            FROM store
                            WHERE ObjectID = @ObjectID AND GoodID = @GoodID", helper.Parameters);
                        helper.AddParameters (new DbParam ("newLotOrder", result + 1));

                        ExecuteNonQuery (@"INSERT INTO store (ObjectID, GoodID, Lot, LotID, Price, Qtty, LotOrder)
                            VALUES(@ObjectID, @GoodID, @Lot, @LotID, @newPriceIn, @Qtty, @newLotOrder)", helper.Parameters);
                    }
                } else {
                    // Set the new price in the store
                    ExecuteNonQuery ("UPDATE store SET Price = @newPriceIn WHERE GoodID = @GoodID", helper.Parameters);

                    // Set the new quantity in the store
                    ExecuteNonQuery ("UPDATE store SET Qtty = Qtty + @Qtty WHERE ObjectID = @ObjectID AND GoodID = @GoodID", helper.Parameters);
                }
            }
        }

        private void SetItemPriceIn (DataField priceOutField, ItemsManagementType imt, long itemId, double price, double newItemQty, SqlHelper helper, bool annulling = false)
        {
            double newPriceIn = price;
            if (imt == ItemsManagementType.AveragePrice || imt == ItemsManagementType.QuickAveragePrice) {
                newPriceIn = CalculateAveragePrice (itemId, price, newItemQty, annulling, imt == ItemsManagementType.QuickAveragePrice);
                if (newPriceIn <= 0)
                    newPriceIn = price;
            }

            if (helper.Parameters.All (p => p.ParameterName != "newPriceIn"))
                helper.AddParameters (new DbParam ("newPriceIn", newPriceIn));
            else
                helper.SetParameterValue ("newPriceIn", newPriceIn);

            if (helper.Parameters.All (p => p.ParameterName != "itemId"))
                helper.AddParameters (new DbParam ("itemId", itemId));
            else
                helper.SetParameterValue ("itemId", itemId);

            // Set the new quantity in the item
            if (priceOutField == DataField.NotSet)
                ExecuteNonQuery ("UPDATE goods SET PriceIn = @newPriceIn WHERE ID = @itemId", helper.Parameters);
            else
                ExecuteNonQuery (string.Format (
                    "UPDATE goods SET PriceIn = @newPriceIn, {0} = @OperationDetailStorePriceOut WHERE ID = @itemId",
                    fieldsTable.GetFieldName (priceOutField)),
                    helper.Parameters);
        }

        public override double CalculateAveragePrice (long itemId, double newPriceIn, double newItemQty, bool annulling, bool quickMode)
        {
            if (quickMode) {
                // Get the available quantity from the store
                double oldStoreQty = ExecuteScalar<double> ("SELECT IFNULL(SUM(Qtty),0) FROM store WHERE GoodID = @itemId",
                    new DbParam ("itemId", itemId));

                // Store is already changed so modify the value accordingly
                oldStoreQty += annulling ? newItemQty : -newItemQty;

                // Get the the trade in price from store
                double oldPriceIn = ExecuteScalar<double> ("SELECT PriceIn FROM goods WHERE ID = @itemId",
                    new DbParam ("itemId", itemId));

                oldStoreQty = Math.Max (oldStoreQty, 0);
                newItemQty = Math.Max (newItemQty, 0);
                oldPriceIn = Math.Max (oldPriceIn, 0);
                newPriceIn = Math.Max (newPriceIn, 0);

                if (annulling)
                    newItemQty = -newItemQty;

                if ((oldStoreQty + newItemQty).IsZero ())
                    return (oldPriceIn + newPriceIn) / 2;

                return (oldPriceIn * oldStoreQty + newPriceIn * newItemQty) / (oldStoreQty + newItemQty);
            }

            // Get the average parameters for the non-zero price operations
            ObjectsContainer<double, double, double, double, double, double> res = ExecuteObject<ObjectsContainer<double, double, double, double, double, double>> (@"
SELECT
SUM(CASE WHEN Qtty * Sign > 0 THEN Qtty * Sign * PriceIn ELSE 0 END) as Value1,
SUM(CASE WHEN Qtty * Sign > 0 THEN Qtty * Sign ELSE 0 END) as Value2,

SUM(CASE WHEN PriceIn > 0 AND Qtty * Sign < 0 THEN Qtty * Sign * PriceIn ELSE 0 END) as Value3,
SUM(CASE WHEN PriceIn > 0 AND Qtty * Sign < 0 THEN Qtty * Sign ELSE 0 END) as Value4,

IFNULL(MIN(PriceIn), 0) as Value5,
IFNULL(MAX(PriceIn), 0) as Value6
FROM operations
WHERE (OperType < @recipeMaterial OR OperType > @cRecipeProduct) AND GoodID = @itemId",
                new DbParam ("itemId", itemId),
                new DbParam ("recipeMaterial", (int) OperationType.RecipeMaterial),
                new DbParam ("cRecipeProduct", (int) OperationType.ComplexRecipeProduct));

            // Get the estimated average parameters for the output operations with zero prices
            DateTime maxOut = ExecuteScalar<DateTime> (@"SELECT Date as maxout
FROM operations
WHERE GoodID = @itemId AND
  Qtty * Sign < 0 AND
  PriceIn = 0
ORDER BY Date desc
LIMIT 1", new DbParam ("itemId", itemId));

            if (maxOut > DateTime.MinValue) {
                DateTime minIn = ExecuteScalar<DateTime> (@"SELECT Date as minin
FROM operations
WHERE GoodID = @itemId AND
  Qtty * Sign > 0 AND
  PriceIn > 0
ORDER BY Date asc
LIMIT 1", new DbParam ("itemId", itemId));

                if (minIn <= maxOut) {
                    List<ObjectsContainer<DateTime, double, int, double>> inOps = ExecuteList<ObjectsContainer<DateTime, double, int, double>> (@"
SELECT Date as Value1, Qtty as Value2, OperType as Value3, PriceIn as Value4
FROM operations
WHERE GoodID = @itemId AND
  Qtty * Sign > 0 AND
  PriceIn > 0
ORDER BY Date desc", new DbParam ("itemId", itemId));

                    List<ObjectsContainer<DateTime, double, int, int>> outOps = ExecuteList<ObjectsContainer<DateTime, double, int, int>> (@"
SELECT Date as Value1, Qtty as Value2, OperType as Value3, Sign as Value4
FROM operations
WHERE GoodID = @itemId AND
  Qtty * Sign < 0 AND
  PriceIn = 0
ORDER BY Date desc", new DbParam ("itemId", itemId));

                    int i = 0;
                    const int stockTaking = (int) OperationType.StockTaking;
                    foreach (ObjectsContainer<DateTime, double, int, int> outOp in outOps) {
                        while (i < inOps.Count && (inOps [i].Value1 > outOp.Value1 || (inOps [i].Value2 < outOp.Value2 && inOps [i].Value3 != stockTaking && outOp.Value3 != stockTaking)))
                            i++;

                        if (i >= inOps.Count)
                            break;

                        double c = outOp.Value2 * outOp.Value4;
                        res.Value3 += c * inOps [i].Value4;
                        res.Value4 += c;
                    }
                }
            }

            return CalculateAveragePrice (res.Value1, res.Value2, res.Value3, res.Value4, res.Value5, res.Value6);
        }

        private static string GetQuantityCondition (string itemParameter, string quantityParameter, string locationParameter, SqlHelper helper, long childLocationId)
        {
            return string.Format ("{0} > @{1} - 0.0000001", GetQuantity (itemParameter, locationParameter, helper, childLocationId), quantityParameter);
        }

        private static string GetQuantity (string itemParameter, string locationParameter, SqlHelper helper, long childLocationId)
        {
            helper.AddParameters (new DbParam ("childLocationId", childLocationId));
            return string.Format (@"Qtty - (SELECT IFNULL(SUM(Qtty), 0) FROM operations WHERE GoodID = @{0} AND (
                (OperType = {1} AND operations.SrcDocID = @{2} AND operations.ObjectID <> @childLocationId) OR
                (OperType = {3} AND operations.ObjectID = @{2})))",
                itemParameter, (int) OperationType.RestaurantOrder, locationParameter, (int) OperationType.SalesOrder);
        }

        /// <summary>
        /// Creates an ID for a new operation created at the location with the specified ID.
        /// </summary>
        /// <param name="operationType">The type of the operation to get an ID for.</param>
        /// <param name="locationId"> </param>
        /// <param name="currentState"></param>
        /// <returns></returns>
        public override long CreateNewOperationId (OperationType operationType, long locationId, OperationState currentState = OperationState.New)
        {
            if (currentState == OperationState.NewPending)
                return (int) OperationState.Pending;

            string prefix = GetOperationAbbreviation (operationType);

            // Check if we have any operations of this type
            object lastId;
            if (operationType == OperationType.RestaurantOrder) {
                DateTime now = Now ();

                ExecuteNonQuery (string.Format (@"DELETE FROM nextacct
                    WHERE NextAcct REGEXP '^{0}[0-9]{{8}}L{1}N[0-9]+$' AND CAST(SUBSTR(NextAcct, 3, 8) AS UNSIGNED) < {2}",
                    prefix,
                    locationId.ToString (CultureInfo.InvariantCulture),
                    now.ToString ("yyyyMMdd")));

                prefix = string.Format ("{0}{1}L{2}N",
                    prefix,
                    now.ToString ("yyyyMMdd"),
                    locationId.ToString (CultureInfo.InvariantCulture));

                lastId = ExecuteScalar (string.Format (@"SELECT MAX(CAST(SUBSTR(NextAcct, @prefixlen) AS UNSIGNED))
                    FROM nextacct
                    WHERE NextAcct REGEXP '^{0}[0-9]+$'", prefix),
                    new DbParam ("prefixlen", prefix.Length + 1));
            } else {
                string table = operationType == OperationType.AdvancePayment ? "payments" : "operations";
                if (locationId > 0 && currentState == OperationState.New) {
                    if (operationType == OperationType.Sale || operationType == OperationType.ConsignmentSale)
                        lastId = ExecuteScalar (string.Format (@"
                            SELECT MAX(Acct) FROM {0} 
                            WHERE (OperType = 2 OR OperType = 16) AND ObjectID = @locationId", table),
                                new DbParam ("locationId", locationId));
                    else
                        lastId = ExecuteScalar (string.Format (@"
                            SELECT MAX(Acct) FROM {0} 
                            WHERE OperType = @operationType AND ObjectID = @locationId", table),
                                new DbParam ("operationType", (int) operationType),
                                new DbParam ("locationId", locationId));
                } else {
                    string func = currentState == OperationState.New ? "MAX(Acct)" : "MIN(Acct)";
                    if (operationType == OperationType.Sale || operationType == OperationType.ConsignmentSale)
                        lastId = ExecuteScalar (string.Format ("SELECT {1} FROM {0} WHERE OperType = 2 OR OperType = 16", table, func),
                            new DbParam ("operationType", (int) operationType));
                    else
                        lastId = ExecuteScalar (string.Format ("SELECT {1} FROM {0} WHERE OperType = @operationType", table, func),
                            new DbParam ("operationType", (int) operationType));
                }
            }

            long newId = IsDBNull (lastId) ? 0 : Convert.ToInt64 (lastId);

            if (currentState == OperationState.NewDraft) {
                if (newId >= -10)
                    newId = -10;

                newId--;
                return newId;
            }

            if (newId < 0)
                newId = 0;

            newId++;

            bool created = false;
            Exception exception = null;

            // This is needed since someone might be creating an operation at this moment and
            // also have reserved only an id in nextacct table. Try 10 more ids before giving up.
            for (int i = 0; i < 100; i++) {
                string acct = prefix + newId.ToString (CultureInfo.InvariantCulture);
                DbParam param = new DbParam ("NextAcct", acct);
                try {
                    if (ExecuteNonQuery (string.Format ("INSERT {0} IGNORE INTO nextacct (NextAcct) VALUES(@NextAcct)", InsertIgnoreSeparator), param) != 1) {
                        newId++;
                        continue;
                    }

                    created = true;
                    break;
                } catch (Exception ex) {
                    newId++;
                    exception = ex;
                }
            }

            if (!created)
                throw new Exception ("Unable to insert new record into nextacct table.", exception);

            return newId;
        }

        public override bool OperationIdExists (long id)
        {
            return ExecuteScalar (@"
                SELECT 1 FROM operations 
                WHERE EXISTS (SELECT Acct FROM operations WHERE Acct = @Acct)", new DbParam ("Acct", id)) != null;
        }

        public override bool OperationNoteExists (string note)
        {
            return ExecuteScalar (@"SELECT 1 FROM operations WHERE Note = @note LIMIT 1", new DbParam ("note", note)) != null;
        }

        public override void DeleteOperationId (OperationType operationType, long operationId)
        {
            ExecuteNonQuery ("DELETE FROM nextacct WHERE NextAcct = @NextAcct",
                new DbParam ("NextAcct", GetOperationAbbreviation (operationType) + operationId));
        }

        public override long? GetOperationId (long detailId)
        {
            return ExecuteScalar<int?> (@"SELECT Acct FROM operations WHERE ID = @id", new DbParam ("id", detailId));
        }

        public override void DeletePayableOperationsBefore (DateTime date, bool onlyPaid)
        {
            string paymentsCondition = onlyPaid ? @"
                payments.OperType <> 16
                GROUP BY payments.OperType, payments.Acct
                HAVING MIN(payments.Date) <= @date AND ABS(SUM(payments.Qtty * payments.Mode)) < 0.0000001" :
                "payments.OperType <> 16 AND payments.Date <= @date";
            foreach (string table in new [] { "documents", "operations" })
                while (ExecuteNonQuery (
                    string.Format (@"
                        DELETE FROM {0} WHERE EXISTS (
                          SELECT payments.OperType, payments.Acct FROM payments 
                          WHERE {0}.Acct = payments.Acct AND {0}.OperType = payments.OperType AND 
                          {1})
                        {2}",
                        table, paymentsCondition, LimitDelete),
                    new DbParam ("date", date)) > 0) {
                }
            if (onlyPaid) {
                while (ExecuteNonQuery (string.Format (@"                        
                    DELETE FROM payments WHERE ID IN (
                      SELECT ID from (
                        SELECT ID FROM payments AS pm
                        WHERE pm.OperType <> 16 AND pm.Date <= @date AND (
                          SELECT ABS(SUM(p.Qtty * p.Mode)) 
						  FROM payments AS p
						  WHERE pm.Acct = p.Acct AND pm.OperType = p.OperType
						  GROUP BY p.Acct, p.OperType) < 0.0000001) AS p1)
                    {0}", LimitDelete), new DbParam ("date", date)) > 0) {
                }
            } else {
                while (ExecuteNonQuery (string.Format (@"
                    DELETE FROM payments
                    WHERE payments.OperType <> 16 AND payments.Date <= @date
                    {0}", LimitDelete), new DbParam ("date", date)) > 0) {
                }
            }
        }

        public override void DeleteNonPayableOperations (DateTime date)
        {
            string sql = string.Format (@"
                DELETE FROM operations
                WHERE PartnerID <> 0 AND Date <= @date AND OperType IN (
                  @offer, @order, @proformaInvoice, @request, @produceIn, @produceOut, @complexProductionMaterial, 
                  @complexProductionProduct, @transferIn, @transferOut, @warrantyCard, @waste, @writeOff)
                {0}", LimitDelete);

            DbParam [] @params =
                {
                    new DbParam ("@date", date),
                    new DbParam ("@offer", (int) OperationType.Offer),
                    new DbParam ("@order", (int) OperationType.SalesOrder),
                    new DbParam ("@proformaInvoice", (int) OperationType.ProformaInvoice),
                    new DbParam ("@request", (int) OperationType.PurchaseOrder),
                    new DbParam ("@produceIn", (int) OperationType.ProduceIn),
                    new DbParam ("@produceOut", (int) OperationType.ProduceOut),
                    new DbParam ("@complexProductionMaterial", (int) OperationType.ComplexProductionMaterial),
                    new DbParam ("@complexProductionProduct", (int) OperationType.ComplexProductionProduct),
                    new DbParam ("@transferIn", (int) OperationType.TransferIn),
                    new DbParam ("@transferOut", (int) OperationType.TransferOut),
                    new DbParam ("@warrantyCard", (int) OperationType.WarrantyCard),
                    new DbParam ("@waste", (int) OperationType.Waste),
                    new DbParam ("@writeOff", (int) OperationType.WriteOff)
                };
            while (ExecuteNonQuery (sql, @params) > 0) {
            }
        }

        public override void AddOperationIdCodes (IEnumerable<string> operationCodes)
        {
            List<List<DbParam>> parameters = operationCodes
                .Select (c => new List<DbParam> (new [] { new DbParam ("@NextAcct", c) }))
                .ToList ();

            BulkInsert ("nextacct", "NextAcct", parameters, "Unable to create next operation code.");
        }

        public override void DeleteOperationIdCodes ()
        {
            while (ExecuteNonQuery (string.Format ("DELETE FROM nextacct {0}", LimitDelete)) > 0) {
            }
        }

        public override void AddOperations (IEnumerable<DictionaryEntry> operations)
        {
            SqlHelper operationHelper = GetSqlHelper ();
            SqlHelper operationDetailHelper = GetSqlHelper ();
            List<List<DbParam>> parameters = new List<List<DbParam>> ();

            List<DataField> operationDetailNonDBFieldsList = new List<DataField> (OperationNonDBFields) { DataField.OperationDetailId, DataField.OperationDetailNote };
            DataField [] operationDetailNonDBFields = operationDetailNonDBFieldsList.ToArray ();
            foreach (DictionaryEntry operation in operations) {
                operationHelper.ChangeObject (operation.Key, OperationNonDBFields);
                foreach (object detail in (IEnumerable) operation.Value) {
                    List<DbParam> dbParams = new List<DbParam> (operationHelper.Parameters);
                    operationHelper.ResetParameters ();
                    operationDetailHelper.ChangeObject (detail, operationDetailNonDBFields);
                    dbParams.AddRange (operationDetailHelper.Parameters);
                    parameters.Add (dbParams);
                }
            }

            StringBuilder columns = new StringBuilder ();
            columns.Append (operationHelper.GetColumns (OperationNonDBFields.Select (f => new DbField (f)).ToArray ()));
            columns.Append (", ");
            columns.Append (operationDetailHelper.GetColumns (operationDetailNonDBFields.Select (f => new DbField (f)).ToArray ()));
            BulkInsert ("operations", columns.ToString (), parameters, "Unable to insert operations.");
        }

        public override LazyListModel<T> GetQuantities<T> (DataQuery dataQuery, bool useLots)
        {
            if (useLots) {
                return ExecuteDataQuery<T> (dataQuery, string.Format (@"
                    {0}
                    FROM (operations INNER JOIN goods ON operations.GoodID = goods.ID) INNER JOIN vatgroups ON goods.TaxGroup = vatgroups.ID
                    GROUP BY operations.ObjectID, operations.GoodID, goods.PriceOut2, operations.PriceIn, operations.Lot, operations.LotID, vatgroups.VATValue",
                    GetSelect (new [] { DataField.OperationDetailItemId, DataField.ItemPriceGroup2,
                        DataField.OperationDetailPriceIn, DataField.OperationDetailLot,
                        DataField.OperationDetailLotId, DataField.VATGroupValue,
                        DataField.OperationLocationId }) +
                    string.Format (", SUM(operations.Qtty * operations.Sign) AS {0}",
                        fieldsTable.GetFieldAlias (DataField.OperationDetailQuantity))));
            }

            return ExecuteDataQuery<T> (dataQuery, string.Format (@"
                {0}
                FROM (operations INNER JOIN goods ON operations.GoodID = goods.ID) LEFT JOIN vatgroups ON goods.TaxGroup = vatgroups.ID
                GROUP BY operations.ObjectID, operations.GoodID, goods.PriceOut2, operations.PriceIn, vatgroups.VATValue",
                GetSelect (new [] { DataField.OperationDetailItemId, DataField.ItemPriceGroup2,
                    DataField.OperationDetailPriceIn, DataField.VATGroupValue,
                    DataField.OperationLocationId }) +
                string.Format (", SUM(operations.Qtty * operations.Sign) AS {0}",
                    fieldsTable.GetFieldAlias (DataField.OperationDetailQuantity))));
        }

        public override List<ObjectsContainer<int, long, long>> GetLastOperationNumbers (bool groupByLocation)
        {
            return ExecuteList<ObjectsContainer<int, long, long>> (string.Format (@"
                SELECT OperType AS Value1, MAX(Acct) AS Value2{0}
                FROM operations 
                WHERE OperType NOT IN (@produceOut, @complexProductionProduct, @recipeMaterial, 
                    @recipeProduct, @complexRecipeProduct, @transferIn, @restaurantOrder)
                GROUP BY OperType{1}",
                groupByLocation ? ", ObjectID AS Value3" : string.Empty,
                groupByLocation ? ", ObjectID" : string.Empty),
                new DbParam ("@produceOut", (int) OperationType.ProduceOut),
                new DbParam ("@complexProductionProduct", (int) OperationType.ComplexProductionProduct),
                new DbParam ("@recipeMaterial", (int) OperationType.RecipeMaterial),
                new DbParam ("@recipeProduct", (int) OperationType.RecipeProduct),
                new DbParam ("@complexRecipeProduct", (int) OperationType.ComplexRecipeProduct),
                new DbParam ("@transferIn", (int) OperationType.TransferIn),
                new DbParam ("@restaurantOrder", (int) OperationType.RestaurantOrder));
        }

        public override T [] GetOperationNumbersUsagePerLocation<T> ()
        {
            return ExecuteArray<T> (string.Format (@"
                SELECT OperType as {0}, ObjectID as {1}, MAX(Acct) as LastUsedNumber, COUNT(Acct) as UsedNumbers
	            FROM operations
                WHERE PartnerID <> 0 AND PartnerID IS NOT NULL
	            GROUP BY OperType, ObjectID",
                fieldsTable.GetFieldAlias (DataField.OperationType),
                fieldsTable.GetFieldAlias (DataField.OperationLocationId)));
        }

        public override ObjectsContainer<OperationType, long> [] GetOperationNumbersUsageStarts ()
        {
            return ExecuteArray<ObjectsContainer<OperationType, long>> (@"
                SELECT OperType as Value1, Acct as Value2
                FROM operations as op1
                WHERE PartnerID <> 0 AND PartnerID IS NOT NULL AND NOT EXISTS (SELECT Acct FROM operations as op2 WHERE PartnerID <> 0 AND PartnerID IS NOT NULL AND op2.Acct = op1.Acct - 1 AND op2.OperType = op1.OperType)
                GROUP BY OperType, Acct
                ORDER BY OperType, Acct");
        }

        /// <summary>
        /// Gets the numbers to be used for objects of type at the location with the specified location ID.
        /// </summary>
        /// <returns>Value1 - LocationId, Value2 - Operation type, Value3 - Start number</returns>
        public override T [] GetOperationStartNumbersPerLocation<T> ()
        {
            return ExecuteArray<T> (string.Format (@"
                SELECT operations.ObjectID AS {0}, operations.OperType AS {1}, MAX(operations.Acct + 1) AS {2}
                FROM operations WHERE PartnerID = 0 OR PartnerID IS NULL
                GROUP BY ObjectID, OperType",
                fieldsTable.GetFieldAlias (DataField.OperationLocationId),
                fieldsTable.GetFieldAlias (DataField.OperationType),
                fieldsTable.GetFieldAlias (DataField.OperationNumber)));
        }

        /// <summary>
        /// Creates for the first time the numbers to be used as ID-s for documents (operations).
        /// </summary>
        /// <param name="minimalRange">The minimal range between the numbers for two consecutive locations.</param>
        /// <param name="recommendedRange">The recommended range between the numbers for two consecutive locations.</param>
        public override void CreateOperationStartNumbersPerLocation (long minimalRange, long recommendedRange)
        {
            OperationType [] operationTypes = GetOperationTypesForStartNumbersPerLocation ();

            object scalar = ExecuteScalar ("SELECT Max(Acct) from operations");
            long maxDocumentNumber = IsDBNull (scalar) ? 0 : Convert.ToInt64 (scalar);
            // +1 for the next number, and +1 because the service number is written as 999999 = 1000000 - 1
            maxDocumentNumber = maxDocumentNumber == 0 ? 0 : maxDocumentNumber + 1 + 1;
            long rangeStart = (maxDocumentNumber / minimalRange + (maxDocumentNumber % minimalRange == 0 ? 0 : 1)) * minimalRange;

            using (DbTransaction transaction = new DbTransaction (this)) {
                List<long> allLocationsIDs = GetAllLocationIds ();
                for (int i = 0; i < allLocationsIDs.Count; i++)
                    AddNumberingPerLocationByIndex (allLocationsIDs [i], i, rangeStart, operationTypes, recommendedRange);

                transaction.Complete ();
            }
        }

        public static OperationType [] GetOperationTypesForStartNumbersPerLocation ()
        {
            return new []
                {
                    OperationType.Purchase,
                    OperationType.Sale,
                    OperationType.Waste,
                    OperationType.StockTaking,
                    OperationType.ProduceIn,

                    OperationType.TransferOut,

                    OperationType.WriteOff,
                    OperationType.PurchaseOrder,
                    OperationType.Offer,
                    OperationType.ProformaInvoice,
                    OperationType.Consignment,

                    OperationType.ConsignmentReturn,
                    OperationType.SalesOrder,
                    OperationType.ComplexProductionMaterial,

                    OperationType.DebitNote,
                    OperationType.CreditNote,
                    OperationType.WarrantyCard,

                    OperationType.Return,
                    OperationType.AdvancePayment,
                    OperationType.Invoice
                };
        }

        private void AddNumberingPerLocationByIndex (long locationId, long locationIndex, long rangeStart, OperationType [] operationTypes, long recommendedRange)
        {
            AddOperationStartNumbersPerLocation (locationId, rangeStart + (locationIndex + 1) * recommendedRange - 1, operationTypes);
        }

        private void AddOperationStartNumbersPerLocation (long locationId, long rangeStart, OperationType [] operationTypes)
        {
            if (operationTypes == null)
                operationTypes = GetOperationTypesForStartNumbersPerLocation ();

            SqlHelper sqlHelper = GetSqlHelper ();
            var locId = new DbParam ("locationId", locationId);
            var range = new DbParam ("number", rangeStart);

            foreach (OperationType operationType in operationTypes) {
                var operType = new DbParam ("operType", (int) operationType);

                ExecuteNonQuery ("DELETE FROM operations WHERE (PartnerID = 0 OR PartnerID IS NULL) AND ObjectID = @locationId AND OperType = @operType",
                    locId, operType);

                ExecuteNonQuery (string.Format (
                    @"INSERT INTO operations(OperType, Acct, ObjectID, Date, UserRealTime)
                      VALUES(@operType, @number, @locationId, {0}, {1})", sqlHelper.CurrentDateFunction, sqlHelper.CurrentDateTimeFunction),
                    locId, range, operType);
            }
        }

        /// <summary>
        /// Updates the numbers used as ID-s for documents (operations).
        /// </summary>
        /// <param name="operations">The operations which contain the new values of the numbers.</param>
        public override void UpdateOperationStartNumbersPerLocation<T> (IEnumerable<T> objectsContainers)
        {
            SqlHelper sqlHelper = GetSqlHelper ();

            using (DbTransaction transaction = new DbTransaction (this)) {
                foreach (var operation in objectsContainers) {
                    sqlHelper.ChangeObject (operation);

                    DbParam operType = new DbParam ("operType", sqlHelper.GetObjectValue (DataField.OperationType));
                    DbParam acct = new DbParam ("acct", (long) sqlHelper.GetObjectValue (DataField.OperationNumber) - 1);
                    DbParam locationId = new DbParam ("locationId", sqlHelper.GetObjectValue (DataField.OperationLocationId));

                    if (ExecuteNonQuery (@"
                        UPDATE operations SET Acct = @acct 
                        WHERE OperType = @operType AND ObjectId = @locationId AND (PartnerID = 0 OR PartnerID IS NULL)",
                        operType, acct, locationId) == 1)
                        continue;

                    ExecuteNonQuery (@"DELETE FROM operations
                        WHERE OperType = @operType AND Acct = @acct AND ObjectId = @locationId AND (PartnerID = 0 OR PartnerID IS NULL)",
                        operType, acct, locationId);

                    ExecuteNonQuery (string.Format (@"
                        INSERT INTO operations(OperType, Acct, ObjectID, PartnerID, Date, UserRealTime)
                        VALUES(@operType, @acct, @locationId, 0, {0}, {1})", sqlHelper.CurrentDateFunction, sqlHelper.CurrentDateTimeFunction),
                        operType, acct, locationId);
                }
                transaction.Complete ();
            }
        }

        /// <summary>
        /// Deletes the numbers used as ID-s for documents (operations) according to their location.
        /// </summary>
        public override void DeleteOperationStartNumbersPerLocation ()
        {
            ExecuteNonQuery ("DELETE FROM operations WHERE PartnerID = 0 OR PartnerID IS NULL");
        }

        private void GetOperationDetailLotInfo (SqlHelper helper, bool usesLots, double newItemQty, out long newLotId, out string newLot, out double newPrice, out long oldLotId, out string oldLot, out double oldPrice)
        {
            oldPrice = 0;
            oldLot = string.Empty;
            oldLotId = 0;
            using (IDataReader reader = ExecuteReader ("SELECT PriceIn, Lot, LotID FROM operations WHERE ID = @ID", helper.Parameters))
                while (reader.Read ()) {
                    oldPrice = Convert.ToDouble (reader.GetValue (0));
                    oldLot = Convert.ToString (reader.GetValue (1));
                    oldLotId = Convert.ToInt64 (reader.GetValue (2));
                    break;
                }

            newPrice = (double) helper.GetObjectValue (DataField.OperationDetailStorePriceIn);
            newLot = (string) helper.GetObjectValue (DataField.OperationDetailLot);
            newLotId = (long) helper.GetObjectValue (DataField.OperationDetailLotId);

            if (newItemQty.IsZero () || !usesLots)
                return;

            newLotId = GetCreateLotId ((string) helper.GetObjectValue (DataField.LotSerialNumber),
                (DateTime?) helper.GetObjectValue (DataField.LotExpirationDate),
                (DateTime?) helper.GetObjectValue (DataField.LotProductionDate),
                (string) helper.GetObjectValue (DataField.LotLocation), newLotId);

            helper.SetObjectValue (DataField.OperationDetailLotId, newLotId);
            helper.SetParameterValue (DataField.OperationDetailLotId, newLotId);
        }

        public override string GetOperationDetailTotalIn (DataQuery query)
        {
            return string.Format ("operations.Qtty * (operations.PriceIn{0})",
                query.VATIsIncluded ? string.Empty : " + operations.VATIn");
        }

        public override string GetOperationDetailTotalOut (DataQuery query)
        {
            const string operTable = "operations";
            string vat = query.VATIsIncluded ? string.Empty : string.Format (" + {0}.VATOut", operTable);

            if (!query.RoundPrices)
                return string.Format ("{1}.Qtty * ({1}.PriceOut{0})", vat, operTable);

            return string.Format ("ROUND(({2}.PriceOut{1})*100/(100-{2}.Discount),{0})*{2}.Qtty-ROUND(ROUND(({2}.PriceOut{1})*100/(100-{2}.Discount),{0})*{2}.Qtty*{2}.Discount/100,{0})",
                query.CurrencyPrecission, vat, operTable);
        }

        public override string GetOperationDetailVatOutSum (DataQuery query)
        {
            const string operTable = "operations";
            if (!query.RoundPrices)
                return string.Format ("{0}.Qtty * {0}.VatOut", operTable);

            return string.Format ("ROUND({1}.VatOut*100/(100-{1}.Discount),{0})*{1}.Qtty-ROUND(ROUND({1}.VatOut*100/(100-{1}.Discount),{0})*{1}.Qtty*{1}.Discount/100,{0})",
                query.CurrencyPrecission, operTable);
        }

        public override string GetOperationDetailVatInSum (DataQuery query)
        {
            return string.Format ("{0}.Qtty * {0}.VatIn", "operations");
        }

        public override string GetOperationPriceInColumns (DataQuery query, bool addTotal = true)
        {
            return string.Format (@"operations.PriceIn as {0}, operations.VATIn as {1},
                (operations.Qtty * operations.PriceIn) as {2},
                ({3}) as {4}{5}",
                fieldsTable.GetFieldAlias (DataField.OperationDetailPriceIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailVatIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumIn),
                GetOperationDetailVatInSum (query),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumVatIn),
                !addTotal ? string.Empty : string.Format (",({0}) as {1}",
                GetOperationDetailTotalIn (query),
                fieldsTable.GetFieldAlias (DataField.OperationDetailTotalIn)));
        }

        public override string GetOperationPriceOutColumns (DataQuery query, bool addTotal = true)
        {
            return string.Format (@"operations.PriceOut as {0}, operations.VATOut as {1},
                (operations.Qtty * operations.PriceOut) as {2},
                ({3}) as {4}{5}",
                fieldsTable.GetFieldAlias (DataField.OperationDetailPriceOut),
                fieldsTable.GetFieldAlias (DataField.OperationDetailVatOut),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumOut),
                GetOperationDetailVatOutSum (query),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumVatOut),
                !addTotal ? string.Empty : string.Format (",({0}) as {1}",
                GetOperationDetailTotalOut (query),
                fieldsTable.GetFieldAlias (DataField.OperationDetailTotalOut)));
        }

        public override DataQueryResult ReportOperations (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT operations.Acct as {0}, operations.OperType as {15}, operations.Date as {1}, 
                  goods.Code as {2}, goods.Name as {3}, goods.BarCode1, goods.BarCode2, goods.BarCode3,
                  goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name, 
                  partners.Company, partnersgroups.Name, objects.Name as {4}, objectsgroups.Name, 
                  users1.Name as {5}, usersgroups1.Name,{11}
                  operations.Qtty as {6}, goods.Measure1 as {16},
                  operations.PriceOut as {7}, operations.VATOut as {8},
                  (operations.Qtty * operations.PriceOut) as {9},
                  ({13}) as {10},
                  operations.PriceIn as {17}, operations.VATIn as {18},
                  (operations.Qtty * operations.PriceIn) as {19},
                  ({21}) as {20},
                  operations.Note as {14}
                FROM (((((((((operations INNER JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                  INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  {12})
                WHERE operations.PartnerID <> 0 AND operations.Acct > 0",
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
                GetReportLotSelect (querySet),
                GetReportLotJoin (querySet),
                GetOperationDetailVatOutSum (querySet),
                fieldsTable.GetFieldAlias (DataField.OperationDetailNote),
                fieldsTable.GetFieldAlias (DataField.OperationType),
                fieldsTable.GetFieldAlias (DataField.ItemMeasUnit),
                fieldsTable.GetFieldAlias (DataField.OperationDetailPriceIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailVatIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumVatIn),
                GetOperationDetailVatInSum (querySet));

            querySet.SetComplexId (DbTable.Operations, DataField.OperationNumber, DataField.OperationType);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportDrafts (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT operations.Acct as {0}, operations.OperType as {15}, operations.Date as {1}, 
                  goods.Code as {2}, goods.Name as {3}, goods.BarCode1, goods.BarCode2, goods.BarCode3,
                  goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name, 
                  partners.Company, partnersgroups.Name, objects.Name as {4}, objectsgroups.Name, 
                  users1.Name as {5}, usersgroups1.Name,{11}
                  operations.Qtty as {6}, goods.Measure1 as {16},
                  operations.PriceOut as {7}, operations.VATOut as {8},
                  (operations.Qtty * operations.PriceOut) as {9},
                  ({13}) as {10},
                  operations.PriceIn as {17}, operations.VATIn as {18},
                  (operations.Qtty * operations.PriceIn) as {19},
                  ({21}) as {20},
                  operations.Note as {14}
                FROM (((((((((operations INNER JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                  INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  {12})
                WHERE operations.PartnerID <> 0 AND operations.Acct < -10",
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
                GetReportLotSelect (querySet),
                GetReportLotJoin (querySet),
                GetOperationDetailVatOutSum (querySet),
                fieldsTable.GetFieldAlias (DataField.OperationDetailNote),
                fieldsTable.GetFieldAlias (DataField.OperationType),
                fieldsTable.GetFieldAlias (DataField.ItemMeasUnit),
                fieldsTable.GetFieldAlias (DataField.OperationDetailPriceIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailVatIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumVatIn),
                GetOperationDetailVatInSum (querySet));

            querySet.SetComplexId (DbTable.Operations, DataField.OperationNumber, DataField.OperationType);

            return ExecuteDataQuery (querySet, query);
        }
    }
}

