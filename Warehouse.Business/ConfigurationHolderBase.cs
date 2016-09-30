//
// ConfigurationHolderBase.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   06/26/2006
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

using System.Collections.Generic;
using Warehouse.Data;

namespace Warehouse.Business
{
    public enum AskDialogState
    {
        NotSaved = -1,
        No = 0,
        Yes = 1
    }

    public abstract class ConfigurationHolderBase : ConfigurationProvider
    {
        private const string DOCUMENT_NUMBERS_PER_LOCATION_KEY = "configkey48";
        private const string MAXIMUM_QUANTITY_KEY = "configkey63";
        private const string MAXIMUM_QUANTITY_WARNING_KEY = "configkey64";
        private const string USE_SALES_TAX_INSTEAD_OF_VAT = "configkey65";
        private const string DEFAULT_BANK_NOTES_AND_COINS = "0.01;0.02;0.05;0.1;0.2;0.5;1;2;5;10;20;50;100;200;500;1000;2000;5000";
        private static readonly double [] allowedBankNotesAndCoins = { 0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000, 50000, 100000, 200000, 500000 };

        #region DataBase configuration

        private string dbProvider;
        [ConfigurationMember ("Warehouse.Data.MySQL.DataProvider", Encrypted = true)]
        public string DbProvider
        {
            get { return dbProvider; }
            set { SetClassConfig (() => DbProvider, ref dbProvider, value); }
        }

        private string dbServer;
        [ConfigurationMember ("localhost")]
        public string DbServer
        {
            get { return dbServer; }
            set { SetClassConfig (() => DbServer, ref dbServer, value); }
        }

        private string dbSlaveServer;
        [ConfigurationMember ("")]
        public string DbSlaveServer
        {
            get { return dbSlaveServer; }
            set { SetClassConfig (() => DbSlaveServer, ref dbSlaveServer, value); }
        }

        private string dbUser;
        [ConfigurationMember ("root")]
        public string DbUser
        {
            get { return dbUser; }
            set { SetClassConfig (() => DbUser, ref dbUser, value); }
        }

        private string dbPassword;
        [ConfigurationMember ("root", Encrypted = true)]
        public string DbPassword
        {
            get { return dbPassword; }
            set { SetClassConfig (() => DbPassword, ref dbPassword, value); }
        }

        private string dbDatabase;
        [ConfigurationMember ("")]
        public string DbDatabase
        {
            get { return dbDatabase; }
            set { SetClassConfig (() => DbDatabase, ref dbDatabase, value); }
        }

        private int dbConnectTimeout;
        [ConfigurationMember ("20")]
        public int DbConnectTimeout
        {
            get { return dbConnectTimeout; }
            set { SetValueConfig (() => DbConnectTimeout, ref dbConnectTimeout, value); }
        }

        private int dbCommandTimeout;
        [ConfigurationMember ("100")]
        public int DbCommandTimeout
        {
            get { return dbCommandTimeout; }
            set { SetValueConfig (() => DbCommandTimeout, ref dbCommandTimeout, value); }
        }

        private long lastLoggedUID;
        [ConfigurationMember ("-1")]
        public long LastLoggedUID
        {
            get { return lastLoggedUID; }
            set { SetValueConfig (() => LastLoggedUID, ref lastLoggedUID, value); }
        }

        private bool checkDbVersion;
        [ConfigurationMember ("true")]
        public bool CheckDbVersion
        {
            get { return checkDbVersion; }
            set { SetValueConfig (() => CheckDbVersion, ref checkDbVersion, value); }
        }

        private string dbLogFile;
        [ConfigurationMember ("")]
        public string DbLogFile
        {
            get { return dbLogFile; }
            set { SetClassConfig (() => DbLogFile, ref dbLogFile, value); }
        }

        private bool allowDbChange;
        [ConfigurationMember ("true")]
        public bool AllowDbChange
        {
            get { return allowDbChange; }
            set { SetValueConfig (() => AllowDbChange, ref allowDbChange, value); }
        }

        #endregion

        #region Visual configuration

        private int currencyPrecision;
        [ConfigurationMember ("2")]
        public int CurrencyPrecision
        {
            get { return currencyPrecision; }
            set { SetValueConfig (() => CurrencyPrecision, ref currencyPrecision, value); }
        }

        private string currencySymbol;
        [ConfigurationMember ("")]
        public string CurrencySymbol
        {
            get { return currencySymbol; }
            set { SetClassConfig (() => CurrencySymbol, ref currencySymbol, value); }
        }

        private int currencySymbolPosition;
        [ConfigurationMember ("-1")]
        public int CurrencySymbolPosition
        {
            get { return currencySymbolPosition; }
            set { SetValueConfig (() => CurrencySymbolPosition, ref currencySymbolPosition, value); }
        }

        private bool usePurchaseCurrencyPrecision;
        [ConfigurationMember ("false")]
        public bool UsePurchaseCurrencyPrecision
        {
            get { return usePurchaseCurrencyPrecision; }
            set { SetValueConfig (() => UsePurchaseCurrencyPrecision, ref usePurchaseCurrencyPrecision, value); }
        }

        private int purchaseCurrencyPrecision;
        [ConfigurationMember ("2")]
        public int PurchaseCurrencyPrecision
        {
            get { return usePurchaseCurrencyPrecision ? purchaseCurrencyPrecision : currencyPrecision; }
            set { SetValueConfig (() => PurchaseCurrencyPrecision, ref purchaseCurrencyPrecision, value); }
        }

