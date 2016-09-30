//
// ReportFilterCompare.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
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

using Gtk;
using Warehouse.Business;
using Warehouse.Business.Reporting;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.Reporting
{
    public class ReportFilterCompare : ReportFilterBase<FilterCompare>
    {
        private Entry txtCompareTo;
        private ComboBox cboSign;

        public ReportFilterCompare (bool alwaysOn, bool enabled, DataFilterLabel fLabel, params DbField [] fNames)
            : this (new FilterCompare (alwaysOn, enabled, fLabel, fNames))
        {
        }

        public ReportFilterCompare (FilterCompare filter)
            : base (filter)
        {
            InitializeWidgets ();

            ColumnVisible = filter.ColumnVisible;
        }

        private void InitializeWidgets ()
        {
            cboSign = new ComboBox ();
            cboSign.Load (Translator.GetReportFilterCompareLogics (), "Key", "Value");
            entry.PackStart (cboSign, true, true, 1);

            txtCompareTo = new Entry { WidthRequest = 110, Alignment = 1 };
            txtCompareTo.ButtonPressEvent += txtCompareTo_ButtonPressEvent;
            txtCompareTo.KeyPressEvent += txtCompareTo_KeyPressEvent;
            entry.PackStart (txtCompareTo, false, true, 1);

            InitializeLabel ();
        }

        #region Field completion handling

        [GLib.ConnectBefore]
        private void txtCompareTo_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type == Gdk.EventType.TwoButtonPress)
                txtCompareTo.Text = ChooseDataFieldValue (args, txtCompareTo.Text);
        }

        [GLib.ConnectBefore]
        private void txtCompareTo_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                txtCompareTo.Text = ChooseDataFieldValue (args, txtCompareTo.Text);
        }

        #endregion

        public override bool ValidateFilter (bool selectErrors, bool skipNulls)
        {
            if (filter.ValidateFilter (txtCompareTo.Text, skipNulls))
                return true;

            if (selectErrors)
                txtCompareTo.SelectRegion (0, txtCompareTo.Text.Length);

            return false;
        }

        public override void ClearFilter ()
        {
            base.ClearFilter ();

            txtCompareTo.Text = filter.Text;
            cboSign.SetSelection (Translator.GetReportFilterCompareLogics (), "Key", "Value", (int) filter.FilterLogic);
        }

        public override DataFilter GetDataFilter ()
        {
            filter.SetData ((DataFilterLogic) cboSign.GetSelectedValue (), txtCompareTo.Text);
            return filter.GetDataFilter ();
        }

        public override void SetDataFilter (DataFilter dataFilter)
        {
            base.SetDataFilter (dataFilter);

            txtCompareTo.Text = filter.Text;
            cboSign.SetSelection (Translator.GetReportFilterCompareLogics (), "Key", "Value", (int) filter.FilterLogic);
        }
    }
}
