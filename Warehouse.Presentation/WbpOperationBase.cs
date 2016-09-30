//
// WbpOperationBase.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/25/2006
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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Gdk;
using Glade;
using GLib;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Component.WorkBook;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Data.Model;
using Warehouse.Presentation.Dialogs;
using Warehouse.Presentation.Suggestion;
using Warehouse.Presentation.Widgets;
using Item = Warehouse.Business.Entities.Item;
using Key = Gdk.Key;

namespace Warehouse.Presentation
{
    public abstract class WbpOperationBase<TOper, TOperDetail> : WbpOperationBase
        where TOperDetail : OperationDetail, new ()
        where TOper : Operation<TOperDetail>
    {
        #region Fields

        protected ListView grid;
        protected ListView secondGrid;
        private GridNavigator gridNavigator;
        private GridNavigator secondGridNavigator;
        private readonly PangoStyle insufficiencyHighlight = new PangoStyle { Color = Colors.Red };
        private readonly Dictionary<long, bool> canUseLocationIdWarning = new Dictionary<long, bool> ();
        private bool pendingCompletionAcknowledged;

        protected TOper operation;

        #endregion

        #region Properties

        public override Operation Operation
        {
            get { return operation; }
        }

        protected bool GridDescriptionVisible
        {
            set { lblGridDescription.Visible = value; }
        }

        protected string GridDescription
        {
            set { lblGridDescription.SetText (value); }
        }

        protected bool SecondGridDescriptionVisible
        {
            set { lblSecondGridDescription.Visible = value; }
        }

        protected string SecondGridDescription
        {
            set { lblSecondGridDescription.SetText (value); }
        }

        protected bool SecondGridVisible
        {
            set { algSecondGrid.Visible = value; }
        }

        protected GridNavigator GridNavigator
        {
            get
            {
                return gridNavigator ?? (gridNavigator = new GridNavigator (grid, EditGridField, GridColumnEditOver, GridColumnEditBelow));
            }
        }

        private GridNavigator SecondGridNavigator
        {
            get
            {
                return secondGridNavigator ?? (secondGridNavigator = new GridNavigator (secondGrid, EditSecondGridField, SecondGridColumnEditOver, SecondGridColumnEditBelow));
            }
        }

        protected virtual bool CanSaveDrafts
        {
            get { return true; }
        }

        #endregion

        #region Initialization steps

        protected override void InitializeEntries ()
        {
            // Setup the partner entry
            if (Partner.TryGetLocked (out partner))
                PartnerSensitive = false;
            else {
                txtPartner.KeyPressEvent += txtPartner_KeyPress;
                txtPartner.Focused += txtPartner_Focused;
                txtPartner.ButtonPressEvent += txtPartner_ButtonPressEvent;
                txtPartner.Changed += txtPartner_Changed;
                btnPickPartner.Clicked += btnPickPartner_Clicked;
            }

            // Setup the location and source location entry
            if (Location.TryGetLocked (ref location)) {
                LocationSensitive = false;
                SrcLocationSensitive = false;
            } else {
                txtLocation.KeyPressEvent += txtLocation_KeyPress;
                txtLocation.Focused += txtLocation_Focused;
                txtLocation.ButtonPressEvent += txtLocation_ButtonPressEvent;
                txtLocation.Changed += txtLocation_Changed;
                btnPickLocation.Clicked += btnPickLocation_Clicked;

                txtSrcLocation.KeyPressEvent += txtSrcLocation_KeyPress;
                txtSrcLocation.Focused += txtSrcLocation_Focused;
                txtSrcLocation.ButtonPressEvent += txtSrcLocation_ButtonPressEvent;
                txtSrcLocation.Changed += txtSrcLocation_Changed;
                btnPickSrcLocation.Clicked += btnPickSrcLocation_Clicked;
            }
            srcLocation = location;

            // Setup the destination location entry
            txtDstLocation.KeyPressEvent += txtDstLocation_KeyPress;
            txtDstLocation.Focused += txtDstLocation_Focused;
            txtDstLocation.ButtonPressEvent += txtDstLocation_ButtonPressEvent;
            txtDstLocation.Changed += txtDstLocation_Changed;
            btnPickDstLocation.Clicked += btnPickDstLocation_Clicked;

            if (BusinessDomain.LoggedUser.UserLevel == UserAccessLevel.Operator) {
                UserSensitive = false;
                DateSensitive = false;
            } else {
                // Setup the user entry
                txtUser.KeyPressEvent += txtUser_KeyPress;
                txtUser.Focused += txtUser_Focused;
                txtUser.ButtonPressEvent += txtUser_ButtonPressEvent;
                txtUser.Changed += txtUser_Changed;
                btnPickUser.Clicked += btnPickUser_Clicked;

                // Setup the date entry
                txtDate.KeyPressEvent += txtDate_KeyPress;
                txtDate.Focused += txtDate_Focused;
                txtDate.ButtonPressEvent += txtDate_ButtonPressEvent;
                txtDate.Changed += txtDate_Changed;
                btnPickDate.Clicked += btnPickDate_Clicked;
            }

            btnSave.Clicked += btnSave_Clicked;
            btnClear.Clicked += btnClear_Clicked;
            btnClose.Clicked += btnClose_Clicked;
            btnAddDiscount.Clicked += btnAddDiscount_Clicked;
            btnAddRemoveVAT.Clicked += btnAddRemoveVat_Clicked;
            btnImport.Clicked += btnImport_Clicked;
            btnSearch.Clicked += btnSearch_Clicked;
            txtSearch.Changed += txtSearch_Changed;
            txtSearch.KeyPressEvent += txtSearch_KeyPressEvent;
            btnSearchUp.Clicked += btnSearchUp_Clicked;
            btnSearchDown.Clicked += btnSearchDown_Clicked;

            vatIncludedInPrices = BusinessDomain.AppConfiguration.VATIncluded;
            UpdateAddRemoveVatText ();
        }

        protected void SetPartnerName ()
        {
            User loggedUser = BusinessDomain.LoggedUser;
            if (loggedUser.LockedPartnerId <= 0 &&
                loggedUser.DefaultPartnerId > 0 &&
                (partner == null || loggedUser.DefaultPartnerId != partner.Id))
                partner = Partner.GetById (loggedUser.DefaultPartnerId);

            if (partner == null)
                txtPartner.Text = string.Empty;
            else
                SetPartner (partner);
        }

        protected void SetLocationName ()
        {
            if (location == null) {
                LazyListModel<Location> allLocations = Location.GetAll ();
                if (allLocations.Count == 1)
                    SetLocation (allLocations [0]);
                else
                    txtLocation.Text = string.Empty;
            } else
                SetLocation (location);
        }

        protected void FocusFirstEntry ()
        {
            if (!grid.IsMapped) {
                grid.SizeAllocated -= GridOnMapped;
                grid.SizeAllocated += GridOnMapped;
                return;
            }

            if (hboPartner.IsMapped && hboPartner.Visible && txtPartner.Sensitive)
                PartnerFocus ();
            else if (hboLocation.IsMapped && hboLocation.Visible && txtLocation.Sensitive)
                LocationFocus ();
            else if (hboSrcLocation.IsMapped && hboSrcLocation.Visible && txtSrcLocation.Sensitive)
                SourceLocationFocus ();
            else if (hboDstLocation.IsMapped && hboDstLocation.Visible && txtDstLocation.Sensitive)
                DestinationLocationFocus ();
            else if (hboUser.IsMapped && hboUser.Visible && txtUser.Sensitive)
                UserFocus ();
            else {
                Column column = grid.ColumnController.FirstOrDefault (c => c.ListCell.IsEditable);
                if (column != null)
                    EditGridField (0, column.Index);
                else
                    InitializeFormStrings ();
            }
        }

        private void GridOnMapped (object sender, EventArgs eventArgs)
        {
            GLib.Timeout.Add (1, () =>
                {
                    FocusFirstEntry ();
                    return false;
                });

            grid.SizeAllocated -= GridOnMapped;
        }

        public override void LoadOperation (Operation oper)
        {
            txtPartner.Text = oper.PartnerName;
            PartnerEvaluate ();

            txtLocation.Text = oper.Location;
            LocationEvaluate ();

            txtUser.Text = oper.UserName;
            UserEvaluate ();

            SetDate (oper.Date);
            SetNote (oper.DetailsBase);
            LoadOperationDetails (oper);
            SetOperationTotalAndNewMode ();
            UpdateOperationTotal ();

            InitializeStatistics ();
            UpdateStatistics ();
        }

        protected virtual void LoadOperationDetails (Operation oper)
        {
            operation.Details.Clear ();
            TOperDetail lastDet = null;
            foreach (OperationDetail detail in oper.DetailsBase) {
                lastDet = detail.Clone<TOperDetail> (detail is StockTakingDetail);
                lastDet.DetailId = -1;
                if (lastDet.UsesSavedLots) {
                    lastDet.LotId = 1;
                    lastDet.Lot = "NA";
                    lastDet.SerialNumber = string.Empty;
                    lastDet.ExpirationDate = null;
                    lastDet.ProductionDate = null;
                    lastDet.LotLocation = string.Empty;
                }
                lastDet.PriceEvaluate ();
                operation.Details.Add (lastDet);
            }

            if (lastDet == null ||
                !lastDet.UsesSavedLots ||
                !BusinessDomain.AppConfiguration.ItemsManagementUseLots)
                return;

            if (BusinessDomain.AppConfiguration.ItemsManagementType == ItemsManagementType.Choice)
                return;

            OperationDetailsValidate (false);
        }

        protected void LoadSecondaryOperationDetails (Operation oper)
        {
            operation.AdditionalDetails.Clear ();
            TOperDetail lastDet = null;
            foreach (OperationDetail detail in oper.AdditionalDetailsBase) {
                lastDet = detail.Clone<TOperDetail> (detail is StockTakingDetail);
                lastDet.DetailId = -1;
                if (lastDet.UsesSavedLots) {
                    lastDet.LotId = 1;
                    lastDet.Lot = "NA";
                    lastDet.SerialNumber = string.Empty;
                    lastDet.ExpirationDate = null;
                    lastDet.ProductionDate = null;
                    lastDet.LotLocation = string.Empty;
                }
                lastDet.PriceEvaluate ();
                operation.AdditionalDetails.Add (lastDet);
            }

            if (lastDet == null ||
                !lastDet.UsesSavedLots ||
                !BusinessDomain.AppConfiguration.ItemsManagementUseLots)
                return;

            if (BusinessDomain.AppConfiguration.ItemsManagementType == ItemsManagementType.Choice)
                return;

            OperationDetailsValidate (false);
        }

        #endregion

        protected override void InitializeHelpStrings ()
        {
            lblOperationTitle.SetText (PageTitle);

            string selectKey = KeyShortcuts.KeyToString (KeyShortcuts.MapRawKeys (KeyShortcuts.ChooseKey));
            switch (cursorAtField) {
                case FormFields.Partner:
                    lblHelp.SetText (string.Format (Translator.GetString ("Enter partner name or select a partner by pressing {0}. Press Enter to continue."), selectKey));
                    break;
                case FormFields.Location:
                case FormFields.SourceLocation:
                case FormFields.DestinationLocation:
                    lblHelp.SetText (string.Format (Translator.GetString ("Enter location name or select a location by pressing {0}. Press Enter to continue."), selectKey));
                    break;
                case FormFields.User:
                    lblHelp.SetText (string.Format (Translator.GetString ("Enter user name or select an user by pressing {0}. Press Enter to continue."), selectKey));
                    break;
                case FormFields.Date:
                    lblHelp.SetText (string.Format (Translator.GetString ("Enter date manually or select a date by pressing {0}. Press Enter to continue."), selectKey));
                    break;
                case FormFields.GridDetails:
                case FormFields.SecondGridDetails:
                    string text = null;
                    if ((colItem != null && cursorAtColumn == colItem.Index) ||
                        (colSecondItem != null && cursorAtColumn == colSecondItem.Index)) {
                        AccelKey accelKey = KeyShortcuts.LookupEntry (btnSave.Name);
                        string saveKey = KeyShortcuts.KeyToString (KeyShortcuts.MapRawKeys (accelKey));
                        text = Translator.GetString ("Enter item code or name or select an item by pressing {0}. Press Enter to continue or {1} to finish the document.");
                        text = string.Format (text, selectKey, saveKey);
                    }

                    if ((colQuantity != null && cursorAtColumn == colQuantity.Index) ||
                        (colSecondQuantity != null && cursorAtColumn == colSecondQuantity.Index))
                        text = Translator.GetString ("Enter item quantity. Press Enter to continue.");

                    if ((colPurchasePrice != null && cursorAtColumn == colPurchasePrice.Index) ||
                        (colSecondPurchaseValue != null && cursorAtColumn == colSecondPurchaseValue.Index))
                        text = Translator.GetString ("Enter purchase price for the item. Press Enter to continue.");

                    if ((colSalePrice != null && cursorAtColumn == colSalePrice.Index) ||
                        (colSecondSalePrice != null && cursorAtColumn == colSecondSalePrice.Index))
                        text = Translator.GetString ("Enter sale price for the item. Press Enter to continue.");

                    if (!string.IsNullOrEmpty (text)) {
                        string deleteRow = Translator.GetString ("To delete a row - {0}.");
                        AccelKey deleteKey = KeyShortcuts.DeleteKey;
                        if (PlatformHelper.Platform != PlatformTypes.MacOSX)
                            deleteKey.AccelMods = ModifierType.ControlMask;
                        lblHelp.SetText (string.Format ("{0} {1}", text,
                            string.Format (deleteRow, KeyShortcuts.KeyToString (KeyShortcuts.MapRawKeys (deleteKey)))));
                    }

                    break;
            }
        }

        protected virtual void BindGrid ()
        {
            BindGrid (grid, operation.Details);

            InitializeStatistics ();
            UpdateStatistics ();

            FocusFirstEntry ();
        }

        protected void BindGrid (ListView listView, object source)
        {
            if (listView == null)
                return;

            if (listView.Model != null)
                listView.Model.ListChanged -= OperationModel_ListChanged;

            listView.Model = new BindingListModel (source);
            listView.Model.ListChanged += OperationModel_ListChanged;
        }

        private void OperationModel_ListChanged (object sender, ListChangedEventArgs e)
        {
            UpdateOperationTotal ();
            UpdateStatistics ();
        }

        protected virtual void EditFormField (FormFields fld)
        {
            grid.DisableEdit = true;
            if (secondGrid != null)
                secondGrid.DisableEdit = true;
            cursorAtField = fld;
            InitializeHelpStrings ();
        }

        protected virtual bool EditGridField (int row, int col)
        {
            if (secondGrid != null)
                secondGrid.DisableEdit = true;
            grid.DisableEdit = false;
            if (!grid.BeginCellEdit (new CellEventArgs (col, row)))
                return false;

            grid.AllignToV (row);
            cursorAtColumn = col;
            cursorAtField = FormFields.GridDetails;
            InitializeHelpStrings ();
            return true;
        }

        protected virtual bool EditSecondGridField (int row, int col)
        {
            grid.DisableEdit = true;
            secondGrid.DisableEdit = false;
            if (!secondGrid.BeginCellEdit (new CellEventArgs (col, row)))
                return false;

            secondGrid.AllignToV (row);
            cursorAtColumn = col;
            cursorAtField = FormFields.SecondGridDetails;
            InitializeHelpStrings ();
            return true;
        }

        protected PriceGroup GetOperationPriceGroup ()
        {
            return Operation.GetPriceGroup (partnerPriceGroup, locationPriceGroup);
        }

        protected void SetOperationTotalAndEditMode ()
        {
            SetOperationTotal (operation);
            editMode = true;
            pageTitle = null;
            if (BusinessDomain.AppConfiguration.DocumentNumbersPerLocation)
                LocationSensitive = false;

            chkTax.Active = true;
            chkTax.Sensitive = false;
        }

        protected void SetOperationTotalAndNewMode ()
        {
            OperationTotalHide ();
            editMode = false;
            pageTitle = null;
            chkTax.Active = true;
            pendingCompletionAcknowledged = false;
        }

        #region Totals handling

        protected virtual void OperationTotalHide ()
        {
            lblAmountValue.Hide ();
            lblAmount.Hide ();
            lblTaxValue.Hide ();
            lblTax.Hide ();
            chkTax.Hide ();
            lblTotalValue.Hide ();
            lblTotal.Hide ();
        }

        protected virtual void SetOperationTotal (Operation oper)
        {
            double vat = oper.VAT;
            double totalPlusVat = oper.TotalPlusVAT;
            double total = totalPlusVat - vat;
            bool hasVat = !vat.IsZero () || !chkTax.Active;
            bool hasTotal = !total.IsZero ();

            if (hasTotal && hasVat) {
                lblAmountValue.SetText (Currency.ToString (total, oper.TotalsPriceType));
                lblAmountValue.Show ();
                lblAmount.Show ();
            } else {
                lblAmountValue.Hide ();
                lblAmount.Hide ();
            }

            if (hasVat) {
                lblTaxValue.SetText (Currency.ToString (vat, oper.TotalsPriceType));
                lblTaxValue.Show ();
                lblTax.Show ();
                chkTax.Show ();
            } else {
                lblTaxValue.Hide ();
                lblTax.Hide ();
                chkTax.Hide ();
            }

            if (hasTotal || hasVat) {
                lblTotalValue.SetText (Currency.ToString (totalPlusVat, oper.TotalsPriceType));
                lblTotalValue.Show ();
                lblTotal.Show ();
            } else {
                lblTotalValue.Hide ();
                lblTotal.Hide ();
            }
        }

        #endregion

        #region Partner entry handling

        private bool PartnerEvaluate ()
        {
            // If the field is not visible then we have nothing to validate
            if (!hboPartner.Visible)
                return true;

            // Empty field is never correct
            if (txtPartner.Text.Length == 0)
                return false;

            if (partner != null)
                return true;

            // See if we have a valid partner with such name or code
            string partnerText = txtPartner.Text.Trim ();

            Partner part = Partner.GetByName (partnerText) ??
                Partner.GetByCode (partnerText) ??
                Partner.GetByBulstat (partnerText) ??
                Partner.GetByCard (partnerText);

            if (part == null)
                return false;

            SetPartner (part);

            return true;
        }

        protected void PartnerFocus ()
        {
            if (!hboPartner.Visible || !txtPartner.Sensitive) {
                PartnerFocusNext ();
                return;
            }

            EditFormField (FormFields.Partner);
            txtPartner.GrabFocus ();
        }

        [ConnectBefore]
        private void txtPartner_Focused (object o, FocusedArgs args)
        {
            EditFormField (FormFields.Partner);
        }

        [ConnectBefore]
        private void txtPartner_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            switch (args.Event.Type) {
                case EventType.TwoButtonPress:
                    PartnerChoose (string.Empty);
                    args.RetVal = true;
                    break;

                case EventType.ButtonPress:
                    txtPartner_Focused (null, null);
                    break;
            }
        }

