//
// EditNewItem.cs
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
using System.IO;
using System.Linq;
using System.Text;
using Gdk;
using Glade;
using GLib;
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Business.BarcodeGenerators;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.Model;
using Warehouse.Presentation.Widgets;
using Item = Warehouse.Business.Entities.Item;
using Key = Gdk.Key;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNewItem : DialogBase
    {
        private enum NotebookPages
        {
            BasicInfo,
            Barcodes,
            AdditionlaInfo,
            Prices,
            Groups
        }

        private Item item;
        private ItemsGroupsEditPanel gEditPanel;
        private long? defaultGroupId;
        private readonly bool allowSaveAndNew;

        private string oldName;
        private readonly List<string> barcodes = new List<string> ();
        private bool acceleratorCancelled;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEditNewItem;
        [Widget]
        private Notebook nbkMain;
        [Widget]
        private Button btnSaveAndNew;
        [Widget]
        private Button btnSave;
        [Widget]
        private Button btnCancel;

        #region Basic Info

        [Widget]
        private Label lblBasicInfoTab;
        [Widget]
        private Label lblCode;
        [Widget]
        private Label lblName;
        [Widget]
        private Label lblDisplayName;
        [Widget]
        private Label lblCatalogNumbers;
        [Widget]
        private Label lblDescription;

        [Widget]
        private Entry txtCode;
        [Widget]
        private Button btnGenerateCode;
        [Widget]
        private Entry txtName;
        [Widget]
        private Entry txtDisplayName;
        [Widget]
        private Entry txtCatalogNumber1;
        [Widget]
        private Entry txtCatalogNumber2;
        [Widget]
        private Entry txtCatalogNumber3;
        [Widget]
        private TextView txvDescription;

        #endregion

        #region Barcodes

        [Widget]
        private Table tableBarcodes;
        [Widget]
        private Label lblBarcodesTab;
        [Widget]
        private Label lblBarCode1;
        [Widget]
        private Label lblBarCode2;
        [Widget]
        private Entry txtBarCode1;
        [Widget]
        private Entry txtBarCode2;
        [Widget]
        private Label lblAdditionalBarcodes;
        [Widget]
        private Alignment alignmentFocusStopper;
        [Widget]
        private ScrolledWindow scwGrid;

        [Widget]
        private Button btnGenerateBarcode;
        [Widget]
        private ToggleButton btnBarcodeOptions;

        [Widget]
        private Label lblGeneratedBarcodeFormat;
        [Widget]
        private Table tblDefaultBarcodeOptions;
        [Widget]
        private Label lblBarcodeDefaultType;
        [Widget]
        private Label lblBarcodeDefaultTypeValue;
        [Widget]
        private Label lblBarcodeDefaultFormat;
        [Widget]
        private Label lblBarcodeDefaultFormatValue;
        [Widget]
        private Table tblBarcodeOptions;
        [Widget]
        private Label lblBarcodeType;
        [Widget]
        private ComboBox cboBarcodeType;
        [Widget]
        private Label lblBarcodeFormat;
        [Widget]
        private Entry txtBarcodeFormat;
        [Widget]
        private ComboBox cboAppendCode;

        #endregion

        #region Additional Info

        [Widget]
        private Label lblAdditionalInfoTab;
        [Widget]
        private Label lblMesUnit;
        [Widget]
        private ComboBoxEntry cbeMesUnit;
        [Widget]
        private Label lblMesRatio;
        [Widget]
        private Entry txtMesRatio;
        [Widget]
        private Label lblMesUnit2;
        [Widget]
        private ComboBoxEntry cbeMesUnit2;
        [Widget]
        private Label lblMinimalQty;
        [Widget]
        private Entry txtMinimalQty;
        [Widget]
        private Label lblNominalQty;
        [Widget]
        private Entry txtNominalQty;
        [Widget]
        private Label lblVATGroup;
        [Widget]
        private ComboBox cboVATGroup;

        #endregion

        #region Prices

        [Widget]
        private Table tblPrices;

        [Widget]
        private Label lblPricesTab;
        [Widget]
        private Label lblTradePrice;
        [Widget]
        private Label lblRegularPrice;
        [Widget]
        private Label lblPriceGroup1;
        [Widget]
        private Label lblPriceGroup2;
        [Widget]
        private Label lblPriceGroup3;
        [Widget]
        private Label lblPriceGroup4;
        [Widget]
        private Label lblPriceGroup5;
        [Widget]
        private Label lblPriceGroup6;
        [Widget]
        private Label lblPriceGroup7;
        [Widget]
        private Label lblPriceGroup8;

        [Widget]
        private Entry txtTradePrice;
        [Widget]
        private Entry txtRegularPrice;
        [Widget]
        private Entry txtPriceGroup1;
        [Widget]
        private Entry txtPriceGroup2;
        [Widget]
        private Entry txtPriceGroup3;
        [Widget]
        private Entry txtPriceGroup4;
        [Widget]
        private Entry txtPriceGroup5;
        [Widget]
        private Entry txtPriceGroup6;
        [Widget]
        private Entry txtPriceGroup7;
        [Widget]
        private Entry txtPriceGroup8;

        #endregion

        #region Groups

        [Widget]
        private Label lblGroupsTab;
        [Widget]
        private Alignment algGroups;

        #endregion

#pragma warning restore 649

        #endregion

        private ListView gridBarcodes;
        private KeyValuePair<Entry, string> [] priceGroupWarnings;
        private readonly List<EditNewItemPage> addins = new List<EditNewItemPage> ();
        private int barcodeFieldFocused = -1;

        public override string HelpFile
        {
            get { return "EditNewGoods.html"; }
        }

        public override Dialog DialogControl
        {
            get { return dlgEditNewItem; }
        }

        public EditNewItem (Item item, long? defaultGroupId = null, bool allowSaveAndNew = true)
        {
            this.item = item;
            this.defaultGroupId = defaultGroupId;
            this.allowSaveAndNew = allowSaveAndNew;

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditNewItem.glade", "dlgEditNewItem");
            form.Autoconnect (this);

            dlgEditNewItem.Icon = FormHelper.LoadImage ("Icons.Goods16.png").Pixbuf;

            btnSaveAndNew.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnSave.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            tblPrices.FocusChain = new Widget []
                {
                    txtRegularPrice, txtTradePrice,
                    txtPriceGroup1, txtPriceGroup2, txtPriceGroup3, txtPriceGroup4,
                    txtPriceGroup5, txtPriceGroup6, txtPriceGroup7, txtPriceGroup8
                };

            gEditPanel = new ItemsGroupsEditPanel ();
            algGroups.Add (gEditPanel);
            gEditPanel.Show ();

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeEntries ();
            InitializeGrid ();

            dlgEditNewItem.KeyPressEvent += dlgEditNewItem_KeyPressEvent;
            dlgEditNewItem.Shown += dlgEditNewItem_Shown;

            oldName = txtName.Text;
            txtName.Changed += txtName_Changed;
            btnGenerateCode.Clicked += btnGenerateCode_Clicked;

            tableBarcodes.FocusChain = new Widget [] { txtBarCode1, txtBarCode2, gridBarcodes, btnGenerateBarcode };

            txtBarCode1.FocusGrabbed += (sender, args) => barcodeFieldFocused = 1;
            txtBarCode1.KeyPressEvent += txtBarCode1_KeyPressEvent;
            txtBarCode2.FocusGrabbed += (sender, args) => barcodeFieldFocused = 2;
            txtBarCode2.KeyPressEvent += txtBarCode2_KeyPressEvent;
            btnGenerateBarcode.Clicked += btnGenerateBarcode_Clicked;
            btnBarcodeOptions.Toggled += (sender, args) =>
                {
                    tblDefaultBarcodeOptions.Visible = !btnBarcodeOptions.Active;
                    tblBarcodeOptions.Visible = btnBarcodeOptions.Active;
                };
            cboAppendCode.Changed += (sender, args) =>
                {
                    txtBarcodeFormat.Text += cboAppendCode.GetSelectedValue ();
                    cboAppendCode.SetSelection (0);
                };

            txtPriceGroup3.KeyPressEvent += txtPriceGroup3_KeyPressEvent;
            txtPriceGroup4.KeyPressEvent += txtPriceGroup4_KeyPressEvent;

            txtRegularPrice.Data.Add (0, PriceGroup.RegularPrice);
            txtTradePrice.Data.Add (0, PriceGroup.TradePrice);
            txtPriceGroup1.Data.Add (0, PriceGroup.PriceGroup1);
            txtPriceGroup2.Data.Add (0, PriceGroup.PriceGroup2);
            txtPriceGroup3.Data.Add (0, PriceGroup.PriceGroup3);
            txtPriceGroup4.Data.Add (0, PriceGroup.PriceGroup4);
            txtPriceGroup5.Data.Add (0, PriceGroup.PriceGroup5);
            txtPriceGroup6.Data.Add (0, PriceGroup.PriceGroup6);
            txtPriceGroup7.Data.Add (0, PriceGroup.PriceGroup7);
            txtPriceGroup8.Data.Add (0, PriceGroup.PriceGroup8);

            priceGroupWarnings = new []
                {
                    new KeyValuePair<Entry, string> (txtRegularPrice, Translator.GetString ("Invalid retail price value!")), 
                    new KeyValuePair<Entry, string> (txtTradePrice, Translator.GetString ("Invalid wholesale price value!")), 
                    new KeyValuePair<Entry, string> (txtPriceGroup1, Translator.GetString ("Invalid price group 1 value!")), 
                    new KeyValuePair<Entry, string> (txtPriceGroup2, Translator.GetString ("Invalid price group 2 value!")), 
                    new KeyValuePair<Entry, string> (txtPriceGroup3, Translator.GetString ("Invalid price group 3 value!")), 
                    new KeyValuePair<Entry, string> (txtPriceGroup4, Translator.GetString ("Invalid price group 4 value!")), 
                    new KeyValuePair<Entry, string> (txtPriceGroup5, Translator.GetString ("Invalid price group 5 value!")), 
                    new KeyValuePair<Entry, string> (txtPriceGroup6, Translator.GetString ("Invalid price group 6 value!")), 
                    new KeyValuePair<Entry, string> (txtPriceGroup7, Translator.GetString ("Invalid price group 7 value!")), 
                    new KeyValuePair<Entry, string> (txtPriceGroup8, Translator.GetString ("Invalid price group 8 value!"))
                };

            btnSaveAndNew.Visible = allowSaveAndNew;

            foreach (EditNewItemPage settingsPage in AddinManager.GetExtensionNodes ("/Warehouse/Presentation/EditNewItem")
                .Cast<TypeExtensionNode> ()
                .Select (node => node.CreateInstance () as EditNewItemPage)
                .Where (page => page != null)
                .OrderBy (page => page.Priority)) {
                settingsPage.Index = nbkMain.AppendPage (settingsPage, settingsPage.PageLabel);
                settingsPage.LoadSettings (item);
                settingsPage.Show ();
                addins.Add (settingsPage);
            }
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            ConfigurationHolder config = BusinessDomain.AppConfiguration;
            dlgEditNewItem.Title = item != null && item.Id > 0 ?
                Translator.GetString ("Edit Item") : Translator.GetString ("New Item");

            lblBasicInfoTab.SetText (Translator.GetString ("General information"));
            lblCode.SetText (Translator.GetString ("Code:"));
            btnGenerateCode.SetChildLabelText (Translator.GetString ("Generate"));
            lblName.SetText (Translator.GetString ("Item:"));
            lblDisplayName.SetText (Translator.GetString ("Display name:"));
            lblCatalogNumbers.SetText (Translator.GetString ("Catalogue:"));
            lblDescription.SetText (Translator.GetString ("Description:"));

            lblBarcodesTab.SetText (Translator.GetString ("Barcodes"));
            lblBarCode1.SetText (Translator.GetString ("Barcode 1:"));
            lblBarCode2.SetText (Translator.GetString ("Barcode 2:"));
            lblAdditionalBarcodes.SetText (Translator.GetString ("Additional barcodes:"));
            btnGenerateBarcode.SetChildLabelText (Translator.GetString ("Generate"));
            btnBarcodeOptions.SetChildLabelText (Translator.GetString ("Change format"));
            lblGeneratedBarcodeFormat.SetText (Translator.GetString ("Barcode generation format"));
            lblBarcodeDefaultType.SetText (Translator.GetString ("Type:"));
            lblBarcodeDefaultTypeValue.SetText (config.GeneratedBarcodeType.ToString ());
            lblBarcodeDefaultFormat.SetText (Translator.GetString ("Format:"));
            switch (config.GeneratedBarcodeType) {
                case GeneratedBarcodeType.EAN8:
                    lblBarcodeDefaultFormatValue.SetText (config.GeneratedBarcodePrefix.PadRight (7, '#') + "C");
                    break;
                default:
                    lblBarcodeDefaultFormatValue.SetText (config.GeneratedBarcodePrefix.PadRight (12, '#') + "C");
                    break;
            }

            lblBarcodeType.SetText (Translator.GetString ("Type:"));
            lblBarcodeFormat.SetText (Translator.GetString ("Format:"));

            KeyValuePair<string, string> [] appendCodes = {
                    new KeyValuePair<string, string> (string.Empty, Translator.GetString ("<select code to append>", "Barcode format")),
                    new KeyValuePair<string, string> ("#", Translator.GetString ("Generated digit (#)")),
                    new KeyValuePair<string, string> ("WW.WWW", Translator.GetString ("Weight (WW.WWW)")),
                    new KeyValuePair<string, string> ("C", Translator.GetString ("Ignored digit (C)")),
                    new KeyValuePair<string, string> ("LLLLLL", Translator.GetString ("Lot number (LLLLLL)"))
                };
            cboAppendCode.Load (appendCodes, "Key", "Value");

            lblAdditionalInfoTab.SetText (Translator.GetString ("Additional information"));
            lblMesUnit.SetText (Translator.GetString ("Measure:"));
            lblMesRatio.SetText (Translator.GetString ("Measure ratio:"));
            lblMesUnit2.SetText (Translator.GetString ("Measure 2:"));
            lblMinimalQty.SetText (Translator.GetString ("Minimal quantity:"));
            lblNominalQty.SetText (Translator.GetString ("Nominal quantity:"));
            lblVATGroup.SetText (Translator.GetString ("VAT Group:"));

            lblPricesTab.SetText (Translator.GetString ("Prices"));
            lblTradePrice.SetText (Translator.GetString ("Wholesale price:"));
            lblRegularPrice.SetText (Translator.GetString ("Retail price:"));
            lblPriceGroup1.SetText (Translator.GetString ("Price group 1:"));
            lblPriceGroup2.SetText (Translator.GetString ("Price group 2:"));
            lblPriceGroup3.SetText (Translator.GetString ("Price group 3:"));
            lblPriceGroup4.SetText (Translator.GetString ("Price group 4:"));
            lblPriceGroup5.SetText (Translator.GetString ("Price group 5:"));
            lblPriceGroup6.SetText (Translator.GetString ("Price group 6:"));
            lblPriceGroup7.SetText (Translator.GetString ("Price group 7:"));
            lblPriceGroup8.SetText (Translator.GetString ("Price group 8:"));

            lblGroupsTab.SetText (Translator.GetString ("Groups"));

            btnSaveAndNew.SetChildLabelText (item != null ?
                Translator.GetString ("Save as New", "Item") : Translator.GetString ("Save and New", "Item"));
            btnSave.SetChildLabelText (Translator.GetString ("Save"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        private void InitializeGrid ()
        {
            gridBarcodes = new ListView ();

            ColumnController columnController = new ColumnController ();
            Column column = new Column (string.Empty, new CellText (string.Empty) { IsEditable = true }, 1);
            column.ButtonPressEvent += Column_ButtonPressEvent;
            column.KeyPressEvent += Column_KeyPressEvent;
            columnController.Add (column);

            scwGrid.Add (gridBarcodes);
            gridBarcodes.HeaderVisible = false;
            gridBarcodes.Show ();

            BindingListModel<string> bindingListModel = new BindingListModel<string> (barcodes);

            gridBarcodes.ColumnController = columnController;
            gridBarcodes.Model = bindingListModel;
            gridBarcodes.AllowSelect = false;
            gridBarcodes.CellsFucusable = true;
            gridBarcodes.ManualFucusChange = true;
            gridBarcodes.RulesHint = true;
            gridBarcodes.CellKeyPressEvent += GridBarcodes_CellKeyPressEvent;
            gridBarcodes.CellFocusIn += GridBarcodes_CellFocusIn;
        }

        private void InitializeEntries ()
        {
            LazyListModel<MesUnit> units = MesUnit.GetAll ();

            if (item == null) {
                item = new Item ();

                if (defaultGroupId.HasValue)
                    gEditPanel.SelectGroupId ((int) defaultGroupId);

                if (BusinessDomain.AppConfiguration.AutoGenerateItemCodes)
                    item.AutoGenerateCode ();
            } else
                gEditPanel.SelectGroupId (item.GroupId);

            txtCode.Text = item.Code;
            txtName.Text = item.Name;
            txtDisplayName.Text = item.Name2;
            txtCatalogNumber1.Text = item.Catalog;
            txtCatalogNumber2.Text = item.Catalog2;
            txtCatalogNumber3.Text = item.Catalog3;
            txvDescription.Buffer.Text = item.Description;

            txtBarCode1.Text = item.BarCode;
            txtBarCode2.Text = item.BarCode2;

            barcodes.Clear ();
            if (!string.IsNullOrWhiteSpace (item.BarCode3))
                foreach (string barcode in item.BarCode3.Split (new [] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    barcodes.Add (barcode);

            if (barcodes.Count == 0)
                barcodes.Add (string.Empty);

            List<KeyValuePair<object, string>> barCodeTypes = Enum.GetValues (typeof (GeneratedBarcodeType))
                .Cast<object> ()
                .Select (value => new KeyValuePair<object, string> (value, value.ToString ()))
                .OrderBy (p => p.Value).ToList ();
            cboBarcodeType.Load (barCodeTypes, "Key", "Value", BusinessDomain.AppConfiguration.CustomGeneratedBarcodeType);
            txtBarcodeFormat.Text = BusinessDomain.AppConfiguration.CustomGeneratedBarcodeFormat;

            List<MesUnit> validUnits = units.Where (u => !string.IsNullOrWhiteSpace (u.Name)).ToList ();

            cbeMesUnit.Load (validUnits, "Name", "Name");
            if (!string.IsNullOrWhiteSpace (item.MUnit))
                cbeMesUnit.Entry.Text = item.MUnit;
            txtMesRatio.Text = Number.ToEditString (item.MUnitRatio);
            cbeMesUnit2.Load (validUnits, "Name", "Name");
            if (!string.IsNullOrWhiteSpace (item.MUnit))
                cbeMesUnit2.Entry.Text = item.MUnit2;
            txtMinimalQty.Text = Quantity.ToEditString (item.MinimalQuantity);
            txtNominalQty.Text = Quantity.ToEditString (item.NominalQuantity);

            LazyListModel<VATGroup> allGroups = VATGroup.GetAll ();
            List<KeyValuePair<long, string>> vatList = new List<KeyValuePair<long, string>> (allGroups
                .Select (vatGroup => new KeyValuePair<long, string> (vatGroup.Id,
                    string.Format ("{0} ({1})", vatGroup.Name, Percent.ToString (vatGroup.VatValue)))));
            cboVATGroup.Load (vatList, "Key", "Value", item.VatGroupId);

            SetPrices (item);
        }

        private void SetPrices (Item sourceItem)
        {
            txtRegularPrice.Text = Currency.ToEditString (sourceItem.RegularPrice);
            txtTradePrice.Text = Currency.ToEditString (sourceItem.TradePrice);
            txtPriceGroup1.Text = Currency.ToEditString (sourceItem.PriceGroup1);
            txtPriceGroup2.Text = Currency.ToEditString (sourceItem.PriceGroup2);
            txtPriceGroup3.Text = Currency.ToEditString (sourceItem.PriceGroup3);
            txtPriceGroup4.Text = Currency.ToEditString (sourceItem.PriceGroup4);
            txtPriceGroup5.Text = Currency.ToEditString (sourceItem.PriceGroup5);
            txtPriceGroup6.Text = Currency.ToEditString (sourceItem.PriceGroup6);
            txtPriceGroup7.Text = Currency.ToEditString (sourceItem.PriceGroup7);
            txtPriceGroup8.Text = Currency.ToEditString (sourceItem.PriceGroup8);
        }

        [ConnectBefore]
        private void dlgEditNewItem_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            switch (args.Event.Key) {
                case Key.Return:
                case Key.KP_Enter:
                    if (BarcodeFocused () || btnSave.HasFocus)
                        dlgEditNewItem.PropagateKeyEvent (args.Event);
                    break;
            }
        }

        private void dlgEditNewItem_Shown (object sender, EventArgs e)
        {
            if (BusinessDomain.AppConfiguration.AutoGenerateItemCodes)
                txtName.GrabFocus ();
            else
                txtCode.GrabFocus ();
        }

        private void txtName_Changed (object sender, EventArgs e)
        {
            if (txtDisplayName.Text == oldName)
                txtDisplayName.Text = txtName.Text;

            oldName = txtName.Text;
        }

        private void btnGenerateCode_Clicked (object sender, EventArgs e)
        {
            item.AutoGenerateCode ();
            txtCode.Text = item.Code;
        }

        private void GridBarcodes_CellKeyPressEvent (object sender, CellKeyPressEventArgs args)
        {
            if (KeyShortcuts.IsScreenModifierControl (args.EventKey.State) &&
                args.EventKey.Key == KeyShortcuts.DeleteKey.Key) {
                DeleteGridRow ();
                // prevent the "Delete" key from acting upon the content of 
                // the newly edited cell (i.e. from deleting the content)
                args.MarkAsHandled ();
                return;
            }

            switch (args.EventKey.Key) {
                case Key.ISO_Left_Tab:
                    if (args.Cell.Row == 0 && args.Cell.Column == 0)
                        alignmentFocusStopper.GrabFocus ();
                    break;
            }
        }

        private void GridBarcodes_CellFocusIn (object sender, CellEventArgs args)
        {
            barcodeFieldFocused = 3;
        }

        private void DeleteGridRow ()
        {
            if (!gridBarcodes.EditedCell.IsValid)
                return;

            if (barcodes.Count > 1) {
                int col = gridBarcodes.EditedCell.Column;
                int row = gridBarcodes.EditedCell.Row;
                int newRow;

                if (row == barcodes.Count - 1) {
                    // If we are deleting the last row move one row up
                    barcodes.RemoveAt (row);
                    newRow = row - 1;
                } else {
                    // If we are deleting row from somewhere in between stay on the same line
                    barcodes.RemoveAt (row);
                    newRow = row;
                }

                EditGridCell (newRow, col);
            } else {
                barcodes.Clear ();
                barcodes.Add (string.Empty);

                EditGridCell (0, 0);
            }
        }

        private void Column_KeyPressEvent (object sender, CellKeyPressEventArgs args)
        {
            switch (args.GdkKey) {
                case Key.Tab:
                case Key.Return:
                case Key.KP_Enter:
                    if (args.Editing) {
                        if (EvaluateBarcode (args.Cell.Row, args.Entry.Text)) {
                            if (barcodes.Count <= gridBarcodes.EditedCell.Row + 1)
                                barcodes.Add (string.Empty);

                            EditGridCell (gridBarcodes.EditedCell.Row + 1, 0);
                        } else {
                            gridBarcodes.CancelCellEdit ();
                            if (args.GdkKey != Key.Tab)
                                btnSave.GrabFocus ();
                        }
                    }
                    break;

                case Key.Right:
                    if (args.Editing) {
                        // If the cursor is at the end of the text
                        if (args.Entry.CursorPosition == args.Entry.Text.Length) {
                            EvaluateBarcode (args.Cell.Row, args.Entry.Text);
                            if (barcodes.Count <= gridBarcodes.EditedCell.Row + 1)
                                barcodes.Add (string.Empty);

                            EditGridCell (gridBarcodes.EditedCell.Row + 1, 0);
                        }
                    }
                    break;

                case Key.Left:
                case Key.ISO_Left_Tab:
                    if (args.Editing) {
                        // If the cursor is at the end of the text
                        if (args.Entry.CursorPosition == 0 ||
                            args.GdkKey == Key.ISO_Left_Tab) {
                            EvaluateBarcode (args.Cell.Row, args.Entry.Text);
                            EditGridCell (gridBarcodes.EditedCell.Row - 1, 0);
                        }
                    }
                    break;

                case Key.Up:
                case Key.KP_Up:
                    if (args.Editing) {
                        if (gridBarcodes.EditedCell.Row > 0) {
                            EvaluateBarcode (args.Cell.Row, args.Entry.Text);
                            EditGridCell (gridBarcodes.EditedCell.Row - 1, 0);
                        }
                    }
                    break;

                case Key.Down:
                case Key.KP_Down:
                    if (args.Editing) {
                        if (gridBarcodes.EditedCell.Row < gridBarcodes.Model.Count - 1) {
                            EvaluateBarcode (args.Cell.Row, args.Entry.Text);
                            EditGridCell (gridBarcodes.EditedCell.Row + 1, 0);
                        }
                    }
                    break;

                case Key.BackSpace:
                    if (args.Editing && args.Entry.Text.Length == 0)
                        DeleteGridRow ();
                    break;
            }
        }

        private void Column_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            if (args.EventButton.Type == EventType.TwoButtonPress)
                EditGridCell (args.Cell.Row, args.Cell.Column);
        }

        private bool EvaluateBarcode (int row, string value)
        {
            barcodes [row] = value;
            if (string.IsNullOrWhiteSpace (value))
                return false;

            return row < barcodes.Count - 1 || GetBarcode3 ().Length <= 254;
        }

        private void EditGridCell (int row, int column)
        {
            if (row >= 0 && row < gridBarcodes.Model.Count)
                gridBarcodes.BeginCellEdit (new CellEventArgs (column, row));
        }

        [ConnectBefore]
        private void txtBarCode1_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            switch (args.Event.Key) {
                case Key.Return:
                case Key.KP_Enter:
                    txtBarCode2.GrabFocus ();
                    break;
            }
        }

        [ConnectBefore]
        private void txtBarCode2_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            switch (args.Event.Key) {
                case Key.Tab:
                case Key.Return:
                case Key.KP_Enter:
                    EditGridCell (0, 0);
                    break;
            }
        }

        private void btnGenerateBarcode_Clicked (object sender, EventArgs e)
        {
            if (btnBarcodeOptions.Active)
                try {
                    SetGeneratedBarcodeValue (BarcodeGenerator.GenerateCustom ((GeneratedBarcodeType) cboBarcodeType.GetSelectedValue (), txtBarcodeFormat.Text));
                } catch (InvalidDataException ex) {
                    MessageError.ShowDialog (ex.Message, ErrorSeverity.Error, ex);
                }
            else
                try {
                    SetGeneratedBarcodeValue (BarcodeGenerator.Generate (BusinessDomain.AppConfiguration.GeneratedBarcodeType));
                } catch (InvalidDataException ex) {
                    MessageError.ShowDialog (Translator.GetString ("Barcode cannot be generated with the current settings. All barcode numbers with the specified prefix and type are in use."),
                        ErrorSeverity.Error, ex);
                }
        }

        private bool BarcodeFocused ()
        {
            return txtBarCode1.HasFocus ||
                txtBarCode2.HasFocus ||
                (gridBarcodes.EditedCell.IsValid && nbkMain.Page == (int) NotebookPages.Barcodes);
        }

        private void SetGeneratedBarcodeValue (string value)
        {
            if (nbkMain.Page != (int) NotebookPages.Barcodes)
                return;

            if (barcodeFieldFocused == 2)
                txtBarCode2.Text = value;
            else if (barcodeFieldFocused == 3 && gridBarcodes.EditedCell.IsValid)
                gridBarcodes.EditedCellValue = value;
            else
                txtBarCode1.Text = value;
        }

        [ConnectBefore]
        private void txtPriceGroup3_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (args.Event.Key == Key.Down || args.Event.Key == Key.KP_Down) {
                txtPriceGroup4.GrabFocus ();
                args.RetVal = true;
            }
        }

        [ConnectBefore]
        private void txtPriceGroup4_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (args.Event.Key == Key.Up || args.Event.Key == Key.KP_Up) {
                txtPriceGroup3.GrabFocus ();
                args.RetVal = true;
            }
        }

        public Item GetItem ()
        {
            item.Code = txtCode.Text.Trim ();
            item.Name = txtName.Text.Trim ();
            item.Name2 = txtDisplayName.Text.Trim ();
            item.BarCode = txtBarCode1.Text.Trim ();
            item.BarCode2 = txtBarCode2.Text.Trim ();
            item.BarCode3 = GetBarcode3 ();

            item.Catalog = txtCatalogNumber1.Text.Trim ();
            item.Catalog2 = txtCatalogNumber2.Text.Trim ();
            item.Catalog3 = txtCatalogNumber3.Text.Trim ();
            item.Description = txvDescription.Buffer.Text.Trim ();

            item.MUnit = cbeMesUnit.Entry.Text.Trim ();
            item.MUnitRatio = Number.ParseExpression (txtMesRatio.Text);
            item.MUnit2 = cbeMesUnit2.Entry.Text.Trim ();
            item.MinimalQuantity = Quantity.ParseExpression (txtMinimalQty.Text);
            item.NominalQuantity = Quantity.ParseExpression (txtNominalQty.Text);

            item.VatGroupId = (long) cboVATGroup.GetSelectedValue ();

            item.RegularPrice = Currency.ParseExpression (txtRegularPrice.Text);
            item.TradePrice = Currency.ParseExpression (txtTradePrice.Text);
            item.PriceGroup1 = Currency.ParseExpression (txtPriceGroup1.Text);
            item.PriceGroup2 = Currency.ParseExpression (txtPriceGroup2.Text);
            item.PriceGroup3 = Currency.ParseExpression (txtPriceGroup3.Text);
            item.PriceGroup4 = Currency.ParseExpression (txtPriceGroup4.Text);
            item.PriceGroup5 = Currency.ParseExpression (txtPriceGroup5.Text);
            item.PriceGroup6 = Currency.ParseExpression (txtPriceGroup6.Text);
            item.PriceGroup7 = Currency.ParseExpression (txtPriceGroup7.Text);
            item.PriceGroup8 = Currency.ParseExpression (txtPriceGroup8.Text);

            long selectedGroupId = gEditPanel.GetSelectedGroupId ();
            if (selectedGroupId == PartnersGroup.DeletedGroupId) {
                item.Deleted = true;
                item.GroupId = ItemsGroup.DefaultGroupId;
            } else
                item.GroupId = selectedGroupId;

            return item;
        }

        private string GetBarcode3 ()
        {
            StringBuilder barcodeBuilder = new StringBuilder ();
            foreach (var barcode in barcodes) {
                if (string.IsNullOrWhiteSpace (barcode.Trim ()))
                    continue;

                barcodeBuilder.Append (barcode.Trim ());
                barcodeBuilder.Append (',');
            }

            if (barcodeBuilder.Length > 0)
                barcodeBuilder.Remove (barcodeBuilder.Length - 1, 1);

            return barcodeBuilder.ToString ();
        }

        private bool Validate ()
        {
            foreach (KeyValuePair<Entry, string> pair in priceGroupWarnings) {
                Entry txtPrice = pair.Key;
                if (Currency.IsValidExpression (txtPrice.Text))
                    continue;

                MessageError.ShowDialog (pair.Value);
                SelectInvalidPrice (txtPrice);
                return false;
            }

            if (!CheckPricesSaleLessThanPurchase ())
                return false;

            Item ret = GetItem ();
            if (string.IsNullOrWhiteSpace (ret.MUnit))
                if (!OnValidateCallback (Translator.GetString ("Measurement unit cannot be empty!"), ErrorSeverity.Error, (int) Item.ErrorCodes.MeasUnitEmpty, null))
                    return false;

            BusinessDomain.AppConfiguration.CustomGeneratedBarcodeType = (GeneratedBarcodeType) cboBarcodeType.GetSelectedValue ();
            BusinessDomain.AppConfiguration.CustomGeneratedBarcodeFormat = txtBarcodeFormat.Text;

            return GetItem ().Validate (OnValidateCallback, null);
        }

        private bool OnValidateCallback (string message, ErrorSeverity severity, int code, StateHolder state)
        {
            using (MessageError dlgError = new MessageError (message, severity))
                if (severity == ErrorSeverity.Warning) {
                    dlgError.Buttons = MessageButtons.YesNo;
                    if (dlgError.Run () != ResponseType.Yes) {
                        SelectFieldByCode ((Item.ErrorCodes) code);
                        return false;
                    }
                } else {
                    dlgError.Run ();
                    SelectFieldByCode ((Item.ErrorCodes) code);
                    return false;
                }

            return true;
        }

        private bool CheckPricesSaleLessThanPurchase ()
        {
            if (!BusinessDomain.AppConfiguration.WarnPricesSaleLowerThanPurchase)
                return true;

            foreach (Entry txtPrice in priceGroupWarnings.Select (p => p.Key)) {
                double price = Currency.ParseExpression (txtPrice.Text);
                if (item.TradeInPrice <= price || price <= 0)
                    continue;

                Entry txt = txtPrice;
                string priceGroupText = Currency.GetAllSalePriceGroups ()
                    .Where (priceGroup => priceGroup.Key == (int) txt.Data [0])
                    .Select (priceGroup => priceGroup.Value)
                    .First ();

                using (MessageYesNoRemember dialog = new MessageYesNoRemember (
                    Translator.GetString ("Sale Price Lower than Purchase Price"), string.Empty,
                    string.Format (Translator.GetString ("The value you entered for \"{0}\" " +
                        "is lower than the purchase price of {1}. Do you want to continue?"), priceGroupText, Currency.ToString (item.TradeInPrice, PriceType.Purchase)),
                    "Icons.Question32.png")) {
                    dialog.SetButtonText (MessageButtons.Remember, Translator.GetString ("Do not warn me anymore"));
                    ResponseType responseType = dialog.Run ();
                    BusinessDomain.AppConfiguration.WarnPricesSaleLowerThanPurchase = !dialog.RememberChoice;
                    switch (responseType) {
                        case ResponseType.Yes:
                            if (dialog.RememberChoice)
                                return true;
                            break;
                        default:
                            nbkMain.Page = (int) NotebookPages.Prices;
                            txtPrice.GrabFocus ();
                            return false;
                    }
                }
            }
            return true;
        }

        private void SelectInvalidPrice (Widget ent)
        {
            nbkMain.CurrentPage = (int) NotebookPages.Prices;
            ent.GrabFocus ();
        }

        private void SelectFieldByCode (Item.ErrorCodes code)
        {
            switch (code) {
                case Item.ErrorCodes.NameEmpty:
                case Item.ErrorCodes.NameInUse:
                    nbkMain.CurrentPage = (int) NotebookPages.BasicInfo;
                    txtName.GrabFocus ();
                    break;
                case Item.ErrorCodes.CodeInUse:
                    nbkMain.CurrentPage = (int) NotebookPages.BasicInfo;
                    txtCode.GrabFocus ();
                    break;
                case Item.ErrorCodes.BarcodeInUse:
                case Item.ErrorCodes.TooManyBarcodes:
                    nbkMain.CurrentPage = (int) NotebookPages.Barcodes;
                    break;
                case Item.ErrorCodes.MeasUnitEmpty:
                    nbkMain.CurrentPage = (int) NotebookPages.AdditionlaInfo;
                    cbeMesUnit.GrabFocus ();
                    break;
            }
        }

        #region Event handling

        [ConnectBefore, UsedImplicitly]
        private void btnSave_CanActivateAccel (object o, AccelCanActivateArgs args)
        {
            acceleratorCancelled = BarcodeFocused () || btnSave.HasFocus;
        }

        [UsedImplicitly]
        private void btnSaveAndNew_Clicked (object o, EventArgs args)
        {
            long oldId = item.Id;
            item.Id = -1;
            if (!Validate ()) {
                item.Id = oldId;
                return;
            }

            Item saved = GetItem ().CommitChanges ();
            if (oldId > 0) {
                dlgEditNewItem.Respond (ResponseType.Ok);
                return;
            }

            item = null;
            InitializeEntries ();

            txtName.Text = saved.Name;
            cbeMesUnit.Entry.Text = saved.MUnit;
            cboVATGroup.SetSelection (saved.VatGroupId);

            SetPrices (saved);
        }

        [UsedImplicitly]
        private void btnSave_Clicked (object o, EventArgs args)
        {
            if (acceleratorCancelled) {
                acceleratorCancelled = false;
                return;
            }

            foreach (EditNewItemPage settingsPage in addins.Where (page => !page.SaveSettings (item))) {
                nbkMain.Page = settingsPage.Index;
                return;
            }

            if (!Validate ())
                return;

            dlgEditNewItem.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        private void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditNewItem.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
