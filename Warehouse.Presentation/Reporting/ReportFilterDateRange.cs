//
// ReportFilterDateRange.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/23/2006
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
using Warehouse.Business;
using Warehouse.Business.Reporting;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.Reporting
{
    public class ReportFilterDateRange : ReportFilterBase<FilterDateRange>
    {
        private readonly List<KeyValuePair<FilterDateRange.DateRanges, string>> dateRanges;
        private ComboBox cboValues;
        protected HBox textWidgets;
        protected Entry txtFrom;
        protected Entry txtTo;
        protected Button btnFrom;
        protected Button btnTo;

        public ReportFilterDateRange (bool alwaysOn, bool enabled, DataFilterLabel fLabel, params DbField [] fNames)
            : this (new FilterDateRange (alwaysOn, enabled, fLabel, fNames))
        {
        }

        public ReportFilterDateRange (FilterDateRange filter)
            : base (filter)
        {
            foreach (DbField field in filter.FilteredFields) {
                DataType type = ReportProvider.GetDataFieldType (field);

                if (type != DataType.Date && type != DataType.DateTime)
                    throw new ArgumentException (string.Format ("Filter is used on column of type {0} but is expected to be on a column of type {1}", type, DataType.Date));
            }

            dateRanges = new List<KeyValuePair<FilterDateRange.DateRanges, string>>
                {
                    new KeyValuePair<FilterDateRange.DateRanges, string> (FilterDateRange.DateRanges.Custom, Translator.GetString ("Custom Dates")), 
                    new KeyValuePair<FilterDateRange.DateRanges, string> (FilterDateRange.DateRanges.Today, Translator.GetString ("Today")), 
                    new KeyValuePair<FilterDateRange.DateRanges, string> (FilterDateRange.DateRanges.Yesterday, Translator.GetString ("Yesterday")), 
                    new KeyValuePair<FilterDateRange.DateRanges, string> (FilterDateRange.DateRanges.ThisWeek, Translator.GetString ("This Week")), 
                    new KeyValuePair<FilterDateRange.DateRanges, string> (FilterDateRange.DateRanges.PastWeek, Translator.GetString ("Past Week")), 
                    new KeyValuePair<FilterDateRange.DateRanges, string> (FilterDateRange.DateRanges.LastWeek, Translator.GetString ("Last Week")), 
                    new KeyValuePair<FilterDateRange.DateRanges, string> (FilterDateRange.DateRanges.ThisMonth, Translator.GetString ("This Month")), 
                    new KeyValuePair<FilterDateRange.DateRanges, string> (FilterDateRange.DateRanges.PastMonth, Translator.GetString ("Past Month")), 
                    new KeyValuePair<FilterDateRange.DateRanges, string> (FilterDateRange.DateRanges.LastMonth, Translator.GetString ("Last Month")), 
                    new KeyValuePair<FilterDateRange.DateRanges, string> (FilterDateRange.DateRanges.ThisYear, Translator.GetString ("This Year")), 
                    new KeyValuePair<FilterDateRange.DateRanges, string> (FilterDateRange.DateRanges.PastYear, Translator.GetString ("Past Year")), 
                    new KeyValuePair<FilterDateRange.DateRanges, string> (FilterDateRange.DateRanges.LastYear, Translator.GetString ("Last Year"))
                };

            InitializeWidgets ();

            ColumnVisible = base.filter.ColumnVisible;
        }

        private void InitializeWidgets ()
        {
            cboValues = new ComboBox ();
            cboValues.Load (dateRanges, "Key", "Value");
            cboValues.Changed += cboValues_Changed;

            textWidgets = new HBox ();

            txtFrom = new Entry { WidthRequest = 110 };
            txtFrom.Changed += txtFrom_Changed;
            txtFrom.ButtonPressEvent += txtFrom_ButtonPressEvent;
            txtFrom.KeyPressEvent += txtFrom_KeyPressEvent;
            textWidgets.PackStart (txtFrom, false, true, 0);
            btnFrom = new Button { Label = " ... " };
            btnFrom.Clicked += btnFrom_Clicked;
            textWidgets.PackStart (btnFrom, false, true, 1);

            Label separator = new Label { Text = "-", WidthRequest = 22 };
            textWidgets.PackStart (separator, true, true, 1);

            txtTo = new Entry { WidthRequest = 110 };
            txtTo.Changed += txtTo_Changed;
            txtTo.ButtonPressEvent += txtTo_ButtonPressEvent;
            txtTo.KeyPressEvent += txtTo_KeyPressEvent;
            textWidgets.PackStart (txtTo, false, true, 0);
            btnTo = new Button { Label = " ... " };
            btnTo.Clicked += btnTo_Clicked;
            textWidgets.PackStart (btnTo, false, true, 1);

            VBox vbox = new VBox { Spacing = 2 };
            vbox.PackStart (cboValues, false, true, 0);
            vbox.PackStart (textWidgets, false, true, 0);

            entry.PackStart (vbox, true, true, 1);
            cboValues_Changed (null, null);

            InitializeLabel ();
        }

        protected bool changingFilter;

        private void cboValues_Changed (object sender, EventArgs e)
        {
            try {
                changingFilter = true;
                FilterDateRange.DateRanges range = (FilterDateRange.DateRanges) cboValues.GetSelectedValue ();
                filter.SetDateRange (range);
                txtFrom.Text = filter.FromDateString;
                txtTo.Text = filter.ToDateString;

                bool isCustom = range == FilterDateRange.DateRanges.Custom;
                txtFrom.Sensitive = isCustom;
                btnFrom.Sensitive = isCustom;
                txtTo.Sensitive = isCustom;
                btnTo.Sensitive = isCustom;
            } finally {
                changingFilter = false;
            }
            OnFilterChanged (EventArgs.Empty);
        }

        #region Field completion handling

        private void txtFrom_Changed (object sender, EventArgs e)
        {
            OnDateChanged (txtFrom);
        }

        private void txtTo_Changed (object sender, EventArgs e)
        {
            OnDateChanged (txtTo);
        }

        private void OnDateChanged (Entry txtDate)
        {
            if (changingFilter)
                return;

            if (!txtDate.Sensitive)
                return;

            string value = txtDate.Text.Trim ();
            if (value.Length > 0 &&
                BusinessDomain.GetDateValue (value) == DateTime.MinValue)
                return;

            int cursorPosition = txtDate.CursorPosition;
            OnFilterChanged (EventArgs.Empty);
            txtDate.GrabFocus ();
            txtDate.SelectRegion (cursorPosition, cursorPosition);
        }

        [ConnectBefore]
        private void txtFrom_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type != Gdk.EventType.TwoButtonPress)
                return;

            ChooseDataFieldFrom (args);
        }

        [ConnectBefore]
        private void txtFrom_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (!KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                return;

            ChooseDataFieldFrom (args);
        }

        private void btnFrom_Clicked (object sender, EventArgs e)
        {
            ChooseDataFieldFrom (new SignalArgs ());
        }

        private void ChooseDataFieldFrom (SignalArgs args)
        {
            txtFrom.Text = ChooseDataFieldValue (args, txtFrom.Text);
        }

        [ConnectBefore]
        private void txtTo_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type != Gdk.EventType.TwoButtonPress)
                return;

            ChooseDataFieldTo (args);
        }

        [ConnectBefore]
        private void txtTo_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (!KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                return;

            ChooseDataFieldTo (args);
        }

        private void btnTo_Clicked (object sender, EventArgs e)
        {
            ChooseDataFieldTo (new SignalArgs ());
        }

        private void ChooseDataFieldTo (SignalArgs args)
        {
            txtTo.Text = ChooseDataFieldValue (args, txtTo.Text);
        }

        #endregion

        public override bool ValidateFilter (bool selectErrors, bool skipNulls)
        {
            if (selectErrors) {
                if (!filter.ValidateField (txtFrom.Text, skipNulls)) {
                    txtFrom.SelectRegion (0, txtFrom.Text.Length);
                } else if (!filter.ValidateField (txtTo.Text, skipNulls)) {
                    txtTo.SelectRegion (0, txtTo.Text.Length);
                }
            }

            return filter.ValidateFilter (txtFrom.Text, txtTo.Text, skipNulls);
        }

        public override void ClearFilter ()
        {
            base.ClearFilter ();

            txtFrom.Text = filter.FromDateString;
            txtTo.Text = filter.ToDateString;
            cboValues.SetSelection (dateRanges, "Key", "Value", filter.TimeRange);
        }

        public override DataFilter GetDataFilter ()
        {
            filter.SetData (txtFrom.Text, txtTo.Text, (FilterDateRange.DateRanges) cboValues.GetSelectedValue ());
            return filter.GetDataFilter ();
        }

        public override void SetDataFilter (DataFilter dataFilter)
        {
            DataFilter oldDataFilter = GetDataFilter ();
            if (Equal (oldDataFilter, dataFilter))
                return;

            try {
                changingFilter = true;
                base.SetDataFilter (dataFilter);

                if (dataFilter.Values.Length > 2)
                    cboValues.SetSelection (dateRanges, "Key", "Value", filter.TimeRange);

                if (filter.TimeRange != FilterDateRange.DateRanges.Custom)
                    return;

                txtFrom.Text = filter.FromDateString;
                txtTo.Text = filter.ToDateString;
                OnFilterChanging ();
            } finally {
                changingFilter = false;
                OnFilterChanged (EventArgs.Empty);
            }
        }

        protected virtual void OnFilterChanging ()
        {
        }
    }
}
