//
// PaymentReceipt.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   26.04.2010
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

using System.Globalization;
using Warehouse.Business.CurrencyTranslation;
using Warehouse.Business.Documenting;
using Warehouse.Business.Operations;
using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public class PaymentReceipt : Receipt
    {
        #region Overrides of Document

        public override string Name
        {
            get { return Translator.GetString ("Payment Receipt"); }
        }

        public override string FormName
        {
            get { return "PaymentReceipt"; }
        }

        [FormMemberMapping ("totalInWords")]
        public override string TotalInWords
        {
            get { return NumberToWords.Translate (Total); }
        }

        [FormMemberMapping ("receiptTitle")]
        public override string ReceiptTitle { get { return Translator.GetString ("Payment Receipt"); } }

        #endregion

        [FormMemberMapping ("debtLabel")]
        public static string DebtLabel
        {
            get
            {
                string s = Translator.GetString ("Debt:");
                return s.EndsWith (":") ? s.Substring (0, s.Length - 1) : s;
            }
        }

        [FormMemberMapping ("balanceLabel")]
        public static string BalanceLabel
        {
            get { return Translator.GetString ("Balance"); }
        }

        [FormMemberMapping ("debt")]
        public string Debt { get; set; }

        [FormMemberMapping ("balance")]
        public string Balance { get; set; }

        public PaymentReceipt ()
        {
        }

        public PaymentReceipt (Payment payment)
        {
            ReceiptDate = BusinessDomain.GetFormattedDate (payment.Date);
            long number = payment.Id;
            if (payment.ParentOperation == null)
                payment.ParentOperation = Operation.GetById ((OperationType) payment.OperationType, payment.OperationId);
            if (payment.ParentOperation != null)
                number = payment.ParentOperation.Id;
            ReceiptNumber = number.ToString (CultureInfo.InvariantCulture).PadLeft (BusinessDomain.AppConfiguration.DocumentNumberLength, '0');

            FillRecipient (payment.PartnerId);
            FillSupplier ();

            Location = payment.LocationName;

            double debt = 0;
            if (payment.ParentOperation != null)
                debt = payment.ParentOperation.TotalPlusVAT;

            Debt = Currency.ToString (debt, PriceType.SaleTotal);
            Total = payment.Quantity;
            Balance = Currency.ToString (-Partner.GetDebt (payment.PartnerId), PriceType.SaleTotal);
        }
    }
}
