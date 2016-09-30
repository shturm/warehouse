//
// CellTextHeader.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Created:
//   10/18/2007
//
// Copyright (C) 2007 Novell, Inc.
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

using Gdk;
using Gtk;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Component.ListView
{
    public class CellTextHeader : CellText
    {
        private const int ARROW_H_PADDING = 8;

        public CellTextHeader ()
            : base (null)
        {
            IsEditable = false;
        }

        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight, CellPosition position)
        {
            if (ParentColumn == null) {
                return;
            }

            if (parentColumn.SortDirection == SortDirection.None) {
                base.Render (context, state, cellWidth, cellHeight, position);
                return;
            }

            context.Theme.DrawHeaderSortBackground (context.Context, new Rectangle (0, 0, (int) cellWidth, (int) cellHeight));

            Rectangle arrowAlloc = new Rectangle ();
            arrowAlloc.Width = (int) (cellHeight / 3.0);
            arrowAlloc.Height = (int) (arrowAlloc.Width / 1.6);
            arrowAlloc.X = (int) cellWidth - arrowAlloc.Width - ARROW_H_PADDING;
            arrowAlloc.Y = ((int) cellHeight - arrowAlloc.Height) / 2;

            base.Render (context, state, cellWidth - arrowAlloc.Width - 2 * ARROW_H_PADDING, cellHeight, position);
            context.Theme.DrawArrow (context.Context, arrowAlloc, ParentColumn.SortDirection);
        }

        public override string ObjectToString (object obj)
        {
            return parentColumn.HeaderText;
        }
    }
}
