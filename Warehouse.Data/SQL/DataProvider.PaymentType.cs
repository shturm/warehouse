//
// DataProvider.PaymentType.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/23/2008
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

        public override LazyListModel<T> GetAllPaymentMethods<T> ()
        {
            return ExecuteLazyModel<T> (string.Format (@"
                SELECT {0}
                FROM paymenttypes",
                PaymentTypeDefaultAliases ()));
        }

        public override T GetPaymentMethodById<T> (long paymentTypeId)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM paymenttypes
                WHERE ID = @paymentTypeId",
                PaymentTypeDefaultAliases ()),
                new DbParam ("paymentTypeId", paymentTypeId));
        }

        public override T GetPaymentMethodByName<T> (string paymentTypeName)
        {
            return ExecuteObject<T> (string.Format (@"
                SELECT {0}
                FROM paymenttypes
                WHERE Name = @paymentTypeName",
                PaymentTypeDefaultAliases ()),
                new DbParam ("paymentTypeName", paymentTypeName));
        }

        #endregion

        #region Save / Delete

        public override void AddUpdatePaymentMethod (object paymentTypeObject)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (paymentTypeObject);

            using (DbTransaction transaction = new DbTransaction (this)) {

                // Check if we already have that item
                long temp = ExecuteScalar<long> ("SELECT count(*) FROM paymenttypes WHERE ID = @ID", helper.Parameters);

                // We are updating location
                if (temp == 1) {
                    temp = ExecuteNonQuery (string.Format ("UPDATE paymenttypes {0} WHERE ID = @ID",
                        helper.GetSetStatement (DataField.PaymentTypesId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot update payment type with ID={0}", helper.GetObjectValue (DataField.PaymentTypesId)));
                } // We are creating new location
                else if (temp == 0) {
                    temp = ExecuteNonQuery (string.Format ("INSERT INTO paymenttypes {0}",
                        helper.GetColumnsAndValuesStatement (DataField.PaymentTypesId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot add payment type with name=\'{0}\'", helper.GetObjectValue (DataField.PaymentTypesName)));

                    temp = GetLastAutoId ();
                    helper.SetObjectValue (DataField.PaymentTypesId, temp);
                } else
                    throw new Exception ("Too many entries with the same ID found in paymenttypes table.");

                transaction.Complete ();
            }
        }

        public override DeletePermission CanDeletePaymentMethod (long paymentTypeId)
        {
            if (paymentTypeId == 1)
                return DeletePermission.Reserved;

            DbParam par = new DbParam ("paymentTypeId", paymentTypeId);
            long ret = ExecuteScalar<long> ("SELECT count(*) FROM payments WHERE Type = @paymentTypeId", par);
            if (ret != 0)
                return DeletePermission.InUse;

            ret = ExecuteScalar<long> ("SELECT count(*) FROM documents WHERE PaymentType = @paymentTypeId", par);
            if (ret != 0)
                return DeletePermission.InUse;

            return DeletePermission.Yes;
        }

        public override void DeletePaymentMethod (long paymentTypeId)
        {
            DbParam par = new DbParam ("paymentTypeId", paymentTypeId);

            ExecuteNonQuery ("DELETE FROM paymenttypes WHERE ID = @paymentTypeId", par);
        }

        #endregion

        private string PaymentTypeDefaultAliases ()
        {
            return GetAliasesString (DataField.PaymentTypesId,
                DataField.PaymentTypesName,
                DataField.PaymentTypesMethod);
        }
    }
}
