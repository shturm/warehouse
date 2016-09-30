//
// StockTakingDetail.cs
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
    public class StockTakingDetail : PriceOutOperationDetail
    {
        #region Private members

        private double enteredQuantity = 0d;
        private double expectedQuantity = 0d;
        private long enteredDetailId = -1;

        #endregion

        #region Public properties

        public long EnteredDetailId
        {
            get { return enteredDetailId; }
            set { enteredDetailId = value; }
        }

        public double OldPriceIn { get; set; }

        public double OldPriceOut { get; set; }

        public double EnteredQuantity
        {
            get { return enteredQuantity; }
            set
            {
                if (enteredQuantity != value) {
                    enteredQuantity = value;
                    Quantity = enteredQuantity - expectedQuantity;
                    OnPropertyChanged ("EnteredQuantity");
                }
            }
        }

        public double ExpectedQuantity
        {
            get { return expectedQuantity; }
            set
            {
                if (expectedQuantity != value) {
                    expectedQuantity = value;
                    Quantity = enteredQuantity - expectedQuantity;
                    OnPropertyChanged ("ExpectedQuantity");
                }
            }
        }

        public override bool UsesSavedLots
        {
            get { return true; }
        }

        #endregion

        public StockTakingDetail ()
        {
            sign = 1;
            lotId = BusinessDomain.AppConfiguration.ItemsManagementUseLots ? -1 : 1;
        }

        protected override void ResetQuantity ()
        {
            OriginalQuantity = 0;
            EnteredQuantity = 1;
        }

        public override void PriceOutEvaluate ()
        {
            VatRateEvaluate ();
            PriceOut = OriginalPriceOut;
            VATEvaluate ();
            TotalEvaluate ();
        }

        protected override bool ValidateQuantityValue ()
        {
            return detailId > 0 || enteredQuantity >= 0;
        }
    }
}