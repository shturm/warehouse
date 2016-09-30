//
// DataProvider.UsersGroup.cs
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

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override T [] GetAllUsersGroups<T> ()
        {
            return ExecuteArray<T> (string.Format ("SELECT {0} FROM usersgroups ORDER BY Code",
                UsersGroupDefaultAliases ()));
        }

        public override T GetUsersGroupById<T> (long groupId)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM usersgroups
                WHERE usersgroups.ID = ABS(@groupID)", UsersGroupDefaultAliases ()),
                new DbParam ("groupID", groupId));
        }

        public override T GetUsersGroupByCode<T> (string groupCode)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM usersgroups
                WHERE usersgroups.Code = @groupCode", UsersGroupDefaultAliases ()),
                new DbParam ("groupCode", groupCode));
        }

        public override T GetUsersGroupByName<T> (string groupName)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM usersgroups
                WHERE usersgroups.Name = @groupName", UsersGroupDefaultAliases ()),
                new DbParam ("groupName", groupName));
        }

        #endregion

        #region Save / Delete

        public override void AddUpdateUsersGroup (object groupObject)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (groupObject);

            using (DbTransaction transaction = new DbTransaction (this)) {

                // Check if we already have that group
                long temp = ExecuteScalar<long> ("SELECT count(*) FROM usersgroups WHERE ID = @ID", helper.Parameters);

                // We are updating group
                if (temp == 1) {
                    temp = ExecuteScalar<long> ("SELECT count(*) FROM usersgroups WHERE ID = @ID AND Code = @Code",
                        helper.Parameters);

                    // We have changed the parent
                    if (temp != 1)
                        UsersGroupCalculateCode (helper);

                    temp = ExecuteNonQuery (string.Format ("UPDATE usersgroups {0} WHERE ID = @ID",
                        helper.GetSetStatement (DataField.UsersGroupsId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot update users group with ID={0}", helper.GetObjectValue (DataField.UsersGroupsId)));
                } // We are creating new group
                else if (temp == 0) {
                    UsersGroupCalculateCode (helper);

                    temp = ExecuteNonQuery (string.Format ("INSERT INTO usersgroups {0}",
                        helper.GetColumnsAndValuesStatement (DataField.UsersGroupsId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot add users group with name=\'{0}\'", helper.GetObjectValue (DataField.UsersGroupsName)));

                    long id = GetLastAutoId ();
                    helper.SetObjectValue (DataField.UsersGroupsId, id);
                } else
                    throw new Exception ("Too many entries with the same ID found in objects table.");

                transaction.Complete ();
            }
        }

        private void UsersGroupCalculateCode (Data.SqlHelper helper)
        {
            string groupCode = (string) helper.GetObjectValue (DataField.UsersGroupsCode);
            string parentCode = string.Empty;
            object lastCodeUsed;
            if (groupCode.Length > 3) {
                parentCode = groupCode.Substring (0, groupCode.Length - 3);
                lastCodeUsed = ExecuteScalar ("SELECT MAX(substr(Code, @CodeStart, 3)) FROM usersgroups WHERE substr(Code, 1, @ParentCodeLength) = @ParentCode AND (Code REGEXP '^[A-Z]+$')",
                    new DbParam ("CodeStart", groupCode.Length - 2),
                    new DbParam ("ParentCodeLength", groupCode.Length - 3),
                    new DbParam ("ParentCode", parentCode));
            } else {
                lastCodeUsed = ExecuteScalar ("SELECT MAX(substr(Code, @CodeStart, 3)) FROM usersgroups WHERE (Code REGEXP '^[A-Z]+$')",
                    new DbParam ("CodeStart", groupCode.Length - 2));
            }

            string nextCode = GetNextGroupCode (IsDBNull (lastCodeUsed) ? null : (string) lastCodeUsed);
            nextCode = parentCode + nextCode;
            helper.SetParameterValue (DataField.UsersGroupsCode, nextCode);
            helper.SetObjectValue (DataField.UsersGroupsCode, nextCode);
        }

        public override void DeleteUsersGroup (long groupId)
        {
            ExecuteNonQuery ("DELETE FROM usersgroups WHERE ID = @groupId", new DbParam ("groupId", groupId));
        }

        public override DeletePermission CanDeleteUsersGroup (long groupId)
        {
            if (groupId == -1 || groupId == 1)
                return DeletePermission.Reserved;

            if (groupId < -1)
                return DeletePermission.No;

            long ret = ExecuteScalar<long> ("SELECT count(*) FROM users WHERE GroupID = @groupId",
                new DbParam ("groupId", groupId));

            if (ret != 0)
                return DeletePermission.InUse;

            string code = ExecuteScalar<string> ("SELECT Code FROM usersgroups WHERE ID = @ID",
                new DbParam ("ID", groupId));

            if (string.IsNullOrEmpty (code))
                return DeletePermission.No;

            ret = ExecuteScalar<long> ("SELECT count(*) FROM usersgroups WHERE substr(usersgroups.Code, 1, @GCodeLength) = @GCode AND length(usersgroups.Code) > @GCodeLength",
                new DbParam ("GCodeLength", code.Length),
                new DbParam ("GCode", code));

            return 0 == ret ? DeletePermission.Yes : DeletePermission.InUse;
        }

        #endregion

        private string UsersGroupDefaultAliases ()
        {
            return GetAliasesString (DataField.UsersGroupsId,
                DataField.UsersGroupsCode,
                DataField.UsersGroupsName);
        }
    }
}
