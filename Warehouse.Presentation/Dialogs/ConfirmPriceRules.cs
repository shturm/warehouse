//
// ConfirmPriceRules.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   11.26.2009
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
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Presentation.Dialogs
{
    public class ConfirmPriceRules<T> : DialogBase where T : OperationDetail, new ()
    {
        private readonly List<PriceRule> rulesToApply;

        #region Glade Widgets

#pragma warning disable 649

        [Widget] 
        private Dialog dlgConfirmPriceRules;
        [Widget]
        private Label lblAppliedPriceRules;
        [Widget]
        private TreeView treeviewPriceRules;
        [Widget]
        private Label lblChangesInOperation;
        [Widget] 
        private ScrolledWindow scwOperationDetails;
        [Widget] 
        private Button btnOK;
        [Widget] 
        private Button btnCancel;

#pragma warning restore 649

        #endregion

        private readonly ListView gridOperationDetails;

        public override Dialog DialogControl
        {
            get { return dlgConfirmPriceRules; }
        }

        private ConfirmPriceRules (IEnumerable<PriceRule> rulesToApply, Operation<T> operation, bool priceWithVAT = false)
        {
            this.rulesToApply = new List<PriceRule> (rulesToApply);
            Initialize ();

            gridOperationDetails = new ListView { Name = "gridOperationDetails", WidthRequest = 600, HeightRequest = 250 };
            ColumnController columnController = new ColumnController ();

            columnController.Add (new Column (Translator.GetString ("Item"), "ItemName", 1));

            CellTextQuantity cellQuantity = new CellTextQuantity ("Quantity");
            Column columnQuantity = new Column (Translator.GetString ("Qtty"), cellQuantity, 0.1) { MinWidth = 55 };
            columnController.Add (columnQuantity);

            CellTextDouble cellPrice = new CellTextCurrency (priceWithVAT ? "OriginalPriceOutPlusVAT" : "OriginalPriceOut");
            Column columnPrice = new Column (Translator.GetString ("Price"), cellPrice, 0.1) { MinWidth = 55 };
            columnController.Add (columnPrice);

            CellTextDouble cellDiscount = new CellTextDouble ("Discount");
            cellDiscount.FixedFaction = BusinessDomain.AppConfiguration.PercentPrecision;
            Column columnDiscount = new Column (Translator.GetString ("Discount %"), cellDiscount, 0.1);
            columnDiscount.Visible = BusinessDomain.AppConfiguration.AllowPercentDiscounts;
            columnDiscount.MinWidth = 100;
            columnController.Add (columnDiscount);

            BindingListModel<T> model = GetChangedDetails (operation);
            PriceType priceType = PriceType.SaleTotal;
            if (model.Count > 0)
                priceType = model [0].TotalsPriceType;

            CellTextDouble cellTotal = new CellTextCurrency (priceWithVAT ? "TotalPlusVAT" : "Total", priceType);
            Column columnTotal = new Column (Translator.GetString ("Amount"), cellTotal, 0.1) { MinWidth = 55 };
            columnController.Add (columnTotal);

            gridOperationDetails.ColumnController = columnController;

            gridOperationDetails.Model = model;
            scwOperationDetails.Add (gridOperationDetails);
            gridOperationDetails.Show ();

            CellRendererToggle cellRendererToggle = new CellRendererToggle { Activatable = true };
            cellRendererToggle.Toggled += (o, args) =>
            {
                TreeIter row;
                TreePath treePath = new TreePath (args.Path);
                treeviewPriceRules.Model.GetIter (out row, treePath);
                PriceRule priceRule = (PriceRule) treeviewPriceRules.Model.GetValue (row, 2);
                bool value = !(bool) treeviewPriceRules.Model.GetValue (row, 0);
                if (value)
                    if (treePath.Indices [0] <= this.rulesToApply.Count)
                        this.rulesToApply.Insert (treePath.Indices [0], priceRule);
                    else
                        this.rulesToApply.Add (priceRule);
                else
                    this.rulesToApply.Remove (priceRule);
                gridOperationDetails.Model = GetChangedDetails (operation);
                treeviewPriceRules.Model.SetValue (row, 0, value);
            };
            treeviewPriceRules.AppendColumn (string.Empty, cellRendererToggle, "active", 0);
            treeviewPriceRules.AppendColumn (Translator.GetString ("Price rule"), new CellRendererText (), "text", 1);
            treeviewPriceRules.AppendColumn (Translator.GetString ("Price rule"), new CellRendererText (), "text", 2).Visible = false;
            TreeStore treeStore = new TreeStore (typeof (bool), typeof (string), typeof (object));
            foreach (PriceRule priceRule in this.rulesToApply)
                treeStore.AppendValues (true, priceRule.Name, priceRule);
            treeviewPriceRules.Model = treeStore;
        }

        private BindingListModel<T> GetChangedDetails (Operation<T> operation)
        {
            Operation<T> clone = operation.Clone<Operation<T>, T> ();
            List<T> changedDetails = new List<T> ();
            List<T> originalDetails = new List<T> (clone.Details);
            List<T> originalAdditionalDetails = new List<T> (clone.AdditionalDetails);

            foreach (PriceRule priceRule in rulesToApply)
                if (priceRule.IsGlobal) {
                    if (priceRule.CheckApplicableToOperation (clone))
                        priceRule.Apply (clone);
                } else {
                    foreach (T detail in originalDetails)
                        if (priceRule.CheckApplicableToDetail (originalDetails, detail)) {
                            priceRule.Apply (detail, clone);
                            if ((!detail.OriginalPriceOut.IsEqualTo (detail.OriginalPriceOut) ||
                                !detail.DiscountValue.IsEqualTo (detail.DiscountValue)) &&
                                !changedDetails.Contains (detail))
                                changedDetails.Add (detail);
                        }

                    foreach (T detail in originalAdditionalDetails)
                        if (priceRule.CheckApplicableToDetail (originalAdditionalDetails, detail)) {
                            priceRule.Apply (detail, clone);
                            if ((!detail.OriginalPriceOut.IsEqualTo (detail.OriginalPriceOut) ||
                                !detail.DiscountValue.IsEqualTo (detail.DiscountValue)) &&
                                !changedDetails.Contains (detail))
                                changedDetails.Add (detail);
                        }
                }

            for (int i = operation.Details.Count; i < clone.Details.Count; i++)
                changedDetails.Add (clone.Details [i]);
            
            for (int i = operation.AdditionalDetails.Count; i < clone.AdditionalDetails.Count; i++)
                changedDetails.Add (clone.AdditionalDetails [i]);
            
            return new BindingListModel<T> (changedDetails);
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ConfirmPriceRules.glade", "dlgConfirmPriceRules");
            form.Autoconnect (this);

            dlgConfirmPriceRules.Icon = FormHelper.LoadImage ("Icons.PriceRules16.png").Pixbuf;
            dlgConfirmPriceRules.Title = Translator.GetString ("Confirm Price Rules");

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();

            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            lblAppliedPriceRules.SetText (Translator.GetString ("Applied price rules"));
            lblChangesInOperation.SetText (Translator.GetString ("Changes in the operation"));

            btnOK.SetChildLabelText (Translator.GetString ("Apply"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        #region Event handling

        protected void btnOK_Clicked (object o, EventArgs args)
        {
            dlgConfirmPriceRules.Respond (ResponseType.Ok);
        }

        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            rulesToApply.Clear ();
            dlgConfirmPriceRules.Respond (ResponseType.Cancel);
        }

        #endregion

        public static List<PriceRule> GetRulesToApply<TOperDetail> (IEnumerable<PriceRule> rules, Operation<TOperDetail> operation, bool priceWithVAT = false) where TOperDetail : OperationDetail, new ()
        {
            using (ConfirmPriceRules<TOperDetail> confirmPriceRules =
                new ConfirmPriceRules<TOperDetail> (rules, operation, priceWithVAT)) {
                confirmPriceRules.Run ();
                return confirmPriceRules.rulesToApply;
            }
        }
    }
}
