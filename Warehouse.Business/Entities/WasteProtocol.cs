//
// WasteProtocol.cs
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
    public class WasteProtocol : Protocol
    {
        public override string Name
        {
            get { return Translator.GetString ("Protocol for Items Waste"); }
        }

        public override string FormName
        {
            get { return "WasteProtocol"; }
        }

        public override PriceType TotalsPriceType
        {
            get { return BusinessDomain.LoggedUser.HideItemsPurchasePrice ? PriceType.SaleTotal : PriceType.PurchaseTotal; }
        }

        public WasteProtocol ()
        {
            ProtocolSubTitle = Translator.GetString ("for items waste");
        }

        public WasteProtocol (Waste waste)
            : this ()
        {
            ProtocolDate = BusinessDomain.GetFormattedDate (waste.Date);
            ProtocolNumber = waste.FormattedOperationNumber;
            Note = waste.Note;

            CompanyRecord company = CompanyRecord.GetDefault ();

            CompanyName = company.Name;
            CompanyNumber = company.Bulstat;
            CompanyCity = company.City;
            CompanyAddress = company.Address;
            CompanyTelephone = company.Telephone;
            CompanyLiablePerson = company.LiablePerson;

            Location = waste.Location2;
            bool usePriceIn = !BusinessDomain.LoggedUser.HideItemsPurchasePrice;
            PriceType priceType = usePriceIn ? PriceType.PurchaseTotal : PriceType.SaleTotal;

            double vat = waste.VAT;
            if (BusinessDomain.AppConfiguration.VATIncluded) {
                Total = Currency.ToString (waste.Total - vat, priceType);
                Vat = Currency.ToString (vat, priceType);
                TotalPlusVat = waste.Total;
            } else {
                Total = Currency.ToString (waste.Total, priceType);
                Vat = Currency.ToString (vat, priceType);
                TotalPlusVat = waste.Total + vat;
            }

            int i = 1;
            foreach (WasteDetail detail in waste.Details)
                ProtocolDetails.Add (new ProtocolDetail (i++, detail, usePriceIn));

            TotalQuantity = Quantity.ToString (waste.Details.Sum (d => d.Quantity));
        }
    }
}
