//
// PageDistribution.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/04/2007
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

namespace Warehouse.Component.Documenting
{
    public class PageDistribution
    {
        private int leftColumn;
        private int rightColumn;
        private int upperRow;
        private int lowerRow;

        public int LeftColumn
        {
            get { return leftColumn; }
            set { leftColumn = value; }
        }

        public int RightColumn
        {
            get { return rightColumn; }
            set { rightColumn = value; }
        }

        public int UpperRow
        {
            get { return upperRow; }
            set { upperRow = value; }
        }

        public int LowerRow
        {
            get { return lowerRow; }
            set { lowerRow = value; }
        }

        public bool IsInPage ()
        {
            return IsInPage (DocumentHelper.CurrentPageSettings);
        }

        private bool IsInPage (PageSettings page)
        {

            if (page.Column < leftColumn || rightColumn < page.Column)
                return false;

            return page.Row >= upperRow && lowerRow >= page.Row;
        }

        public bool IsInAnyPage (int pageColumns, int startPage, int endPage)
        {
            for (int i = startPage; i <= endPage; i++)
                if (IsInPage (new PageSettings { Column = i % pageColumns, Row = i / pageColumns }))
                    return true;

            return false;
        }

        public int GetFirstPage (int pageColumns)
        {
            return leftColumn + upperRow * pageColumns;
        }

        public int GetLastPage (int pageColumns)
        {
            return rightColumn + lowerRow * pageColumns;
        }

        public int GetColumnsUsed ()
        {
            return (1 + rightColumn - leftColumn);
        }

        public int GetRowsUsed ()
        {
            return (1 + lowerRow - upperRow);
        }

        public int GetPagesUsed ()
        {
            return GetColumnsUsed () * GetRowsUsed ();
        }

        public override string ToString ()
        {
            return string.Format ("l:{0} r:{1} t:{2} b:{3}", leftColumn, rightColumn, upperRow, lowerRow);
        }
    }
}
