//
// ReportFilterRange.cs
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

using System;
using GLib;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Reporting;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.Reporting
{
    public class ReportFilterRange : ReportFilterBase<FilterRange>
    {
        private Entry txtFrom;
        private Entry txtTo;
        private Button btnFrom;
        private Button btnTo;

        public ReportFilterRange (bool alwaysOn, bool enabled, DataFilterLabel fLabel, params DbField [] fNames)
            : this (new FilterRange (alwaysOn, enabled, fLabel, fNames))
        {
        }

        public ReportFilterRange (FilterRange filter)
            : base (filter)
        {
            InitializeWidgets ();

            ColumnVisible = filter.ColumnVisible;
        }

        private void InitializeWidgets ()
        {
            bool canChoose = FormHelper.CanChooseDataField (filter.FilteredFields [0]);
            txtFrom = new Entry { WidthRequest = 110 };
            entry.PackStart (txtFrom, false, true, 1);
            if (canChoose) {
                txtFrom.ButtonPressEvent += txtFrom_ButtonPressEvent;
                txtFrom.KeyPressEvent += txtFrom_KeyPressEvent;
                btnFrom = new Button { Label = " ... " };
                entry.PackStart (btnFrom, false, true, 1);
                btnFrom.Clicked += btnFrom_Clicked;
            }
            txtFrom.Changed += txtFrom_Changed;

            Label separator = new Label { Text = "-", WidthRequest = 22 };
            entry.PackStart (separator, true, true, 1);

            txtTo = new Entry { WidthRequest = 110 };
            entry.PackStart (txtTo, false, true, 1);
            if (canChoose) {
                txtTo.ButtonPressEvent += txtTo_ButtonPressEvent;
                txtTo.KeyPressEvent += txtTo_KeyPressEvent;
                btnTo = new Button { Label = " ... " };
                entry.PackStart (btnTo, false, true, 1);
                btnTo.Clicked += btnTo_Clicked;
            }
            txtTo.Changed += txtTo_Changed;

            InitializeLabel ();
        }

        private void txtFrom_Changed (object sender, EventArgs e)
        {
            if (!txtFrom.Sensitive)
                return;

            int cursorPosition = txtFrom.CursorPosition;
            OnFilterChanged (EventArgs.Empty);
            txtFrom.GrabFocus ();
            txtFrom.SelectRegion (cursorPosition, cursorPosition);
        }

        private void txtTo_Changed (object sender, EventArgs e)
        {
            if (!txtTo.Sensitive)
                return;

            int cursorPosition = txtTo.CursorPosition;
            OnFilterChanged (EventArgs.Empty);
            txtTo.GrabFocus ();
            txtTo.SelectRegion (cursorPosition, cursorPosition);
        }

        #region Field completion handling

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

        private void btnFrom_Clicked (object sender, System.EventArgs e)
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

        private void btnTo_Clicked (object sender, System.EventArgs e)
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

            txtFrom.Text = filter.From;
            txtTo.Text = filter.To;
        }

        public override DataFilter GetDataFilter ()
        {
            filter.SetData (txtFrom.Text, txtTo.Text);
            return filter.GetDataFilter ();
        }

        public override void SetDataFilter (DataFilter dataFilter)
        {
            DataFilter oldDataFilter = GetDataFilter ();
            if (Equal (oldDataFilter, dataFilter))
                return;
            base.SetDataFilter (dataFilter);

            txtFrom.Text = filter.From;
            txtTo.Text = filter.To;
            ColumnVisible = filter.ColumnVisible;
            OnFilterChanged (EventArgs.Empty);
        }
    }
}
