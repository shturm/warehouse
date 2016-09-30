//
// DataProvider.CompanyRecord.cs
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

        public override LazyListModel<T> GetAllCompanyRecords<T> ()
        {
            return ExecuteLazyModel<T> (string.Format ("SELECT {0} FROM registration", CompanyDefaultAliases ()));
        }

        public override T GetCompanyRecordById<T> (long companyRecordId)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM registration WHERE ID = @companyRecordId", CompanyDefaultAliases ()),
                new DbParam ("companyRecordId", companyRecordId));
        }

        public override T GetCompanyRecordByName<T> (string companyRecordName)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM registration WHERE Company = @companyRecordName", CompanyDefaultAliases ()),
                new DbParam ("companyRecordName", companyRecordName));
        }

        public override T GetCompanyRecordByCode<T> (string companyRecordCode)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM registration WHERE Code = @companyRecordCode", CompanyDefaultAliases ()),
                new DbParam ("companyRecordCode", companyRecordCode));
        }

        public override T GetDefaultCompanyRecord<T> ()
        {
            long temp = ExecuteScalar<long> ("SELECT count(*) FROM registration WHERE IsDefault = -1");

            return ExecuteObject<T> (temp == 0 ?
                string.Format ("SELECT {0} FROM registration WHERE ID = 1", CompanyDefaultAliases ()) :
                string.Format ("SELECT {0} FROM registration WHERE IsDefault = -1", CompanyDefaultAliases ()));
        }

        #endregion

        #region Save / Delete

        public override void AddUpdateCompanyRecord (object companyRecordObject)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (companyRecordObject);

            using (DbTransaction transaction = new DbTransaction (this)) {

                // Check if we already have that item
                long temp = ExecuteScalar<long> ("SELECT count(*) FROM registration WHERE ID = @ID", helper.Parameters);

                // We are updating company record
                long id;
                if (temp == 1) {
                    temp = ExecuteNonQuery (string.Format ("UPDATE registration {0} WHERE ID = @ID",
                        helper.GetSetStatement (DataField.CompanyId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot add company record with id=\'{0}\'", helper.GetObjectValue (DataField.CompanyId)));

                    id = Convert.ToInt64 (helper.GetObjectValue (DataField.CompanyId));
                } // We are creating new company record
                else if (temp == 0) {
                    temp = ExecuteNonQuery (string.Format ("INSERT INTO registration {0}",
                        helper.GetColumnsAndValuesStatement (DataField.CompanyId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot add company record with name=\'{0}\'", helper.GetObjectValue (DataField.CompanyName)));

                    id = GetLastAutoId ();
                    helper.SetObjectValue (DataField.CompanyId, id);
                } else
                    throw new Exception ("Too many entries with the same ID found in registration table.");

                // If the newly placed record is default then remove any other default record(s)
                if ((int) helper.GetObjectValue (DataField.CompanyDefault) == -1) {
                    ExecuteNonQuery (string.Format ("UPDATE registration SET IsDefault = 0 WHERE ID != {0}", id));
                } // If the newly placed record is not default see if there are other default record(s)
                else {
                    temp = ExecuteScalar<long> ("SELECT count(*) FROM registration WHERE IsDefault = -1");

                    if (temp == 0)
                        ExecuteNonQuery ("UPDATE registration SET IsDefault = -1 WHERE ID = 1");
                }

                transaction.Complete ();
            }
        }

        public override void DeleteCompanyRecord (long companyRecordId)
        {
            DbParam par = new DbParam ("companyRecordId", companyRecordId);

            int isDefault = ExecuteScalar<int> ("SELECT IsDefault FROM registration WHERE ID = @companyRecordId", par);

            ExecuteNonQuery ("DELETE FROM registration WHERE ID = @companyRecordId", par);

            // If the deleted record was the default one then set the reserved record as default
            if (isDefault == -1)
                ExecuteNonQuery ("UPDATE registration SET IsDefault = -1 WHERE ID = 1");

        }

        public override DeletePermission CanDeleteCompanyRecord (long companyRecordId)
        {
            return companyRecordId == 1 ? DeletePermission.Reserved : DeletePermission.Yes;
        }

        #endregion

        private string CompanyDefaultAliases ()
        {
            return GetAliasesString (DataField.CompanyId,
                DataField.CompanyCode,
                DataField.CompanyName,
                DataField.CompanyLiablePerson,
                DataField.CompanyCity,
                DataField.CompanyAddress,
                DataField.CompanyPhone,
                DataField.CompanyFax,
                DataField.CompanyEmail,
                DataField.CompanyTaxNumber,
                DataField.CompanyBulstat,
                DataField.CompanyBankName,
                DataField.CompanyBankCode,
                DataField.CompanyBankAcct,
                DataField.CompanyBankVATAcct,
                DataField.CompanyCreatorId,
                DataField.CompanyCreationTimeStamp,
                DataField.CompanyDefault,
                DataField.CompanyNote1,
                DataField.CompanyNote2);
        }
    }
}
