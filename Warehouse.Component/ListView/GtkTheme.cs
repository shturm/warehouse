//
// GtkTheme.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Created:
//   09/23/2008
//
// Copyright (C) 2007-2008 Novell, Inc.
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

using Cairo;
using Gtk;
using Warehouse.Data;
using Rectangle = Gdk.Rectangle;

namespace Warehouse.Component.ListView
{
    public class GtkTheme : Theme
    {
        private Color rule_color;
        protected Color border_color;

        protected GtkTheme (Widget widget)
            : base (widget)
        {
        }

        public static GtkTheme CreateTheme (Widget widget)
        {
            return PlatformHelper.Platform == PlatformTypes.MacOSX ?
                new GtkMacTheme (widget) : new GtkTheme (widget);
        }

        protected override void OnColorsRefreshed ()
        {
            base.OnColorsRefreshed ();

            rule_color = ViewFill.ColorShade (0.95);
            border_color = Colors.GetWidgetColor (GtkColorClass.Dark, StateType.Active);
        }

        public override void DrawArrow (Context cr, Rectangle alloc, SortDirection type)
        {
            if (type == SortDirection.None)
                return;

            double x1 = alloc.X;
            double x3 = alloc.X + alloc.Width / 2.0;
            double x2 = x3 + (x3 - x1);
            double y1 = alloc.Y;
            double y2 = alloc.Bottom;

            cr.LineWidth = 1;
            cr.Translate (0.5, 0.5);
            if (type == SortDirection.Ascending) {
                cr.MoveTo (x1, y1);
                cr.LineTo (x2, y1);
                cr.LineTo (x3, y2);
                cr.LineTo (x1, y1);
            } else {
                cr.MoveTo (x3, y1);
                cr.LineTo (x2, y2);
                cr.LineTo (x1, y2);
                cr.LineTo (x3, y1);
            }

            cr.Color = Colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal);
            cr.FillPreserve ();
            cr.Color = Colors.GetWidgetColor (GtkColorClass.Text, StateType.Normal);
            cr.Stroke ();
            cr.Translate (-0.5, -0.5);
        }

        protected override void DrawFrameBackground (Context cr, Rectangle alloc, Color color, Pattern pattern = null)
        {
            color.A = Context.FillAlpha;
            if (pattern != null)
                cr.Pattern = pattern;
            else
                cr.Color = color;

            cr.RoundedRectangle (alloc.X, alloc.Y, alloc.Width, alloc.Height, Context.Radius);
            cr.Fill ();
        }

        public override void DrawFrameBorder (Context cr, Rectangle alloc)
        {
            cr.LineWidth = BorderWidth;
            cr.Color = border_color;
            double offset = BorderWidth / 2.0;
            cr.RoundedRectangle (alloc.X + offset, alloc.Y + offset, alloc.Width - BorderWidth, alloc.Height - BorderWidth, Context.Radius);
            cr.Stroke ();
        }

        public override void DrawColumnHighlight (Context cr, Rectangle alloc, Color color)
        {
            Color light_color = color.ColorShade (1.6);
            Color dark_color = color.ColorShade (1.3);

            LinearGradient grad = new LinearGradient (alloc.X, alloc.Y, alloc.X, alloc.Bottom - 1);
            grad.AddColorStop (0, light_color);
            grad.AddColorStop (1, dark_color);

            cr.Pattern = grad;
            cr.Rectangle (alloc.X + 1.5, alloc.Y + 1.5, alloc.Width - 3, alloc.Height - 2);
            cr.Fill ();
            grad.Destroy ();
        }

        public override void DrawHeaderBackground (Context cr, Rectangle alloc)
        {
            Color gtk_background_color = Colors.GetWidgetColor (GtkColorClass.Background, StateType.Normal);
            Color light_color = gtk_background_color.ColorShade (1.1);
            Color dark_color = gtk_background_color.ColorShade (0.95);

            LinearGradient grad = new LinearGradient (alloc.X, alloc.Y, alloc.X, alloc.Bottom);
            grad.AddColorStop (0, light_color);
            grad.AddColorStop (0.75, dark_color);
            grad.AddColorStop (0, light_color);

            cr.Pattern = grad;
            cr.RoundedRectangle (alloc.X, alloc.Y, alloc.Width, alloc.Height, Context.Radius, CairoCorners.TopLeft | CairoCorners.TopRight);
            cr.Fill ();

            cr.Color = border_color;
            cr.Rectangle (alloc.X, alloc.Bottom, alloc.Width, BorderWidth);
            cr.Fill ();
            grad.Destroy ();
        }

