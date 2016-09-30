//
// WbpProduction.cs
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
using System.Linq;
using Gtk;

using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Presentation.Dialogs;
using Warehouse.Presentation.Widgets;
using Image = Gtk.Image;
using Item = Warehouse.Business.Entities.Item;
using Key = Gdk.Key;

namespace Warehouse.Presentation
{
    public class WbpProduction : WbpOperationBase<ComplexProduction, ComplexProductionDetail>
    {
        private ComplexRecipe selectedRecipe;
        private PangoStyle highlight;
        private PictureButton btnLoadRecipe;
        private PictureButton btnSaveRecipe;

        protected override string PageDescription
        {
            get { return "Production"; }
        }

        public override string HelpFile
        {
            get { return "Production.html"; }
        }

        public override string PageTitle
        {
            get
            {
                if (pageTitle == null) {
                    if (operation.State == OperationState.Draft)
                        pageTitle = Translator.GetString ("Edit production draft");
                    else if (editMode)
                        pageTitle = string.Format ("{0} {1}",
                            Translator.GetString ("Edit production No."),
                            operation.FormattedOperationNumber);
                    else 
                        pageTitle = Translator.GetString ("Production");

                }

                return pageTitle;
            }
        }

        public WbpProduction ()
            : this (null)
        {
            cursorAtColumn = colItem.Index;
        }

        public WbpProduction (long? prodId)
        {
            InitializeForm (prodId);
        }

        #region Initialization steps

        private void InitializeForm (long? prodId)
        {
            Image icon = FormHelper.LoadImage ("Icons.Production32.png");
            evbIcon.Add (icon);
            icon.Show ();

            LocationVisible = true;
            UserVisible = true;
            DateVisible = true;
            GridDescriptionVisible = true;
            SecondGridDescriptionVisible = true;
            SecondGridVisible = true;
            btnImport.Visible = BusinessDomain.DataImporters.Count > 0;

            btnLoadRecipe = new PictureButton ();
            btnLoadRecipe.Clicked += btnLoadRecipe_Clicked;
            btnLoadRecipe.SetImage (FormHelper.LoadImage ("Icons.RecipeLoad24.png"));
            btnLoadRecipe.SetText (Translator.GetString ("Load Recipe"));
            vbxAdditionalButtons.PackStart (btnLoadRecipe, false, true, 0);

            btnSaveRecipe = new PictureButton ();
            btnSaveRecipe.Clicked += btnSaveRecipe_Clicked;
            btnSaveRecipe.SetImage (FormHelper.LoadImage ("Icons.RecipeSave24.png"));
            btnSaveRecipe.SetText (Translator.GetString ("Save Recipe"));
            vbxAdditionalButtons.PackStart (btnSaveRecipe, false, true, 0);
            algAdditionalButtons.ShowAll ();

            highlight = new PangoStyle { Color = Colors.Blue };

            ReInitializeForm (prodId);
        }

        private void ReInitializeForm (long? id)
        {
            if (id != null) {
                operation = ComplexProduction.GetById (id.Value);

                txtLocation.Text = operation.Location;
                txtUser.Text = operation.UserName;
                SetDate (operation.Date);
                SetNote (operation.Details);

                SetOperationTotalAndEditMode ();
            } else {
                operation = new ComplexProduction ();

                operation.AddNewDetail ();
                operation.AddNewAdditionalDetail ();
                operation.LoggedUserId = BusinessDomain.LoggedUser.Id;

                SetLocationName ();
                SetUser (BusinessDomain.LoggedUser);
                SetDate (BusinessDomain.Today);
                SetNote (operation.Details);

                SetOperationTotalAndNewMode ();
            }

            InitializeGrid ();
            InitializeSecondGrid ();
            BindGrid ();
        }

        public override void LoadOperation (Operation oper)
        {
            txtLocation.Text = oper.Location;
            LocationEvaluate ();

            txtUser.Text = oper.UserName;
            UserEvaluate ();

            SetDate (oper.Date);
            SetNote (operation.Details);
            LoadOperationDetails (oper);
            LoadSecondaryOperationDetails (oper);
            SetOperationTotalAndNewMode ();
            UpdateOperationTotal ();

            InitializeStatistics ();
            UpdateStatistics ();
            
            LocationFocus ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            GridDescription = Translator.GetString ("Materials");
            SecondGridDescription = Translator.GetString ("Products");
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();
            grid.CellStyleProvider = grid_CellStyleProvider;
        }

