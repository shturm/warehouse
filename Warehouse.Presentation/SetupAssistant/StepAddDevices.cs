//
// StepAddDevices.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   09.26.2011
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
using Glade;
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Presentation.Dialogs;

namespace Warehouse.Presentation.SetupAssistant
{
    [Extension ("/Warehouse/Presentation/SetupAssistant/Step")]
    public class StepAddDevices : StepBase
    {
        private bool initializing;

        #region Glade widgets

#pragma warning disable 649

        [Widget]
        private Table tblRoot;
        [Widget]
        private CheckButton chkReceiptPrinter;
        [Widget]
        private Alignment algReceiptPrinter;
        [Widget]
        private CheckButton chkKitchenPrinter;
        [Widget]
        private Alignment algKitchenPrinter;
        [Widget]
        private CheckButton chkExtDisplay;
        [Widget]
        private Alignment algExtDisplay;
        [Widget]
        private CheckButton chkScale;
        [Widget]
        private Alignment algScale;
        [Widget]
        private CheckButton chkCardReader;
        [Widget]
        private Alignment algCardReader;

#pragma warning restore 649

        #endregion

        #region Overrides of StepBase

        public override string Title
        {
            get { return Translator.GetString ("Devices"); }
        }

        public override int Ordinal
        {
            get { return 20; }
        }

        public override AssistType Group
        {
            get { return AssistType.ApplicationSetup; }
        }

        #endregion

        protected override void CreateBody ()
        {
            CreateBody (Translator.GetString ("Add your peripheral devices"));
         
            XML form = FormHelper.LoadGladeXML ("SetupAssistant.StepAddDevices.glade", "tblRoot");
            form.Autoconnect (this);

            chkReceiptPrinter.Label = Translator.GetString ("I have a cash receipt printer");
            chkKitchenPrinter.Label = Translator.GetString ("I have a kitchen printer");
            chkExtDisplay.Label = Translator.GetString ("I have an external display connected to this computer");
            chkScale.Label = Translator.GetString ("I have an electronic scale connected to this computer");
            chkCardReader.Label = Translator.GetString ("I have a card reader connected to the serial port");

            vboBody.PackStart (tblRoot, true, true, 6);

            chkReceiptPrinter.Toggled += chkReceiptPrinter_Toggled;
            chkKitchenPrinter.Toggled += chkKitchenPrinter_Toggled;
            chkExtDisplay.Toggled += chkExtDisplay_Toggled;
            chkScale.Toggled += chkScale_Toggled;
            chkCardReader.Toggled += chkCardReader_Toggled;

            try {
                initializing = true;
                RefreshDevcesTable (algReceiptPrinter, DeviceType.CashReceiptPrinter, chkReceiptPrinter);
                RefreshDevcesTable (algKitchenPrinter, DeviceType.KitchenPrinter, chkKitchenPrinter);
                RefreshDevcesTable (algExtDisplay, DeviceType.ExternalDisplay, chkExtDisplay);
                RefreshDevcesTable (algScale, DeviceType.ElectronicScale, chkScale);
                RefreshDevcesTable (algCardReader, DeviceType.CardReader, chkCardReader);
            } finally {
                initializing = false;
            }

            WrapLabel footer = new WrapLabel
                {
                    Markup = string.Format (Translator.GetString ("To change the device settings later go to:{0}{1}"),
                        Environment.NewLine,
                        new PangoStyle
                            {
                                Italic = true,
                                Bold = true,
                                Text = Translator.GetString ("Edit->Devices...")
                            })
                };
            footer.Show ();
            vboBody.PackStart (footer, true, true, 4);
        }

        private void chkReceiptPrinter_Toggled (object sender, EventArgs e)
        {
            DeviceUsageToggle (algReceiptPrinter, chkReceiptPrinter, DeviceType.CashReceiptPrinter);
        }

        private void chkKitchenPrinter_Toggled (object sender, EventArgs e)
        {
            DeviceUsageToggle (algKitchenPrinter, chkKitchenPrinter, DeviceType.KitchenPrinter);
        }

