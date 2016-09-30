//
// ReportQueryItemsMinimalQuantities.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   11.05.2009
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

namespace Warehouse.Business.Reporting
{
	public class ReportQueryItemsMinimalQuantities : ReportQueryBase
	{
        public override string HelpFile
        {
            get { return "ReportGoodsMinimalQuantities.html"; }
        }

        public ReportQueryItemsMinimalQuantities ()
		{
            Name = Translator.GetString ("Items below Minimal Availability Report");

            AppendFilter (new FilterFind (true, true, DataFilterLabel.ItemCode, DataField.ItemCode));
            AppendFilter (new FilterFind (true, true, DataFilterLabel.ItemName, DataField.ItemName));
            AppendFilter (new FilterFind (false, false, 
                DataFilterLabel.ItemBarcode, DataField.ItemBarcode1, DataField.ItemBarcode2, DataField.ItemBarcode3));
            AppendFilter (new FilterFind (false, false, 
                DataFilterLabel.ItemCatalog, DataField.ItemCatalog1, DataField.ItemCatalog2, DataField.ItemCatalog3));
            AppendFilter (new FilterGroupFind (false, true, DataFilterLabel.ItemsGroupName, DataField.ItemsGroupName));
            AppendFilter (new FilterFind (true, true, DataFilterLabel.LocationName, DataField.LocationName));
            AppendFilter (new FilterCompare (true, true, DataFilterLabel.StoreItemAvailableQuantitySum, DataField.StoreAvailableQuantity));
		    AppendOrder (new Order (DataField.ItemCode, DataField.ItemName, DataField.LocationName));
		}

		public override DataQueryResult ExecuteReport (DataQuery dataQuery)
        {
            return BusinessDomain.DataAccessProvider.ReportItemsMinimalQuantities (dataQuery);
		}
	}
}
