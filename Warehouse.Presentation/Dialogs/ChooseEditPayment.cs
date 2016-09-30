//
// ChooseEditPayment.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   01.11.2010
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
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Business.Reporting;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Presentation.Reporting;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseEditPayment : ChooseEdit<Payment, EmptyGroup>
    {
        private ReportFilterDateRange reportFilterDateRange;

        public ChooseEditPayment ()
        {
            Initialize ();

            algGridGroups.Visible = false;
            btnGroups.Visible = false;
        }

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            editAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditPaysPaymentsbtnEdit") == UserRestrictionState.Allowed;
            deleteAllowed = BusinessDomain.RestrictionTree.GetRestriction ("mnuEditPaysPaymentsbtnDelete") == UserRestrictionState.Allowed;

            InitializeGrid ();

            CreateDateFilter ();

            ReinitializeGrid (true, null);
            grid.Sort ("OperationId", SortDirection.Descending);

            InitializeFormStrings ();
        }

        protected override string KeyForKeyboardBingdings
        {
            get { return "mnuEditPaysPayments"; }
        }

        private void CreateDateFilter ()
        {
            reportFilterDateRange = new ReportFilterDateRange (true, true, DataFilterLabel.PaymentDate, DataField.PaymentDate);

            DataFilter dataFilter = null;
            DataQuery dataQuery;
            if (BusinessDomain.DocumentQueryStates.TryGetValue (typeof (Payment).FullName, out dataQuery))
                dataFilter = dataQuery.Filters.Count > 0 ? dataQuery.Filters [0] : null;

            if (dataFilter == null)
                dataFilter = new DataFilter { Values = new object [] { null, null, FilterDateRange.DateRanges.Today } };

            reportFilterDateRange.SetDataFilter (dataFilter);
            reportFilterDateRange.FilterChanged += (sender, e) => ReinitializeGrid (true, null);

            tblFilter.Attach (reportFilterDateRange.Label, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            tblFilter.Attach (reportFilterDateRange.EntryWidget, 1, 3, 1, 2, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            tblFilter.ShowAll ();
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();

            grid.WidthRequest = 620;
            grid.HeightRequest = 300;

            ColumnController columnController = new ColumnController ();

            CellTextNumber cellTextNumber = new CellTextNumber ("OperationId") { FixedDigits = BusinessDomain.AppConfiguration.DocumentNumberLength };
            string documentNumber = Translator.GetString ("Document No.");
            columnController.Add (new Column (documentNumber, cellTextNumber, 1, cellTextNumber.PropertyName) { MinWidth = 70 });

            CellTextLookup<int> cellOperation = new CellTextLookup<int> ("OperationType");
            foreach (OperationType operationType in Enum.GetValues (typeof (OperationType)))
                if (operationType > 0)
                    cellOperation.Lookup.Add ((int) operationType, Translator.GetOperationTypeGlobalName (operationType));

            string operation = Translator.GetString ("Operation");
            columnController.Add (new Column (operation, cellOperation, 1, cellOperation.PropertyName) { MinWidth = 70 });

            string partner = Translator.GetString ("Partner");
            columnController.Add (new Column (partner, "PartnerName", 2, "PartnerName") { MinWidth = 100 });

            string location = Translator.GetString ("Location");
            columnController.Add (new Column (location, "LocationName", 2, "LocationName") { MinWidth = 100 });

            CellTextCurrency cellTextDouble = new CellTextCurrency ("Quantity");
            string balance = Translator.GetString ("Balance");
            columnController.Add (new Column (balance, cellTextDouble, 1, cellTextDouble.PropertyName) { MinWidth = 70 });

            grid.ColumnController = columnController;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseEdit.Title = Translator.GetString ("Payments");

            btnNew.SetChildLabelText (Translator.GetString ("New", "Payment"));
        }

        public override string [] SelectedItemsText
        {
            get { throw new NotImplementedException (); }
        }

        protected override void btnNew_Clicked (object o, EventArgs args)
        {
            Partner partner = null;
            if (BusinessDomain.LoggedUser.LockedPartnerId > 0)
                partner = Partner.GetById (BusinessDomain.LoggedUser.LockedPartnerId);
            else
                using (ChooseEditPartner dialog = new ChooseEditPartner (true, string.Empty))
                    if (dialog.Run () == ResponseType.Ok && dialog.SelectedItems.Length > 0)
                        partner = dialog.SelectedItems [0];

            if (partner == null)
                return;

            using (EditNewAdvancePayment dialog = new EditNewAdvancePayment (partner)) {
                if (dialog.Run () != ResponseType.Ok || dialog.Payments.Count <= 0)
                    return;
                
                BindList<Payment> advances = dialog.Payments;
                List<Payment> savedPayments = Payment.DistributeAdvances (advances, partner.Id);
                for (int i = advances.Count - 1; i >= 0; i--) {
                    if (advances [i].Sign < 0)
                        advances.RemoveAt (i);
                    else
                        advances [i].CommitAdvance ();
                }

                savedPayments.AddRange (advances);
                if (savedPayments.Count > 0)
                    dialog.PrintPayments (savedPayments);

                ReinitializeGrid (true, null);
            }
        }

        protected override void btnEdit_Clicked (object o, EventArgs args)
        {
            if (grid.Selection.Count <= 0) {
                MessageError.ShowDialog (Translator.GetString ("Please select an operation to edit a payment of."), ErrorSeverity.Error);
                return;
            }

            Payment selectedPayment = entities [grid.Selection [0]];
            string message;
            if (!BusinessDomain.CanEditPayment (selectedPayment, out message)) {
                MessageError.ShowDialog (message);
                return;
            }

            selectedId = selectedPayment.Id;
            if (selectedPayment.OperationType == (int) OperationType.AdvancePayment) {
                using (new DbMasterScope (BusinessDomain.DataAccessProvider))
                using (EditNewAdvancePayment dialog = new EditNewAdvancePayment (selectedPayment)) {
                    if (dialog.Run () != ResponseType.Ok || dialog.Payments.Count == 0)
                        return;

                    foreach (Payment payment in dialog.Payments)
                        payment.EditAdvance ();

                    dialog.PrintPayments (dialog.Payments);
                    ReinitializeGrid (true, null);
                    return;
                }
            }

            Operation operation = GetOperationOfSelectedPayment ();
            if (operation == null)
                return;

            using (new DbMasterScope (BusinessDomain.DataAccessProvider))
            using (EditNewPayment dialog = new EditNewPayment (operation)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                operation.CommitPayments ();
                dialog.PrintPayments (dialog.OriginalPayments, operation);
                ReinitializeGrid (true, null);
            }
        }

        protected override void btnDelete_Clicked (object o, EventArgs args)
        {
            DeleteSelectedEntities ();
        }

        protected override bool DeleteEntity (Payment entity)
        {
            selectedId = entity.Id;

            if (entity.OperationType == (int) OperationType.AdvancePayment) {
                entity.Quantity = 0;
                try {
                    entity.CommitChanges ();
                } catch (Exception ex) {
                    ErrorHandling.LogException (ex);
                }
                return true;
            }

            Operation operation = GetOperationOfSelectedPayment (entity);
            if (operation == null)
                return false;

            operation.Payments.AddRange (Payment.GetForOperation (operation, PaymentMode.Paid));
            if (operation.Payments.Find (p => p.Mode == PaymentMode.Paid) == null) {
                MessageError.ShowDialog (Translator.GetString ("There are no payments made for this operation."), ErrorSeverity.Information);
                return false;
            }

            foreach (Payment payment in operation.Payments)
                payment.Quantity = 0;

            operation.CommitPayments ();
            return true;
        }

        protected override bool AskDeleteSingleEntity (Payment entity)
        {
            if (entity.OperationType == (int) OperationType.AdvancePayment)
                using (MessageOkCancel deletePayments = new MessageOkCancel (
                    Translator.GetString ("Delete Advance Payment"), string.Empty,
                    Translator.GetString ("Are you sure you want to delete this advance payment?"), "Icons.Delete32.png"))
                    return deletePayments.Run () == ResponseType.Ok;

            using (MessageOkCancel deletePayments = new MessageOkCancel (
                Translator.GetString ("Delete Payments"), string.Empty,
                Translator.GetString ("Are you sure you want to delete all payments of this operation?"), "Icons.Delete32.png"))
                return deletePayments.Run () == ResponseType.Ok;
        }

        protected override bool AskDeleteMultipleEntities (int count)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Payments"), string.Empty,
                string.Format (Translator.GetString ("Do you want to delete the payments for the selected {0} operations?"), count),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        private Operation GetOperationOfSelectedPayment (Payment entity = null)
        {
            Payment selectedPayment = entity ?? entities [grid.Selection [0]];
            selectedId = selectedPayment.Id;

            Operation operation = Operation.GetById ((OperationType) selectedPayment.OperationType, selectedPayment.OperationId);
            if (operation == null)
                return null;

            Payment [] duePayments = Payment.GetForOperation (operation, PaymentMode.Due);
            if (duePayments.Length > 0)
                operation.Debt = duePayments [0];
            if (operation.Payments.Count > 0)
                selectedId = operation.Payments [0].Id;
            return operation;
        }

        protected override void GetAllEntities ()
        {
            DataQuery dataQuery = new DataQuery (reportFilterDateRange.GetDataFilter ());

            entities = Payment.GetAllPerOperation (dataQuery);
            entities.FilterProperties.Add ("OperationId");
            entities.FilterProperties.Add ("PartnerName");
            entities.FilterProperties.Add ("LocationName");

            BusinessDomain.DocumentQueryStates [typeof (Payment).FullName] = dataQuery;
        }
    }
}
