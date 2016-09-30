//
// WbpPurchase.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/18/2006
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
using Warehouse.Presentation.Dialogs;
using Item = Warehouse.Business.Entities.Item;

namespace Warehouse.Presentation
{
    public class WbpPurchase : WbpOperationWithPriceRules<Purchase, PurchaseDetail>
    {
        public override string HelpFile
        {
            get { return "Purchase.html"; }
        }

        public override string PageTitle
        {
            get
            {
                if (pageTitle == null) {
                    if (operation.State == OperationState.Draft)
                        pageTitle = Translator.GetString ("Edit purchase draft");
                    else if (editMode)
                        pageTitle = string.Format ("{0} {1}",
                            Translator.GetString ("Edit purchase No."),
                            operation.FormattedOperationNumber);
                    else
                        pageTitle = Translator.GetString ("Purchase");
                }

                return pageTitle;
            }
        }

        public WbpPurchase ()
            : this (null)
        {
            cursorAtColumn = colItem.Index;
        }

        public WbpPurchase (long? purchaseId)
        {
            InitializeForm (purchaseId);
        }

        #region Initialization steps

        private void InitializeForm (long? purchaseId)
        {
            Image icon = FormHelper.LoadImage ("Icons.Purchase32.png");
            evbIcon.Add (icon);
            icon.Show ();

            PartnerVisible = true;
            LocationVisible = true;
            UserVisible = true;
            DateVisible = true;
            if (BusinessDomain.AppConfiguration.AllowPercentDiscounts || BusinessDomain.AppConfiguration.AllowValueDiscounts)
                btnAddDiscount.Visible = true;
            if (!BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT)
                btnAddRemoveVAT.Visible = true;
            btnImport.Visible = BusinessDomain.DataImporters.Count > 0;

            ReInitializeForm (purchaseId);
        }

