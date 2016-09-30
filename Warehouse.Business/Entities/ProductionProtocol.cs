//
// ProductionProtocol.cs
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

using Warehouse.Business.Documenting;
using Warehouse.Business.Operations;
using Warehouse.Data.DataBinding;

namespace Warehouse.Business.Entities
{
    public class ProductionProtocol : Document
    {
        public override string Name
        {
            get { return Translator.GetString ("Protocol for Items Production"); }
        }

        public override string FormName
        {
            get { return "ProductionProtocol"; }
        }

        public override PriceType TotalsPriceType
        {
            get { return PriceType.PurchaseTotal; }
        }

        #region Public properties

        #region Protocol header info

        [FormMemberMapping ("protocolDate")]
        public string ProtocolDate { get; set; }

        [FormMemberMapping ("protocolNumber")]
        public string ProtocolNumber
        {
            get { return DocumentNumber; }
            set { DocumentNumber = value; }
        }

        [FormMemberMapping ("pointOfSale")]
        public string Location { get; set; }

        [FormMemberMapping ("protocolTitle")]
        public static string ProtocolTitle { get { return Translator.GetString ("Protocol"); } }

        [FormMemberMapping ("protocolSubTitle")]
        public static string ProtocolSubTitle { get { return Translator.GetString ("for Items Production"); } }

        [FormMemberMapping ("dateFormat", FormMemberType.Format)]
        public static string DateFormat { get { return Translator.GetString ("Date: {0}"); } }

        [FormMemberMapping ("numberFormat", FormMemberType.Format)]
        public static string NumberFormat { get { return Translator.GetString ("Number: {0}"); } }

        [FormMemberMapping ("pointOfSaleFormat", FormMemberType.Format)]
        public static string LocationFormat { get { return Translator.GetString ("Location: {0}"); } }

        #endregion

        #region Protocol partners info

        [FormMemberMapping ("companyName")]
        public string CompanyName { get; set; }

        [FormMemberMapping ("companyNumber")]
        public string CompanyNumber { get; set; }

        [FormMemberMapping ("companyCity")]
        public string CompanyCity { get; set; }

        [FormMemberMapping ("companyAddress")]
        public string CompanyAddress { get; set; }

        [FormMemberMapping ("companyTelephone")]
        public string CompanyTelephone { get; set; }

        [FormMemberMapping ("companyLiablePerson")]
        public string CompanyLiablePerson { get; set; }

        [FormMemberMapping ("companyNameLabel")]
        public static string CompanyNameLabel { get { return Translator.GetString ("Company:"); } }

        [FormMemberMapping ("companyNumberLabel")]
        public static string CompanyNumberLabel { get { return Translator.GetString ("UIC:"); } }

        [FormMemberMapping ("companyCityLabel")]
        public static string CompanyCityLabel { get { return Translator.GetString ("City:"); } }

        [FormMemberMapping ("companyAddressLabel")]
        public static string CompanyAddressLabel { get { return Translator.GetString ("Address:"); } }

        [FormMemberMapping ("companyTelephoneLabel")]
        public static string CompanyTelephoneLabel { get { return Translator.GetString ("Phone:"); } }

        [FormMemberMapping ("companyLiablePersonLabel")]
        public static string CompanyLiablePersonLabel { get { return Translator.GetString ("Contact Name:"); } }

        #endregion

        #region Protocol details

        [FormMemberMapping ("materialsLabel")]
        public static string MaterialsLabel { get { return Translator.GetString ("Materials:"); } }

        [FormMemberMapping ("protocolDetailsMaterials")]
        public BindList<ProtocolDetail> ProtocolDetailsMaterials { get; private set; }

        [FormMemberMapping ("productsLabel")]
        public static string ProductsLabel { get { return Translator.GetString ("Products:"); } }

        [FormMemberMapping ("protocolDetailsProducts")]
        public BindList<ProtocolDetail> ProtocolDetailsProducts { get; private set; }

        #endregion

        #region Protocol footer info

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

        #endregion

        #endregion

        public ProductionProtocol ()
        {
            ProtocolDetailsProducts = new BindList<ProtocolDetail> ();
            ProtocolDetailsMaterials = new BindList<ProtocolDetail> ();
        }

        public ProductionProtocol (ComplexProduction production)
            : this ()
        {
            ProtocolDate = BusinessDomain.GetFormattedDate (production.Date);
            ProtocolNumber = production.FormattedOperationNumber;
            Note = production.Note;

            CompanyRecord company = CompanyRecord.GetDefault ();

            CompanyName = company.Name;
            CompanyNumber = company.Bulstat;
            CompanyCity = company.City;
            CompanyAddress = company.Address;
            CompanyTelephone = company.Telephone;
            CompanyLiablePerson = company.LiablePerson;

            Location = production.Location2;

            double vat = production.VAT;
            if (BusinessDomain.AppConfiguration.VATIncluded) {
                Total = Currency.ToString (production.Total - vat, PriceType.Purchase);
                Vat = Currency.ToString (vat, PriceType.Purchase);
                TotalPlusVat = production.Total;
            } else {
                Total = Currency.ToString (production.Total, PriceType.Purchase);
                Vat = Currency.ToString (vat, PriceType.Purchase);
                TotalPlusVat = production.Total + vat;
            }

            int i = 1;
            foreach (ComplexProductionDetail detail in production.Details)
                ProtocolDetailsMaterials.Add (new ProtocolDetail (i++, detail));

            i = 1;
            foreach (ComplexProductionDetail detail in production.DetailsProd)
                ProtocolDetailsProducts.Add (new ProtocolDetail (i++, detail));
        }
    }
}
