//
// ListView.Rendering.cs
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
using Warehouse.Data;
using Window = Gdk.Window;

namespace Warehouse.Component.ListView
{
    public delegate int ListViewRowHeightHandler (Widget widget);

    public partial class ListView
    {
        private readonly List<int> selected_rows = new List<int> ();

        private Cairo.Context cairo_context;
        private Pango.Layout pango_layout;
        private CellContext cell_context;

        private Theme theme;
        protected Theme Theme
        {
            get { return theme; }
        }

        public event EventHandler<RowEventArgs> RowItemLoaded;  

        protected override void OnStyleSet (Style old_style)
        {
            base.OnStyleSet (old_style);
            RecomputeRowHeight = true;
            theme = GtkTheme.CreateTheme (this);

            // Save the drawable so we can reuse it
            Drawable drawable = cell_context != null ? cell_context.Drawable : null;

            cell_context = new CellContext ();
            cell_context.Theme = theme;
            cell_context.Widget = this;
            cell_context.Drawable = drawable;
        }

        protected override bool OnExposeEvent (EventExpose evnt)
        {
            Rectangle damage = new Rectangle ();
            foreach (Rectangle rect in evnt.Region.GetRectangles ()) {
                damage = damage.Union (rect);
            }

            cairo_context = CairoHelper.Create (evnt.Window);
            if (pango_layout == null) {
                pango_layout = this.CreateLayout (cairo_context);
            }

            cell_context.Context = cairo_context;
            cell_context.Layout = pango_layout;

            Theme.DrawFrameBackground (cairo_context, Allocation, true);
            if (header_visible && column_controller != null) {
                PaintHeader (damage);
            }
            if (footer_visible && column_controller != null) {
                PaintFooter (damage);
            }
            Theme.DrawFrameBorder (cairo_context, Allocation);
            if (model != null)
                PaintRows (damage);
            PaintDraggingColumn (damage);

            ((IDisposable) cairo_context.Target).Dispose ();
            ((IDisposable) cairo_context).Dispose ();

            return true;
        }

        private void PaintHeader (Rectangle clip)
        {
            Rectangle rect = header_rendering_alloc;
            rect.Height += Theme.BorderWidth;
            clip.Intersect (rect);
            cairo_context.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
            cairo_context.Clip ();

            Theme.DrawHeaderBackground (cairo_context, header_rendering_alloc);

            Rectangle cell_area = new Rectangle ();
            cell_area.Y = header_rendering_alloc.Y;
            cell_area.Height = header_rendering_alloc.Height;

            cell_context.Clip = clip;
            cell_context.Sensitive = true;
            cell_context.TextAsForeground = true;

            for (int ci = 0; ci < column_cache.Length; ci++) {
                if (!column_cache [ci].Column.Visible)
                    continue;

                if (pressed_column_is_dragging && pressed_column_index == ci) {
                    continue;
                }

                cell_area.X = column_cache [ci].X1 + Theme.TotalBorderWidth + header_rendering_alloc.X - (int) hadjustment.Value;
                cell_area.Width = column_cache [ci].Width;
                PaintHeaderCell (cell_area, ci, false);
            }

            if (pressed_column_is_dragging && pressed_column_index >= 0) {
                cell_area.X = pressed_column_x_drag + Allocation.X - (int) hadjustment.Value;
                cell_area.Width = column_cache [pressed_column_index].Width;
                PaintHeaderCell (cell_area, pressed_column_index, true);
            }

            cairo_context.ResetClip ();
        }

        private void PaintHeaderCell (Rectangle area, int ci, bool dragging)
        {
            if (ci < 0 || column_cache.Length <= ci)
                return;

            if (dragging) {
                Theme.DrawColumnHighlight (cairo_context, area,
                    Theme.Colors.GetWidgetColor (GtkColorClass.Dark, StateType.Normal).ColorShade (0.9));

                Cairo.Color stroke_color = Theme.Colors.GetWidgetColor (
                    GtkColorClass.Base, StateType.Normal).ColorShade (0.0);
                stroke_color.A = 0.3;

                cairo_context.Color = stroke_color;
                cairo_context.MoveTo (area.X + 0.5, area.Y + 1.0);
                cairo_context.LineTo (area.X + 0.5, area.Bottom);
                cairo_context.MoveTo (area.Right - 0.5, area.Y + 1.0);
                cairo_context.LineTo (area.Right - 0.5, area.Bottom);
                cairo_context.Stroke ();
            }

            Cell cell = column_cache [ci].Column.HeaderCell;

            if (cell != null) {
                cairo_context.Save ();
                cairo_context.Translate (area.X, area.Y);
                cell_context.Area = area;
                cell.Render (cell_context, StateType.Normal, area.Width, area.Height, new CellPosition (ci, -1));
                cairo_context.Restore ();
            }

            bool isLastVisibleColumn = true;
            for (int i = ci + 1; i < column_cache.Length; i++) {
                if (!column_cache [i].Column.Visible)
                    continue;
                
                isLastVisibleColumn = false;
                break;
            }

            if (!dragging && !isLastVisibleColumn) {
                Theme.DrawHeaderSeparator (cairo_context, area, area.Right);
            }
        }