        private int quantityPrecision;
        [ConfigurationMember ("3")]
        public int QuantityPrecision
        {
            get { return quantityPrecision; }
            set { SetValueConfig (() => QuantityPrecision, ref quantityPrecision, value); }
        }

        private int percentPrecision;
        [ConfigurationMember ("2")]
        public int PercentPrecision
        {
            get { return percentPrecision; }
            set { SetValueConfig (() => PercentPrecision, ref percentPrecision, value); }
        }

        private string dateFormat;
        [ConfigurationMember ("", Encrypted = true)]
        public string DateFormat
        {
            get { return dateFormat; }
            set { SetClassConfig (() => DateFormat, ref dateFormat, value); }
        }

        private int documentNumberLength;
        [ConfigurationMember ("10")]
        public int DocumentNumberLength
        {
            get { return documentNumberLength; }
            set { SetValueConfig (() => DocumentNumberLength, ref documentNumberLength, value); }
        }

        private bool showSplashScreen;
        [ConfigurationMember ("true")]
        public bool ShowSplashScreen
        {
            get { return showSplashScreen; }
            set { SetValueConfig (() => ShowSplashScreen, ref showSplashScreen, value); }
        }

        private bool alwaysChoosePartnerInTradePoint;
        [ConfigurationMember ("false")]
        public bool AlwaysChoosePartnerInTradePoint
        {
            get { return alwaysChoosePartnerInTradePoint; }
            set { SetValueConfig (() => AlwaysChoosePartnerInTradePoint, ref alwaysChoosePartnerInTradePoint, value); }
        }

        private AskDialogState askBeforeDocumentPrint;
        [ConfigurationMember ("NotSaved")]
        public AskDialogState AskBeforeDocumentPrint
        {
            get { return askBeforeDocumentPrint; }
            set { SetValueConfig (() => AskBeforeDocumentPrint, ref askBeforeDocumentPrint, value); }
        }

        private bool invoicePrintOriginal;
        [ConfigurationMember ("true")]
        public bool InvoicePrintOriginal
        {
            get { return invoicePrintOriginal; }
            set { SetValueConfig (() => InvoicePrintOriginal, ref invoicePrintOriginal, value); }
        }

        private bool invoicePrintCopy;
        [ConfigurationMember ("false")]
        public bool InvoicePrintCopy
        {
            get { return invoicePrintCopy; }
            set { SetValueConfig (() => InvoicePrintCopy, ref invoicePrintCopy, value); }
        }

        private int invoicePrintCopyNumber;
        [ConfigurationMember ("1")]
        public int InvoicePrintCopyNumber
        {
            get { return invoicePrintCopyNumber; }
            set { SetValueConfig (() => InvoicePrintCopyNumber, ref invoicePrintCopyNumber, value); }
        }

        private bool invoicePrintInternational;
        [ConfigurationMember ("false")]
        public bool InvoicePrintInternational
        {
            get { return invoicePrintInternational; }
            set { SetValueConfig (() => InvoicePrintInternational, ref invoicePrintInternational, value); }
        }

        private bool invoicePrintFiscal;
        [ConfigurationMember ("false")]
        public bool InvoicePrintFiscal
        {
            get { return invoicePrintFiscal; }
            set { SetValueConfig (() => InvoicePrintFiscal, ref invoicePrintFiscal, value); }
        }

        private bool extDisplayDigitsOnly;
        [ConfigurationMember ("false")]
        public bool ExtDisplayDigitsOnly
        {
            get { return extDisplayDigitsOnly; }
            set { SetValueConfig (() => ExtDisplayDigitsOnly, ref extDisplayDigitsOnly, value); }
        }

        private bool showItemsGroups;
        [ConfigurationMember ("false")]
        public bool ShowItemsGroups
        {
            get { return showItemsGroups; }
            set { SetValueConfig (() => ShowItemsGroups, ref showItemsGroups, value); }
        }

        private bool showUsersGroups;
        [ConfigurationMember ("false")]
        public bool ShowUsersGroups
        {
            get { return showUsersGroups; }
            set { SetValueConfig (() => ShowUsersGroups, ref showUsersGroups, value); }
        }

        private bool showPartnersGroups;
        [ConfigurationMember ("false")]
        public bool ShowPartnersGroups
        {
            get { return showPartnersGroups; }
            set { SetValueConfig (() => ShowPartnersGroups, ref showPartnersGroups, value); }
        }

        private bool showLocationsGroups;
        [ConfigurationMember ("false")]
        public bool ShowLocationsGroups
        {
            get { return showLocationsGroups; }
            set { SetValueConfig (() => ShowLocationsGroups, ref showLocationsGroups, value); }
        }

        private bool showDevicesGroups;
        [ConfigurationMember ("false")]
        public bool ShowDevicesGroups
        {
            get { return showDevicesGroups; }
            set { SetValueConfig (() => ShowDevicesGroups, ref showDevicesGroups, value); }
        }

        private long lastItemsGroupId;
        [ConfigurationMember ("-1")]
        public long LastItemsGroupId
        {
            get { return lastItemsGroupId; }
            set { SetValueConfig (() => LastItemsGroupId, ref lastItemsGroupId, value); }
        }

