//
// User.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   06/30/2006
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
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Entities
{
    public enum UserAccessLevel
    {
        Operator,
        Manager,
        Administrator,
        Owner
    }

    public class User : ICacheableEntity<User>, IPersistableEntity<User>, IStrongEntity, IHirarchicalEntity, INotifyPropertyChanged
    {
        private const string LOCKED_LOCATION_KEY = "configkey47";
        private const string LOCKED_PARTNER_KEY = "configkey62";
        private const string DEFAULT_PARTNER_KEY = "DefaultPartnerId";
        private const string DEFAULT_COMPANY_KEY = "configkey49";
        private const string ALLOW_ZERO_PRICES_KEY = "configkey60";
        private const string HIDE_ITEMS_AVAILABILITY_KEY = "configkey10";
        private const string HIDE_ITEMS_PURCHASE_PRICE_KEY = "configkey8";

        public event PropertyChangedEventHandler PropertyChanged;

        #region Private fields

        private long id = -1;
        private string name = string.Empty;
        private string name2 = string.Empty;
        private string code = string.Empty;
        private string passswordEncryped = string.Empty;
        private UserAccessLevel userLevel = UserAccessLevel.Operator;
        private int order = -1;
        private int deletedDb;
        private long groupId = 1;
        private string groupName;
        private string cardNo = string.Empty;

        #endregion

        public const long DefaultId = 1;
        public const long AllId = -1;

        #region Public properties

        [DbColumn (DataField.UserId)]
        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        [ExchangeProperty ("Name", true)]
        [DbColumn (DataField.UserName, 255)]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [ExchangeProperty ("Name 2")]
        [DbColumn (DataField.UserName2, 255)]
        public string Name2
        {
            get { return string.IsNullOrWhiteSpace (name2) ? name : name2; }
            set { name2 = value; }
        }

        [ExchangeProperty ("Code")]
        [DbColumn (DataField.UserCode, 255)]
        public string Code
        {
            get { return code; }
            set { code = value; }
        }

        // For write only purposes
        public string Password
        {
            set { passswordEncryped = Encryption.EncryptUserPassword (value); }
        }

        // For read only purposes
        //TODO add UI checking for password len
        [ExchangeProperty ("Password")]
        [DbColumn (DataField.UserPassword, 50)]
        public string PasswordEncrypted
        {
            get
            {
                if (string.IsNullOrEmpty (passswordEncryped))
                    passswordEncryped = Encryption.EncryptUserPassword (string.Empty);

                return passswordEncryped;
            }
            set { passswordEncryped = value; }
        }

        [ExchangeProperty ("Access Level")]
        [DbColumn (DataField.UserLevel)]
        public UserAccessLevel UserLevel
        {
            get { return userLevel; }
            set
            {
                if (userLevel == value)
                    return;

                userLevel = value;
                OnPropertyChanged ("UserLevel");
            }
        }

        [DbColumn (DataField.UserOrder)]
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

        [DbColumn (DataField.UserDeleted)]
        public int DeletedDb
        {
            get { return deletedDb; }
            set { deletedDb = value; }
        }

        [DbColumn (DataField.UserGroupId)]
        public long GroupId
        {
            get { return groupId; }
            set { groupId = value; }
        }

        [ExchangeProperty ("Group Name", false, DataField.LocationsGroupsName)]
        public string GroupName
        {
            get
            {
                if (!string.IsNullOrEmpty (groupName))
                    return groupName;

                return GroupBase<UsersGroup>.GetPath (Math.Abs (groupId), UsersGroup.Cache);
            }
            set { groupName = value; }
        }

        [ExchangeProperty ("Card No")]
        [DbColumn (DataField.UserCardNo, 255)]
        public string CardNo
        {
            get { return cardNo; }
            set { cardNo = value; }
        }

        private long? lockedLocationId;
        private bool lockedLocationIdDirty;
        public long LockedLocationId
        {
            get
            {
                if (lockedLocationId == null) {
                    if (id < 0)
                        return -1;

                    lockedLocationId = ConfigEntry.GetIntegerValue (LOCKED_LOCATION_KEY, id);
                }

                return lockedLocationId.Value;
            }
            set
            {
                if (lockedLocationId == value)
                    return;

                lockedLocationId = value;
                lockedLocationIdDirty = true;
            }
        }

        private long? lockedPartnerId;
        private bool lockedPartnerIdDirty;
        public long LockedPartnerId
        {
            get
            {
                if (lockedPartnerId == null) {
                    if (id < 0)
                        return -1;

                    lockedPartnerId = ConfigEntry.GetIntegerValue (LOCKED_PARTNER_KEY, id);
                }

                return lockedPartnerId.Value;
            }
            set
            {
                if (lockedPartnerId == value)
                    return;

                lockedPartnerId = value;
                lockedPartnerIdDirty = true;
            }
        }

        private long? defaultPartnerId;
        private bool defaultPartnerIdDirty;
        public long DefaultPartnerId
        {
            get
            {
                if (defaultPartnerId == null) {
                    if (id < 0)
                        return -1;

                    defaultPartnerId = ConfigEntry.GetIntegerValue (DEFAULT_PARTNER_KEY, id);
                }

                return defaultPartnerId.Value;
            }
            set
            {
                if (defaultPartnerId == value)
                    return;

                defaultPartnerId = value;
                defaultPartnerIdDirty = true;
            }
        }

        private long? defaultCompanyId;
        private bool defaultCompanyIdDirty;
        public long DefaultCompanyId
        {
            get
            {
                if (defaultCompanyId == null) {
                    if (id < 0)
                        return -1;

                    defaultCompanyId = ConfigEntry.GetIntegerValue (DEFAULT_COMPANY_KEY, id);
                }

                return defaultCompanyId.Value;
            }
            set
            {
                if (defaultCompanyId == value)
                    return;

                defaultCompanyId = value;
                defaultCompanyIdDirty = true;
            }
        }

        private bool? hideItemsPurchasePrice;
        private bool hideItemsPurchasePriceDirty;
        public bool HideItemsPurchasePrice
        {
            get
            {
                if (hideItemsPurchasePrice == null) {
                    if (id < 0)
                        return false;

                    hideItemsPurchasePrice = ConfigEntry.GetBoolValue (HIDE_ITEMS_PURCHASE_PRICE_KEY, id);
                }

                return hideItemsPurchasePrice.Value;
            }
            set
            {
                if (hideItemsPurchasePrice == value)
                    return;

                hideItemsPurchasePrice = value;
                hideItemsPurchasePriceDirty = true;
            }
        }

        private bool? hideItemsAvailability;
        private bool hideItemsAvailabilityDirty;
        public bool HideItemsAvailability
        {
            get
            {
                if (hideItemsAvailability == null) {
                    if (id < 0)
                        return false;

                    hideItemsAvailability = ConfigEntry.GetBoolValue (HIDE_ITEMS_AVAILABILITY_KEY, id);
                }

                return hideItemsAvailability.Value;
            }
            set
            {
                if (hideItemsAvailability == value)
                    return;

                hideItemsAvailability = value;
                hideItemsAvailabilityDirty = true;
            }
        }

        private bool? allowZeroPrices;
        private bool allowZeroPricesDirty;
        public bool AllowZeroPrices
        {
            get
            {
                if (allowZeroPrices == null) {
                    if (id < 0)
                        return true;

                    allowZeroPrices = ConfigEntry.GetBoolValue (ALLOW_ZERO_PRICES_KEY, id, true);
                }

                return allowZeroPrices.Value;
            }
            set
            {
                if (allowZeroPrices == value)
                    return;

                allowZeroPrices = value;
                allowZeroPricesDirty = true;
            }
        }

        public bool IsSaved
        {
            get { return id > 0; }
        }

        #endregion

        private static readonly CacheEntityCollection<User> cache = new CacheEntityCollection<User> ();
        public static CacheEntityCollection<User> Cache
        {
            get { return cache; }
        }

        public User CommitChanges ()
        {
            if (BusinessDomain.AppConfiguration.AutoGenerateUserCodes && string.IsNullOrWhiteSpace (code))
                AutoGenerateCode ();

            if (!string.IsNullOrEmpty (groupName) && groupId <= 1) {
                UsersGroup g = GroupBase<UsersGroup>.EnsureByPath (groupName, UsersGroup.Cache);
                groupId = g.Id;
            }

            bool isNew = id < 0;
            BusinessDomain.DataAccessProvider.AddUpdateUser (this);
            cache.Set (this);

            if (isNew) {
                BusinessDomain.RestrictionTree.ResetLevelRestrictions (id, userLevel);
                BusinessDomain.RestrictionTree.SaveRestrictions ();
            }

            if (lockedLocationIdDirty) {
                ConfigEntry.SaveValue (LOCKED_LOCATION_KEY, LockedLocationId, id);
                lockedLocationIdDirty = false;
            }

            if (lockedPartnerIdDirty) {
                ConfigEntry.SaveValue (LOCKED_PARTNER_KEY, LockedPartnerId, id);
                lockedPartnerIdDirty = false;
            }

            if (defaultPartnerIdDirty) {
                ConfigEntry.SaveValue (DEFAULT_PARTNER_KEY, DefaultPartnerId, id);
                defaultPartnerIdDirty = false;
            }

            if (defaultCompanyIdDirty) {
                ConfigEntry.SaveValue (DEFAULT_COMPANY_KEY, DefaultCompanyId, id);
                BusinessDomain.CurrentCompany = CompanyRecord.GetDefault ();
                defaultCompanyIdDirty = false;
            }

            if (hideItemsPurchasePriceDirty) {
                ConfigEntry.SaveValue (HIDE_ITEMS_PURCHASE_PRICE_KEY, HideItemsPurchasePrice, id);
                hideItemsPurchasePriceDirty = false;
            }

            if (hideItemsAvailabilityDirty) {
                ConfigEntry.SaveValue (HIDE_ITEMS_AVAILABILITY_KEY, HideItemsAvailability, id);
                hideItemsAvailabilityDirty = false;
            }

            if (allowZeroPricesDirty) {
                ConfigEntry.SaveValue (ALLOW_ZERO_PRICES_KEY, AllowZeroPrices, id);
                allowZeroPricesDirty = false;
            }

            if (BusinessDomain.LoggedUser.Id == id)
                BusinessDomain.LoggedUser = this;

            return this;
        }

        public void AutoGenerateCode ()
        {
            string pattern = BusinessDomain.AppConfiguration.UserCodePattern;
            ulong lastCode = BusinessDomain.DataAccessProvider.GetMaxCodeValue (DbTable.Users, pattern);
            code = CodeGenerator.GenerateCode (pattern, lastCode + 1);
        }

        public bool CheckPassword (string password)
        {
            return (PasswordEncrypted == Encryption.EncryptUserPassword (password));
        }

        public bool CanEdit ()
        {
            User loggedUser = BusinessDomain.LoggedUser;
            return loggedUser.UserLevel == UserAccessLevel.Owner ||
                UserLevel < loggedUser.UserLevel ||
                id == loggedUser.Id;
        }

        public static DeletePermission RequestDelete (long userId)
        {
            return BusinessDomain.DataAccessProvider.CanDeleteUser (userId);
        }

        public static void Delete (long userId, bool deleteRestrictions = false)
        {
            BusinessDomain.DataAccessProvider.DeleteUser (userId);
            cache.Remove (userId);

            if (!deleteRestrictions)
                return;

            ConfigEntry.Delete (LOCKED_LOCATION_KEY, userId);
            ConfigEntry.Delete (LOCKED_PARTNER_KEY, userId);
            ConfigEntry.Delete (DEFAULT_COMPANY_KEY, userId);
            ConfigEntry.Delete (HIDE_ITEMS_PURCHASE_PRICE_KEY, userId);
            ConfigEntry.Delete (HIDE_ITEMS_AVAILABILITY_KEY, userId);
            ConfigEntry.Delete (ALLOW_ZERO_PRICES_KEY, userId);
        }

        public static LazyListModel<User> GetAll ()
        {
            return BusinessDomain.DataAccessProvider.GetAllUsers<User> ();
        }

        public static LazyListModel<User> GetAll (UserAccessLevel maxUserLevel)
        {
            return BusinessDomain.DataAccessProvider.GetAllUsers<User> ((int) maxUserLevel, BusinessDomain.LoggedUser.Id);
        }

        public static LazyListModel<User> GetAll (long? groupId)
        {
            return BusinessDomain.DataAccessProvider.GetAllUsers<User> (groupId);
        }

        public static User GetById (long userId)
        {
            User ret = BusinessDomain.DataAccessProvider.GetUserById<User> (userId);
            cache.Set (ret);
            return ret;
        }

        public static User GetByName (string userName)
        {
            if (userName == null || userName.Trim ().Length == 0)
                return null;

            User ret = BusinessDomain.DataAccessProvider.GetUserByName<User> (userName);
            cache.Set (ret);
            return ret;
        }

        public static User GetByCode (string userCode)
        {
            User ret = BusinessDomain.DataAccessProvider.GetUserByCode<User> (userCode);
            cache.Set (ret);
            return ret;
        }

        public static User GetByCard (string userCardNo)
        {
            User ret = BusinessDomain.DataAccessProvider.GetUserByCard<User> (userCardNo);
            cache.Set (ret);
            return ret;
        }

        public static bool CheckOwnerExists ()
        {
            return BusinessDomain.DataAccessProvider.CheckUserOwnerExists ();
        }

        public static KeyValuePair<int, string> [] GetAllAccessLevels ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) UserAccessLevel.Operator, Translator.GetString ("Operator")),
                    new KeyValuePair<int, string> ((int) UserAccessLevel.Manager, Translator.GetString ("Manager")),
                    new KeyValuePair<int, string> ((int) UserAccessLevel.Administrator, Translator.GetString ("Administrator")),
                    new KeyValuePair<int, string> ((int) UserAccessLevel.Owner, Translator.GetString ("Owner"))
                };
        }

        public static KeyValuePair<int, string> [] GetAllAccessLevelFilters ()
        {
            List<KeyValuePair<int, string>> filters = new List<KeyValuePair<int, string>>
                {
                    new KeyValuePair<int, string> (-1, Translator.GetString ("All"))
                };

            filters.AddRange (GetAllAccessLevels ());

            return filters.ToArray ();
        }

        #region Implementation of IStrongEntity

        public bool Validate (ValidateCallback callback, StateHolder state)
        {
            if (callback == null)
                throw new ArgumentNullException ("callback");

            if (name.Length == 0) {
                if (!callback (Translator.GetString ("User name cannot be empty!"), ErrorSeverity.Error, 0, state))
                    return false;
            }

            User u = GetByName (name);
            if (u != null && u.Id != id) {
                if (!callback (string.Format (Translator.GetString ("User with the name \"{0}\" already exists! Do you want to save the user anyway?"), name),
                    ErrorSeverity.Warning, 1, state))
                    return false;
            }

            if (!string.IsNullOrEmpty (code)) {
                u = GetByCode (code);
                if (u != null && u.Id != id) {
                    if (!callback (string.Format (Translator.GetString ("User with the code \"{0}\" already exists. Do you want to save the user anyway?"), code),
                        ErrorSeverity.Warning, 2, state))
                        return false;
                }
            }

            if (!string.IsNullOrEmpty (cardNo)) {
                u = GetByCard (cardNo);
                if (u != null && u.Id != id) {
                    if (!callback (string.Format (Translator.GetString ("User with the card \"{0}\" already exists. Do you want to save the user anyway?"), cardNo),
                        ErrorSeverity.Warning, 3, state))
                        return false;
                }
            }

            return true;
        }

        #endregion

        private void OnPropertyChanged (string property)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler (this, new PropertyChangedEventArgs (property));
        }

        #region Overrides of ICacheableEntityBase<User>

        public User GetEntityById (long entityId)
        {
            return GetById (entityId);
        }

        public User GetEntityByCode (string entityCode)
        {
            return GetByCode (entityCode);
        }

        public User GetEntityByName (string entityName)
        {
            return GetByName (entityName);
        }

        public IEnumerable<User> GetAllEntities ()
        {
            return GetAll ();
        }

        public object Clone ()
        {
            return MemberwiseClone ();
        }

        #endregion

        public void LoadPreferences ()
        {
            long i = LockedLocationId;
            i = LockedPartnerId;
            i = DefaultPartnerId;
            i = DefaultCompanyId;
            bool b = HideItemsPurchasePrice;
            b = HideItemsAvailability;
            b = AllowZeroPrices;
        }

        public void LogSuccessfulLogin ()
        {
#if DEBUG
            if (id > 0)
                return;
#endif
            string hostName = null;
            IPAddress hostAddress = null;
            try {
                hostName = Dns.GetHostName ();
                hostAddress = Dns.GetHostAddresses (hostName).FirstOrDefault (a => a.AddressFamily == AddressFamily.InterNetwork);
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
            }

            ApplicationLogEntry.AddNew (string.Format (Translator.GetString ("Login successful. Workstation: \"{0}\" ({1}). Product version: {2}"),
                hostName ?? "Unknown",
                hostAddress != null ? hostAddress.ToString () : "Unknown",
                BusinessDomain.ApplicationVersionString), id);
        }

        public void LogFailedLogin ()
        {
#if DEBUG
            if (id > 0)
                return;
#endif
            string hostName = null;
            IPAddress hostAddress = null;
            try {
                hostName = Dns.GetHostName ();
                hostAddress = Dns.GetHostAddresses (hostName).FirstOrDefault (a => a.AddressFamily == AddressFamily.InterNetwork);
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
            }

            ApplicationLogEntry.AddNew (string.Format (Translator.GetString ("Login failed! Workstation: \"{0}\" ({1}). Product version: {2}"),
                hostName ?? "Unknown",
                hostAddress != null ? hostAddress.ToString () : "Unknown",
                BusinessDomain.ApplicationVersionString), id);
        }

        public void LogSuccessfulLogout ()
        {
#if DEBUG
            if (id > 0)
                return;
#endif
            ApplicationLogEntry.AddNew (Translator.GetString ("Logout successful."), id);
        }
    }
}