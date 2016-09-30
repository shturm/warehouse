//
// PurchaseReceipt.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   03.21.2011
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

using System.Linq;
using Warehouse.Business.Operations;

namespace Warehouse.Business.Entities
{
    public class PurchaseReceipt : Receipt
    {
        #region Overrides of Document

        public override string Name
        {
            get { return Translator.GetString ("Stock Receipt for Items Purchase"); }
        }

        public override string FormName
        {
            get { return "PurchaseReceipt"; }
        }

        public override PriceType TotalsPriceType
        {
            get { return PriceType.PurchaseTotal; }
        }

        #endregion

        public PurchaseReceipt ()
        {
            ReceiptSubTitle = Translator.GetString ("for items purchase");
        }

        public PurchaseReceipt (Purchase purchase)
            : this ()
        {
            ReceiptDate = BusinessDomain.GetFormattedDate (purchase.Date);
            ReceiptNumber = purchase.FormattedOperationNumber;
            Note = purchase.Note;

            Invoice invoice = Invoice.GetReceivedForOperation (purchase.Id);
            if (invoice != null) {
                InvoiceDate = invoice.DateString;
                InvoiceNumber = invoice.NumberString;
            }

            FillRecipient ();
            FillSupplier (purchase.PartnerId);

            Location = purchase.Location2;

            double vat = purchase.VAT;
            if (BusinessDomain.AppConfiguration.VATIncluded) {
                Total = purchase.Total - vat;
                Vat = Currency.ToString (vat, PriceType.Purchase);
                TotalPlusVat = purchase.Total;
            } else {
                Total = purchase.Total;
                Vat = Currency.ToString (vat, PriceType.Purchase);
                TotalPlusVat = purchase.Total + vat;
            }

            int i = 1;
            foreach (PurchaseDetail detail in purchase.Details)
                ReceiptDetails.Add (new ReceiptDetail (i++, detail));

            TotalQuantity = Quantity.ToString (purchase.Details.Sum (d => d.Quantity));
        }
    }
}