        private long lastUsersGroupId;
        [ConfigurationMember ("-1")]
        public long LastUsersGroupId
        {
            get { return lastUsersGroupId; }
            set { SetValueConfig (() => LastUsersGroupId, ref lastUsersGroupId, value); }
        }

        private long lastPartnersGroupId;
        [ConfigurationMember ("-1")]
        public long LastPartnersGroupId
        {
            get { return lastPartnersGroupId; }
            set { SetValueConfig (() => LastPartnersGroupId, ref lastPartnersGroupId, value); }
        }

        private long lastLocationsGroupId;
        [ConfigurationMember ("-1")]
        public long LastLocationsGroupId
        {
            get { return lastLocationsGroupId; }
            set { SetValueConfig (() => LastLocationsGroupId, ref lastLocationsGroupId, value); }
        }

        private long lastDevicesGroupId;
        [ConfigurationMember ("-1")]
        public long LastDevicesGroupId
        {
            get { return lastDevicesGroupId; }
            set { SetValueConfig (() => LastDevicesGroupId, ref lastDevicesGroupId, value); }
        }

        private bool showProductionsPreview;
        [ConfigurationMember ("false")]
        public bool ShowProductionsPreview
        {
            get { return showProductionsPreview; }
            set { SetValueConfig (() => ShowProductionsPreview, ref showProductionsPreview, value); }
        }

        private bool showPurchasesPreview;
        [ConfigurationMember ("false")]
        public bool ShowPurchasesPreview
        {
            get { return showPurchasesPreview; }
            set { SetValueConfig (() => ShowPurchasesPreview, ref showPurchasesPreview, value); }
        }

        private bool showSalesPreview;
        [ConfigurationMember ("false")]
        public bool ShowSalesPreview
        {
            get { return showSalesPreview; }
            set { SetValueConfig (() => ShowSalesPreview, ref showSalesPreview, value); }
        }

        private bool showInvoicesPreview;
        [ConfigurationMember ("false")]
        public bool ShowInvoicesPreview
        {
            get { return showInvoicesPreview; }
            set { SetValueConfig (() => ShowInvoicesPreview, ref showInvoicesPreview, value); }
        }

        private bool showStockTakingsPreview;
        [ConfigurationMember ("false")]
        public bool ShowStockTakingsPreview
        {
            get { return showStockTakingsPreview; }
            set { SetValueConfig (() => ShowStockTakingsPreview, ref showStockTakingsPreview, value); }
        }

        private bool showTransfersPreview;
        [ConfigurationMember ("false")]
        public bool ShowTransfersPreview
        {
            get { return showTransfersPreview; }
            set { SetValueConfig (() => ShowTransfersPreview, ref showTransfersPreview, value); }
        }

        private bool showWastesPreview;
        [ConfigurationMember ("false")]
        public bool ShowWastesPreview
        {
            get { return showWastesPreview; }
            set { SetValueConfig (() => ShowWastesPreview, ref showWastesPreview, value); }
        }

        #endregion

        #region Operations configuration

        public bool ItemsManagementUseLots
        {
            get
            {
                return itemsManagementType != ItemsManagementType.AveragePrice &&
                    itemsManagementType != ItemsManagementType.LastPurchasePrice;
            }
        }

        private ItemsManagementType itemsManagementType;
        public ItemsManagementType ItemsManagementType
        {
            get
            {
                return itemsManagementType == ItemsManagementType.AveragePrice && quickAveragePurchasePriceRecalculation ?
                    ItemsManagementType.QuickAveragePrice : itemsManagementType;
            }
            set
            {
                if (value == ItemsManagementType.QuickAveragePrice) {
                    QuickAveragePurchasePriceRecalculation = true;
                    value = ItemsManagementType.AveragePrice;
                } else
                    QuickAveragePurchasePriceRecalculation = false;

                SetValueConfig (() => ItemsManagementType, ref itemsManagementType, value);
            }
        }

        [ConfigurationMember ("0", DbKey = "configkey23")]
        public int ItemsManagementTypeDb
        {
            get { return (int) itemsManagementType; }
            set { itemsManagementType = (ItemsManagementType) value; }
        }

        private bool quickAveragePurchasePriceRecalculation;
        [ConfigurationMember ("false", DbKey = "QuickAPPCalc")]
        public bool QuickAveragePurchasePriceRecalculation
        {
            get { return quickAveragePurchasePriceRecalculation; }
            set { SetValueConfig (() => QuickAveragePurchasePriceRecalculation, ref quickAveragePurchasePriceRecalculation, value); }
        }

        private bool enableLineNumber;
        [ConfigurationMember ("false")]
        public bool EnableLineNumber
        {
            get { return enableLineNumber; }
            set { SetValueConfig (() => EnableLineNumber, ref enableLineNumber, value); }
        }

        private bool enableItemCode;
        [ConfigurationMember ("false")]
        public bool EnableItemCode
        {
            get { return enableItemCode; }
            set { SetValueConfig (() => EnableItemCode, ref enableItemCode, value); }
        }

        private bool allowPercentDiscounts;
        [ConfigurationMember ("true")]
        public bool AllowPercentDiscounts
        {
            get { return allowPercentDiscounts; }
            set { SetValueConfig (() => AllowPercentDiscounts, ref allowPercentDiscounts, value); }
        }

        private bool allowValueDiscounts;
        [ConfigurationMember ("false")]
        public bool AllowValueDiscounts
        {
            get { return allowValueDiscounts; }
            set { SetValueConfig (() => AllowValueDiscounts, ref allowValueDiscounts, value); }
        }

