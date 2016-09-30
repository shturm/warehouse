//
// EditUserRestrictions.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
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
using System.Text;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data.Model;

namespace Warehouse.Presentation.Dialogs
{
    public class EditUserRestrictions : DialogBase
    {
        private ListView gridUsers;
        private ListView gridAccessLevels;
        private LazyListModel<User> users;
        private TreeStore restsTreeStore;
        private long? selectedUserId;
        private UserAccessLevel? selectedAccessLevel;
        private RestrictionNode restsRoot;
        private readonly List<UserAccessLevel> changedAccessLevels = new List<UserAccessLevel> ();

        public override Dialog DialogControl
        {
            get { return dlgEditRestrictions; }
        }

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEditRestrictions;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;
        [Widget]
        private Button btnApply;
        [Widget]
        private Button btnReset;

        [Widget]
        private ScrolledWindow scwUsers;
        [Widget]
        private ScrolledWindow scwAccessLevels;
        [Widget]
        private TreeView treeView;
        [Widget]
        private Alignment algDialogIcon;

#pragma warning restore 649

        #endregion

        public EditUserRestrictions ()
        {
            Initialize ();
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditUserRestrictions.glade", "dlgEditRestrictions");
            form.Autoconnect (this);

            Image icon = FormHelper.LoadImage ("Icons.Security32.png");
            dlgEditRestrictions.Icon = icon.Pixbuf;
            algDialogIcon.Add (icon);
            icon.Show ();

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            btnApply.SetChildImage (FormHelper.LoadImage ("Icons.Apply24.png"));
            btnReset.SetChildImage (FormHelper.LoadImage ("Icons.Clear24.png"));

            dlgEditRestrictions.HeightRequest = 500;
            dlgEditRestrictions.WidthRequest = 800;
            btnApply.Sensitive = false;

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeUsersGrid ();
            InitializeAccessLevelsGrid ();
            InitilizeTreeView ();
        }

        protected override void InitializeFormStrings ()
        {
            dlgEditRestrictions.Title = Translator.GetString ("Permissions");

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
            btnApply.SetChildLabelText (Translator.GetString ("Apply"));
            btnReset.SetChildLabelText (Translator.GetString ("Reset"));

            base.InitializeFormStrings ();
        }

        #region Grids

        private void InitializeUsersGrid ()
        {
            users = User.GetAll (BusinessDomain.LoggedUser.UserLevel);
            gridUsers = new ListView ();

            scwUsers.Add (gridUsers);
            gridUsers.Show ();

            ColumnController cc = new ColumnController ();

            Column col = new Column (Translator.GetString ("User"), "Name", 0.2, "Name");
            col.MinWidth = 70;
            col.ListCell.IsEditable = false;
            cc.Add (col);

            gridUsers.ColumnController = cc;
            gridUsers.Model = users;
            gridUsers.AllowMultipleSelect = false;
            gridUsers.CellsFucusable = true;
            gridUsers.RulesHint = true;
            gridUsers.SortColumnsHint = true;
            gridUsers.Realized += grid_Realized;
            gridUsers.Selection.Changed += gridUsers_SelectionChanged;
        }

        private void InitializeAccessLevelsGrid ()
        {
            gridAccessLevels = new ListView ();

            scwAccessLevels.Add (gridAccessLevels);
            gridAccessLevels.Show ();

            ColumnController cc = new ColumnController ();

            Column col = new Column (Translator.GetString ("Access level"), "Value", 0.2, "Value");
            col.MinWidth = 70;
            col.ListCell.IsEditable = false;
            cc.Add (col);

            gridAccessLevels.ColumnController = cc;
            gridAccessLevels.Model = new BindingListModel (User.GetAllAccessLevels ().Where (
                k => k.Key < (int) BusinessDomain.LoggedUser.UserLevel ||
                    (BusinessDomain.LoggedUser.UserLevel == UserAccessLevel.Owner && k.Key == (int) UserAccessLevel.Owner)));
            if (gridAccessLevels.Model.Count == 0) {
                scwAccessLevels.Visible = false;
                return;
            }
            gridAccessLevels.AllowMultipleSelect = false;
            gridAccessLevels.CellsFucusable = true;
            gridAccessLevels.RulesHint = true;
            gridAccessLevels.SortColumnsHint = true;
            gridAccessLevels.Selection.Changed += gridAccessLevels_SelectionChanged;
        }

        private void grid_Realized (object sender, EventArgs e)
        {
            if (users.Count > 0) {
                gridUsers.Selection.Select (0);
                gridUsers.FocusRow (0);
            }
        }

