//
// FilterDateRange.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   09.30.2010
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
using Warehouse.Data;

namespace Warehouse.Business.Reporting
{
    public class FilterDateRange : FilterBase
    {
        public enum DateRanges
        {
            Custom,
            Today,
            Past24h,
            Yesterday,
            ThisWeek,
            PastWeek,
            LastWeek,
            ThisMonth,
            PastMonth,
            LastMonth,
            ThisYear,
            PastYear,
            LastYear,
        }

        protected string fromDateString;
        protected string toDateString;
        protected DateTime? fromDate;
        protected DateTime? toDate;
        protected DateRanges timeRange;

        public string FromDateString
        {
            get
            {
                return fromDateString ?? (fromDate.HasValue ?
                    (fromDateString = BusinessDomain.GetFormattedDate (fromDate.Value)) : string.Empty);
            }
            set
            {
                fromDateString = value;
                fromDate = null;
            }
        }

        public string ToDateString
        {
            get
            {
                return toDateString ?? (toDate.HasValue ?
                    (toDateString = BusinessDomain.GetFormattedDate (toDate.Value)) : string.Empty);
            }
            set
            {
                toDateString = value;
                toDate = null;
            }
        }

        public DateTime FromDate
        {
            get
            {
                return fromDate ?? (fromDate = string.IsNullOrWhiteSpace (fromDateString) ?
                    DateTime.MinValue : BusinessDomain.GetDateValue (fromDateString).Date).Value;
            }
            set
            {
                fromDate = value;
                fromDateString = null;
            }
        }

        public DateTime ToDate
        {
            get
            {
                return toDate ?? (toDate = string.IsNullOrWhiteSpace (toDateString) ?
                    DateTime.MinValue : BusinessDomain.GetDateValue (toDateString).Date).Value;
            }
            set
            {
                toDate = value;
                toDateString = null;
            }
        }

        public DateRanges TimeRange
        {
            get { return timeRange; }
        }

        public FilterDateRange (bool constantVisibility, bool columnVisible, DataFilterLabel filter, params DbField [] fNames)
            : base (constantVisibility, columnVisible, filter, fNames)
        {
            FilterLogic = DataFilterLogic.InRange;
        }

        public bool ValidateFilter (string from, string to, bool skipNulls)
        {
            bool fromValid = ValidateField (from, skipNulls);
            bool toValid = ValidateField (to, skipNulls);

            if (skipNulls) {
                if (!fromValid || !toValid)
                    return false;
            } else {
                if (!fromValid && !toValid)
                    return false;
            }

            return true;
        }

        public void SetData (string from, string to, DateRanges range = DateRanges.Custom)
        {
            fromDateString = from;
            fromDate = BusinessDomain.GetDateValue (from);
            if (fromDate.Value != DateTime.MinValue)
                fromDate = fromDate.Value.Date;
            else
                fromDate = null;
            
            toDateString = to;
            toDate = BusinessDomain.GetDateValue (to);
            if (toDate.Value != DateTime.MinValue)
                toDate = toDate.Value.Date;
            else
                toDate = null;

            timeRange = range;
        }

        public override DataFilter GetDataFilter (params object [] objects)
        {
            object fromValue = GetFieldValue (fromDateString);
            object toValue = GetFieldValue (toDateString);

            // This will allow us to include in the range values from the whole day
            if (toValue != null) {
                DateTime endDate = (DateTime) toValue;
                if (endDate.Hour == 0 && endDate.Minute == 0) {
                    endDate = endDate.AddDays (1);
                    endDate = endDate.AddSeconds (-1);
                    toValue = endDate;
                }
            }

            DataFilter dataFilter = base.GetDataFilter ();
            dataFilter.Values = new [] { fromValue, toValue, (int) timeRange };
            dataFilter.IsValid = ValidateFilter (fromDateString, toDateString, false);

            return dataFilter;
        }

        #region Overrides of FilterBase

        public override void SetDataFilter (DataFilter dataFilter)
        {
            timeRange = DateRanges.Custom;

            if (dataFilter.Values.Length > 2) {
                timeRange = (DateRanges) Enum.ToObject (typeof (DateRanges), dataFilter.Values [2]);
                SetDateRange (timeRange);
            }

            if (timeRange != DateRanges.Custom ||
                dataFilter.Values.Length < 1)
                return;

            fromDateString = GetFieldText (dataFilter.Values [0]);
            fromDate = (DateTime?) dataFilter.Values [0];

            if (dataFilter.Values.Length < 2)
                return;

            toDateString = GetFieldText (dataFilter.Values [1]);
            toDate = (DateTime?) dataFilter.Values [1];

            base.SetDataFilter (dataFilter);
        }

