//
// WbpSale.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   03/26/2006
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
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Presentation.Dialogs;

namespace Warehouse.Presentation
{
    public class WbpSale : WbpSale<Sale, SaleDetail>
    {
        public WbpSale ()
        {
        }

        public WbpSale (long? saleId)
            : base (saleId)
        {
        }

        #region Overrides of WbpSale<Sale,SaleDetail>

        protected override Sale GetById (long saleId)
        {
            return Sale.GetById (saleId);
        }

        #endregion
    }

    public abstract class WbpSale<TOper, TOperDetail> : WbpOperationWithPriceRules<TOper, TOperDetail>
        where TOperDetail : SaleDetail, new ()
        where TOper : Sale<TOperDetail>, new ()
    {
        protected override string PageDescription
        {
            get { return "Sale"; }
        }

        public override string HelpFile
        {
            get { return "Sale.html"; }
        }

        public override string PageTitle
        {
            get
            {
                if (pageTitle == null) {
                    if (operation.State == OperationState.Draft)
                        pageTitle = Translator.GetString ("Edit sale draft");
                    else if (editMode)
                        pageTitle = string.Format ("{0} {1}",
                            Translator.GetString ("Edit sale No."),
                            operation.FormattedOperationNumber);
                    else
                        pageTitle = Translator.GetString ("Sale");
                }

                return pageTitle;
            }
        }

        protected WbpSale ()
            : this (null)
        {
            cursorAtColumn = colItem.Index;
        }

        protected WbpSale (long? saleId)
        {
            InitializeForm (saleId);
        }

        #region Initialization steps

        protected virtual void InitializeForm (long? saleId)
        {
            Image icon = FormHelper.LoadImage ("Icons.Sale32.png");
            evbIcon.Add (icon);
            icon.Show ();

            PartnerVisible = true;
            LocationVisible = true;
            UserVisible = true;
            DateVisible = true;
            btnAddDiscount.Visible = BusinessDomain.AppConfiguration.AllowPercentDiscounts || BusinessDomain.AppConfiguration.AllowValueDiscounts;
            btnAddRemoveVAT.Visible = true;
            btnImport.Visible = BusinessDomain.DataImporters.Count > 0;

            ReInitializeForm (saleId);
        }

        protected override void ReInitializeForm (long? saleId)
        {
            base.ReInitializeForm (saleId);

            if (saleId != null) {
                operation = GetById (saleId.Value);

                txtPartner.Text = operation.PartnerName;
                txtLocation.Text = operation.Location;
                txtUser.Text = operation.UserName;
                SetDate (operation.Date);
                SetNote (operation.Details);

                SetOperationTotalAndEditMode ();
            } else {
                operation = new TOper ();

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

        protected abstract TOper GetById (long saleId);

        public override void LoadOperation (Operation oper)
        {
            base.LoadOperation (oper);
            PartnerFocus ();
        }

        protected override void InitializePurchasePriceColumn (ColumnController cc)
        {
        }

        protected override void InitializeSalePriceColumn (ColumnController cc)
        {
            base.InitializeSalePriceColumn (cc);
            colSalePrice.ListCell.PropertyName = "OriginalPriceOut";
            colSalePrice.HeaderText = Translator.GetString ("Price");
        }

        protected override bool EditGridField (int row, int col)
        {
            if (row != grid.FocusedRow && grid.FocusedRow >= 0 && grid.FocusedRow < operation.Details.Count)
                ShowSimpleView ();

            if (!base.EditGridField (row, col))
                return false;
            ShowSimpleView ();
            return true;
        }

        protected virtual void ShowSimpleView ()
        {
            int row = grid.FocusedCell.Row;
            if (row < 0)
                return;

            SaleDetail saleRow = operation.Details [row];
            if (string.IsNullOrEmpty (saleRow.ItemName))
                return;

            HardwareErrorResponse res;
            do {
                res.Retry = false;
                try {
                    BusinessDomain.DeviceManager.DisplaySaleDetail (operation, saleRow);
                } catch (HardwareErrorException ex) {
                    res = FormHelper.HandleHardwareError (ex);
                    if (!res.Retry)
                        break;
                }
            } while (res.Retry);
        }

        public override void OnPageAdding ()
        {
            MenuItemWrapper restNode = PresentationDomain.MainForm.MainMenu.FindMenuItem ("mnuOperSales");
            if (restNode.GetRestriction (BusinessDomain.LoggedUser.Id) != UserRestrictionState.Allowed)
                throw new WorkPageAddException (Translator.GetString ("Opening the Sale screen is not allowed!"));

            OnPageAddingFinish ();
        }

        #endregion

        #region Finalization steps

        public override void OnOperationSave (bool askForConfirmation)
        {
            FinalizeSale (null);
        }

        protected virtual void FinalizeSale (BasePaymentType? paymentType)
        {
            #region Prepare the sale object

            if (!OperationValidate ())
                return;

            if (!OperationDetailsValidate (true))
                return;

            #endregion

            if (!TryApplyRules ())
                return;

            Sale kitchenSale = (Sale) operation.GetSale ().Clone ();
            if (!TryAddServiceCharge ())
                return;

            if (!TryFinalizeSale (paymentType, kitchenSale))
                return;

            Sale sale = operation.GetSale ();

            PrintInvoice (sale);

            if (AskPrintReceipt ())
                PrintReceipt (sale);

            OnOperationSaved (sale);

            #region Prepare for the next sale

            if (editMode) {
                OnPageClose ();
            } else {
                ReInitializeForm (null);
            }

            #endregion
        }

        protected static bool AskPrintReceipt ()
        {
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

            return printReceipt;
        }

        protected void PrintReceipt (Sale sale)
        {
            if (!WaitForPendingOperationCompletion (sale))
                return;

            try {
                SaleReceipt receipt = new SaleReceipt ((Sale) sale.Clone ());
                FormHelper.PrintPreviewObject (receipt);
            } catch (Exception ex) {
                MessageError.ShowDialog (
                    Translator.GetString ("An error occurred while generating stock receipt! Please check the printer settings."),
                    ErrorSeverity.Error, ex);
            }
        }

        protected void PrintInvoice (Sale sale)
        {
            if (!BusinessDomain.AppConfiguration.AutoCreateInvoiceOnSale || editMode)
                return;

            if (!WaitForPendingOperationCompletion (sale))
                return;

            using (EditNewInvoice dlgInvoice = new EditNewInvoice ((Sale) sale.Clone ())) {
                dlgInvoice.Run ();
            }
        }

        protected bool TryFinalizeSale (BasePaymentType? paymentType, Sale kitchenSale)
        {
            HardwareErrorResponse res;
            do {
                res.Retry = false;
                try {
                    BusinessDomain.DeviceManager.DisplayTotal (operation.TotalPlusVAT);
                } catch (HardwareErrorException ex) {
                    res = FormHelper.HandleHardwareError (ex);
                    if (!res.Retry)
                        break;
                }
            } while (res.Retry);

            double total = 0;
            double totalReceived = 0;

            res.AskForPayment = true;
            res.RetryWithoutPrint = false;
            FinalizeAction action = FinalizeAction.CommitSale | FinalizeAction.PrintKitchen;
            do {
                res.Retry = false;

                try {
                    BindList<Payment> finalPayments = null;
                    BindList<Payment> advances = new BindList<Payment> ();
                    bool printPayment = false;
                    if (res.AskForPayment) {
                        action = FinalizeAction.CommitSale;
                        if (!editMode)
                            action |= FinalizeAction.PrintKitchen;

                        bool printFiscal = BusinessDomain.AppConfiguration.PrintFinalReceiptByDefault;
                        if (paymentType == BasePaymentType.BankTransfer && !BusinessDomain.AppConfiguration.PrintBankCashReceiptByDefault)
                            printFiscal = false;

                        using (EditNewPayment dialogFinalize = new EditNewPayment (operation, paymentType, printFiscal)) {
                            PreparePaymentDialog (dialogFinalize);
                            if (dialogFinalize.Run () != ResponseType.Ok) {
                                ClearDetailsFromPriceRules (true);
                                return false;
                            }
                            if (dialogFinalize.ChangeIsReturnedSensitive)
                                BusinessDomain.AppConfiguration.SaleChangeIsReturned = dialogFinalize.ChangeIsReturned;

                            total = dialogFinalize.Total;
                            totalReceived = dialogFinalize.TotalReceived;
                            finalPayments = dialogFinalize.OriginalPayments;
                            advances.AddRange (dialogFinalize.UsedAdvances);

                            ICashReceiptPrinterController cashReceiptPrinter = BusinessDomain.DeviceManager.CashReceiptPrinterDriver;
                            object hasFiscalMemory;
                            bool canReprint = cashReceiptPrinter != null &&
                                (cashReceiptPrinter.GetAttributes ().TryGetValue (DriverBase.HAS_FISCAL_MEMORY, out hasFiscalMemory) && (bool) hasFiscalMemory == false);

                            if ((!editMode || canReprint) && (dialogFinalize.PrintFiscal || !BusinessDomain.WorkflowManager.AllowSaleWithoutReceipt))
                                action |= FinalizeAction.PrintCashReceipt | FinalizeAction.CollectSaleData;
                            printPayment = dialogFinalize.PrintDocument;
                        }
                        res.AskForPayment = false;
                    }

                    if ((action & FinalizeAction.PrintCashReceipt) == FinalizeAction.PrintCashReceipt &&
                        operation.IsVATExempt) {
                        if (VATGroup.GetExemptGroup () == null) {
                            MessageError.ShowDialog (string.Format (Translator.GetString ("To print a receipt for an operation without VAT you need to define a 0% VAT group in {0} and in the receipt printer if necessary!"), DataHelper.ProductName));
                            ClearDetailsFromPriceRules (true);
                            return false;
                        }
                    }

                    if (!PriceRule.ApplyOnPaymentSet (operation))
                        return false;

                    FinalizeOperationOptions options = new FinalizeOperationOptions
                        {
                            Sale = operation.GetSale (),
                            OnSaleCommitted = OnSaleCommited,
                            Action = res.RetryWithoutPrint ? (action & ~FinalizeAction.PrintAny) : action,
                            KitchenSale = kitchenSale.GetSale (),
                            KitchenTitle = Translator.GetString ("KITCHEN ORDER"),
                            FinalPayments = finalPayments
                        };
                    options.EditedAdvancePayments.AddRange (advances);

                    var newPayments = GetAllNewPayments (printPayment);
                    PrepareOpertionForSaving (options.Sale);
                    BusinessDomain.DeviceManager.FinalizeOperation (options);
                    PrintAllNewPayments (newPayments);
                } catch (InsufficientItemAvailabilityException ex) {
                    MessageError.ShowDialog (
                        string.Format (Translator.GetString ("The sale cannot be saved due to insufficient quantity of item \"{0}\"."), ex.ItemName),
                        ErrorSeverity.Warning, ex);

                    ClearDetailsFromPriceRules ();
                    EditGridField (ex.ItemName);
                    return false;
                } catch (HardwareErrorException ex) {
                    res = FormHelper.HandleHardwareError (ex);
                    if (!res.Retry) {
                        ClearDetailsFromPriceRules (true);
                        return false;
                    }
                } catch (Exception ex) {
                    MessageError.ShowDialog (
                        Translator.GetString ("An error occurred while saving sale document!"),
                        ErrorSeverity.Error, ex);

                    ClearDetailsFromPriceRules (true);
                    return false;
                }
            } while (res.Retry);

            do {
                res.Retry = false;
                try {
                    BusinessDomain.DeviceManager.DisplayPayment (total, totalReceived);
                } catch (HardwareErrorException ex) {
                    res = FormHelper.HandleHardwareError (ex);
                    if (!res.Retry)
                        break;
                }
            } while (res.Retry);

            ApplyFinalPriceRules ();
            return true;
        }

        protected override void CommitOperation ()
        {
            operation.Commit ();
        }

        protected virtual void PreparePaymentDialog (EditNewPayment dialogFinalize)
        {
            dialogFinalize.ChangeIsReturned = BusinessDomain.AppConfiguration.SaleChangeIsReturned;
        }

        protected bool TryAddServiceCharge (bool priceWithVAT = false)
        {
            return editMode || PriceRule.TryAddServiceCharge<TOper, TOperDetail> (operation,
                (rules, sale) => ConfirmPriceRules<TOperDetail>.GetRulesToApply (rules, sale, priceWithVAT));
        }

        protected override Message GetAskSaveEditDialog ()
        {
            return new MessageOkCancel (
                Translator.GetString ("Edit Sale"), string.Empty,
                Translator.GetString ("Exit without saving the changes?"), "Icons.Question32.png");
        }

        protected override Message GetAskSaveNewDialog ()
        {
            return new MessageOkCancel (
                Translator.GetString ("Saving Sale"), string.Empty,
                Translator.GetString ("Exit without saving the sale?"), "Icons.Question32.png");
        }

        protected override string GetNoValidRowsWarning ()
        {
            return Translator.GetString ("There are no valid rows in the sale document!");
        }

        protected override string GetInsufficientAvailabilityWarning ()
        {
            return Translator.GetString ("The sale cannot be saved due to insufficient quantity of item \"{0}\".");
        }

        private void OnSaleCommited ()
        {
            if (!editMode)
                PriceRule.ApplyOnOperationSaved (operation, PriceRules);
        }

        protected override void ApplyFinalPriceRules ()
        {
            if (!editMode)
                PriceRule.ApplyAfterOperationSaved (operation, PriceRules);
        }

        #endregion
    }
}