        protected override void InitializeItemColumn (ColumnController cc)
        {
            base.InitializeItemColumn (cc);
            colItem.ListCell.CellEditBegin += CellEditBeginOnMatRecipe;
        }

        protected override void InitializeDiscountColumn (ColumnController cc)
        {
        }

        protected override void InitializeDiscountValueColumn (ColumnController cc)
        {
        }

        private void InitializeSecondGrid ()
        {
            if (secondGrid == null) {
                secondGrid = new ListView { Name = "secondGrid" };

                ScrolledWindow sWindow = new ScrolledWindow { HscrollbarPolicy = PolicyType.Automatic, VscrollbarPolicy = PolicyType.Automatic };
                sWindow.Add (secondGrid);

                algSecondGrid.Add (sWindow);
                sWindow.Show ();
                secondGrid.Show ();
            }

            ColumnController cc = new ColumnController ();

            if (BusinessDomain.AppConfiguration.EnableLineNumber) {
                CellTextEnumerator cte = new CellTextEnumerator ();
                colSecondLineNumber = new Column ("№", cte, 0.01) { MinWidth = 25 };
                cc.Add (colSecondLineNumber);
            }

            CellText ct;
            if (BusinessDomain.AppConfiguration.EnableItemCode) {
                ct = new CellText ("ItemCode");
                colSecondItemCode = new Column (Translator.GetString ("Code"), ct, 0.1) { MinWidth = 70 };
                cc.Add (colSecondItemCode);
            }

            ct = new CellText ("ItemName") { IsEditable = true };
            ct.CellEditBegin += CellEditBeginOnProdRecipe;
            colSecondItem = new Column (Translator.GetString ("Item"), ct, 1);
            colSecondItem.ButtonPressEvent += SecondItemColumn_ButtonPressEvent;
            colSecondItem.KeyPressEvent += SecondItemColumn_KeyPress;
            cc.Add (colSecondItem);

            colSecondMeasure = new Column (Translator.GetString ("Measure"), "MUnitName", 0.1) { MinWidth = 70 };
            cc.Add (colSecondMeasure);

            CellTextQuantity ctq = new CellTextQuantity ("Quantity") { IsEditable = true };
            colSecondQuantity = new Column (Translator.GetString ("Qtty"), ctq, 0.1) { MinWidth = 70 };
            colSecondQuantity.ButtonPressEvent += SecondQtyColumn_ButtonPressEvent;
            colSecondQuantity.KeyPressEvent += SecondQtyColumn_KeyPress;
            cc.Add (colSecondQuantity);

            CellTextCurrency ctc = new CellTextCurrency ("PriceIn", PriceType.Purchase);
            colSecondPurchaseValue = new Column (Translator.GetString ("Purchase price"), ctc, 0.1) { MinWidth = 70 };
            cc.Add (colSecondPurchaseValue);

            if (BusinessDomain.AppConfiguration.AllowItemLotName) {
                ct = new CellText ("Lot") { IsEditable = true };
                colSecondLot = new Column (Translator.GetString ("Lot"), ct, 0.1) { MinWidth = 70 };
                colSecondLot.ButtonPressEvent += SecondLotColumn_ButtonPressEvent;
                colSecondLot.KeyPressEvent += SecondLotColumn_KeyPress;
                cc.Add (colSecondLot);
            }

            if (BusinessDomain.AppConfiguration.AllowItemSerialNumber) {
                ct = new CellText ("SerialNumber") { IsEditable = true };
                colSecondSerialNo = new Column (Translator.GetString ("Serial number"), ct, 0.1) { MinWidth = 80 };
                colSecondSerialNo.ButtonPressEvent += SecondSerialNoColumn_ButtonPressEvent;
                colSecondSerialNo.KeyPressEvent += SecondSerialNoColumn_KeyPress;
                cc.Add (colSecondSerialNo);
            }

            CellTextDate ctd;
            if (BusinessDomain.AppConfiguration.AllowItemExpirationDate) {
                ctd = new CellTextDate ("ExpirationDate") { IsEditable = true };
                colSecondExpirationDate = new Column (Translator.GetString ("Expiration date"), ctd, 0.1) { MinWidth = 70 };
                colSecondExpirationDate.ButtonPressEvent += SecondExpirationDateColumn_ButtonPressEvent;
                colSecondExpirationDate.KeyPressEvent += SecondExpirationDateColumn_KeyPress;
                cc.Add (colSecondExpirationDate);
            }

            if (BusinessDomain.AppConfiguration.AllowItemManufacturedDate) {
                ctd = new CellTextDate ("ProductionDate") { IsEditable = true };
                colSecondProductionDate = new Column (Translator.GetString ("Production date"), ctd, 0.1) { MinWidth = 70 };
                colSecondProductionDate.ButtonPressEvent += SecondProductionDateColumn_ButtonPressEvent;
                colSecondProductionDate.KeyPressEvent += SecondProductionDateColumn_KeyPress;
                cc.Add (colSecondProductionDate);
            }

            if (BusinessDomain.AppConfiguration.AllowItemLocation) {
                ct = new CellText ("LotLocation") { IsEditable = true };
                colSecondLotLocation = new Column (Translator.GetString ("Lot location"), ct, 0.1) { MinWidth = 70 };
                colSecondLotLocation.ButtonPressEvent += SecondLotLocationColumn_ButtonPressEvent;
                colSecondLotLocation.KeyPressEvent += SecondLotLocationColumn_KeyPress;
                cc.Add (colSecondLotLocation);
            }

            if (BusinessDomain.AppConfiguration.EnableItemVatRate) {
                CellTextDouble ctf = new CellTextDouble ("VatRate") { IsEditable = true, FixedFaction = BusinessDomain.AppConfiguration.PercentPrecision };
                colSecondVatRate = new Column (BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                    Translator.GetString ("Tax %") :
                    Translator.GetString ("VAT %"), ctf, 0.1) { MinWidth = 70 };
                colSecondVatRate.ButtonPressEvent += SecondVatRateColumn_ButtonPressEvent;
                colSecondVatRate.KeyPressEvent += SecondVatRateColumn_KeyPress;
                cc.Add (colSecondVatRate);
            }

            ctc = new CellTextCurrency ("Total", PriceType.Purchase);
            colSecondTotal = new Column (Translator.GetString ("Amount"), ctc, 0.1) { MinWidth = 70 };
            cc.Add (colSecondTotal);

            secondGrid.ColumnController = cc;
            secondGrid.AllowSelect = false;
            secondGrid.CellsFucusable = true;
            secondGrid.ManualFucusChange = true;
            secondGrid.RulesHint = true;
            secondGrid.CellKeyPressEvent -= secondGrid_CellKeyPressEvent;
            secondGrid.CellKeyPressEvent += secondGrid_CellKeyPressEvent;
            secondGrid.CellStyleProvider = secondGrid_CellStyleProvider;
        }

