//
// ReportFilterDateTimeRange.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   03.25.2010
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
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Reporting;
using Warehouse.Data;

namespace Warehouse.Presentation.Reporting
{
    public class ReportFilterDateTimeRange : ReportFilterDateRange
    {
        private readonly SpinButton spbHoursTo;
        private readonly SpinButton spbMinutesTo;
        private readonly SpinButton spbHoursFrom;
        private readonly SpinButton spbMinutesFrom;

        public ReportFilterDateTimeRange (bool alwaysOn, bool enabled, DataFilterLabel fLabel, params DbField [] fNames)
            : this (new FilterDateTimeRange (alwaysOn, enabled, fLabel, fNames))
        {
        }

        public ReportFilterDateTimeRange (FilterDateTimeRange filter)
            : base (filter)
        {
            spbHoursFrom = new SpinButton (0, 23, 1) { WidthChars = 2, Numeric = true, Xalign = 1 };
            spbHoursFrom.ValueChanged += spbHoursFrom_ValueChanged;
            textWidgets.PackStart (spbHoursFrom, false, false, 4);
            textWidgets.ReorderChild (spbHoursFrom, 2);

            textWidgets.ReorderChild (AddTimeSeparator (textWidgets), 3);

            spbMinutesFrom = new SpinButton (0, 59, 1) { WidthChars = 2, Numeric = true, Xalign = 1 };
            spbMinutesFrom.ValueChanged += spbMinutesFrom_ValueChanged;
            textWidgets.PackStart (spbMinutesFrom, false, false, 0);
            textWidgets.ReorderChild (spbMinutesFrom, 4);

            spbHoursTo = new SpinButton (0, 23, 1) { Value = 23, WidthChars = 2, Numeric = true, Xalign = 1 };
            spbHoursTo.ValueChanged += spbHoursTo_ValueChanged;
            textWidgets.PackStart (spbHoursTo, false, false, 6);

            AddTimeSeparator (textWidgets);

            spbMinutesTo = new SpinButton (0, 59, 1) { Value = 59, WidthChars = 2, Numeric = true, Xalign = 1 };
            spbMinutesTo.ValueChanged += spbMinutesTo_ValueChanged;
            textWidgets.PackStart (spbMinutesTo, false, false, 0);
            filter.ToTime = new TimeSpan (23, 59, 0);
        }

        private void spbHoursFrom_ValueChanged (object sender, EventArgs e)
        {
            OnTimeChanged (spbHoursFrom);
        }

        private void spbMinutesFrom_ValueChanged (object sender, EventArgs e)
        {
            OnTimeChanged (spbMinutesFrom);
        }

        private void spbHoursTo_ValueChanged (object sender, EventArgs e)
        {
            OnTimeChanged (spbHoursTo);
        }

        private void spbMinutesTo_ValueChanged (object sender, EventArgs e)
        {
            OnTimeChanged (spbMinutesTo);
        }

        private void OnTimeChanged (SpinButton spbDate)
        {
            if (changingFilter)
                return;

            if (!spbDate.Sensitive)
                return;

            int cursorPosition = spbDate.CursorPosition;
            OnFilterChanged (EventArgs.Empty);
            spbDate.GrabFocus ();
            spbDate.SelectRegion (cursorPosition, cursorPosition);
        }

        private static Widget AddTimeSeparator (Box hbox)
        {
            Alignment algTimeFromSeparator = new Alignment (0, 0, 1, 1)
                {
                    RightPadding = 4,
                    Child = new Label (CultureInfo.CurrentCulture.DateTimeFormat.TimeSeparator)
                };
            hbox.PackStart (algTimeFromSeparator, false, false, 4);
            return algTimeFromSeparator;
        }

        public override DataFilter GetDataFilter ()
        {
            base.GetDataFilter ();
            FilterDateTimeRange f = (FilterDateTimeRange) filter;
            f.SetData (f.FromDate.Add (new TimeSpan (spbHoursFrom.ValueAsInt, spbMinutesFrom.ValueAsInt, 0)),
                f.ToDate.Add (new TimeSpan (spbHoursTo.ValueAsInt, spbMinutesTo.ValueAsInt, 0)),
                f.TimeRange);

            return f.GetDataFilter ();
        }

        protected override void OnFilterChanging ()
        {
            base.OnFilterChanging ();

            FilterDateTimeRange f = (FilterDateTimeRange) filter;
            spbHoursFrom.Value = f.FromTime.Hours;
            spbMinutesFrom.Value = f.FromTime.Minutes;
            spbHoursTo.Value = f.ToTime.Hours;
            spbMinutesTo.Value = f.ToTime.Minutes;
        }

        public override void ClearFilter ()
        {
            FilterDateTimeRange f = (FilterDateTimeRange) filter;
            f.SetData (f.FromDate.Add (new TimeSpan (spbHoursFrom.ValueAsInt, spbMinutesFrom.ValueAsInt, 0)),
                f.ToDate.Add (new TimeSpan (spbHoursTo.ValueAsInt, spbMinutesTo.ValueAsInt, 0)),
                f.TimeRange);
            
            base.ClearFilter ();

            txtFrom.Text = filter.FromDate == DateTime.MinValue ?
                string.Empty : BusinessDomain.GetFormattedDate (filter.FromDate);
            txtTo.Text = filter.ToDate == DateTime.MinValue ?
                string.Empty : BusinessDomain.GetFormattedDate (filter.ToDate);
        }
    }
}
