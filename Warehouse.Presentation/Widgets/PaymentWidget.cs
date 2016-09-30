//
// PaymentWidget.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   09.26.2011
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
using Glade;
using Gtk;
using Pango;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Presentation.Dialogs;
using Alignment = Gtk.Alignment;
using Window = Gtk.Window;

namespace Warehouse.Presentation.Widgets
{
    public class PaymentWidget
    {
        public event EventHandler PaymentDeleted;

        private readonly BindList<Payment> payments;
        private readonly PriceType priceType;
        private Window parentWindow;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Frame frmPayments;
        [Widget]
        private Alignment algPayments;
        [Widget]
        private ScrolledWindow scwPayments;
        [Widget]
        private Table tblPayments;
        [Widget]
        private Label lblPayments;
        [Widget]
        private ComboBox cboPaymentMethod;
        [Widget]
        private Entry txtReceived;
        [Widget]
        private ToggleButton btnChooseMoney;
        [Widget]
        private Label aligner;
        [Widget]
        private VSeparator vspMoneySelection;
        [Widget]
        private Alignment algChooseMoney;

#pragma warning restore 649

        #endregion

        private ChooseMoneyPanel pnlChooseMoney;
        private Payment selectedPayment;
        private ComboBox cboSelectedType;
        private Entry txtSelectedAmount;

        public double Received
        {
            get
            {
                return Currency.ParseExpression (txtReceived.Text, priceType);
            }
            set { txtReceived.Text = Currency.ToEditString (value, priceType); }
        }

        public Container WidgetPayments
        {
            get { return frmPayments; }
        }

        public Widget WidgetChooseMoney
        {
            get { return algChooseMoney; }
        }

        public VSeparator WidgetSeparator
        {
            get { return vspMoneySelection; }
        }

        public Table TablePayments
        {
            get { return tblPayments; }
        }

        public Window ParentWindow
        {
            set { parentWindow = value; }
        }

        public class ValueChangedEventArgs : EventArgs
        {
            public double Value { get; private set; }

            public ValueChangedEventArgs (double value)
            {
                Value = value;
            }
        }

        public event EventHandler<ValueChangedEventArgs> ValueChanged;

        public PaymentWidget (BindList<Payment> payments, PriceType priceType)
        {
            XML form = FormHelper.LoadGladeXML ("Widgets.PaymentWidget.glade", "hboxMain");
            form.Autoconnect (this);

            this.payments = payments;
            this.priceType = priceType;
            btnChooseMoney.SetChildImage (FormHelper.LoadImage ("Icons.Banknote16.png"));

            lblPayments.SetText (Translator.GetString ("Payments"));

            cboPaymentMethod.FocusInEvent += cboPaymentMethod_FocusInEvent;
            txtReceived.Changed += OnValueChanged;
            txtReceived.FocusInEvent += cboPaymentMethod_FocusInEvent;
            scwPayments.VScrollbar.Shown += VScrollbar_Shown;
            scwPayments.VScrollbar.Hidden += VScrollbar_Hidden;
            btnChooseMoney.Toggled += btnChooseMoney_Toggled;
            btnChooseMoney.Active = BusinessDomain.AppConfiguration.LastChooseMoneyVisible;

            txtReceived.GrabFocus ();
        }

        public void SetSelectedPaymentType (BasePaymentType paymentType)
        {
            bool includePrinterPayments = false;
            DeviceManagerBase dManager = BusinessDomain.DeviceManager;
            if (dManager.CashReceiptPrinterConnected &&
                dManager.CashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.GetPaymentName)) {
                includePrinterPayments = true;
            }

            List<KeyValuePair<long, string>> paymentPairs = new List<KeyValuePair<long, string>> ();
            foreach (PaymentType paymentMethod in PaymentType.GetAll ()) {
                string paymentName;
                if (includePrinterPayments) {
                    dManager.CashReceiptPrinterDriver.GetPaymentName (paymentMethod.BaseType, out paymentName);
                    paymentName = string.IsNullOrEmpty (paymentName) ?
                        paymentMethod.Name :
                        string.Format ("{0} ({1})", paymentMethod.Name, paymentName);
                } else
                    paymentName = paymentMethod.Name;

                paymentPairs.Add (new KeyValuePair<long, string> (paymentMethod.Id, paymentName));
            }
            cboPaymentMethod.Load (paymentPairs, "Key", "Value", (long) paymentType);
        }

