//
// EditNewAdvancePayment.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   19.04.2011
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
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Presentation.Widgets;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNewAdvancePayment : EditNewPaymentBase
    {
        private readonly Payment paymentToEdit;
        private readonly BasePaymentType paymentType;
        private readonly BindList<Payment> payments = new BindList<Payment> ();
        private readonly Partner partner;
        private readonly bool alwaysReceive;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEditNewAdvancePayment;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;
        [Widget]
        private HBox hboxMain;
        [Widget]
        private VBox vboxPayments;
        [Widget]
        private Label lblPartner;
        [Widget]
        private Label lblPartnerName;
        [Widget]
        private Label lblDebt;
        [Widget]
        private Label lblDebtValue;
        [Widget]
        private Button btnAdd;
        [Widget]
        private CheckButton chkPrintCashReceipt;
        [Widget]
        private CheckButton chkPrintDocument;
        [Widget]
        private HSeparator hspChangeButtons;

        private readonly PaymentWidget paymentWidget;
        private readonly double debt;

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
            get { return dlgEditNewAdvancePayment; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        public BindList<Payment> Payments
        {
            get { return payments; }
        }

        public EditNewAdvancePayment (Partner partner, BasePaymentType? paymentType = null, bool alwaysReceive = false)
        {
            this.partner = partner;
            this.alwaysReceive = alwaysReceive;
            this.paymentType = paymentType ?? BusinessDomain.AppConfiguration.LastPaymentMethod;

            paymentWidget = new PaymentWidget (payments, PriceType.SaleTotal);

            Initialize ();

            debt = Partner.GetDebt (partner.Id);
            lblDebtValue.SetText (Currency.ToString (Math.Abs (debt)));
            lblDebt.SetText (debt < 0 ? Translator.GetString ("We owe:") : Translator.GetString ("Debt:"));

            if (Partner.GetDuePayments (partner.Id).Length > 0)
                paymentWidget.Received = Math.Abs (debt);
            else
                this.alwaysReceive = true;

            ArrangePaymentWidget ();

            chkPrintCashReceipt.Sensitive = BusinessDomain.AppConfiguration.CashReceiptPrinterEnabled;
        }

        public EditNewAdvancePayment (Payment payment)
            : this (new Partner { Id = payment.PartnerId, Name = payment.PartnerName })
        {
            paymentToEdit = payment;
            paymentType = payment.Type.BaseType;

            hspChangeButtons.Visible = false;

            paymentWidget.SetSelectedPaymentType (paymentType);
            paymentWidget.Received = payment.Quantity;

            paymentWidget.TablePayments.Remove (btnAdd);
            paymentWidget.TablePayments.NColumns--;
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditNewAdvancePayment.glade", "dlgEditNewAdvancePayment");
            form.Autoconnect (this);

            dlgEditNewAdvancePayment.Icon = FormHelper.LoadImage ("Icons.TradePoint32.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            btnAdd = new Button ();
            btnAdd.Clicked += btnAdd_Clicked;
            uint columns = ++paymentWidget.TablePayments.NColumns;
            paymentWidget.TablePayments.Attach (btnAdd, columns - 1, columns, 0, 1, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
            btnAdd.SetChildImage (FormHelper.LoadImage ("Icons.Add16.png"));
            btnAdd.Show ();

            base.InitializeForm ();
            InitializeFormStrings ();
            paymentWidget.RefreshGrid ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgEditNewAdvancePayment.Title = Translator.GetString ("Advance Payment");

            lblPartner.SetText (Translator.GetString ("Partner:"));
            lblPartnerName.SetText (partner.Name);

            paymentWidget.SetSelectedPaymentType (paymentType);
            paymentWidget.Received = 0;

            chkPrintCashReceipt.Label = Translator.GetString ("Print cash receipt");
            chkPrintDocument.Label = Translator.GetString ("Print payment document");

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        private void ArrangePaymentWidget ()
        {
            paymentWidget.ParentWindow = dlgEditNewAdvancePayment;
            paymentWidget.WidgetSeparator.Unparent ();
            hboxMain.PackStart (paymentWidget.WidgetSeparator, false, false, 4);
            paymentWidget.WidgetChooseMoney.Unparent ();
            hboxMain.PackStart (paymentWidget.WidgetChooseMoney, false, false, 0);
            paymentWidget.WidgetPayments.Unparent ();
            vboxPayments.PackStart (paymentWidget.WidgetPayments, true, true, 0);
            vboxPayments.ReorderChild (paymentWidget.WidgetPayments, 1);
            paymentWidget.WidgetPayments.Show ();
        }

        [UsedImplicitly]
        private void btnAdd_Clicked (object sender, EventArgs e)
        {
            double amountReceived = paymentWidget.Received;
            if (amountReceived <= 0)
                return;

            AddPayment (amountReceived);
            paymentWidget.Received = 0;
            paymentWidget.FocusReceived ();
        }

        private Payment AddPayment (double amountReceived)
        {
            Payment payment = new Payment
                {
                    Quantity = amountReceived,
                    Sign = alwaysReceive ? 1 : Math.Sign (debt),
                    Mode = PaymentMode.Paid,
                    PartnerId = partner.Id,
                    PartnerName = partner.Name,
                    TypeId = paymentWidget.GetPaymentTypeId ()
                };
            payments.Add (payment);
            paymentWidget.RefreshGrid ();

            return payment;
        }

        public void SetReceived (double value)
        {
            paymentWidget.Received = value;
        }

        #region Event handling

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (paymentToEdit != null) {
                paymentToEdit.PartnerId = partner.Id;
                paymentToEdit.PartnerName = partner.Name;
                paymentToEdit.TypeId = paymentWidget.GetPaymentTypeId ();
                paymentToEdit.Quantity = paymentWidget.Received;
                payments.Add (paymentToEdit);
            } else {
                double amountReceived = paymentWidget.Received;
                if (amountReceived > 0) {
                    Payment payment = AddPayment (amountReceived);
                    if (!BusinessDomain.OnPaymentProcessed (payment)) {
                        payments.Remove (payment);
                        paymentWidget.RefreshGrid ();
                        return;
                    }
                }

                // Add the new payments
                for (int i = payments.Count - 1; i >= 0; i--)
                    if (payments [i].Quantity.IsZero ())
                        payments.RemoveAt (i);
            }

            BusinessDomain.AppConfiguration.LastPaymentMethod = (BasePaymentType) paymentWidget.GetPaymentTypeId ();
            dlgEditNewAdvancePayment.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditNewAdvancePayment.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
