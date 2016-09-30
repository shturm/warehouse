//
// ReportOrder.cs
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
    public class ReportOrder : ReportEntryBase
    {
        private readonly Order order;
        private readonly ComboBox cboFields = new ComboBox ();
        private readonly ComboBox cboDirection = new ComboBox ();

        public DbField OrderBy
        {
            get { return order.Selection; }
        }

        public SortDirection OrderDirection
        {
            get { return order.Direction; }
        }

        public KeyValuePair<DbField, string> [] OrderByChoices
        {
            get { return order.Choices; }
        }

        public ReportOrder (params DbField [] fields)
            : this (new Order (fields))
        {
        }

        public ReportOrder (Order order)
        {
            this.order = order;
            cboFields.Load (order.Choices, "Key", "Value");
            cboFields_Changed (cboFields, EventArgs.Empty);
            cboFields.Changed += cboFields_Changed;
            entry.PackStart (cboFields, true, true, 1);

            cboDirection.Load (Translator.GetSortDirections (), "Key", "Value");
            cboDirection_Changed (cboDirection, EventArgs.Empty);
            cboDirection.Changed += cboDirection_Changed;
            entry.PackStart (cboDirection, false, true, 1);

            InitializeLabel ();
            lblFieldName.SetText (Translator.GetString ("Sort by:"));
        }

        private void cboFields_Changed (object sender, EventArgs e)
        {
            order.Selection = (DbField) cboFields.GetSelectedValue ();
            cboDirection.Sensitive = order.Selection != DataField.NotSet;
        }

        private void cboDirection_Changed (object sender, EventArgs e)
        {
            order.Direction = (SortDirection) cboDirection.GetSelectedValue ();
        }

        public void LoadOrder (KeyValuePair<DbField, string>[] orderByChoices, DbField orderBy, SortDirection orderDirection)
        {
            order.LoadOrder (orderByChoices, orderBy, orderDirection);
            cboFields.Load (orderByChoices, "Key", "Value", order.Selection);
            cboDirection.Load (Translator.GetSortDirections (), "Key", "Value", (int) order.Direction);
        }

        public void ClearOrder ()
        {
            order.ClearOrder ();
            cboFields.SetSelection (0);
        }
    }
}
