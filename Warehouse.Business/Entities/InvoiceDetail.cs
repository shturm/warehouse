//
// InvoiceDetail.cs
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

using Warehouse.Business.Documenting;
using Warehouse.Business.Operations;

namespace Warehouse.Business.Entities
{
    public class InvoiceDetail : PriceOutOperationDetail
    {
        #region Private members

        private string number = string.Empty;

        #endregion

        #region Public properties

        public string Number
        {
            get { return number; }
            set { number = value; }
        }

        public string ExpirationDateString
        {
            get
            {
                return expirationDate.HasValue ?
                    BusinessDomain.GetFormattedDate (expirationDate.Value) : string.Empty;
            }
        }

        public string ProductionDateString
        {
            get
            {
                return productionDate.HasValue ?
                    BusinessDomain.GetFormattedDate (productionDate.Value) : string.Empty;
            }
        }

        public string QuantityString
        {
            get { return Entities.Quantity.ToString (Quantity); }
        }

        public string PriceString
        {
            get { return Currency.ToString (PriceOut); }
        }

        [FormMemberMapping ("priceBankNotes")]
        public string PriceBankNotes
        {
            get { return Currency.BankNotesString (Price); }
        }

        [FormMemberMapping ("priceCoins")]
        public string PriceCoins
        {
            get { return Currency.CoinsString (Price); }
        }

        public string PriceInString
        {
            get { return Currency.ToString (PriceIn, PriceType.Purchase); }
        }

        public double PriceWithoutVAT
        {
            get { return GetWithoutVAT (PriceOut); }
        }

        public string PriceWithoutVATString
        {
            get { return Currency.ToString (PriceWithoutVAT); }
        }

        [FormMemberMapping ("priceNoVATBankNotes")]
        public string PriceNoTaxBankNotes
        {
            get { return Currency.BankNotesString (PriceWithoutVAT); }
        }

        [FormMemberMapping ("priceNoVATCoins")]
        public string PriceNoTaxCoins
        {
            get { return Currency.CoinsString (PriceWithoutVAT); }
        }

        [FormMemberMapping ("vatBankNotes")]
        public string TaxBankNotes
        {
            get { return Currency.BankNotesString (VATOut); }
        }

        [FormMemberMapping ("vatCoins")]
        public string TaxCoins
        {
            get { return Currency.CoinsString (VATOut); }
        }

        public string TotalString
        {
            get { return Currency.ToString (Total, PriceType.SaleTotal); }
        }

        public override double TotalWithoutVAT
        {
            get { return Quantity * PriceWithoutVAT; }
        }

        public string TotalWithoutVATString
        {
            get { return Currency.ToString (TotalWithoutVAT, TotalsPriceType); }
        }

        [FormMemberMapping ("totalNoVATBankNotes")]
        public string TotalNoTaxBankNotes
        {
            get { return Currency.BankNotesString (TotalWithoutVAT); }
        }

        [FormMemberMapping ("totalNoVATCoins")]
        public string TotalNoTaxCoins
        {
            get { return Currency.CoinsString (TotalWithoutVAT); }
        }

        public string TotalVATString
        {
            get { return Currency.ToString (TotalVAT, PriceType.SaleTotal); }
        }

        [FormMemberMapping ("totalVATBankNotes")]
        public string TotalBankNotes
        {
            get { return Currency.BankNotesString (TotalVAT); }
        }

        [FormMemberMapping ("totalVATCoins")]
        public string TotalCoins
        {
            get { return Currency.CoinsString (TotalVAT); }
        }

        public string TotalVATInString
        {
            get { return Currency.ToString (Currency.Round (VATIn * quantity, PriceType.Purchase), PriceType.Purchase); }
        }

        #endregion

        public InvoiceDetail ()
        {
            itemId = 1;
        }

        public override object Clone ()
        {
            return MemberwiseClone ();
        }
    }
}