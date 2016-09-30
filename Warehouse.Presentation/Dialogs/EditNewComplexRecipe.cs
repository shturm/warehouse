//
// EditNewComplexRecipe.cs
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

using Glade;
using Gtk;

using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.Model;
using Item = Warehouse.Business.Entities.Item;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNewComplexRecipe : DialogBase
    {
        private int cursorAtColumn;
        private bool barcodeUsed;
        private double codeQtty;
        private bool editMode;
        private ComplexRecipe recipe;
        private ListView grdMaterials;
        private ListView grdProducts;
        private GridNavigator gridNavigator;
        private GridNavigator secondGridNavigator;

        private Column colItem;
        private Column colMUnit;
        private Column colQuantity;
        private Column colPurchaseValue;
        private Column colTotal;
        private Column colSecondItem;
        private Column colSecondMUnit;
        private Column colSecondQuantity;
        private Column colSecondPurchaseValue;
        private Column colSecondTotal;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEditNewComplexRecipe;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

        [Widget]
        private Label lblName;
        [Widget]
        private Label lblTotal;
        [Widget]
        private Label lblTotalValue;
        [Widget]
        private Label lblMaterials;
        [Widget]
        private Label lblProducts;

        [Widget]
        private Entry txtName;
        [Widget]
        private Alignment algMaterials;
        [Widget]
        private Alignment algProducts;

#pragma warning restore 649

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgEditNewComplexRecipe; }
        }

        private GridNavigator GridNavigator
        {
            get
            {
                return gridNavigator ?? (gridNavigator = new GridNavigator (grdMaterials, MatEditGridField, MatGridEditOver, MatGridEditBelow));
            }
        }

        private GridNavigator SecondGridNavigator
        {
            get
            {
                return secondGridNavigator ?? (secondGridNavigator = new GridNavigator (grdProducts, ProdEditGridField, ProdGridEditOver, ProdGridEditBelow));
            }
        }

        public EditNewComplexRecipe (ComplexRecipe recipe)
        {
            this.recipe = recipe;
            if (recipe != null) {
                foreach (ComplexRecipeDetail detail in recipe.DetailsMat)
                    detail.TotalEvaluate ();
                foreach (ComplexRecipeDetail detail in recipe.DetailsProd)
                    detail.TotalEvaluate ();
                recipe.RecalculatePrices ();
            }

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditNewComplexRecipe.glade", "dlgEditNewComplexRecipe");
            form.Autoconnect (this);

            dlgEditNewComplexRecipe.Icon = FormHelper.LoadImage ("Icons.Recipe16.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();

            dlgEditNewComplexRecipe.HeightRequest = 400;
            dlgEditNewComplexRecipe.WidthRequest = 550;

            InitializeFormStrings ();
            InitializeEntries ();
            dlgEditNewComplexRecipe.Shown += dlgEditNewComplexRecipe_Shown;
        }

        private void dlgEditNewComplexRecipe_Shown (object sender, EventArgs e)
        {
            txtName.GrabFocus ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgEditNewComplexRecipe.Title = recipe != null ?
                Translator.GetString ("Edit Recipe") :
                Translator.GetString ("New Recipe");

            lblName.SetText (Translator.GetString ("Name:"));
            lblTotal.SetText (Translator.GetString ("Total:"));
            lblMaterials.SetText (Translator.GetString ("Materials"));
            lblProducts.SetText (Translator.GetString ("Products"));

            btnOK.SetChildLabelText (Translator.GetString ("Save"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        private void InitializeEntries ()
        {
            if (recipe == null) {
                recipe = new ComplexRecipe ();
                recipe.AddNewDetail ();
                recipe.AddNewAdditionalDetail ();
                recipe.UserId = BusinessDomain.LoggedUser.Id;
                recipe.LoggedUserId = BusinessDomain.LoggedUser.Id;
                OperationTotalHide ();

                editMode = false;
            } else {
                txtName.Text = recipe.Name;
                SetOperationTotal ();
                OperationTotalShow ();

                editMode = true;
            }

            txtName.ButtonPressEvent += txtName_ButtonPressEvent;
            txtName.Focused += txtName_Focused;
            txtName.KeyPressEvent += txtName_KeyPressEvent;
            InitializeMaterialsGrid ();
            InitializeProductsGrid ();
        }

        #region Totals handling

        protected virtual void OperationTotalShow ()
        {
            lblTotalValue.Show ();
            lblTotal.Show ();
        }

        protected virtual void OperationTotalHide ()
        {
            lblTotalValue.Hide ();
            lblTotal.Hide ();
        }

        protected virtual void SetOperationTotal ()
        {
            lblTotalValue.SetText (Currency.ToString (recipe.Total, PriceType.Purchase));
        }

        #endregion

        [GLib.ConnectBefore]
        private void txtName_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            txtName_Focused (null, null);
        }

        [GLib.ConnectBefore]
        private void txtName_Focused (object o, FocusedArgs args)
        {
            grdMaterials.DisableEdit = true;
            grdProducts.DisableEdit = true;
        }

        [GLib.ConnectBefore]
        private void txtName_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            switch (args.Event.Key) {
                case Gdk.Key.Tab:
                case Gdk.Key.Return:
                case Gdk.Key.KP_Enter:
                    if (txtName.Text.Trim ().Length > 0) {
                        MatCellEdit (0, colItem.Index);
                        args.RetVal = true;
                    }
                    break;
            }
        }

        #region Materials grid handling

        private void InitializeMaterialsGrid ()
        {
            if (grdMaterials == null) {
                grdMaterials = new ListView { Name = "grdMaterials" };

                ScrolledWindow sWindow = new ScrolledWindow { HscrollbarPolicy = PolicyType.Automatic, VscrollbarPolicy = PolicyType.Automatic };
                sWindow.Add (grdMaterials);

                algMaterials.Add (sWindow);
                sWindow.Show ();
                grdMaterials.Show ();
            }

            ColumnController cc = new ColumnController ();

            CellText ct = new CellText ("ItemName") { IsEditable = true };
            colItem = new Column (Translator.GetString ("Item"), ct, 1);
            colItem.ButtonPressEvent += MatItem_ButtonPressEvent;
            colItem.KeyPressEvent += MatItem_KeyPress;
            cc.Add (colItem);

            colMUnit = new Column (Translator.GetString ("Measure"), "MUnitName", 0.1) { MinWidth = 70 };
            cc.Add (colMUnit);

            CellTextQuantity ctq = new CellTextQuantity ("Quantity") { IsEditable = true };
            colQuantity = new Column (Translator.GetString ("Qtty"), ctq, 0.1) { MinWidth = 70 };
            colQuantity.ButtonPressEvent += MatQty_ButtonPressEvent;
            colQuantity.KeyPressEvent += MatQty_KeyPress;
            cc.Add (colQuantity);

            CellTextCurrency ctc = new CellTextCurrency ("PriceIn", PriceType.Purchase);
            colPurchaseValue = new Column (Translator.GetString ("Purchase price"), ctc, 0.1) { MinWidth = 70 };
            cc.Add (colPurchaseValue);

            ctc = new CellTextCurrency ("Total", PriceType.Purchase);
            colTotal = new Column (Translator.GetString ("Amount"), ctc, 0.1) { MinWidth = 70 };
            cc.Add (colTotal);

            grdMaterials.ColumnController = cc;
            grdMaterials.Model = new BindingListModel (recipe.DetailsMat);
            grdMaterials.Model.ListChanged += OperationModel_ListChanged;
            grdMaterials.AllowSelect = false;
            grdMaterials.CellsFucusable = true;
            grdMaterials.ManualFucusChange = true;
            grdMaterials.RulesHint = true;
            grdMaterials.CellKeyPressEvent += grdMaterials_CellKeyPressEvent;
        }

        private void grdMaterials_CellKeyPressEvent (object sender, CellKeyPressEventArgs args)
        {
            if (KeyShortcuts.IsScreenModifierControl (args.EventKey.State) &&
                args.EventKey.Key == KeyShortcuts.DeleteKey.Key &&
                !editMode) {
                MatDeleteRow (true);
            }
        }

        private void MatCellEdit (int row, int column)
        {
            grdProducts.DisableEdit = true;
            grdMaterials.DisableEdit = false;

            if (OperationValidate ()) {
                if (row >= 0 && column >= 0)
                    MatEditGridField (row, column);
            } else {
                grdMaterials.DisableEdit = true;
            }
        }

        private void MatGridEditBelow (int column)
        {
            if (!grdMaterials.EditedCell.IsValid)
                return;

            if (grdMaterials.EditedCell.Row + 1 < grdMaterials.Model.Count) {
                MatEditGridField (grdMaterials.EditedCell.Row + 1, column);
            }
        }

        private void MatGridEditOver (int column)
        {
            if (!grdMaterials.EditedCell.IsValid)
                return;

            if (grdMaterials.EditedCell.Row > 0) {
                MatEditGridField (grdMaterials.EditedCell.Row - 1, column);
            }
        }

        private void MatCurrentColumnEvaluate ()
        {
            CellPosition editPos = grdMaterials.EditedCell;

            if (!editPos.IsValid)
                return;

            if (cursorAtColumn == colItem.Index) {
                MatItemEvaluate (editPos.Row, grdMaterials.EditedCellValue.ToString ());
            } else if (cursorAtColumn == colQuantity.Index) {
                MatQtyEvaluate (editPos.Row, grdMaterials.EditedCellValue.ToString ());
            }
        }

        private void MatDeleteRow (bool keepRowPos)
        {
            if (!grdMaterials.EditedCell.IsValid)
                return;

            if (recipe.DetailsMat.Count > 1) {
                int col = grdMaterials.EditedCell.Column;
                int row = grdMaterials.EditedCell.Row;
                int newRow;

                if (row == 0) {
                    // If we are deleting the first row stay on the same line
                    recipe.DetailsMat.RemoveAt (row);
                    newRow = row;
                } else if (row == recipe.DetailsMat.Count - 1) {
                    // If we are deleting the last row move one row up
                    recipe.DetailsMat.RemoveAt (row);
                    newRow = row - 1;
                } else {
                    // If we are deleting row from somewhere in between stay on the same line
                    recipe.DetailsMat.RemoveAt (row);
                    newRow = row;
                }

                if (keepRowPos)
                    MatEditGridField (newRow, col);
                else
                    MatItemEditPrev (newRow, Gdk.Key.Left);
            } else {
                recipe.DetailsMat.Clear ();
                recipe.AddNewDetail ();

                MatEditGridField (0, colItem.Index);
            }
        }

        private bool MatEditGridField (int row, int col)
        {
            grdProducts.DisableEdit = true;
            grdMaterials.DisableEdit = false;
            if (!grdMaterials.BeginCellEdit (new CellEventArgs (col, row)))
                return false;

            cursorAtColumn = col;
            //InitializeHelpStrings ();
            return true;
        }

        #region Item column handling

        private bool MatItemEvaluate (int row, string itemName)
        {
            ComplexRecipeDetail detail = recipe.DetailsMat [row];
            if (detail.ItemId >= 0 && detail.ItemName == itemName)
                return true;

            double currentQuantity = detail.Quantity;

            string codeLot;
            long lotId;
            Item item = Item.GetByAny (itemName, out barcodeUsed, out codeQtty, out codeLot, out lotId);

            if (!MatItemEvaluate (row, item, false))
                return false;

            // no quantity from the barcode scanner
            if (codeQtty.IsZero ())
                codeQtty = currentQuantity;

            return true;
        }

        private bool MatItemEvaluate (int row, Item item, bool updatePrice)
        {
            return recipe.DetailsMat [row].ItemEvaluate (item, PriceGroup.RegularPrice, updatePrice);
        }

        private void MatItemChoose (string filter)
        {
            int row;
            bool itemAdded;
            using (ChooseEditItem dialog = new ChooseEditItem (true, filter)) {
                row = grdMaterials.EditedCell.Row;
                if (dialog.Run () != ResponseType.Ok) {
                    MatEditGridField (row, colItem.Index);
                    return;
                }

                itemAdded = false;
                foreach (Item item in dialog.SelectedItems) {
                    if (recipe.DetailsMat.Count <= row)
                        recipe.AddNewDetail ();

                    if (MatItemEvaluate (row, item, true)) {
                        itemAdded = true;
                        row++;
                    } else
                        recipe.RemoveDetail (recipe.Details.Count - 1, false);
                }
            }

            if (itemAdded)
                MatItemEditNext (grdMaterials.EditedCell.Row, Gdk.Key.Return);
            else
                MatEditGridField (row, colItem.Index);
        }

        private void MatItem_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            if (grdMaterials.EditedCell != args.Cell) {
                MatCurrentColumnEvaluate ();
                MatCellEdit (args.Cell.Row, args.Cell.Column);
            } else {
                MatCellEdit (-1, -1);
            }

            if (grdMaterials.DisableEdit || args.EventButton.Type != Gdk.EventType.TwoButtonPress)
                return;

            object cellValue = grdMaterials.EditedCellValue;
            GridNavigator.ChooseCellValue (MatItemEvaluate, MatItemChoose,
                cellValue == null ? string.Empty : cellValue.ToString ());
        }

        private void MatItem_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            if (GridNavigator.ColumnKeyPress (args, colItem.Index, MatItemChoose,
                MatItemEvaluate, MatItemEditPrev, MatItemEditNext))
                return;

            string gdkKey = KeyShortcuts.KeyToString (args.EventKey);
            string quickGoods;
            if (!BusinessDomain.QuickItems.TryGetValue (gdkKey, out quickGoods))
                return;

            if (!MatItemEvaluate (grdMaterials.EditedCell.Row, quickGoods))
                return;

            MatQtyEvaluate (grdMaterials.EditedCell.Row, 1);

            if (recipe.DetailsMat.Count <= grdMaterials.EditedCell.Row + 1)
                recipe.AddNewDetail ();

            MatEditGridField (grdMaterials.EditedCell.Row + 1, colItem.Index);

            args.MarkAsHandled ();
        }

        private void MatItemEditPrev (int row, Gdk.Key keyCode)
        {
            if (keyCode == Gdk.Key.ISO_Left_Tab) {
                MatCurrentColumnEvaluate ();
                return;
            }

            GridNavigator.EditPrevOnFirst (row, keyCode, colQuantity, MatQtyEditPrev,
                r =>
                {
                    if (!editMode)
                        MatDeleteRow (false);
                });
        }

        private void MatItemEditNext (int row, Gdk.Key keyCode)
        {
            if (keyCode == Gdk.Key.Tab) {
                MatCurrentColumnEvaluate ();
                ProdEditGridField (0, colItem.Index);
                return;
            }

            if (codeQtty != 0)
                MatQtyEvaluate (row, codeQtty);

            if (barcodeUsed) {
                if (codeQtty == 0)
                    MatQtyEvaluate (row, 1);

                if (recipe.DetailsMat.Count <= row + 1)
                    recipe.AddNewDetail ();

                MatEditGridField (row + 1, colItem.Index);
            } else if (colQuantity != null && colQuantity.ListCell.IsEditable)
                MatEditGridField (row, colQuantity.Index);
            else
                MatQtyEditNext (row, keyCode);
        }

        #endregion

        #region Quantity column handling

        private bool MatQtyEvaluate (int row, string quantity)
        {
            MatQtyEvaluate (row, Quantity.ParseExpression (quantity));
            return true;
        }

        private void MatQtyEvaluate (int row, double qtyValue)
        {
            recipe.DetailsMat [row].Quantity = qtyValue;
        }

        private void MatQty_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            MatCurrentColumnEvaluate ();
            MatCellEdit (args.Cell.Row, args.Cell.Column);
        }

        private void MatQty_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            GridNavigator.ColumnKeyPress (args, colQuantity.Index,
                MatQtyEvaluate, MatQtyEditPrev, MatQtyEditNext);
        }

        private void MatQtyEditPrev (int row, Gdk.Key keyCode)
        {
            if (keyCode == Gdk.Key.ISO_Left_Tab) {
                MatCurrentColumnEvaluate ();
                return;
            }

            GridNavigator.EditPrev (row, keyCode, colItem, MatItemEditPrev);
        }

        private void MatQtyEditNext (int row, Gdk.Key keyCode)
        {
            if (keyCode == Gdk.Key.Tab) {
                MatCurrentColumnEvaluate ();
                ProdEditGridField (0, colItem.Index);
                return;
            }

            GridNavigator.EditNextOnLast (row, keyCode, colItem, MatItemEditNext, recipe.DetailsMat, recipe.AddNewDetail);
        }

        #endregion

        #endregion

        #region Products grid handling

        private void InitializeProductsGrid ()
        {
            if (grdProducts == null) {
                grdProducts = new ListView { Name = "grdProducts" };

                ScrolledWindow sWindow = new ScrolledWindow { HscrollbarPolicy = PolicyType.Automatic, VscrollbarPolicy = PolicyType.Automatic };
                sWindow.Add (grdProducts);

                algProducts.Add (sWindow);
                sWindow.Show ();
                grdProducts.Show ();
            }

            ColumnController cc = new ColumnController ();

            CellText ct = new CellText ("ItemName") { IsEditable = true };
            colSecondItem = new Column (Translator.GetString ("Item"), ct, 1);
            colSecondItem.ButtonPressEvent += ProdItem_ButtonPressEvent;
            colSecondItem.KeyPressEvent += ProdItem_KeyPress;
            cc.Add (colSecondItem);

            colSecondMUnit = new Column (Translator.GetString ("Measure"), "MUnitName", 0.1) { MinWidth = 70 };
            cc.Add (colSecondMUnit);

            CellTextQuantity ctq = new CellTextQuantity ("Quantity") { IsEditable = true };
            colSecondQuantity = new Column (Translator.GetString ("Qtty"), ctq, 0.1) { MinWidth = 70 };
            colSecondQuantity.ButtonPressEvent += ProdQty_ButtonPressEvent;
            colSecondQuantity.KeyPressEvent += ProdQty_KeyPress;
            cc.Add (colSecondQuantity);

            CellTextCurrency ctc = new CellTextCurrency ("PriceIn", PriceType.Purchase);
            colSecondPurchaseValue = new Column (Translator.GetString ("Purchase price"), ctc, 0.1) { MinWidth = 70 };
            cc.Add (colSecondPurchaseValue);

            ctc = new CellTextCurrency ("Total", PriceType.Purchase);
            colSecondTotal = new Column (Translator.GetString ("Amount"), ctc, 0.1) { MinWidth = 70 };
            cc.Add (colSecondTotal);

            grdProducts.ColumnController = cc;
            grdProducts.Model = new BindingListModel (recipe.DetailsProd);
            grdProducts.Model.ListChanged += OperationModel_ListChanged;
            grdProducts.AllowSelect = false;
            grdProducts.CellsFucusable = true;
            grdProducts.ManualFucusChange = true;
            grdProducts.RulesHint = true;
            grdProducts.CellKeyPressEvent += grdProducts_CellKeyPressEvent;
        }

        private void OperationModel_ListChanged (object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            SetOperationTotal ();
            OperationTotalShow ();
        }

        private void grdProducts_CellKeyPressEvent (object sender, CellKeyPressEventArgs args)
        {
            if (KeyShortcuts.IsScreenModifierControl (args.EventKey.State) &&
                args.EventKey.Key == KeyShortcuts.DeleteKey.Key &&
                !editMode) {
                ProdDeleteRow (true);
            }
        }

        private void ProdCellEdit (int row, int column)
        {
            grdMaterials.DisableEdit = true;
            grdProducts.DisableEdit = false;

            if (OperationValidate ()) {
                if (row >= 0 && column >= 0)
                    ProdEditGridField (row, column);
            } else {
                grdProducts.DisableEdit = true;
            }
        }

        private void ProdGridEditBelow (int column)
        {
            if (!grdProducts.EditedCell.IsValid)
                return;

            if (grdProducts.EditedCell.Row + 1 < grdProducts.Model.Count) {
                ProdEditGridField (grdProducts.EditedCell.Row + 1, column);
            }
        }

        private void ProdGridEditOver (int column)
        {
            if (!grdProducts.EditedCell.IsValid)
                return;

            if (grdProducts.EditedCell.Row > 0) {
                ProdEditGridField (grdProducts.EditedCell.Row - 1, column);
            }
        }

        private void ProdCurrentColumnEvaluate ()
        {
            CellPosition editPos = grdProducts.EditedCell;

            if (!editPos.IsValid)
                return;

            if (cursorAtColumn == colSecondItem.Index) {
                ProdItemEvaluate (editPos.Row, grdProducts.EditedCellValue.ToString ());
            } else if (cursorAtColumn == colSecondQuantity.Index) {
                ProdQtyEvaluate (editPos.Row, grdProducts.EditedCellValue.ToString ());
            }
        }

        private void ProdDeleteRow (bool keepRowPos)
        {
            if (!grdProducts.EditedCell.IsValid)
                return;

            if (recipe.DetailsProd.Count > 1) {
                int col = grdProducts.EditedCell.Column;
                int row = grdProducts.EditedCell.Row;
                int newRow;

                if (row == 0) {
                    // If we are deleting the first row stay on the same line
                    recipe.DetailsProd.RemoveAt (row);
                    newRow = row;
                } else if (row == recipe.DetailsProd.Count - 1) {
                    // If we are deleting the last row move one row up
                    recipe.DetailsProd.RemoveAt (row);
                    newRow = row - 1;
                } else {
                    // If we are deleting row from somewhere in between stay on the same line
                    recipe.DetailsProd.RemoveAt (row);
                    newRow = row;
                }

                if (keepRowPos)
                    ProdEditGridField (newRow, col);
                else
                    ProdItemEditPrev (newRow, Gdk.Key.Left);
            } else {
                recipe.DetailsProd.Clear ();
                recipe.AddNewAdditionalDetail ();

                ProdEditGridField (0, colSecondItem.Index);
            }
        }

        private bool ProdEditGridField (int row, int col)
        {
            grdMaterials.DisableEdit = true;
            grdProducts.DisableEdit = false;
            if (!grdProducts.BeginCellEdit (new CellEventArgs (col, row)))
                return false;

            cursorAtColumn = col;
            //InitializeHelpStrings ();
            return true;
        }

        #region Item column handling

        private bool ProdItemEvaluate (int row, string itemName)
        {
            ComplexRecipeDetail detail = recipe.DetailsProd [row];
            if (detail.ItemId >= 0 && detail.ItemName == itemName)
                return true;

            double currentQuantity = detail.Quantity;

            string codeLot;
            long lotId;
            Item item = Item.GetByAny (itemName, out barcodeUsed, out codeQtty, out codeLot, out lotId);

            if (!ProdItemEvaluate (row, item, true))
                return false;

            // no quantity from the barcode scanner
            if (codeQtty.IsZero ())
                codeQtty = currentQuantity;

            return true;
        }

        private bool ProdItemEvaluate (int row, Item item, bool updatePrice)
        {
            return recipe.DetailsProd [row].ItemEvaluate (item, PriceGroup.RegularPrice, updatePrice);
        }

        private void ProdItemChoose (string filter)
        {
            int row;
            bool itemAdded;
            using (ChooseEditItem dialog = new ChooseEditItem (true, filter)) {
                row = grdProducts.EditedCell.Row;
                if (dialog.Run () != ResponseType.Ok) {
                    ProdEditGridField (row, colSecondItem.Index);
                    return;
                }

                itemAdded = false;
                foreach (Item item in dialog.SelectedItems) {
                    if (recipe.DetailsProd.Count <= row)
                        recipe.AddNewAdditionalDetail ();

                    if (ProdItemEvaluate (row, item, true)) {
                        row++;
                        itemAdded = true;
                    } else
                        recipe.RemoveDetail (recipe.Details.Count - 1, false);
                }
            }

            if (itemAdded)
                ProdItemEditNext (grdProducts.EditedCell.Row, Gdk.Key.Return);
            else
                ProdEditGridField (row, colSecondItem.Index);
        }

        private void ProdItem_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            if (grdProducts.EditedCell != args.Cell) {
                ProdCurrentColumnEvaluate ();
                ProdCellEdit (args.Cell.Row, args.Cell.Column);
            } else {
                ProdCellEdit (-1, -1);
            }

            if (grdProducts.DisableEdit || args.EventButton.Type != Gdk.EventType.TwoButtonPress)
                return;

            object cellValue = grdProducts.EditedCellValue;
            SecondGridNavigator.ChooseCellValue (ProdItemEvaluate, ProdItemChoose,
                cellValue == null ? string.Empty : cellValue.ToString ());
        }

        private void ProdItem_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            if (SecondGridNavigator.ColumnKeyPress (args, colSecondItem.Index, ProdItemChoose,
                ProdItemEvaluate, ProdItemEditPrev, ProdItemEditNext))
                return;

            string gdkKey = KeyShortcuts.KeyToString (args.EventKey);
            string quickGoods;
            if (!BusinessDomain.QuickItems.TryGetValue (gdkKey, out quickGoods))
                return;

            ProdItemEvaluate (grdProducts.EditedCell.Row, quickGoods);

            ProdQtyEvaluate (grdProducts.EditedCell.Row, 1);

            if (recipe.DetailsProd.Count <= grdProducts.EditedCell.Row + 1)
                recipe.AddNewAdditionalDetail ();

            ProdEditGridField (grdProducts.EditedCell.Row + 1, colSecondItem.Index);

            args.MarkAsHandled ();
        }

        private void ProdItemEditPrev (int row, Gdk.Key keyCode)
        {
            if (keyCode == Gdk.Key.ISO_Left_Tab) {
                ProdCurrentColumnEvaluate ();
                MatEditGridField (grdProducts.Model.Count - 1, colQuantity.Index);
                return;
            }

            SecondGridNavigator.EditPrevOnFirst (row, keyCode, colSecondQuantity, ProdQtyEditPrev,
                r =>
                {
                    if (!editMode)
                        ProdDeleteRow (false);
                });
        }

        private void ProdItemEditNext (int row, Gdk.Key keyCode)
        {
            if (keyCode == Gdk.Key.Tab) {
                ProdCurrentColumnEvaluate ();
                //btnOK.GrabFocus ();
                return;
            }

            if (!codeQtty.IsZero ())
                ProdQtyEvaluate (row, codeQtty);

            if (barcodeUsed) {
                if (codeQtty.IsZero ())
                    ProdQtyEvaluate (row, 1);

                if (recipe.DetailsProd.Count <= row + 1)
                    recipe.AddNewAdditionalDetail ();

                ProdEditGridField (row + 1, colSecondItem.Index);
            } else if (colSecondQuantity != null && colSecondQuantity.ListCell.IsEditable)
                ProdEditGridField (row, colSecondQuantity.Index);
            else
                ProdQtyEditNext (row, keyCode);
        }

        #endregion

        #region Quantity column handling

        private bool ProdQtyEvaluate (int row, string quantity)
        {
            ProdQtyEvaluate (row, Quantity.ParseExpression (quantity));
            return true;
        }

        private void ProdQtyEvaluate (int row, double qtyValue)
        {
            recipe.DetailsProd [row].Quantity = qtyValue;
        }

        private void ProdQty_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            ProdCurrentColumnEvaluate ();
            ProdCellEdit (args.Cell.Row, args.Cell.Column);
        }

        private void ProdQty_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            SecondGridNavigator.ColumnKeyPress (args, colSecondQuantity.Index,
                ProdQtyEvaluate, ProdQtyEditPrev, ProdQtyEditNext);
        }

        private void ProdQtyEditPrev (int row, Gdk.Key keyCode)
        {
            if (keyCode == Gdk.Key.ISO_Left_Tab) {
                ProdCurrentColumnEvaluate ();
                MatEditGridField (grdProducts.Model.Count - 1, colQuantity.Index);
                return;
            }

            SecondGridNavigator.EditPrev (row, keyCode, colSecondItem, ProdItemEditPrev);
        }

        private void ProdQtyEditNext (int row, Gdk.Key keyCode)
        {
            if (keyCode == Gdk.Key.Tab) {
                ProdCurrentColumnEvaluate ();
                //btnOK.GrabFocus ();
                return;
            }

            SecondGridNavigator.EditNextOnLast (row, keyCode, colSecondItem, ProdItemEditNext, recipe.DetailsProd, recipe.AddNewAdditionalDetail);
        }

        #endregion

        #endregion

        public ComplexRecipe GetRecipe ()
        {
            recipe.Name = txtName.Text.Trim ();

            return recipe;
        }

        private bool Validate ()
        {
            if (!OperationValidate ())
                return false;

            if (!OperationDetailsValidate ())
                return false;

            grdMaterials.CancelCellEdit ();
            grdProducts.CancelCellEdit ();

            return GetRecipe ().Validate ((message, severity, code, state) =>
                {
                    using (MessageError dlgError = new MessageError (message, severity, null))
                        if (severity == ErrorSeverity.Warning) {
                            dlgError.Buttons = MessageButtons.YesNo;
                            if (dlgError.Run () != ResponseType.Yes)
                                return false;
                        } else {
                            dlgError.Run ();
                            return false;
                        }

                    return true;
                }, null);
        }

        private bool OperationValidate ()
        {
            if (string.IsNullOrEmpty (txtName.Text.Trim ())) {
                txtName.GrabFocus ();
                return false;
            }

            return true;
        }

        private bool OperationDetailsValidate ()
        {
            #region Validate materials

            if (recipe.DetailsMat.Count == 0)
                return false;

            MatCurrentColumnEvaluate ();
            bool hasValidMaterialQuantities = false;

            for (int i = recipe.DetailsMat.Count - 1; i >= 0; i--) {

                try {
                    // Validate Item
                    string itemName = recipe.DetailsMat [i].ItemName.Trim ();
                    // If the gooods field is empty then this line has to be skipped
                    if (itemName.Length == 0) {
                        // If this is not the first line then delete it
                        if (i > 0) {
                            recipe.DetailsMat.RemoveAt (i);
                            continue;
                        }

                        DetailsValidationWarning (Translator.GetString ("There are no valid materials!"));
                        MatEditGridField (0, colItem.Index);
                        return false;
                    }

                    Item item = Item.GetById (recipe.DetailsMat [i].ItemId);
                    if (item == null || item.Name != itemName) {
                        DetailsValidationWarning (string.Format (Translator.GetString ("Invalid item at row {0}!"), i + 1));
                        MatEditGridField (i, colItem.Index);
                        return false;
                    }

                    // Validate quantity
                    double qty = recipe.DetailsMat [i].Quantity;
                    if ((!editMode && qty <= 0) || (editMode && qty < 0)) {
                        DetailsValidationWarning (string.Format (Translator.GetString ("Invalid quantity of item \"{0}\"!"), item.Name));
                        MatEditGridField (i, colQuantity.Index);
                        return false;
                    }

                    if (qty > 0)
                        hasValidMaterialQuantities = true;
                } catch {
                    DetailsValidationWarning (string.Format (Translator.GetString ("Error at row {0}!"), i + 1));
                    MatEditGridField (i, colItem.Index);
                    return false;
                }
            }

            if (!hasValidMaterialQuantities) {
                if (editMode) {
                    ResponseType ret = DetailsValidationQuestion (Translator.GetString ("There are no materials with quantities greater than 0! The recipe will be deleted. Are you sure?"));
                    if (ret == ResponseType.No)
                        return false;
                } else {
                    DetailsValidationWarning (Translator.GetString ("The recipe must contain at least one material with quantity greater than 0."));
                    return false;
                }
            }


            #endregion

            #region Validate products

            if (recipe.DetailsProd.Count == 0)
                return false;

            ProdCurrentColumnEvaluate ();
            bool hasValidProductQuantities = false;

            for (int i = recipe.DetailsProd.Count - 1; i >= 0; i--) {

                try {
                    // Validate Item
                    string itemName = recipe.DetailsProd [i].ItemName.Trim ();
                    // If the gooods field is empty then this line has to be skipped
                    if (itemName.Length == 0) {
                        // If this is not the first line then delete it
                        if (i > 0) {
                            recipe.DetailsProd.RemoveAt (i);
                            continue;
                        }

                        DetailsValidationWarning (Translator.GetString ("There are no valid products!"));
                        ProdEditGridField (0, colSecondItem.Index);
                        return false;
                    }

                    Item item = Item.GetById (recipe.DetailsProd [i].ItemId);
                    if (item == null || item.Name != itemName) {
                        DetailsValidationWarning (string.Format (Translator.GetString ("Invalid item at row {0}!"), i + 1));
                        ProdEditGridField (i, colSecondItem.Index);
                        return false;
                    }

                    // Validate quantity
                    double qty = recipe.DetailsProd [i].Quantity;
                    if ((!editMode && qty <= 0) || (editMode && qty < 0)) {
                        DetailsValidationWarning (string.Format (Translator.GetString ("Invalid quantity of item \"{0}\"!"), item.Name));
                        ProdEditGridField (i, colSecondQuantity.Index);
                        return false;
                    }

                    if (qty > 0)
                        hasValidProductQuantities = true;
                } catch {
                    DetailsValidationWarning (string.Format (Translator.GetString ("Error at row {0}!"), i + 1));
                    ProdEditGridField (i, colSecondItem.Index);
                    return false;
                }
            }

            if (hasValidMaterialQuantities && !hasValidProductQuantities) {
                if (editMode) {
                    ResponseType ret = DetailsValidationQuestion (Translator.GetString ("There are no products with quantities greater than 0! The recipe will be deleted. Are you sure?"));
                    if (ret == ResponseType.No)
                        return false;
                } else {
                    DetailsValidationWarning (Translator.GetString ("The recipe must contain at least one product with quantity greater than 0."));
                    return false;
                }
            }

            #endregion

            for (int i = 0; i < recipe.DetailsMat.Count; i++) {
                ComplexRecipeDetail detMat = recipe.DetailsMat [i];
                foreach (ComplexRecipeDetail detProd in recipe.DetailsProd) {
                    if (detMat.ItemId != detProd.ItemId)
                        continue;

                    DetailsValidationWarning (string.Format (Translator.GetString ("The item \"{0}\" is used as a material and as a product!"), detMat.ItemName));
                    MatEditGridField (i, colItem.Index);
                    return false;
                }
            }

            if (!hasValidMaterialQuantities || !hasValidProductQuantities)
                recipe.ClearDetails ();

            return true;
        }

        private void DetailsValidationWarning (string message)
        {
            MessageError.ShowDialog (message);
        }

        private ResponseType DetailsValidationQuestion (string message)
        {
            return MessageError.ShowDialog (message, buttons: MessageButtons.YesNo);
        }

        #region Event handling

        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (!Validate ())
                return;

            dlgEditNewComplexRecipe.Respond (ResponseType.Ok);
        }

        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditNewComplexRecipe.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
