//
// ListViewGraphics.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Created:
//   10/18/2007
//
// Copyright (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using Gtk;
using Gdk;
using Cairo;
using Color = Cairo.Color;

namespace Warehouse.Component.ListView
{
    public enum ColumnBackgroundType
    {
        Normal,
        Hint,
        Sorted,
        SortedHint
    }

    public class ListViewGraphics
    {
        #region Fields

        private const int BORDER_RADIUS = 5;

        private bool refreshing = false;
        private Cairo.Color [] gtk_colors;

        private Cairo.Color selection_fill;
        private Cairo.Color selection_stroke;

        private Cairo.Color view_fill;
        private Cairo.Color view_fill_transparent;

        private Cairo.Color border_color;

        private readonly Widget widget;

        #endregion

        #region Properties

        public Cairo.Color ViewFill
        {
            get { return view_fill; }
        }

        public Cairo.Color ViewFillTransparent
        {
            get { return view_fill_transparent; }
        }

        public Cairo.Color SelectionFill
        {
            get { return selection_fill; }
        }

        public Cairo.Color SelectionStroke
        {
            get { return selection_stroke; }
        }

        #endregion

        public ListViewGraphics (Widget widget)
        {
            this.widget = widget;
            widget.StyleSet += delegate { RefreshColors (); };
        }

        public Cairo.Color GetWidgetColor (GtkColorClass @class, StateType state)
        {
            if (gtk_colors == null) {
                RefreshColors ();
            }

            return gtk_colors [(int) @class * (int) GtkColorClass.Foreground + (int) state];
        }

        public void RefreshColors ()
        {
            if (refreshing) {
                return;
            }

            refreshing = true;

            int mc = (int) GtkColorClass.Foreground;
            int ms = (int) StateType.Insensitive;

            if (gtk_colors == null) {
                gtk_colors = new Cairo.Color [(mc + 1) * (ms + 1)];
            }

            for (int c = (int) GtkColorClass.Light; c <= mc; c++) {
                for (int s = (int) StateType.Normal; s <= ms; s++) {
                    Gdk.Color color = Gdk.Color.Zero;

                    if (widget != null && widget.IsRealized) {
                        switch ((GtkColorClass) c) {
                            case GtkColorClass.Light: color = widget.Style.LightColors [s]; break;
                            case GtkColorClass.Mid: color = widget.Style.MidColors [s]; break;
                            case GtkColorClass.Dark: color = widget.Style.DarkColors [s]; break;
                            case GtkColorClass.Base: color = widget.Style.BaseColors [s]; break;
                            case GtkColorClass.Background: color = widget.Style.Backgrounds [s]; break;
                            case GtkColorClass.Foreground: color = widget.Style.Foregrounds [s]; break;
                        }
                    } else {
                        color = new Gdk.Color (0, 0, 0);
                    }

                    gtk_colors [c * mc + s] = color.ToCairoColor ();
                }
            }

            selection_fill = GetWidgetColor (GtkColorClass.Dark, StateType.Active);
            selection_stroke = GetWidgetColor (GtkColorClass.Background, StateType.Selected);

            view_fill = GetWidgetColor (GtkColorClass.Base, StateType.Normal);
            view_fill_transparent = view_fill;
            view_fill_transparent.A = 0;

            border_color = GetWidgetColor (GtkColorClass.Dark, StateType.Active);

            refreshing = false;
        }

