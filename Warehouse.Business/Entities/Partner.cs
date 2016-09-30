//
// Partner.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/11/2006
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
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Entities
{
    public enum PartnerType
    {
        Universal = 0,
        Supplier = 1,
        Client = 2
    }

    public class Partner : ICacheableEntity<Partner>, IPersistableEntity<Partner>, IStrongEntity, IHirarchicalEntity
    {
        #region Private fields

        private long id = -1;
        private string name = string.Empty;
        private string name2 = string.Empty;
        private string liablePerson = string.Empty;
        private string liablePerson2 = string.Empty;
        private string city = string.Empty;
        private string city2 = string.Empty;
        private string address = string.Empty;
        private string address2 = string.Empty;
        private string telephone = string.Empty;
        private string telephone2 = string.Empty;
        private string bulstat = string.Empty;
        private string bankAccount = string.Empty;
        private string cardNumber = string.Empty;

        private string code = string.Empty;
        private string fax = string.Empty;
        private string email = string.Empty;
        private string bankName = string.Empty;
        private string bankCode = string.Empty;
        private string bankVATAccount = string.Empty;
        private PriceGroup priceGroup = PriceGroup.RegularPrice;
        private PartnerType businessType;
        private long creatorId = 1;
        private DateTime timeStamp;
        private string note = " ";
        private string note2 = " ";

        private string taxNumber = string.Empty;
        private string bankVATName = string.Empty;
        private string bankVATCode = string.Empty;
        private int order = -1;
        private int deletedDb;
        private long groupId = 1;
        private string groupName;
        private int paymentDays;

        #endregion

        public const int DefaultId = 1;

        #region Public properties

        [DbColumn (DataField.PartnerId)]
        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        [ExchangeProperty ("Name", true)]
        [DbColumn (DataField.PartnerName, 255)]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [ExchangeProperty ("Name 2")]
        [DbColumn (DataField.PartnerName2, 255)]
        public string Name2
        {
            get { return string.IsNullOrWhiteSpace (name2) ? name : name2; }
            set { name2 = value; }
        }

        [ExchangeProperty ("Liable Person")]
        [DbColumn (DataField.PartnerLiablePerson, 255)]
        public string LiablePerson
        {
            get { return liablePerson; }
            set { liablePerson = value; }
        }

        [ExchangeProperty ("Liable Person 2")]
        [DbColumn (DataField.PartnerLiablePerson2, 255)]
        public string LiablePerson2
        {
            get { return string.IsNullOrWhiteSpace (liablePerson2) ? liablePerson : liablePerson2; }
            set { liablePerson2 = value; }
        }

        [ExchangeProperty ("City")]
        [DbColumn (DataField.PartnerCity, 255)]
        public string City
        {
            get { return city; }
            set { city = value; }
        }

        [ExchangeProperty ("City 2")]
        [DbColumn (DataField.PartnerCity2, 255)]
        public string City2
        {
            get { return string.IsNullOrWhiteSpace (city2) ? city : city2; }
            set { city2 = value; }
        }

        [ExchangeProperty ("Address")]
        [DbColumn (DataField.PartnerAddress, 255)]
        public string Address
        {
            get { return address; }
            set { address = value; }
        }

        [ExchangeProperty ("Address 2")]
        [DbColumn (DataField.PartnerAddress2, 255)]
        public string Address2
        {
            get { return string.IsNullOrWhiteSpace (address2) ? address : address2; }
            set { address2 = value; }
        }

        [ExchangeProperty ("Telephone")]
        [DbColumn (DataField.PartnerPhone, 255)]
        public string Telephone
        {
            get { return telephone; }
            set { telephone = value; }
        }

        [ExchangeProperty ("Telephone 2")]
        [DbColumn (DataField.PartnerPhone2, 255)]
        public string Telephone2
        {
            get { return string.IsNullOrWhiteSpace (telephone2) ? telephone : telephone2; }
            set { telephone2 = value; }
        }

        [ExchangeProperty ("UIC")]
        [DbColumn (DataField.PartnerBulstat, 255)]
        public string Bulstat
        {
            get { return bulstat; }
            set { bulstat = value; }
        }

        [ExchangeProperty ("Code")]
        [DbColumn (DataField.PartnerCode, 255)]
        public string Code
        {
            get { return code; }
            set { code = value; }
        }

        [ExchangeProperty ("Fax")]
        [DbColumn (DataField.PartnerFax, 255)]
        public string Fax
        {
            get { return fax; }
            set { fax = value; }
        }

        [ExchangeProperty ("Email")]
        [DbColumn (DataField.PartnerEmail, 255)]
        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        [ExchangeProperty ("Bank Name")]
        [DbColumn (DataField.PartnerBankName, 255)]
        public string BankName
        {
            get { return bankName; }
            set { bankName = value; }
        }

        [ExchangeProperty ("Bank Code")]
        [DbColumn (DataField.PartnerBankCode, 255)]
        public string BankCode
        {
            get { return bankCode; }
            set { bankCode = value; }
        }

        [ExchangeProperty ("Bank Account")]
        [DbColumn (DataField.PartnerBankAcct, 255)]
        public string BankAccount
        {
            get { return bankAccount; }
            set { bankAccount = value; }
        }

        [ExchangeProperty ("Bank VAT Name")]
        [DbColumn (DataField.PartnerBankVATName, 255)]
        public string BankVATName
        {
            get { return bankVATName; }
            set { bankVATName = value; }
        }

        [ExchangeProperty ("Bank VAT Code")]
        [DbColumn (DataField.PartnerBankVATCode, 255)]
        public string BankVATCode
        {
            get { return bankVATCode; }
            set { bankVATCode = value; }
        }

        [ExchangeProperty ("Bank VAT Account")]
        [DbColumn (DataField.PartnerBankVATAcct, 255)]
        public string BankVATAccount
        {
            get { return bankVATAccount; }
            set { bankVATAccount = value; }
        }

        [ExchangeProperty ("Price Group")]
        [DbColumn (DataField.PartnerPriceGroup)]
        public PriceGroup PriceGroup
        {
            get { return priceGroup; }
            set { priceGroup = value; }
        }

        [ExchangeProperty ("Type")]
        [DbColumn (DataField.PartnerType)]
        public PartnerType BusinessType
        {
            get { return businessType; }
            set { businessType = value; }
        }

        [DbColumn (DataField.PartnerCreatorId)]
        public long CreatorId
        {
            get { return creatorId; }
            set { creatorId = value; }
        }

        [DbColumn (DataField.PartnerTimeStamp)]
        public DateTime TimeStamp
        {
            get { return timeStamp; }
            set { timeStamp = value; }
        }

        [ExchangeProperty ("Tax Number")]
        [DbColumn (DataField.PartnerTaxNumber, 255)]
        public string TaxNumber
        {
            get { return taxNumber; }
            set { taxNumber = value; }
        }

        /// <summary>
        /// Gets or sets the number with which a partner is entered through a card.
        /// </summary>
        /// <value>The number with which a partner is entered through a card.</value>
        [ExchangeProperty ("Card Number")]
        [DbColumn (DataField.PartnerCardNumber, 255)]
        public string CardNumber
        {
            get { return cardNumber; }
            set { cardNumber = value; }
        }

        [DbColumn (DataField.PartnerOrder)]
        public int Order
        {
            get { return order; }
            set { order = value; }
        }

        public bool Deleted
        {
            get { return deletedDb == -1; }
            set { deletedDb = value ? -1 : 0; }
        }

        [DbColumn (DataField.PartnerDeleted)]
        public int DeletedDb
        {
            get { return deletedDb; }
            set { deletedDb = value; }
        }

        [DbColumn (DataField.PartnerGroupId)]
        public long GroupId
        {
            get { return groupId; }
            set { groupId = value; }
        }

        [ExchangeProperty ("Group Name", false, DataField.PartnersGroupsName)]
        public string GroupName
        {
            get
            {
                if (!string.IsNullOrEmpty (groupName))
                    return groupName;

                return PartnersGroup.GetPath (Math.Abs (groupId), PartnersGroup.Cache);
            }
            set { groupName = value; }
        }

        [DbColumn (DataField.PartnerNote, 255)]
        public string Note
        {
            get { return note; }
            set { note = value; }
        }

        [DbColumn (DataField.PartnerNote2, 255)]
        public string Note2
        {
            get { return string.IsNullOrWhiteSpace (note2) ? note : note2; }
            set { note2 = value; }
        }

        [DbColumn (DataField.PartnerPaymentDays)]
        public int PaymentDays
        {
            get { return paymentDays; }
            set { paymentDays = value; }
        }

        #endregion

        private static readonly CacheEntityCollection<Partner> cache = new CacheEntityCollection<Partner> ();
        public static CacheEntityCollection<Partner> Cache
        {
            get { return cache; }
        }

        public Partner CommitChanges ()
        {
            if (BusinessDomain.AppConfiguration.AutoGeneratePartnerCodes && string.IsNullOrWhiteSpace (code))
                AutoGenerateCode ();

            if (!string.IsNullOrEmpty (groupName) && groupId <= 1) {
                PartnersGroup g = PartnersGroup.EnsureByPath (groupName, PartnersGroup.Cache);
                groupId = g.Id;
            }

            BusinessDomain.DataAccessProvider.AddUpdatePartner (this);
            cache.Set (this);

            return this;
        }

        public void AutoGenerateCode ()
        {
            string pattern = BusinessDomain.AppConfiguration.PartnerCodePattern;
            ulong lastCode = BusinessDomain.DataAccessProvider.GetMaxCodeValue (DbTable.Partners, pattern);
            code = CodeGenerator.GenerateCode (pattern, lastCode + 1);
        }

        public static DeletePermission RequestDelete (long partnerId)
        {
            return BusinessDomain.DataAccessProvider.CanDeletePartner (partnerId);
        }

        public static void Delete (long partnerId)
        {
            BusinessDomain.DataAccessProvider.DeletePartner (partnerId);
            cache.Remove (partnerId);
        }

        public static LazyListModel<Partner> GetAll (long? groupId = null)
        {
            return BusinessDomain.DataAccessProvider.GetAllPartners<Partner> (groupId);
        }

        public static Partner GetById (long partnerId)
        {
            Partner ret = BusinessDomain.DataAccessProvider.GetPartnerById<Partner> (partnerId);
            cache.Set (ret);
            return ret;
        }

        public static Partner GetByName (string partnerName)
        {
            Partner ret = BusinessDomain.DataAccessProvider.GetPartnerByName<Partner> (partnerName);
            cache.Set (ret);
            return ret;
        }

        public static Partner GetByCode (string partnerCode)
        {
            Partner ret = BusinessDomain.DataAccessProvider.GetPartnerByCode<Partner> (partnerCode);
            cache.Set (ret);
            return ret;
        }

        public static Partner GetByBulstat (string partnerBulstat)
        {
            Partner ret = BusinessDomain.DataAccessProvider.GetPartnerByBulstat<Partner> (partnerBulstat);
            cache.Set (ret);
            return ret;
        }

        /// <summary>
        /// Gets the partner with the specified number on a magnetic card.
        /// </summary>
        /// <param name="partnerCardNo">The number of the partner's magnetic card.</param>
        /// <returns>The partner with the specified number on a magnetic card.</returns>
        public static Partner GetByCard (string partnerCardNo)
        {
            Partner ret = BusinessDomain.DataAccessProvider.GetPartnerByCard<Partner> (partnerCardNo);
            cache.Set (ret);
            return ret;
        }

        public static Partner GetByEmail (string partnerEmail)
        {
            Partner ret = BusinessDomain.DataAccessProvider.GetPartnerByEmail<Partner> (partnerEmail);
            cache.Set (ret);
            return ret;
        }

        public static Partner GetByPhone (string partnerPhone)
        {
            Partner ret = BusinessDomain.DataAccessProvider.GetPartnerByPhone<Partner> (partnerPhone);
            cache.Set (ret);
            return ret;
        }

        public static Partner GetByLiablePerson (string partnerLiablePerson)
        {
            Partner ret = BusinessDomain.DataAccessProvider.GetPartnerByLiablePerson<Partner> (partnerLiablePerson);
            cache.Set (ret);
            return ret;
        }

        public static double GetTurnover (long partnerId)
        {
            return BusinessDomain.DataAccessProvider.GetPartnerTurnover (partnerId);
        }

        public static double GetDebt (long partnerId)
        {
            return BusinessDomain.DataAccessProvider.GetPartnerDebt (partnerId);
        }

        public static bool TryGetLocked (out Partner partner)
        {
            if (BusinessDomain.LoggedUser.LockedPartnerId > 0) {
                partner = GetById (BusinessDomain.LoggedUser.LockedPartnerId);
                return true;
            }

            if (BusinessDomain.LoggedUser.DefaultPartnerId > 0) {
                partner = GetById (BusinessDomain.LoggedUser.DefaultPartnerId);
                return false;
            }

            LazyListModel<Partner> all = GetAll ();
            partner = all.Count == 1 ? all [0] : null;

            return false;
        }

        public static long GetDefaultId ()
        {
            long defaultPartnerId = BusinessDomain.LoggedUser.LockedPartnerId;
            if (defaultPartnerId > 0)
                return defaultPartnerId;

            defaultPartnerId = BusinessDomain.LoggedUser.DefaultPartnerId;
            if (defaultPartnerId > 0)
                return defaultPartnerId;

            return DefaultId;
        }

        public static Payment [] GetDuePayments (long partnerId)
        {
            return BusinessDomain.DataAccessProvider.GetPartnerDuePayments<Payment> (partnerId);
        }

        public static double GetUnpaidAmountWithExpiredDueDate (long partnerId, DateTime operationDate)
        {
            return BusinessDomain.DataAccessProvider.GetPartnerUnpaidAmountWithExpiredDueDate (partnerId, operationDate);
        }

        #region Implementation of IStrongEntity

        public bool Validate (ValidateCallback callback, StateHolder state)
        {
            if (callback == null)
                throw new ArgumentNullException ("callback");

            if (string.IsNullOrEmpty (name)) {
                if (!callback (Translator.GetString ("Partner name cannot be empty!"), ErrorSeverity.Error, 0, state))
                    return false;
            }

            Partner p = GetByName (name);
            if (p != null && p.Id != id) {
                if (!callback (string.Format (Translator.GetString ("Partner with the name \"{0}\" already exists! Do you want to save the partner anyway?"), name),
                    ErrorSeverity.Warning, 1, state))
                    return false;
            }

            if (!string.IsNullOrEmpty (code)) {
                p = GetByCode (code);
                if (p != null && p.Id != id) {
                    if (!callback (string.Format (Translator.GetString ("Partner with the code \"{0}\" already exists! Do you want to save the partner anyway?"), code),
                        ErrorSeverity.Warning, 2, state))
                        return false;
                }
            }

            return true;
        }

        #endregion

        #region Overrides of ICacheableEntityBase<Partner>

        public Partner GetEntityById (long entityId)
        {
            return GetById (entityId);
        }

        public Partner GetEntityByCode (string entityCode)
        {
            return GetByCode (entityCode);
        }

        public Partner GetEntityByName (string entityName)
        {
            return GetByName (entityName);
        }

        public IEnumerable<Partner> GetAllEntities ()
        {
            return GetAll ();
        }

        public object Clone ()
        {
            return MemberwiseClone ();
        }

        #endregion

        public static KeyValuePair<int, string> [] GetAllTypes ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) PartnerType.Universal, Translator.GetString ("Universal")),
                    new KeyValuePair<int, string> ((int) PartnerType.Supplier, Translator.GetString ("Supplier")),
                    new KeyValuePair<int, string> ((int) PartnerType.Client, Translator.GetString ("Customer"))
                };
        }

        public static KeyValuePair<int, string> [] GetAllTypeFilters ()
        {
            List<KeyValuePair<int, string>> filters = new List<KeyValuePair<int, string>>
                {
                    new KeyValuePair<int, string> (-1, Translator.GetString ("All"))
                };

            filters.AddRange (GetAllTypes ());

            return filters.ToArray ();
        }
    }
}