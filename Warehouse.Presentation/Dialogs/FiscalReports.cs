//
// FiscalReports.cs
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
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Devices;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public class FiscalReports : DialogBase
    {
        private readonly ICashReceiptPrinterController cashReceiptDriver;

        #region Glade Widgets

        [Widget]
        protected Dialog dlgFiscalReports;
        [Widget]
        protected Button btnClose;

        [Widget]
        protected Label lblGeneralReports;
        [Widget]
        protected Frame frmDailyReports;
        [Widget]
        protected Label lblDailyReports;
        [Widget]
        protected CheckButton chkResetOperations;
        [Widget]
        protected RadioButton rbnDailyXReport;
        [Widget]
        protected RadioButton rbnDailyZReport;
        [Widget]
        protected RadioButton rbnDailyEJReport;
        [Widget]
        protected Button btnDailyReport;

        [Widget]
        protected Frame frmSpecialReports;
        [Widget]
        protected Label lblSpecialReports;
        [Widget]
        protected RadioButton rbnOperatorsReport;
        [Widget]
        protected RadioButton rbnVATChangesReport;
        [Widget]
        protected RadioButton rbnRAMResetsReport;
        [Widget]
        protected Button btnSpecialReport;

        [Widget]
        protected Label lblAdditionalReports;
        [Widget]
        protected Frame frmDateReports;
        [Widget]
        protected Label lblDateReports;
        [Widget]
        protected RadioButton rbnDateReport;
        [Widget]
        protected RadioButton rbnDateDetailReport;
        [Widget]
        protected Entry txtDateFrom;
        [Widget]
        protected Entry txtDateTo;
        [Widget]
        protected Button btnDateReport;

        [Widget]
        protected Frame frmNumberReports;
        [Widget]
        protected Label lblNumberReports;
        [Widget]
        protected RadioButton rbnNumberReport;
        [Widget]
        protected RadioButton rbnNumberDetailReport;
        [Widget]
        protected Entry txtNumberFrom;
        [Widget]
        protected Entry txtNumberTo;
        [Widget]
        protected Button btnNumberReport;

        #endregion

        public override string HelpFile
        {
            get { return "EditFiscalReports.html"; }
        }

        public override Dialog DialogControl
        {
            get { return dlgFiscalReports; }
        }

        public FiscalReports ()
        {
            cashReceiptDriver = BusinessDomain.DeviceManager.SalesDataControllerDriver as ICashReceiptPrinterController
                ?? BusinessDomain.DeviceManager.CashReceiptPrinterDriver;
            if (cashReceiptDriver == null)
                throw new HardwareErrorException (new ErrorState (ErrorState.CashReceiptPrinterDisconnected, HardwareErrorSeverity.Error));

            Initialize ();

            #region Daily reports

            bool supportsAny = false;
            if (cashReceiptDriver.SupportedCommands.Contains (DeviceCommands.DailyEJReport)) {
                rbnDailyEJReport.Sensitive = true;
                rbnDailyEJReport.Active = true;
                supportsAny = true;
            }
            if (cashReceiptDriver.SupportedCommands.Contains (DeviceCommands.DailyZReport)) {
                rbnDailyZReport.Sensitive = true;
                rbnDailyZReport.Active = true;
                supportsAny = true;
            }
            if (cashReceiptDriver.SupportedCommands.Contains (DeviceCommands.DailyXReport)) {
                rbnDailyXReport.Sensitive = true;
                rbnDailyXReport.Active = true;
                supportsAny = true;
            }
            if (!supportsAny)
                frmDailyReports.Sensitive = false;

            #endregion

            #region Special reports

            supportsAny = false;
            if (cashReceiptDriver.SupportedCommands.Contains (DeviceCommands.RAMResetsReport)) {
                rbnRAMResetsReport.Sensitive = true;
                rbnRAMResetsReport.Active = true;
                supportsAny = true;
            }
            if (cashReceiptDriver.SupportedCommands.Contains (DeviceCommands.VATRateChangesReport)) {
                rbnVATChangesReport.Sensitive = true;
                rbnVATChangesReport.Active = true;
                supportsAny = true;
            }
            if (cashReceiptDriver.SupportedCommands.Contains (DeviceCommands.OperatorsReport)) {
                rbnOperatorsReport.Sensitive = true;
                rbnOperatorsReport.Active = true;
                supportsAny = true;
            }

            if (!supportsAny)
                frmSpecialReports.Sensitive = false;

            #endregion

            #region Reports by dates reports

            supportsAny = false;
            if (cashReceiptDriver.SupportedCommands.Contains (DeviceCommands.DetailFMReportByDates)) {
                rbnDateDetailReport.Sensitive = true;
                rbnDateDetailReport.Active = true;
                supportsAny = true;
            }
            if (cashReceiptDriver.SupportedCommands.Contains (DeviceCommands.ShortFMReportByDates)) {
                rbnDateReport.Sensitive = true;
                rbnDateReport.Active = true;
                supportsAny = true;
            }
            if (!supportsAny)
                frmDateReports.Sensitive = false;

            #endregion

            #region Reports by numbers reports

            supportsAny = false;
            if (cashReceiptDriver.SupportedCommands.Contains (DeviceCommands.DetailFMReportByNumbers)) {
                rbnNumberDetailReport.Sensitive = true;
                rbnNumberDetailReport.Active = true;
                supportsAny = true;
            }
            if (cashReceiptDriver.SupportedCommands.Contains (DeviceCommands.ShortFMReportByNumbers)) {
                rbnNumberReport.Sensitive = true;
                rbnNumberReport.Active = true;
                supportsAny = true;
            }
            if (!supportsAny)
                frmNumberReports.Sensitive = false;

            #endregion
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.FiscalReports.glade", "dlgFiscalReports");
            form.Autoconnect (this);

            btnClose.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();
            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgFiscalReports.Icon = FormHelper.LoadImage ("Icons.Report32.png").Pixbuf;
            btnDailyReport.SetChildLabelText (Translator.GetString ("Print"));
            btnDateReport.SetChildLabelText (Translator.GetString ("Print"));
            btnNumberReport.SetChildLabelText (Translator.GetString ("Print"));
            btnSpecialReport.SetChildLabelText (Translator.GetString ("Print"));
            btnClose.SetChildLabelText (Translator.GetString ("Close"));

            lblGeneralReports.SetText (Translator.GetString ("General reports"));
            lblDailyReports.SetText (Translator.GetString ("Daily fiscal reports"));
            lblSpecialReports.SetText (Translator.GetString ("Special reports"));
            lblAdditionalReports.SetText (Translator.GetString ("Additional reports"));
            lblDateReports.SetText (Translator.GetString ("Fiscal report by dates"));
            lblNumberReports.SetText (Translator.GetString ("Fiscal report by numbers"));
            dlgFiscalReports.Title = Translator.GetString ("Fiscal Reports");

            rbnDailyXReport.Label = Translator.GetString ("X Report");
            rbnDailyZReport.Label = Translator.GetString ("Z Report");
            rbnDailyEJReport.Label = Translator.GetString ("Electronic Journal Report");
            chkResetOperations.Label = Translator.GetString ("Reset Operations");

            rbnOperatorsReport.Label = Translator.GetString ("Operators Report");
            rbnVATChangesReport.Label = Translator.GetString ("VAT Changes Report");
            rbnRAMResetsReport.Label = Translator.GetString ("RAM Resets Report");

            rbnDateReport.Label = Translator.GetString ("Short Fiscal Memory Report");
            rbnDateDetailReport.Label = Translator.GetString ("Detail Fiscal Memory Report");

            rbnNumberReport.Label = Translator.GetString ("Short Fiscal Memory Report");
            rbnNumberDetailReport.Label = Translator.GetString ("Detail Fiscal Memory Report");

            txtDateFrom.ButtonPressEvent += txtDateFrom_ButtonPressEvent;
            txtDateFrom.KeyPressEvent += txtDateFrom_KeyPressEvent;
            txtDateTo.ButtonPressEvent += txtDateTo_ButtonPressEvent;
            txtDateTo.KeyPressEvent += txtDateTo_KeyPressEvent;
        }

        #region Event handling

        [UsedImplicitly]
        protected void btnDailyReport_Clicked (object o, EventArgs args)
        {
            string closure;
            Dictionary<FiscalPrinterTaxGroup, double> amounts;
            bool resetOps = chkResetOperations.Active;

            FormHelper.TryReceiptPrinterCommand (delegate
                {
                    BusinessDomain.DeviceManager.TryDeviceCommand (delegate
                        {
                            if (rbnDailyXReport.Active) {
                                cashReceiptDriver.DailyXReport (resetOps, out closure, out amounts);
                            } else if (rbnDailyZReport.Active) {
                                cashReceiptDriver.DailyZReport (resetOps, out closure, out amounts);
                            } else if (rbnDailyEJReport.Active) {
                                cashReceiptDriver.DailyEJReport (resetOps, out closure, out amounts);
                            }
                        });
                }, true, MessageButtons.All & ~MessageButtons.Retry);
        }

        [UsedImplicitly]
        protected void btnSpecialReport_Clicked (object o, EventArgs args)
        {
            FormHelper.TryReceiptPrinterCommand (delegate
                {
                    BusinessDomain.DeviceManager.TryDeviceCommand (delegate
                        {
                            if (rbnOperatorsReport.Active) {
                                cashReceiptDriver.OperatorsReport ();
                            } else if (rbnVATChangesReport.Active) {
                                cashReceiptDriver.VATRateChangesReport ();
                            } else if (rbnRAMResetsReport.Active) {
                                cashReceiptDriver.RAMResetsReport ();
                            }
                        });
                });
        }

        [GLib.ConnectBefore]
        private void txtDateFrom_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type != Gdk.EventType.TwoButtonPress)
                return;
            
            object ret = txtDateFrom.Text;
            if (FormHelper.ChooseDataFieldValue (DataField.OperationDate, ref ret) != ResponseType.Ok)
                return;

            txtDateFrom.Text = (string) ret;
        }

        [GLib.ConnectBefore]
        private void txtDateFrom_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (!KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                return;
            
            object ret = txtDateFrom.Text;
            if (FormHelper.ChooseDataFieldValue (DataField.OperationDate, ref ret) != ResponseType.Ok)
                return;

            txtDateFrom.Text = (string) ret;
            args.RetVal = true;
        }

        [GLib.ConnectBefore]
        private void txtDateTo_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type != Gdk.EventType.TwoButtonPress)
                return;
            
            object ret = txtDateTo.Text;
            if (FormHelper.ChooseDataFieldValue (DataField.OperationDate, ref ret) != ResponseType.Ok)
                return;

            txtDateTo.Text = (string) ret;
        }

        [GLib.ConnectBefore]
        private void txtDateTo_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (!KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                return;
            
            object ret = txtDateTo.Text;
            if (FormHelper.ChooseDataFieldValue (DataField.OperationDate, ref ret) != ResponseType.Ok)
                return;

            txtDateTo.Text = (string) ret;
            args.RetVal = true;
        }

        [UsedImplicitly]
        protected void btnDateReport_Clicked (object o, EventArgs args)
        {
            DateTime dtStart = BusinessDomain.GetDateValue (txtDateFrom.Text);
            if (dtStart == DateTime.MinValue) {
                txtDateFrom.SelectRegion (0, txtDateFrom.Text.Length);
                return;
            }

            DateTime dtEnd = BusinessDomain.GetDateValue (txtDateTo.Text);
            if (dtEnd == DateTime.MinValue) {
                txtDateTo.SelectRegion (0, txtDateTo.Text.Length);
                return;
            }

            if (dtEnd < dtStart) {
                MessageError.ShowDialog (
                    Translator.GetString ("The end date must be after the start date!"),
                    ErrorSeverity.Error);
                return;
            }

            FormHelper.TryReceiptPrinterCommand (delegate
                {
                    BusinessDomain.DeviceManager.TryDeviceCommand (delegate
                        {
                            if (rbnDateReport.Active) {
                                cashReceiptDriver.ShortFMReportByDates (dtStart, dtEnd);
                            } else if (rbnDateDetailReport.Active) {
                                cashReceiptDriver.DetailFMReportByDates (dtStart, dtEnd);
                            }
                        });
                });
        }

        [UsedImplicitly]
        protected void btnNumberReport_Clicked (object o, EventArgs args)
        {
            int numStart;
            if (!int.TryParse (txtNumberFrom.Text, out numStart)) {
                txtNumberFrom.SelectRegion (0, txtNumberFrom.Text.Length);
                return;
            }

            int numEnd;
            if (!int.TryParse (txtNumberTo.Text, out numEnd)) {
                txtNumberTo.SelectRegion (0, txtNumberTo.Text.Length);
                return;
            }

            if (numEnd < numStart) {
                MessageError.ShowDialog (
                    Translator.GetString ("The final number must be after the start number!"),
                    ErrorSeverity.Error);
                return;
            }

            FormHelper.TryReceiptPrinterCommand (delegate
                {
                    BusinessDomain.DeviceManager.TryDeviceCommand (delegate
                        {
                            if (rbnNumberReport.Active) {
                                cashReceiptDriver.ShortFMReportByNumbers (numStart, numEnd);
                            } else if (rbnNumberDetailReport.Active) {
                                cashReceiptDriver.DetailFMReportByNumbers (numStart, numEnd);
                            }
                        });
                });
        }

        #endregion
    }
}
