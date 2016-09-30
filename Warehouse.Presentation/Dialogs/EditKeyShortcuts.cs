//
// EditKeyShortcuts.cs
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
using System.Globalization;
using System.IO;
using System.Linq;
using Gdk;
using Glade;
using GLib;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Data;
using Item = Warehouse.Business.Entities.Item;
using Key = Gdk.Key;

namespace Warehouse.Presentation.Dialogs
{
    /// <summary>
    /// Allows the user to change the key bindings used for quick access to elements of the GUI.
    /// </summary>
    public class EditKeyShortcuts : DialogBase
    {
        private readonly MenuItemCollection menu;
        private readonly AccelGroup accelGroup;
        private readonly List<KeyValuePair<string, AccelKey>> changedShortcuts = new List<KeyValuePair<string, AccelKey>> ();
        private AccelKey enteredAccelKey;
        private bool edited;

        #region Glade Widgets

        [Widget]
        protected Dialog dlgEditKeyShortcuts;
        [Widget]
        protected TreeView treeViewMenu;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;
        [Widget]
        protected Button btnClear;

        #endregion

        public override string HelpFile
        {
            get { return "KeyShortcuts.html"; }
        }

        public override Dialog DialogControl
        {
            get { return dlgEditKeyShortcuts; }
        }

        public EditKeyShortcuts (MenuItemCollection menu, AccelGroup accelGroup)
        {
            this.menu = menu;
            this.accelGroup = accelGroup;
            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditKeyShortcuts.glade", "dlgEditKeyShortcuts");
            form.Autoconnect (this);

            btnClear.SetChildImage (FormHelper.LoadImage ("Icons.Clear24.png"));
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();

            InitializeFormStrings ();

            InitializeTreeView ();
        }

        protected override void InitializeFormStrings ()
        {
            dlgEditKeyShortcuts.Title = Translator.GetString ("Key Shortcuts");

            btnClear.SetChildLabelText (Translator.GetString ("Clear"));
            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            base.InitializeFormStrings ();
        }

        private void InitializeTreeView ()
        {
            string menuTranslation = Translator.GetString ("Menu");
            treeViewMenu.AppendColumn (menuTranslation, new CellRendererText { Width = 300 }, "text", 0);

            string bindingTranslation = Translator.GetString ("Shortcut");
            CellRendererText cellRendererText = new CellRendererText { Editable = true };
            cellRendererText.EditingStarted += CellRendererText_EditingStarted;
            cellRendererText.Edited += CellRendererText_Edited;
            treeViewMenu.AppendColumn (bindingTranslation, cellRendererText, "text", 1);

            treeViewMenu.AppendColumn (string.Empty, new CellRendererText (), "text", 2).Visible = false;
            // this column is used when checking if a key shortcut is already used
            // for example, the key F2 can be used in both the screen for Item and the one for partners
            // however, F2 cannot be used twice as a shortcut to different menu items
            treeViewMenu.AppendColumn (string.Empty, new CellRendererText (), "text", 3).Visible = false;

            treeViewMenu.Selection.Changed += Selection_Changed;

            BusinessDomain.RestrictionTree.ReloadRestrictions ();
            LoadTreeView ();
        }

        private void Selection_Changed (object sender, EventArgs e)
        {
            TreeIter selectedRow = GetSelectedRow ();
            if (!IsSelectionValid (selectedRow))
                return;

            string selectedName = (string) treeViewMenu.Model.GetValue (selectedRow, 2);
            enteredAccelKey = KeyShortcuts.LookupEntry (selectedName);
        }

        private void CellRendererText_EditingStarted (object o, EditingStartedArgs args)
        {
            ((Widget) args.Editable).KeyPressEvent += Key_KeyPress;
            args.Editable.EditingDone += Editable_EditingDone;
            edited = true;
        }

