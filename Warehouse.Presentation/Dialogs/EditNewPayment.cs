//
// EditNewPayment.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/15/2006
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
using System.ComponentModel;
using Gdk;
using Glade;
using GLib;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Presentation.Widgets;
using System.Linq;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNewPayment : EditNewPaymentBase
    {
        private readonly double total;
        private readonly BasePaymentType paymentType;
        private readonly Operation operation;
        private readonly BindList<Payment> payments = new BindList<Payment> ();
        private readonly BindList<Payment> advances = new BindList<Payment> ();
        private readonly Dictionary<Payment, List<long>> usedAdvances = new Dictionary<Payment, List<long>> ();
        private readonly PaymentWidget paymentWidget;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEditNewPayment;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

        [Widget]
        private HBox hboxMain;
        [Widget]
        private Table tablePayments;
        [Widget]
        private Label lblTotal;
        [Widget]
        private Label lblTotalValue;
        [Widget]
        private Label lblDueDate;
        [Widget]
        private Entry txtDueDate;
        [Widget]
        private Label lblRemainingDays;
        [Widget]
        private SpinButton spbRemainingDays;
        [Widget]
        private CheckButton chkUseAdvances;
        [Widget]
        private Label lblChange;
        [Widget]
        private Label lblChangeValue;
        [Widget]
        private CheckButton chkPrintCashReceipt;
        [Widget]
        private CheckButton chkChangeIsReturned;
        [Widget]
        private CheckButton chkPrintDocument;

#pragma warning restore 649

        #endregion

        public override string HelpFile
        {
            get { return "ChooseEditPayment.html"; }
        }

        public override bool PrintFiscal
        {
            get { return chkPrintCashReceipt.Active; }
        }

        public override bool PrintDocument
        {
            get { return chkPrintDocument.Active; }
        }

        public override Dialog DialogControl
        {
            get { return dlgEditNewPayment; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        public double Total
        {
            get { return total; }
        }

        public double TotalReceived
        {
            get { return payments.Sum (payment => payment.Quantity); }
        }

        public bool ChangeIsReturned
        {
            set { chkChangeIsReturned.Active = value; }
            get { return chkChangeIsReturned.Active; }
        }

        public bool ChangeIsReturnedSensitive
        {
            set { chkChangeIsReturned.Sensitive = value; }
            get { return chkChangeIsReturned.Sensitive; }
        }

        public BindList<Payment> OriginalPayments
        {
            get { return payments; }
        }

        public Payment [] UsedAdvances
        {
            get
            {
                return usedAdvances
                    .SelectMany (p => p.Value.Select (id => advances.First (a => a.Id == id)))
                    .ToArray ();
            }
        }

        public EditNewPayment (Operation operation, BasePaymentType? paymentType = null, bool printFiscal = false)
        {
            this.operation = operation;
            if (this.operation.Id <= 0)
                this.operation.Payments.Clear ();

            total = Currency.Round (operation.TotalPlusVAT, operation.TotalsPriceType);
            this.paymentType = paymentType ?? BusinessDomain.AppConfiguration.LastPaymentMethod;

            paymentWidget = new PaymentWidget (payments, operation.TotalsPriceType);
            paymentWidget.PaymentDeleted += (sender, e) => RecalculateChange (paymentWidget.Received);
            paymentWidget.ValueChanged += (sender, e) => RecalculateChange (e.Value);

            Initialize ();

            ArrangePaymentWidget ();

            chkChangeIsReturned.Visible = operation.UseChange;

            ICashReceiptPrinterController cashReceiptPrinter = BusinessDomain.DeviceManager.CashReceiptPrinterDriver;
            object hasFiscalMemory;
            chkPrintCashReceipt.Sensitive = cashReceiptPrinter != null &&
                operation.AllowFiscal &&
                (operation.Id < 0 || (cashReceiptPrinter.GetAttributes ().TryGetValue (DriverBase.HAS_FISCAL_MEMORY, out hasFiscalMemory) && (bool) hasFiscalMemory == false));

            chkPrintCashReceipt.Visible = BusinessDomain.WorkflowManager.AllowSaleWithoutReceipt;
            chkPrintCashReceipt.Active = !BusinessDomain.WorkflowManager.AllowSaleWithoutReceipt || (chkPrintCashReceipt.Sensitive && printFiscal);
            chkPrintDocument.Sensitive = BusinessDomain.AppConfiguration.IsPrintingAvailable ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditNewPayment.glade", "dlgEditNewPayment");
            form.Autoconnect (this);

            dlgEditNewPayment.Icon = FormHelper.LoadImage ("Icons.TradePoint32.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            txtDueDate.Text = BusinessDomain.GetFormattedDate (GetDueDate ());

            base.InitializeForm ();
            InitializeFormStrings ();
            paymentWidget.RefreshGrid ();

            chkUseAdvances.Visible = operation.UseAdvancePayments && operation.Id < 0;
            if (!chkUseAdvances.Visible)
                return;

            advances.AddRange (Payment.GetAdvances (operation.PartnerId));
            foreach (Payment payment in advances)
                payment.Type.BaseType = BasePaymentType.Advance;

            if (advances.Count == 0)
                chkUseAdvances.Visible = false;
            else
                chkUseAdvances.Label = string.Format ("{0} {1}",
                    Translator.GetString ("Use Advance Payments"),
                    string.Format (Translator.GetString ("(total: {0})"), Currency.ToString (advances.Sum (p => p.Quantity), operation.TotalsPriceType)));
        }

        private DateTime GetDueDate ()
        {
            return operation.Debt != null ? operation.Debt.EndDate : operation.Date;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            chkUseAdvances.Label = Translator.GetString ("Use Advance Payments");
            lblDueDate.SetText (string.Format ("{0}:", Translator.GetString ("Due Date")));
            lblRemainingDays.SetText (string.Format ("{0}:", Translator.GetString ("Remaining Days")));
            lblTotal.SetText (Translator.GetString ("Total"));
            lblChange.SetText (Translator.GetString ("Change"));
            dlgEditNewPayment.Title = Translator.GetString ("Payment");

            lblTotalValue.SetText (Currency.ToString (total, operation.TotalsPriceType));

            paymentWidget.SetSelectedPaymentType (paymentType);

            double remainder = total;

            IList<Payment> oprPayments = Payment.GetForOperation (operation, PaymentMode.Paid);
            if (oprPayments.Count > 0) {
                foreach (Payment payment in oprPayments) {
                    Payment currentPayment = payment;
                    if (operation.Payments.Find (p => p.Id == currentPayment.Id) == null)
                        operation.Payments.Add (payment);

                    currentPayment.ParentOperation = operation;
                    payments.Add ((Payment) currentPayment.Clone ());
                }
                paymentWidget.Received = 0;
            } else
                paymentWidget.Received = remainder;

            RecalculateChange (paymentWidget.Received);

            chkPrintCashReceipt.Label = Translator.GetString ("Print cash receipt");
            chkChangeIsReturned.Label = Translator.GetString ("Change is returned");
            chkPrintDocument.Label = Translator.GetString ("Print payment document");
        }

        private void ArrangePaymentWidget ()
        {
            paymentWidget.ParentWindow = dlgEditNewPayment;
            paymentWidget.WidgetSeparator.Unparent ();
            hboxMain.PackStart (paymentWidget.WidgetSeparator, false, false, 4);
            paymentWidget.WidgetChooseMoney.Unparent ();
            hboxMain.PackStart (paymentWidget.WidgetChooseMoney, false, false, 0);
            paymentWidget.WidgetPayments.Unparent ();
            tablePayments.Attach (paymentWidget.WidgetPayments, 0, 2, 1, 2, AttachOptions.Fill, AttachOptions.Fill | AttachOptions.Expand, 0, 0);
            paymentWidget.WidgetPayments.Show ();
        }

        private double AddAdvancePayments ()
        {
            double remainder = total - payments.Sum (p => p.Quantity);

            List<Payment> displayedAdvancePayments = new List<Payment> ();
            foreach (Payment advance in advances) {
                // Find an already used advance with the same payment type
                Payment payment = displayedAdvancePayments.Find (p => p.TypeId == advance.TypeId);
                if (payment == null) {
                    payment = (Payment) advance.Clone ();
                    payment.OperationId = operation.Id;
                    payment.LocationId = operation.LocationId;
                    payment.ParentOperation = operation;
                    payment.Quantity = 0;
                    displayedAdvancePayments.Add (payment);
                }

                // Record the usage of the advance payment with the similar payment type
                if (!usedAdvances.ContainsKey (payment))
                    usedAdvances [payment] = new List<long> ();

                usedAdvances [payment].Add (advance.Id);

                double sum = Math.Min (remainder, advance.Quantity);
                payment.Quantity += sum;
                remainder -= sum;
                if (remainder.IsZero ())
                    break;
            }

            foreach (Payment advancePayment in displayedAdvancePayments)
                advancePayment.PropertyChanged += payment_PropertyChanged;

            payments.AddRange (displayedAdvancePayments);
            return remainder;
        }

        private void payment_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            paymentWidget.Received = Math.Max (total - payments.Sum (p => p.Quantity), 0);
            RecalculateChange (paymentWidget.Received);
        }

        private void RecalculateChange (double value)
        {
            lblChangeValue.SetText (Currency.ToString (value + TotalReceived - total, operation.TotalsPriceType));
        }

        private bool Validate ()
        {
            double amountReceived = paymentWidget.Received;

            if (!ValidateDate ())
                return false;

            if (Currency.Round (amountReceived + TotalReceived, operation.TotalsPriceType) < total) {
                string text;
                if (PrintFiscal)
                    text = Translator.GetString ("The paid amount is less than the total. " +
                        "Do you want to add another payment? (You can only print a fiscal receipt for a fully paid operation.)");
                else
                    text = Translator.GetString ("The paid amount is less than the total. " +
                        "Do you want to add another payment?");

                switch (Message.ShowDialog (Translator.GetString ("Not enough received money"),
                    "Icons.TradePoint32.png", text, "Icons.Warning32.png", MessageButtons.YesNo)) {
                    case ResponseType.No:
                        if (AllowOperationWithoutReceipt ()) {
                            AddPayment (amountReceived);
                            return true;
                        }
                        paymentWidget.FocusReceived ();
                        return false;

                    case ResponseType.DeleteEvent:
                        paymentWidget.FocusReceived ();
                        return false;
                }

                if (amountReceived > 0)
                    AddPayment (amountReceived);

                paymentWidget.Received = total - TotalReceived;
                paymentWidget.FocusReceived ();
                return false;
            }
            if (amountReceived > 0)
                AddPayment (amountReceived);

            return true;
        }

        private bool ValidateDate ()
        {
            DateTime date = BusinessDomain.GetDateValue (txtDueDate.Text.Trim ());
            if (date == DateTime.MinValue) {
                MessageError.ShowDialog (Translator.GetString ("The entered date is invalid."), ErrorSeverity.Error);
                txtDueDate.GrabFocus ();
                return false;
            }

            if (date >= operation.Date)
                return true;

            ShowMessageTooEarlyDate ();
            return false;
        }

        private bool AllowOperationWithoutReceipt ()
        {
            return operation.OperationType != OperationType.Sale ||
                BusinessDomain.WorkflowManager.AllowSaleWithoutReceipt;
        }

        private void AddPayment (double amountReceived)
        {
            payments.Add (new Payment (operation, paymentWidget.GetPaymentTypeId (), PaymentMode.Paid)
                {
                    Quantity = amountReceived,
                    EndDate = BusinessDomain.GetDateValue (txtDueDate.Text.Trim ())
                });
            paymentWidget.RefreshGrid ();
            RecalculateChange (paymentWidget.Received);
        }

        #region Event handling

        [UsedImplicitly]
        private void chkUseAdvances_Toggled (object o, EventArgs args)
        {
            usedAdvances.Clear ();

            double remainder;
            if (chkUseAdvances.Active) {
                remainder = AddAdvancePayments ();
            } else {
                remainder = paymentWidget.Received;
                for (int i = payments.Count - 1; i >= 0; i--)
                    if (advances.Any (a => a.Id == payments [i].Id)) {
                        remainder += payments [i].Quantity;
                        payments.RemoveAt (i);
                    }
            }
            paymentWidget.RefreshGrid ();
            paymentWidget.Received = remainder;
            paymentWidget.FocusReceived ();
            RecalculateChange (paymentWidget.Received);
        }

        [ConnectBefore, UsedImplicitly]
        protected void OnDueDateButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type == EventType.TwoButtonPress)
                ChooseDate ();
        }

        [UsedImplicitly]
        protected void OnDueDateKeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                ChooseDate ();
        }

        private bool changingDueDate;

        [UsedImplicitly]
        protected void OnDueDateChanged (object o, EventArgs args)
        {
            if (changingDueDate)
                return;

            DateTime date = BusinessDomain.GetDateValue (txtDueDate.Text.Trim ());
            if (date == DateTime.MinValue)
                return;

            changingDueDate = true;
            spbRemainingDays.Value = (date - GetDueDate ()).Days;
            changingDueDate = false;
        }

        [UsedImplicitly]
        protected void OnDueDateClicked (object o, EventArgs args)
        {
            ChooseDate ();
        }

        private void ChooseDate ()
        {
            DateTime selectedDate = BusinessDomain.GetDateValue (txtDueDate.Text.Trim ());
            if (selectedDate == DateTime.MinValue)
                selectedDate = BusinessDomain.Today;

            using (ChooseDate chooseDate = new ChooseDate (selectedDate))
                if (chooseDate.Run () == ResponseType.Ok)
                    if (chooseDate.Selection.Date >= operation.Date)
                        txtDueDate.Text = BusinessDomain.GetFormattedDate (chooseDate.Selection);
                    else
                        ShowMessageTooEarlyDate ();
        }

        private void ShowMessageTooEarlyDate ()
        {
            MessageError.ShowDialog (Translator.GetString ("The due date of the payment cannot be earlier than the date of the operation."), ErrorSeverity.Error);
            txtDueDate.GrabFocus ();
        }

        [UsedImplicitly]
        protected void OnRemainingDaysValueChanged (object sender, EventArgs args)
        {
            if (changingDueDate)
                return;

            changingDueDate = true;
            txtDueDate.Text = BusinessDomain.GetFormattedDate (GetDueDate ().AddDays (spbRemainingDays.ValueAsInt));
            changingDueDate = false;
        }

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (!Validate ())
                return;

            // Mark payments in the operation for deletion
            foreach (Payment payment in operation.Payments
                .Where (payment => payment.Mode == PaymentMode.Paid && !payments.Any (p => p.Id >= 0 && p.Id == payment.Id)))
                payment.Quantity = 0;

            // Add the new payments
            foreach (Payment payment in payments) {
                if (payment.OperationType == (int) OperationType.AdvancePayment) {
                    double remainder = payment.Quantity;
                    foreach (var id in usedAdvances [payment]) {
                        long id1 = id;
                        Payment advance = advances.Find (p => p.Id == id1);
                        double sum = Math.Min (remainder, advance.Quantity);
                        advance.Quantity -= sum;

                        // Make sure credit card transactions made for advances are not annulled
                        if (advance.Quantity.IsZero ())
                            advance.TransactionNumber = null;

                        remainder -= sum;
                        if (remainder.IsZero ())
                            break;
                    }
                    // mark as new
                    payment.Id = -1;
                    payment.OperationType = (int) operation.OperationType;
                }

                // Add the payment to the operation if new
                if (payment.Id < 0) {
                    operation.Payments.Add ((Payment) payment.Clone ());
                    continue;
                }

                // Replace the payment if already used in the operation
                Payment currentPayment = payment;
                Payment existingPayment = operation.Payments.Find (p => p.Id == currentPayment.Id);
                if (existingPayment != null)
                    operation.Payments [operation.Payments.IndexOf (existingPayment)] = (Payment) payment.Clone ();
            }

            double subtractedChange = 0d;
            if (chkChangeIsReturned.Active)
                subtractedChange = TotalReceived - Total;

            if (subtractedChange > 0)
                foreach (Payment payment in operation.Payments) {
                    if (payment.Mode != PaymentMode.Paid)
                        continue;

                    if (payment.Type.BaseType != BasePaymentType.Cash)
                        continue;

                    double newQtty = Math.Max (0, payment.Quantity - subtractedChange);
                    subtractedChange -= payment.Quantity - newQtty;
                    payment.Quantity = newQtty;
                    if (subtractedChange <= 0)
                        break;
                }

            if (subtractedChange > 0)
                foreach (Payment payment in operation.Payments) {
                    if (payment.Mode != PaymentMode.Paid)
                        continue;

                    if (payment.Type.BaseType == BasePaymentType.Cash)
                        continue;

                    double newQtty = Math.Max (0, payment.Quantity - subtractedChange);
                    subtractedChange -= payment.Quantity - newQtty;
                    payment.Quantity = newQtty;
                    if (subtractedChange <= 0)
                        break;
                }

            DateTime endDate = BusinessDomain.GetDateValue (txtDueDate.Text.Trim ());
            if (operation.Debt != null)
                operation.Debt.EndDate = endDate;

            foreach (Payment payment in operation.Payments) {
                payment.EndDate = endDate;

                if (!BusinessDomain.OnPaymentProcessed (payment)) {
                    paymentWidget.Received = 0;
                    paymentWidget.FocusReceived ();
                    return;
                }
            }

            BusinessDomain.AppConfiguration.LastPaymentMethod = (BasePaymentType) paymentWidget.GetPaymentTypeId ();
            dlgEditNewPayment.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditNewPayment.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
