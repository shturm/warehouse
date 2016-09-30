//
// PreviewInvoice.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   03/26/2006
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
    public class PreviewInvoice : PreviewOperation<Sale, SaleDetail>
    {
        public PreviewInvoice ()
        {
            InitializeForm ();
        }

        #region Initialization steps

        private void InitializeForm ()
        {
            PartnerVisible = true;
            LocationVisible = true;
            UserVisible = true;

            ReInitializeForm (null);
        }

        public override void LoadOperation (object newOperation)
        {
            ReInitializeForm (newOperation != null ?
                ((Invoice) newOperation).GetSale () : null);
        }

        private void ReInitializeForm (Sale sale)
        {
            operation = sale;

            SetPartner ();
            SetLocation ();
            SetUser ();

            SetOperationTotalAndEditMode ();
            InitializeGrid ();
            BindGrid ();
        }

        protected override void InitializePurchasePriceColumn (ColumnController cc)
        {
        }

        protected override void InitializeSalePriceColumn (ColumnController cc)
        {
            CellTextCurrency ctf = new CellTextCurrency ("OriginalPriceOut");
            colSalePrice = new Column (Translator.GetString ("Price"), ctf, 0.1) { MinWidth = 70 };
            cc.Add (colSalePrice);
        }

        #endregion
    }
}
