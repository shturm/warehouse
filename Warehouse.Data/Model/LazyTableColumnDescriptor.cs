//
// LazyTableColumnDescriptor.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   02/13/2009
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
using System.ComponentModel;

namespace Warehouse.Data.Model
{
    public class LazyTableColumnDescriptor : PropertyDescriptor
    {
        private readonly DbColumnManager manager;

        public LazyTableColumnDescriptor (DbColumnManager manager)
            : base (manager.DbField.Field, null)
        {
            this.manager = manager;
        }

        public override bool CanResetValue (object component)
        {
            return false;
        }

        public override object GetValue (object component)
        {
            return manager.GetValue (component);
        }

        public override void ResetValue (object component)
        {
        }

        public override void SetValue (object component, object value)
        {
            manager.SetValue (component, value);
        }

        public override bool ShouldSerializeValue (object component)
        {
            return false;
        }

        public override Type ComponentType
        {
            get { return typeof (LazyTableDataRow); }
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type PropertyType
        {
            get { return typeof (object); }
        }
    }
}
