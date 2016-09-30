//
// CellText.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   10/18/2007
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
using Cairo;
using Gtk;
using Pango;
using Warehouse.Data.Model;
using Alignment = Pango.Alignment;
using Color = Cairo.Color;
using Context = Cairo.Context;

namespace Warehouse.Component.ListView
{
    public class CellProgress : Cell
    {
        private readonly string progressPropertyName;
        private double progressValue;

        public CellProgress (string textPropertyName, string progressPropertyName)
            : base (textPropertyName)
        {
            this.progressPropertyName = progressPropertyName;
            comparer = new BasicComparer ();
        }

        public override void BindListItem (int rowIndex)
        {
            base.BindListItem (rowIndex);

            ListView parentListView = parentColumn.ParentListView;
            IListModel model = parentListView.Model;

            try {
                progressValue = (double) model [rowIndex, progressPropertyName];
            } catch (ArgumentOutOfRangeException) {
                throw new CellNotValidException (rowIndex);
            }
        }

        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight, CellPosition position)
        {
            Context cairo = context.Context;

            double x = 1.5;
            double y = 1.5;
            double r = 1;
            cellWidth -= 3;
            cellHeight -= 3;
            // Draw backgound fill
            LinearGradient bgPattern = new LinearGradient (0, 0, 0, cellHeight);
            bgPattern.AddColorStop (0.0, new Color (219 / 255d, 219 / 255d, 219 / 255d));
            bgPattern.AddColorStop (0.2, new Color (233 / 255d, 233 / 255d, 233 / 255d));
            bgPattern.AddColorStop (0.2, new Color (221 / 255d, 221 / 255d, 221 / 255d));
            bgPattern.AddColorStop (0.3, new Color (221 / 255d, 221 / 255d, 221 / 255d));
            bgPattern.AddColorStop (0.3, new Color (218 / 255d, 218 / 255d, 218 / 255d));
            bgPattern.AddColorStop (0.4, new Color (218 / 255d, 218 / 255d, 218 / 255d));
            bgPattern.AddColorStop (0.8, new Color (250 / 255d, 250 / 255d, 250 / 255d));
            bgPattern.AddColorStop (1.0, new Color (221 / 255d, 221 / 255d, 221 / 255d));
            cairo.Pattern = bgPattern;
            cairo.RoundedRectangle (x - 0.5, y - 0.5, cellWidth + 1, cellHeight + 1, r);
            cairo.Fill ();

            // Draw frame
            cairo.LineWidth = 1;
            cairo.Color = new Color (181 / 255d, 181 / 255d, 181 / 255d);
            cairo.MoveTo (x + r, y);
            cairo.Arc (x + cellWidth - r, y + r, r, Math.PI * 1.5, Math.PI * 2);
            cairo.Stroke ();

            cairo.Color = new Color (215 / 255d, 215 / 255d, 215 / 255d);
            cairo.MoveTo (x + cellWidth, y + r);
            cairo.Arc (x + cellWidth - r, y + cellHeight - r, r, 0, Math.PI * 0.5);
            cairo.Stroke ();

            cairo.Color = new Color (181 / 255d, 181 / 255d, 181 / 255d);
            cairo.MoveTo (x + cellWidth - r, y + cellHeight);
            cairo.Arc (x + r, y + cellHeight - r, r, Math.PI * 0.5, Math.PI);
            cairo.Stroke ();

            cairo.Color = new Color (215 / 255d, 215 / 255d, 215 / 255d);
            cairo.MoveTo (x, y + cellHeight - r);
            cairo.Arc (x + r, y + r, r, Math.PI, Math.PI * 1.5);
            cairo.Stroke ();

            // Draw progress fill
            double progressWidth = (cellWidth - 1) * Math.Max (Math.Min (double.IsNaN (progressValue) ? 0 : progressValue, 1), 0);
            LinearGradient fillPattern = new LinearGradient (0, 0, 0, cellHeight);
            fillPattern.AddColorStop (0.0, new Color (128 / 255d, 168 / 255d, 121 / 255d));
            fillPattern.AddColorStop (0.1, new Color (128 / 255d, 168 / 255d, 121 / 255d));
            fillPattern.AddColorStop (0.2, new Color (146 / 255d, 186 / 255d, 229 / 255d));
            fillPattern.AddColorStop (0.4, new Color (83  / 255d, 149 / 255d, 223 / 255d));
            fillPattern.AddColorStop (0.5, new Color (66  / 255d, 143 / 255d, 231 / 255d));
            fillPattern.AddColorStop (0.8, new Color (119 / 255d, 201 / 255d, 255 / 255d));
            fillPattern.AddColorStop (1.0, new Color (125 / 255d, 191 / 255d, 255 / 255d));
            cairo.Pattern = fillPattern;
            cairo.Rectangle (x + 0.5, y - 0.5, progressWidth, cellHeight);
            cairo.Fill ();
            cairo.MoveTo (x + 0.5, y);
            cairo.LineTo (x + progressWidth, y);
            cairo.Color = new Color (53 / 255d, 100 / 255d, 191 / 255d);
            cairo.Stroke ();

            // Draw text
            PangoStyle style = parentColumn.ParentListView.QueryCellStyle (state, position);

            const double hPadding = 4;
            context.Layout.Width = (int) ((cellWidth - hPadding * 2) * Pango.Scale.PangoScale);
            context.Layout.Ellipsize = EllipsizeMode.End;
            context.Layout.Alignment = Alignment.Center;

            context.Layout.SetMarkup (style.GetMarkup (ObjectToString (boundObject)));
            int text_width;
            int text_height;
            context.Layout.GetPixelSize (out text_width, out text_height);

            cairo.MoveTo (x + hPadding, y + ((int) cellHeight - text_height) / 2);
            Color color = context.Theme.Colors.GetWidgetColor (
                context.TextAsForeground ? GtkColorClass.Foreground : GtkColorClass.Text, StateType.Normal);
            color.A = context.Sensitive ? 1.0 : 0.3;

            cairo.Color = color;
            cairo.ShowLayout (context.Layout);
        }
    }
}
