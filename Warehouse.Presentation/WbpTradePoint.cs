//
// WbpTradePoint.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/25/2006
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
using Gdk;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Presentation.Dialogs;
using Image = Gtk.Image;

namespace Warehouse.Presentation
{
    public class WbpTradePoint : WbpSale
    {
        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private VBox vboxButtons;
        [Widget]
        private Button btnCash;
        [Widget]
        private Button btnCard;
        [Widget]
        private Button btnBank;
        [Widget]
        private Button btnReports;

#pragma warning restore 649

        #endregion

        protected override string PageDescription
        {
            get { return "Point of Sale"; }
        }

        public override string HelpFile
        {
            get { return "TradePoint.html"; }
        }

        public override string PageTitle
        {
            get { return pageTitle ?? (pageTitle = Translator.GetString ("Point of Sale")); }
        }

        #region Initialization steps

        protected override void InitializeForm (long? saleId)
        {
            XML form = FormHelper.LoadGladeXML ("WbpTradePoint.glade", "vboxButtons");
            form.Autoconnect (this);

            base.InitializeForm (saleId);

            evbIcon.DestroyChild ();
            Image icon = FormHelper.LoadImage ("Icons.TradePoint32.png");
            evbIcon.Add (icon);
            icon.Show ();

            DateVisible = false;
            hboxBigTotal.Visible = true;
            tblTotal.Visible = false;

            btnCash.SetChildImage (FormHelper.LoadImage ("Icons.Cash24.png"));
            btnCard.SetChildImage (FormHelper.LoadImage ("Icons.Card24.png"));
            btnBank.SetChildImage (FormHelper.LoadImage ("Icons.Bank24.png"));
            btnReports.SetChildImage (FormHelper.LoadImage ("Icons.Report24.png"));

            btnCash.SetChildLabelText (Translator.GetString ("In cash"));
            btnCard.SetChildLabelText (Translator.GetString ("Card"));
            btnBank.SetChildLabelText (Translator.GetString ("By bank"));
            btnReports.SetChildLabelText (Translator.GetString ("Reports"));

            foreach (Widget widget in vboxButtons.Children) {
                widget.Unparent ();
                vbxAdditionalButtons.PackStart (widget, false, true, 0);
            }
            algAdditionalButtons.ShowAll ();

            foreach (Button button in new [] { btnCash, btnCard, btnBank, btnReports })
                KeyShortcuts.SetAccelPath (button, FrmMain.AccelGroup, "mnuOperTradeObject/" + button.Name);

            btnCash.Visible = BusinessDomain.RestrictionTree.GetRestriction ("mnuOperTradeObjectCash") == UserRestrictionState.Allowed;
            btnCard.Visible = BusinessDomain.RestrictionTree.GetRestriction ("mnuOperTradeObjectCard") == UserRestrictionState.Allowed;
            btnBank.Visible = BusinessDomain.RestrictionTree.GetRestriction ("mnuOperTradeObjectBank") == UserRestrictionState.Allowed;
            btnReports.Visible = BusinessDomain.RestrictionTree.GetRestriction ("mnuOperTradeObjectReports") == UserRestrictionState.Allowed;

            algSave.Visible = !btnCash.Visible && !btnCard.Visible && !btnBank.Visible;

            lblSimpleView.SetText ("W");
            int width;
            int height;
            lblSimpleView.Layout.GetPixelSize (out width, out height);
            lblSimpleView.HeightRequest = height;
            lblSimpleView.SetText (string.Empty);

            evbSimpleView.ModifyBg (StateType.Normal, new Color (255, 255, 255));
            algSimpleView.Visible = true;
            btnAddRemoveVAT.Visible = false;
            btnImport.Visible = false;

            SetUser (BusinessDomain.LoggedUser);
            if (PresentationDomain.ScreenResolution < ScreenResolution.Normal) {
                UserVisible = false;
                btnClose.Visible = false;
            }
        }

        protected override void ReInitializeForm (long? saleId)
        {
            base.ReInitializeForm (saleId);

            colSalePrice.ListCell.PropertyName = "OriginalPriceOutPlusVAT";
            colSalePrice.ListCell.IsEditable = false;
            colTotal.ListCell.PropertyName = "TotalPlusVAT";

            operation.BasePricesOnPricePlusVAT = true;
        }

        protected override void InitializeVatRateColumn (ColumnController cc)
        {
        }

        protected override void ShowSimpleView ()
        {
            int row = grid.FocusedCell.Row;
            if (row < 0)
                return;

            SaleDetail saleRow = operation.Details [row];
            if (!string.IsNullOrEmpty (saleRow.ItemName)) {
                string text = string.Format ("{0}\t{1} {2} x {3} = {4}",
                    saleRow.ItemName, saleRow.Quantity, saleRow.MUnitName, Currency.ToString (saleRow.PriceOutPlusVAT), Currency.ToString (saleRow.TotalPlusVAT, operation.TotalsPriceType));

                lblSimpleView.SetText (text);
                base.ShowSimpleView ();
            } else if (operation.Details.Count <= 1) {
                lblSimpleView.SetText (string.Empty);
                base.ShowSimpleView ();
            }
        }