        private void Editable_EditingDone (object sender, EventArgs e)
        {
            Entry txt = (Entry) sender;
            txt.KeyPressEvent -= Key_KeyPress;
            txt.EditingDone -= Editable_EditingDone;
        }

        private void CellRendererText_Edited (object o, EditedArgs args)
        {
            TryApplyShortcut ();
            edited = false;
        }

        private void LoadTreeView ()
        {
            TreeStore treeStore = new TreeStore (typeof (string), typeof (string), typeof (string), typeof (string));
            AddNodes (treeStore, menu.Menu.Children, TreeIter.Zero);
            treeViewMenu.Model = treeStore;
        }

        private void AddNodes (TreeStore treeStore, IEnumerable<Widget> menuItems, TreeIter parent)
        {
            // TODO: this method, and some of its callees, use too many hard-coded strings, improve this
            string parentName = string.Empty;
            foreach (MenuItem menuItem in menuItems) {
                Label label = menuItem.Child as Label;
                if (label == null)
                    continue;

                if (BusinessDomain.RestrictionTree.GetRestriction (menuItem.Name) != UserRestrictionState.Allowed)
                    continue;

                AccelKey key = KeyShortcuts.LookupEntry (menuItem.Name);
                parentName = menuItem.Parent.Name;
                TreeIter row = parent.Equals (TreeIter.Zero) ?
                    treeStore.AppendValues (label.Text, KeyShortcuts.KeyToString (key), menuItem.Name, KeyShortcuts.MENU_NEIGHBOURING_SHORTCUT) :
                    treeStore.AppendValues (parent, label.Text, KeyShortcuts.KeyToString (key), menuItem.Name, KeyShortcuts.MENU_NEIGHBOURING_SHORTCUT);

                Container container = menuItem.Submenu as Container;
                if (container != null)
                    AddNodes (treeStore, (container).Children, row);

                switch (menuItem.Name) {
                    case "mnuEditPartners":
                    case "mnuEditGoods":
                    case "mnuEditUsers":
                    case "mnuEditObjects":
                    case "mnuEditVATGroups":
                    case "mnuEditDevices":
                    case "mnuEditAdminPriceRules":
                        AddEditDialogShortcuts (row, treeStore, menuItem.Name);
                        break;
                    case "mnuOperTradeObject":
                        AddPOSShortcuts (row, treeStore, menuItem.Name);
                        break;
                }

                MenuItem item = menuItem;
                foreach (ICustomKeyShortcut shortcut in
                    from customKeyShortcut in KeyShortcuts.CustomKeyShortcuts
                    where customKeyShortcut.Parent == item.Name
                    orderby customKeyShortcut.Ordinal
                    select customKeyShortcut)
                    AddCustomShortcut (row, treeStore, shortcut.Path, shortcut.Label, shortcut.Type);
            }

            if (parentName == menu.Menu.Name)
                AddCustomShortcut (parent, treeStore, KeyShortcuts.CHOOSE_KEY, Translator.GetString ("Select"), KeyShortcuts.MENU_NEIGHBOURING_SHORTCUT);

            if (parentName == "mnuOperations_menu") {
                AddCustomShortcut (parent, treeStore, "txtPartner", Translator.GetString ("Partner"), KeyShortcuts.MENU_NEIGHBOURING_SHORTCUT);
                AddCustomShortcut (parent, treeStore, "btnSave", Translator.GetString ("Save Operation"), KeyShortcuts.MENU_NEIGHBOURING_SHORTCUT);
                AddCustomShortcut (parent, treeStore, "btnClear", Translator.GetString ("Clear Operation"), KeyShortcuts.MENU_NEIGHBOURING_SHORTCUT);
            }

            if (parentName == "mnuHelp_menu")
                AddCustomShortcut (parent, treeStore, KeyShortcuts.HELP_KEY, Translator.GetString ("Show Help"), KeyShortcuts.MENU_NEIGHBOURING_SHORTCUT);

        }

