//
// PriceInOperationDetail.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   31.05.2011
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

using Warehouse.Business.Entities;

namespace Warehouse.Business.Operations
{
    /// <summary>
    /// Represents an <see cref="OperationDetail"/> that is part of an operation that imports in the warehouse.
    /// </summary>
    public abstract class PriceInOperationDetail : OperationDetail
    {
        public override double OriginalPricePlusVAT
        {
            get
            {
                return GetUsePriceIn () ?
                    OriginalPriceInPlusVAT : OriginalPriceOutPlusVAT;
            }
        }

        public override double OriginalPrice
        {
            get
            {
                return GetUsePriceIn () ?
                    OriginalPriceIn : OriginalPriceOut;
            }
        }

        protected override double Price
        {
            get
            {
                return GetUsePriceIn () ?
                    priceIn : priceOut;
            }
        }

        public override double VAT
        {
            get
            {
                return GetUsePriceIn () ?
                    vatIn : vatOut;
            }
        }

        public override PriceType TotalsPriceType
        {
            get
            {
                return GetUsePriceIn () ?
                    PriceType.PurchaseTotal : PriceType.SaleTotal;
            }
        }

        public override void OriginalPriceEvaluate (double value)
        {
            if (GetUsePriceIn ())
                OriginalPriceInEvaluate (value);
            else
                OriginalPriceOutEvaluate (value);
        }

        public override void PriceEvaluate ()
        {
            if (GetUsePriceIn ())
                PriceInEvaluate ();
            else
                PriceOutEvaluate ();
        }

        protected virtual bool GetUsePriceIn ()
        {
            return true;
        }
    }
}
