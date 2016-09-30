//
// PurchaseDetail.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
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
    public class PurchaseDetail : PriceInOperationDetail
    {
        public PurchaseDetail ()
        {
            sign = 1;
        }

        public override void DiscountEvaluate (double discountPercent)
        {
            CalculateDiscount (discountPercent);
        }

        public override void DiscountValueEvaluate (double value)
        {
            CalculateValueDiscount (value);
        }

        public override void OriginalPriceInEvaluate (double value, Operation operation = null)
        {
            var oldValue = OriginalPriceIn;

            OriginalPriceIn = value;
            DiscountEvaluate (discount);

            OnAfterPriceInEvaluate (operation, oldValue);
        }

        public override void PriceInEvaluate ()
        {
            VatRateEvaluate ();
            PriceIn = OriginalPriceIn * (100 - discount) / 100;
            VATEvaluate ();
            TotalEvaluate ();
        }
    }
}