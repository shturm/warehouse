//
// ReportQueryItems.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   09.30.2010
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
    public class ReportQueryItems : ReportQueryBase
    {
        public override string HelpFile
        {
            get { return "ReportGoods.html"; }
        }

        public ReportQueryItems ()
        {
            Name = Translator.GetString ("Items Report");

            AppendFilter (new FilterFind (true, true, DataFilterLabel.ItemCode, DataField.ItemCode));
            AppendFilter (new FilterFind (true, true, DataFilterLabel.ItemName, DataField.ItemName));
            AppendFilter (new FilterFind (false, true, DataFilterLabel.ItemMeasUnit, DataField.ItemMeasUnit));
            AppendFilter (new FilterCompare (false, true, DataFilterLabel.ItemTradeOutPrice, DataField.ItemTradePrice, DataField.ItemRegularPrice, DataField.ItemPriceGroup1, DataField.ItemPriceGroup2, DataField.ItemPriceGroup3, DataField.ItemPriceGroup4, DataField.ItemPriceGroup5, DataField.ItemPriceGroup6, DataField.ItemPriceGroup7, DataField.ItemPriceGroup8));
            AppendFilter (new FilterCompare (false, true, DataFilterLabel.ItemTradeInPrice, DataField.ItemPurchasePrice));
            AppendFilter (new FilterFind (false, true, DataFilterLabel.ItemBarcode, DataField.ItemBarcode1, DataField.ItemBarcode2, DataField.ItemBarcode3));
            AppendFilter (new FilterFind (false, true, DataFilterLabel.ItemCatalog, DataField.ItemCatalog1, DataField.ItemCatalog2, DataField.ItemCatalog3));
            AppendFilter (new FilterGroupFind (false, true, DataFilterLabel.ItemsGroupName, DataField.ItemsGroupName));
            AppendFilter (new FilterCompare (false, true, DataFilterLabel.ItemMinQuantity, DataField.ItemMinQuantity));
            AppendFilter (new FilterCompare (false, true, DataFilterLabel.ItemNomQuantity, DataField.ItemNomQuantity));
            AppendFilter (new FilterFind (false, true, DataFilterLabel.ItemDescription, DataField.ItemDescription));
            AppendOrder (new Order (
                DataField.ItemName,
                DataField.ItemCode));
        }

        #region Overrides of ReportQueryBase

        public override DataQueryResult ExecuteReport (DataQuery dataQuery)
        {
            return BusinessDomain.DataAccessProvider.ReportItems (dataQuery);
        }

        #endregion
    }
}
