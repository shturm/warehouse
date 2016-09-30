//
// ChooseEdit.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/15/2006
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
using System.Linq;
using System.Reflection;
using System.Text;
using Glade;
using Gdk;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.Model;
using Warehouse.Presentation.Widgets;
using Drag = Gtk.Drag;
using RowActivatedArgs = Warehouse.Data.RowActivatedArgs;

namespace Warehouse.Presentation.Dialogs
{
    public abstract class ChooseEdit<T, G> : ChooseEdit
        where G : GroupBase<G>, new ()
        where T : class
    {
        protected readonly GroupsPanel<G> groupsPanel = new GroupsPanel<G> ();
        protected IManageableListModel<T> entities;
        private PropertyInfo propertyForID;

        public virtual T [] SelectedItems
        {
            get
            {
                List<T> ret = new List<T> ();
                foreach (int sel in grid.Selection) {
                    // HACK: an inexplicable ArgumentOutOfRangeException thrown by the indexer of LazyListModel; trying to trace it
                    if (sel < grid.Model.Count)
                        ret.Add ((T) grid.Model [sel]);
                    else {
                        FieldInfo field = grid.Model.GetType ().GetField ("commandText", BindingFlags.Instance | BindingFlags.NonPublic);
                        string command = "<field commandText not found>";
                        if (field != null)
                            command = (string) field.GetValue (grid.Model);
                        ErrorHandling.LogError (
                            string.Format ("Invalid model. From screen: {0}, command: \"{1}\", count: {2}, requested index: {3}.",
                                command, GetType ().Name, grid.Model.Count, sel),
                            ErrorSeverity.FatalError);
                    }
                }
                return ret.ToArray ();
            }
        }

        protected ChooseEdit ()
        {
        }

        protected ChooseEdit (string filter)
            : base (filter)
        {
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();

            groupsPanel.Show ();
            algGridGroupsTree.Add (groupsPanel);

            grid.Selection.Changed += Selection_Changed;
            grid.CellFocusIn += FocusedCell_Changed;

            propertyForID = GetPropertyForId ();

            btnNew.Sensitive = newAllowed;

            if (editAllowed) {
                grid.RowsDraggable = true;
                algGridGroups.Shown += (sender, e) => grid.RowsDraggable = true;
                algGridGroups.Hidden += (sender, e) => grid.RowsDraggable = false;
                groupsPanel.GroupsTree.DragDrop += GroupsTree_DragDrop;
            }
        }

        protected virtual PropertyInfo GetPropertyForId ()
        {
            return typeof (T).GetProperty ("Id");
        }

        private void Selection_Changed (object sender, EventArgs e)
        {
            if (grid.Selection.Count == 0)
                return;

            int selectedRow = grid.Selection.Contains (grid.FocusedRow) ? grid.FocusedRow : grid.Selection [0];
            if (selectedRow > entities.Count - 1 || selectedRow < 0)
                return;

            try {
                selectedId = (long?) propertyForID.GetValue (entities [selectedRow], null);
            } catch (ArgumentOutOfRangeException ex) {
                /*The count may have chanaged in another thread already. If so, just ignore it */
                ErrorHandling.LogException (ex);
            }
        }

        // Do we need that??
        private void FocusedCell_Changed (object sender, CellEventArgs args)
        {
            if (grid.FocusedRow >= entities.Count || grid.FocusedRow < 0)
                return;

            try {
                selectedId = (long?) propertyForID.GetValue (entities [grid.FocusedRow], null);
            } catch (ArgumentOutOfRangeException ex) {
                /*The count may have chanaged in another thread already. If so, just ignore it */
                ErrorHandling.LogException (ex);
            }
        }

        private void GroupsTree_DragDrop (object o, DragDropArgs args)
        {
            Drag.Finish (args.Context, true, true, args.Time);
            if (grid.Selection.Count == 0)
                return;

            TreePath path;
            TreeViewDropPosition pos;
            groupsPanel.GroupsTree.GetDestRowAtPos (args.X, args.Y, out path, out pos);

            TreeIter row;
            groupsPanel.GroupsTree.Model.GetIter (out row, path);
            G group = (G) groupsPanel.GroupsTree.Model.GetValue (row, 2);

            using (Message messageDialog = GetMovingToGroupMessage (group.Name)) {
                messageDialog.Buttons = MessageButtons.YesNo;
                if (messageDialog.Run () != ResponseType.Yes)
                    return;

                foreach (int selectedIndex in grid.Selection) {
                    object entity = grid.Model [selectedIndex];
                    IPersistableEntity<T> persistableEntity = entity as IPersistableEntity<T>;
                    if (persistableEntity == null)
                        continue;

                    IHirarchicalEntity hirarchicalEntity = entity as IHirarchicalEntity;
                    if (hirarchicalEntity == null)
                        continue;

                    if (group.Id == GroupBase<G>.DeletedGroupId) {
                        hirarchicalEntity.Deleted = true;
                        hirarchicalEntity.GroupId = GroupBase<G>.DefaultGroupId;
                    } else {
                        hirarchicalEntity.Deleted = false;
                        hirarchicalEntity.GroupId = group.Id;
                    }
                    persistableEntity.CommitChanges ();
                }
                OnEntitiesChanged (groupsPanel.GetSelectedGroupId ());
                OnEntitiesMovedToAGroup ();
            }
        }

        protected virtual void OnEntitiesMovedToAGroup ()
        {
        }

        protected override void ReinitializeGrid (bool reinitGroups, long? selectedGroup)
        {
            if (reinitializing)
                return;

            try {
                reinitializing = true;
                if (btnGroups.Active) {
                    if (reinitGroups) {
                        if (selectedGroup.HasValue)
                            selectedGroupId = selectedGroup.Value;
                        else if (groupsPanel.GroupsCount > 0)
                            selectedGroupId = groupsPanel.GetSelectedGroupId ();
                        else LoadSavedGroup ();

                        groupsPanel.LoadGroups (GetAllGroups ());

                        if (SelectedItems.Length > 0) {
                            T lastSelected = SelectedItems [SelectedItems.Length - 1];
                            IHirarchicalEntity lastSelectedHieararchical = lastSelected as IHirarchicalEntity;
                            if (lastSelectedHieararchical == null || !lastSelectedHieararchical.Deleted)
                                selectedGroupId = GetEntityGroup (lastSelected);
                        }

                        if ((!selectedGroupId.HasValue || !groupsPanel.SelectGroupId ((int) selectedGroupId)) &&
                            groupsPanel.SelectFirstGroup ())
                            selectedGroupId = groupsPanel.GetSelectedGroupId ();
                    }
                    SaveGroup ();
                } else {
                    selectedGroupId = null;
                }

                GetNewEntities ();
                grid.GrabFocus ();

                bool selected = false;
                if (selectedId.HasValue) {
                    int index = entities.FindIndex (e => Equals (propertyForID.GetValue (e, null), selectedId));
                    if (index >= 0) {
                        grid.Selection.Clear ();
                        grid.Selection.Select (index);
                        grid.FocusRow (index);
                        selected = true;
                    }
                }

                if (selected)
                    RefreshTotalRows ();
                else
                    SelectFirstRow ();
            } finally {
                reinitializing = false;
            }
        }

        protected virtual void LoadSavedGroup ()
        {

        }

        protected virtual void SaveGroup ()
        {

        }

        protected virtual Message GetMovingToGroupMessage (string targetGroup)
        {
            return new Message (string.Empty, string.Empty, targetGroup, "Icons.Question32.png");
        }

        private void GetNewEntities ()
        {
            IListModel<T> oldEntities = entities;

            GetAllEntities ();
            grid.Model = entities;

            IDisposable disposable = oldEntities as IDisposable;
            if (disposable != null)
                disposable.Dispose ();
        }

        protected abstract void GetAllEntities ();

        protected void DeleteSelectedEntities ()
        {
            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                T selectedItem = null;
                int row = 0;

                List<int> selected = new List<int> (grid.Selection);
                selected.Sort ();
                if (selected.Count == 0)
                    return;

                if (selected.Count == 1) {
                    if (!AskDeleteSingleEntity (entities [selected [0]]))
                        return;
                } else {
                    if (!AskDeleteMultipleEntities (selected.Count))
                        return;
                }

                bool deleted = false;
                for (int i = selected.Count - 1; i >= 0; i--) {
                    row = selected [i];
                    selectedItem = entities [row];
                    deleted |= DeleteEntity (selectedItem);
                }

                if (selectedItem == null || !deleted)
                    return;

                if (row > 0)
                    selectedId = (long?) propertyForID.GetValue (entities [row - 1], null);
                else
                    selectedId = null;

                OnEntitiesChanged (GetEntityGroup (selectedItem));
            }
        }

