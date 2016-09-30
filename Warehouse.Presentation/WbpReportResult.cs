//
// WbpReportResult.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   03/27/2006
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
using System.Globalization;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Presentation.Visualization;
using HBox = Gtk.HBox;

namespace Warehouse.Presentation
{
    public class WbpReportResult : WbpBase
    {
        private readonly DataQueryResult qSetObject;
        private string title;
        private DataQueryVisualizer visualizer;
        private readonly VisualizerSettingsCollection visualizerSettings = new VisualizerSettingsCollection ();

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private HBox hboReportResultRoot;
        [Widget]
        private Alignment algGrid;
        [Widget]
        private Button btnClose;
        [Widget]
        private Button btnPrint;
        [Widget]
        private Button btnExport;
        [Widget]
        private ToggleButton btnSum;
        [Widget]
        private Button btnView;
        [Widget]
        private Button btnRefresh;
        [Widget]
        private Label lblRows;
        [Widget]
        private Label lblRowsValue;
        [Widget]
        private Alignment algReportResultIcon;

#pragma warning restore 649

        #endregion

        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        public string ReportTypeName { get; set; }

        public string HelpFile
        {
            get { return "ReportResult.html"; }
        }

        protected override bool PersistGridsState
        {
            get { return false; }
        }

        public WbpReportResult ()
        {
            InitializeForm ();
        }

        public WbpReportResult (DataQueryResult querySet)
        {
            qSetObject = querySet;
            InitializeForm ();
        }

        private void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("WbpReportResult.glade", "hboReportResultRoot");
            form.Autoconnect (this);

            btnClose.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            btnPrint.SetChildImage (FormHelper.LoadImage ("Icons.Print24.png"));
            btnExport.SetChildImage (FormHelper.LoadImage ("Icons.Export24.png"));
            btnSum.SetChildImage (FormHelper.LoadImage ("Icons.Sum24.png"));
            btnView.SetChildImage (FormHelper.LoadImage ("Icons.Visualizer24.png"));
            btnRefresh.SetChildImage (FormHelper.LoadImage ("Icons.Refresh24.png"));

            Image icon = FormHelper.LoadImage ("Icons.Report32.png");
            algReportResultIcon.Add (icon);
            icon.Show ();

            Add (hboReportResultRoot);
            hboReportResultRoot.Show ();
            hboReportResultRoot.KeyPressEvent += WbpReportResult_KeyPressEvent;
            OuterKeyPressed += WbpReportResult_KeyPressEvent;

            btnPrint.Clicked += btnPrint_Clicked;
            btnExport.Clicked += btnExport_Clicked;
            btnSum.Toggled += btnSum_Toggled;
            btnView.Clicked += btnView_Clicked;
            lblRows.Visible = false;

            SetVisualizer (new TableVisualizer ());

            InitializeStrings ();
            visualizer.Initialize (qSetObject, visualizerSettings);

            BusinessDomain.FeedbackProvider.TrackEvent ("Report", ReportTypeName);
        }

        private void WbpReportResult_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (!KeyShortcuts.Equal (args.Event, KeyShortcuts.HelpKey))
                return;

            if (string.IsNullOrEmpty (HelpFile))
                return;

            FormHelper.ShowWindowHelp (HelpFile);
        }

        private void visualizer_Initialized (object sender, EventArgs e)
        {
            btnPrint.Visible = visualizer.SupportsPrinting && BusinessDomain.AppConfiguration.IsPrintingAvailable ();
            btnExport.Visible = visualizer.SupportsExporting && BusinessDomain.DataExporters.Count > 0;
            btnSum.Visible = visualizer.SupportsSumming;
            btnView.Visible = VisualizerSettingsDialog.SettingsNodes.Count > 1;

            btnClose.Sensitive = btnPrint.Sensitive = btnExport.Sensitive = btnSum.Sensitive = btnView.Sensitive = btnRefresh.Sensitive = lblRows.Visible = true;

            lblRowsValue.SetText (visualizer.Model.Count.ToString (CultureInfo.InvariantCulture));
        }

        private void InitializeStrings ()
        {
            btnClose.SetChildLabelText (Translator.GetString ("Close"));
            btnPrint.SetChildLabelText (Translator.GetString ("Document"));
            btnExport.SetChildLabelText (Translator.GetString ("Export"));
            btnSum.SetChildLabelText (Translator.GetString ("Totals"));
            btnView.SetChildLabelText (Translator.GetString ("View"));
            btnRefresh.SetChildLabelText (Translator.GetString ("Refresh"));
            lblRows.SetText (Translator.GetString ("Rows"));
            lblRowsValue.SetText (string.Empty);
        }

        [UsedImplicitly]
        public void btnClose_Clicked (object o, EventArgs args)
        {
            OnPageClose ();
        }

        private void btnPrint_Clicked (object sender, EventArgs e)
        {
            Report report = visualizer.GetPrintData (Title);
            report.SetName (string.Format ("{0} - {1}", Title, Translator.GetString ("Document")));
            FormHelper.PrintPreviewObject (report);
        }

        private void btnExport_Clicked (object sender, EventArgs e)
        {
            FormHelper.ExportData ("report", ReportTypeName ?? title, visualizer.GetExportData (title));
        }

        private void btnSum_Toggled (object sender, EventArgs e)
        {
            if (btnSum.Active)
                visualizer.ShowTotals ();
            else
                visualizer.HideTotals ();
        }

        private void btnView_Clicked (object sender, EventArgs e)
        {
            using (VisualizerSettingsDialog settingsDialog = new VisualizerSettingsDialog (qSetObject, visualizerSettings)) {
                if (settingsDialog.Run () != ResponseType.Ok)
                    return;
            }

            DataQueryVisualizer vis;
            if (visualizer != null) {
                vis = visualizerSettings.GetVisualizerType () == visualizer.GetType () ? visualizer : visualizerSettings.GetVisualizerInstance ();
            } else {
                vis = visualizerSettings.GetVisualizerInstance ();
            }

            SetVisualizer (vis);
            vis.Initialize (qSetObject, visualizerSettings);
        }

        private void SetVisualizer (DataQueryVisualizer vis)
        {
            if (algGrid.Children.Length > 0) {
                Widget child = algGrid.Child;
                if (ReferenceEquals (child, vis))
                    return;

                DataQueryVisualizer oldVisualizer = child as DataQueryVisualizer;
                if (oldVisualizer != null)
                    oldVisualizer.Initialized -= visualizer_Initialized;

                algGrid.Remove (child);
                child.Destroy ();
            }

            visualizer = null;
            if (vis == null)
                return;

            algGrid.Add (vis);
            vis.Show ();
            visualizer = vis;
            visualizer.Initialized += visualizer_Initialized;
        }

        [UsedImplicitly]
        private void btnRefresh_Clicked (object sender, EventArgs e)
        {
            visualizer.Refresh ();
        }

        protected override void OnPageClose ()
        {
            if (qSetObject != null && qSetObject.Result != null)
                qSetObject.Result.Dispose ();

            base.OnPageClose ();
        }

        #region WorkBookPage Members

        public override ViewProfile ViewProfile
        {
            get
            {
                return ViewProfile.GetByName ("ReportResult") ?? new ViewProfile
                    {
                        Name = "ReportResult"
                    };
            }
        }

        protected override string PageDescription
        {
            get { return null; }
        }

        public override string PageTitle
        {
            get { return title; }
        }

        #endregion
    }
}
