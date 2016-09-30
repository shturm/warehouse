//
// DocumentBase.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
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
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Warehouse.Business.Documenting;
using Warehouse.Business.Operations;
using Warehouse.Data;
using Warehouse.Data.DataBinding;

namespace Warehouse.Business.Entities
{
    public abstract class DocumentBase<TDetail> : DocumentBase where TDetail : InvoiceDetail, new ()
    {
        #region Private members

        private long id = -1;
        private long operationNumber = -1;
        protected long operationType = -1;

        #region Document header info

        private string invoiceType = string.Empty;

        private long number = 1;

        private DateTime date = BusinessDomain.Today;
        private string dateString = string.Empty;

        private long referenceNumber = 1;
        private string referenceNumberString = string.Empty;

        private DateTime referenceDate = BusinessDomain.Today;
        private string referenceDateString = string.Empty;

        #endregion

        #region Document partners info

        #endregion

        private string description = string.Empty;
        private string location = string.Empty;

        #region Document details

        private readonly BindList<TDetail> details = new BindList<TDetail> ();

        #endregion

        #region Document footer info

        private long paymentMethod = (long) BasePaymentType.Cash;
        private string paymentMethodString = string.Empty;

        private DateTime taxDate = BusinessDomain.Today;
        private string taxDateString = string.Empty;

        private string reason = string.Empty;

        protected DocumentBase ()
        {
            OperationDate = BusinessDomain.Today;
            RecipientTaxNumber = string.Empty;
        }

        #endregion

        #endregion

        #region Public properties

        [DbColumn (DataField.DocumentId)]
        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        [DbColumn (DataField.DocumentOperationNumber)]
        public long OperationNumber
        {
            get { return operationNumber; }
            set { operationNumber = value; }
        }

        [DbColumn (DataField.DocumentOperationType)]
        public long OperationType
        {
            get { return operationType; }
            set { operationType = value; }
        }

        #region Document header info

        [FormMemberMapping ("invoiceType")]
        public string InvoiceType
        {
            get { return invoiceType; }
            set { invoiceType = value; }
        }

        [FormMemberMapping ("numberLabel")]
        public static string NumberLabel { get { return Translator.GetString ("Number:"); } }

        [FormMemberMapping ("number")]
        public long Number
        {
            get { return number; }
            set
            {
                number = value;
                DocumentNumber = value < 1 ? string.Empty : GetFormattedDocumentNumber (value);
            }
        }

        [FormMemberMapping ("number")]
        [DbColumn (DataField.DocumentNumber, 255)]
        public string NumberString
        {
            get { return DocumentNumber; }
            set
            {
                DocumentNumber = value;
                long.TryParse (value, out number);
            }
        }

        [FormMemberMapping ("referenceNumberLabel")]
        public static string ReferenceNumberLabel { get { return Translator.GetString ("To Document No.:"); } }

        public long ReferenceNumber
        {
            get { return referenceNumber; }
            set
            {
                referenceNumber = value;
                if (value < 1) {
                    referenceNumberString = string.Empty;
                } else {
                    referenceNumberString = GetFormattedDocumentNumber (value);
                }
            }
        }

        [FormMemberMapping ("referenceNumber")]
        [DbColumn (DataField.DocumentReferenceNumber, 255)]
        public string ReferenceNumberString
        {
            get { return referenceNumberString; }
            set
            {
                referenceNumberString = value;
                long.TryParse (value, out referenceNumber);
            }
        }

        [FormMemberMapping ("dateLabel")]
        public static string DateLabel { get { return Translator.GetString ("Date:"); } }

        [DbColumn (DataField.DocumentDate)]
        public DateTime Date
        {
            get { return date; }
            set
            {
                date = value;
                dateString = value == DateTime.MinValue ? string.Empty : BusinessDomain.GetFormattedDate (value);
            }
        }

        [DbColumn (DataField.OperationDate)]
        public DateTime OperationDate { get; set; }

        [FormMemberMapping ("date")]
        public string DateString
        {
            get { return dateString; }
            set
            {
                dateString = value;
                date = BusinessDomain.GetDateValue (value);
            }
        }

        [FormMemberMapping ("referenceDateLabel")]
        public static string ReferenceDateLabel { get { return Translator.GetString ("from date:"); } }

