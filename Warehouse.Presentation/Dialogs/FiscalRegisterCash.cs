//
// FiscalRegisterCash.cs
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
using System.Text;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public class FiscalRegisterCash : DialogBase
    {
        private readonly ICashReceiptPrinterController cashReceiptDriver;
        private double total;

        #region Glade Widgets

        [Widget]
        protected Dialog dlgFiscalRegisterCash;

        [Widget]
        protected RadioButton rbnCashInput;
        [Widget]
        protected RadioButton rbnCashOutput;
        [Widget]
        protected Button btnChoose;
        [Widget]
        protected Label lblAmount;
        [Widget]
        protected Label lblAmountValue;
        [Widget]
        protected Label lblCashDescription;
        [Widget]
        protected TextView txvCashDescription;
        [Widget]
        protected Label lblReason;
        [Widget]
        protected TextView txvReason;

        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;

        #endregion

        public override string HelpFile
        {
            get { return "EditRegisterCash.html"; }
        }

        public override Dialog DialogControl
        {
            get { return dlgFiscalRegisterCash; }
        }

        public FiscalRegisterCash ()
        {
            cashReceiptDriver = BusinessDomain.DeviceManager.SalesDataControllerDriver as ICashReceiptPrinterController
                ?? BusinessDomain.DeviceManager.CashReceiptPrinterDriver;
            if (cashReceiptDriver == null)
                throw new HardwareErrorException (new ErrorState (ErrorState.CashReceiptPrinterDisconnected, HardwareErrorSeverity.Error));

            Initialize ();

            if (cashReceiptDriver.SupportedCommands.Contains (DeviceCommands.RegisterCash)) {
                rbnCashInput.Sensitive = true;
                rbnCashOutput.Sensitive = true;
                btnChoose.Sensitive = true;
                txvCashDescription.Sensitive = true;
                txvReason.Sensitive = true;
                btnOK.Sensitive = true;
            }

            lblAmountValue.SetText (Currency.ToString (0, PriceType.SaleTotal));
            btnChoose.Clicked += btnChoose_Clicked;
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.FiscalRegisterCash.glade", "dlgFiscalRegisterCash");
            form.Autoconnect (this);

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            btnChoose.SetChildImage (FormHelper.LoadImage ("Icons.Banknote16.png"));

            base.InitializeForm ();
            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgFiscalRegisterCash.Icon = FormHelper.LoadImage ("Icons.Report32.png").Pixbuf;
            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            lblAmount.SetText (Translator.GetString ("Amount"));
            lblReason.SetText (Translator.GetString ("Reason"));
            lblCashDescription.SetText (Translator.GetString ("Cash description"));
            dlgFiscalRegisterCash.Title = Translator.GetString ("Register Cash");

            rbnCashInput.Label = Translator.GetString ("Cash input");
            rbnCashOutput.Label = Translator.GetString ("Cash output");
        }

        #region Event handling

        private void btnChoose_Clicked (object sender, EventArgs e)
        {
            StringBuilder sb;
            using (ChooseMoney dialog = new ChooseMoney (PriceType.SaleTotal)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                total = dialog.GetTotal ();
                string totalString = Currency.ToString (total, PriceType.SaleTotal);
                lblAmountValue.SetText (totalString);
                int maxPriceLen = totalString.Length;

                sb = new StringBuilder ();
                foreach (KeyValuePair<double, double> amount in dialog.GetAmounts ()) {
                    sb.AppendLine (string.Format ("{0} x {1} = {2}",
                        Currency.ToString (amount.Key).PadLeft (maxPriceLen, ' '),
                        amount.Value.ToString ().PadLeft (3, ' '),
                        Currency.ToString (amount.Key * amount.Value, PriceType.SaleTotal).PadLeft (maxPriceLen, ' ')));
                }
            }
            txvCashDescription.Buffer.Text = sb.ToString ();
        }

        protected virtual void btnOK_Clicked (object o, EventArgs args)
        {
            if (total == 0) {
                MessageError.ShowDialog (Translator.GetString ("Please select a valid cash amount before proceeding!"));
                return;
            }

            if (!FormHelper.TryReceiptPrinterCommand (delegate
                {
                    BusinessDomain.DeviceManager.TryDeviceCommand (delegate
                    {
                        if (rbnCashInput.Active) {
                            cashReceiptDriver.RegisterCash (total, txvCashDescription.Buffer.Text, txvReason.Buffer.Text);
                        } else if (rbnCashOutput.Active) {
                            cashReceiptDriver.RegisterCash (-total, txvCashDescription.Buffer.Text, txvReason.Buffer.Text);
                        }
                    });
                }))
                return;

            dlgFiscalRegisterCash.Respond (ResponseType.Ok);
        }

        protected virtual void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgFiscalRegisterCash.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
