//
// EditNewDocument.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/21/2006
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
using GLib;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public abstract class EditNewDocument<TDetail> : DialogBase where TDetail : InvoiceDetail, new ()
    {
        protected readonly List<Operation> operations = new List<Operation> ();
        protected Sale sale;
        protected readonly DocumentBase<TDetail> document;
        private long documentNumber;
        private long referenceNumber;
        private bool isPrintable;
        private long partnerId;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEditNewDocument;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

        [Widget]
        private HBox hboPartner;
        [Widget]
        private Label lblPartner;
        [Widget]
        private Label lblPartnerValue;
        [Widget]
        private Button btnPartner;

        [Widget]
        private Frame fraMain;
        [Widget]
        private Label lblMain;
        [Widget]
        private Label lblNumber;
        [Widget]
        private Label lblDate;
        [Widget]
        private Label lblRefNumber;
        [Widget]
        private Label lblRefDate;
        [Widget]
        private Label lblRecipient;
        [Widget]
        private Label lblEGN;
        [Widget]
        private Label lblProvider;
        [Widget]
        private Entry txtNumber;
        [Widget]
        private Entry txtDate;
        [Widget]
        private Button btnChooseDate;
        [Widget]
        private Entry txtRefNumber;
        [Widget]
        private Entry txtRefDate;
        [Widget]
        private Button btnChooseRefDate;
        [Widget]
        private ComboBoxEntry cbeRecipient;
        [Widget]
        private ComboBoxEntry cbeEGN;
        [Widget]
        private ComboBoxEntry cbeProvider;

        [Widget]
        private Frame fraAdditional;
        [Widget]
        private Label lblAdditional;
        [Widget]
        private Label lblPaymentType;
        [Widget]
        private Label lblTaxDate;
        [Widget]
        private Label lblReason;
        [Widget]
        private Label lblDescription;
        [Widget]
        private Label lblLocation;
        [Widget]
        private ComboBox cboPaymentType;
        [Widget]
        private Entry txtTaxDate;
        [Widget]
        private Button btnChooseTaxDate;
        [Widget]
        private ComboBoxEntry cbeReason;
        [Widget]
        private ComboBoxEntry cbeDescription;
        [Widget]
        private ComboBoxEntry cbeLocation;

        [Widget]
        private Frame fraPrint;
        [Widget]
        private Label lblPrint;
        [Widget]
        private CheckButton chkPrintOriginal;
        [Widget]
        private CheckButton chkPrintCopy;
        [Widget]
        private SpinButton spnPrintCopy;
        [Widget]
        private HSeparator sepPrintCashReceipt;
        [Widget]
        private CheckButton chkPrintCashReceipt;
        [Widget]
        private CheckButton chkPrintInternational;

#pragma warning restore 649

        #endregion

        protected bool IsPrintable
        {
            set
            {
                isPrintable = value;
                fraPrint.Visible = value;
            }
        }

        protected bool UsesReferenceDocument
        {
            set
            {
                txtRefNumber.Visible = value;
                txtRefDate.Visible = value;
                lblRefNumber.Visible = value;
                lblRefDate.Visible = value;
                btnChooseRefDate.Visible = value;
            }
        }

        protected bool AllowCashReceipts
        {
            set
            {
                sepPrintCashReceipt.Visible = value;
                chkPrintCashReceipt.Visible = value;
            }
        }

        protected abstract bool CheckDuplicateNumbers { get; }
        protected abstract bool IsEditable { get; }

        public override Dialog DialogControl
        {
            get { return dlgEditNewDocument; }
        }

        protected EditNewDocument (DocumentBase<TDetail> document, IEnumerable<Operation> operations = null)
        {
            if (document == null)
                throw new ArgumentNullException ("document");

            this.document = document;
            if (operations != null)
                this.operations.AddRange (operations);
            if (this.operations.Count > 0) {
                Operation operation = this.operations.Last ();
                partnerId = operation.PartnerId;
                sale = operation as Sale;
            }

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditNewDocument.glade", "dlgEditNewDocument");
            form.Autoconnect (this);

            dlgEditNewDocument.Icon = GetDialogIcon ().Pixbuf;
            SetDialogTitle ();

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            dlgEditNewDocument.DeleteEvent += dlgEditNewDocument_DeleteEvent;

            if (BusinessDomain.AppConfiguration.IsPrintingAvailable ()) {
                chkPrintOriginal.Active = BusinessDomain.AppConfiguration.InvoicePrintOriginal;
                chkPrintCopy.Active = BusinessDomain.AppConfiguration.InvoicePrintCopy;
                spnPrintCopy.Value = BusinessDomain.AppConfiguration.InvoicePrintCopyNumber;
                chkPrintInternational.Active = BusinessDomain.AppConfiguration.InvoicePrintInternational;
                chkPrintOriginal.Sensitive = true;
                chkPrintCopy.Sensitive = true;
                spnPrintCopy.Sensitive = true;
                chkPrintInternational.Sensitive = true;
            } else {
                chkPrintOriginal.Active = false;
                chkPrintCopy.Active = false;
                spnPrintCopy.Value = 1d;
                chkPrintOriginal.Sensitive = false;
                chkPrintCopy.Sensitive = false;
                spnPrintCopy.Sensitive = false;
                chkPrintInternational.Sensitive = false;
            }

            if (BusinessDomain.AppConfiguration.CashReceiptPrinterEnabled &&
                BusinessDomain.WorkflowManager.AllowSaleWithoutReceipt) {
                chkPrintCashReceipt.Active = BusinessDomain.AppConfiguration.InvoicePrintFiscal;
                chkPrintCashReceipt.Sensitive = true;
            } else {
                chkPrintCashReceipt.Active = false;
                chkPrintCashReceipt.Sensitive = false;
            }

            spnPrintCopy.Sensitive = chkPrintCopy.Active;
            chkPrintOriginal.Toggled += chkPrintOriginal_Toggled;
            chkPrintCopy.Toggled += chkPrintCopy_Toggled;
            spnPrintCopy.ValueChanged += spnPrintCopy_Changed;
            chkPrintCashReceipt.Toggled += ChkPrintCashReceiptToggled;

            txtDate.ButtonPressEvent += txtDate_ButtonPressEvent;
            txtDate.KeyPressEvent += txtDate_KeyPressEvent;
            btnChooseDate.Clicked += btnChooseDate_Clicked;
            txtRefDate.ButtonPressEvent += txtRefDate_ButtonPressEvent;
            txtRefDate.KeyPressEvent += txtRefDate_KeyPressEvent;
            btnChooseRefDate.Clicked += btnChooseRefDate_Clicked;
            txtTaxDate.ButtonPressEvent += txtTaxDate_ButtonPressEvent;
            txtTaxDate.KeyPressEvent += txtTaxDate_KeyPressEvent;
            btnChooseTaxDate.Clicked += btnChooseTaxDate_Clicked;

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeEntries ();
        }

        protected abstract Image GetDialogIcon ();
        protected abstract void SetDialogTitle ();

        private void chkPrintOriginal_Toggled (object sender, EventArgs e)
        {
            BusinessDomain.AppConfiguration.InvoicePrintOriginal = chkPrintOriginal.Active;
        }

        private void chkPrintCopy_Toggled (object sender, EventArgs e)
        {
            spnPrintCopy.Sensitive = chkPrintCopy.Active;

            BusinessDomain.AppConfiguration.InvoicePrintCopy = chkPrintCopy.Active;
        }

        private void spnPrintCopy_Changed (object sender, EventArgs e)
        {
            BusinessDomain.AppConfiguration.InvoicePrintCopyNumber = spnPrintCopy.ValueAsInt;
        }

        [UsedImplicitly]
        private void chkPrintInternational_Toggled (object sender, EventArgs e)
        {
            BusinessDomain.AppConfiguration.InvoicePrintInternational = chkPrintInternational.Active;
        }

        private void ChkPrintCashReceiptToggled (object sender, EventArgs e)
        {
            BusinessDomain.AppConfiguration.InvoicePrintFiscal = chkPrintCashReceipt.Active;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            lblPartner.SetText (Translator.GetString ("Partner:"));

            lblMain.SetText (Translator.GetString ("Main"));
            lblNumber.SetText (Translator.GetString ("Number:"));
            lblDate.SetText (Translator.GetString ("Date:"));
            lblRefNumber.SetText (Translator.GetString ("To invoice:"));
            lblRefDate.SetText (Translator.GetString ("from date:"));
            lblRecipient.SetText (Translator.GetString ("Recipient:"));
            lblEGN.SetText (Translator.GetString ("UCN:"));
            lblProvider.SetText (Translator.GetString ("Issued by:"));

            lblAdditional.SetText (Translator.GetString ("Additional"));
            lblPaymentType.SetText (Translator.GetString ("Payment:"));
            lblTaxDate.SetText (Translator.GetString ("Date of financial event:"));
            lblReason.SetText (Translator.GetString ("Reason:"));
            lblDescription.SetText (Translator.GetString ("Description:"));
            lblLocation.SetText (Translator.GetString ("Location:"));

            lblPrint.SetText (Translator.GetString ("Print"));
            chkPrintOriginal.Label = Translator.GetString ("Original");
            chkPrintCopy.Label = Translator.GetString ("Copies");
            chkPrintInternational.Label = Translator.GetString ("International");
            chkPrintCashReceipt.Label = Translator.GetString ("Print cash receipt");

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        private void InitializeEntries ()
        {
            txtNumber.Text = document.NumberString;

            List<KeyValuePair<long, string>> paymentPairs = PaymentType.GetAll ().Select (p => new KeyValuePair<long, string> (p.Id, p.Name)).ToList ();

            if (IsEditable) {
                cboPaymentType.Load (paymentPairs, "Key", "Value", BusinessDomain.AppConfiguration.LastDocumentPaymentMethodId);
                Operation operation = operations.Last ();
                cbeRecipient.Load (DocumentBase.GetRecipientSuggestions (operation.PartnerId), null, null);
                cbeEGN.Load (DocumentBase.GetEGNSuggestions (operation.PartnerId), null, null);
                cbeProvider.Load (DocumentBase.GetProviderSuggestions (), null, null);
                cbeReason.Load (DocumentBase.GetReasonSuggestions (), null, null);
                cbeDescription.Load (DocumentBase.GetDescriptionSuggestions (), null, null);
                cbeLocation.Load (DocumentBase.GetLocationSuggestions (), null, null);

                txtDate.Text = BusinessDomain.GetFormattedDate (BusinessDomain.Today);
                txtTaxDate.Text = BusinessDomain.GetFormattedDate (operation.Date);
                Partner partner = Partner.GetById (partnerId);
                if (partner != null)
                    lblPartnerValue.SetText (partner.Name);
                txtNumber.GrabFocus ();
            } else {
                txtRefNumber.Text = document.ReferenceNumberString;
                txtRefDate.Text = document.ReferenceDateString;
                cboPaymentType.Load (paymentPairs, "Key", "Value", document.PaymentMethod);
                cbeRecipient.Entry.Text = document.Recipient;
                cbeEGN.Entry.Text = document.RecipientEGN;
                cbeProvider.Entry.Text = document.Provider;
                cbeReason.Entry.Text = document.Reason;
                cbeDescription.Entry.Text = document.Description;
                lblPartnerValue.SetText (document.RecipientName);
                cbeLocation.Entry.Text = document.Location;

                txtDate.Text = document.DateString;
                txtTaxDate.Text = document.TaxDateString;
                hboPartner.Sensitive = false;
                fraMain.Sensitive = false;
                fraAdditional.Sensitive = false;
            }
        }

        #region Field completion nandling

        [ConnectBefore]
        private void txtDate_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type != Gdk.EventType.TwoButtonPress)
                return;

            ChooseDate ();
        }

        private void txtDate_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (!KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                return;

            ChooseDate ();
            args.RetVal = true;
        }

        private void btnChooseDate_Clicked (object sender, EventArgs e)
        {
            ChooseDate ();
        }

        private void ChooseDate ()
        {
            object ret = txtDate.Text;
            if (FormHelper.ChooseDataFieldValue (DataField.OperationDate, ref ret) != ResponseType.Ok)
                return;

            txtDate.Text = (string) ret;
        }

        [ConnectBefore]
        private void txtRefDate_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type != Gdk.EventType.TwoButtonPress)
                return;

            ChooseRefDate ();
        }

        private void txtRefDate_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (!KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                return;

            ChooseRefDate ();
            args.RetVal = true;
        }

        private void btnChooseRefDate_Clicked (object sender, EventArgs e)
        {
            ChooseRefDate ();
        }

        private void ChooseRefDate ()
        {
            object ret = txtRefDate.Text;
            if (FormHelper.ChooseDataFieldValue (DataField.OperationDate, ref ret) != ResponseType.Ok)
                return;

            txtRefDate.Text = (string) ret;
        }

        [ConnectBefore]
        private void txtTaxDate_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type != Gdk.EventType.TwoButtonPress)
                return;

            ChooseTaxDate ();
        }

        private void txtTaxDate_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (!KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                return;

            ChooseTaxDate ();
            args.RetVal = true;
        }

        private void btnChooseTaxDate_Clicked (object sender, EventArgs e)
        {
            ChooseTaxDate ();
        }

        private void ChooseTaxDate ()
        {
            object ret = txtTaxDate.Text;
            if (FormHelper.ChooseDataFieldValue (DataField.OperationDate, ref ret) != ResponseType.Ok)
                return;

            txtTaxDate.Text = (string) ret;
        }

        #endregion

        #region Event handling

        private bool CheckEntries ()
        {
            string number = txtNumber.Text.Trim ();
            if (number.Length == 0 || !long.TryParse (number, out documentNumber)) {
                MessageError.ShowDialog (
                    Translator.GetString ("Invalid invoice number!"), ErrorSeverity.Error);
                txtNumber.GrabFocus ();
                txtNumber.SelectRegion (0, txtNumber.Text.Length);
                return false;
            }

            if (BusinessDomain.AppConfiguration.LimitDocumentNumber && 
                number.Length > BusinessDomain.AppConfiguration.DocumentNumberLength) {
                MessageError.ShowDialog (
                    Translator.GetString ("The entered invoice number is longer than allowed. " + 
                        "You may change the allowed length at Settings -> Visual or remove this check at Settings -> Operations."), ErrorSeverity.Error);
                txtNumber.GrabFocus ();
                txtNumber.SelectRegion (0, txtNumber.Text.Length);
                return false;
            }

            if (CheckDuplicateNumbers && DocumentBase.IsNumberUsed (number)) {
                MessageError.ShowDialog (
                    Translator.GetString ("Entered invoice number already exists!"), ErrorSeverity.Error);
                txtNumber.GrabFocus ();
                txtNumber.SelectRegion (0, txtNumber.Text.Length);
                return false;
            }

            if (txtDate.Text.Length == 0 ||
                (BusinessDomain.GetDateValue (txtDate.Text) == DateTime.MinValue)) {
                MessageError.ShowDialog (
                    Translator.GetString ("The selected invoice date is not valid!"), ErrorSeverity.Error);
                txtDate.GrabFocus ();
                txtDate.SelectRegion (0, txtDate.Text.Length);
                return false;
            }

            referenceNumber = 0;
            if (txtRefNumber.Visible) {
                if (txtRefNumber.Text.Length == 0 || !long.TryParse (txtRefNumber.Text, out referenceNumber)) {
                    MessageError.ShowDialog (
                        Translator.GetString ("Invalid reference document number!"), ErrorSeverity.Error);
                    txtRefNumber.GrabFocus ();
                    txtRefNumber.SelectRegion (0, txtRefNumber.Text.Length);
                    return false;
                }

                if (referenceNumber == 0 || !DocumentBase.IsNumberUsed (txtRefNumber.Text)) {
                    MessageError.ShowDialog (
                        Translator.GetString ("The selected reference document number does not exist."), ErrorSeverity.Error);
                    txtRefNumber.GrabFocus ();
                    txtRefNumber.SelectRegion (0, txtRefNumber.Text.Length);
                    return false;
                }
            }

            if (txtRefDate.Visible)
                if (txtRefDate.Text.Length == 0 ||
                    (BusinessDomain.GetDateValue (txtRefDate.Text) == DateTime.MinValue)) {
                    MessageError.ShowDialog (
                        Translator.GetString ("The selected reference document date is not valid!"), ErrorSeverity.Error);
                    txtRefDate.GrabFocus ();
                    txtRefDate.SelectRegion (0, txtRefDate.Text.Length);
                    return false;
                }

            if (txtTaxDate.Text.Length != 0 &&
                (BusinessDomain.GetDateValue (txtTaxDate.Text) == DateTime.MinValue)) {
                MessageError.ShowDialog (
                    Translator.GetString ("The selected date of the financial event is not valid!"), ErrorSeverity.Error);
                txtTaxDate.GrabFocus ();
                txtTaxDate.SelectRegion (0, txtTaxDate.Text.Length);
                return false;
            }

            return true;
        }

        private void SetDocumentFields ()
        {
            document.Number = long.Parse (txtNumber.Text);
            document.Date = BusinessDomain.GetDateValue (txtDate.Text);
            if (txtRefNumber.Visible)
                document.ReferenceNumber = long.Parse (txtRefNumber.Text);
            document.ReferenceDate = BusinessDomain.GetDateValue (txtRefDate.Text);
            document.Description = cbeDescription.Entry.Text;
            document.Location = cbeLocation.Entry.Text;
            document.TaxDate = BusinessDomain.GetDateValue (txtTaxDate.Text);
            document.Reason = cbeReason.Entry.Text;
            document.PaymentMethod = (long) cboPaymentType.GetSelectedValue ();
            document.PaymentMethodString = cboPaymentType.GetSelectedText ();
            document.Recipient = cbeRecipient.Entry.Text;
            document.RecipientEGN = cbeEGN.Entry.Text;
            document.Provider = cbeProvider.Entry.Text;
        }

        private void SaveOperationPartner ()
        {
            Operation operation = operations.Last ();
            if (operation.PartnerId == partnerId) 
                return;

            switch (operation.OperationType) {
                case OperationType.Purchase:
                case OperationType.Sale:
                    foreach (var purchase in operations) {
                        purchase.PartnerId = partnerId;
                        purchase.Commit ();
                    }
                    break;
            }
        }

        protected virtual bool SaveDocument (FinalizeAction action = FinalizeAction.CommitDocument)
        {
            if (sale != null && chkPrintCashReceipt.Active) {
                action |= FinalizeAction.PrintCashReceipt | FinalizeAction.CollectSaleData;

                if (sale.IsVATExempt || (sale.VAT.IsZero () && sale.Total > 0))
                    if (VATGroup.GetExemptGroup () == null) {
                        MessageError.ShowDialog (string.Format (Translator.GetString ("To print a receipt for a document without VAT you need to define a 0% VAT group in {0} and in the receipt printer if necessary!"), DataHelper.ProductName));
                        return false;
                    }
            }

            HardwareErrorResponse res;
            do {
                res.Retry = false;
                try {
                    BusinessDomain.DeviceManager.FinalizeOperation (new FinalizeOperationOptions { Sale = sale, Document = document, Action = action });
                } catch (InvoiceNumberInUseException ex) {
                    txtNumber.SelectRegion (0, txtNumber.Text.Length);
                    MessageError.ShowDialog (
                        Translator.GetString ("The entered document number already exists!"),
                        ErrorSeverity.Error, ex);
                    return false;
                } catch (HardwareErrorException ex) {
                    res = FormHelper.HandleHardwareError (ex);
                    if (!res.Retry) {
                        dlgEditNewDocument.Respond (ResponseType.Cancel);
                        return false;
                    }
                } catch (InsufficientItemAvailabilityException ex) {
                    MessageError.ShowDialog (
                        string.Format (Translator.GetString ("The document cannot be saved due to insufficient quantities of item \"{0}\"."), ex.ItemName),
                        ErrorSeverity.Warning, ex);
                    dlgEditNewDocument.Respond (ResponseType.Cancel);
                    return false;
                } catch (Exception ex) {
                    MessageError.ShowDialog (
                        Translator.GetString ("An error occurred while saving the document!"),
                        ErrorSeverity.Error, ex);
                    dlgEditNewDocument.Respond (ResponseType.Cancel);
                    return false;
                }
            } while (res.Retry);

            if (sale != null)
                document.Signature = sale.Signature;

            return true;
        }

        private void ChoosePartner ()
        {
            Partner [] selected;
            if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditPartners") == UserRestrictionState.Allowed) {
                using (ChooseEditPartner choosePartner = new ChooseEditPartner (true, partnerId)) {
                    if (choosePartner.Run () != ResponseType.Ok) {
                        btnPartner.GrabFocus ();
                        return;
                    }

                    selected = choosePartner.SelectedItems;
                }
            } else {
                selected = new Partner [0];
            }

            btnPartner.GrabFocus ();
            if (selected.Length <= 0)
                return;

            if (partnerId != selected [0].Id) {
                partnerId = selected [0].Id;
                string customRecipient = cbeRecipient.Entry.Text;
                cbeRecipient.Load (DocumentBase.GetRecipientSuggestions (partnerId), null, null);
                cbeRecipient.Entry.Text = customRecipient;

                string customEGN = cbeEGN.Entry.Text;
                cbeEGN.Load (DocumentBase.GetEGNSuggestions (partnerId), null, null);
                cbeEGN.Entry.Text = customEGN;

                string customProvider = cbeProvider.Entry.Text;
                cbeProvider.Load (DocumentBase.GetProviderSuggestions (), null, null);
                cbeProvider.Entry.Text = customProvider;

                string customReason = cbeReason.Entry.Text;
                cbeReason.Load (DocumentBase.GetReasonSuggestions (), null, null);
                cbeReason.Entry.Text = customReason;
            }
            lblPartnerValue.SetText (selected [0].Name);
            document.RecipientName = selected [0].Name;
        }

        [UsedImplicitly]
        private void btnPartner_Clicked (object o, EventArgs args)
        {
            ChoosePartner ();
        }

        private void dlgEditNewDocument_DeleteEvent (object o, DeleteEventArgs args)
        {
            dlgEditNewDocument.Respond (ResponseType.Cancel);
        }

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (IsEditable) {
                if (!CheckEntries ())
                    return;

                SetDocumentFields ();

                SaveOperationPartner ();

                if (!SaveDocument ())
                    return;

                BusinessDomain.AppConfiguration.LastDocumentPaymentMethodId = (long) cboPaymentType.GetSelectedValue ();
            } else {
                SetDocumentFields ();
                if (!SaveDocument (FinalizeAction.None))
                    return;
            }

            try {
                if (isPrintable && (chkPrintOriginal.Active || chkPrintCopy.Active || chkPrintInternational.Active)) {
                    document.PrintOriginal = chkPrintOriginal.Active;
                    document.PrintCopies = chkPrintCopy.Active ? spnPrintCopy.ValueAsInt : 0;
                    document.PrintInternational = chkPrintInternational.Active;

                    document.IsOriginal = document.PrintOriginal;

                    // Hide this dialog so the preview can show on a clean screen
                    dlgEditNewDocument.Hide ();
                    DocumentBase doc = document;
                    if (!document.PrintOriginal && document.PrintInternational) {
                        doc = document.GetInternational ();
                        doc.IsOriginal = null;
                    }
                    FormHelper.PrintPreviewObject (doc);
                }

                dlgEditNewDocument.Respond (ResponseType.Ok);
            } catch (Exception ex) {
                MessageError.ShowDialog (GetErrorWhileGeneratingMessage (), ErrorSeverity.Error, ex);

                dlgEditNewDocument.Respond (ResponseType.Cancel);
            }
        }

        protected abstract string GetErrorWhileGeneratingMessage ();

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditNewDocument.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
