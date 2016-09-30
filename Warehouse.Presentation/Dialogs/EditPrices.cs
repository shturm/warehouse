//
// EditPrices.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/24/2006
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
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Item = Warehouse.Business.Entities.Item;
using Key = Gdk.Key;

namespace Warehouse.Presentation.Dialogs
{
    /// <summary>
    /// Allows a user to edit the prices of all available Entities.
    /// </summary>
    public class EditPrices : ChooseEditItem
    {
        private readonly List<Item> dirtyItems = new List<Item> ();
        private PriceGroup currentPriceGroup;

        #region Initialization steps

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            btnNew.SetChildImage (FormHelper.LoadImage ("Icons.Apply24.png"));
            btnNew.Sensitive = false;

            btnEdit.Visible = btnDelete.Visible = btnImport.Visible = btnExport.Visible = btnLocation.Visible = false;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseEdit.Title = Translator.GetString ("Change prices");

            btnNew.SetChildLabelText (Translator.GetString ("Apply"));

            lblFilter.SetText (Translator.GetString ("Filter"));
            lblHelp.SetText (Translator.GetString ("OK - Save and exit / Cancel - Exit without save / Apply - Save changes"));
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();

            ColumnController cc = new ColumnController
                {
                    new Column (Translator.GetString ("Code"), "Code", 0.1, "Code") { MinWidth = 100 }, 
                    new Column (Translator.GetString ("Name"), "Name", 0.1, "Name") { MinWidth = 110 }
                };

            CellTextCurrency ctf = new CellTextCurrency ("TradeInPrice", PriceType.Purchase);
            cc.Add (new Column (Translator.GetString ("Purchase price"), ctf, 0.1, "TradeInPrice") { MinWidth = 100 });

            ctf = new CellTextCurrency ("TradePrice") { IsEditable = true };
            cc.Add (new Column (Translator.GetString ("Wholesale price"), ctf, 0.1, "TradePrice") { MinWidth = 100 });

            ctf = new CellTextCurrency ("RegularPrice") { IsEditable = true };
            cc.Add (new Column (Translator.GetString ("Retail price"), ctf, 0.1, "RegularPrice") { MinWidth = 100 });

            ctf = new CellTextCurrency ("PriceGroup1") { IsEditable = true };
            cc.Add (new Column (Translator.GetString ("Price group 1"), ctf, 0.1, "PriceGroup1") { MinWidth = 100 });

            ctf = new CellTextCurrency ("PriceGroup2") { IsEditable = true };
            cc.Add (new Column (Translator.GetString ("Price group 2"), ctf, 0.1, "PriceGroup2") { MinWidth = 100 });

            ctf = new CellTextCurrency ("PriceGroup3") { IsEditable = true };
            cc.Add (new Column (Translator.GetString ("Price group 3"), ctf, 0.1, "PriceGroup3") { MinWidth = 100 });

            ctf = new CellTextCurrency ("PriceGroup4") { IsEditable = true };
            cc.Add (new Column (Translator.GetString ("Price group 4"), ctf, 0.1, "PriceGroup4") { MinWidth = 100 });

            ctf = new CellTextCurrency ("PriceGroup5") { IsEditable = true };
            cc.Add (new Column (Translator.GetString ("Price group 5"), ctf, 0.1, "PriceGroup5") { MinWidth = 100 });

            ctf = new CellTextCurrency ("PriceGroup6") { IsEditable = true };
            cc.Add (new Column (Translator.GetString ("Price group 6"), ctf, 0.1, "PriceGroup6") { MinWidth = 100 });

            ctf = new CellTextCurrency ("PriceGroup7") { IsEditable = true };
            cc.Add (new Column (Translator.GetString ("Price group 7"), ctf, 0.1, "PriceGroup7") { MinWidth = 100 });

            ctf = new CellTextCurrency ("PriceGroup8") { IsEditable = true };
            cc.Add (new Column (Translator.GetString ("Price group 8"), ctf, 0.1, "PriceGroup8") { MinWidth = 100 });

            grid.ColumnController = cc;
            grid.AllowSelect = false;
            grid.AllowMultipleSelect = false;
            grid.CellsFucusable = true;
            grid.ManualFucusChange = true;
            grid.RowsDraggable = false;
            grid.CellKeyPressEvent += (sender, args) => CellKeyPress (args, GetColumnPriceGroup (args.Cell.Column));
            grid.CellButtonPressEvent += (sender, args) => CellButtonPress (args, GetColumnPriceGroup (args.Cell.Column));
            grid.AutoFilterChanged += (sender, args) => SaveChanges ();
            algGridGroups.Shown += (sender, e) => grid.RowsDraggable = false;
        }

        protected override void ReinitializeGrid (bool reinitGroups, long? selectedGroup)
        {
            SaveChanges ();
            base.ReinitializeGrid (reinitGroups, selectedGroup);
        }

