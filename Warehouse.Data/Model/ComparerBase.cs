//
// ComparerBase.cs
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
using System.Collections;

namespace Warehouse.Data.Model
{
    public abstract class ComparerBase : IComparer
    {
        private SortDirection sortDirection;

        public SortDirection SortDirection
        {
            get { return sortDirection; }
            set { sortDirection = value; }
        }

        public ComparerBase ()
            : this (SortDirection.Ascending)
        {
        }

        public ComparerBase (SortDirection direction)
        {
            sortDirection = direction;
        }

        public abstract int Compare (object x, object y);

        public static ComparerBase GetComparer (Type type)
        {
            switch (type.ToString ()) {
                case "System.Boolean":
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.UInt16":
                case "System.UInt32":
                case "System.UInt64":
                case "System.Single":
                case "System.Double":
                case "System.Decimal":
                case "System.DateTime":
                case "System.Guid":
                case "System.String":
                case "System.Char":
                    return new BasicComparer ();
                default:
                    return null;
            }
        }
    }
}