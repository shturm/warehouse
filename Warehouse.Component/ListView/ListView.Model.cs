//
// ListView.Model.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/23/2008
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
using System.Reflection;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Component.ListView
{
    public partial class ListView
    {
        private void OnColumnControllerUpdatedHandler (object o, EventArgs args)
        {
            OnColumnControllerUpdated ();
        }

        private string row_sensitive_property_name = "Sensitive";
        private PropertyInfo row_sensitive_property_info;
        bool row_sensitive_property_invalid = false;

        public string RowSensitivePropertyName
        {
            get { return row_sensitive_property_name; }
            set
            {
                if (value == row_sensitive_property_name) {
                    return;
                }

                row_sensitive_property_name = value;
                row_sensitive_property_info = null;
                row_sensitive_property_invalid = false;

                InvalidateList ();
            }
        }

        private bool IsRowSensitive (object item)
        {
            if (item == null || row_sensitive_property_invalid) {
                return true;
            }

            if (row_sensitive_property_info == null || row_sensitive_property_info.ReflectedType != item.GetType ()) {
                row_sensitive_property_info = item.GetType ().GetProperty (row_sensitive_property_name);
                if (row_sensitive_property_info == null || row_sensitive_property_info.PropertyType != typeof (bool)) {
                    row_sensitive_property_info = null;
                    row_sensitive_property_invalid = true;
                    return true;
                }
            }

            return (bool) row_sensitive_property_info.GetValue (item, null);
        }

        public void Sort (string key, SortDirection direction)
        {
            if (sortModel == null)
                return;

            for (int i = 0; i < column_cache.Length; i++) {
                if (!column_cache [i].Column.Visible)
                    continue;

                Column column = column_cache [i].Column;
                if (!column.IsSortable || column.SortKey != key)
                    continue;

                column.SortDirection = direction;
                sortModel.Sort (column);
                break;
            }
        }
    }
}
