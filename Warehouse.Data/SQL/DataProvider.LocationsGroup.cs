//
// DataProvider.LocationsGroup.cs
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

        public override T [] GetAllLocationsGroups<T> ()
        {
            return ExecuteArray<T> (string.Format ("SELECT {0} FROM objectsgroups ORDER BY Code",
                LocationsGroupDefaultAliases ()));
        }

        public override T GetLocationsGroupById<T> (long groupId)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM objectsgroups
                WHERE objectsgroups.ID = ABS(@groupID)", LocationsGroupDefaultAliases ()),
                new DbParam ("groupID", groupId));
        }

        public override T GetLocationsGroupByCode<T> (string groupCode)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM objectsgroups
                WHERE objectsgroups.Code = @groupCode", LocationsGroupDefaultAliases ()),
                new DbParam ("groupCode", groupCode));
        }

        public override T GetLocationsGroupByName<T> (string groupName)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM objectsgroups
                WHERE objectsgroups.Name = @groupName", LocationsGroupDefaultAliases ()),
                new DbParam ("groupName", groupName));
        }

        #endregion

        #region Save / Delete

        public override void AddUpdateLocationsGroup (object groupObject)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (groupObject);

            using (DbTransaction transaction = new DbTransaction (this)) {

                // Check if we already have that group
                long temp = ExecuteScalar<long> ("SELECT count(*) FROM objectsgroups WHERE ID = @ID", helper.Parameters);

                // We are updating group
                if (temp == 1) {
                    temp = ExecuteScalar<long> ("SELECT count(*) FROM objectsgroups WHERE ID = @ID AND Code = @Code",
                        helper.Parameters);

                    // We have changed the parent
                    if (temp != 1)
                        LocationsGroupCalculateCode (helper);

                    temp = ExecuteNonQuery (string.Format ("UPDATE objectsgroups {0} WHERE ID = @ID",
                        helper.GetSetStatement (DataField.LocationsGroupsId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot update points of sale group with ID={0}", helper.GetObjectValue (DataField.LocationsGroupsId)));
                } // We are creating new group
                else if (temp == 0) {
                    LocationsGroupCalculateCode (helper);

                    temp = ExecuteNonQuery (string.Format ("INSERT INTO objectsgroups {0}",
                        helper.GetColumnsAndValuesStatement (DataField.LocationsGroupsId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot add points of sale group with name=\'{0}\'", helper.GetObjectValue (DataField.LocationsGroupsName)));

                    long id = GetLastAutoId ();
                    helper.SetObjectValue (DataField.LocationsGroupsId, id);
                } else
                    throw new Exception ("Too many entries with the same ID found in objectsgroups table.");

                transaction.Complete ();
            }
        }

        private void LocationsGroupCalculateCode (SqlHelper helper)
        {
            string groupCode = (string) helper.GetObjectValue (DataField.LocationsGroupsCode);
            string parentCode = string.Empty;
            object lastCodeUsed;
            if (groupCode.Length > 3) {
                parentCode = groupCode.Substring (0, groupCode.Length - 3);
                lastCodeUsed = ExecuteScalar ("SELECT MAX(substr(Code, @CodeStart, 3)) FROM objectsgroups WHERE substr(Code, 1, @ParentCodeLength) = @ParentCode AND (Code REGEXP '^[A-Z]+$')",
                    new DbParam ("CodeStart", groupCode.Length - 2),
                    new DbParam ("ParentCodeLength", groupCode.Length - 3),
                    new DbParam ("ParentCode", parentCode));
            } else {
                lastCodeUsed = ExecuteScalar ("SELECT MAX(substr(Code, @CodeStart, 3)) FROM objectsgroups WHERE (Code REGEXP '^[A-Z]+$')",
                    new DbParam ("CodeStart", groupCode.Length - 2));
            }

            string nextCode = GetNextGroupCode (IsDBNull (lastCodeUsed) ? null : (string) lastCodeUsed);
            nextCode = parentCode + nextCode;
            helper.SetParameterValue (DataField.LocationsGroupsCode, nextCode);
            helper.SetObjectValue (DataField.LocationsGroupsCode, nextCode);
        }

        public override void DeleteLocationsGroup (long groupId)
        {
            ExecuteNonQuery ("DELETE FROM objectsgroups WHERE ID = @groupId", new DbParam ("groupId", groupId));
        }

        public override DeletePermission CanDeleteLocationsGroup (long groupId)
        {
            if (groupId == -1 || groupId == 1)
                return DeletePermission.Reserved;

            if (groupId < -1)
                return DeletePermission.No;

            long ret = ExecuteScalar<long> ("SELECT count(*) FROM objects WHERE GroupID = @groupId",
                new DbParam ("groupId", groupId));

            if (ret != 0)
                return DeletePermission.InUse;

            string code = ExecuteScalar<string> ("SELECT Code FROM objectsgroups WHERE ID = @ID",
                new DbParam ("ID", groupId));

            if (string.IsNullOrEmpty (code))
                return DeletePermission.No;

            ret = ExecuteScalar<long> ("SELECT count(*) FROM objectsgroups WHERE substr(objectsgroups.Code, 1, @GCodeLength) = @GCode AND length(objectsgroups.Code) > @GCodeLength",
                new DbParam ("GCodeLength", code.Length),
                new DbParam ("GCode", code));

            return 0 == ret ? DeletePermission.Yes : DeletePermission.InUse;
        }

        #endregion

        private string LocationsGroupDefaultAliases ()
        {
            return GetAliasesString (DataField.LocationsGroupsId,
                DataField.LocationsGroupsCode,
                DataField.LocationsGroupsName);
        }
    }
}
