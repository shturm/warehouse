//
// ReportFilterChoose.cs
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
using System.Collections.Generic;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Reporting;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.Reporting
{
    public class ReportFilterChooseLong : ReportFilterBase<FilterChooseLong>
    {
        private ComboBox cboValues;

        public ReportFilterChooseLong (bool alwaysOn, bool enabled, KeyValuePair<long, string> [] values, DataFilterLabel fLabel, params DbField [] fNames)
            : this (new FilterChooseLong (alwaysOn, enabled, values, fLabel, fNames))
        {
        }

        public ReportFilterChooseLong (FilterChooseLong filter)
            : base (filter)
        {
            InitializeWidgets ();

            ColumnVisible = filter.ColumnVisible;
        }

        private void InitializeWidgets ()
        {
            cboValues = new ComboBox ();
            cboValues.Load (filter.Values, "Key", "Value");
            cboValues.Changed += cboValues_Changed;
            entry.PackStart (cboValues, true, true, 1);

            InitializeLabel ();
        }

        private void cboValues_Changed (object sender, EventArgs e)
        {
            OnFilterChanged (EventArgs.Empty);
        }

        public override bool ValidateFilter (bool selectErrors, bool skipNulls)
        {
            return true;
        }

        public override void ClearFilter ()
        {
            base.ClearFilter ();
            cboValues.SetSelection (0);
        }

        public override DataFilter GetDataFilter ()
        {
            filter.SetData (cboValues.GetSelectedValue ());
            return filter.GetDataFilter ();
        }

        public override void SetDataFilter (DataFilter dataFilter)
        {
            base.SetDataFilter (dataFilter);
            cboValues.SetSelection (filter.Values, "Key", "Value", filter.Value);
        }
    }
}
