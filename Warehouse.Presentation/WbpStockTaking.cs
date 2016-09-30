//
// WbpStockTaking.cs
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
using System.Collections.Generic;
using System.Linq;
using Gtk;

using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Presentation.Dialogs;
using Item = Warehouse.Business.Entities.Item;
using Key = Gdk.Key;

namespace Warehouse.Presentation
{
    public class WbpStockTaking : WbpOperationBase<StockTaking, StockTakingDetail>
    {
        private bool useCalculatedAvailability;

        protected override string PageDescription
        {
            get { return "Stock-taking"; }
        }

        public override string HelpFile
        {
            get { return "StockTaking.html"; }
        }

        public override string PageTitle
        {
            get
            {
                if (pageTitle == null) {
                    if (operation.State == OperationState.Draft)
                        pageTitle = Translator.GetString ("Edit stock-taking draft");
                    else if (editMode)
                        pageTitle = string.Format ("{0} {1}",
                            Translator.GetString ("Edit stock-taking No."),
                            operation.FormattedOperationNumber);
                    else
                        pageTitle = Translator.GetString ("Stock-taking");
                }

                return pageTitle;
            }
        }

        public WbpStockTaking ()
            : this (null)
        {
            cursorAtColumn = colItem.Index;
        }

        public WbpStockTaking (long? stockTakingId)
        {
            InitializeForm (stockTakingId);
        }

        #region Initialization steps

        private void InitializeForm (long? stockTakingId)
        {
            Image icon = FormHelper.LoadImage ("Icons.StockTaking32.png");
            evbIcon.Add (icon);
            icon.Show ();

            LocationVisible = true;
            UserVisible = true;
            DateVisible = true;
            btnImport.Visible = BusinessDomain.DataImporters.Count > 0;

            ReInitializeForm (stockTakingId);
        }

