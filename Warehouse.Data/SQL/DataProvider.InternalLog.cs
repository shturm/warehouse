//
// DataProvider.InternalLog.cs
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

using System.Collections.Generic;
using System.Text;
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetAllInternalLogEntries<T> (string search, int? maxEntries)
        {
            string query = string.Format (@"SELECT {0}
                FROM internallog
                WHERE Message LIKE {1}
                ORDER BY ID Asc
                {2}",
                InternalLogDefaultAliases (),
                GetConcatStatement ("'%'", "@search", "'%'"),
                maxEntries != null ? "LIMIT @maxEntries" : string.Empty);

            return ExecuteLazyModel<T> (query, new DbParam ("search", search), new DbParam ("maxEntries", maxEntries ?? -1));
        }

        #endregion

        #region Save / Delete

        public override void AddInternalLogEntry (string message)
        {
            ExecuteNonQuery ("INSERT INTO internallog (Message) VALUES(@message)", new DbParam ("message", message));
        }

        public override void DeleteInternalLogEntries (params long [] id)
        {
            List<DbParam> ids = new List<DbParam> ();
            StringBuilder query = new StringBuilder ();
            for (int i = 0; i < id.Length; i++) {
                DbParam par = new DbParam ("id" + i, id [i]);
                ids.Add (par);

                if (query.Length > 0)
                    query.Append (",");
                query.Append (fieldsTable.GetParameterName (par.ParameterName));
            }

            ExecuteNonQuery (string.Format ("DELETE FROM internallog WHERE ID IN ({0})", query), ids.ToArray ());
        }

        #endregion

        private string InternalLogDefaultAliases ()
        {
            return GetAliasesString (DataField.InternalLogId,
                DataField.InternalLogMessage);
        }
    }
}