        protected abstract bool DeleteEntity (T entity);

        protected abstract bool AskDeleteSingleEntity (T entity);

        protected abstract bool AskDeleteMultipleEntities (int count);

        protected virtual long? GetEntityGroup (T entity)
        {
            IHirarchicalEntity groupEnt = entity as IHirarchicalEntity;
            if (groupEnt != null)
                return groupEnt.GroupId;

            return null;
        }

        #region Groups management

        protected override void btnGroups_Toggled (object o, EventArgs args)
        {
            ReinitializeGrid (true, null);

            if (btnGroups.Active) {
                algGridGroups.Show ();
                groupsPanel.GrabFocus ();
            } else {
                algGridGroups.Hide ();
                grid.GrabFocus ();
            }
        }

        protected override void btnGroupNew_Clicked (object sender, EventArgs e)
        {
            G newGroup = CreateNewGroup ();
            newGroup.Parent = groupsPanel.GetSelectedGroup ();

            IEnumerable<G> allGroups = groupsPanel.GetAllGroups ();

            using (EditNewGroup<G> dialog = new EditNewGroup<G> (newGroup, allGroups, GetAllGroupsRoot (allGroups))) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                newGroup = dialog.GetGroup ();
            }
            newGroup.CommitChanges ();
            ReinitializeGrid (true, newGroup.Id);
            OnGroupCreated ();
        }

        protected override void btnGroupDelete_Clicked (object sender, EventArgs e)
        {
            G group = groupsPanel.GetSelectedGroup ();
            DeletePermission permission = GetDeletePermission (group);

            switch (permission) {
                case DeletePermission.InUse:
                    MessageError.ShowDialog (
                        Translator.GetString ("This group cannot be deleted, because it is not empty. Please, delete or move to another group the contents of this group in order to be able to delete it!"),
                        "Icons.Group16.png");
                    return;

                case DeletePermission.Reserved:
                case DeletePermission.No:
                    MessageError.ShowDialog (
                        string.Format (Translator.GetString ("Cannot delete group \"{0}\"!"), group.Name),
                        "Icons.Group16.png");
                    return;
            }

            DeleteGroup (group);
            ReinitializeGrid (true, @group.Parent != null ? @group.Parent.Id : -1);
            OnGroupDeleted ();
        }

        protected virtual IEnumerable<G> GetAllGroups ()
        {
            return new G [0];
        }

        protected virtual G CreateNewGroup ()
        {
            throw new NotImplementedException ();
        }

        protected virtual void OnGroupCreated ()
        {
        }

        protected virtual DeletePermission GetDeletePermission (G group)
        {
            throw new NotImplementedException ();
        }

        protected virtual void DeleteGroup (G group)
        {
            throw new NotImplementedException ();
        }

        protected virtual void OnGroupDeleted ()
        {
        }

        protected virtual G GetAllGroupsRoot (IEnumerable<G> groups)
        {
            return null;
        }

        protected void GroupsTreeSelection_Changed (object sender, EventArgs e)
        {
            if (reinitializing)
                return;

            long newGroupId = groupsPanel.GetSelectedGroupId ();
            if (newGroupId == selectedGroupId)
                return;

            selectedGroupId = newGroupId;
            selectedId = null;
            ReinitializeGrid (false, newGroupId);
        }

        #endregion

        public override void Dispose ()
        {
            base.Dispose ();

            groupsPanel.Dispose ();
        }

        protected override void Disposing ()
        {
            // Make sure there is nothing selected before we dispose the entities
            grid.Selection.QuietClear ();
            IDisposable disposable = entities as IDisposable;
            if (disposable != null)
                disposable.Dispose ();
        }
    }

    public abstract class ChooseEdit : DialogBase
    {
        protected bool pickMode;
        protected readonly ListView grid = new ListView ();
        protected ScrolledWindow sWindow;
        protected long? selectedGroupId = -2;
        protected long? selectedId;
        protected bool reinitializing;
        protected bool changingFilter;
        protected bool newAllowed = true;
        protected bool editAllowed = true;
        protected bool deleteAllowed = true;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        protected Dialog dlgChooseEdit;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;
        [Widget]
        protected Button btnNew;
        [Widget]
        protected Button btnEdit;
        [Widget]
        protected Button btnDelete;
        [Widget]
        protected ToggleButton btnGroups;
        [Widget]
        protected Button btnImport;
        [Widget]
        protected Button btnExport;
        [Widget]
        protected Button btnLocation;
        [Widget]
        protected Label lblLocation;
        [Widget]
        protected Label lblLocationValue;
        [Widget]
        protected VBox vboxGrid;
        [Widget]
        protected Viewport viewportHelp;
        [Widget]
        protected Alignment algDialogIcon;
        [Widget]
        protected HPaned hpnMain;
        [Widget]
        protected Alignment algGrid;
        [Widget]
        protected Alignment algGridGroups;
        [Widget]
        protected Alignment algGridGroupsTree;
        [Widget]
        protected Button btnGroupNew;
        [Widget]
        protected Button btnGroupDelete;
        [Widget]
        protected Label lblHelp;
        [Widget]
        protected Table tblFilter;
        [Widget]
        protected Label lblFilter;
        [Widget]
        protected Entry txtFilter;
        [Widget]
        protected Button btnClear;
        [Widget]
        protected Label lblRows;
        [Widget]
        protected Label lblRowsValue;

#pragma warning restore 649

        #endregion

        public abstract string [] SelectedItemsText
        {
            get;
        }

        public override Dialog DialogControl
        {
            get { return dlgChooseEdit; }
        }

        protected virtual bool CanEditGroups
        {
            get { return false; }
        }

        protected ChooseEdit ()
        {
            reinitializing = false;
        }

        protected ChooseEdit (string filter)
        {
            reinitializing = false;
            grid.SetAutoFilter (filter, false);
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChooseEdit.glade", "dlgChooseEdit");
            form.Autoconnect (this);

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            btnNew.SetChildImage (FormHelper.LoadImage ("Icons.New24.png"));
            btnEdit.SetChildImage (FormHelper.LoadImage ("Icons.Edit24.png"));
            btnDelete.SetChildImage (FormHelper.LoadImage ("Icons.Delete24.png"));
            btnGroups.SetChildImage (FormHelper.LoadImage ("Icons.Group24.png"));
            btnImport.SetChildImage (FormHelper.LoadImage ("Icons.Import24.png"));
            btnExport.SetChildImage (FormHelper.LoadImage ("Icons.Export24.png"));

            txtFilter.KeyPressEvent += txtFilter_KeyPressEvent;
            txtFilter.Changed += txtFilter_Changed;
            txtFilter.Activated += txtFilter_Activated;
            btnClear.Clicked += btnClear_Clicked;
            btnGroupNew.Clicked += btnGroupNew_Clicked;
            btnGroupDelete.Clicked += btnGroupDelete_Clicked;
            btnGroupNew.Visible = CanEditGroups;
            btnGroupDelete.Visible = CanEditGroups;

            AccelGroup accelGroup = new AccelGroup ();
            dlgChooseEdit.AddAccelGroup (accelGroup);

            if (!string.IsNullOrEmpty (KeyForKeyboardBingdings))
                foreach (Button button in new [] { btnNew, btnEdit, btnDelete, btnImport, btnExport })
                    KeyShortcuts.SetAccelPath (button, accelGroup, KeyForKeyboardBingdings + "/" + button.Name);

            base.InitializeForm ();
        }

        protected virtual string KeyForKeyboardBingdings
        {
            get { return string.Empty; }
        }

        [GLib.ConnectBefore]
        private static void txtFilter_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (args.Event.Key == Gdk.Key.Delete)
                args.RetVal = true;
        }

        private void txtFilter_Activated (object sender, EventArgs e)
        {
            grid.ActivateRow ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
            btnEdit.SetChildLabelText (Translator.GetString ("Edit"));
            btnDelete.SetChildLabelText (Translator.GetString ("Delete"));
            btnGroups.SetChildLabelText (Translator.GetString ("Groups"));
            btnImport.SetChildLabelText (Translator.GetString ("Import"));
            btnExport.SetChildLabelText (Translator.GetString ("Export"));
            btnClear.SetChildLabelText (Translator.GetString ("Clear"));

            btnGroupNew.SetChildLabelText (Translator.GetString ("New group"));
            btnGroupDelete.SetChildLabelText (Translator.GetString ("Delete group"));

            lblFilter.SetText (Translator.GetString ("Filter"));
            lblLocation.SetText (Translator.GetString ("Location"));
            lblLocationValue.SetText (Translator.GetString ("All"));
            lblRows.SetText (Translator.GetString ("Rows"));
            lblRowsValue.SetText (string.Empty);

            SetShortcutsText ();
        }

        private void SetShortcutsText ()
        {
            StringBuilder shortcutsTextBuilder = new StringBuilder ();
            string [] texts = new []
                {
                    Translator.GetString ("New - {0}"),
                    Translator.GetString ("Edit - {0}"),
                    Translator.GetString ("Delete - {0}")
                };
            int i = 0;
            const string separator = " / ";
            foreach (string key in new [] { btnNew, btnEdit, btnDelete }.Select (button =>
                KeyShortcuts.KeyToString (KeyShortcuts.MapRawKeys (KeyShortcuts.LookupEntry (KeyForKeyboardBingdings + "/" + button.Name))))) {
                if (!string.IsNullOrEmpty (key)) {
                    shortcutsTextBuilder.Append (string.Format (texts [i], key));
                    shortcutsTextBuilder.Append (separator);
                }
                ++i;
            }

            if (shortcutsTextBuilder.Length >= separator.Length) {
                shortcutsTextBuilder.Remove (shortcutsTextBuilder.Length - separator.Length, separator.Length);
                lblHelp.SetText (shortcutsTextBuilder.ToString ());
            } else
                viewportHelp.Hide ();
        }

        protected virtual void InitializeGrid ()
        {
            sWindow = new ScrolledWindow { HscrollbarPolicy = PolicyType.Automatic, VscrollbarPolicy = PolicyType.Automatic };
            sWindow.Add (grid);

            algGrid.Add (sWindow);
            sWindow.Show ();
            grid.Show ();

            grid.AllowSelect = true;
            grid.AllowMultipleSelect = true;
            grid.CellsFucusable = false;
            grid.RulesHint = true;
            grid.SortColumnsHint = true;
            grid.RowActivated += grid_RowActivated;
            grid.EnableAutoFilter = true;
            grid.AutoFilterChanged += grid_AutoFilterChanged;
            grid.AutoFilterApplied += grid_AutoFilterApplied;
            grid.Realized += grid_Realized;
        }

        private void grid_RowActivated (object o, RowActivatedArgs args)
        {
            // We are reviewing entities and probably we want to edit
            if (!pickMode && editAllowed)
                btnEdit_Clicked (o, null);
            else
                btnOK_Clicked (o, null);
        }

        private void grid_AutoFilterChanged (object sender, EventArgs e)
        {
            if (changingFilter)
                return;

            changingFilter = true;
            txtFilter.Text = grid.AutoFilterValue;
            changingFilter = false;
        }

        private void grid_AutoFilterApplied (object sender, EventArgs e)
        {
            SelectFirstRow ();
        }

        protected void grid_Realized (object sender, EventArgs e)
        {
            SelectFirstRow ();
        }

        private void txtFilter_Changed (object sender, EventArgs e)
        {
            if (changingFilter)
                return;

            changingFilter = true;
            grid.AutoFilterValue = txtFilter.Text;
            changingFilter = false;
        }

        protected virtual void btnClear_Clicked (object sender, EventArgs e)
        {
            grid.SetAutoFilter (string.Empty, false);
        }

        #region Groups management

        protected abstract void btnGroupNew_Clicked (object sender, EventArgs e);

        protected abstract void btnGroupDelete_Clicked (object sender, EventArgs e);

        #endregion

        protected virtual void SelectFirstRow ()
        {
            grid.Selection.Clear ();
            if (grid.Model.Count > 0) {
                grid.GrabFocus ();
                grid.Selection.Select (0);
                grid.FocusRow (0);
            } else {
                grid.DefocusCell ();
            }

            RefreshTotalRows ();
        }

        protected virtual void RefreshTotalRows ()
        {
            lblRowsValue.SetText (grid.Model.Count.ToString (CultureInfo.InvariantCulture));

            if (grid.Model.Count > 0) {
                btnOK.Sensitive = true;
                btnEdit.Sensitive = editAllowed;
                btnDelete.Sensitive = deleteAllowed;
                btnGroupDelete.Sensitive = false;
            } else {
                btnOK.Sensitive = false;
                btnEdit.Sensitive = false;
                btnDelete.Sensitive = false;
                btnGroupDelete.Sensitive = deleteAllowed;
            }
            btnGroupNew.Sensitive = newAllowed;
        }

        #region Event handling

        protected virtual void btnOK_Clicked (object o, EventArgs args)
        {
            dlgChooseEdit.Respond (ResponseType.Ok);
        }

        protected virtual void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgChooseEdit.Respond (ResponseType.Cancel);
        }

        protected abstract void btnNew_Clicked (object o, EventArgs args);

        protected abstract void btnEdit_Clicked (object o, EventArgs args);

        protected abstract void btnDelete_Clicked (object o, EventArgs args);

        protected abstract void btnGroups_Toggled (object o, EventArgs args);

        protected virtual void btnImport_Clicked (object o, EventArgs args)
        {
        }

        protected virtual void btnExport_Clicked (object o, EventArgs args)
        {
        }

        #endregion

        protected virtual void OnEntitiesChanged (long? groupId = null)
        {
            ReinitializeGrid (true, groupId);
        }

        protected abstract void ReinitializeGrid (bool reinitGroups, long? selectedGroup);
    }
}