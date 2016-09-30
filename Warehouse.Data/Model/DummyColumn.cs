//
// DummyColumn.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/23/2008
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
    public class DummyColumn : ColumnBase
    {
        private bool isSortable = true;
        private string sortKey;
        private SortDirection sortDirection;
        private readonly IComparer comparer = new BasicComparer ();
        private string binding = string.Empty;

        #region IColumn Members

        public override bool IsSortable
        {
            get { return isSortable; }
            set { isSortable = value; }
        }

        public override string SortKey
        {
            get { return sortKey; }
            set { sortKey = value; }
        }

        public override string Binding
        {
            get { return binding; }
            set { binding = value; }
        }

        public override SortDirection SortDirection
        {
            get { return sortDirection; }
            set { sortDirection = value; }
        }

        public override IComparer Comparer
        {
            get { return comparer; }
        }

        #endregion

        public DummyColumn (string sortKey, SortDirection sortDirection)
        {
            this.sortKey = sortKey;
            this.sortDirection = sortDirection;
        }
    }
}