        public long GetPaymentTypeId ()
        {
            return (long) cboPaymentMethod.GetSelectedValue ();
        }

        public void RefreshGrid (bool? amountEdited = null)
        {
            for (int i = tblPayments.Children.Length - 1; i >= 0; i--) {
                Widget child = tblPayments.Children [i];
                tblPayments.Remove (child);
                child.Destroy ();
            }
            if (payments.Count > 3) {
                scwPayments.HeightRequest = 100;
                scwPayments.VscrollbarPolicy = PolicyType.Automatic;
            } else {
                scwPayments.HeightRequest = -1;
                scwPayments.VscrollbarPolicy = PolicyType.Never;
            }

            uint rows = 0;
            foreach (Payment payment in payments) {
                if (ReferenceEquals (selectedPayment, payment)) {
                    cboSelectedType = new ComboBox ();
                    cboSelectedType.Load (PaymentType.GetAll (), "Id", "Name", payment.Type.Id);
                    tblPayments.Attach (cboSelectedType, 0, 1, rows, rows + 1, AttachOptions.Fill | AttachOptions.Expand, 0, 0, 1);
                    cboSelectedType.Changed += cboSelectedType_Changed;

                    txtSelectedAmount = new Entry
                        {
                            Text = Currency.ToEditString (payment.Quantity, priceType),
                            WidthRequest = 100,
                            Xalign = 1f
                        };
                    txtSelectedAmount.Changed += OnSelectedValueChanged;
                    tblPayments.Attach (txtSelectedAmount, 1, 2, rows, rows + 1, AttachOptions.Fill, AttachOptions.Fill, 0, 1);
                    if (amountEdited == true)
                        txtSelectedAmount.GrabFocus ();
                    if (amountEdited == false)
                        cboSelectedType.GrabFocus ();
                } else {
                    Label lblType = new Label (payment.Type.Name) { Xalign = 0f, Ellipsize = EllipsizeMode.End };
                    Button btnType = new Button (lblType) { Relief = ReliefStyle.None };
                    btnType.Data.Add ("Payment", payment);
                    btnType.Clicked += btnEditType_Clicked;
                    tblPayments.Attach (btnType, 0, 1, rows, rows + 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 1);

                    Label lblAmount = new Label (Currency.ToString (payment.Quantity, priceType)) { Xalign = 1f };
                    Button btnAmount = new Button (lblAmount) { Relief = ReliefStyle.None, WidthRequest = 100 };
                    btnAmount.Data.Add ("Payment", payment);
                    btnAmount.Clicked += btnEditAmount_Clicked;
                    tblPayments.Attach (btnAmount, 1, 2, rows, rows + 1, AttachOptions.Fill, AttachOptions.Fill, 0, 1);
                }

                Button btnDelete = new Button (FormHelper.LoadImage ("Icons.Delete16.png"));
                btnDelete.Data.Add ("Payment", payment);
                btnDelete.Clicked += btnDelete_Clicked;
                tblPayments.Attach (btnDelete, tblPayments.NColumns - 1, tblPayments.NColumns, rows, rows + 1, AttachOptions.Shrink, AttachOptions.Shrink, 0, 1);
                rows++;
            }

            tblPayments.ShowAll ();
            if (payments.Count > 0) {
                lblPayments.Visible = true;
                lblPayments.HeightRequest = -1;
                frmPayments.ShadowType = ShadowType.EtchedIn;
                algPayments.LeftPadding = 4;
                algPayments.RightPadding = 4;
            } else {
                lblPayments.Visible = false;
                lblPayments.HeightRequest = 1;
                frmPayments.ShadowType = ShadowType.None;
                algPayments.LeftPadding = 0;
                algPayments.RightPadding = 0;
            }
            RefreshChooseMoneyPanel ();
        }

        private void cboSelectedType_Changed (object sender, EventArgs e)
        {
            if (selectedPayment != null && cboSelectedType != null)
                selectedPayment.TypeId = (long) cboSelectedType.GetSelectedValue ();
        }

        private void btnChooseMoney_Toggled (object sender, EventArgs e)
        {
            if (pnlChooseMoney == null) {
                pnlChooseMoney = new ChooseMoneyPanel (priceType);
                pnlChooseMoney.TotalChanged += pnlChooseMoney_TotalChanged;
                pnlChooseMoney.Show ();
                algChooseMoney.Add (pnlChooseMoney);
            }

            bool chooseMoney = btnChooseMoney.Active;
            BusinessDomain.AppConfiguration.LastChooseMoneyVisible = chooseMoney;
            vspMoneySelection.Visible = chooseMoney;
            pnlChooseMoney.Visible = chooseMoney;
            if (selectedPayment != null)
                RefreshChooseMoneyPanel ();
            if (!chooseMoney)
                parentWindow.Resize (10, 10);
        }