        public bool AllowItemLotName
        {
            get { return enableItemLotName && ItemsManagementUseLots; }
        }

        private bool enableItemLotName;
        [ConfigurationMember ("false")]
        public bool EnableItemLotName
        {
            get { return enableItemLotName; }
            set { SetValueConfig (() => EnableItemLotName, ref enableItemLotName, value); }
        }

        public bool AllowItemSerialNumber
        {
            get { return enableItemSerialNumber && ItemsManagementUseLots; }
        }

        private bool enableItemSerialNumber;
        [ConfigurationMember ("false")]
        public bool EnableItemSerialNumber
        {
            get { return enableItemSerialNumber; }
            set { SetValueConfig (() => EnableItemSerialNumber, ref enableItemSerialNumber, value); }
        }

        public bool AllowItemExpirationDate
        {
            get { return enableItemExpirationDate && ItemsManagementUseLots; }
        }

        private bool enableItemExpirationDate;
        [ConfigurationMember ("false")]
        public bool EnableItemExpirationDate
        {
            get { return enableItemExpirationDate; }
            set { SetValueConfig (() => EnableItemExpirationDate, ref enableItemExpirationDate, value); }
        }

        public bool AllowItemManufacturedDate
        {
            get { return enableItemManufacturedDate && ItemsManagementUseLots; }
        }

        private bool enableItemManufacturedDate;
        [ConfigurationMember ("false")]
        public bool EnableItemManufacturedDate
        {
            get { return enableItemManufacturedDate; }
            set { SetValueConfig (() => EnableItemManufacturedDate, ref enableItemManufacturedDate, value); }
        }

        public bool AllowItemLocation
        {
            get { return enableItemLocation && ItemsManagementUseLots; }
        }

        private bool enableItemLocation;
        [ConfigurationMember ("false")]
        public bool EnableItemLocation
        {
            get { return enableItemLocation; }
            set { SetValueConfig (() => EnableItemLocation, ref enableItemLocation, value); }
        }

        private bool enableItemVatRate;
        [ConfigurationMember ("false")]
        public bool EnableItemVatRate
        {
            get { return enableItemVatRate; }
            set { SetValueConfig (() => EnableItemVatRate, ref enableItemVatRate, value); }
        }

        private bool allowNegativeAvailability;
        public bool AllowNegativeAvailability
        {
            get { return allowNegativeAvailability; }
            set { SetValueConfig (() => AllowNegativeAvailability, ref allowNegativeAvailability, value); }
        }

        [ConfigurationMember ("-1", DbKey = "configkey12")]
        public int AllowNegativeAvailabilityDb
        {
            get { return allowNegativeAvailability ? -1 : 0; }
            set { allowNegativeAvailability = value == -1; }
        }

        private bool vatIncluded;
        public bool VATIncluded
        {
            get { return vatIncluded; }
            set { SetValueConfig (() => VATIncluded, ref vatIncluded, value); }
        }

        [ConfigurationMember ("0", DbKey = "configkey20")]
        public int VATIncludedDb
        {
            get { return vatIncluded ? -1 : 0; }
            set { vatIncluded = value == -1; }
        }

        private bool roundedPrices;
        [ConfigurationMember ("false")]
        public bool RoundedPrices
        {
            get { return roundedPrices; }
            set { SetValueConfig (() => RoundedPrices, ref roundedPrices, value); }
        }

        private bool warnPricesSaleLowerThanPurchase;
        [ConfigurationMember ("true")]
        public bool WarnPricesSaleLowerThanPurchase
        {
            get { return warnPricesSaleLowerThanPurchase; }
            set { SetValueConfig (() => WarnPricesSaleLowerThanPurchase, ref warnPricesSaleLowerThanPurchase, value); }
        }

        private bool autoProduction;
        public bool AutoProduction
        {
            get { return autoProduction; }
            set { SetValueConfig (() => AutoProduction, ref autoProduction, value); }
        }

        [ConfigurationMember ("0", DbKey = "configkey1")]
        public int AutoProductionDb
        {
            get { return autoProduction ? -1 : 0; }
            set { autoProduction = value == -1; }
        }

        private bool autoCreateInvoiceOnSale;
        [ConfigurationMember ("false", DbKey = "AutoCreateInvoiceOnSale", MigrateToDb = true)]
        public bool AutoCreateInvoiceOnSale
        {
            get { return autoCreateInvoiceOnSale; }
            set { SetValueConfig (() => AutoCreateInvoiceOnSale, ref autoCreateInvoiceOnSale, value); }
        }

        private bool autoCreateInvoiceOnPurchase;
        [ConfigurationMember ("false", DbKey = "AutoCreateInvoiceOnPurchase", MigrateToDb = true)]
        public bool AutoCreateInvoiceOnPurchase
        {
            get { return autoCreateInvoiceOnPurchase; }
            set { SetValueConfig (() => AutoCreateInvoiceOnPurchase, ref autoCreateInvoiceOnPurchase, value); }
        }

        private double maximumAllowedQuantity;
        [ConfigurationMember ("0", DbKey = MAXIMUM_QUANTITY_KEY)]
        public double MaximumAllowedQuantity
        {
            get { return maximumAllowedQuantity; }
            set { SetValueConfig (() => MaximumAllowedQuantity, ref maximumAllowedQuantity, value); }
        }

