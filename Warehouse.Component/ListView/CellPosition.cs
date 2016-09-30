//
// CellPosition.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
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

namespace Warehouse.Component.ListView
{
    public struct CellPosition
    {
        private int column;
        private int row;
        public static CellPosition Empty;

        public int Column
        {
            get { return column; }
            set { column = value; }
        }

        public int Row
        {
            get { return row; }
            set { row = value; }
        }

        public bool IsValid
        {
            get
            {
                return column >= 0 && row >= 0;
            }
        }

        static CellPosition ()
        {
            Empty.Column = -1;
            Empty.Row = -1;
        }

        public CellPosition (int column, int row)
        {
            this.column = column;
            this.row = row;
        }

        public override string ToString ()
        {
            return string.Format ("Cell position C:{0}, R:{1}", column, row);
        }

        public override bool Equals (object obj)
        {
            if (!(obj is CellPosition))
                return false;

            CellPosition pos = (CellPosition) obj;
            if (pos.row != row || pos.column != column)
                return false;

            return true;
        }

        public static bool operator == (CellPosition pos1, CellPosition pos2)
        {
            if (pos1.row != pos2.row || pos1.column != pos2.column)
                return false;

            return true;
        }

        public static bool operator != (CellPosition pos1, CellPosition pos2)
        {
            if (pos1.row != pos2.row || pos1.column != pos2.column)
                return true;

            return false;
        }

        public override int GetHashCode ()
        {
            return row.GetHashCode () + column.GetHashCode ();
        }
    }
}
