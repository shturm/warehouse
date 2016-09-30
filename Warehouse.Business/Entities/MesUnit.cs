//
// MesUnit.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/12/2006
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

using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Entities
{
    public class MesUnit
    {
        #region Private members

        private string name;

        #endregion

        #region Public properties

        [DbColumn (DataField.ItemMeasUnit)]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        #endregion

        public static LazyListModel<MesUnit> GetAll ()
        {
            return BusinessDomain.DataAccessProvider.GetAllUnits<MesUnit> ();
        }

        public static MesUnit [] GetByItem (Item item)
        {
            ObjectsContainer<string, string> ret = BusinessDomain.DataAccessProvider.GetUnitsForItem (item.Id);
            return ret != null ?
                new [] { new MesUnit { Name = ret.Value1 }, new MesUnit { Name = ret.Value2 } } :
                new MesUnit [0];
        }
    }
}