        [DbColumn (DataField.DocumentReferenceDate)]
        public DateTime ReferenceDate
        {
            get { return referenceDate; }
            set
            {
                referenceDate = value;
                if (value == DateTime.MinValue) {
                    referenceDateString = string.Empty;
                } else {
                    referenceDateString = BusinessDomain.GetFormattedDate (value);
                }
            }
        }

        [FormMemberMapping ("referenceDate")]
        public string ReferenceDateString
        {
            get { return referenceDateString; }
            set
            {
                referenceDateString = value;
                referenceDate = BusinessDomain.GetDateValue (value);
            }
        }

        #endregion

        #region Document partners info

        [FormMemberMapping ("recipientNameLabel")]
        public static string RecipientNameLabel { get { return Translator.GetString ("Recipient:"); } }

        [FormMemberMapping ("recipientName"), DbColumn (DataField.PartnerName)]
        public string RecipientName { get; set; }

        [FormMemberMapping ("recipientNumberLabel")]
        public static string RecipientNumberLabel { get { return Translator.GetString ("UIC:"); } }

        [FormMemberMapping ("recipientNumber"), DbColumn (DataField.PartnerBulstat)]
        public string RecipientNumber { get; set; }

        [FormMemberMapping ("recipientTaxNumberLabel")]
        public static string RecipientTaxNumberLabel { get { return Translator.GetString ("VAT Number:"); } }

        [FormMemberMapping ("recipientTaxNumber"), DbColumn (DataField.PartnerTaxNumber)]
        public string RecipientTaxNumber { get; set; }

        [FormMemberMapping ("recipientCityLabel")]
        public static string RecipientCityLabel { get { return Translator.GetString ("City:"); } }

        [FormMemberMapping ("recipientCity"), DbColumn (DataField.PartnerCity)]
        public string RecipientCity { get; set; }

        [FormMemberMapping ("recipientAddressLabel")]
        public static string RecipientAddressLabel { get { return Translator.GetString ("Address:"); } }

        [FormMemberMapping ("recipientAddress"), DbColumn (DataField.PartnerAddress)]
        public string RecipientAddress { get; set; }

        [FormMemberMapping ("recipientLiablePersonLabel")]
        public static string RecipientLiablePersonLabel { get { return Translator.GetString ("Contact Name:"); } }

        [FormMemberMapping ("recipientLiablePerson"), DbColumn (DataField.PartnerLiablePerson)]
        public string RecipientLiablePerson { get; set; }

        [FormMemberMapping ("recipientTelephoneLabel")]
        public static string RecipientTelephoneLabel { get { return Translator.GetString ("Phone:"); } }

        [FormMemberMapping ("recipientTelephone")]
        public string RecipientTelephone { get; set; }

        [FormMemberMapping ("recipientBankNameLabel")]
        public static string RecipientBankNameLabel { get { return Translator.GetString ("Bank name:"); } }

        [FormMemberMapping ("recipientBankName"), DbColumn (DataField.PartnerBankName)]
        public string RecipientBankName { get; set; }

        [FormMemberMapping ("recipientBankAccountLabel")]
        public static string RecipientBankAccountLabel { get { return Translator.GetString ("Bank account:"); } }

        [FormMemberMapping ("recipientBankAccount"), DbColumn (DataField.PartnerBankAcct)]
        public string RecipientBankAccount { get; set; }

        [FormMemberMapping ("recipientCode"), DbColumn (DataField.PartnerCode)]
        public string RecipientCode { get; set; }

        #endregion

        [FormMemberMapping ("descriptionLabel")]
        public static string DescriptionLabel { get { return Translator.GetString ("Deal description:"); } }

        [FormMemberMapping ("description")]
        [DbColumn (DataField.DocumentDescription, 255)]
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        [FormMemberMapping ("pointOfSaleLabel")]
        public static string LocationLabel { get { return Translator.GetString ("Deal place:"); } }

        [FormMemberMapping ("pointOfSale")]
        [DbColumn (DataField.DocumentLocation, 255)]
        public string Location
        {
            get { return location; }
            set { location = value; }
        }

        #region Document details

        [FormMemberMapping ("invoiceDetails")]
        public BindList<TDetail> Details
        {
            get { return details; }
        }

