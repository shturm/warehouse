//
// Purchase.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/18/2006
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
using Warehouse.Business.Entities;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Operations
{
    public class Purchase : Operation<PurchaseDetail>
    {
        #region Public properties

        /// <summary>
        /// Gets a value indicating whether it makes sense for this <see cref="Operation"/> to use money change.
        /// </summary>
        /// <value>
        ///   <c>true</c> if change; otherwise, <c>false</c>.
        /// </value>
        public override bool UseChange
        {
            get { return false; }
        }

        public override PriceType TotalsPriceType
        {
            get { return PriceType.PurchaseTotal; }
        }

        #endregion

        public Purchase ()
        {
            operationType = OperationType.Purchase;
        }

        public override void Commit ()
        {
            Commit (GetPriceGroup ());
        }

        public void Commit (PriceGroup priceGroup)
        {
            Payment [] paidPayments;
            Payment [] duePayments;
            CreatePayments (out duePayments, out paidPayments);

            Commit (duePayments, paidPayments, priceGroup);
        }

        private void Commit (Payment [] duePayments, Payment [] paidPayments, PriceGroup priceGroup)
        {
            bool editMode = true;

            using (DbTransaction transaction = new DbTransaction (BusinessDomain.DataAccessProvider)) {
                transaction.SnapshotObject (this);
                // Create a new operation Id if needed
                OperationState operationState = State;
                if (operationState == OperationState.New || operationState == OperationState.NewDraft || operationState == OperationState.NewPending) {
                    id = CreateNewId ();

                    // Set the new operation id
                    foreach (Payment payment in duePayments) {
                        transaction.SnapshotObject (payment);
                        payment.OperationId = id;
                    }
                    foreach (Payment payment in paidPayments) {
                        transaction.SnapshotObject (payment);
                        payment.OperationId = id;
                    }
                    editMode = false;
                }

                // Save the purchase in the database
                BusinessDomain.DataAccessProvider.AddUpdatePurchase (this, details.ToArray (), BusinessDomain.AppConfiguration.AllowNegativeAvailability, Item.GetPriceGroupField (priceGroup));
                InvalidateItemsCache ();

                CommitPayments (duePayments, paidPayments, editMode, true);

                if (editMode)
                    RemoveAllEmptyDetails ();
                
                transaction.Complete ();
            }
        }

        protected override string LogVoidOperation ()
        {
            return Translator.GetString ("Void purchase No.{0} from {1}");
        }

        protected override string LogEditOperation ()
        {
            return Translator.GetString ("Edit purchase No.{0} from {1}");
        }

        public static LazyListModel<Purchase> GetAll ()
        {
            return GetAll (new DataQuery ());
        }

        public static LazyListModel<Purchase> GetAll (DataQuery dataQuery, bool onlyUninvoiced = false)
        {
            AddPartnerLocationFilters (ref dataQuery);
            return BusinessDomain.DataAccessProvider.GetAllPurchases<Purchase> (dataQuery, onlyUninvoiced);
        }

        public static Purchase GetById (long purchaseId)
        {
            Purchase purchase = BusinessDomain.DataAccessProvider.GetOperationWithPartnerById<Purchase> (OperationType.Purchase, purchaseId);
            if (purchase != null) {
                purchase.LoadDetails ();
                purchase.IsDirty = false;
            }

            return purchase;
        }

        public static Purchase GetPending (long partnerId, long locationId)
        {
            Purchase purchase = BusinessDomain.DataAccessProvider.GetPendingOperationWithPartner<Purchase> (OperationType.Purchase, partnerId, locationId);
            if (purchase != null) {
                purchase.LoadDetails ();
                purchase.IsDirty = false;
            }

            return purchase;
        }

        public static Dictionary<long, long> GetAllIdsWithInvoices ()
        {
            return BusinessDomain.DataAccessProvider.GetAllPurchaseIdsWithInvoices ();
        }
    }
}