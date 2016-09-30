//
// ReceiptReports.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
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
using System.Timers;
using Glade;
using Gtk;
using Pango;
using Warehouse.Business;
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Business.Reporting;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Presentation.Reporting;
using Alignment = Gtk.Alignment;

namespace Warehouse.Presentation.Dialogs
{
    public class ReceiptReports : DialogBase
    {
        private readonly List<ReportFilterBase> filters = new List<ReportFilterBase> ();
        private ReceiptReport receiptReport;
        private MessageProgress exportProgress;
        private Timer filterTimer;
        private bool debounce;

        public override Dialog DialogControl
        {
            get { return dlgReceiptReports; }
        }

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgReceiptReports;
        [Widget]
        private RadioButton rbnSales;
        [Widget]
        private RadioButton rbnReturns;
        [Widget]
        private RadioButton rbnTurnover;
        [Widget]
        private RadioButton rbnOperations;
        [Widget]
        private Button btnPrint;
        [Widget]
        private Button btnClose;
        [Widget]
        private Button btnClear;
        [Widget]
        private Alignment algDialogIcon;
        [Widget]
        private Table tblFilters;
        [Widget]
        private TextView txvPreview;

#pragma warning restore 649

        #endregion

        public ReceiptReports ()
        {
            Initialize ();
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ReceiptReports.glade", "dlgReceiptReports");
            form.Autoconnect (this);

            dlgReceiptReports.Title = Translator.GetString ("Reports");

            dlgReceiptReports.Icon = FormHelper.LoadImage ("Icons.Report32.png").Pixbuf;
            btnPrint.SetChildImage (FormHelper.LoadImage ("Icons.Print24.png"));
            btnClose.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            btnClear.SetChildImage (FormHelper.LoadImage ("Icons.Clear24.png"));

            Image img = FormHelper.LoadImage ("Icons.Report32.png");
            algDialogIcon.Add (img);
            img.Show ();

            // If Sylfaen is used then we probably need it to display special characters
            // for ka and hy locales. In this cases don't modify the font in order to display
            // everything correctly
            if (txvPreview.Style.FontDescription.Family != "Sylfaen")
                txvPreview.ModifyFont (FontDescription.FromString (FormHelper.DefaultMonospaceFont));

            btnPrint.Sensitive = BusinessDomain.AppConfiguration.CustomerOrdersPrinterEnabled;

            base.InitializeForm ();

            InitializeFormStrings ();

            filterTimer = new Timer { AutoReset = false };
            filterTimer.Elapsed += (sender, args) => GLib.Timeout.Add (1, () =>
                {
                    DisplayReport ();
                    return false;
                });

            debounce = false;
            rbnSales.Toggle ();
            debounce = true;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            btnPrint.SetChildLabelText (Translator.GetString ("Print"));
            btnClose.SetChildLabelText (Translator.GetString ("Close"));
            btnClear.SetChildLabelText (Translator.GetString ("Clear"));

            rbnSales.Label = Translator.GetString ("Sales");
            rbnReturns.Label = Translator.GetString ("Returns");
            rbnTurnover.Label = Translator.GetString ("Turnover");
            rbnOperations.Label = Translator.GetString ("Operations");
        }

        #endregion

        private void AppendFilters ()
        {
            foreach (ReportFilterBase filterBase in filters)
                filterBase.FilterChanged -= Filter_FilterChanged;

            filters.Clear ();
            for (int i = tblFilters.Children.Length - 1; i >= 0; i--) {
                Widget child = tblFilters.Children [i];
                tblFilters.Remove (child);
                child.Destroy ();
            }

            AppendFilter (new ReportFilterDateTimeRange (new FilterDateTimeRange (
                true, true, receiptReport.DateLabel, receiptReport.DateField) { ClearToToday = true }));
            if (receiptReport.AllowOperatorsManage ())
                AppendUsersFilter ();

            if (filters.Count == receiptReport.Filters.Count)
                for (int i = 0; i < filters.Count; i++)
                    filters [i].SetDataFilter (receiptReport.Filters [i]);

            tblFilters.ShowAll ();
        }

