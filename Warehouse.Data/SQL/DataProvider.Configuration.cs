//
// DataProvider.Configuration.cs
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

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override T GetConfigurationByKey<T> (string keyName, long? userId)
        {
            string query = string.Format ("SELECT {0} FROM configuration WHERE `Key` = @keyName{1}",
                ConfigurationDefaultAliases (),
                userId.HasValue ? " AND UserID = @userId" : string.Empty);

            List<DbParam> pars = new List<DbParam> { new DbParam ("keyName", keyName) };
            if (userId.HasValue)
                pars.Add (new DbParam ("userId", userId.Value));

            return ExecuteObject<T> (query, pars.ToArray ());
        }

        public override string GetConfigurationByKey (string keyName, long? userId)
        {
            string query = string.Format ("SELECT Value FROM configuration WHERE `Key` = @keyName{0}",
                userId.HasValue ? " AND UserID = @userId" : string.Empty);

            List<DbParam> pars = new List<DbParam> { new DbParam ("keyName", keyName) };
            if (userId.HasValue)
                pars.Add (new DbParam ("userId", userId.Value));

            return ExecuteScalar<string> (query, pars.ToArray ());
        }

        public override void SetConfiguration (string key, long? userId, string value)
        {
            string query = string.Format ("SELECT ID FROM configuration WHERE `Key` = @keyName{0}",
                userId.HasValue ? " AND UserID = @userId" : string.Empty);

            List<DbParam> pars = new List<DbParam> { new DbParam ("keyName", key) };
            if (userId.HasValue)
                pars.Add (new DbParam ("userId", userId.Value));

            int? configId = ExecuteScalar<int?> (query, pars.ToArray ());
            if (configId != null)
                ExecuteNonQuery (@"UPDATE configuration SET `Value` = @value WHERE ID = @id",
                    new DbParam ("value", value),
                    new DbParam ("id", configId.Value));
            else
                ExecuteNonQuery (@"INSERT INTO configuration (`Key`, `Value`, `UserID`) VALUES (@key, @value, @userId)",
                    new DbParam ("key", key),
                    new DbParam ("value", value),
                    new DbParam ("userId", userId ?? -1));
        }

        #endregion

        #region Save / Delete

        public override void AddUpdateConfiguration (object configurationObject)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (configurationObject);

            using (DbTransaction transaction = new DbTransaction (this)) {
                // Check if we already have that item
                long temp = ExecuteScalar<long> ("SELECT count(*) FROM configuration WHERE ID = @ID", helper.Parameters);

                // We are updating location
                if (temp == 1) {
                    temp = ExecuteNonQuery (string.Format ("UPDATE configuration {0} WHERE ID = @ID",
                        helper.GetSetStatement (DataField.ConfigEntryId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot update configuration with ID={0}", helper.GetObjectValue (DataField.ConfigEntryId)));
                } // We are creating new location
                else if (temp == 0) {
                    temp = ExecuteNonQuery (string.Format ("INSERT INTO configuration {0}",
                        helper.GetColumnsAndValuesStatement (DataField.ConfigEntryId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot add configuration with key=\'{0}\'", helper.GetObjectValue (DataField.ConfigEntryKey)));

                    temp = GetLastAutoId ();
                    helper.SetObjectValue (DataField.ConfigEntryId, temp);
                } else
                    throw new Exception ("Too many entries with the same ID found in configuration table.");

                transaction.Complete ();
            }
        }

        public override void DeleteConfiguration (string key, long? userId)
        {
            List<DbParam> pars = new List<DbParam> ();
            pars.Add (new DbParam ("key", key));
            if (userId.HasValue)
                pars.Add (new DbParam ("userId", userId.Value));

            ExecuteNonQuery (string.Format ("DELETE FROM configuration WHERE `Key` = @key{0}",
                userId.HasValue ? " AND UserID = @userId" : string.Empty), pars.ToArray ());
        }

        #endregion

        private string ConfigurationDefaultAliases ()
        {
            return GetAliasesString (DataField.ConfigEntryId,
                DataField.ConfigEntryKey,
                DataField.ConfigEntryValue,
                DataField.ConfigEntryUserId);
        }
    }
}