        private void pnlChooseMoney_TotalChanged (object sender, EventArgs e)
        {
            double amount = pnlChooseMoney.GetTotal ();
            if (txtSelectedAmount != null)
                txtSelectedAmount.Text = Currency.ToEditString (amount, priceType);
            else
                txtReceived.Text = Currency.ToEditString (amount, priceType);
        }

        private void RefreshChooseMoneyPanel ()
        {
            if (!btnChooseMoney.Active)
                return;

            if (selectedPayment == null && payments.Count > 0)
                pnlChooseMoney.SetTotal (Currency.ParseExpression (txtReceived.Text));
            else if (txtSelectedAmount != null)
                pnlChooseMoney.SetTotal (Currency.ParseExpression (txtSelectedAmount.Text));
        }

        private void VScrollbar_Shown (object o, EventArgs args)
        {
            aligner.Show ();
            aligner.WidthRequest = scwPayments.VScrollbar.SizeRequest ().Width + 1;
        }

        private void VScrollbar_Hidden (object o, EventArgs args)
        {
            aligner.Hide ();
        }

        public void FocusReceived ()
        {
            txtReceived.GrabFocus ();
        }

        private void btnEditType_Clicked (object sender, EventArgs e)
        {
            EditPayment (sender, false);
        }

        private void btnEditAmount_Clicked (object sender, EventArgs e)
        {
            EditPayment (sender, true);
        }

        private void EditPayment (object sender, bool amountEdited)
        {
            Button btn = (Button) sender;
            Payment payment = btn.Data ["Payment"] as Payment;
            if (payment == null)
                return;

            if (cboSelectedType != null)
                selectedPayment.TypeId = (long) cboSelectedType.GetSelectedValue ();

            if (txtSelectedAmount != null)
                selectedPayment.Quantity = Currency.ParseExpression (txtSelectedAmount.Text);

            selectedPayment = payment;
            RefreshGrid (amountEdited);
        }

        private void btnDelete_Clicked (object sender, EventArgs e)
        {
            Button btn = (Button) sender;
            Payment payment = btn.Data ["Payment"] as Payment;
            if (payment == null)
                return;

            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Payment"),
                "Icons.TradePoint32.png",
                Translator.GetString ("Do you want to delete the selected payment?"),
                "Icons.Delete32.png")) {
                if (dialog.Run () != ResponseType.Ok)
                    return;
            }

            payments.Remove (payment);

            RefreshGrid ();
            parentWindow.Resize (10, 10);
            txtReceived.GrabFocus ();
            OnPaymentDeleted (EventArgs.Empty);
        }

        private void cboPaymentMethod_FocusInEvent (object o, FocusInEventArgs args)
        {
            if (selectedPayment != null) {
                if (cboSelectedType != null)
                    selectedPayment.TypeId = (long) cboSelectedType.GetSelectedValue ();

                if (txtSelectedAmount != null) {
                    txtSelectedAmount.Changed -= OnSelectedValueChanged;
                    selectedPayment.Quantity = Currency.ParseExpression (txtSelectedAmount.Text);
                }
            }

            selectedPayment = null;
            cboSelectedType = null;
            txtSelectedAmount = null;
            RefreshGrid ();
        }

        private void OnPaymentDeleted (EventArgs e)
        {
            EventHandler handler = PaymentDeleted;
            if (handler != null)
                handler (this, e);
        }

        private void OnSelectedValueChanged (object sender, EventArgs e)
        {
            if (selectedPayment != null) {
                if (cboSelectedType != null)
                    selectedPayment.TypeId = (long) cboSelectedType.GetSelectedValue ();

                if (txtSelectedAmount != null)
                    selectedPayment.Quantity = Currency.ParseExpression (txtSelectedAmount.Text, priceType);
            }

            OnValueChanged (sender, e);
        }

        private void OnValueChanged (object sender, EventArgs e)
        {
            EventHandler<ValueChangedEventArgs> handler = ValueChanged;
            if (handler != null) {
                double value = Currency.ParseExpression (txtReceived.Text, priceType);
                handler (this, new ValueChangedEventArgs (value));
            }
        }
    }
}
