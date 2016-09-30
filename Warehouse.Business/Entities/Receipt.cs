//
// Receipt.cs
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

using System;
using Warehouse.Business.Documenting;
using Warehouse.Data.DataBinding;

namespace Warehouse.Business.Entities
{
    public abstract class Receipt : Document
    {
        #region Public properties

        #region Receipt header info

        [FormMemberMapping ("receiptSubTitle")]
        public string ReceiptSubTitle { get; set; }

        [FormMemberMapping ("invoiceDate")]
        public string InvoiceDate { get; set; }

        [FormMemberMapping ("invoiceNumber")]
        public string InvoiceNumber { get; set; }

        [FormMemberMapping ("invoiceDateLabel")]
        public static string InvoiceDateLabel { get { return Translator.GetString ("Invoice Date:"); } }

        [FormMemberMapping ("invoiceNumberLabel")]
        public static string InvoiceNumberLabel { get { return Translator.GetString ("Invoice Number:"); } }

        [FormMemberMapping ("receiptDate")]
        public string ReceiptDate { get; set; }

        /// <summary>
        /// Gets the day of the month in which the <see cref="Receipt"/> is given.
        /// </summary>
        /// <value>The day of the month in which the <see cref="Receipt"/> is given.</value>
        [FormMemberMapping ("receiptDay")]
        public string ReceiptDay
        {
            get
            {
                return BusinessDomain.GetDateValue (ReceiptDate).Day.ToString ();
            }
        }

        /// <summary>
        /// Gets the name of the month in which the <see cref="Receipt"/> is given.
        /// </summary>
        /// <value>The name of the month in which the <see cref="Receipt"/> is given.</value>
        [FormMemberMapping ("receiptMonthName")]
        public string ReceiptMonthName
        {
            get
            {
                int month = BusinessDomain.GetDateValue (ReceiptDate).Month - 1;

                return Translator.GetMonths () [month].Value;
            }
        }

        /// <summary>
        /// Gets or sets the year in which the <see cref="Receipt"/> is given.
        /// </summary>
        /// <value>The receipt year in which the <see cref="Receipt"/> is given.</value>
        [FormMemberMapping ("receiptYear")]
        public string ReceiptYear
        {
            get
            {
                return BusinessDomain.GetDateValue (ReceiptDate).Year.ToString ();
            }
        }

        [FormMemberMapping ("receiptNumber")]
        public string ReceiptNumber
        {
            get { return DocumentNumber; }
            set { DocumentNumber = value; }
        }

        [FormMemberMapping ("dateLabel")]
        public static string DateLabel { get { return Translator.GetString ("Date:"); } }

        [FormMemberMapping ("numberLabel")]
        public static string NumberLabel { get { return Translator.GetString ("Number:"); } }

        [FormMemberMapping ("pointOfSale")]
        public string Location { get; set; }

        [FormMemberMapping ("receiptTitle")]
        public virtual string ReceiptTitle { get { return Translator.GetString ("Stock Receipt"); } }

        [FormMemberMapping ("nrcdReceiptTitle")]
        public static string NRCDReceiptTitle { get { return Translator.GetString ("Notice of reception and finding differences"); } }

        [FormMemberMapping ("dateFormat", FormMemberType.Format)]
        public static string DateFormat { get { return Translator.GetString ("Date: {0}"); } }

        [FormMemberMapping ("numberFormat", FormMemberType.Format)]
        public static string NumberFormat { get { return Translator.GetString ("Number: {0}"); } }

        [FormMemberMapping ("pointOfSaleFormat", FormMemberType.Format)]
        public static string LocationFormat { get { return Translator.GetString ("Location: {0}"); } }

        #endregion

        #region Receipt partners info

        [FormMemberMapping ("recipientNameLabel")]
        public static string RecipientNameLabel { get { return Translator.GetString ("Recipient:"); } }

        [FormMemberMapping ("recipientName")]
        public string RecipientName { get; set; }

        [FormMemberMapping ("recipientNumberLabel")]
        public static string RecipientNumberLabel { get { return Translator.GetString ("UIC:"); } }

        [FormMemberMapping ("recipientNumber")]
        public string RecipientNumber { get; set; }

        [FormMemberMapping ("recipientTaxNumberLabel")]
        public static string RecipientTaxNumberLabel { get { return Translator.GetString ("VAT Number:"); } }

        [FormMemberMapping ("recipientTaxNumber")]
        public string RecipientTaxNumber { get; set; }

        [FormMemberMapping ("recipientCityLabel")]
        public static string RecipientCityLabel { get { return Translator.GetString ("City:"); } }

