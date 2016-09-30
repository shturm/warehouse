//
// DocumentDetail.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   02.03.2011
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
using Warehouse.Business.Operations;

namespace Warehouse.Business.Entities
{
    public abstract class DocumentDetail
    {
        private readonly bool usePriceIn;
        private string number = string.Empty;
        private string itemCode = string.Empty;
        private string itemName = string.Empty;
        private string mUnit = string.Empty;
        private string quantity = string.Empty;
        private string price = string.Empty;
        private string total = string.Empty;

        [FormMemberMapping ("Number")]
        public string Number
        {
            get { return number; }
            set { number = value; }
        }

        [FormMemberMapping ("GoodsCode")]
        public string ItemCode
        {
            get { return itemCode; }
            set { itemCode = value; }
        }

        [FormMemberMapping ("GoodsName")]
        public string ItemName
        {
            get { return itemName; }
            set { itemName = value; }
        }

        [FormMemberMapping ("MUnit")]
        public string MUnit
        {
            get { return mUnit; }
            set { mUnit = value; }
        }

        [FormMemberMapping ("Quantity")]
        public string Quantity
        {
            get { return quantity; }
            set { quantity = value; }
        }

        [FormMemberMapping ("Price")]
        public string Price
        {
            get { return price; }
            set { price = value; }
        }

        [FormMemberMapping ("Discount")]
        public string Discount { get; set; }

        [FormMemberMapping ("DiscountValue")]
        public string DiscountValue { get; set; }

        [FormMemberMapping ("Currency")]
        public string Currency
        {
            get { return "BGN"; }
        }

        [FormMemberMapping ("Total")]
        public string Total
        {
            get { return total; }
            set { total = value; }
        }

        [FormMemberMapping ("Lot")]
        public string Lot { get; set; }

        [FormMemberMapping ("SerialNumber")]
        public string SerialNumber { get; set; }

        [FormMemberMapping ("ExpirationDate")]
        public string ExpirationDate { get; set; }

        [FormMemberMapping ("ProductionDate")]
        public string ProductionDate { get; set; }

        [FormMemberMapping ("LotLocation")]
        public string LotLocation { get; set; }

        [FormMemberMapping ("Note")]
        public string Note { get; set; }
        
        public PriceType TotalsPriceType
        {
            get { return usePriceIn ? PriceType.PurchaseTotal : PriceType.SaleTotal; }
        }

        protected DocumentDetail ()
        {
        }

        protected DocumentDetail (int detailNumber, OperationDetail detail, bool usePriceIn = true)
        {
            this.usePriceIn = usePriceIn;
            Number = Data.Number.ToEditString (detailNumber);

            Item item = Item.GetById (detail.ItemId);
            ItemCode = item.Code;
            ItemName = item.Name2;
            MUnit = detail.MUnitName;
            Quantity = Entities.Quantity.ToString (detail.Quantity);
            Discount = Percent.ToString (detail.Discount);
            DiscountValue = Entities.Currency.ToString (detail.DiscountValue);
            Price = Entities.Currency.ToString (usePriceIn ? detail.PriceIn : detail.PriceOut, usePriceIn ? PriceType.Purchase : PriceType.Sale);
            Total = Entities.Currency.ToString (detail.Total, usePriceIn ? PriceType.Purchase : PriceType.Sale);

            Lot = detail.Lot;
            SerialNumber = detail.SerialNumber;
            ExpirationDate = detail.ExpirationDate.HasValue ? BusinessDomain.GetFormattedDate (detail.ExpirationDate.Value) : String.Empty;
            ProductionDate = detail.ProductionDate.HasValue ? BusinessDomain.GetFormattedDate (detail.ProductionDate.Value) : String.Empty;
            LotLocation = detail.LotLocation;
            Note = detail.Note;
        }
    }
}
