//
// PreviewTransfer.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   10/04/2007
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
using Warehouse.Business;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Component.ListView;

namespace Warehouse.Presentation.Preview
{
    public class PreviewTransfer : PreviewOperation<Transfer, TransferDetail>
    {
        public PreviewTransfer ()
        {
            InitializeForm ();
        }

        #region Initialization steps

        private void InitializeForm ()
        {
            SrcLocationVisible = true;
            DstLocationVisible = true;
            UserVisible = true;

            ReInitializeForm (null);
        }

        public override void LoadOperation (object newOperation)
        {
            ReInitializeForm ((Transfer) newOperation);
        }

        private void ReInitializeForm (Transfer transfer)
        {
            operation = transfer;

            if (operation != null)
                try {
                    operation.LoadDetails ();
                } catch (ArgumentException) {
                    operation = null;
                }

            SetSrcLocation ();
            SetDstLocation ();
            SetUser ();

            SetOperationTotalAndEditMode ();
            InitializeGrid ();
            BindGrid ();
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

        private void SetSrcLocation ()
        {
            lblSrcLocationValue.SetText (operation == null ? string.Empty : operation.SourceLocation);
        }

        private void SetDstLocation ()
        {
            lblDstLocationValue.SetText (operation == null ? string.Empty : operation.TargetLocation);
        }
    }
}