        [FormMemberMapping ("recipientCity")]
        public string RecipientCity { get; set; }

        [FormMemberMapping ("recipientAddressLabel")]
        public static string RecipientAddressLabel { get { return Translator.GetString ("Address:"); } }

        [FormMemberMapping ("recipientAddress")]
        public string RecipientAddress { get; set; }

        [FormMemberMapping ("recipientTelephoneLabel")]
        public static string RecipientTelephoneLabel { get { return Translator.GetString ("Phone:"); } }

        [FormMemberMapping ("recipientTelephone")]
        public string RecipientTelephone { get; set; }

        [FormMemberMapping ("recipientLiablePersonLabel")]
        public static string RecipientLiablePersonLabel { get { return Translator.GetString ("Contact Name:"); } }

        [FormMemberMapping ("recipientLiablePerson")]
        public string RecipientLiablePerson { get; set; }

        /// <summary>
        /// Gets or sets the explanatory label of the field for the copy.
        /// </summary>
        /// <value>The explanatory label of the field for the copy.</value>
        [FormMemberMapping ("copyLabel")]
        public static string CopyLabel { get { return Translator.GetString ("Copy"); } }

        [FormMemberMapping ("shipmentSentBy")]
        public static string ShipmentSentBy { get { return Translator.GetString ("Shipment Sent by:"); } }

        [FormMemberMapping ("acceptedShipmentLabel")]
        public static string ShipmentReceivedByLabel { get { return Translator.GetString ("Shipment Received by:"); } }

        #endregion

        #region Receipt details

        [FormMemberMapping ("receiptDetails")]
        public BindList<ReceiptDetail> ReceiptDetails { get; private set; }

        [FormMemberMapping ("detailHeaderCurrency", FormMemberType.Detail)]
        public static string DetailHeaderCurrency { get { return Translator.GetString ("Currency"); } }

        #endregion

        #region Receipt footer info

        public double Total { get; set; }

        [FormMemberMapping ("total")]
        public string TotalString { get { return Currency.ToString (Total, TotalsPriceType); } }

        [FormMemberMapping ("vat")]
        public string Vat { get; set; }

        /// <summary>
        /// Gets the amount of bank notes (the whole part) of the total price of this <see cref="Receipt"/>.
        /// </summary>
        [FormMemberMapping ("totalPlusVATBankNotes")]
        public string TotalPlusVATBankNotes
        {
            get { return Currency.ToString (Math.Floor (TotalPlusVat), 0); }
        }

        /// <summary>
        /// Gets the amount of coins (the fractional part) of the total price of this <see cref="Receipt"/>.
        /// </summary>
        [FormMemberMapping ("totalPlusVATCoins")]
        public string TotalPlusVATCoins
        {
            get { return Currency.ToString ((TotalPlusVat - Math.Floor (TotalPlusVat)) * 100, 0); }
        }

        [FormMemberMapping ("totalLabel")]
        public static string TotalLabel { get { return Translator.GetString ("Amount"); } }

        [FormMemberMapping ("recipientSignLabel")]
        public static string RecipientSignLabel { get { return Translator.GetString ("Received by:"); } }

        [FormMemberMapping ("creatorSignLabel")]
        public static string CreatorSignLabel { get { return Translator.GetString ("Issued by:"); } }

        [FormMemberMapping ("supplierSignLabel")]
        public static string SupplierSignLabel { get { return Translator.GetString ("Given by:"); } }

        #endregion

        #endregion

        protected Receipt ()
        {
            ReceiptDetails = new BindList<ReceiptDetail> ();
        }

        protected void FillRecipient ()
        {
            CompanyRecord company = CompanyRecord.GetDefault () ?? new CompanyRecord ();

            RecipientName = company.Name;
            RecipientNumber = company.Bulstat;
            RecipientTaxNumber = company.TaxNumber;
            RecipientCity = company.City;
            RecipientAddress = company.Address;
            RecipientTelephone = company.Telephone;
            RecipientLiablePerson = company.LiablePerson;
        }

        protected void FillRecipient (long partnerId)
        {
            Partner partner = Partner.GetById (partnerId);

            RecipientName = partner.Name2;
            RecipientNumber = partner.Bulstat;
            RecipientTaxNumber = partner.TaxNumber;
            RecipientCity = partner.City2;
            RecipientAddress = partner.Address2;
            RecipientTelephone = partner.Telephone2;
            RecipientLiablePerson = partner.LiablePerson2;
        }
    }
}