        public override string GetExplanation ()
        {
            switch (timeRange) {
                case DateRanges.Custom:
                    if (fromDate != null && toDate != null)
                        return string.Format (Translator.GetString ("Between \"{0}\" and \"{1}\""), fromDateString, toDateString);
                    if (fromDate != null)
                        return string.Format (Translator.GetString ("Dates starting from \"{0}\""), fromDateString);
                    if (toDate != null)
                        return string.Format (Translator.GetString ("Dates up to \"{0}\""), toDateString);
                    return Translator.GetString ("Any dates");
                case DateRanges.Today:
                    return Translator.GetString ("From today");
                case DateRanges.Past24h:
                    return Translator.GetString ("From past 24 hours");
                case DateRanges.Yesterday:
                    return Translator.GetString ("From yesterday");
                case DateRanges.ThisWeek:
                    return Translator.GetString ("From this week");
                case DateRanges.PastWeek:
                    return Translator.GetString ("From past week");
                case DateRanges.LastWeek:
                    return Translator.GetString ("From last week");
                case DateRanges.ThisMonth:
                    return Translator.GetString ("From this month");
                case DateRanges.PastMonth:
                    return Translator.GetString ("From past month");
                case DateRanges.LastMonth:
                    return Translator.GetString ("From last month");
                case DateRanges.ThisYear:
                    return Translator.GetString ("From this year");
                case DateRanges.PastYear:
                    return Translator.GetString ("From past year");
                case DateRanges.LastYear:
                    return Translator.GetString ("From last year");
                default:
                    throw new ArgumentOutOfRangeException ();
            }
        }

        #endregion

        public void SetDateRange (DateRanges dateRange)
        {
            DateTime today = BusinessDomain.Today;
            DayOfWeek dow = today.DayOfWeek;
            int dayOfWeek = dow == DayOfWeek.Sunday ? 7 : (int) dow;

            DateTime? from = null;
            DateTime? to = null;

            switch (dateRange) {
                case DateRanges.Today:
                    from = today;
                    to = today;
                    break;
                case DateRanges.Yesterday:
                    from = today.AddDays (-1);
                    to = today.AddDays (-1);
                    break;
                case DateRanges.ThisWeek:
                    from = today.AddDays (1 - dayOfWeek);
                    to = today.AddDays (7 - dayOfWeek);
                    break;
                case DateRanges.PastWeek:
                    from = today.AddDays (-7);
                    to = today;
                    break;
                case DateRanges.LastWeek:
                    from = today.AddDays (1 - 7 - dayOfWeek);
                    to = today.AddDays (7 - 7 - dayOfWeek);
                    break;
                case DateRanges.ThisMonth:
                    from = new DateTime (today.Year, today.Month, 1);
                    to = new DateTime (today.Year, today.Month, DateTime.DaysInMonth (today.Year, today.Month));
                    break;
                case DateRanges.PastMonth:
                    from = today.AddMonths (-1);
                    to = today;
                    break;
                case DateRanges.LastMonth:
                    today = today.AddMonths (-1);
                    from = new DateTime (today.Year, today.Month, 1);
                    to = new DateTime (today.Year, today.Month, DateTime.DaysInMonth (today.Year, today.Month));
                    break;
                case DateRanges.ThisYear:
                    from = new DateTime (today.Year, 1, 1);
                    to = new DateTime (today.Year, 12, DateTime.DaysInMonth (today.Year, 12));
                    break;
                case DateRanges.PastYear:
                    from = today.AddYears (-1);
                    to = today;
                    break;
                case DateRanges.LastYear:
                    today = today.AddYears (-1);
                    from = new DateTime (today.Year, 1, 1);
                    to = new DateTime (today.Year, 12, DateTime.DaysInMonth (today.Year, 12));
                    break;
            }

            if (from.HasValue) {
                fromDateString = BusinessDomain.GetFormattedDate (from.Value);
                fromDate = from;
            }

            if (to.HasValue) {
                toDateString = BusinessDomain.GetFormattedDate (to.Value);
                toDate = to;
            }
        }

        #region Overrides of FilterBase

        public override void Clear ()
        {
            base.Clear ();

            fromDate = null;
            fromDateString = string.Empty;
            toDate = null;
            toDateString = string.Empty;
            timeRange = DateRanges.Custom;
        }

        #endregion
    }
}
