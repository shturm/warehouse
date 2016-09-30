//
// DataProvider.UsersSecurity.cs
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
using System.Data;
using System.Globalization;
using System.Text;
using System.Linq;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override T [] GetAllRestrictions<T> ()
        {
            string query = string.Format (@"
                SELECT {0}
                FROM userssecurity",
                RestrictionsDefaultAliases ());

            return ExecuteArray<T> (query);
        }

        public override T [] GetRestrictionsByUser<T> (long userId)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddParameters (new DbParam ("userId", userId));

            string query = string.Format (@"
                SELECT {0}
                FROM userssecurity WHERE UserID = @userId",
                RestrictionsDefaultAliases ());

            return ExecuteArray<T> (query, helper.Parameters);
        }

        public override T [] GetRestrictionsByName<T> (string name)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddParameters (new DbParam ("name", name));

            string query = string.Format (@"
                SELECT {0}
                FROM userssecurity WHERE ControlName = @name",
                RestrictionsDefaultAliases ());

            return ExecuteArray<T> (query, helper.Parameters);
        }

        #endregion

        #region Save / Delete

        public override void AddUpdateUserRestriction<T> (IEnumerable<T> restrictionObjects)
        {
            using (DbTransaction transaction = new DbTransaction (this)) {

                var lookup = ExecuteList<ObjectsContainer<string, long, int>> (
                    "SELECT ControlName as Value1, UserID as Value2, State as Value3 FROM userssecurity")
                    .ToLookup (v => v.Value1 + "+" + v.Value2.ToString (CultureInfo.InvariantCulture));

                List<List<DbParam>> insertParams = new List<List<DbParam>> ();
                List<DbParam> deleteParams = new List<DbParam> ();
                string insertColumns = null;
                SqlHelper helper = GetSqlHelper ();

                foreach (object restrictionObject in restrictionObjects) {
                    helper.ChangeObject (restrictionObject);
                    string key = string.Format ("{0}+{1}",
                        helper.GetObjectValue (DataField.UsersSecurityControlName),
                        ((long) helper.GetObjectValue (DataField.UsersSecurityUserId)).ToString (CultureInfo.InvariantCulture));
                    int state = (int) helper.GetObjectValue (DataField.UsersSecurityState);

                    if (lookup.Contains (key)) {
                        bool entryFound = false;
                        foreach (var entry in lookup [key]) {
                            if (state != entry.Value3 && state == 2 && !entryFound) {
                                int temp = ExecuteNonQuery (string.Format ("UPDATE userssecurity {0} WHERE ID = @ID",
                                    helper.GetSetStatement (DataField.UsersSecurityId)), helper.Parameters);
                                if (temp != 1)
                                    throw new Exception (string.Format ("Cannot update user restriction with id=\'{0}\'",
                                        helper.GetObjectValue (DataField.UsersSecurityId)));
                            } else if (state != 2 || entryFound)
                                deleteParams.Add (helper.Parameters.First (p => p.ParameterName == "@ID"));

                            entryFound = true;
                        }
                    } else if (state == 2) {
                        helper.ChangeObject (restrictionObject, DataField.UsersSecurityId);
                        insertParams.Add (new List<DbParam> (helper.Parameters));
                        if (insertColumns == null)
                            insertColumns = helper.GetColumns (DataField.UsersSecurityId);
                    }
                }

                if (deleteParams.Count > 0)
                    DeleteRestrictions (deleteParams);

                if (insertColumns != null)
                    BulkInsert ("userssecurity", insertColumns, insertParams, "Cannot insert user restrictions");

                transaction.Complete ();
            }
        }

        private void DeleteRestrictions (IList<DbParam> toDelete)
        {
            StringBuilder deleteBuilder = new StringBuilder ();
            List<DbParam> finalParameters = new List<DbParam> ();
            for (int i = 0; i < toDelete.Count; i++) {
                DbParam idParam = toDelete [i];
                idParam.ParameterName += "x" + i.ToString (CultureInfo.InvariantCulture);
                deleteBuilder.AppendFormat ("{0}, ", fieldsTable.GetParameterName (idParam.ParameterName));
                finalParameters.Add (idParam);
            }
            deleteBuilder.Remove (deleteBuilder.Length - 2, 2);

            int result = ExecuteNonQuery (string.Format ("DELETE FROM userssecurity WHERE ID IN ({0})", deleteBuilder),
                finalParameters.ToArray ());

            if (result != toDelete.Count)
                throw new Exception (string.Format ("Cannot update user restrictions"));
        }

        #endregion

        private string RestrictionsDefaultAliases ()
        {
            return GetAliasesString (DataField.UsersSecurityId,
                DataField.UsersSecurityUserId,
                DataField.UsersSecurityControlName,
                DataField.UsersSecurityState);
        }
    }
}
