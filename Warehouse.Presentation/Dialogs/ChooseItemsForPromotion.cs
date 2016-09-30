// 
// ChooseItemsForPromotion.cs
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
using System.Collections;
using Gdk;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Data.Model;
using Item = Warehouse.Business.Entities.Item;
using Key = Gdk.Key;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseItemsForPromotion : DialogBase
    {
        #region Glade Widgets

        [Widget]
        protected ScrolledWindow scwGrid;
        [Widget]
        protected Dialog dlgChooseItemsForPromotion;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;

        #endregion

        private ListView grid;
        private GridNavigator gridNavigator;
        protected bool barcodeUsed;
        protected double codeQtty;

        private readonly BindList<SaleDetail> selectedDetails;
        private int currentColumn;

        public ChooseItemsForPromotion (IEnumerable promotions)
        {
            selectedDetails = new BindList<SaleDetail> ();
            if (promotions != null)
                foreach (SaleDetail promotion in promotions)
                    selectedDetails.Add (promotion);
            else
                selectedDetails.Add (new SaleDetail ());

            Initialize ();
        }

        public override Dialog DialogControl
        {
            get { return dlgChooseItemsForPromotion; }
        }

        public BindList<SaleDetail> SelectedDetails
        {
            get { return selectedDetails; }
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChooseItemsForPromotion.glade", "dlgChooseItemsForPromotion");
            form.Autoconnect (this);

            dlgChooseItemsForPromotion.Icon = FormHelper.LoadImage ("Icons.Goods16.png").Pixbuf;

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            InitializeGrid ();

            base.InitializeForm ();

            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseItemsForPromotion.Title = Translator.GetString ("Add Promotional Item to the Operation");

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        #region Grid

        private void InitializeGrid ()
        {
            grid = new ListView ();
            scwGrid.Add (grid);
            grid.Show ();

            grid.WidthRequest = 500;
            grid.HeightRequest = 200;

            ColumnController cc = new ColumnController ();

            CellText ct = new CellText ("ItemName") { IsEditable = true };
            colItem = new Column (Translator.GetString ("Item"), ct, 1);
            colItem.ButtonPressEvent += ItemColumn_ButtonPressEvent;
            colItem.KeyPressEvent += ItemColumn_KeyPress;
            cc.Add (colItem);

            CellTextQuantity ctq = new CellTextQuantity ("Quantity") { IsEditable = true };
            colQtty = new Column (Translator.GetString ("Qtty"), ctq, 0.1) { MinWidth = 70 };
            colQtty.ButtonPressEvent += QttyColumn_ButtonPressEvent;
            colQtty.KeyPressEvent += QtyColumn_KeyPress;
            cc.Add (colQtty);

            CellTextCurrency ctc = new CellTextCurrency ("OriginalPriceOut") { IsEditable = true };
            colSalePrice = new Column (Translator.GetString ("Price"), ctc, 0.1) { MinWidth = 70 };
            colSalePrice.ButtonPressEvent += SalePriceColumn_ButtonPressEvent;
            colSalePrice.KeyPressEvent += SalePriceColumn_KeyPress;
            cc.Add (colSalePrice);

            grid.ColumnController = cc;
            grid.Model = new BindingListModel (selectedDetails);
            grid.AllowSelect = false;
            grid.CellsFucusable = true;
            grid.ManualFucusChange = true;
            grid.RulesHint = true;
            grid.CellKeyPressEvent += Grid_CellKeyPressEvent;
            grid.Mapped += (sender, e) => EditGridCell (0, 0);
            gridNavigator = new GridNavigator (grid, EditGridCell, GridColumnEditOver, GridColumnEditBelow);
        }

        private Column colItem;

        private void ItemColumnChoose (string filter)
        {
            using (ChooseEditItem dialog = new ChooseEditItem (true, filter)) {
                if (dialog.Run () != ResponseType.Ok) {
                    EditGridCell (grid.EditedCell.Row, 0);
                    return;
                }

                for (int i = selectedDetails.Count - 1; i >= 0; i--) {
                    if (selectedDetails [i].ItemId == -1)
                        selectedDetails.RemoveAt (i);
                }

                foreach (Item item in dialog.SelectedItems) {
                    OperationDetail saleDetail;
                    if (grid.EditedCell.Row >= selectedDetails.Count) {
                        saleDetail = new SaleDetail ();
                        selectedDetails.Add ((SaleDetail) saleDetail);
                    } else
                        saleDetail = selectedDetails [grid.EditedCell.Row];
                    saleDetail.ItemEvaluate (item, PriceGroup.RegularPrice);
                }
            }

            EditGridCell (grid.EditedCell.Row, 1);
        }

        private bool ItemColumnEvaluate (int row, string itemName)
        {
            OperationDetail detail = selectedDetails [row];
            if (detail.ItemId >= 0 && detail.ItemName == itemName)
                return true;

            double currentQuantity = detail.Quantity;
            
            string codeLot;
            long lotId;
            Item item = Item.GetByAny (itemName, out barcodeUsed, out codeQtty, out codeLot, out lotId);

            bool result = selectedDetails [row].ItemEvaluate (item, PriceGroup.RegularPrice);

            // no quantity from the barcode scanner
            if (codeQtty.IsZero ())
                codeQtty = currentQuantity;

            return result;
        }

        protected virtual void ItemColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            if (grid.EditedCell != args.Cell) {
                CurrentColumnEvaluate ();
                EditGridCell (args.Cell.Row, args.Cell.Column);
            }

            if (args.EventButton.Type != EventType.TwoButtonPress)
                return;

            object cellValue = grid.EditedCellValue;
            gridNavigator.ChooseCellValue (ItemColumnEvaluate, ItemColumnChoose,
                cellValue == null ? string.Empty : cellValue.ToString ());
        }

        private void ItemColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            if (gridNavigator.ColumnKeyPress (args, colItem.Index, ItemColumnChoose,
                ItemColumnEvaluate, ItemColumnEditPrev, ItemColumnEditNext))
                return;

            string gdkKey = KeyShortcuts.KeyToString (args.EventKey);
            string quickGoods;
            if (!BusinessDomain.QuickItems.TryGetValue (gdkKey, out quickGoods))
                return;
            
            ItemColumnEvaluate (grid.EditedCell.Row, quickGoods);

            QtyColumnEvaluate (grid.EditedCell.Row, 1);

            if (selectedDetails.Count <= grid.EditedCell.Row + 1)
                selectedDetails.Add (new SaleDetail ());

            EditGridCell (grid.EditedCell.Row + 1, colItem.Index);

            args.MarkAsHandled ();
        }

        protected virtual void ItemColumnEditPrev (int row, Key keyCode)
        {
            gridNavigator.EditPrevOnFirst (row, keyCode, colSalePrice, SalePriceColumnEditPrev,
                r => DeleteGridRow (false));
        }

        protected virtual void ItemColumnEditNext (int row, Key keyCode)
        {
            if (!codeQtty.IsZero ())
                QtyColumnEvaluate (row, codeQtty);

            if (barcodeUsed) {
                if (codeQtty.IsZero ())
                    QtyColumnEvaluate (row, 1);

                if (selectedDetails.Count <= row + 1)
                    selectedDetails.AddNew ();

                EditGridCell (row + 1, colItem.Index);
            } else if (colQtty != null && colQtty.ListCell.IsEditable)
                EditGridCell (row, colQtty.Index);
            else
                QtyColumnEditNext (row, keyCode);
        }

        private Column colQtty;

        private bool QtyColumnEvaluate (int row, string quantity)
        {
            return QtyColumnEvaluate (row, Quantity.ParseExpression (quantity));
        }

        protected virtual bool QtyColumnEvaluate (int row, double qtyValue)
        {
            selectedDetails [row].Quantity = qtyValue;
            return true;
        }

        private void QttyColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditGridCell (args.Cell.Row, args.Cell.Column);
        }

        private void QtyColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            gridNavigator.ColumnKeyPress (args, colQtty.Index,
                QtyColumnEvaluate, QtyColumnEditPrev, QtyColumnEditNext);
        }

        protected virtual void QtyColumnEditPrev (int row, Key keyCode)
        {
            gridNavigator.EditPrev (row, keyCode, colItem, ItemColumnEditPrev);
        }

        protected virtual void QtyColumnEditNext (int row, Key keyCode)
        {
            gridNavigator.EditNext (row, keyCode, colSalePrice, SalePriceColumnEditNext);
        }

        private Column colSalePrice;

        private bool SalePriceColumnEvaluate (int row, string text)
        {
            selectedDetails [row].OriginalPriceOutEvaluate (Currency.ParseExpression (text));
            return true;
        }

        private void SalePriceColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            CurrentColumnEvaluate ();
            EditGridCell (args.Cell.Row, args.Cell.Column);
        }

        private void SalePriceColumn_KeyPress (object sender, CellKeyPressEventArgs args)
        {
            gridNavigator.ColumnKeyPress (args, colSalePrice.Index,
                SalePriceColumnEvaluate, SalePriceColumnEditPrev, SalePriceColumnEditNext);
        }

        protected virtual void SalePriceColumnEditPrev (int row, Key keyCode)
        {
            gridNavigator.EditPrev (row, keyCode, colQtty, QtyColumnEditPrev);
        }

        protected virtual void SalePriceColumnEditNext (int row, Key keyCode)
        {
            gridNavigator.EditNextOnLast (row, keyCode, colItem, ItemColumnEditNext, selectedDetails);
        }

        private void Grid_CellKeyPressEvent (object sender, CellKeyPressEventArgs args)
        {
            if (KeyShortcuts.IsScreenModifierControl (args.EventKey.State) &&
                args.EventKey.Key == KeyShortcuts.DeleteKey.Key) {
                DeleteGridRow (true);
            }
        }

        protected virtual void DeleteGridRow (bool keepRowPos)
        {
            if (!grid.EditedCell.IsValid)
                return;

            if (selectedDetails.Count > 1) {
                int col = grid.EditedCell.Column;
                int row = grid.EditedCell.Row;
                int newRow;

                if (row == selectedDetails.Count - 1) {
                    // If we are deleting the last row move one row up
                    selectedDetails.RemoveAt (row);
                    newRow = row - 1;
                } else {
                    // If we are deleting row from somewhere in between stay on the same line
                    selectedDetails.RemoveAt (row);
                    newRow = row;
                }

                EditGridCell (newRow, col);
            } else {
                selectedDetails.Clear ();
                selectedDetails.Add (new SaleDetail ());

                EditGridCell (0, 0);
            }
        }

        protected virtual void CurrentColumnEvaluate ()
        {
            if (grid.EditedCell.IsValid) {
                switch (currentColumn) {
                    case 0:
                        ItemColumnEvaluate (grid.EditedCell.Row, grid.EditedCellValue.ToString ());
                        break;
                    case 1:
                        selectedDetails [grid.EditedCell.Row].Quantity = Convert.ToDouble (grid.EditedCellValue);
                        break;
                    case 2:
                        selectedDetails [grid.EditedCell.Row].OriginalPriceOutEvaluate (Convert.ToDouble (grid.EditedCellValue));
                        break;
                }
            }
        }

        private bool EditGridCell (int row, int column)
        {
            grid.BeginCellEdit (new CellEventArgs (column, row));
            currentColumn = column;
            return true;
        }

        private void GridColumnEditBelow (int column)
        {
            if (!grid.EditedCell.IsValid)
                return;

            if (grid.EditedCell.Row + 1 >= grid.Model.Count)
                return;

            EditGridCell (grid.EditedCell.Row + 1, column);
        }

        private void GridColumnEditOver (int column)
        {
            if (!grid.EditedCell.IsValid)
                return;

            if (grid.EditedCell.Row <= 0)
                return;

            EditGridCell (grid.EditedCell.Row - 1, column);
        }

        #endregion

        #region Event handling

        protected void btnOK_Clicked (object o, EventArgs args)
        {
            dlgChooseItemsForPromotion.Respond (ResponseType.Ok);
        }

        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgChooseItemsForPromotion.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
