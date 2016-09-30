//
// ChooseEditComplexRecipe.cs
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
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseEditComplexRecipe : ChooseEdit<ComplexRecipe, EmptyGroup>
    {
        public override string [] SelectedItemsText
        {
            get
            {
                return SelectedItems.Select (sel => sel.Name).ToArray ();
            }
        }

        public ChooseEditComplexRecipe ()
        {
            Initialize ();
        }

        public ChooseEditComplexRecipe (bool pickMode, string filter)
            : base (filter)
        {
            this.pickMode = pickMode;

            Initialize ();
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            Image icon = FormHelper.LoadImage ("Icons.Recipe16.png");
            dlgChooseEdit.Icon = icon.Pixbuf;
            icon.Show ();

            icon = FormHelper.LoadImage ("Icons.Recipe32.png");
            algDialogIcon.Add (icon);
            icon.Show ();

            btnImport.Visible = false; //BusinessDomain.DataImporters.Count > 0;
            btnExport.Visible = false; //BusinessDomain.DataExporters.Count > 0;
            dlgChooseEdit.HeightRequest = 400;
            dlgChooseEdit.WidthRequest = 650;

            InitializeFormStrings ();
            InitializeGrid ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseEdit.Title = Translator.GetString ("Recipes");

            btnNew.SetChildLabelText (Translator.GetString ("New", "Recipe"));
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();

            ColumnController cc = new ColumnController ();

            CellTextNumber ctn = new CellTextNumber ("Id") { FixedDigits = BusinessDomain.AppConfiguration.DocumentNumberLength };
            cc.Add (new Column (Translator.GetString ("Number"), ctn, 0.1, "Id") { MinWidth = 90 });
            cc.Add (new Column (Translator.GetString ("Name"), "Name", 1, "Name") { MinWidth = 100 });

            grid.ColumnController = cc;

            btnGroups.Visible = false;
            btnGroups.Active = false;
            btnGroups_Toggled (null, null);
        }

        protected override void GetAllEntities ()
        {
            entities = ComplexRecipe.GetAll ();
            entities.FilterProperties.Add ("Id");
            entities.FilterProperties.Add ("Name");
        }

        #endregion

        #region Button handling

        protected override void btnEdit_Clicked (object o, EventArgs args)
        {
            int selectedRow = grid.FocusedRow;
            if (selectedRow < 0)
                return;

            ComplexRecipe recipe = entities [selectedRow];
            selectedId = recipe.Id;

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                using (EditNewComplexRecipe dialog = new EditNewComplexRecipe (recipe)) {
                    if (dialog.Run () == ResponseType.Ok)
                        dialog.GetRecipe ().CommitChanges ();
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
                using (EditNewComplexRecipe dialog = new EditNewComplexRecipe (null)) {
                    if (dialog.Run () != ResponseType.Ok)
                        return;

                    selectedId = dialog.GetRecipe ().CommitChanges ().Id;
                }

                OnEntitiesChanged ();
            }
        }

        protected override void btnDelete_Clicked (object o, EventArgs args)
        {
            DeleteSelectedEntities ();
        }

        protected override bool DeleteEntity (ComplexRecipe entity)
        {
            selectedId = entity.Id;

            entity.ClearDetails ();
            entity.CommitChanges ();
            return true;
        }

        protected override bool AskDeleteSingleEntity (ComplexRecipe entity)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (Translator.GetString ("Delete Recipe"),
                "Icons.Recipe16.png",
                string.Format (Translator.GetString ("Do you want to delete recipe \"{0}\"?"), entity.Name),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override bool AskDeleteMultipleEntities (int count)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Recipes"),
                "Icons.Recipe16.png",
                string.Format (Translator.GetString ("Do you want to delete the selected {0} recipes?"), count),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override void btnImport_Clicked (object o, EventArgs args)
        {
            //int selectedRow = grid.FocusedRow;
            //if (selectedRow >= 0)
            //    selectedId = Entities [selectedRow].Id;

            //FormHelper.ImportData<User> (CustomValidate, dlgChooseEdit);
            //ReinitializeGrid (true);
        }

        //private static void CustomValidate (object sender, ValidateEventArgs e)
        //{
        //    User user = (User) sender;
        //    if (user.UserLevel > BusinessDomain.LoggedUser.UserLevel) {
        //        e.Callback (string.Format (Translator.GetString ("The user \"{0}\" has higher access level than current\'s one and will not be imported!"), user.Name),
        //            ErrorSeverity.Error, -1, e.State);
        //        e.IsValid = false;
        //    } else {
        //        e.IsValid = true;
        //    }
        //}

        protected override void btnExport_Clicked (object o, EventArgs args)
        {
            //FormHelper.ExportData (Translator.GetString ("Users"), Entities.ToDataTable (), dlgChooseEdit);
        }

        #endregion
    }
}
