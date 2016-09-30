//
// PaymentType.cs
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
using System.Collections.Generic;
using System.Linq;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Entities
{
    public class PaymentType : ICacheableEntity<PaymentType>
    {
        private long id = -1;
        private string name = string.Empty;
        private BasePaymentType baseType = BasePaymentType.Cash;

        #region Public properties

        [DbColumn (DataField.PaymentTypesId)]
        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Code
        {
            get { return name; }
            set { name = value; }
        }

        [DbColumn (DataField.PaymentTypesName, 255)]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [DbColumn (DataField.PaymentTypesMethod)]
        public BasePaymentType BaseType
        {
            get { return baseType; }
            set { baseType = value; }
        }

        private static readonly CacheEntityCollection<PaymentType> cache = new CacheEntityCollection<PaymentType> ();
        public static CacheEntityCollection<PaymentType> Cache
        {
            get { return cache; }
        }

        #endregion

        public PaymentType CommitChanges ()
        {
            BusinessDomain.DataAccessProvider.AddUpdatePaymentMethod (this);
            cache.Set (this);

            return this;
        }

        public static DeletePermission RequestDelete (long paymentTypeId)
        {
            return BusinessDomain.DataAccessProvider.CanDeletePaymentMethod (paymentTypeId);
        }

        public static void Delete (long paymentTypeId)
        {
            BusinessDomain.DataAccessProvider.DeletePaymentMethod (paymentTypeId);
            cache.Remove (paymentTypeId);
        }

        public static LazyListModel<PaymentType> GetAll ()
        {
            return BusinessDomain.DataAccessProvider.GetAllPaymentMethods<PaymentType> ();
        }

        public static PaymentType GetById (long paymentTypeId)
        {
            PaymentType ret = BusinessDomain.DataAccessProvider.GetPaymentMethodById<PaymentType> (paymentTypeId);
            cache.Set (ret);
            return ret;
        }

        public static PaymentType GetByName (string paymentTypeName)
        {
            PaymentType ret = BusinessDomain.DataAccessProvider.GetPaymentMethodByName<PaymentType> (paymentTypeName);
            cache.Set (ret);
            return ret;
        }

        public override string ToString ()
        {
            return string.Format ("PaymentType: Id: {0}, Name: {1}, Method: {2}", id, name, baseType);
        }

        public object Clone ()
        {
            return MemberwiseClone ();
        }

        public static KeyValuePair<int, string> [] GetAllBaseTypePairs ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) BasePaymentType.Cash, Translator.GetString ("In cash")),
                    new KeyValuePair<int, string> ((int) BasePaymentType.BankTransfer, Translator.GetString ("To bank account")),
                    new KeyValuePair<int, string> ((int) BasePaymentType.Card, Translator.GetString ("By card")),
                    new KeyValuePair<int, string> ((int) BasePaymentType.Coupon, Translator.GetString ("Other"))
                };
        }

        public static KeyValuePair<int, string> [] GetAllBaseTypePairsWithAdvance ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) BasePaymentType.Cash, Translator.GetString ("In cash")),
                    new KeyValuePair<int, string> ((int) BasePaymentType.BankTransfer, Translator.GetString ("To bank account")),
                    new KeyValuePair<int, string> ((int) BasePaymentType.Card, Translator.GetString ("By card")),
                    new KeyValuePair<int, string> ((int) BasePaymentType.Coupon, Translator.GetString ("Other")),
                    new KeyValuePair<int, string> ((int) BasePaymentType.Advance, Translator.GetString ("Advance Payment"))
                };
        }

        public static string GetBasePaymentTypeName (BasePaymentType type)
        {
            switch (type) {
                case BasePaymentType.Cash:
                    return Translator.GetString ("In cash");

                case BasePaymentType.BankTransfer:
                    return Translator.GetString ("To bank account");

                case BasePaymentType.Card:
                    return Translator.GetString ("By card");

                case BasePaymentType.Coupon:
                    return Translator.GetString ("Other");

                case BasePaymentType.Advance:
                    return Translator.GetString ("Advance Payment");

                default:
                    throw new ArgumentOutOfRangeException ("type");
            }
        }

        public static KeyValuePair<long, string> [] GetAllFilters ()
        {
            List<KeyValuePair<long, string>> filters = new List<KeyValuePair<long, string>>
                {
                    new KeyValuePair<long, string> (-1, Translator.GetString ("All"))
                };
            filters.AddRange (GetAll ().Select (type => new KeyValuePair<long, string> (type.Id, type.Name)));

            return filters.ToArray ();
        }

        #region Overrides of ICacheableEntity<PaymentType>

        public PaymentType GetEntityById (long entityId)
        {
            return GetById (entityId);
        }

        public PaymentType GetEntityByCode (string entityCode)
        {
            return GetByName (entityCode);
        }

        public PaymentType GetEntityByName (string entityName)
        {
            return GetByName (entityName);
        }

        public IEnumerable<PaymentType> GetAllEntities ()
        {
            return GetAll ();
        }

        #endregion
    }
}
