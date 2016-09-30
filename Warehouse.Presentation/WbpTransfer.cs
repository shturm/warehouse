//
// WbpTransfer.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   10/04/2007
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

using Gtk;

using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Data.Model;
using Warehouse.Presentation.Dialogs;
using Item = Warehouse.Business.Entities.Item;

namespace Warehouse.Presentation
{
    public class WbpTransfer : WbpOperationBase<Transfer, TransferDetail>
    {
        protected override string PageDescription
        {
            get { return "Transfer"; }
        }

        public override string HelpFile
        {
            get { return "Transfer.html"; }
        }

        public override string PageTitle
        {
            get
            {
                if (pageTitle == null) {
                    if (operation.State == OperationState.Draft)
                        pageTitle = Translator.GetString ("Edit transfer draft");
                    else if (editMode)
                        pageTitle = string.Format ("{0} {1}",
                            Translator.GetString ("Edit transfer No."),
                            operation.FormattedOperationNumber);
                    else
                        pageTitle = Translator.GetString ("Transfer");
                }

                return pageTitle;
            }
        }

        public WbpTransfer ()
            : this (null)
        {
            cursorAtColumn = colItem.Index;
        }

        public WbpTransfer (long? transferId)
        {
            InitializeForm (transferId);
        }

        #region Initialization steps

        private void InitializeForm (long? transferId)
        {
            Image icon = FormHelper.LoadImage ("Icons.Transfer32.png");
            evbIcon.Add (icon);
            icon.Show ();

            SrcLocationVisible = true;
            DstLocationVisible = true;
            UserVisible = true;
            DateVisible = true;
            btnImport.Visible = BusinessDomain.DataImporters.Count > 0;

            ReInitializeForm (transferId);
        }

        private void ReInitializeForm (long? transferId)
        {
            if (transferId != null) {
                operation = Transfer.GetById (transferId.Value);

                txtSrcLocation.Text = operation.SourceLocation;
                txtDstLocation.Text = operation.TargetLocation;
                txtUser.Text = operation.UserName;
                SetDate (operation.Date);
                SetNote (operation.Details);

                SrcLocationSensitive = false;
                SetOperationTotalAndEditMode ();
            } else {
                operation = new Transfer ();

                operation.AddNewDetail ();
                operation.LoggedUserId = BusinessDomain.LoggedUser.Id;

                if (srcLocation == null) {
                    LazyListModel<Location> allLocations = Location.GetAll ();
                    if (allLocations.Count == 1)
                        SetSourceLocation (allLocations [0]);
                    else
                        txtSrcLocation.Text = string.Empty;
                } else
                    SetSourceLocation (srcLocation);
                txtDstLocation.Text = string.Empty;
                SetUser (BusinessDomain.LoggedUser);
                SetDate (BusinessDomain.Today);
                SetNote (operation.Details);

                SetOperationTotalAndNewMode ();
            }

            InitializeGrid ();
            BindGrid ();
        }

        public override void LoadOperation (Operation oper)
        {
            txtSrcLocation.Text = oper.Location;
            SourceLocationEvaluate ();

            txtUser.Text = oper.UserName;
            UserEvaluate ();

            SetDate (oper.Date);
            LoadOperationDetails (oper);
            SetOperationTotalAndNewMode ();
            SourceLocationFocus ();
        }

        protected override void InitializePurchasePriceColumn (ColumnController cc)
        {
            if (BusinessDomain.LoggedUser.HideItemsPurchasePrice)
                return;

            base.InitializePurchasePriceColumn (cc);
        }

        protected override void InitializeSalePriceColumn (ColumnController cc)
        {
            if (!BusinessDomain.LoggedUser.HideItemsPurchasePrice)
                return;

            base.InitializeSalePriceColumn (cc);
            colSalePrice.ListCell.IsEditable = false;
        }

        protected override void InitializeDiscountColumn (ColumnController cc)
        {
        }

        protected override void InitializeDiscountValueColumn (ColumnController cc)
        {
        }

        protected override void InitializeVatRateColumn (ColumnController cc)
        {
            base.InitializeVatRateColumn (cc);
            colVatRate.ListCell.IsEditable = false;
        }

        public override void OnPageAdding ()
        {
            MenuItemWrapper restNode = PresentationDomain.MainForm.MainMenu.FindMenuItem ("mnuOperTransfer");
            if (restNode.GetRestriction (BusinessDomain.LoggedUser.Id) != UserRestrictionState.Allowed)
                throw new WorkPageAddException (Translator.GetString ("Opening the Transfer screen is not allowed!"));

            OnPageAddingFinish ();
        }

        #endregion

        #region Finalization steps

