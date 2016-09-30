//
// PriceOutOperationDetail.cs
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
using Warehouse.Data;

namespace Warehouse.Business.Operations
{
    /// <summary>
    /// Represents an <see cref="OperationDetail"/> that is part of an operation that exports out of the warehouse.
    /// </summary>
    public abstract class PriceOutOperationDetail : OperationDetail
    {
        public override double OriginalPricePlusVAT
        {
            get { return OriginalPriceOutPlusVAT; }
        }

        public override double OriginalPrice
        {
            get { return OriginalPriceOut; }
        }

        protected override double Price
        {
            get { return priceOut; }
        }

        [DbColumn (DataField.OperationDetailPriceOut)]
        public override double PriceOutDB
        {
            get { return priceOut; }
            set
            {
                if (priceOut != value) {
                    priceOut = value;
                    originalPriceOut = -1;
                    OnPropertyChanged ("PriceOut");
                }
            }
        }

        public override double PriceOut
        {
            get
            {
                return Currency.Round (BusinessDomain.AppConfiguration.RoundedPrices ?
                    OriginalPriceOut - Currency.Round (OriginalPriceOut * discount / 100) :
                    OriginalPriceOut * (100 - discount) / 100);
            }
            set
            {
                if (priceOut == value)
                    return;

                priceOut = value;
                // resetting because we must recalculate the out-price each time otherwise fiscal rounding may cause errors
                originalPriceOut = -1;
                OnPropertyChanged ("PriceOut");
                OnPropertyChanged ("PriceOutPlusVAT");
            }
        }

        public override double VAT
        {
            get { return vatOut; }
        }

        public override double OriginalTotal
        {
            get { return Currency.Round (Quantity * OriginalPriceOut); }
        }

        public override PriceType TotalsPriceType
        {
            get { return PriceType.SaleTotal; }
        }

        public override double TotalPlusVAT
        {
            get
            {
                if (!baseTotalOnPricePlusVAT)
                    return Currency.Round (GetWithVAT (Total), TotalsPriceType);

                double originalTotal = OriginalPricePlusVAT * Quantity;
                return Currency.Round (originalTotal - Currency.Round (originalTotal * Discount / 100, TotalsPriceType), TotalsPriceType);
            }
        }

        public override void OriginalPriceEvaluate (double value)
        {
            OriginalPriceOutEvaluate (value);
        }

        public override void PriceEvaluate ()
        {
            PriceOutEvaluate ();
        }

        public override void PriceOutEvaluate ()
        {
            VatRateEvaluate ();
            PriceOut = OriginalPriceOut * (100 - discount) / 100;
            VATEvaluate ();
            TotalEvaluate ();
        }
    }
}
