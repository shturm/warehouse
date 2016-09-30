//
// DataProvider.User.cs
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
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetAllUsers<T> ()
        {
            return ExecuteLazyModel<T> (string.Format ("SELECT {0} FROM users WHERE Deleted <> -1", UserDefaultAliases ()));
        }

        public override LazyListModel<T> GetAllUsers<T> (int maxUserLevel, long currentUserId)
        {
            // Allow owners to edit other owners too
            if (maxUserLevel == 3)
                maxUserLevel = 4;

            return ExecuteLazyModel<T> (string.Format (@"
                SELECT {0} FROM users 
                WHERE (UserLevel < @maxUserLevel OR ID = @currentUserId) AND Deleted <> -1",
                UserDefaultAliases ()),
                new DbParam ("maxUserLevel", maxUserLevel),
                new DbParam ("currentUserId", currentUserId));
        }

        public override LazyListModel<T> GetAllUsers<T> (long? groupId)
        {
            SqlHelper helper = GetSqlHelper ();
            string query;

            if (groupId.HasValue) {
                if (groupId == int.MinValue) {
                    query = string.Format (@"
                        SELECT {0}
                        FROM users
                        WHERE Deleted = -1", UserDefaultAliases ());
                } else {
                    helper.AddParameters (new DbParam ("groupId", groupId.Value));

                    query = string.Format (@"
                        SELECT {0}
                        FROM users
                        WHERE ABS(GroupID) = @groupId AND Deleted <> -1", UserDefaultAliases ());
                }
            } else {
                query = string.Format (@"
                    SELECT {0}
                    FROM users
                    WHERE Deleted <> -1",
                    UserDefaultAliases ());
            }

            return ExecuteLazyModel<T> (query, helper.Parameters);
        }

        public override T GetUserById<T> (long userId)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM users WHERE ID = @userId", UserDefaultAliases ()),
                new DbParam ("userId", userId));
        }

        public override T GetUserByName<T> (string userName)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM users WHERE Name = @userName AND Deleted <> -1", UserDefaultAliases ()),
                new DbParam ("userName", userName));
        }

        public override T GetUserByCode<T> (string userCode)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM users WHERE Code = @userCode AND Deleted <> -1", UserDefaultAliases ()),
                new DbParam ("userCode", userCode));
        }

        public override T GetUserByCard<T> (string userCardNo)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM users WHERE CardNumber = @userCardNo AND Deleted <> -1", UserDefaultAliases ()),
                new DbParam ("userCardNo", userCardNo));
        }

        #endregion

        #region Save / Delete

        public override void AddUpdateUser (object userObject)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (userObject);

            using (DbTransaction transaction = new DbTransaction (this)) {

                // Check if we already have that user
                long temp = ExecuteScalar<long> ("SELECT count(*) FROM users WHERE ID = @ID", helper.Parameters);

                // We are updating user information
                if (temp == 1) {
                    temp = ExecuteNonQuery (string.Format ("UPDATE users {0} WHERE ID = @ID",
                        helper.GetSetStatement (DataField.UserId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot update user with id=\'{0}\'", helper.GetObjectValue (DataField.UserId)));
                } // We are creating new user information
                else if (temp == 0) {
                    temp = ExecuteNonQuery (string.Format ("INSERT INTO users {0}",
                        helper.GetColumnsAndValuesStatement (DataField.UserId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot add user with name=\'{0}\'", helper.GetObjectValue (DataField.UserName)));

                    temp = GetLastAutoId ();
                    helper.SetObjectValue (DataField.UserId, temp);
                } else
                    throw new Exception ("Too many entries with the same ID found in users table.");

                transaction.Complete ();
            }
        }

        public override void DeleteUser (long userId)
        {
            ExecuteNonQuery ("DELETE FROM users WHERE ID = @userId",
                new DbParam ("userId", userId));
        }

        public override bool CheckUserOwnerExists ()
        {
            return ExecuteScalar (@"
                SELECT 1 FROM users WHERE EXISTS (
                    SELECT * FROM users
                    WHERE UserLevel = 3 AND ID <> 1)") != null;
        }

        public override DeletePermission CanDeleteUser (long userId)
        {
            if (userId == 1)
                return DeletePermission.Reserved;

            DbParam par = new DbParam ("userId", userId);

            long ret = ExecuteScalar<long> ("SELECT count(*) FROM operations WHERE UserID = @userId", par);
            if (0 != ret)
                return DeletePermission.InUse;

            ret = ExecuteScalar<long> ("SELECT count(*) FROM operations WHERE OperatorID = @userId", par);
            return 0 == ret ? DeletePermission.Yes : DeletePermission.InUse;
        }

        #endregion

        #region Reports

        public override DataQueryResult ReportUsers (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT users.ID, users.Code as {0}, users.Name as {1}, users.UserLevel as {2}, usersgroups.Name
                FROM users LEFT JOIN usersgroups ON ABS(users.GroupID) = usersgroups.ID",
                fieldsTable.GetFieldAlias (DataField.UserCode),
                fieldsTable.GetFieldAlias (DataField.UserName),
                fieldsTable.GetFieldAlias (DataField.UserLevel));

            querySet.SetSimpleId (DbTable.Users, DataField.UserId, 0);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportOperationsByUsers (DataQuery querySet)
        {
            string query = string.Format (@"
                {0}
                FROM (users INNER JOIN operations ON ABS(users.ID) = operations.UserID) 
                  INNER JOIN partners ON operations.PartnerID = partners.ID
                GROUP BY operations.Acct, operations.OperType",
                GetSelect (new [] { DataField.UserName, DataField.OperationType, DataField.OperationNumber, 
                    DataField.OperationTimeStamp, DataField.PartnerName, DataField.OperationTotal }));

            querySet.SetSimpleId (DbTable.Operations, DataField.OperationNumber, 0);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportReturnsByUser (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT objects.Name as {0}, IFNULL(objects.Name2, objects.Name) as {1},
                  partners.Company as {2}, IFNULL(partners.Company2, partners.Company) as {3},
                  users1.Name as {4}, IFNULL(users1.Name2, users1.Name) as {5},
                  users2.Name as {6}, IFNULL(users2.Name2, users2.Name) as {7},
                  SUM({11}) as {8},
                  operations.Acct as {9}, operations.UserRealTime as {10}
                FROM ((((operations INNER JOIN partners ON operations.PartnerID = partners.ID) INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.OperType = {12} AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY operations.Acct, objects.Name, objects.Name2, partners.Company, partners.Company2",
                fieldsTable.GetFieldAlias (DataField.OperationLocation),
                fieldsTable.GetFieldAlias (DataField.OperationLocation2),
                fieldsTable.GetFieldAlias (DataField.OperationPartner),
                fieldsTable.GetFieldAlias (DataField.OperationPartner2),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2),
                fieldsTable.GetFieldAlias (DataField.OperationTotal),
                fieldsTable.GetFieldAlias (DataField.OperationNumber),
                fieldsTable.GetFieldAlias (DataField.OperationTimeStamp),
                GetOperationDetailTotalOut (querySet),
                (int) OperationType.Return);

            querySet.SetComplexId (DbTable.Operations, DataField.OperationNumber, DataField.OperationType, OperationType.Sale);

            return ExecuteDataQuery (querySet, query);
        }

        #endregion

        private string UserDefaultAliases ()
        {
            return GetAliasesString (DataField.UserId,
                DataField.UserCode,
                DataField.UserName,
                DataField.UserName2,
                DataField.UserOrder,
                DataField.UserDeleted,
                DataField.UserGroupId,
                DataField.UserPassword,
                DataField.UserLevel,
                DataField.UserCardNo);
        }
    }
}