        private static void AddEditDialogShortcuts (TreeIter parent, TreeStore treeStore, string parentName)
        {
            AddCustomShortcut (parent, treeStore, parentName + "/btnNew", Translator.GetString ("New"), parentName);
            AddCustomShortcut (parent, treeStore, parentName + "/btnEdit", Translator.GetString ("Edit"), parentName);
            AddCustomShortcut (parent, treeStore, parentName + "/btnDelete", Translator.GetString ("Delete"), parentName);
        }

        private static void AddPOSShortcuts (TreeIter parent, TreeStore treeStore, string parentName)
        {
            AddCustomShortcut (parent, treeStore, parentName + "/btnCash", Translator.GetString ("Pay in Cash"), KeyShortcuts.MENU_NEIGHBOURING_SHORTCUT);
            AddCustomShortcut (parent, treeStore, parentName + "/btnCard", Translator.GetString ("Pay with Card"), KeyShortcuts.MENU_NEIGHBOURING_SHORTCUT);
            AddCustomShortcut (parent, treeStore, parentName + "/btnBank", Translator.GetString ("Pay by Bank"), KeyShortcuts.MENU_NEIGHBOURING_SHORTCUT);
            AddCustomShortcut (parent, treeStore, parentName + "/btnReports", Translator.GetString ("Reports"), KeyShortcuts.MENU_NEIGHBOURING_SHORTCUT);
        }

        private static void AddCustomShortcut (TreeIter parent, TreeStore treeStore, string path, string name, string type)
        {
            // only menu shortcuts supported on the mac because of the GTK "cmd is alt" bug
            if (PlatformHelper.Platform == PlatformTypes.MacOSX &&
                (path.Contains ("btn") || path == KeyShortcuts.CHOOSE_KEY || path == KeyShortcuts.HELP_KEY))
                return;

            string parentPath = (string) treeStore.GetValue (parent, 2);
            int indexOfSeparator = path.LastIndexOf ('/');
            TreeIter finalParent = parent;
            if (indexOfSeparator >= 0) {
                string parentNode = path.Substring (0, indexOfSeparator);
                if (parentNode != parentPath) {
                    for (int i = 0; i < treeStore.IterNChildren (parent); i++) {
                        TreeIter row;
                        treeStore.IterNthChild (out row, parent, i);
                        string child = (string) treeStore.GetValue (row, 2);
                        if (child.Substring (child.LastIndexOf ('/') + 1) != parentNode)
                            continue;

                        finalParent = row;
                        break;
                    }
                    name = name.Substring (name.LastIndexOf ('/') + 1);
                }
            }
            AccelKey key = KeyShortcuts.LookupEntry (path);
            // set "menu" as the type because these custom bindings cannot be the same as some real menu bindings
            if (finalParent.Equals (TreeIter.Zero))
                treeStore.AppendValues (name, KeyShortcuts.KeyToString (key), path, type);
            else
                treeStore.AppendValues (finalParent, name, KeyShortcuts.KeyToString (key), path, type);
        }

        private bool IsSelectionValid (TreeIter selectedRow)
        {
            if (selectedRow.Equals (TreeIter.Zero))
                return false;

            TreeModel model = treeViewMenu.Model;
            if ((string) model.GetValue (selectedRow, 3) == KeyShortcuts.MENU_NEIGHBOURING_SHORTCUT) {
                int childrenCount = model.IterNChildren (selectedRow);
                for (int i = 0; i < childrenCount; i++) {
                    TreeIter row;
                    model.IterNthChild (out row, selectedRow, i);
                    if ((string) model.GetValue (row, 3) == KeyShortcuts.MENU_NEIGHBOURING_SHORTCUT &&
                        !((string) model.GetValue (row, 2)).Contains ("/btn"))
                        return false;
                }
            }
            return true;
        }

