//
// ChooseEditItem.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/16/2006
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
using System.Linq;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Image = Gtk.Image;
using Item = Warehouse.Business.Entities.Item;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseEditItem : ChooseEdit<Item, ItemsGroup>
    {
        private readonly PriceGroup priceGroup = PriceGroup.RegularPrice;
        private Location location;
        private Column colQtty;
        private CheckButton chkAvailable;

        public override string HelpFile
        {
            get { return "ChooseEditGoods.html"; }
        }

        public override string [] SelectedItemsText
        {
            get
            {
                return SelectedItems.Select (sel => sel.Name).ToArray ();
            }
        }

        public ChooseEditItem (bool pickMode = false)
        {
            this.pickMode = pickMode;

            Initialize ();
        }

        public ChooseEditItem (bool pickMode, string filter)
            : base (filter)
        {
            this.pickMode = pickMode;

            Initialize ();
        }

        public ChooseEditItem (bool pickMode, long? selectedID)
            : this (pickMode)
        {
            selectedId = selectedID;

            grid.AllowMultipleSelect = false;
            grid.Realized -= grid_Realized;
            grid.Realized += (sender, e) =>
                {
                    int index = entities.FindIndex (g => g.Id == selectedId);
                    if (index >= 0) {
                        grid.Selection.Clear ();
                        grid.Selection.Select (index);
                        grid.FocusRow (index);
                    } else
                        SelectFirstRow ();
                };
        }

        public ChooseEditItem (long locationId, PriceGroup priceGroup, string filter)
            : base (filter)
        {
            this.priceGroup = priceGroup;
            location = Location.GetById (locationId);
            pickMode = true;

            Initialize ();
        }

        private void btnLocation_Clicked (object sender, EventArgs e)
        {
            long? selectedLocation = location != null ? location.Id : (long?) null;
            using (ChooseEditLocation chooseEditLocation = new ChooseEditLocation (true, selectedLocation)) {
                if (chooseEditLocation.Run () == ResponseType.Ok && chooseEditLocation.SelectedItems.Length > 0)
                    location = chooseEditLocation.SelectedItems [0];
                else
                    location = null;

                ShowLocationAvailability ();
                ReinitializeGrid (false, null);
            }
        }

        private void ShowLocationAvailability ()
        {
            if (BusinessDomain.LoggedUser.HideItemsAvailability)
                return;

            grid.ColumnController [colQtty.Index].Visible = true;
            lblLocationValue.SetText (location != null ?
                location.Name : Translator.GetString ("All"));
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            Image icon = FormHelper.LoadImage ("Icons.Goods16.png");
            dlgChooseEdit.Icon = icon.Pixbuf;
            icon.Show ();

            icon = FormHelper.LoadImage ("Icons.Goods32.png");
            algDialogIcon.Add (icon);
            icon.Show ();

            tblFilter.Remove (btnClear);
            tblFilter.Attach (btnClear, 3, 4, 0, 1, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            chkAvailable = new CheckButton (Translator.GetString ("Only available"));
            chkAvailable.Toggled += chkAvailable_Toggled;
            tblFilter.Attach (chkAvailable, 2, 3, 0, 1, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            tblFilter.ShowAll ();

            newAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditGoodsbtnNew") == UserRestrictionState.Allowed;
            editAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditGoodsbtnEdit") == UserRestrictionState.Allowed;
            deleteAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditGoodsbtnDelete") == UserRestrictionState.Allowed;
            btnImport.Sensitive = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditGoodsbtnImport") == UserRestrictionState.Allowed;
            btnExport.Sensitive = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditGoodsbtnExport") == UserRestrictionState.Allowed;
            btnImport.Visible = BusinessDomain.DataImporters.Count > 0;
            btnExport.Visible = BusinessDomain.DataExporters.Count > 0;

            btnLocation.Visible = !BusinessDomain.LoggedUser.HideItemsAvailability;
            btnLocation.Clicked += btnLocation_Clicked;

            dlgChooseEdit.WidthRequest = 800;
            dlgChooseEdit.HeightRequest = 480;

            InitializeFormStrings ();
            InitializeGrid ();

            if (Location.TryGetLocked (ref location))
                btnLocation.Sensitive = false;

            ShowLocationAvailability ();
            btnGroups_Toggled (null, null);
        }

        private void chkAvailable_Toggled (object sender, EventArgs e)
        {
            ReinitializeGrid (false, null);
        }

        protected override string KeyForKeyboardBingdings
        {
            get { return "mnuEditGoods"; }
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseEdit.Title = Translator.GetString ("Items");

            btnNew.SetChildLabelText (Translator.GetString ("New", "Item"));
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();

            ColumnController cc = new ColumnController
                {
                    new Column (Translator.GetString ("Code"), "Code", 0.1, "Code") { MinWidth = 70 },
                    new Column (Translator.GetString ("Item"), "Name", 1, "Name") { MinWidth = 70 },
                    new Column (Translator.GetString ("Measure"), "MUnit", 0.1, "MUnit") { MinWidth = 70 }
                };

            CellTextItemQuantity ctq = new CellTextItemQuantity ("Quantity");
            colQtty = new Column (Translator.GetString ("Qtty"), ctq, 0.1, "Quantity") { MinWidth = 70, Visible = !BusinessDomain.LoggedUser.HideItemsAvailability };
            cc.Add (colQtty);

            CellTextCurrency ctc;
            if (!BusinessDomain.LoggedUser.HideItemsPurchasePrice) {
                ctc = new CellTextCurrency ("TradeInPrice", PriceType.Purchase);
                cc.Add (new Column (Translator.GetString ("Purchase price"), ctc, 0.1, "TradeInPrice") { MinWidth = 70 });
            }

            string partnerPriceColumn = Item.GetPriceGroupProperty (priceGroup);

            ctc = new CellTextCurrency (partnerPriceColumn);
            cc.Add (new Column (Translator.GetString ("Sale price"), ctc, 0.1, partnerPriceColumn) { MinWidth = 70 });

            grid.ColumnController = cc;

            btnGroups.Active = BusinessDomain.AppConfiguration.ShowItemsGroups;
            btnGroups.Toggled += btnGroups_Toggled;
            groupsPanel.GroupsTree.Selection.Changed += GroupsTreeSelection_Changed;
        }

        protected override void LoadSavedGroup ()
        {
            if (BusinessDomain.AppConfiguration.LastItemsGroupId >= 0)
                selectedGroupId = BusinessDomain.AppConfiguration.LastItemsGroupId;
            else
                selectedGroupId = null;
        }

        protected override void SaveGroup ()
        {
            BusinessDomain.AppConfiguration.LastItemsGroupId = selectedGroupId ?? -1;
        }

        protected override void GetAllEntities ()
        {
            entities = !BusinessDomain.LoggedUser.HideItemsAvailability && location != null ?
                Item.GetAllByLocation (location.Id, selectedGroupId, chkAvailable.Active, false) :
                Item.GetAll (selectedGroupId, chkAvailable.Active, false);

            entities.FilterProperties.Add ("Code");
            entities.FilterProperties.Add ("Name");
            entities.FilterProperties.Add ("BarCode");
            entities.FilterProperties.Add ("BarCode2");
            entities.FilterProperties.Add ("BarCode3");
        }

        #endregion

        protected override Message GetMovingToGroupMessage (string targetGroup)
        {
            return new Message (Translator.GetString ("Move Items"), "Icons.Goods16.png",
                string.Format (Translator.GetString ("Are you sure you want to move the selected items to group \"{0}\"?"),
                targetGroup), "Icons.Question32.png");
        }

        #region Groups management

        protected override bool CanEditGroups
        {
            get { return true; }
        }

        protected override IEnumerable<ItemsGroup> GetAllGroups ()
        {
            return ItemsGroup.GetAll ();
        }

        protected override ItemsGroup CreateNewGroup ()
        {
            return new ItemsGroup ();
        }

        protected override DeletePermission GetDeletePermission (ItemsGroup group)
        {
            return ItemsGroup.RequestDelete (group.Id);
        }

        protected override void DeleteGroup (ItemsGroup group)
        {
            ItemsGroup.Delete (group.Id);
        }

        #endregion

        #region Button events

        protected override void btnClear_Clicked (object sender, EventArgs e)
        {
            if (chkAvailable.Active) {
                grid.EnableAutoFilter = false;
                grid.SetAutoFilter (string.Empty, false);
                grid.EnableAutoFilter = true;
                changingFilter = true;
                txtFilter.Text = string.Empty;
                changingFilter = false;
                chkAvailable.Active = false;
            } else
                grid.SetAutoFilter (string.Empty, false);
        }

        protected override void btnEdit_Clicked (object o, EventArgs args)
        {
            int selectedRow = grid.FocusedRow;
            if (selectedRow < 0)
                return;

            Item item = entities [selectedRow];
            selectedId = item.Id;

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                using (EditNewItem dialog = new EditNewItem (item, selectedGroupId)) {
                    if (dialog.Run () != ResponseType.Ok) {
                        ReinitializeGrid (true, null);
                        return;
                    }

                    item = dialog.GetItem ().CommitChanges ();
                    selectedId = item.Id;
                }

                OnEntitiesChanged (item.Deleted ? ItemsGroup.DeletedGroupId : item.GroupId);
            }
        }

        protected override void btnNew_Clicked (object o, EventArgs args)
        {
            selectedId = null;

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                Item item;
                using (EditNewItem dialog = new EditNewItem (null, selectedGroupId)) {
                    if (dialog.Run () != ResponseType.Ok) {
                        ReinitializeGrid (true, null);
                        return;
                    }

                    item = dialog.GetItem ().CommitChanges ();
                    selectedId = item.Id;
                }

                OnEntitiesChanged (item.GroupId);
            }
        }

        protected override void btnDelete_Clicked (object o, EventArgs args)
        {
            DeleteSelectedEntities ();
        }

        protected override bool DeleteEntity (Item entity)
        {
            selectedId = entity.Id;

            switch (Item.RequestDelete (entity.Id)) {
                case DeletePermission.InUse:
                    string title = Translator.GetString ("Item in Use");
                    string translation = Translator.GetString (
                        "The item \"{0}\" cannot be deleted, because it is used in operations. Do you want to move it to the \"Deleted\" group?");
                    string message = string.Format (translation, entity.Name);
                    if (Message.ShowDialog (title, "Icons.Goods16.png", message, "Icons.Question32.png",
                        MessageButtons.YesNo) == ResponseType.Yes) {
                        entity.Deleted = true;
                        entity.GroupId = ItemsGroup.DefaultGroupId;
                        entity.CommitChanges ();
                        return true;
                    }
                    return false;

                case DeletePermission.Reserved:
                case DeletePermission.No:
                    MessageError.ShowDialog (
                        string.Format (Translator.GetString ("Cannot delete item \"{0}\"!"), entity.Name),
                        "Icons.Goods16.png");
                    return false;
            }

            Item.Delete (entity.Id);
            return true;
        }

        protected override bool AskDeleteSingleEntity (Item entity)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Item"),
                "Icons.Goods16.png",
                string.Format (Translator.GetString ("Do you want to delete item with name \"{0}\"?"), entity.Name),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override bool AskDeleteMultipleEntities (int count)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Items"),
                "Icons.Goods16.png",
                string.Format (Translator.GetString ("Do you want to delete the selected {0} items?"), count),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override void btnGroups_Toggled (object o, EventArgs args)
        {
            base.btnGroups_Toggled (o, args);

            BusinessDomain.AppConfiguration.ShowItemsGroups = btnGroups.Active;
        }

        protected override void btnImport_Clicked (object o, EventArgs args)
        {
            int selectedRow = grid.FocusedRow;
            long? groupId = null;
            if (selectedRow >= 0) {
                selectedId = entities [selectedRow].Id;
                groupId = entities [selectedRow].GroupId;
            }

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                List<Item> imported = new List<Item> ();
                long? locationId = null;
                FormHelper.ImportData<Item> (null, (e, a) =>
                    {
                        imported.Add ((Item) e);
                        locationId = a.LocationId;
                    }, true);

                if (locationId != null && imported.Count > 0) {
                    StockTaking stockTaking = new StockTaking
                        {
                            LocationId = (int) locationId,
                            UserId = BusinessDomain.LoggedUser.Id,
                            LoggedUserId = BusinessDomain.LoggedUser.Id,
                            Date = BusinessDomain.Today
                        };

                    bool allowNegativeAvail = BusinessDomain.AppConfiguration.AllowNegativeAvailability;

                    foreach (Item item in imported) {
                        // Skip items with no imported availability
                        if (!item.Quantity.IsZero () && (item.Quantity > 0 || allowNegativeAvail)) {
                            StockTakingDetail detail = stockTaking.AddNewDetail ();
                            detail.ItemEvaluate (item, PriceGroup.RegularPrice, true);
                            detail.ExpectedQuantity = 0;
                            detail.EnteredQuantity = item.Quantity;
                            detail.Note = Translator.GetString ("Auto Stock-taking after items import");
                        }
                    }

                    if (stockTaking.Details.Count > 0)
                        stockTaking.Commit (PriceGroup.RegularPrice);
                }
                OnEntitiesChanged (groupId);
            }
        }

        protected override void btnExport_Clicked (object o, EventArgs args)
        {
            FormHelper.ExportData ("items", "Items", new DataExchangeSet (Translator.GetString ("Items"), entities.ToDataTable (false)));
        }

        #endregion
    }
}