        private static PriceGroup GetColumnPriceGroup (int columnIndex)
        {
            switch (columnIndex) {
                case 3:
                    return PriceGroup.TradePrice;
                case 4:
                    return PriceGroup.RegularPrice;
                case 5:
                    return PriceGroup.PriceGroup1;
                case 6:
                    return PriceGroup.PriceGroup2;
                case 7:
                    return PriceGroup.PriceGroup3;
                case 8:
                    return PriceGroup.PriceGroup4;
                case 9:
                    return PriceGroup.PriceGroup5;
                case 10:
                    return PriceGroup.PriceGroup6;
                case 11:
                    return PriceGroup.PriceGroup7;
                case 12:
                    return PriceGroup.PriceGroup8;
            }

            return PriceGroup.RegularPrice;
        }

        #endregion

        #region Column events management

        private void ColumnEditNext (PriceGroup groupColumn)
        {
            int row = grid.FocusedCell.Row;

            switch (groupColumn) {
                case PriceGroup.TradePrice:
                    CellEdit (row, PriceGroup.RegularPrice);
                    break;
                case PriceGroup.RegularPrice:
                    CellEdit (row, PriceGroup.PriceGroup1);
                    break;
                case PriceGroup.PriceGroup1:
                    CellEdit (row, PriceGroup.PriceGroup2);
                    break;
                case PriceGroup.PriceGroup2:
                    CellEdit (row, PriceGroup.PriceGroup3);
                    break;
                case PriceGroup.PriceGroup3:
                    CellEdit (row, PriceGroup.PriceGroup4);
                    break;
                case PriceGroup.PriceGroup4:
                    CellEdit (row, PriceGroup.PriceGroup5);
                    break;
                case PriceGroup.PriceGroup5:
                    CellEdit (row, PriceGroup.PriceGroup6);
                    break;
                case PriceGroup.PriceGroup6:
                    CellEdit (row, PriceGroup.PriceGroup7);
                    break;
                case PriceGroup.PriceGroup7:
                    CellEdit (row, PriceGroup.PriceGroup8);
                    break;
                case PriceGroup.PriceGroup8:
                    if (row < entities.Count - 1)
                        CellEdit (row + 1, PriceGroup.TradePrice);
                    break;
            }
        }

        private void ColumnEditPrev (PriceGroup groupColumn)
        {
            int row = grid.FocusedCell.Row;

            switch (groupColumn) {
                case PriceGroup.TradePrice:
                    if (row > 0)
                        CellEdit (row - 1, PriceGroup.PriceGroup8);
                    break;
                case PriceGroup.RegularPrice:
                    CellEdit (row, PriceGroup.TradePrice);
                    break;
                case PriceGroup.PriceGroup1:
                    CellEdit (row, PriceGroup.RegularPrice);
                    break;
                case PriceGroup.PriceGroup2:
                    CellEdit (row, PriceGroup.PriceGroup1);
                    break;
                case PriceGroup.PriceGroup3:
                    CellEdit (row, PriceGroup.PriceGroup2);
                    break;
                case PriceGroup.PriceGroup4:
                    CellEdit (row, PriceGroup.PriceGroup3);
                    break;
                case PriceGroup.PriceGroup5:
                    CellEdit (row, PriceGroup.PriceGroup4);
                    break;
                case PriceGroup.PriceGroup6:
                    CellEdit (row, PriceGroup.PriceGroup5);
                    break;
                case PriceGroup.PriceGroup7:
                    CellEdit (row, PriceGroup.PriceGroup6);
                    break;
                case PriceGroup.PriceGroup8:
                    CellEdit (row, PriceGroup.PriceGroup7);
                    break;
            }
        }

        private void ColumnEditBelow (PriceGroup column)
        {
            if (!grid.FocusedCell.IsValid)
                return;

            if (grid.FocusedCell.Row + 1 < grid.Model.Count) {
                CellEdit (grid.FocusedCell.Row + 1, column);
            }
        }

        private void ColumnEditOver (PriceGroup column)
        {
            if (!grid.FocusedCell.IsValid)
                return;

            if (grid.FocusedCell.Row > 0) {
                CellEdit (grid.FocusedCell.Row - 1, column);
            }
        }

        #endregion

        #region Cell events management

        private void CurrentCellEvaluate ()
        {
            CellPosition editPos = grid.EditedCell;
            if (!editPos.IsValid)
                return;

            string cellValue = grid.EditedCellValue.ToString ();
            CellEvaluate (cellValue, GetColumnPriceGroup (editPos.Column));
        }

        private void CellEdit (int row, PriceGroup groupColumn)
        {
            int col = grid.ColumnController.GetColumnOrdinal (Item.GetPriceGroupProperty (groupColumn));

            grid.DisableEdit = false;
            grid.BeginCellEdit (new CellEventArgs (col, row));
            currentPriceGroup = groupColumn;
        }