        public override void DrawHeaderSortBackground (Context cr, Rectangle alloc)
        {
        }

        public override void DrawHeaderSeparator (Context cr, Rectangle alloc, int x)
        {
            Color gtk_background_color = Colors.GetWidgetColor (GtkColorClass.Background, StateType.Normal);
            Color dark_color = gtk_background_color.ColorShade (0.80);
            Color light_color = gtk_background_color.ColorShade (1.1);

            int y_1 = alloc.Top + 4;
            int y_2 = alloc.Bottom - 3;

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

            cr.Antialias = Antialias.Default;
        }

        public override void DrawFooterBackground (Context cr, Rectangle alloc)
        {
            Color gtk_background_color = Colors.GetWidgetColor (GtkColorClass.Background, StateType.Normal);
            Color light_color = gtk_background_color.ColorShade (1.05);
            Color dark_color = gtk_background_color.ColorShade (0.92);

            LinearGradient grad = new LinearGradient (alloc.X, alloc.Y, alloc.X, alloc.Bottom);
            grad.AddColorStop (0, light_color);
            grad.AddColorStop (0.75, dark_color);
            grad.AddColorStop (0, light_color);

            cr.Pattern = grad;
            cr.RoundedRectangle (alloc.X, alloc.Y, alloc.Width, alloc.Height, Context.Radius, CairoCorners.BottomLeft | CairoCorners.BottomRight);
            cr.Fill ();

            cr.Color = border_color;
            cr.Rectangle (alloc.X, alloc.Y, alloc.Width, BorderWidth);
            cr.Fill ();
            grad.Destroy ();
        }

        public override void DrawFooterSeparator (Context cr, Rectangle alloc, int x)
        {
            Color gtk_background_color = Colors.GetWidgetColor (GtkColorClass.Background, StateType.Normal);
            Color dark_color = gtk_background_color.ColorShade (0.80);
            Color light_color = gtk_background_color.ColorShade (1.1);

            int y_1 = alloc.Top + 3;
            int y_2 = alloc.Bottom - 4;

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

            cr.Antialias = Antialias.Default;
        }

        public override void DrawRowSelection (Context cr, int x, int y, int width, int height,
            bool filled, bool stroked, Color color, CairoCorners corners = CairoCorners.All)
        {
            Color selection_color = color;
            Color selection_stroke = selection_color.ColorShade (0.85);
            selection_stroke.A = color.A;

            if (filled) {
                Color selection_fill_light = selection_color.ColorShade (1.12);
                Color selection_fill_dark = selection_color;

                selection_fill_light.A = color.A;
                selection_fill_dark.A = color.A;

                LinearGradient grad = new LinearGradient (x, y, x, y + height);
                grad.AddColorStop (0, selection_fill_light);
                grad.AddColorStop (0.4, selection_fill_dark);
                grad.AddColorStop (1, selection_fill_light);

                cr.Pattern = grad;
                cr.RoundedRectangle (x, y, width, height, Context.Radius, corners, true);
                cr.Fill ();
                grad.Destroy ();
            }

            if (!stroked)
                return;

            cr.LineWidth = 1.0;
            cr.Color = selection_stroke;
            cr.RoundedRectangle (x + 0.5, y + 0.5, width - 1, height - 1, Context.Radius, corners, true);
            cr.Stroke ();
        }

        public override void DrawRowRule (Context cr, int x, int y, int width, int height)
        {
            cr.Color = new Color (rule_color.R, rule_color.G, rule_color.B, Context.FillAlpha);
            cr.Rectangle (x, y, width, height);
            cr.Fill ();
        }
    }
}
