//
// ChooseEditCompanyRecord.cs
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
    public class ChooseEditCompanyRecord : ChooseEdit<CompanyRecord, EmptyGroup>
    {
        public override string [] SelectedItemsText
        {
            get
            {
                return SelectedItems.Select (sel => sel.Name).ToArray ();
            }
        }

        public ChooseEditCompanyRecord ()
        {
            Initialize ();
        }

        public ChooseEditCompanyRecord (bool pickMode)
        {
            this.pickMode = pickMode;

            Initialize ();
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            Image icon = FormHelper.LoadImage ("Icons.CompanyRecord16.png");
            dlgChooseEdit.Icon = icon.Pixbuf;
            icon.Show ();

            icon = FormHelper.LoadImage ("Icons.CompanyRecord32.png");
            algDialogIcon.Add (icon);
            icon.Show ();

            dlgChooseEdit.HeightRequest = 320;
            dlgChooseEdit.WidthRequest = 560;

            InitializeFormStrings ();
            InitializeGrid ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseEdit.Title = Translator.GetString ("Company information");

            btnNew.SetChildLabelText (Translator.GetString ("New", "Company"));
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

            btnGroups.Visible = false;
            btnGroups.Active = false;

            btnGroups.Toggled += btnGroups_Toggled;
            groupsPanel.GroupsTree.Selection.Changed += GroupsTreeSelection_Changed;

            btnGroups_Toggled (null, null);
        }

        protected override void GetAllEntities ()
        {
            entities = CompanyRecord.GetAll ();
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

            selectedId = entities [selectedRow].Id;

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                using (EditNewCompanyRecord dialog = new EditNewCompanyRecord (entities [selectedRow])) {
                    if (dialog.Run () != ResponseType.Ok)
                        return;

                    dialog.GetCompanyRecord ().CommitChanges ();
                }
                OnEntitiesChanged ();
            }
        }

        protected override void btnNew_Clicked (object o, EventArgs args)
        {
            selectedId = null;

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                using (EditNewCompanyRecord dialog = new EditNewCompanyRecord (null)) {
                    if (dialog.Run () != ResponseType.Ok)
                        return;

                    selectedId = dialog.GetCompanyRecord ().CommitChanges ().Id;
                }
                OnEntitiesChanged ();
            }
        }

        protected override void btnDelete_Clicked (object o, EventArgs args)
        {
            DeleteSelectedEntities ();
        }

        protected override bool DeleteEntity (CompanyRecord entity)
        {
            selectedId = entity.Id;

            switch (CompanyRecord.CanDelete (entity.Id)) {
                case DeletePermission.InUse:
                    MessageError.ShowDialog (
                        string.Format (Translator.GetString ("The record \"{0}\" cannot be deleted, because it is used in operations. Please, delete the corresponding operations in order to delete this record!"), entity.Name),
                        "Icons.CompanyRecord16.png");
                    return false;

                case DeletePermission.Reserved:
                case DeletePermission.No:
                    MessageError.ShowDialog (
                        string.Format (Translator.GetString ("Cannot delete record \"{0}\"!"), entity.Name),
                        "Icons.CompanyRecord16.png");
                    return false;
            }

            CompanyRecord.Delete (entity.Id);
            return true;
        }

        protected override bool AskDeleteSingleEntity (CompanyRecord entity)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Company Record"),
                "Icons.CompanyRecord16.png",
                string.Format (Translator.GetString ("Do you want to delete company with name \"{0}\"?"), entity.Name),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override bool AskDeleteMultipleEntities (int count)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Company Records"),
                "Icons.CompanyRecord16.png",
                string.Format (Translator.GetString ("Do you want to delete the selected {0} company records?"), count),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        #endregion

        protected override void OnEntitiesChanged (long? groupId = null)
        {
            BusinessDomain.CurrentCompany = CompanyRecord.GetDefault ();
            PresentationDomain.RefreshMainFormStatusBar ();

            base.OnEntitiesChanged (groupId);
        }
    }
}