        private void AppendFilter (ReportFilterBase filter)
        {
            uint rows = tblFilters.NRows;

            filters.Add (filter);

            VBox da = new VBox { WidthRequest = 4 };

            tblFilters.Attach (filter.Label, 0, 1, rows, rows + 1,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, 0, 1);
            tblFilters.Attach (da, 1, 2, rows, rows + 1);
            tblFilters.Attach (filter.EntryWidget, 2, 3, rows, rows + 1,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, 0, 1);

            filter.FilterChanged += Filter_FilterChanged;
        }

        private void AppendUsersFilter ()
        {
            List<KeyValuePair<long, string>> keyValuePairs = new List<KeyValuePair<long, string>>
                {
                    new KeyValuePair<long, string> (-1, Translator.GetString ("Any"))
                };

            int defUserIndex = -1;
            int i = 1;
            foreach (User user in User.GetAll ()) {
                if (user.Id == User.DefaultId)
                    defUserIndex = i;

                keyValuePairs.Add (new KeyValuePair<long, string> (user.Id, user.Name));
                i++;
            }

            // Remove the default user from the list if there are other users added
            if (i > 2 && defUserIndex >= 0)
                keyValuePairs.RemoveAt (defUserIndex);

            AppendFilter (new ReportFilterChooseLong (true, true, keyValuePairs.ToArray (),
                receiptReport.UserLabel, receiptReport.UserField));
        }

        private void Filter_FilterChanged (object sender, EventArgs e)
        {
            filterTimer.Stop ();
            filterTimer.Interval = debounce ? 500 : 10;
            filterTimer.Start ();
        }

        private bool ValidateFilters ()
        {
            return filters.All (filter => filter.ValidateFilter (true, true));
        }

        private void DisplayReport ()
        {
            if (!ValidateFilters ())
                return;

            receiptReport.Filters.Clear ();
            receiptReport.Filters.AddRange (filters.Select (f => f.GetDataFilter ()));
            txvPreview.Buffer.Text = receiptReport.Display (receiptReport.GetReportLines ());
        }

        #region Event handling

        [UsedImplicitly]
        private void rbtnReport_Toggled (object o, EventArgs args)
        {
            if (rbnSales.Active)
                receiptReport = new ReceiptReportSales ();
            else if (rbnReturns.Active)
                receiptReport = new ReceiptReportReturns ();
            else if (rbnTurnover.Active)
                receiptReport = new ReceiptReportTurnover ();
            else
                receiptReport = new ReceiptReportOperations ();

            AppendFilters ();
            DisplayReport ();
        }

        [UsedImplicitly]
        private void btnPrint_Clicked (object o, EventArgs args)
        {
            if (!ValidateFilters ())
                return;

            DisplayReport ();

            if (string.IsNullOrEmpty (txvPreview.Buffer.Text)) {
                using (MessageError dlgMsg = new MessageError (
                    Translator.GetString ("No matches."), "Icons.Report16.png",
                    ErrorSeverity.Information, null)) {
                    dlgMsg.Run ();
                }
            } else {
                try {
                    exportProgress = new MessageProgress (Translator.GetString ("Printing report..."), null, null);
                    exportProgress.Show ();

                    FormHelper.TryReceiptPrinterCommand (delegate
                        {
                            DeviceManagerBase devMan = BusinessDomain.DeviceManager;
                            ICashReceiptPrinterController printer = devMan.CustomerOrderPrinter as ICashReceiptPrinterController;
                            if (printer != null)
                                PrintNonFiscal (devMan, printer);
                            else {
                                IKitchenPrinterController kitchenPrinter = devMan.CustomerOrderPrinter as IKitchenPrinterController;
                                if (kitchenPrinter != null)
                                    PrintKitchen (devMan, kitchenPrinter);
                            }
                        }, false);
                } finally {
                    exportProgress.Dispose ();
                    exportProgress = null;
                }
            }
        }

