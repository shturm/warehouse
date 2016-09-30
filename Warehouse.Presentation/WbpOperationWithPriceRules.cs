// 
// WbpOperationWithPriceRules.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at yahoo dot com>
// 
// Created:
//    04.09.2012
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
using System.Collections.Generic;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Data;
using Warehouse.Data.Model;
using Warehouse.Presentation.Dialogs;
using Item = Warehouse.Business.Entities.Item;

namespace Warehouse.Presentation
{
    public abstract class WbpOperationWithPriceRules<TOper, TOperDetail> : WbpOperationBase<TOper, TOperDetail>
        where TOper : Operation<TOperDetail>
        where TOperDetail : OperationDetail, new ()
    {
        private bool initialPartner = true;
        private bool initialLocation = true;
        private bool initialUser = true;
        private bool initialDate = true;

        protected virtual void ReInitializeForm (long? operationId)
        {
            initialPartner = true;
            initialLocation = true;
            initialUser = true;
            initialDate = true;

            if (grid != null)
                grid.CancelCellEdit ();

            if (secondGrid != null)
                secondGrid.CancelCellEdit ();
        }

        private IManageableListModel<PriceRule> priceRules;

        protected IManageableListModel<PriceRule> PriceRules
        {
            get { return priceRules ?? (priceRules = PriceRule.GetAll ()); }
        }

        protected override void SetPartner (Partner p)
        {
            bool partnerChanged = (!initialPartner && partner == null && p != null) ||
                (partner != null && p != null && partner.Id != p.Id);
            initialPartner = false;
            base.SetPartner (p);

            if (!editMode && partnerChanged)
                PriceRule.ReapplyOnPartnerChanged (operation, (s, action) =>
                    {
                        using (Message message = new Message (Translator.GetString ("Price Rules"), string.Empty, s, "Icons.Question32.png")) {
                            message.Buttons = MessageButtons.YesNo;
                            if (message.Run () == ResponseType.Yes)
                                action ();
                        }
                    }, PriceRules);
        }

        protected override void SetLocation (Location newLocation)
        {
            bool locationChanged = (!initialLocation && location == null && newLocation != null) ||
                (location != null && newLocation != null && location.Id != newLocation.Id);
            initialLocation = false;
            base.SetLocation (newLocation);

            if (!editMode && locationChanged)
                PriceRule.ReapplyOnLocationChanged (operation, (s, action) =>
                    {
                        using (Message message = new Message (Translator.GetString ("Price Rules"), string.Empty, s, "Icons.Question32.png")) {
                            message.Buttons = MessageButtons.YesNo;
                            if (message.Run () == ResponseType.Yes)
                                action ();
                        }
                    }, PriceRules);
        }

        protected override void SetUser (User u)
        {
            bool userChanged = (!initialUser && user == null && u != null) ||
                (user != null && u != null && user.Id != u.Id);
            initialUser = false;
            base.SetUser (u);

            if (!editMode && userChanged)
                PriceRule.ReapplyOnUserChanged (operation, (s, action) =>
                    {
                        using (Message message = new Message (Translator.GetString ("Price Rules"), string.Empty, s, "Icons.Question32.png")) {
                            message.Buttons = MessageButtons.YesNo;
                            if (message.Run () == ResponseType.Yes)
                                action ();
                        }
                    }, PriceRules);
        }

        protected override void SetDate (DateTime d)
        {
            bool dateChanged = !initialDate && date != d;
            initialDate = false;
            base.SetDate (d);

            if (!editMode && dateChanged)
                PriceRule.ReapplyOnDateChanged (operation, (s, action) =>
                    {
                        using (Message message = new Message (Translator.GetString ("Price Rules"), string.Empty, s, "Icons.Question32.png")) {
                            message.Buttons = MessageButtons.YesNo;
                            if (message.Run () == ResponseType.Yes)
                                action ();
                        }
                    }, PriceRules);
        }

        protected override bool ItemColumnEvaluate (int row, Item item, bool updatePrice)
        {
            TOperDetail detail = operation.Details [row];
            bool itemChanged = item != null && detail.ItemId != item.Id;
            if (!base.ItemColumnEvaluate (row, item, updatePrice))
                return false;

            // only apply if the Item is being edited, that is, the quantity has been already entered
            TryApplyRulesOnDetail (detail, itemChanged && detail.AppliedPriceRules != PriceRule.AppliedActions.None);
            return true;
        }

        protected override bool QtyColumnEvaluate (ref int row, double qtyValue)
        {
            TOperDetail detail = operation.Details [row];
            bool qtyChanged = !detail.Quantity.IsEqualTo (qtyValue);
            bool result = base.QtyColumnEvaluate (ref row, qtyValue);
            // the quantity by default is one as is the cell value so if no difference check if this is the first time
            TryApplyRulesOnDetail (detail, qtyChanged || detail.AppliedPriceRules == PriceRule.AppliedActions.None);
            return result;
        }

        protected override bool SalePriceColumnEvaluate (int row, string price)
        {
            double newPrice = Currency.ParseExpression (price);
            TOperDetail detail = operation.Details [row];
            if (!detail.OriginalPriceOut.IsEqualTo (newPrice))
                detail.ManualSalePrice = true;
            return base.SalePriceColumnEvaluate (row, price);
        }

        protected override bool PurchaseValueColumnEvaluate (int row, string price)
        {
            double newPrice = Currency.ParseExpression (price);
            TOperDetail detail = operation.Details [row];
            if (!detail.OriginalPriceIn.IsEqualTo (newPrice))
                detail.ManualPurchasePrice = true;
            return base.PurchaseValueColumnEvaluate (row, price);
        }

        protected override void DiscountColumnEvaluate (int row, double discountPercent)
        {
            TOperDetail detail = operation.Details [row];
            if (!detail.Discount.IsEqualTo (discountPercent))
                detail.ManualDiscount = true;
            base.DiscountColumnEvaluate (row, discountPercent);
        }

        protected override void DiscountValueColumnEvaluate (int row, double discountValue)
        {
            TOperDetail detail = operation.Details [row];
            if (!detail.DiscountValue.IsEqualTo (discountValue))
                detail.ManualDiscount = true;
            base.DiscountValueColumnEvaluate (row, discountValue);
        }

        private void TryApplyRulesOnDetail (TOperDetail detail, bool changed)
        {
            if (!editMode && changed && detail.ApplyPriceRules (PriceRules, operation, partnerPriceGroup, locationPriceGroup))
                PresentationDomain.MainForm.ShowNotification (Translator.GetString ("Price rule applied"));
        }

        protected bool TryApplyRules (bool priceWithVAT = false)
        {
            return editMode || PriceRule.ApplyBeforeOperationSaved (operation,
                (rules, details) => ConfirmPriceRules<TOperDetail>.GetRulesToApply (rules, operation, priceWithVAT), true, PriceRules);
        }

        protected virtual void ApplyFinalPriceRules ()
        {
            if (editMode)
                return;

            PriceRule.ApplyOnOperationSaved (operation, PriceRules);
            PriceRule.ApplyAfterOperationSaved (operation, PriceRules);
        }
    }
}