        private TreeIter GetSelectedRow ()
        {
            TreePath [] selectedRows = treeViewMenu.Selection.GetSelectedRows ();
            if (selectedRows.Length > 0) {
                TreeIter selectedRow;
                treeViewMenu.Model.GetIter (out selectedRow, selectedRows [0]);
                return selectedRow;
            }
            return TreeIter.Zero;
        }

        [ConnectBefore]
        protected void Key_KeyPress (object o, KeyPressEventArgs args)
        {
            Entry txtKey = (Entry) o;
            if (!IsSelectionValid (GetSelectedRow ()))
                return;

            Key key;
            ModifierType mod;
            KeyShortcuts.MapRawKeys (args.Event, out key, out mod);
            AccelKey newAccelKey = new AccelKey (key, mod, AccelFlags.Visible);
            if (KeyShortcuts.CombinationValid (key, mod)) {
                enteredAccelKey = newAccelKey;
                txtKey.Text = KeyShortcuts.KeyToString (enteredAccelKey);
            }
            switch (args.Event.Key) {
                case Key.BackSpace:
                    if (KeyShortcuts.GetAllowedModifier (args.Event.State) == ModifierType.None) {
                        enteredAccelKey = new AccelKey (Key.VoidSymbol, ModifierType.None, AccelFlags.Visible);
                        txtKey.Text = string.Empty;
                    }
                    break;
                case Key.KP_Enter:
                case Key.Return:
                case Key.Escape:
                    return;
            }
            args.RetVal = true;
        }

        private void TryApplyShortcut ()
        {
            TreeIter selectedRow = GetSelectedRow ();
            string menuItemName = (string) treeViewMenu.Model.GetValue (selectedRow, 2);
            if (!IsFreeToUse (enteredAccelKey)) {
                treeViewMenu.GrabFocus ();
                return;
            }
            ApplyShortcut (menuItemName, enteredAccelKey);
            treeViewMenu.Model.SetValue (selectedRow, 1, KeyShortcuts.KeyToString (enteredAccelKey));
        }

        private void ApplyShortcut (string menuItemName, AccelKey accelKey)
        {
            string accelPath = KeyShortcuts.GetAccelPath (menuItemName);
            changedShortcuts.Add (new KeyValuePair<string, AccelKey> (accelPath,
                new AccelKey (accelKey.Key, KeyShortcuts.GetAllowedModifier (accelKey.AccelMods), AccelFlags.Visible)));
            MenuItemWrapper menuItemWrapper = menu.FindMenuItem (menuItemName);
            if (menuItemWrapper == null)
                return;
            if (accelKey.Key != Key.VoidSymbol) 
                return;

            AccelKey key = KeyShortcuts.LookupEntry (menuItemName);
            menuItemWrapper.Item.RemoveAccelerator (accelGroup, (uint) key.Key, key.AccelMods);
        }

        private bool IsFreeToUse (AccelKey newKey)
        {
            return newKey.Key == 0 || newKey.Key == Key.VoidSymbol || (!UsedInMenuOrSameScreen (newKey) && !IsUsedInQuickItems (newKey));
        }

