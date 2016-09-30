//
// ListView.Header.cs
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
using System.Collections.Generic;
using Gdk;
using Gtk;

namespace Warehouse.Component.ListView
{
    public partial class ListView
    {
        internal struct CachedColumn
        {
            public static readonly CachedColumn Zero;

            public Column Column;
            public int X1;
            public int X2;
            public int Width;
            public int ResizeX1;
            public int ResizeX2;
            public int Index;
            public double CalculatedWidth;

            static CachedColumn ()
            {
                Zero = new CachedColumn ();
                Zero.Index = -1;
            }
        }

        private static Gdk.Cursor resize_x_cursor = new Gdk.Cursor (Gdk.CursorType.SbHDoubleArrow);
        private static Gdk.Cursor drag_cursor = new Gdk.Cursor (Gdk.CursorType.Fleur);

        private int header_width;
        private int sort_column_index = -1;
        private int resizing_column_index = -1;
        private int pressed_column_index = -1;
        private int pressed_column_x = -1;
        private int pressed_column_x_start = -1;
        private int pressed_column_x_offset = -1;
        private int pressed_column_x_drag = -1;
        private int pressed_column_x_start_hadjustment = -1;
        private bool pressed_column_is_dragging = false;

        private Pango.Layout column_layout;

        private CachedColumn [] column_cache;
        private List<int> elasticColumns;

        #region Columns

        private void InvalidateColumnCache ()
        {
            column_cache = null;
        }

        private void UpdateColumnCache ()
        {
            if (column_controller == null) {
                return;
            }

            if (column_cache == null) {
                GenerateColumnCache ();
            }

            CachedColumn lastColumn = CachedColumn.Zero;
            for (int i = 0; i < column_cache.Length; i++) {
                if (!column_cache [i].Column.Visible)
                    continue;

                column_cache [i].Width = (int) Math.Round (header_width * column_cache [i].Column.Width, MidpointRounding.AwayFromZero);
                column_cache [i].X1 = lastColumn.X2;
                column_cache [i].X2 = column_cache [i].X1 + column_cache [i].Width;
                column_cache [i].ResizeX1 = column_cache [i].X2;
                column_cache [i].ResizeX2 = column_cache [i].ResizeX1 + 2;

                lastColumn = column_cache [i];
            }

            // TODO handle max width
            if (lastColumn.Index < 0)
                return;

            column_cache [lastColumn.Index].X2 = header_width;
            column_cache [lastColumn.Index].Width = lastColumn.X2 - lastColumn.X1;
        }

        private void GenerateColumnCache ()
        {
            column_cache = new CachedColumn [column_controller.Count];

            int i = 0;
            double total = 0.0;

            // Calculate the total relative width and the minimal column widths
            foreach (Column column in column_controller) {
                int w;
                int h;
                column_layout.SetText (column.HeaderText);
                column_layout.GetPixelSize (out w, out h);

                column_cache [i] = new CachedColumn ();
                column_cache [i].Column = column;
                column_cache [i].Index = i;
                column.MinWidth = Math.Max (column.MinWidth, w);

                if (column.Visible)
                    total += column.Width;
                i++;
            }

            // Normalize the relative column widths
            double scale_factor = 1.0 / total;
            for (i = 0; i < column_cache.Length; i++) {
                if (!column_cache [i].Column.Visible)
                    continue;

                column_cache [i].Column.Width *= scale_factor;
            }

            AutoCalculateColumnSizes ();
        }

        private void AutoCalculateColumnSizes ()
        {
            if (column_cache == null) {
                return;
            }

            if (header_interaction_alloc.Width == 0)
                return;

            header_width = header_interaction_alloc.Width;

            if (elasticColumns == null)
                elasticColumns = new List<int> (column_cache.Length);
            else
                elasticColumns.Clear ();

            for (int i = 0; i < column_cache.Length; i++) {
                if (!column_cache [i].Column.Visible)
                    continue;

                elasticColumns.Add (i);
                column_cache [i].CalculatedWidth = 0.0;
            }

            double remaining_width = AutoCalculateColumnSizes (header_width, header_width);

            while (remaining_width != 0 && elasticColumns.Count > 0) {
                double total_elastic_width = 0.0;
                foreach (int i in elasticColumns) {
                    total_elastic_width += column_cache [i].CalculatedWidth;
                }
                remaining_width = AutoCalculateColumnSizes (remaining_width, total_elastic_width);
            }

            double new_header_width = 0;
            for (int i = 0; i < column_cache.Length; i++) {
                if (!column_cache [i].Column.Visible)
                    continue;

                new_header_width += column_cache [i].CalculatedWidth;
            }
            header_width = (int) new_header_width;

            for (int i = 0; i < column_cache.Length; i++) {
                if (!column_cache [i].Column.Visible)
                    continue;

                column_cache [i].Column.Width = column_cache [i].CalculatedWidth / (double) header_width;
            }
        }

