//
// ChooseEditDevice.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/05/2006
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
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseEditDevice : ChooseEdit<Device, DevicesGroup>
    {
        public override string [] SelectedItemsText
        {
            get
            {
                return SelectedItems.Select (sel => sel.Name).ToArray ();
            }
        }

        public ChooseEditDevice ()
        {
            Initialize ();
        }

        public ChooseEditDevice (bool pickMode, string filter)
            : base (filter)
        {
            this.pickMode = pickMode;

            Initialize ();
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            Image icon = FormHelper.LoadImage ("Icons.Device16.png");
            dlgChooseEdit.Icon = icon.Pixbuf;
            icon.Show ();

            icon = FormHelper.LoadImage ("Icons.Device32.png");
            algDialogIcon.Add (icon);
            icon.Show ();

            newAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditDevicesbtnNew") == UserRestrictionState.Allowed && (BusinessDomain.DeviceManager.AllDrivers.Length > 0);
            editAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditDevicesbtnEdit") == UserRestrictionState.Allowed;
            deleteAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditDevicesbtnDelete") == UserRestrictionState.Allowed;
            dlgChooseEdit.HeightRequest = 440;
            dlgChooseEdit.WidthRequest = 730;
            groupsPanel.HideDeletedGroup = true;

            InitializeFormStrings ();
            InitializeGrid ();
        }

        protected override string KeyForKeyboardBingdings
        {
            get { return "mnuEditDevices"; }
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseEdit.Title = Translator.GetString ("Devices");

            btnNew.SetChildLabelText (Translator.GetString ("New", "Device"));
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();

            ColumnController cc = new ColumnController ();

            CellTextLookup<bool> ctl = new CellTextLookup<bool> ("Enabled")
                {
                    Alignment = Pango.Alignment.Center,
                    Lookup = new Dictionary<bool, string>
                        {
                            { false, string.Empty },
                            { true, Translator.GetString ("Yes") }
                        }
                };
            Column col = new Column (Translator.GetString ("Enabled"), ctl, 0.1, "Enabled") { MinWidth = 70 };
            cc.Add (col);

            col = new Column (Translator.GetString ("Driver"), "DriverName", 0.1, "DriverName") { MinWidth = 200 };
            cc.Add (col);

            col = new Column (Translator.GetString ("Name"), "Name", 0.8, "Name") { MinWidth = 100 };
            cc.Add (col);

            //ctl = new CellTextLookup<bool> ("PrintCashReceipts");
            //ctl.Lookup = lookup;
            //ctl.Alignment = Pango.Alignment.Center;
            //col = new Column (Translator.GetString ("Default Printer"), ctl, 0.1, "PrintCashReceipts");
            //col.MinWidth = 70;
            //cc.Add (col);

            //ctl = new CellTextLookup<bool> ("DisplaySaleInfo");
            //ctl.Lookup = lookup;
            //ctl.Alignment = Pango.Alignment.Center;
            //col = new Column (Translator.GetString ("Default Display"), ctl, 0.1, "DisplaySaleInfo");
            //col.MinWidth = 70;
            //cc.Add (col);

            //ctl = new CellTextLookup<bool> ("ReadIdCards");
            //ctl.Lookup = lookup;
            //ctl.Alignment = Pango.Alignment.Center;
            //col = new Column (Translator.GetString ("Default Card Reader"), ctl, 0.1, "ReadIdCards");
            //col.MinWidth = 70;
            //cc.Add (col);

            grid.ColumnController = cc;

            btnGroups.Active = BusinessDomain.AppConfiguration.ShowDevicesGroups;
            btnGroups.Toggled += btnGroups_Toggled;
            grid.RowsDraggable = false;
            algGridGroups.Shown += (sender, e) => grid.RowsDraggable = false;
            groupsPanel.GroupsTree.Selection.Changed += GroupsTreeSelection_Changed;
            btnGroups_Toggled (null, null);
        }

        protected override void LoadSavedGroup ()
        {
            if (BusinessDomain.AppConfiguration.LastItemsGroupId >= 0)
                selectedGroupId = BusinessDomain.AppConfiguration.LastDevicesGroupId;
            else
                selectedGroupId = null;
        }

        protected override IEnumerable<DevicesGroup> GetAllGroups ()
        {
            return DevicesGroup.GetAll ();
        }

        protected override void SaveGroup ()
        {
            BusinessDomain.AppConfiguration.LastDevicesGroupId = selectedGroupId ?? -1;
        }

        protected override void GetAllEntities ()
        {
            if (!selectedGroupId.HasValue || selectedGroupId <= 0)
                entities = new BindingListModel<Device> (Device.GetAll ());
            else
                entities = new BindingListModel<Device> (Device.GetAllByType ((DeviceType) selectedGroupId));

            entities.FilterProperties.Add ("Name");
            grid.Model = entities;
        }

        protected virtual void GetGroupOfLastSelected (Device lastSelected)
        {
            selectedGroupId = (int?) lastSelected.DriverInfo.DeviceType;
        }

        #endregion

        #region Button handling

        protected override void btnEdit_Clicked (object o, EventArgs args)
        {
            int selectedRow = grid.FocusedRow;
            if (selectedRow < 0)
                return;

            Device selectedItem = entities [selectedRow];
            selectedId = selectedItem.Id;
            Device newItem;

            using (EditNewDevice dialog = new EditNewDevice (selectedItem)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                newItem = dialog.GetDevice ().CommitChanges ();
            }

            ReinitializeHardware (selectedItem, newItem);
            OnEntitiesChanged ((int) newItem.DriverInfo.DevicePrimaryType);
        }

        protected override void btnNew_Clicked (object o, EventArgs args)
        {
            selectedId = null;
            Device newItem;

            using (EditNewDevice dialog = new EditNewDevice (null)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                newItem = dialog.GetDevice ().CommitChanges ();
                selectedId = newItem.Id;
            }

            ReinitializeHardware (null, newItem);
            OnEntitiesChanged ((int) newItem.DriverInfo.DevicePrimaryType);
        }

        protected override void btnDelete_Clicked (object o, EventArgs args)
        {
            DeleteSelectedEntities ();
        }

        protected override bool DeleteEntity (Device entity)
        {
            selectedId = entity.Id;

            switch (Device.RequestDelete (entity.Id)) {
                case DeletePermission.InUse:
                    MessageError.ShowDialog (
                        string.Format (Translator.GetString ("The device \"{0}\" cannot be deleted, because it is in use."), entity.Name),
                        "Icons.Device16.png");
                    return false;

                case DeletePermission.Reserved:
                case DeletePermission.No:
                    MessageError.ShowDialog (
                        string.Format (Translator.GetString ("Cannot delete device \"{0}\"!"), entity.Name),
                        "Icons.Device16.png");
                    return false;
            }

            Device.Delete (entity.Id);
            ReinitializeHardware (entity, null);
            return true;
        }

        protected override bool AskDeleteSingleEntity (Device entity)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Device"),
                "Icons.Device16.png",
                string.Format (Translator.GetString ("Do you want to delete device \"{0}\"?"), entity.Name),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override bool AskDeleteMultipleEntities (int count)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Devices"),
                "Icons.Device16.png",
                string.Format (Translator.GetString ("Do you want to delete the selected {0} devices?"), count),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override long? GetEntityGroup (Device entity)
        {
            if (entity.DriverInfo == null)
                return (int?) DeviceType.CashReceiptPrinter;

            return (int?) entity.DriverInfo.DevicePrimaryType;
        }

        protected override void btnGroups_Toggled (object o, EventArgs args)
        {
            base.btnGroups_Toggled (o, args);

            BusinessDomain.AppConfiguration.ShowDevicesGroups = btnGroups.Active;
        }

        private static void ReinitializeHardware (Device oldItem, Device newItem)
        {
            BusinessDomain.ReinitializeHardware (oldItem, newItem,
                () => MessageError.ShowDialog (Translator.GetString ("An error occurred while connecting to the cash receipt device. Check the input parameters.")),
                () => MessageError.ShowDialog (Translator.GetString ("An error occurred while connecting to the receipt device. Check the input parameters.")),
                () => MessageError.ShowDialog (Translator.GetString ("An error occurred while connecting to the external display. Check the input parameters.")),
                () => MessageError.ShowDialog (Translator.GetString ("An error occurred while connecting to the card reader device. Check the input parameters.")),
                () => MessageError.ShowDialog (Translator.GetString ("An error occurred while connecting to the electronic scale. Check the input parameters.")),
                () => MessageError.ShowDialog (Translator.GetString ("An error occurred while connecting to the sales data controller. Check the input parameters.")),
                () => MessageError.ShowDialog (Translator.GetString ("An error occurred while connecting to the kitchen printer. Check the input parameters.")),
                () => MessageError.ShowDialog (Translator.GetString ("An error occurred while connecting to the barcode scanner. Check the input parameters.")));
        }

        #endregion
    }
}
