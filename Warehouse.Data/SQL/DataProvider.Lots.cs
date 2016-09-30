//
// DataProvider.Lots.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
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
using System.Collections.Generic;
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetLots<T> (long itemId, long? locationId, ItemsManagementType imt)
        {
            DbParam [] pars =
                {
                    new DbParam ("itemId", itemId),  
                    new DbParam ("locationId", locationId ?? -1)  
                };

            string query = string.Format (@"SELECT s.Qtty as {0}, s.Price as {1}, s.Lot as {2}, l.ID as {3}, l.SerialNo as {4},
                l.EndDate as {5}, l.ProductionDate as {6}, l.Location as {7}
                FROM store as s LEFT JOIN lots as l ON s.LotID = l.ID
                WHERE s.GoodID = @itemId{8}
                {9}
                ORDER BY {{0}}",
                fieldsTable.GetFieldAlias (DataField.StoreQtty),
                fieldsTable.GetFieldAlias (DataField.StorePrice),
                fieldsTable.GetFieldAlias (DataField.StoreLot),
                fieldsTable.GetFieldAlias (DataField.LotId),
                fieldsTable.GetFieldAlias (DataField.LotSerialNumber),
                fieldsTable.GetFieldAlias (DataField.LotExpirationDate),
                fieldsTable.GetFieldAlias (DataField.LotProductionDate),
                fieldsTable.GetFieldAlias (DataField.LotLocation),
                locationId.HasValue ? " AND s.ObjectID = @locationId" : string.Empty,
                locationId.HasValue ? string.Empty : "GROUP BY LotID, Lot");

            switch (imt) {
                case ItemsManagementType.AveragePrice:
                case ItemsManagementType.QuickAveragePrice:
                case ItemsManagementType.LastPurchasePrice:
                    return new LazyListModel<T> ();
                case ItemsManagementType.FIFO:
                case ItemsManagementType.Choice:
                    return ExecuteLazyModel<T> (string.Format (query, "s.LotOrder ASC"), pars);
                case ItemsManagementType.LIFO:
                    return ExecuteLazyModel<T> (string.Format (query, "s.LotOrder DESC"), pars);
                case ItemsManagementType.FEFO:
                    return ExecuteLazyModel<T> (string.Format (query, "l.EndDate ASC"), pars);
                default:
                    throw new ArgumentOutOfRangeException ("imt");
            }
        }

        public override T GetLotByStoreId<T> (long storeId)
        {
            return ExecuteObject<T> (string.Format (@"SELECT s.Qtty as {0}, s.Price as {1}, s.Lot as {2}, l.ID as {3}, l.SerialNo as {4},
                l.EndDate as {5}, l.ProductionDate as {6}, l.Location as {7}
                FROM store as s LEFT JOIN lots as l ON s.LotID = l.ID
                WHERE s.ID = @storeId",
                fieldsTable.GetFieldAlias (DataField.StoreQtty),
                fieldsTable.GetFieldAlias (DataField.StorePrice),
                fieldsTable.GetFieldAlias (DataField.StoreLot),
                fieldsTable.GetFieldAlias (DataField.LotId),
                fieldsTable.GetFieldAlias (DataField.LotSerialNumber),
                fieldsTable.GetFieldAlias (DataField.LotExpirationDate),
                fieldsTable.GetFieldAlias (DataField.LotProductionDate),
                fieldsTable.GetFieldAlias (DataField.LotLocation)),
                new DbParam ("storeId", storeId));
        }

        #endregion

        #region Save / Delete

        private long GetCreateLotId (string serialNo, DateTime? endDate, DateTime? prodDate, string location, long? lastLotId)
        {
            string filter = string.Format ("SerialNo = @serialNo AND Location = @location AND {0} AND {1}",
                endDate.HasValue ? "EndDate = @endDate" : "EndDate IS NULL",
                prodDate.HasValue ? "ProductionDate = @prodDate" : "ProductionDate IS NULL");

            DbParam [] pars =
                {
                    new DbParam ("serialNo", serialNo),
                    new DbParam ("location", location ?? string.Empty),
                    new DbParam ("endDate", endDate), 
                    new DbParam ("prodDate", prodDate)
                };

            using (DbTransaction trans = new DbTransaction (this)) {
                long temp;
                if (lastLotId.HasValue && lastLotId != DefaultLotId) {
                    DbParam lotId = new DbParam ("lotId", lastLotId);
                    temp = ExecuteScalar<long> ("SELECT count(1) FROM store WHERE LotID = @lotId", lotId);
                    if (temp == 1) {
                        temp = ExecuteScalar<long> ("SELECT count(1) FROM operations WHERE LotID = @lotId", lotId);
                        if (temp == 1) {
                            List<DbParam> pars1 = new List<DbParam> (pars) { lotId };
                            temp = ExecuteNonQuery (string.Format ("UPDATE lots SET SerialNo = @serialNo, Location = @location{0}{1} WHERE ID = @lotId",
                                endDate.HasValue ? ", EndDate = @endDate" : string.Empty,
                                prodDate.HasValue ? ", ProductionDate = @prodDate" : string.Empty), pars1.ToArray ());
                            if (temp != 1)
                                throw new Exception (string.Format ("Cannot change lot with id = \'{0}\' serial=\'{1}\' endDate=\'{2}\' prodDate=\'{3}\' location=\'{4}\'",
                                    lotId, serialNo, endDate, prodDate, location));

                            return lastLotId.Value;
                        }
                    }
                }

                temp = ExecuteScalar<long> (string.Format ("SELECT count(1) FROM lots WHERE {0}", filter), pars);

                int ret;
                switch (temp) {
                    case 1:
                        ret = ExecuteScalar<int> (string.Format ("SELECT ID FROM lots WHERE {0}", filter), pars);
                        break;
                    case 0:
                        temp = ExecuteNonQuery (string.Format ("INSERT INTO lots (SerialNo, Location{0}{1}) VALUES(@serialNo, @location{2}{3})",
                            endDate.HasValue ? ", EndDate" : string.Empty,
                            prodDate.HasValue ? ", ProductionDate" : string.Empty,
                            endDate.HasValue ? ", @endDate" : string.Empty,
                            prodDate.HasValue ? ", @prodDate" : string.Empty), pars);
                        if (temp != 1)
                            throw new Exception (string.Format ("Cannot add lot with serial=\'{0}\' endDate=\'{1}\' prodDate=\'{2}\' location=\'{3}\'",
                                serialNo, endDate, prodDate, location));
                        ret = (int) GetLastAutoId ();
                        break;
                    default:
                        throw new Exception ("Too many entries with the same ID found in lots table.");
                }

                trans.Complete ();
                return ret;
            }
        }

        private DeletePermission DeleteLot (long lotId)
        {
            if (lotId == DefaultLotId)
                return DeletePermission.Reserved;

            using (DbTransaction trans = new DbTransaction (this)) {
                DbParam par = new DbParam ("lotId", lotId);
                long temp = ExecuteScalar<long> ("SELECT count(1) FROM store WHERE LotID = @lotId", par);
                if (temp != 0)
                    return DeletePermission.No;

                temp = ExecuteScalar<long> ("SELECT count(1) FROM operations WHERE LotID = @lotId", par);
                if (temp != 0)
                    return DeletePermission.No;

                temp = ExecuteNonQuery ("DELETE FROM lots WHERE ID = @lotId", par);
                if (temp != 1)
                    return DeletePermission.No;

                trans.Complete ();
                return DeletePermission.Yes;
            }
        }

        public override void EnableLots ()
        {
            using (DbTransaction transaction = new DbTransaction (this)) {
                ItemsManagementType imt = GetItemsManagementType ();

                if (imt != ItemsManagementType.AveragePrice &&
                    imt != ItemsManagementType.QuickAveragePrice &&
                    imt != ItemsManagementType.LastPurchasePrice)
                    return;

                long unavailable = ExecuteScalar<long> ("SELECT count(1) FROM store WHERE Qtty < 0");
                if (unavailable > 0)
                    throw new InsufficientItemAvailabilityException (null);

                string mixedPriceInItem = ExecuteScalar<string> (@"SELECT goods.Name
FROM operations as op INNER JOIN store ON op.GoodID = store.GoodID AND op.PriceIn <> store.Price
  INNER JOIN goods ON op.GoodID = goods.ID
LIMIT 1");
                if (!string.IsNullOrWhiteSpace (mixedPriceInItem))
                    throw new MixedPriceInItemException (mixedPriceInItem);

                ExecuteNonQuery ("DELETE FROM store WHERE ABS(Qtty) < 0.0000001");
                ExecuteNonQuery ("UPDATE operations SET Lot = 'NA'");
                ExecuteNonQuery ("UPDATE store SET Lot = 'NA', LotOrder = 1");

                transaction.Complete ();
            }
        }

        public override void DisableLots ()
        {
            using (DbTransaction transaction = new DbTransaction (this)) {
                ItemsManagementType imt = GetItemsManagementType ();

                if (imt == ItemsManagementType.AveragePrice ||
                    imt == ItemsManagementType.QuickAveragePrice ||
                    imt == ItemsManagementType.LastPurchasePrice)
                    return;

                long operId = CreateNewOperationId (OperationType.Temp, -1);
                DbParam [] pars =
                    {
                        new DbParam ("operType", (int)OperationType.Temp),
                        new DbParam ("operId", operId)     
                    };

                ExecuteNonQuery (@"
                    INSERT INTO operations (OperType, Acct, ObjectID, GoodID, Qtty)
                    SELECT @operType, @operId, ObjectID, GoodID, SUM(Qtty)
                    FROM store
                    GROUP BY ObjectID, GoodID", pars);

                ExecuteNonQuery ("DELETE FROM store");

                ExecuteNonQuery (@"
                    INSERT INTO store (ObjectID, GoodID, Qtty, Price, Lot, LotID, LotOrder)
                    SELECT o.ID, g.ID, SUM(IFNULL(op.Qtty, 0)), g.PriceIn, ' ', 1, 1
                    FROM (goods as g, objects as o)
                     LEFT JOIN operations as op ON op.GoodID = g.ID AND op.ObjectID = o.ID AND op.OperType = @operType AND op.Acct = @operId
                    GROUP BY o.ID, g.ID", pars);

                ExecuteNonQuery ("DELETE FROM operations WHERE OperType = @operType AND Acct = @operId", pars);
                DeleteOperationId (OperationType.Temp, operId);

                ExecuteNonQuery ("UPDATE operations SET Lot = ' ', LotID = 1");
                ExecuteNonQuery ("DELETE FROM lots WHERE ID <> 1");

                transaction.Complete ();
            }
        }

        #endregion

        #region Reports

        #endregion

        public override string GetReportLotSelect (DataQuery querySet)
        {
            return querySet.UseLots ? string.Format (@"operations.Lot as {0}, lots.SerialNo as {1}, lots.EndDate as {2}, lots.ProductionDate as {3}, lots.Location as {4},",
                fieldsTable.GetFieldAlias (DataField.OperationDetailLot),
                fieldsTable.GetFieldAlias (DataField.LotSerialNumber),
                fieldsTable.GetFieldAlias (DataField.LotExpirationDate),
                fieldsTable.GetFieldAlias (DataField.LotProductionDate),
                fieldsTable.GetFieldAlias (DataField.LotLocation)) : string.Empty;
        }

        public override string GetReportLotJoin (DataQuery querySet)
        {
            return querySet.UseLots ? "LEFT JOIN lots ON operations.LotID = lots.ID" : string.Empty;
        }

        public override string GetReportLotGroup (DataQuery querySet)
        {
            return querySet.UseLots ? @", operations.Lot, lots.SerialNo, lots.EndDate, lots.ProductionDate, lots.Location" : string.Empty;
        }

        private string GetReportStoreLotSelect (DataQuery querySet)
        {
            return querySet.UseLots ? string.Format (@"store.Lot as {0}, lots.SerialNo as {1}, lots.EndDate as {2}, lots.ProductionDate as {3}, lots.Location as {4},",
                fieldsTable.GetFieldAlias (DataField.StoreLot),
                fieldsTable.GetFieldAlias (DataField.LotSerialNumber),
                fieldsTable.GetFieldAlias (DataField.LotExpirationDate),
                fieldsTable.GetFieldAlias (DataField.LotProductionDate),
                fieldsTable.GetFieldAlias (DataField.LotLocation)) : string.Empty;
        }

        private string GetReportStoreLotJoin (DataQuery querySet)
        {
            return querySet.UseLots ? "LEFT JOIN lots ON store.LotID = lots.ID" : string.Empty;
        }

        private string GetReportStoreLotGroup (DataQuery querySet)
        {
            return querySet.UseLots ? @", store.Lot, lots.SerialNo, lots.EndDate, lots.ProductionDate, lots.Location" : string.Empty;
        }
    }
}
