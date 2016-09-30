//
// Lot.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   03.11.2010
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
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Entities
{
    public class Lot
    {
        [DbColumn (DataField.LotId)]
        public long ID { get; set; }

        [DbColumn (DataField.StorePrice)]
        public double PriceIn { get; set; }

        [DbColumn (DataField.StoreQtty)]
        public double AvailableQuantity { get; set; }

        [DbColumn (DataField.StoreLot, 255)]
        public string Name { get; set; }

        [DbColumn (DataField.LotSerialNumber, 255)]
        public string SerialNumber { get; set; }

        [DbColumn (DataField.LotExpirationDate)]
        public DateTime? ExpirationDate { get; set; }

        [DbColumn (DataField.LotProductionDate)]
        public DateTime? ProductionDate { get; set; }

        [DbColumn (DataField.LotLocation, 255)]
        public string Location { get; set; }

        public const int DefaultLotId = 1;

        public const string DefaultLotName = "NA";

        public bool Equals (Lot other)
        {
            if (ReferenceEquals (null, other))
                return false;

            if (ReferenceEquals (this, other))
                return true;

            return other.ID == ID &&
                other.PriceIn == PriceIn &&
                CompareLots (other.Name, Name) &&
                other.SerialNumber == SerialNumber &&
                other.ExpirationDate == ExpirationDate &&
                other.ProductionDate == ProductionDate &&
                other.Location == Location;
        }

        public override bool Equals (object obj)
        {
            if (ReferenceEquals (null, obj))
                return false;

            if (ReferenceEquals (this, obj))
                return true;

            return obj.GetType () == typeof (Lot) && Equals ((Lot) obj);
        }

        public static bool operator == (Lot l1, Lot l2)
        {
            bool l1IsNull = ReferenceEquals (l1, null);
            bool l2IsNull = ReferenceEquals (l2, null);

            if (l1IsNull && l2IsNull)
                return true;

            if (l1IsNull || l2IsNull)
                return false;

            return l1.Equals (l2);
        }

        public static bool operator != (Lot l1, Lot l2)
        {
            return !(l1 == l2);
        }

        public override int GetHashCode ()
        {
            unchecked {
                int result = (int) ID;
                result = (result * 397) ^ PriceIn.GetHashCode ();
                result = (result * 397) ^ (Name != null ? Name.GetHashCode () : 0);
                result = (result * 397) ^ (SerialNumber != null ? SerialNumber.GetHashCode () : 0);
                result = (result * 397) ^ (ExpirationDate.HasValue ? ExpirationDate.Value.GetHashCode () : 0);
                result = (result * 397) ^ (ProductionDate.HasValue ? ProductionDate.Value.GetHashCode () : 0);
                result = (result * 397) ^ (Location != null ? Location.GetHashCode () : 0);
                return result;
            }
        }

        public static LazyListModel<Lot> GetAvailable (long itemId, long? locationId)
        {
            return BusinessDomain.DataAccessProvider.GetLots<Lot> (itemId, locationId, BusinessDomain.AppConfiguration.ItemsManagementType);
        }

        public static Lot GetByStoreId (long storeId)
        {
            return BusinessDomain.DataAccessProvider.GetLotByStoreId<Lot> (storeId);
        }

        public static void EnableLots ()
        {
            BusinessDomain.DataAccessProvider.EnableLots ();
        }

        public static void DisableLots ()
        {
            BusinessDomain.DataAccessProvider.DisableLots ();
        }

        public static bool CompareLots (string left, string right)
        {
            return ((left == "NA" || string.IsNullOrWhiteSpace (left)) && (right == "NA" || string.IsNullOrWhiteSpace (right))) ||
                left == right;
        }
    }
}
