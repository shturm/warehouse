//
// DataProvider.RestaurantOrder.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   10.13.2007
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
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetRestaurantOrders<T> (long? locationId, long? customerId, long? userId, DateTime? @from, DateTime? to)
        {
            string dateTime = fieldsTable.GetFieldFullName (DataField.OperationTimeStamp);
            string query = string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, partners.Company as {3}, partners.Company2 as {4}, users1.Name as {5}, users1.Name2 as {6}, users2.Name as {7}, users2.Name2 as {8}
                FROM ((((operations INNER JOIN partners ON operations.PartnerID = partners.ID) INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.Acct <= 0 AND operations.OperType = {9}{10}{11}{12}{13}{14}
                GROUP BY operations.ObjectID,operations.PartnerID,operations.OperatorID",
                OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2),
                (int) OperationType.RestaurantOrder,
                locationId.HasValue ? " AND operations.ObjectID = @locationId" : string.Empty,
                customerId.HasValue ? " AND operations.PartnerID = @customerId" : string.Empty,
                userId.HasValue ? " AND operations.OperatorID = @userId" : string.Empty,
                from.HasValue ? string.Format (" AND {0} >= @from", dateTime) : string.Empty,
                to.HasValue ? string.Format (" AND {0} <= @to", dateTime) : string.Empty);

            List<DbParam> pars = new List<DbParam> ();
            if (locationId.HasValue)
                pars.Add (new DbParam ("locationId", locationId.Value));
            if (customerId.HasValue)
                pars.Add (new DbParam ("customerId", customerId.Value));
            if (userId.HasValue)
                pars.Add (new DbParam ("userId", userId.Value));
            if (from.HasValue)
                pars.Add (new DbParam ("from", from.Value));
            if (to.HasValue)
                pars.Add (new DbParam ("to", to.Value));

            return ExecuteLazyModel<T> (query, pars.ToArray ());
        }

        public override LazyListModel<T> GetRestaurantOrderDetails<T> (long? operationId, long locationId, long? customerId, long? userId)
        {
            string query = string.Format (@"
                SELECT {0}, goods.GroupID as {1}, goods.Measure1 as {2}, goods.Name as {3}, goods.Name2 as {4}, PriceOut * Qtty as {5}
                FROM operations INNER JOIN goods ON operations.GoodID = goods.ID
                WHERE operations.OperType = {6} AND operations.ObjectID = @locationId{7}{8}{9}",
                OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.ItemGroupId),
                fieldsTable.GetFieldAlias (DataField.ItemMeasUnit),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.ItemName2),
                fieldsTable.GetFieldAlias (DataField.OperationDetailTotal),
                (int) OperationType.RestaurantOrder,
                operationId.HasValue ? " AND operations.Acct = @operationId" : " AND operations.Acct = 0",
                customerId.HasValue ? " AND operations.PartnerID = @customerId" : string.Empty,
                userId.HasValue ? " AND operations.OperatorID = @userId" : string.Empty);

            List<DbParam> pars = new List<DbParam> { new DbParam ("locationId", locationId) };
            if (operationId.HasValue)
                pars.Add (new DbParam ("operationId", operationId.Value));
            if (customerId.HasValue)
                pars.Add (new DbParam ("customerId", customerId.Value));
            if (userId.HasValue)
                pars.Add (new DbParam ("userId", userId.Value));

            return ExecuteLazyModel<T> (query, pars.ToArray ());
        }

        #endregion

        #region Save

        public override void AddUpdateRestaurantOrder (object restOrderObject, object [] restOrderDetailObjects)
        {
            foreach (object detail in restOrderDetailObjects)
                AddUpdateDetail (restOrderObject, detail);
        }

        #endregion

        #region Reports

        public override DataQueryResult ReportOrders (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT objects.Name as {0}, IFNULL(objects.Name2, objects.Name) as {1},
                  partners.Company as {2}, IFNULL(partners.Company2, partners.Company) as {3},
                  users1.Name as {4}, IFNULL(users1.Name2, users1.Name) as {5},
                  users2.Name as {6}, IFNULL(users2.Name2, users2.Name) as {7},
                  SUM({9}) as {8}
                FROM ((((operations INNER JOIN partners ON operations.PartnerID = partners.ID) INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.OperType = 33
                GROUP BY operations.ObjectID,operations.PartnerID,operations.OperatorID,objects.Name, objects.Name2, partners.Company, partners.Company2",
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2),
                fieldsTable.GetFieldAlias (DataField.OperationTotal),
                GetOperationDetailTotalOut (querySet));

            return ExecuteDataQuery (querySet, query);
        }

        #endregion
    }
}