        protected override void BindGrid ()
        {
            BindGrid (grid, operation.DetailsMat);
            BindGrid (secondGrid, operation.DetailsProd);

            InitializeStatistics ();
            UpdateStatistics ();

            FocusFirstEntry ();
        }

        private void grid_CellStyleProvider (object sender, CellStyleQueryEventArgs args)
        {
            ComplexRecipe currectRecipe = operation.DetailsMat [args.Cell.Row].SourceRecipe;

            if ((selectedRecipe == null && currectRecipe != null) ||
                (selectedRecipe != null && ReferenceEquals (currectRecipe, selectedRecipe)))
                args.Style = highlight;
        }

        private void secondGrid_CellStyleProvider (object sender, CellStyleQueryEventArgs args)
        {
            ComplexRecipe currectRecipe = operation.DetailsProd [args.Cell.Row].SourceRecipe;

            if ((selectedRecipe == null && currectRecipe != null) ||
                (selectedRecipe != null && ReferenceEquals (currectRecipe, selectedRecipe)))
                args.Style = highlight;
        }

        private void secondGrid_CellKeyPressEvent (object sender, CellKeyPressEventArgs args)
        {
            OnCellKeyPress (sender, args, false, operation.DetailsProd);
        }

        private void CellEditBeginOnMatRecipe (object sender, CellEditBeginEventArgs args)
        {
            args.Cancel = operation.DetailsMat [args.Cell.Row].SourceRecipe != null;
        }

        private void CellEditBeginOnProdRecipe (object sender, CellEditBeginEventArgs args)
        {
            args.Cancel = operation.DetailsProd [args.Cell.Row].SourceRecipe != null;
        }

        protected override void EditFormField (FormFields fld)
        {
            btnLoadRecipe.Sensitive = false;
            btnSaveRecipe.Sensitive = false;
            base.EditFormField (fld);
        }

