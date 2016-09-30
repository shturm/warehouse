//
// ChooseEditVATGroup.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
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
using System.Linq;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseEditVATGroup : ChooseEdit<VATGroup, EmptyGroup>
    {
        public override string [] SelectedItemsText
        {
            get { return SelectedItems.Select (sel => sel.Name).ToArray (); }
        }

        protected override string KeyForKeyboardBingdings
        {
            get { return "editVATGroups"; }
        }

        public ChooseEditVATGroup ()
        {
            Initialize ();
        }

        public ChooseEditVATGroup (bool pickMode, string filter)
            : base (filter)
        {
            this.pickMode = pickMode;

            Initialize ();
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            Image icon = FormHelper.LoadImage ("Icons.VATGroup16.png");
            dlgChooseEdit.Icon = icon.Pixbuf;
            icon.Show ();

            icon = FormHelper.LoadImage ("Icons.VATGroup32.png");
            algDialogIcon.Add (icon);
            icon.Show ();

            newAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditVATGroupsbtnNew") == UserRestrictionState.Allowed;
            editAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditVATGroupsbtnEdit") == UserRestrictionState.Allowed;
            deleteAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditVATGroupsbtnDelete") == UserRestrictionState.Allowed;
            btnGroups.Visible = false;
            btnImport.Sensitive = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditVATGroupsbtnImport") == UserRestrictionState.Allowed;
            btnExport.Sensitive = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditVATGroupsbtnExport") == UserRestrictionState.Allowed;
            btnImport.Visible = BusinessDomain.DataImporters.Count > 0;
            btnExport.Visible = BusinessDomain.DataExporters.Count > 0;
            dlgChooseEdit.HeightRequest = 440;
            dlgChooseEdit.WidthRequest = 610;

            InitializeFormStrings ();
            InitializeGrid ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseEdit.Title = BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                Translator.GetString ("Tax Groups") : Translator.GetString ("VAT Groups");

            btnNew.SetChildLabelText (Translator.GetString ("New", "VAT group"));
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();

            grid.ColumnController = new ColumnController
                {
                    new Column (Translator.GetString ("Code"), "CodeName", 0.1, "CodeName") { MinWidth = 70 },
                    new Column (Translator.GetString ("Name"), "Name", 0.2, "Name") { MinWidth = 70 },
                    new Column (Translator.GetString ("Value (%)"), new CellTextDouble ("VatValue") { FixedFaction = BusinessDomain.AppConfiguration.PercentPrecision },
                        0.05, "VatValue") { MinWidth = 70 }
                };

            btnGroups.Active = false;
            btnGroups_Toggled (null, null);
        }

        protected override void GetAllEntities ()
        {
            entities = VATGroup.GetAll ();
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
                using (EditNewVATGroup dialog = new EditNewVATGroup (entities [selectedRow])) {
                    if (dialog.Run () != ResponseType.Ok)
                        return;

                    dialog.GetVATGroup ().CommitChanges ();
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
                using (EditNewVATGroup dialog = new EditNewVATGroup (null)) {
                    if (dialog.Run () != ResponseType.Ok)
                        return;

                    selectedId = dialog.GetVATGroup ().CommitChanges ().Id;
                }

                OnEntitiesChanged ();
            }
        }

        protected override void btnDelete_Clicked (object o, EventArgs args)
        {
            DeleteSelectedEntities ();
        }

        protected override bool DeleteEntity (VATGroup entity)
        {
            selectedId = entity.Id;

            switch (VATGroup.RequestDelete (entity.Id)) {
                case DeletePermission.InUse:
                    MessageError.ShowDialog (BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                        Translator.GetString ("This tax group cannot be deleted, because it is used by items.") :
                        Translator.GetString ("This VAT group cannot be deleted, because it is used by items."), "Icons.VATGroup16.png");
                    return false;

                case DeletePermission.Reserved:
                case DeletePermission.No:
                    MessageError.ShowDialog (string.Format (BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                        Translator.GetString ("Cannot delete tax group \"{0}\"!") :
                        Translator.GetString ("Cannot delete VAT group \"{0}\"!"), entity.Name),
                        "Icons.VATGroup16.png");
                    return false;
            }

            VATGroup.Delete (entity.Id);
            return true;
        }

        protected override bool AskDeleteSingleEntity (VATGroup entity)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                    Translator.GetString ("Delete Tax Group") :
                    Translator.GetString ("Delete VAT Group"),
                "Icons.VATGroup16.png",
                string.Format (BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                    Translator.GetString ("Do you want to delete tax group \"{0}\"?") :
                    Translator.GetString ("Do you want to delete VAT group \"{0}\"?"), entity.Name),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override bool AskDeleteMultipleEntities (int count)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                    Translator.GetString ("Delete Tax Groups") :
                    Translator.GetString ("Delete VAT Groups"),
                "Icons.VATGroup16.png",
                string.Format (BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                    Translator.GetString ("Do you want to delete the selected {0} Tax groups?") :
                    Translator.GetString ("Do you want to delete the selected {0} VAT groups?"), count),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override void btnImport_Clicked (object o, EventArgs args)
        {
            int selectedRow = grid.FocusedRow;
            if (selectedRow >= 0)
                selectedId = entities [selectedRow].Id;

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                FormHelper.ImportData<VATGroup> ();
                OnEntitiesChanged ();
            }
        }

        protected override void btnExport_Clicked (object o, EventArgs args)
        {
            FormHelper.ExportData<VATGroup> ("vatgroups", "VAT Groups", new DataExchangeSet (BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                Translator.GetString ("Tax Groups") :
                Translator.GetString ("VAT Groups"), entities.ToDataTable (false)));
        }

        #endregion

        protected override void OnEntitiesChanged (long? groupId = null)
        {
            base.OnEntitiesChanged (groupId);

            BusinessDomain.InvalidateHideVATColumns ();
        }
    }
}