        private bool UsedInMenuOrSameScreen (AccelKey newKey)
        {
            TreeIter selectedRow = GetSelectedRow ();
            string selectedName = (string) treeViewMenu.Model.GetValue (selectedRow, 2);
            string selectedType = (string) treeViewMenu.Model.GetValue (selectedRow, 3);
            TreeIter ownerRow = TreeIter.Zero;
            treeViewMenu.Model.Foreach ((model, path, row) =>
                {
                    string name = (string) model.GetValue (row, 2);
                    if (selectedName == name)
                        return false;

                    string type = (string) model.GetValue (row, 3);
                    AccelKey key = KeyShortcuts.LookupEntry (name);
                    // when comparing with menus, ignore paths containing a '/' because menus do not have such (in incomplete paths)
                    if (selectedType == type && (selectedType != KeyShortcuts.MENU_NEIGHBOURING_SHORTCUT || !name.Contains ("/")) &&
                        KeyShortcuts.KeyEqual ((uint) newKey.Key, (uint) key.Key) && newKey.AccelMods == key.AccelMods) {
                        ownerRow = row;
                        return true;
                    }
                    return false;
                });

            if (!ownerRow.Equals (TreeIter.Zero)) {
                string translation = Translator.GetString ("The selected shortcut is already used for the \"{0}\" menu item. " +
                    "Do you want to remove the shortcut for \"{0}\" to reassign it?");
                string message = string.Format (translation, treeViewMenu.Model.GetValue (ownerRow, 0));
                if (Message.ShowDialog (Translator.GetString ("Warning!"), string.Empty,
                    message, "Icons.Question32.png",
                    MessageButtons.YesNo) == ResponseType.Yes) {
                    string ownerItem = (string) treeViewMenu.Model.GetValue (ownerRow, 2);
                    ApplyShortcut (ownerItem, new AccelKey (Key.VoidSymbol, ModifierType.None, AccelFlags.Visible));
                    treeViewMenu.Model.SetValue (ownerRow, 1, KeyShortcuts.KeyToString (AccelKey.Zero));
                    return false;
                }
                return true;
            }
            return false;
        }

        private static bool IsUsedInQuickItems (AccelKey newKey)
        {
            string key = KeyShortcuts.KeyToString (newKey);
            if (!BusinessDomain.QuickItems.ContainsKey (key))
                return false;
            
            using (Message dialog = GetInUseQuickItemMessage (key)) {
                dialog.Buttons = MessageButtons.YesNo;
                if (dialog.Run () != ResponseType.Yes)
                    return true;
                
                RemoveQuickItem (key);
                return false;
            }
        }

        private static Message GetInUseQuickItemMessage (string key)
        {
            string message = Translator.GetString ("\"{0}\" is already " +
                "used for the \"{1}\" quick item! Do you want to remove this quick item?");
            message = string.Format (message, key, BusinessDomain.QuickItems [key]);

            return new Message (Translator.GetString ("Warning!"), string.Empty, message, "Icons.Question32.png");
        }

        private static void RemoveQuickItem (string key)
        {
            string quickItem = BusinessDomain.QuickItems [key];
            BusinessDomain.QuickItems.Remove (key);
            Item item = Item.GetByAny (quickItem);
            if (item != null)
                AccelMap.ChangeEntry (KeyShortcuts.GetAccelPath (item.Id.ToString (CultureInfo.InvariantCulture)), (uint) Key.VoidSymbol, 0, true);
        }

        private static void RemoveQuickItem (string key, Dictionary<string, AccelKey> shortcuts)
        {
            string quickItem = BusinessDomain.QuickItems [key];
            BusinessDomain.QuickItems.Remove (key);
            Item item = Item.GetByAny (quickItem);
            if (item != null)
                shortcuts.Add (KeyShortcuts.GetAccelPath (item.Id.ToString (CultureInfo.InvariantCulture)), AccelKey.Zero);
        }

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (edited) {
                TryApplyShortcut ();
                BusinessDomain.FeedbackProvider.TrackEvent ("Key shortcuts", "Edited");
            }
            if (changedShortcuts.Count > 0) {
                if (Message.ShowDialog (Translator.GetString ("Restart now?"), null,
                    string.Format (Translator.GetString ("In order for the change to take effect you need to restart {0}. Do you want to restart {0} now?"), DataHelper.ProductName), 
                    "Icons.Question32.png",
                    MessageButtons.Restart | MessageButtons.Cancel) != ResponseType.Apply)
                    return;

                KeyShortcuts.Save (changedShortcuts);
                PresentationDomain.QueueRestart ();
            }
            dlgEditKeyShortcuts.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditKeyShortcuts.Respond (ResponseType.Cancel);
        }

