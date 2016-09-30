//
// WbpWaste.cs
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
using Warehouse.Presentation.Dialogs;
using Item = Warehouse.Business.Entities.Item;

namespace Warehouse.Presentation
{
    public class WbpWaste : WbpOperationBase<Waste, WasteDetail>
    {
        protected override string PageDescription
        {
            get { return "Waste"; }
        }

        public override string HelpFile
        {
            get { return "Waste.html"; }
        }

        public override string PageTitle
        {
            get
            {
                if (pageTitle == null) {
                    if (operation.State == OperationState.Draft)
                        pageTitle = Translator.GetString ("Edit waste draft");
                    else if (editMode)
                        pageTitle = string.Format ("{0} {1}",
                            Translator.GetString ("Edit waste No."),
                            operation.FormattedOperationNumber);
                    else
                        pageTitle = Translator.GetString ("Waste");
                }

                return pageTitle;
            }
        }

        public WbpWaste ()
            : this (null)
        {
            cursorAtColumn = colItem.Index;
        }

        public WbpWaste (long? wasteId)
        {
            InitializeForm (wasteId);
        }

        #region Initialization steps

        private void InitializeForm (long? wasteId)
        {
            Image icon = FormHelper.LoadImage ("Icons.Waste32.png");
            evbIcon.Add (icon);
            icon.Show ();

            LocationVisible = true;
            UserVisible = true;
            DateVisible = true;
            btnImport.Visible = BusinessDomain.DataImporters.Count > 0;

            ReInitializeForm (wasteId);
        }

        private void ReInitializeForm (long? wasteId)
        {
            if (wasteId != null) {
                operation = Waste.GetById (wasteId.Value);

                txtLocation.Text = operation.Location;
                txtUser.Text = operation.UserName;
                SetDate (operation.Date);
                SetNote (operation.Details);

                SetOperationTotalAndEditMode ();
            } else {
                operation = new Waste ();

                operation.AddNewDetail ();
                operation.LoggedUserId = BusinessDomain.LoggedUser.Id;

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
            LocationFocus ();
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

        public override void OnPageAdding ()
        {
            MenuItemWrapper restNode = PresentationDomain.MainForm.MainMenu.FindMenuItem ("mnuOperWaste");
            if (restNode.GetRestriction (BusinessDomain.LoggedUser.Id) != UserRestrictionState.Allowed)
                throw new WorkPageAddException (Translator.GetString ("Opening the Waste screen is not allowed!"));

            OnPageAddingFinish ();
        }

        #endregion

        #region Finalization steps

        public override void OnOperationSave (bool askForConfirmation)
        {
            bool printReceipt = false;

            if (!OperationValidate ())
                return;

            if (!OperationDetailsValidate (true))
                return;

            if (askForConfirmation) {
                MessageOkCancel dialogSave;
                if (editMode) {
                    dialogSave = new MessageOkCancel (
                        Translator.GetString ("Edit waste"), string.Empty,
                        Translator.GetString ("Do you want to save the changes?"), "Icons.Question32.png");
                } else {
                    dialogSave = new MessageOkCancel (
                        Translator.GetString ("Saving waste"), string.Empty,
                        Translator.GetString ("Do you want to save the operation?"), "Icons.Question32.png");
                }

                if (dialogSave.Run () != ResponseType.Ok) {
                    operation.AddNewDetail ();
                    EditGridField (operation.Details.Count - 1, colItem.Index);
                    return;
                }
            }

            if (BusinessDomain.AppConfiguration.IsPrintingAvailable ()) {
                if (BusinessDomain.AppConfiguration.AskBeforeDocumentPrint == AskDialogState.NotSaved) {
                    using (MessageYesNoRemember dialogPrint = new MessageYesNoRemember (
                        Translator.GetString ("Print document"), string.Empty,
                        Translator.GetString ("Do you want to print a document?"), "Icons.Question32.png")) {
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
                PrepareOpertionForSaving ();
                CommitOperation ();
            } catch (InsufficientItemAvailabilityException ex) {
                MessageError.ShowDialog (string.Format (Translator.GetString ("The waste cannot be saved due to insufficient quantity of item \"{0}\"."), ex.ItemName),
                    ErrorSeverity.Warning, ex);
                EditGridField (ex.ItemName);
                return;
            } catch (Exception ex) {
                MessageError.ShowDialog (
                    Translator.GetString ("An error occurred while saving the waste operation!"),
                    ErrorSeverity.Error, ex);
                return;
            }

            if (printReceipt && WaitForPendingOperationCompletion ()) {
                try {
                    WasteProtocol protocol = new WasteProtocol (operation);
                    FormHelper.PrintPreviewObject (protocol);
                } catch (Exception ex) {
                    MessageError.ShowDialog (
                        Translator.GetString ("An error occurred while generating the protocol!"),
                        ErrorSeverity.Error, ex);
                }
            }

            OnOperationSaved ();

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
                Translator.GetString ("Edit Waste"), string.Empty,
                Translator.GetString ("Exit without saving the changes?"), "Icons.Question32.png");
        }

        protected override Message GetAskSaveNewDialog ()
        {
            return new MessageOkCancel (
                Translator.GetString ("Saving Waste"), string.Empty,
                Translator.GetString ("Exit without saving the waste?"), "Icons.Question32.png");
        }

        protected override string GetNoValidRowsWarning ()
        {
            return Translator.GetString ("There are no valid rows in the waste document!");
        }

        protected override string GetInsufficientAvailabilityWarning ()
        {
            return Translator.GetString ("The waste cannot be saved due to insufficient quantity of item \"{0}\".");
        }

        protected override bool ValidatePurchaseValue (Item item, int i, bool showWarning)
        {
            return true;
        }

        protected override bool ValidateSalePrice (Item item, int i, bool showWarning)
        {
            return true;
        }

        #endregion
    }
}
