//
// TableVisualizer.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   06/21/2009
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
using System.Data;
using Gtk;
using Pango;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.Model;
using Image = Gtk.Image;
using Item = Warehouse.Business.Entities.Item;
using Table = Gtk.Table;

namespace Warehouse.Presentation.Visualization
{
    public class TableVisualizer : DataQueryVisualizer
    {
        private readonly ScrolledWindow scrolledWindow;
        private readonly ListView grid;
        private readonly Image imgLoading;
        private readonly Label lblNoData;
        private readonly Label lblError;
        private readonly Table tblCalculating;
        private readonly ProgressBar prgCalculating;
        private readonly object listChangedSync = new object ();
        private DataQueryResult dataQueryResult;
        private TableVisualizerSettings currentSettings;
        private List<DbField> skip;
        private LazyTableModel model;
        private bool initialized;
        private bool totalsCalculated;
        private bool listReset;
        private bool listChangedRized;
        private bool supportsSumming;
        private DateTime? loadingStart;
        private TimeSpan? lastLoadTime;

        public override LazyTableModel Model
        {
            get { return model; }
        }

        public override VisualizerSettingsBase CurrentSettings
        {
            get { return currentSettings; }
        }

        public override bool TotalsShown
        {
            get { return grid.FooterVisible; }
        }

        public int MaxRowsInWindow
        {
            get { return grid.RowsInView; }
        }

        public override bool SupportsSumming
        {
            get { return supportsSumming; }
        }

        public override bool SupportsPrinting
        {
            get { return true; }
        }

        public override bool SupportsExporting
        {
            get { return true; }
        }

        public override event EventHandler Initialized;

