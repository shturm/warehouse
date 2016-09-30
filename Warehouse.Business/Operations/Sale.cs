//
// Sale.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
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

using System.Collections.Generic;
using System.Linq;
using Warehouse.Business.Entities;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Operations
{
    public class Sale : Sale<SaleDetail>
    {
        #region Overrides of Sale<SaleDetail>

        public override Sale GetSale ()
        {
            return this;
        }

        #endregion
    }

    public abstract class Sale<T> : Operation<T> where T : SaleDetail
    {
        #region Public properties

        public override bool AllowFiscal
        {
            get { return true; }
        }

        public override bool UseAdvancePayments
        {
            get { return true; }
        }

        public override double TotalPlusVAT
        {
            get
            {
                if (!baseTotalOnPricePlusVAT)
                    return base.TotalPlusVAT;

                double totalPlusVAT = details.Sum (detail => detail.TotalPlusVAT);

                return Currency.Round (totalPlusVAT, TotalsPriceType);
            }
        }

        [DbColumn (DataField.PurchaseTotal)]
        public double PurchaseTotal { get; set; }

        [DbColumn (DataField.PurchaseVATSum)]
        public double PurchaseVATSum { get; set; }

        public SaleSignature Signature { get; set; }

        #endregion

        protected Sale ()
        {
            operationType = OperationType.Sale;
        }

        public override void Commit ()
        {
            Commit (-1);
        }

        public void Commit (long childLocationId)
        {
            Payment [] duePayments;
            Payment [] paidPayments;
            CreatePayments (out duePayments, out paidPayments);

            Commit (duePayments, paidPayments, childLocationId);
        }

        private void Commit (IList<Payment> duePayments, Payment [] paidPayments, long childLocationId)
        {
            bool editMode = true;

            using (DbTransaction transaction = new DbTransaction (BusinessDomain.DataAccessProvider)) {
                transaction.SnapshotObject (this);
                if (BusinessDomain.AppConfiguration.AutoProduction)
                    AutomaticProduction (locationId, childLocationId);

                // Create a new operation Id if needed);
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

                // Save the sale in the database
                BusinessDomain.DataAccessProvider.AddUpdateSale (this, details.ToArray (), 
                    BusinessDomain.AppConfiguration.AllowNegativeAvailability, childLocationId);
                LogChanges (false);

                CommitPayments (duePayments, paidPayments, editMode, true);

                if (editMode)
                    RemoveAllEmptyDetails ();

                transaction.Complete ();
            }
        }

        protected override string LogVoidOperation ()
        {
            return Translator.GetString ("Void sale No.{0} from {1}");
        }

        protected override string LogEditOperation ()
        {
            return Translator.GetString ("Edit sale No.{0} from {1}");
        }

        public override void LoadDetails ()
        {
            LoadDetails (false);
        }

        public abstract Sale GetSale ();

        public static LazyListModel<Sale> GetAll (DataQuery dataQuery = null, OperationType saleType = OperationType.Any)
        {
            if (dataQuery == null)
                dataQuery = new DataQuery ();

            AddPartnerLocationFilters (ref dataQuery);
            return BusinessDomain.DataAccessProvider.GetAllSales<Sale> (dataQuery, saleType);
        }

        public static LazyListModel<Sale> GetAllUninvoiced (DataQuery dataQuery, bool fullyPaid = false)
        {
            AddPartnerLocationFilters (ref dataQuery);
            return BusinessDomain.DataAccessProvider.GetAllUninvoicedSales<Sale> (dataQuery, fullyPaid);
        }

        public static Sale GetCashReport (DataQuery dataQuery)
        {
            AddPartnerLocationFilters (ref dataQuery);
            return BusinessDomain.DataAccessProvider.GetCashReport<Sale> (dataQuery);
        }

        public static Sale GetById (long saleId)
        {
            return GetById<Sale> (saleId);
        }

        protected static TOper GetById<TOper> (long saleId) where TOper : Operation
        {
            TOper sale = BusinessDomain.DataAccessProvider.GetSaleById<TOper> (saleId);
            if (sale != null) {
                sale.LoadDetails ();
                sale.IsDirty = false;
            }

            return sale;
        }

        public static Sale GetPending (long partnerId, long locationId)
        {
            return GetPending<Sale> (partnerId, locationId);
        }

        protected static TOper GetPending<TOper> (long partnerId, long locationId) where TOper : Operation
        {
            TOper sale = BusinessDomain.DataAccessProvider.GetPendingSale<TOper> (partnerId, locationId);
            if (sale != null) {
                sale.LoadDetails ();
                sale.IsDirty = false;
            }

            return sale;
        }

        public static Sale GetByNote (string note)
        {
            Sale sale = BusinessDomain.DataAccessProvider.GetSaleByNote<Sale> (note);
            if (sale != null) {
                sale.LoadDetails ();
                sale.IsDirty = false;
            }

            return sale;
        }

        public static Dictionary<long, long> GetAllIdsWithInvoices ()
        {
            return BusinessDomain.DataAccessProvider.GetAllSaleIdsWithInvoices ();
        }
    }
}