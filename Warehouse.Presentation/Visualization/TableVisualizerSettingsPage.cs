//
// TableVisualizerSettingsPage.cs
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
using GLib;
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.Visualization
{
    [Extension ("/Warehouse/Presentation/VisualizationSettings")]
    public class TableVisualizerSettingsPage : VisualizerSettingsPageBase
    {
        private readonly Notebook nbkMain;
        private TableVisualizerSettings currentSettings;

        private Table tblColumns;
        private CheckButton chkShowTotals;
        private bool initialized;
        private readonly List<KeyValuePair<CheckButton, DbField>> buttons = new List<KeyValuePair<CheckButton, DbField>> ();

        public override Widget PageLabel
        {
            get
            {
                VBox vbx = new VBox ();
                vbx.PackStart (FormHelper.LoadImage ("Icons.TableView32.png"), false, true, 0);
                vbx.PackStart (new Label { WidthRequest = 40, HeightRequest = 1 }, false, true, 0);
                vbx.PackStart (new Label { Text = Translator.GetString ("Table") }, false, true, 0);
                vbx.ShowAll ();

                Alignment alg = new Alignment (0, 0, 1, 1) { LeftPadding = 2, RightPadding = 2, TopPadding = 2, BottomPadding = 2 };
                alg.Add (vbx);

                return alg;
            }
        }

        public override Notebook NotebookWidget
        {
            get { return nbkMain; }
        }

        public override string VisualizerTypeName
        {
            get { return typeof (TableVisualizer).AssemblyQualifiedName; }
        }

        public override VisualizerSettingsBase CurrentSettings
        {
            get
            {
                if (currentSettings == null)
                    currentSettings = new TableVisualizerSettings ();

                UpdateCurrentSettings ();

                return currentSettings;
            }
        }

        public TableVisualizerSettingsPage ()
        {
            nbkMain = new Notebook ();
            nbkMain.AppendPage (CreateDataSection (), CreateNewTabLabel (Translator.GetString ("Data")));
            nbkMain.AppendPage (CreateChartSection (), CreateNewTabLabel (Translator.GetString ("Chart")));
            nbkMain.Show ();

            Alignment alg = new Alignment (0, 0, 1, 1) { LeftPadding = 4, RightPadding = 4, TopPadding = 4, BottomPadding = 4 };
            alg.Show ();
            alg.Add (nbkMain);

            Add (alg);
        }

        private Widget CreateDataSection ()
        {
            Table tblMain = new Table (1, 1, false) { RowSpacing = 2 };
            tblMain.Attach (new Label { Markup = new PangoStyle { Bold = true, Text = Translator.GetString ("Displayed columns") } },
                0, 1, 0, 1,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Fill, 0, 2);

            tblColumns = new Table (1, 1, false) { RowSpacing = 2 };

            Alignment algColumns = new Alignment (0, 0, 1, 1) { LeftPadding = 4, RightPadding = 4, TopPadding = 4, BottomPadding = 4 };
            algColumns.Add (tblColumns);

            Viewport viewport = new Viewport { ShadowType = ShadowType.None };
            viewport.Add (algColumns);

            ScrolledWindow scw = new ScrolledWindow { VscrollbarPolicy = PolicyType.Automatic, HscrollbarPolicy = PolicyType.Never };
            scw.Add (viewport);

            tblMain.Attach (scw, 0, 1, 1, 2,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Expand | AttachOptions.Fill, 0, 2);

            Alignment alg = new Alignment (0, 0, 1, 1) { LeftPadding = 4, RightPadding = 4, TopPadding = 4, BottomPadding = 4 };
            alg.Add (tblMain);

            EventBox evbMain = new EventBox { alg };
            evbMain.ShowAll ();

            return evbMain;
        }

        private Widget CreateChartSection ()
        {
            Table tblMain = new Table (1, 1, false) { RowSpacing = 2 };
            tblMain.Show ();

            chkShowTotals = new CheckButton (Translator.GetString ("Calculate column totals"));
            chkShowTotals.Show ();
            tblMain.Attach (chkShowTotals, 0, 1, 0, 1,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Fill, 0, 2);

            VBox daSpace = new VBox ();
            daSpace.Show ();
            tblMain.Attach (daSpace, 0, 1, 1, 2,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Expand | AttachOptions.Fill, 0, 2);

            Alignment alg = new Alignment (0, 0, 1, 1) { LeftPadding = 4, RightPadding = 4, TopPadding = 4, BottomPadding = 4 };
            alg.Add (tblMain);
            alg.Show ();

            EventBox evbMain = new EventBox { alg };
            evbMain.Show ();

            return evbMain;
        }

        public override bool LoadSettings (DataQueryResult result, VisualizerSettingsCollection settings)
        {
            if (result == null)
                throw new ArgumentNullException ("result");
            if (settings == null)
                throw new ArgumentNullException ("settings");
            if (initialized)
                throw new ApplicationException ("The settings are already initialized");

            TableVisualizerSettings tableSettings = settings.GetSettings<TableVisualizerSettings> () ?? new TableVisualizerSettings ();
            chkShowTotals.Active = tableSettings.ShowTotals;
            List<DbField> skip = new List<DbField> (tableSettings.SkippedColumns);

            for (uint i = 0; i < result.Columns.Length; i++) {
                string columnName = ReportProvider.GetReportFieldColumnName (result, (int) i);
                DbField columnField = result.Columns [i].Field;

                CheckButton btn = new CheckButton (columnName);
                tblColumns.Attach (btn, 0, 1, i, i + 1,
                    AttachOptions.Expand | AttachOptions.Fill | AttachOptions.Shrink,
                    AttachOptions.Fill, 0, 0);
                btn.Active = !skip.Contains (columnField);
                btn.Show ();
                buttons.Add (new KeyValuePair<CheckButton, DbField> (btn, columnField));
            }

            currentSettings = tableSettings;
            initialized = true;

            return true;
        }

        public override bool SaveSettings (VisualizerSettingsCollection settings)
        {
            if (settings == null)
                throw new ArgumentNullException ("settings");

            UpdateCurrentSettings ();
            settings.SetSettings (currentSettings);

            return true;
        }

        private void UpdateCurrentSettings ()
        {
            List<DbField> skip = new List<DbField> ();
            for (int i = 0; i < buttons.Count; i++) {
                if (!buttons [i].Key.Active)
                    skip.Add (buttons [i].Value);
            }

            currentSettings.SkippedColumns = skip.ToArray ();
            currentSettings.ShowTotals = chkShowTotals.Active;
        }
    }
}