        protected override void ReInitializeForm (long? purchaseId)
        {
            base.ReInitializeForm (purchaseId);

            if (purchaseId != null) {
                operation = Purchase.GetById (purchaseId.Value);

                txtPartner.Text = operation.PartnerName;
                txtLocation.Text = operation.Location;
                txtUser.Text = operation.UserName;
                SetDate (operation.Date);
                SetNote (operation.Details);

                SetOperationTotalAndEditMode ();
            } else {
                operation = new Purchase ();

                operation.AddNewDetail ();
                operation.LoggedUserId = BusinessDomain.LoggedUser.Id;

                SetPartnerName ();
                SetLocationName ();
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
            base.LoadOperation (oper);
            PartnerFocus ();
        }

        protected override void InitializePurchasePriceColumn (ColumnController cc)
        {
            base.InitializePurchasePriceColumn (cc);
            colPurchasePrice.ListCell.IsEditable = true;
            colPurchasePrice.ListCell.PropertyName = "OriginalPriceIn";
        }

        protected override void InitializeLotNameColumn (ColumnController cc)
        {
            base.InitializeLotNameColumn (cc);
            colLot.ListCell.IsEditable = true;
        }

        protected override void InitializeSerialNumberColumn (ColumnController cc)
        {
            base.InitializeSerialNumberColumn (cc);
            colSerialNo.ListCell.IsEditable = true;
        }

        protected override void InitializeExpirationDateColumn (ColumnController cc)
        {
            base.InitializeExpirationDateColumn (cc);
            colExpirationDate.ListCell.IsEditable = true;
        }

        protected override void InitializeManufacturedDateColumn (ColumnController cc)
        {
            base.InitializeManufacturedDateColumn (cc);
            colProductionDate.ListCell.IsEditable = true;
        }

        protected override void InitializeLotLocationColumn (ColumnController cc)
        {
            base.InitializeLotLocationColumn (cc);
            colLotLocation.ListCell.IsEditable = true;
        }

        public override void OnPageAdding ()
        {
            MenuItemWrapper restNode = PresentationDomain.MainForm.MainMenu.FindMenuItem ("mnuOperDeliveries");
            if (restNode.GetRestriction (BusinessDomain.LoggedUser.Id) != UserRestrictionState.Allowed)
                throw new WorkPageAddException (Translator.GetString ("Opening the Purchase screen is not allowed!"));

            OnPageAddingFinish ();
        }

        #endregion

        protected override bool PurchaseValueColumnEvaluate (int row, string price)
        {
            return CheckPurchasePrice (row, price) && base.PurchaseValueColumnEvaluate (row, price);
        }

        protected override bool SalePriceColumnEvaluate (int row, string price)
        {
            return CheckSalePrice (row, price) && base.SalePriceColumnEvaluate (row, price);
        }

        #region Finalization steps

        public override void OnOperationSave (bool askForConfirmation)
        {
            #region Prepare the purchase object

            if (!OperationValidate ())
                return;

            if (!OperationDetailsValidate (true))
                return;

            #endregion

            if (!TryApplyRules ())
                return;

            bool printPayment;
            using (EditNewPayment dialogFinalize = new EditNewPayment (operation)) {
                if (dialogFinalize.Run () != ResponseType.Ok) {
                    ClearDetailsFromPriceRules (true);
                    return;
                }
                printPayment = dialogFinalize.PrintDocument;
            }

            if (!PriceRule.ApplyOnPaymentSet (operation))
                return;

            try {
                var newPayments = GetAllNewPayments (printPayment);
                PrepareOpertionForSaving ();
                CommitOperation ();
                PrintAllNewPayments (newPayments);
            } catch (InsufficientItemAvailabilityException ex) {
                MessageError.ShowDialog (string.Format (Translator.GetString ("The purchase cannot be saved due to insufficient quantities of item \"{0}\"."), ex.ItemName),
                    ErrorSeverity.Warning, ex);
                ClearDetailsFromPriceRules ();
                EditGridField (ex.ItemName);
                return;
            } catch (Exception ex) {
                MessageError.ShowDialog (Translator.GetString ("An error occurred while saving purchase document!"),
                    ErrorSeverity.Error, ex);
                ClearDetailsFromPriceRules (true);
                return;
            }

            ApplyFinalPriceRules ();

            if (BusinessDomain.AppConfiguration.AutoCreateInvoiceOnPurchase && !editMode &&
                WaitForPendingOperationCompletion ())
                using (EditNewInvoice dlgInvoice = new EditNewInvoice (operation))
                    dlgInvoice.Run ();

            #region Ask to print a document

            bool printReceipt = false;

            if (BusinessDomain.AppConfiguration.IsPrintingAvailable () && BusinessDomain.WorkflowManager.AllowPurchaseReceiptPrint (operation, false)) {
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

            if (printReceipt && WaitForPendingOperationCompletion ()) {
                try {
                    PurchaseReceipt receipt = new PurchaseReceipt (operation);
                    FormHelper.PrintPreviewObject (receipt);
                } catch (Exception ex) {
                    MessageError.ShowDialog (
                        Translator.GetString ("An error occurred while generating stock receipt!"),
                        ErrorSeverity.Error, ex);
                }
            }

            #endregion

            OnOperationSaved ();

            if (editMode) {
                OnPageClose ();
            } else {
                ReInitializeForm (null);
            }
        }

        protected override void CommitOperation ()
        {
            operation.Commit (GetOperationPriceGroup ());
        }

        protected override Message GetAskSaveEditDialog ()
        {
            return new MessageOkCancel (
                Translator.GetString ("Edit Purchase"), string.Empty,
                Translator.GetString ("Exit without saving the changes?"), "Icons.Question32.png");
        }

        protected override Message GetAskSaveNewDialog ()
        {
            return new MessageOkCancel (
                Translator.GetString ("Saving Purchase"), string.Empty,
                Translator.GetString ("Exit without saving the purchase?"), "Icons.Question32.png");
        }

        protected override string GetNoValidRowsWarning ()
        {
            return Translator.GetString ("There are no valid rows in the purchase document!");
        }

        protected override bool ValidateLotQuantity (Item item, BindList<PurchaseDetail> details, int i, bool showWarning)
        {
            return true;
        }

        protected override string GetInsufficientAvailabilityWarning ()
        {
            return Translator.GetString ("The purchase cannot be saved due to insufficient quantities of item \"{0}\".");
        }

        #endregion

        protected override string PageDescription
        {
            get { return "Purchase"; }
        }
    }
}