        private void gridUsers_SelectionChanged (object sender, EventArgs eventArgs)
        {
            if (gridUsers.Selection.Count == 0)
                return;

            if (gridAccessLevels.Selection.Count > 0) {
                gridAccessLevels.Selection.Clear ();
                gridAccessLevels.QueueDraw ();
                selectedAccessLevel = null;
            }

            btnReset.Sensitive = true;
            SetAccessLevelRestrictions (changedAccessLevels);
            long newUserId = (long) gridUsers.Model [gridUsers.Selection [0], "Id"];
            if (newUserId == selectedUserId)
                return;

            selectedUserId = newUserId;
            UpdateTreeStore (restsRoot, TreeIter.Zero, true);
        }

        private void gridAccessLevels_SelectionChanged (object sender, EventArgs eventArgs)
        {
            if (gridAccessLevels.Selection.Count == 0)
                return;

            if (gridUsers.Selection.Count > 0) {
                gridUsers.Selection.Clear ();
                gridUsers.QueueDraw ();
                selectedUserId = null;
            }

            btnReset.Sensitive = false;
            UserAccessLevel newAccessLevel = (UserAccessLevel) ((KeyValuePair<int, string>) gridAccessLevels.Model [gridAccessLevels.Selection [0]]).Key;
            if (newAccessLevel == selectedAccessLevel)
                return;

            selectedAccessLevel = newAccessLevel;
            UpdateTreeStore (restsRoot, TreeIter.Zero, true);
        }

        #endregion

        protected void InitilizeTreeView ()
        {
            restsTreeStore = new TreeStore (typeof (string), typeof (bool), typeof (string));
            restsRoot = BusinessDomain.RestrictionTree;
            restsRoot.ReloadRestrictions ();

            CreateTreeStore (restsRoot, TreeIter.Zero, true);

            treeView.Model = restsTreeStore;

            CellRendererText textCellRend = new CellRendererText ();
            textCellRend.Editable = false;

            TreeViewColumn menuNameColumn = new TreeViewColumn ();
            menuNameColumn.PackStart (textCellRend, true);
            menuNameColumn.AddAttribute (textCellRend, "text", 0);
            menuNameColumn.Title = Translator.GetString ("Menu");
            menuNameColumn.Expand = true;

            CellRendererToggle boolCellRend = new CellRendererToggle ();
            boolCellRend.Activatable = true;
            boolCellRend.Toggled += boolCellRend_Toggled;

            TreeViewColumn menuEnabledColumn = new TreeViewColumn ();
            menuEnabledColumn.PackStart (boolCellRend, true);
            menuEnabledColumn.AddAttribute (boolCellRend, "active", 1);
            menuEnabledColumn.Title = Translator.GetString ("Enabled");
            menuEnabledColumn.Expand = false;

            treeView.AppendColumn (menuNameColumn);
            treeView.AppendColumn (menuEnabledColumn);
        }

        private void CreateTreeStore (TreeNodeBase<RestrictionNode, string> node, TreeIter parentIter, bool root)
        {
            foreach (RestrictionNode child in node.Children) {
                UserRestrictionState state;
                if (selectedUserId != null)
                    state = child.GetRestriction (selectedUserId.Value);
                else if (selectedAccessLevel != null)
                    state = child.GetRestriction (selectedAccessLevel.Value);
                else
                    state = UserRestrictionState.Allowed;

                bool val = state == UserRestrictionState.Allowed;
                TreeIter iter = root ?
                    restsTreeStore.AppendValues (child.Value, val, child.Name) :
                    restsTreeStore.AppendValues (parentIter, child.Value, val, child.Name);

                CreateTreeStore (child, iter, false);
            }
        }

        private void UpdateTreeStore (TreeNodeBase<RestrictionNode, string> node, TreeIter parentIter, bool root)
        {
            TreeIter iter;
            if (root)
                restsTreeStore.IterChildren (out iter);
            else
                restsTreeStore.IterChildren (out iter, parentIter);

            foreach (RestrictionNode child in node.Children) {
                UserRestrictionState rest;
                if (selectedUserId != null)
                    rest = child.GetRestriction (selectedUserId.Value);
                else if (selectedAccessLevel != null)
                    rest = child.GetRestriction (selectedAccessLevel.Value);
                else
                    rest = UserRestrictionState.Allowed;

                restsTreeStore.SetValue (iter, 1, rest == UserRestrictionState.Allowed);

                UpdateTreeStore (child, iter, false);
                restsTreeStore.IterNext (ref iter);
            }
        }

        private void boolCellRend_Toggled (object o, ToggledArgs args)
        {
            TreeIter iter;
            restsTreeStore.GetIterFromString (out iter, args.Path);
            bool oldVal = (bool) restsTreeStore.GetValue (iter, 1);
            bool newVal = !oldVal;

            SetTreeStoreChildren (iter, newVal);
            if (newVal)
                SetTreeStoreParent (iter, true);

            btnApply.Sensitive = true;
            if (selectedAccessLevel == null)
                return;

            UserAccessLevel changedAccessLevel = selectedAccessLevel.Value;
            if (!changedAccessLevels.Contains (changedAccessLevel))
                changedAccessLevels.Add (changedAccessLevel);
        }