        private double warningMaximumQuantity;
        [ConfigurationMember ("100000", DbKey = MAXIMUM_QUANTITY_WARNING_KEY)]
        public double WarningMaximumQuantity
        {
            get { return warningMaximumQuantity; }
            set { SetValueConfig (() => WarningMaximumQuantity, ref warningMaximumQuantity, value); }
        }

        private bool logAllChangesInOperations;
        [ConfigurationMember ("false")]
        public bool LogAllChangesInOperations
        {
            get { return logAllChangesInOperations; }
            set { SetValueConfig (() => LogAllChangesInOperations, ref logAllChangesInOperations, value); }
        }

        private bool limitDocumentNumber;
        [ConfigurationMember ("true")]
        public bool LimitDocumentNumber
        {
            get { return limitDocumentNumber; }
            set { SetValueConfig (() => LimitDocumentNumber, ref limitDocumentNumber, value); }
        }

        #endregion

        #region Barcodes configuration

        private GeneratedBarcodeType generatedBarcodeType;
        [ConfigurationMember ("2")]
        public GeneratedBarcodeType GeneratedBarcodeType
        {
            get { return generatedBarcodeType; }
            set { SetValueConfig (() => GeneratedBarcodeType, ref generatedBarcodeType, value); }
        }

        private string generatedBarcodePrefix;
        [ConfigurationMember ("20")]
        public string GeneratedBarcodePrefix
        {
            get { return generatedBarcodePrefix; }
            set { SetClassConfig (() => GeneratedBarcodePrefix, ref generatedBarcodePrefix, value); }
        }

        private GeneratedBarcodeType customGeneratedBarcodeType;
        [ConfigurationMember ("2")]
        public GeneratedBarcodeType CustomGeneratedBarcodeType
        {
            get { return customGeneratedBarcodeType; }
            set { SetValueConfig (() => CustomGeneratedBarcodeType, ref customGeneratedBarcodeType, value); }
        }

        private string customGeneratedBarcodFormat;
        [ConfigurationMember ("20#####WW.WWWC")]
        public string CustomGeneratedBarcodeFormat
        {
            get { return customGeneratedBarcodFormat; }
            set { SetClassConfig (() => CustomGeneratedBarcodeFormat, ref customGeneratedBarcodFormat, value); }
        }

        #endregion

        #region E-mail configuration

        private string smtpServer;
        [ConfigurationMember ("")]
        public string SmtpServer
        {
            get { return smtpServer; }
            set { SetClassConfig (() => SmtpServer, ref smtpServer, value); }
        }

        private int smtpPort;
        [ConfigurationMember ("25")]
        public int SmtpPort
        {
            get { return smtpPort; }
            set { SetValueConfig (() => SmtpPort, ref smtpPort, value); }
        }

        private string emailSender;
        [ConfigurationMember ("")]
        public string EmailSender
        {
            get { return emailSender; }
            set { SetClassConfig (() => EmailSender, ref emailSender, value); }
        }

        private string emailSubject;
        [ConfigurationMember ("Warehouse Price Rules")]
        public string EmailSubject
        {
            get { return emailSubject; }
            set { SetClassConfig (() => EmailSubject, ref emailSubject, value); }
        }

        private bool smtpUseSsl;
        [ConfigurationMember ("false")]
        public bool SmtpUseSsl
        {
            get { return smtpUseSsl; }
            set { SetValueConfig (() => SmtpUseSsl, ref smtpUseSsl, value); }
        }

        private string smtpUserName;
        [ConfigurationMember ("")]
        public string SmtpUserName
        {
            get { return smtpUserName; }
            set { SetClassConfig (() => SmtpUserName, ref smtpUserName, value); }
        }

        private string smtpPassword;
        [ConfigurationMember ("", Encrypted = true)]
        public string SmtpPassword
        {
            get { return smtpPassword; }
            set { SetClassConfig (() => SmtpPassword, ref smtpPassword, value); }
        }

        private string lastExportEmail;
        [ConfigurationMember ("", Encrypted = true)]
        public string LastExportEmail
        {
            get { return lastExportEmail; }
            set { SetClassConfig (() => LastExportEmail, ref lastExportEmail, value); }
        }

        private string lastExportEmailSubject;
        [ConfigurationMember ("")]
        public string LastExportEmailSubject
        {
            get { return lastExportEmailSubject; }
            set { SetClassConfig (() => LastExportEmailSubject, ref lastExportEmailSubject, value); }
        }

        #endregion

        #region Localization configuration

        private string localePackageName;
        [ConfigurationMember ("Warehouse")]
        public string LocalePackageName
        {
            get { return localePackageName; }
            set { SetClassConfig (() => LocalePackageName, ref localePackageName, value); }
        }

        private bool useSystemLocalization;
        [ConfigurationMember ("true")]
        public bool UseSystemLocalization
        {
            get { return useSystemLocalization; }
            set
            {
                if (SetValueConfig (() => UseSystemLocalization, ref useSystemLocalization, value))
                    DataHelper.DefaultDocumentsFont = null;
            }
        }

        private string localization;
        [ConfigurationMember ("")]
        public string Localization
        {
            get { return localization; }
            set
            {
                if (SetClassConfig (() => Localization, ref localization, value))
                    DataHelper.DefaultDocumentsFont = null;
            }
        }