        protected override bool EditGridField (int row, int col)
        {
            btnLoadRecipe.Sensitive = true;
            btnSaveRecipe.Sensitive = true;
            if (!base.EditGridField (row, col))
                return false;

            selectedRecipe = operation.DetailsMat [row].SourceRecipe;
            grid.QueueDraw ();
            secondGrid.QueueDraw ();
            return true;
        }

        protected override bool EditGridCell (int row, int column)
        {
            btnLoadRecipe.Sensitive = true;
            btnSaveRecipe.Sensitive = true;
            bool ret = base.EditGridCell (row, column);
            if (!ret) {
                btnLoadRecipe.Sensitive = false;
                btnSaveRecipe.Sensitive = false;
            }

            return ret;
        }

        protected override bool EditSecondGridField (int row, int col)
        {
            btnLoadRecipe.Sensitive = true;
            btnSaveRecipe.Sensitive = true;
            if (!base.EditSecondGridField (row, col))
                return false;

            selectedRecipe = operation.DetailsProd [row].SourceRecipe;
            grid.QueueDraw ();
            secondGrid.QueueDraw ();
            return true;
        }

        protected override bool EditSecondGridCell (int row, int column)
        {
            btnLoadRecipe.Sensitive = true;
            btnSaveRecipe.Sensitive = true;
            bool ret = base.EditSecondGridCell (row, column);
            if (!ret) {
                btnLoadRecipe.Sensitive = false;
                btnSaveRecipe.Sensitive = false;
            }

            return ret;
        }

        public override void OnPageAdding ()
        {
            MenuItemWrapper restNode = PresentationDomain.MainForm.MainMenu.FindMenuItem ("mnuOperProduction");
            if (restNode.GetRestriction (BusinessDomain.LoggedUser.Id) != UserRestrictionState.Allowed)
                throw new WorkPageAddException (Translator.GetString ("Opening the Production screen is not allowed!"));

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
                        Translator.GetString ("Edit Production"), string.Empty,
                        Translator.GetString ("Do you want to save the changes?"), "Icons.Question32.png");
                } else {
                    dialogSave = new MessageOkCancel (
                        Translator.GetString ("Saving production"), string.Empty,
                        Translator.GetString ("Do you want to save the operation?"), "Icons.Question32.png");
                }

                if (dialogSave.Run () != ResponseType.Ok) {
                    operation.AddNewDetail ();
                    EditGridField (operation.DetailsMat.Count - 1, colItem.Index);
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
                MessageError.ShowDialog (
                    string.Format (Translator.GetString ("The production cannot be saved due to insufficient quantity of item \"{0}\"."), ex.ItemName),
                    ErrorSeverity.Warning, ex);

                for (int i = 0; i < operation.Details.Count; i++) {
                    if (operation.Details [i].ItemName != ex.ItemName)
                        continue;

                    EditGridField (i, operation.DetailsMat [i].SourceRecipe != null ? colQuantity.Index : colItem.Index);
                    break;
                }
                return;
            } catch (Exception ex) {
                MessageError.ShowDialog (
                    Translator.GetString ("An error occurred while saving the production operation!"),
                    ErrorSeverity.Error, ex);
                return;
            }

            if (printReceipt && WaitForPendingOperationCompletion ()) {
                try {
                    ProductionProtocol protocol = new ProductionProtocol (operation);
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
                Translator.GetString ("Edit Production"), string.Empty,
                Translator.GetString ("Exit without saving the changes?"), "Icons.Question32.png");
        }

        protected override Message GetAskSaveNewDialog ()
        {
            return new MessageOkCancel (
                Translator.GetString ("Saving Production"), string.Empty,
                Translator.GetString ("Exit without saving the production?"), "Icons.Question32.png");
        }