        public override void OnOperationSave (bool askForConfirmation)
        {
            if (!OperationValidate ())
                return;

            if (!OperationDetailsValidate (true))
                return;

            if (askForConfirmation) {
                MessageOkCancel dialogSave;
                if (editMode) {
                    dialogSave = new MessageOkCancel (
                        Translator.GetString ("Edit transfer"), string.Empty,
                        Translator.GetString ("Do you want to save the changes?"), "Icons.Question32.png");
                } else {
                    dialogSave = new MessageOkCancel (
                        Translator.GetString ("Saving transfer"), string.Empty,
                        Translator.GetString ("Do you want to save the operation?"), "Icons.Question32.png");
                }

                if (dialogSave.Run () != ResponseType.Ok) {
                    operation.AddNewDetail ();
                    EditGridField (operation.Details.Count - 1, colItem.Index);
                    return;
                }
            }

            bool printReceipt = false;
            if (BusinessDomain.AppConfiguration.IsPrintingAvailable ()) {
                if (BusinessDomain.AppConfiguration.AskBeforeDocumentPrint == AskDialogState.NotSaved) {
                    using (MessageYesNoRemember dialogPrint = new MessageYesNoRemember (
                        Translator.GetString ("Print document"), string.Empty,
                        Translator.GetString ("Do you want to print a stock receipt?"), "Icons.Question32.png")) {
                        ResponseType resp = dialogPrint.Run ();
                        if (resp == ResponseType.Yes)
                            printReceipt = true;

                        if (dialogPrint.RememberChoice) {
                            BusinessDomain.AppConfiguration.AskBeforeDocumentPrint = resp == ResponseType.Yes ? AskDialogState.Yes : AskDialogState.No;
                        }
                    }
                } else if (BusinessDomain.AppConfiguration.AskBeforeDocumentPrint == AskDialogState.Yes) {
                    printReceipt = true;
                }
            }

            try {
                PrepareOpertionForSaving (operation, operation.SourceLocationId);
                CommitOperation ();
            } catch (InsufficientItemAvailabilityException ex) {
                MessageError.ShowDialog (
                    string.Format (Translator.GetString ("The transfer cannot be saved due to insufficient quantity of item \"{0}\"."), ex.ItemName),
                    ErrorSeverity.Warning, ex);
                EditGridField (ex.ItemName);
                return;
            } catch (Exception ex) {
                MessageError.ShowDialog (
                    Translator.GetString ("An error occurred while saving the transfer operation!"),
                    ErrorSeverity.Error, ex);
                return;
            }

            if (printReceipt && WaitForPendingOperationCompletion (operation, operation.SourceLocationId)) {
                try {
                    TransferReceipt receipt = new TransferReceipt (operation);
                    FormHelper.PrintPreviewObject (receipt);
                } catch (Exception ex) {
                    MessageError.ShowDialog (
                        Translator.GetString ("An error occurred while generating the stock receipt!"),
                        ErrorSeverity.Error, ex);
                }
            }

            OnOperationSaved (operation, operation.SourceLocationId);

            if (editMode) {
                OnPageClose ();
            } else {
                ReInitializeForm (null);
            }
        }

        protected override void CommitOperation ()
        {
            operation.Commit ();
        }

        protected override Message GetAskSaveEditDialog ()
        {
            return new MessageOkCancel (
                Translator.GetString ("Edit Transfer"), string.Empty,
                Translator.GetString ("Exit without saving the changes?"), "Icons.Question32.png");
        }

        protected override Message GetAskSaveNewDialog ()
        {
            return new MessageOkCancel (
                Translator.GetString ("Saving Transfer"), string.Empty,
                Translator.GetString ("Exit without saving the transfer?"), "Icons.Question32.png");
        }

        protected override string GetNoValidRowsWarning ()
        {
            return Translator.GetString ("There are no valid rows in the transfer document!");
        }

        protected override string GetInsufficientAvailabilityWarning ()
        {
            return Translator.GetString ("The transfer cannot be saved due to insufficient quantity of item \"{0}\".");
        }

        #endregion

        #region Entries handling

        protected override void SetSourceLocation (Location pos)
        {
            if (pos == null)
                return;

            operation.SourceLocationId = pos.Id;
            operation.SourceLocation = pos.Name;
            operation.SourceLocation2 = pos.Name2;
            txtSrcLocation.Text = pos.Name;
            locationPriceGroup = pos.PriceGroup;
            base.SetSourceLocation (pos);
        }

        protected override void SetDestinationLocation (Location pos)
        {
            if (pos == null)
                return;

            operation.TargetLocationId = pos.Id;
            operation.TargetLocation = pos.Name;
            operation.TargetLocation2 = pos.Name2;
            txtDstLocation.Text = pos.Name;
            base.SetDestinationLocation (pos);
        }

        #endregion

        #region Item column handling

        protected override long AvailabilityLocationId
        {
            get { return operation.SourceLocationId; }
        }

        #endregion
    }
}