        private string documentTemplatesFolder;
        [ConfigurationMember ("")]
        public string DocumentTemplatesFolder
        {
            get { return documentTemplatesFolder; }
            set { SetClassConfig (() => DocumentTemplatesFolder, ref documentTemplatesFolder, value); }
        }

        #endregion

        #region Error logging configuration

        private bool verboseErrorLogging;
        [ConfigurationMember ("false")]
        public bool VerboseErrorLogging
        {
            get { return verboseErrorLogging; }
            set { SetValueConfig (() => VerboseErrorLogging, ref verboseErrorLogging, value); }
        }

        private string errorLogFileName;
        [ConfigurationMember ("errlog.xml")]
        public string ErrorLogFileName
        {
            get { return errorLogFileName; }
            set { SetClassConfig (() => ErrorLogFileName, ref errorLogFileName, value); }
        }

        private int errorLogMaxFileSize;
        [ConfigurationMember ("1000000")]
        public int ErrorLogMaxFileSize
        {
            get { return errorLogMaxFileSize; }
            set { SetValueConfig (() => ErrorLogMaxFileSize, ref errorLogMaxFileSize, value); }
        }

        #endregion

        protected bool useDefaultDocumentPrinter;
        [ConfigurationMember ("true")]
        public virtual bool UseDefaultDocumentPrinter
        {
            get { return useDefaultDocumentPrinter; }
            set { SetValueConfig (() => UseDefaultDocumentPrinter, ref useDefaultDocumentPrinter, value); }
        }

        protected string documentPrinterName;
        [ConfigurationMember ("")]
        public string DocumentPrinterName
        {
            get { return documentPrinterName; }
            set { SetClassConfig (() => DocumentPrinterName, ref documentPrinterName, value); }
        }

        #region Receipt printing configuration

        private bool printSaleCode;
        [ConfigurationMember ("true")]
        public bool PrintSaleCode
        {
            get { return printSaleCode; }
            set { SetValueConfig (() => PrintSaleCode, ref printSaleCode, value); }
        }

        private bool printOrderCodeOnReceipts;
        [ConfigurationMember ("false")]
        public bool PrintOrderCodeOnReceipts
        {
            get { return printOrderCodeOnReceipts; }
            set { SetValueConfig (() => PrintOrderCodeOnReceipts, ref printOrderCodeOnReceipts, value); }
        }

        private bool printLocationOnReceipts;
        [ConfigurationMember ("true")]
        public bool PrintLocationOnReceipts
        {
            get { return printLocationOnReceipts; }
            set { SetValueConfig (() => PrintLocationOnReceipts, ref printLocationOnReceipts, value); }
        }

        private bool printPartnerOnReceipts;
        [ConfigurationMember ("false")]
        public bool PrintPartnerOnReceipts
        {
            get { return printPartnerOnReceipts; }
            set { SetValueConfig (() => PrintPartnerOnReceipts, ref printPartnerOnReceipts, value); }
        }

        private bool printOperatorOnReceipts;
        [ConfigurationMember ("true")]
        public bool PrintOperatorOnReceipts
        {
            get { return printOperatorOnReceipts; }
            set { SetValueConfig (() => PrintOperatorOnReceipts, ref printOperatorOnReceipts, value); }
        }

        private bool printSaleBarCode;
        [ConfigurationMember ("false")]
        public bool PrintSaleBarCode
        {
            get { return printSaleBarCode; }
            set { SetValueConfig (() => PrintSaleBarCode, ref printSaleBarCode, value); }
        }

        private GeneratedBarcodeType saleBarCodeType;
        [ConfigurationMember ("EAN13")]
        public GeneratedBarcodeType SaleBarCodeType
        {
            get { return saleBarCodeType; }
            set { SetValueConfig (() => SaleBarCodeType, ref saleBarCodeType, value); }
        }

        private bool printSaleBarCodeNumber;
        [ConfigurationMember ("true")]
        public bool PrintSaleBarCodeNumber
        {
            get { return printSaleBarCodeNumber; }
            set { SetValueConfig (() => PrintSaleBarCodeNumber, ref printSaleBarCodeNumber, value); }
        }

        #endregion

        #region Other configuration

        private bool registerCashAtStartup;
        [ConfigurationMember ("false")]
        public bool RegisterCashAtStartup
        {
            get { return registerCashAtStartup; }
            set { SetValueConfig (() => RegisterCashAtStartup, ref registerCashAtStartup, value); }
        }

        private string startupPageClass;
        [ConfigurationMember ("", Encrypted = true)]
        public string StartupPageClass
        {
            get { return startupPageClass; }
            set { SetClassConfig (() => StartupPageClass, ref startupPageClass, value); }
        }

        private bool cashReceiptPrinterEnabled;
        public bool CashReceiptPrinterEnabled
        {
            get { return cashReceiptPrinterEnabled; }
            set { SetValueConfig (() => CashReceiptPrinterEnabled, ref cashReceiptPrinterEnabled, value); }
        }

        private bool customerOrdersPrinterEnabled;
        public bool CustomerOrdersPrinterEnabled
        {
            get { return customerOrdersPrinterEnabled; }
            set { SetValueConfig (() => CustomerOrdersPrinterEnabled, ref customerOrdersPrinterEnabled, value); }
        }

        private bool externalDisplayEnabled;
        public bool ExternalDisplayEnabled
        {
            get { return externalDisplayEnabled; }
            set { SetValueConfig (() => ExternalDisplayEnabled, ref externalDisplayEnabled, value); }
        }

