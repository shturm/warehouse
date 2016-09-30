//
// Invoice.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   07/27/2006
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
using Warehouse.Business.Documenting;
using Warehouse.Business.Operations;
using Warehouse.Data;
using Warehouse.Data.Model;
using System.Linq;

namespace Warehouse.Business.Entities
{
    public class Invoice : DocumentBase<InvoiceDetail>
    {
        private Operation operation;
        private readonly List<Operation> allOperations = new List<Operation> ();

        public override string Name
        {
            get
            {
                if (string.IsNullOrEmpty (NumberString))
                    return Translator.GetString ("Invoice");
                return string.Format ("{0} {1}", Translator.GetString ("Invoice No."), NumberString);
            }
        }

        public override string FormName
        {
            get { return "Invoice"; }
        }

        [FormMemberMapping ("title")]
        public static string Title { get { return Translator.GetString ("Invoice"); } }

        [DbColumn (DataField.DocumentType)]
        public static long DocumentType { get; set; }

        static Invoice ()
        {
            DocumentType = (long) Data.DocumentType.Invoice;
        }

        public Invoice ()
        {

        }

        public Invoice (IEnumerable<Sale> sales)
        {
            LoadOperations (sales);
        }

        public Invoice (IEnumerable<Purchase> purchases)
        {
            LoadOperations (purchases);
        }

        public override void CommitChanges ()
        {
            List<Invoice> invoices = new List<Invoice> ();
            for (int i = 0; i < allOperations.Count - 1; i++) {
                Invoice invoice = new Invoice ();
                CopyTo (invoice);
                invoice.OperationNumber = allOperations [i].Id;
                invoice.OperationType = (long) allOperations [i].OperationType;
                invoices.Add (invoice);
            }
            invoices.Add (this);
            BusinessDomain.DataAccessProvider.AddInvoice (invoices, BusinessDomain.AppConfiguration.DocumentNumberLength, operationType == (long) Data.OperationType.Sale);
        }

        public override DocumentBase GetInternational ()
        {
            InternationalInvoice internationalInvoice = new InternationalInvoice ();
            CopyTo (internationalInvoice);
            internationalInvoice.operation = operation;
            internationalInvoice.LoadOperationDetailInfo (operation.DetailsBase);
            Transliterator.TransliterateProperties (internationalInvoice);
            return internationalInvoice;
        }

        public override void LoadOperationInfo<TOperDetail> (Operation<TOperDetail> newOperation)
        {
            operation = newOperation;

            base.LoadOperationInfo (newOperation);
        }

        private void LoadOperations<TDetail> (IEnumerable<Operation<TDetail>> operations) where TDetail : OperationDetail
        {
            allOperations.Clear ();
            allOperations.AddRange (operations);
            Operation<TDetail> lastOperation = (Operation<TDetail>) allOperations.Last ();
            OperationNumber = lastOperation.Id;
            OperationType = (long) lastOperation.OperationType;

            if (typeof (TDetail) == typeof (SaleDetail) && Id < 0) {
                Number = BusinessDomain.AppConfiguration.DocumentNumbersPerLocation
                    ? BusinessDomain.DataAccessProvider.GetNextDocumentId (lastOperation.LocationId)
                    : BusinessDomain.DataAccessProvider.GetNextDocumentId (0);
            }

            List<TDetail> details = new List<TDetail> ();
            foreach (Operation<TDetail> currentOperation in allOperations) {
                if (currentOperation.Details.Count == 0)
                    currentOperation.LoadDetails ();
                details.AddRange (currentOperation.Details);
            }
            lastOperation.Details.Clear ();
            lastOperation.Details.AddRange (details);

            LoadOperationInfo (lastOperation);
        }

