//
// DataProvider.Item.cs
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
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetAllItems<T> (long? groupId, bool onlyAvailable, bool autoStart, bool includeDeleted)
        {
            string query = string.Format (@"
                SELECT IFNULL(SUM(store.Qtty), 0) - 
                  (SELECT IFNULL(SUM(Qtty), 0) FROM operations 
                   WHERE operations.GoodID = goods.ID AND operations.OperType IN ({0},{1})) AS {2}, {3}
                FROM (goods LEFT JOIN store ON goods.ID = store.GoodID)
                WHERE {{0}}
                GROUP BY goods.ID{4}",
                (int) OperationType.RestaurantOrder,
                (int) OperationType.SalesOrder,
                fieldsTable.GetFieldAlias (DataField.StoreQtty),
                ItemDefaultAliases,
                onlyAvailable ?
                    string.Format (" HAVING {0} > 0 OR ({1} & @nonInv = @nonInv)",
                        fieldsTable.GetFieldAlias (DataField.StoreQtty),
                        fieldsTable.GetFieldAlias (DataField.ItemType)) :
                    string.Empty);

            if (!groupId.HasValue)
                return ExecuteLazyModel<T> (autoStart, string.Format (query, includeDeleted ? "1 = 1" : "Deleted <> -1"),
                    new DbParam ("nonInv", (int) ItemType.NonInventory));

            if (groupId == int.MinValue)
                return ExecuteLazyModel<T> (autoStart, string.Format (query, "Deleted = -1"),
                    new DbParam ("nonInv", (int) ItemType.NonInventory));

            return ExecuteLazyModel<T> (autoStart, string.Format (query, "ABS(GroupID) = @groupId" + (includeDeleted ? string.Empty : " AND Deleted <> -1")),
                new DbParam ("nonInv", (int) ItemType.NonInventory),
                new DbParam ("groupId", groupId.Value));
        }

        public override LazyListModel<T> GetAllItemsByLocation<T> (long locationId, long? groupId, bool onlyAvailable, bool autoStart)
        {
            List<DbParam> pars = new List<DbParam>
                {
                    new DbParam ("locationId", locationId),
                    new DbParam ("nonInv", (int) ItemType.NonInventory)
                };
            if (groupId.HasValue)
                pars.Add (new DbParam ("groupId", groupId.Value));

            string query = string.Format (
                @"SELECT {0} AS {1}, {2}
                FROM (goods LEFT JOIN store ON goods.ID = store.GoodID AND store.ObjectID = @locationId)
                {3}
                GROUP BY goods.ID, store.ObjectID{4}",
                GetStoreQuantity (pars, -1),
                fieldsTable.GetFieldAlias (DataField.StoreQtty),
                ItemDefaultAliases,
                groupId.HasValue ? (groupId == int.MinValue ? "WHERE Deleted = -1" : "WHERE ABS(goods.GroupID) = @groupId AND Deleted <> -1") : "WHERE Deleted <> -1",
                onlyAvailable ?
                    string.Format (" HAVING {0} > 0 OR ({1} & @nonInv = @nonInv)",
                        fieldsTable.GetFieldAlias (DataField.StoreQtty),
                        fieldsTable.GetFieldAlias (DataField.ItemType)) :
                    string.Empty);

            return ExecuteLazyModel<T> (autoStart, query, pars.ToArray ());
        }

        public override LazyListModel<T> GetAllAvailableItemsAtLocation<T> (long locationId, long? groupId)
        {
            List<DbParam> pars = new List<DbParam>
                {
                    new DbParam ("locationId", locationId),
                    new DbParam ("nonInv", (int) ItemType.NonInventory)
                };
            if (groupId.HasValue)
                pars.Add (new DbParam ("groupId", groupId.Value));

            string query = string.Format (@"
                SELECT SUM(store.Qtty) as {0}, {1}
                FROM (goods LEFT JOIN store ON goods.ID = store.GoodID AND store.ObjectID = @locationId)
                {2}
                GROUP BY goods.ID, store.ObjectID
                HAVING SUM(store.Qtty) > 0 OR ({3} & @nonInv = @nonInv)",
                fieldsTable.GetFieldAlias (DataField.StoreQtty),
                ItemDefaultAliases,
                groupId.HasValue ? (groupId == int.MinValue ? "WHERE Deleted = -1" : "WHERE ABS(goods.GroupID) = @groupId AND Deleted <> -1") : "WHERE Deleted <> -1",
                fieldsTable.GetFieldAlias (DataField.ItemType));

            return ExecuteLazyModel<T> (query, pars.ToArray ());
        }

        public override ItemType? GetItemType (long itemId)
        {
            return (ItemType?) ExecuteScalar<int?> ("SELECT Type FROM goods WHERE ID = @id", new DbParam ("id", itemId));
        }

        public override double GetItemAvailability (long itemId, long locationId, long childLocationId)
        {
            List<DbParam> pars = new List<DbParam>
                {
                    new DbParam ("id", itemId), 
                    new DbParam ("locationId", locationId)
                };

            double? ret = ExecuteScalar<double?> (string.Format (@"
                SELECT {0}
                FROM (goods LEFT JOIN store ON goods.ID = store.GoodID AND store.ObjectID = @locationId)
                WHERE (goods.ID = @id)
                GROUP BY goods.ID, store.ObjectID",
                GetStoreQuantity (pars, childLocationId)), pars.ToArray ());

            return ret ?? 0;
        }

        public override double GetItemAvailabilityAtDate (long itemId, long locationId, DateTime date)
        {
            double? ret = ExecuteScalar<double?> (string.Format (@"
                SELECT IFNULL(SUM(operations.Qtty * operations.Sign), 0)
                FROM operations
                WHERE operations.GoodID = @id AND operations.ObjectID = @locationId{0} AND operations.PartnerID <> 0 AND operations.Acct > 0",
                date == DateTime.MaxValue ? string.Empty : " AND operations.Date <= @date"),
                new DbParam ("id", itemId),
                new DbParam ("locationId", locationId),
                new DbParam ("date", date));

            return ret ?? 0;
        }

        public override long GetMaxBarcodeSubNumber (string prefix, int barcodeLength, int subNumberStart, int subNumberLen)
        {
            DbParam paramPrefix = new DbParam ("prefix", prefix + "%");
            DbParam paramLength = new DbParam ("length", barcodeLength);
            DbParam paramSubStart = new DbParam ("subStart", subNumberStart + 1);
            DbParam paramSubLen = new DbParam ("subLen", subNumberLen);

            long ret = -1;

            foreach (string barcodeColumn in new [] { "BarCode1", "BarCode2", "BarCode3" }) {
                long maxNumber = ExecuteScalar<long> (string.Format (@"SELECT IFNULL(MAX(CAST(SUBSTR(REPLACE({0}, '.', ''), @subStart, @subLen) AS UNSIGNED)), -1)
                    FROM goods
                    WHERE {0} LIKE @prefix AND LENGTH(REPLACE({0}, '.', '')) = @length", barcodeColumn),
                    paramPrefix, paramLength, paramSubStart, paramSubLen);

                ret = Math.Max (ret, maxNumber);
            }

            return ret;
        }

        private static string GetStoreQuantity (ICollection<DbParam> pars, long childLocationId)
        {
            pars.Add (new DbParam ("childLocationId", childLocationId));
            return string.Format (@"IFNULL(SUM(store.Qtty), 0) - (SELECT IFNULL(SUM(Qtty), 0) FROM operations 
                WHERE operations.GoodID = goods.ID AND (
                (operations.OperType = {0} AND operations.SrcDocID = @locationId AND operations.ObjectID <> @childLocationId) OR
                (operations.OperType = {1} AND operations.ObjectID = @locationId)))",
                (int) OperationType.RestaurantOrder,
                (int) OperationType.SalesOrder);
        }

        public override T GetItemById<T> (long id)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM goods
                WHERE goods.ID = @id", ItemDefaultAliases),
                new DbParam ("id", id));
        }

        public override T GetItemByName<T> (string name)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM goods
                WHERE goods.Name = @goodsName AND Deleted <> -1", ItemDefaultAliases),
                new DbParam ("goodsName", name));
        }

        public override T GetItemByCode<T> (string code)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM goods
                WHERE goods.Code = @goodsCode AND Deleted <> -1", ItemDefaultAliases),
                new DbParam ("goodsCode", code));
        }

        public override T GetItemByBarcode<T> (string barcode)
        {
            string query = string.Format (@"
                SELECT {0}
                FROM goods
                WHERE ((@barCode LIKE REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(BarCode1, '.', ''), 'W', '_'), 'w', '_'), 'C', '_'), 'c', '_'), 'L', '_'), 'l', '_')) OR
                  (@barCode LIKE REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(BarCode2, '.', ''), 'W', '_'), 'w', '_'), 'C', '_'), 'c', '_'), 'L', '_'), 'l', '_')) OR
                  BarCode3 REGEXP {1}) AND Deleted <> -1", ItemDefaultAliases, GetConcatStatement ("'(^|,)'", "@barCode", "'($|,)'"));

            return ExecuteObject<T> (query,
                new DbParam ("barCode", Regex.Escape (barcode)));
        }

        public override T GetItemByCatalog<T> (string catalog)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM goods
                WHERE (Catalog1 = @catalog OR Catalog2 = @catalog OR Catalog3 = @catalog) AND Deleted <> -1", ItemDefaultAliases),
                new DbParam ("catalog", catalog));
        }

        public override T GetItemBySerialNumber<T> (string serial, long locationId, ItemsManagementType imt)
        {
            if (string.IsNullOrWhiteSpace (serial))
                return default (T);

            DbParam [] pars = { new DbParam ("serial", serial), new DbParam ("locationId", locationId) };

            string query = string.Format (@"
                SELECT {0}, store.ID as {1}
                FROM (store INNER JOIN lots ON store.LotID = lots.ID) INNER JOIN goods ON store.GoodID = goods.ID
                WHERE store.ObjectID = @locationId AND lots.SerialNo = @serial AND goods.Deleted <> -1
                ORDER BY {{0}}
                LIMIT 1", ItemDefaultAliases, fieldsTable.GetFieldAlias (DataField.StoreQtty));

            switch (imt) {
                case ItemsManagementType.AveragePrice:
                case ItemsManagementType.QuickAveragePrice:
                case ItemsManagementType.LastPurchasePrice:
                    return default (T);
                case ItemsManagementType.FIFO:
                case ItemsManagementType.Choice:
                    return ExecuteObject<T> (string.Format (query, "store.LotOrder ASC"), pars);
                case ItemsManagementType.LIFO:
                    return ExecuteObject<T> (string.Format (query, "store.LotOrder DESC"), pars);
                case ItemsManagementType.FEFO:
                    return ExecuteObject<T> (string.Format (query, "lots.EndDate ASC"), pars);
                default:
                    throw new ArgumentOutOfRangeException ("imt");
            }
        }

        #endregion

        #region Save / Delete

        public override void AddUpdateItem (object itemObject)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (itemObject);

            using (DbTransaction transaction = new DbTransaction (this)) {

                // Check if we already have that item
                long temp = ExecuteScalar<long> ("SELECT count(*) FROM goods WHERE ID = @ID", helper.Parameters);

                // We are updating item information
                if (temp == 1) {
                    temp = ExecuteNonQuery (string.Format ("UPDATE goods {0} WHERE ID = @ID",
                        helper.GetSetStatement (DataField.ItemId, DataField.StoreQtty)),
                        helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot update goods with ID={0}", helper.GetObjectValue (DataField.ItemId)));
                } // We are creating new item information
                else if (temp == 0) {
                    temp = ExecuteNonQuery (string.Format ("INSERT INTO goods {0}",
                        helper.GetColumnsAndValuesStatement (DataField.ItemId, DataField.StoreQtty)),
                        helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot add goods with name=\'{0}\'", helper.GetObjectValue (DataField.ItemName)));

                    temp = GetLastAutoId ();
                    helper.SetObjectValue (DataField.ItemId, temp);

                    ItemsManagementType imt = GetItemsManagementType ();
                    if (imt == ItemsManagementType.AveragePrice ||
                        imt == ItemsManagementType.QuickAveragePrice ||
                        imt == ItemsManagementType.LastPurchasePrice) {
                        // Add store availability for the items in the new location
                        ExecuteNonQuery ("INSERT INTO store (ObjectID, GoodID, Qtty, Price, Lot, LotID, LotOrder) SELECT ID, @GoodID, 0, 0, ' ', 1, 1 FROM objects",
                            new DbParam ("GoodID", temp));
                    }
                } else
                    throw new Exception ("Too many entries with the same ID found in goods table.");

                transaction.Complete ();
            }
        }

        public override void DeleteItem (long id)
        {
            DbParam par = new DbParam ("id", id);

            using (DbTransaction transaction = new DbTransaction (this)) {
                ExecuteNonQuery ("DELETE FROM goods WHERE ID = @id", par);
                ExecuteNonQuery ("DELETE FROM store WHERE GoodID = @id", par);
                transaction.Complete ();
            }
        }

        public override DeletePermission CanDeleteItem (long id)
        {
            if (id == 1)
                return DeletePermission.Reserved;

            if (id < 0)
                return DeletePermission.No;

            long ret = ExecuteScalar<long> ("SELECT count(*) FROM operations WHERE GoodID = @id", new DbParam ("id", id));

            return 0 == ret ? DeletePermission.Yes : DeletePermission.InUse;
        }

        #endregion

        #region Items Reports

        public override DataQueryResult ReportItems (DataQuery querySet)
        {
            querySet.SetSimpleId (DbTable.Items, DataField.ItemId, 0);

            return ExecuteDataQuery (querySet, @"
                SELECT goods.ID, goods.Code, goods.Name, goodsgroups.Name, goods.Measure1, goods.Measure2, goods.Ratio,
                  goods.PriceIn, goods.PriceOut1, goods.PriceOut2, goods.PriceOut3, goods.PriceOut4, 
                  goods.PriceOut5, goods.PriceOut6, goods.PriceOut7, goods.PriceOut8,  goods.PriceOut9, goods.PriceOut10,
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goods.MinQtty, goods.NormalQtty
                FROM goods LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID");
        }

        public override DataQueryResult ReportItemsAvailability (DataQuery querySet, string quantityTranslation)
        {
            StringBuilder sb = new StringBuilder ();
            string defLocationName = string.Empty;
            string availFormat = string.Format ("SUM(CASE WHEN objects.ID = {{1}} THEN store.Qtty ELSE 0 END) AS `{0}`,", quantityTranslation);
            string singleLocationName = string.Empty;

            // Check if we are filtering by location
            foreach (DataFilter filter in querySet.Filters.Where (f => f.IsValid)) {
                if (filter.FilteredFields.Any (filterField => filterField == DataField.LocationName))
                    singleLocationName = (string) filter.Values [0];

                if (!string.IsNullOrEmpty (singleLocationName))
                    break;
            }

            SqlHelper helper = GetSqlHelper ();
            int i = 0;

            DbParam par;
            int translatedColNumber = querySet.UseLots ? 16 : 11;
            if (string.IsNullOrEmpty (singleLocationName)) {
                List<KeyValuePair<string, long>> locations = new List<KeyValuePair<string, long>> ();
                using (IDataReader reader = ExecuteReader ("SELECT ID, Name FROM objects"))
                    while (reader.Read ()) {
                        locations.Add (new KeyValuePair<string, long> ((string) reader.GetValue (1), Convert.ToInt64 (reader.GetValue (0))));
                    }

                List<string> usedNames = new List<string> ();

                for (int j = 0; j < locations.Count; j++) {
                    KeyValuePair<string, long> obj = locations [j];
                    string name = obj.Key;
                    long id = obj.Value;
                    int c = 0;
                    if (usedNames.Contains (name))
                        while (locations.Count (o => o.Key == name) > 1) {
                            name = string.Format ("{0} # {1}", obj.Key, ++c);
                            locations [j] = new KeyValuePair<string, long> (name, id);
                        }

                    usedNames.Add (name);
                    par = new DbParam ("locationId" + i, id);
                    helper.AddParameters (par);

                    if (id == 1)
                        defLocationName = name;
                    else
                        sb.AppendFormat (availFormat, name, fieldsTable.GetParameterName (par.ParameterName));

                    querySet.TranslatedColumns.Add (translatedColNumber++);
                    i++;
                }

                if (string.IsNullOrEmpty (defLocationName))
                    throw new Exception ("Default location not found.");

                sb.AppendFormat (availFormat, defLocationName, 1);
            } else {
                par = new DbParam ("LocationName", singleLocationName);
                object locationIdObject = ExecuteScalar ("SELECT ID FROM objects WHERE Name = @LocationName", par);

                // If there is no such point of sale then return empty result set
                if (locationIdObject == null || IsDBNull (locationIdObject))
                    return new DataQueryResult (querySet) { Result = new LazyTableModel () };

                par = new DbParam ("locationId", Convert.ToInt64 (locationIdObject));
                helper.AddParameters (par);
                sb.AppendFormat (availFormat, singleLocationName, fieldsTable.GetParameterName (par.ParameterName));
                querySet.TranslatedColumns.Add (translatedColNumber);

                // If we are showing a single location quantities hide the total quantities column
                querySet.Filters.Add (new DataFilter (DataField.StoreAvailableQuantity) { ShowColumns = false });
            }

            helper.AddParameters (new DbParam ("nonInv", (int) ItemType.NonInventory));

            string query = string.Format (@"
                SELECT goods.ID, goods.Code, goods.Name, goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, 
                  goods.Catalog2, goods.Catalog3, goods.Measure1, goodsgroups.Name,{9}
                  {0}
                  SUM(store.Qtty) as {1},
                  {14} as {2},
                  SUM(store.Qtty * {14}) as {3},
                  SUM(store.Qtty * {14}){12} as {4},
                  goods.PriceOut2 as {5},
                  (SUM(store.Qtty) * goods.PriceOut2) as {6},
                  (SUM(store.Qtty) * goods.PriceOut2){13} as {7},
                  ' ' as {8}
                FROM (((((goods LEFT JOIN store ON store.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)                 
                  LEFT JOIN objects ON objects.ID = store.ObjectID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN vatgroups ON goods.TaxGroup = vatgroups.ID){10}
                WHERE goods.Deleted = 0 AND (goods.Type & @nonInv = 0)
                GROUP BY goods.ID, goods.Code, goods.Name, goods.PriceOut2, goods.BarCode1, 
                  goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goods.Measure1, goodsgroups.Name, {14}, vatgroups.VATValue{11}",
                sb,
                fieldsTable.GetFieldAlias (DataField.StoreAvailableQuantity),
                fieldsTable.GetFieldAlias (DataField.StorePrice),
                fieldsTable.GetFieldAlias (DataField.ItemTradeInSum),
                fieldsTable.GetFieldAlias (DataField.ItemTradeInVAT),
                fieldsTable.GetFieldAlias (DataField.ItemRegularPrice),
                fieldsTable.GetFieldAlias (DataField.ItemTradeSum),
                fieldsTable.GetFieldAlias (DataField.ItemTradeVAT),
                fieldsTable.GetFieldAlias (DataField.StoreCountedQuantity),
                GetReportStoreLotSelect (querySet),
                GetReportStoreLotJoin (querySet),
                GetReportStoreLotGroup (querySet),
                querySet.VATIsIncluded ? " - (SUM(store.Qtty * store.Price) / (1 + vatgroups.VATValue / 100))" : " * vatgroups.VATValue / 100",
                querySet.VATIsIncluded ? " - (SUM(store.Qtty * goods.PriceOut2) / (1 + vatgroups.VATValue / 100))" : " * vatgroups.VATValue / 100",
                querySet.UseLots ? "store.Price" : "goods.PriceIn");

            querySet.SetSimpleId (DbTable.Items, DataField.ItemId, 0);

            DataQueryResult result = ExecuteDataQuery (querySet, query, helper.Parameters);

            for (i = 0; i < result.Columns.Length; i++) {
                if (result.Columns [i].IsTranslated)
                    result.Columns [i].Field = new DbField (DataField.StoreQtty);
            }

            return result;
        }

        public override DataQueryResult ReportItemsAvailabilityAtDate (DataQuery querySet, string dateString)
        {
            string query = string.Format (@"
                SELECT goods.ID, '{0}' as {1}, objects.Name as {2}, objectsgroups.Name, goods.Code as {3},
                  goods.BarCode1 as {4}, goods.BarCode2 as {5}, goods.BarCode3 as {6}, 
                  goods.Catalog1 as {7}, goods.Catalog2 as {8}, goods.Catalog3 as {9},
                  goods.Name as {10}, goods.Measure1 as {11}, goodsgroups.Name,
                  SUM(IFNULL(operations.Qtty * operations.Sign, 0)) as {12},
                  goods.PriceIn as {13},
                  SUM(IFNULL(operations.Qtty * {21}, 0)) * goods.PriceIn as {14},
                  SUM(IFNULL(operations.Qtty * {21}, 0)) * goods.PriceIn{16} as {15},
                  goods.PriceOut2 as {17},
                  SUM(IFNULL(operations.Qtty * {21}, 0)) * goods.PriceOut2 as {18},
                  SUM(IFNULL(operations.Qtty * {21}, 0)) * goods.PriceOut2{20} as {19}
                FROM ((((operations
                  LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)                 
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN vatgroups ON goods.TaxGroup = vatgroups.ID
                WHERE operations.PartnerID <> 0 AND operations.Acct > 0 AND (goods.Type & @nonInv = 0)
                GROUP BY goods.Code, goods.Name, goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goods.ID, goods.Measure1, goodsgroups.Name, objects.ID, objects.Name, objectsgroups.Name, goods.PriceIn, goods.PriceOut2, vatgroups.VATValue", dateString,
                fieldsTable.GetFieldAlias (DataField.ReportDate),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemBarcode1),
                fieldsTable.GetFieldAlias (DataField.ItemBarcode2),
                fieldsTable.GetFieldAlias (DataField.ItemBarcode3),
                fieldsTable.GetFieldAlias (DataField.ItemCatalog1),
                fieldsTable.GetFieldAlias (DataField.ItemCatalog2),
                fieldsTable.GetFieldAlias (DataField.ItemCatalog3),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.ItemMeasUnit),
                fieldsTable.GetFieldAlias (DataField.StoreAvailableQuantity),
                fieldsTable.GetFieldAlias (DataField.ItemPurchasePrice),
                fieldsTable.GetFieldAlias (DataField.ItemTradeInSum),
                fieldsTable.GetFieldAlias (DataField.ItemTradeInVAT),
                querySet.VATIsIncluded ?
                    " - (SUM(IFNULL(operations.Qtty * " + GetOperationsPriceSignQuery () + ", 0)) * goods.PriceIn / (1 + vatgroups.VATValue / 100))" :
                    " * vatgroups.VATValue / 100",
                fieldsTable.GetFieldAlias (DataField.ItemRegularPrice),
                fieldsTable.GetFieldAlias (DataField.ItemTradeSum),
                fieldsTable.GetFieldAlias (DataField.ItemTradeVAT),
                querySet.VATIsIncluded ?
                    " - (SUM(IFNULL(operations.Qtty * " + GetOperationsPriceSignQuery () + ", 0)) * goods.PriceOut2 / (1 + vatgroups.VATValue / 100))" :
                    " * vatgroups.VATValue / 100",
                GetOperationsPriceSignQuery ());

            querySet.SetSimpleId (DbTable.Items, DataField.ItemId, 0);

            return ExecuteDataQuery (querySet, query, new DbParam ("nonInv", (int) ItemType.NonInventory));
        }

        public override DataQueryResult ReportItemsMinimalQuantities (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT objects.Name AS {0}, goodsgroups.Name AS {1}, 
                  goods.Code AS {2}, goods.Name AS {3}, goods.Measure1 AS {4}, 
                  SUM(IFNULL(store.Qtty, 0)) AS {5}, goods.MinQtty AS {6}  
                FROM (((((goods LEFT JOIN store ON store.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)                 
                  LEFT JOIN objects ON objects.ID = store.ObjectID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN vatgroups ON goods.TaxGroup = vatgroups.ID)
                WHERE goods.MinQtty > 0 
				GROUP BY goods.Code, objects.Name, goods.Name, 
                  goodsgroups.Code, goodsgroups.Name, goods.MinQtty, goods.Measure1 
                HAVING SUM(IFNULL(store.Qtty, 0)) <= goods.MinQtty",
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.ItemsGroupName),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.ItemMeasUnit),
                fieldsTable.GetFieldAlias (DataField.StoreAvailableQuantity),
                fieldsTable.GetFieldAlias (DataField.ItemMinQuantity));

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportItemsFlow (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT goods.ID, objects.Name as {0}, objectsgroups.Name, goods.Code as {1}, goods.Name as {2},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3, goods.Measure1,
                  goodsgroups.Name,{20}
                  SUM(operations.Qtty * {23}) as {3},
                  SUM(CASE WHEN operations.Opertype=1 THEN operations.Qtty ELSE 0 END) as {4},
                  SUM(CASE WHEN operations.Opertype=2 THEN operations.Qtty ELSE 0 END) as {5},
                  SUM(CASE WHEN operations.Opertype=3 THEN operations.Qtty ELSE 0 END) as {6},
                  SUM(CASE WHEN operations.Opertype=4 THEN operations.Qtty ELSE 0 END) as {7},
                  SUM(CASE WHEN operations.Opertype=7 THEN operations.Qtty ELSE 0 END) as {8},
                  SUM(CASE WHEN operations.Opertype=8 THEN operations.Qtty ELSE 0 END) as {9},
                  SUM(CASE WHEN operations.Opertype=11 THEN operations.Qtty ELSE 0 END) as {10},
                  SUM(CASE WHEN operations.Opertype=15 THEN operations.Qtty ELSE 0 END) as {11},
                  SUM(CASE WHEN operations.Opertype=16 THEN operations.Qtty ELSE 0 END) as {12},
                  SUM(CASE WHEN operations.Opertype=17 THEN operations.Qtty ELSE 0 END) as {13},
                  SUM(CASE WHEN operations.Opertype=24 THEN operations.Qtty ELSE 0 END) as {14},
                  SUM(CASE WHEN operations.Opertype=25 THEN operations.Qtty ELSE 0 END) as {15},
                  SUM(CASE WHEN operations.Opertype=26 THEN operations.Qtty ELSE 0 END) as {16},
                  SUM(CASE WHEN operations.Opertype=27 THEN operations.Qtty ELSE 0 END) as {17},
                  SUM(CASE WHEN operations.Opertype=34 THEN operations.Qtty ELSE 0 END) as {18},
                  SUM(CASE WHEN operations.Opertype=39 THEN operations.Qtty ELSE 0 END) as {19}
                FROM ((((operations LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)                 
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID){21}
                WHERE operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY objects.Name, objectsgroups.Name, goods.ID, goods.Code, goods.Name,
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3, goods.Measure1, goodsgroups.Name{22}",
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.OperationQuantitySignedSum),
                fieldsTable.GetFieldAlias (DataField.PurchaseQuantitySum),
                fieldsTable.GetFieldAlias (DataField.SaleQuantitySum),
                fieldsTable.GetFieldAlias (DataField.WasteQuantitySum),
                fieldsTable.GetFieldAlias (DataField.StockTakingQuantitySum),
                fieldsTable.GetFieldAlias (DataField.TransferInQuantitySum),
                fieldsTable.GetFieldAlias (DataField.TransferOutQuantitySum),
                fieldsTable.GetFieldAlias (DataField.WriteOffQuantitySum),
                fieldsTable.GetFieldAlias (DataField.ConsignedQuantitySum),
                fieldsTable.GetFieldAlias (DataField.ConsignedQuantitySoldSum),
                fieldsTable.GetFieldAlias (DataField.ConsignedQuantityReturnedSum),
                fieldsTable.GetFieldAlias (DataField.ProductionMatQuantitySum),
                fieldsTable.GetFieldAlias (DataField.ProductionProdQuantitySum),
                fieldsTable.GetFieldAlias (DataField.DebitNoteQuantitySum),
                fieldsTable.GetFieldAlias (DataField.CreditNoteQuantitySum),
                fieldsTable.GetFieldAlias (DataField.ReturnQuantitySum),
                fieldsTable.GetFieldAlias (DataField.PurchaseReturnQuantitySum),
                GetReportLotSelect (querySet),
                GetReportLotJoin (querySet),
                GetReportLotGroup (querySet),
                GetOperationsPriceSignQuery ());

            querySet.SetSimpleId (DbTable.Items, DataField.ItemId, 0);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportItemsByProfit (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT goods.ID, operations.Date as {0}, goods.Code as {1}, goods.Name as {2},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.Name, objects.Name as {3}, objectsgroups.Name, users1.Name as {4}, usersgroups1.Name,
                  partners.Company, partnersgroups.Name,
                  SUM(-{14} * operations.Qtty) as {5},
                  SUM(-{14} * operations.Qtty * operations.PriceIn) as {6},
                  SUM(-{14} * operations.Qtty * operations.PriceOut) as {7},
                  SUM(-{14} * operations.Qtty * (operations.PriceOut - operations.PriceIn)) as {8}
                FROM ((((((((operations LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                WHERE operations.OperType IN ({9},{10},{11},{12},{13}) AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY goods.ID, goods.Code, operations.Date, goods.Name,
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name, 
                  objects.Name, objectsgroups.Name, users1.Name, usersgroups1.Name, partners.Company, partnersgroups.Name",
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.SaleQuantitySum),
                fieldsTable.GetFieldAlias (DataField.PurchaseSum),
                fieldsTable.GetFieldAlias (DataField.SaleSum),
                fieldsTable.GetFieldAlias (DataField.OperationProfit),
                (int) OperationType.Sale,
                (int) OperationType.ConsignmentSale,
                (int) OperationType.Return,
                (int) OperationType.CreditNote,
                (int) OperationType.DebitNote,
                GetOperationsPriceSignQuery ());

            querySet.SetSimpleId (DbTable.Items, DataField.ItemId, 0);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportInvoicedItems (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT documents.InvoiceNumber, objects.Name AS {0}, objectsgroups.Name, 
                  partners.Company AS {1}, partnersgroups.Name,
                  users1.Name AS {2}, usersgroups1.Name, goods.Code AS {3}, goods.Name AS {4},
                  goods.BarCode1 AS {5}, goods.BarCode2 AS {6}, goods.BarCode3 AS {7}, 
                  goods.Catalog1 AS {8}, goods.Catalog2 AS {9}, goods.Catalog3 AS {10},
                  goods.Measure1, goodsgroups.Name,
                  SUM(-{16} * operations.Qtty) AS {11},
                  SUM(-{16} * operations.Qtty * operations.PriceOut) AS {12},
                  SUM(-{16} * operations.Qtty * operations.VATOut) AS {13},
                  SUM(-{16} * operations.Qtty * operations.PriceIn) AS {14},
                  SUM(-{16} * operations.Qtty * operations.VATIn) AS {15},
                  operations.OperType
                FROM (((((((((documents
                  LEFT JOIN operations ON operations.Acct = documents.Acct AND operations.OperType = documents.OperType)
                  LEFT JOIN goods ON goods.ID = operations.GoodID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                GROUP BY goods.Code, goods.Name, objects.Name, objectsgroups.Name, 
                  partners.Company, partnersgroups.Name, users1.Name, usersgroups1.Name, goods.BarCode1, 
                  goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3, 
                  documents.InvoiceNumber, goods.Measure1, goodsgroups.Name, users1.ID, operations.VATOut, operations.VATIn
                ORDER BY goods.Name, goods.Code",
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.PartnerName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.ItemBarcode1),
                fieldsTable.GetFieldAlias (DataField.ItemBarcode2),
                fieldsTable.GetFieldAlias (DataField.ItemBarcode3),
                fieldsTable.GetFieldAlias (DataField.ItemCatalog1),
                fieldsTable.GetFieldAlias (DataField.ItemCatalog2),
                fieldsTable.GetFieldAlias (DataField.ItemCatalog3),
                fieldsTable.GetFieldAlias (DataField.OperationQuantitySum),
                fieldsTable.GetFieldAlias (DataField.SaleSum),
                fieldsTable.GetFieldAlias (DataField.SaleVATSum),
                fieldsTable.GetFieldAlias (DataField.PurchaseSum),
                fieldsTable.GetFieldAlias (DataField.PurchaseVATSum),
                GetOperationsPriceSignQuery ());

            return ExecuteDataQuery (querySet, query);
        }

        #endregion
    }
}