        protected override bool OperationDetailsValidate (bool showWarning)
        {
            if (operation.DetailsMat.Count == 0)
                return false;

            CurrentColumnEvaluate ();
            bool hasValidMaterialQuantities = false;

            for (int i = operation.DetailsMat.Count - 1; i >= 0; i--) {
                try {
                    // Validate Item
                    string itemName = operation.DetailsMat [i].ItemName;
                    // If the gooods field is empty then this line has to be skipped
                    if (itemName.Length == 0) {
                        // If this is not the first line then delete it
                        if (i > 0) {
                            operation.DetailsMat.RemoveAt (i);
                            continue;
                        }

                        OperationDetailValidationWarning (Translator.GetString ("There are no valid materials in the production document!"), showWarning);
                        EditGridField (0, colItem.Index);
                        return false;
                    }

                    Item item = Item.GetById (operation.DetailsMat [i].ItemId);
                    if (item == null || item.Name != itemName) {
                        OperationDetailValidationWarning (string.Format (Translator.GetString ("Invalid material at row {0}!"), i + 1), showWarning);
                        EditGridField (i, operation.DetailsMat [i].SourceRecipe != null ? colQuantity.Index : colItem.Index);
                        return false;
                    }

                    // Validate quantity
                    double qty = operation.DetailsMat [i].Quantity;
                    if ((!editMode && qty <= 0) || (editMode && qty < 0)) {
                        OperationDetailValidationWarning (string.Format (Translator.GetString ("Invalid quantity of material \"{0}\"!"), item.Name), showWarning);
                        EditGridField (i, colQuantity.Index);
                        return false;
                    }
                    if (operation.DetailsMat [i].SourceRecipe == null &&
                        !BusinessDomain.AppConfiguration.AutoProduction &&
                        BusinessDomain.AppConfiguration.ItemsManagementUseLots &&
                        operation.DetailsMat [i].LotId <= 0) {
                        OperationDetailValidationWarning (string.Format (Translator.GetString ("The sale cannot be saved due to insufficient quantity of item \"{0}\"."), item.Name), showWarning);
                        EditGridField (i, colQuantity.Index);
                        return false;
                    }

                    if (qty > 0)
                        hasValidMaterialQuantities = true;
                } catch {
                    OperationDetailValidationWarning (string.Format (Translator.GetString ("Error at row {0}!"), i + 1), showWarning);
                    EditGridField (i, operation.DetailsMat [i].SourceRecipe != null ? colQuantity.Index : colItem.Index);
                    return false;
                }
            }

            if (!hasValidMaterialQuantities) {
                if (editMode) {
                    ResponseType ret = OperationDetailValidationQuestion (Translator.GetString ("There are no materials with quantities greater than 0! The production will be deleted. Are you sure?"), showWarning);
                    if (ret == ResponseType.No)
                        return false;
                } else {
                    OperationDetailValidationWarning (Translator.GetString ("The production must contain at least one material with quantity greater than 0."), showWarning);
                    return false;
                }
            }

            bool hasValidProductQuantities = false;
            for (int i = operation.DetailsProd.Count - 1; i >= 0; i--) {
                try {
                    // Validate Item
                    string itemName = operation.DetailsProd [i].ItemName;
                    // If the gooods field is empty then this line has to be skipped
                    if (itemName.Length == 0) {
                        // If this is not the first line then delete it
                        if (i > 0) {
                            operation.DetailsProd.RemoveAt (i);
                            continue;
                        }

                        OperationDetailValidationWarning (Translator.GetString ("There are no valid products in the production document!"), showWarning);
                        EditSecondGridField (0, colItem.Index);
                        return false;
                    }

                    Item item = Item.GetById (operation.DetailsProd [i].ItemId);
                    if (item == null || item.Name != itemName) {
                        OperationDetailValidationWarning (string.Format (Translator.GetString ("Invalid product at row {0}!"), i + 1), showWarning);
                        EditSecondGridField (i, operation.DetailsMat [i].SourceRecipe != null ? colSecondQuantity.Index : colSecondItem.Index);
                        return false;
                    }

                    // Validate quantity
                    double qty = operation.DetailsProd [i].Quantity;
                    if ((!editMode && qty <= 0) || (editMode && qty < 0)) {
                        OperationDetailValidationWarning (string.Format (Translator.GetString ("Invalid quantity of product \"{0}\"!"), item.Name), showWarning);
                        EditSecondGridField (i, colQuantity.Index);
                        return false;
                    }

                    if (qty > 0)
                        hasValidProductQuantities = true;
                } catch {
                    OperationDetailValidationWarning (string.Format (Translator.GetString ("Error at row {0}!"), i + 1), showWarning);
                    EditSecondGridField (i, operation.DetailsMat [i].SourceRecipe != null ? colSecondQuantity.Index : colSecondItem.Index);
                    return false;
                }
            }

            if (!hasValidProductQuantities) {
                if (editMode) {
                    ResponseType ret = OperationDetailValidationQuestion (Translator.GetString ("There are no products with quantities greater than 0! The recipe will be deleted. Are you sure?"), showWarning);
                    if (ret == ResponseType.No)
                        return false;
                } else {
                    OperationDetailValidationWarning (Translator.GetString ("The recipe must contain at least one product with quantity greater than 0."), showWarning);
                    return false;
                }
            }

            for (int i = 0; i < operation.DetailsMat.Count; i++) {
                ComplexProductionDetail detMat = operation.DetailsMat [i];
                if (operation.DetailsProd.All (detProd => detMat.ItemId != detProd.ItemId))
                    continue;

                OperationDetailValidationWarning (string.Format (Translator.GetString ("The item \"{0}\" is used as a material and as a product!"), detMat.ItemName), showWarning);
                EditGridField (i, colItem.Index);
                return false;
            }

            if (!hasValidMaterialQuantities || !hasValidProductQuantities)
                operation.ClearDetails ();

            CancelEditing ();
            return true;
        }

