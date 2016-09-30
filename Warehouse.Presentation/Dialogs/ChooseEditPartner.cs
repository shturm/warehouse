//
// ChooseEditPartner.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/05/2006
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
    public class ChooseEditPartner : ChooseEdit<Partner, PartnersGroup>
    {
        public override string [] SelectedItemsText
        {
            get
            {
                return SelectedItems.Select (sel => sel.Name).ToArray ();
            }
        }

        public ChooseEditPartner ()
        {
            Initialize ();
        }

        public ChooseEditPartner (bool pickMode, string filter)
            : base (filter)
        {
            this.pickMode = pickMode;

            Initialize ();
        }

        public ChooseEditPartner (bool pickMode, long? selectedID)
            : this ()
        {
            this.pickMode = pickMode;
            selectedId = selectedID;

            grid.AllowMultipleSelect = false;
            grid.Realized -= grid_Realized;
            grid.Realized += (sender, e) =>
                {
                    int index = entities.FindIndex (p => p.Id == selectedId);
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

            Image icon = FormHelper.LoadImage ("Icons.Partner16.png");
            dlgChooseEdit.Icon = icon.Pixbuf;
            icon.Show ();

            icon = FormHelper.LoadImage ("Icons.Partner32.png");
            algDialogIcon.Add (icon);
            icon.Show ();

            newAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditPartnersbtnNew") == UserRestrictionState.Allowed;
            editAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditPartnersbtnEdit") == UserRestrictionState.Allowed;
            deleteAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditPartnersbtnDelete") == UserRestrictionState.Allowed;
            btnImport.Sensitive = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditPartnersbtnImport") == UserRestrictionState.Allowed;
            btnExport.Sensitive = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditPartnersbtnExport") == UserRestrictionState.Allowed;
            btnImport.Visible = BusinessDomain.DataImporters.Count > 0;
            btnExport.Visible = BusinessDomain.DataExporters.Count > 0;
            dlgChooseEdit.HeightRequest = 440;
            dlgChooseEdit.WidthRequest = 610;

            InitializeFormStrings ();
            InitializeGrid ();
        }

        protected override string KeyForKeyboardBingdings
        {
            get { return "mnuEditPartners"; }
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseEdit.Title = Translator.GetString ("Partners");

            btnNew.SetChildLabelText (Translator.GetString ("New", "Partner"));
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();

            grid.ColumnController = new ColumnController
                {
                    new Column (Translator.GetString ("Code"), "Code", 0.1, "Code") { MinWidth = 70 },
                    new Column (Translator.GetString ("Company"), "Name", 0.2, "Name") { MinWidth = 70 },
                    new Column (Translator.GetString ("Contact Name"), "LiablePerson", 0.2, "LiablePerson") { MinWidth = 70 }
                };

            btnGroups.Active = BusinessDomain.AppConfiguration.ShowPartnersGroups;

            btnGroups.Toggled += btnGroups_Toggled;
            groupsPanel.GroupsTree.Selection.Changed += GroupsTreeSelection_Changed;

            btnGroups_Toggled (null, null);
        }

        protected override void LoadSavedGroup ()
        {
            if (BusinessDomain.AppConfiguration.LastPartnersGroupId >= 0)
                selectedGroupId = BusinessDomain.AppConfiguration.LastPartnersGroupId;
            else
                selectedGroupId = null;
        }

        protected override void SaveGroup ()
        {
            BusinessDomain.AppConfiguration.LastPartnersGroupId = selectedGroupId ?? -1;
        }

        protected override void GetAllEntities ()
        {
            entities = Partner.GetAll (selectedGroupId);
            entities.FilterProperties.Add ("Code");
            entities.FilterProperties.Add ("Name");
            entities.FilterProperties.Add ("CardNumber");
        }

        #endregion

        protected override Message GetMovingToGroupMessage (string targetGroup)
        {
            return new Message (Translator.GetString ("Move Partners"), "Icons.Partner16.png",
                string.Format (Translator.GetString ("Are you sure you want to move the selected partners to group \"{0}\"?"), targetGroup), "Icons.Question32.png");
        }

        #region Groups management

        protected override bool CanEditGroups
        {
            get { return true; }
        }

        protected override IEnumerable<PartnersGroup> GetAllGroups ()
        {
            return PartnersGroup.GetAll ();
        }

        protected override PartnersGroup CreateNewGroup ()
        {
            return new PartnersGroup ();
        }

        protected override DeletePermission GetDeletePermission (PartnersGroup group)
        {
            return PartnersGroup.RequestDelete (group.Id);
        }

        protected override void DeleteGroup (PartnersGroup group)
        {
            PartnersGroup.Delete (group.Id);
        }

        #endregion

        #region Button handling

        protected override void btnEdit_Clicked (object o, EventArgs args)
        {
            int selectedRow = grid.FocusedRow;
            if (selectedRow < 0)
                return;

            Partner partner = entities [selectedRow];
            selectedId = partner.Id;

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                using (EditNewPartner dialog = new EditNewPartner (partner, selectedGroupId)) {
                    if (dialog.Run () != ResponseType.Ok) {
                        ReinitializeGrid (true, null);
                        return;
                    }

                    partner = dialog.GetPartner ().CommitChanges ();
                    selectedId = partner.Id;
                }

                OnEntitiesChanged (partner.Deleted ? PartnersGroup.DeletedGroupId : partner.GroupId);
            }
        }

        protected override void btnNew_Clicked (object o, EventArgs args)
        {
            selectedId = null;

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                Partner partner;
                using (EditNewPartner dialog = new EditNewPartner (null, selectedGroupId)) {
                    if (dialog.Run () != ResponseType.Ok) {
                        ReinitializeGrid (true, null);
                        return;
                    }

                    partner = dialog.GetPartner ().CommitChanges ();
                    selectedId = partner.Id;
                }

                OnEntitiesChanged (partner.GroupId);
            }
        }

        protected override void btnDelete_Clicked (object o, EventArgs args)
        {
            DeleteSelectedEntities ();
        }

        protected override bool DeleteEntity (Partner entity)
        {
            selectedId = entity.Id;

            switch (Partner.RequestDelete (entity.Id)) {
                case DeletePermission.InUse:
                    string title = Translator.GetString ("Partner in Use");
                    string translation = Translator.GetString (
                        "The partner \"{0}\" cannot be deleted, because it is used in operations. Do you want to move it to the \"Deleted\" group?");
                    string message = string.Format (translation, entity.Name);

                    if (Message.ShowDialog (title, "Icons.Partner16.png", message, "Icons.Question32.png",
                        MessageButtons.YesNo) == ResponseType.Yes) {
                        entity.Deleted = true;
                        entity.GroupId = PartnersGroup.DefaultGroupId;
                        entity.CommitChanges ();
                        return true;
                    }
                    return false;

                case DeletePermission.Reserved:
                case DeletePermission.No:
                    MessageError.ShowDialog (
                        string.Format (Translator.GetString ("Cannot delete partner \"{0}\"!"), entity.Name),
                        "Icons.Partner16.png");
                    return false;
            }

            Partner.Delete (entity.Id);
            return true;
        }

        protected override bool AskDeleteSingleEntity (Partner entity)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Partner"),
                "Icons.Partner16.png",
                string.Format (Translator.GetString ("Do you want to delete partner with name \"{0}\"?"), entity.Name),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override bool AskDeleteMultipleEntities (int count)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Partners"),
                "Icons.Partner16.png",
                string.Format (Translator.GetString ("Do you want to delete the selected {0} partners?"), count),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override void btnGroups_Toggled (object o, EventArgs args)
        {
            base.btnGroups_Toggled (o, args);

            BusinessDomain.AppConfiguration.ShowPartnersGroups = btnGroups.Active;
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
                FormHelper.ImportData<Partner> ();
                OnEntitiesChanged (groupId);
            }
        }

        protected override void btnExport_Clicked (object o, EventArgs args)
        {
            FormHelper.ExportData ("partners", "Partners", new DataExchangeSet (Translator.GetString ("Partners"), entities.ToDataTable (false)));
        }

        #endregion
    }
}