        private bool cardReaderEnabled;
        public bool CardReaderEnabled
        {
            get { return cardReaderEnabled; }
            set { SetValueConfig (() => CardReaderEnabled, ref cardReaderEnabled, value); }
        }

        private bool electronicScaleEnabled;
        public bool ElectronicScaleEnabled
        {
            get { return electronicScaleEnabled; }
            set { SetValueConfig (() => ElectronicScaleEnabled, ref electronicScaleEnabled, value); }
        }

        private bool salesDataControllerEnabled;
        public bool SalesDataControllerEnabled
        {
            get { return salesDataControllerEnabled; }
            set { SetValueConfig (() => SalesDataControllerEnabled, ref salesDataControllerEnabled, value); }
        }

        private bool barcodeScannerEnabled;
        public bool BarcodeScannerEnabled
        {
            get { return barcodeScannerEnabled; }
            set { SetValueConfig (() => BarcodeScannerEnabled, ref barcodeScannerEnabled, value); }
        }

        private bool localizationSet;
        [ConfigurationMember ("false")]
        public bool LocalizationSet
        {
            get { return localizationSet; }
            set { SetValueConfig (() => LocalizationSet, ref localizationSet, value); }
        }

        private bool appSetupFinished;
        [ConfigurationMember ("false")]
        public bool AppSetupFinished
        {
            get { return appSetupFinished; }
            set { SetValueConfig (() => AppSetupFinished, ref appSetupFinished, value); }
        }

        private string lastExporter;
        [ConfigurationMember ("", Encrypted = true)]
        public string LastExporter
        {
            get { return lastExporter; }
            set { SetClassConfig (() => LastExporter, ref lastExporter, value); }
        }

        private string lastExportFolder;
        [ConfigurationMember ("")]
        public string LastExportFolder
        {
            get { return lastExportFolder; }
            set { SetClassConfig (() => LastExportFolder, ref lastExportFolder, value); }
        }

        private string lastDocsExporter;
        [ConfigurationMember ("", Encrypted = true)]
        public string LastDocsExporter
        {
            get { return lastDocsExporter; }
            set { SetClassConfig (() => LastDocsExporter, ref lastDocsExporter, value); }
        }

        private string lastExportDocsFolder;
        [ConfigurationMember ("")]
        public string LastExportDocsFolder
        {
            get { return lastExportDocsFolder; }
            set { SetClassConfig (() => LastExportDocsFolder, ref lastExportDocsFolder, value); }
        }

        private string lastImporter;
        [ConfigurationMember ("", Encrypted = true)]
        public string LastImporter
        {
            get { return lastImporter; }
            set { SetClassConfig (() => LastImporter, ref lastImporter, value); }
        }

        private string lastImportFolder;
        [ConfigurationMember ("")]
        public string LastImportFolder
        {
            get { return lastImportFolder; }
            set { SetClassConfig (() => LastImportFolder, ref lastImportFolder, value); }
        }

        private string lastSettingsBackupAt;
        [ConfigurationMember ("")]
        public string LastSettingsBackupAt
        {
            get { return lastSettingsBackupAt; }
            set { SetClassConfig (() => LastSettingsBackupAt, ref lastSettingsBackupAt, value); }
        }

        private bool documentNumbersPerLocation;
        public bool DocumentNumbersPerLocation
        {
            get { return documentNumbersPerLocation; }
            set { SetValueConfig (() => DocumentNumbersPerLocation, ref documentNumbersPerLocation, value); }
        }

        [ConfigurationMember ("0", DbKey = DOCUMENT_NUMBERS_PER_LOCATION_KEY)]
        public int DocumentNumbersPerLocationDb
        {
            get { return documentNumbersPerLocation ? -1 : 0; }
            set { documentNumbersPerLocation = value == -1; }
        }

        private bool confirmPriceRules;
        [ConfigurationMember ("true")]
        public bool ConfirmPriceRules
        {
            get { return confirmPriceRules; }
            set { SetValueConfig (() => ConfirmPriceRules, ref confirmPriceRules, value); }
        }

        private bool useSalesTaxInsteadOfVAT;
        public bool UseSalesTaxInsteadOfVAT
        {
            get { return useSalesTaxInsteadOfVAT; }
            set { SetValueConfig (() => UseSalesTaxInsteadOfVAT, ref useSalesTaxInsteadOfVAT, value); }
        }

        [ConfigurationMember ("0", DbKey = USE_SALES_TAX_INSTEAD_OF_VAT)]
        public int UseSalesTaxInsteadOfVATDb
        {
            get { return useSalesTaxInsteadOfVAT ? -1 : 0; }
            set { useSalesTaxInsteadOfVAT = value == -1; }
        }

        private string bankNotesAndCoins;
        [ConfigurationMember (DEFAULT_BANK_NOTES_AND_COINS)]
        public string BankNotesAndCoins
        {
            get
            {
                if (!Validator.ValidateBankNotesAndCoins (bankNotesAndCoins))
                    bankNotesAndCoins = DEFAULT_BANK_NOTES_AND_COINS;
                return bankNotesAndCoins;
            }
            set { SetClassConfig (() => BankNotesAndCoins, ref bankNotesAndCoins, value); }
        }