        protected override string GetNoValidRowsWarning ()
        {
            throw new NotImplementedException ();
        }

        protected override string GetInsufficientAvailabilityWarning ()
        {
            return Translator.GetString ("The production cannot be saved due to insufficient quantity of item \"{0}\".");
        }

        #endregion

        protected virtual void btnLoadRecipe_Clicked (object sender, EventArgs e)
        {
            ComplexRecipe [] recipes;

            using (ChooseEditComplexRecipe dialog = new ChooseEditComplexRecipe (true, string.Empty)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                recipes = dialog.SelectedItems;
            }

            CurrentColumnEvaluate ();

            foreach (ComplexRecipe recipe in recipes) {
                operation.AddRecipe (recipe, GetOperationPriceGroup ());
            }

            EditSecondGridField (operation.DetailsProd.Count - 1, colQuantity.Index);
        }

        protected virtual void btnSaveRecipe_Clicked (object sender, EventArgs e)
        {
            if (!OperationValidate ())
                return;

            if (!OperationDetailsValidate (true))
                return;

            ComplexRecipe recipe;
            using (EditNewComplexRecipe dialog = new EditNewComplexRecipe (new ComplexRecipe (operation))) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                recipe = dialog.GetRecipe ();
            }

            recipe.CommitChanges ();

