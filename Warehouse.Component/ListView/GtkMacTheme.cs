// 
// GtkMacTheme.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
// 
// Created:
//    26.04.2012
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

using Cairo;
using Gtk;
using Warehouse.Data;
using Rectangle = Gdk.Rectangle;

namespace Warehouse.Component.ListView
{
    public class GtkMacTheme : GtkTheme
    {
        protected internal GtkMacTheme (Widget widget)
            : base (widget)
        {
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
            cr.Color = new Color (92 / 255d, 139 / 255d, 195 / 255d);
            cr.FillPreserve ();
            cr.Color = new Color (85 / 255d, 131 / 255d, 181 / 255d);
            cr.Stroke ();

            if (type == SortDirection.Ascending) {
                cr.MoveTo (x1, y1 - 1);
                cr.LineTo (x2, y1 - 1);
            } else {
                cr.MoveTo (x1, y2 + 1);
                cr.LineTo (x2, y2 + 1);
            }
            cr.Color = new Color (190 / 255d, 221 / 255d, 248 / 255d);
            cr.Stroke ();

            cr.Translate (-0.5, -0.5);
        }

        public override void DrawHeaderBackground (Context cr, Rectangle alloc)
        {
            LinearGradient grad = new LinearGradient (alloc.X, alloc.Y, alloc.X, alloc.Bottom);
            grad.AddColorStop (0.0, new Color (255d / 255d, 255d / 255d, 255d / 255d));
            grad.AddColorStop (0.2, new Color (255d / 255d, 255d / 255d, 255d / 255d));
            grad.AddColorStop (0.2, new Color (252d / 255d, 252d / 255d, 252d / 255d));
            grad.AddColorStop (0.5, new Color (244d / 255d, 244d / 255d, 244d / 255d));
            grad.AddColorStop (0.5, new Color (236d / 255d, 236d / 255d, 236d / 255d));
            grad.AddColorStop (0.8, new Color (236d / 255d, 236d / 255d, 236d / 255d));
            grad.AddColorStop (1.0, new Color (244d / 255d, 244d / 255d, 244d / 255d));

            cr.Pattern = grad;
            cr.RoundedRectangle (alloc.X, alloc.Y, alloc.Width, alloc.Height, Context.Radius, CairoCorners.TopLeft | CairoCorners.TopRight);
            cr.Fill ();
            grad.Destroy ();

            cr.Color = border_color;
            cr.Rectangle (alloc.X, alloc.Bottom, alloc.Width, BorderWidth);
            cr.Fill ();
        }

        public override void DrawHeaderSortBackground (Context cr, Rectangle alloc)
        {
            //Do not overlap the column divider on the left
            alloc.X += 2;
            alloc.Width -= 2;

            LinearGradient grad = new LinearGradient (alloc.X, alloc.Y, alloc.X, alloc.Bottom);
            grad.AddColorStop (0.0, new Color (205d / 255d, 228d / 255d, 248d / 255d));
            grad.AddColorStop (0.4, new Color (145d / 255d, 196d / 255d, 240d / 255d));
            grad.AddColorStop (0.6, new Color (136d / 255d, 192d / 255d, 242d / 255d));
            grad.AddColorStop (1.0, new Color (188d / 255d, 230d / 255d, 246d / 255d));

            cr.Pattern = grad;
            cr.Rectangle (alloc.X, alloc.Y, alloc.Width, alloc.Height - BorderWidth);
            cr.Fill ();

            grad.Destroy ();
        }

        public override void DrawRowSelection (Context cr, int x, int y, int width, int height, bool filled, bool stroked, Color color, CairoCorners corners = CairoCorners.All)
        {
            Color selection_color = new Color (56d / 255, 117d / 255, 215d / 255, color.A);
            Color selection_stroke = selection_color.ColorShade (0.85);
            selection_stroke.A = color.A;

            if (filled) {
                cr.Color = selection_color;
                cr.RoundedRectangle (x, y, width, height, Context.Radius, corners, true);
                cr.Fill ();
            }

            if (!stroked)
                return;

            cr.LineWidth = 1.0;
            cr.Color = selection_stroke;
            cr.RoundedRectangle (x + 0.5, y + 0.5, width - 1, height - 1, Context.Radius, corners, true);
            cr.Stroke ();
        }
    }
}
