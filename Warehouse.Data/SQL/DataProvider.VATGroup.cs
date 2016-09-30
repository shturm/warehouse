//
// DataProvider.VATGroup.cs
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
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetAllVATGroups<T> ()
        {
            return ExecuteLazyModel<T> (string.Format (@"
                SELECT {0}
                FROM vatgroups",
                VATGroupDefaultAliases ()));
        }

        public override T GetVATGroupById<T> (long vatGroupId)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM vatgroups
                WHERE ID = @vatGroupId",
                VATGroupDefaultAliases ()),
                new DbParam ("vatGroupId", vatGroupId));
        }

        public override T GetVATGroupByCode<T> (string vatGroupCode)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM vatgroups
                WHERE Code = @vatGroupCode",
                VATGroupDefaultAliases ()),
                new DbParam ("vatGroupCode", vatGroupCode));
        }

        public override T GetVATGroupByName<T> (string vatGroupName)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM vatgroups
                WHERE Name = @vatGroupName",
                VATGroupDefaultAliases ()),
                new DbParam ("vatGroupName", vatGroupName));
        }

        #endregion

        #region Save / Delete

        public override void AddUpdateVATGroup (object vatGroupObject)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (vatGroupObject);

            using (DbTransaction transaction = new DbTransaction (this)) {

                // Check if we already have that goods
                long temp = ExecuteScalar<long> ("SELECT count(*) FROM vatgroups WHERE ID = @ID", helper.Parameters);

                // We are updating location
                if (temp == 1) {
                    temp = ExecuteNonQuery (string.Format ("UPDATE vatgroups {0} WHERE ID = @ID",
                        helper.GetSetStatement (DataField.VATGroupId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot update VAT Group with ID={0}", helper.GetObjectValue (DataField.VATGroupId)));
                } // We are creating new location
                else if (temp == 0) {
                    temp = ExecuteNonQuery (string.Format ("INSERT INTO vatgroups {0}",
                        helper.GetColumnsAndValuesStatement (DataField.VATGroupId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot add VAT Group with name=\'{0}\'", helper.GetObjectValue (DataField.VATGroupName)));

                    temp = GetLastAutoId ();
                    helper.SetObjectValue (DataField.VATGroupId, temp);
                } else
                    throw new Exception ("Too many entries with the same ID found in vatgroups table.");

                transaction.Complete ();
            }
        }

        public override void DeleteVATGroup (long vatGroupId)
        {
            DbParam par = new DbParam ("vatGroupId", vatGroupId);

            ExecuteNonQuery ("DELETE FROM vatgroups WHERE ID = @vatGroupId", par);
        }

        public override DeletePermission CanDeleteVATGroup (long vatGroupId)
        {
            if (vatGroupId == 1)
                return DeletePermission.Reserved;

            long ret = ExecuteScalar<long> ("SELECT count(*) FROM goods WHERE TaxGroup = @vatGroupId",
                new DbParam ("vatGroupId", vatGroupId));

            return 0 == ret ? DeletePermission.Yes : DeletePermission.InUse;
        }

        #endregion

        private string VATGroupDefaultAliases ()
        {
            return GetAliasesString (DataField.VATGroupId,
                DataField.VATGroupCode,
                DataField.VATGroupName,
                DataField.VATGroupValue);
        }
    }
}