        public TableVisualizer ()
        {
            grid = new ListView ();
            grid.Show ();

            scrolledWindow = new ScrolledWindow { HscrollbarPolicy = PolicyType.Automatic, VscrollbarPolicy = PolicyType.Automatic };
            scrolledWindow.Add (grid);
            scrolledWindow.Show ();

            imgLoading = FormHelper.LoadAnimation ("Icons.Loading66.gif");
            imgLoading.Show ();

            lblNoData = new Label { Text = Translator.GetString ("No data available.") };
            lblNoData.Show ();

            lblError = new Label { Ellipsize = EllipsizeMode.End, Xalign = 0.5f };
            lblError.Show ();

            tblCalculating = new Table (4, 3, false) { RowSpacing = 3 };
            VBox daSpace = new VBox ();
            daSpace.Show ();
            tblCalculating.Attach (daSpace, 0, 3, 0, 1,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, 0, 0);

            daSpace = new VBox ();
            daSpace.Show ();
            tblCalculating.Attach (daSpace, 0, 3, 3, 4,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, 0, 0);

            daSpace = new VBox ();
            daSpace.Show ();
            tblCalculating.Attach (daSpace, 0, 1, 1, 3,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            daSpace = new VBox ();
            daSpace.Show ();
            tblCalculating.Attach (daSpace, 2, 3, 1, 3,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            Label lblMessage = new Label { Markup = new PangoStyle { Bold = true, Text = Translator.GetString ("Calculating totals in progress...") }, Xalign = 0 };
            lblMessage.Show ();
            tblCalculating.Attach (lblMessage, 1, 2, 1, 2,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            prgCalculating = new ProgressBar ();
            prgCalculating.Show ();
            tblCalculating.Attach (prgCalculating, 1, 2, 2, 3,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);
        }

        private bool initializing;

        public override void Initialize (DataQueryResult query, VisualizerSettingsCollection settings, bool preserveSort = true)
        {
            if (query == null)
                throw new ArgumentNullException ("query");
            if (settings == null)
                throw new ArgumentNullException ("settings");

            try {
                if (initializing)
                    return;

                initializing = true;
                initialized = false;
                currentSettings = settings.GetSettings<TableVisualizerSettings> () ?? new TableVisualizerSettings ();
                skip = new List<DbField> (currentSettings.SkippedColumns);

                if (dataQueryResult != null)
                    dataQueryResult.Result.ListChanged -= OnModelListChanged;

                dataQueryResult = query;
                if (model != null && preserveSort)
                    dataQueryResult.Result.Sort (model.SortColumn);
                dataQueryResult.Result.ListChanged += OnModelListChanged;

                LazyTableModel oldModel = model;
                model = dataQueryResult.Result;
                model.SortChanged += sortModel_SortChanged;

                if (oldModel != null && !ReferenceEquals (oldModel, model)) {
                    oldModel.SortChanged -= sortModel_SortChanged;
                    oldModel.Dispose ();
                }

                listReset = true;
                listChangedRized = false;
                if (!dataQueryResult.Result.Start ()) {
                    // Already started and possibly finished
                    OnModelListChanged (query.Result, new ListChangedEventArgs (ListChangedType.Reset, query.Result.Count - 1));
                } else
                    SwitchToWidget (imgLoading);
            } finally {
                initializing = false;
            }
        }

        private void sortModel_SortChanged (object sender, SortChangedEventArgs e)
        {
            OnSortChanged (e);
        }

        public override void Refresh ()
        {
            listReset = true;
            listChangedRized = false;
            if (currentSettings != null)
                currentSettings.ShowTotals = TotalsShown;

            dataQueryResult.Result.Restart ();
            SwitchToWidget (imgLoading);
        }

        private Exception exception;

        private void OnModelListChanged (object sender, ListChangedEventArgs e)
        {
            if (listChangedRized)
                return;

            exception = null;
            try {
                if (((e.ListChangedType != ListChangedType.Reset) || (model.Count - 1 != e.NewIndex)) &&
                    ((e.ListChangedType != ListChangedType.ItemAdded)))
                    return;
            } catch (Exception ex) {
                exception = ex;
            }

            listChangedRized = true;
            GLib.Timeout.Add (0, ListChanged_GlibHandler);
        }

        private bool ListChanged_GlibHandler ()
        {
            if (!listChangedRized)
                return false;

            lock (listChangedSync) {
                listReset = false;
                if (exception != null)
                    ShowErrorWidget ();
                else
                    OnListChanged ();
            }

            return false;
        }

        private void OnListChanged ()
        {
            if (BusinessDomain.AppConfiguration == null)
                return;

            InitializeGrid ();

            ShowDataWidget ();

            totalsCalculated = false;
            if (currentSettings != null && currentSettings.ShowTotals)
                ShowTotals ();
            else
                HideTotals ();
        }

        private void InitializeGrid ()
        {
            try {
                if (initialized)
                    return;

                ColumnController cc = new ColumnController ();
                supportsSumming = false;

                for (int i = 0; i < dataQueryResult.Result.Columns.Count; i++) {
                    DbField field = dataQueryResult.Columns [i].Field;
                    DataType fieldType = ReportProvider.GetDataFieldType (field);
                    string columnName = dataQueryResult.Result.Columns [i];
                    string columnHeaderText = ReportProvider.GetReportFieldColumnName (dataQueryResult, i);

                    CellText cell;
                    bool thisColumnSummable = false;
                    switch (fieldType) {
                        case DataType.Date:
                            cell = new CellTextDate (columnName);
                            break;

                        case DataType.DateTime:
                            cell = new CellTextDateTime (columnName);
                            break;

                        case DataType.Quantity:
                            cell = new CellTextQuantity (columnName);
                            thisColumnSummable = true;
                            break;

                        case DataType.CurrencyIn:
                            cell = new CellTextCurrency (columnName, PriceType.Purchase);
                            thisColumnSummable = true;
                            break;

                        case DataType.CurrencyOut:
                            cell = new CellTextCurrency (columnName);
                            thisColumnSummable = true;
                            break;

                        case DataType.Currency:
                            cell = new CellTextCurrency (columnName, PriceType.Unknown);
                            thisColumnSummable = true;
                            break;

                        case DataType.Percent:
                            cell = new CellTextDouble (columnName) { FixedFaction = BusinessDomain.AppConfiguration.PercentPrecision };
                            break;

                        case DataType.Id:
                        case DataType.UserId:
                            cell = new CellTextNumber (columnName);
                            break;

                        case DataType.DocumentNumber:
                            cell = new CellTextNumber (columnName) { FixedDigits = BusinessDomain.AppConfiguration.DocumentNumberLength };
                            break;

                        case DataType.OperationType:
                            cell = new CellTextLookup<int> (columnName);
                            CellTextLookup<int> cellOperationType = (CellTextLookup<int>) cell;
                            foreach (OperationType operationType in Enum.GetValues (typeof (OperationType)))
                                if (operationType > 0)
                                    cellOperationType.Lookup.Add ((int) operationType, Translator.GetOperationTypeName (operationType));
                            break;

                        case DataType.DocumentType:
                            cell = new CellTextLookup<int> (columnName).Load (DocumentBase.GetAllDocumentTypes ());
                            break;

                        case DataType.BasePaymentType:
                            cell = new CellTextLookup<int> (columnName).Load (PaymentType.GetAllBaseTypePairs ());
                            break;

                        case DataType.PaymentType:
                            cell = new CellTextLookup<long> (columnName);
                            CellTextLookup<long> cellPaymentType = (CellTextLookup<long>) cell;
                            foreach (PaymentType paymentType in PaymentType.GetAll ())
                                cellPaymentType.Lookup.Add (paymentType.Id, paymentType.Name);
                            break;

                        case DataType.PriceGroupType:
                            cell = new CellTextLookup<int> (columnName).Load (Currency.GetAllPriceGroups ());
                            break;

                        case DataType.PartnerType:
                            cell = new CellTextLookup<int> (columnName).Load (Partner.GetAllTypes ());
                            break;

                        case DataType.ItemType:
                            cell = new CellTextLookup<int> (columnName).Load (Item.GetAllTypes ());
                            break;

                        case DataType.UserAccessLevel:
                            cell = new CellTextLookup<int> (columnName).Load (User.GetAllAccessLevels ());
                            break;

                        case DataType.TurnoverType:
                            cell = new CellTextLookup<int> (columnName).Load (CashBookEntry.GetAllTurnoverTypes ());
                            break;

                        case DataType.TurnoverDirection:
                            cell = new CellTextLookup<int> (columnName).Load (CashBookEntry.GetAllTurnoverDirections ());
                            break;

                        case DataType.TaxGroupCode:
                            cell = new CellTextLookup<string> (columnName).Load (VATGroup.AllCodes);
                            break;

                        case DataType.Sign:
                            cell = new CellTextLookup<int> (columnName).Load (Payment.GetAllSignTypes ());
                            break;

                        case DataType.PaymentMode:
                            cell = new CellTextLookup<int> (columnName).Load (Payment.GetAllModeTypes ());
                            break;

                        case DataType.Text:
                            cell = new CellText (columnName);
                            break;

                        default:
                            continue;
                    }
                    Column col = new Column (columnHeaderText, cell, 0.1, columnName)
                        {
                            MinWidth = 100,
                            Visible = !skip.Contains (field) && CheckColumnVisible (dataQueryResult, i)
                        };
                    cc.Add (col);
                    supportsSumming |= thisColumnSummable && col.Visible;
                }

                grid.ColumnController = cc;
                // Prevent the grid from reapplying the old sort
                grid.Model = null;
                grid.Model = model;
                grid.AllowSelect = true;
                grid.AllowMultipleSelect = true;
                grid.CellsFucusable = true;
                grid.RulesHint = true;
                grid.SortColumnsHint = true;
                grid.RowActivated -= grid_RowActivated;
                grid.RowActivated += grid_RowActivated;
                initialized = true;
            } finally {
                EventHandler onInitialized = Initialized;
                if (onInitialized != null)
                    onInitialized (this, EventArgs.Empty);
            }
        }

        private void grid_RowActivated (object o, Data.RowActivatedArgs args)
        {
            DbTable? table = dataQueryResult.Table;
            if (table == null)
                return;

            DataField [] idFields = dataQueryResult.IdFields;
            if (idFields == null || idFields.Length == 0)
                return;

            SourceItemId id = new SourceItemId (
                new RowReader (dataQueryResult.Result [args.Row], dataQueryResult.Columns),
                table, idFields);

            id.AddConstants (dataQueryResult.IdConstants);
            BusinessDomain.FeedbackProvider.TrackEvent ("Feature", "Open Entity from Report");

            EntityEditor.OpenEntityForEdit (id);
        }

        private void ShowErrorWidget ()
        {
            try {
                if (initializing)
                    return;

                initializing = true;
                if (exception != null) {
                    lblError.Text = string.Format (Translator.GetString (
                        "An error occurred while retrieving data from the database: {0}\"{1}\""), Environment.NewLine, exception.Message);
                    SwitchToWidget (lblError);
                }
            } finally {
                initializing = false;
            }
        }

        private void ShowDataWidget ()
        {
            try {
                if (initializing)
                    return;

                initializing = true;
                if (model.Count == 0)
                    SwitchToWidget (lblNoData);
                else
                    SwitchToWidget (scrolledWindow);
            } finally {
                initializing = false;
            }
        }

        private void SwitchToWidget (Widget newWidget)
        {
            if (ReferenceEquals (newWidget, imgLoading)) {
                loadingStart = DateTime.Now;
                if (lastLoadTime != null && lastLoadTime < TimeSpan.FromMilliseconds (MINIMAL_INTERVAL_FOR_LOADING_ANIMATION))
                    return;
            } else if (loadingStart != null) {
                lastLoadTime = DateTime.Now - loadingStart;
                loadingStart = null;
            }

            if (Children.Length > 0) {
                if (!ReferenceEquals (Children [0], newWidget)) {
                    Remove (Children [0]);
                    Add (newWidget);
                }
            } else
                Add (newWidget);

            if (!Sensitive)
                return;

            newWidget.Sensitive = false;
            newWidget.Sensitive = true;
        }

        public override Report GetPrintData (string title)
        {
            return new Report
                {
                    ReportDetails = grid.ToDataTable (true),
                    ReportHasFooter = TotalsShown,
                    ReportDate = BusinessDomain.GetFormattedDate (BusinessDomain.Now),
                    ReportName = title
                };
        }

        public override DataExchangeSet GetExportData (string title)
        {
            DataType [] columnTypes = new DataType [dataQueryResult.Result.Columns.Count];
            for (int i = 0; i < dataQueryResult.Result.Columns.Count; i++) {
                DbField field = dataQueryResult.Columns [i].Field;
                columnTypes [i] = ReportProvider.GetDataFieldType (field);
            }

            return new DataExchangeSet (title, grid.ToDataTable (false), TotalsShown, columnTypes);
        }

        public override void ShowTotals ()
        {
            if (!initialized)
                throw new ApplicationException ("Visualizer not initialized.");

            if (totalsCalculated) {
                grid.FooterVisible = true;
                return;
            }

            Dictionary<int, int> indexes = new Dictionary<int, int> ();
            List<double> sums = new List<double> ();

            for (int i = 0; i < model.Columns.Count; i++) {
                DbField field = dataQueryResult.Columns [i].Field;
                if (skip.Contains (field))
                    continue;

                switch (ReportProvider.GetDataFieldType (field)) {
                    case DataType.Quantity:
                    case DataType.CurrencyIn:
                    case DataType.CurrencyOut:
                    case DataType.Currency:
                        indexes.Add (i, sums.Count);
                        sums.Add (0);
                        break;
                }
            }

            // If there are many rows to be calculated show progress message else calculate directly
            int total = model.Count;
            try {
                if (total > 10000) {
                    SwitchToWidget (tblCalculating);

                    for (int i = 0; i < total; i++) {
                        foreach (KeyValuePair<int, int> pair in indexes) {
                            if (listReset)
                                return;
                            sums [pair.Value] += (double) (dataQueryResult.Result [i] [pair.Key] ?? 0d);
                        }

                        if (i % 1000 != 0)
                            continue;

                        tblCalculating.Show ();
                        double val = Math.Min ((double) i / total, 1);
                        val = Math.Max (val, 0d);

                        prgCalculating.Fraction = val;
                        prgCalculating.Text = string.Format (Translator.GetString ("{0} of {1}"), i, total);
                        PresentationDomain.ProcessUIEvents ();
                    }

                    ShowDataWidget ();
                } else {
                    for (int i = 0; i < total; i++) {
                        foreach (KeyValuePair<int, int> pair in indexes) {
                            if (listReset)
                                return;
                            sums [pair.Value] += (double) (dataQueryResult.Result [i] [pair.Key] ?? 0d);
                        }
                    }
                }
            } catch (ArgumentOutOfRangeException) {
                return;
            }

            for (int i = 0; i < dataQueryResult.Result.Columns.Count; i++) {
                int index;
                if (!indexes.TryGetValue (i, out index))
                    continue;

                CellTextFooter footer;
                Column column = grid.ColumnController [i];
                column.FooterValue = sums [index];

                DbField field = dataQueryResult.Columns [i].Field;
                DataType fieldType = ReportProvider.GetDataFieldType (field);
                switch (fieldType) {
                    case DataType.Quantity:
                        column.FooterText = Quantity.ToString (sums [index]);
                        footer = (CellTextFooter) column.FooterCell;
                        footer.Alignment = Pango.Alignment.Right;
                        break;

                    case DataType.CurrencyIn:
                        column.FooterText = Currency.ToString (sums [index], PriceType.Purchase);
                        footer = (CellTextFooter) column.FooterCell;
                        footer.Alignment = Pango.Alignment.Right;
                        break;

                    case DataType.CurrencyOut:
                        column.FooterText = Currency.ToString (sums [index]);
                        footer = (CellTextFooter) column.FooterCell;
                        footer.Alignment = Pango.Alignment.Right;
                        break;

                    case DataType.Currency:
                        column.FooterText = Currency.ToString (sums [index], PriceType.Unknown);
                        footer = (CellTextFooter) column.FooterCell;
                        footer.Alignment = Pango.Alignment.Right;
                        break;
                }
            }

            grid.FooterVisible = true;
            totalsCalculated = true;
        }

        public override void HideTotals ()
        {
            grid.FooterVisible = false;
        }

        public override void Dispose ()
        {
            base.Dispose ();

            model = null;
        }
    }
}
