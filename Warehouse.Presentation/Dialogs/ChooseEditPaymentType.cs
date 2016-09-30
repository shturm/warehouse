//
// ChooseEditPaymentType.cs
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
    public class ChooseEditPaymentType : ChooseEdit<PaymentType, EmptyGroup>
    {
        public override string [] SelectedItemsText
        {
            get
            {
                return SelectedItems.Select (sel => sel.Name).ToArray ();
            }
        }

        public ChooseEditPaymentType ()
        {
            Initialize ();
        }

        public ChooseEditPaymentType (bool pickMode, string filter)
            : base (filter)
        {
            this.pickMode = pickMode;

            Initialize ();
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            Image icon = FormHelper.LoadImage ("Icons.PaymentType16.png");
            dlgChooseEdit.Icon = icon.Pixbuf;
            icon.Show ();

            icon = FormHelper.LoadImage ("Icons.PaymentType32.png");
            algDialogIcon.Add (icon);
            icon.Show ();

            dlgChooseEdit.HeightRequest = 440;
            dlgChooseEdit.WidthRequest = 610;

            InitializeFormStrings ();
            InitializeGrid ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseEdit.Title = Translator.GetString ("Payment types");

            btnNew.SetChildLabelText (Translator.GetString ("New", "Payment type"));
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();

            ColumnController cc = new ColumnController ();

            Column col = new Column (Translator.GetString ("Name"), "Name", 0.2, "Name") { MinWidth = 100 };
            cc.Add (col);

            CellTextLookup<int> cellPaymentType = new CellTextLookup<int> ("BaseType")
                .Load (PaymentType.GetAllBaseTypePairs ());
            col = new Column (Translator.GetString ("Type"), cellPaymentType, 0.1, "BaseType") { MinWidth = 100 };
            cc.Add (col);

            grid.ColumnController = cc;

            btnGroups.Visible = false;
            btnGroups.Active = false;
            btnGroups_Toggled (null, null);
        }

        protected override void GetAllEntities ()
        {
            entities = PaymentType.GetAll ();
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
            using (DbMasterScope scope = new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                using (EditNewPaymentType dialog = new EditNewPaymentType (entities [selectedRow])) {
                    if (dialog.Run () != ResponseType.Ok)
                        return;

                    dialog.GetPaymentType ().CommitChanges ();
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
                using (EditNewPaymentType dialog = new EditNewPaymentType (null)) {
                    if (dialog.Run () != ResponseType.Ok)
                        return;

                    selectedId = dialog.GetPaymentType ().CommitChanges ().Id;
                }

                OnEntitiesChanged ();
            }
        }

        protected override void btnDelete_Clicked (object o, EventArgs args)
        {
            DeleteSelectedEntities ();
        }

        protected override bool DeleteEntity (PaymentType entity)
        {
            selectedId = entity.Id;

            switch (PaymentType.RequestDelete (entity.Id)) {
                case DeletePermission.InUse:
                    MessageError.ShowDialog (
                        string.Format (Translator.GetString ("The payment type \"{0}\" cannot be deleted, because it is used for payments."), entity.Name),
                        "Icons.PaymentType16.png");
                    return false;

                case DeletePermission.Reserved:
                case DeletePermission.No:
                    MessageError.ShowDialog (
                        string.Format (Translator.GetString ("Cannot delete payment type \"{0}\"!"), entity.Name),
                        "Icons.PaymentType16.png");
                    return false;
            }

            PaymentType.Delete (entity.Id);
            return true;
        }

        protected override bool AskDeleteSingleEntity (PaymentType entity)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Payment Type"),
                "Icons.PaymentType16.png",
                string.Format (Translator.GetString ("Do you want to delete payment type \"{0}\"?"), entity.Name),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override bool AskDeleteMultipleEntities (int count)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Payment Types"),
                "Icons.PaymentType16.png",
                string.Format (Translator.GetString ("Do you want to delete the selected {0} payment types?"), count),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        #endregion
    }
}
