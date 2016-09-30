//
// CellTextEnumerator.cs
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

using Gtk;
using Warehouse.Data.Model;

namespace Warehouse.Component.ListView
{
    public class CellTextEnumerator : Cell
    {
        private Pango.Weight font_weight = Pango.Weight.Normal;
        private Pango.EllipsizeMode ellipsize_mode = Pango.EllipsizeMode.End;
        private Pango.Alignment alignment = Pango.Alignment.Center;
        private double opacity = 1.0;
        private int hPadding = 4;
        private int vPadding = 4;
        private int firstRow = 1;

        public Pango.Alignment Alignment
        {
            get { return alignment; }
            set { alignment = value; }
        }

        public virtual Pango.Weight FontWeight
        {
            get { return font_weight; }
            set { font_weight = value; }
        }

        public virtual Pango.EllipsizeMode EllipsizeMode
        {
            get { return ellipsize_mode; }
            set { ellipsize_mode = value; }
        }

        public virtual double Opacity
        {
            get { return opacity; }
            set { opacity = value; }
        }

        public int HPadding
        {
            get { return hPadding; }
            set { hPadding = value; }
        }

        public int VPadding
        {
            get { return vPadding; }
            set { vPadding = value; }
        }

        public int FirstRow
        {
            get { return firstRow; }
            set { firstRow = value; }
        }

        public CellTextEnumerator ()
            : base (null)
        {
            comparer = new BasicComparer ();
        }

        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight, CellPosition position)
        {
            PangoStyle style = parentColumn.ParentListView.QueryCellStyle (state, position);

            context.Layout.Width = (int) ((cellWidth - hPadding * 2) * Pango.Scale.PangoScale);
            context.Layout.Ellipsize = EllipsizeMode;
            context.Layout.Alignment = alignment;

            context.Layout.SetMarkup (style.GetMarkup ((firstRow + position.Row).ToString ()));
            int text_width;
            int text_height;
            context.Layout.GetPixelSize (out text_width, out text_height);

            context.Context.MoveTo (hPadding, ((int) cellHeight - text_height) / 2);
            Cairo.Color color = context.Theme.Colors.GetWidgetColor (
                context.TextAsForeground ? GtkColorClass.Foreground : GtkColorClass.Text, state);
            color.A = context.Sensitive ? 1.0 : 0.3;

            context.Context.Color = color;
            context.Context.ShowLayout (context.Layout);
        }
    }
}
