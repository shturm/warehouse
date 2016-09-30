//
// EditNewDevice.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   06/30/2006
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
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Component.Documenting;
using Warehouse.Data;
using Warehouse.Presentation.Widgets;
using HBox = Gtk.HBox;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNewDevice : DialogBase
    {
        private readonly Dictionary<string, int> handshakings = new Dictionary<string, int> ();
        private readonly List<string> documentPrinters = new List<string> ();
        private KeyValuePair<string, int> [] encodings;
        private readonly DeviceType type = DeviceType.None;
        private Device device;
        private ItemsGroupsEditPanel gEditPanel;
        private string defaultDocumentPrinter = string.Empty;
        private MessageProgress testProgress;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEditNewDevice;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;
        [Widget]
        private Button btnTest;
        [Widget]
        private Notebook nbkMain;

        [Widget]
        private Label lblGeneralInfoTab;
        [Widget]
        private Label lblConnectionProps;
        [Widget]
        private Label lblName;
        [Widget]
        private Entry txtName;
        [Widget]
        private Label lblMake;
        [Widget]
        private ComboBox cboMake;
        [Widget]
        private Label lblModel;
        [Widget]
        private ComboBox cboModel;
        [Widget]
        private Alignment algFrontOfficeLicense;

        [Widget]
        private Frame frmConnectionProps;
        [Widget]
        private Label lblSerialPort;
        [Widget]
        private ComboBox cboSerialPort;
        [Widget]
        private Label lblSerialPortBaudRate;
        [Widget]
        private ComboBox cboSerialPortBaudRate;
        [Widget]
        private Label lblSerialPortHandshaking;
        [Widget]
        private ComboBox cboSerialPortHandshaking;
        [Widget]
        private Label lblNetworkAddress;
        [Widget]
        private Entry txtNetworkAddress;
        [Widget]
        private Label lblNetworkPort;
        [Widget]
        private SpinButton spbNetworkPort;
        [Widget]
        private CheckButton chkDefaultDocumentPrinter;
        [Widget]
        private Label lblDocumentPrinter;
        [Widget]
        private ComboBox cboDocumentPrinter;
        [Widget]
        private CheckButton chkUseCustomLineWidth;
        [Widget]
        private Label lblLineWidth;
        [Widget]
        private SpinButton spbLineWidth;

        [Widget]
        private Label lblImagesFolder;
        [Widget]
        private HBox hboImagesFolder;
        [Widget]
        private Entry txtImagesFolder;
        [Widget]
        private Button btnImagesFolder;

        [Widget]
        private Label lblOperatorCode;
        [Widget]
        private Entry txtOperatorCode;
        [Widget]
        private Label lblLogicalAddress;
        [Widget]
        private Entry txtLogicalAddress;
        [Widget]
        private Label lblPassword;
        [Widget]
        private Entry txtPassword;
        [Widget]
        private Label lblAdminPassword;
        [Widget]
        private Entry txtAdminPassword;
        [Widget]
        private Label lblEncoding;
        [Widget]
        private ComboBox cboEncoding;
        [Widget]
        private Label lblCharactersInLine;
        [Widget]
        private SpinButton spbCharactersInLine;
        [Widget]
        private Label lblBlankLinesBeforeCut;
        [Widget]
        private SpinButton spbBlankLinesBeforeCut;

        [Widget]
        private Label lblDrawerCommand;
        [Widget]
        private Entry txtDrawerCommand;

        [Widget]
        private Label lblFunctions;
        [Widget]
        private CheckButton chkEnable;
        [Widget]
        private CheckButton chkPrintCashReceipts;
        [Widget]
        private CheckButton chkPrintCustomerOrders;
        [Widget]
        private CheckButton chkPrintKitchenOrders;
        [Widget]
        private CheckButton chkDisplaySaleInfo;
        [Widget]
        private CheckButton chkReadIdCards;
        [Widget]
        private CheckButton chkMeasureWeight;
        [Widget]
        private CheckButton chkCollectSalesData;
        [Widget]
        private CheckButton chkOpenDrawer;
        [Widget]
        private CheckButton chkPrintTotalOnly;

        [Widget]
        private Label lblKitchenItemGroupsTab;
        [Widget]
        private EventBox evbKitchenItemGroups;
        [Widget]
        private CheckButton chkAllItemGroups;
        [Widget]
        private Alignment algItemGroupsPanel;

        [Widget]
        private Label lblHeaderAndFooterTab;
        [Widget]
        private EventBox evbHeaderAndFooter;
        [Widget]
        private Label lblHeaderText;
        [Widget]
        private TextView txvHeaderText;
        [Widget]
        private Label lblFooterText;
        [Widget]
        private TextView txvFooterText;
        [Widget]
        private Label lblFormatting;
        [Widget]
        private Label lblAllTextCentered;
        [Widget]
        private Label lblDoubleWidth;
        [Widget]
        private Label lblDoubleHeight;
        [Widget]
        private Label lblDoubleWidthHeight;

        [Widget]
        private Label lblReceiptDesign;
        [Widget]
        private EventBox evbReceiptDesign;
        [Widget]
        private Alignment algFiscalReceiptDesign;
        [Widget]
        private Alignment algCustomerReceiptDesign;
        [Widget]
        private Alignment algKitchenReceiptDesign;

#pragma warning restore 649

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgEditNewDevice; }
        }

        public override string HelpFile
        {
            get { return "ChooseEditDevice.html"; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        public EditNewDevice (DeviceType type)
        {
            this.type = type;

            Initialize ();
        }

        public EditNewDevice (Device device)
        {
            this.device = device;

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditNewDevice.glade", "dlgEditNewDevice");
            form.Autoconnect (this);

            dlgEditNewDevice.Icon = FormHelper.LoadImage ("Icons.Device16.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            btnTest.SetChildImage (FormHelper.LoadImage ("Icons.DeviceTest24.png"));

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeEntries ();
            dlgEditNewDevice.Shown += dlgEditNewDevice_Shown;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgEditNewDevice.Title = device != null ?
                Translator.GetString ("Edit Device") :
                Translator.GetString ("New Device");

            lblGeneralInfoTab.SetText (Translator.GetString ("General information"));
            lblConnectionProps.SetText (Translator.GetString ("Connection properties"));
            lblName.SetText (Translator.GetString ("Name:"));
            lblMake.SetText (Translator.GetString ("Make:", "Device"));
            lblModel.SetText (Translator.GetString ("Model:", "Device"));
            WrapLabel wl = new WrapLabel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Markup = new PangoStyle
                        {
                            Italic = true,
                            Text = Translator.GetString ("This driver requires a Front Office license to work without limitations after 100 sales!")
                        }
                };
            wl.Show ();
            algFrontOfficeLicense.Add (wl);

            lblSerialPort.SetText (Translator.GetString ("Serial port:"));
            lblSerialPortBaudRate.SetText (Translator.GetString ("Baud rate:"));
            lblSerialPortHandshaking.SetText (Translator.GetString ("Handshaking:"));
            lblNetworkAddress.SetText (Translator.GetString ("Network address:"));
            lblNetworkPort.SetText (Translator.GetString ("Network port:"));
            chkDefaultDocumentPrinter.Label = Translator.GetString ("Use default printer");
            lblDocumentPrinter.SetText (Translator.GetString ("Printer:"));
            chkUseCustomLineWidth.Label = Translator.GetString ("Use custom line width");
            lblImagesFolder.SetText (Translator.GetString ("Images folder:"));
            btnImagesFolder.Label = " ... ";
            lblOperatorCode.SetText (Translator.GetString ("Operator code:"));
            lblLogicalAddress.SetText (Translator.GetString ("Logical address:"));
            lblPassword.SetText (Translator.GetString ("Password:"));
            lblAdminPassword.SetText (Translator.GetString ("Administrator password:"));
            lblEncoding.SetText (Translator.GetString ("Encoding:"));
            lblCharactersInLine.SetText (Translator.GetString ("Characters in line:"));
            lblBlankLinesBeforeCut.SetText (Translator.GetString ("Blank lines before cut:"));

            lblDrawerCommand.SetText (Translator.GetString ("Open drawer command:"));

            lblFunctions.SetText (Translator.GetString ("Functions"));
            chkEnable.Label = Translator.GetString ("Enabled");
            chkPrintCashReceipts.Label = Translator.GetString ("Print cash receipts");
            chkPrintCustomerOrders.Label = Translator.GetString ("Print customer orders");
            chkPrintKitchenOrders.Label = Translator.GetString ("Print kitchen orders");
            chkDisplaySaleInfo.Label = Translator.GetString ("Display sale information");
            chkReadIdCards.Label = Translator.GetString ("Read identity cards");
            chkMeasureWeight.Label = Translator.GetString ("Measure weight");
            chkCollectSalesData.Label = Translator.GetString ("Collect sales data");
            chkOpenDrawer.Label = Translator.GetString ("Open cash drawer");
            chkPrintTotalOnly.Label = Translator.GetString ("Print only total on cash receipts");

            lblKitchenItemGroupsTab.SetText (Translator.GetString ("Items in kitchen orders"));
            chkAllItemGroups.Label = Translator.GetString ("All item groups");

            lblHeaderAndFooterTab.SetText (Translator.GetString ("Header & Footer"));
            lblHeaderText.SetText (Translator.GetString ("Header text"));
            lblFooterText.SetText (Translator.GetString ("Footer text"));
            lblFormatting.SetText (Translator.GetString ("Text formatting"));
            lblAllTextCentered.SetText (Translator.GetString ("All the text is centered. Use one or more _ characters to change the font."));
            lblDoubleWidth.SetText (string.Format ("_{0}_", Translator.GetString ("Double width")));
            lblDoubleHeight.SetText (string.Format ("__{0}__", Translator.GetString ("Double height")));
            lblDoubleWidthHeight.SetText (string.Format ("___{0}___", Translator.GetString ("Double width and height")));

            lblReceiptDesign.SetText (Translator.GetString ("Receipts Design"));
            wl = new WrapLabel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Markup = new PangoStyle
                    {
                        Italic = true,
                        Markup = string.Format (
                            Translator.GetString ("You can modify the design of the final receipts using the document designer from:\n Edit->Document templates->{0}"),
                            new PangoStyle { Bold = true, Text = Translator.GetString ("Cash Receipt") })
                    }
                };
            wl.Show ();
            algFiscalReceiptDesign.Add (wl);

            wl = new WrapLabel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Markup = new PangoStyle
                        {
                            Italic = true,
                            Markup = string.Format (
                                Translator.GetString ("You can modify the design of the client receipts using the document designer from:\n Edit->Document templates->{0}"),
                                new PangoStyle { Bold = true, Text = Translator.GetString ("Non-fiscal Cash Receipt") })
                        }
                };
            wl.Show ();
            algCustomerReceiptDesign.Add (wl);

            wl = new WrapLabel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Markup = new PangoStyle
                        {
                            Italic = true,
                            Markup = string.Format (
                                Translator.GetString ("You can modify the design of the kitchen receipts using the document designer from:\n Edit->Document templates->{0}"),
                                new PangoStyle { Bold = true, Text = Translator.GetString ("Kitchen Receipt") })
                        }
                };
            wl.Show ();
            algKitchenReceiptDesign.Add (wl);

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
            btnTest.SetChildLabelText (Translator.GetString ("Test"));
        }

        private void InitializeEntries ()
        {
            if (device == null)
                device = new Device
                    {
                        Enabled = true,
                        PrintCashReceipts = true,
                        PrintCustomerOrders = true,
                        PrintKitchenOrders = true,
                        ReadIdCards = true,
                        MeasureWeight = true,
                        CollectSalesData = true,
                        ScanBarcodes = true
                    };

            txtName.Text = device.Name;

            string [] allMakes;
            if (type == DeviceType.None)
                allMakes = BusinessDomain.DeviceManager.AllDrivers
                    .SelectMany (d => d.SupportedDevices)
                    .Select (d => d.Make)
                    .Distinct ()
                    .OrderBy (m => m)
                    .ToArray ();
            else
                allMakes = BusinessDomain.DeviceManager.AllDrivers
                    .Where (d => (d.DeviceType & type) == type)
                    .SelectMany (d => d.SupportedDevices)
                    .Select (d => d.Make)
                    .Distinct ()
                    .OrderBy (m => m)
                    .ToArray ();

            if (device.DriverInfo != null)
                cboMake.Load (allMakes, null, null, device.DeviceMake ?? device.DriverInfo.SupportedDevices [0].Make);
            else
                cboMake.Load (allMakes, null, null);

            cboSerialPort.Load (DriverHelper.GetAllSerialPorts (), null, null, device.SerialPort);

            cboSerialPortBaudRate.Load (DriverHelper.GetAllSerialPortSpeeds (), null, null, device.SerialPortBaudRate);

            handshakings.Add ("None", (int) Handshake.None);
            handshakings.Add ("RTS", (int) Handshake.RequestToSend);
            handshakings.Add ("XOn / XOff", (int) Handshake.XOnXOff);
            handshakings.Add ("RTS, XOn / XOff", (int) Handshake.RequestToSendXOnXOff);
            cboSerialPortHandshaking.Load (handshakings, "Value", "Key", device.SerialPortHandshaking);

            txtNetworkAddress.Text = device.NetworkAddress;
            spbNetworkPort.Value = device.NetworkPort;

            chkDefaultDocumentPrinter.Active = device.UseDefaultDocumentPrinter;
            chkDefaultDocumentPrinter.Toggled += chkDefaultDocumentPrinter_Toggled;
            chkDefaultDocumentPrinter_Toggled (null, null);

            documentPrinters.AddRange (BusinessDomain.AppConfiguration.GetAllInstalledPrinters ());
            defaultDocumentPrinter = BusinessDomain.AppConfiguration.GetDefaultPrinterName ();
            chkUseCustomLineWidth.Active = device.UseCustomLineWidth;
            chkUseCustomLineWidth.Toggled += chkUseCustomLineWidth_Toggled;
            chkUseCustomLineWidth_Toggled (null, null);

            string docPrinter = device.UseDefaultDocumentPrinter ? defaultDocumentPrinter : device.DocumentPrinterName;
            cboDocumentPrinter.Load (documentPrinters, null, null, docPrinter);

            txtImagesFolder.Text = device.ImagesFolder;
            btnImagesFolder.Clicked += btnImagesFolder_Clicked;
            txtOperatorCode.Text = device.OperatorCode;
            txtLogicalAddress.Text = device.LogicalAddress;
            txtPassword.Text = device.Password;
            txtAdminPassword.Text = device.AdminPassword;

            encodings = DriverHelper.GetAllEncodings ();
            cboEncoding.Load (encodings, "Value", "Key", device.Encoding);

            spbCharactersInLine.Value = device.CharactersInLine;
            spbBlankLinesBeforeCut.Value = device.BlankLinesBeforeCut;

            txtDrawerCommand.Text = device.DrawerCommand;

            chkEnable.Active = device.Enabled;
            chkPrintCashReceipts.Active = device.PrintCashReceipts;
            chkPrintCustomerOrders.Active = device.PrintCustomerOrders;
            chkPrintKitchenOrders.Active = device.PrintKitchenOrders;
            chkDisplaySaleInfo.Active = device.DisplaySaleInfo;
            chkReadIdCards.Active = device.ReadIdCards;
            chkMeasureWeight.Active = device.MeasureWeight;
            chkCollectSalesData.Active = device.CollectSalesData;
            chkOpenDrawer.Active = device.OpenDrawer;
            chkPrintTotalOnly.Active = device.PrintTotalOnly;

            gEditPanel = new ItemsGroupsEditPanel (true);
            gEditPanel.GroupsPanel.EnableGroups (device.KitchenItemGroups.Keys);
            gEditPanel.Show ();
            algItemGroupsPanel.Add (gEditPanel);

            chkAllItemGroups.Active = device.AllItemGroups;

            cboMake.Changed += CboMakeChanged;
            CboMakeChanged (null, null);
            cboModel.Changed += CboModelChanged;
            CboModelChanged (device.Id < 0 ? cboModel : null, null);
            chkPrintCashReceipts.Toggled += chkPrintCashReceipts_Toggled;
            chkCollectSalesData.Toggled += chkCollectSalesData_Toggled;
            chkPrintKitchenOrders.Toggled += chkPrintKitchenOrders_Toggled;
            chkPrintKitchenOrders_Toggled (null, null);
            chkAllItemGroups.Clicked += chkAllItemGroups_Clicked;
            chkAllItemGroups_Clicked (null, null);

            spbLineWidth.Value = device.LineWidth;

            txvHeaderText.Buffer.Text = device.HeaderText;
            txvFooterText.Buffer.Text = device.FooterText;
        }

        [UsedImplicitly]
        private void btnTest_Clicked (object sender, EventArgs e)
        {
            if (!Validate (true))
                return;

            MessageError.ShowDialog (GetDevice ().DriverInfo.Attributes.ContainsKey (DriverBase.DEVICE_TEST_ON_CONNECT) ?
                Translator.GetString ("Connection with the device was successful!") :
                Translator.GetString ("Connection parameters seem correct. Device test can not be performed!"),
                ErrorSeverity.Information);
        }

        private void CboMakeChanged (object sender, EventArgs e)
        {
            string make = (string) cboMake.GetSelectedValue ();
            SortedList<string, Type> allModels = new SortedList<string, Type> ();

            foreach (DriverInfo driverInfo in BusinessDomain.DeviceManager.AllDrivers) {
                foreach (DeviceInfo deviceInfo in driverInfo.SupportedDevices) {
                    // There might be addins with drivers for the same device so use the first one only
                    if (deviceInfo.Make == make && !allModels.ContainsKey (deviceInfo.Make))
                        allModels.Add (deviceInfo.Model, driverInfo.DriverType);
                }
            }

            cboModel.Load (allModels, "Value", "Key");
            string key = device.DeviceModel;
            if (key == null && device.DriverInfo != null && device.DriverInfo.SupportedDevices.Length > 0)
                key = device.DriverInfo.SupportedDevices [0].Model;

            int index = -1;
            if (key != null)
                index = allModels.IndexOfKey (key);

            if (index >= 0)
                cboModel.SetSelection (index);
        }

        private void CboModelChanged (object sender, EventArgs e)
        {
            Type driverType = (Type) cboModel.GetSelectedValue ();
            DriverInfo driverInfo = DriverHelper.GetDriverInfoByType (driverType);
            SerializableDictionary<string, object> attributes = null;
            if (driverInfo != null)
                attributes = driverInfo.Attributes;
            bool propertiesUsed = false;

            algFrontOfficeLicense.Visible = attributes != null && attributes.ContainsKey (DriverBase.USES_FRONT_OFFICE_LICENSE);

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_SERIAL_PORT)) {
                lblSerialPort.Visible = true;
                cboSerialPort.Visible = true;
                propertiesUsed = true;
            } else {
                lblSerialPort.Visible = false;
                cboSerialPort.Visible = false;
            }

            object defaultValue;
            if (attributes != null && attributes.ContainsKey (DriverBase.USES_BAUD_RATE)) {
                lblSerialPortBaudRate.Visible = true;
                cboSerialPortBaudRate.Visible = true;
                propertiesUsed = true;

                if (sender != null && attributes.TryGetValue (DriverBase.DEFAULT_BAUD_RATE, out defaultValue))
                    cboSerialPortBaudRate.SetSelection (defaultValue);
            } else {
                lblSerialPortBaudRate.Visible = false;
                cboSerialPortBaudRate.Visible = false;
            }

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_SERIAL_HANDSHAKING)) {
                lblSerialPortHandshaking.Visible = true;
                cboSerialPortHandshaking.Visible = true;
                propertiesUsed = true;

                if (sender != null && attributes.TryGetValue (DriverBase.DEFAULT_SERIAL_HANDSHAKING, out defaultValue))
                    cboSerialPortHandshaking.Load (handshakings, "Value", "Key", (int) defaultValue);
            } else {
                lblSerialPortHandshaking.Visible = false;
                cboSerialPortHandshaking.Visible = false;
            }

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_NETWORK_ADDRESS)) {
                lblNetworkAddress.Visible = true;
                txtNetworkAddress.Visible = true;
                propertiesUsed = true;

                if (sender != null && attributes.TryGetValue (DriverBase.DEFAULT_NETWORK_ADDRESS, out defaultValue))
                    txtNetworkAddress.Text = (string) defaultValue;
            } else {
                lblNetworkAddress.Visible = false;
                txtNetworkAddress.Visible = false;
            }

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_NETWORK_PORT)) {
                lblNetworkPort.Visible = true;
                spbNetworkPort.Visible = true;
                propertiesUsed = true;

                if (sender != null && attributes.TryGetValue (DriverBase.DEFAULT_NETWORK_PORT, out defaultValue))
                    spbNetworkPort.Value = (int) defaultValue;
            } else {
                lblNetworkPort.Visible = false;
                spbNetworkPort.Visible = false;
            }

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_DOCUMENT_PRINTER)) {
                chkDefaultDocumentPrinter.Visible = true;
                lblDocumentPrinter.Visible = true;
                cboDocumentPrinter.Visible = true;
                propertiesUsed = true;

                if (sender != null) {
                    chkDefaultDocumentPrinter.Active = BusinessDomain.AppConfiguration.UseDefaultDocumentPrinter;
                    cboDocumentPrinter.Load (documentPrinters, null, null, BusinessDomain.AppConfiguration.DocumentPrinterName);
                }
            } else {
                chkDefaultDocumentPrinter.Visible = false;
                lblDocumentPrinter.Visible = false;
                cboDocumentPrinter.Visible = false;
            }

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_USE_CUSTOM_LINE_WIDTH)) {
                chkUseCustomLineWidth.Visible = true;
                propertiesUsed = true;

                if (sender != null && attributes.TryGetValue (DriverBase.DEFAULT_USE_CUSTOM_LINE_WIDTH, out defaultValue))
                    chkUseCustomLineWidth.Active = (bool) defaultValue;
            } else {
                chkUseCustomLineWidth.Visible = false;
                chkUseCustomLineWidth.Active = true;
            }

            if (attributes != null && (attributes.ContainsKey (DriverBase.USES_CUSTOM_LINE_WIDTH) || attributes.ContainsKey (DriverBase.USES_CUSTOM_LINE_CHARS_WIDTH))) {
                lblLineWidth.SetText (attributes.ContainsKey (DriverBase.USES_CUSTOM_LINE_WIDTH) ?
                    Translator.GetString ("Line width (pixels):") : Translator.GetString ("Line width (characters):"));
                lblLineWidth.Visible = true;
                spbLineWidth.Visible = true;
                propertiesUsed = true;

                if (sender != null) {
                    if (attributes.TryGetValue (DriverBase.DEFAULT_MIN_CUSTOM_LINE_WIDTH, out defaultValue))
                        spbLineWidth.Adjustment.Lower = (int) defaultValue;

                    spbLineWidth.Value = 300;
                    if (attributes.TryGetValue (DriverBase.DEFAULT_CUSTOM_LINE_WIDTH, out defaultValue))
                        spbLineWidth.Value = (int) defaultValue;
                }
            } else {
                lblLineWidth.Visible = false;
                spbLineWidth.Visible = false;
            }

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_OUTPUT_FILE)) {
                lblImagesFolder.Visible = true;
                hboImagesFolder.Visible = true;
                propertiesUsed = true;

                if (sender != null && attributes.TryGetValue (DriverBase.DEFAULT_OUTPUT_FOLDER, out defaultValue))
                    txtImagesFolder.Text = (string) defaultValue;
            } else {
                lblImagesFolder.Visible = false;
                hboImagesFolder.Visible = false;
            }

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_OPERATOR_CODE)) {
                lblOperatorCode.Visible = true;
                txtOperatorCode.Visible = true;
                propertiesUsed = true;

                if (sender != null && attributes.TryGetValue (DriverBase.DEFAULT_OPERATOR_CODE, out defaultValue))
                    txtOperatorCode.Text = (string) defaultValue;
            } else {
                lblOperatorCode.Visible = false;
                txtOperatorCode.Visible = false;
            }

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_LOGICAL_ADDRESS)) {
                lblLogicalAddress.Visible = true;
                txtLogicalAddress.Visible = true;
                propertiesUsed = true;

                if (sender != null && attributes.TryGetValue (DriverBase.DEFAULT_LOGICAL_ADDRESS, out defaultValue))
                    txtLogicalAddress.Text = (string) defaultValue;
            } else {
                lblLogicalAddress.Visible = false;
                txtLogicalAddress.Visible = false;
            }

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_PASSWORD)) {
                lblPassword.Visible = true;
                txtPassword.Visible = true;
                propertiesUsed = true;

                if (sender != null && attributes.TryGetValue (DriverBase.DEFAULT_PASSWORD, out defaultValue))
                    txtPassword.Text = (string) defaultValue;
            } else {
                lblPassword.Visible = false;
                txtPassword.Visible = false;
            }

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_ADMIN_PASSWORD)) {
                lblAdminPassword.Visible = true;
                txtAdminPassword.Visible = true;
                propertiesUsed = true;

                if (sender != null && attributes.TryGetValue (DriverBase.DEFAULT_ADMIN_PASSWORD, out defaultValue))
                    txtAdminPassword.Text = (string) defaultValue;
            } else {
                lblAdminPassword.Visible = false;
                txtAdminPassword.Visible = false;
            }

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_ENCODING)) {
                lblEncoding.Visible = true;
                cboEncoding.Visible = true;
                propertiesUsed = true;

                if (sender != null && attributes.TryGetValue (DriverBase.DEFAULT_ENCODING, out defaultValue))
                    cboEncoding.Load (encodings, "Value", "Key", (int) defaultValue);
            } else {
                lblEncoding.Visible = false;
                cboEncoding.Visible = false;
            }

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_CHARS_IN_LINE)) {
                lblCharactersInLine.Visible = true;
                spbCharactersInLine.Visible = true;
                propertiesUsed = true;

                if (sender != null && attributes.TryGetValue (DriverBase.DEFAULT_CHARS_IN_LINE, out defaultValue))
                    spbCharactersInLine.Value = (int) defaultValue;
            } else {
                lblCharactersInLine.Visible = false;
                spbCharactersInLine.Visible = false;
            }

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_BLANK_LINES_BEFORE_CUT)) {
                lblBlankLinesBeforeCut.Visible = true;
                spbBlankLinesBeforeCut.Visible = true;
                propertiesUsed = true;

                if (sender != null && attributes.TryGetValue (DriverBase.DEFAULT_BLANK_LINES_BEFORE_CUT, out defaultValue))
                    spbBlankLinesBeforeCut.Value = (int) defaultValue;
            } else {
                lblBlankLinesBeforeCut.Visible = false;
                spbBlankLinesBeforeCut.Visible = false;
            }

            if (attributes != null && attributes.ContainsKey (DriverBase.USES_DRAWER_COMMAND)) {
                lblDrawerCommand.Visible = true;
                txtDrawerCommand.Visible = true;
                propertiesUsed = true;

                if (sender != null && attributes.TryGetValue (DriverBase.DEFAULT_DRAWER_COMMAND, out defaultValue))
                    txtDrawerCommand.Text = (string) defaultValue;
            } else {
                lblDrawerCommand.Visible = false;
                txtDrawerCommand.Visible = false;
            }

            evbHeaderAndFooter.Visible = attributes != null && attributes.ContainsKey (DriverBase.USES_HEADER_AND_FOOTER);
            evbReceiptDesign.Visible = attributes != null && attributes.ContainsKey (DriverBase.USES_RECEIPT_DOCUMENT_TEMPLATES);

            frmConnectionProps.Visible = propertiesUsed;
            chkPrintCashReceipts.Visible = driverInfo != null && driverInfo.IsCashReceiptPrinterDriver;
            chkPrintCustomerOrders.Visible = driverInfo != null && (driverInfo.IsCustomerOrderPrinterDriver || driverInfo.IsKitchenDriver);
            chkPrintKitchenOrders.Visible = driverInfo != null && (driverInfo.IsCustomerOrderPrinterDriver || driverInfo.IsKitchenDriver);
            chkDisplaySaleInfo.Visible = driverInfo != null && driverInfo.IsDisplayDriver;
            chkReadIdCards.Visible = driverInfo != null && driverInfo.IsCardReaderDriver;
            chkMeasureWeight.Visible = driverInfo != null && driverInfo.IsElectronicScaleDriver;
            chkCollectSalesData.Visible = driverInfo != null && driverInfo.IsSalesDataControllerDriver;
            chkOpenDrawer.Visible = driverInfo != null && driverInfo.IsCashDrawerDriver;
            chkPrintTotalOnly.Visible = driverInfo != null && driverInfo.IsCashReceiptPrinterDriver;
            if (sender != null && driverInfo != null) {
                if (device.Id < 0) {
                    chkPrintCustomerOrders.Active = driverInfo.IsCustomerOrderPrinterDriver;
                    chkPrintKitchenOrders.Active = driverInfo.IsKitchenDriver;
                }
                chkCollectSalesData.Active = driverInfo.IsSalesDataControllerDriver && (!driverInfo.IsCashReceiptPrinterDriver || !chkPrintCashReceipts.Active);
                chkPrintCashReceipts_Toggled (null, null);
                chkCollectSalesData_Toggled (null, null);
            }

            dlgEditNewDevice.Resize (10, 10);
        }

        private void chkDefaultDocumentPrinter_Toggled (object sender, EventArgs e)
        {
            if (chkDefaultDocumentPrinter.Active) {
                lblDocumentPrinter.Sensitive = false;
                cboDocumentPrinter.Sensitive = false;
                cboDocumentPrinter.SetSelection (documentPrinters, null, null, defaultDocumentPrinter);
            } else {
                lblDocumentPrinter.Sensitive = true;
                cboDocumentPrinter.Sensitive = true;
            }
        }

        private void chkUseCustomLineWidth_Toggled (object sender, EventArgs e)
        {
            if (chkUseCustomLineWidth.Active) {
                lblLineWidth.Sensitive = true;
                spbLineWidth.Sensitive = true;
            } else {
                lblLineWidth.Sensitive = false;
                spbLineWidth.Sensitive = false;
            }
        }

        private void btnImagesFolder_Clicked (object sender, EventArgs e)
        {
            string folder;
            if (!PresentationDomain.OSIntegration.ChooseFolder (Translator.GetString ("Choose Output Folder"),
                txtImagesFolder.Text, out folder))
                return;

            txtImagesFolder.Text = folder;
        }

        private void chkCollectSalesData_Toggled (object sender, EventArgs e)
        {
            if (!chkPrintCashReceipts.Visible)
                return;

            if (chkCollectSalesData.Active) {
                chkPrintCashReceipts.Active = false;
                chkPrintCashReceipts.Sensitive = false;
            } else
                chkPrintCashReceipts.Sensitive = true;
        }

        private void chkPrintCashReceipts_Toggled (object sender, EventArgs e)
        {
            if (!chkCollectSalesData.Visible)
                return;

            if (chkPrintCashReceipts.Active) {
                chkCollectSalesData.Active = false;
                chkCollectSalesData.Sensitive = false;
            } else
                chkCollectSalesData.Sensitive = true;
        }

        private void chkPrintKitchenOrders_Toggled (object sender, EventArgs e)
        {
            if (chkPrintKitchenOrders.Active) {
                evbKitchenItemGroups.Visible = true;
                //nbkMain.ShowTabs = true;
            } else {
                evbKitchenItemGroups.Visible = false;
                //nbkMain.ShowTabs = false;
                nbkMain.CurrentPage = 0;
            }
        }

        private void chkAllItemGroups_Clicked (object sender, EventArgs e)
        {
            gEditPanel.Sensitive = !chkAllItemGroups.Active;
        }

        private void dlgEditNewDevice_Shown (object sender, EventArgs e)
        {
            txtName.GrabFocus ();
        }

        public Device GetDevice ()
        {
            Device ret = new Device
                {
                    Id = device.Id,
                    Name = txtName.Text,
                    DeviceMake = cboMake.GetSelectedText (),
                    DeviceModel = cboModel.GetSelectedText (),
                    DriverTypeName = ((Type) cboModel.GetSelectedValue ()).AssemblyQualifiedName,
                    SerialPort = (string) cboSerialPort.GetSelectedValue (),
                    SerialPortBaudRate = (int) cboSerialPortBaudRate.GetSelectedValue (),
                    SerialPortHandshaking = (int) cboSerialPortHandshaking.GetSelectedValue (),
                    NetworkAddress = txtNetworkAddress.Text,
                    NetworkPort = spbNetworkPort.ValueAsInt,
                    UseDefaultDocumentPrinter = chkDefaultDocumentPrinter.Active,
                    DocumentPrinterName = (string) cboDocumentPrinter.GetSelectedValue (),
                    UseCustomLineWidth = chkUseCustomLineWidth.Active,
                    LineWidth = spbLineWidth.ValueAsInt,
                    ImagesFolder = txtImagesFolder.Text,
                    OperatorCode = txtOperatorCode.Text,
                    LogicalAddress = txtLogicalAddress.Text,
                    Password = txtPassword.Text,
                    AdminPassword = txtAdminPassword.Text,
                    Encoding = (int) cboEncoding.GetSelectedValue (),
                    CharactersInLine = spbCharactersInLine.ValueAsInt,
                    BlankLinesBeforeCut = spbBlankLinesBeforeCut.ValueAsInt,
                    DrawerCommand = txtDrawerCommand.Text,
                    Enabled = chkEnable.Active,
                    PrintCashReceipts = chkPrintCashReceipts.Visible && chkPrintCashReceipts.Active,
                    PrintCustomerOrders = chkPrintCustomerOrders.Visible && chkPrintCustomerOrders.Active,
                    PrintKitchenOrders = chkPrintKitchenOrders.Visible && chkPrintKitchenOrders.Active,
                    DisplaySaleInfo = chkDisplaySaleInfo.Visible && chkDisplaySaleInfo.Active,
                    ReadIdCards = chkReadIdCards.Visible && chkReadIdCards.Active,
                    MeasureWeight = chkMeasureWeight.Visible && chkMeasureWeight.Active,
                    CollectSalesData = chkCollectSalesData.Visible && chkCollectSalesData.Active,
                    OpenDrawer = chkOpenDrawer.Visible && chkOpenDrawer.Active,
                    PrintTotalOnly = chkPrintTotalOnly.Visible && chkPrintTotalOnly.Active,
                    AllItemGroups = chkAllItemGroups.Active,
                    HeaderText = txvHeaderText.Buffer.Text,
                    FooterText = txvFooterText.Buffer.Text
                };

            ret.KitchenItemGroups.Clear ();
            foreach (int groupId in gEditPanel.GroupsPanel.GetEnabledGroupIds ()) {
                ret.KitchenItemGroups.Add (groupId, true);
            }

            return ret;
        }

        private bool Validate (bool forceTest)
        {
            if (!ValidateName ())
                return false;

            DriverInfo driverInfo = DriverHelper.GetDriverInfoByType ((Type) cboModel.GetSelectedValue ());
            if (!ValidateImagesFolder (driverInfo))
                return false;

            if (!ValidatePassword (driverInfo))
                return false;

            if (!ValidateAdminPassword (driverInfo))
                return false;

            if (!ValidateOperatorId (driverInfo))
                return false;

            if (!ValidateLogicalAddress (driverInfo))
                return false;

            if (!ValidateNetworkAddress (driverInfo))
                return false;

            if (!ValidateDrawerCommand (driverInfo))
                return false;

            Device d = GetDevice ();
            if (!ValidateSerialPort (d))
                return false;

            if (forceTest || (d.Enabled && (
                d.PrintCashReceipts ||
                d.PrintCustomerOrders ||
                d.PrintKitchenOrders ||
                d.DisplaySaleInfo ||
                d.ReadIdCards ||
                d.MeasureWeight ||
                d.CollectSalesData ||
                d.ScanBarcodes))) {
                BusinessDomain.DeviceManager.DisconnectConflictingDevices (d);

                if (!TestConnection (d))
                    return false;
            }

            return true;
        }

        private bool ValidateName ()
        {
            string name = txtName.Text.Trim ();
            if (name.Length == 0) {
                MessageError.ShowDialog (Translator.GetString ("Device name cannot be empty!"));
                return false;
            }

            Device d = Device.GetByName (name);
            if (d != null && d.Id != device.Id) {
                if (Message.ShowDialog (
                    Translator.GetString ("Warning!"), string.Empty,
                    Translator.GetString ("Device with this name already exists! Do you want to continue?"), "Icons.Warning32.png",
                    MessageButtons.YesNo) != ResponseType.Yes) {
                    txtName.GrabFocus ();
                    return false;
                }
            }
            return true;
        }

        private bool ValidateImagesFolder (DriverInfo driverInfo)
        {
            if (driverInfo.Attributes.ContainsKey (DriverBase.USES_OUTPUT_FILE)) {
                if (string.IsNullOrEmpty (txtImagesFolder.Text) || !Directory.Exists (txtImagesFolder.Text)) {
                    MessageError.ShowDialog (Translator.GetString ("The images folder does not exist! Please select an existing folder."),
                        ErrorSeverity.Error);
                    txtImagesFolder.GrabFocus ();
                    return false;
                }
            }
            return true;
        }

        private bool ValidatePassword (DriverInfo driverInfo)
        {
            if (driverInfo.Attributes.ContainsKey (DriverBase.USES_PASSWORD)) {
                if (!txtPassword.Text.All (char.IsLetterOrDigit)) {
                    MessageError.ShowDialog (Translator.GetString ("The password entered contains invalid characters! The password may contain only letters or digits."),
                        ErrorSeverity.Error);
                    txtPassword.GrabFocus ();
                    return false;
                }
            }
            return true;
        }

        private bool ValidateAdminPassword (DriverInfo driverInfo)
        {
            if (driverInfo.Attributes.ContainsKey (DriverBase.USES_ADMIN_PASSWORD)) {
                if (!txtAdminPassword.Text.All (char.IsLetterOrDigit)) {
                    MessageError.ShowDialog (Translator.GetString ("The administrator password entered contains invalid characters! The administrator password may contain only letters or digits."),
                        ErrorSeverity.Error);
                    txtAdminPassword.GrabFocus ();
                    return false;
                }
            }
            return true;
        }

        private bool ValidateOperatorId (DriverInfo driverInfo)
        {
            if (driverInfo.Attributes.ContainsKey (DriverBase.USES_OPERATOR_CODE)) {
                if (!txtOperatorCode.Text.All (char.IsLetterOrDigit)) {
                    MessageError.ShowDialog (Translator.GetString ("The operator code entered contains invalid characters! The operator code may contain only letters or digits."),
                        ErrorSeverity.Error);
                    txtOperatorCode.GrabFocus ();
                    return false;
                }
            }
            return true;
        }

        private bool ValidateLogicalAddress (DriverInfo driverInfo)
        {
            if (driverInfo.Attributes.ContainsKey (DriverBase.USES_LOGICAL_ADDRESS)) {
                if (!txtLogicalAddress.Text.All (char.IsLetterOrDigit)) {
                    MessageError.ShowDialog (Translator.GetString ("The logical address entered contains invalid characters! The logical address may contain only letters or digits."),
                        ErrorSeverity.Error);
                    txtLogicalAddress.GrabFocus ();
                    return false;
                }
            }
            return true;
        }

        private bool ValidateNetworkAddress (DriverInfo driverInfo)
        {
            if (driverInfo.Attributes.ContainsKey (DriverBase.USES_NETWORK_ADDRESS)) {
                try {
                    if (string.IsNullOrWhiteSpace (txtNetworkAddress.Text))
                        throw new ArgumentException ("NetowrkAddress");

                    IPAddress ip;
                    if (!IPAddress.TryParse (txtNetworkAddress.Text, out ip))
                        Dns.GetHostEntry (txtNetworkAddress.Text);
                } catch (Exception ex) {
                    MessageError.ShowDialog (Translator.GetString ("The network address entered cannot be found!"),
                        ErrorSeverity.Error, ex);
                    txtNetworkAddress.GrabFocus ();
                    return false;
                }
            }
            return true;
        }

        private bool ValidateDrawerCommand (DriverInfo driverInfo)
        {
            if (driverInfo.Attributes.ContainsKey (DriverBase.USES_DRAWER_COMMAND)) {
                if (!txtDrawerCommand.Text.All (c => char.IsDigit (c) || c == ',')) {
                    MessageError.ShowDialog (Translator.GetString ("The entered open drawer command contains invalid characters! The open drawer command may contain only digits and commas."),
                        ErrorSeverity.Error);
                    txtDrawerCommand.GrabFocus ();
                    return false;
                }
            }
            return true;
        }

        private bool ValidateSerialPort (Device d)
        {
            if (d.DriverInfo.Attributes.ContainsKey (DriverBase.USES_SERIAL_PORT)) {
                string serialPort = (string) cboSerialPort.GetSelectedValue ();
                Device d1 = Device.GetBySerialPort (serialPort);
                if (d1 != null && d1.Enabled && d1.Id != d.Id) {
                    MessageError.ShowDialog (string.Format (Translator.GetString ("The device \"{0}\" uses the same serial port! Two devices cannot be connected to the same serial port."), d1.Name),
                        ErrorSeverity.Error);
                    return false;
                }
            }
            return true;
        }

        private bool TestConnection (Device d)
        {
            DeviceManagerBase devMan = BusinessDomain.DeviceManager;
            try {
                testProgress = new MessageProgress (Translator.GetString ("Testing Device Connection..."), null, null);
                testProgress.Show ();

                devMan.HardwareResponseWaitPoll += DevManHardwareResponseWaitPoll;
                HardwareErrorResponse res = new HardwareErrorResponse { Retry = true };

                while (res.Retry) {
                    IConnectableDevice connectable = null;
                    try {
                        connectable = devMan.ConnectDevice (d);
                        res.Retry = false;
                    } catch (HardwareErrorException hwEx) {
                        res = FormHelper.HandleHardwareError (hwEx);
                        if (!res.Retry)
                            return false;
                    } catch (Exception ex) {
                        MessageError.ShowDialog (Translator.GetString ("Connection with the device failed!"),
                            ErrorSeverity.Warning, ex);
                        return false;
                    } finally {
                        try {
                            if (connectable != null)
                                devMan.DisconnectDevice (connectable);
                        } catch {
                        }
                    }
                }
            } finally {
                devMan.HardwareResponseWaitPoll -= DevManHardwareResponseWaitPoll;
                testProgress.Dispose ();
                testProgress = null;
            }

            return true;
        }

        private void DevManHardwareResponseWaitPoll (object sender, EventArgs e)
        {
            if (testProgress != null)
                testProgress.PulseCallback ();
        }

        #region Event handling

        [UsedImplicitly]
        private void btnOK_Clicked (object o, EventArgs args)
        {
            if (!Validate (false))
                return;

            dlgEditNewDevice.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        private void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditNewDevice.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