        [ConnectBefore]
        private void txtPartner_KeyPress (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey)) {
                PartnerChoose (string.Empty);
                args.RetVal = true;
                return;
            }

            switch (args.Event.Key) {
                case Key.ISO_Left_Tab:
                case Key.Up:
                case Key.KP_Up:
                    args.RetVal = true;
                    break;
                case Key.Tab:
                case Key.Down:
                case Key.KP_Down:
                case Key.Return:
                case Key.KP_Enter:
                    args.RetVal = true;
                    if (txtPartner.Text.Length > 0 && PartnerEvaluate ()) {
                        PartnerFocusNext ();
                        break;
                    }
                    PartnerChoose (txtPartner.Text);
                    break;
            }
        }

        [ConnectBefore]
        private void txtPartner_Changed (object sender, EventArgs e)
        {
            partner = null;
        }

        private void btnPickPartner_Clicked (object sender, EventArgs e)
        {
            PartnerChoose (string.Empty);
        }

        private void PartnerChoose (string filter)
        {
            Partner [] selected = null;
            if (BusinessDomain.AppConfiguration.ShowPartnerSuggestionsWhenNotFound)
                selected = EntityMissing.ShowPartnerMissing (filter, operation);

            if (selected == null) {
                if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditPartners") == UserRestrictionState.Allowed) {
                    using (ChooseEditPartner choosePartner = new ChooseEditPartner (true, filter)) {
                        if (choosePartner.Run () != ResponseType.Ok) {
                            txtPartner.GrabFocus ();
                            return;
                        }

                        selected = choosePartner.SelectedItems;
                    }
                } else
                    selected = new Partner [0];
            }

            if (selected.Length <= 0) {
                txtPartner.GrabFocus ();
                return;
            }

            SetPartner (selected [0]);
            PartnerFocusNext ();
        }

        protected virtual void SetPartner (Partner p)
        {
            if (p == null)
                return;

            operation.PartnerId = p.Id;
            operation.PartnerName = p.Name;
            operation.PartnerName2 = p.Name2;
            PriceGroup oldPriceGroup = Operation.GetPriceGroup (partnerPriceGroup, locationPriceGroup);
            PriceGroup newPriceGroup = Operation.GetPriceGroup (p.PriceGroup, locationPriceGroup);
            operation.UpdatePrices (oldPriceGroup, newPriceGroup);
            txtPartner.Text = p.Name;
            partnerPriceGroup = p.PriceGroup;
            partner = p;
        }

        private void PartnerFocusNext ()
        {
            LocationFocus ();
        }

        #endregion

        #region Location entry handling

        protected bool LocationEvaluate ()
        {
            // If the field is not visible then we have nothing to validate
            if (!hboLocation.Visible)
                return true;

            // Empty field is never correct
            if (txtLocation.Text.Length == 0)
                return false;

            if (location != null)
                return true;

            // See if we have a valid stock with such name
            Location pos = Location.GetByName (txtLocation.Text) ??
                Location.GetByCode (txtLocation.Text);

            if (pos == null)
                return false;

            SetLocation (pos);

            return true;
        }

        protected void LocationFocus ()
        {
            if (!hboLocation.Visible || !txtLocation.Sensitive) {
                LocationFocusNext ();
                return;
            }

            EditFormField (FormFields.Location);
            txtLocation.GrabFocus ();
        }

        [ConnectBefore]
        private void txtLocation_Focused (object o, FocusedArgs args)
        {
            EditFormField (FormFields.Location);
            //InitializeLocationCompletion();
        }

        [ConnectBefore]
        private void txtLocation_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            switch (args.Event.Type) {
                case EventType.TwoButtonPress:
                    LocationChoose (string.Empty);
                    args.RetVal = true;
                    break;

                case EventType.ButtonPress:
                    txtLocation_Focused (null, null);
                    break;
            }
        }

        [ConnectBefore]
        private void txtLocation_KeyPress (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey)) {
                LocationChoose (string.Empty);
                args.RetVal = true;
                return;
            }

            switch (args.Event.Key) {
                case Key.ISO_Left_Tab:
                case Key.Up:
                case Key.KP_Up:
                    args.RetVal = true;
                    PartnerFocus ();
                    break;
                case Key.Tab:
                case Key.Down:
                case Key.KP_Down:
                case Key.Return:
                case Key.KP_Enter:
                    args.RetVal = true;
                    if (txtLocation.Text.Length > 0 && LocationEvaluate ()) {
                        LocationFocusNext ();
                        break;
                    }
                    LocationChoose (txtLocation.Text);
                    break;
            }
        }

        [ConnectBefore]
        private void txtLocation_Changed (object sender, EventArgs e)
        {
            location = null;
        }

        private void btnPickLocation_Clicked (object sender, EventArgs e)
        {
            LocationChoose (string.Empty);
        }

        private void LocationChoose (string filter)
        {
            Location [] selected;

            if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditObjects") == UserRestrictionState.Allowed) {
                using (ChooseEditLocation chooseLocation = new ChooseEditLocation (true, filter)) {
                    if (chooseLocation.Run () != ResponseType.Ok) {
                        txtLocation.GrabFocus ();
                        return;
                    }

                    selected = chooseLocation.SelectedItems;
                }
            } else {
                selected = new Location [0];
            }

            if (selected.Length <= 0) {
                txtLocation.GrabFocus ();
                return;
            }

            SetLocation (selected [0]);
            LocationFocusNext ();
        }

        protected virtual void SetLocation (Location newLocation)
        {
            if (newLocation == null)
                return;

            operation.LocationId = newLocation.Id;
            operation.Location = newLocation.Name;
            operation.Location2 = newLocation.Name2;
            txtLocation.Text = newLocation.Name;
            locationPriceGroup = newLocation.PriceGroup;
            location = newLocation;
        }

        private void LocationFocusNext ()
        {
            SourceLocationFocus ();
        }

        #endregion

        #region Source Location entry handling

        protected bool SourceLocationEvaluate ()
        {
            // If the field is not visible then we have nothing to validate
            if (!hboSrcLocation.Visible)
                return true;

            // Empty field is never correct
            if (txtSrcLocation.Text.Length == 0)
                return false;

            if (srcLocation != null)
                return true;

            // See if we have a valid stock with such name
            Location pos = Location.GetByName (txtSrcLocation.Text) ??
                Location.GetByCode (txtSrcLocation.Text);

            if (pos == null)
                return false;

            if (dstLocation != null && dstLocation.Id == pos.Id) {
                MessageError.ShowDialog (Translator.GetString ("The selected locations must be different!."));
                return false;
            }

            SetSourceLocation (pos);

            return true;
        }

        protected void SourceLocationFocus ()
        {
            if (!hboSrcLocation.Visible || !txtSrcLocation.Sensitive) {
                SourceLocationFocusNext ();
                return;
            }

            EditFormField (FormFields.SourceLocation);
            txtSrcLocation.GrabFocus ();
        }

        [ConnectBefore]
        private void txtSrcLocation_Focused (object o, FocusedArgs args)
        {
            EditFormField (FormFields.SourceLocation);
        }

        [ConnectBefore]
        private void txtSrcLocation_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            switch (args.Event.Type) {
                case EventType.TwoButtonPress:
                    SourceLocationChoose (string.Empty);
                    args.RetVal = true;
                    break;

                case EventType.ButtonPress:
                    txtSrcLocation_Focused (null, null);
                    break;
            }
        }

        [ConnectBefore]
        private void txtSrcLocation_KeyPress (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey)) {
                SourceLocationChoose (string.Empty);
                args.RetVal = true;
                return;
            }

            switch (args.Event.Key) {
                case Key.ISO_Left_Tab:
                case Key.Up:
                case Key.KP_Up:
                    args.RetVal = true;
                    break;
                case Key.Tab:
                case Key.Down:
                case Key.KP_Down:
                case Key.Return:
                case Key.KP_Enter:
                    args.RetVal = true;
                    if (txtSrcLocation.Text.Length > 0 && SourceLocationEvaluate ()) {
                        SourceLocationFocusNext ();
                        break;
                    }
                    SourceLocationChoose (txtSrcLocation.Text);
                    break;
            }
        }

        private void txtSrcLocation_Changed (object sender, EventArgs e)
        {
            srcLocation = null;
        }

        private void btnPickSrcLocation_Clicked (object sender, EventArgs e)
        {
            SourceLocationChoose (string.Empty);
        }

        private void SourceLocationChoose (string filter)
        {
            Location [] selected;

            if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditObjects") == UserRestrictionState.Allowed) {
                using (ChooseEditLocation chooseSourceLocation = new ChooseEditLocation (true, filter)) {
                    if (chooseSourceLocation.Run () != ResponseType.Ok) {
                        txtSrcLocation.GrabFocus ();
                        return;
                    }

                    selected = chooseSourceLocation.SelectedItems;
                }
            } else {
                selected = new Location [0];
            }

            if (selected.Length <= 0) {
                txtSrcLocation.GrabFocus ();
                return;
            }

            SetSourceLocation (selected [0]);
            SourceLocationFocusNext ();
        }

        protected virtual void SetSourceLocation (Location pos)
        {
            srcLocation = pos;
        }

        private void SourceLocationFocusNext ()
        {
            DestinationLocationFocus ();
        }

        #endregion

        #region Destination Location entry handling

        private bool DestinationLocationEvaluate ()
        {
            // If the field is not visible then we have nothing to validate
            if (!hboDstLocation.Visible)
                return true;

            // Empty field is never correct
            if (txtDstLocation.Text.Length == 0)
                return false;

            if (dstLocation != null)
                return true;

            // See if we have a valid store with such name
            Location pos = Location.GetByName (txtDstLocation.Text) ??
                Location.GetByCode (txtDstLocation.Text);

            if (pos == null)
                return false;

            if (srcLocation != null && srcLocation.Id == pos.Id) {
                MessageError.ShowDialog (Translator.GetString ("The selected locations must be different!."));
                return false;
            }

            SetDestinationLocation (pos);

            return true;
        }

        private void DestinationLocationFocus ()
        {
            if (!hboDstLocation.Visible || !txtDstLocation.Sensitive) {
                DestinationLocationFocusNext ();
                return;
            }

            EditFormField (FormFields.DestinationLocation);
            txtDstLocation.GrabFocus ();
        }

        [ConnectBefore]
        private void txtDstLocation_Focused (object o, FocusedArgs args)
        {
            EditFormField (FormFields.DestinationLocation);
        }

        [ConnectBefore]
        private void txtDstLocation_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            switch (args.Event.Type) {
                case EventType.TwoButtonPress:
                    DestinationLocationChoose (string.Empty);
                    args.RetVal = true;
                    break;

                case EventType.ButtonPress:
                    txtDstLocation_Focused (null, null);
                    break;
            }
        }

        [ConnectBefore]
        private void txtDstLocation_KeyPress (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey)) {
                DestinationLocationChoose (string.Empty);
                args.RetVal = true;
                return;
            }

            switch (args.Event.Key) {
                case Key.ISO_Left_Tab:
                case Key.Up:
                case Key.KP_Up:
                    args.RetVal = true;
                    SourceLocationFocus ();
                    break;
                case Key.Tab:
                case Key.Down:
                case Key.KP_Down:
                case Key.Return:
                case Key.KP_Enter:
                    args.RetVal = true;
                    if (txtDstLocation.Text.Length > 0 && DestinationLocationEvaluate ()) {
                        DestinationLocationFocusNext ();
                        break;
                    }
                    DestinationLocationChoose (txtDstLocation.Text);
                    break;
            }
        }

        [ConnectBefore]
        private void txtDstLocation_Changed (object sender, EventArgs e)
        {
            dstLocation = null;
        }

        private void btnPickDstLocation_Clicked (object sender, EventArgs e)
        {
            DestinationLocationChoose (string.Empty);
        }

        private void DestinationLocationChoose (string filter)
        {
            Location [] selected;

            if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditObjects") == UserRestrictionState.Allowed) {
                using (ChooseEditLocation chooseDestinationLocation = new ChooseEditLocation (true, filter)) {
                    if (chooseDestinationLocation.Run () != ResponseType.Ok) {
                        txtDstLocation.GrabFocus ();
                        return;
                    }

                    selected = chooseDestinationLocation.SelectedItems;
                }
            } else {
                selected = new Location [0];
            }

            if (selected.Length <= 0) {
                txtDstLocation.GrabFocus ();
                return;
            }

            txtDstLocation.Text = selected [0].Name;
            if (DestinationLocationEvaluate ()) {
                DestinationLocationFocusNext ();
            } else {
                DestinationLocationFocus ();
            }
        }

        protected virtual void SetDestinationLocation (Location pos)
        {
            dstLocation = pos;
        }

        private void DestinationLocationFocusNext ()
        {
            UserFocus ();
        }

        #endregion

        #region User entry handling

        protected bool UserEvaluate ()
        {
            // If the field is not visible then we have nothing to validate
            if (!hboUser.Visible)
                return true;

            string userName = txtUser.Text.Trim ();

            // Empty field is never correct
            if (userName.Length == 0)
                return false;

            if (user != null)
                return true;

            // See if we have a valid stock with such name
            User usr = User.GetByName (userName) ??
                User.GetByCode (userName) ??
                User.GetByCard (userName);

            if (usr == null)
                return false;

            SetUser (usr);

            return true;
        }

        protected void UserFocus ()
        {
            if (!hboUser.Visible || !txtUser.Sensitive) {
                UserFocusNext ();
                return;
            }

            EditFormField (FormFields.User);
            txtUser.GrabFocus ();
        }

        [ConnectBefore]
        private void txtUser_Focused (object o, FocusedArgs args)
        {
            EditFormField (FormFields.User);
        }

        [ConnectBefore]
        private void txtUser_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            switch (args.Event.Type) {
                case EventType.TwoButtonPress:
                    UserChoose (string.Empty);
                    args.RetVal = true;
                    break;

                case EventType.ButtonPress:
                    txtUser_Focused (null, null);
                    break;
            }
        }

        [ConnectBefore]
        private void txtUser_KeyPress (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey)) {
                UserChoose (string.Empty);
                args.RetVal = true;
                return;
            }

            switch (args.Event.Key) {
                case Key.ISO_Left_Tab:
                case Key.Up:
                case Key.KP_Up:
                    args.RetVal = true;
                    if (hboDstLocation.Visible)
                        DestinationLocationFocus ();
                    else
                        LocationFocus ();
                    break;
                case Key.Tab:
                case Key.Down:
                case Key.KP_Down:
                case Key.Return:
                case Key.KP_Enter:
                    args.RetVal = true;
                    if (txtUser.Text.Length > 0 && UserEvaluate ()) {
                        UserFocusNext ();
                        break;
                    }
                    UserChoose (txtUser.Text);
                    break;
            }
        }

        [ConnectBefore]
        private void txtUser_Changed (object sender, EventArgs e)
        {
            user = null;
        }

        private void btnPickUser_Clicked (object sender, EventArgs e)
        {
            UserChoose (string.Empty);
        }

        private void UserChoose (string filter)
        {
            User [] selected;

            if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditUsers") == UserRestrictionState.Allowed) {
                using (ChooseEditUser chooseUser = new ChooseEditUser (true, filter)) {
                    if (chooseUser.Run () != ResponseType.Ok) {
                        txtUser.GrabFocus ();
                        return;
                    }

                    selected = chooseUser.SelectedItems;
                }
            } else {
                selected = new User [0];
            }

            if (selected.Length <= 0) {
                txtUser.GrabFocus ();
                return;
            }

            SetUser (selected [0]);
            UserFocusNext ();
        }

        protected virtual void SetUser (User u)
        {
            if (u == null)
                return;

            operation.UserId = u.Id;
            operation.UserName = u.Name;
            operation.UserName2 = u.Name2;
            txtUser.Text = u.Name;
            user = u;
        }

        protected virtual void UserFocusNext ()
        {
            EditGridCell (grid.Model.Count - 1, colItem.Index);
        }

        #endregion

        #region Date entry handling

        protected bool DateEvaluate ()
        {
            // If the field is not visible then we have nothing to validate
            if (!hboDate.Visible)
                return true;

            // Empty field is never correct
            if (txtDate.Text.Length == 0)
                return false;

            if (date != DateTime.MinValue)
                return true;

            DateTime dat = BusinessDomain.GetDateValue (txtDate.Text);

            if (dat == DateTime.MinValue) {
                return false;
            }

            SetDate (dat);

            return true;
        }

        protected void DateFocus ()
        {
            EditFormField (FormFields.Date);
            txtDate.GrabFocus ();
        }

        [ConnectBefore]
        private void txtDate_Focused (object o, FocusedArgs args)
        {
            EditFormField (FormFields.Date);
        }

        [ConnectBefore]
        private void txtDate_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            switch (args.Event.Type) {
                case EventType.TwoButtonPress:
                    DateChoose ();
                    args.RetVal = true;
                    break;

                case EventType.ButtonPress:
                    txtDate_Focused (null, null);
                    break;
            }
        }

        [ConnectBefore]
        private void txtDate_KeyPress (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey)) {
                DateChoose ();
                args.RetVal = true;
                return;
            }

            switch (args.Event.Key) {
                case Key.ISO_Left_Tab:
                case Key.Up:
                case Key.KP_Up:
                    args.RetVal = true;
                    UserFocus ();
                    break;
                case Key.Tab:
                case Key.Down:
                case Key.KP_Down:
                case Key.Return:
                case Key.KP_Enter:
                    args.RetVal = true;
                    if (txtDate.Text.Length > 0 && DateEvaluate ()) {
                        DateFocusNext ();
                        break;
                    }
                    DateChoose ();
                    break;
            }
        }

        [ConnectBefore]
        private void txtDate_Changed (object sender, EventArgs e)
        {
            date = DateTime.MinValue;
        }

        private void btnPickDate_Clicked (object sender, EventArgs e)
        {
            DateChoose ();
        }

        private void DateChoose ()
        {
            using (ChooseDate chooseDate = new ChooseDate ()) {
                if (date != DateTime.MinValue)
                    chooseDate.Selection = date;

                if (chooseDate.Run () != ResponseType.Ok) {
                    txtDate.GrabFocus ();
                    return;
                }

                SetDate (chooseDate.Selection);
            }

            DateFocusNext ();
        }

        protected virtual void SetDate (DateTime d)
        {
            if (d == DateTime.MinValue)
                return;

            operation.Date = d;
            txtDate.Text = BusinessDomain.GetFormattedDate (d);
            date = d;
        }

        protected virtual void DateFocusNext ()
        {
            EditGridCell (grid.Model.Count - 1, colItem.Index);
        }

        #endregion

        protected void SetNote (IEnumerable<OperationDetail> details)
        {
            string [] notes = details.Select (d => d.Note).Distinct ().ToArray ();

            switch (notes.Length) {
                case 0:
                    txtNote.Buffer.Clear ();
                    NoteVisible = true;
                    break;
                case 1:
                    txtNote.Buffer.Text = notes [0];
                    NoteVisible = true;
                    break;
                default:
                    NoteVisible = false;
                    break;
            }

            lblNote.SetText (Translator.GetString ("Note"));
            expNote.Expanded = notes.Length == 1 && !string.IsNullOrWhiteSpace (notes [0]);

            operation.PropertyChanged -= operation_PropertyChanged;
            operation.PropertyChanged += operation_PropertyChanged;
        }

        private void operation_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Note")
                SetNote (operation.Details);
        }

        protected void EditGridField (string itemName)
        {
            for (int i = 0; i < operation.Details.Count; i++) {
                if (operation.Details [i].ItemName != itemName)
                    continue;

                EditGridField (i, colItem.Index);
                return;
            }

            if (operation.AdditionalDetails == null)
                return;

            for (int i = 0; i < operation.AdditionalDetails.Count; i++) {
                if (operation.AdditionalDetails [i].ItemName != itemName)
                    continue;

                EditSecondGridField (i, colItem.Index);
                return;
            }
        }

        protected virtual bool OperationValidate ()
        {
            if (!PartnerEvaluate ()) {
                PartnerFocus ();
                return false;
            }

            string error = null;
            string warning = null;
            if (!LocationEvaluate () ||
                (location != null && !BusinessDomain.CanUseLocationInOperation (operation.OperationType, location.Id, out error, out warning))) {
                if (warning != null) {
                    bool value;
                    if (!canUseLocationIdWarning.TryGetValue (location.Id, out value) || !value) {
                        if (MessageError.ShowDialog (warning, buttons: MessageButtons.YesNo) != ResponseType.Yes) {
                            LocationFocus ();
                            canUseLocationIdWarning [location.Id] = false;
                            return false;
                        }

                        canUseLocationIdWarning [location.Id] = true;
                    }
                } else {
                    if (error != null)
                        MessageError.ShowDialog (error);

                    LocationFocus ();
                    return false;
                }
            }

            if (!SourceLocationEvaluate () ||
                (srcLocation != null && !BusinessDomain.CanUseLocationInOperation (operation.OperationType, srcLocation.Id, out error, out warning))) {
                if (warning != null) {
                    bool value;
                    if (!canUseLocationIdWarning.TryGetValue (srcLocation.Id, out value) || !value) {
                        if (MessageError.ShowDialog (warning, buttons: MessageButtons.YesNo) != ResponseType.Yes) {
                            SourceLocationFocus ();
                            canUseLocationIdWarning [srcLocation.Id] = false;
                            return false;
                        }

                        canUseLocationIdWarning [srcLocation.Id] = true;
                    }
                } else {
                    if (error != null)
                        MessageError.ShowDialog (error);

                    SourceLocationFocus ();
                    return false;
                }
            }

            if (!DestinationLocationEvaluate ()) {
                DestinationLocationFocus ();
                return false;
            }

            if (!UserEvaluate ()) {
                UserFocus ();
                return false;
            }

            if (!DateEvaluate ()) {
                DateFocus ();
                return false;
            }

            if (NoteVisible) {
                operation.Note = txtNote.Buffer.Text;
            }

            return true;
        }

        protected bool CurrentColumnEvaluate ()
        {
            CellPosition editPos;

            if (grid != null) {
                editPos = grid.EditedCell;
                if (editPos.IsValid) {
                    int row = editPos.Row;
                    string cellValue = grid.EditedCellValue.ToString ();
                    if (colItem != null && cursorAtColumn == colItem.Index)
                        return ItemColumnEvaluate (row, cellValue);
                    if (colQuantity != null && cursorAtColumn == colQuantity.Index)
                        return QtyColumnEvaluate (row, cellValue);
                    if (colPurchasePrice != null && cursorAtColumn == colPurchasePrice.Index)
                        return PurchaseValueColumnEvaluate (row, cellValue);
                    if (colSalePrice != null && cursorAtColumn == colSalePrice.Index)
                        return SalePriceColumnEvaluate (row, cellValue);
                    if (colDiscount != null && cursorAtColumn == colDiscount.Index)
                        return DiscountColumnEvaluate (row, cellValue);
                    if (colDiscountValue != null && cursorAtColumn == colDiscountValue.Index)
                        return DiscountValueColumnEvaluate (row, cellValue);
                    if (colLot != null && cursorAtColumn == colLot.Index)
                        return LotColumnEvaluate (row, cellValue);
                    if (colSerialNo != null && cursorAtColumn == colSerialNo.Index)
                        return SerialNoColumnEvaluate (row, cellValue);
                    if (colExpirationDate != null && cursorAtColumn == colExpirationDate.Index)
                        return ExpirationDateColumnEvaluate (row, cellValue);
                    if (colProductionDate != null && cursorAtColumn == colProductionDate.Index)
                        return ProductionDateColumnEvaluate (row, cellValue);
                    if (colLotLocation != null && cursorAtColumn == colLotLocation.Index)
                        return LotLocationColumnEvaluate (row, cellValue);
                }
            }

            if (secondGrid != null) {
                editPos = secondGrid.EditedCell;
                if (editPos.IsValid) {
                    int row = editPos.Row;
                    string cellValue = secondGrid.EditedCellValue.ToString ();
                    if (colSecondItem != null && cursorAtColumn == colSecondItem.Index)
                        return SecondItemColumnEvaluate (row, cellValue);
                    if (colSecondQuantity != null && cursorAtColumn == colSecondQuantity.Index)
                        return SecondQtyColumnEvaluate (row, cellValue);
                    if (colSecondPurchaseValue != null && cursorAtColumn == colSecondPurchaseValue.Index)
                        return SecondPurchaseValueColumnEvaluate (row, cellValue);
                    if (colSecondSalePrice != null && cursorAtColumn == colSecondSalePrice.Index)
                        return SecondSalePriceColumnEvaluate (row, cellValue);
                    if (colSecondDiscount != null && cursorAtColumn == colSecondDiscount.Index)
                        return SecondDiscountColumnEvaluate (row, cellValue);
                    if (colSecondDiscountValue != null && cursorAtColumn == colSecondDiscountValue.Index)
                        return SecondDiscountValueColumnEvaluate (row, cellValue);
                    if (colSecondLot != null && cursorAtColumn == colSecondLot.Index)
                        return SecondLotColumnEvaluate (row, cellValue);
                    if (colSecondSerialNo != null && cursorAtColumn == colSecondSerialNo.Index)
                        return SecondSerialNoColumnEvaluate (row, cellValue);
                    if (colSecondExpirationDate != null && cursorAtColumn == colSecondExpirationDate.Index)
                        return SecondExpirationDateColumnEvaluate (row, cellValue);
                    if (colSecondProductionDate != null && cursorAtColumn == colSecondProductionDate.Index)
                        return SecondProductionDateColumnEvaluate (row, cellValue);
                    if (colSecondLotLocation != null && cursorAtColumn == colSecondLotLocation.Index)
                        return SecondLotLocationColumnEvaluate (row, cellValue);
                }
            }
            return true;
        }

        protected virtual void InitializeGrid ()
        {
            if (grid == null) {
                grid = new ListView { Name = "grid" };

                ScrolledWindow sWindow = new ScrolledWindow { HscrollbarPolicy = PolicyType.Automatic, VscrollbarPolicy = PolicyType.Automatic };
                sWindow.Add (grid);

                algGrid.Add (sWindow);
                sWindow.Show ();
                grid.Show ();
            }

            ColumnController cc = new ColumnController ();

            if (BusinessDomain.AppConfiguration.EnableLineNumber)
                InitializeLineNumberColumn (cc);

            if (BusinessDomain.AppConfiguration.EnableItemCode)
                InitializeItemCodeColumn (cc);

            InitializeItemColumn (cc);

            colMeasure = new Column (Translator.GetString ("Measure"), "MUnitName", 0.1) { MinWidth = 70 };
            cc.Add (colMeasure);

            InitializeQuantityColumn (cc);

            InitializePurchasePriceColumn (cc);

            InitializeSalePriceColumn (cc);

            if (BusinessDomain.AppConfiguration.AllowPercentDiscounts)
                InitializeDiscountColumn (cc);

            if (BusinessDomain.AppConfiguration.AllowValueDiscounts)
                InitializeDiscountValueColumn (cc);

            if (BusinessDomain.AppConfiguration.AllowItemLotName)
                InitializeLotNameColumn (cc);
            else
                InitializeDisabledLotNameColumn (cc);

            if (BusinessDomain.AppConfiguration.AllowItemSerialNumber)
                InitializeSerialNumberColumn (cc);

            if (BusinessDomain.AppConfiguration.AllowItemExpirationDate)
                InitializeExpirationDateColumn (cc);

            if (BusinessDomain.AppConfiguration.AllowItemManufacturedDate)
                InitializeManufacturedDateColumn (cc);

            if (BusinessDomain.AppConfiguration.AllowItemLocation)
                InitializeLotLocationColumn (cc);
            else
                InitializeDisabledLotLocationColumn (cc);

            if (BusinessDomain.AppConfiguration.EnableItemVatRate)
                InitializeVatRateColumn (cc);

            InitializeTotalColumn (cc);

            grid.ColumnController = cc;
            grid.AllowSelect = false;
            grid.CellsFucusable = true;
            grid.ManualFucusChange = true;
            grid.RulesHint = true;
            grid.CellKeyPressEvent -= grid_CellKeyPressEvent;
            grid.CellKeyPressEvent += grid_CellKeyPressEvent;
            grid.CellStyleProvider = (sender, args) =>
                {
                    if (operation.Details [args.Cell.Row].InsufficientQuantity && args.Cell.Column == colQuantity.Index)
                        args.Style = insufficiencyHighlight;
                };
        }

        protected virtual void InitializeLineNumberColumn (ColumnController cc)
        {
            CellTextEnumerator cte = new CellTextEnumerator ();
            colLineNumber = new Column ("№", cte, 0.01) { MinWidth = 25 };
            cc.Add (colLineNumber);
        }

        protected virtual void InitializeItemCodeColumn (ColumnController cc)
        {
            CellText ct = new CellText ("ItemCode");
            colItemCode = new Column (Translator.GetString ("Code"), ct, 0.1) { MinWidth = 70 };
            cc.Add (colItemCode);
        }

        protected virtual void InitializeItemColumn (ColumnController cc)
        {
            CellText ct = new CellText ("ItemName") { IsEditable = true };
            colItem = new Column (Translator.GetString ("Item"), ct, 1);
            colItem.ButtonPressEvent += ItemColumn_ButtonPressEvent;
            colItem.KeyPressEvent += ItemColumn_KeyPress;
            cc.Add (colItem);
        }

        protected virtual void InitializeQuantityColumn (ColumnController cc)
        {
            CellTextQuantity ctf = new CellTextQuantity ("Quantity") { IsEditable = true };
            colQuantity = new Column (Translator.GetString ("Qtty"), ctf, 0.1) { MinWidth = 70 };
            colQuantity.ButtonPressEvent += QtyColumn_ButtonPressEvent;
            colQuantity.KeyPressEvent += QtyColumn_KeyPress;
            cc.Add (colQuantity);
        }

        protected virtual void InitializePurchasePriceColumn (ColumnController cc)
        {
            CellTextCurrency ctf = new CellTextCurrency ("PriceIn", PriceType.Purchase);
            colPurchasePrice = new Column (Translator.GetString ("Purchase price"), ctf, 0.1) { MinWidth = 70 };
            colPurchasePrice.ButtonPressEvent += PurchasePriceColumn_ButtonPressEvent;
            colPurchasePrice.KeyPressEvent += PurchasePriceColumn_KeyPress;
            cc.Add (colPurchasePrice);
        }

        protected virtual void InitializeSalePriceColumn (ColumnController cc)
        {
            CellTextCurrency ctf = new CellTextCurrency ("PriceOut") { IsEditable = true };
            colSalePrice = new Column (Translator.GetString ("Sale price"), ctf, 0.1) { MinWidth = 70 };
            colSalePrice.ButtonPressEvent += SalePriceColumn_ButtonPressEvent;
            colSalePrice.KeyPressEvent += SalePriceColumn_KeyPress;
            cc.Add (colSalePrice);
        }

        protected virtual void InitializeDiscountColumn (ColumnController cc)
        {
            CellTextDouble ctf = new CellTextDouble ("Discount") { IsEditable = true, FixedFaction = BusinessDomain.AppConfiguration.PercentPrecision };
            colDiscount = new Column (Translator.GetString ("Discount %"), ctf, 0.1) { MinWidth = 70 };
            colDiscount.ButtonPressEvent += DiscountColumn_ButtonPressEvent;
            colDiscount.KeyPressEvent += DiscountColumn_KeyPress;
            cc.Add (colDiscount);
        }

        protected virtual void InitializeDiscountValueColumn (ColumnController cc)
        {
            CellTextCurrency ctf = new CellTextCurrency ("DiscountValue") { IsEditable = true };
            colDiscountValue = new Column (Translator.GetString ("Discount value"), ctf, 0.1) { MinWidth = 70 };
            colDiscountValue.ButtonPressEvent += DiscountValueColumn_ButtonPressEvent;
            colDiscountValue.KeyPressEvent += DiscountValueColumn_KeyPress;
            cc.Add (colDiscountValue);
        }

        protected virtual void InitializeLotNameColumn (ColumnController cc)
        {
            CellText ct = new CellText ("Lot");
            colLot = new Column (Translator.GetString ("Lot"), ct, 0.1) { MinWidth = 70 };
            colLot.ButtonPressEvent += LotColumn_ButtonPressEvent;
            colLot.KeyPressEvent += LotColumn_KeyPress;
            cc.Add (colLot);
        }

        protected virtual void InitializeDisabledLotNameColumn (ColumnController cc)
        {
        }

        protected virtual void InitializeSerialNumberColumn (ColumnController cc)
        {
            CellText ct = new CellText ("SerialNumber");
            colSerialNo = new Column (Translator.GetString ("Serial number"), ct, 0.1) { MinWidth = 80 };
            colSerialNo.ButtonPressEvent += SerialNoColumn_ButtonPressEvent;
            colSerialNo.KeyPressEvent += SerialNoColumn_KeyPress;
            cc.Add (colSerialNo);
        }

        protected virtual void InitializeExpirationDateColumn (ColumnController cc)
        {
            CellTextDate ctd = new CellTextDate ("ExpirationDate");
            colExpirationDate = new Column (Translator.GetString ("Expiration date"), ctd, 0.1) { MinWidth = 70 };
            colExpirationDate.ButtonPressEvent += ExpirationDateColumn_ButtonPressEvent;
            colExpirationDate.KeyPressEvent += ExpirationDateColumn_KeyPress;
            cc.Add (colExpirationDate);
        }

        protected virtual void InitializeManufacturedDateColumn (ColumnController cc)
        {
            CellTextDate ctd = new CellTextDate ("ProductionDate");
            colProductionDate = new Column (Translator.GetString ("Production date"), ctd, 0.1) { MinWidth = 70 };
            colProductionDate.ButtonPressEvent += ProductionDateColumn_ButtonPressEvent;
            colProductionDate.KeyPressEvent += ProductionDateColumn_KeyPress;
            cc.Add (colProductionDate);
        }

        protected virtual void InitializeLotLocationColumn (ColumnController cc)
        {
            CellText ct = new CellText ("LotLocation");
            colLotLocation = new Column (Translator.GetString ("Lot location"), ct, 0.1) { MinWidth = 70 };
            colLotLocation.ButtonPressEvent += LotLocationColumn_ButtonPressEvent;
            colLotLocation.KeyPressEvent += LotLocationColumn_KeyPress;
            cc.Add (colLotLocation);
        }

        protected virtual void InitializeDisabledLotLocationColumn (ColumnController cc)
        {
        }

        protected virtual void InitializeVatRateColumn (ColumnController cc)
        {
            CellTextPercent ctf = new CellTextPercent ("VatRate");
            ICashReceiptPrinterController cashReceiptPrinter = BusinessDomain.DeviceManager.CashReceiptPrinterDriver;
            if (cashReceiptPrinter == null || cashReceiptPrinter.GetAttributes ().ContainsKey (DriverBase.USES_CUSTOM_VAT_RATE))
                ctf.IsEditable = true;

            colVatRate = new Column (BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                Translator.GetString ("Tax %") :
                Translator.GetString ("VAT %"), ctf, 0.1) { MinWidth = 70 };
            colVatRate.ButtonPressEvent += VatRateColumn_ButtonPressEvent;
            colVatRate.KeyPressEvent += VatRateColumn_KeyPress;
            cc.Add (colVatRate);
        }

        protected virtual void InitializeTotalColumn (ColumnController cc)
        {
            CellTextCurrency ctf = new CellTextCurrency ("Total", operation.TotalsPriceType);
            colTotal = new Column (Translator.GetString ("Amount"), ctf, 0.1) { MinWidth = 70 };
            cc.Add (colTotal);
        }

        protected void InitializeStatistics ()
        {
            if (!BusinessDomain.AppConfiguration.ShowOperationStatistics)
                return;

            Column column = colLineNumber ?? colItem;
            column.FooterCell = new CellTextFooter ();
            colQuantity.FooterCell = new CellTextFooter { Alignment = Pango.Alignment.Right };
            grid.FooterVisible = true;
            if (secondGrid == null)
                return;

            column = colSecondLineNumber ?? colSecondItem;
            column.FooterCell = new CellTextFooter ();
            colSecondQuantity.FooterCell = new CellTextFooter { Alignment = Pango.Alignment.Right };
            secondGrid.FooterVisible = true;
        }

        private void grid_CellKeyPressEvent (object sender, CellKeyPressEventArgs args)
        {
            OnCellKeyPress (sender, args, true, operation.Details);
        }

        #region Grid column handling

        protected virtual bool EditGridCell (int row, int column)
        {
            grid.DisableEdit = false;

            if (OperationValidate ()) {
                if (row >= 0 && column >= 0)
                    return EditGridField (row, column);

                return true;
            }

            grid.DisableEdit = true;
            return false;
        }

        protected virtual void GridColumnEditBelow (int column)
        {
            if (!grid.EditedCell.IsValid)
                return;

            if (grid.EditedCell.Row + 1 >= grid.Model.Count)
                return;

            EditGridField (grid.EditedCell.Row + 1, column);
        }

        private void GridColumnEditOver (int column)
        {
            if (!grid.EditedCell.IsValid)
                return;

            if (grid.EditedCell.Row <= 0)
                return;

            EditGridField (grid.EditedCell.Row - 1, column);
        }

        protected virtual void DeleteGridRow (bool keepColumnPos, bool logChange = true)
        {
            if (!grid.EditedCell.IsValid)
                return;

            if (editMode) {
                CellPosition editPos = grid.EditedCell;
                if (editPos.Column == colQuantity.Index)
                    grid.CancelCellEdit ();

                operation.Details [editPos.Row].Quantity = 0;

                EditGridField (editPos.Row, editPos.Column);
                return;
            }

            if (operation.Details.Count > 1) {
                int col = grid.EditedCell.Column;
                int row = grid.EditedCell.Row;
                int newRow;

                if (row == operation.Details.Count - 1) {
                    // If we are deleting the last row move one row up
                    operation.RemoveDetail (row, logChange);
                    newRow = row - 1;
                } else {
                    // If we are deleting row from somewhere in between stay on the same line
                    operation.RemoveDetail (row, logChange);
                    newRow = row;
                }

                if (keepColumnPos)
                    EditGridField (newRow, col);
                else
                    ItemColumnEditPrev (row, Key.Left);
            } else {
                operation.ClearDetails (logChange);
                operation.AddNewDetail ();

                EditGridField (0, colItem.Index);
            }
        }

        #region Line number column handling

        private Column colLineNumber;

        #endregion

        #region Item Code column handling

        protected Column colItemCode;

        #endregion

        #region Item column handling

        protected Column colItem;

        protected bool ItemColumnEvaluate (int row, string itemName)
        {
            barcodeUsed = false;
            TOperDetail detail = operation.Details [row];
            if (detail.ItemId >= 0 && detail.ItemName == itemName)
                return true;

            double currentQuantity = detail.Quantity;

            long codeStoreId;
            Item item = Item.GetByAny (itemName, out barcodeUsed, out codeQtty, out codeLot, out codeStoreId, detail.UsesSavedLots ? operation.LocationId : (long?) null);

            if (!ItemColumnEvaluate (row, item, false))
                return false;

            // no quantity from the barcode scanner
            if (codeQtty.IsZero ())
                codeQtty = currentQuantity;

            if (detail.UsesSavedLots && codeStoreId >= 0) {
                Lot lot = Lot.GetByStoreId (codeStoreId);
                if (lot != null)
                    detail.LotEvaluate (lot);
            }

            return true;
        }

        protected virtual bool ItemColumnEvaluate (int row, Item item, bool updatePrice)
        {
            return operation.Details [row].ItemEvaluate (item, GetOperationPriceGroup (), updatePrice);
        }

        protected virtual long AvailabilityLocationId
        {
            get { return operation.LocationId; }
        }

        protected void ItemColumnChoose (string filter)
        {
            Item [] items = null;
            if (BusinessDomain.AppConfiguration.ShowItemSuggestionsWhenNotFound)
                items = EntityMissing.ShowItemMissing (filter, operation);

            if (items == null) {
                if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditGoods") == UserRestrictionState.Allowed) {
                    using (ChooseEditItem dialog = new ChooseEditItem (AvailabilityLocationId, GetOperationPriceGroup (), filter))
                        if (dialog.Run () == ResponseType.Ok)
                            items = dialog.SelectedItems.ToArray ();
                } else
                    MessageError.ShowDialog (string.Format (Translator.GetString ("Item \"{0}\" cannot be found!"), filter));
            }

            barcodeUsed = false;
            codeQtty = 0;
            codeLot = null;

            int row = grid.EditedCell.Row;
            bool itemAdded = false;

            if (items != null)
                foreach (Item item in items) {
                    if (row >= operation.Details.Count)
                        operation.AddNewDetail ();

                    if (ItemColumnEvaluate (row, item, true)) {
                        itemAdded = true;
                        row++;
                    }
                }

            if (itemAdded)
                ItemColumnEditNext (grid.EditedCell.Row, Key.Return);
            else
                EditGridField (row, colItem.Index);
        }

        protected virtual void ItemColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            if (grid.EditedCell != args.Cell) {
                CurrentColumnEvaluate ();
                EditGridCell (args.Cell.Row, args.Cell.Column);
            } else {
                EditGridCell (-1, -1);
            }

            if (grid.DisableEdit || args.EventButton.Type != EventType.TwoButtonPress)
                return;

            object cellValue = grid.EditedCellValue;
            GridNavigator.ChooseCellValue (ItemColumnEvaluate, ItemColumnChoose,
                cellValue == null ? string.Empty : cellValue.ToString ());
        }

        private void ItemColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            if (GridNavigator.ColumnKeyPress (args, colItem.Index, ItemColumnChoose,
                ItemColumnEvaluate, ItemColumnEditPrev, ItemColumnEditNext))
                return;

            string quickGoods;
            if (!BusinessDomain.QuickItems.TryGetValue (KeyShortcuts.KeyToString (args.EventKey), out quickGoods))
                return;

            if (!ItemColumnEvaluate (grid.EditedCell.Row, quickGoods))
                return;

            int row = grid.EditedCell.Row;
            QtyColumnEvaluate (ref row, 1);

            if (operation.Details.Count <= row + 1)
                operation.AddNewDetail ();

            EditGridField (row + 1, colItem.Index);

            args.MarkAsHandled ();
        }

        protected void ItemColumnEditPrev (int row, Key keyCode)
        {
            GridNavigator.EditPrevOnFirst (row, keyCode, colVatRate, VatRateColumnEditPrev,
                r => DeleteGridRow (false));
        }

        protected virtual void ItemColumnEditNext (int row, Key keyCode)
        {
            if (!codeQtty.IsZero ())
                QtyColumnEvaluate (ref row, codeQtty);

            if (barcodeUsed) {
                if (codeQtty.IsZero ())
                    QtyColumnEvaluate (ref row, operation.Details [row].QuantityIncrement);

                if (operation.Details.Count <= row + 1)
                    operation.AddNewDetail ();

                EditGridField (row + 1, colItem.Index);
            } else if (colQuantity != null && colQuantity.ListCell.IsEditable)
                EditGridField (row, colQuantity.Index);
            else
                QtyColumnEditNext (row, keyCode);
        }

        #endregion

        #region Measure column handling

        protected Column colMeasure;

        #endregion

        #region Quantity column handling

        protected Column colQuantity;

        protected bool QtyColumnEvaluate (int row, string quantity)
        {
            return QtyColumnEvaluate (ref row, Quantity.ParseExpression (quantity));
        }

        protected virtual bool QtyColumnEvaluate (ref int row, double qtyValue)
        {
            if (!CheckQuantityLimitations (qtyValue, row, grid))
                return false;

            TOperDetail detail = operation.Details [row];
            detail.Quantity = qtyValue;
            if (location != null)
                detail.CheckForInsufficiency (operation, location.Id);

            LotsEvaluate (operation.Details, detail);
            if (barcodeUsed) {
                TOperDetail detailWithSameItem = operation.Details.LastOrDefault (d => d != detail &&
                    d.PriceInDB.IsEqualTo (detail.PriceInDB) && d.PriceOutDB.IsEqualTo (detail.PriceOutDB) &&
                    d.ItemId == detail.ItemId && d.LotId == detail.LotId && Lot.CompareLots (d.Lot, detail.Lot) &&
                    d.Discount.IsEqualTo (detail.Discount));

                if (detailWithSameItem != null) {
                    double increment = operation.Details [row].QuantityIncrement;
                    detailWithSameItem.Quantity = detailWithSameItem.Quantity + increment * (codeQtty.IsZero () ? codeQtty = 1 : codeQtty);
                    DeleteGridRow (true, false);
                    --row;
                    return true;
                }
            }
            return true;
        }

        protected void QtyColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditGridCell (args.Cell.Row, args.Cell.Column);
        }

        protected void QtyColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            GridNavigator.ColumnKeyPress (args, colQuantity.Index,
                QtyColumnEvaluate, QtyColumnEditPrev, QtyColumnEditNext);
        }

        protected virtual void QtyColumnEditPrev (int row, Key keyCode)
        {
            GridNavigator.EditPrev (row, keyCode, colItem, ItemColumnEditPrev);
        }

        protected virtual void QtyColumnEditNext (int row, Key keyCode)
        {
            GridNavigator.EditNext (row, keyCode, colPurchasePrice, PurchaseValueColumnEditNext);
        }

        #endregion

        #region Purchase value column handling

        protected Column colPurchasePrice;

        protected virtual bool PurchaseValueColumnEvaluate (int row, string price)
        {
            operation.Details [row].OriginalPriceInEvaluate (Currency.ParseExpression (price, PriceType.Purchase), operation);
            return true;
        }

        protected void PurchasePriceColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditGridCell (args.Cell.Row, args.Cell.Column);
        }

        protected void PurchasePriceColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            GridNavigator.ColumnKeyPress (args, colPurchasePrice.Index,
                                          PurchaseValueColumnEvaluate, PurchaseValueColumnEditPrev, PurchaseValueColumnEditNext);
        }

        protected virtual void PurchaseValueColumnEditPrev (int row, Key keyCode)
        {
            GridNavigator.EditPrev (row, keyCode, colQuantity, QtyColumnEditPrev);
        }

        protected virtual void PurchaseValueColumnEditNext (int row, Key keyCode)
        {
            GridNavigator.EditNext (row, keyCode, colSalePrice, SalePriceColumnEditNext);
        }

        #endregion

        #region Sale price column handling

        protected Column colSalePrice;

        protected virtual bool SalePriceColumnEvaluate (int row, string price)
        {
            operation.Details [row].OriginalPriceOutEvaluate (Currency.ParseExpression (price));
            return true;
        }

        private void SalePriceColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditGridCell (args.Cell.Row, args.Cell.Column);
        }

        private void SalePriceColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            GridNavigator.ColumnKeyPress (args, colSalePrice.Index,
                                          SalePriceColumnEvaluate, SalePriceColumnEditPrev, SalePriceColumnEditNext);
        }

        private void SalePriceColumnEditPrev (int row, Key keyCode)
        {
            GridNavigator.EditPrev (row, keyCode, colPurchasePrice, PurchaseValueColumnEditPrev);
        }

        private void SalePriceColumnEditNext (int row, Key keyCode)
        {
            GridNavigator.EditNext (row, keyCode, colDiscount, DiscountColumnEditNext);
        }

        #endregion

        #region Discount column handling

        protected Column colDiscount;

        protected bool DiscountColumnEvaluate (int row, string discount)
        {
            DiscountColumnEvaluate (row, Percent.ParseExpression (discount));
            return true;
        }

        protected virtual void DiscountColumnEvaluate (int row, double discountPercent)
        {
            operation.Details [row].DiscountEvaluate (discountPercent);
        }

        protected void DiscountColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditGridCell (args.Cell.Row, args.Cell.Column);
        }

        protected void DiscountColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            GridNavigator.ColumnKeyPress (args, colDiscount.Index,
                                          DiscountColumnEvaluate, DiscountColumnEditPrev, DiscountColumnEditNext);
        }

        protected virtual void DiscountColumnEditPrev (int row, Key keyCode)
        {
            GridNavigator.EditPrev (row, keyCode, colSalePrice, SalePriceColumnEditPrev);
        }

        protected virtual void DiscountColumnEditNext (int row, Key keyCode)
        {
            GridNavigator.EditNext (row, keyCode, colDiscountValue, DiscountValueColumnEditNext);
        }

        #endregion

        #region Discount value column handling

        protected Column colDiscountValue;

        protected bool DiscountValueColumnEvaluate (int row, string discount)
        {
            DiscountValueColumnEvaluate (row, Currency.ParseExpression (discount, operation.TotalsPriceType));
            return true;
        }

        protected virtual void DiscountValueColumnEvaluate (int row, double discountValue)
        {
            operation.Details [row].DiscountValueEvaluate (discountValue);
        }

        protected void DiscountValueColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditGridCell (args.Cell.Row, args.Cell.Column);
        }

        protected void DiscountValueColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            GridNavigator.ColumnKeyPress (args, colDiscountValue.Index,
                                          DiscountValueColumnEvaluate, DiscountValueColumnEditPrev, DiscountValueColumnEditNext);
        }

        protected virtual void DiscountValueColumnEditPrev (int row, Key keyCode)
        {
            GridNavigator.EditPrev (row, keyCode, colDiscount, DiscountColumnEditPrev);
        }

        protected virtual void DiscountValueColumnEditNext (int row, Key keyCode)
        {
            GridNavigator.EditNext (row, keyCode, colLot, LotColumnEditNext);
        }

        #endregion

        #region Lot column handling

        protected Column colLot;

        protected virtual bool LotColumnEvaluate (int row, string value)
        {
            operation.Details [row].Lot = string.IsNullOrWhiteSpace (value) ? "NA" : value;
            return true;
        }

        private void LotColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditGridCell (args.Cell.Row, args.Cell.Column);
        }

        private void LotColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            GridNavigator.ColumnKeyPress (args, colLot.Index,
                                          LotColumnEvaluate, LotColumnEditPrev, LotColumnEditNext);
        }

        private void LotColumnEditPrev (int row, Key keyCode)
        {
            GridNavigator.EditPrev (row, keyCode, colDiscountValue, DiscountValueColumnEditPrev);
        }

        private void LotColumnEditNext (int row, Key keyCode)
        {
            GridNavigator.EditNext (row, keyCode, colSerialNo, SerialNoColumnEditNext);
        }

        #endregion

        #region Serial number column handling

        protected Column colSerialNo;

        private bool SerialNoColumnEvaluate (int row, string value)
        {
            operation.Details [row].SerialNumber = value;
            return true;
        }

        private void SerialNoColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditGridCell (args.Cell.Row, args.Cell.Column);
        }

        private void SerialNoColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            GridNavigator.ColumnKeyPress (args, colSerialNo.Index,
                SerialNoColumnEvaluate, SerialNoColumnEditPrev, SerialNoColumnEditNext);
        }

        private void SerialNoColumnEditPrev (int row, Key keyCode)
        {
            GridNavigator.EditPrev (row, keyCode, colLot, LotColumnEditPrev);
        }

        private void SerialNoColumnEditNext (int row, Key keyCode)
        {
            GridNavigator.EditNext (row, keyCode, colExpirationDate, ExpirationDateColumnEditNext);
        }

        #endregion

        #region Expiration date column handling

        protected Column colExpirationDate;

        private bool ExpirationDateColumnEvaluate (int row, string value)
        {
            DateTime dateValue = BusinessDomain.GetDateValue (value);
            ExpirationDateColumnEvaluate (row, dateValue == DateTime.MinValue ? (DateTime?) null : dateValue);
            return string.IsNullOrWhiteSpace (value) || dateValue != DateTime.MinValue;
        }

        private void ExpirationDateColumnEvaluate (int row, DateTime? value)
        {
            operation.Details [row].ExpirationDate = value;
        }

        private void ExpirationDateColumnChoose (string filter)
        {
            DateTime d = BusinessDomain.GetDateValue (filter);

            using (ChooseDate chooseDate = new ChooseDate ()) {
                if (d != DateTime.MinValue)
                    chooseDate.Selection = d;

                if (chooseDate.Run () != ResponseType.Ok) {
                    EditGridField (grid.EditedCell.Row, colExpirationDate.Index);
                    return;
                }

                ExpirationDateColumnEvaluate (grid.EditedCell.Row, chooseDate.Selection);
                ExpirationDateColumnEditNext (grid.EditedCell.Row, Key.Return);
            }
        }

        private void ExpirationDateColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            if (!EditGridCell (args.Cell.Row, args.Cell.Column))
                return;

            if (grid.DisableEdit || args.EventButton.Type != EventType.TwoButtonPress)
                return;

            object cellValue = grid.EditedCellValue;
            GridNavigator.ChooseCellValue (ExpirationDateColumnEvaluate, ExpirationDateColumnChoose,
                cellValue == null ? string.Empty : cellValue.ToString ());
        }

        private void ExpirationDateColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            GridNavigator.ColumnKeyPress (args, colExpirationDate.Index, ExpirationDateColumnChoose,
                ExpirationDateColumnEvaluate, ExpirationDateColumnEditPrev, ExpirationDateColumnEditNext);
        }

        private void ExpirationDateColumnEditPrev (int row, Key keyCode)
        {
            GridNavigator.EditPrev (row, keyCode, colSerialNo, SerialNoColumnEditPrev);
        }

        private void ExpirationDateColumnEditNext (int row, Key keyCode)
        {
            GridNavigator.EditNext (row, keyCode, colProductionDate, ProductionDateColumnEditNext);
        }

        #endregion

        #region Production date column handling

        protected Column colProductionDate;

        private bool ProductionDateColumnEvaluate (int row, string value)
        {
            DateTime dateValue = BusinessDomain.GetDateValue (value);
            ProductionDateColumnEvaluate (row, dateValue == DateTime.MinValue ? (DateTime?) null : dateValue);
            return string.IsNullOrWhiteSpace (value) || dateValue != DateTime.MinValue;
        }

        private void ProductionDateColumnEvaluate (int row, DateTime? value)
        {
            operation.Details [row].ProductionDate = value;
        }

        private void ProductionDateColumnChoose (string filter)
        {
            DateTime d = BusinessDomain.GetDateValue (filter);

            using (ChooseDate chooseDate = new ChooseDate ()) {
                if (d != DateTime.MinValue)
                    chooseDate.Selection = d;

                if (chooseDate.Run () != ResponseType.Ok) {
                    EditGridField (grid.EditedCell.Row, colProductionDate.Index);
                    return;
                }

                ProductionDateColumnEvaluate (grid.EditedCell.Row, chooseDate.Selection);
                ProductionDateColumnEditNext (grid.EditedCell.Row, Key.Return);
            }
        }

        private void ProductionDateColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            if (!EditGridCell (args.Cell.Row, args.Cell.Column))
                return;

            if (grid.DisableEdit || args.EventButton.Type != EventType.TwoButtonPress)
                return;

            object cellValue = grid.EditedCellValue;
            GridNavigator.ChooseCellValue (ProductionDateColumnEvaluate, ProductionDateColumnChoose,
                cellValue == null ? string.Empty : cellValue.ToString ());
        }

        private void ProductionDateColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            GridNavigator.ColumnKeyPress (args, colProductionDate.Index, ProductionDateColumnChoose,
                ProductionDateColumnEvaluate, ProductionDateColumnEditPrev, ProductionDateColumnEditNext);
        }

        private void ProductionDateColumnEditPrev (int row, Key keyCode)
        {
            GridNavigator.EditPrev (row, keyCode, colExpirationDate, ExpirationDateColumnEditPrev);
        }

        private void ProductionDateColumnEditNext (int row, Key keyCode)
        {
            GridNavigator.EditNext (row, keyCode, colLotLocation, LotLocationColumnEditNext);
        }

        #endregion

        #region Lot location column handling

        protected Column colLotLocation;

        protected virtual bool LotLocationColumnEvaluate (int row, string value)
        {
            operation.Details [row].LotLocation = value;
            return true;
        }

        protected void LotLocationColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditGridCell (args.Cell.Row, args.Cell.Column);
        }

        protected void LotLocationColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            GridNavigator.ColumnKeyPress (args, colLotLocation.Index,
                LotLocationColumnEvaluate, LotLocationColumnEditPrev, LotLocationColumnEditNext);
        }

        protected virtual void LotLocationColumnEditPrev (int row, Key keyCode)
        {
            GridNavigator.EditPrev (row, keyCode, colProductionDate, ProductionDateColumnEditPrev);
        }

        protected virtual void LotLocationColumnEditNext (int row, Key keyCode)
        {
            GridNavigator.EditNext (row, keyCode, colVatRate, VatRateColumnEditNext);
        }

        #endregion

        #region Vat rate column handling

        protected Column colVatRate;

        private bool VatRateColumnEvaluate (int row, string price)
        {
            operation.Details [row].VatRate = Percent.ParseExpression (price);
            return true;
        }

        private void VatRateColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditGridCell (args.Cell.Row, args.Cell.Column);
        }

        private void VatRateColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            GridNavigator.ColumnKeyPress (args, colVatRate.Index,
                VatRateColumnEvaluate, VatRateColumnEditPrev, VatRateColumnEditNext);
        }

        private void VatRateColumnEditPrev (int row, Key keyCode)
        {
            GridNavigator.EditPrev (row, keyCode, colLotLocation, LotLocationColumnEditPrev);
        }

        private void VatRateColumnEditNext (int row, Key keyCode)
        {
            GridNavigator.EditNextOnLast (row, keyCode, colItem, ItemColumnEditNext, operation.Details, operation.AddNewDetail);
        }

        #endregion

        #region Total column handling

        protected Column colTotal;

        protected void UpdateOperationTotal ()
        {
            SetOperationTotal (operation);
        }

        protected void UpdateStatistics ()
        {
            if (!BusinessDomain.AppConfiguration.ShowOperationStatistics)
                return;

            Column column = colLineNumber ?? colItem;
            column.FooterText = operation.Details.Count (d => d.ItemId > 0).ToString (CultureInfo.InvariantCulture);
            colQuantity.FooterText = Quantity.ToString (operation.Details.Sum (d => d.Quantity));

            if (secondGrid == null)
                return;

            column = colSecondLineNumber ?? colSecondItem;
            column.FooterText = operation.AdditionalDetails.Count (d => d.ItemId > 0).ToString (CultureInfo.InvariantCulture);
            colSecondQuantity.FooterText = Quantity.ToString (operation.AdditionalDetails.Sum (d => d.Quantity));
        }

        #endregion

        protected virtual void LotsEvaluate (BindList<TOperDetail> detailsList, TOperDetail detail, bool forceChoice = false)
        {
            if (!BusinessDomain.AppConfiguration.ItemsManagementUseLots)
                return;

            if (detail.ItemId < 0)
                return;

            if (!detail.UsesSavedLots) {
                if (codeLot != null)
                    detail.Lot = codeLot;

                return;
            }

            if (detail.LotId > 0)
                return;

            if (!forceChoice && BusinessDomain.AppConfiguration.ItemsManagementType != ItemsManagementType.Choice) {
                operation.LotsEvaluate (detailsList, detail, codeLot);
                return;
            }

            Lot selectedLot;
            using (ChooseLot dialog = new ChooseLot (detail.ItemId, detail.ItemName, operation.LocationId)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                selectedLot = dialog.SelectedItem;
                if (selectedLot == null)
                    return;
            }

            double qttyToUse = Math.Min (selectedLot.AvailableQuantity, operation.GetDetailQuantityForLot (detail));
            operation.EvaluateQuantityForLot (detail, qttyToUse, qttyToUse);
            detail.LotEvaluate (selectedLot);
        }

        #endregion

        #region Second Grid column handling

        protected virtual bool EditSecondGridCell (int row, int column)
        {
            secondGrid.DisableEdit = false;

            if (OperationValidate ()) {
                if (row >= 0 && column >= 0)
                    return EditSecondGridField (row, column);

                return true;
            }

            secondGrid.DisableEdit = true;
            return false;
        }

        private void SecondGridColumnEditBelow (int column)
        {
            if (!secondGrid.EditedCell.IsValid)
                return;

            if (secondGrid.EditedCell.Row + 1 >= secondGrid.Model.Count)
                return;

            EditSecondGridField (secondGrid.EditedCell.Row + 1, column);
        }

        protected virtual void SecondGridColumnEditOver (int column)
        {
            if (!secondGrid.EditedCell.IsValid)
                return;

            if (secondGrid.EditedCell.Row <= 0)
                return;

            EditSecondGridField (secondGrid.EditedCell.Row - 1, column);
        }

        protected virtual void DeleteSecondGridRow (bool keepRowPos)
        {
            if (!secondGrid.EditedCell.IsValid)
                return;

            if (editMode) {
                CellPosition editPos = secondGrid.EditedCell;
                if (editPos.Column == colSecondQuantity.Index)
                    secondGrid.CancelCellEdit ();

                operation.AdditionalDetails [editPos.Row].Quantity = 0;

                EditSecondGridField (editPos.Row, editPos.Column);
                return;
            }

            if (operation.AdditionalDetails.Count > 1) {
                int col = secondGrid.EditedCell.Column;
                int row = secondGrid.EditedCell.Row;
                int newRow;

                if (row == operation.AdditionalDetails.Count - 1) {
                    // If we are deleting the last row move one row up
                    operation.RemoveAdditionalDetail (row);
                    newRow = row - 1;
                } else {
                    // If we are deleting row from somewhere in between stay on the same line
                    operation.RemoveAdditionalDetail (row);
                    newRow = row;
                }

                if (keepRowPos)
                    EditSecondGridField (newRow, col);
                else
                    SecondItemColumnEditPrev (newRow, Key.Left);
            } else {
                operation.ClearDetails ();
                operation.AddNewDetail ();

                EditSecondGridField (0, colSecondItem.Index);
            }
        }

        #region Line number column handling

        protected Column colSecondLineNumber;

        #endregion

        #region Item Code column handling

        protected Column colSecondItemCode;

        #endregion

        #region Item column handling

        protected Column colSecondItem;

        private bool SecondItemColumnEvaluate (int row, string itemName)
        {
            barcodeUsed = false;
            TOperDetail detail = operation.AdditionalDetails [row];
            if (detail.ItemId >= 0 && detail.ItemName == itemName)
                return true;

            double currentQuantity = detail.Quantity;

            long codeStoreId;
            Item item = Item.GetByAny (itemName, out barcodeUsed, out codeQtty, out codeLot, out codeStoreId, detail.UsesSavedLots ? operation.LocationId : (long?) null);

            if (!SecondItemColumnEvaluate (row, item, false))
                return false;

            // no quantity from the barcode scanner
            if (codeQtty.IsZero ())
                codeQtty = currentQuantity;

            if (detail.UsesSavedLots && codeStoreId >= 0) {
                Lot lot = Lot.GetByStoreId (codeStoreId);
                if (lot != null)
                    detail.LotEvaluate (lot);
            }

            return true;
        }

        protected virtual bool SecondItemColumnEvaluate (int row, Item item, bool updatePrice)
        {
            return operation.AdditionalDetails [row].ItemEvaluate (item, GetOperationPriceGroup (), updatePrice);
        }

        private void SecondItemColumnChoose (string filter)
        {
            Item [] items = null;
            if (BusinessDomain.AppConfiguration.ShowItemSuggestionsWhenNotFound)
                items = EntityMissing.ShowItemMissing (filter, operation);

            if (items == null) {
                if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditGoods") == UserRestrictionState.Allowed) {
                    using (ChooseEditItem dialog = new ChooseEditItem (AvailabilityLocationId, GetOperationPriceGroup (), filter))
                        if (dialog.Run () == ResponseType.Ok)
                            items = dialog.SelectedItems.ToArray ();
                } else
                    MessageError.ShowDialog (string.Format (Translator.GetString ("Item \"{0}\" cannot be found!"), filter));
            }

            barcodeUsed = false;
            codeQtty = 0;
            codeLot = null;

            int row = secondGrid.EditedCell.Row;
            bool itemAdded = false;

            if (items != null)
                foreach (Item item in items) {
                    if (row >= operation.AdditionalDetails.Count)
                        operation.AddNewAdditionalDetail ();

                    if (SecondItemColumnEvaluate (row, item, true)) {
                        row++;
                        itemAdded = true;
                    }
                }

            if (itemAdded)
                SecondItemColumnEditNext (secondGrid.EditedCell.Row, Key.Return);
            else
                EditSecondGridField (row, colSecondItem.Index);
        }

        protected void SecondItemColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            if (secondGrid.EditedCell != args.Cell) {
                CurrentColumnEvaluate ();
                EditSecondGridCell (args.Cell.Row, args.Cell.Column);
            } else {
                EditSecondGridCell (-1, -1);
            }

            if (secondGrid.DisableEdit || args.EventButton.Type != EventType.TwoButtonPress)
                return;

            object cellValue = secondGrid.EditedCellValue;
            SecondGridNavigator.ChooseCellValue (SecondItemColumnEvaluate, SecondItemColumnChoose,
                cellValue == null ? string.Empty : cellValue.ToString ());
        }

        protected void SecondItemColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            if (SecondGridNavigator.ColumnKeyPress (args, colSecondItem.Index, SecondItemColumnChoose,
                SecondItemColumnEvaluate, SecondItemColumnEditPrev, SecondItemColumnEditNext))
                return;

            string gdkKey = KeyShortcuts.KeyToString (args.EventKey);
            string quickGoods;
            if (!BusinessDomain.QuickItems.TryGetValue (gdkKey, out quickGoods))
                return;

            SecondItemColumnEvaluate (secondGrid.EditedCell.Row, quickGoods);

            int row = secondGrid.EditedCell.Row;
            SecondQtyColumnEvaluate (ref row, 1);

            if (operation.AdditionalDetails.Count <= row + 1)
                operation.AddNewAdditionalDetail ();

            EditSecondGridField (row + 1, colSecondItem.Index);

            args.MarkAsHandled ();
        }

        protected virtual void SecondItemColumnEditPrev (int row, Key keyCode)
        {
            SecondGridNavigator.EditPrevOnFirst (row, keyCode, colSecondVatRate, SecondVatRateColumnEditPrev,
                r => DeleteSecondGridRow (false));
        }

        protected virtual void SecondItemColumnEditNext (int row, Key keyCode)
        {
            if (!codeQtty.IsZero ())
                SecondQtyColumnEvaluate (ref row, codeQtty);

            if (barcodeUsed) {
                if (codeQtty.IsZero ())
                    SecondQtyColumnEvaluate (ref row, operation.AdditionalDetails [row].QuantityIncrement);

                if (operation.AdditionalDetails.Count <= row + 1)
                    operation.AddNewAdditionalDetail ();

                EditSecondGridField (row + 1, colSecondItem.Index);
            } else if (colSecondQuantity != null && colSecondQuantity.ListCell.IsEditable)
                EditSecondGridField (secondGrid.EditedCell.Row, colSecondQuantity.Index);
            else
                SecondQtyColumnEditNext (row, keyCode);
        }

        #endregion

        #region Measure column handling

        protected Column colSecondMeasure;

        #endregion

        #region Quantity column handling

        protected Column colSecondQuantity;

        protected bool SecondQtyColumnEvaluate (int row, string quantity)
        {
            return SecondQtyColumnEvaluate (ref row, Quantity.ParseExpression (quantity));
        }

        protected virtual bool SecondQtyColumnEvaluate (ref int row, double qtyValue)
        {
            if (!CheckQuantityLimitations (qtyValue, row, secondGrid))
                return false;
            TOperDetail detail = operation.AdditionalDetails [row];
            detail.Quantity = qtyValue;
            LotsEvaluate (operation.AdditionalDetails, detail);
            return true;
        }

        protected void SecondQtyColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditSecondGridCell (args.Cell.Row, args.Cell.Column);
        }

        protected void SecondQtyColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            SecondGridNavigator.ColumnKeyPress (args, colSecondQuantity.Index,
                                                SecondQtyColumnEvaluate, SecondQtyColumnEditPrev, SecondQtyColumnEditNext);
        }

        protected virtual void SecondQtyColumnEditPrev (int row, Key keyCode)
        {
            SecondGridNavigator.EditPrev (row, keyCode, colSecondItem, SecondItemColumnEditPrev);
        }

        protected virtual void SecondQtyColumnEditNext (int row, Key keyCode)
        {
            SecondGridNavigator.EditNext (row, keyCode, colSecondPurchaseValue, SecondPurchaseValueColumnEditNext);
        }

        #endregion

        #region Purchase value column handling

        protected Column colSecondPurchaseValue;

        private bool SecondPurchaseValueColumnEvaluate (int row, string price)
        {
            operation.AdditionalDetails [row].OriginalPriceInEvaluate (Currency.ParseExpression (price, PriceType.Purchase), operation);
            return true;
        }

        protected void SecondPurchaseValueColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditSecondGridCell (args.Cell.Row, args.Cell.Column);
        }

        protected void SecondPurchaseValueColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            SecondGridNavigator.ColumnKeyPress (args, colPurchasePrice.Index,
                SecondPurchaseValueColumnEvaluate, SecondPurchaseValueColumnEditPrev, SecondPurchaseValueColumnEditNext);
        }

        protected virtual void SecondPurchaseValueColumnEditPrev (int row, Key keyCode)
        {
            SecondGridNavigator.EditPrev (row, keyCode, colQuantity, SecondQtyColumnEditPrev);
        }

        protected virtual void SecondPurchaseValueColumnEditNext (int row, Key keyCode)
        {
            SecondGridNavigator.EditNext (row, keyCode, colSalePrice, SecondSalePriceColumnEditNext);
        }

        #endregion

        #region Sale price column handling

        protected Column colSecondSalePrice;

        protected bool SecondSalePriceColumnEvaluate (int row, string price)
        {
            operation.AdditionalDetails [row].OriginalPriceOutEvaluate (Currency.ParseExpression (price));
            return true;
        }

        protected void SecondSalePriceColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditSecondGridCell (args.Cell.Row, args.Cell.Column);
        }

        protected void SecondSalePriceColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            SecondGridNavigator.ColumnKeyPress (args, colSalePrice.Index,
                SecondSalePriceColumnEvaluate, SecondSalePriceColumnEditPrev, SecondSalePriceColumnEditNext);
        }

        protected virtual void SecondSalePriceColumnEditPrev (int row, Key keyCode)
        {
            SecondGridNavigator.EditPrev (row, keyCode, colPurchasePrice, SecondPurchaseValueColumnEditPrev);
        }

        protected virtual void SecondSalePriceColumnEditNext (int row, Key keyCode)
        {
            SecondGridNavigator.EditNext (row, keyCode, colDiscount, SecondDiscountColumnEditNext);
        }

        #endregion

        #region Discount column handling

        protected Column colSecondDiscount;

        protected bool SecondDiscountColumnEvaluate (int row, string discount)
        {
            SecondDiscountColumnEvaluate (row, Percent.ParseExpression (discount));
            return true;
        }

        protected void SecondDiscountColumnEvaluate (int row, double discountPercent)
        {
            operation.AdditionalDetails [row].DiscountEvaluate (discountPercent);
        }

        protected void SecondDiscountColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditSecondGridCell (args.Cell.Row, args.Cell.Column);
        }

        protected void SecondDiscountColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            SecondGridNavigator.ColumnKeyPress (args, colSecondDiscount.Index,
                SecondDiscountColumnEvaluate, SecondDiscountColumnEditPrev, SecondDiscountColumnEditNext);
        }

        protected virtual void SecondDiscountColumnEditPrev (int row, Key keyCode)
        {
            SecondGridNavigator.EditPrev (row, keyCode, colSecondSalePrice, SecondSalePriceColumnEditPrev);
        }

        protected virtual void SecondDiscountColumnEditNext (int row, Key keyCode)
        {
            SecondGridNavigator.EditNext (row, keyCode, colSecondDiscountValue, SecondDiscountValueColumnEditNext);
        }

        #endregion

        #region Discount value column handling

        protected Column colSecondDiscountValue;

        protected bool SecondDiscountValueColumnEvaluate (int row, string discount)
        {
            SecondDiscountValueColumnEvaluate (row, Currency.ParseExpression (discount, operation.TotalsPriceType));
            return true;
        }

        protected void SecondDiscountValueColumnEvaluate (int row, double discountValue)
        {
            operation.AdditionalDetails [row].DiscountValueEvaluate (discountValue);
        }

        protected void SecondDiscountValueColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditSecondGridCell (args.Cell.Row, args.Cell.Column);
        }

        protected void SecondDiscountValueColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            SecondGridNavigator.ColumnKeyPress (args, colSecondDiscountValue.Index,
                SecondDiscountValueColumnEvaluate, SecondDiscountValueColumnEditPrev, SecondDiscountValueColumnEditNext);
        }

        protected void SecondDiscountValueColumnEditPrev (int row, Key keyCode)
        {
            SecondGridNavigator.EditPrev (row, keyCode, colSecondDiscount, SecondDiscountColumnEditPrev);
        }

        protected void SecondDiscountValueColumnEditNext (int row, Key keyCode)
        {
            SecondGridNavigator.EditNext (row, keyCode, colSecondLot, SecondLotColumnEditNext);
        }

        #endregion

        #region Lot column handling

        protected Column colSecondLot;

        protected bool SecondLotColumnEvaluate (int row, string value)
        {
            operation.AdditionalDetails [row].Lot = value;
            return true;
        }

        protected void SecondLotColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditSecondGridCell (args.Cell.Row, args.Cell.Column);
        }

        protected void SecondLotColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            SecondGridNavigator.ColumnKeyPress (args, colSecondLot.Index,
                                                SecondLotColumnEvaluate, SecondLotColumnEditPrev, SecondLotColumnEditNext);
        }

        protected virtual void SecondLotColumnEditPrev (int row, Key keyCode)
        {
            SecondGridNavigator.EditPrev (row, keyCode, colSecondSalePrice, SecondDiscountValueColumnEditPrev);
        }

        protected virtual void SecondLotColumnEditNext (int row, Key keyCode)
        {
            SecondGridNavigator.EditNext (row, keyCode, colSecondSerialNo, SecondSerialNoColumnEditNext);
        }

        #endregion

        #region Serial number column handling

        protected Column colSecondSerialNo;

        private bool SecondSerialNoColumnEvaluate (int row, string value)
        {
            operation.AdditionalDetails [row].SerialNumber = value;
            return true;
        }

        protected void SecondSerialNoColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditSecondGridCell (args.Cell.Row, args.Cell.Column);
        }

        protected void SecondSerialNoColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            SecondGridNavigator.ColumnKeyPress (args, colSecondSerialNo.Index,
                SecondSerialNoColumnEvaluate, SecondSerialNoColumnEditPrev, SecondSerialNoColumnEditNext);
        }

        protected virtual void SecondSerialNoColumnEditPrev (int row, Key keyCode)
        {
            SecondGridNavigator.EditPrev (row, keyCode, colSecondLot, SecondLotColumnEditPrev);
        }

        private void SecondSerialNoColumnEditNext (int row, Key keyCode)
        {
            SecondGridNavigator.EditNext (row, keyCode, colSecondExpirationDate, SecondExpirationDateColumnEditNext);
        }

        #endregion

        #region Expiration date column handling

        protected Column colSecondExpirationDate;

        private bool SecondExpirationDateColumnEvaluate (int row, string value)
        {
            DateTime dateValue = BusinessDomain.GetDateValue (value);
            SecondExpirationDateColumnEvaluate (row, dateValue == DateTime.MinValue ? (DateTime?) null : dateValue);
            return string.IsNullOrWhiteSpace (value) || dateValue != DateTime.MinValue;
        }

        private void SecondExpirationDateColumnEvaluate (int row, DateTime? value)
        {
            operation.AdditionalDetails [row].ExpirationDate = value;
        }

        private void SecondExpirationDateColumnChoose (string filter)
        {
            DateTime d = BusinessDomain.GetDateValue (filter);

            using (ChooseDate chooseDate = new ChooseDate ()) {
                if (d != DateTime.MinValue)
                    chooseDate.Selection = d;

                if (chooseDate.Run () != ResponseType.Ok) {
                    EditSecondGridField (secondGrid.EditedCell.Row, colSecondExpirationDate.Index);
                    return;
                }

                SecondExpirationDateColumnEvaluate (secondGrid.EditedCell.Row, chooseDate.Selection);
                SecondExpirationDateColumnEditNext (secondGrid.EditedCell.Row, Key.Return);
            }
        }

        protected void SecondExpirationDateColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            if (!EditSecondGridCell (args.Cell.Row, args.Cell.Column))
                return;

            if (secondGrid.DisableEdit || args.EventButton.Type != EventType.TwoButtonPress)
                return;

            object cellValue = secondGrid.EditedCellValue;
            SecondGridNavigator.ChooseCellValue (SecondExpirationDateColumnEvaluate, SecondExpirationDateColumnChoose,
                cellValue == null ? string.Empty : cellValue.ToString ());
        }

        protected void SecondExpirationDateColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            SecondGridNavigator.ColumnKeyPress (args, colSecondExpirationDate.Index, SecondExpirationDateColumnChoose,
                SecondExpirationDateColumnEvaluate, SecondExpirationDateColumnEditPrev, SecondExpirationDateColumnEditNext);
        }

        protected virtual void SecondExpirationDateColumnEditPrev (int row, Key keyCode)
        {
            SecondGridNavigator.EditPrev (row, keyCode, colSecondSerialNo, SecondSerialNoColumnEditPrev);
        }

        private void SecondExpirationDateColumnEditNext (int row, Key keyCode)
        {
            SecondGridNavigator.EditNext (row, keyCode, colSecondProductionDate, SecondProductionDateColumnEditNext);
        }

        #endregion

        #region Production date column handling

        protected Column colSecondProductionDate;

        private bool SecondProductionDateColumnEvaluate (int row, string value)
        {
            DateTime dateValue = BusinessDomain.GetDateValue (value);
            SecondProductionDateColumnEvaluate (row, dateValue == DateTime.MinValue ? (DateTime?) null : dateValue);
            return string.IsNullOrWhiteSpace (value) || dateValue != DateTime.MinValue;
        }

        private void SecondProductionDateColumnEvaluate (int row, DateTime? value)
        {
            operation.AdditionalDetails [row].ProductionDate = value;
        }

        private void SecondProductionDateColumnChoose (string filter)
        {
            DateTime d = BusinessDomain.GetDateValue (filter);

            using (ChooseDate chooseDate = new ChooseDate ()) {
                if (d != DateTime.MinValue)
                    chooseDate.Selection = d;

                if (chooseDate.Run () != ResponseType.Ok) {
                    EditSecondGridField (secondGrid.EditedCell.Row, colSecondProductionDate.Index);
                    return;
                }

                SecondProductionDateColumnEvaluate (secondGrid.EditedCell.Row, chooseDate.Selection);
                SecondProductionDateColumnEditNext (secondGrid.EditedCell.Row, Key.Return);
            }
        }

        protected void SecondProductionDateColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            if (!EditSecondGridCell (args.Cell.Row, args.Cell.Column))
                return;

            if (secondGrid.DisableEdit || args.EventButton.Type != EventType.TwoButtonPress)
                return;

            object cellValue = secondGrid.EditedCellValue;
            SecondGridNavigator.ChooseCellValue (SecondProductionDateColumnEvaluate, SecondProductionDateColumnChoose,
                cellValue == null ? string.Empty : cellValue.ToString ());
        }

        protected void SecondProductionDateColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            SecondGridNavigator.ColumnKeyPress (args, colSecondProductionDate.Index, SecondProductionDateColumnChoose,
                SecondProductionDateColumnEvaluate, SecondProductionDateColumnEditPrev, SecondProductionDateColumnEditNext);
        }

        protected virtual void SecondProductionDateColumnEditPrev (int row, Key keyCode)
        {
            SecondGridNavigator.EditPrev (row, keyCode, colSecondExpirationDate, SecondExpirationDateColumnEditPrev);
        }

        private void SecondProductionDateColumnEditNext (int row, Key keyCode)
        {
            SecondGridNavigator.EditNext (row, keyCode, colSecondLotLocation, SecondLotLocationColumnEditNext);
        }

        #endregion

        #region Lot location column handling

        protected Column colSecondLotLocation;

        private bool SecondLotLocationColumnEvaluate (int row, string value)
        {
            operation.AdditionalDetails [row].LotLocation = value;
            return true;
        }

        protected void SecondLotLocationColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditSecondGridCell (args.Cell.Row, args.Cell.Column);
        }

        protected void SecondLotLocationColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            SecondGridNavigator.ColumnKeyPress (args, colSecondLotLocation.Index,
                SecondLotLocationColumnEvaluate, SecondLotLocationColumnEditPrev, SecondLotLocationColumnEditNext);
        }

        protected virtual void SecondLotLocationColumnEditPrev (int row, Key keyCode)
        {
            SecondGridNavigator.EditPrev (row, keyCode, colSecondProductionDate, SecondProductionDateColumnEditPrev);
        }

        private void SecondLotLocationColumnEditNext (int row, Key keyCode)
        {
            SecondGridNavigator.EditNextOnLast (row, keyCode, colSecondItem, SecondItemColumnEditNext, operation.AdditionalDetails, operation.AddNewAdditionalDetail);
        }

        #endregion

        #region Vat rate column handling

        protected Column colSecondVatRate;

        private bool SecondVatRateColumnEvaluate (int row, string price)
        {
            operation.AdditionalDetails [row].VatRate = Percent.ParseExpression (price);
            return true;
        }

        protected void SecondVatRateColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditSecondGridCell (args.Cell.Row, args.Cell.Column);
        }

        protected void SecondVatRateColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            SecondGridNavigator.ColumnKeyPress (args, colSecondVatRate.Index,
                SecondVatRateColumnEvaluate, SecondVatRateColumnEditPrev, SecondVatRateColumnEditNext);
        }

        private void SecondVatRateColumnEditPrev (int row, Key keyCode)
        {
            SecondGridNavigator.EditPrev (row, keyCode, colSecondLotLocation, SecondLotLocationColumnEditPrev);
        }

        private void SecondVatRateColumnEditNext (int row, Key keyCode)
        {
            SecondGridNavigator.EditNextOnLast (row, keyCode, colSecondItem, SecondItemColumnEditNext, operation.AdditionalDetails, operation.AddNewAdditionalDetail);
        }

        #endregion

        #region Total column handling

        protected Column colSecondTotal;

        #endregion

        #endregion

        protected void ClearDetailsFromPriceRules (bool selectLastItem = false)
        {
            PriceRule.RemoveAddedDetails (operation);
            PriceRule.RollbackOnOperationSaveFailed (operation);

            if (!selectLastItem)
                return;

            operation.AddNewDetail ();
            EditGridField (operation.Details.Count - 1, colItem.Index);
        }

        protected override void OnPageCloseRequested (object sender, WorkBookPageCloseArgs args)
        {
            try {
                CancelEditing ();

                pageCloseRequested = true;

                if (OperationDetailsValidate (false)) {
                    if (!editMode || operation.IsDirty) {
                        Message dialog = editMode ? GetAskSaveEditDialog () : GetAskSaveNewDialog ();
                        if (CanSaveDrafts) {
                            dialog.SetButtonText (MessageButtons.Yes, Translator.GetString ("Save draft"));
                            dialog.SetButtonImage (MessageButtons.Yes, FormHelper.LoadImage ("Icons.Save24.png"));
                            dialog.Buttons |= MessageButtons.Yes;
                        }

                        using (dialog) {
                            switch (dialog.Run ()) {
                                case ResponseType.Ok:
                                    args.Canceled = false;
                                    break;

                                case ResponseType.Yes:
                                    if (operation.State != OperationState.Draft)
                                        operation.SetState (OperationState.NewDraft);

                                    CommitOperation ();
                                    break;

                                default:
                                    args.Canceled = true;
                                    break;
                            }
                        }
                    }
                }

                if (args.Canceled)
                    return;

                operation.LogChanges (true);
                if (operation.State == OperationState.New &&
                    BusinessDomain.AppConfiguration.LogAllChangesInOperations &&
                    (operation.TotalPlusVAT > 0 || operation.HasRemovedDetails))
                    ApplicationLogEntry.AddNew (string.Format (Translator.GetString ("{0} (total: {1}, with VAT: {2}) - operation cancelled."),
                        Translator.GetOperationTypeGlobalName (operation.OperationType),
                        operation.Total,
                        operation.TotalPlusVAT));
            } finally {
                if (!args.Canceled)
                    PresentationDomain.CardRecognized -= PresentationDomain_CardRecognized;

                pageCloseRequested = false;
            }
        }

        protected abstract Message GetAskSaveEditDialog ();

        protected abstract Message GetAskSaveNewDialog ();

        protected virtual bool OperationDetailsValidate (bool showWarning)
        {
            bool result = OperationDetailsValidate (operation.Details, showWarning);

            operation.LotsEvaluate (operation.Details);
            operation.LotsEvaluate (operation.AdditionalDetails);

            if (result)
                CancelEditing ();
            return result;
        }

        private bool OperationDetailsValidate (BindList<TOperDetail> details, bool showWarning)
        {
            if (details.Count == 0)
                return false;

            bool currentColumnEvaluate = CurrentColumnEvaluate ();

            for (int i = details.Count - 1; i >= 0; i--) {
                try {
                    // Validate Item
                    string itemName = details [i].ItemName;
                    // If the gooods field is empty then this line has to be skipped
                    if (itemName.Length == 0) {
                        // If this is not the first line then delete it
                        if (i > 0) {
                            if ((grid != null && grid.EditedCell.IsValid && grid.EditedCell.Row == i) ||
                                (secondGrid != null && secondGrid.EditedCell.IsValid && secondGrid.EditedCell.Row == i))
                                currentColumnEvaluate = true;
                            operation.RemoveDetail (i, false);
                            continue;
                        }

                        OperationDetailValidationWarning (GetNoValidRowsWarning (), showWarning);
                        EditGridField (0, colItem.Index);
                        return false;
                    }

                    Item item = Item.GetById (details [i].ItemId);
                    if (item == null || item.Name != itemName) {
                        OperationDetailValidationWarning (string.Format (Translator.GetString ("Invalid item at row {0}!"), i + 1), showWarning);
                        EditGridField (i, colItem.Index);
                        return false;
                    }

                    // Validate measuring unit
                    string mUnit = details [i].MUnitName;
                    if (mUnit != item.MUnit) {
                        OperationDetailValidationWarning (string.Format (Translator.GetString ("Invalid measure of item \"{0}\"!"), item.Name), showWarning);
                        EditGridField (i, colItem.Index);
                        return false;
                    }

                    // Validate quantity
                    if (!ValidateQuantity (item, details, i, showWarning))
                        return false;

                    if (!ValidateLotQuantity (item, details, i, showWarning))
                        return false;

                    // Validate cost of purchase
                    if (!ValidatePurchaseValue (item, i, showWarning))
                        return false;

                    // Validate sale price
                    if (!ValidateSalePrice (item, i, showWarning))
                        return false;
                } catch {
                    OperationDetailValidationWarning (string.Format (Translator.GetString ("Error at row {0}!"), i + 1), showWarning);
                    EditGridField (i, colItem.Index);
                    return false;
                }
            }

            TOperDetail detail = details.FirstOrDefault (d => d.InsufficientQuantity);
            if (detail != null) {
                if (!pageCloseRequested) {
                    MessageError.ShowDialog (string.Format (GetInsufficientAvailabilityWarning (), detail.ItemName));
                    EditGridField (operation.Details.IndexOf (detail), colQuantity.Index);
                    return false;
                }

                // If we are closing the page signal for invalid details only if all are invalid
                return !details.All (d => d.InsufficientQuantity);
            }

            return currentColumnEvaluate;
        }

        protected abstract string GetNoValidRowsWarning ();

        private bool ValidateQuantity (Item item, BindList<TOperDetail> details, int i, bool showWarning)
        {
            if (details [i].ValidateQuantity ())
                return true;

            OperationDetailValidationWarning (string.Format (Translator.GetString ("Invalid quantity of item \"{0}\"!"), item.Name), showWarning);
            EditGridField (i, colQuantity.Index);
            return false;
        }

        protected virtual bool ValidateLotQuantity (Item item, BindList<TOperDetail> details, int i, bool showWarning)
        {
            if (!BusinessDomain.AppConfiguration.AutoProduction && BusinessDomain.AppConfiguration.ItemsManagementUseLots && details [i].LotId <= 0) {
                OperationDetailValidationWarning (string.Format (GetInsufficientAvailabilityWarning (), item.Name), showWarning);
                EditGridField (i, colQuantity.Index);
                return false;
            }
            return true;
        }

        protected abstract string GetInsufficientAvailabilityWarning ();

        protected virtual bool ValidatePurchaseValue (Item item, int i, bool showWarning)
        {
            if (colPurchasePrice == null)
                return true;

            double price = operation.Details [i].OriginalPriceIn;
            bool error = false;
            if (price < 0) {
                OperationDetailValidationWarning (string.Format (Translator.GetString ("Invalid purchase price for item \"{0}\"!"), item.Name), showWarning);
                error = true;
            }
            if (price.IsZero () && !BusinessDomain.LoggedUser.AllowZeroPrices) {
                OperationDetailValidationWarning (string.Format (Translator.GetString ("The purchase price of item \"{0}\" is zero. You are not allowed to use zero prices! Please contact the administrator for more information."), item.Name), showWarning);
                error = true;
            }

            if (error) {
                EditGridField (i, colPurchasePrice.Index);
                return false;
            }
            return true;
        }

        protected virtual bool ValidateSalePrice (Item item, int i, bool showWarning)
        {
            if (colSalePrice == null)
                return true;

            double price = operation.Details [i].OriginalPriceOut;
            bool error = false;
            if (price < 0) {
                OperationDetailValidationWarning (string.Format (Translator.GetString ("Invalid sale price of item \"{0}\"!"), item.Name), showWarning);
                error = true;
            }
            if (price.IsZero () && !BusinessDomain.LoggedUser.AllowZeroPrices) {
                OperationDetailValidationWarning (string.Format (Translator.GetString ("The sale price of item \"{0}\" is zero. You are not allowed to use zero prices! Please contact the administrator for more information."), item.Name), showWarning);
                error = true;
            }

            if (error) {
                EditGridField (i, colSalePrice.ListCell.IsEditable ? colSalePrice.Index : colItem.Index);
                return false;
            }
            return true;
        }

        protected void OperationDetailValidationWarning (string message, bool showMessage)
        {
            if (!showMessage)
                return;

            MessageError.ShowDialog (message);
        }

        protected ResponseType OperationDetailValidationQuestion (string message, bool showMessage)
        {
            if (!showMessage)
                return ResponseType.Yes;

            return MessageError.ShowDialog (message, buttons: MessageButtons.YesNo);
        }

        /// <summary>
        /// Checks the limitations, if any, of the specified quantity which was entered in the specified row of the specified grid.
        /// </summary>
        /// <param name="quantity">The quantity to check.</param>
        /// <param name="row">The index of the row in which the quantity is entered.</param>
        /// <param name="editedGrid">The grid in one of which rows the quantity is entered.</param>
        /// <returns><c>true</c> if the quantity does not exceed the limitations; otherwise, <c>false</c>.</returns>
        protected bool CheckQuantityLimitations (double quantity, int row, ListView editedGrid)
        {
            // prevent the dialogue from popping if the page is being closed because it's annoying otherwise
            if (pageCloseRequested)
                return false;

            bool result = true;
            if (BusinessDomain.AppConfiguration.MaximumAllowedQuantity > 0 &&
                quantity > BusinessDomain.AppConfiguration.MaximumAllowedQuantity) {
                MessageError.ShowDialog (string.Format (Translator.GetString ("The maximum allowed quantity is {0}."), BusinessDomain.AppConfiguration.MaximumAllowedQuantity),
                    ErrorSeverity.Error);
                result = false;
            } else if (BusinessDomain.AppConfiguration.WarningMaximumQuantity > 0 &&
                quantity > BusinessDomain.AppConfiguration.WarningMaximumQuantity) {
                result = MessageError.ShowDialog (
                    string.Format (Translator.GetString ("The entered quantity is larger than {0}. Are you sure you want to continue?"), BusinessDomain.AppConfiguration.WarningMaximumQuantity),
                    buttons: MessageButtons.YesNo) == ResponseType.Yes;
            }
            if (!result) {
                object editedCellValue = editedGrid.EditedCellValue;
                if (editedGrid == grid)
                    EditGridCell (row, colQuantity.Index);
                else
                    EditSecondGridCell (row, colSecondQuantity.Index);
                editedGrid.EditedCellValue = editedCellValue;
            }
            return result;
        }

        protected void OnCellKeyPress (object sender, CellKeyPressEventArgs args, bool isFirstGrid,
            IList<TOperDetail> operationDetails)
        {
            if (!KeyShortcuts.IsScreenModifierControl (args.EventKey.State))
                return;

            if (args.EventKey.Key == KeyShortcuts.DeleteKey.Key) {
                if (isFirstGrid)
                    DeleteGridRow (true);
                else
                    DeleteSecondGridRow (true);
                // Mark the event as handled so the Entry does not decide to delete evrything from itself
                args.MarkAsHandled ();
                return;
            }

            switch (args.EventKey.Key) {
                case Key.F:
                case Key.f:
                case Key.Cyrillic_EF:
                case Key.Cyrillic_ef:
                    if (args.Editing) {
                        args.MarkAsHandled ();
                        double originalPriceOut = operationDetails [args.Cell.Row].PriceOut;
                        double quantity = 1;
                        HardwareErrorResponse res;
                        do {
                            res.Retry = false;
                            try {
                                quantity = BusinessDomain.DeviceManager.GetWeightFromElectronicScale (originalPriceOut);
                            } catch (HardwareErrorException ex) {
                                res = FormHelper.HandleHardwareError (ex);
                                if (!res.Retry)
                                    return;
                            }
                        } while (res.Retry);

                        int row = args.Cell.Row;
                        if (isFirstGrid)
                            QtyColumnEvaluate (ref row, quantity);
                        else
                            SecondQtyColumnEvaluate (ref row, quantity);

                        string editedValue = args.Entry.Text;
                        ListView activeGrid = (ListView) sender;
                        // make sure we get the focus back after a "Retry" dialog is closed
                        activeGrid.BeginCellEdit (new CellEventArgs (args.Cell.Column, row));
                        args.Entry.Text = editedValue;
                        if (args.Cell.Column == colQuantity.Index)
                            if (isFirstGrid)
                                QtyColumnEditNext (row, args.GdkKey);
                            else
                                SecondQtyColumnEditNext (row, args.GdkKey);
                    }
                    break;
            }
        }

        private void btnSave_Clicked (object sender, EventArgs e)
        {
            if (operation.State == OperationState.NewDraft ||
                operation.State == OperationState.Draft)
                operation.SetState (OperationState.New);

            OnOperationSave (true);
        }

        protected List<Payment> GetAllNewPayments (bool printPayment)
        {
            List<Payment> newPayments = new List<Payment> ();
            if (printPayment)
                newPayments.AddRange (operation.Payments.Where (p => p.Id < 0).Select (p => (Payment) p.Clone ()));

            return newPayments;
        }

        protected void PrintAllNewPayments (List<Payment> newPayments)
        {
            foreach (PaymentReceipt paymentReceipt in newPayments.Select (p => new PaymentReceipt (p))) {
                if (!WaitForPendingOperationCompletion ())
                    return;

                FormHelper.PrintPreviewObject (paymentReceipt);
            }
        }

        protected virtual void btnClear_Clicked (object sender, EventArgs e)
        {
            if (!AskForOperationClear ())
                return;

            if (editMode) {
                CellPosition editPos = grid.EditedCell;
                if (editPos.IsValid && editPos.Column == colQuantity.Index)
                    grid.CancelCellEdit ();

                operation.ClearDetails ();

                if (editPos.IsValid)
                    EditGridField (editPos.Row, editPos.Column);
            } else {
                operation.ClearDetails ();
                operation.AddNewDetail ();

                EditGridField (0, colItem.Index);
                OperationTotalHide ();
            }
        }

        protected bool AskForOperationClear ()
        {
            if (!OperationValidate ())
                return false;

            return Message.ShowDialog (
                Translator.GetString ("Clear the operation?"), string.Empty,
                Translator.GetString ("Do you want to delete all the rows from the operation?"), "Icons.Question32.png",
                MessageButtons.YesNo) == ResponseType.Yes;
        }

        private void btnClose_Clicked (object o, EventArgs args)
        {
            RequestClose ();
        }

        private void btnAddDiscount_Clicked (object sender, EventArgs e)
        {
            CellPosition activeCell = grid.EditedCell;
            CellPosition secondActiveCell = secondGrid != null ? secondGrid.EditedCell : CellPosition.Empty;

            CurrentColumnEvaluate ();
            CancelEditing ();

            try {
                double originalTotal = operation.Details.Sum (detail => operation.BaseDiscountOnPricePlusVAT ? detail.OriginalTotalPlusVAT : detail.OriginalTotal);
                if (originalTotal.IsZero ()) {
                    MessageError.ShowDialog (Translator.GetString ("There are no valid items to apply the discount to!"));
                    return;
                }

                using (AddDiscount dialog = new AddDiscount ()) {
                    if (dialog.Run () != ResponseType.Ok)
                        return;

                    double discount = 0;
                    if (dialog.PercentDiscount.HasValue)
                        discount = dialog.PercentDiscount.Value * originalTotal / 100;
                    else if (dialog.ValueDiscount.HasValue)
                        discount = dialog.ValueDiscount.Value;

                    double [] discountValues = operation.CalculateDistributedDiscount (ref discount);

                    if (!discount.IsZero ()) {
                        if (Message.ShowDialog (
                            Translator.GetString ("Warning!"), null,
                            Translator.GetString ("Unable to apply the full discount because the discount will go out of valid boundaries. Do you want to apply the maximum possible discount?"), "Icons.Warning32.png",
                            MessageButtons.YesNo) != ResponseType.Yes)
                            return;
                    }

                    operation.ApplyDistributedDiscount (discountValues);
                }
            } finally {
                if (activeCell.IsValid)
                    EditGridCell (activeCell.Row, activeCell.Column);
                else if (secondActiveCell.IsValid)
                    EditSecondGridCell (secondActiveCell.Row, secondActiveCell.Column);
            }
        }

        private void btnAddRemoveVat_Clicked (object sender, EventArgs e)
        {
            CellPosition activeCell = grid.EditedCell;
            CellPosition secondActiveCell = secondGrid != null ? secondGrid.EditedCell : CellPosition.Empty;

            CurrentColumnEvaluate ();
            CancelEditing ();

            vatIncludedInPrices = !vatIncludedInPrices;

            if (vatIncludedInPrices)
                operation.SubtractVAT ();
            else
                operation.AddVAT ();

            SetOperationTotal (operation);
            UpdateAddRemoveVatText ();
            BusinessDomain.FeedbackProvider.TrackEvent ("Button", "Add/Remove VAT");

            if (activeCell.IsValid)
                EditGridCell (activeCell.Row, activeCell.Column);
            else if (secondActiveCell.IsValid)
                EditSecondGridCell (secondActiveCell.Row, secondActiveCell.Column);
        }

        private void btnImport_Clicked (object sender, EventArgs e)
        {
            if (!OperationValidate ()) {
                MessageError.ShowDialog (Translator.GetString ("Please fill in the header details before importing details in the operation."));
                OperationValidate ();
                return;
            }

            bool usePrimaryDetails = cursorAtField != FormFields.SecondGridDetails;
            BindList<TOperDetail> details = usePrimaryDetails ?
                operation.Details :
                operation.AdditionalDetails;

            CancelEditing ();

            if (details.Count > 0) {
                TOperDetail detail = details [details.Count - 1];
                if (detail.ItemName.Length == 0)
                    details.RemoveAt (details.Count - 1);
            }

            FormHelper.ImportData<OperationImportDescription> (null, (o, a) =>
                {
                    var item = (OperationImportDescription) o;
                    if (usePrimaryDetails) {
                        operation.AddNewDetail ();
                        int row = details.Count - 1;

                        if (ItemColumnEvaluate (row, item.ResolvedItem, true))
                            QtyColumnEvaluate (ref row, item.Quantity);
                    } else {
                        operation.AddNewAdditionalDetail ();
                        int row = details.Count - 1;

                        if (SecondItemColumnEvaluate (row, item.ResolvedItem, true))
                            SecondQtyColumnEvaluate (ref row, item.Quantity);
                    }
                });

            if (usePrimaryDetails) {
                operation.AddNewDetail ();
                EditGridField (details.Count - 1, colItem.Index);
            } else {
                operation.AddNewAdditionalDetail ();
                EditSecondGridField (details.Count - 1, colSecondItem.Index);
            }
        }

        #region Search in the operation

        private void btnSearch_Clicked (object sender, EventArgs e)
        {
            algSearch.Visible = btnSearch.Active;
            if (btnSearch.Active) {
                txtSearch.GrabFocus ();
                grid.AllowSelect = true;
                if (secondGrid != null)
                    secondGrid.AllowSelect = true;

                BusinessDomain.FeedbackProvider.TrackEvent ("Operation search", "Opened");
            } else {
                grid.AllowSelect = false;
                grid.DefocusCell ();
                if (secondGrid != null) {
                    secondGrid.AllowSelect = false;
                    secondGrid.DefocusCell ();
                }
            }
        }

        [ConnectBefore]
        private void txtSearch_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            int focusedRow = grid.FocusedRow;
            int secondFocusedRow = secondGrid != null ? secondGrid.FocusedRow : -1;
            if (focusedRow < 0 && secondFocusedRow < 0)
                return;

            switch (args.Event.Key) {
                case Key.Return:
                case Key.KP_Enter:
                    btnSearch.Active = false;
                    if (focusedRow >= 0)
                        if (colItem.ListCell.IsEditable)
                            EditGridField (focusedRow, colItem.Index);
                        else
                            ItemColumnEditNext (focusedRow, Key.Return);

                    if (secondFocusedRow >= 0)
                        if (colSecondItem.ListCell.IsEditable)
                            EditSecondGridField (secondFocusedRow, colSecondItem.Index);
                        else
                            SecondItemColumnEditNext (secondFocusedRow, Key.Return);

                    BusinessDomain.FeedbackProvider.TrackEvent ("Operation search", "Item selected");
                    break;

                case Key.Up:
                case Key.KP_Up:
                    args.RetVal = true;
                    btnSearchUp.Click ();
                    break;

                case Key.Down:
                case Key.KP_Down:
                    args.RetVal = true;
                    btnSearchDown.Click ();
                    break;
            }
        }

        private void txtSearch_Changed (object sender, EventArgs e)
        {
            if (!DoSearch (0, true, true))
                DoSearch (0, true, false);
        }

        private void btnSearchUp_Clicked (object sender, EventArgs e)
        {
            if (secondGrid != null && secondGrid.FocusedRow >= 0 && DoSearch (secondGrid.FocusedRow - 1, false, false))
                return;

            if (grid.FocusedRow >= 0) {
                if (DoSearch (grid.FocusedRow - 1, false, true))
                    return;
            } else if (DoSearch (grid.Model.Count - 1, false, true))
                return;

            if (secondGrid == null || !DoSearch (secondGrid.Model.Count - 1, false, false))
                DoSearch (grid.Model.Count - 1, false, true);
        }

        private void btnSearchDown_Clicked (object sender, EventArgs e)
        {
            if (grid.FocusedRow >= 0 && DoSearch (grid.FocusedRow + 1, true, true))
                return;

            if (secondGrid != null) {
                if (secondGrid.FocusedRow >= 0) {
                    if (DoSearch (secondGrid.FocusedRow + 1, true, false))
                        return;
                } else if (DoSearch (0, true, false))
                    return;
            }

            if (!DoSearch (0, true, true))
                DoSearch (0, true, false);
        }

        private bool DoSearch (int startIndex, bool forward, bool primaryGrid)
        {
            grid.DefocusCell ();
            grid.Selection.Clear ();
            if (secondGrid != null) {
                secondGrid.DefocusCell ();
                secondGrid.Selection.Clear ();
            } else if (!primaryGrid)
                return false;

            txtSearch.ModifyText (StateType.Normal);

            var text = txtSearch.Text.ToLowerInvariant ();
            if (string.IsNullOrWhiteSpace (text))
                return true;

            string [] tokens = text.Split (new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int detailsCount = primaryGrid ? operation.Details.Count : operation.AdditionalDetails.Count;
            for (int i = startIndex; forward ? i < detailsCount : i > 0; i += forward ? 1 : -1) {
                var detail = primaryGrid ? operation.Details [i] : operation.AdditionalDetails [i];
                if (tokens.Any (t => !detail.ItemName.ToLowerInvariant ().Contains (t) &&
                    !detail.ItemCode.ToLowerInvariant ().Contains (t) &&
                    !detail.ItemBarcode.Contains (t)))
                    continue;

                if (primaryGrid)
                    grid.FocusRow (i);
                else
                    secondGrid.FocusRow (i);

                if (!txtSearch.HasFocus)
                    txtSearch.GrabFocus ();
                return true;
            }

            txtSearch.ModifyText (StateType.Normal, new Color (255, 0, 0));
            return false;
        }

        #endregion

        [UsedImplicitly]
        private void chkTax_Toggled (object sender, EventArgs e)
        {
            bool useTax = chkTax.Active;
            if (!useTax) {
                if (MessageError.ShowDialog (BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                    Translator.GetString ("Are you sure you want to make this operation tax exempt? To remove tax from the entered prices press \"No\" and use the \"Subtract Tax\" button bellow.") :
                    Translator.GetString ("Are you sure you want to make this operation VAT exempt? To remove VAT from the entered prices press \"No\" and use the \"Subtract VAT\" button bellow."), buttons: MessageButtons.YesNo) != ResponseType.Yes) {
                    chkTax.Active = true;
                    return;
                }
            }

            CurrentColumnEvaluate ();
            operation.SetVATExempt (!useTax);
            //operation.UpdatePrices (GetOperationPriceGroup ());
            SetOperationTotal (operation);
            RefreshEditedValue (grid, colSalePrice, colPurchasePrice);
            RefreshEditedValue (secondGrid, colSecondSalePrice, colSecondPurchaseValue);

            PangoStyle style = new PangoStyle { Italic = true, Bold = true, Size = PangoStyle.TextSize.XLarge, Strikethrough = !useTax };
            lblTax.Markup = style.GetMarkup (lblTax.Text);
            lblTaxValue.Markup = style.GetMarkup (lblTaxValue.Text);
        }

        private void RefreshEditedValue (ListView dataGrid, Column columnSalePrice, Column columnPurchasePrice)
        {
            if (dataGrid != null && dataGrid.EditedCell.IsValid) {
                if (columnSalePrice != null && dataGrid.EditedCell.Column == columnSalePrice.Index) {
                    dataGrid.EditedCellValue = operation.Details [dataGrid.EditedCell.Row].PriceOut;
                    grid.FocusCell (dataGrid.EditedCell.Column, dataGrid.EditedCell.Row);
                    if (dataGrid == grid)
                        EditGridField (dataGrid.EditedCell.Row, dataGrid.EditedCell.Column);
                    else
                        EditSecondGridField (dataGrid.EditedCell.Row, dataGrid.EditedCell.Column);
                }
                if (columnPurchasePrice != null && dataGrid.EditedCell.Column == columnPurchasePrice.Index) {
                    dataGrid.EditedCellValue = operation.Details [dataGrid.EditedCell.Row].PriceIn;
                    grid.FocusCell (dataGrid.EditedCell.Column, dataGrid.EditedCell.Row);
                    if (dataGrid == grid)
                        EditGridField (dataGrid.EditedCell.Row, dataGrid.EditedCell.Column);
                    else
                        EditSecondGridField (dataGrid.EditedCell.Row, dataGrid.EditedCell.Column);
                }
            }
        }

        protected void CancelEditing ()
        {
            grid.CancelCellEdit ();
            if (secondGrid != null)
                secondGrid.CancelCellEdit ();
        }

        protected void PrepareOpertionForSaving (Operation oper = null, long? locationId = null)
        {
            if (oper == null)
                oper = operation;

            bool value;
            if (canUseLocationIdWarning.TryGetValue (locationId ?? oper.LocationId, out value) && value)
                oper.SetState (OperationState.NewPending);
        }

        public override bool WaitForPendingOperationCompletion (Operation oper = null, long? locationId = null)
        {
            if (pendingCompletionAcknowledged)
                return true;

            if (oper == null)
                oper = operation;

            bool value;
            if (!canUseLocationIdWarning.TryGetValue (locationId ?? oper.LocationId, out value))
                return true;

            if (!BusinessDomain.WaitForPendingOperationToComplete (oper, 60 * 1000))
                return false;

            pendingCompletionAcknowledged = true;
            return true;
        }

        protected override void OnOperationSaved (Operation oper = null, long? locationId = null)
        {
            if (oper == null)
                oper = operation;

            base.OnOperationSaved (oper, locationId ?? oper.LocationId);
        }

        protected override void OnPageAddingFinish ()
        {
            base.OnPageAddingFinish ();
            OnPageOpened ();
        }

        #region WorkBookPage Members

        public override string PageTitle
        {
            get { return pageTitle; }
        }

        #endregion

        protected bool CheckPurchasePrice (int row, string price)
        {
            TOperDetail detail = operation.Details [row];
            double enteredPurchasePrice = Currency.ParseExpression (price);
            // assuming zero sale price means unentered yet sale price
            return detail.OriginalPriceOut.IsZero () ||
                CheckPricesSaleLessThanPurchase (enteredPurchasePrice, detail.OriginalPriceOut, row, colPurchasePrice.Index, detail);
        }

        protected bool CheckSalePrice (int row, string price)
        {
            TOperDetail detail = operation.Details [row];
            double enteredSalePrice = Currency.ParseExpression (price);
            return CheckPricesSaleLessThanPurchase (detail.OriginalPriceIn, enteredSalePrice, row, colSalePrice.Index, detail);
        }

        private bool CheckPricesSaleLessThanPurchase (double purchasePrice, double salePrice, int row, int column, TOperDetail detail)
        {
            // prevent the dialogue from popping if the page is being closed because it's annoying otherwise
            if (pageCloseRequested)
                return false;

            if (detail.CheckPurchaseSalePrices (purchasePrice, salePrice))
                return true;

            using (MessageYesNoRemember dialog = new MessageYesNoRemember (Translator.GetString ("Sale Price Lower than Purchase Price"), string.Empty,
                Translator.GetString ("The sale price you entered for this " +
                "item is lower than its purchase price. Do you want to continue?"), "Icons.Question32.png")) {
                dialog.SetButtonText (MessageButtons.Remember, Translator.GetString ("Do not warn me anymore"));
                ResponseType responseType = dialog.Run ();
                BusinessDomain.AppConfiguration.WarnPricesSaleLowerThanPurchase = !dialog.RememberChoice;
                switch (responseType) {
                    case ResponseType.Yes:
                        return true;
                    default:
                        object editedCellValue = grid.EditedCellValue;
                        EditGridCell (row, column);
                        grid.EditedCellValue = editedCellValue;
                        return false;
                }
            }
        }

        public override WbpOperationBase SetReadOnly (bool value = true)
        {
            if (!value)
                return this;

            PartnerSensitive = false;
            LocationSensitive = false;
            SrcLocationSensitive = false;
            DstLocationSensitive = false;
            UserSensitive = false;
            DateSensitive = false;

            if (grid != null) {
                grid.Sensitive = false;
                foreach (var column in grid.ColumnController)
                    column.ListCell.IsEditable = false;
            }

            if (secondGrid != null) {
                secondGrid.Sensitive = false;
                foreach (var column in secondGrid.ColumnController)
                    column.ListCell.IsEditable = false;
            }

            chkTax.Sensitive = false;
            btnSave.Sensitive = false;
            btnClear.Sensitive = false;
            btnAddDiscount.Sensitive = false;
            btnAddRemoveVAT.Sensitive = false;
            btnImport.Sensitive = false;
            algAdditionalButtons.Sensitive = false;

            return this;
        }
    }

    public abstract class WbpOperationBase : WbpBase
    {
        protected enum FormFields
        {
            Partner,
            Location,
            SourceLocation,
            DestinationLocation,
            User,
            Date,
            GridDetails,
            SecondGridDetails,
            None
        }

        #region Fields

        protected bool editMode;
        protected string pageTitle = string.Empty;
        protected FormFields cursorAtField = FormFields.None;
        protected int cursorAtColumn;

        protected PriceGroup partnerPriceGroup = PriceGroup.RegularPrice;
        protected PriceGroup locationPriceGroup = PriceGroup.RegularPrice;
        protected bool barcodeUsed;
        protected double codeQtty;
        protected string codeLot;
        protected bool vatIncludedInPrices;

        protected Partner partner;
        protected Location location;
        protected Location srcLocation;
        protected Location dstLocation;
        protected User user;
        protected DateTime date;

        protected bool pageCloseRequested;

        public event EventHandler<OperationSavedEventArgs> OperationSaved;
        public static event EventHandler OperationPageOpened;

        #endregion

        #region Glade Widgets

        [Widget]
        protected VBox vbxOperationRoot;

        [Widget]
        protected Label lblOperationTitle;

        [Widget]
        protected Table tableOperation;
        [Widget]
        protected VBox vboxTotal;

        [Widget]
        protected Label lblPartner;
        [Widget]
        protected HBox hboPartner;
        [Widget]
        protected Entry txtPartner;
        [Widget]
        protected Button btnPickPartner;

        [Widget]
        protected Label lblLocation;
        [Widget]
        protected HBox hboLocation;
        [Widget]
        protected Entry txtLocation;
        [Widget]
        protected Button btnPickLocation;

        [Widget]
        protected Label lblSrcLocation;
        [Widget]
        protected HBox hboSrcLocation;
        [Widget]
        protected Entry txtSrcLocation;
        [Widget]
        protected Button btnPickSrcLocation;

        [Widget]
        protected Label lblDstLocation;
        [Widget]
        protected HBox hboDstLocation;
        [Widget]
        protected Entry txtDstLocation;
        [Widget]
        protected Button btnPickDstLocation;

        [Widget]
        protected Label lblUser;
        [Widget]
        protected HBox hboUser;
        [Widget]
        protected Entry txtUser;
        [Widget]
        protected Button btnPickUser;

        [Widget]
        protected Label lblDate;
        [Widget]
        protected HBox hboDate;
        [Widget]
        protected Entry txtDate;
        [Widget]
        protected Button btnPickDate;

        [Widget]
        protected Label lblNote;
        [Widget]
        protected Expander expNote;
        [Widget]
        protected TextView txtNote;

        [Widget]
        protected Alignment evbIcon;

        [Widget]
        protected HBox hboxBigTotal;
        [Widget]
        protected Label lblBigTotal;
        [Widget]
        protected Label lblBigTotalValue;
        [Widget]
        protected Table tblTotal;
        [Widget]
        protected Label lblAmount;
        [Widget]
        protected Label lblAmountValue;
        [Widget]
        protected CheckButton chkTax;
        [Widget]
        protected Label lblTax;
        [Widget]
        protected Label lblTaxValue;
        [Widget]
        protected Label lblTotal;
        [Widget]
        protected Label lblTotalValue;

        [Widget]
        protected Alignment algSimpleView;
        [Widget]
        protected EventBox evbSimpleView;
        [Widget]
        protected Label lblSimpleView;

        [Widget]
        protected Label lblGridDescription;
        [Widget]
        protected Alignment algGrid;
        [Widget]
        protected Label lblSecondGridDescription;
        [Widget]
        protected Alignment algSecondGrid;

        [Widget]
        protected Alignment algSave;
        [Widget]
        protected Button btnSave;
        [Widget]
        protected Button btnClear;
        [Widget]
        protected Button btnClose;
        [Widget]
        protected Button btnAddDiscount;
        [Widget]
        protected Button btnAddRemoveVAT;
        [Widget]
        protected Button btnImport;
        [Widget]
        protected Alignment algAdditionalButtons;
        [Widget]
        protected VBox vbxAdditionalButtons;

        [Widget]
        protected ToggleButton btnSearch;
        [Widget]
        protected Alignment algSearch;
        [Widget]
        protected Entry txtSearch;
        [Widget]
        protected Button btnSearchUp;
        [Widget]
        protected Button btnSearchDown;
        [Widget]
        protected EventBox evbHelp;
        [Widget]
        protected Label lblHelp;

        #endregion

        #region Properties

        public abstract string HelpFile
        {
            get;
        }

        public abstract Operation Operation { get; }

        protected bool PartnerVisible
        {
            set
            {
                lblPartner.Visible = value;
                hboPartner.Visible = value;
            }
        }

        protected bool PartnerSensitive
        {
            set
            {
                txtPartner.Sensitive = value;
                btnPickPartner.Sensitive = value;
            }
        }

        protected bool LocationVisible
        {
            set
            {
                lblLocation.Visible = value;
                hboLocation.Visible = value;
            }
        }

        protected bool LocationSensitive
        {
            set
            {
                txtLocation.Sensitive = value;
                btnPickLocation.Sensitive = value;
            }
        }

        protected bool SrcLocationVisible
        {
            set
            {
                lblSrcLocation.Visible = value;
                hboSrcLocation.Visible = value;
            }
        }

        protected bool SrcLocationSensitive
        {
            set
            {
                txtSrcLocation.Sensitive = value;
                btnPickSrcLocation.Sensitive = value;
            }
        }

        protected bool DstLocationVisible
        {
            set
            {
                lblDstLocation.Visible = value;
                hboDstLocation.Visible = value;
            }
        }

        protected bool DstLocationSensitive
        {
            set
            {
                txtDstLocation.Sensitive = value;
                btnPickDstLocation.Sensitive = value;
            }
        }

        protected bool UserVisible
        {
            set
            {
                lblUser.Visible = value;
                hboUser.Visible = value;
            }
        }

        protected bool UserSensitive
        {
            set
            {
                txtUser.Sensitive = value;
                btnPickUser.Sensitive = value;
            }
        }

        protected bool DateVisible
        {
            set
            {
                lblDate.Visible = value;
                hboDate.Visible = value;
            }
        }

        protected bool DateSensitive
        {
            set
            {
                txtDate.Sensitive = value;
                btnPickDate.Sensitive = value;
            }
        }

        protected bool NoteVisible
        {
            set { expNote.Visible = value; }
            get { return expNote.Visible; }
        }

        protected bool NoteSensitive
        {
            set { expNote.Sensitive = value; }
        }

        #endregion

        protected WbpOperationBase ()
        {
            InitializeForm ();
        }

        private void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("WbpOperationBase.glade", "vbxOperationRoot");
            form.Autoconnect (this);
            Add (vbxOperationRoot);

            evbHelp.ModifyBg (StateType.Normal, new Color (255, 255, 0));

            PresentationDomain.CardRecognized += PresentationDomain_CardRecognized;

            vbxOperationRoot.KeyPressEvent += OperationForm_KeyPressEvent;
            OuterKeyPressed += OperationForm_KeyPressEvent;

            btnSave.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnClear.SetChildImage (FormHelper.LoadImage ("Icons.Clear24.png"));
            btnClose.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            btnAddDiscount.SetChildImage (FormHelper.LoadImage ("Icons.Discount24.png"));
            btnAddRemoveVAT.SetChildImage (FormHelper.LoadImage ("Icons.Sum24.png"));
            btnImport.SetChildImage (FormHelper.LoadImage ("Icons.Import24.png"));

            btnSearch.SetChildImage (FormHelper.LoadImage ("Icons.Search16.png"));
            btnSearchUp.SetChildImage (FormHelper.LoadImage ("Icons.Up16.png"));
            btnSearchDown.SetChildImage (FormHelper.LoadImage ("Icons.Down16.png"));

            foreach (Button button in new [] { btnSave, btnClear, btnAddDiscount })
                KeyShortcuts.SetAccelPath (button, FrmMain.AccelGroup, button.Name);

            if (BusinessDomain.LoggedUser.LockedPartnerId <= 0) {
                AccelKey key = KeyShortcuts.LookupEntry (txtPartner.Name);
                txtPartner.AddAccelerator ("grab_focus", FrmMain.AccelGroup, key);
                txtPartner.TooltipText = string.Format (" {0} ", KeyShortcuts.KeyToString (key));
            }

            InitializeEntries ();
            InitializeFormStrings ();
        }

        protected abstract void InitializeEntries ();

        protected virtual void InitializeFormStrings ()
        {
            lblPartner.SetText (Translator.GetString ("Partner"));
            lblLocation.SetText (Translator.GetString ("Location"));
            lblSrcLocation.SetText (Translator.GetString ("From location"));
            lblDstLocation.SetText (Translator.GetString ("To location"));
            lblUser.SetText (Translator.GetString ("User"));
            lblDate.SetText (Translator.GetString ("Date"));
            lblBigTotal.SetText (Translator.GetString ("Total:"));
            lblAmount.SetText (Translator.GetString ("Amount:"));
            lblTax.SetText (BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                Translator.GetString ("Tax:") :
                Translator.GetString ("VAT:"));
            lblTotal.SetText (Translator.GetString ("Total:"));

            btnSave.SetChildLabelText (Translator.GetString ("Save"));
            btnClear.SetChildLabelText (Translator.GetString ("Clear"));
            btnClose.SetChildLabelText (Translator.GetString ("Close"));
            btnAddDiscount.SetChildLabelText (Translator.GetString ("Add Discount"));
            btnImport.SetChildLabelText (Translator.GetString ("Import"));

            InitializeHelpStrings ();
        }

        protected abstract void InitializeHelpStrings ();

        protected void UpdateAddRemoveVatText ()
        {
            if (vatIncludedInPrices)
                btnAddRemoveVAT.SetChildLabelText (BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                    Translator.GetString ("Add Tax") : Translator.GetString ("Add VAT"));
            else
                btnAddRemoveVAT.SetChildLabelText (BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                    Translator.GetString ("Subtract Tax") : Translator.GetString ("Subtract VAT"));
        }

        protected void PresentationDomain_CardRecognized (object sender, CardReadArgs e)
        {
            txtPartner.Text = e.CardId;
        }

        private void OperationForm_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.HelpKey) && !string.IsNullOrEmpty (HelpFile)) {
                FormHelper.ShowWindowHelp (HelpFile);
                return;
            }

            OnOperationKeyPressEvent (args);
        }

        private void OnOperationKeyPressEvent (KeyPressEventArgs args)
        {
            if (args.Event.Key == Key.Escape) {
                args.RetVal = true;
                RequestClose ();
            }
        }

        public abstract void OnOperationSave (bool askForConfirmation);

        protected abstract void CommitOperation ();

        public abstract bool WaitForPendingOperationCompletion (Operation oper = null, long? locationId = null);

        protected virtual void OnOperationSaved (Operation oper = null, long? locationId = null)
        {
            BusinessDomain.FeedbackProvider.TrackEvent ("Operation saved", Enum.GetName (typeof (OperationType), oper.OperationType).CamelSpace ());

            EventHandler<OperationSavedEventArgs> operSaved = OperationSaved;
            if (operSaved != null)
                operSaved (this, new OperationSavedEventArgs
                    {
                        Operation = oper,
                        LocationId = locationId ?? -1
                    });
        }

        protected void OnPageOpened ()
        {
            EventHandler pageOpened = OperationPageOpened;
            if (pageOpened != null)
                pageOpened (this, EventArgs.Empty);
        }

        public abstract void LoadOperation (Operation oper);

        public void AppendAdditionalButton (PictureButton button)
        {
            vbxAdditionalButtons.PackStart (button, false, true, 0);
            vbxAdditionalButtons.Show ();
            algAdditionalButtons.Show ();
        }

        public abstract WbpOperationBase SetReadOnly (bool value = true);
    }
}
