//
// RestaurantOrder.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/11/2006
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
using System.Linq;
using Warehouse.Business.Entities;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Data.Model;

namespace Warehouse.Business.Operations
{
    public class RestaurantOrder : Operation<RestaurantOrderDetail>
    {
        public override double TotalPlusVAT
        {
            get
            {
                double totalPlusVAT = details.Sum (detail => detail.TotalPlusVAT);

                return Currency.Round (totalPlusVAT, TotalsPriceType);
            }
        }

        public RestaurantOrder ()
        {
            operationType = OperationType.RestaurantOrder;
            Details = new BindList<RestaurantOrderDetail> ();
            BasePricesOnPricePlusVAT = true;
        }

        public override void Commit ()
        {
            CommitChanges ();
        }

        public virtual void CommitChanges (bool logChanges = false, bool logNewDetailChanges = false, long? mainAvailabilityLocation = null, bool checkAvailability = false)
        {
            using (DbTransaction transaction = new DbTransaction (BusinessDomain.DataAccessProvider)) {
                transaction.SnapshotObject (this);
                IEnumerable<string> changes = logChanges ? GetChanges (logNewDetailChanges, false) : new List<string> ();

                Dictionary<long, double> orderedQuantities = new Dictionary<long, double> (details.Count);
                if (checkAvailability && mainAvailabilityLocation != null)
                    if (BusinessDomain.AppConfiguration.AutoProduction)
                        AutomaticProduction (mainAvailabilityLocation.Value, locationId);
                    else if (!BusinessDomain.AppConfiguration.AllowNegativeAvailability) {
                        foreach (RestaurantOrderDetail orderDetail in details.Where (d => d.DetailId >= 0 || d.Quantity > 0)) {
                            if (orderedQuantities.ContainsKey (orderDetail.ItemId))
                                orderedQuantities [orderDetail.ItemId] += orderDetail.Quantity;
                            else
                                orderedQuantities.Add (orderDetail.ItemId, orderDetail.Quantity);
                        }

                        foreach (KeyValuePair<long, double> orderedQuantity in orderedQuantities)
                            if (Item.GetAvailability (orderedQuantity.Key, mainAvailabilityLocation.Value, locationId) < orderedQuantity.Value)
                                throw new InsufficientItemAvailabilityException (details.Find (d => d.ItemId == orderedQuantity.Key).ItemName);
                    }

                if (State == OperationState.New) {
                    if (BusinessDomain.AppConfiguration.PrintOrderCodeOnReceipts)
                        id = -10 - BusinessDomain.DataAccessProvider.CreateNewOperationId (operationType, mainAvailabilityLocation ?? Entities.Location.DefaultId, currentState: State);
                    else
                        id = 0;
                }

                foreach (RestaurantOrderDetail detail in details)
                    detail.ReferenceDocumentId = referenceDocumentId;

                // Save the order in the database
                BusinessDomain.DataAccessProvider.AddUpdateRestaurantOrder (this, isDirty ?
                    details.ToArray () :
                    details.Where (detail => detail.IsDirty).ToArray ());

                for (int i = details.Count - 1; i >= 0; i--)
                    if (details [i].Quantity.IsZero ())
                        details.RemoveAt (i);

                IsDirty = false;

                foreach (string change in changes)
                    ApplicationLogEntry.AddNew (change);

                transaction.Complete ();
            }
        }

        public Sale CreateSale (bool skipOldDetails)
        {
            Sale ret = CloneOperationBody<Sale> ();
            if (id < 0)
                ret.Id = id;
            ret.OperationType = OperationType.Sale;
            ret.BasePricesOnPricePlusVAT = true;

            foreach (RestaurantOrderDetail detail in details) {
                if (detail.DetailId >= 0 && skipOldDetails)
                    continue;

                if (detail.DetailId < 0 && detail.Quantity.IsZero ())
                    continue;

                SaleDetail saleDetail = new SaleDetail (detail);
                if (operationType != OperationType.Sale)
                    saleDetail.DetailId = -1;
                ret.Details.Add (saleDetail);
            }

            return ret;
        }

        public Sale CreateDeltaSale (bool skipNotes)
        {
            Sale ret = CloneOperationBody<Sale> ();
            ret.OperationType = OperationType.Sale;
            ret.BasePricesOnPricePlusVAT = true;

            foreach (RestaurantOrderDetail detail in details) {
                if (detail.DetailId < 0)
                    continue;

                if (detail.OriginalQuantity.IsEqualTo (detail.Quantity) &&
                    (skipNotes || detail.OriginalNote.IsEqualTo (detail.Note)))
                    continue;

                ret.Details.Add (new SaleDetail (detail));
            }

            return ret;
        }

        public virtual RestaurantOrder GetRestaurantOrder ()
        {
            RestaurantOrder ret = Clone<RestaurantOrder, RestaurantOrderDetail> ();
            if (id < 0)
                ret.id = id;
            return ret;
        }

        public static LazyListModel<RestaurantOrder> Get (long? locationId, long? customerId, long? userId, DateTime? from = null, DateTime? to = null)
        {
            LazyListModel<RestaurantOrder> orders = BusinessDomain.DataAccessProvider.GetRestaurantOrders<RestaurantOrder> (locationId, customerId, userId, @from, to);
            foreach (RestaurantOrder order in orders)
                order.LoadDetails ();

            return orders;
        }

        protected override void LoadDetails (bool usePriceIn)
        {
            details.Clear ();
            details.AddRange (BusinessDomain.DataAccessProvider.GetRestaurantOrderDetails<RestaurantOrderDetail> (id, locationId, partnerId, userId));
            // this may be a reservation with no order
            if (details.Count > 0)
                referenceDocumentId = details [0].ReferenceDocumentId;

            foreach (RestaurantOrderDetail restaurantOrderDetail in details)
                restaurantOrderDetail.CheckForInsufficiency (this, referenceDocumentId);

            IsDirty = false;
        }
    }
}
