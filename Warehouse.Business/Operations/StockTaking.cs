//
// StockTaking.cs
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
using System.Linq;
using Warehouse.Business.Entities;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Operations
{
    public class StockTaking : Operation<StockTakingDetail>
    {
        private bool isNewFormat = true;

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

        #endregion

        public StockTaking ()
        {
            partnerId = 1;
            operationType = OperationType.StockTaking;
        }

        public override void CommitPayments ()
        {
            CommitPayments (false);
        }

        public override void Commit ()
        {
            Commit (GetPriceGroup (-1, locationId));
        }

        public void Commit (PriceGroup priceGroup, bool annul = false)
        {
            Payment [] duePayments;
            Payment [] paidPayments;
            CreatePayments (out duePayments, out paidPayments);

            Commit (duePayments, paidPayments, priceGroup, annul);
        }

        public override void Annul ()
        {
            if (!CheckIsNewFormat ())
                return;

            string error;
            string warning;
            if (BusinessDomain.CanAnnulOperation (this, out error, out warning)) {
                LoadDetails ();
                ClearDetails ();
                Commit (GetPriceGroup (-1, locationId), true);
            } else if (warning != null)
                BusinessDomain.RequestOperationAnnul (this);
        }

        public bool CheckIsNewFormat ()
        {
            if (details.Count == 0)
                LoadDetails ();

            return isNewFormat;
        }

        private void Commit (Payment [] duePayments, Payment [] paidPayments, PriceGroup priceGroup, bool annul)
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

                LotsEvaluate (Details);

                // Save the stock-taking in the database
                IEnumerable<StockTakingDetail> detailsToSave = GetDetailsToSave (annul);
                BusinessDomain.DataAccessProvider.AddUpdateStockTaking (this, detailsToSave.ToArray (),
                    BusinessDomain.AppConfiguration.AllowNegativeAvailability, Item.GetPriceGroupField (priceGroup), annul);
                InvalidateItemsCache ();

                CommitPayments (duePayments, paidPayments, editMode, false);

                transaction.Complete ();
            }
        }

        public IEnumerable<StockTakingDetail> GetDetailsToSave (bool annul)
        {
            List<StockTakingDetail> detailsToSave = new List<StockTakingDetail> ();
            foreach (StockTakingDetail detail in details) {
                StockTakingDetail firstDetail = (StockTakingDetail) detail.Clone ();
                firstDetail.Quantity = annul ? 0 : -firstDetail.ExpectedQuantity;
                firstDetail.PriceIn = firstDetail.OldPriceIn;
                firstDetail.PriceOut = firstDetail.OldPriceOut;
                // compatibility
                firstDetail.ReferenceDocumentId = 1;

                detailsToSave.Add (firstDetail);
                StockTakingDetail secondDetail = (StockTakingDetail) detail.Clone ();
                secondDetail.Quantity = annul ? 0 : detail.EnteredQuantity;
                // compatibility
                secondDetail.ReferenceDocumentId = 2;
                secondDetail.DetailId = firstDetail.EnteredDetailId;
                detailsToSave.Add (secondDetail);
            }
            return detailsToSave;
        }

        protected override string LogVoidOperation ()
        {
            return Translator.GetString ("Void stock-taking No.{0} from {1}");
        }

        protected override string LogEditOperation ()
        {
            return Translator.GetString ("Edit stock-taking No.{0} from {1}");
        }

        public override double GetDetailQuantityForLot (StockTakingDetail detail)
        {
            return detail.EnteredQuantity;
        }

        public override void EvaluateQuantityForLot (StockTakingDetail detail, double availableQuantity, double qttyToUse)
        {
            detail.ExpectedQuantity = availableQuantity;
            detail.EnteredQuantity = qttyToUse;
        }

        protected override void EvaluateQuantityForEmptyLot (StockTakingDetail detail, double qttyToUse)
        {
            detail.EnteredQuantity = 0;
            detail.ExpectedQuantity = 0;
        }

        public override void LoadDetails ()
        {
            LoadDetails (false);
        }

        protected override void LoadDetails (bool usePriceIn)
        {
            isNewFormat = false;
            base.LoadDetails (usePriceIn);

            for (int i = details.Count - 1; i >= 1; i--) {
                referenceDocumentId = details [i].ReferenceDocumentId;
                StockTakingDetail newDetail = details [i];
                if (newDetail.EnteredDetailId > 0)
                    continue;

                StockTakingDetail oldDetail = null;
                int removeIndex = i;
                int secondDetailIndex = -1;
                for (int j = i - 1; j >= 0; j--) {
                    StockTakingDetail d = details [j];
                    if (d.DetailId == newDetail.DetailId ||
                        d.ReferenceDocumentId == newDetail.ReferenceDocumentId ||
                        d.ItemId != newDetail.ItemId ||
                        d.LotId != newDetail.LotId)
                        continue;

                    oldDetail = d;
                    secondDetailIndex = j;
                    break;
                }

                if (oldDetail == null)
                    continue;

                if (oldDetail.ReferenceDocumentId == 2 && newDetail.ReferenceDocumentId == 1) {
                    StockTakingDetail temp = oldDetail;
                    oldDetail = newDetail;
                    newDetail = temp;
                    removeIndex = secondDetailIndex;
                }

                if (oldDetail.ReferenceDocumentId != 1 || newDetail.ReferenceDocumentId != 2)
                    continue;

                isNewFormat = true;

                oldDetail.OldPriceIn = oldDetail.PriceIn;
                oldDetail.OldPriceOut = oldDetail.PriceOut;
                oldDetail.ExpectedQuantity = -oldDetail.Quantity;

                oldDetail.EnteredDetailId = newDetail.DetailId;
                oldDetail.PriceIn = newDetail.PriceIn;
                oldDetail.PriceOut = newDetail.PriceOut;
                oldDetail.EnteredQuantity = newDetail.Quantity;

                details.RemoveAt (removeIndex);
            }
        }

        public static LazyListModel<StockTaking> GetAll (DataQuery dataQuery)
        {
            AddPartnerLocationFilters (ref dataQuery);
            return BusinessDomain.DataAccessProvider.GetAllStockTakings<StockTaking> (dataQuery);
        }

        public static StockTaking GetById (long stockTakingId)
        {
            StockTaking stockTaking = BusinessDomain.DataAccessProvider.GetOperationWithPartnerById<StockTaking> (OperationType.StockTaking, stockTakingId);
            if (stockTaking != null) {
                stockTaking.LoadDetails ();
                stockTaking.IsDirty = false;
            }

            return stockTaking;
        }

        public static StockTaking GetPending (long pId, long lId)
        {
            StockTaking stockTaking = BusinessDomain.DataAccessProvider.GetPendingOperationWithPartner<StockTaking> (OperationType.StockTaking, pId, lId);
            if (stockTaking != null) {
                stockTaking.LoadDetails ();
                stockTaking.IsDirty = false;
            }

            return stockTaking;
        }
    }
}