//
// ReportFilter.cs
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
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Reporting;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Presentation.Reporting;

namespace Warehouse.Presentation.Dialogs
{
    public class ReportFilter : DialogBase
    {
        private readonly ReportQueryBase reportQuery;
        private readonly List<ReportFilterBase> filters = new List<ReportFilterBase> ();
        private ReportOrder order;

        public override Dialog DialogControl
        {
            get { return dlgReportBase; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        public override string HelpFile
        {
            get
            {
                string helpFile = reportQuery.HelpFile;
                if (!string.IsNullOrEmpty (helpFile))
                    return helpFile;

                helpFile = reportQuery.GetType ().Name + ".html";
                helpFile = helpFile.Replace ("ReportQuery", "Report");
                return helpFile;
            }
        }

        #region Public properties

        public string Title
        {
            get { return dlgReportBase.Title; }
            set { dlgReportBase.Title = value; }
        }

        #endregion

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgReportBase;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;
        [Widget]
        private Button btnClear;
        [Widget]
        private Alignment algDialogIcon;
        [Widget]
        private Table tblFilters;

#pragma warning restore 649

        #endregion

        public ReportFilter ()
        {
            Initialize ();
        }

        public ReportFilter (ReportQueryBase report)
            : this ()
        {
            reportQuery = report;
            Title = report.Name;

            foreach (FilterBase filter in report.Filters)
                AppendFilter (filter);

            if (report.Order != null)
                AppendOrder (new ReportOrder (report.Order));
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ReportFilter.glade", "dlgReportBase");
            form.Autoconnect (this);

            dlgReportBase.Icon = FormHelper.LoadImage ("Icons.Report32.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            btnClear.SetChildImage (FormHelper.LoadImage ("Icons.Clear24.png"));

            Image img = FormHelper.LoadImage ("Icons.Report32.png");
            algDialogIcon.Add (img);
            img.Show ();

            base.InitializeForm ();

            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
            btnClear.SetChildLabelText (Translator.GetString ("Clear"));
        }

        #endregion

        public override void Show ()
        {
            tblFilters.ShowAll ();

            base.Show ();
        }

        public override ResponseType Run ()
        {
            tblFilters.ShowAll ();

            return base.Run ();
        }

        private void AppendFilter (FilterBase filter)
        {
            AppendFilter (ReportFilterBase.CreateFromFilterBase (filter));
        }

        private void AppendFilter (ReportFilterBase filter)
        {
            filters.Add (filter);

            VBox da = new VBox { WidthRequest = 4 };
            uint rows = tblFilters.NRows;

            tblFilters.Attach (filter.LabelWidget, 0, 1, rows, rows + 1,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, 0, 1);
            tblFilters.Attach (da, 1, 2, rows, rows + 1);
            tblFilters.Attach (filter.EntryWidget, 2, 3, rows, rows + 1,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, 0, 1);
            filter.ColumnVisibilityToggled += filter_ColumnVisibilityToggled;
        }

        private void AppendOrder (ReportOrder rOrder)
        {
            if (order != null)
                throw new Exception ("Only one order element can exist in the dialog.");

            uint rows = tblFilters.NRows;
            order = rOrder;

            VBox da = new VBox { WidthRequest = 4 };

            tblFilters.Attach (rOrder.LabelWidget, 0, 1, rows, rows + 1,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, 0, 1);
            tblFilters.Attach (da, 1, 2, rows, rows + 1);
            tblFilters.Attach (rOrder.EntryWidget, 2, 3, rows, rows + 1,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, 0, 1);
        }

        private void filter_ColumnVisibilityToggled (object sender, EventArgs e)
        {
            btnOK.Sensitive = CheckReportExecutable ();
        }

        private bool CheckReportExecutable ()
        {
            return !reportQuery.AllFieldsFilterable || filters.Any (filter => filter.ColumnVisible);
        }

        private bool ValidateFilters ()
        {
            return filters.All (filter => filter.ValidateFilter (true, true));
        }

        public DataQuery GetDataQuery (bool saveSettings)
        {
            DataQuery dataQuery = ReportProvider.CreateDataQuery ();

            foreach (ReportFilterBase filter in filters)
                dataQuery.Filters.Add (filter.GetDataFilter ());

            if (order != null) {
                dataQuery.OrderBy = order.OrderBy;
                dataQuery.OrderDirection = order.OrderDirection;
            }

            return reportQuery.PrepareDataQuery (dataQuery, saveSettings);
        }

        public void SetDataQuery ()
        {
            DataQuery querySet;
            if (!BusinessDomain.ReportQueryStates.TryGetValue (reportQuery.ReportType, out querySet))
                return;

            SetDataQuery (querySet);
        }

        public void SetDataQuery (DataQuery querySet)
        {
            if (filters.Count == querySet.Filters.Count)
                for (int i = 0; i < filters.Count; i++)
                    filters [i].SetDataFilter (querySet.Filters [i]);

            if (order != null)
                order.LoadOrder (order.OrderByChoices, querySet.OrderBy, querySet.OrderDirection);
        }

        public DataQueryResult ExecuteReport (bool saveSettings)
        {
            return reportQuery.ExecuteReport (GetDataQuery (saveSettings));
        }

        #region Event handling

        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (!ValidateFilters ())
                return;

            dlgReportBase.Respond (ResponseType.Ok);
        }

        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgReportBase.Respond (ResponseType.Cancel);
        }

        protected void btnClear_Clicked (object o, EventArgs args)
        {
            foreach (ReportFilterBase filter in filters) {
                filter.ClearFilter ();
            }

            if (order != null)
                order.ClearOrder ();
        }

        #endregion
    }
}
