//
// PreviewStockTaking.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/18/2006
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

using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component.ListView;

namespace Warehouse.Presentation.Preview
{
    public class PreviewStockTaking : PreviewOperation<StockTaking, StockTakingDetail>
    {
        public PreviewStockTaking ()
        {
            InitializeForm ();
        }

        #region Initialization steps

        private void InitializeForm ()
        {
            LocationVisible = true;
            UserVisible = true;

            ReInitializeForm (null);
        }

        public override void LoadOperation (object newOperation)
        {
            ReInitializeForm ((StockTaking) newOperation);
        }

        private void ReInitializeForm (StockTaking stockTaking)
        {
            operation = stockTaking;

            SetLocation ();
            SetUser ();
            SetOperationTotalAndEditMode ();

            InitializeGrid ();
            BindGrid ();
        }

        protected override void InitializeQuantityColumn (ColumnController cc)
        {
            CellTextQuantity ctq = new CellTextQuantity ("ExpectedQuantity");
            Column col = new Column (Translator.GetString ("Available"), ctq, 0.1) { MinWidth = 70 };
            cc.Add (col);

            ctq = new CellTextQuantity ("EnteredQuantity");
            colQuantity = new Column (Translator.GetString ("Qtty"), ctq, 0.1) { MinWidth = 70 };
            cc.Add (colQuantity);
        }

        protected override void InitializePurchasePriceColumn (ColumnController cc)
        {
            if (BusinessDomain.LoggedUser.HideItemsPurchasePrice)
                return;

            base.InitializePurchasePriceColumn (cc);
        }

        protected override void InitializeSalePriceColumn (ColumnController cc)
        {
            if (!BusinessDomain.LoggedUser.HideItemsPurchasePrice)
                return;

            base.InitializeSalePriceColumn (cc);
        }

        protected override void InitializeDiscountColumn (ColumnController cc)
        {
        }

        protected override void InitializeDiscountValueColumn (ColumnController cc)
        {
        }

        #endregion
    }
}
