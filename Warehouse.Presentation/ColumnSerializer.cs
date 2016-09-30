//
// ColumnSerializer.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   07.07.2011
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

using System.Collections.Generic;
using Gtk;
using Warehouse.Business;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Presentation
{
    public static class ColumnSerializer
    {
        public static void SaveColumnSettings (Container container, WindowSettings settings)
        {
            settings.Columns.Clear ();
            foreach (ListView listView in ComponentHelper.GetChildWidgetsByType<ListView> (container))
                foreach (Column column in listView.ColumnController)
                    settings.Columns.Add (new ColumnSettings
                        {
                            Owner = listView.Name,
                            Key = column.ListCell.PropertyName,
                            IsSortable = column.IsSortable,
                            SortDirection = column.SortDirection,
                            SortKey = column.SortKey,
                            Width = column.Width
                        });

            settings.CommitChanges ();
        }

        public static void LoadColumnSettings (Container container, WindowSettings settings)
        {
            foreach (ListView grid in ComponentHelper.GetChildWidgetsByType<ListView> (container)) {
                Column sortedColumn = null;
                ListView local = grid;
                List<ColumnSettings> columnSettings = settings.Columns.FindAll (p => p.Owner == local.Name);
                List<Column> gridColumns = new List<Column> (grid.ColumnController);

                foreach (ColumnSettings columnSetting in columnSettings) {
                    string key = columnSetting.Key;
                    Column gridColumn = gridColumns.Find (c => c.ListCell.PropertyName == key);
                    if (gridColumn == null)
                        continue;

                    gridColumn.IsSortable = columnSetting.IsSortable;
                    gridColumn.SortDirection = columnSetting.SortDirection;
                    gridColumn.SortKey = columnSetting.SortKey;
                    gridColumn.Width = columnSetting.Width;
                    if (gridColumn.SortDirection != SortDirection.None)
                        sortedColumn = gridColumn;
                }

                if (sortedColumn == null || !sortedColumn.IsSortable)
                    continue;

                ISortable sortable = grid.Model as ISortable;
                if (sortable != null)
                    sortable.Sort (sortedColumn);
            }
        }
    }
}