        private void chkExtDisplay_Toggled (object sender, EventArgs e)
        {
            DeviceUsageToggle (algExtDisplay, chkExtDisplay, DeviceType.ExternalDisplay);
        }

        private void chkScale_Toggled (object sender, EventArgs e)
        {
            DeviceUsageToggle (algScale, chkScale, DeviceType.ElectronicScale);
        }

        private void chkCardReader_Toggled (object sender, EventArgs e)
        {
            DeviceUsageToggle (algCardReader, chkCardReader, DeviceType.CardReader);
        }

        private void DeviceUsageToggle (Alignment alignment, CheckButton check, DeviceType deviceType)
        {
            if (initializing)
                return;

            alignment.Sensitive = check.Active;

            if (!check.Active)
                return;

            Table tbl = (Table) alignment.Child;
            if (tbl.Children.Length != 0)
                return;

            using (EditNewDevice choose = new EditNewDevice (deviceType)) {
                if (choose.Run () != ResponseType.Ok) {
                    alignment.Sensitive = false;
                    check.Active = false;
                    return;
                }

                choose.GetDevice ().CommitChanges ();
            }

            RefreshDevcesTable (alignment, deviceType, check);
        }

        private void RefreshDevcesTable (Alignment alignment, DeviceType deviceType, CheckButton check = null)
        {
            Table tbl = (Table) alignment.Child;
            for (int i = tbl.Children.Length - 1; i >= 0; i--)
                tbl.Remove (tbl.Children [i]);

            uint row = 0;
            foreach (Device device in Device.GetAllByType (deviceType)) {
                Label lblDevice = new Label (device.Name) { Xalign = 0f };
                tbl.Attach (lblDevice, 0, 1, row, row + 1, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);

                Button btnRemove = new Button { Label = Translator.GetString ("Remove") };
                btnRemove.Data ["DeviceType"] = deviceType;
                btnRemove.Data ["Alignment"] = alignment;
                btnRemove.Data ["CheckButton"] = check;
                btnRemove.Data ["Device"] = device;
                btnRemove.Clicked += btnRemove_Clicked;
                tbl.Attach (btnRemove, 1, 2, row, row + 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                row++;
            }

            if (row == 0) {
                if (check != null)
                    check.Active = false;

                alignment.Visible = false;
            } else {
                if (check != null)
                    check.Active = true;

                alignment.Visible = true;
                Button btnAdd = new Button { Label = Translator.GetString ("Add") };
                btnAdd.Data ["DeviceType"] = deviceType;
                btnAdd.Data ["Alignment"] = alignment;
                btnAdd.Clicked += btnAdd_Clicked;
                tbl.Attach (btnAdd, 1, 2, row, row + 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            }

            tbl.ShowAll ();
        }

        private void btnRemove_Clicked (object sender, EventArgs e)
        {
            Button btn = (Button) sender;
            DeviceType deviceType = (DeviceType) btn.Data ["DeviceType"];
            Alignment alignment = (Alignment) btn.Data ["Alignment"];
            CheckButton check = (CheckButton) btn.Data ["CheckButton"];
            Device dev = (Device) btn.Data ["Device"];

            Device.Delete (dev.Id);
            RefreshDevcesTable (alignment, deviceType, check);
        }

        private void btnAdd_Clicked (object sender, EventArgs e)
        {
            Button btn = (Button) sender;
            DeviceType deviceType = (DeviceType) btn.Data ["DeviceType"];
            Alignment alignment = (Alignment) btn.Data ["Alignment"];

            using (EditNewDevice choose = new EditNewDevice (deviceType)) {
                if (choose.Run () != ResponseType.Ok)
                    return;

                choose.GetDevice ().CommitChanges ();
            }

            RefreshDevcesTable (alignment, deviceType);
        }

        public override bool Complete (Assistant assistant)
        {
            if (Device.GetDefaultCashReceiptPrinter () != null)
                BusinessDomain.AppConfiguration.RoundedPrices = true;

            return base.Complete (assistant);
        }
    }
}
