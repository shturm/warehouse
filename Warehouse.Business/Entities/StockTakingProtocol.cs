//
// StockTakingProtocol.cs
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
using Warehouse.Business.Documenting;
using Warehouse.Business.Operations;
using Warehouse.Data.DataBinding;

namespace Warehouse.Business.Entities
{
    public class StockTakingProtocol : Protocol
	{
		[FormMemberMapping("detailExpectedQty", FormMemberType.Detail)]
		public static string DetailExpectedQty { get { return Translator.GetString ("Qty"); } }

		[FormMemberMapping("detailEnteredQty", FormMemberType.Detail)]
		public static string DetailEnteredQty { get { return Translator.GetString ("Actual Qty"); } }

        [FormMemberMapping ("detailHeaderQtyDiff", FormMemberType.Detail)]
        public static string DetailHeaderQtyDiff { get { return Translator.GetString ("Difference"); } }

		[FormMemberMapping("stockTakingProtocolDetails")]
		public BindList<StockTakingProtocolDetail> StockTakingProtocolDetails { get; private set; }

        public override string Name
        {
            get { return Translator.GetString ("Protocol for Items Stock-Taking"); }
        }

        public override string FormName
        {
            get { return "StockTakingProtocol"; }
        }

        public StockTakingProtocol ()
        {
            ProtocolSubTitle = Translator.GetString ("for items stock-taking");
			StockTakingProtocolDetails = new BindList<StockTakingProtocolDetail> ();
        }

        public StockTakingProtocol (StockTaking stockTaking)
            : this ()
        {
            ProtocolDate = BusinessDomain.GetFormattedDate (stockTaking.Date);
            ProtocolNumber = stockTaking.FormattedOperationNumber;
            Note = stockTaking.Note;

            CompanyRecord company = CompanyRecord.GetDefault ();

            CompanyName = company.Name;
            CompanyNumber = company.Bulstat;
            CompanyCity = company.City;
            CompanyAddress = company.Address;
            CompanyTelephone = company.Telephone;
            CompanyLiablePerson = company.LiablePerson;

            Location = stockTaking.Location2;

            double vat = stockTaking.VAT;
            if (BusinessDomain.AppConfiguration.VATIncluded) {
                Total = Currency.ToString (stockTaking.Total - vat);
                Vat = Currency.ToString (vat);
                TotalPlusVat = stockTaking.Total;
            } else {
                Total = Currency.ToString (stockTaking.Total);
                Vat = Currency.ToString (vat);
                TotalPlusVat = stockTaking.Total + vat;
            }

            int i = 1;
            foreach (StockTakingDetail detail in stockTaking.Details) {
            	StockTakingProtocolDetail stockTakingProtocolDetail = new StockTakingProtocolDetail (i++, detail, false);
            	stockTakingProtocolDetail.ExpectedQuantity = Quantity.ToString (detail.ExpectedQuantity);
            	stockTakingProtocolDetail.EnteredQuantity = Quantity.ToString (detail.EnteredQuantity);
            	StockTakingProtocolDetails.Add (stockTakingProtocolDetail);
            }

            TotalQuantity = Quantity.ToString (stockTaking.Details.Sum (d => d.EnteredQuantity));
        }
    }
}
