//
// DataProvider.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   03/22/2006
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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Warehouse.Data.Model;
using Warehouse.Data.SQL;

namespace Warehouse.Data
{
    public abstract class DataProvider : IDisposable
    {
        public const int MaxDeletedRows = 20000;
        protected const int DefaultLotId = 1;

        #region Fields

        protected string server = string.Empty;
        protected string slaveServer = string.Empty;
        protected string user = string.Empty;
        protected string password = string.Empty;
        protected string database = string.Empty;
        protected int connectTimeout = 5;
        protected int commandTimeout = 10000;
        protected string connString;
        protected string slaveConnString;

        private static Type @enum = typeof (Enum);

        protected string logFile = string.Empty;
        protected ITranslationProvider translator;
        protected readonly Dictionary<int, ServerContext> serverContexts = new Dictionary<int, ServerContext> ();
        protected readonly List<UpgradeEntry> upgradeTable = new List<UpgradeEntry> ();
        protected FieldTranslationCollection fieldsTable;
        protected Mutex viewProfileFileLock = new Mutex (false);

        #endregion

        #region Public Properties

        public virtual bool UsesServer { get { return false; } }

        public string Server
        {
            get { return server; }
            set
            {
                if (server == value)
                    return;

                server = value;
                InvalidateConnectionStrings ();
            }
        }

        public virtual bool UsesSlaveServer { get { return false; } }

        public string SlaveServer
        {
            get { return slaveServer; }
            set
            {
                if (slaveServer == value)
                    return;

                slaveServer = value;
                InvalidateConnectionStrings ();
            }
        }

        public virtual bool UsesUser { get { return false; } }

        public string User
        {
            get { return user; }
            set
            {
                if (user == value)
                    return;

                user = value;
                InvalidateConnectionStrings ();
            }
        }

        public virtual bool UsesPassword { get { return false; } }

        public string Password
        {
            get { return password; }
            set
            {
                if (password == value)
                    return;

                password = value;
                InvalidateConnectionStrings ();
            }
        }

        public virtual bool UsesDatabase { get { return false; } }

        public string Database
        {
            get { return database; }
            set
            {
                if (database == value)
                    return;

                database = value;
                InvalidateConnectionStrings ();
            }
        }

        public int ConnectTimeout
        {
            get { return connectTimeout; }
            set
            {
                if (connectTimeout == value)
                    return;

                connectTimeout = value;
                InvalidateConnectionStrings ();
            }
        }

        public int CommandTimeout
        {
            get { return commandTimeout; }
            set
            {
                if (commandTimeout == value)
                    return;

                commandTimeout = value;
                InvalidateConnectionStrings ();
            }
        }

        public string ConnString
        {
            get { return connString ?? (connString = GenerateConnectionString (true)); }
        }

        public string SlaveConnString
        {
            get { return slaveConnString ?? (slaveConnString = GenerateConnectionString (false)); }
        }

        internal FieldTranslationCollection FieldTranslation
        {
            get { return fieldsTable; }
        }

        public string ProviderVersion
        {
            get { return "3.07"; }
        }

        public abstract int MaxInsertedRows { get; }

        public abstract string LimitDelete { get; }

        private int purchaseCurrencyPrecision;
        public int PurchaseCurrencyPrecision
        {
            get { return purchaseCurrencyPrecision; }
            set
            {
                if (purchaseCurrencyPrecision == value)
                    return;

                purchaseCurrencyPrecision = value;
                purchaseCurrencyPrecisionString = null;
            }
        }

        private string purchaseCurrencyPrecisionString;
        protected string PurchaseCurrencyPrecisionString
        {
            get
            {
                if (purchaseCurrencyPrecisionString == null) {
                    double power = Math.Pow (10, -purchaseCurrencyPrecision);
                    NumberFormatInfo nfi = new NumberFormatInfo { NumberGroupSeparator = "", NumberDecimalSeparator = "." };
                    purchaseCurrencyPrecisionString = power.ToString (nfi);
                }
                return purchaseCurrencyPrecisionString;
            }
        }

        public event EventHandler<DataCommandEventArgs> CommandExecuted;

        public event EventHandler<LocationAddedArgs> LocationAdded;

        public string LogFile
        {
            get { return logFile; }
            set { logFile = value; }
        }

        public FieldTranslationCollection FieldsTable
        {
            get { return fieldsTable; }
        }

