//
// ComplexRecipeDetail.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   07/02/2006
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

namespace Warehouse.Business.Operations
{
    public class ComplexRecipeDetail : PriceInOperationDetail
    {
        public ComplexRecipeDetail ()
        {
            sign = 0;
        }

        public ComplexRecipeDetail (ComplexProductionDetail detail)
        {
            itemId = detail.ItemId;
            itemGroupId = detail.ItemGroupId;
            itemName = detail.ItemName;
            itemName2 = detail.ItemName2;
            quantity = detail.Quantity;
            originalQuantity = detail.OriginalQuantity;
            sign = detail.Sign;
            priceIn = detail.PriceIn;
            priceOut = detail.PriceOut;
            vatIn = detail.VATIn;
            vatOut = detail.VATOut;
            discount = detail.Discount;
            originalDiscount = detail.OriginalDiscount;
            currencyId = detail.CurrencyId;
            lot = detail.Lot;
            note = detail.Note;
            originalNote = detail.OriginalNote;
            lotId = detail.LotId;
            mUnitName = detail.MUnitName;
            total = detail.Total;
        }

        public override void VATEvaluate ()
        {
        }
    }
}
