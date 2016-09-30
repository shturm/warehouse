//
// DataProvider.AppLog.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
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
using System.Text;
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetLastApplicationLogEntries<T> (long? userId, int? maxEntries)
        {
            StringBuilder query = new StringBuilder (string.Format ("SELECT {0} FROM applicationlog", AppLogDefaultAliases ()));
            List<DbParam> pars = new List<DbParam> ();

            if (userId.HasValue) {
                query.AppendFormat (" WHERE applicationlog.UserID = @userId");
                pars.Add (new DbParam ("userId", userId.Value));
            }

            query.Append (" ORDER BY UserRealTime Desc");

            if (maxEntries.HasValue) {
                query.AppendFormat (" LIMIT @maxEntries");
                pars.Add (new DbParam ("maxEntries", maxEntries.Value));
            }

            return ExecuteLazyModel<T> (query.ToString (), pars.ToArray ());
        }

        #endregion

        #region Save / Delete

        public override void AddApplicationLogEntry (string message, long userId, DateTime timeStamp, string source)
        {
            ExecuteNonQuery ("INSERT INTO applicationlog (Message, UserID, UserRealTime, MessageSource) VALUES(@message, @userId, @timeStamp, @source)",
                new DbParam ("message", message),
                new DbParam ("userId", userId),
                new DbParam ("timeStamp", timeStamp),
                new DbParam ("source", source));
        }

        #endregion

        #region Reports

        public override DataQueryResult ReportAppLogEntries (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT applicationlog.ID, applicationlog.Message as {0}, users.Name as {1}, usersgroups.Name as {2}, 
                  applicationlog.UserRealTime as {3}, applicationlog.MessageSource as {4}
                FROM applicationlog LEFT JOIN users ON applicationlog.UserID = users.ID LEFT JOIN usersgroups ON users.GroupID = usersgroups.ID",
                fieldsTable.GetFieldAlias (DataField.AppLogMessage),
                fieldsTable.GetFieldAlias (DataField.UserName),
                fieldsTable.GetFieldAlias (DataField.UsersGroupsName),
                fieldsTable.GetFieldAlias (DataField.AppLogTimeStamp),
                fieldsTable.GetFieldAlias (DataField.AppLogMessageSource));

            querySet.SetSimpleId (DbTable.ApplicationLog, DataField.AppLogId, 0);

            return ExecuteDataQuery (querySet, query);
        }

        #endregion

        private string AppLogDefaultAliases ()
        {
            return GetAliasesString (DataField.AppLogId,
                DataField.AppLogMessage,
                DataField.AppLogUserId,
                DataField.AppLogTimeStamp,
                DataField.AppLogMessageSource);
        }
    }
}
