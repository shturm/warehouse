//
// SaleReceipt.cs
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
    public class SaleReceipt : Receipt
    {
        public override string Name
        {
            get { return Translator.GetString ("Stock Receipt for Items Sale"); }
        }

        public override string FormName
        {
            get { return "SaleReceipt"; }
        }

        public SaleReceipt ()
        {
            ReceiptSubTitle = Translator.GetString ("for items sale");
        }

        public SaleReceipt (Sale sale)
            : this ()
        {
            ReceiptDate = BusinessDomain.GetFormattedDate (sale.Date);
            ReceiptNumber = sale.FormattedOperationNumber;
            Note = sale.Note;

            FillRecipient (sale.PartnerId);
            FillSupplier ();

            Location = sale.Location2;

            double vat = sale.VAT;
            if (BusinessDomain.AppConfiguration.VATIncluded) {
                Total = sale.Total - vat;
                Vat = Currency.ToString (vat);
                TotalPlusVat = sale.Total;
            } else {
                Total = sale.Total;
                Vat = Currency.ToString (vat);
                TotalPlusVat = sale.Total + vat;
            }

            int i = 1;
            foreach (SaleDetail detail in sale.Details)
                ReceiptDetails.Add (new ReceiptDetail (i++, detail, false));

            TotalQuantity = Quantity.ToString (sale.Details.Sum (d => d.Quantity));
        }
    }
}