        [FormMemberMapping ("invoiceDetailsVATGrouped")]
        public SortedDictionary<string, List<TDetail>> DocumentDetailsVATGrouped
        {
            get
            {
                //bool useSalesTax = BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT;
                SortedDictionary<string, List<TDetail>> groups = new SortedDictionary<string, List<TDetail>> ();
                foreach (TDetail d in details) {
                    TDetail detail = (TDetail) d.Clone ();

                    //string vatGroupName = Percent.IsVisiblyEqual (detail.VatRate, detail.VatGroup.VatValue) ?
                    //    string.Format ("{0} ({1})", detail.VatGroup.Name, Percent.ToString (detail.VatGroup.VatValue)) :
                    //    string.Format ("{0} ({1})", useSalesTax ? Translator.GetString ("Sales tax") : Translator.GetString ("VAT"), Percent.ToString (detail.VatRate));
                    string vatGroupName = string.Format ("{0} ({1})", detail.VatGroup.Name, Percent.ToString (detail.VatGroup.VatValue));
                    List<TDetail> groupDetails;

                    if (groups.ContainsKey (vatGroupName)) {
                        groupDetails = groups [vatGroupName];
                        groupDetails.Add (detail);
                    } else {
                        groupDetails = new List<TDetail> (new [] { detail });
                        groups.Add (vatGroupName, groupDetails);
                    }

                    groupDetails [groupDetails.Count - 1].Number = groupDetails.Count.ToString (CultureInfo.InvariantCulture);
                }

                return groups;
            }
        }

        [FormMemberMapping ("detailHeaderPriceWithoutVAT", FormMemberType.Detail)]
        public static string DetailHeaderPriceWithoutVAT { get { return Translator.GetString ("Price w/o VAT"); } }

        [FormMemberMapping ("detailHeaderTotalWithoutVAT", FormMemberType.Detail)]
        public static string DetailHeaderTotalWithoutVAT { get { return Translator.GetString ("Amount w/o VAT"); } }

        [FormMemberMapping ("detailHeaderTotalVAT", FormMemberType.Detail)]
        public static string DetailHeaderTotalVAT { get { return Translator.GetString ("VAT Amount"); } }

        #endregion

        #region Document footer info

        [FormMemberMapping ("paymentTypeLabel")]
        public static string PaymentTypeLabel { get { return Translator.GetString ("Payment:"); } }

        [DbColumn (DataField.DocumentPaymentTypeId)]
        public long PaymentMethod
        {
            get { return paymentMethod; }
            set { paymentMethod = value; }
        }

        [FormMemberMapping ("paymentType")]
        public string PaymentMethodString
        {
            get { return paymentMethodString; }
            set { paymentMethodString = value; }
        }

        [FormMemberMapping ("totalLabel")]
        public static string TotalLabel { get { return Translator.GetString ("Net Amount"); } }

        [DbColumn (DataField.OperationTotal)]
        public double Total { get; set; }

        [FormMemberMapping ("total")]
        public string TotalString
        {
            get { return Currency.ToString (Total, TotalsPriceType); }
        }

        [DbColumn (DataField.OperationVatSum)]
        public double Vat { get; set; }

        [FormMemberMapping ("vat")]
        public string VatString
        {
            get { return Currency.ToString (Vat); }
        }

        [FormMemberMapping ("vatGrouped")]
        public SortedDictionary<string, string> VatGrouped
        {
            get
            {
                //bool useSalesTax = BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT;
                Dictionary<string, double> groups = new Dictionary<string, double> ();
                double detailsVAT = 0;
                foreach (TDetail detail in details) {

                    //string vatGroupName = Percent.IsVisiblyEqual (detail.VatRate, detail.VatGroup.VatValue) ?
                    //    string.Format ("{0} ({1})", detail.VatGroup.Name, Percent.ToString (detail.VatGroup.VatValue)) :
                    //    string.Format ("{0} ({1})", useSalesTax ? Translator.GetString ("Sales tax") : Translator.GetString ("VAT"), Percent.ToString (detail.VatRate));
                    string vatGroupName = string.Format ("{0} ({1})", detail.VatGroup.Name, Percent.ToString (detail.VatGroup.VatValue));
                    double value = detail.VATOut * detail.Quantity;

                    if (groups.ContainsKey (vatGroupName))
                        groups [vatGroupName] += value;
                    else
                        groups.Add (vatGroupName, value);
                    detailsVAT += value;
                }

                SortedDictionary<string, string> ret = new SortedDictionary<string, string> ();
                double remainderShare = Vat.IsZero () ? 0 : (Vat - detailsVAT) / Vat;
                foreach (KeyValuePair<string, double> group in groups) {
                    // distribute the possible rounding loss among tax groups
                    double value = (1 + remainderShare) * group.Value;
                    ret.Add (group.Key, Currency.ToString (Currency.Round (value, TotalsPriceType), TotalsPriceType));
                }

                return ret;
            }
        }

