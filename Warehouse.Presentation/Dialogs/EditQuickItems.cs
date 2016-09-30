//
// EditQuickItems.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   05.10.2010
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
using System.Text;
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
    public class EditQuickItems : DialogBase
    {
        public class ItemShortcut : PriceOutOperationDetail
        {
            private AccelKey shortcut;
            private string shortcutName;

            public AccelKey Shortcut
            {
                get { return shortcut; }
                set
                {
                    if (!shortcut.Equals (value)) {
                        shortcut = value;
                        ShortcutName = KeyShortcuts.KeyToString (shortcut);
                    }
                }
            }

            public string ShortcutName
            {
                get { return shortcutName; }
                set
                {
                    if (shortcutName != value) {
                        shortcutName = value;
                        OnPropertyChanged ("ShortcutName");
                    }
                }
            }
        }

        private ListView grid;
        private GridNavigator gridNavigator;

        private readonly BindList<ItemShortcut> itemShortcuts;
        private readonly List<string> changedMenus;
        private readonly MenuItemCollection mainMenu;
        private AccelKey currentKey;
        private readonly AccelGroup accelGroup;

        #region Glade Widgets

        [Widget]
        protected Dialog dlgEditQuickItems;
        [Widget]
        protected ScrolledWindow scwGrid;
        [Widget]
        protected Button btnApply;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;
        [Widget]
        protected Button btnClear;

        #endregion

        public override string HelpFile
        {
            get { return "QuickItems.html"; }
        }

        public override Dialog DialogControl
        {
            get { return dlgEditQuickItems; }
        }

        public EditQuickItems (MenuItemCollection mainMenu, AccelGroup accelGroup)
        {
            this.mainMenu = mainMenu;
            this.accelGroup = accelGroup;
            itemShortcuts = new BindList<ItemShortcut> ();
            changedMenus = new List<string> ();
            AccelMap.Foreach (IntPtr.Zero, AddItemShortcut);
            if (itemShortcuts.Count == 0)
                itemShortcuts.Add (new ItemShortcut ());
            Initialize ();
        }

        private void AddItemShortcut (IntPtr data, string accelPath, uint accelKey, ModifierType accelMods, bool changed)
        {
            long itemId;
            if (IsKeyForItem (accelPath, out itemId) && accelKey != 0 && accelKey != (uint) Key.VoidSymbol) {
                ItemShortcut itemShortcut = new ItemShortcut ();
                if (!itemShortcut.ItemEvaluate (Item.GetById (itemId), PriceGroup.RegularPrice))
                    return;

                itemShortcut.Shortcut = new AccelKey ((Key) accelKey, KeyShortcuts.GetAllowedModifier (accelMods), AccelFlags.Visible);
                itemShortcuts.Add (itemShortcut);
            }
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditQuickItems.glade", "dlgEditQuickItems");
            form.Autoconnect (this);

            btnClear.SetChildImage (FormHelper.LoadImage ("Icons.Clear24.png"));
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            InitializeGrid ();

            base.InitializeForm ();

            InitializeFormStrings ();
        }

        private void InitializeGrid ()
        {
            grid = new ListView ();
            gridNavigator = new GridNavigator (grid, EditGridCell, EditGridCellOver, EditGridCellBelow);

            ColumnController columns = new ColumnController ();

            Column columnItem = new Column (Translator.GetString ("Item"), new CellText ("ItemName") { IsEditable = true }, 1, string.Empty);
            columnItem.ButtonPressEvent += ColumnItem_ButtonPressEvent;
            columnItem.KeyPressEvent += ColumnItem_KeyPressEvent;
            columns.Add (columnItem);

            Column columnShortcut = new Column (Translator.GetString ("Shortcut"), new CellText ("ShortcutName") { IsEditable = true }, 0.4, string.Empty);
            columnShortcut.ButtonPressEvent += ColumnShortcut_ButtonPressEvent;
            columnShortcut.KeyPressEvent += ColumnShortcut_KeyPressEvent;
            columns.Add (columnShortcut);

            grid.ColumnController = columns;
            grid.Model = new BindingListModel<ItemShortcut> (itemShortcuts);
            grid.AllowSelect = false;
            grid.CellsFucusable = true;
            grid.ManualFucusChange = true;
            grid.RulesHint = true;
            long itemId = itemShortcuts [0].ItemId;
            grid.Mapped += (sender, e) => EditGridCell (itemId < 0 ? 0 : itemShortcuts.Count - 1, itemId < 0 ? 0 : 1);
            grid.CellFocusIn += Grid_CellFocusIn;
            grid.CellKeyPressEvent += Grid_CellKeyPressEvent;

            scwGrid.Add (grid);
            grid.Show ();
        }

        private void Grid_CellKeyPressEvent (object sender, CellKeyPressEventArgs args)
        {
            if (KeyShortcuts.IsScreenModifierControl (args.EventKey.State) &&
                args.EventKey.Key == KeyShortcuts.DeleteKey.Key)
                DeleteGridRow (0);
        }

        private void Grid_CellFocusIn (object sender, CellEventArgs args)
        {
            if (itemShortcuts.Count > args.Cell.Row)
                currentKey = itemShortcuts [args.Cell.Row].Shortcut;
        }

        private void ColumnItem_KeyPressEvent (object sender, CellKeyPressEventArgs args)
        {
            gridNavigator.ColumnKeyPress (args, args.Cell.Column, ItemColumnChoose,
                ItemColumnEvaluate, ItemsColumnEditPrev, ItemColumnEditNext);
        }

        private void ItemColumnEditNext (int row, Key keycode)
        {
            gridNavigator.EditNext (row, keycode, grid.ColumnController [1], ShortcutColumnEditNext);
        }

        private void ShortcutColumnEditNext (int row, Key keycode)
        {
            gridNavigator.EditNextOnLast (row, keycode, grid.ColumnController [0], ItemColumnEditNext, itemShortcuts);
        }

        private void DeleteGridRow (int i)
        {
            if (!grid.EditedCell.IsValid)
                return;

            if (itemShortcuts.Count > 1) {
                int col = grid.EditedCell.Column;
                int row = grid.EditedCell.Row;
                int newRow;

                if (row == itemShortcuts.Count - 1) {
                    // If we are deleting the last row move one row up
                    itemShortcuts.RemoveAt (row);
                    newRow = row - 1;
                } else {
                    // If we are deleting row from somewhere in between stay on the same line
                    itemShortcuts.RemoveAt (row);
                    newRow = row;
                }

                EditGridCell (newRow, col);
            } else {
                itemShortcuts.Clear ();
                itemShortcuts.Add (new ItemShortcut ());

                EditGridCell (0, 0);
            }

        }

        private void ItemsColumnEditPrev (int row, Key keycode)
        {
            gridNavigator.EditPrevOnFirst (row, keycode, grid.ColumnController [1], ShortcutColumnEditPrev, DeleteGridRow);
        }

        private void ShortcutColumnEditPrev (int row, Key keycode)
        {
            gridNavigator.EditPrev (row, keycode, grid.ColumnController [0], ItemsColumnEditPrev);
        }

        private void ColumnShortcut_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            ShortcutColumnEvaluate (grid.EditedCell.Row, string.Empty);
            if (grid.EditedCell != args.Cell)
                EditGridCell (args.Cell.Row, args.Cell.Column);
        }

        private void ColumnShortcut_KeyPressEvent (object sender, CellKeyPressEventArgs args)
        {
            if (args.Editing && KeyShortcuts.CombinationValidForItem (args.EventKey)) {
                currentKey = new AccelKey (args.EventKey.Key, args.EventKey.State, AccelFlags.Visible);
                args.Entry.Text = KeyShortcuts.KeyToString (currentKey);
                args.MarkAsHandled ();
            }
            switch (args.EventKey.Key) {
                case Key.BackSpace:
                case Key.Delete:
                    currentKey = AccelKey.Zero;
                    args.Entry.Text = string.Empty;
                    break;
            }
            gridNavigator.ColumnKeyPress (args, args.Cell.Column, ShortcutColumnEvaluate, ShortcutColumnEditPrev, ShortcutColumnEditNext);
        }

        private bool ShortcutColumnEvaluate (int row, string value)
        {
            bool combinationValid = KeyShortcuts.CombinationValidForItem (currentKey) || currentKey.Key == 0;
            if (!combinationValid || row >= itemShortcuts.Count)
                return false;

            if (!UsedInMenu (row))
                return false;

            if (!UsedWithAnotherItem (row))
                return false;

            itemShortcuts [row].Shortcut = currentKey;
            return true;
        }

        private bool UsedWithAnotherItem (int row)
        {
            ItemShortcut itemShortcut = itemShortcuts.Find (i => i.Shortcut.Key == currentKey.Key && i.Shortcut.AccelMods == currentKey.AccelMods);
            if (itemShortcut != null && itemShortcut != itemShortcuts [row] && currentKey.Key > 0) {
                string title = Translator.GetString ("Warning!");
                string translation = Translator.GetString ("The selected shortcut is already used for the \"{0}\" quick item. " +
                    "Do you want to remove the shortcut for \"{0}\" and assign it to \"{1}\"?");
                string message = string.Format (translation, itemShortcut.ItemName, itemShortcuts [row].ItemName);
                if (Message.ShowDialog (title, string.Empty, message, "Icons.Question32.png",
                    MessageButtons.YesNo) != ResponseType.Yes)
                    return false;
             
                itemShortcut.Shortcut = AccelKey.Zero;
            }
            return true;
        }

        private bool UsedInMenu (int row)
        {
            bool result = true;
            AccelMap.Foreach (dlgEditQuickItems.Handle,
                (data, accelPath, accelKey, accelMods, changed) =>
                {
                    if (changedMenus.Contains (accelPath) || currentKey.Key == 0)
                        return;
                    string name = accelPath.Substring (accelPath.IndexOf ('/') + 1);
                    MenuItemWrapper menuItem = mainMenu.FindMenuItem (name);
                    if (menuItem != null && (uint) currentKey.Key == accelKey && currentKey.AccelMods == accelMods) {
                        string title = Translator.GetString ("Warning!");
                        string translation = Translator.GetString ("The selected shortcut is already used for menu item \"{0}\". " +
                            "Do you want to remove the shortcut for \"{0}\" and assign it to \"{1}\"?");
                        string message = string.Format (translation, menuItem.Text, itemShortcuts [row].ItemName);
                        if (Message.ShowDialog (title, string.Empty, message, "Icons.Question32.png",
                            MessageButtons.YesNo) == ResponseType.Yes) {
                            AccelMap.ChangeEntry (accelPath, (uint) Key.VoidSymbol, 0, true);
                            changedMenus.Add (accelPath);
                            AccelKey key = KeyShortcuts.LookupEntry (name);
                            menuItem.Item.RemoveAccelerator (accelGroup, (uint) key.Key, key.AccelMods);
                        } else
                            result = false;
                    }
                });
            if (!result)
                return false;
            return true;
        }

        private void ColumnItem_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            ShortcutColumnEvaluate (grid.EditedCell.Row, string.Empty);
            if (grid.EditedCell != args.Cell)
                EditGridCell (args.Cell.Row, args.Cell.Column);

            if (args.EventButton.Type != EventType.TwoButtonPress)
                return;

            object cellValue = grid.EditedCellValue;
            gridNavigator.ChooseCellValue (ItemColumnEvaluate, ItemColumnChoose,
                cellValue == null ? string.Empty : cellValue.ToString ());
        }

        private bool ItemColumnEvaluate (int row, string value)
        {
            Item item = Item.GetByAny (value);
            return item != null && itemShortcuts [row].ItemEvaluate (item, PriceGroup.RegularPrice);
        }

        private void ItemColumnChoose (string filter)
        {
            if (!grid.EditedCell.IsValid)
                return;

            int row = itemShortcuts.Count - grid.EditedCell.Row;
            using (ChooseEditItem dialog = new ChooseEditItem (true, filter)) {
                if (dialog.Run () == ResponseType.Ok) {
                    for (int i = itemShortcuts.Count - 1; i >= 0; i--)
                        if (itemShortcuts [i].ItemId == -1)
                            itemShortcuts.RemoveAt (i);

                    List<Item> alreadyAddedItems = new List<Item> ();

                    int currentRow = grid.EditedCell.Row;
                    foreach (Item item in dialog.SelectedItems) {
                        if (IsAlreadyAdded (item)) {
                            alreadyAddedItems.Add (item);
                            continue;
                        }

                        ItemShortcut itemShortcut;
                        if (currentRow >= itemShortcuts.Count) {
                            itemShortcut = new ItemShortcut ();
                            itemShortcuts.Add (itemShortcut);
                        } else
                            itemShortcut = itemShortcuts [currentRow];
                        
                        if (itemShortcut.ItemEvaluate (item, PriceGroup.RegularPrice))
                            currentRow++;
                    }

                    if (alreadyAddedItems.Count == 0) {
                        ItemColumnEditNext (grid.EditedCell.Row, Key.Return);
                        return;
                    }

                    string message;
                    const int maximumItemsToShow = 15;
                    if (alreadyAddedItems.Count > maximumItemsToShow)
                        message = Translator.GetString ("Not all of the selected items can be added. 15 of the items which already have shortcuts are: {0}");
                    else
                        message = Translator.GetString ("Not all of the selected items can be added. The items which already have shortcuts are: {0}");

                    string itemNames = GetItemNames (alreadyAddedItems, maximumItemsToShow);
                    if (alreadyAddedItems.Count < dialog.SelectedItems.Length) {
                        MessageError.ShowDialog (string.Format (message, itemNames));
                        return;
                    }
                    if (alreadyAddedItems.Count == dialog.SelectedItems.Length) {
                        message = Translator.GetString ("The selected items already have shortcuts!");
                        MessageError.ShowDialog (message);
                    }
                }
            }
            EditGridCell (itemShortcuts.Count - row, 0);
        }

        private bool IsAlreadyAdded (Item item)
        {
            return itemShortcuts.Find (i => i.ItemId == item.Id) != null &&
                (itemShortcuts.Count <= grid.EditedCell.Row || itemShortcuts [grid.EditedCell.Row].ItemId != item.Id);
        }

        private static string GetItemNames (IList<Item> alreadyAddedItems, int maximumItemsToShow)
        {
            StringBuilder itemsBuilder = new StringBuilder ();
            for (int i = 0; i < Math.Min (maximumItemsToShow, alreadyAddedItems.Count); i++) {
                itemsBuilder.Append (Environment.NewLine);
                itemsBuilder.Append (alreadyAddedItems [i].Name);
            }
            return itemsBuilder.ToString ();
        }

        protected override void InitializeFormStrings ()
        {
            dlgEditQuickItems.Title = Translator.GetString ("Quick Items");

            btnClear.SetChildLabelText (Translator.GetString ("Clear"));
            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            base.InitializeFormStrings ();
        }

        private bool EditGridCell (int row, int col)
        {
            return grid.BeginCellEdit (new CellEventArgs (col, row));
        }

        private void EditGridCellBelow (int column)
        {
            if (!grid.EditedCell.IsValid)
                return;

            if (grid.EditedCell.Row + 1 >= grid.Model.Count)
                return;

            EditGridCell (grid.EditedCell.Row + 1, column);
        }

        private void EditGridCellOver (int column)
        {
            if (!grid.EditedCell.IsValid)
                return;

            if (grid.EditedCell.Row <= 0)
                return;

            EditGridCell (grid.EditedCell.Row - 1, column);
        }

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (grid.EditedCell.IsValid && grid.EditedCell.Row < itemShortcuts.Count &&
                !ShortcutColumnEvaluate (grid.EditedCell.Row, string.Empty))
                return;

            BusinessDomain.QuickItems.Clear ();
            List<KeyValuePair<string, AccelKey>> changedShortcuts = new List<KeyValuePair<string, AccelKey>> ();
            foreach (ItemShortcut itemShortcut in itemShortcuts) {
                if (itemShortcut.ItemId < 0)
                    continue;
                string accelPath = KeyShortcuts.GetAccelPath (itemShortcut.ItemId.ToString ());
                AccelMap.ChangeEntry (accelPath, (uint) itemShortcut.Shortcut.Key,
                    KeyShortcuts.GetAllowedModifier (itemShortcut.Shortcut.AccelMods), true);
                if (itemShortcut.Shortcut.Key > 0) {
                    string key = KeyShortcuts.KeyToString (itemShortcut.Shortcut.Key, itemShortcut.Shortcut.AccelMods);
                    BusinessDomain.QuickItems.Add (key, itemShortcut.ItemName);
                }
                changedShortcuts.Add (new KeyValuePair<string, AccelKey> (accelPath, itemShortcut.Shortcut));
            }
            KeyShortcuts.Save (changedShortcuts);
            dlgEditQuickItems.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            AccelMap.Load (StoragePaths.KeyMapFile);
            dlgEditQuickItems.Respond (ResponseType.Cancel);
        }

        [UsedImplicitly]
        protected void Clear_Clicked (object o, EventArgs args)
        {
            if (itemShortcuts.Count == 1 && itemShortcuts [0].ItemId < 0) {
                EditGridCell (0, 0);
                return;
            }

            string title = Translator.GetString ("Warning!");
            string message = Translator.GetString ("This action will delete the shortcuts for all items. Are you sure you want to continue?");
            if (Message.ShowDialog (title, string.Empty, message, "Icons.Question32.png",
                MessageButtons.YesNo) != ResponseType.Yes)
                return;

            AccelMap.Foreach (dlgEditQuickItems.Handle, DeleteItemKey);
            itemShortcuts.Clear ();
            itemShortcuts.Add (new ItemShortcut ());
            EditGridCell (0, 0);
        }

        private static void DeleteItemKey (IntPtr data, string accelPath, uint accelKey, ModifierType accelMods, bool changed)
        {
            long itemId;
            bool result = IsKeyForItem (accelPath, out itemId);
            if (result)
                AccelMap.ChangeEntry (accelPath, (uint) Key.VoidSymbol, 0, true);
        }

        private static bool IsKeyForItem (string accelPath, out long itemId)
        {
            string name = accelPath.Substring (accelPath.IndexOf ('/') + 1);
            return long.TryParse (name, out itemId);
        }
    }
}