        private void PaintFooter (Rectangle clip)
        {
            Rectangle rect = footer_rendering_alloc;
            rect.Height += Theme.BorderWidth;
            rect.X -= Theme.BorderWidth;
            clip.Intersect (rect);
            cairo_context.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
            cairo_context.Clip ();

            Theme.DrawFooterBackground (cairo_context, footer_rendering_alloc);

            Rectangle cell_area = new Rectangle ();
            cell_area.Y = footer_rendering_alloc.Y;
            cell_area.Height = footer_rendering_alloc.Height;

            cell_context.Clip = clip;
            cell_context.Sensitive = true;
            cell_context.TextAsForeground = true;

            for (int ci = 0; ci < column_cache.Length; ci++) {
                if (!column_cache [ci].Column.Visible)
                    continue;

                if (pressed_column_is_dragging && pressed_column_index == ci) {
                    continue;
                }

                cell_area.X = column_cache [ci].X1 + Theme.TotalBorderWidth + footer_rendering_alloc.X - (int) hadjustment.Value;
                cell_area.Width = column_cache [ci].Width;
                PaintFooterCell (cell_area, ci);
            }

            cairo_context.ResetClip ();
        }

        private void PaintFooterCell (Rectangle area, int ci)
        {
            if (ci < 0 || column_cache.Length <= ci)
                return;

            Cell cell = column_cache [ci].Column.FooterCell;

            if (cell != null) {
                cairo_context.Save ();
                cairo_context.Translate (area.X, area.Y);
                cell_context.Area = area;
                cell.Render (cell_context, StateType.Normal, area.Width, area.Height, new CellPosition (ci, -2));
                cairo_context.Restore ();
            }

            if (ci < column_cache.Length - 1) {
                Theme.DrawFooterSeparator (cairo_context, area, area.Right);
            }
        }

        private void PaintRows (Rectangle clip)
        {
            sort_column_index = -1;
            for (int i = 0; i < column_cache.Length; i++) {
                if (!column_cache [i].Column.Visible)
                    continue;

                Column column = column_cache [i].Column;
                if (sortModel == null)
                    continue;

                CellTextHeader column_cell = column.HeaderCell as CellTextHeader;
                if (column_cell == null)
                    continue;

                if (column.IsSortable && sortModel.SortColumn == column) {
                    sort_column_index = i;
                }
            }

            if (sort_column_index != -1 && (!pressed_column_is_dragging || pressed_column_index != sort_column_index)) {
                CachedColumn col = column_cache [sort_column_index];
                Theme.DrawRowRule (cairo_context,
                    list_rendering_alloc.X + col.X1 - (int) hadjustment.Value,
                    header_rendering_alloc.Bottom + Theme.BorderWidth,
                    col.Width, list_rendering_alloc.Height + Theme.InnerBorderWidth * 2);
            }

            clip.Intersect (list_rendering_alloc);
            cairo_context.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
            cairo_context.Clip ();

            cell_context.Clip = clip;
            cell_context.TextAsForeground = false;

            int vadjustment_value = (int) vadjustment.Value;
            int first_row = vadjustment_value / RowHeight;
            int last_row;
            try {
                last_row = Math.Min (model.Count, first_row + RowsInView);
            } catch (DbConnectionLostException) {
                last_row = first_row + RowsInView;
            }
            int offset = list_rendering_alloc.Y - vadjustment_value % RowHeight;

            Rectangle selected_focus_alloc = Rectangle.Zero;
            Rectangle single_list_alloc = new Rectangle ();

            single_list_alloc.X = list_rendering_alloc.X - (int) (hadjustment.Value);
            single_list_alloc.Y = offset;
            single_list_alloc.Width = list_rendering_alloc.Width;
            single_list_alloc.Height = RowHeight;

            int selection_height = 0;
            int selection_y = 0;
            selected_rows.Clear ();

            for (int ri = first_row; ri < last_row; ri++) {
                if (Selection != null && Selection.Contains (ri)) {
                    if (selection_height == 0) {
                        selection_y = single_list_alloc.Y;
                    }

                    selection_height += single_list_alloc.Height;
                    selected_rows.Add (ri);

                    if (Selection.FocusedCell.Row == ri) {
                        selected_focus_alloc = single_list_alloc;
                    }
                } else {
                    if (rules_hint && ri % 2 != 0) {
                        Theme.DrawRowRule (cairo_context, list_rendering_alloc.X, single_list_alloc.Y,
                            single_list_alloc.Width, single_list_alloc.Height);
                    }

                    if (ri == drag_reorder_row_index && Reorderable) {
                        cairo_context.Save ();
                        cairo_context.LineWidth = 1.0;
                        cairo_context.Antialias = Cairo.Antialias.None;
                        cairo_context.MoveTo (single_list_alloc.Left, single_list_alloc.Top);
                        cairo_context.LineTo (single_list_alloc.Right, single_list_alloc.Top);
                        cairo_context.Color = Theme.Colors.GetWidgetColor (GtkColorClass.Text, StateType.Normal);
                        cairo_context.Stroke ();
                        cairo_context.Restore ();
                    }

                    if (Selection != null && Selection.FocusedCell.Row == ri && !Selection.Contains (ri) && AllowSelect) {
                        CairoCorners corners = CairoCorners.All;

                        if (Selection.Contains (ri - 1)) {
                            corners &= ~(CairoCorners.TopLeft | CairoCorners.TopRight);
                        }

                        if (Selection.Contains (ri + 1)) {
                            corners &= ~(CairoCorners.BottomLeft | CairoCorners.BottomRight);
                        }

                        Theme.DrawRowSelection (cairo_context, single_list_alloc.X, single_list_alloc.Y,
                            single_list_alloc.Width, single_list_alloc.Height, false, true,
                            Theme.Colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected), corners);
                    }

                    if (selection_height > 0) {
                        Theme.DrawRowSelection (cairo_context, list_rendering_alloc.X, selection_y, list_rendering_alloc.Width, selection_height);
                        selection_height = 0;
                    }

                    PaintRow (ri, single_list_alloc, StateType.Normal);
                }

                single_list_alloc.Y += single_list_alloc.Height;
            }

