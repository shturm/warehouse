//
// DataProvider.Partner.cs
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

        public override LazyListModel<T> GetAllPartners<T> (long? groupId)
        {
            if (groupId.HasValue) {
                if (groupId == int.MinValue) {
                    return ExecuteLazyModel<T> (false, string.Format (@"
                        SELECT {0}
                        FROM partners
                        WHERE Deleted = -1", PartnerDefaultAliases ()));
                }
                return ExecuteLazyModel<T> (false, string.Format (@"
                    SELECT {0}
                    FROM partners 
                    WHERE ABS(GroupID) = @groupId AND Deleted <> -1", PartnerDefaultAliases ()),
                    new DbParam ("groupId", groupId.Value));
            }

            return ExecuteLazyModel<T> (false, string.Format (@"
                SELECT {0}
                FROM partners
                WHERE Deleted <> -1", PartnerDefaultAliases ()));
        }

        public override T GetPartnerById<T> (long partnerId)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM partners WHERE ID = @partnerId", PartnerDefaultAliases ()),
                new DbParam ("partnerId", partnerId));
        }

        public override T GetPartnerByName<T> (string partnerName)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM partners WHERE Company = @partnerName AND Deleted <> -1", PartnerDefaultAliases ()),
                new DbParam ("partnerName", partnerName));
        }

        public override T GetPartnerByCode<T> (string partnerCode)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM partners WHERE Code = @partnerCode AND Deleted <> -1", PartnerDefaultAliases ()),
                new DbParam ("partnerCode", partnerCode));
        }

        public override T GetPartnerByBulstat<T> (string partnerBulstat)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM partners WHERE Bulstat = @partnerBulstat AND Deleted <> -1", PartnerDefaultAliases ()),
                new DbParam ("partnerBulstat", partnerBulstat));
        }

        /// <summary>
        /// Gets the partner with the specified number on a magnetic card.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="partnerCardNo">The number of the partner's magnetic card.</param>
        /// <returns>
        /// The partner with the specified number on a magnetic card.
        /// </returns>
        public override T GetPartnerByCard<T> (string partnerCardNo)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM partners WHERE CardNumber = @partnerCardNo", PartnerDefaultAliases ()),
                new DbParam ("partnerCardNo", partnerCardNo));
        }

        public override T GetPartnerByEmail<T> (string email)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM partners WHERE eMail = @email AND Deleted <> -1", PartnerDefaultAliases ()),
                new DbParam ("email", email));
        }

        public override T GetPartnerByPhone<T> (string phone)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM partners WHERE Telephone = @phone AND Deleted <> -1", PartnerDefaultAliases ()),
                new DbParam ("phone", phone));
        }

        public override T GetPartnerByLiablePerson<T> (string name)
        {
            return ExecuteObject<T> (string.Format ("SELECT {0} FROM partners WHERE MOL = @name AND Deleted <> -1", PartnerDefaultAliases ()),
                new DbParam ("name", name));
        }

        public override double GetPartnerTurnover (long partnerId)
        {
            object scalar = ExecuteScalar (string.Format (
                @"SELECT SUM({0}) FROM payments
                  WHERE Mode > 0 AND OperType IN (@sale, @consignmentSale, @debitNote) AND PartnerID = @partnerId",
                fieldsTable.GetFieldFullName (DataField.PaymentAmount)),
                new DbParam ("sale", OperationType.Sale),
                new DbParam ("consignmentSale", OperationType.ConsignmentSale),
                new DbParam ("debitNote", OperationType.DebitNote),
                new DbParam ("partnerID", partnerId));

            return scalar == null || IsDBNull (scalar) ? -1d : (double) scalar;
        }

        public override double GetPartnerDebt (long partnerId)
        {
            object scalar = ExecuteScalar (string.Format (
                @"SELECT -1 * SUM({0} * {1} * {2}) FROM payments 
                  WHERE PartnerID = @partnerID",
                fieldsTable.GetFieldFullName (DataField.PaymentAmount),
                fieldsTable.GetFieldFullName (DataField.PaymentMode),
                fieldsTable.GetFieldFullName (DataField.PaymentSign)),
                new DbParam ("partnerID", partnerId));

            return scalar == null || IsDBNull (scalar) ? 0 : (double) scalar;
        }

        public override T [] GetPartnerDuePayments<T> (long partnerId)
        {
            string query = string.Format (@"
                SELECT {0}, (-1 * SUM({1} * {2})) AS {3}, partners.Company AS {4}
                FROM payments INNER JOIN partners ON payments.PartnerID = partners.ID
                WHERE payments.PartnerID = @partnerId
                GROUP BY payments.Acct, payments.OperType, partners.Company
                HAVING {3} > 0",
                PaymentDefaultAliases (),
                fieldsTable.GetFieldFullName (DataField.PaymentAmount),
                fieldsTable.GetFieldFullName (DataField.PaymentMode),
                fieldsTable.GetFieldAlias (DataField.PaymentAmount),
                fieldsTable.GetFieldAlias (DataField.PartnerName));

            return ExecuteArray<T> (query, new DbParam ("partnerID", partnerId));
        }

        public override double GetPartnerUnpaidAmountWithExpiredDueDate (long partnerId, DateTime operationDate)
        {
            object scalar = ExecuteScalar (string.Format (
                "SELECT -1 * SUM({0} * {1}) FROM payments WHERE PartnerID = @partnerId AND EndDate <= @endDate",
                fieldsTable.GetFieldFullName (DataField.PaymentAmount),
                fieldsTable.GetFieldFullName (DataField.PaymentMode)),
                new DbParam ("partnerID", partnerId),
                new DbParam ("endDate", operationDate));

            return scalar == null || IsDBNull (scalar) ? -1d : (double) scalar;
        }

        #endregion

        #region Save / Delete

        public override void AddUpdatePartner (object partnerObject)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (partnerObject);

            using (DbTransaction transaction = new DbTransaction (this)) {

                // Check if we already have that item
                long temp = ExecuteScalar<long> ("SELECT count(*) FROM partners WHERE ID = @ID", helper.Parameters);

                // We are updating item information
                if (temp == 1) {
                    helper.UpdateTimeStamp = false;

                    temp = ExecuteNonQuery (string.Format ("UPDATE partners {0} WHERE ID = @ID",
                        helper.GetSetStatement (DataField.PartnerId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot update partner with id=\'{0}\'", helper.GetObjectValue (DataField.PartnerId)));
                } // We are creating new item information
                else if (temp == 0) {
                    temp = ExecuteNonQuery (string.Format ("INSERT INTO partners {0}",
                        helper.GetColumnsAndValuesStatement (DataField.PartnerId)), helper.Parameters);

                    if (temp != 1)
                        throw new Exception (string.Format ("Cannot add partner with name=\'{0}\'", helper.GetObjectValue (DataField.PartnerName)));

                    temp = GetLastAutoId ();
                    helper.SetObjectValue (DataField.PartnerId, temp);
                } else
                    throw new Exception ("Too many entries with the same ID found in partners table.");

                transaction.Complete ();
            }
        }

        public override void DeletePartner (long partnerId)
        {
            ExecuteNonQuery ("DELETE FROM partners WHERE ID = @partnerId",
                new DbParam ("partnerId", partnerId));
        }

        public override DeletePermission CanDeletePartner (long partnerId)
        {
            if (partnerId == 1)
                return DeletePermission.Reserved;

            long ret = ExecuteScalar<long> ("SELECT count(*) FROM operations WHERE PartnerID = @partnerId",
                new DbParam ("partnerId", partnerId));
            return 0 == ret ? DeletePermission.Yes : DeletePermission.InUse;
        }

        #endregion

        #region Partner Reports

        public override DataQueryResult ReportPartners (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT partners.ID, partners.Code as {0}, partners.Company as {1}, partners.City as {2}, partners.Address as {3},
                 partners.MOL as {4}, partners.Bulstat as {5},
                 partners.PriceGroup as {6},
                 partners.Discount as {7},
                 partners.Type as {8},
                 partners.Phone as {9}, partners.Fax as {10}, partners.Address2 as {11}, partners.eMail as {12},
                 partners.BankName as {13}, partners.BankCode as {14}, partners.BankAcct as {15},
                 partners.BankVATName as {16}, partners.BankVATCode as {17}, partners.BankVATAcct as {18},
                 partnersgroups.Name
                FROM partners LEFT JOIN partnersgroups ON partners.GroupID = partnersgroups.ID",
                fieldsTable.GetFieldAlias (DataField.PartnerCode),
                fieldsTable.GetFieldAlias (DataField.PartnerName),
                fieldsTable.GetFieldAlias (DataField.PartnerCity),
                fieldsTable.GetFieldAlias (DataField.PartnerAddress),
                fieldsTable.GetFieldAlias (DataField.PartnerLiablePerson),
                fieldsTable.GetFieldAlias (DataField.PartnerBulstat),
                fieldsTable.GetFieldAlias (DataField.PriceGroup),
                fieldsTable.GetFieldAlias (DataField.PartnerDiscount),
                fieldsTable.GetFieldAlias (DataField.PartnerType),
                fieldsTable.GetFieldAlias (DataField.PartnerPhone),
                fieldsTable.GetFieldAlias (DataField.PartnerFax),
                fieldsTable.GetFieldAlias (DataField.PartnerAddress2),
                fieldsTable.GetFieldAlias (DataField.PartnerEmail),
                fieldsTable.GetFieldAlias (DataField.PartnerBankName),
                fieldsTable.GetFieldAlias (DataField.PartnerBankCode),
                fieldsTable.GetFieldAlias (DataField.PartnerBankAcct),
                fieldsTable.GetFieldAlias (DataField.PartnerBankVATName),
                fieldsTable.GetFieldAlias (DataField.PartnerBankVATCode),
                fieldsTable.GetFieldAlias (DataField.PartnerBankVATAcct));

            querySet.SetSimpleId (DbTable.Partners, DataField.PartnerId, 0);

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportPartnersByProfit (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT operations.Date as {0}, goods.Code as {1}, goods.Name as {2},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.Name, objects.Name as {3}, objectsgroups.Name, 
                  users1.Name as {4}, usersgroups1.Name, partners.Company, partnersgroups.Name,
                  SUM(-{11} * operations.Qtty * (operations.PriceOut - operations.PriceIn )) as {5}
                FROM ((((((((operations LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN objectsgroups ON ABS(objects.GroupID) = objectsgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                WHERE operations.OperType IN ({6},{7},{8},{9},{10}) AND operations.PartnerID <> 0 AND operations.Acct > 0
                GROUP BY goods.Code, operations.Date, goods.Name,
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name, 
                  objects.Name, objectsgroups.Name, users1.Name, usersgroups1.Name, partners.Company, partnersgroups.Name",
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.LocationName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationProfit),
                (int) OperationType.Sale,
                (int) OperationType.ConsignmentSale,
                (int) OperationType.Return,
                (int) OperationType.CreditNote,
                (int) OperationType.DebitNote,
                GetOperationsPriceSignQuery ());

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportPartnersDebt (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT partners.ID, partners.Code as {0}, partners.Company as {1}, partners.City as {2}, partners.Address as {3},
                 partners.MOL as {4}, partners.Bulstat as {5},
                 partnersgroups.Name, -1 * SUM(payments.Qtty * payments.Mode * payments.Sign) as {6}
                FROM (partners LEFT JOIN partnersgroups ON partners.GroupID = partnersgroups.ID) LEFT JOIN payments ON partners.ID = payments.PartnerID
                GROUP BY partners.ID",
                fieldsTable.GetFieldAlias (DataField.PartnerCode),
                fieldsTable.GetFieldAlias (DataField.PartnerName),
                fieldsTable.GetFieldAlias (DataField.PartnerCity),
                fieldsTable.GetFieldAlias (DataField.PartnerAddress),
                fieldsTable.GetFieldAlias (DataField.PartnerLiablePerson),
                fieldsTable.GetFieldAlias (DataField.PartnerBulstat),
                fieldsTable.GetFieldAlias (DataField.PartnerDebt));

            querySet.SetSimpleId (DbTable.Partners, DataField.PartnerId, 0);

            return ExecuteDataQuery (querySet, query);
        }

        #endregion

        private static DataField [] PartnerDefaultFields ()
        {
            return new [] {
                DataField.PartnerId,
                DataField.PartnerCode,
                DataField.PartnerName,
                DataField.PartnerName2,
                DataField.PartnerLiablePerson,
                DataField.PartnerLiablePerson2,
                DataField.PartnerCity,
                DataField.PartnerCity2,
                DataField.PartnerAddress,
                DataField.PartnerAddress2,
                DataField.PartnerPhone,
                DataField.PartnerPhone2,
                DataField.PartnerFax,
                DataField.PartnerEmail,
                DataField.PartnerTaxNumber,
                DataField.PartnerBulstat,
                DataField.PartnerBankName,
                DataField.PartnerBankCode,
                DataField.PartnerBankAcct,
                DataField.PartnerBankVATName,
                DataField.PartnerBankVATCode,
                DataField.PartnerBankVATAcct,
                DataField.PartnerPriceGroup,
                DataField.PartnerDiscount,
                DataField.PartnerType,
                DataField.PartnerOrder,
                DataField.PartnerDeleted,
                DataField.PartnerCreatorId,
                DataField.PartnerGroupId,
                DataField.PartnerCardNumber,
                DataField.PartnerNote,
                DataField.PartnerNote2,
                DataField.PartnerTimeStamp,
                DataField.PartnerPaymentDays
            };
        }

        private string PartnerDefaultAliases ()
        {
            return GetAliasesString (PartnerDefaultFields ());
        }
    }
}
