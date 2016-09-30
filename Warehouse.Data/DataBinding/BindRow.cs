//
// BindRow.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   03/28/2007
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

using System.Collections.Generic;

namespace Warehouse.Data.DataBinding
{
    public class BindRow<T>
    {
        private readonly BindManager<T> manager;
        private readonly Dictionary<int, object> values = new Dictionary<int, object> ();
        private readonly T rowObject;

        public BindManager<T> Manager
        {
            get { return manager; }
        }

        public object this [int index]
        {
            get
            {
                if (!values.ContainsKey (index))
                    values [index] = manager.Columns.GetRowValue (rowObject, index);

                return values [index];
            }
            set
            {
                values [index] = value;
                manager.Columns.SetRowValue (rowObject, index, value);
            }
        }

        public object this [string name]
        {
            get
            {
                int index = manager.Columns [name].Index;
                if (!values.ContainsKey (index))
                    values [index] = manager.Columns.GetRowValue (rowObject, index);

                return values [index];
            }
            set
            {
                int index = manager.Columns [name].Index;
                values [index] = value;
                manager.Columns.SetRowValue (rowObject, index, value);
            }
        }

        public T Value
        {
            get { return rowObject; }
        }

        internal BindRow (BindManager<T> manager, T rowObject)
        {
            this.manager = manager;
            this.rowObject = rowObject;
        }

        public object [] ToArray ()
        {
            return manager.Columns.GetRowValues (rowObject);
        }
    }
}
