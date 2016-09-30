//
// FilterDateTimeRange.cs
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
    public class FilterDateTimeRange : FilterDateRange
    {
        private string fromTimeString;
        private string toTimeString;
        private TimeSpan? fromTime;
        private TimeSpan? toTime;

        public string FromTimeString
        {
            get
            {
                return fromTimeString ?? (fromTime.HasValue ?
                    (fromTimeString = BusinessDomain.GetFormattedTime (fromTime.Value)) : string.Empty);
            }
            set
            {
                fromTimeString = value;
                fromTime = null;
            }
        }

        public string ToTimeString
        {
            get
            {
                return toTimeString ?? (toTime.HasValue ?
                    (toTimeString = BusinessDomain.GetFormattedTime (toTime.Value)) : string.Empty);
            }
            set
            {
                toTimeString = value;
                toTime = null;
            }
        }

        public TimeSpan FromTime
        {
            get
            {
                return fromTime ?? (fromTime = (string.IsNullOrWhiteSpace (fromTimeString) ?
                    new TimeSpan () : BusinessDomain.GetTimeValue (fromTimeString))).Value;
            }
            set
            {
                fromTime = value;
                fromTimeString = null;
            }
        }

        public TimeSpan ToTime
        {
            get
            {
                return toTime ?? (toTime = (string.IsNullOrWhiteSpace (toTimeString) ?
                    new TimeSpan () : BusinessDomain.GetTimeValue (toTimeString))).Value;
            }
            set
            {
                toTime = value;
                toTimeString = null;
            }
        }

        public DateTime FromDateTime
        {
            get { return FromDate.Add (FromTime); }
            set
            {
                FromDate = value.Date;
                FromTime = value.TimeOfDay;
            }
        }

        public DateTime ToDateTime
        {
            get { return ToDate.Add (ToTime); }
            set
            {
                ToDate = value.Date;
                ToTime = value.TimeOfDay;
            }
        }

        public bool ClearToToday { get; set; }

        public FilterDateTimeRange (bool columnAlwaysVisible, bool columnVisible, DataFilterLabel filter, params DbField [] fNames)
            : base (columnAlwaysVisible, columnVisible, filter, fNames)
        {
            FilterLogic = DataFilterLogic.InRange;
        }

        public void SetData (DateTime from, DateTime to, DateRanges range = DateRanges.Custom)
        {
            fromDateString = BusinessDomain.GetFormattedDate (from);
            fromTimeString = BusinessDomain.GetFormattedTime (from);
            fromDate = from.Date;
            fromTime = from.TimeOfDay;

            toDateString = BusinessDomain.GetFormattedDate (to);
            toTimeString = BusinessDomain.GetFormattedTime (to);
            toDate = to.Date;
            toTime = to.TimeOfDay;

            timeRange = range;
        }

        public override DataFilter GetDataFilter (params object [] objects)
        {
            DataFilter dataFilter = base.GetDataFilter ();

            if (dataFilter.IsValid) {
                // Force recalculation
                fromDate = null;
                fromTime = null;
                toDate = null;
                toTime = null;

                DateTime fromValue = FromDateTime;
                DateTime toValue = ToDateTime;

                // This will allow us to include in the range values from the whole day
                if (fromValue == toValue) {
                    toValue = toValue.AddDays (1);
                    toValue = toValue.AddSeconds (-1);
                }

                if (dataFilter.Values [0] != null)
                    dataFilter.Values [0] = fromValue;

                if (dataFilter.Values [1] != null)
                    dataFilter.Values [1] = toValue;
            }

            return dataFilter;
        }

        #region Overrides of FilterBase

        public override void SetDataFilter (DataFilter dataFilter)
        {
            base.SetDataFilter (dataFilter);
            if (timeRange != DateRanges.Custom)
                return;

            if (fromDate != null) {
                fromTime = fromDate.Value.TimeOfDay;
                fromDate = fromDate.Value.Date;
                fromDateString = null;
            }

            if (toDate != null) {
                toTime = toDate.Value.TimeOfDay;
                toDate = toDate.Value.Date;
                toDateString = null;
            }
        }

        public override void Clear ()
        {
            if (!ClearToToday) {
                base.Clear ();
                return;
            }
            
            DateTime now = BusinessDomain.Now;
            DateTime today = now.Date;
            if (FromTime >= ToTime) {
                //if (today.Add (ToTime) > now) {
                //    FromDate = today;
                //    ToDate = today.AddDays (1);
                //} else {
                    FromDate = today.AddDays (-1);
                    ToDate = today;
                //}
            } else {
                FromDate = today;
                ToDate = today;
            }
            timeRange = DateRanges.Custom;
        }

        #endregion
    }
}