            operation.DetailsMat.Clear ();
            operation.DetailsProd.Clear ();
            operation.AddRecipe (recipe, GetOperationPriceGroup ());
            EditSecondGridField (operation.DetailsProd.Count - 1, colQuantity.Index);
        }

        protected override void btnClear_Clicked (object sender, EventArgs e)
        {
            if (!AskForOperationClear ())
                return;

            if (editMode) {
                CellPosition gridMatEditPos = grid.EditedCell;
                if (gridMatEditPos.IsValid && gridMatEditPos.Column == colQuantity.Index)
                    grid.CancelCellEdit ();

                CellPosition gridProdEditPos = secondGrid.EditedCell;
                if (gridProdEditPos.IsValid && gridProdEditPos.Column == colSecondQuantity.Index)
                    secondGrid.CancelCellEdit ();

                operation.ClearDetails ();

                if (gridMatEditPos.IsValid)
                    EditGridField (gridMatEditPos.Row, gridMatEditPos.Column);

                if (gridProdEditPos.IsValid)
                    EditSecondGridField (gridProdEditPos.Row, gridProdEditPos.Column);
            } else {
                operation.ClearDetails ();
                operation.AddNewDetail ();
                operation.AddNewAdditionalDetail ();

                EditGridField (0, colItem.Index);
                OperationTotalHide ();
            }
        }

        protected override void GridColumnEditBelow (int column)
        {
            if (!grid.EditedCell.IsValid)
                return;

            if (grid.EditedCell.Row + 1 < grid.Model.Count) {
                EditGridField (grid.EditedCell.Row + 1, column);
            } else {
                EditSecondGridField (0, column);
            }
        }

        protected override void SecondGridColumnEditOver (int column)
        {
            if (!secondGrid.EditedCell.IsValid)
                return;

            if (secondGrid.EditedCell.Row > 0) {
                EditSecondGridField (secondGrid.EditedCell.Row - 1, column);
            } else if (grid.ColumnController [column].ListCell.IsEditable) {
                EditGridField (grid.Model.Count - 1, column);
            }
        }

        protected override void DeleteGridRow (bool keepRowPos, bool logChange = true)
        {
            if (!grid.EditedCell.IsValid)
                return;

            int col = grid.EditedCell.Column;
            int row = grid.EditedCell.Row;
            ComplexRecipe recipe = operation.DetailsMat [row].SourceRecipe;

            if (recipe != null) {
                using (MessageYesNoRemember message = new MessageYesNoRemember (
                    Translator.GetString ("Remove recipe"), null,
                    Translator.GetString ("This material is part of a recipe. Do you want to delete the entire recipe?"), "Icons.Question32.png")) {
                    message.Buttons = MessageButtons.YesNo;
                    if (message.Run () != ResponseType.Yes)
                        return;
                }

                for (int i = operation.DetailsProd.Count - 1; i >= 0; i--) {
                    if (!ReferenceEquals (recipe, operation.DetailsProd [i].SourceRecipe))
                        continue;
                    operation.DetailsProd.RemoveAt (i);
                }

                for (int i = operation.DetailsMat.Count - 1; i >= 0; i--) {
                    if (!ReferenceEquals (recipe, operation.DetailsMat [i].SourceRecipe))
                        continue;
                    DeleteGridRow (keepRowPos, col, i);
                }
            } else {
                DeleteGridRow (keepRowPos, col, row);
            }
        }

        private void DeleteGridRow (bool keepRowPos, int col, int row)
        {
            int newRow;
            if (row == operation.DetailsMat.Count - 1) {
                // If we are deleting the last row move one row up
                operation.DetailsMat.RemoveAt (row);
                newRow = row - 1;
            } else {
                // If we are deleting row from somewhere in between stay on the same line
                operation.DetailsMat.RemoveAt (row);
                newRow = row;
            }

            if (operation.DetailsMat.Count > 0) {
                if (keepRowPos)
                    EditGridField (newRow, col);
                else
                    ItemColumnEditPrev (newRow, Key.Left);
            } else {
                operation.AddNewDetail ();

                EditGridField (0, colItem.Index);
            }
        }

        protected override void DeleteSecondGridRow (bool keepRowPos)
        {
            if (!secondGrid.EditedCell.IsValid)
                return;

            int col = secondGrid.EditedCell.Column;
            int row = secondGrid.EditedCell.Row;
            ComplexRecipe recipe = operation.DetailsProd [row].SourceRecipe;

            if (recipe != null) {
                using (MessageYesNoRemember message = new MessageYesNoRemember (
                    Translator.GetString ("Remove recipe"), null, 
                    Translator.GetString ("This product is part of a recipe. Do you want to delete the entire recipe?"), "Icons.Question32.png")) {
                    message.Buttons = MessageButtons.YesNo;
                    if (message.Run () != ResponseType.Yes)
                        return;
                }

                for (int i = operation.DetailsMat.Count - 1; i >= 0; i--) {
                    if (!ReferenceEquals (recipe, operation.DetailsMat [i].SourceRecipe))
                        continue;
                    operation.DetailsMat.RemoveAt (i);
                }

                for (int i = operation.DetailsProd.Count - 1; i >= 0; i--) {
                    if (!ReferenceEquals (recipe, operation.DetailsProd [i].SourceRecipe))
                        continue;
                    DeleteSecondGridRow (keepRowPos, col, i);
                }
            } else {
                DeleteSecondGridRow (keepRowPos, col, row);
            }
        }

        private void DeleteSecondGridRow (bool keepRowPos, int col, int row)
        {
            int newRow;
            if (row == operation.DetailsProd.Count - 1) {
                // If we are deleting the last row move one row up
                operation.DetailsProd.RemoveAt (row);
                newRow = row - 1;
            } else {
                // If we are deleting row from somewhere in between stay on the same line
                operation.DetailsProd.RemoveAt (row);
                newRow = row;
            }

            if (operation.DetailsProd.Count > 0) {
                if (keepRowPos)
                    EditSecondGridField (newRow, col);
                else
                    SecondItemColumnEditPrev (newRow, Key.Left);
            } else {
                operation.AddNewAdditionalDetail ();

                EditSecondGridField (0, colItem.Index);
            }
        }

        #region Item column handling

        protected override bool ItemColumnEvaluate (int row, Item item, bool updatePrice)
        {
            return operation.DetailsMat [row].ItemEvaluate (item, GetOperationPriceGroup (), updatePrice, operation);
        }

        protected override void ItemColumnEditNext (int row, Key keyCode)
        {
            if (keyCode == Key.Tab)
                EditSecondGridField (0, colItem.Index);
            else
                base.ItemColumnEditNext (row, keyCode);
        }

        #endregion

        #region Quantity column handling

        protected override bool QtyColumnEvaluate (ref int row, double qtyValue)
        {
            if (!CheckQuantityLimitations (qtyValue, row, grid))
                return false;
            ComplexProductionDetail detail = operation.DetailsMat [row];
            detail.QuantityEvaluate (qtyValue, operation);
            if (detail.SourceRecipe == null)
                LotsEvaluate (operation.DetailsMat, detail);
            return true;
        }

        protected override void QtyColumnEditNext (int row, Key keyCode)
        {
            if (keyCode == Key.Tab)
                EditSecondGridField (0, colItem.Index);
            else
                base.QtyColumnEditNext (row, keyCode);
        }

        #endregion

        #region Sale Price column handling

        protected override bool SalePriceColumnEvaluate (int row, string price)
        {
            return CheckSalePrice (row, price) && base.SalePriceColumnEvaluate (row, price);
        }

        #endregion

        #region Product Item column handling

        protected override bool SecondItemColumnEvaluate (int row, Item item, bool updatePrice)
        {
            return operation.DetailsProd [row].ItemEvaluate (item, GetOperationPriceGroup (), updatePrice, operation);
        }

        protected override void SecondItemColumnEditPrev (int row, Key keyCode)
        {
            if (keyCode == Key.ISO_Left_Tab)
                GridNavigator.EditPrev (grid.Model.Count - 1, keyCode, colLotLocation, LotLocationColumnEditPrev);
            else
                base.SecondItemColumnEditPrev (row, keyCode);
        }

        #endregion

        #region Product Quantity column handling

        protected override bool SecondQtyColumnEvaluate (ref int row, double qtyValue)
        {
            if (!CheckQuantityLimitations (qtyValue, row, secondGrid))
                return false;
            operation.DetailsProd [row].QuantityEvaluate (qtyValue, operation);
            return true;
        }

        protected override void SecondQtyColumnEditPrev (int row, Key keyCode)
        {
            if (keyCode == Key.ISO_Left_Tab)
                GridNavigator.EditPrev (grid.Model.Count - 1, keyCode, colLotLocation, LotLocationColumnEditPrev);
            else
                base.SecondQtyColumnEditPrev (row, keyCode);
        }

        #endregion

        #region Product Lot column handling

        protected override void SecondLotColumnEditPrev (int row, Key keyCode)
        {
            if (keyCode == Key.ISO_Left_Tab)
                GridNavigator.EditPrev (grid.Model.Count - 1, keyCode, colLotLocation, LotLocationColumnEditPrev);
            else
                base.SecondLotColumnEditPrev (row, keyCode);
        }

        #endregion

        #region Product Serial number column handling

        protected override void SecondSerialNoColumnEditPrev (int row, Key keyCode)
        {
            if (keyCode == Key.ISO_Left_Tab)
                GridNavigator.EditPrev (grid.Model.Count - 1, keyCode, colLotLocation, LotLocationColumnEditPrev);
            else
                base.SecondSerialNoColumnEditPrev (row, keyCode);
        }

        #endregion

        #region Product Expiration date column handling

        protected override void SecondExpirationDateColumnEditPrev (int row, Key keyCode)
        {
            if (keyCode == Key.ISO_Left_Tab)
                GridNavigator.EditPrev (grid.Model.Count - 1, keyCode, colLotLocation, LotLocationColumnEditPrev);
            else
                base.SecondExpirationDateColumnEditPrev (row, keyCode);
        }

        #endregion

        #region Product Production date column handling

        protected override void SecondProductionDateColumnEditPrev (int row, Key keyCode)
        {
            if (keyCode == Key.ISO_Left_Tab)
                GridNavigator.EditPrev (grid.Model.Count - 1, keyCode, colLotLocation, LotLocationColumnEditPrev);
            else
                base.SecondProductionDateColumnEditPrev (row, keyCode);
        }

        #endregion

        #region Product Lot column handling

        protected override void SecondLotLocationColumnEditPrev (int row, Key keyCode)
        {
            if (keyCode == Key.ISO_Left_Tab)
                GridNavigator.EditPrev (grid.Model.Count - 1, keyCode, colLotLocation, LotLocationColumnEditPrev);
            else
                base.SecondLotLocationColumnEditPrev (row, keyCode);
        }

        #endregion
    }
}