        public void DrawHeaderBackground (Cairo.Context cr, Gdk.Rectangle alloc, int bottom_offset, bool fill)
        {
            Cairo.Color gtk_background_color = GetWidgetColor (GtkColorClass.Background, StateType.Normal);
            Cairo.Color gtk_base_color = GetWidgetColor (GtkColorClass.Base, StateType.Normal);
            Cairo.Color light_color = gtk_background_color.ColorShade (1.1);
            Cairo.Color dark_color = gtk_background_color.ColorShade (0.95);

            CairoCorners corners = CairoCorners.TopLeft | CairoCorners.TopRight;

            if (fill) {
                LinearGradient grad = new LinearGradient (alloc.X, alloc.Y, alloc.X, alloc.Y + alloc.Height);
                grad.AddColorStop (0, light_color);
                grad.AddColorStop (0.75, dark_color);
                grad.AddColorStop (0, light_color);

                cr.Pattern = grad;
                cr.RoundedRectangle (alloc.X, alloc.Y, alloc.Width, alloc.Height - bottom_offset, BORDER_RADIUS, corners);
                cr.Fill ();

                cr.Color = gtk_base_color;
                cr.Rectangle (alloc.X, alloc.Y + alloc.Height - bottom_offset, alloc.Width, bottom_offset);
                cr.Fill ();
            } else {
                cr.Color = gtk_base_color;
                cr.RoundedRectangle (alloc.X, alloc.Y, alloc.Width, alloc.Height, BORDER_RADIUS, corners);
                cr.Fill ();
            }

            cr.LineWidth = 1.0;
            cr.Translate (alloc.X + 0.5, alloc.Y + 0.5);

            cr.Color = border_color;
            cr.RoundedRectangle (alloc.X, alloc.Y, alloc.Width - 1, alloc.Height + 4, BORDER_RADIUS, corners);
            cr.Stroke ();

            if (fill) {
                cr.LineWidth = 1;
                cr.Antialias = Cairo.Antialias.None;
                cr.MoveTo (alloc.X + 1, alloc.Y + alloc.Height - 1 - bottom_offset);
                cr.LineTo (alloc.X + alloc.Width - 1, alloc.Y + alloc.Height - 1 - bottom_offset);
                cr.Stroke ();
                cr.Antialias = Cairo.Antialias.Default;
            }
        }

        public void DrawHeaderSeparator (Cairo.Context cr, Gdk.Rectangle alloc, int x, int bottom_offset)
        {
            Cairo.Color gtk_background_color = GetWidgetColor (GtkColorClass.Background, StateType.Normal);
            Cairo.Color dark_color = gtk_background_color.ColorShade (0.80);
            Cairo.Color light_color = gtk_background_color.ColorShade (1.1);

            int y_1 = alloc.Y + 3;
            int y_2 = alloc.Y + alloc.Height - 3 - bottom_offset;

            cr.LineWidth = 1;
            cr.Antialias = Cairo.Antialias.None;

            cr.Color = dark_color;
            cr.MoveTo (x, y_1);
            cr.LineTo (x, y_2);
            cr.Stroke ();

            cr.Color = light_color;
            cr.MoveTo (x + 1, y_1);
            cr.LineTo (x + 1, y_2);
            cr.Stroke ();

            cr.Antialias = Cairo.Antialias.Default;
        }

        public void DrawHeaderHighlight (Cairo.Context cr, Gdk.Rectangle alloc, int bottom_offset)
        {
            Cairo.Color gtk_selection_color = GetWidgetColor (GtkColorClass.Background, StateType.Selected);
            Cairo.Color light_color = gtk_selection_color.ColorShade (1.6);
            Cairo.Color dark_color = gtk_selection_color.ColorShade (1.3);

            LinearGradient grad = new LinearGradient (alloc.X, alloc.Y + 2, alloc.X, alloc.Y + alloc.Height - 3 - bottom_offset);
            grad.AddColorStop (0, light_color);
            grad.AddColorStop (1, dark_color);

            cr.Pattern = grad;
            cr.Rectangle (alloc.X, alloc.Y + 2, alloc.Width - 2, alloc.Height - 2 - bottom_offset);
            cr.Fill ();
        }

        public void DrawFooterBackground (Cairo.Context cr, Gdk.Rectangle alloc, int top_offset, bool fill)
        {
            Color gtk_background_color = GetWidgetColor (GtkColorClass.Background, StateType.Normal);
            Color gtk_base_color = GetWidgetColor (GtkColorClass.Base, StateType.Normal);
            Color light_color = gtk_background_color.ColorShade (1.1);
            Color dark_color = gtk_background_color.ColorShade (0.95);

            const CairoCorners corners = CairoCorners.BottomLeft | CairoCorners.BottomRight;

            if (fill) {
                LinearGradient grad = new LinearGradient (alloc.X, alloc.Y, alloc.X, alloc.Y + alloc.Height);
                grad.AddColorStop (0, light_color);
                grad.AddColorStop (0.75, dark_color);
                grad.AddColorStop (0, light_color);

                cr.Pattern = grad;
                cr.RoundedRectangle (alloc.X, alloc.Y + top_offset, alloc.Width, alloc.Height - top_offset, BORDER_RADIUS, corners);
                cr.Fill ();

                cr.Color = gtk_base_color;
                cr.Rectangle (alloc.X, alloc.Y, alloc.Width, top_offset);
                cr.Fill ();
            } else {
                cr.Color = gtk_base_color;
                cr.RoundedRectangle (alloc.X, alloc.Y, alloc.Width, alloc.Height, BORDER_RADIUS, corners);
                cr.Fill ();
            }

            cr.LineWidth = 1.0;
            cr.Translate (alloc.Y + 0.5, alloc.Y - 0.5);

            cr.Color = border_color;
            cr.RoundedRectangle (alloc.X, alloc.Y - 4, alloc.Width - 1, alloc.Height + 4, BORDER_RADIUS, corners);
            cr.Stroke ();

            if (fill) {
                cr.LineWidth = 1;
                cr.Antialias = Cairo.Antialias.None;
                cr.MoveTo (alloc.X + 1, alloc.Y + 1 + top_offset);
                cr.LineTo (alloc.X + alloc.Width - 1, alloc.Y + 1 + top_offset);
                cr.Stroke ();
                cr.Antialias = Cairo.Antialias.Default;
            }
        }