        internal ServerContext ThreadServerContext
        {
            get
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                ServerContext context;
                lock (serverContexts)
                    return serverContexts.TryGetValue (threadId, out context) ? context : null;
            }
            set
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                lock (serverContexts) {
                    if (serverContexts.ContainsKey (threadId)) {
                        if (value == null)
                            serverContexts.Remove (threadId);
                        else
                            serverContexts [threadId] = value;
                    } else {
                        if (value != null)
                            serverContexts.Add (threadId, value);
                    }
                }
            }
        }

        public bool DisableConnectionLostErrors { get; set; }

        public static readonly DataField [] OperationNonDBFields = new [] {
            DataField.ItemCode,
            DataField.ItemName,
            DataField.ItemName2,
            DataField.ItemGroupId,
            DataField.ItemMeasUnit,
            DataField.OperationDetailTotalIn,
            DataField.OperationDetailTotalOut,
            DataField.OperationDetailTotal,
            DataField.OperationDetailStorePriceIn,
            DataField.OperationDetailStorePriceOut,
            DataField.OperationsOperatorName,
            DataField.OperationsOperatorName2,
            DataField.OperationPartner,
            DataField.OperationPartner2,
            DataField.OperationLocation,
            DataField.OperationLocation2,
            DataField.OperationReferenceId,
            DataField.OperationsUserName,
            DataField.OperationsUserName2,
            DataField.OperationDateTime,
            DataField.OperationTotal,
            DataField.OperationVatSum,
            DataField.ConsignmentDetailRemainingQuantity,
            DataField.PartnerId,
            DataField.LotSerialNumber,
            DataField.LotExpirationDate,
            DataField.LotProductionDate,
            DataField.LotLocation,
            DataField.PurchaseTotal,
            DataField.PurchaseVATSum,
            DataField.DocumentNumber};

        private string operationDefaultAliases = string.Empty;
        public string OperationDefaultAliases
        {
            get
            {
                if (string.IsNullOrEmpty (operationDefaultAliases))
                    operationDefaultAliases = GetAliasesString (DataField.OperationType,
                        DataField.OperationNumber,
                        DataField.OperationLocationId,
                        DataField.OperationPartnerId,
                        DataField.OperationOperatorId,
                        DataField.OperationReferenceId,
                        DataField.OperationUserId,
                        DataField.OperationTimeStamp,
                        DataField.OperationDate,
                        DataField.OperationDetailId,
                        DataField.OperationDetailItemId,
                        DataField.OperationDetailQuantity,
                        DataField.OperationDetailPriceIn,
                        DataField.OperationDetailPriceOut,
                        DataField.OperationDetailDiscount,
                        DataField.OperationDetailCurrencyId,
                        DataField.OperationDetailCurrencyRate,
                        DataField.OperationDetailReferenceId,
                        DataField.OperationDetailLot,
                        DataField.OperationDetailNote,
                        DataField.OperationDetailVatIn,
                        DataField.OperationDetailVatOut,
                        DataField.OperationDetailSign,
                        DataField.OperationDetailLotId);
                return operationDefaultAliases;
            }
        }

        private string invoiceDefaultAliases;

        public string InvoiceDefaultAliases
        {
            get
            {
                return invoiceDefaultAliases ?? (invoiceDefaultAliases =
                    GetAliasesString (DataField.OperationType,
                        DataField.DocumentId,
                        DataField.DocumentOperationNumber,
                        DataField.DocumentNumber,
                        DataField.DocumentOperationType,
                        DataField.DocumentDate,
                        DataField.DocumentType,
                        DataField.DocumentReferenceDate,
                        DataField.DocumentReferenceNumber,
                        DataField.DocumentPaymentTypeId,
                        DataField.DocumentRecipient,
                        DataField.DocumentRecipientEGN,
                        DataField.DocumentProvider,
                        DataField.DocumentTaxDate,
                        DataField.DocumentReason,
                        DataField.DocumentDescription,
                        DataField.DocumentLocation));
            }
        }

        public static readonly DataField [] DocumentNonDBFields = new [] {
            DataField.DocumentId, 
            DataField.PartnerName, 
            DataField.PartnerBulstat, 
            DataField.PartnerTaxNumber, 
            DataField.PartnerCity, 
            DataField.PartnerAddress, 
            DataField.PartnerLiablePerson, 
            DataField.PartnerBankName, 
            DataField.PartnerBankAcct,
            DataField.PartnerCode,
            DataField.OperationDate,
            DataField.OperationTotal,
            DataField.OperationVatSum,
            DataField.PurchaseTotal,
            DataField.PurchaseVATSum,
            DataField.OperationDetailNote};

        #endregion

        protected DataProvider ()
        {
            CommandExecuted += DataProvider_CommandExecuted;
        }

        protected virtual void InitFieldNames ()
        {
            #region Application log table fields

            fieldsTable.Add (DataField.AppLogId, "applicationlog.ID");
            fieldsTable.Add (DataField.AppLogMessage, "applicationlog.Message");
            fieldsTable.Add (DataField.AppLogUserId, "applicationlog.UserID");
            fieldsTable.Add (DataField.AppLogTimeStamp, "applicationlog.UserRealTime");
            fieldsTable.Add (DataField.AppLogMessageSource, "applicationlog.MessageSource");

            #endregion

            #region Items table translations

            fieldsTable.Add (DataField.ItemId, "goods.ID");
            fieldsTable.Add (DataField.ItemCode, "goods.Code");
            fieldsTable.Add (DataField.ItemBarcode1, "goods.BarCode1");
            fieldsTable.Add (DataField.ItemBarcode2, "goods.BarCode2");
            fieldsTable.Add (DataField.ItemBarcode3, "goods.BarCode3");
            fieldsTable.Add (DataField.ItemCatalog1, "goods.Catalog1");
            fieldsTable.Add (DataField.ItemCatalog2, "goods.Catalog2");
            fieldsTable.Add (DataField.ItemCatalog3, "goods.Catalog3");
            fieldsTable.Add (DataField.ItemName, "goods.Name");
            fieldsTable.Add (DataField.ItemName2, "goods.Name2");
            fieldsTable.Add (DataField.ItemMeasUnit, "goods.Measure1");
            fieldsTable.Add (DataField.ItemMeasUnit2, "goods.Measure2");
            fieldsTable.Add (DataField.ItemMeasRatio, "goods.Ratio");
            fieldsTable.Add (DataField.ItemPurchasePrice, "goods.PriceIn");
            fieldsTable.Add (DataField.ItemTradePrice, "goods.PriceOut1");
            fieldsTable.Add (DataField.ItemRegularPrice, "goods.PriceOut2");
            fieldsTable.Add (DataField.ItemPriceGroup1, "goods.PriceOut3");
            fieldsTable.Add (DataField.ItemPriceGroup2, "goods.PriceOut4");
            fieldsTable.Add (DataField.ItemPriceGroup3, "goods.PriceOut5");
            fieldsTable.Add (DataField.ItemPriceGroup4, "goods.PriceOut6");
            fieldsTable.Add (DataField.ItemPriceGroup5, "goods.PriceOut7");
            fieldsTable.Add (DataField.ItemPriceGroup6, "goods.PriceOut8");
            fieldsTable.Add (DataField.ItemPriceGroup7, "goods.PriceOut9");
            fieldsTable.Add (DataField.ItemPriceGroup8, "goods.PriceOut10");
            fieldsTable.Add (DataField.ItemMinQuantity, "goods.MinQtty");
            fieldsTable.Add (DataField.ItemNomQuantity, "goods.NormalQtty");
            fieldsTable.Add (DataField.ItemDescription, "goods.Description");
            fieldsTable.Add (DataField.ItemType, "goods.Type");
            fieldsTable.Add (DataField.ItemOrder, "goods.IsVeryUsed");
            fieldsTable.Add (DataField.ItemDeleted, "goods.Deleted");
            fieldsTable.Add (DataField.ItemGroupId, "goods.GroupID");
            fieldsTable.Add (DataField.ItemIsRecipe, "goods.IsRecipe");
            fieldsTable.Add (DataField.ItemTaxGroupId, "goods.TaxGroup");

            fieldsTable.Add (DataField.ItemTradeInVAT);
            fieldsTable.Add (DataField.ItemTradeInSum);
            fieldsTable.Add (DataField.ItemTradeVAT);
            fieldsTable.Add (DataField.ItemTradeSum);

            #endregion

            #region ItemsGroups table translations

            fieldsTable.Add (DataField.ItemsGroupId, "goodsgroups.ID");
            fieldsTable.Add (DataField.ItemsGroupCode, "goodsgroups.Code");
            fieldsTable.Add (DataField.ItemsGroupName, "goodsgroups.Name");

            #endregion

            #region ItemsGroups table translations

            fieldsTable.Add (DataField.InternalLogId, "internallog.ID");
            fieldsTable.Add (DataField.InternalLogMessage, "internallog.Message");

            #endregion

            #region Store table translations

            fieldsTable.Add (DataField.StorePrice, "store.Price");
            fieldsTable.Add (DataField.StoreQtty, "store.Qtty");
            fieldsTable.Add (DataField.StoreLot, "store.Lot");
            fieldsTable.Add (DataField.StoreAvailableQuantity, "SUM(store.Qtty)");
            fieldsTable.Add (DataField.StoreCountedQuantity, "' '");

            #endregion

            #region Partners table translations

            fieldsTable.Add (DataField.PartnerId, "partners.ID");
            fieldsTable.Add (DataField.PartnerCode, "partners.Code");
            fieldsTable.Add (DataField.PartnerName, "partners.Company");
            fieldsTable.Add (DataField.PartnerName2, "partners.Company2");
            fieldsTable.Add (DataField.PartnerLiablePerson, "partners.MOL");
            fieldsTable.Add (DataField.PartnerLiablePerson2, "partners.MOL2");
            fieldsTable.Add (DataField.PartnerCity, "partners.City");
            fieldsTable.Add (DataField.PartnerCity2, "partners.City2");
            fieldsTable.Add (DataField.PartnerAddress, "partners.Address");
            fieldsTable.Add (DataField.PartnerAddress2, "partners.Address2");
            fieldsTable.Add (DataField.PartnerPhone, "partners.Phone");
            fieldsTable.Add (DataField.PartnerPhone2, "partners.Phone2");
            fieldsTable.Add (DataField.PartnerFax, "partners.Fax");
            fieldsTable.Add (DataField.PartnerEmail, "partners.eMail");
            fieldsTable.Add (DataField.PartnerTaxNumber, "partners.TaxNo");
            fieldsTable.Add (DataField.PartnerCardNumber, "partners.CardNumber");
            fieldsTable.Add (DataField.PartnerBulstat, "partners.Bulstat");
            fieldsTable.Add (DataField.PartnerBankName, "partners.BankName");
            fieldsTable.Add (DataField.PartnerBankCode, "partners.BankCode");
            fieldsTable.Add (DataField.PartnerBankAcct, "partners.BankAcct");
            fieldsTable.Add (DataField.PartnerBankVATName, "partners.BankVATName");
            fieldsTable.Add (DataField.PartnerBankVATCode, "partners.BankVATCode");
            fieldsTable.Add (DataField.PartnerBankVATAcct, "partners.BankVATAcct");
            fieldsTable.Add (DataField.PartnerPriceGroup, "partners.PriceGroup");
            fieldsTable.Add (DataField.PartnerDiscount, "partners.Discount");
            fieldsTable.Add (DataField.PartnerType, "partners.Type");
            fieldsTable.Add (DataField.PartnerOrder, "partners.IsVeryUsed");
            fieldsTable.Add (DataField.PartnerDeleted, "partners.Deleted");
            fieldsTable.Add (DataField.PartnerCreatorId, "partners.UserID");
            fieldsTable.Add (DataField.PartnerGroupId, "partners.GroupID");
            fieldsTable.Add (DataField.PartnerNote, "partners.Note1");
            fieldsTable.Add (DataField.PartnerNote2, "partners.Note2");
            fieldsTable.Add (DataField.PartnerTimeStamp, "partners.UserRealTime");
            fieldsTable.Add (DataField.PartnerPaymentDays, "partners.PaymentDays");
            fieldsTable.Add (DataField.PartnerDebt, "-1 * SUM(payments.Qtty * payments.Mode * payments.Sign)");

            #endregion

            #region PartnersGroups table translations

            fieldsTable.Add (DataField.PartnersGroupsId, "partnersgroups.ID");
            fieldsTable.Add (DataField.PartnersGroupsCode, "partnersgroups.Code");
            fieldsTable.Add (DataField.PartnersGroupsName, "partnersgroups.Name");

            #endregion

            #region Registration table translations

            fieldsTable.Add (DataField.CompanyId, "registration.ID");
            fieldsTable.Add (DataField.CompanyCode, "registration.Code");
            fieldsTable.Add (DataField.CompanyName, "registration.Company");
            fieldsTable.Add (DataField.CompanyLiablePerson, "registration.MOL");
            fieldsTable.Add (DataField.CompanyCity, "registration.City");
            fieldsTable.Add (DataField.CompanyAddress, "registration.Address");
            fieldsTable.Add (DataField.CompanyPhone, "registration.Phone");
            fieldsTable.Add (DataField.CompanyFax, "registration.Fax");
            fieldsTable.Add (DataField.CompanyEmail, "registration.eMail");
            fieldsTable.Add (DataField.CompanyTaxNumber, "registration.TaxNo");
            fieldsTable.Add (DataField.CompanyBulstat, "registration.Bulstat");
            fieldsTable.Add (DataField.CompanyBankName, "registration.BankName");
            fieldsTable.Add (DataField.CompanyBankCode, "registration.BankCode");
            fieldsTable.Add (DataField.CompanyBankAcct, "registration.BankAcct");
            fieldsTable.Add (DataField.CompanyBankVATAcct, "registration.BankVATAcct");
            fieldsTable.Add (DataField.CompanyCreatorId, "registration.UserID");
            fieldsTable.Add (DataField.CompanyCreationTimeStamp, "registration.UserRealTime");
            fieldsTable.Add (DataField.CompanyDefault, "registration.IsDefault");
            fieldsTable.Add (DataField.CompanyNote1, "registration.Note1");
            fieldsTable.Add (DataField.CompanyNote2, "registration.Note2");

            #endregion

            #region Users table translations

            fieldsTable.Add (DataField.UserId, "users.ID");
            fieldsTable.Add (DataField.UserCode, "users.Code");
            fieldsTable.Add (DataField.UserName, "users.Name");
            fieldsTable.Add (DataField.UserName2, "users.Name2");
            fieldsTable.Add (DataField.UserOrder, "users.IsVeryUsed");
            fieldsTable.Add (DataField.UserDeleted, "users.Deleted");
            fieldsTable.Add (DataField.UserGroupId, "users.GroupID");
            fieldsTable.Add (DataField.UserPassword, "users.Password");
            fieldsTable.Add (DataField.UserLevel, "users.UserLevel");
            fieldsTable.Add (DataField.UserCardNo, "users.CardNumber");

            #endregion

            #region UsersGroups table translations

            fieldsTable.Add (DataField.UsersGroupsId, "usersgroups.ID");
            fieldsTable.Add (DataField.UsersGroupsCode, "usersgroups.Code");
            fieldsTable.Add (DataField.UsersGroupsName, "usersgroups.Name");

            #endregion

            #region UsersSecurity table translations

            fieldsTable.Add (DataField.UsersSecurityId, "userssecurity.ID");
            fieldsTable.Add (DataField.UsersSecurityUserId, "userssecurity.UserId");
            fieldsTable.Add (DataField.UsersSecurityControlName, "userssecurity.ControlName");
            fieldsTable.Add (DataField.UsersSecurityState, "userssecurity.State");

            #endregion

            #region Objects table translations

            fieldsTable.Add (DataField.LocationId, "objects.ID");
            fieldsTable.Add (DataField.LocationCode, "objects.Code");
            fieldsTable.Add (DataField.LocationName, "objects.Name");
            fieldsTable.Add (DataField.LocationName2, "objects.Name2");
            fieldsTable.Add (DataField.LocationOrder, "objects.IsVeryUsed");
            fieldsTable.Add (DataField.LocationDeleted, "objects.Deleted");
            fieldsTable.Add (DataField.LocationGroupId, "objects.GroupID");
            fieldsTable.Add (DataField.LocationPriceGroup, "objects.PriceGroup");
            fieldsTable.Add (DataField.SourceLocationName);
            fieldsTable.Add (DataField.TargetLocationName);

            #endregion

            #region ObjectsGroups table translations

            fieldsTable.Add (DataField.LocationsGroupsId, "objectsgroups.ID");
            fieldsTable.Add (DataField.LocationsGroupsCode, "objectsgroups.Code");
            fieldsTable.Add (DataField.LocationsGroupsName, "objectsgroups.Name");

            #endregion

            #region Operations table translation

            fieldsTable.Add (DataField.OperationType, "operations.OperType");
            fieldsTable.Add (DataField.OperationNumber, "operations.Acct");
            fieldsTable.Add (DataField.OperationLocationId, "operations.ObjectID");
            fieldsTable.Add (DataField.OperationLocation, "operations.ObjectName");
            fieldsTable.Add (DataField.OperationLocation2, "operations.ObjectName2");
            fieldsTable.Add (DataField.OperationPartnerId, "operations.PartnerID");
            fieldsTable.Add (DataField.OperationPartner, "operations.PartnerName");
            fieldsTable.Add (DataField.OperationPartner2, "operations.PartnerName2");
            fieldsTable.Add (DataField.OperationOperatorId, "operations.OperatorID");
            fieldsTable.Add (DataField.OperationReferenceId, "operations.SrcDocID");
            fieldsTable.Add (DataField.OperationUserId, "operations.UserID");
            fieldsTable.Add (DataField.OperationTimeStamp, "operations.UserRealTime");
            fieldsTable.Add (DataField.OperationDetailReferenceId, "operations.SrcDocId");
            fieldsTable.Add (DataField.OperationDate, "operations.`Date`");
            fieldsTable.Add (DataField.OperationDateTime, "(operations.Date + INTERVAL operations.Note HOUR_MINUTE)");
            fieldsTable.Add (DataField.OperationQuantitySum, "SUM(operations.Qtty)");
            fieldsTable.Add (DataField.OperationQuantitySignedSum, "SUM(operations.Qtty * " + GetOperationsPriceSignQuery () + ")");
            fieldsTable.Add (DataField.OperationVatSum, "SUM(CASE WHEN operations.OperType in (2,4,14,15,16,17,26,27,34) THEN operations.VATOut ELSE operations.VATIn END * operations.Qtty)");
            fieldsTable.Add (DataField.OperationSum, "SUM(CASE WHEN operations.OperType in (2,4,14,15,16,17,26,27,34) THEN operations.PriceOut ELSE operations.PriceIn END * operations.Qtty)");
            fieldsTable.Add (DataField.OperationTotal, "SUM(CASE WHEN operations.OperType in (2,4,14,15,16,17,26,27,34) THEN operations.PriceOut + operations.VATOut ELSE operations.PriceIn + operations.VATIn END * operations.Qtty)",
                "SUM(CASE WHEN operations.OperType in (2,4,14,15,16,17,26,27,34) THEN operations.PriceOut ELSE operations.PriceIn END * operations.Qtty)");
            fieldsTable.Add (DataField.OperationProfit, "SUM(operations.Qtty * (operations.PriceOut - operations.PriceIn))");

            fieldsTable.Add (DataField.OperationDetailId, "operations.ID");
            fieldsTable.Add (DataField.OperationDetailItemId, "operations.GoodID");
            fieldsTable.Add (DataField.OperationDetailAvailableQuantity, "operations.Qtty");
            fieldsTable.Add (DataField.OperationDetailQuantity, "operations.Qtty");
            fieldsTable.Add (DataField.OperationDetailDifference, "operations.Qtty");
            fieldsTable.Add (DataField.OperationDetailPriceIn, "operations.PriceIn");
            fieldsTable.Add (DataField.OperationDetailPriceOut, "operations.PriceOut");
            fieldsTable.Add (DataField.OperationDetailVatIn, "operations.VATIn");
            fieldsTable.Add (DataField.OperationDetailVatOut, "operations.VATOut");
            fieldsTable.Add (DataField.OperationDetailVat, "CASE WHEN operations.OperType in (2,4,14,15,16,17,26,27,34) THEN operations.VATOut ELSE operations.VATIn END");
            fieldsTable.Add (DataField.OperationDetailDiscount, "operations.Discount");
            fieldsTable.Add (DataField.OperationDetailCurrencyId, "operations.CurrencyID");
            fieldsTable.Add (DataField.OperationDetailCurrencyRate, "operations.CurrencyRate");
            fieldsTable.Add (DataField.OperationDetailLot, "operations.Lot");
            fieldsTable.Add (DataField.OperationDetailWarrantySerialNumber, "operations.Lot");
            fieldsTable.Add (DataField.OperationDetailNote, "operations.Note");
            fieldsTable.Add (DataField.OperationDetailWarrantyPeriod, "operations.Note");
            fieldsTable.Add (DataField.OperationDetailSign, "operations.Sign");
            fieldsTable.Add (DataField.OperationDetailLotId, "operations.LotID");
            fieldsTable.Add (DataField.OperationDetailSumIn, "operations.PriceIn * operations.Qtty");
            fieldsTable.Add (DataField.OperationDetailSumOut, "operations.PriceOut * operations.Qtty");
            fieldsTable.Add (DataField.OperationDetailSum, "CASE WHEN operations.OperType in (2,4,14,15,16,17,26,27,34) THEN operations.PriceOut ELSE operations.PriceIn END * operations.Qtty");
            fieldsTable.Add (DataField.OperationDetailSumVatIn, "operations.VATIn * operations.Qtty");
            fieldsTable.Add (DataField.OperationDetailSumVatOut, "operations.VATOut * operations.Qtty");
            fieldsTable.Add (DataField.OperationDetailSumVat, "CASE WHEN operations.OperType in (2,4,14,15,16,17,26,27,34) THEN operations.VATOut ELSE operations.VATIn END * operations.Qtty");
            fieldsTable.Add (DataField.OperationDetailTotalIn, "(operations.PriceIn + operations.VATIn) * operations.Qtty",
                "operations.PriceIn * operations.Qtty");
            fieldsTable.Add (DataField.OperationDetailTotalOut, "(operations.PriceOut + operations.VATOut) * operations.Qtty",
                "operations.PriceOut * operations.Qtty");
            fieldsTable.Add (DataField.OperationDetailTotal, "CASE WHEN operations.OperType in (2,4,14,15,16,17,26,27,34) " +
                "THEN operations.PriceOut + operations.VATOut ELSE operations.PriceIn + operations.VATIn END * operations.Qtty",
                "CASE WHEN operations.OperType in (2,4,14,15,16,17,26,27,34) THEN operations.PriceOut ELSE operations.PriceIn END * operations.Qtty");
            fieldsTable.Add (DataField.OperationDetailStorePriceIn);
            fieldsTable.Add (DataField.OperationDetailStorePriceOut);
            fieldsTable.Add (DataField.ConsignmentDetailRemainingQuantity);
            fieldsTable.Add (DataField.RemainingQuantity);

            #region Purchase

            fieldsTable.Add (DataField.PurchaseQuantitySum);
            fieldsTable.Add (DataField.PurchaseSum);
            fieldsTable.Add (DataField.PurchaseVATSum);
            fieldsTable.Add (DataField.PurchaseTotal);

            #endregion

            #region Sale

            fieldsTable.Add (DataField.SaleQuantitySum);
            fieldsTable.Add (DataField.SaleSum);
            fieldsTable.Add (DataField.SaleVATSum);
            fieldsTable.Add (DataField.SaleTotal);

            #endregion

            #region Waste

            fieldsTable.Add (DataField.WasteQuantitySum);

            #endregion

            #region Stock-taking

            fieldsTable.Add (DataField.StockTakingDetailPrice);
            fieldsTable.Add (DataField.StockTakingQuantitySum);
            fieldsTable.Add (DataField.StockTakingTotal);
            fieldsTable.Add (DataField.StockTakingSum);

            #endregion

            #region Transfer

            fieldsTable.Add (DataField.TransferInQuantitySum);
            fieldsTable.Add (DataField.TransferOutQuantitySum);

            #endregion

            #region Write-off

            fieldsTable.Add (DataField.WriteOffQuantitySum);

            #endregion

            #region Consignments

            fieldsTable.Add (DataField.ConsignedQuantitySum);
            fieldsTable.Add (DataField.ConsignedQuantitySoldSum);
            fieldsTable.Add (DataField.ConsignedQuantityReturnedSum);

            #endregion

            #region Production

            fieldsTable.Add (DataField.ProductionMatQuantitySum);
            fieldsTable.Add (DataField.ProductionProdQuantitySum);

            #endregion

            #region Debit Note

            fieldsTable.Add (DataField.DebitNoteQuantitySum);

            #endregion

            #region Credit Note

            fieldsTable.Add (DataField.CreditNoteQuantitySum);

            #endregion

            #region Return

            fieldsTable.Add (DataField.ReturnQuantitySum);
            fieldsTable.Add (DataField.ReturnSum);

            #endregion

            #endregion

            #region OperationsOperators table translations

            fieldsTable.Add (DataField.OperationsOperatorId, "users1.ID");
            fieldsTable.Add (DataField.OperationsOperatorCode, "users1.Code");
            fieldsTable.Add (DataField.OperationsOperatorName, "users1.Name");
            fieldsTable.Add (DataField.OperationsOperatorName2, "users1.Name2");
            fieldsTable.Add (DataField.OperationsOperatorOrder, "users1.IsVeryUsed");
            fieldsTable.Add (DataField.OperationsOperatorGroupId, "users1.GroupID");
            fieldsTable.Add (DataField.OperationsOperatorPassword, "users1.Password");
            fieldsTable.Add (DataField.OperationsOperatorLevel, "users1.UserLevel");
            fieldsTable.Add (DataField.OperationsOperatorCardNo, "users1.CardNumber");

            #endregion

            #region OperationsOperatorsGroups table translations

            fieldsTable.Add (DataField.OperationsOperatorsGroupsId, "usersgroups1.ID");
            fieldsTable.Add (DataField.OperationsOperatorsGroupsCode, "usersgroups1.Code");
            fieldsTable.Add (DataField.OperationsOperatorsGroupsName, "usersgroups1.Name");

            #endregion

            #region OperationsUsers table translations

            fieldsTable.Add (DataField.OperationsUserId, "users2.ID");
            fieldsTable.Add (DataField.OperationsUserCode, "users2.Code");
            fieldsTable.Add (DataField.OperationsUserName, "users2.Name");
            fieldsTable.Add (DataField.OperationsUserName2, "users2.Name2");
            fieldsTable.Add (DataField.OperationsUserOrder, "users2.IsVeryUsed");
            fieldsTable.Add (DataField.OperationsUserGroupId, "users2.GroupID");
            fieldsTable.Add (DataField.OperationsUserPassword, "users2.Password");
            fieldsTable.Add (DataField.OperationsUserLevel, "users2.UserLevel");
            fieldsTable.Add (DataField.OperationsUserCardNo, "users2.CardNumber");

            #endregion

            #region OperationsUsersGroups table translations

            fieldsTable.Add (DataField.OperationsUsersGroupsId, "usersgroups2.ID");
            fieldsTable.Add (DataField.OperationsUsersGroupsCode, "usersgroups2.Code");
            fieldsTable.Add (DataField.OperationsUsersGroupsName, "usersgroups2.Name");

            #endregion

            #region Payment table translations

            fieldsTable.Add (DataField.PaymentId, "payments.ID");
            fieldsTable.Add (DataField.PaymentOperationId, "payments.Acct");
            fieldsTable.Add (DataField.PaymentOperationType, "payments.OperType");
            fieldsTable.Add (DataField.PaymentPartnerId, "payments.PartnerID");
            fieldsTable.Add (DataField.PaymentAmount, "payments.Qtty");
            fieldsTable.Add (DataField.PaymentMode, "payments.Mode");
            fieldsTable.Add (DataField.PaymentDate, "payments.`Date`");
            fieldsTable.Add (DataField.PaymentOperatorId, "payments.UserID");
            fieldsTable.Add (DataField.PaymentTimeStamp, "payments.UserRealTime");
            fieldsTable.Add (DataField.PaymentTypeId, "payments.Type");
            fieldsTable.Add (DataField.PaymentTransaction, "payments.TransactionNumber");
            fieldsTable.Add (DataField.PaymentEndDate, "payments.EndDate");
            fieldsTable.Add (DataField.PaymentLocationId, "payments.ObjectID");
            fieldsTable.Add (DataField.PaymentSign, "payments.Sign");

            fieldsTable.Add (DataField.PaymentDueSum, "SUM(CASE payments.Mode WHEN -1 THEN payments.Qtty ELSE 0 END)");
            fieldsTable.Add (DataField.PaymentRemainingSum, "SUM(payments.Qtty * payments.Mode * - 1)");
            fieldsTable.Add (DataField.PaymentsInCash, "SUM(CASE paymenttypes.PaymentMethod * payments.Mode WHEN 1 THEN payments.Qtty ELSE 0 END)");
            fieldsTable.Add (DataField.PaymentsByBankOrder, "SUM(CASE paymenttypes.PaymentMethod * payments.Mode WHEN 2 THEN payments.Qtty ELSE 0 END)");
            fieldsTable.Add (DataField.PaymentsByDebitCreditCard, "SUM(CASE paymenttypes.PaymentMethod * payments.Mode WHEN 3 THEN payments.Qtty ELSE 0 END)");
            fieldsTable.Add (DataField.PaymentsByVoucher, "SUM(CASE paymenttypes.PaymentMethod * payments.Mode WHEN 4 THEN payments.Qtty ELSE 0 END)");

            #endregion

            #region Payment types table translations

            fieldsTable.Add (DataField.PaymentTypesId, "paymenttypes.ID");
            fieldsTable.Add (DataField.PaymentTypesName, "paymenttypes.Name");
            fieldsTable.Add (DataField.PaymentTypesMethod, "paymenttypes.PaymentMethod");

            #endregion

            #region Price rules table translations

            fieldsTable.Add (DataField.PriceRuleId, "pricerules.ID");
            fieldsTable.Add (DataField.PriceRuleName, "pricerules.Name");
            fieldsTable.Add (DataField.PriceRuleFormula, "pricerules.Formula");
            fieldsTable.Add (DataField.PriceRuleEnabled, "pricerules.Enabled");
            fieldsTable.Add (DataField.PriceRulePriority, "pricerules.Priority");

            #endregion

            #region Purchase return

            fieldsTable.Add (DataField.PurchaseReturnQuantitySum);

            #endregion

            #region Cashbook table translations

            fieldsTable.Add (DataField.CashEntryId, "cashbook.ID");
            fieldsTable.Add (DataField.CashEntryDate, "cashbook.`Date`");
            fieldsTable.Add (DataField.CashEntryDescription, "cashbook.`Desc`");
            fieldsTable.Add (DataField.CashEntryTurnoverType, "cashbook.OperType");
            fieldsTable.Add (DataField.CashEntryDirection, "cashbook.Sign");
            fieldsTable.Add (DataField.CashEntryAmount, "cashbook.Profit");
            fieldsTable.Add (DataField.CashEntryOperatorId, "cashbook.UserID");
            fieldsTable.Add (DataField.CashEntryTimeStamp, "cashbook.UserRealTime");
            fieldsTable.Add (DataField.CashEntryLocationId, "cashbook.ObjectID");

            #endregion

            #region Documents table translations

            fieldsTable.Add (DataField.DocumentId, "documents.ID");
            fieldsTable.Add (DataField.DocumentOperationNumber, "documents.Acct");
            fieldsTable.Add (DataField.DocumentNumber, "documents.InvoiceNumber");
            fieldsTable.Add (DataField.DocumentOperationType, "documents.OperType");
            fieldsTable.Add (DataField.DocumentDate, "documents.InvoiceDate");
            fieldsTable.Add (DataField.DocumentType, "documents.DocumentType");
            fieldsTable.Add (DataField.DocumentReferenceDate, "documents.ExternalInvoiceDate");
            fieldsTable.Add (DataField.DocumentReferenceNumber, "documents.ExternalInvoiceNumber");
            fieldsTable.Add (DataField.DocumentPaymentTypeId, "documents.PaymentType");
            fieldsTable.Add (DataField.DocumentRecipient, "documents.Recipient");
            fieldsTable.Add (DataField.DocumentRecipientEGN, "documents.EGN");
            fieldsTable.Add (DataField.DocumentProvider, "documents.Provider");
            fieldsTable.Add (DataField.DocumentTaxDate, "documents.TaxDate");
            fieldsTable.Add (DataField.DocumentReason, "documents.Reason");
            fieldsTable.Add (DataField.DocumentDescription, "documents.Description");
            fieldsTable.Add (DataField.DocumentLocation, "documents.Place");

            #endregion

            #region VATGroups table translations

            fieldsTable.Add (DataField.VATGroupId, "vatgroups.ID");
            fieldsTable.Add (DataField.VATGroupCode, "vatgroups.Code");
            fieldsTable.Add (DataField.VATGroupName, "vatgroups.Name");
            fieldsTable.Add (DataField.VATGroupValue, "vatgroups.VATValue");

            #endregion

            #region Configuration table translations

            fieldsTable.Add (DataField.ConfigEntryId, "configuration.ID");
            fieldsTable.Add (DataField.ConfigEntryKey, "configuration.`Key`");
            fieldsTable.Add (DataField.ConfigEntryValue, "configuration.Value");
            fieldsTable.Add (DataField.ConfigEntryUserId, "configuration.UserID");

            #endregion

            #region ECR receipts table translations

            fieldsTable.Add (DataField.ECRReceiptID, "ecrreceipts.ID");
            fieldsTable.Add (DataField.ECRReceiptOperationType, "ecrreceipts.OperType");
            fieldsTable.Add (DataField.ECRReceiptDate, "ecrreceipts.ReceiptDate");

            #endregion

            #region Currencies table translations

            fieldsTable.Add (DataField.CurrencyID, "currencies.ID");
            fieldsTable.Add (DataField.CurrencyName, "currencies.Currency");
            fieldsTable.Add (DataField.CurrencyDescription, "currencies.Description");
            fieldsTable.Add (DataField.CurrencyExchangeRate, "currencies.ExchangeRate");
            fieldsTable.Add (DataField.CurrencyDeleted, "currencies.Deleted");

            #endregion

            #region Lots table translations

            fieldsTable.Add (DataField.LotId, "lots.ID");
            fieldsTable.Add (DataField.LotSerialNumber, "lots.SerialNo");
            fieldsTable.Add (DataField.LotExpirationDate, "lots.EndDate");
            fieldsTable.Add (DataField.LotProductionDate, "lots.ProductionDate");
            fieldsTable.Add (DataField.LotLocation, "lots.Location");

            #endregion

            #region Other translations

            fieldsTable.Add (DataField.PriceGroup);
            fieldsTable.Add (DataField.ReportDate);

            #endregion
        }

        protected virtual void InvalidateConnectionStrings ()
        {
            connString = null;
            slaveConnString = null;
        }

        protected abstract string GenerateConnectionString (bool masterServer);

        #region General methods

        public static DataProvider CreateProvider (string providerTypeName, ITranslationProvider translator, params object [] args)
        {
            Type type = Type.GetType (providerTypeName);
            if (type == null)
                return null;

            DataProvider instance = (DataProvider) Activator.CreateInstance (type, args);
            instance.translator = translator;

            return instance;
        }

        protected object GetConnection (bool readOnly, out object transaction)
        {
            TransactionContext context = TransactionContext.Current;
            if (context != null) {
                transaction = context.Transaction;
                return context.GetConnection ();
            }

            object conn = GetConnection (readOnly);
            transaction = null;
            return conn;
        }

        protected abstract object GetConnection (bool readOnly);

        public abstract bool TryConnect ();

        public abstract void Disconnect ();

        public abstract string GetAliasesString (params DataField [] fields);

        #region Database management

        public abstract string GetScript (string scriptFile, IList<DbParam> parameters);

        public abstract void CreateDatabase (string dbName, CreateDatabaseType type, Action<double> pCallback);

        public abstract bool CheckDatabaseExists (string dbName);

        public abstract bool DropDatabase (string dbName);

        public abstract string [] GetDatabases ();

        public static string FilterSqlParameter (string param)
        {
            param = param.Replace ("%", string.Empty);
            param = param.Replace ("--", string.Empty);
            param = param.Replace ("*", string.Empty);
            param = param.Replace (";", string.Empty);
            param = param.Replace ("@", string.Empty);
            param = param.Replace ("|", string.Empty);
            param = param.Replace ("/", string.Empty);
            param = param.Replace ("\\", string.Empty);

            return param;
        }

        public static bool IsValidDatabaseName (string name)
        {
            return Regex.IsMatch (name, @"^[a-zA-Z0-9_-]+$");
        }

        public abstract string GetSchemaVersion (int productId = 1);

        public abstract string GetDatabaseVersion ();

        public abstract bool CheckCompatible (string sourceVersion);

        public virtual bool CheckUpgradeable (string sourceVersion)
        {
            int i;
            bool sourceFound;
            string targetVersion = ProviderVersion;

            for (i = 0, sourceFound = false; i < upgradeTable.Count; i++) {
                UpgradeEntry entry = upgradeTable [i];
                if (!sourceFound) {
                    if (entry.SourceVersion == sourceVersion)
                        sourceFound = true;
                }

                if (!sourceFound)
                    continue;

                if (entry.TargetVersion == targetVersion)
                    return true;
            }

            return false;
        }

        public virtual void UpgradeDatabase (string sourceVersion, Action<double> pCallback)
        {
            int i;
            bool sourceFound;
            string targetVersion = ProviderVersion;
            List<UpgradeEntry> upgradePath = new List<UpgradeEntry> ();

            for (i = 0, sourceFound = false; i < upgradeTable.Count; i++) {
                UpgradeEntry entry = upgradeTable [i];
                if (!sourceFound) {
                    if (entry.SourceVersion == sourceVersion)
                        sourceFound = true;
                }

                if (!sourceFound)
                    continue;

                upgradePath.Add (entry);
                if (entry.TargetVersion == targetVersion)
                    break;
            }

            for (i = 0; i < upgradePath.Count; i++) {
                int i1 = i;
                upgradePath [i].Execute (this, p =>
                {
                    if (pCallback != null)
                        pCallback ((i1 * 100 + p) / upgradePath.Count);
                });
            }
        }

        /// <summary>
        /// Gets the current date and time on the server.
        /// </summary>
        /// <returns>The current date and time on the server.</returns>
        public abstract DateTime Now ();

        #endregion

        #region Transactions management

        public abstract void BeginTransaction ();

        public abstract void SnapshotObject (object obj, bool replaceSnapshots = false);

        public abstract void CommitTransaction ();

        public abstract void RollbackTransaction ();

        public abstract bool IsMasterScopeOpen { get; }

        public abstract void BeginMasterScope ();

        public abstract void EndMasterScope ();

        #endregion

        public abstract ulong GetMaxCodeValue (DbTable table, string pattern);

        #endregion

        #region -= Application Log =-

        public abstract LazyListModel<T> GetLastApplicationLogEntries<T> (long? userId, int? maxEntries);

        public abstract void AddApplicationLogEntry (string message, long userId, DateTime timeStamp, string source);

        #region Reports

        public abstract DataQueryResult ReportAppLogEntries (DataQuery querySet);

        #endregion

        #endregion

        #region -= CashBook =-

        public abstract LazyListModel<T> GetAllCashBookEntries<T> (DataQuery dataQuery);

        public abstract IEnumerable<long> GetCashBookLocationIds (DataQuery dataQuery);

        public abstract IDictionary<int, double> GetCashBookBalances<T> (DataQuery dataQuery);

        public abstract bool AddUpdateCashBookEntry (object chashBookEntryObject);

        public abstract void AddCashBookEntries (IEnumerable<object> cashBookEntries);

        public abstract bool DeleteCashBookEntry (long id);

        public abstract void DeleteCashBookEntries (DateTime date, long locationId);

        public abstract IEnumerable<string> GetRecentDescriptions ();

        #endregion

        #region -= Company Record =-

        public abstract LazyListModel<T> GetAllCompanyRecords<T> ();

        public abstract T GetCompanyRecordById<T> (long companyRecordId);

        public abstract T GetCompanyRecordByName<T> (string companyRecordName);

        public abstract T GetCompanyRecordByCode<T> (string companyRecordCode);

        public abstract T GetDefaultCompanyRecord<T> ();

        public abstract void AddUpdateCompanyRecord (object companyRecordObject);

        public abstract DeletePermission CanDeleteCompanyRecord (long companyRecordId);

        public abstract void DeleteCompanyRecord (long companyRecordId);

        #endregion

        #region -= Currencies =-

        public abstract LazyListModel<T> GetAllCurrencies<T> ();

        #endregion

        #region -= ECR Receipts =-

        public abstract void DeleteECRReceipts (DataQuery dataQuery);

        #endregion

        #region -= Items =-

        public abstract LazyListModel<T> GetAllItems<T> (long? groupId, bool onlyAvailable, bool autoStart, bool includeDeleted);

        public abstract LazyListModel<T> GetAllItemsByLocation<T> (long locationId, long? groupId, bool onlyAvailable, bool autoStart);

        public abstract LazyListModel<T> GetAllAvailableItemsAtLocation<T> (long locationId, long? groupId);

        public abstract ItemType? GetItemType (long itemId);

        public abstract double GetItemAvailability (long itemId, long locationId, long childLocationId);

        public abstract double GetItemAvailabilityAtDate (long itemId, long locationId, DateTime date);

        public abstract T GetItemById<T> (long id);

        public abstract T GetItemByName<T> (string name);

        public abstract T GetItemByCode<T> (string code);

        public abstract T GetItemByBarcode<T> (string barcode);

        public abstract T GetItemByCatalog<T> (string catalog);

        public abstract T GetItemBySerialNumber<T> (string serial, long locationId, ItemsManagementType imt);

        public abstract long GetMaxBarcodeSubNumber (string prefix, int barcodeLength, int subNumberStart, int subNumberLen);

        public abstract void AddUpdateItem (object itemObject);

        public abstract DeletePermission CanDeleteItem (long id);

        public abstract void DeleteItem (long id);

        #region Reports

        public abstract DataQueryResult ReportItems (DataQuery querySet);

        public abstract DataQueryResult ReportItemsAvailability (DataQuery querySet, string quantityTranslation);

        public abstract DataQueryResult ReportItemsAvailabilityAtDate (DataQuery querySet, string dateString);

        public abstract DataQueryResult ReportItemsMinimalQuantities (DataQuery querySet);

        public abstract DataQueryResult ReportItemsFlow (DataQuery querySet);

        public abstract DataQueryResult ReportItemsByProfit (DataQuery querySet);

        public abstract DataQueryResult ReportInvoicedItems (DataQuery querySet);

        #endregion

        private string itemDefaultAliases;

        public string ItemDefaultAliases
        {
            get
            {
                return itemDefaultAliases ?? (itemDefaultAliases = GetAliasesString (DataField.ItemId,
                    DataField.ItemCode,
                    DataField.ItemBarcode1,
                    DataField.ItemBarcode2,
                    DataField.ItemBarcode3,
                    DataField.ItemCatalog1,
                    DataField.ItemCatalog2,
                    DataField.ItemCatalog3,
                    DataField.ItemName,
                    DataField.ItemName2,
                    DataField.ItemMeasUnit,
                    DataField.ItemMeasUnit2,
                    DataField.ItemMeasRatio,
                    DataField.ItemPurchasePrice,
                    DataField.ItemTradePrice,
                    DataField.ItemRegularPrice,
                    DataField.ItemPriceGroup1,
                    DataField.ItemPriceGroup2,
                    DataField.ItemPriceGroup3,
                    DataField.ItemPriceGroup4,
                    DataField.ItemPriceGroup5,
                    DataField.ItemPriceGroup6,
                    DataField.ItemPriceGroup7,
                    DataField.ItemPriceGroup8,
                    DataField.ItemMinQuantity,
                    DataField.ItemNomQuantity,
                    DataField.ItemDescription,
                    DataField.ItemType,
                    DataField.ItemGroupId,
                    DataField.ItemIsRecipe,
                    DataField.ItemTaxGroupId,
                    DataField.ItemOrder));
            }
        }

        #endregion

        #region -= ItemsGroups =-

        public abstract T [] GetAllItemsGroups<T> ();

        public abstract T GetItemsGroupById<T> (long groupId);

        public abstract T GetItemsGroupByCode<T> (string groupCode);

        public abstract T GetItemsGroupByName<T> (string groupName);

        public abstract void AddUpdateItemsGroup (object groupObject);

        public abstract DeletePermission CanDeleteItemsGroup (long groupId);

        public abstract void DeleteItemsGroup (long groupId);

        #endregion

        #region -= Internal Log =-

        public abstract void AddInternalLogEntry (string message);

        public abstract LazyListModel<T> GetAllInternalLogEntries<T> (string search, int? maxEntries);

        public abstract void DeleteInternalLogEntries (params long [] id);

        #endregion

        #region -= Invoice =-

        public abstract T [] GetIssuedInvoicesByNumber<T> (long id);

        public abstract T [] GetReceivedInvoicesByNumber<T> (long id);

        public abstract T GetReceivedInvoiceForOperation<T> (long operationNumber);

        public abstract long GetNextDocumentId (long locationId);

        public abstract long GetLastDocumentNumber (long operationNumber, long operationType);

        public abstract bool IsDocumentNumberUsed (long number);

        public abstract bool DocumentExistsForOperation (long operationNumber, long operationType);

        public abstract LazyListModel<T> GetAllDocuments<T> (DataQuery dataQuery, DocumentType type,
            bool extendedInfo = false, params OperationType [] operationTypes);

        public abstract List<string> GetDocumentRecipientSuggestions (long partnerId);

        public abstract List<string> GetDocumentEGNSuggestions (long partnerId);

        public abstract List<string> GetDocumentProviderSuggestions ();

        public abstract List<string> GetDocumentReasonSuggestions ();

        public abstract List<string> GetDocumentDescriptionSuggestions ();

        public abstract IEnumerable<string> GetDocumentLocationSuggestions ();

        public abstract void CreateNewDocumentId (long invoiceId, int docNumberLen);

        public abstract void AddInvoice (IEnumerable<object> invoiceObjects, int docNumberLen, bool createNewId);

        public abstract void DeleteInvoice (long invoiceNumber);

        #region Reports

        public abstract DataQueryResult ReportInvoicesIssued (DataQuery querySet);

        public abstract DataQueryResult ReportInvoicesReceived (DataQuery querySet);

        #endregion

        #endregion

        #region -= Measuring Units =-

        public abstract LazyListModel<T> GetAllUnits<T> ();

        public abstract ObjectsContainer<string, string> GetUnitsForItem (long itemId);

        #endregion

        #region -= Operations =-

        public abstract double CalculateAveragePrice (long itemId, double newPriceIn, double newItemQty, bool annulling, bool quickMode);

        public static double CalculateAveragePrice (double purchaseAmount, double purchaseQuantity, double saleAmount, double saleQuantity, double minPriceIn, double maxPriceIn)
        {
            double countSum = purchaseQuantity + saleQuantity;
            if (countSum > 0) {
                double avgPrice = (purchaseAmount + saleAmount) / countSum;
                if (avgPrice >= 0) {
                    if (purchaseQuantity.IsZero () || saleQuantity.IsZero ())
                        return avgPrice;

                    if ((avgPrice <= maxPriceIn || maxPriceIn.IsZero ()) &&
                        avgPrice >= minPriceIn)
                        return avgPrice;
                }
            }

            if (!purchaseQuantity.IsZero ())
                return Math.Abs (purchaseAmount / purchaseQuantity);

            if (!saleQuantity.IsZero ())
                return Math.Abs (saleAmount / saleQuantity);

            return 0;
        }

        /// <summary>
        /// Creates an ID for a new operation created at the location with the specified ID.
        /// </summary>
        /// <param name="operationType">The type of the operation to get an ID for.</param>
        /// <param name="locationId"> </param>
        /// <param name="currentState"></param>
        /// <returns></returns>
        public abstract long CreateNewOperationId (OperationType operationType, long locationId, OperationState currentState = OperationState.New);

        public abstract bool OperationIdExists (long id);

        public abstract bool OperationNoteExists (string note);

        public abstract void DeleteOperationId (OperationType operationType, long operationId);

        public abstract long? GetOperationId (long detailId);

        public abstract long GetMaxOperationId ();

        public abstract void DeletePayableOperationsBefore (DateTime date, bool onlyPaid);

        public abstract void AddOperations (IEnumerable<DictionaryEntry> operations);

        public abstract void AddOperationIdCodes (IEnumerable<string> operationCodes);

        public abstract void DeleteOperationIdCodes ();

        public abstract void DeleteNonPayableOperations (DateTime date);

        /// <summary>
        /// Gets the last operation numbers per operation type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="groupByLocation"></param>
        /// <returns>Value1 - Operation type, Value2 - Last number, Value3 - Location Id (if grouping by id)</returns>
        public abstract List<ObjectsContainer<int, long, long>> GetLastOperationNumbers (bool groupByLocation);

        public abstract T [] GetOperationNumbersUsagePerLocation<T> ();

        public abstract ObjectsContainer<OperationType, long> [] GetOperationNumbersUsageStarts ();

        /// <summary>
        /// Gets the numbers to be used for objects of type <typeparamref name="T"/> at the location with the specified location ID.
        /// </summary>
        /// <typeparam name="T">The type of the objects to be numbered with the obtained numbers.</typeparam>
        /// <returns></returns>
        public abstract T [] GetOperationStartNumbersPerLocation<T> ();

        /// <summary>
        /// Creates for the first time the numbers to be used as ID-s for documents (operations).
        /// </summary>
        /// <param name="minimalRange">The minimal range between the numbers for two consecutive locations.</param>
        /// <param name="recommendedRange">The recommended range between the numbers for two consecutive locations.</param>
        public abstract void CreateOperationStartNumbersPerLocation (long minimalRange, long recommendedRange);

        /// <summary>
        /// Updates the numbers used as ID-s for documents (operations) according to their location.
        /// </summary>
        /// <param name="operations">The operations which contain the new values of the numbers.</param>
        public abstract void UpdateOperationStartNumbersPerLocation<T> (IEnumerable<T> objectsContainers);

        /// <summary>
        /// Deletes the numbers used as ID-s for documents (operations) according to their location.
        /// </summary>
        public abstract void DeleteOperationStartNumbersPerLocation ();

        public abstract T GetFirstOperation<T> ();

        public abstract T GetOperationById<T> (OperationType operationType, long operationId);

        public abstract T GetOperationWithPartnerById<T> (OperationType operationType, long operationId);

        public abstract T [] GetAllAfter<T> (DateTime operationDate);

        public abstract T GetPendingOperation<T> (OperationType operationType, long partnerId, long locationId);

        public abstract T GetPendingOperationWithPartner<T> (OperationType operationType, long partnerId, long locationId);

        /// <summary>
        /// Gets all the pending operations
        /// </summary>
        /// <returns>Container with the operation type, partner ID and location ID</returns>
        public abstract ObjectsContainer<OperationType, long, long> [] GetAllPendingOperations ();

        public abstract LazyListModel<T> GetOperationDetailsById<T> (OperationType operationType, long operationId, long? partnerId, long? locationId, DateTime? date, long? userId, long loggedUserId, int currencyPrecission, bool roundPrices, bool usePriceIn);

        public abstract LazyListModel<T> GetQuantities<T> (DataQuery dataQuery, bool useLots);

        public abstract LazyListModel<long> GetOperationLocationIds (DataQuery dataQuery);

        public abstract void UpdateOperationUser (OperationType operationType, long operationId, long userId);

        public void AddUpdateDetail (object operation, object operationDetail,
            bool allowNegativeQty = false,
            ItemsManagementType imt = ItemsManagementType.AveragePrice,
            DataField priceOutField = DataField.NotSet,
            long childLocationId = -1,
            string updateClause = "UPDATE operations {0} WHERE ID = @ID",
            string deleteClause = "DELETE FROM operations WHERE ID = @ID")
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (operation);
            helper.AddObject (operationDetail);

            long id = (long) helper.GetObjectValue (DataField.OperationNumber);
            int sign;
            if (id < 0) {
                helper.SetObjectValue (DataField.OperationDetailSign, 0);
                helper.SetParameterValue (DataField.OperationDetailSign, 0);
                sign = 0;
            } else
                sign = (int) helper.GetObjectValue (DataField.OperationDetailSign);

            if (sign < 0)
                AddUpdateDetailOut (helper, allowNegativeQty, imt, childLocationId, updateClause, deleteClause);
            else if (sign == 0)
                AddUpdateStoreNeutralDetail (helper);
            else
                AddUpdateDetailIn (helper, allowNegativeQty, priceOutField, imt);
        }

        public abstract void AddUpdateStoreNeutralDetail (SqlHelper helper);

        public abstract void AddUpdateDetailOut (SqlHelper helper, bool allowNegativeQty, ItemsManagementType imt, long childLocationId, string updateClause = "", string deleteClause = "");

        public abstract void AddUpdateDetailIn (SqlHelper helper, bool allowNegativeQty, DataField priceOutField, ItemsManagementType imt);

        public abstract string GetOperationDetailTotalIn (DataQuery query);

        public abstract string GetOperationDetailTotalOut (DataQuery query);

        public abstract string GetOperationDetailVatOutSum (DataQuery query);

        public abstract string GetOperationDetailVatInSum (DataQuery query);

        public abstract string GetOperationPriceInColumns (DataQuery query, bool addTotal = true);

        public abstract string GetOperationPriceOutColumns (DataQuery query, bool addTotal = true);

        public static string GetOperationAbbreviation (OperationType operationType)
        {
            switch (operationType) {
                case OperationType.Purchase:
                    return "D";

                case OperationType.Sale:
                case OperationType.ConsignmentSale:
                    return "S";

                case OperationType.Waste:
                    return "WA";

                case OperationType.StockTaking:
                    return "Rev";

                case OperationType.ProduceIn:
                    return "PI";

                case OperationType.TransferIn:
                case OperationType.TransferOut:
                    return "T";

                case OperationType.WriteOff:
                    return "WO";

                case OperationType.PurchaseOrder:
                    return "R";

                case OperationType.Offer:
                    return "OF";

                case OperationType.ProformaInvoice:
                    return "P";

                case OperationType.Consignment:
                    return "CG";

                case OperationType.ConsignmentReturn:
                    return "CR";

                case OperationType.TakeConsignation:
                    return "CT";

                case OperationType.SalesOrder:
                    return "OR";

                case OperationType.ComplexRecipeMaterial:
                case OperationType.ComplexRecipeProduct:
                    return "RCI";

                case OperationType.ComplexProductionMaterial:
                case OperationType.ComplexProductionProduct:
                    return "PCI";

                case OperationType.DebitNote:
                    return "DN";

                case OperationType.CreditNote:
                    return "CN";

                case OperationType.WarrantyCard:
                    return "G";

                case OperationType.RestaurantOrder:
                    return "RO";

                case OperationType.Return:
                    return "Rf";

                case OperationType.Reservation:
                    return "RR";

                case OperationType.AdvancePayment:
                    return "AP";

                case OperationType.Temp:
                    return "TMP";
            }

            return String.Empty;
        }

        public abstract DataQueryResult ReportOperations (DataQuery querySet);

        public abstract DataQueryResult ReportDrafts (DataQuery querySet);

        #endregion

        #region -= Partner =-

        public abstract LazyListModel<T> GetAllPartners<T> (long? groupId);

        public abstract T GetPartnerById<T> (long partnerId);

        public abstract T GetPartnerByName<T> (string partnerName);

        public abstract T GetPartnerByCode<T> (string partnerCode);

        public abstract T GetPartnerByBulstat<T> (string partnerBulstat);

        /// <summary>
        /// Gets the partner with the specified number on a magnetic card.
        /// </summary>
        /// <param name="partnerCardNo">The number of the partner's magnetic card.</param>
        /// <returns>The partner with the specified number on a magnetic card.</returns>
        public abstract T GetPartnerByCard<T> (string partnerCardNo);

        public abstract T GetPartnerByEmail<T> (string email);

        public abstract T GetPartnerByPhone<T> (string phone);

        public abstract T GetPartnerByLiablePerson<T> (string name);

        public abstract double GetPartnerTurnover (long partnerId);

        public abstract double GetPartnerDebt (long partnerId);

        public abstract T [] GetPartnerDuePayments<T> (long partnerId);

        public abstract double GetPartnerUnpaidAmountWithExpiredDueDate (long partnerId, DateTime operationDate);

        public abstract void AddUpdatePartner (object partnerObject);

        public abstract DeletePermission CanDeletePartner (long partnerId);

        public abstract void DeletePartner (long partnerId);

        #region Reports

        public abstract DataQueryResult ReportPartners (DataQuery querySet);

        public abstract DataQueryResult ReportPartnersByProfit (DataQuery querySet);

        public abstract DataQueryResult ReportPartnersDebt (DataQuery querySet);

        #endregion

        #endregion

        #region -= PartnersGroups =-

        public abstract T [] GetAllPartnersGroups<T> ();

        public abstract T GetPartnersGroupById<T> (long groupId);

        public abstract T GetPartnersGroupByCode<T> (string groupCode);

        public abstract T GetPartnersGroupByName<T> (string groupName);

        public abstract void AddUpdatePartnersGroup (object groupObject);

        public abstract DeletePermission CanDeletePartnersGroup (long groupId);

        public abstract void DeletePartnersGroup (long groupId);

        #endregion

        #region -= Payment =-

        public abstract LazyListModel<T> GetAllPaymentsPerOperation<T> (DataQuery dataQuery, bool onlyPaid = false);

        public abstract T [] GetPaymentsForOperation<T> (long operationId, int paymentType, int operationType, long locationId, long partnerId);

        public abstract bool AddUpdatePayment (object paymentObject);

        public abstract void AddPayments (IEnumerable<object> payments);

        public abstract void DeletePayment (long paymentId);

        public abstract void AddAdvancePayment (object payment);

        public abstract void EditAdvancePayment (object payment);

        public abstract LazyListModel<T> GetAdvancePayments<T> (long partnerId);

        public abstract LazyListModel<T> GetAdvancePayments<T> (DataQuery dataQuery);

        #region Reports

        public abstract DataQueryResult ReportPaymentsByDocuments (DataQuery querySet);

        public abstract DataQueryResult ReportPaymentsByPartners (DataQuery querySet);

        public abstract DataQueryResult ReportPaymentsDueDates (DataQuery querySet);

        public abstract DataQueryResult ReportPaymentsHistory (DataQuery querySet);

        public abstract DataQueryResult ReportIncome (DataQuery querySet);

        public abstract DataQueryResult ReportPaymentsAdvance (DataQuery querySet);

        public abstract DataQueryResult ReportTurnover (DataQuery querySet);

        #endregion

        #endregion

        #region -= Payment Type =-

        public abstract LazyListModel<T> GetAllPaymentMethods<T> ();

        public abstract T GetPaymentMethodById<T> (long paymentTypeId);

        public abstract T GetPaymentMethodByName<T> (string paymentTypeName);

        public abstract void AddUpdatePaymentMethod (object paymentTypeObject);

        public abstract DeletePermission CanDeletePaymentMethod (long paymentTypeId);

        public abstract void DeletePaymentMethod (long paymentTypeId);

        #endregion

        #region -= Price Rules =-

        public abstract LazyListModel<T> GetAllPriceRules<T> ();

        public abstract void AddUpdatePriceRule (object priceRule);

        public abstract DeletePermission CanDeletePriceRule (long id);

        public abstract void DeletePriceRule (long priceRuleId);

        #endregion

        #region -= Location =-

        public abstract LazyListModel<T> GetAllLocations<T> (long? groupId, bool includeDeleted);

        /// <summary>
        /// Gets the ID-s of all points of sale.
        /// </summary>
        /// <typeparam name="T">The type of the ID-s to return.</typeparam>
        /// <returns>A list of the ID-s of all points of sale.</returns>
        public abstract List<long> GetAllLocationIds ();

        public abstract T GetLocationById<T> (long locationId);

        public abstract T GetLocationByName<T> (string locationName);

        public abstract T GetLocationByCode<T> (string locationCode);

        public abstract List<long> GetAllLocationsWithRestaurantOrders (long? userId);

        public abstract bool LocationHasRestaurantOrders (long? locationId, long? partnerId, long? userId);

        public abstract bool LocationHasChildLocationWithRestaurantOrders (long locationId);

        public abstract void AddUpdateLocation (object locationObject, bool documentNumbersPerLocation, long recommendedRange);

        public abstract DeletePermission CanDeleteLocation (long locationId);

        public abstract void DeleteLocation (long locationId);

        #region Reports

        public abstract DataQueryResult ReportLocations (DataQuery querySet);

        public abstract DataQueryResult ReportLocationsByProfit (DataQuery querySet);

        #endregion

        #endregion

        #region -= LocationsGroups =-

        public abstract T [] GetAllLocationsGroups<T> ();

        public abstract T GetLocationsGroupById<T> (long groupId);

        public abstract T GetLocationsGroupByCode<T> (string groupCode);

        public abstract T GetLocationsGroupByName<T> (string groupName);

        public abstract void AddUpdateLocationsGroup (object groupObject);

        public abstract DeletePermission CanDeleteLocationsGroup (long groupId);

        public abstract void DeleteLocationsGroup (long groupId);

        #endregion

        #region -= Purchase =-

        public abstract LazyListModel<T> GetAllPurchases<T> (DataQuery dataQuery, bool onlyUninvoiced);

        public abstract Dictionary<long, long> GetAllPurchaseIdsWithInvoices ();

        public abstract void AddUpdatePurchase (object purchaseObject, object [] purchaseDetailObjects, bool allowNegativeQty, DataField priceOutField);

        #region Reports

        public abstract DataQueryResult ReportPurchases (DataQuery querySet);

        public abstract DataQueryResult ReportPurchasesByItems (DataQuery querySet);

        public abstract DataQueryResult ReportPurchasesByPartners (DataQuery querySet);

        public abstract DataQueryResult ReportPurchasesByLocations (DataQuery querySet);

        public abstract DataQueryResult ReportPurchasesByTotal (DataQuery querySet);

        #endregion

        #endregion

        #region -= Reservation =-

        public abstract LazyListModel<T> GetReservations<T> (long? locationId, long? customerId, long? userId, DateTime? @from, DateTime? to);

        public abstract void AddUpdateReservation (object resObject, object resObjectDetail);

        #region Reports

        public abstract DataQueryResult ReportReservations (DataQuery querySet);

        #endregion

        #endregion

        #region -= RestaurantOrder =-

        public abstract LazyListModel<T> GetRestaurantOrders<T> (long? locationId, long? customerId, long? userId, DateTime? @from, DateTime? to);

        public abstract LazyListModel<T> GetRestaurantOrderDetails<T> (long? operationId, long locationId, long? customerId, long? userId);

        public abstract void AddUpdateRestaurantOrder (object restOrderObject, object [] restOrderDetailObjects);

        #region Reports

        public abstract DataQueryResult ReportOrders (DataQuery querySet);

        #endregion

        #endregion

        #region -= Sale =-

        public abstract LazyListModel<T> GetAllSales<T> (DataQuery dataQuery, OperationType saleType = OperationType.Any);

        public abstract LazyListModel<T> GetAllUninvoicedSales<T> (DataQuery dataQuery, bool fullyPaid = false);

        public abstract T GetCashReport<T> (DataQuery dataQuery) where T : class;

        public abstract Dictionary<long, long> GetAllSaleIdsWithInvoices ();

        public abstract T GetSaleById<T> (long saleId);

        public abstract T GetSaleByNote<T> (string note);

        public abstract T GetPendingSale<T> (long partnerId, long locationId);

        public abstract void AddUpdateSale (object saleObject, object [] saleDetailObjects, bool allowNegativeQty, long childLocationId);

        #region Reports

        public abstract DataQueryResult ReportSales (DataQuery querySet);

        public abstract DataQueryResult ReportSalesByItems (DataQuery querySet);

        public abstract DataQueryResult ReportSalesByPartners (DataQuery querySet);

        public abstract DataQueryResult ReportSalesByLocations (DataQuery querySet);

        public abstract DataQueryResult ReportSalesByTotal (DataQuery querySet);

        public abstract DataQueryResult ReportSalesByUser (DataQuery querySet);

        #endregion

        #endregion

        #region -= Stock-taking =-

        public abstract LazyListModel<T> GetAllStockTakings<T> (DataQuery dataQuery);

        public abstract void AddUpdateStockTaking (object stockTakingObject, object [] stockTakingDetailObjects, bool allowNegativeQty, DataField priceOutField, bool annul);

        #region Reports

        public abstract DataQueryResult ReportStockTakings (DataQuery querySet);

        public abstract DataQueryResult ReportStockTakingsByTotal (DataQuery querySet);

        #endregion

        #endregion

        #region -= Transfer =-

        public abstract LazyListModel<T> GetAllTransfers<T> (DataQuery dataQuery);

        public abstract void AddUpdateTransferOut (object transferObject, object [] transferDetailObjects, bool allowNegativeQty);

        public abstract void AddUpdateTransferIn (object transferObject, object [] transferDetailObjects, bool allowNegativeQty, bool increaseStoreAvailability);

        public abstract void ApplyTransferOutDeleteToStore (SqlHelper helper, bool usesLots);

        public abstract void ApplyTransferInInsertToStore (SqlHelper helper, bool usesLots);

        #region Reports

        public abstract DataQueryResult ReportTransfers (DataQuery querySet, bool usePriceIn);

        #endregion

        #endregion

        #region -= User =-

        public abstract LazyListModel<T> GetAllUsers<T> ();

        public abstract LazyListModel<T> GetAllUsers<T> (int maxUserLevel, long currentUserId);

        public abstract LazyListModel<T> GetAllUsers<T> (long? groupId);

        public abstract T GetUserById<T> (long userId);

        public abstract T GetUserByName<T> (string userName);

        public abstract T GetUserByCode<T> (string userCode);

        public abstract T GetUserByCard<T> (string userCardNo);

        public abstract void AddUpdateUser (object userObject);

        public abstract DeletePermission CanDeleteUser (long userId);

        public abstract void DeleteUser (long userId);

        public abstract bool CheckUserOwnerExists ();

        #region Reports

        public abstract DataQueryResult ReportUsers (DataQuery querySet);

        public abstract DataQueryResult ReportOperationsByUsers (DataQuery querySet);

        public abstract DataQueryResult ReportReturnsByUser (DataQuery querySet);

        #endregion

        #endregion

        #region -= UsersGroups =-

        public abstract T [] GetAllUsersGroups<T> ();

        public abstract T GetUsersGroupById<T> (long groupId);

        public abstract T GetUsersGroupByCode<T> (string groupCode);

        public abstract T GetUsersGroupByName<T> (string groupName);

        public abstract void AddUpdateUsersGroup (object groupObject);

        public abstract DeletePermission CanDeleteUsersGroup (long groupId);

        public abstract void DeleteUsersGroup (long groupId);

        #endregion

        #region -= User Restrictions  =-

        public abstract void AddUpdateUserRestriction<T> (IEnumerable<T> restrictionObjects);

        public abstract T [] GetAllRestrictions<T> ();

        public abstract T [] GetRestrictionsByUser<T> (long userId);

        public abstract T [] GetRestrictionsByName<T> (string name);

        #endregion

        #region -= Configuration =-

        public abstract T GetConfigurationByKey<T> (string keyName, long? userId);

        public abstract string GetConfigurationByKey (string keyName, long? userId);

        public abstract void SetConfiguration (string key, long? userId, string value);

        public abstract void AddUpdateConfiguration (object configurationObject);

        public abstract void DeleteConfiguration (string key, long? userId);

        #endregion

        #region -= VAT Groups =-

        public abstract LazyListModel<T> GetAllVATGroups<T> ();

        public abstract T GetVATGroupById<T> (long vatGroupId);

        public abstract T GetVATGroupByCode<T> (string vatGroupCode);

        public abstract T GetVATGroupByName<T> (string vatGroupName);

        public abstract void AddUpdateVATGroup (object vatGroupObject);

        public abstract DeletePermission CanDeleteVATGroup (long vatGroupId);

        public abstract void DeleteVATGroup (long vatGroupId);

        #endregion

        #region -= Waste =-

        public abstract LazyListModel<T> GetAllWastes<T> (DataQuery dataQuery);

        public abstract void AddUpdateWaste (object wasteObject, object [] wasteDetailObjects, bool allowNegativeQty);

        #region Reports

        public abstract DataQueryResult ReportWastes (DataQuery querySet, bool usePriceIn);

        #endregion

        #endregion

        #region -= Complex Recipe =-

        public abstract LazyListModel<T> GetAllComplexRecipes<T> ();

        public abstract T [] GetAllComplexRecipesByProductId<T> (long id);

        public abstract T GetComplexRecipeById<T> (long id);

        public abstract LazyListModel<T> GetComplexRecipeMatDetailsById<T> (long id);

        public abstract LazyListModel<T> GetComplexRecipeProdDetailsById<T> (long id);

        public abstract void AddUpdateComplexRecipeMat (object obj, object [] detailObj);

        public abstract void AddUpdateComplexRecipeProd (object obj, object [] detailObj);

        public abstract DataQueryResult ReportComplexRecipes (DataQuery querySet);

        #endregion

        #region -= Complex Production =-

        public abstract LazyListModel<T> GetAllComplexProductions<T> (DataQuery dataQuery);

        public abstract LazyListModel<T> GetComplexProductionMatDetailsById<T> (long id, DataField operPriceGroup);

        public abstract LazyListModel<T> GetComplexProductionProdDetailsById<T> (long id, DataField operPriceGroup);

        public abstract void AddUpdateComplexProductionMat (object obj, object [] detailObj, bool allowNegativeQty);

        public abstract void AddUpdateComplexProductionProd (object obj, object [] detailObj, bool allowNegativeQty);

        public abstract DataQueryResult ReportComplexProductions (DataQuery querySet);

        #endregion

        #region -= Lots =-

        public ItemsManagementType GetItemsManagementType ()
        {
            string key = GetConfigurationByKey ("configkey23", null);
            int lotSetting;
            if (!int.TryParse (key, out lotSetting))
                return ItemsManagementType.AveragePrice;

            ItemsManagementType ret = (ItemsManagementType) lotSetting;
            if (ret == ItemsManagementType.AveragePrice) {
                key = GetConfigurationByKey ("QuickAPPCalc", null);
                if (key != null && key.ToLowerInvariant () == "true")
                    ret = ItemsManagementType.QuickAveragePrice;
            }

            return ret;
        }

        public abstract LazyListModel<T> GetLots<T> (long itemId, long? locationId, ItemsManagementType imt);

        public abstract T GetLotByStoreId<T> (long storeId);

        public abstract void EnableLots ();

        public abstract void DisableLots ();

        public abstract string GetReportLotSelect (DataQuery querySet);

        public abstract string GetReportLotJoin (DataQuery querySet);

        public abstract string GetReportLotGroup (DataQuery querySet);

        #endregion

        #region -= Report Query States =-

        public T GetReportsQueryState<T> ()
        {
            return GetObjectFromXML<T> (null, StoragePaths.ReportQueryStateFile);
        }

        public void SaveReportsQueryState<T> (T state)
        {
            SaveObjectToXML (StoragePaths.ReportQueryStateFile, state);
        }

        #endregion

        #region -= Operation Query States =-

        public T GetDocumentQueryStates<T> ()
        {
            return GetObjectFromXML<T> (null, StoragePaths.DocumentQueryStateFile);
        }

        public void SaveDocumentQueryStates<T> (T state)
        {
            SaveObjectToXML (StoragePaths.DocumentQueryStateFile, state);
        }

        #endregion

        #region -= Custom Configs =-

        public abstract string [] GetAllCustomConfigProfiles (string category);

        public abstract T [] GetCustomConfig<T> (string category, string profile);

        public abstract void AddUpdateCustomConfig<T> (T [] configs, string category, string profile);

        public abstract void DeleteCustomConfig (string category, string profile);

        #endregion

        #region Helper methods

        #region Execute object

        public static object GetDefaultValue (Type objType)
        {
            switch (objType.ToString ()) {
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                    return 0;
                case "System.Single":
                    return 0f;
                case "System.Double":
                    return 0d;
                case "System.Decimal":
                    return 0m;
                case "System.DateTime":
                    return DateTime.MinValue;
                case "System.String":
                    return string.Empty;
                case "System.Char":
                    return ' ';
                case "System.Boolean":
                    return false;
                case "System.Guid":
                    return Guid.Empty;
                case "System.Object":
                    return null;
                default:
                    // Enumerations default to the first entry
                    if (objType.BaseType != null && objType.BaseType == typeof (Enum)) {
                        Array objEnumValues = Enum.GetValues (objType);
                        Array.Sort (objEnumValues);
                        return Enum.ToObject (objType, objEnumValues.GetValue (0));
                    }

                    // complex object
                    return Activator.CreateInstance (objType);
            }
        }

        public virtual bool IsDBNull (object value)
        {
            return value == DBNull.Value;
        }

        public static object CreateObject (Type objType)
        {
            // Patch as string class does not have parameterless contructor
            if (objType == typeof (string))
                return new string (new char [0]);

            // DateTime is a structure and therefore, for some strange reason, 
            // the default empty constructor is not returned by Type.GetConstructors ().
            if (objType == typeof (DateTime))
                return new DateTime ();

            // may throw exceptions but better here instead of a crash somewhere else because of a returned null
            return Activator.CreateInstance (objType);
        }

        public static DbColumnManager [] GetDBManagers (Type objType)
        {
            List<DbColumnManager> boMembers = new List<DbColumnManager> ();

            foreach (PropertyInfo property in objType.GetProperties ()) {
                foreach (object attrib in property.GetCustomAttributes (true)) {
                    DbColumnAttribute boAttribute = attrib as DbColumnAttribute;
                    if (boAttribute == null)
                        continue;

                    boMembers.Add (new DbColumnManager (boAttribute, property));
                    break;
                }
            }

            return boMembers.ToArray ();
        }

        internal static DbColumnManager [] GetAllManagers (Type objType)
        {
            List<DbColumnManager> boMembers = new List<DbColumnManager> ();

            foreach (PropertyInfo property in objType.GetProperties ()) {
                DbColumnAttribute boAttribute = null;
                foreach (object attrib in property.GetCustomAttributes (true)) {
                    boAttribute = attrib as DbColumnAttribute;
                    if (boAttribute != null)
                        break;
                }
                boMembers.Add (new DbColumnManager (boAttribute, property));
            }

            return boMembers.ToArray ();
        }

        public T FillObject<T> (T obj, IDataReader dr, ref DbColumnManager [] objProperties, bool fixedColumnsOrder = true)
        {
            object value;

            Type objectType = obj.GetType ();
            if (objProperties == null) {
                objProperties = GetDBManagers (objectType);
                if (objProperties.Length > 0)
                    fieldsTable.FindOrdinals (dr, objProperties);
            } else if (!fixedColumnsOrder && objProperties.Length > 0)
                fieldsTable.FindOrdinals (dr, objProperties);

            if (objProperties.Length == 0) {
                value = dr.GetValue (0);

                try {
                    return IsDBNull (value) ?
                        (T) GetDefaultValue (objectType) :
                        (T) Convert.ChangeType (value, objectType);
                } catch (InvalidCastException ex) {
                    throw new Exception (string.Format ("Could not change value {0} to type {1}",
                        value ?? "<null>",
                        objectType.FullName), ex);
                }
            }

            foreach (DbColumnManager objProperty in objProperties) {

                if (!objProperty.CanWrite)
                    continue;

                if (objProperty.DbPosition < 0)
                    continue;

                value = dr.GetValue (objProperty.DbPosition);

                if (IsDBNull (value))
                    value = GetDefaultValue (objProperty.MemberType);

                try {
                    if (@enum.IsAssignableFrom (objProperty.MemberType)) {
                        int result;
                        objProperty.SetValue (obj, Enum.ToObject (objProperty.MemberType, int.TryParse (value.ToString (), out result) ? result : value));
                    } else {
                        if (value == null || objProperty.MemberType.IsInstanceOfType (value))
                            objProperty.SetValue (obj, value);
                        else if (objProperty.MemberType.IsGenericType && objProperty.MemberType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                            value = Convert.ChangeType (value, Nullable.GetUnderlyingType (objProperty.MemberType));
                            objProperty.SetValue (obj, value);
                        } else if (Type.GetTypeCode (objProperty.MemberType) == TypeCode.Object) {
                            value = Activator.CreateInstance (objProperty.MemberType);
                            DbColumnManager [] properties = null;
                            FillObject (value, dr, ref properties);
                            objProperty.SetValue (obj, value);
                        } else
                            objProperty.SetValue (obj, Convert.ChangeType (value, objProperty.MemberType));
                    }
                } catch (InvalidCastException ex) {
                    throw new Exception (string.Format ("Could not set value {0} to property {1} of type {2}",
                        value ?? "<null>",
                        objProperty.Member.Name,
                        objProperty.MemberType.FullName), ex);
                }
            }

            return obj;
        }

        public T ExecuteObject<T> (string commandText, params DbParam [] parameters)
        {
            DbColumnManager [] objProperties = null;

            try {
                using (IDataReader dr = ExecuteReader (commandText, parameters)) {
                    if (dr != null && dr.Read ()) {
                        object obj = CreateObject (typeof (T));
                        return (T) FillObject (obj, dr, ref objProperties);
                    }
                }
            } catch (DbConnectionLostException) {
                if (!DisableConnectionLostErrors)
                    throw;
            }

            return default (T);
        }

        public List<T> ExecuteList<T> (string commandText, params DbParam [] parameters)
        {
            List<T> objCollection = new List<T> ();
            DbColumnManager [] objProperties = null;
            bool finished = false;

            try {
                using (IDataReader dr = ExecuteReader (commandText, parameters)) {
                    while (!finished) {
                        if (dr != null && dr.Read ()) {
                            object obj = CreateObject (typeof (T));
                            objCollection.Add ((T) FillObject (obj, dr, ref objProperties));
                        } else
                            finished = true;
                    }
                }
            } catch (DbConnectionLostException) {
                if (!DisableConnectionLostErrors)
                    throw;
            }

            return objCollection;
        }

        public T [] ExecuteArray<T> (string commandText, params DbParam [] parameters)
        {
            return ExecuteList<T> (commandText, parameters).ToArray ();
        }

        public Dictionary<TKey, TValue> ExecuteDictionary<TKey, TValue> (string commandText, params DbParam [] parameters)
        {
            Dictionary<TKey, TValue> ret = new Dictionary<TKey, TValue> ();
            try {
                using (IDataReader reader = ExecuteReader (commandText, parameters)) {
                    Type keyType = typeof (TKey);
                    Type valType = typeof (TValue);
                    while (reader != null && reader.Read ()) {
                        ret.Add ((TKey) Convert.ChangeType (reader.GetValue (0), keyType), (TValue) Convert.ChangeType (reader.GetValue (1), valType));
                    }
                }
            } catch (DbConnectionLostException) {
                if (!DisableConnectionLostErrors)
                    throw;
            }

            return ret;
        }

        public LazyListModel<T> ExecuteLazyModel<T> (string commandText, params DbParam [] parameters)
        {
            return new LazyListModel<T> (this, true, commandText, parameters);
        }

        public LazyListModel<T> ExecuteLazyModel<T> (bool autoStart, string commandText, params DbParam [] parameters)
        {
            return new LazyListModel<T> (this, autoStart, commandText, parameters);
        }

        #endregion

        #region Execute XML object

        private static T GetObjectFromXML<T> (Mutex fileLock, string fileName)
        {
            try {
                if (fileLock != null)
                    fileLock.WaitOne ();

                if (!File.Exists (fileName))
                    return default (T);

                try {
                    XmlSerializer xs = new XmlSerializer (typeof (T));
                    using (StreamReader sr = File.OpenText (fileName)) {
                        return (T) xs.Deserialize (sr);
                    }
                } catch (XmlException) {
                    return default (T);
                } catch (InvalidOperationException) {
                    return default (T);
                }
            } finally {
                if (fileLock != null)
                    fileLock.ReleaseMutex ();
            }
        }

        public static T [] GetAllObjectsFromXML<T> (Mutex fileLock, string fileName)
        {
            try {
                if (fileLock != null)
                    fileLock.WaitOne ();

                if (!File.Exists (fileName))
                    return new T [0];

                try {
                    XmlSerializer xs = new XmlSerializer (typeof (T []));
                    using (StreamReader sr = File.OpenText (fileName)) {
                        return (T []) xs.Deserialize (sr);
                    }
                } catch (XmlException) {
                    return new T [0];
                } catch (InvalidOperationException) {
                    return new T [0];
                }
            } finally {
                if (fileLock != null)
                    fileLock.ReleaseMutex ();
            }
        }

        public static bool CreateNewObjectId<T> (List<T> allObjects, DbColumnManager [] managers, T newObject, DbField idField)
        {
            long maxId = GetMaxObjectProperty<T, long> (allObjects, idField, managers);
            long id = -1;
            if (maxId < int.MaxValue)
                id = maxId + 1;
            else {
                // This is a very extreme situation that I believe will
                // never happen but we need to be sure we can handle it
                for (int i = 0; i < 10000; i++) {
                    if (FindObjectIndexByField (allObjects, idField, i, managers) < 0)
                        id = i;
                }
            }

            // If this happens juts give up
            if (id == -1)
                return false;

            SetObjectField (newObject, idField, id, managers);

            return true;
        }

        private static void SaveObjectToXML<T> (string fileName, T obj)
        {
            using (Stream fs = new FileStream (fileName, FileMode.Create))
            using (XmlWriter writer = new XmlTextWriter (fs, Encoding.Unicode)) {
                XmlSerializer xs = new XmlSerializer (typeof (T));
                xs.Serialize (writer, obj);
            }
        }

        public static void SaveAllObjectsToXML<T> (string fileName, T [] allObjects)
        {
            using (Stream fs = new FileStream (fileName, FileMode.Create))
            using (XmlWriter writer = new XmlTextWriter (fs, Encoding.Unicode)) {
                XmlSerializer xs = new XmlSerializer (typeof (T []));
                xs.Serialize (writer, allObjects);
            }
        }

        public static int FindObjectIndexByField<T, K> (IList<T> allObjects, DbField field, K criteria, params DbColumnManager [] managers)
        {
            if (managers.Length == 0)
                managers = GetDBManagers (typeof (T));

            DbColumnManager criteriaManager = null;
            foreach (DbColumnManager manager in managers) {
                if (manager.DbField == field) {
                    criteriaManager = manager;
                    break;
                }
            }

            if (criteriaManager == null)
                return -1;

            for (int i = 0; i < allObjects.Count; i++) {
                object prop = criteriaManager.GetValue (allObjects [i]);
                if (prop == null) {
                    if (Equals (criteria, default (K)))
                        return i;

                    continue;
                }

                if (prop.Equals (criteria))
                    return i;
            }

            return -1;
        }

        public static T [] FindObjectsByField<T, K> (IEnumerable<T> allObjects, DbField field, K criteria, params DbColumnManager [] managers)
        {
            if (managers.Length == 0)
                managers = GetDBManagers (typeof (T));

            DbColumnManager criteriaManager = null;
            foreach (DbColumnManager manager in managers) {
                if (manager.DbField == field) {
                    criteriaManager = manager;
                    break;
                }
            }

            if (criteriaManager == null)
                return new T [0];

            List<T> ret = new List<T> ();
            foreach (T obj in allObjects) {
                object prop = criteriaManager.GetValue (obj);
                if (prop == null) {
                    if (Equals (criteria, default (K)))
                        ret.Add (obj);

                    continue;
                }

                if (prop.Equals (criteria)) {
                    ret.Add (obj);
                    continue;
                }

                if (criteriaManager.MemberType == typeof (string)) {
                    if (string.Compare ((string) prop, criteria as string, true) == 0)
                        ret.Add (obj);
                }
            }

            return ret.ToArray ();
        }

        private static K GetMaxObjectProperty<T, K> (IEnumerable<T> allObjects, DbField field, params DbColumnManager [] managers) where K : IComparable
        {
            if (managers.Length == 0)
                managers = GetDBManagers (typeof (T));

            DbColumnManager criteriaManager = null;
            foreach (DbColumnManager manager in managers) {
                if (manager.DbField == field) {
                    criteriaManager = manager;
                    break;
                }
            }

            K ret = default (K);

            if (criteriaManager == null)
                return ret;

            foreach (T obj in allObjects) {
                K val = (K) criteriaManager.GetValue (obj);
                if (val.CompareTo (ret) > 0)
                    ret = val;
            }

            return ret;
        }

        private static void SetObjectField<T> (T obj, DbField field, object value, params DbColumnManager [] managers)
        {
            if (managers.Length == 0)
                managers = GetDBManagers (typeof (T));

            DbColumnManager fieldManager = null;
            foreach (DbColumnManager manager in managers) {
                if (manager.DbField == field) {
                    fieldManager = manager;
                    break;
                }
            }

            if (fieldManager == null)
                return;

            fieldManager.SetValue (obj, value);
        }

        #endregion

        #region Execute other

        public abstract IDataReader ExecuteReader (string commandText, params DbParam [] parameters);

        public object ExecuteScalar (string commandText, params DbParam [] parameters)
        {
            object ret = null;

            try {
                using (IDataReader reader = ExecuteReader (commandText, parameters)) {
                    if (reader != null && reader.Read ())
                        ret = reader.GetValue (0);
                }
            } catch (DbConnectionLostException) {
                if (!DisableConnectionLostErrors)
                    throw;
            }

            return ret;
        }

        public T ExecuteScalar<T> (string commandText, params DbParam [] parameters)
        {
            object ret = null;

            try {
                using (IDataReader reader = ExecuteReader (commandText, parameters)) {
                    if (reader != null && reader.Read ())
                        ret = reader.GetValue (0);
                }
            } catch (DbConnectionLostException) {
                if (!DisableConnectionLostErrors)
                    throw;
            }

            return GetDBValue<T> (ret);
        }

        public T GetDBValue<T> (object value)
        {
            return value == null || IsDBNull (value) ?
                default (T) :
                (T) Convert.ChangeType (value, Nullable.GetUnderlyingType (typeof (T)) ?? typeof (T));
        }

        public int ExecuteNonQuery (string commandText, params DbParam [] parameters)
        {
            int ret = 0;

            try {
                using (IDataReader reader = ExecuteReader (commandText, parameters)) {
                    if (reader != null)
                        ret = reader.RecordsAffected;
                }
            } catch (DbConnectionLostException) {
                if (!DisableConnectionLostErrors)
                    throw;
            }

            return ret;
        }

        public abstract void BulkInsert (string table, string columns, IList<List<DbParam>> insertParams, string errorMessage = "");

        public abstract long GetLastAutoId ();

        #endregion

        #region Helper methods

        public abstract long GetQueryRowsCount (string commandText, params DbParam [] parameters);

        public abstract string GetQueryWithSort (string commandText, string sortKey, DbField sortField, SortDirection direction);

        public abstract string GetQueryWithFilter (string commandText, IList<DbField> filterFields, string filter, out DbParam [] pars);

        public abstract SqlHelper GetSqlHelper ();

        public abstract string GetSelect (IEnumerable<DataField> dataFields, IDictionary<DataField, DataField> aliases = null);

        public abstract SelectBuilder GetSelectBuilder (string select = null);

        public abstract DataQueryResult ExecuteDataQuery (DataQuery querySet, string query, params DbParam [] pars);

        public abstract LazyListModel<T> ExecuteDataQuery<T> (DataQuery querySet, string query, params DbParam [] pars);

        #endregion

        #endregion

        protected void OnCommandExecuted (string commandText, DateTime start, DbParam [] pars, bool isReadOnly)
        {
            EventHandler<DataCommandEventArgs> handler = CommandExecuted;
            if (handler != null)
                handler (this, new DataCommandEventArgs (commandText, start, pars, isReadOnly));
        }

        protected void OnLocationAdded (LocationAddedArgs e)
        {
            EventHandler<LocationAddedArgs> handler = LocationAdded;
            if (handler != null)
                handler (this, e);
        }

        private void DataProvider_CommandExecuted (object sender, DataCommandEventArgs args)
        {
            try {
                string errLog = LogFile;
                if (string.IsNullOrEmpty (errLog))
                    return;

                TimeSpan duration = DateTime.Now - args.StartAt;
                StringBuilder message = new StringBuilder ();
                message.AppendFormat ("{0} - {1} -> \"{2}\"", args.StartAt, duration.TotalSeconds, args.Command);
                foreach (DbParam par in args.Parameters) {
                    if (message.Length == 0)
                        message.Append (" params: ");
                    else
                        message.Append (", ");

                    if (par.Value != null)
                        message.AppendFormat ("{0}='{1}'", fieldsTable.GetParameterName (par.ParameterName), par.Value);
                    else
                        message.AppendFormat ("{0}=null", fieldsTable.GetParameterName (par.ParameterName));
                }
                message.AppendLine ();

                File.AppendAllText (errLog, message.ToString ());
            } catch {
            }
        }

        #region IDisposable Members

        public virtual void Dispose ()
        {
            Disconnect ();
        }

        #endregion

        public static string GetNextGroupCode (string code)
        {
            if (string.IsNullOrEmpty (code) || code.Length != 3)
                return "AAA";

            StringBuilder ret = new StringBuilder ();
            if (code [2] < 'Z') {
                ret.Append (code [0]);
                ret.Append (code [1]);
                ret.Append ((char) (code [2] + 1));
            } else if (code [1] < 'Z') {
                ret.Append (code [0]);
                ret.Append ((char) (code [1] + 1));
                ret.Append ('A');
            } else {
                ret.Append ((char) (code [0] + 1));
                ret.Append ('A');
                ret.Append ('A');
            }

            return ret.ToString ();
        }

        protected static string GetOperationsPriceSignQuery (string operationsTable = "operations")
        {
            return string.Format (@"(CASE WHEN {0}.OperType in ({1},{2},{3},{4},{5},{6}) THEN -1 WHEN {0}.OperType in ({7},{8},{9},{10},{11},{12},{13},{14}) THEN 0 ELSE 1 END)",
                operationsTable,
                (int) OperationType.Sale,
                (int) OperationType.Consignment,
                (int) OperationType.DebitNote,
                (int) OperationType.PurchaseReturn,
                (int) OperationType.WriteOff,
                (int) OperationType.Waste,
                (int) OperationType.Offer,
                (int) OperationType.ProformaInvoice,
                (int) OperationType.PurchaseOrder,
                (int) OperationType.SalesOrder,
                (int) OperationType.ComplexRecipeMaterial,
                (int) OperationType.ComplexRecipeProduct,
                (int) OperationType.RestaurantOrder,
                (int) OperationType.ConsignmentSale);
        }
    }
}
