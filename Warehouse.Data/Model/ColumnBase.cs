//
// ColumnBase.cs
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

using System.Collections;

namespace Warehouse.Data.Model
{
    public abstract class ColumnBase
    {
        public abstract bool IsSortable { get; set; }
        public abstract string SortKey { get; set; }
        public abstract string Binding { get; set; }
        public abstract SortDirection SortDirection { get; set; }
        public abstract IComparer Comparer { get; }

        public static bool operator == (ColumnBase a, ColumnBase b)
        {
            bool aIsNull = ReferenceEquals (a, null);
            bool bIsNull = ReferenceEquals (b, null);

            if (aIsNull && bIsNull)
                return true;

            if (aIsNull || bIsNull)
                return false;

            if (ReferenceEquals (a, b))
                return true;

            if (a.IsSortable != b.IsSortable)
                return false;

            if (a.SortKey != b.SortKey)
                return false;

            if (a.SortDirection != b.SortDirection)
                return false;

            return a.Binding == b.Binding;
        }

        public static bool operator != (ColumnBase a, ColumnBase b)
        {
            return !(a == b);
        }

        public bool Equals (ColumnBase obj)
        {
            return this == obj;
        }

        public override bool Equals (object obj)
        {
            return this == (obj as ColumnBase);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
