//
// DataProvider.Payment.cs
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
using System.Collections.Generic;
using System.Text;
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetAllPaymentsPerOperation<T> (DataQuery dataQuery, bool onlyPaid = false)
        {
            List<DataField> dataFields = new List<DataField> (paymentDefaultFields);
            dataFields.Remove (DataField.PaymentAmount);
            string query = string.Format (@"
                SELECT {0}, partners.Code AS {1}, partners.Company AS {2}, objects.Name AS {3}, objects.Code AS {4},
                    (SUM(payments.Qtty * payments.Mode) * CASE WHEN payments.OperType = 36 THEN {7} END) AS {5} 
                FROM payments LEFT JOIN partners ON payments.PartnerID = partners.ID
                    LEFT JOIN objects ON payments.ObjectID = objects.ID
                {6}
                GROUP BY payments.Acct, payments.OperType
                ORDER BY payments.ID",
                GetAliasesString (dataFields.ToArray ()),
                fieldsTable.GetFieldAlias (DataField.PartnerCode),
                fieldsTable.GetFieldAlias (DataField.PartnerName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.LocationCode),
                fieldsTable.GetFieldAlias (DataField.PaymentAmount),
                onlyPaid ? "WHERE payments.Mode = 1" : string.Empty,
                onlyPaid ? "-1 ELSE 1" : "1 ELSE -1");

            return ExecuteDataQuery<T> (dataQuery, query);
        }

        public override T [] GetPaymentsForOperation<T> (long operationId, int paymentType, int operationType, long locationId, long partnerId)
        {
            List<DbParam> pars = new List<DbParam>
                {
                    new DbParam ("OperType", operationType),
                    new DbParam ("Mode", paymentType)
                };
            if (operationId < 0) {
                pars.Add (new DbParam ("Acct", -3));
                pars.Add (new DbParam ("locationId", locationId));
                pars.Add (new DbParam ("partnerId", partnerId));
            } else
                pars.Add (new DbParam ("Acct", operationId));

            return ExecuteArray<T> (string.Format (@"SELECT {0} FROM payments
                WHERE OperType = @OperType AND Acct = @Acct AND Mode = @Mode{1}",
                PaymentDefaultAliases (),
                operationId < 0 ? " AND ObjectID = @locationId AND PartnerID = @partnerId" : string.Empty),
                pars.ToArray ());
        }

        public override LazyListModel<T> GetAdvancePayments<T> (long partnerId)
        {
            return ExecuteLazyModel<T> (string.Format (@"
                SELECT {0} FROM payments 
                WHERE payments.Mode = 1 AND OperType = @OperType AND payments.PartnerID = @partnerId", PaymentDefaultAliases ()),
                new DbParam ("OperType", OperationType.AdvancePayment),
                new DbParam ("partnerId", partnerId));
        }

        public override LazyListModel<T> GetAdvancePayments<T> (DataQuery dataQuery)
        {
            return ExecuteDataQuery<T> (dataQuery, string.Format (@"
                    SELECT {0}, partners.Company AS {1} FROM payments LEFT JOIN partners ON payments.PartnerID = partners.ID 
                    WHERE payments.Mode = 1 AND OperType = @OperType",
                    PaymentDefaultAliases (),
                    fieldsTable.GetFieldAlias (DataField.PartnerName)),
                new DbParam ("OperType", OperationType.AdvancePayment));
        }

        #endregion

        #region Save / Delete

        public override bool AddUpdatePayment (object paymentObject)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (paymentObject);

            bool ret = false;

            // Check if we already have that payment
            long temp = ExecuteScalar<long> ("SELECT count(*) FROM payments WHERE ID = @ID", helper.Parameters);

            // We are updating payment information
            if (temp == 1) {
                // Get the quantity from the new detail
                double newQty = (double) helper.GetObjectValue (DataField.PaymentAmount);
                if (newQty.IsZero ()) {
                    temp = ExecuteNonQuery ("DELETE FROM payments WHERE ID = @ID", helper.Parameters);
                    if ((OperationType) helper.GetObjectValue (DataField.PaymentOperationType) == OperationType.AdvancePayment)
                        DeleteOperationId (OperationType.AdvancePayment, (long) helper.GetObjectValue (DataField.PaymentOperationId));
                } else {
                    temp = ExecuteNonQuery (string.Format ("UPDATE payments {0} WHERE ID = @ID",
                        helper.GetSetStatement (DataField.PaymentId, DataField.PartnerName, DataField.LocationName, DataField.PartnerId)),
                            helper.Parameters);
                }

                if (temp != 1)
                    throw new Exception ("Unable to update operation detail.");
            } // We are creating new payment information
            else if (temp == 0) {
                temp = ExecuteNonQuery (string.Format ("INSERT INTO payments {0}",
                    helper.GetColumnsAndValuesStatement (DataField.PaymentId, DataField.PartnerName, DataField.LocationName, DataField.PartnerId)),
                        helper.Parameters);

                if (temp != 1)
                    throw new Exception ("Unable to insert operation detail.");

                long lastAutoId = GetLastAutoId ();
                helper.SetObjectValue (DataField.PaymentId, lastAutoId);

                ret = true;
            } else
                throw new Exception ("Wrong number of payments found with the given Id.");

            return ret;
        }

        public override void AddPayments (IEnumerable<object> payments)
        {
            SqlHelper helper = GetSqlHelper ();

            List<List<DbParam>> parameters = new List<List<DbParam>> ();
            foreach (object payment in payments) {
                helper.ChangeObject (payment, DataField.PaymentId);
                parameters.Add (new List<DbParam> (helper.Parameters));
            }

            BulkInsert ("payments", helper.GetColumns (DataField.PaymentId), parameters, "Unable to create payment.");
        }

        public override void DeletePayment (long paymentId)
        {
            ExecuteNonQuery ("DELETE FROM payments WHERE ID = @paymentId",
                new DbParam ("paymentId", paymentId));
        }

        public override void AddAdvancePayment (object payment)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (payment);

            // Check if we already have that payment
            long temp = ExecuteScalar<long> ("SELECT count(*) FROM payments WHERE ID = @ID", helper.Parameters);

            // We are updating payment information
            if (temp == 1) {
                // Get the quantity from the new detail
                double newQty = (double) helper.GetObjectValue (DataField.PaymentAmount);
                if (newQty.IsZero ()) {
                    temp = ExecuteNonQuery ("DELETE FROM payments WHERE ID = @ID", helper.Parameters);
                    DeleteOperationId (OperationType.AdvancePayment, (long) helper.GetObjectValue (DataField.PaymentOperationId));
                } else {
                    temp = ExecuteNonQuery (string.Format ("UPDATE payments {0} WHERE ID = @ID",
                        helper.GetSetStatement (DataField.PaymentId, DataField.PartnerName, DataField.LocationName, DataField.PartnerId)),
                            helper.Parameters);
                }

                if (temp != 1)
                    throw new Exception ("Unable to update operation detail.");
            } // We are creating new payment information
            else if (temp == 0) {
                long operationId = CreateNewOperationId (OperationType.AdvancePayment, 0);
                helper.SetObjectValue (DataField.PaymentOperationId, operationId);
                helper.SetObjectValue (DataField.PaymentOperationType, (int) OperationType.AdvancePayment);
                helper.ResetParameters ();

                temp = ExecuteNonQuery (string.Format ("INSERT INTO payments {0}",
                    helper.GetColumnsAndValuesStatement (DataField.PaymentId, DataField.PartnerName, DataField.LocationName, DataField.PartnerId)),
                        helper.Parameters);

                if (temp != 1)
                    throw new Exception ("Unable to insert payment.");

                long lastAutoId = GetLastAutoId ();
                helper.SetObjectValue (DataField.PaymentId, lastAutoId);
            } else
                throw new Exception ("Wrong number of payments found with the given Id.");
        }

        public override void EditAdvancePayment (object payment)
        {
            using (DbTransaction transaction = new DbTransaction (this)) {

                SqlHelper helper = GetSqlHelper ();
                helper.AddObject (payment);

                ExecuteNonQuery (string.Format ("UPDATE payments {0} WHERE payments.ID = @ID",
                    helper.GetSetStatement (DataField.PaymentId, DataField.PartnerName, DataField.LocationName, DataField.PartnerId)),
                    helper.Parameters);

                transaction.Complete ();
            }
        }

        #endregion

        #region Reports

        public override DataQueryResult ReportPaymentsByDocuments (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT payments.Acct AS {0}, payments.OperType AS {1}, 
                  MIN(payments.Date) AS {2}, partners.Company AS {3}, partnersgroups.Name,
                  objects.Name AS {4}, objectsgroups.Name, users.Name AS {5}, usersgroups.Name,
                  SUM(CASE payments.Mode WHEN -1 THEN payments.Qtty ELSE 0 END) AS {6}, 
                  SUM(payments.Qtty * payments.Mode * - 1) AS {7},
                  SUM(CASE paymenttypes.PaymentMethod * payments.Mode WHEN 1 THEN payments.Qtty ELSE 0 END) AS {8},  
                  SUM(CASE paymenttypes.PaymentMethod * payments.Mode WHEN 2 THEN payments.Qtty ELSE 0 END) AS {9},  
                  SUM(CASE paymenttypes.PaymentMethod * payments.Mode WHEN 3 THEN payments.Qtty ELSE 0 END) AS {10},  
                  SUM(CASE paymenttypes.PaymentMethod * payments.Mode WHEN 4 THEN payments.Qtty ELSE 0 END) AS {11}
                FROM (((((payments LEFT JOIN partners ON payments.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                  LEFT JOIN paymenttypes ON payments.Type = paymenttypes.ID) 
                  LEFT JOIN objects ON payments.ObjectID = objects.ID) 
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users ON payments.UserID = users.ID  
                  LEFT JOIN usersgroups ON ABS(users.GroupID) = usersgroups.ID
                GROUP BY payments.Acct, payments.OperType, partners.Company, partnersgroups.Name, 
                  objects.Name, objectsgroups.Name, users.Name, usersgroups.Name",
                fieldsTable.GetFieldAlias (DataField.PaymentOperationId),
                fieldsTable.GetFieldAlias (DataField.PaymentOperationType),
                fieldsTable.GetFieldAlias (DataField.PaymentDate),
                fieldsTable.GetFieldAlias (DataField.PartnerName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.UserName),
                fieldsTable.GetFieldAlias (DataField.PaymentDueSum),
                fieldsTable.GetFieldAlias (DataField.PaymentRemainingSum),
                fieldsTable.GetFieldAlias (DataField.PaymentsInCash),
                fieldsTable.GetFieldAlias (DataField.PaymentsByBankOrder),
                fieldsTable.GetFieldAlias (DataField.PaymentsByDebitCreditCard),
                fieldsTable.GetFieldAlias (DataField.PaymentsByVoucher));

            querySet.SetComplexId (DbTable.Payments, DataField.PaymentOperationId, DataField.PaymentOperationType);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportPaymentsByPartners (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT partners.ID, payments.OperType AS {0}, partners.Company AS {1}, partnersgroups.Name,
                  objects.Name AS {2}, objectsgroups.Name, users.Name AS {3}, usersgroups.Name,
                  SUM(CASE payments.Mode WHEN - 1 THEN payments.Qtty ELSE 0 END) AS {4}, 
                  SUM(payments.Qtty * payments.Mode * -1) AS {5},
                  SUM(CASE paymenttypes.PaymentMethod * payments.Mode WHEN 1 THEN payments.Qtty ELSE 0 END) AS {6},  
                  SUM(CASE paymenttypes.PaymentMethod * payments.Mode WHEN 2 THEN payments.Qtty ELSE 0 END) AS {7},  
                  SUM(CASE paymenttypes.PaymentMethod * payments.Mode WHEN 3 THEN payments.Qtty ELSE 0 END) AS {8},  
                  SUM(CASE paymenttypes.PaymentMethod * payments.Mode WHEN 4 THEN payments.Qtty ELSE 0 END) AS {9}
                FROM (((((payments LEFT JOIN partners ON payments.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                  LEFT JOIN paymenttypes ON payments.Type = paymenttypes.ID)
                  LEFT JOIN objects ON payments.ObjectID = objects.ID) 
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users ON payments.UserID = users.ID
                  LEFT JOIN usersgroups ON ABS(users.GroupID) = usersgroups.ID
                GROUP BY payments.OperType, partners.ID, partners.Company, partnersgroups.Name, 
                  objects.Name, objectsgroups.Name, users.Name, usersgroups.Name",
                fieldsTable.GetFieldAlias (DataField.PaymentOperationType),
                fieldsTable.GetFieldAlias (DataField.PartnerName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.UserName),
                fieldsTable.GetFieldAlias (DataField.PaymentDueSum),
                fieldsTable.GetFieldAlias (DataField.PaymentRemainingSum),
                fieldsTable.GetFieldAlias (DataField.PaymentsInCash),
                fieldsTable.GetFieldAlias (DataField.PaymentsByBankOrder),
                fieldsTable.GetFieldAlias (DataField.PaymentsByDebitCreditCard),
                fieldsTable.GetFieldAlias (DataField.PaymentsByVoucher));

            querySet.SetSimpleId (DbTable.Partners, DataField.PartnerId, 0);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportPaymentsDueDates (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT payments.Acct AS {0}, payments.OperType AS {1}, 
                  MIN(payments.Date) AS {2}, payments.EndDate AS {3}, 
                  partners.Company AS {4}, partnersgroups.Name, objects.Name AS {5}, objectsgroups.Name,
                  SUM(CASE payments.Mode WHEN - 1 THEN payments.Qtty ELSE 0 END) AS {6}, 
                  SUM(payments.Qtty * payments.Mode * - 1) AS {7}
                FROM (((payments LEFT JOIN partners ON payments.PartnerID = partners.ID) 
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                  LEFT JOIN objects ON payments.ObjectID = objects.ID) 
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID
                GROUP BY payments.Acct, payments.OperType, payments.EndDate, 
                  partners.Company, partnersgroups.Name, objects.Name, objectsgroups.Name",
                fieldsTable.GetFieldAlias (DataField.PaymentOperationId),
                fieldsTable.GetFieldAlias (DataField.PaymentOperationType),
                fieldsTable.GetFieldAlias (DataField.PaymentDate),
                fieldsTable.GetFieldAlias (DataField.PaymentEndDate),
                fieldsTable.GetFieldAlias (DataField.PartnerName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.PaymentDueSum),
                fieldsTable.GetFieldAlias (DataField.PaymentRemainingSum));

            querySet.SetComplexId (DbTable.Payments, DataField.PaymentOperationId, DataField.PaymentOperationType);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportPaymentsHistory (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT payments.Acct as {0}, payments.Date as {1},
                  partners.Company, partnersgroups.Name,
                  payments.OperType AS {2},
                  payments.Type as {3},
                  SUM(payments.Qtty * payments.Mode) as {4}
                FROM (payments LEFT JOIN partners ON payments.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID
                GROUP BY payments.Acct, payments.Date, partners.Company, payments.OperType, payments.ID, payments.Type",
                fieldsTable.GetFieldAlias (DataField.PaymentOperationId),
                fieldsTable.GetFieldAlias (DataField.PaymentDate),
                fieldsTable.GetFieldAlias (DataField.PaymentOperationType),
                fieldsTable.GetFieldAlias (DataField.PaymentTypeId),
                fieldsTable.GetFieldAlias (DataField.OperationSum));

            querySet.SetComplexId (DbTable.Payments, DataField.PaymentOperationId, DataField.PaymentOperationType);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportIncome (DataQuery querySet)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddParameters (new DbParam ("paymentMode", 1));

            string query = string.Format (@"
                SELECT payments.Acct AS {0}, payments.Date AS {1}, 
                  partners.Company AS {2}, partnersgroups.Name, 
                  objects.Name AS {3}, objectsgroups.Name,
                  users.Name AS {4}, usersgroups.Name, 
                  payments.OperType AS {5}, 
                  payments.Type AS {6},
                  SUM(payments.Qtty * payments.Mode) AS {7}  
                FROM ((((payments LEFT JOIN partners ON payments.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                  LEFT JOIN objects ON payments.ObjectID = objects.ID) 
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users ON payments.UserID = users.ID 
                  LEFT JOIN usersgroups ON ABS(users.GroupID) = usersgroups.ID
                WHERE payments.Mode = @paymentMode
                GROUP BY payments.Acct, payments.Date, partners.Company, partnersgroups.Name, 
                  objects.Name, objectsgroups.Name, users.Name, usersgroups.Name,
                  payments.Mode, payments.Qtty, payments.Type, payments.OperType",
                fieldsTable.GetFieldAlias (DataField.PaymentOperationId),
                fieldsTable.GetFieldAlias (DataField.PaymentDate),
                fieldsTable.GetFieldAlias (DataField.PartnerName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.UserName),
                fieldsTable.GetFieldAlias (DataField.PaymentOperationType),
                fieldsTable.GetFieldAlias (DataField.PaymentTypeId),
                fieldsTable.GetFieldAlias (DataField.OperationSum));

            querySet.SetComplexId (DbTable.Payments, DataField.PaymentOperationId, DataField.PaymentOperationType);

            return ExecuteDataQuery (querySet, query, helper.Parameters);
        }

        public override DataQueryResult ReportPaymentsAdvance (DataQuery querySet)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddParameters (new DbParam ("paymentMode", 1));
            helper.AddParameters (new DbParam ("operationType", OperationType.AdvancePayment));

            string query = string.Format (@"
                SELECT payments.Acct AS {0}, payments.Date AS {1}, 
                  partners.Company AS {2}, partnersgroups.Name, 
                  users.Name AS {3}, usersgroups.Name, 
                  payments.Type AS {4},
                  SUM(payments.Qtty * payments.Mode) AS {5} 
                FROM (((payments LEFT JOIN partners ON payments.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                  LEFT JOIN objects ON payments.ObjectID = objects.ID) 
                  LEFT JOIN users ON payments.UserID = users.ID 
                  LEFT JOIN usersgroups ON ABS(users.GroupID) = usersgroups.ID
                WHERE payments.Mode = @paymentMode AND payments.OperType = @operationType
                GROUP BY payments.Acct, payments.Date, partners.Company, partnersgroups.Name, 
                  users.Name, usersgroups.Name,
                  payments.Mode, payments.Qtty, payments.Type",
                fieldsTable.GetFieldAlias (DataField.PaymentOperationId),
                fieldsTable.GetFieldAlias (DataField.PaymentDate),
                fieldsTable.GetFieldAlias (DataField.PartnerName),
                fieldsTable.GetFieldAlias (DataField.UserName),
                fieldsTable.GetFieldAlias (DataField.PaymentTypeId),
                fieldsTable.GetFieldAlias (DataField.PaymentAmount));

            querySet.SetComplexId (DbTable.Payments, DataField.PaymentOperationId, DataField.PaymentOperationType, OperationType.AdvancePayment);

            return ExecuteDataQuery (querySet, query, helper.Parameters);
        }

        public override DataQueryResult ReportTurnover (DataQuery querySet)
        {
            return ExecuteDataQuery (querySet, string.Format (@"
                SELECT users.Name as {0},
                  paymenttypes.Name as {1},
                  SUM(payments.Qtty * payments.Mode * payments.Sign) as {2}
                FROM (payments INNER JOIN paymenttypes ON payments.Type = paymenttypes.ID) 
                  LEFT JOIN users ON payments.UserID = users.ID
                WHERE payments.Mode = 1 AND payments.OperType IN (@saleType, @returnType)
                GROUP BY payments.UserID, payments.Type",
                fieldsTable.GetFieldAlias (DataField.UserName),
                fieldsTable.GetFieldAlias (DataField.PaymentTypesName),
                fieldsTable.GetFieldAlias (DataField.PaymentAmount)),
                new DbParam ("saleType", (int) OperationType.Sale),
                new DbParam ("returnType", (int) OperationType.Return));
        }

        #endregion

        private string PaymentDefaultAliases ()
        {
            return GetAliasesString (paymentDefaultFields);
        }

        private static readonly DataField [] paymentDefaultFields = new [] {
            DataField.PaymentId,
            DataField.PaymentOperationId,
            DataField.PaymentOperationType,
            DataField.PaymentPartnerId,
            DataField.PaymentAmount,
            DataField.PaymentMode,
            DataField.PaymentDate,
            DataField.PaymentOperatorId,
            DataField.PaymentTimeStamp,
            DataField.PaymentTypeId,
            DataField.PaymentTransaction,
            DataField.PaymentEndDate,
            DataField.PaymentLocationId,
            DataField.PaymentSign };
    };
}