        public void DrawFooterSeparator (Cairo.Context cr, Gdk.Rectangle alloc, int x, int top_offset)
        {
            Color gtk_background_color = GetWidgetColor (GtkColorClass.Background, StateType.Normal);
            Color dark_color = gtk_background_color.ColorShade (0.80);
            Color light_color = gtk_background_color.ColorShade (1.1);

            int y_1 = alloc.Y + 3 + top_offset;
            int y_2 = alloc.Y + alloc.Height - 3;

            cr.LineWidth = 1;
            cr.Antialias = Antialias.None;

            cr.Color = dark_color;
            cr.MoveTo (x, y_1);
            cr.LineTo (x, y_2);
            cr.Stroke ();

            cr.Color = light_color;
            cr.MoveTo (x + 1, y_1);
            cr.LineTo (x + 1, y_2);
            cr.Stroke ();

            cr.Antialias = Cairo.Antialias.Default;
        }

        public void DrawLeftBorder (Context cr, Gdk.Rectangle alloc)
        {
            DrawBorder (cr, alloc.X + 1, alloc);
        }

        public void DrawRightBorder (Context cr, Gdk.Rectangle alloc)
        {
            DrawBorder (cr, alloc.X + alloc.Width, alloc);
        }

        private void DrawBorder (Context cr, int x, Gdk.Rectangle alloc)
        {
            cr.LineWidth = 1.0;
            cr.Antialias = Antialias.None;

            cr.Color = border_color;
            cr.MoveTo (x, alloc.Y);
            cr.LineTo (x, alloc.Y + alloc.Height);
            cr.Stroke ();

            cr.Antialias = Antialias.Default;
        }

        public void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height)
        {
            Color selection_color = GetWidgetColor (GtkColorClass.Background, StateType.Selected);

            LinearGradient grad = new LinearGradient (x, y, x, y + height);
            grad.AddColorStop (0, selection_color.ColorShade (1.1));
            grad.AddColorStop (1, selection_color.ColorShade (0.85));

            cr.Pattern = grad;
            cr.RoundedRectangle (x, y, width, height, BORDER_RADIUS);
            cr.Fill ();

            cr.LineWidth = 0.5;
            cr.Color = selection_color.ColorShade (0.75);
            cr.RoundedRectangle (x + 1, y + 1, width - 2, height - 2, BORDER_RADIUS);
            cr.Stroke ();
        }

        public void DrawCellBackground (Context cr, ColumnBackgroundType bgType , int x, int y, int width, int height)
        {
            Color bg_color = GetWidgetColor (GtkColorClass.Background, StateType.Active);
            Color bg_normal = GetWidgetColor (GtkColorClass.Background, StateType.Normal);

            switch (bgType) {
                case ColumnBackgroundType.Normal:
                    cr.Color = bg_normal;
                    cr.Rectangle (x, y, width, height);
                    cr.Fill ();
                    break;
                case ColumnBackgroundType.Hint:
                    cr.Color = bg_color.ColorShade (1.2);
                    cr.Rectangle (x, y, width, height);
                    cr.Fill ();
                    break;
                case ColumnBackgroundType.Sorted:
                    cr.Color = bg_color.ColorShade (1.2);
                    cr.Rectangle (x, y, width, height);
                    cr.Fill ();
                    break;
                case ColumnBackgroundType.SortedHint:
                    cr.Color = bg_color.ColorShade (1.1); ;
                    cr.Rectangle (x, y, width, height);
                    cr.Fill ();
                    break;
                default:
                    throw new ArgumentOutOfRangeException ("bgType");
            }
        }
    }
}
