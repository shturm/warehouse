//
// ChooseEditUser.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
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
using System.Linq;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseEditUser : ChooseEdit<User, UsersGroup>
    {
        public override string [] SelectedItemsText
        {
            get { return SelectedItems.Select (sel => sel.Name).ToArray (); }
        }

        public ChooseEditUser ()
        {
            Initialize ();
        }

        public ChooseEditUser (bool pickMode, string filter)
            : base (filter)
        {
            this.pickMode = pickMode;

            Initialize ();
        }

        public ChooseEditUser (bool pickMode, long? selectedID)
            : this ()
        {
            this.pickMode = pickMode;
            selectedId = selectedID;

            grid.AllowMultipleSelect = false;
            grid.Realized -= grid_Realized;
            grid.Realized += (sender, e) =>
                {
                    int index = entities.FindIndex (u => u.Id == selectedId);
                    if (index >= 0) {
                        grid.Selection.Clear ();
                        grid.Selection.Select (index);
                        grid.FocusRow (index);
                    } else
                        SelectFirstRow ();
                };
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            Image icon = FormHelper.LoadImage ("Icons.User16.png");
            dlgChooseEdit.Icon = icon.Pixbuf;
            icon.Show ();

            icon = FormHelper.LoadImage ("Icons.User32.png");
            algDialogIcon.Add (icon);
            icon.Show ();

            newAllowed = BusinessDomain.LoggedUser.UserLevel > UserAccessLevel.Operator &&
                BusinessDomain.RestrictionTree.GetRestriction ("mnuEditUsersbtnNew") == UserRestrictionState.Allowed;
            editAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditUsersbtnEdit") == UserRestrictionState.Allowed;
            deleteAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditUsersbtnDelete") == UserRestrictionState.Allowed;
            btnImport.Sensitive = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditUsersbtnImport") == UserRestrictionState.Allowed;
            btnExport.Sensitive = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditUsersbtnExport") == UserRestrictionState.Allowed;
            btnImport.Visible = BusinessDomain.DataImporters.Count > 0;
            btnExport.Visible = BusinessDomain.DataExporters.Count > 0;
            dlgChooseEdit.HeightRequest = 400;
            dlgChooseEdit.WidthRequest = 650;

            InitializeFormStrings ();
            InitializeGrid ();
        }

        protected override string KeyForKeyboardBingdings
        {
            get { return "mnuEditUsers"; }
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseEdit.Title = Translator.GetString ("Users");

            btnNew.SetChildLabelText (Translator.GetString ("New", "User"));
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();

            grid.ColumnController = new ColumnController
                {
                    new Column (Translator.GetString ("Code"), "Code", 0.1, "Code") { MinWidth = 70 },
                    new Column (Translator.GetString ("Name"), "Name", 0.2, "Name") { MinWidth = 70 },
                    new Column (Translator.GetString ("Access level"), new CellTextLookup<int> ("UserLevel")
                        .Load (User.GetAllAccessLevels ()), 0.1, "UserLevel") { MinWidth = 100 }
                };

            btnGroups.Active = BusinessDomain.AppConfiguration.ShowUsersGroups;

            btnGroups.Toggled += btnGroups_Toggled;
            groupsPanel.GroupsTree.Selection.Changed += GroupsTreeSelection_Changed;
            groupsPanel.GroupsTree.DragMotion += GroupsTree_DragMotion;

            btnGroups_Toggled (null, null);
        }

        private void GroupsTree_DragMotion (object o, DragMotionArgs args)
        {
            if (Drag.GetSourceWidget (args.Context) == groupsPanel.GroupsTree)
                return;

            TreePath path;
            TreeViewDropPosition pos;
            if (!groupsPanel.GroupsTree.GetDestRowAtPos (args.X, args.Y, out path, out pos)) {
                args.RetVal = false;
                return;
            }

            foreach (int index in grid.Selection) {
                User entity = (User) grid.Model [index];
                if (entity.CanEdit ())
                    continue;

                args.RetVal = false;
                return;
            }

            switch (pos) {
                case TreeViewDropPosition.Before:
                    groupsPanel.GroupsTree.SetDragDestRow (path, TreeViewDropPosition.IntoOrBefore);
                    break;
                case TreeViewDropPosition.After:
                    groupsPanel.GroupsTree.SetDragDestRow (path, TreeViewDropPosition.IntoOrAfter);
                    break;
            }
            Gdk.Drag.Status (args.Context, args.Context.SuggestedAction, args.Time);

            args.RetVal = true;
        }

        protected override void LoadSavedGroup ()
        {
            if (BusinessDomain.AppConfiguration.LastUsersGroupId >= 0)
                selectedGroupId = BusinessDomain.AppConfiguration.LastUsersGroupId;
            else
                selectedGroupId = null;
        }

        protected override void SaveGroup ()
        {
            BusinessDomain.AppConfiguration.LastUsersGroupId = selectedGroupId ?? -1;
        }

        protected override void GetAllEntities ()
        {
            entities = User.GetAll (selectedGroupId);
            entities.FilterProperties.Add ("Code");
            entities.FilterProperties.Add ("Name");
        }

        #endregion

        protected override Message GetMovingToGroupMessage (string targetGroup)
        {
            return new Message (Translator.GetString ("Move Users"), "Icons.User16.png",
                string.Format (Translator.GetString ("Are you sure you want to move the selected users to group \"{0}\"?"), targetGroup), "Icons.Question32.png");
        }

        #region Groups management

        protected override bool CanEditGroups
        {
            get { return true; }
        }

        protected override IEnumerable<UsersGroup> GetAllGroups ()
        {
            return UsersGroup.GetAll ();
        }

        protected override UsersGroup CreateNewGroup ()
        {
            return new UsersGroup ();
        }

        protected override DeletePermission GetDeletePermission (UsersGroup group)
        {
            return UsersGroup.RequestDelete (group.Id);
        }

        protected override void DeleteGroup (UsersGroup group)
        {
            UsersGroup.Delete (group.Id);
        }

        #endregion

        #region Button handling

        protected override void btnEdit_Clicked (object o, EventArgs args)
        {
            int selectedRow = grid.FocusedRow;
            if (selectedRow < 0)
                return;

            User user = entities [selectedRow];
            selectedId = user.Id;

            if (user.Id == User.DefaultId) {
                MessageError.ShowDialog (string.Format (Translator.GetString ("Cannot edit the default user \"{0}\"!"), user.Name), "Icons.User16.png");
                return;
            }

            if (!user.CanEdit ()) {
                MessageError.ShowDialog (Translator.GetString ("Editing users with access level higher or equal of the current\'s one is not allowed!"));
                return;
            }

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                UserAccessLevel oldUserLevel = user.UserLevel;
                using (EditNewUser dialog = new EditNewUser (user, selectedGroupId)) {
                    if (dialog.Run () != ResponseType.Ok) {
                        ReinitializeGrid (true, null);
                        return;
                    }

                    user = dialog.GetUser ().CommitChanges ();
                    selectedId = user.Id;

                    bool refreshRestrictions = false;
                    if (oldUserLevel != user.UserLevel) {
                        if (Message.ShowDialog (Translator.GetString ("Reset Permissions"), null,
                            Translator.GetString ("The access level of the user was changed. " +
                                "Do you want to reset the permissions of this user to the default ones for the new access level?"), "Icons.Question32.png",
                            MessageButtons.YesNo) == ResponseType.Yes) {
                            BusinessDomain.RestrictionTree.ResetLevelRestrictions (user.Id, user.UserLevel);
                            BusinessDomain.RestrictionTree.SaveRestrictions ();
                            refreshRestrictions = true;
                        }
                    }

                    if (BusinessDomain.LoggedUser.Id == user.Id) {
                        PresentationDomain.RefreshMainFormStatusBar ();
                        if (refreshRestrictions)
                            PresentationDomain.RefreshMainFormRestrictions ();
                    }
                }

                OnEntitiesChanged (user.Deleted ? UsersGroup.DeletedGroupId : user.GroupId);
            }
        }

        protected override void btnNew_Clicked (object o, EventArgs args)
        {
            selectedId = null;

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                User user;
                using (EditNewUser dialog = new EditNewUser (null, selectedGroupId)) {
                    if (dialog.Run () != ResponseType.Ok) {
                        ReinitializeGrid (true, null);
                        return;
                    }

                    user = dialog.GetUser ().CommitChanges ();
                    selectedId = user.Id;
                }

                OnEntitiesChanged (user.GroupId);
            }
        }

        protected override void btnDelete_Clicked (object o, EventArgs args)
        {
            DeleteSelectedEntities ();
        }

        protected override bool DeleteEntity (User entity)
        {
            selectedId = entity.Id;

            User loggedUser = BusinessDomain.LoggedUser;
            if (loggedUser.UserLevel != UserAccessLevel.Owner &&
                entity.UserLevel >= loggedUser.UserLevel) {
                MessageError.ShowDialog (Translator.GetString ("Deleting users with access level higher or equal of the current\'s one is not allowed!"));
                return false;
            }

            if (entity.Id == loggedUser.Id) {
                MessageError.ShowDialog (Translator.GetString ("Cannot delete the currently active user!"));
                return false;
            }

            switch (User.RequestDelete (entity.Id)) {
                case DeletePermission.InUse:
                    if (Message.ShowDialog (Translator.GetString ("User in Use"), "Icons.User16.png",
                        string.Format (Translator.GetString ("The user \"{0}\" cannot be deleted, because it is used in operations. Do you want to move it to the \"Deleted\" group?"), entity.Name), "Icons.Question32.png",
                        MessageButtons.YesNo) == ResponseType.Yes) {
                        entity.Deleted = true;
                        entity.GroupId = UsersGroup.DefaultGroupId;
                        entity.CommitChanges ();
                        return true;
                    }
                    return false;

                case DeletePermission.Reserved:
                case DeletePermission.No:
                    MessageError.ShowDialog (string.Format (Translator.GetString ("Cannot delete user \"{0}\"!"), entity.Name),
                        "Icons.User16.png");
                    return false;
            }

            User.Delete (entity.Id, true);
            return true;
        }

        protected override bool AskDeleteSingleEntity (User entity)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (Translator.GetString ("Delete User"),
                "Icons.User16.png",
                string.Format (Translator.GetString ("Do you want to delete user with name \"{0}\"?"), entity.Name),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override bool AskDeleteMultipleEntities (int count)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Users"),
                "Icons.User16.png",
                string.Format (Translator.GetString ("Do you want to delete the selected {0} users?"), count),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override void btnGroups_Toggled (object o, EventArgs args)
        {
            base.btnGroups_Toggled (o, args);

            BusinessDomain.AppConfiguration.ShowUsersGroups = btnGroups.Active;
        }

        protected override void btnImport_Clicked (object o, EventArgs args)
        {
            if (!User.CheckOwnerExists ()) {
                string text = Translator.GetString ("You need to add an owner before you can import users.");
                MessageError.ShowDialog (text, ErrorSeverity.Error);
                return;
            }

            int selectedRow = grid.FocusedRow;
            long? groupId = null;
            if (selectedRow >= 0) {
                selectedId = entities [selectedRow].Id;
                groupId = entities [selectedRow].GroupId;
            }

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                FormHelper.ImportData<User> (CustomValidate);
                OnEntitiesChanged (groupId);
            }
        }

        private static void CustomValidate (object sender, ValidateEventArgs e)
        {
            User user = (User) sender;
            if (BusinessDomain.LoggedUser.UserLevel != UserAccessLevel.Owner &&
                user.UserLevel >= BusinessDomain.LoggedUser.UserLevel) {
                e.Callback (string.Format (Translator.GetString ("The user \"{0}\" has higher or equal access level than current\'s one and will not be imported!"), user.Name),
                    ErrorSeverity.Error, -1, e.State);
                e.IsValid = false;
            } else {
                e.IsValid = true;
            }
        }

        protected override void btnExport_Clicked (object o, EventArgs args)
        {
            FormHelper.ExportData ("users", "Users", new DataExchangeSet (Translator.GetString ("Users"), entities.ToDataTable (false)));
        }

        #endregion
    }
}