        [FormMemberMapping ("taxDateLabel")]
        public static string TaxDateLabel { get { return Translator.GetString ("Date of financial event:"); } }

        [DbColumn (DataField.DocumentTaxDate)]
        public DateTime TaxDate
        {
            get { return taxDate; }
            set
            {
                taxDate = value;
                taxDateString = value == DateTime.MinValue ?
                    string.Empty : BusinessDomain.GetFormattedDate (value);
            }
        }

        [FormMemberMapping ("taxDate")]
        public string TaxDateString
        {
            get { return taxDateString; }
            set
            {
                taxDateString = value;
                taxDate = BusinessDomain.GetDateValue (value);
            }
        }

        [FormMemberMapping ("reasonLabel")]
        public static string ReasonLabel { get { return Translator.GetString ("Deal reason:"); } }

        [FormMemberMapping ("reason")]
        [DbColumn (DataField.DocumentReason, 255)]
        public string Reason
        {
            get { return reason; }
            set { reason = value; }
        }

        [DbColumn (DataField.PurchaseTotal)]
        public double PurchaseTotal { get; set; }

        [DbColumn (DataField.PurchaseVATSum)]
        public double PurchaseVATSum { get; set; }

        [DbColumn (DataField.OperationDetailNote)]
        public string OperationNote { get; set; }

        #endregion

        public SaleSignature Signature { get; set; }

        [FormMemberMapping ("internalDataLabel")]
        public string InternalDataLabel { get { return Signature != null ? Translator.GetString ("Internal data:") : string.Empty; } }

        [FormMemberMapping ("internalData")]
        public string InternalData { get { return Signature != null ? Signature.InternalInfo : string.Empty; } }

        [FormMemberMapping ("receiptSignatureLabel")]
        public string ReceiptSignatureLabel { get { return Signature != null ? Translator.GetString ("Receipt signature:") : string.Empty; } }

        [FormMemberMapping ("receiptSignature")]
        public string ReceiptSignature { get { return Signature != null ? string.Join (Environment.NewLine, Signature.ReceiptInfo.Wrap (56, 10)) : string.Empty; } }

        [FormMemberMapping ("sdcIdLabel")]
        public string SDCIDLabel { get { return Signature != null ? Translator.GetString ("SDC ID:") : string.Empty; } }

        [FormMemberMapping ("sdcId")]
        public string SDCID { get { return Signature != null ? Signature.DeviceId : string.Empty; } }

        #endregion

        public static string GetFormattedDocumentNumber (long number)
        {
            return Operation.GetFormattedOperationNumber (number);
        }

        public virtual void LoadOperationInfo<TOperDetail> (Operation<TOperDetail> operation) where TOperDetail : OperationDetail
        {
            Partner partner = Partner.GetById (operation.PartnerId);
            RecipientName = partner.Name2;
            RecipientNumber = partner.Bulstat;
            RecipientTaxNumber = partner.TaxNumber;
            RecipientCity = partner.City2;
            RecipientAddress = partner.Address2;
            RecipientLiablePerson = partner.LiablePerson2;
            RecipientTelephone = partner.Telephone2;
            RecipientBankName = partner.BankName;
            RecipientBankAccount = partner.BankAccount;
            RecipientCode = partner.Code;

            FillSupplier ();

            double operationVAT = operation.VAT;
            if (BusinessDomain.AppConfiguration.VATIncluded) {
                Total = operation.Total - operationVAT;
                Vat = operationVAT;
                TotalPlusVat = operation.Total;
            } else {
                Total = operation.Total;
                Vat = operationVAT;
                TotalPlusVat = operation.Total + operationVAT;
            }
            LoadOperationDetailInfo (operation.Details);
        }

        protected void LoadOperationDetailInfo (IEnumerable<OperationDetail> operationDetails)
        {
            int i = 1;
            foreach (OperationDetail detail in operationDetails) {
                Item item = Item.GetById (detail.ItemId);

                details.Add (new TDetail
                    {
                        Number = i.ToString (CultureInfo.InvariantCulture),
                        DetailId = detail.DetailId,
                        ItemId = item.Id,
                        ItemCode = item.Code,
                        ItemName = item.Name2,
                        MUnitName = detail.MUnitName,
                        Quantity = detail.Quantity,
                        LotId = detail.LotId,
                        Lot = detail.Lot,
                        SerialNumber = detail.SerialNumber,
                        ExpirationDate = detail.ExpirationDate,
                        ProductionDate = detail.ProductionDate,
                        LotLocation = detail.LotLocation,
                        PriceIn = detail.PriceInDB,
                        VATIn = detail.VATIn,
                        PriceOut = detail.PriceOutDB,
                        VATOut = detail.VATOut,
                        Discount = detail.Discount,
                        DiscountValue = detail.DiscountValue,
                        Total = detail.Total
                    });

                i++;
            }
        }
    }

