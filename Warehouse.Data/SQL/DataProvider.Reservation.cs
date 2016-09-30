//
// DataProvider.Reservation.cs
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

        public override LazyListModel<T> GetReservations<T> (long? locationId, long? customerId, long? userId, DateTime? @from, DateTime? to)
        {
            string dateTime = fieldsTable.GetFieldName (DataField.OperationDateTime);
            string query = string.Format (@"
                SELECT {0}, objects.Name as {1}, objects.Name2 as {2}, partners.Company as {3}, partners.Company2 as {4},
                  users1.Name as {5}, users1.Name2 as {6}, users2.Name as {7}, users2.Name2 as {8},
                  {15} as {9}
                FROM ((((operations INNER JOIN partners ON operations.PartnerID = partners.ID)
                  INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.OperType = @operType{10}{11}{12}{13}{14}
                GROUP BY operations.Acct,operations.ObjectID,operations.PartnerID,operations.OperatorID",
                OperationDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2),
                fieldsTable.GetFieldAlias (DataField.OperationDateTime),
                locationId.HasValue ? " AND operations.ObjectID = @locationId" : string.Empty,
                customerId.HasValue ? " AND operations.PartnerID = @customerId" : string.Empty,
                userId.HasValue ? " AND operations.OperatorID = @userId" : string.Empty,
                from.HasValue ? string.Format (" AND {0} >= @from", dateTime) : string.Empty,
                to.HasValue ? string.Format (" AND {0} <= @to", dateTime) : string.Empty,
                dateTime);

            List<DbParam> pars = new List<DbParam> { new DbParam ("operType", (int) OperationType.Reservation) };
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

            return ExecuteLazyModel<T> (false, query, pars.ToArray ());
        }

        #endregion

        #region Save

        public override void AddUpdateReservation (object resObject, object resObjectDetail)
        {
            AddUpdateDetail (resObject, resObjectDetail);
        }

        #endregion

        #region Reports

        public override DataQueryResult ReportReservations (DataQuery querySet)
        {
            return ExecuteDataQuery (querySet, string.Format (@"
                SELECT objects.Name as {0}, IFNULL(objects.Name2, objects.Name) as {1},
                  partners.Company as {2}, IFNULL(partners.Company2, partners.Company) as {3},
                  users1.Name as {4}, IFNULL(users1.Name2, users1.Name) as {5},
                  users2.Name as {6}, IFNULL(users2.Name2, users2.Name) as {7},
                  {8} as {9}
                FROM ((((operations INNER JOIN partners ON operations.PartnerID = partners.ID)
                  INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.OperType = @operType
                GROUP BY operations.Acct,operations.ObjectID,operations.PartnerID,operations.OperatorID,objects.Name,objects.Name2,partners.Company,partners.Company2
                ORDER BY {8}",
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2),
                fieldsTable.GetFieldName (DataField.OperationDateTime),
                fieldsTable.GetFieldAlias (DataField.OperationDateTime)),
                new DbParam ("operType", (int) OperationType.Reservation));
        }

        #endregion
    }
}