        private double AutoCalculateColumnSizes (double freeWidth, double totalWidth)
        {
            double remainingWidth = freeWidth;

            for (int index = 0; index < elasticColumns.Count; index++) {
                int i = elasticColumns [index];
                double delta = column_cache [i].Column.Width * header_width * freeWidth / totalWidth;
                if (remainingWidth - delta < 0 && elasticColumns.Count == 1)
                    delta = remainingWidth;

                // TODO handle max widths
                double width = column_cache [i].CalculatedWidth + delta;
                if (width < column_cache [i].Column.MinWidth) {
                    delta = column_cache [i].Column.MinWidth - column_cache [i].CalculatedWidth;
                    elasticColumns.RemoveAt (index);
                    index--;
                } else if (width > column_cache [i].Column.MaxWidth) {
                    delta = column_cache [i].Column.MaxWidth - column_cache [i].CalculatedWidth;
                    elasticColumns.RemoveAt (index);
                    index--;
                }

                remainingWidth -= delta;
                column_cache [i].CalculatedWidth += delta;
            }

            return Math.Round (remainingWidth, MidpointRounding.AwayFromZero);
        }

        protected virtual void OnColumnControllerUpdated ()
        {
            InvalidateColumnCache ();
            UpdateColumnCache ();
            UpdateAdjustments (null, null);
            QueueDraw ();
        }

        protected virtual void OnColumnRightClicked (Column clickedColumn, int x, int y)
        {
            Menu menu = new Menu ();

            if (!string.IsNullOrEmpty (clickedColumn.ListCell.PropertyName)) { // FIXME: Also restrict if the column vis can't be changed
                menu.Append (new ColumnHideMenuItem (clickedColumn));
                menu.Append (new SeparatorMenuItem ());
            }

            Column [] columns = ColumnController.ToArray ();
            Array.Sort (columns, delegate (Column a, Column b)
            {
                // Fully qualified type name to avoid Mono 1.2.4 bug
                return System.String.Compare (a.HeaderText, b.HeaderText);
            });

            foreach (Column column in columns) {
                if (string.IsNullOrEmpty (column.ListCell.PropertyName)) {
                    continue;
                }

                menu.Append (new ColumnToggleMenuItem (column));
            }

            menu.ShowAll ();
            menu.Popup (null, null, delegate (Menu popup, out int pos_x, out int pos_y, out bool push_in)
            {
                int win_x, win_y;
                GdkWindow.GetOrigin (out win_x, out win_y);

                pos_x = win_x + x;
                pos_y = win_y + y;
                push_in = true;
            }, 3, Gtk.Global.CurrentEventTime);
        }

        private void ResizeColumn (double x)
        {
            CachedColumn resizingColumn = column_cache [resizing_column_index];
            double resizeDelta = x - resizingColumn.ResizeX2;

            resizeDelta = Math.Max (resizeDelta, resizingColumn.Column.MinWidth - resizingColumn.Width);
            resizeDelta = Math.Min (resizeDelta, resizingColumn.Column.MaxWidth - resizingColumn.Width);

            if (resizeDelta == 0) {
                return;
            }

            int sign = Math.Sign (resizeDelta);
            resizeDelta = Math.Abs (resizeDelta);
            double total_elastic_width = 0.0;

            for (int i = resizing_column_index + 1; i < column_cache.Length; i++) {
                column_cache [i].CalculatedWidth = sign == 1
                    ? column_cache [i].Width - column_cache [i].Column.MinWidth
                    : column_cache [i].Column.MaxWidth - column_cache [i].Width;

                total_elastic_width += column_cache [i].CalculatedWidth;
            }

            if (total_elastic_width != 0 && sign == -1 && header_width > header_interaction_alloc.Width) {
                total_elastic_width = 0;
            }

            if (total_elastic_width > 0) {
                double resizeFactor = Math.Min (resizeDelta, total_elastic_width);
                resizeFactor = sign * resizeFactor / header_width;

                for (int i = resizing_column_index + 1; i < column_cache.Length; i++) {
                    column_cache [i].Column.Width += -resizeFactor * column_cache [i].CalculatedWidth / total_elastic_width;
                }
            }

            resizingColumn.Column.Width += sign * resizeDelta / header_width;

            System.Diagnostics.Debug.Write ("Columns: ");
            double total_width = 0;
            for (int i = 0; i < column_cache.Length; i++) {
                if (!column_cache [i].Column.Visible)
                    continue;

                double col_width = column_cache [i].Column.Width * header_width;
                // Do a range check on the column size
                col_width = Math.Max (col_width, column_cache [i].Column.MinWidth);
                col_width = Math.Min (col_width, column_cache [i].Column.MaxWidth);
                // Save the possible modifications back
                column_cache [i].Column.Width = col_width / header_width;

                System.Diagnostics.Debug.Write (((int) col_width).ToString ().PadLeft (3, ' '));
                System.Diagnostics.Debug.Write (", ");
                total_width += col_width;
            }
            System.Diagnostics.Debug.WriteLine ("Total: " + total_width);

            if (header_width != (int) total_width) {
                double scale_factor = header_width / total_width;
                for (int i = 0; i < column_cache.Length; i++) {
                    if (!column_cache [i].Column.Visible)
                        continue;

                    column_cache [i].Column.Width *= scale_factor;
                }
                header_width = (int) total_width;
            }

            UpdateColumnCache ();
            UpdateAdjustments (null, null);
            QueueDraw ();
        }

