//
// EditNewPriceRule.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   11.05.2009
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Data.Calculator;
using Item = Warehouse.Business.Entities.Item;
using RowActivatedArgs = Gtk.RowActivatedArgs;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNewPriceRule : DialogBase
    {
        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEditNewPriceRule;
        [Widget]
        private Notebook notebookPriceRule;
        [Widget]
        private Label lblConditionsTab;
        [Widget]
        private TreeView treeViewConditions;
        [Widget]
        private TreeView treeViewActions;
        [Widget]
        private TreeView treeViewExceptions;
        [Widget]
        private Label lblActionsTab;
        [Widget]
        private Label lblExceptionsTab;
        [Widget]
        private Label lblOperationsTab;
        [Widget]
        private Label lblName;
        [Widget]
        private Entry txtName;
        [Widget]
        private TreeView treeViewOperations;
        [Widget]
        private CheckButton chkActive;
        [Widget]
        private TextView txvPreview;

        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

#pragma warning restore 649

        #endregion

        private PriceRule priceRule;
        private readonly int priority;

        public override Dialog DialogControl
        {
            get { return dlgEditNewPriceRule; }
        }

        public override string HelpFile
        {
            get { return "ChooseEditPriceRule.html"; }
        }

        public EditNewPriceRule (int priority)
        {
            this.priority = priority;
            Initialize ();
        }

        public EditNewPriceRule (PriceRule priceRule)
        {
            this.priceRule = priceRule;
            priority = priceRule.Priority;

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditNewPriceRule.glade", "dlgEditNewPriceRule");
            form.Autoconnect (this);

            dlgEditNewPriceRule.WidthRequest = 600;
            dlgEditNewPriceRule.Icon = FormHelper.LoadImage ("Icons.PriceRules16.png").Pixbuf;

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            CreateListItems (treeViewConditions, Translator.GetString ("Condition"), PriceRule.GetAllConditions (), (sender, e) => MarkCondition (e, treeViewConditions));
            CreateListItems (treeViewActions, Translator.GetString ("Action"), PriceRule.GetAllActions (), (sender, e) => MarkAction (e));
            CreateListItems (treeViewExceptions, Translator.GetString ("Exception"), PriceRule.GetAllExceptions (), (sender, e) => MarkCondition (e, treeViewExceptions));

            CreateListOperations ();

            treeViewConditions.RowActivated += TreeViewConditions_RowActivated;
            treeViewActions.RowActivated += TreeViewActions_RowActivated;
            treeViewExceptions.RowActivated += TreeViewExceptions_RowActivated;

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeEntries ();
        }

        private void MarkCondition (ToggledArgs e, TreeView treeView)
        {
            TreePath treePath = new TreePath (e.Path);
            TreeIter row;
            treeView.Model.GetIter (out row, treePath);

            PriceRule.ConditionType conditionType = (PriceRule.ConditionType) treeView.Model.GetValue (row, 4);
            bool isException = treeView == treeViewExceptions;
            PriceRuleCondition condition = priceRule.Conditions
                .Find (c => c.Type == conditionType && c.IsException == isException);

            bool isChecked = (bool) treeView.Model.GetValue (row, 0);

            if (isChecked) {
                condition.IsActive = false;
                treeView.Model.SetValue (row, 0, false);
                RefreshPreview ();
            } else {
                if (CheckNoDuplicateConditionType (conditionType, isException)) {
                    if (condition == null)
                        ManageCondition (treeView, treePath);
                    else {
                        condition.IsActive = true;
                        treeView.Model.SetValue (row, 0, true);
                        treeView.Model.SetValue (row, 2, condition.ToString ());
                        RefreshPreview ();
                    }
                }
            }
        }

        private void MarkAction (ToggledArgs e)
        {
            TreePath treePath = new TreePath (e.Path);
            TreeIter row;
            treeViewActions.Model.GetIter (out row, treePath);

            PriceRule.ActionType actionType = (PriceRule.ActionType) treeViewActions.Model.GetValue (row, 4);
            PriceRuleAction action = priceRule.Actions.Find (a => a.Type == actionType);

            bool isChecked = (bool) treeViewActions.Model.GetValue (row, 0);

            if (isChecked) {
                action.IsActive = false;
                treeViewActions.Model.SetValue (row, 0, false);
                RefreshPreview ();
            } else {
                if (action == null)
                    ManageAction (treeViewActions, treePath);
                else {
                    action.IsActive = true;
                    treeViewActions.Model.SetValue (row, 0, true);
                    treeViewActions.Model.SetValue (row, 2, action.ToString ());
                    RefreshPreview ();
                }
            }
        }

        private void TreeViewConditions_RowActivated (object o, RowActivatedArgs args)
        {
            ManageCondition (o, args.Path);
        }

        private void TreeViewActions_RowActivated (object o, RowActivatedArgs args)
        {
            ManageAction (o, args.Path);
        }

        private void TreeViewExceptions_RowActivated (object o, RowActivatedArgs args)
        {
            ManageCondition (o, args.Path);
        }

        private void ManageCondition (object o, TreePath treePath)
        {
            ListStore listStore = (ListStore) ((TreeView) o).Model;
            TreeIter row;
            listStore.GetIter (out row, treePath);

            PriceRule.ConditionType conditionType = (PriceRule.ConditionType) listStore.GetValue (row, 4);
            bool isException = o == treeViewExceptions;
            object entityId = null;
            PriceRuleCondition existingCondition =
                priceRule.Conditions.Find (c => c.Type == conditionType && c.IsException == isException);
            if (existingCondition != null && existingCondition.Values [0] != null)
                entityId = existingCondition.Values [0];

            long? groupId = null;
            PriceRuleCondition newCondition = null;

            switch (conditionType) {
                case PriceRule.ConditionType.Partner:
                    newCondition = ProcessChoiceCondition (new ChooseEditPartner (true, entityId as int?), conditionType, isException);
                    break;

                case PriceRule.ConditionType.PartnerGroup:
                    if (entityId != null) {
                        long id;
                        var g = PartnersGroup.Cache.GetByCode ((string) entityId);
                        if (g == null && long.TryParse ((string) entityId, out id))
                            g = PartnersGroup.Cache.GetById (id);

                        if (g != null)
                            groupId = g.Id;
                    }
                    newCondition = ProcessGroupCondition (conditionType, isException, new ChooseEditPartnersGroup (groupId));
                    break;

                case PriceRule.ConditionType.Object:
                    newCondition = ProcessChoiceCondition (new ChooseEditLocation (true, entityId as int?), conditionType, isException);
                    break;

                case PriceRule.ConditionType.ObjectGroup:
                    if (entityId != null) {
                        long id;
                        var g = LocationsGroup.Cache.GetByCode ((string) entityId);
                        if (g == null && long.TryParse ((string) entityId, out id))
                            g = LocationsGroup.Cache.GetById (id);

                        if (g != null)
                            groupId = g.Id;
                    }
                    newCondition = ProcessGroupCondition (conditionType, isException, new ChooseEditLocationsGroup (groupId));
                    break;

                case PriceRule.ConditionType.User:
                    newCondition = ProcessChoiceCondition (new ChooseEditUser (true, entityId as int?), conditionType, isException);
                    break;

                case PriceRule.ConditionType.UserGroup:
                    if (entityId != null) {
                        long id;
                        var g = UsersGroup.Cache.GetByCode ((string) entityId);
                        if (g == null && long.TryParse ((string) entityId, out id))
                            g = UsersGroup.Cache.GetById (id);
                        
                        if (g != null)
                            groupId = g.Id;
                    }
                    newCondition = ProcessGroupCondition (conditionType, isException, new ChooseEditUsersGroup (groupId));
                    break;

                case PriceRule.ConditionType.Good:
                case PriceRule.ConditionType.ContainsGood:
                    newCondition = ProcessChoiceCondition (new ChooseEditItem (true, entityId as int?), conditionType, isException);
                    break;

                case PriceRule.ConditionType.GoodGroup:
                case PriceRule.ConditionType.ContainsGGroup:
                    if (entityId != null) {
                        long id;
                        var g = ItemsGroup.Cache.GetByCode ((string) entityId);
                        if (g == null && long.TryParse ((string) entityId, out id))
                            g = ItemsGroup.Cache.GetById (id);

                        if (g != null)
                            groupId = g.Id;
                    }
                    newCondition = ProcessGroupCondition (conditionType, isException, new ChooseEditItemsGroup (groupId));
                    break;

                case PriceRule.ConditionType.Time:
                    ChooseTimeInterval chooseTimeInterval;
                    if (existingCondition != null && existingCondition.Values.Length >= 2 &&
                        (existingCondition.Values [0] != null || existingCondition.Values [1] != null)) {
                        DateTime? timeFrom = (DateTime?) existingCondition.Values [0];
                        DateTime? timeTo = (DateTime?) existingCondition.Values [1];
                        chooseTimeInterval = new ChooseTimeInterval (timeFrom, timeTo);
                    } else
                        chooseTimeInterval = new ChooseTimeInterval ();

                    using (chooseTimeInterval)
                        if (chooseTimeInterval.Run () == ResponseType.Ok)
                            newCondition = priceRule.AddCondition (conditionType, isException, chooseTimeInterval.TimeFrom,
                                chooseTimeInterval.TimeTo);
                    break;

                case PriceRule.ConditionType.Date:
                    ChooseDateInterval chooseDateInterval;
                    if (existingCondition != null && existingCondition.Values.Length >= 2 &&
                        (existingCondition.Values [0] != null || existingCondition.Values [1] != null)) {
                        DateTime? dateFrom = (DateTime?) existingCondition.Values [0];
                        DateTime? dateTo = (DateTime?) existingCondition.Values [1];
                        chooseDateInterval = new ChooseDateInterval (dateFrom, dateTo);
                    } else
                        chooseDateInterval = new ChooseDateInterval ();

                    using (chooseDateInterval)
                        if (chooseDateInterval.Run () == ResponseType.Ok)
                            newCondition = priceRule.AddCondition (conditionType, isException,
                                chooseDateInterval.DateFrom, chooseDateInterval.DateTo);
                    break;

                case PriceRule.ConditionType.DocumentSum:
                case PriceRule.ConditionType.TurnoverSum:
                case PriceRule.ConditionType.GoodQttySum:
                case PriceRule.ConditionType.PaymentSum:
                case PriceRule.ConditionType.UnpaidDocumentsSum:
                    ChooseNumericInterval chooseNumericInterval;
                    if (existingCondition != null && existingCondition.Values.Length >= 2 &&
                        (existingCondition.Values [0] != null || existingCondition.Values [1] != null)) {
                        double? valueFrom = (double?) existingCondition.Values [0];
                        double? valueTo = (double?) existingCondition.Values [1];
                        chooseNumericInterval = new ChooseNumericInterval (valueFrom, valueTo);
                    } else
                        chooseNumericInterval = new ChooseNumericInterval ();

                    chooseNumericInterval.DialogControl.Title = PriceRuleCondition.TypeToString (conditionType);

                    using (chooseNumericInterval)
                        if (chooseNumericInterval.Run () == ResponseType.Ok)
                            newCondition = priceRule.AddCondition (conditionType, isException,
                                chooseNumericInterval.ValueFrom, chooseNumericInterval.ValueTo);
                    break;

                case PriceRule.ConditionType.Weekdays:
                    ChooseWeekDays chooseWeekDays;
                    if (existingCondition != null && existingCondition.Values.Length >= 2 &&
                        existingCondition.Values [0] != null)
                        chooseWeekDays = new ChooseWeekDays ((IEnumerable<DayOfWeek>) existingCondition.Values [0]);
                    else
                        chooseWeekDays = new ChooseWeekDays ();

                    using (chooseWeekDays)
                        if (chooseWeekDays.Run () == ResponseType.Ok && chooseWeekDays.SelectedWeekDays.Count > 0)
                            newCondition = priceRule.AddCondition (conditionType, isException, chooseWeekDays.SelectedWeekDays);
                    break;

                case PriceRule.ConditionType.PaymentTypeUsed:
                    ChoosePaymentTypes choosePaymentTypes;
                    if (existingCondition != null && existingCondition.Values.Length >= 2 &&
                        existingCondition.Values [0] != null)
                        choosePaymentTypes = new ChoosePaymentTypes ((IEnumerable<BasePaymentType>) existingCondition.Values [0]);
                    else
                        choosePaymentTypes = new ChoosePaymentTypes ();

                    using (choosePaymentTypes)
                        if (choosePaymentTypes.Run () == ResponseType.Ok && choosePaymentTypes.Selected.Count > 0)
                            newCondition = priceRule.AddCondition (conditionType, isException, choosePaymentTypes.Selected);
                    break;

                case PriceRule.ConditionType.DatabaseUpdated:
                    if (existingCondition != null &&
                        existingCondition.Values.Length > 0 &&
                        existingCondition.Values [0] != null)
                        newCondition = existingCondition;
                    else
                        newCondition = priceRule.AddCondition (conditionType, isException, 30);
                    break;
            }

            if (newCondition != null)
                DisplayIndication (listStore, row, newCondition);
        }

        private void ManageAction (object o, TreePath treePath)
        {
            ListStore listStore = (ListStore) ((TreeView) o).Model;
            TreeIter row;
            listStore.GetIter (out row, treePath);

            PriceRule.ActionType actionType = (PriceRule.ActionType) listStore.GetValue (row, 4);
            PriceRuleAction existingAction = priceRule.Actions.Find (a => a.Type == actionType);
            string message;
            PriceRuleAction action;

            switch (actionType) {
                case PriceRule.ActionType.Stop:
                case PriceRule.ActionType.Exit:
                case PriceRule.ActionType.Message:
                    message = existingAction != null && existingAction.Values != null && existingAction.Values.Length > 0 ?
                        (string) existingAction.Values [0] : string.Empty;
                    using (ChooseMessage chooseMessage = new ChooseMessage (message))
                        if (chooseMessage.Run () == ResponseType.Ok) {
                            action = priceRule.AddAction (actionType, chooseMessage.Message);
                            DisplayAction (listStore, row, action);
                        }
                    break;

                case PriceRule.ActionType.Email:
                    string email = string.Empty;
                    message = string.Empty;
                    if (existingAction != null && existingAction.Values != null) {
                        if (existingAction.Values.Length > 0)
                            email = (string) existingAction.Values [0];

                        if (existingAction.Values.Length > 1)
                            message = (string) existingAction.Values [1];
                    }

                    using (ChooseMessage chooseMessage = new ChooseMessage (email, message))
                        if (chooseMessage.Run () == ResponseType.Ok) {
                            action = priceRule.AddAction (actionType, chooseMessage.Message, chooseMessage.Email);
                            DisplayAction (listStore, row, action);
                        }
                    break;

                case PriceRule.ActionType.AddGood:
                case PriceRule.ActionType.AddGlobalGood:
                    IEnumerable promotions = existingAction != null ? existingAction.Values : null;
                    using (ChooseItemsForPromotion chooseGoodsForPromotions = new ChooseItemsForPromotion (promotions))
                        if (chooseGoodsForPromotions.Run () == ResponseType.Ok) {
                            List<SaleDetail> nonEmptyDetails = chooseGoodsForPromotions.SelectedDetails.FindAll (d => d.ItemId > 0);
                            if (nonEmptyDetails.Count > 0) {
                                action = priceRule.AddAction (actionType, nonEmptyDetails.Cast<object> ().ToArray ());
                                DisplayAction (listStore, row, action);
                            }
                        }
                    break;

                case PriceRule.ActionType.Price:
                    ChoosePrice choosePrice;
                    if (existingAction != null && existingAction.Values != null) {
                        if (existingAction.Values.Length > 2) {
                            PriceGroup priceGroup = (PriceGroup) existingAction.Values [0];
                            OperatorType operatorType = (OperatorType) existingAction.Values [1];
                            double value = (double) existingAction.Values [2];
                            choosePrice = new ChoosePrice (priceGroup, operatorType, value);
                        } else
                            choosePrice = new ChoosePrice ((double) existingAction.Values [0]);
                    } else
                        choosePrice = new ChoosePrice ();

                    using (choosePrice)
                        if (choosePrice.Run () == ResponseType.Ok) {
                            object [] values = choosePrice.IsPriceGroupSelected ?
                                new object [] { choosePrice.PriceGroup, choosePrice.ArithmeticOperation, choosePrice.PriceGroupValue } :
                                new object [] { choosePrice.Value };
                            action = priceRule.AddAction (actionType, values);
                            DisplayAction (listStore, row, action);
                        }
                    break;

                case PriceRule.ActionType.Discount:
                    double initialDiscount = existingAction != null && existingAction.Values != null && existingAction.Values.Length > 0 ?
                        (double) existingAction.Values [0] : 0d;
                    using (AddDiscount addDiscount = new AddDiscount (true, initialDiscount))
                        if (addDiscount.Run () == ResponseType.Ok) {
                            action = priceRule.AddAction (actionType, addDiscount.PercentDiscount);
                            DisplayAction (listStore, row, action);
                        }
                    break;

                case PriceRule.ActionType.ServiceCharge:
                    Item serviceChargeItem;
                    double initialServiceCharge;
                    if (existingAction != null && existingAction.Values != null && existingAction.Values.Length > 1) {
                        serviceChargeItem = (Item) existingAction.Values [0];
                        initialServiceCharge = (double) existingAction.Values [1];
                    } else {
                        serviceChargeItem = null;
                        initialServiceCharge = 0d;
                    }

                    using (AddServiceCharge addServiceCharge = new AddServiceCharge (serviceChargeItem, initialServiceCharge)) {
                        if (addServiceCharge.Run () == ResponseType.Ok) {
                            action = priceRule.AddAction (actionType, addServiceCharge.Item, addServiceCharge.Amount);
                            DisplayAction (listStore, row, action);
                        }
                    }
                    break;

                case PriceRule.ActionType.Payment:
                case PriceRule.ActionType.AskAdvancePayment:
                    double advancePercentage = existingAction != null && existingAction.Values != null && existingAction.Values.Length > 0 ?
                        (double) existingAction.Values [0] : 0d;
                    using (AddAdvancePercentage addDiscount = new AddAdvancePercentage (advancePercentage))
                        if (addDiscount.Run () == ResponseType.Ok) {
                            action = priceRule.AddAction (actionType, addDiscount.PercentDiscount);
                            DisplayAction (listStore, row, action);
                        }
                    break;
            }
        }

        private void DisplayAction (TreeModel listStore, TreeIter row, PriceRuleAction action)
        {
            listStore.Foreach ((model, path, iter) =>
                {
                    model.SetValue (iter, 0, false);
                    model.SetValue (iter, 2, string.Empty);
                    return false;
                });
            DisplayIndication (listStore, row, action);
        }

        private void DisplayIndication (TreeModel listStore, TreeIter row, object priceRuleIndication)
        {
            listStore.SetValue (row, 0, true);
            listStore.SetValue (row, 2, priceRuleIndication.ToString ());

            RefreshPreview ();
        }

        private void RefreshPreview ()
        {
            txvPreview.Buffer.Text = priceRule.ToString ();
        }

        private PriceRuleCondition ProcessChoiceCondition<T, G> (ChooseEdit<T, G> chooseEdit, PriceRule.ConditionType conditionType, bool isException)
            where G : GroupBase<G>, new ()
            where T : class, IIdentifiableEntity
        {
            if (!CheckNoDuplicateConditionType (conditionType, isException))
                return null;

            using (chooseEdit)
                if (chooseEdit.Run () == ResponseType.Ok && chooseEdit.SelectedItems.Length > 0)
                    return priceRule.AddCondition (conditionType, isException, chooseEdit.SelectedItems [0].Id);

            return null;
        }

        private PriceRuleCondition ProcessGroupCondition<T> (PriceRule.ConditionType conditionType, bool isException, ChooseEditGroup<T> chooseEditGroup) where T : GroupBase<T>, new ()
        {
            if (!CheckNoDuplicateConditionType (conditionType, isException))
                return null;

            using (chooseEditGroup)
                if (chooseEditGroup.Run () == ResponseType.Ok && chooseEditGroup.GetSelectedGroup () != null)
                    return priceRule.AddCondition (conditionType, isException, chooseEditGroup.GetSelectedGroup ().Code);

            return null;
        }

        private bool CheckNoDuplicateConditionType (PriceRule.ConditionType conditionType, bool isException)
        {
            PriceRuleCondition existingCondition = priceRule.Conditions.Find (c => c.Type == conditionType &&
                c.IsException == !isException);

            if (existingCondition == null || !existingCondition.IsActive)
                return true;

            MessageError.ShowDialog (isException ?
                Translator.GetString ("There is already a condition of this type.") :
                Translator.GetString ("There is already an exception of this type."), ErrorSeverity.Error);

            return false;
        }

        private void InitializeEntries ()
        {
            if (priceRule == null)
                priceRule = new PriceRule ();
            else {
                txtName.Text = priceRule.Name;
                foreach (PriceRuleCondition condition in priceRule.Conditions) {
                    TreeView treeView = condition.IsException ? treeViewExceptions : treeViewConditions;
                    TreeIter row = MarkIndication ((int) condition.Type, treeView);
                    treeView.Model.SetValue (row, 2, condition.ToString ());
                    if (condition.Error)
                        treeView.Model.SetValue (row, 3, "red");
                }
                foreach (PriceRuleAction action in priceRule.Actions) {
                    TreeIter row = MarkIndication ((int) action.Type, treeViewActions);
                    treeViewActions.Model.SetValue (row, 2, action.ToString ());
                    if (action.Error)
                        treeViewActions.Model.SetValue (row, 3, "red");
                }
                foreach (OperationType operationType in priceRule.Operations) {
                    OperationType type = operationType;
                    treeViewOperations.Model.Foreach ((model, path, row) =>
                        {
                            if ((OperationType) model.GetValue (row, 2) == type)
                                model.SetValue (row, 0, true);
                            return false;
                        });
                }
                chkActive.Active = priceRule.Enabled;
                RefreshPreview ();
            }
        }

        private static TreeIter MarkIndication (int index, TreeView treeView)
        {
            TreeIter row;
            treeView.Model.GetIter (out row, new TreePath (new [] { index }));
            treeView.Model.SetValue (row, 0, true);
            return row;
        }

        private static void CreateListItems (TreeView treeView, string entityHeader, List<KeyValuePair<object, string>> items, ToggledHandler toggledHandler)
        {
            ListStore listStore = new ListStore (typeof (bool), typeof (string), typeof (string), typeof (string), typeof (object));
            foreach (KeyValuePair<object, string> item in items)
                listStore.AppendValues (false, item.Value, null, null, item.Key);

            CellRendererToggle cellRendererToggle = new CellRendererToggle { Activatable = true };
            cellRendererToggle.Toggled += toggledHandler;

            treeView.AppendColumn (string.Empty, cellRendererToggle, "active", 0);
            treeView.AppendColumn (entityHeader, new CellRendererText (), "text", 1);
            treeView.AppendColumn (Translator.GetString ("Value"), new CellRendererText (), "text", 2, "foreground", 3);
            treeView.Model = listStore;
        }

        private void CreateListOperations ()
        {
            ListStore listStore = new ListStore (typeof (bool), typeof (string), typeof (OperationType));
            foreach (OperationType operationType in new [] { OperationType.Purchase, OperationType.Sale, OperationType.Consignment, OperationType.SalesOrder })
                listStore.AppendValues (false, Translator.GetOperationTypeGlobalName (operationType), operationType);

            CellRendererToggle cellRendererToggle = new CellRendererToggle { Activatable = true };
            cellRendererToggle.Toggled += (sender, e) =>
                {
                    TreeIter row;
                    listStore.GetIter (out row, new TreePath (e.Path));
                    bool value = !(bool) listStore.GetValue (row, 0);
                    OperationType operationType = (OperationType) listStore.GetValue (row, 2);
                    if (value)
                        priceRule.Operations.Add (operationType);
                    else
                        priceRule.Operations.Remove (operationType);
                    listStore.SetValue (row, 0, value);
                    RefreshPreview ();
                };

            treeViewOperations.AppendColumn (string.Empty, cellRendererToggle, "active", 0);
            treeViewOperations.AppendColumn (string.Empty, new CellRendererText (), "text", 1);
            treeViewOperations.AppendColumn (string.Empty, new CellRendererText (), "text", 2).Visible = false;
            treeViewOperations.Model = listStore;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgEditNewPriceRule.Title = priceRule == null ?
                Translator.GetString ("New Price Rule") :
                Translator.GetString ("Edit Price Rule");

            lblName.SetText (Translator.GetString ("Name"));
            chkActive.Label = Translator.GetString ("Active");
            lblConditionsTab.SetText (Translator.GetString ("Conditions"));
            lblActionsTab.SetText (Translator.GetString ("Actions"));
            lblExceptionsTab.SetText (Translator.GetString ("Exceptions"));
            lblOperationsTab.SetText (Translator.GetString ("Operations"));

            btnOK.SetChildLabelText (Translator.GetString ("Save"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        public PriceRule GetPriceRule ()
        {
            priceRule.Name = txtName.Text.Trim ();
            priceRule.Enabled = chkActive.Active;
            priceRule.Priority = priority;
            return priceRule;
        }

        #region Event handling

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (!GetPriceRule ().Validate ((message, severity, code, state) =>
                {
                    MessageError.ShowDialog (message, severity);
                    return false;
                }, null)) {
                if (string.IsNullOrEmpty (priceRule.Name)) {
                    txtName.GrabFocus ();
                    return;
                }
                if (priceRule.Conditions.Count == 0) {
                    notebookPriceRule.Page = 0;
                    treeViewConditions.GrabFocus ();
                    return;
                }
                if (priceRule.Actions.Count == 0) {
                    notebookPriceRule.Page = 1;
                    treeViewActions.GrabFocus ();
                    return;
                }
                if (priceRule.Operations.Count == 0) {
                    notebookPriceRule.Page = 3;
                    treeViewOperations.GrabFocus ();
                }
                return;
            }
            priceRule.Conditions.RemoveAll (condition => !condition.IsActive);
            priceRule.Actions.RemoveAll (action => !action.IsActive);
            dlgEditNewPriceRule.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditNewPriceRule.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
