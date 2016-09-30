//
// PageSettings.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   11/12/2006
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

namespace Warehouse.Component.Documenting
{
    public class PageSettings : ICloneable
    {
        int column;
        private int columns;
        int row;
        double width;
        double height;
        double inPageDrawTop;
        double inPageDrawLeft;

        public int Column
        {
            get { return column; }
            set { column = value; }
        }

        public int Columns
        {
            get { return columns; }
        }

        public int Row
        {
            get { return row; }
            set { row = value; }
        }

        public double Width
        {
            get { return width; }
            set { width = value; }
        }

        public double Height
        {
            get { return height; }
            set { height = value; }
        }

        public void SetDrawStart (PointD start)
        {
            inPageDrawLeft = start.X;
            inPageDrawTop = start.Y;
        }

        public void SetPageNumber (PageDistribution dist, int pageNumber)
        {
            SetPageNumber (dist.GetColumnsUsed (), pageNumber);
        }

        public void SetPageNumber (int pageColumns, int pageNumber)
        {
            row = pageNumber / pageColumns;
            column = pageNumber % pageColumns;
            columns = pageColumns;
        }

        public RectangleD GetPageRectangle ()
        {
            return new RectangleD (column * width, row * height, width, height);
        }

        public PointD GetInPageDrawLocation (PointD location, int? pageColumn = null, int? pageRow = null)
        {
            location.X += inPageDrawLeft - (pageColumn ?? column) * width;
            location.Y += inPageDrawTop - (pageRow ?? row) * height;

            return location;
        }

        #region ICloneable Members

        public object Clone ()
        {
            return MemberwiseClone ();
        }

        #endregion
    }
}
