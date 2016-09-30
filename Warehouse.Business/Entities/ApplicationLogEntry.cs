//
// ApplicationLogEntry.cs
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

using System;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Entities
{
    public class ApplicationLogEntry
    {
        #region Private fields

        private long id;
        private string message;
        private int userId;
        private DateTime timeStamp;
        private string messageSource;

        #endregion

        #region Public properties

        [DbColumn (DataField.AppLogId)]
        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        [DbColumn (DataField.AppLogMessage, 255)]
        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        [DbColumn (DataField.AppLogUserId)]
        public int UserId
        {
            get { return userId; }
            set { userId = value; }
        }

        [DbColumn (DataField.AppLogTimeStamp)]
        public DateTime TimeStamp
        {
            get { return timeStamp; }
            set { timeStamp = value; }
        }

        [DbColumn (DataField.AppLogMessageSource, 50)]
        public string MessageSource
        {
            get { return messageSource; }
            set { messageSource = value; }
        }

        #endregion

        public static void AddNew (string message)
        {
            AddNew (message, BusinessDomain.LoggedUser.Id, BusinessDomain.Now);
        }

        public static void AddNew (string message, long userId)
        {
            AddNew (message, userId, BusinessDomain.Now);
        }

        public static void AddNew (string message, long userId, DateTime timeStamp)
        {
            BusinessDomain.DataAccessProvider.AddApplicationLogEntry (message, userId, timeStamp, DataHelper.ProductFullName);
        }

        public static LazyListModel<ApplicationLogEntry> GetLast (long? userId, int? maxEntries)
        {
            return BusinessDomain.DataAccessProvider.GetLastApplicationLogEntries<ApplicationLogEntry> (userId, maxEntries);
        }
    }
}