        private bool CellEvaluate (string newValue, PriceGroup groupColumn)
        {
            bool ret;
            int row = grid.EditedCell.Row;

            Item item = entities [row];
            double oldPrice = item.GetPriceGroupPrice (groupColumn);
            double newPrice;
            if (Currency.TryParseExpression (newValue, out newPrice)) {
                if (BusinessDomain.AppConfiguration.WarnPricesSaleLowerThanPurchase &&
                    item.TradeInPrice > newPrice && newPrice > 0) {
                    string priceGroupText = Currency.GetAllSalePriceGroups ()
                        .Where (priceGroup => priceGroup.Key == (int) groupColumn)
                        .Select (priceGroup => priceGroup.Value)
                        .First ();

                    using (MessageYesNoRemember dialog = new MessageYesNoRemember (Translator.GetString ("Sale Price Lower than Purchase Price"), string.Empty,
                        string.Format (Translator.GetString ("The value you entered for \"{0}\" is lower than the purchase price. Do you want to continue?"), priceGroupText), "Icons.Question32.png")) {
                        dialog.SetButtonText (MessageButtons.Remember, Translator.GetString ("Do not warn me anymore"));
                        ret = dialog.Run () == ResponseType.Yes;
                        BusinessDomain.AppConfiguration.WarnPricesSaleLowerThanPurchase = !dialog.RememberChoice;
                    }
                } else
                    ret = true;

                if (ret)
                    item.SetPriceGroupPrice (groupColumn, newPrice);
            } else {
                item.SetPriceGroupPrice (groupColumn, 0);
                ret = false;
            }

            if (ret && !oldPrice.IsEqualTo (newPrice) && !dirtyItems.Contains (item)) {
                btnNew.Sensitive = true;
                dirtyItems.Add (item);
            }

            return ret;
        }

        private void CellKeyPress (CellKeyPressEventArgs args, PriceGroup groupColumn)
        {
            switch (args.GdkKey) {
                case Key.Tab:
                case Key.Return:
                case Key.KP_Enter:
                    if (args.Editing && !CellEvaluate (args.Entry.Text, groupColumn))
                        CellEdit (grid.FocusedCell.Row, groupColumn);
                    else
                        ColumnEditNext (groupColumn);
                    break;

                case Key.ISO_Left_Tab:
                    if (args.Editing && !CellEvaluate (args.Entry.Text, groupColumn))
                        CellEdit (grid.FocusedCell.Row, groupColumn);
                    else
                        ColumnEditPrev (groupColumn);
                    break;

                case Key.Right:
                    if (args.Editing && args.Entry.CursorPosition == args.Entry.Text.Length &&
                        !CellEvaluate (args.Entry.Text, groupColumn))
                        CellEdit (grid.FocusedCell.Row, groupColumn);
                    else
                        ColumnEditNext (groupColumn);
                    break;

                case Key.Left:
                    if (args.Editing &&
                        (args.Entry.CursorPosition == args.Entry.Text.Length || args.Entry.CursorPosition == 0) &&
                        !CellEvaluate (args.Entry.Text, groupColumn))
                        CellEdit (grid.FocusedCell.Row, groupColumn);
                    else
                        ColumnEditPrev (groupColumn);
                    break;

                case Key.Up:
                case Key.KP_Up:
                    if (args.Editing && !CellEvaluate (args.Entry.Text, groupColumn))
                        CellEdit (grid.FocusedCell.Row, groupColumn);
                    else
                        ColumnEditOver (groupColumn);
                    break;

                case Key.Down:
                case Key.KP_Down:
                    if (args.Editing && !CellEvaluate (args.Entry.Text, groupColumn))
                        CellEdit (grid.FocusedCell.Row, groupColumn);
                    else
                        ColumnEditBelow (groupColumn);
                    break;
            }
        }

        private void CellButtonPress (CellEventArgs args, PriceGroup groupColumn)
        {
            if (grid.EditedCell.IsValid)
                CellEvaluate (grid.EditedCellValue.ToString (), currentPriceGroup);

            CellEdit (args.Cell.Row, groupColumn);
        }

        #endregion

        #region Button event handling

        private void SaveChanges ()
        {
            if (dirtyItems.Count == 0)
                return;

            if (Message.ShowDialog (Translator.GetString ("Change prices"), "Icons.Goods16.png",
                Translator.GetString ("Do you want to save the changes?"), "Icons.Question32.png",
                MessageButtons.YesNo) != ResponseType.Yes)
                return;

            foreach (Item item in dirtyItems)
                item.CommitChanges ();

            dirtyItems.Clear ();
            btnNew.Sensitive = false;
        }

        protected override void btnOK_Clicked (object o, EventArgs args)
        {
            CurrentCellEvaluate ();
            SaveChanges ();
            base.btnOK_Clicked (o, args);
        }

        protected override void btnNew_Clicked (object o, EventArgs args)
        {
            CurrentCellEvaluate ();
            SaveChanges ();
        }

        protected override void btnEdit_Clicked (object o, EventArgs args)
        {
        }

        protected override void btnDelete_Clicked (object o, EventArgs args)
        {
        }

        #endregion
    }
}