        private int distributedChargeMethod;
        [ConfigurationMember ("0", DbKey = "DistributedChargeMethod")]
        public int DistributedChargeMethod
        {
            get { return distributedChargeMethod; }
            set { SetValueConfig (() => DistributedChargeMethod, ref distributedChargeMethod, value); }
        }

        private bool lastKeypadOnLoginActive;
        [ConfigurationMember ("false")]
        public bool LastKeypadOnLoginActive
        {
            get { return lastKeypadOnLoginActive; }
            set { SetValueConfig (() => LastKeypadOnLoginActive, ref lastKeypadOnLoginActive, value); }
        }

        private bool autoGeneratePartnerCodes;
        [ConfigurationMember ("true")]
        public bool AutoGeneratePartnerCodes
        {
            get { return autoGeneratePartnerCodes; }
            set { SetValueConfig (() => AutoGeneratePartnerCodes, ref autoGeneratePartnerCodes, value); }
        }

        private string partnerCodePattern;
        [ConfigurationMember ("#")]
        public string PartnerCodePattern
        {
            get { return partnerCodePattern; }
            set { SetClassConfig (() => PartnerCodePattern, ref partnerCodePattern, value); }
        }

        private bool autoGenerateItemCodes;
        [ConfigurationMember ("true")]
        public bool AutoGenerateItemCodes
        {
            get { return autoGenerateItemCodes; }
            set { SetValueConfig (() => AutoGenerateItemCodes, ref autoGenerateItemCodes, value); }
        }

        private string itemCodePattern;
        [ConfigurationMember ("#")]
        public string ItemCodePattern
        {
            get { return itemCodePattern; }
            set { SetClassConfig (() => ItemCodePattern, ref itemCodePattern, value); }
        }

        private bool autoGenerateUserCodes;
        [ConfigurationMember ("true")]
        public bool AutoGenerateUserCodes
        {
            get { return autoGenerateUserCodes; }
            set { SetValueConfig (() => AutoGenerateUserCodes, ref autoGenerateUserCodes, value); }
        }

        private string userCodePattern;
        [ConfigurationMember ("#")]
        public string UserCodePattern
        {
            get { return userCodePattern; }
            set { SetClassConfig (() => UserCodePattern, ref userCodePattern, value); }
        }

        private bool autoGenerateLocationCodes;
        [ConfigurationMember ("true")]
        public bool AutoGenerateLocationCodes
        {
            get { return autoGenerateLocationCodes; }
            set { SetValueConfig (() => AutoGenerateLocationCodes, ref autoGenerateLocationCodes, value); }
        }

        private string locationCodePattern;
        [ConfigurationMember ("#")]
        public string LocationCodePattern
        {
            get { return locationCodePattern; }
            set { SetClassConfig (() => LocationCodePattern, ref locationCodePattern, value); }
        }

        private bool lastChooseMoneyVisible;
        [ConfigurationMember ("false")]
        public bool LastChooseMoneyVisible
        {
            get { return lastChooseMoneyVisible; }
            set { SetValueConfig (() => LastChooseMoneyVisible, ref lastChooseMoneyVisible, value); }
        }

        private bool saleChangeIsReturned;
        [ConfigurationMember ("false")]
        public bool SaleChangeIsReturned
        {
            get { return saleChangeIsReturned; }
            set { SetValueConfig (() => SaleChangeIsReturned, ref saleChangeIsReturned, value); }
        }

        private BasePaymentType lastPaymentMethod;
        [ConfigurationMember ("Cash")]
        public BasePaymentType LastPaymentMethod
        {
            get { return lastPaymentMethod; }
            set { SetValueConfig (() => LastPaymentMethod, ref lastPaymentMethod, value); }
        }

        private long lastDocumentPaymentMethodId;
        [ConfigurationMember ("-1")]
        public long LastDocumentPaymentMethodId
        {
            get { return lastDocumentPaymentMethodId; }
            set { SetValueConfig (() => LastDocumentPaymentMethodId, ref lastDocumentPaymentMethodId, value); }
        }

        private bool printFinalReceiptByDefault;
        [ConfigurationMember ("true")]
        public bool PrintFinalReceiptByDefault
        {
            get { return printFinalReceiptByDefault; }
            set { SetValueConfig (() => PrintFinalReceiptByDefault, ref printFinalReceiptByDefault, value); }
        }

        private bool printBankCashReceiptByDefault;
        [ConfigurationMember ("false")]
        public bool PrintBankCashReceiptByDefault
        {
            get { return printBankCashReceiptByDefault; }
            set { SetValueConfig (() => PrintBankCashReceiptByDefault, ref printBankCashReceiptByDefault, value); }
        }

        public static IList<double> AllowedBankNotesAndCoins
        {
            get { return allowedBankNotesAndCoins; }
        }

        private bool lastExportToFile;
        [ConfigurationMember ("true")]
        public bool LastExportToFile
        {
            get { return lastExportToFile; }
            set { SetValueConfig (() => LastExportToFile, ref lastExportToFile, value); }
        }

        private bool openExportedFile;
        [ConfigurationMember ("true")]
        public bool OpenExportedFile
        {
            get { return openExportedFile; }
            set { SetValueConfig (() => OpenExportedFile, ref openExportedFile, value); }
        }

        private bool lastExportToEmail;
        [ConfigurationMember ("false")]
        public bool LastExportToEmail
        {
            get { return lastExportToEmail; }
            set { SetValueConfig (() => LastExportToEmail, ref lastExportToEmail, value); }
        }

        #endregion
    }
}
