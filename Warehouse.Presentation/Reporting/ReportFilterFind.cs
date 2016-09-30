//
// ReportFilterFind.cs
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
    public class ReportFilterFind : ReportFilterBase<FilterFind>
    {
        private Entry txtFind;
        private Button btnFind;

        public ReportFilterFind (bool alwaysOn, bool enabled, DataFilterLabel fLabel, params DbField [] fNames)
            : this (new FilterFind (alwaysOn, enabled, fLabel, fNames))
        {
        }

        public ReportFilterFind (FilterFind filter)
            : base (filter)
        {
            InitializeWidgets ();

            ColumnVisible = filter.ColumnVisible;
        }

        private void InitializeWidgets ()
        {
            txtFind = new Entry ();
            entry.PackStart (txtFind, true, true, 1);

            InitializeLabel ();

            if (filter.IsFrozen) {
                txtFind.Text = filter.Text;
                txtFind.Sensitive = false;
                return;
            }
            txtFind.Changed += txtFind_Changed;

            if (!FormHelper.CanChooseDataField (filter.FilteredFields [0]))
                return;

            txtFind.ButtonPressEvent += txtFind_ButtonPressEvent;
            txtFind.KeyPressEvent += txtFind_KeyPressEvent;

            btnFind = new Button { Label = " ... " };
            entry.PackEnd (btnFind, false, true, 1);
            btnFind.Clicked += btnFind_Clicked;
        }

        private void txtFind_Changed (object sender, EventArgs e)
        {
            OnFilterChanged (EventArgs.Empty);
        }

        #region Field completion handling

        [ConnectBefore]
        private void txtFind_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type == Gdk.EventType.TwoButtonPress)
                ChooseDataFieldValue (args);
        }

        [ConnectBefore]
        private void txtFind_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                ChooseDataFieldValue (args);
        }

        private void btnFind_Clicked (object sender, System.EventArgs e)
        {
            ChooseDataFieldValue (new SignalArgs ());
        }

        private void ChooseDataFieldValue (SignalArgs args)
        {
            object ret = txtFind.Text;
            if (FormHelper.ChooseDataFieldValue (filter.FilteredFields [0], ref ret) != ResponseType.Ok)
                return;

            txtFind.Text = (string) ret;
            args.RetVal = true;
        }

        #endregion

        public override bool ValidateFilter (bool selectErrors, bool skipNulls)
        {
            if (filter.ValidateFilter (txtFind.Text, skipNulls))
                return true;

            if (selectErrors)
                txtFind.SelectRegion (0, txtFind.Text.Length);

            return false;
        }

        public override void ClearFilter ()
        {
            base.ClearFilter ();

            if (!filter.IsFrozen)
                txtFind.Text = filter.Text;
        }

        public override DataFilter GetDataFilter ()
        {
            filter.SetData (txtFind.Text);
            return filter.GetDataFilter ();
        }

        public override void SetDataFilter (DataFilter dataFilter)
        {
            base.SetDataFilter (dataFilter);

            if (!filter.IsFrozen)
                txtFind.Text = filter.Text;
            OnFilterChanged (EventArgs.Empty);
        }
    }
}