        public Sale GetSale ()
        {
            if (allOperations.Count == 0) {
                KeyValuePair<int, OperationType> [] operations;
                if (operationType == (int) Data.OperationType.Sale)
                    operations = GetOperationsByIssuedNumber (Number);
                else if (operationType == (int) Data.OperationType.Purchase)
                    operations = GetOperationsByReceivedNumber (Number);
                else
                    return null;

                allOperations.AddRange (operations.Select (o => Operation.GetById (o.Value, o.Key)));
            }

            Operation last = allOperations.LastOrDefault ();
            Sale lastSale = last as Sale;
            Purchase lastPurchase = last as Purchase;

            Sale ret;
            if (lastSale != null)
                ret = lastSale.CloneOperationBody<Sale> ();
            else if (lastPurchase != null)
                ret = lastPurchase.CloneOperationBody<Sale> ();
            else
                return null;

            List<SaleDetail> details = new List<SaleDetail> ();
            foreach (var oper in allOperations) {
                Sale s = oper as Sale;
                if (s != null) {
                    if (s.Details.Count == 0)
                        s.LoadDetails ();

                    details.AddRange (s.Details);
                    continue;
                }

                Purchase p = oper as Purchase;
                if (p != null) {
                    if (p.Details.Count == 0)
                        p.LoadDetails ();

                    details.AddRange (p.Details.Select (pd => pd.Clone<SaleDetail> ()));
                }
            }
            ret.Details.Clear ();
            ret.Details.AddRange (details);

            return ret;
        }

        public void Annul ()
        {
            if (Number >= 0)
                Delete (Number);
        }

        public static void Delete (long invoiceNumber)
        {
            BusinessDomain.DataAccessProvider.DeleteInvoice (invoiceNumber);

            ApplicationLogEntry.AddNew (string.Format (Translator.GetString ("Void invoice No.{0}"), Operation.GetFormattedOperationNumber (invoiceNumber)));
        }

        public static LazyListModel<Invoice> GetAllIssued (DataQuery dataQuery, bool getPurchaseTotal = false)
        {
            Operation.AddPartnerLocationFilters (ref dataQuery);
            return BusinessDomain.DataAccessProvider.GetAllDocuments<Invoice> (dataQuery,
                Data.DocumentType.Invoice, getPurchaseTotal, Data.OperationType.Sale, Data.OperationType.ConsignmentSale);
        }

        public static LazyListModel<Invoice> GetAllReceived (DataQuery dataQuery, bool getPurchaseTotal = false)
        {
            Operation.AddPartnerLocationFilters (ref dataQuery);
            return BusinessDomain.DataAccessProvider.GetAllDocuments<Invoice> (dataQuery,
                Data.DocumentType.Invoice, getPurchaseTotal, Data.OperationType.Purchase);
        }

        public static Invoice GetIssuedByNumber (long id)
        {
            Invoice [] invoices = BusinessDomain.DataAccessProvider.GetIssuedInvoicesByNumber<Invoice> (id);
            if (invoices.Length == 0)
                return null;

            Invoice result = invoices.Last ();
            result.FillDetails (invoices);
            return result;
        }

        public static KeyValuePair<int, OperationType> [] GetOperationsByIssuedNumber (long id)
        {
            return BusinessDomain.DataAccessProvider.GetIssuedInvoicesByNumber<Invoice> (id)
                .Select (inv => new KeyValuePair<int, OperationType> ((int) inv.OperationNumber, (OperationType) inv.OperationType))
                .ToArray ();
        }

        public static KeyValuePair<int, OperationType> [] GetOperationsByReceivedNumber (long id)
        {
            return BusinessDomain.DataAccessProvider.GetReceivedInvoicesByNumber<Invoice> (id)
                .Select (inv => new KeyValuePair<int, OperationType> ((int) inv.OperationNumber, (OperationType) inv.OperationType))
                .ToArray ();
        }

        private void FillDetails (IEnumerable<Invoice> invoices)
        {
            switch ((OperationType) OperationType) {
                case Data.OperationType.Purchase:
                    LoadOperations (invoices.Select (invoice => Purchase.GetById ((int) invoice.OperationNumber)).Where (p => p != null));
                    break;
                case Data.OperationType.Sale:
                case Data.OperationType.ConsignmentSale:
                    LoadOperations (invoices.Select (invoice => Sale.GetById ((int) invoice.OperationNumber)).Where (s => s != null));
                    break;
            }
        }

        public static Invoice GetReceivedForOperation (long operId)
        {
            return BusinessDomain.DataAccessProvider.GetReceivedInvoiceForOperation<Invoice> (operId);
        }
    }
}