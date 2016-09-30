//
// InternalLogEntry.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   10/30/2008
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
    public class InternalLogEntry
    {
        #region Public properties

        [DbColumn (DataField.InternalLogId)]
        public long Id { get; set; }

        [DbColumn (DataField.InternalLogMessage, 3000)]
        public string Message { get; set; }

        #endregion

        public static void AddNew (string message)
        {
            BusinessDomain.DataAccessProvider.AddInternalLogEntry (message);
        }

        public static LazyListModel<InternalLogEntry> GetAll (string search, int? maxEntries)
        {
            return BusinessDomain.DataAccessProvider.GetAllInternalLogEntries<InternalLogEntry> (search, maxEntries);
        }

        public static void Delete (params long [] id)
        {
            BusinessDomain.DataAccessProvider.DeleteInternalLogEntries (id);
        }
    }
}
