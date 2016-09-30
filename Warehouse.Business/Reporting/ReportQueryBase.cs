//
// ReportQueryBase.cs
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

using System;
using System.Collections.Generic;
using Warehouse.Business.Operations;
using Warehouse.Data;

namespace Warehouse.Business.Reporting
{
    public abstract class ReportQueryBase
    {
        private readonly List<FilterBase> filters = new List<FilterBase> ();
        private Order order;

        public string Name { get; set; }

        public virtual string HelpFile { get { return null; } }

        public virtual bool AllFieldsFilterable { get { return false; } }

        public virtual string ReportType
        {
            get { return GetType ().FullName; }
        }

        public virtual string ReportTypeName
        {
            get
            {
                string name = GetType ().Name;
                name = name.Replace ("ReportQuery", "Report");

                return name.CamelSpace ();
            }
        }

        public List<FilterBase> Filters
        {
            get { return filters; }
        }

        public Order Order
        {
            get { return order; }
            protected set { order = value; }
        }

        protected FilterBase AppendFilter (FilterBase filter)
        {
            filters.Add (filter);
            return filter;
        }

        protected void AppendFiltersForLots ()
        {
            if (!BusinessDomain.AppConfiguration.ItemsManagementUseLots)
                return;

            AppendFilter (new FilterFind (false, false, DataFilterLabel.OperationDetailLot, DataField.OperationDetailLot));
            AppendFilter (new FilterFind (false, false, DataFilterLabel.LotSerialNumber, DataField.LotSerialNumber));
            AppendFilter (new FilterDateRange (false, false, DataFilterLabel.LotProductionDate, DataField.LotProductionDate));
            AppendFilter (new FilterDateRange (false, false, DataFilterLabel.LotExpirationDate, DataField.LotExpirationDate));
            AppendFilter (new FilterFind (false, false, DataFilterLabel.LotLocation, DataField.LotLocation));
        }

        protected void AppendFiltersForLotsStore ()
        {
            if (!BusinessDomain.AppConfiguration.ItemsManagementUseLots)
                return;

            AppendFilter (new FilterFind (false, false, DataFilterLabel.StoreLot, DataField.StoreLot));
            AppendFilter (new FilterFind (false, false, DataFilterLabel.LotSerialNumber, DataField.LotSerialNumber));
            AppendFilter (new FilterDateRange (false, false, DataFilterLabel.LotProductionDate, DataField.LotProductionDate));
            AppendFilter (new FilterDateRange (false, false, DataFilterLabel.LotExpirationDate, DataField.LotExpirationDate));
            AppendFilter (new FilterFind (false, false, DataFilterLabel.LotLocation, DataField.LotLocation));
        }

        protected void AppendFilterForPayableOperations (bool enabled)
        {
            AppendFilter (new FilterChoose (false, enabled, Operation.GetAllWithPaymentFilters (), DataFilterLabel.OperationType, DataField.PaymentOperationType));
        }

        protected void AppendOrder (Order rOrder)
        {
            if (order != null)
                throw new Exception ("Only one order element can exist in the report query.");

            order = rOrder;
        }

        private DataQuery GetDataQuery (bool saveSettings)
        {
            DataQuery dataQuery = ReportProvider.CreateDataQuery ();

            foreach (FilterBase filter in filters)
                dataQuery.Filters.Add (filter.GetDataFilter ());

            if (order != null) {
                dataQuery.OrderBy = order.Selection;
                dataQuery.OrderDirection = order.Direction;
            }

            return PrepareDataQuery (dataQuery, saveSettings);
        }

        public DataQuery PrepareDataQuery (DataQuery dataQuery, bool saveSettings)
        {
            if (saveSettings)
                BusinessDomain.ReportQueryStates [ReportType] = dataQuery.Clone ();

            if (BusinessDomain.HideVATColumns) {
                dataQuery.Filters.Add (new DataFilter (DataField.OperationVatSum) { ShowColumns = false });
                dataQuery.Filters.Add (new DataFilter (DataField.OperationDetailVat) { ShowColumns = false });
                dataQuery.Filters.Add (new DataFilter (DataField.OperationDetailVatIn) { ShowColumns = false });
                dataQuery.Filters.Add (new DataFilter (DataField.OperationDetailVatOut) { ShowColumns = false });
                dataQuery.Filters.Add (new DataFilter (DataField.OperationDetailSumVat) { ShowColumns = false });
                dataQuery.Filters.Add (new DataFilter (DataField.OperationDetailSumVatIn) { ShowColumns = false });
                dataQuery.Filters.Add (new DataFilter (DataField.OperationDetailSumVatOut) { ShowColumns = false });
                dataQuery.Filters.Add (new DataFilter (DataField.ItemPurchasePrice) { ShowColumns = false });
                dataQuery.Filters.Add (new DataFilter (DataField.ItemPurchasePrice) { ShowColumns = false });
                dataQuery.Filters.Add (new DataFilter (DataField.ItemPurchasePrice) { ShowColumns = false });
                dataQuery.Filters.Add (new DataFilter (DataField.ItemPurchasePrice) { ShowColumns = false });
            }

            return dataQuery;
        }

        /// <summary>
        /// For use only when no filters are attached from the UI
        /// </summary>
        public void SetDataQuery ()
        {
            DataQuery querySet;
            if (!BusinessDomain.ReportQueryStates.TryGetValue (ReportType, out querySet))
                return;

            SetDataQuery (querySet);
        }

        /// <summary>
        /// For use only when no filters are attached from the UI
        /// </summary>
        /// <param name="querySet"></param>
        public void SetDataQuery (DataQuery querySet)
        {
            if (filters.Count == querySet.Filters.Count)
                for (int i = 0; i < filters.Count; i++)
                    filters [i].SetDataFilter (querySet.Filters [i]);

            if (order != null)
                order.LoadOrder (order.Choices, querySet.OrderBy, querySet.OrderDirection);
        }

        public abstract DataQueryResult ExecuteReport (DataQuery dataQuery);

        /// <summary>
        /// For use only when no filters are attached from the UI
        /// </summary>
        /// <param name="saveSettings"></param>
        /// <returns></returns>
        public DataQueryResult ExecuteReport (bool saveSettings)
        {
            return ExecuteReport (GetDataQuery (saveSettings));
        }
    }
}