        [UsedImplicitly]
        protected void Clear_Clicked (object o, EventArgs args)
        {
            string title = Translator.GetString ("Warning!");
            string message = Translator.GetString ("This action will reset all key shortcuts to their default values. Are you sure you want to continue?");
            if (Message.ShowDialog (title, string.Empty, message, "Icons.Question32.png",
                MessageButtons.YesNo) != ResponseType.Yes)
                return;

            // Load the default key map
            string temp = Path.GetTempFileName ();
            File.WriteAllText (temp, DataHelper.GetDefaultKeyMap ());
            AccelMap.Load (temp);
            File.Delete (temp);

            // Get the key shortcuts in a dictionary
            Dictionary<string, AccelKey> shortcuts = new Dictionary<string, AccelKey> ();
            ResponseType choice = ResponseType.None;
            AccelMap.Foreach (IntPtr.Zero,
                (data, accelPath, accelKey, accelMods, changed) =>
                {
                    string key = KeyShortcuts.KeyToString ((Key) accelKey, accelMods);
                    string name = accelPath.Substring (accelPath.IndexOf ('/') + 1);
                    long itemId;
                    if (long.TryParse (name, out itemId))
                        return;

                    if (!BusinessDomain.QuickItems.ContainsKey (key) ||
                        (menu.FindMenuItem (name) == null && name != KeyShortcuts.CHOOSE_KEY && name != KeyShortcuts.HELP_KEY))
                        return;

                    switch (choice) {
                        case ResponseType.None:
                            using (Message messageBox = GetInUseQuickItemMessage (key)) {
                                messageBox.Buttons = MessageButtons.YesNo | MessageButtons.Cancel | MessageButtons.Remember;
                                ResponseType responseType = messageBox.Run ();
                                switch (responseType) {
                                    case ResponseType.Yes:
                                        RemoveQuickItem (key, shortcuts);
                                        if (messageBox.RememberChoice)
                                            choice = responseType;
                                        break;
                                    case ResponseType.No:
                                        shortcuts.Add (accelPath, AccelKey.Zero);
                                        if (messageBox.RememberChoice)
                                            choice = responseType;
                                        break;
                                    case ResponseType.DeleteEvent:
                                    case ResponseType.Cancel:
                                        choice = ResponseType.Cancel;
                                        break;
                                }
                            }
                            break;
                        case ResponseType.Yes:
                            RemoveQuickItem (key, shortcuts);
                            break;
                        case ResponseType.No:
                            shortcuts.Add (accelPath, AccelKey.Zero);
                            break;
                    }
                });
            if (choice == ResponseType.Cancel) {
                LoadTreeView ();
                return;
            }
            File.Delete (StoragePaths.KeyMapFile);
            bool quickGoods = false;
            AccelMap.Foreach (IntPtr.Zero, (data, path, key, mods, changed) =>
                {
                    string wholeKey = KeyShortcuts.KeyToString ((Key) key, mods);
                    if (!BusinessDomain.QuickItems.ContainsKey (wholeKey))
                        return;

                    AccelMap.ChangeEntry (path, key, KeyShortcuts.GetAllowedModifier (mods), true);
                    quickGoods = true;
                });

            if (quickGoods)
                AccelMap.Save (StoragePaths.KeyMapFile);

            AccelMap.Foreach (IntPtr.Zero,
                (data, accelPath, accelKey, accelMods, changed) =>
                {
                    if (!shortcuts.ContainsKey (accelPath))
                        return;

                    AccelKey key = shortcuts [accelPath];
                    AccelMap.ChangeEntry (accelPath, (uint) key.Key,
                        KeyShortcuts.GetAllowedModifier (key.AccelMods), true);
                });

            foreach (ICustomKeyShortcut shortcut in KeyShortcuts.CustomKeyShortcuts)
                AccelMap.ChangeEntry (KeyShortcuts.GetAccelPath (shortcut.Path), (uint) shortcut.DefaultKey,
                    KeyShortcuts.GetAllowedModifier (shortcut.DefaultModifier), true);
            LoadTreeView ();
        }
    }
}