        private Column GetColumnForResizeHandle (int x)
        {
            if (column_cache == null) {
                return null;
            }

            for (int i = 0; i < column_cache.Length - 1; i++) {
                if (x >= column_cache [i].ResizeX1 - 2 &&
                    x <= column_cache [i].ResizeX2 + 2 &&
                    column_cache [i].Column.MaxWidth != column_cache [i].Column.MinWidth) {
                    return column_cache [i].Column;
                }
            }

            return null;
        }

        private CachedColumn GetCachedColumnForColumn (Column col)
        {
            foreach (CachedColumn ca_col in column_cache) {
                if (!ca_col.Column.Visible)
                    continue;

                if (ca_col.Column == col) {
                    return ca_col;
                }
            }

            return CachedColumn.Zero;
        }

        private ColumnController column_controller;
        public ColumnController ColumnController
        {
            get { return column_controller; }
            set
            {
                if (column_controller == value) {
                    return;
                }

                if (column_controller != null) {
                    column_controller.Updated -= OnColumnControllerUpdatedHandler;
                }

                column_controller = value;

                foreach (Column column in column_controller) {
                    column.ParentListView = this;
                }

                OnColumnControllerUpdated ();

                if (column_controller != null) {
                    column_controller.Updated += OnColumnControllerUpdatedHandler;
                }
            }
        }

        #endregion

        #region Header

        private ListViewRowHeightHandler header_height_handler;
        public virtual ListViewRowHeightHandler HeaderHeightProvider
        {
            get { return header_height_handler; }
            set
            {
                if (value != header_height_handler) {
                    header_height_handler = value;
                    header_height = 0;
                }
            }
        }

        private int header_height = 0;
        private int HeaderHeight
        {
            get
            {
                if (!header_visible) {
                    return 0;
                }

                if (header_height == 0) {
                    header_height = header_height_handler != null
                        ? header_height_handler (this)
                        : CellText.ComputeRowHeight (this, HeaderStyleProvider) + 2;
                }

                return header_height;
            }
        }

        private bool header_visible = true;
        public bool HeaderVisible
        {
            get { return header_visible; }
            set
            {
                header_visible = value;
                RecalculateWindowSizes (Allocation);
                QueueDraw ();
            }
        }

        #endregion

        #region Gtk.MenuItem Wrappers for the column context menu

        private class ColumnToggleMenuItem : CheckMenuItem
        {
            private Column column;
            private bool ready = false;

            public ColumnToggleMenuItem (Column column)
                : base (column.HeaderText ?? String.Empty)
            {
                this.column = column;
                Active = column.Visible;
                ready = true;
            }

            protected override void OnActivated ()
            {
                base.OnActivated ();

                if (!ready) {
                    return;
                }

                column.Visible = Active;
            }
        }

        private class ColumnHideMenuItem : MenuItem
        {
            private Column column;

            public ColumnHideMenuItem (Column column)
                : base (String.Format ("Hide {0}", column.HeaderText))
            {
                this.column = column;
            }

            protected override void OnActivated ()
            {
                column.Visible = false;
            }
        }

        #endregion

    }
}