            if (selection_height > 0) {
                Theme.DrawRowSelection (cairo_context, list_rendering_alloc.X, selection_y,
                    list_rendering_alloc.Width, selection_height);
            }

            if (Selection != null && Selection.Count > 1 &&
                !selected_focus_alloc.Equals (Rectangle.Zero) && HasFocus) {
                Theme.DrawRowSelection (cairo_context, selected_focus_alloc.X, selected_focus_alloc.Y,
                    selected_focus_alloc.Width, selected_focus_alloc.Height, false, true,
                    Theme.Colors.GetWidgetColor (GtkColorClass.Dark, StateType.Selected));
            }

            foreach (int ri in selected_rows) {
                single_list_alloc.Y = offset + ((ri - first_row) * single_list_alloc.Height);
                PaintRow (ri, single_list_alloc, StateType.Selected);
            }

            cairo_context.ResetClip ();
        }

        private void PaintRow (int row_index, Rectangle area, StateType state)
        {
            if (column_cache == null) {
                return;
            }

            object item;
            try {
                item = model [row_index];
            } catch (ArgumentOutOfRangeException) {
                return;
            } catch (DbConnectionLostException) {
                return;
            }

            EventHandler<RowEventArgs> rowItemLoaded = RowItemLoaded;
            if (rowItemLoaded != null)
                rowItemLoaded (this, new RowEventArgs (row_index));

            bool sensitive = IsRowSensitive (item);

            Rectangle cell_area = new Rectangle ();
            cell_area.Height = RowHeight;
            cell_area.Y = area.Y;

            for (int ci = 0; ci < column_cache.Length; ci++) {
                if (!column_cache [ci].Column.Visible)
                    continue;

                if (pressed_column_is_dragging && pressed_column_index == ci) {
                    continue;
                }

                cell_area.Width = column_cache [ci].Width;
                cell_area.X = column_cache [ci].X1 + area.X;
                Rectangle cell_visible_area = cell_area;
                cell_visible_area.Offset ((int) -hadjustment.Value, 0);
                if (cell_visible_area.IntersectsWith (area))
                    PaintCell (ci, row_index, cell_area, sensitive, state, false);
            }

            if (pressed_column_is_dragging && pressed_column_index >= 0) {
                cell_area.Width = column_cache [pressed_column_index].Width;
                cell_area.X = pressed_column_x_drag + list_rendering_alloc.X - list_interaction_alloc.X;
                PaintCell (pressed_column_index, row_index, cell_area, sensitive, state, true);
            }
        }