        private void SetTreeStoreChildren (TreeIter parent, bool val)
        {
            SetRestriction (parent, val);
            TreeIter child;
            if (!restsTreeStore.IterChildren (out child, parent))
                return;

            do {
                SetTreeStoreChildren (child, val);
            } while (restsTreeStore.IterNext (ref child));
        }

        private void SetTreeStoreParent (TreeIter child, bool val)
        {
            TreeIter parent;
            if (!restsTreeStore.IterParent (out parent, child))
                return;

            SetRestriction (parent, val);
            SetTreeStoreParent (parent, val);
        }

        private void SetRestriction (TreeIter parent, bool val)
        {
            string nodeName = (string) restsTreeStore.GetValue (parent, 2);
            UserRestrictionState state = val ? UserRestrictionState.Allowed : UserRestrictionState.Restricted;
            if (selectedUserId != null)
                restsRoot.SetRestriction (selectedUserId.Value, nodeName, state);
            else if (selectedAccessLevel != null)
                restsRoot.SetRestriction (selectedAccessLevel.Value, nodeName, state);

            restsTreeStore.SetValue (parent, 1, val);
        }

        #endregion

        private void SaveChanges ()
        {
            SetAccessLevelRestrictions (changedAccessLevels);
            BusinessDomain.RestrictionTree.SaveAccessLevelRestrictions ();
            BusinessDomain.RestrictionTree.SaveRestrictions ();
            // Needed in order to get the restriction set with genegated IDs
            PresentationDomain.MainForm.RefreshRestrictions (true);
        }

        private void SetAccessLevelRestrictions (IList<UserAccessLevel> accessLevels)
        {
            if (accessLevels.Count == 0)
                return;

            string message;
            KeyValuePair<int, string> [] allAccessLevels = User.GetAllAccessLevels ();
            if (accessLevels.Count == 1) {
                message = Translator.GetString ("The default permissions for the access level of {0} were changed. " +
                    "Do you want to reset the permissions of all users with access level of {0} to the new defaults?");
                message = string.Format (message, string.Format ("\"{0}\"", allAccessLevels [(int) accessLevels [0]].Value));
            } else {
                message = Translator.GetString ("The default permissions for the access levels of {0} were changed. " +
                    "Do you want to reset the permissions of all users with access levels of {0} to the new defaults?");
                StringBuilder accessLevelsBuilder = new StringBuilder ();
                for (int i = 0; i < accessLevels.Count; i++) {
                    UserAccessLevel accessLevel = accessLevels [i];
                    accessLevelsBuilder.AppendFormat ("\"{0}\"", allAccessLevels.First (k => k.Key == (int) accessLevel).Value);
                    if (i < accessLevels.Count - 2)
                        accessLevelsBuilder.Append (", ");
                    else if (i < accessLevels.Count - 1)
                        accessLevelsBuilder.AppendFormat (" {0} ", Translator.GetString ("and"));
                }
                message = string.Format (message, accessLevelsBuilder);
            }

            if (Message.ShowDialog (Translator.GetString ("Access Level Permissions"), null,
                message, "Icons.Question32.png", MessageButtons.YesNo) == ResponseType.Yes) {
                foreach (UserAccessLevel level in accessLevels)
                    foreach (User user in users)
                        if (user.UserLevel == level)
                            restsRoot.ResetLevelRestrictions (user.Id, level);
            }

            accessLevels.Clear ();
        }

        #region Button event handling

        protected void btnOK_Clicked (object o, EventArgs args)
        {
            SaveChanges ();
            dlgEditRestrictions.Respond (ResponseType.Ok);
        }

        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            BusinessDomain.RestrictionTree.ReloadRestrictions ();
            dlgEditRestrictions.Respond (ResponseType.Cancel);
        }

        protected void btnApply_Clicked (object o, EventArgs args)
        {
            SaveChanges ();
            btnApply.Sensitive = false;
        }

        protected void btnReset_Clicked (object o, EventArgs args)
        {
            User user = (User) gridUsers.Model [gridUsers.Selection [0]];
            string message = string.Format (Translator.GetString ("Are you sure you want to reset the permissions " +
                "of the user {0} to the defaults for his access level of \"{1}\"?"),
                user.Name, User.GetAllAccessLevels ().First (k => k.Key == (int) user.UserLevel).Value);

            if (Message.ShowDialog (Translator.GetString ("Reset Permissions"), null, message, "Icons.Question32.png",
                MessageButtons.YesNo) != ResponseType.Yes)
                return;

            restsRoot.ResetLevelRestrictions (user.Id, user.UserLevel);
            UpdateTreeStore (restsRoot, TreeIter.Zero, true);
            btnApply.Sensitive = true;
        }

        #endregion
    }
}
