//
// DataProvider.Location.cs
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
using System.Collections.Generic;
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetAllLocations<T> (long? groupId, bool includeDeleted)
        {
            if (groupId.HasValue) {
                if (groupId == int.MinValue) {
                    return ExecuteLazyModel<T> (false, string.Format (@"
                        SELECT {0}
                        FROM objects
                        WHERE Deleted = -1", LocationDefaultAliases ()));
                }
                return ExecuteLazyModel<T> (false, string.Format (@"
                    SELECT {0}
                    FROM objects 
                    WHERE ABS(GroupID) = @groupId AND Deleted <> -1", LocationDefaultAliases ()),
                    new DbParam ("groupId", groupId.Value));
            }

            return ExecuteLazyModel<T> (false, string.Format (@"
                SELECT {0}
                FROM objects
                WHERE {1}",
                LocationDefaultAliases (),
                includeDeleted ? "1 = 1" : "Deleted <> -1"));
        }

        /// <summary>
        /// Gets the ID-s of all points of sale.
        /// </summary>
        /// <returns>
        /// A list of the ID-s of all points of sale.
        /// </returns>
        public override List<long> GetAllLocationIds ()
        {
            return ExecuteList<long> (@"SELECT objects.ID FROM objects WHERE Deleted <> -1");
        }

        public override T GetLocationById<T> (long locationId)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM objects WHERE ID = @locationId", LocationDefaultAliases ()),
                new DbParam ("locationId", locationId));
        }

        public override T GetLocationByName<T> (string locationName)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM objects WHERE Name = @locationName AND Deleted <> -1", LocationDefaultAliases ()),
                new DbParam ("locationName", locationName));
        }

        public override T GetLocationByCode<T> (string locationCode)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM objects WHERE Code = @locationCode AND Deleted <> -1", LocationDefaultAliases ()),
                new DbParam ("locationCode", locationCode));
        }

        public override List<long> GetAllLocationsWithRestaurantOrders (long? userId)
        {
            string query = string.Format (@"
                SELECT DISTINCT(ObjectID)
                FROM operations
                WHERE operations.Acct <= 0 AND operations.OperType = {0}{1}",
                (int) OperationType.RestaurantOrder,
                userId.HasValue ? " AND operations.OperatorID = @userId" : string.Empty);

            List<DbParam> pars = new List<DbParam> ();
            if (userId.HasValue)
                pars.Add (new DbParam ("userId", userId.Value));

            return ExecuteList<long> (query, pars.ToArray ());
        }

        public override bool LocationHasRestaurantOrders (long? locationId, long? partnerId, long? userId)
        {
            string query = string.Format (@"
                SELECT count(1)
                FROM operations
                WHERE operations.Acct <= 0 AND operations.OperType = {0}{1}{2}{3}",
                (int) OperationType.RestaurantOrder,
                locationId.HasValue ? " AND operations.ObjectID = @locationId" : string.Empty,
                partnerId.HasValue ? " AND operations.PartnerID = @partnerId" : string.Empty,
                userId.HasValue ? " AND operations.OperatorID = @userId" : string.Empty);

            List<DbParam> pars = new List<DbParam> ();
            if (locationId.HasValue)
                pars.Add (new DbParam ("locationId", locationId.Value));
            if (partnerId.HasValue)
                pars.Add (new DbParam ("partnerId", partnerId.Value));
            if (userId.HasValue)
                pars.Add (new DbParam ("userId", userId.Value));

            return ExecuteScalar<long> (query, pars.ToArray ()) > 0;
        }

        public override bool LocationHasChildLocationWithRestaurantOrders (long locationId)
        {
            return ExecuteScalar<int> (string.Format (@"SELECT IFNULL(SUM(Qtty), 0) > 0 FROM operations 
                 WHERE operations.OperType = {0} AND operations.SrcDocID=@locationId",
                 (int) OperationType.RestaurantOrder),
                 new DbParam ("locationId", locationId)) == 1;
        }

        #endregion

        #region Save / Delete

        public override void AddUpdateLocation (object locationObject, bool documentNumbersPerLocation, long recommendedRange)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (locationObject);

            using (DbTransaction transaction = new DbTransaction (this)) {

                // Check if we already have that item
                long temp = ExecuteScalar<long> ("SELECT count(*) FROM objects WHERE ID = @ID", helper.Parameters);

                // We are updating location
                if (temp == 1) {
                    temp = ExecuteNonQuery (string.Format ("UPDATE objects {0} WHERE ID = @ID",
                        helper.GetSetStatement (DataField.LocationId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot update location with ID={0}", helper.GetObjectValue (DataField.LocationId)));
                } // We are creating new location
                else if (temp == 0) {
                    temp = ExecuteNonQuery (string.Format ("INSERT INTO objects {0}",
                        helper.GetColumnsAndValuesStatement (DataField.LocationId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot add location with name=\'{0}\'", helper.GetObjectValue (DataField.LocationName)));

                    temp = GetLastAutoId ();
                    helper.SetObjectValue (DataField.LocationId, temp);

                    ItemsManagementType imt = GetItemsManagementType ();
                    if (imt == ItemsManagementType.AveragePrice || imt == ItemsManagementType.QuickAveragePrice || imt == ItemsManagementType.LastPurchasePrice) {
                        // Add store availability for the items in the new location
                        ExecuteNonQuery ("INSERT INTO store (ObjectID, GoodID, Qtty, Price, Lot, LotID, LotOrder) SELECT @ObjectID, ID, 0, PriceIn, ' ', 1, 1 FROM goods",
                            new DbParam ("ObjectID", temp));
                    }

                    if (documentNumbersPerLocation) {
                        List<long> documentNumbers = ExecuteList<long> (@"
                            SELECT Acct FROM operations 
                            WHERE PartnerID = 0 OR PartnerID IS NULL 
                            GROUP BY ObjectID
                            ORDER BY Acct DESC");

                        if (documentNumbers.Count > 1) {
                            long maxRangeSize = long.MinValue;
                            for (int i = 0; i < documentNumbers.Count - 1; i++)
                                maxRangeSize = Math.Max (maxRangeSize, documentNumbers [i] - documentNumbers [i + 1]);
                            AddOperationStartNumbersPerLocation (temp, documentNumbers [0] + maxRangeSize, null);
                        } else {
                            long locationIndex = ExecuteScalar<long> ("SELECT COUNT(*) - 1 FROM objects");
                            long rangeStart = documentNumbers.Count == 1 ? documentNumbers [0] : 0;
                            AddNumberingPerLocationByIndex (temp, locationIndex - 1, rangeStart + 1, null, recommendedRange);
                        }
                    }

                    OnLocationAdded (new LocationAddedArgs { LocationId = temp });
                } else
                    throw new Exception ("Too many entries with the same ID found in objects table.");

                transaction.Complete ();
            }
        }

        public override void DeleteLocation (long locationId)
        {
            DbParam par = new DbParam ("locationId", locationId);

            using (DbTransaction transaction = new DbTransaction (this)) {
                ExecuteNonQuery ("DELETE FROM objects WHERE ID = @locationId", par);
                ExecuteNonQuery ("DELETE FROM store WHERE ObjectID = @locationId", par);
                ExecuteNonQuery ("DELETE FROM operations WHERE ObjectID = @locationId AND (operations.PartnerID = 0 OR PartnerID IS NULL)", par);
                transaction.Complete ();
            }
        }

        public override DeletePermission CanDeleteLocation (long locationId)
        {
            if (locationId == 1)
                return DeletePermission.Reserved;

            long ret = ExecuteScalar<long> (@"SELECT count(*) FROM operations 
                WHERE ObjectID = @locationId AND operations.PartnerID <> 0",
                new DbParam ("locationId", locationId));

            return 0 == ret ? DeletePermission.Yes : DeletePermission.InUse;
        }

        #endregion

        #region Reports

        public override DataQueryResult ReportLocations (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT objects.ID, objects.Code as {0}, objects.Name as {1}, objectsgroups.Name
                FROM objects LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID",
                fieldsTable.GetFieldAlias (DataField.LocationCode),
                fieldsTable.GetFieldAlias (DataField.LocationName));

            querySet.SetSimpleId (DbTable.Objects, DataField.LocationId, 0);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportLocationsByProfit (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT objects.ID, operations.Date as {0}, goods.Code as {1}, goods.Name as {2},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.Name, objects.Name as {3}, objectsgroups.Name, 
                  users1.Name as {4}, usersgroups1.Name, partners.Company, partnersgroups.Name,
                  SUM(-{11} * operations.Qtty * (operations.PriceOut - operations.PriceIn )) as {5}
                FROM (((((((operations LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID
                WHERE operations.OperType IN ({6},{7},{8},{9},{10}) AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY goods.Code, operations.Date, goods.Name,
                 goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name,
                 objects.ID, objects.Name, objectsgroups.Name, users1.Name, usersgroups1.Name, partners.Company, partnersgroups.Name",
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationProfit),
                (int) OperationType.Sale,
                (int) OperationType.ConsignmentSale,
                (int) OperationType.Return,
                (int) OperationType.CreditNote,
                (int) OperationType.DebitNote,
                GetOperationsPriceSignQuery ());

            querySet.SetSimpleId (DbTable.Objects, DataField.LocationId, 0);

            return ExecuteDataQuery (querySet, query);
        }

        #endregion

        private string LocationDefaultAliases ()
        {
            return GetAliasesString (DataField.LocationId,
                DataField.LocationCode,
                DataField.LocationName,
                DataField.LocationName2,
                DataField.LocationOrder,
                DataField.LocationDeleted,
                DataField.LocationGroupId,
                DataField.LocationPriceGroup);
        }
    }
}