        private void ReInitializeForm (long? stockTakingId)
        {
            if (stockTakingId != null) {
                operation = StockTaking.GetById (stockTakingId.Value);

                txtLocation.Text = operation.Location;
                txtUser.Text = operation.UserName;
                SetDate (operation.Date);
                SetNote (operation.Details);

                SetOperationTotalAndEditMode ();
                LocationSensitive = false;
            } else {
                operation = new StockTaking ();

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
            txtLocation.Text = oper.Location;
            LocationEvaluate ();

            txtUser.Text = oper.UserName;
            UserEvaluate ();

            SetDate (oper.Date);
            LoadOperationDetails (oper);
            SetOperationTotalAndNewMode ();
            UserFocus ();
        }

        protected override void InitializeItemColumn (ColumnController cc)
        {
            base.InitializeItemColumn (cc);
            colItem.ListCell.IsEditable = !editMode;
        }

        protected override void InitializeQuantityColumn (ColumnController cc)
        {
            CellTextQuantity ctq = new CellTextQuantity ("ExpectedQuantity");
            Column col = new Column (Translator.GetString ("Available"), ctq, 0.1) { MinWidth = 70 };
            col.ButtonPressEvent += QtyColumn_ButtonPressEvent;
            col.KeyPressEvent += QtyColumn_KeyPress;
            cc.Add (col);

            base.InitializeQuantityColumn (cc);
            colQuantity.ListCell.PropertyName = "EnteredQuantity";
        }

        protected override void InitializePurchasePriceColumn (ColumnController cc)
        {
            base.InitializePurchasePriceColumn (cc);
            colPurchasePrice.ListCell.IsEditable = !editMode;
            if (BusinessDomain.AppConfiguration.ItemsManagementUseLots)
                colPurchasePrice.ListCell.CellEditBegin += CellEditBeginOnEmptyLot;
        }

        protected override void InitializeSalePriceColumn (ColumnController cc)
        {
            base.InitializeSalePriceColumn (cc);
            colSalePrice.ListCell.IsEditable = !editMode;
        }

        protected override void InitializeDiscountColumn (ColumnController cc)
        {
        }

        protected override void InitializeDiscountValueColumn (ColumnController cc)
        {
        }

        protected override void InitializeLotNameColumn (ColumnController cc)
        {
            base.InitializeLotNameColumn (cc);
            colLot.ListCell.IsEditable = true;
            colLot.CellEditBegin += CellEditBeginOnEmptyLot;
        }

        protected override void InitializeSerialNumberColumn (ColumnController cc)
        {
            base.InitializeSerialNumberColumn (cc);
            colSerialNo.ListCell.IsEditable = true;
            colSerialNo.CellEditBegin += CellEditBeginOnEmptyLot;
        }

        protected override void InitializeExpirationDateColumn (ColumnController cc)
        {
            base.InitializeExpirationDateColumn (cc);
            colExpirationDate.ListCell.IsEditable = true;
            colExpirationDate.CellEditBegin += CellEditBeginOnEmptyLot;
        }

        protected override void InitializeManufacturedDateColumn (ColumnController cc)
        {
            base.InitializeManufacturedDateColumn (cc);
            colProductionDate.ListCell.IsEditable = true;
            colProductionDate.CellEditBegin += CellEditBeginOnEmptyLot;
        }

        protected override void InitializeLotLocationColumn (ColumnController cc)
        {
            base.InitializeLotLocationColumn (cc);
            colLotLocation.ListCell.IsEditable = true;
            colLotLocation.CellEditBegin += CellEditBeginOnEmptyLot;
        }

        private void CellEditBeginOnEmptyLot (object sender, CellEditBeginEventArgs args)
        {
            args.Cancel = operation.Details [args.Cell.Row].LotId > 0 && operation.Details [args.Cell.Row].ExpectedQuantity > 0;
        }

        public override void OnPageAdding ()
        {
            MenuItemWrapper restNode = PresentationDomain.MainForm.MainMenu.FindMenuItem ("mnuEditAdminRevision");
            if (restNode.GetRestriction (BusinessDomain.LoggedUser.Id) != UserRestrictionState.Allowed)
                throw new WorkPageAddException (Translator.GetString ("Opening the Stock-taking screen is not allowed!"));

            OnPageAddingFinish ();
        }

        #endregion

        #region Finalization steps

        public override void OnOperationSave (bool askForConfirmation)
        {
            #region Prepare the stock-taking object

            if (!OperationValidate ())
                return;

            if (!OperationDetailsValidate (true))
                return;

            #endregion

            #region Ask to save the stock-taking

            bool printPayment = false;
            if (operation.TotalPlusVAT > 0)
                using (EditNewPayment dialogFinalize = new EditNewPayment (operation)) {
                    if (dialogFinalize.Run () != ResponseType.Ok) {
                        operation.AddNewDetail ();
                        EditGridField (operation.Details.Count - 1, colItem.Index);
                        return;
                    }
                    printPayment = dialogFinalize.PrintDocument;
                }
            else if (askForConfirmation) {
                MessageOkCancel dialogSave;
                if (editMode) {
                    dialogSave = new MessageOkCancel (
                        Translator.GetString ("Edit Stock-taking"), string.Empty,
                        Translator.GetString ("Do you want to save the changes?"), "Icons.Question32.png");
                } else {
                    dialogSave = new MessageOkCancel (
                        Translator.GetString ("Saving Stock-taking"), string.Empty,
                        Translator.GetString ("Do you want to save the operation?"), "Icons.Question32.png");
                }

                if (dialogSave.Run () != ResponseType.Ok) {
                    operation.AddNewDetail ();
                    EditGridField (operation.Details.Count - 1, colItem.Index);
                    return;
                }
            }

            #endregion

            #region Ask to print a document

            bool printProtocol = false;

            if (BusinessDomain.AppConfiguration.IsPrintingAvailable ()) {
                if (BusinessDomain.AppConfiguration.AskBeforeDocumentPrint == AskDialogState.NotSaved) {
                    using (MessageYesNoRemember dialogPrint = new MessageYesNoRemember (
                        Translator.GetString ("Print document"), string.Empty,
                        Translator.GetString ("Do you want to print a document?"), "Icons.Question32.png")) {
                        ResponseType resp = dialogPrint.Run ();
                        if (resp == ResponseType.Yes)
                            printProtocol = true;

                        if (dialogPrint.RememberChoice) {
                            BusinessDomain.AppConfiguration.AskBeforeDocumentPrint = resp == ResponseType.Yes ? AskDialogState.Yes : AskDialogState.No;
                        }
                    }
                } else if (BusinessDomain.AppConfiguration.AskBeforeDocumentPrint == AskDialogState.Yes) {
                    printProtocol = true;
                }
            }

            #endregion

            try {
                List<Payment> newPayments = null;
                if (operation.TotalPlusVAT > 0)
                    newPayments = GetAllNewPayments (printPayment);

                PrepareOpertionForSaving ();
                CommitOperation ();
                if (operation.TotalPlusVAT > 0)
                    PrintAllNewPayments (newPayments);
            } catch (InsufficientItemAvailabilityException ex) {
                MessageError.ShowDialog (
                    string.Format (Translator.GetString ("The stock-taking cannot be saved due to insufficient quantity of item \"{0}\"."), ex.ItemName),
                    ErrorSeverity.Warning, ex);

                EditGridField (ex.ItemName);
                return;
            } catch (Exception ex) {
                MessageError.ShowDialog (
                    Translator.GetString ("An error occurred while saving the stock-taking operation!"),
                    ErrorSeverity.Error, ex);
                return;
            }

            if (printProtocol) {
                if (!WaitForPendingOperationCompletion ())
                    return;

                try {
                    StockTakingProtocol protocol = new StockTakingProtocol (operation);
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
            operation.Commit (GetOperationPriceGroup ());
        }

        protected override Message GetAskSaveEditDialog ()
        {
            return new MessageOkCancel (
                Translator.GetString ("Edit Stock-taking"), string.Empty,
                Translator.GetString ("Exit without saving the changes?"), "Icons.Question32.png");
        }

        protected override Message GetAskSaveNewDialog ()
        {
            return new MessageOkCancel (
                Translator.GetString ("Saving Stock-taking"), string.Empty,
                Translator.GetString ("Exit without saving the stock-taking?"), "Icons.Question32.png");
        }

        protected override string GetNoValidRowsWarning ()
        {
            return Translator.GetString ("There are no valid rows in the stock-taking document!");
        }

        protected override bool ValidateLotQuantity (Item item, BindList<StockTakingDetail> details, int i, bool showWarning)
        {
            return true;
        }

        protected override string GetInsufficientAvailabilityWarning ()
        {
            return Translator.GetString ("The stock-taking cannot be saved due to insufficient quantity of item \"{0}\".");
        }

        #endregion

        #region Entries handling

        protected override void UserFocusNext ()
        {
            EditGridCell (grid.Model.Count - 1, editMode ? colQuantity.Index : colItem.Index);
        }

        protected override void DateFocusNext ()
        {
            EditGridCell (grid.Model.Count - 1, editMode ? colQuantity.Index : colItem.Index);
        }

        #endregion

        protected override void SetLocation (Location newLocation)
        {
            if (newLocation == null)
                return;

            if (Location.HasChildLocationWithOrders (newLocation.Id)) {
                if (Message.ShowDialog (Translator.GetString ("Incomplete Orders"), string.Empty,
                    Translator.GetString ("There are incomplete orders in the " +
                        "selected location. If you continue the shown availability will have the ordered amounts subtracted. " +
                        "Do you want to continue?"), "Icons.Warning32.png",
                    MessageButtons.YesNo) == ResponseType.Yes)
                    base.SetLocation (newLocation);
            } else
                base.SetLocation (newLocation);
        }

        protected override void SetDate (DateTime d)
        {
            useCalculatedAvailability = false;
            if (date != DateTime.MinValue && d < BusinessDomain.Today) {
                if (BusinessDomain.AppConfiguration.AllowNegativeAvailability)
                    useCalculatedAvailability = Message.ShowDialog (Translator.GetString ("Stock-taking in the Past"), string.Empty,
                        Translator.GetString ("You are setting a date in the past. Do you want to use calculated availability for the selected date?"), "Icons.Question32.png",
                        MessageButtons.YesNo) == ResponseType.Yes;
                else
                    MessageError.ShowDialog (Translator.GetString ("You are setting a date in the past. Using calculated items availability " +
                        "is only available when working with negative availability is allowed. The items availability in this moment will be used."));
            }

            base.SetDate (d);
        }

        #region Item column handling

        protected override bool ItemColumnEvaluate (int row, Item item, bool updatePrice)
        {
            if (item == null)
                return false;

            StockTakingDetail detail = operation.Details [row];
            Item oldItem = (detail.ItemId >= 0) ? Item.GetById (detail.ItemId) : null;
            bool ret = detail.ItemEvaluate (item, GetOperationPriceGroup (), updatePrice);

            if (ret && (oldItem == null || item.Id != oldItem.Id)) {
                if (operation.LocationId >= 0 && !editMode) {
                    double qtty = useCalculatedAvailability ?
                        Item.GetAvailabilityAtDate (item.Id, operation.LocationId, date) :
                        Item.GetAvailability (item.Id, operation.LocationId);
                    detail.EnteredQuantity = qtty;
                    detail.ExpectedQuantity = qtty;
                    if (qtty > 0)
                        LotsEvaluate (operation.Details, detail);
                }
            }
            foreach (StockTakingDetail stockTakingDetail in operation.Details) {
                stockTakingDetail.OldPriceIn = stockTakingDetail.PriceIn * 100 / (100 - stockTakingDetail.Discount);
                stockTakingDetail.OldPriceOut = stockTakingDetail.PriceOut * 100 / (100 - stockTakingDetail.Discount);
            }

            return ret;
        }

        protected override void ItemColumn_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            if (grid.EditedCell != args.Cell) {
                CurrentColumnEvaluate ();
                EditGridCell (args.Cell.Row, args.Cell.Column);
            } else {
                EditGridCell (-1, -1);
            }

            if (grid.DisableEdit || args.EventButton.Type != Gdk.EventType.TwoButtonPress || editMode)
                return;

            object cellValue = grid.EditedCellValue;
            GridNavigator.ChooseCellValue (ItemColumnEvaluate, ItemColumnChoose,
                cellValue == null ? string.Empty : cellValue.ToString ());
        }

        #endregion

        #region Quantity column handling

        protected override bool QtyColumnEvaluate (ref int row, double qtyValue)
        {
            if (!CheckQuantityLimitations (qtyValue, row, grid))
                return false;

            StockTakingDetail detail = operation.Details [row];
            if (barcodeUsed) {
                StockTakingDetail detailWithSameItem = operation.Details.LastOrDefault (d => d != detail &&
                    d.PriceInDB.IsEqualTo (detail.PriceInDB) && d.PriceOutDB.IsEqualTo (detail.PriceOutDB) &&
                    d.ItemId == detail.ItemId && d.LotId == detail.LotId && Lot.CompareLots (d.Lot, detail.Lot));
             
                if (detailWithSameItem != null) {
                    double increment = operation.Details [row].QuantityIncrement;
                    detailWithSameItem.EnteredQuantity += increment * (codeQtty.IsZero () ? codeQtty = 1 : codeQtty);
                    DeleteGridRow (true, false);
                    --row;
                    return true;
                }
            }
            detail.EnteredQuantity = qtyValue;
            return true;
        }

        #endregion

        #region Sale Price column handling

        protected override bool SalePriceColumnEvaluate (int row, string price)
        {
            return CheckSalePrice (row, price) && base.SalePriceColumnEvaluate (row, price);
        }

        #endregion

        #region Lot location column handling

        protected override void LotLocationColumnEditNext (int row, Key keyCode)
        {
            if (editMode)
                GridNavigator.EditNextOnLast (row, keyCode, colItem, ItemColumnEditNext, operation.Details, operation.AddNewDetail, false);
            else
                base.LotLocationColumnEditNext (row, keyCode);
        }

        #endregion

        protected override void LotsEvaluate (BindList<StockTakingDetail> detailsList, StockTakingDetail detail, bool forceChoice = false)
        {
            if (!BusinessDomain.AppConfiguration.ItemsManagementUseLots)
                return;

            if (detail.ItemId < 0)
                return;

            if (detail.LotId > 0)
                return;

            operation.LotsEvaluate (detailsList, detail, codeLot, barcodeUsed);
        }
    }
}
