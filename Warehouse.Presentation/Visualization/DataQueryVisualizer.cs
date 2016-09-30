//
// DataQueryVisualizer.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   06/21/2009
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
using System.Data;
using System.Linq;
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Presentation.Visualization
{
    [TypeExtensionPoint ("/Warehouse/Presentation/Visualization")]
    public abstract class DataQueryVisualizer : EventBox
    {
        public const int MINIMAL_INTERVAL_FOR_LOADING_ANIMATION = 1000;

        private static readonly DbField [] PuchasePriceFields = new DbField [] { DataField.StorePrice, DataField.OperationDetailPriceIn, DataField.OperationDetailVatIn, DataField.OperationDetailSumVatIn, DataField.OperationDetailSumIn, DataField.OperationDetailTotalIn, DataField.PurchaseSum, DataField.ItemPurchasePrice, DataField.ItemTradeInVAT, DataField.ItemTradeInSum };
        private static readonly DbField [] AvailabilityFields = new DbField [] { DataField.StoreAvailableQuantity, DataField.StoreQtty };

        public event EventHandler<SortChangedEventArgs> SortChanged;

        protected void OnSortChanged (SortChangedEventArgs e)
        {
            EventHandler<SortChangedEventArgs> handler = SortChanged;
            if (handler != null)
                handler (this, e);
        }

        public abstract LazyTableModel Model { get; }
        public abstract VisualizerSettingsBase CurrentSettings { get; }
        public abstract bool TotalsShown { get; }
        public abstract bool SupportsSumming { get; }
        public abstract bool SupportsPrinting { get; }
        public abstract bool SupportsExporting { get; }
        public abstract event EventHandler Initialized;
        public abstract void Initialize (DataQueryResult query, VisualizerSettingsCollection settings, bool preserveSort = true);
        public abstract void Refresh ();
        public abstract Report GetPrintData (string title);
        public abstract DataExchangeSet GetExportData (string title);
        public abstract void ShowTotals ();
        public abstract void HideTotals ();

        public static bool CheckColumnVisible (DataQueryResult queryResult, int i)
        {
            ColumnInfo columnInfo = queryResult.Columns [i];
            if (columnInfo.IsHidden)
                return false;

            User loggedUser = BusinessDomain.LoggedUser;
            if (!loggedUser.IsSaved)
                return false;

            DbField field = columnInfo.Field;
            return (!loggedUser.HideItemsPurchasePrice || !PuchasePriceFields.Contains (field)) &&
                (!loggedUser.HideItemsAvailability || !AvailabilityFields.Contains (field));
        }
    }
}
