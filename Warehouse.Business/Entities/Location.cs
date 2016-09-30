//
// Location.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   06/30/2006
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
using Warehouse.Business.Operations;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Entities
{
    public class Location : ICacheableEntity<Location>, IPersistableEntity<Location>, IStrongEntity, IHirarchicalEntity
    {
        private long id = -1;
        private string name = string.Empty;
        private string name2 = string.Empty;
        private int order = -1;
        private int deletedDb;
        private string code = string.Empty;
        private PriceGroup priceGroup = PriceGroup.RegularPrice;
        private long groupId = GroupBase<LocationsGroup>.DefaultGroupId;
        private string groupName;

        public const int DefaultId = 1;

        #region Public properties

        [DbColumn (DataField.LocationId)]
        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        [ExchangeProperty ("Name", true)]
        [DbColumn (DataField.LocationName, 255)]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [ExchangeProperty ("Name 2")]
        [DbColumn (DataField.LocationName2, 255)]
        public string Name2
        {
            get { return string.IsNullOrWhiteSpace (name2) ? name : name2; }
            set { name2 = value; }
        }

        [ExchangeProperty ("Code")]
        [DbColumn (DataField.LocationCode, 255)]
        public string Code
        {
            get { return code; }
            set { code = value; }
        }

        [DbColumn (DataField.LocationGroupId)]
        public long GroupId
        {
            get { return groupId; }
            set { groupId = value; }
        }

        [DbColumn (DataField.LocationOrder)]
        public int Order
        {
            get { return order; }
            set { order = value; }
        }

        public bool Deleted
        {
            get { return deletedDb == -1; }
            set { deletedDb = value ? -1 : 0; }
        }

        [DbColumn (DataField.LocationDeleted)]
        public int DeletedDb
        {
            get { return deletedDb; }
            set { deletedDb = value; }
        }

        [ExchangeProperty ("Group Name", false, DataField.LocationsGroupsName)]
        public string GroupName
        {
            get
            {
                if (!string.IsNullOrEmpty (groupName))
                    return groupName;

                return GroupBase<LocationsGroup>.GetPath (Math.Abs (groupId), LocationsGroup.Cache);
            }
            set { groupName = value; }
        }

        [ExchangeProperty ("Price Group")]
        [DbColumn (DataField.LocationPriceGroup)]
        public PriceGroup PriceGroup
        {
            get { return priceGroup; }
            set { priceGroup = value; }
        }

        #endregion

        private static readonly CacheEntityCollection<Location> cache = new CacheEntityCollection<Location> ();
        public static CacheEntityCollection<Location> Cache
        {
            get { return cache; }
        }

        public Location CommitChanges ()
        {
            if (BusinessDomain.AppConfiguration.AutoGenerateLocationCodes && string.IsNullOrWhiteSpace (code))
                AutoGenerateCode ();

            if (!string.IsNullOrEmpty (groupName) && groupId <= 1) {
                LocationsGroup g = GroupBase<LocationsGroup>.EnsureByPath (groupName, LocationsGroup.Cache);
                groupId = g.Id;
            }

            BusinessDomain.DataAccessProvider.AddUpdateLocation (this,
                BusinessDomain.AppConfiguration.DocumentNumbersPerLocation,
                OperationNumberingInfo.RECOMMENDED_NUMBERS_PER_LOCATION);
            cache.Set (this);

            return this;
        }

        public void AutoGenerateCode ()
        {
            string pattern = BusinessDomain.AppConfiguration.LocationCodePattern;
            ulong lastCode = BusinessDomain.DataAccessProvider.GetMaxCodeValue (DbTable.Objects, pattern);
            code = CodeGenerator.GenerateCode (pattern, lastCode + 1);
        }

        public static DeletePermission RequestDelete (long locationId)
        {
            return BusinessDomain.DataAccessProvider.CanDeleteLocation (locationId);
        }

        public static void Delete (long locationId)
        {
            BusinessDomain.DataAccessProvider.DeleteLocation (locationId);
            cache.Remove (locationId);
        }

        public static LazyListModel<Location> GetAll (long? groupId = null, bool includeDeleted = false)
        {
            return BusinessDomain.DataAccessProvider.GetAllLocations<Location> (groupId, includeDeleted);
        }

        public static Location GetById (long locationId)
        {
            Location ret = BusinessDomain.DataAccessProvider.GetLocationById<Location> (locationId);
            cache.Set (ret);
            return ret;
        }

        public static Location GetByName (string locationName)
        {
            Location ret = BusinessDomain.DataAccessProvider.GetLocationByName<Location> (locationName);
            cache.Set (ret);
            return ret;
        }

        public static Location GetByCode (string locationCode)
        {
            Location ret = BusinessDomain.DataAccessProvider.GetLocationByCode<Location> (locationCode);
            cache.Set (ret);
            return ret;
        }

        public static bool TryGetLocked (ref Location location)
        {
            if (BusinessDomain.LoggedUser.LockedLocationId > 0) {
                location = GetById (BusinessDomain.LoggedUser.LockedLocationId);
                return true;
            }

            LazyListModel<Location> allLocations = GetAll ();
            if (allLocations.Count == 1)
                location = allLocations [0];

            return false;
        }

        public static long GetDefaultId ()
        {
            long defaultLocationId = BusinessDomain.LoggedUser.LockedLocationId;
            return defaultLocationId > 0 ? defaultLocationId : DefaultId;
        }

        public static List<long> GetAllLocationsWithRestaurantOrders (long? userId = null)
        {
            return BusinessDomain.DataAccessProvider.GetAllLocationsWithRestaurantOrders (userId);
        }

        public static bool HasRestaurantOrders (long? locationId = null, long? partnerId = null, long? userId = null)
        {
            return BusinessDomain.DataAccessProvider.LocationHasRestaurantOrders (locationId, partnerId, userId);
        }

        public static bool HasChildLocationWithOrders (long locationId)
        {
            return BusinessDomain.DataAccessProvider.LocationHasChildLocationWithRestaurantOrders (locationId);
        }

        #region Implementation of IStrongEntity

        public bool Validate (ValidateCallback callback, StateHolder state)
        {
            if (callback == null)
                throw new ArgumentNullException ("callback");

            if (string.IsNullOrEmpty (name)) {
                if (!callback (Translator.GetString ("Location name cannot be empty!"), ErrorSeverity.Error, 0, state))
                    return false;
            }

            Location p = GetByName (name);
            if (p != null && p.Id != id) {
                if (!callback (string.Format (Translator.GetString ("Location with the name \"{0}\" already exists! Do you want to save the location anyway?"), name),
                    ErrorSeverity.Warning, 1, state))
                    return false;
            }

            if (!string.IsNullOrEmpty (code)) {
                p = GetByCode (code);
                if (p != null && p.Id != id) {
                    if (!callback (string.Format (Translator.GetString ("Location with the code \"{0}\" already exists. Do you want to save the location anyway?"), code),
                        ErrorSeverity.Warning, 2, state))
                        return false;
                }
            }

            return true;
        }

        #endregion

        #region Overrides of ICacheableEntityBase<Location>

        public Location GetEntityById (long entityId)
        {
            return GetById (entityId);
        }

        public Location GetEntityByCode (string entityCode)
        {
            return GetByCode (entityCode);
        }

        public Location GetEntityByName (string entityName)
        {
            return GetByName (entityName);
        }

        public IEnumerable<Location> GetAllEntities ()
        {
            return GetAll ();
        }

        public object Clone ()
        {
            return MemberwiseClone ();
        }

        #endregion
    }
}