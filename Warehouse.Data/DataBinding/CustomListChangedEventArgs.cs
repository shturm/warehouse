//
// CustomListChangedEventArgs.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   01.27.2011
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

using System.ComponentModel;

namespace Warehouse.Data.DataBinding
{
    public class CustomListChangedEventArgs : ListChangedEventArgs
    {
        private object deletedObject;

        public object DeletedObject
        {
            get { return deletedObject; }
        }

        public CustomListChangedEventArgs (ListChangedType listChangedType, int newIndex)
            : base (listChangedType, newIndex)
        {
        }

        public CustomListChangedEventArgs (ListChangedType listChangedType, int newIndex, PropertyDescriptor propDesc)
            : base (listChangedType, newIndex, propDesc)
        {
        }

        public CustomListChangedEventArgs (ListChangedType listChangedType, PropertyDescriptor propDesc)
            : base (listChangedType, propDesc)
        {
        }

        public CustomListChangedEventArgs (ListChangedType listChangedType, int newIndex, int oldIndex, object deletedObject)
            : base (listChangedType, newIndex, oldIndex)
        {
            this.deletedObject = deletedObject;
        }

        public CustomListChangedEventArgs (ListChangedType listChangedType, int newIndex, int oldIndex)
            : base (listChangedType, newIndex, oldIndex)
        {
        }
    }
}
