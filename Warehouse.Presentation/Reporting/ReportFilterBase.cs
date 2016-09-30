//
// ReportFilterBase.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   08/15/2006
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
using GLib;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Reporting;
using Warehouse.Component;
using Warehouse.Data;
using System.Linq;

namespace Warehouse.Presentation.Reporting
{
    public abstract class ReportFilterBase<T> : ReportFilterBase where T : FilterBase
    {
        protected readonly T filter;

        private string LabelText
        {
            set
            {
                if (filter.ConstantVisibility) {
                    if (lblFieldName != null)
                        lblFieldName.SetText (value);
                } else {
                    if (chkColumnVisible != null)
                        chkColumnVisible.Label = value;
                }
            }
        }

        public override bool ColumnVisible
        {
            get
            {
                if (filter.ConstantVisibility)
                    return true;

                return chkColumnVisible != null ? chkColumnVisible.Active : filter.ColumnVisible;
            }
            set
            {
                if (filter.ConstantVisibility)
                    return;

                if (chkColumnVisible != null)
                    chkColumnVisible.Active = value;
                
                filter.ColumnVisible = value;
            }
        }

        protected ReportFilterBase (T filter)
        {
            this.filter = filter;
            ColumnVisible = filter.ColumnVisible;
        }

        protected override void InitializeLabel ()
        {
            if (filter.ConstantVisibility) {
                base.InitializeLabel ();
            } else {
                chkColumnVisible = new CheckButton { Active = filter.ColumnVisible };
                chkColumnVisible.Toggled += chkColumnVisible_Toggled;
                label.PackStart (chkColumnVisible, false, true, 0);
            }

            LabelText = Translator.GetReportFilterName (filter.DisplayedField) + ":";
        }

        private void chkColumnVisible_Toggled (object sender, EventArgs e)
        {
            ColumnVisible = chkColumnVisible.Active;
            OnColumnVisibilityToggled (e);
        }

        protected string ChooseDataFieldValue (SignalArgs args, string value)
        {
            object ret = value;
            if (FormHelper.ChooseDataFieldValue (filter.FilteredFields [0], ref ret) != ResponseType.Ok)
                return value;

            args.RetVal = true;
            return (string) ret;
        }

        public override void ClearFilter ()
        {
            filter.Clear ();
            ColumnVisible = filter.ColumnVisible;
        }

        public override void SetDataFilter (DataFilter dataFilter)
        {
            filter.SetDataFilter (dataFilter);
            ColumnVisible = filter.ColumnVisible;
        }

        protected static bool Equal (DataFilter dataFilter, DataFilter oldDataFilter)
        {
            return 
                oldDataFilter.CombineLogic == dataFilter.CombineLogic && oldDataFilter.Logic == dataFilter.Logic &&
                oldDataFilter.ShowColumns == dataFilter.ShowColumns &&
                oldDataFilter.FilteredFields.Length == dataFilter.FilteredFields.Length &&
                oldDataFilter.Values.Length == dataFilter.Values.Length &&
                oldDataFilter.FilteredFields.Where ((t, i) => Equals (t, dataFilter.FilteredFields [i])).Count () ==
                dataFilter.FilteredFields.Length &&
                oldDataFilter.Values.Where ((t, i) => Equals (t, dataFilter.Values [i])).Count () == dataFilter.Values.Length;
        }
    }

    public abstract class ReportFilterBase : ReportEntryBase
    {
        public event EventHandler FilterChanged;

        public event EventHandler ColumnVisibilityToggled;

        public static ReportFilterBase CreateFromFilterBase (FilterBase filter)
        {
            FilterChoose fChoose = filter as FilterChoose;
            if (fChoose != null)
                return new ReportFilterChoose (fChoose);

            FilterChooseLong fChooseLong = filter as FilterChooseLong;
            if (fChooseLong != null)
                return new ReportFilterChooseLong (fChooseLong);

            FilterCompare fCompare = filter as FilterCompare;
            if (fCompare != null)
                return new ReportFilterCompare (fCompare);

            FilterDateTimeRange fDateTimeRange = filter as FilterDateTimeRange;
            if (fDateTimeRange != null)
                return new ReportFilterDateTimeRange (fDateTimeRange);

            FilterDateRange fDateRange = filter as FilterDateRange;
            if (fDateRange != null)
                return new ReportFilterDateRange (fDateRange);

            FilterDateTimeCompare fDateTimeCompare = filter as FilterDateTimeCompare;
            if (fDateTimeCompare != null)
                return new ReportFilterDateTimeCompare (fDateTimeCompare);

            FilterEmpty fEmpty = filter as FilterEmpty;
            if (fEmpty != null)
                return new ReportFilterEmpty (fEmpty);

            FilterGroupFind fgFind = filter as FilterGroupFind;
            if (fgFind != null)
                return new ReportFilterGroupFind (fgFind);

            FilterFind fFind = filter as FilterFind;
            if (fFind != null)
                return new ReportFilterFind (fFind);

            FilterRange fRange = filter as FilterRange;
            if (fRange != null)
                return new ReportFilterRange (fRange);

            throw new ArgumentException ("Unrecognized filter type.");
        }
        
        public abstract bool ValidateFilter (bool selectErrors, bool skipNulls);

        public abstract void ClearFilter ();

        public abstract DataFilter GetDataFilter ();

        public abstract void SetDataFilter (DataFilter dataFilter);

        protected void OnColumnVisibilityToggled (EventArgs e)
        {
            EventHandler eh = ColumnVisibilityToggled;
            if (eh != null)
                eh (this, e);
        }

        protected void OnFilterChanged (EventArgs e)
        {
            if (FilterChanged != null)
                FilterChanged (this, e);
        }
    }
}
