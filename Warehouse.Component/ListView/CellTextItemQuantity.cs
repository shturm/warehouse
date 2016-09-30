//
// CellTextItemQuantity.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   09.22.2011
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
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Component.ListView
{
    public class CellTextItemQuantity : CellTextQuantity
    {
        private Item rowObject;

        public CellTextItemQuantity (string propertyName)
            : base (propertyName)
        {
            FixedFaction = BusinessDomain.AppConfiguration.QuantityPrecision;
        }

        public override void BindListItem (int rowIndex)
        {
            base.BindListItem (rowIndex);

            ListView parentListView = parentColumn.ParentListView;
            IListModel model = parentListView.Model;

            try {
                rowObject = model [rowIndex] as Item;
            } catch (ArgumentOutOfRangeException) {
                throw new CellNotValidException (rowIndex);
            }
        }

        public override string ObjectToString (object obj)
        {
            if (rowObject != null && (rowObject.Type & ItemType.NonInventory) == ItemType.NonInventory)
                return "∞";

            return base.ObjectToString (obj);
        }
    }
}
