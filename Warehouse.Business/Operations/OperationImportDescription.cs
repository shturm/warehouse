//
// OperationImportDescription.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   01.03.2013
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

using Warehouse.Business.Entities;
using Warehouse.Data;

namespace Warehouse.Business.Operations
{
    public class OperationImportDescription : IStrongEntity, IPersistableEntity<OperationImportDescription>
    {
        private double quantity = 1;

        [ExchangeProperty ("Item", true, DataField.ItemName)]
        public string ItemName { get; set; }

        public Item ResolvedItem { get; set; }

        [ExchangeProperty ("Quantity", false, DataField.OperationDetailQuantity)]
        public double Quantity
        {
            get { return quantity; }
            set { quantity = value; }
        }

        public bool Validate (ValidateCallback callback, StateHolder state)
        {
            if (string.IsNullOrWhiteSpace (ItemName))
                if (!callback (Translator.GetString ("Item name cannot be empty!"), ErrorSeverity.Error, 0, state))
                    return false;

            ResolvedItem = Item.GetByAny (ItemName);
            if (ResolvedItem == null)
                if (!callback (string.Format (Translator.GetString ("Item \"{0}\" cannot be found!"), ItemName), ErrorSeverity.Error, 0, state))
                    return false;

            return true;
        }

        public OperationImportDescription CommitChanges ()
        {
            return this;
        }
    }
}
