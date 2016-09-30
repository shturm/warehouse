//
// Document.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   10.06.2010
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

using Warehouse.Business.CurrencyTranslation;
using Warehouse.Business.Documenting;
using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public abstract class Document : IDocument
    {
        [FormMemberMapping ("detailHeaderNumber", FormMemberType.Detail)]
        public static string DetailHeaderNumber { get { return Translator.GetString ("No."); } }

        [FormMemberMapping ("detailHeaderCode", FormMemberType.Detail)]
        public static string DetailHeaderCode { get { return Translator.GetString ("Code"); } }

        [FormMemberMapping ("detailHeaderGoods", FormMemberType.Detail)]
        public static string DetailHeaderItem { get { return Translator.GetString ("Item"); } }

        [FormMemberMapping ("detailHeaderMUnit", FormMemberType.Detail)]
        public static string DetailHeaderMUnit { get { return Translator.GetString ("Measure"); } }

        [FormMemberMapping ("detailHeaderQtty", FormMemberType.Detail)]
        public static string DetailHeaderQtty { get { return Translator.GetString ("Qty"); } }

        [FormMemberMapping ("detailHeaderPrice", FormMemberType.Detail)]
        public static string DetailHeaderPrice { get { return Translator.GetString ("Price"); } }

        [FormMemberMapping ("detailHeaderTotal", FormMemberType.Detail)]
        public static string DetailHeaderTotal { get { return Translator.GetString ("Amount"); } }

        [FormMemberMapping ("detailHeaderLot", FormMemberType.Detail)]
        public static string DetailHeaderLot { get { return Translator.GetString ("Lot"); } }

        [FormMemberMapping ("detailHeaderSerialNumber", FormMemberType.Detail)]
        public static string DetailHeaderSerialNumber { get { return Translator.GetString ("Serial number"); } }

        [FormMemberMapping ("detailHeaderExpirationDate", FormMemberType.Detail)]
        public static string DetailHeaderExpirationDate { get { return Translator.GetString ("Expiration date"); } }

        [FormMemberMapping ("detailHeaderProductionDate", FormMemberType.Detail)]
        public static string DetailHeaderProductionDate { get { return Translator.GetString ("Production date"); } }

        [FormMemberMapping ("detailHeaderLotLocation", FormMemberType.Detail)]
        public static string DetailHeaderLotLocation { get { return Translator.GetString ("Lot location"); } }

        [FormMemberMapping ("totalPlusVatLabel")]
        public static string TotalPlusVatLabel { get { return Translator.GetString ("Total"); } }

        [FormMemberMapping ("totalInWordsLabel")]
        public static string TotalInWordsLabel { get { return Translator.GetString ("Total (in words):"); } }

        [FormMemberMapping ("vatLabel")]
        public virtual string VatLabel { get { return Translator.GetString ("VAT"); } }

        [FormMemberMapping ("currentPageFormat", FormMemberType.Format)]
        public static string CurrentPageFormat { get { return Translator.GetString ("page {0}"); } }

        [FormMemberMapping ("totalPagesFormat", FormMemberType.Format)]
        public static string TotalPagesFormat { get { return Translator.GetString ("from {0}"); } }

        [FormMemberMapping ("productId")]
        public static string ProductId { get { return string.Format (Translator.GetString ("Printed by {0}, {1}"), DataHelper.ProductName, DataHelper.CompanyWebSite); } }

        private string documentNumber = string.Empty;
        private string recipient = string.Empty;
        private string recipientEGN = string.Empty;
        private string provider = string.Empty;

        [FormMemberMapping ("documentNumber")]
        public string DocumentNumber
        {
            get { return documentNumber; }
            set { documentNumber = value; }
        }

        public abstract string Name { get; }

        public string FileName
        {
            get { return string.Format ("{0}-{1}", FormName, documentNumber); }
        }

        public abstract string FormName { get; }

        [FormMemberMapping ("totalInWords")]
        public virtual string TotalInWords
        {
            get { return NumberToWords.Translate (TotalPlusVat); }
        }

        public double TotalPlusVat { get; set; }

        [FormMemberMapping ("totalPlusVat")]
        public string TotalPlusVatString { get { return Currency.ToString (TotalPlusVat, TotalsPriceType); } }

        public virtual PriceType TotalsPriceType
        {
            get { return PriceType.SaleTotal; }
        }

        [FormMemberMapping ("totalQuantity")]
        public string TotalQuantity { get; set; }

        [FormMemberMapping ("supplierNameLabel")]
        public static string SupplierNameLabel { get { return Translator.GetString ("Supplier:"); } }

        [FormMemberMapping ("supplierName")]
        public string SupplierName { get; set; }

        [FormMemberMapping ("supplierNumberLabel")]
        public static string SupplierNumberLabel { get { return Translator.GetString ("UIC:"); } }

        [FormMemberMapping ("supplierNumber")]
        public string SupplierNumber { get; set; }

        [FormMemberMapping ("supplierTaxNumberLabel")]
        public static string SupplierTaxNumberLabel { get { return Translator.GetString ("VAT Number:"); } }

        [FormMemberMapping ("supplierTaxNumber")]
        public string SupplierTaxNumber { get; set; }

        [FormMemberMapping ("supplierCityLabel")]
        public static string SupplierCityLabel { get { return Translator.GetString ("City:"); } }

        [FormMemberMapping ("supplierCity")]
        public string SupplierCity { get; set; }

        [FormMemberMapping ("supplierAddressLabel")]
        public static string SupplierAddressLabel { get { return Translator.GetString ("Address:"); } }

        [FormMemberMapping ("supplierAddress")]
        public string SupplierAddress { get; set; }

        [FormMemberMapping ("supplierLiablePersonLabel")]
        public static string SupplierLiablePersonLabel { get { return Translator.GetString ("Contact Name:"); } }

        [FormMemberMapping ("supplierLiablePerson")]
        public string SupplierLiablePerson { get; set; }

        [FormMemberMapping ("supplierTelephoneLabel")]
        public static string SupplierTelephoneLabel { get { return Translator.GetString ("Phone:"); } }

        [FormMemberMapping ("supplierTelephone")]
        public string SupplierTelephone { get; set; }

        [FormMemberMapping ("supplierBankNameLabel")]
        public static string SupplierBankNameLabel { get { return Translator.GetString ("Bank name:"); } }

        [FormMemberMapping ("supplierBankName")]
        public string SupplierBankName { get; set; }

        [FormMemberMapping ("supplierBankAccountLabel")]
        public static string SupplierBankAccountLabel { get { return Translator.GetString ("Bank account:"); } }

        [FormMemberMapping ("supplierBankAccount")]
        public string SupplierBankAccount { get; set; }

        [FormMemberMapping ("supplierBankBIC")]
        public string SupplierBankBIC { get; set; }

        [FormMemberMapping ("recipientLabel")]
        public static string RecipientLabel { get { return Translator.GetString ("Received by:"); } }

        [FormMemberMapping ("recipient")]
        [DbColumn (DataField.DocumentRecipient, 255)]
        public string Recipient
        {
            get { return recipient; }
            set { recipient = value; }
        }

        [FormMemberMapping ("recipientEGNLabel")]
        public static string RecipientEGNLabel { get { return Translator.GetString ("UCN:"); } }

        [FormMemberMapping ("recipientEGN")]
        [DbColumn (DataField.DocumentRecipientEGN, 255)]
        public string RecipientEGN
        {
            get { return recipientEGN; }
            set { recipientEGN = value; }
        }

        [FormMemberMapping ("recipientSigantureLabel")]
        public static string RecipientSigantureLabel { get { return Translator.GetString ("Responsible:"); } }

        [FormMemberMapping ("bankLabel")]
        public static string BankLabel { get { return Translator.GetString ("Bank:"); } }

        [FormMemberMapping ("bicLabel")]
        public static string BicLabel { get { return Translator.GetString ("BIC:"); } }

        [FormMemberMapping ("ibanLabel")]
        public static string IbanLabel { get { return Translator.GetString ("IBAN:"); } }

        [FormMemberMapping ("providerLabel")]
        public static string ProviderLabel { get { return Translator.GetString ("Issued by:"); } }

        [FormMemberMapping ("provider")]
        [DbColumn (DataField.DocumentProvider, 255)]
        public string Provider
        {
            get { return provider; }
            set { provider = value; }
        }

        [FormMemberMapping ("providerSignatureLabel")]
        public static string ProviderSignatureLabel { get { return Translator.GetString ("Responsible:"); } }

        [FormMemberMapping ("noteLabel")]
        public static string NoteLabel { get { return Translator.GetString ("Note:"); } }

        [FormMemberMapping ("note")]
        public string Note { get; set; }

        protected void FillSupplier ()
        {
            CompanyRecord company = CompanyRecord.GetDefault () ?? new CompanyRecord ();

            SupplierName = company.Name;
            SupplierNumber = company.Bulstat;
            SupplierTaxNumber = company.TaxNumber;
            SupplierCity = company.City;
            SupplierAddress = company.Address;
            SupplierLiablePerson = company.LiablePerson;
            SupplierTelephone = company.Telephone;
            SupplierBankName = company.BankName;
            SupplierBankBIC = company.BankCode;
            SupplierBankAccount = company.BankAccount;
        }

        protected void FillSupplier (long partnerId)
        {
            Partner partner = Partner.GetById (partnerId);

            SupplierName = partner.Name2;
            SupplierNumber = partner.Bulstat;
            SupplierTaxNumber = partner.TaxNumber;
            SupplierCity = partner.City2;
            SupplierAddress = partner.Address2;
            SupplierLiablePerson = partner.LiablePerson2;
            SupplierTelephone = partner.Telephone2;
            SupplierBankName = partner.BankName;
            SupplierBankBIC = partner.BankCode;
            SupplierBankAccount = partner.BankAccount;
        }

        public object Clone ()
        {
            return MemberwiseClone ();
        }
    }
}
