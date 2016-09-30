//
// ReportFilterEmpty.cs
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

using Gtk;
using Warehouse.Business;
using Warehouse.Business.Reporting;
using Warehouse.Data;

namespace Warehouse.Presentation.Reporting
{
    public class ReportFilterEmpty : ReportFilterBase<FilterEmpty>
    {
        private Entry txtEmpty;

        public ReportFilterEmpty (bool alwaysOn, bool enabled, DataFilterLabel fLabel, params DbField [] fNames)
            : this (new FilterEmpty (alwaysOn, enabled, fLabel, fNames))
        {
        }

        public ReportFilterEmpty (FilterEmpty filter)
            : base (filter)
        {
            InitializeWidgets ();

            ColumnVisible = filter.ColumnVisible;
        }

        private void InitializeWidgets ()
        {
            txtEmpty = new Entry { Sensitive = false };
            entry.PackStart (txtEmpty, true, true, 1);

            InitializeLabel ();
        }

        public override bool ValidateFilter (bool selectErrors, bool skipNulls)
        {
            return filter.ValidateFilter (txtEmpty.Text, skipNulls);
        }

        public override DataFilter GetDataFilter ()
        {
            filter.SetData (txtEmpty.Text);
            return filter.GetDataFilter ();
        }
    }
}