        public override void OnPageAdding ()
        {
            MenuItemWrapper restNode = PresentationDomain.MainForm.MainMenu.FindMenuItem ("mnuOperTradeObject");
            if (restNode.GetRestriction (BusinessDomain.LoggedUser.Id) != UserRestrictionState.Allowed)
                throw new WorkPageAddException (Translator.GetString ("Opening the Point of Sale screen is not allowed!"));

            OnPageAddingFinish ();
        }

        #endregion

        #region Finalization steps

        protected override void FinalizeSale (BasePaymentType? paymentType)
        {
            if (NoteVisible)
                operation.Note = txtNote.Buffer.Text;

            if (!OperationDetailsValidate (true))
                return;

            // Users cannot choose the date here, so auto set the date again so if the screen was opened yesterday the operation date will be accurate
            SetDate (BusinessDomain.Today);
            if (!TryApplyRules (true))
                return;

            Sale kitchenSale = (Sale) operation.GetSale ().Clone ();
            if (!TryAddServiceCharge (true))
                return;

            bool printReceipt = AskPrintReceipt ();

            if (!TryFinalizeSale (paymentType, kitchenSale))
                return;

            Sale sale = operation.GetSale ();

            if (printReceipt)
                PrintReceipt (sale);

            PrintInvoice (sale);

            OnOperationSaved (sale);

            lblSimpleView.SetText (string.Empty);

            Partner oldPartner = partner;
            Location oldPOS = location;
            User oldUser = user;

            ReInitializeForm (null);

            SetLocation (oldPOS);
            SetUser (oldUser);
            if (BusinessDomain.AppConfiguration.AlwaysChoosePartnerInTradePoint) {
                SetPartnerName ();
                PartnerFocus ();
            } else {
                SetPartner (oldPartner);
                EditGridCell (0, colItem.Index);
            }

            SetOperationTotal (operation);
        }

        protected override void PreparePaymentDialog (EditNewPayment dialogFinalize)
        {
            dialogFinalize.ChangeIsReturned = true;
            dialogFinalize.ChangeIsReturnedSensitive = false;
        }

        #endregion

        protected override void btnClear_Clicked (object sender, EventArgs e)
        {
            if (!AskForOperationClear ())
                return;

            if (editMode) {
                CellPosition editPos = grid.EditedCell;
                if (editPos.IsValid && editPos.Column == colQuantity.Index)
                    grid.CancelCellEdit ();

                operation.ClearDetails ();

                if (editPos.IsValid)
                    EditGridField (editPos.Row, editPos.Column);
            } else {
                operation.ClearDetails ();
                operation.AddNewDetail ();

                EditGridField (0, colItem.Index);
                SetOperationTotal (operation);
            }
        }

        [UsedImplicitly]
        private void btnCash_Clicked (object sender, EventArgs e)
        {
            FinalizeSale (BasePaymentType.Cash);
        }

        [UsedImplicitly]
        private void btnCard_Clicked (object sender, EventArgs e)
        {
            FinalizeSale (BasePaymentType.Card);
        }

        [UsedImplicitly]
        private void btnBank_Clicked (object sender, EventArgs e)
        {
            FinalizeSale (BasePaymentType.BankTransfer);
        }

        [UsedImplicitly]
        private void btnReports_Clicked (object sender, EventArgs e)
        {
            using (ReceiptReports receiptReports = new ReceiptReports ())
                receiptReports.Run ();
        }

        #region Totals handling

        protected override void OperationTotalHide ()
        {
            lblBigTotalValue.Hide ();
            lblBigTotal.Hide ();
        }

        protected override void SetOperationTotal (Operation oper)
        {
            double totalPlusVat = oper.TotalPlusVAT;

            if (totalPlusVat > 0) {
                lblBigTotalValue.SetText (Currency.ToString (totalPlusVat, operation.TotalsPriceType));
                lblBigTotalValue.Show ();
                lblBigTotal.Show ();
            } else {
                lblBigTotalValue.Hide ();
                lblBigTotal.Hide ();
            }
        }

        #endregion

        #region Grid column handling

        protected override bool EditGridCell (int row, int column)
        {
            if (!base.EditGridCell (row, column))
                return false;

            if (!BusinessDomain.AppConfiguration.AlwaysChoosePartnerInTradePoint)
                PartnerSensitive = false;
            LocationSensitive = false;
            UserSensitive = false;
            return true;
        }

        #endregion

        #region WorkBookPage Members

        public override ViewProfile ViewProfile
        {
            get
            {
                return ViewProfile.GetByName ("PointOfSale") ?? new ViewProfile
                    {
                        Name = "PointOfSale",
                        ShowToolbar = false,
                        ShowTabs = PresentationDomain.ScreenResolution >= ScreenResolution.Normal,
                        ShowStatusBar = false
                    };
            }
        }

        #endregion
    }
}