    public abstract class DocumentBase : Document
    {
        #region Private fields

        private bool? isOriginal;
        private string original = string.Empty;

        #endregion

        #region Public properties

        public string OriginalString { get; protected set; }
        public string DuplicateString { get; protected set; }

        [FormMemberMapping ("isOriginal")]
        public bool? IsOriginal
        {
            get { return isOriginal; }
            set
            {
                isOriginal = value;
                switch (value) {
                    case true:
                        original = OriginalString;
                        break;
                    case false:
                        original = DuplicateString;
                        break;
                    case null:
                        original = string.Empty;
                        break;
                }
            }
        }

        [FormMemberMapping ("original")]
        public string Original
        {
            get { return original; }
        }

        public bool PrintOriginal { get; set; }

        public int PrintCopies { get; set; }

        public bool PrintInternational { get; set; }

        #endregion

        protected DocumentBase ()
        {
            OriginalString = Translator.GetString ("Original");
            DuplicateString = Translator.GetString ("Duplicate");
        }

        public abstract void CommitChanges ();

        #region Fields suggestions

        public static List<string> GetRecipientSuggestions (long partnerId)
        {
            return BusinessDomain.DataAccessProvider.GetDocumentRecipientSuggestions (partnerId);
        }

        public static List<string> GetEGNSuggestions (long partnerId)
        {
            return BusinessDomain.DataAccessProvider.GetDocumentEGNSuggestions (partnerId);
        }

        public static List<string> GetProviderSuggestions ()
        {
            return BusinessDomain.DataAccessProvider.GetDocumentProviderSuggestions ();
        }

        public static List<string> GetReasonSuggestions ()
        {
            return BusinessDomain.DataAccessProvider.GetDocumentReasonSuggestions ();
        }

        public static List<string> GetDescriptionSuggestions ()
        {
            return BusinessDomain.DataAccessProvider.GetDocumentDescriptionSuggestions ();
        }

        public static IEnumerable<string> GetLocationSuggestions ()
        {
            return BusinessDomain.DataAccessProvider.GetDocumentLocationSuggestions ();
        }

        public static KeyValuePair<int, string> [] GetAllDocumentTypes ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) DocumentType.Invoice, Translator.GetString ("Invoice")),
                    new KeyValuePair<int, string> ((int) DocumentType.CreditNote, Translator.GetString ("Credit Note")),
                    new KeyValuePair<int, string> ((int) DocumentType.DebitNote, Translator.GetString ("Debit Note"))
                };
        }

        #endregion

        public abstract DocumentBase GetInternational ();

        protected void CopyTo (object destination)
        {
            PropertyInfo [] properties = GetType ().GetProperties (BindingFlags.Public | BindingFlags.Instance);
            Type type = destination.GetType ();
            foreach (PropertyInfo property in properties) {
                PropertyInfo destinationProperty = type.GetProperty (property.Name, BindingFlags.Public | BindingFlags.Instance);
                if (destinationProperty.CanWrite && property.CanRead && (destinationProperty.PropertyType == property.PropertyType) &&
                    destinationProperty.GetSetMethod () != null)
                    destinationProperty.SetValue (destination, property.GetValue (this, null), null);
            }
        }

        public static long GetLastDocumentNumber (long operationNumber, OperationType operationType)
        {
            return BusinessDomain.DataAccessProvider.GetLastDocumentNumber (operationNumber, (long) operationType);
        }

        public static bool IsNumberUsed (string number)
        {
            long num;

            if (!long.TryParse (number, out num))
                throw new Exception (string.Format ("Badly formatted document number:\"{0}\"", number));

            return BusinessDomain.DataAccessProvider.IsDocumentNumberUsed (num);
        }

        public static bool DocumentExistsForOperation (long operationNumber, OperationType operationType)
        {
            return BusinessDomain.DataAccessProvider.DocumentExistsForOperation (operationNumber, (long) operationType);
        }
    }
}