        private void PrintNonFiscal (DeviceManagerBase devMan, ICashReceiptPrinterController printer)
        {
            if (!printer.SupportedCommands.Contains (DeviceCommands.OpenNonFiscal) ||
                (!printer.SupportedCommands.Contains (DeviceCommands.PrintTextNonFiscal) &&
                !printer.SupportedCommands.Contains (DeviceCommands.PrintKeyValueNonFiscal)) ||
                !printer.SupportedCommands.Contains (DeviceCommands.CloseNonFiscal))
                return;

            KeyValuePair<string, string> [] lines = receiptReport.GetReportLines ();
            txvPreview.Buffer.Text = receiptReport.Display (lines);

            devMan.TryDeviceCommand (delegate
                {
                    printer.OpenNonFiscal (false);

                    if (printer.SupportedCommands.Contains (DeviceCommands.PrintTitleNonFiscal))
                        printer.PrintTitleNonFiscal (receiptReport.Title);
                    else
                        printer.PrintTextNonFiscal (receiptReport.Title.AlignCenter (printer.NonFiscalTextCharsPerLine));
                    
                    printer.PrintTextNonFiscal (DriverBase.SEPARATOR);
                });

            if (printer.SupportedCommands.Contains (DeviceCommands.PrintKeyValueNonFiscal)) {
                for (int i = 0; i < lines.Length; i++) {
                    if (i % 5 == 0)
                        ShowProgress (((double) i * 100) / lines.Length);

                    string key = lines [i].Key;
                    string val = lines [i].Value;
                    devMan.TryDeviceCommand (() => printer.PrintKeyValueNonFiscal (key, val, " "));
                }
            } else {
                for (int i = 0; i < lines.Length; i++) {
                    if (i % 5 == 0)
                        ShowProgress (((double) i * 100) / lines.Length);

                    string key = lines [i].Key;
                    string val = lines [i].Value;
                    devMan.TryDeviceCommand (() => printer.PrintTextNonFiscal (ReceiptReport.GetReportLine (key, val, printer.NonFiscalTextCharsPerLine)));
                }
            }

            devMan.TryDeviceCommand (printer.CloseNonFiscal);
        }

        private void PrintKitchen (DeviceManagerBase devMan, IKitchenPrinterController kitchenPrinter)
        {
            if (!kitchenPrinter.SupportedCommands.Contains (DeviceCommands.PrintFreeText))
                return;

            KeyValuePair<string, string> [] lines = receiptReport.GetReportLines ();
            txvPreview.Buffer.Text = receiptReport.Display (lines);

            devMan.TryDeviceCommand (delegate
                {
                    kitchenPrinter.PrintFreeText (receiptReport.Title.AlignCenter (kitchenPrinter.TextCharsPerLine));
                    kitchenPrinter.PrintFreeText (DriverBase.SEPARATOR);

                    if (kitchenPrinter.SupportedCommands.Contains (DeviceCommands.PaperFeed))
                        kitchenPrinter.PaperFeed ();
                });

            for (int i = 0; i < lines.Length; i++) {
                if (i % 5 == 0)
                    ShowProgress (((double) i * 100) / lines.Length);

                string key = lines [i].Key;
                string val = lines [i].Value;
                devMan.TryDeviceCommand (() => kitchenPrinter.PrintFreeText (ReceiptReport.GetReportLine (key, val, kitchenPrinter.TextCharsPerLine)));
            }

            if (kitchenPrinter.SupportedCommands.Contains (DeviceCommands.PaperCut))
                devMan.TryDeviceCommand (kitchenPrinter.PaperCut);
        }

        private void ShowProgress (double progress)
        {
            PresentationDomain.Invoke (() => exportProgress.Progress = progress, true);
        }

        [UsedImplicitly]
        protected void btnClose_Clicked (object o, EventArgs args)
        {
            dlgReceiptReports.Respond (ResponseType.Cancel);
        }

        [UsedImplicitly]
        protected void btnClear_Clicked (object o, EventArgs args)
        {
            foreach (ReportFilterBase filter in filters)
                filter.ClearFilter ();

            filterTimer.Stop ();
            filterTimer.Start ();
        }

        #endregion
    }
}
