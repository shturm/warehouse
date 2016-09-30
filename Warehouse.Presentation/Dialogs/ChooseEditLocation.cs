//
// ChooseEditLocation.cs
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
using System.Linq;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseEditLocation : ChooseEditLocation<Location>
    {
        public override string HelpFile
        {
            get { return "ChooseEditPOS.html"; }
        }

        public override string [] SelectedItemsText
        {
            get
            {
                return SelectedItems.Select (sel => sel.Name).ToArray ();
            }
        }

        public ChooseEditLocation ()
            : base (true)
        {
        }

        public ChooseEditLocation (bool pickMode, string filter)
            : base (pickMode, filter)
        {
        }

        public ChooseEditLocation (bool pickMode, long? selectedID)
            : base (pickMode, true)
        {
            grid.Realized -= grid_Realized;
            grid.Realized += (sender, e) =>
            {
                selectedId = selectedID;
                int index = entities.FindIndex (ps => ps.Id == selectedId);
                if (index >= 0) {
                    grid.Selection.Clear ();
                    grid.Selection.Select (index);
                    grid.FocusRow (index);
                } else
                    SelectFirstRow ();
            };
        }

        #region Initialization steps

        protected override void GetAllEntities ()
        {
            entities = Location.GetAll (selectedGroupId);
            entities.FilterProperties.Add ("Code");
            entities.FilterProperties.Add ("Name");
        }

        #endregion

        #region Button handling

        protected override void btnEdit_Clicked (object o, EventArgs args)
        {
            int selectedRow = grid.FocusedRow;
            if (selectedRow < 0)
                return;

            Location location = entities [selectedRow];
            selectedId = location.Id;

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                using (EditNewLocation dialog = new EditNewLocation (location, selectedGroupId)) {
                    if (dialog.Run () != ResponseType.Ok) {
                        ReinitializeGrid (true, null);
                        return;
                    }

                    location = dialog.GetLocation ().CommitChanges ();
                }

                OnEntitiesChanged (location.Deleted ? LocationsGroup.DeletedGroupId : location.GroupId);
            }
        }

        protected override void btnNew_Clicked (object o, EventArgs args)
        {
            selectedId = null;

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                Location location;
                using (EditNewLocation dialog = new EditNewLocation (null, selectedGroupId)) {
                    if (dialog.Run () != ResponseType.Ok) {
                        ReinitializeGrid (true, null);
                        return;
                    }

                    location = dialog.GetLocation ().CommitChanges ();
                    selectedId = location.Id;
                }

                OnEntitiesChanged (location.GroupId);

                if (BusinessDomain.AppConfiguration.DocumentNumbersPerLocation)
                    using (EditDocumentNumbersPerLocation editDocumentNumbersPerLocation = new EditDocumentNumbersPerLocation (selectedId.Value))
                        editDocumentNumbersPerLocation.Run ();
            }
        }

        protected override bool DeleteEntity (Location entity)
        {
            selectedId = entity.Id;

            switch (Location.RequestDelete (entity.Id)) {
                case DeletePermission.InUse:
                    string title = Translator.GetString ("Location in Use");
                    string translation = Translator.GetString (
                        "The location \"{0}\" cannot be deleted, because it is used in operations. Do you want to move it to the \"Deleted\" group?");
                    string message = string.Format (translation, entity.Name);

                    if (Message.ShowDialog (title, "Icons.Location16.png", message, "Icons.Question32.png",
                        MessageButtons.YesNo) == ResponseType.Yes) {
                        entity.Deleted = true;
                        entity.GroupId = LocationsGroup.DefaultGroupId;
                        entity.CommitChanges ();
                        return true;
                    }
                    return false;

                case DeletePermission.Reserved:
                case DeletePermission.No:
                    MessageError.ShowDialog (string.Format (Translator.GetString ("Cannot delete location \"{0}\"!"), entity.Name),
                        "Icons.Location16.png");
                    return false;
            }

            Location.Delete (entity.Id);
            return true;
        }

        protected override bool AskDeleteSingleEntity (Location entity)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Location"),
                "Icons.Location16.png",
                string.Format (Translator.GetString ("Do you want to delete the location with name \"{0}\"?"), entity.Name),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override void btnImport_Clicked (object o, EventArgs args)
        {
            int selectedRow = grid.FocusedRow;
            long? groupId = null;
            if (selectedRow >= 0) {
                selectedId = entities [selectedRow].Id;
                groupId = entities [selectedRow].GroupId;
            }

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                FormHelper.ImportData<Location> ();
                OnEntitiesChanged (groupId);
            }
        }

        protected override void btnExport_Clicked (object o, EventArgs args)
        {
            FormHelper.ExportData ("locations", "Locations", new DataExchangeSet (Translator.GetString ("Locations"), entities.ToDataTable (false)));
        }

        #endregion
    }

    public abstract class ChooseEditLocation<T> : ChooseEdit<T, LocationsGroup> where T : class
    {
        protected ChooseEditLocation (bool initialize)
        {
            if (initialize)
                Initialize ();
        }

        protected ChooseEditLocation (bool pickMode, string filter)
            : base (filter)
        {
            this.pickMode = pickMode;

            Initialize ();
        }

        protected ChooseEditLocation (bool pickMode, bool initialize)
            : this (initialize)
        {
            this.pickMode = pickMode;

            grid.AllowMultipleSelect = false;
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            Image icon = FormHelper.LoadImage ("Icons.Location16.png");
            dlgChooseEdit.Icon = icon.Pixbuf;
            icon.Show ();

            icon = FormHelper.LoadImage ("Icons.Location32.png");
            algDialogIcon.Add (icon);
            icon.Show ();

            newAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditObjectsbtnNew") == UserRestrictionState.Allowed;
            editAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditObjectsbtnEdit") == UserRestrictionState.Allowed;
            deleteAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditObjectsbtnDelete") == UserRestrictionState.Allowed;
            btnImport.Sensitive = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditObjectsbtnImport") == UserRestrictionState.Allowed;
            btnExport.Sensitive = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditObjectsbtnExport") == UserRestrictionState.Allowed;
            btnImport.Visible = BusinessDomain.DataImporters.Count > 0;
            btnExport.Visible = BusinessDomain.DataExporters.Count > 0;
            dlgChooseEdit.HeightRequest = 400;
            dlgChooseEdit.WidthRequest = 610;

            InitializeFormStrings ();
            InitializeGrid ();
        }

        protected override string KeyForKeyboardBingdings
        {
            get { return "mnuEditObjects"; }
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseEdit.Title = Translator.GetString ("Locations");

            btnNew.SetChildLabelText (Translator.GetString ("New", "Location"));
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();

            grid.ColumnController = new ColumnController
                {
                    new Column (Translator.GetString ("Code"), "Code", 0.1, "Code") { MinWidth = 70 },
                    new Column (Translator.GetString ("Name"), "Name", 0.3, "Name") { MinWidth = 70 }
                };

            btnGroups.Active = BusinessDomain.AppConfiguration.ShowLocationsGroups;

            btnGroups.Toggled += btnGroups_Toggled;
            groupsPanel.GroupsTree.Selection.Changed += GroupsTreeSelection_Changed;

            btnGroups_Toggled (null, null);
        }

        protected override void LoadSavedGroup ()
        {
            if (BusinessDomain.AppConfiguration.LastLocationsGroupId >= 0)
                selectedGroupId = BusinessDomain.AppConfiguration.LastLocationsGroupId;
            else
                selectedGroupId = null;
        }

        protected override void SaveGroup ()
        {
            BusinessDomain.AppConfiguration.LastLocationsGroupId = selectedGroupId ?? -1;
        }

        #endregion

        protected override Message GetMovingToGroupMessage (string targetGroup)
        {
            string title = Translator.GetString ("Move Locations");
            string message = Translator.GetString ("Are you sure you want to move the selected locations to group \"{0}\"?");

            return new Message (title, "Icons.Location16.png", string.Format (message, targetGroup), "Icons.Question32.png");
        }

        #region Groups management

        protected override bool CanEditGroups
        {
            get { return true; }
        }

        protected override IEnumerable<LocationsGroup> GetAllGroups ()
        {
            return LocationsGroup.GetAll ();
        }

        protected override LocationsGroup CreateNewGroup ()
        {
            return new LocationsGroup ();
        }

        protected override DeletePermission GetDeletePermission (LocationsGroup group)
        {
            return LocationsGroup.RequestDelete (group.Id);
        }

        protected override void DeleteGroup (LocationsGroup group)
        {
            LocationsGroup.Delete (group.Id);
        }

        #endregion

        #region Button handling

        protected override void btnDelete_Clicked (object o, EventArgs args)
        {
            DeleteSelectedEntities ();
        }

        protected override bool AskDeleteMultipleEntities (int count)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Locations"),
                "Icons.Location16.png",
                string.Format (Translator.GetString ("Do you want to delete the selected {0} locations?"), count),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override void btnGroups_Toggled (object o, EventArgs args)
        {
            base.btnGroups_Toggled (o, args);

            BusinessDomain.AppConfiguration.ShowLocationsGroups = btnGroups.Active;
        }

        #endregion
    }
}
