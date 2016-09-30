//
// TransferReceipt.cs
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

using System.Linq;
using Warehouse.Business.Documenting;
using Warehouse.Business.Operations;
using Warehouse.Data.DataBinding;

namespace Warehouse.Business.Entities
{
    public class TransferReceipt : Document
    {
        #region Public properties

        public override string Name
        {
            get { return Translator.GetString ("Stock Receipt for Items Transfer"); }
        }

        public override string FormName
        {
            get { return "TransferReceipt"; }
        }

        public override PriceType TotalsPriceType
        {
            get { return BusinessDomain.LoggedUser.HideItemsPurchasePrice ? PriceType.SaleTotal : PriceType.PurchaseTotal; }
        }

        #region Receipt header info

        [FormMemberMapping ("receiptTitle")]
        public static string ReceiptTitle { get { return Translator.GetString ("Stock Receipt"); } }

        [FormMemberMapping ("receiptSubTitle")]
        public static string ReceiptSubTitle { get { return Translator.GetString ("for Items Transfer"); } }

        [FormMemberMapping ("dateFormat", FormMemberType.Format)]
        public static string DateFormat { get { return Translator.GetString ("Date: {0}"); } }

        [FormMemberMapping ("receiptDate")]
        public string ReceiptDate { get; set; }

        [FormMemberMapping ("numberFormat", FormMemberType.Format)]
        public static string NumberFormat { get { return Translator.GetString ("Number: {0}"); } }

        [FormMemberMapping ("receiptNumber")]
        public string ReceiptNumber
        {
            get { return DocumentNumber; } 
            set { DocumentNumber = value; }
        }

        [FormMemberMapping ("sourcePointOfSale")]
        public string SourceLocation { get; set; }

        [FormMemberMapping ("sourcePointOfSaleFormat", FormMemberType.Format)]
        public static string SourceLocationFormat { get { return Translator.GetString ("From location: {0}"); } }

        [FormMemberMapping ("targetPointOfSale")]
        public string TargetLocation { get; set; }

        [FormMemberMapping ("targetPointOfSaleFormat", FormMemberType.Format)]
        public static string TargetLocationFormat { get { return Translator.GetString ("To location: {0}"); } }

        #endregion

        #region Receipt details

        [FormMemberMapping ("receiptDetails")]
        public BindList<ReceiptDetail> ReceiptDetails { get; private set; }

        [FormMemberMapping ("detailHeaderCurrency", FormMemberType.Detail)]
        public static string DetailHeaderCurrency { get { return Translator.GetString ("Currency"); } }

        #endregion

        #region Receipt footer info

        [FormMemberMapping ("total")]
        public string Total { get; set; }

        [FormMemberMapping ("vat")]
        public string Vat { get; set; }

        [FormMemberMapping ("totalLabel")]
        public static string TotalLabel { get { return Translator.GetString ("Net Amount"); } }

        [FormMemberMapping ("recipientSignLabel")]
        public static string RecipientSignLabel { get { return Translator.GetString ("Received by:"); } }

        [FormMemberMapping ("creatorSignLabel")]
        public static string CreatorSignLabel { get { return Translator.GetString ("Issued by:"); } }

        [FormMemberMapping ("supplierSignLabel")]
        public static string SupplierSignLabel { get { return Translator.GetString ("Given by:"); } }

        #endregion

        #endregion

        public TransferReceipt ()
        {
            ReceiptDetails = new BindList<ReceiptDetail> ();
        }

        public TransferReceipt (Transfer transfer)
            : this ()
        {
            ReceiptDate = BusinessDomain.GetFormattedDate (transfer.Date);
            ReceiptNumber = transfer.FormattedOperationNumber;
            Note = transfer.Note;

            FillSupplier ();

            SourceLocation = transfer.SourceLocation2;
            TargetLocation = transfer.TargetLocation2;
            Transfer clone = transfer.Clone<Transfer, TransferDetail> ();
            if (BusinessDomain.AppConfiguration.AlwaysPrintTransfersUsingSalePrices)
                clone.SetUsePriceIn (false);

            int i = 1;
            foreach (TransferDetail detail in clone.Details) {
                detail.TotalEvaluate ();
                ReceiptDetails.Add (new ReceiptDetail (i++, detail, clone.GetUsePriceIn ()));
            }

            double vat = clone.VAT;
            if (BusinessDomain.AppConfiguration.VATIncluded) {
                Total = Currency.ToString (clone.Total - vat, clone.TotalsPriceType);
                Vat = Currency.ToString (vat, clone.TotalsPriceType);
                TotalPlusVat = clone.Total;
            } else {
                Total = Currency.ToString (clone.Total, clone.TotalsPriceType);
                Vat = Currency.ToString (vat, clone.TotalsPriceType);
                TotalPlusVat = clone.Total + vat;
            }

            TotalQuantity = Quantity.ToString (clone.Details.Sum (d => d.Quantity));
        }
    }
}