        private void PaintCell (int column_index, int row_index, Rectangle area, bool sensitive, StateType state, bool dragging)
        {
            Cell cell = column_cache [column_index].Column.ListCell;
            try {
                cell.BindListItem (row_index);
            } catch (CellNotValidException) {
                return;
            }

            if (dragging) {
                Cairo.Color fill_color = Theme.Colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal);
                fill_color.A = 0.5;
                cairo_context.Color = fill_color;
                cairo_context.Rectangle (area.X, area.Y, area.Width, area.Height);
                cairo_context.Fill ();
            }

            cairo_context.Save ();
            cairo_context.Translate (area.X, area.Y);
            cell_context.Area = area;
            cell_context.Sensitive = sensitive;
            ISizeRequestCell sr_cell = cell as ISizeRequestCell;
            if (sr_cell != null && sr_cell.RestrictSize) {
                int w, h;
                sr_cell.GetSize (out w, out h);
                column_cache [column_index].Column.MinWidth = w;
                column_cache [column_index].Column.MaxWidth = w;
            }
            cell.Render (cell_context, dragging ? StateType.Normal : state, area.Width, area.Height, new CellPosition (column_index, row_index));
            cairo_context.Restore ();
        }

        private void PaintDraggingColumn (Rectangle clip)
        {
            if (!pressed_column_is_dragging || pressed_column_index < 0) {
                return;
            }

            CachedColumn column = column_cache [pressed_column_index];

            int x = pressed_column_x_drag + Allocation.X + 1 - (int) hadjustment.Value;

            Cairo.Color fill_color = Theme.Colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal);
            fill_color.A = 0.45;

            Cairo.Color stroke_color = Theme.Colors.GetWidgetColor (
                GtkColorClass.Base, StateType.Normal).ColorShade (0.0);
            stroke_color.A = 0.3;

            cairo_context.Rectangle (x, header_rendering_alloc.Bottom + 1, column.Width - 2,
                list_rendering_alloc.Bottom - header_rendering_alloc.Bottom - 1);
            cairo_context.Color = fill_color;
            cairo_context.Fill ();

            cairo_context.MoveTo (x - 0.5, header_rendering_alloc.Bottom + 0.5);
            cairo_context.LineTo (x - 0.5, list_rendering_alloc.Bottom + 0.5);
            cairo_context.LineTo (x + column.Width - 1.5, list_rendering_alloc.Bottom + 0.5);
            cairo_context.LineTo (x + column.Width - 1.5, header_rendering_alloc.Bottom + 0.5);

            cairo_context.Color = stroke_color;
            cairo_context.LineWidth = 1.0;
            cairo_context.Stroke ();
        }

        private void InvalidateList ()
        {
            if (IsRealized) {
                QueueDrawArea (list_rendering_alloc.X, list_rendering_alloc.Y, list_rendering_alloc.Width, list_rendering_alloc.Height);
            }
        }

        private void InvalidateHeader ()
        {
            if (IsRealized) {
                QueueDrawArea (header_rendering_alloc.X, header_rendering_alloc.Y, header_rendering_alloc.Width, header_rendering_alloc.Height);
            }
        }

        private void InvalidateFooter ()
        {
            if (IsRealized) {
                QueueDrawArea (footer_rendering_alloc.X, footer_rendering_alloc.Y, footer_rendering_alloc.Width, footer_rendering_alloc.Height);
            }
        }

        private bool rules_hint = false;
        public bool RulesHint
        {
            get { return rules_hint; }
            set
            {
                rules_hint = value;
                InvalidateList ();
            }
        }

        private ListViewRowHeightHandler row_height_handler;
        public virtual ListViewRowHeightHandler RowHeightProvider
        {
            get { return row_height_handler; }
            set
            {
                if (value != row_height_handler) {
                    row_height_handler = value;
                    RecomputeRowHeight = true;
                }
            }
        }

        private bool recompute_row_height = true;
        protected bool RecomputeRowHeight
        {
            get { return recompute_row_height; }
            set
            {
                recompute_row_height = value;
                if (value && IsMapped && IsRealized) {
                    QueueDraw ();
                }
            }
        }

        private int row_height = 32;
        private int RowHeight
        {
            get
            {
                if (RecomputeRowHeight) {
                    row_height = RowHeightProvider != null
                        ? RowHeightProvider (this)
                        : CellText.ComputeRowHeight (this, CellStyleProvider);

                    header_height = 0;
                    RecalculateWindowSizes (Allocation);

                    RecomputeRowHeight = false;
                }

                return row_height;
            }
        }
    }
}
