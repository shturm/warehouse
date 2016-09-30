//
// Translator.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   07/20/2006
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
using System.Globalization;
using System.Linq;
using System.Threading;
using Warehouse.Business.Entities;
using Warehouse.Data;

namespace Warehouse.Business
{
    public enum DataFilterLabel
    {
        #region Application log filters

        AppLogId,
        AppLogMessage,
        AppLogMessageSource,
        AppLogTimeStamp,

        #endregion

        #region Cashbook table fields

        CashEntryNumber,
        CashEntryDate,
        CashEntryDescription,
        CashEntryTurnoverType,
        CashEntryDirection,
        CashEntryQuantity,
        CashEntryTimeStamp,

        #endregion

        #region Item filters

        ItemId,
        ItemCode,
        ItemBarcode,
        ItemCatalog,
        ItemName,
        ItemName2,
        ItemMeasUnit,
        ItemMeasUnit2,
        ItemMeasRatio,
        ItemTradeInPrice,
        ItemTradeOutPrice,
        ItemTradePrice,
        ItemRegularPrice,
        ItemPriceGroup1,
        ItemPriceGroup2,
        ItemPriceGroup3,
        ItemPriceGroup4,
        ItemPriceGroup5,
        ItemPriceGroup6,
        ItemPriceGroup7,
        ItemPriceGroup8,
        ItemMinQuantity,
        ItemNomQuantity,
        ItemDescription,
        ItemType,
        ItemOrder,
        ItemDeleted,

        #endregion

        #region Item group filters

        ItemsGroupId,
        ItemsGroupName,
        ItemsGroupCode,

        #endregion

        #region Invoice filters

        InvoiceId,
        InvoiceOperationNumber,
        InvoiceNumber,
        InvoiceOperationType,
        InvoiceDate,
        InvoiceDocumentType,
        InvoiceReferenceDate,
        InvoiceReferenceNumber,
        InvoiceRecipient,
        InvoiceRecipientEGN,
        InvoiceProvider,
        InvoiceTaxDate,
        InvoiceReason,
        InvoiceDescription,
        InvoiceLocation,
        InvoicePaymentTypeId,

        #endregion

        #region Location filters

        LocationId,
        LocationCode,
        LocationName,
        SourceLocationName,
        TargetLocationName,
        LocationOrder,
        LocationDeleted,
        LocationName2,
        LocationPriceGroup,

        #endregion

        #region Location group filters

        LocationsGroupId,
        LocationsGroupName,
        LocationsGroupCode,

        #endregion

        #region Lot filters

        LotId,
        LotSerialNumber,
        LotProductionDate,
        LotExpirationDate,
        LotLocation,

        #endregion

        #region Operation filters

        OperationType,
        OperationNumber,
        OperationDate,
        OperationDateTime,
        OperationTimeStamp,
        OperationDetailReferenceId,
        OperationQuantitySum,
        OperationQuantitySignedSum,
        OperationQuantity,
        OperationVatSum,
        OperationSum,
        OperationTotal,
        OperationProfit,

        OperationDetailId,
        OperationDetailQuantity,
        OperationDetailPriceIn,
        OperationDetailPriceOut,
        OperationDetailDiscount,
        OperationDetailSign,
        OperationDetailVatIn,
        OperationDetailVatOut,
        OperationDetailVat,
        OperationDetailVatSum,
        OperationDetailSum,
        OperationDetailTotal,
        OperationDetailNote,
        OperationDetailWarrantyPeriod,
        OperationDetailLot,
        OperationDetailLotId,

        #endregion

        #region Partner filters

        PartnerId,
        PartnerCode,
        PartnerName,
        PartnerName2,
        PartnerLiablePerson,
        PartnerLiablePerson2,
        PartnerCity,
        PartnerCity2,
        PartnerAddress,
        PartnerAddress2,
        PartnerPhone,
        PartnerPhone2,
        PartnerFax,
        PartnerEmail,
        PartnerTaxNumber,
        PartnerBulstat,
        PartnerBankName,
        PartnerBankCode,
        PartnerBankAcct,
        PartnerBankVATName,
        PartnerBankVATCode,
        PartnerBankVATAcct,
        PartnerPriceGroup,
        PartnerDiscount,
        PartnerType,
        PartnerOrder,
        PartnerDeleted,
        PartnerCreationTimeStamp,
        PartnerCardNumber,
        PartnerNote,
        PartnerNote2,

        #endregion

        #region Partners group filters

        PartnersGroupsId,
        PartnersGroupsName,
        PartnersGroupsCode,

        #endregion

        #region Payment filters

        PaymentId,
        PaymentOperationId,
        PaymentOperationType,
        PaymentPartnerId,
        PaymentAmount,
        PaymentMode,
        PaymentDate,
        PaymentOperatorId,
        PaymentTimeStamp,
        PaymentTypeId,
        PaymentTransaction,
        PaymentEndDate,
        PaymentLocationId,
        PaymentSign,
        PaymentDueSum,
        PaymentRemainingSum,
        PaymentsInCash,
        PaymentsByBankOrder,
        PaymentsByDebitCreditCard,
        PaymentsByVoucher,

        #endregion

        #region Payment types filters

        PaymentTypesId,
        PaymentTypesName,
        PaymentTypesMethod,

        #endregion

        #region Store filters

        StorePrice,
        StoreQtty,
        StoreItemAvailableQuantitySum,
        StoreItemCountedQuantity,
        StoreLot,

        #endregion

        #region User filters

        UserId,
        UserCode,
        UserName,
        UserName2,
        UserOrder,
        UserDeleted,
        UserLevel,
        UserCardNo,

        #endregion

        #region Users group filters

        UsersGroupsId,
        UsersGroupsName,
        UsersGroupsCode,

        #endregion

        #region Operations operators filters

        OperationsOperatorId,
        OperationsOperatorCode,
        OperationsOperatorName,
        OperationsOperatorName2,
        OperationsOperatorOrder,
        OperationsOperatorLevel,
        OperationsOperatorCardNo,

        #endregion

        #region Operations operators groups filters

        OperationsOperatorsGroupsId,
        OperationsOperatorsGroupsName,
        OperationsOperatorsGroupsCode,

        #endregion

        #region Operations users filters

        OperationsUserId,
        OperationsUserCode,
        OperationsUserName,
        OperationsUserName2,
        OperationsUserOrder,
        OperationsUserLevel,
        OperationsUserCardNo,

        #endregion

        #region Operations users groups filters

        OperationsUsersGroupsId,
        OperationsUsersGroupsName,
        OperationsUsersGroupsCode,

        #endregion

        #region VAT group filters

        VATGroupId,
        VATGroupCode,
        VATGroupName,
        VATGroupValue,

        #endregion

        #region Compatibility filters

        #region Goods filters

        GoodsId,
        GoodsCode,
        GoodsBarCode,
        GoodsCatalog,
        GoodsName,
        GoodsName2,
        GoodsMeasUnit,
        GoodsMeasUnit2,
        GoodsMeasRatio,
        GoodsTradeInPrice,
        GoodsTradeOutPrice,
        GoodsTradePrice,
        GoodsRegularPrice,
        GoodsPriceGroup1,
        GoodsPriceGroup2,
        GoodsPriceGroup3,
        GoodsPriceGroup4,
        GoodsPriceGroup5,
        GoodsPriceGroup6,
        GoodsPriceGroup7,
        GoodsPriceGroup8,
        GoodsMinQuantity,
        GoodsNomQuantity,
        GoodsDescription,
        GoodsType,
        GoodsOrder,
        GoodsDeleted,

        #endregion

        #region Goods group filters

        GoodsGroupsId,
        GoodsGroupsName,
        GoodsGroupsCode,

        #endregion

        InvoicePointOfSale,

        #region Location filters

        PointOfSaleId,
        PointOfSaleCode,
        PointOfSaleName,
        SourcePointOfSaleName,
        TargetPointOfSaleName,
        PointOfSaleOrder,
        PointOfSaleDeleted,
        PointOfSaleName2,
        PointOfSalePriceGroup,

        #endregion

        #region Location group filters

        PointsOfSaleGroupId,
        PointsOfSaleGroupName,
        PointsOfSaleGroupCode,

        #endregion

        StoreGoodsAvailableQuantitySum,
        StoreGoodsCountedQuantity,

        #endregion

        NotSet
    }

    public static class Translator
    {
        private static CultureInfo culture;
        private static ConfigurationHolderBase config;
        private static ITranslationProvider provider;

        public static void ResetCulture ()
        {
            culture = null;
        }

        public static void Init (ITranslationProvider translator)
        {
            config = BusinessDomain.AppConfiguration;
            config.PropertyChanged += config_PropertyChanged;
            provider = translator;

            InitThread (config, Thread.CurrentThread);

            #region Restrinctions Tree translations

            BusinessDomain.RestrictionTree = new RestrictionNode ("mnuRoot", () => "root",
                new RestrictionNode ("mnuFile", () => GetString ("File"),
                    new RestrictionNode ("mnuEditAdminUserChange", () => GetString ("Logout")),
                    new RestrictionNode ("mnuFileOpenBase", () => GetString ("Change Database...")).RestrictByDefaultFor (UserAccessLevel.Operator),
                    new RestrictionNode ("mnuFileLoadSettings", () => GetString ("Load Settings...")).RestrictByDefaultFor (UserAccessLevel.Operator, UserAccessLevel.Manager),
                    new RestrictionNode ("mnuFileSaveSettings", () => GetString ("Save Settings...")),
                    new RestrictionNode ("mnuFileExit", () => GetString ("Exit"))),
                new RestrictionNode ("mnuOperations", () => GetString ("Operations"),
                    new RestrictionNode ("mnuOperSales", () => GetString ("Sale")),
                    new RestrictionNode ("mnuOperDeliveries", () => GetString ("Purchase")),
                    new RestrictionNode ("mnuOperProduction", () => GetString ("Production"),
                        new RestrictionNode ("mnuOperProductionComplexRecipes", () => GetString ("Recipes...")),
                        new RestrictionNode ("mnuOperProductionComplexProducing", () => GetString ("Production"))),
                    new RestrictionNode ("mnuOperTransfer", () => GetString ("Transfer")),
                    new RestrictionNode ("mnuOperWaste", () => GetString ("Waste")),
                    new RestrictionNode ("mnuOperInvoice", () => GetString ("Invoicing"),
                        new RestrictionNode ("mnuOperInvoicePublish", () => GetString ("Issue Invoice...")),
                        new RestrictionNode ("mnuOperInvoiceReceive", () => GetString ("Receive Invoice...")),
                        new RestrictionNode ("mnuOperInvoicePublishCancel", () => GetString ("Void Issued Invoice...")),
                        new RestrictionNode ("mnuOperInvoiceReceiveCancel", () => GetString ("Void Received Invoice..."))),
                    new RestrictionNode ("mnuOperTradeObject", () => GetString ("Point of Sale"),
                        new RestrictionNode ("mnuOperTradeObjectCash", () => GetString ("Make sales in cash")),
                        new RestrictionNode ("mnuOperTradeObjectCard", () => GetString ("Make sales with card")),
                        new RestrictionNode ("mnuOperTradeObjectBank", () => GetString ("Make sales by bank")),
                        new RestrictionNode ("mnuOperTradeObjectReports", () => GetString ("View Reports")))),
                new RestrictionNode ("mnuEdit", () => GetString ("Edit"),
                    new RestrictionNode ("mnuEditPartners", () => GetString ("Partners..."),
                        new RestrictionNode ("mnuEditPartnersbtnNew", () => GetString ("New", "Partner")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditPartnersbtnEdit", () => GetString ("Edit")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditPartnersbtnDelete", () => GetString ("Delete")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditPartnersbtnImport", () => GetString ("Import")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditPartnersbtnExport", () => GetString ("Export")).RestrictByDefaultFor (UserAccessLevel.Operator)),
                    new RestrictionNode ("mnuEditGoods", () => GetString ("Items..."),
                        new RestrictionNode ("mnuEditGoodsbtnNew", () => GetString ("New", "Item")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditGoodsbtnEdit", () => GetString ("Edit")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditGoodsbtnDelete", () => GetString ("Delete")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditGoodsbtnImport", () => GetString ("Import")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditGoodsbtnExport", () => GetString ("Export")).RestrictByDefaultFor (UserAccessLevel.Operator)),
                    new RestrictionNode ("mnuEditUsers", () => GetString ("Users..."),
                        new RestrictionNode ("mnuEditUsersbtnNew", () => GetString ("New", "User")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditUsersbtnEdit", () => GetString ("Edit")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditUsersbtnDelete", () => GetString ("Delete")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditUsersbtnImport", () => GetString ("Import")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditUsersbtnExport", () => GetString ("Export")).RestrictByDefaultFor (UserAccessLevel.Operator)),
                    new RestrictionNode ("mnuEditObjects", () => GetString ("Locations..."),
                        new RestrictionNode ("mnuEditObjectsbtnNew", () => GetString ("New", "Location")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditObjectsbtnEdit", () => GetString ("Edit")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditObjectsbtnDelete", () => GetString ("Delete")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditObjectsbtnImport", () => GetString ("Import")).RestrictByDefaultFor (UserAccessLevel.Operator),
                        new RestrictionNode ("mnuEditObjectsbtnExport", () => GetString ("Export")).RestrictByDefaultFor (UserAccessLevel.Operator)),
                    new RestrictionNode ("mnuEditVATGroups", () => string.Empty, //Assigned later in FrmMain.cs
                        new RestrictionNode ("mnuEditVATGroupsbtnNew", () => GetString ("New", "VAT group")),
                        new RestrictionNode ("mnuEditVATGroupsbtnEdit", () => GetString ("Edit")),
                        new RestrictionNode ("mnuEditVATGroupsbtnDelete", () => GetString ("Delete")),
                        new RestrictionNode ("mnuEditVATGroupsbtnImport", () => GetString ("Import")),
                        new RestrictionNode ("mnuEditVATGroupsbtnExport", () => GetString ("Export"))).RestrictByDefaultFor (UserAccessLevel.Operator),
                    new RestrictionNode ("mnuEditPayments", () => GetString ("Payments"),
                        new RestrictionNode ("mnuEditPaysPayments", () => GetString ("Payments..."),
                            new RestrictionNode ("mnuEditPaysPaymentsbtnEdit", () => GetString ("Edit")),
                            new RestrictionNode ("mnuEditPaysPaymentsbtnDelete", () => GetString ("Delete"))),
                        new RestrictionNode ("mnuEditPaymentTypes", () => GetString ("Payment Types...")).RestrictByDefaultFor (UserAccessLevel.Operator)),
                    new RestrictionNode ("mnuEditDevices", () => GetString ("Devices..."),
                        new RestrictionNode ("mnuEditDevicesbtnNew", () => GetString ("New", "Device")),
                        new RestrictionNode ("mnuEditDevicesbtnEdit", () => GetString ("Edit")),
                        new RestrictionNode ("mnuEditDevicesbtnDelete", () => GetString ("Delete"))).RestrictByDefaultFor (UserAccessLevel.Operator),
                    new RestrictionNode ("mnuEditDocuments", () => GetString ("Documents"),
                        new RestrictionNode ("mnuEditDocsSale", () => GetString ("Sale...")),
                        new RestrictionNode ("mnuEditDocsDelivery", () => GetString ("Purchase...")),
                        new RestrictionNode ("mnuEditDocsComplexProduction", () => GetString ("Production...")),
                        new RestrictionNode ("mnuEditDocsTransfer", () => GetString ("Transfer...")),
                        new RestrictionNode ("mnuEditDocsWaste", () => GetString ("Waste...")),
                        new RestrictionNode ("mnuEditDocsRevision", () => GetString ("Stock-taking..."))).RestrictByDefaultFor (UserAccessLevel.Operator),
                    new RestrictionNode ("mnuEditPrintAgain", () => GetString ("Reprint Documents"),
                        new RestrictionNode ("mnuEditPrintAgainSale", () => GetString ("Sale...")),
                        new RestrictionNode ("mnuEditPrintAgainDelivery", () => GetString ("Purchase...")),
                        new RestrictionNode ("mnuEditPrintAgainComplexProductions", () => GetString ("Production...")),
                        new RestrictionNode ("mnuEditPrintAgainTransfer", () => GetString ("Transfer...")),
                        new RestrictionNode ("mnuEditPrintAgainWaste", () => GetString ("Waste...")),
                        new RestrictionNode ("mnuEditPrintAgainTaxDocuments", () => GetString ("Invoicing"),
                            new RestrictionNode ("mnuEditPrintAgainTaxDocumentsInvoice", () => GetString ("Issued Invoice..."))),
                        new RestrictionNode ("mnuEditPrintAgainInspection", () => GetString ("Stock-taking...")),
                        new RestrictionNode ("mnuEditPrintAgainAdvancePayment", () => GetString ("Advance Payment..."))),
                    new RestrictionNode ("mnuEditAdministrate", () => GetString ("Administration"),
                        new RestrictionNode ("mnuEditRegObjects", () => GetString ("Company Information...")),
                        new RestrictionNode ("mnuEditAdminPriceChange", () => GetString ("Change Prices...")),
                        new RestrictionNode ("mnuEditAdminPriceRules", () => GetString ("Price Rules...")),
                        new RestrictionNode ("mnuEditAdminRevision", () => GetString ("Stock-taking...")),
                        new RestrictionNode ("mnuEditAdminPermissions", () => GetString ("Permissions...")),
                        new RestrictionNode ("mnuEditAdminRegisterCash", () => GetString ("Register Cash...")),
                        new RestrictionNode ("mnuEditAdminFReports", () => GetString ("Fiscal Reports...")),
                        new RestrictionNode ("mnuEditAdminPrintDuplicateOfLastReceipt", () => GetString ("Print Duplicate of Last Receipt")),
                        new RestrictionNode ("mnuEditAdminDocumentNumbersPerLocation", () => GetString ("Document Numbers per Location..."))).RestrictByDefaultFor (UserAccessLevel.Operator)),
                new RestrictionNode ("mnuView", () => GetString ("View"),
                    new RestrictionNode ("mnuViewToolbar", () => GetString ("Show Toolbar")),
                    new RestrictionNode ("mnuViewShowTabs", () => GetString ("Show Tabs")),
                    new RestrictionNode ("mnuViewStatusbar", () => GetString ("Show Status Bar"))),
                new RestrictionNode ("mnuReports", () => GetString ("Reports"),
                    new RestrictionNode ("mnuRepSale", () => GetString ("Sales...")),
                    new RestrictionNode ("mnuRepDelivery", () => GetString ("Purchases...")),
                    new RestrictionNode ("mnuReportsProduction", () => GetString ("Production"),
                        new RestrictionNode ("mnuReportsProductionComplexRecipes", () => GetString ("Recipes...")),
                        new RestrictionNode ("mnuReportsProductionComplexProducing", () => GetString ("Production..."))),
                    new RestrictionNode ("mnuRepTransfer", () => GetString ("Transfers...")),
                    new RestrictionNode ("mnuRepWaste", () => GetString ("Wastes...")),
                    new RestrictionNode ("mnuRepRevision", () => GetString ("Stock-takings...")),
                    new RestrictionNode ("mnuReportsAllOperations", () => GetString ("Operations...")),
                    new RestrictionNode ("mnuReportsAllDrafts", () => GetString ("Drafts...")),
                    new RestrictionNode ("mnuRepLast", () => GetString ("Last Report"),
                        new RestrictionNode ("mnuRepLastName", () => GetString ("None", "Report"))),
                    new RestrictionNode ("mnuRepNomenclatures", () => GetString ("Lists"),
                        new RestrictionNode ("mnuRepNomenclaturesPartners", () => GetString ("Partners...")),
                        new RestrictionNode ("mnuRepNomenclaturesGoodsByName", () => GetString ("Items...")),
                        new RestrictionNode ("mnuRepNomenclaturesOperators", () => GetString ("Users...")),
                        new RestrictionNode ("mnuRepNomenclaturesObjects", () => GetString ("Locations..."))),
                    new RestrictionNode ("mnuRepGoods", () => GetString ("Items"),
                        new RestrictionNode ("mnuRepGoogsPath", () => GetString ("Items Flow...")),
                        new RestrictionNode ("mnuRepGoodsQtty", () => GetString ("Items in Stock...")),
                        new RestrictionNode ("mnuRepGoodsQttyInDate", () => GetString ("Availability as of...")),
                        new RestrictionNode ("mnuRepGoodsLessMinQtty", () => GetString ("Items below Minimal Availability...")),
                        new RestrictionNode ("mnuRepGoodsStockTaking", () => GetString ("Items Stock-taking...")),
                        new RestrictionNode ("mnuRepGoodsDelivery", () => GetString ("Purchases by Items...")),
                        new RestrictionNode ("mnuRepGoodsSale", () => GetString ("Sales by Items...")),
                        new RestrictionNode ("mnuRepGoodsBestProfit", () => GetString ("Profit by Items...")),
                        new RestrictionNode ("mnuRepGoodsInvoicedGoods", () => GetString ("Invoiced Items..."))),
                    new RestrictionNode ("mnuRepPartners", () => GetString ("Partners"),
                        new RestrictionNode ("mnuRepPartnersDeliveries", () => GetString ("Purchases by Partners...")),
                        new RestrictionNode ("mnuRepPartnersSales", () => GetString ("Sales by Partners...")),
                        new RestrictionNode ("mnuRepPartnersByProfit", () => GetString ("Profit by Partners...")),
                        new RestrictionNode ("mnuRepPartnersDebt", () => GetString ("Partners Debt..."))),
                    new RestrictionNode ("mnuReportsObjectsSep1", () => GetString ("Locations"),
                        new RestrictionNode ("mnuRepObjectsDeliveries", () => GetString ("Purchases by Locations...")),
                        new RestrictionNode ("mnuRepObjectsSales", () => GetString ("Sales by Locations...")),
                        new RestrictionNode ("mnuRepObjectsByProfit", () => GetString ("Profit by Locations..."))),
                    new RestrictionNode ("mnuRepPayments", () => GetString ("Payments"),
                        new RestrictionNode ("mnuRepPaymentsByDocuments", () => GetString ("Payments by Documents...")),
                        new RestrictionNode ("mnuRepPaymentsByPartners", () => GetString ("Payments by Partners...")),
                        new RestrictionNode ("mnuRepPaymentsDueDates", () => GetString ("Payments Due Dates...")),
                        new RestrictionNode ("mnuRepPaymentsChronology", () => GetString ("Payments History...")),
                        new RestrictionNode ("mnuRepPaymentsAdvance", () => GetString ("Advance Payments...")),
                        new RestrictionNode ("mnuRepPaymentsIncome", () => GetString ("Income..."))),
                    new RestrictionNode ("mnuRepDocuments", () => GetString ("Documents"),
                        new RestrictionNode ("mnuRepDocumentsSalesBySum", () => GetString ("Sales by Amounts...")),
                        new RestrictionNode ("mnuRepDocumentsDeliveriesBySum", () => GetString ("Purchases by Amount...")),
                        new RestrictionNode ("mnuRepDocumentsRevisionsBySum", () => GetString ("Stock-takings by Amount...")),
                        new RestrictionNode ("mnuRepDocumentsPublishedInvoices", () => GetString ("Issued Invoices...")),
                        new RestrictionNode ("mnuRepDocumentsReceivedInvoices", () => GetString ("Received Invoices..."))),
                    new RestrictionNode ("mnuRepAdministration", () => GetString ("Administration"),
                        new RestrictionNode ("mnuRepAdministrationAppLog", () => GetString ("Application Log...")))).RestrictByDefaultFor (UserAccessLevel.Operator),
                new RestrictionNode ("mnuTools", () => GetString ("Tools"),
                    new RestrictionNode ("mnuToolsSetup", () => GetString ("Settings...")).RestrictByDefaultFor (UserAccessLevel.Operator, UserAccessLevel.Manager),
                    new RestrictionNode ("mnuToolsKeyShortcuts", () => GetString ("Key Shortcuts...")),
                    new RestrictionNode ("mnuToolsQuickItems", () => GetString ("Quick Items..."))).RestrictByDefaultFor (UserAccessLevel.Operator),
                new RestrictionNode ("mnuWindow", () => GetString ("Windows"),
                    new RestrictionNode ("mnuWinowClose", () => GetString ("Close Active Window")),
                    new RestrictionNode ("mnuWinowCloseAll", () => GetString ("Close All Windows"))),
                new RestrictionNode ("mnuHelp", () => GetString ("Help"),
                    new RestrictionNode ("mnuHelpDocumentation", () => GetString ("Documentation...")),
                    new RestrictionNode ("mnuHelpAbout", () => GetString ("About..."))));

            #endregion
        }

        public static void TranslateRestrictions ()
        {
            TranslateRestriction (BusinessDomain.RestrictionTree);
        }

        private static void TranslateRestriction (RestrictionNode restrictionNode)
        {
            restrictionNode.Translate ();
            foreach (RestrictionNode child in restrictionNode.Children)
                TranslateRestriction (child);
        }

        private static void config_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName) {
                case "UseSystemLocalization":
                case "Localization":
                case "VATIncluded":
                case "UseSalesTaxInsteadOfVAT":
                    reportFilterNames = null;
                    reportFieldColumnNames = null;
                    break;
            }
        }

        public static void InitThread (Thread thread)
        {
            InitThread (BusinessDomain.AppConfiguration, thread);
        }

        public static void InitThread (ConfigurationHolderBase cfg, Thread thread)
        {
            try {
                string locale = cfg.Localization;

                if (culture == null) {
                    try {
                        if (cfg.UseSystemLocalization || string.IsNullOrWhiteSpace (locale)) {
                            // Try to use the environment variables for comaptibility first
                            string lang = Environment.GetEnvironmentVariable ("LANG");
                            string language = Environment.GetEnvironmentVariable ("LANGUAGE");
                            if (!string.IsNullOrWhiteSpace (lang))
                                InitCulture (lang);
                            else if (!string.IsNullOrWhiteSpace (language))
                                InitCulture (language);
                            else
                                InitCulture ();
                        } else
                            InitCulture (locale);
                    } catch (ArgumentException) {
                        InitCulture ();
                    }
                }

                provider.Init (cfg.LocalePackageName, thread, culture);
            } catch (NotSupportedException ex) {
                ErrorHandling.LogException (ex, ErrorSeverity.Warning);
            } catch (ArgumentException ex) {
                ErrorHandling.LogException (ex, ErrorSeverity.Warning);
            }
        }

        private static void InitCulture ()
        {
            if (PlatformHelper.Platform == PlatformTypes.Windows)
                Thread.CurrentThread.CurrentCulture.ClearCachedData ();
            Thread threadReader = new Thread (s => culture = Thread.CurrentThread.CurrentCulture);
            threadReader.Start ();
            threadReader.Join ();
        }

        private static void InitCulture (string locale)
        {
            culture = locale.Length == 2
                ? CultureInfo.CreateSpecificCulture (locale) : new CultureInfo (locale);
        }

        public static string GetString (string message, string context = null)
        {
            return provider.GetString (message, context);
        }

        public static string GetPluralString (string singleMsg, string pluralMsg, int number)
        {
            return provider.GetPluralString (singleMsg, pluralMsg, number);
        }

        public static ITranslationProvider GetHelper ()
        {
            return provider;
        }

        public static KeyValuePair<int, string> [] GetMonths ()
        {
            return new []
                {
                    new KeyValuePair<int, string> (1, GetString ("January")),
                    new KeyValuePair<int, string> (2, GetString ("February")),
                    new KeyValuePair<int, string> (3, GetString ("March")),
                    new KeyValuePair<int, string> (4, GetString ("April")),
                    new KeyValuePair<int, string> (5, GetString ("May")),
                    new KeyValuePair<int, string> (6, GetString ("June")),
                    new KeyValuePair<int, string> (7, GetString ("July")),
                    new KeyValuePair<int, string> (8, GetString ("August")),
                    new KeyValuePair<int, string> (9, GetString ("September")),
                    new KeyValuePair<int, string> (10, GetString ("October")),
                    new KeyValuePair<int, string> (11, GetString ("November")),
                    new KeyValuePair<int, string> (12, GetString ("December"))
                };
        }

        public static KeyValuePair<int, string> [] GetReportFilterCompareLogics ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) DataFilterLogic.ExactMatch, GetString ("equals")),
                    new KeyValuePair<int, string> ((int) DataFilterLogic.Greather, GetString ("more than")),
                    new KeyValuePair<int, string> ((int) DataFilterLogic.Less, GetString ("less than"))
                };
        }

        public static KeyValuePair<int, string> [] GetReportFilterDateTimeCompareLogics ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) DataFilterLogic.MoreThanMinutesAgo, GetString ("more than minutes ago")),
                    new KeyValuePair<int, string> ((int) DataFilterLogic.LessThanMinutesAgo, GetString ("less than minutes ago"))
                };
        }

        public static KeyValuePair<int, string> [] GetSortDirections ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) SortDirection.Ascending, GetString ("Ascending")),
                    new KeyValuePair<int, string> ((int) SortDirection.Descending, GetString ("Descending"))
                };
        }

        public static KeyValuePair<int, string> [] GetAskDialogStates ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) AskDialogState.NotSaved, GetString ("Ask")),
                    new KeyValuePair<int, string> ((int) AskDialogState.Yes, GetString ("Always")),
                    new KeyValuePair<int, string> ((int) AskDialogState.No, GetString ("Never"))
                };
        }

        public static KeyValuePair<int, string> [] GetBarcodeTypes ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) GeneratedBarcodeType.EAN13, "EAN-13"),
                    new KeyValuePair<int, string> ((int) GeneratedBarcodeType.EAN8, "EAN-8")
                };
        }

        private static Dictionary<DataFilterLabel, string> reportFilterNames;
        private static Dictionary<DataFilterLabel, string> ReportFilterNames
        {
            get
            {
                return reportFilterNames ?? (reportFilterNames = new Dictionary<DataFilterLabel, string>
                    {
                        #region Application log filters
                        
                        { DataFilterLabel.AppLogId, GetString ("Message No.") },
                        { DataFilterLabel.AppLogMessage, GetString ("Message") },
                        { DataFilterLabel.AppLogMessageSource, GetString ("Source") },
                        { DataFilterLabel.AppLogTimeStamp, GetString ("Creation date/time") },
                
                        #endregion

                        #region Cashbook table columns

                        { DataFilterLabel.CashEntryNumber, GetString ("Entry No.") },
                        { DataFilterLabel.CashEntryDate, GetString ("Date") },
                        { DataFilterLabel.CashEntryDescription, GetString ("Description") },
                        { DataFilterLabel.CashEntryTurnoverType, GetString ("Turnover type") },
                        { DataFilterLabel.CashEntryDirection, GetString ("Direction") },
                        { DataFilterLabel.CashEntryQuantity, GetString ("Amount") },
                        { DataFilterLabel.CashEntryTimeStamp, GetString ("Date/Time") },

                        #endregion

                        #region Items filters
                        
                        { DataFilterLabel.ItemId, GetString ("Item No.") },
                        { DataFilterLabel.ItemCode, GetString ("Item code") },
                        { DataFilterLabel.ItemBarcode, GetString ("Barcode") },
                        { DataFilterLabel.ItemCatalog, GetString ("Catalogue") },
                        { DataFilterLabel.ItemName, GetString ("Item name") },
                        { DataFilterLabel.ItemName2, GetString ("Display item name") },
                        { DataFilterLabel.ItemMeasUnit, GetString ("Measure") },
                        { DataFilterLabel.ItemMeasUnit2, GetString ("Add. measure") },
                        { DataFilterLabel.ItemMeasRatio, GetString ("Ratio") },
                        { DataFilterLabel.ItemTradeInPrice, GetString ("Purchase price") },
                        { DataFilterLabel.ItemTradeOutPrice, GetString ("Sale price") },
                        { DataFilterLabel.ItemTradePrice, GetString ("Wholesale price") },
                        { DataFilterLabel.ItemRegularPrice, GetString ("Retail price") },
                        { DataFilterLabel.ItemPriceGroup1, GetString ("Price group 1") },
                        { DataFilterLabel.ItemPriceGroup2, GetString ("Price group 2") },
                        { DataFilterLabel.ItemPriceGroup3, GetString ("Price group 3") },
                        { DataFilterLabel.ItemPriceGroup4, GetString ("Price group 4") },
                        { DataFilterLabel.ItemPriceGroup5, GetString ("Price group 5") },
                        { DataFilterLabel.ItemPriceGroup6, GetString ("Price group 6") },
                        { DataFilterLabel.ItemPriceGroup7, GetString ("Price group 7") },
                        { DataFilterLabel.ItemPriceGroup8, GetString ("Price group 8") },
                        { DataFilterLabel.ItemMinQuantity, GetString ("Minimal quantity") },
                        { DataFilterLabel.ItemNomQuantity, GetString ("Nom. quantity") },
                        { DataFilterLabel.ItemDescription, GetString ("Description") },
                        { DataFilterLabel.ItemType, GetString ("Type") },
                        { DataFilterLabel.ItemOrder, GetString ("Order") },
                        { DataFilterLabel.ItemDeleted, GetString ("Deleted") },

                        #endregion

                        #region Item group filters

                        { DataFilterLabel.ItemsGroupId, GetString ("Item group No.") },
                        { DataFilterLabel.ItemsGroupName, GetString ("Item group") },
                        { DataFilterLabel.ItemsGroupCode, GetString ("Item group code") },

                        #endregion
                        
                        #region Store filters

                        { DataFilterLabel.StorePrice, GetString ("Price") },
                        { DataFilterLabel.StoreQtty, GetString ("Quantity") },
                        { DataFilterLabel.StoreLot, GetString ("Lot") },
                        { DataFilterLabel.StoreItemAvailableQuantitySum, GetString ("Quantity") },
                        { DataFilterLabel.StoreItemCountedQuantity, GetString ("Counted") },

                        #endregion

                        #region Partner filters

                        { DataFilterLabel.PartnerId, GetString ("Partner No.") },
                        { DataFilterLabel.PartnerCode, GetString ("Partner code") },
                        { DataFilterLabel.PartnerName, GetString ("Partner name") },
                        { DataFilterLabel.PartnerName2, GetString ("Display partner name") },
                        { DataFilterLabel.PartnerLiablePerson, GetString ("Contact name") },
                        { DataFilterLabel.PartnerLiablePerson2, GetString ("Display contact name") },
                        { DataFilterLabel.PartnerCity, GetString ("City") },
                        { DataFilterLabel.PartnerCity2, GetString ("Display city") },
                        { DataFilterLabel.PartnerAddress, GetString ("Address") },
                        { DataFilterLabel.PartnerAddress2, GetString ("Display address") },
                        { DataFilterLabel.PartnerPhone, GetString ("Phone") },
                        { DataFilterLabel.PartnerPhone2, GetString ("Display phone") },
                        { DataFilterLabel.PartnerFax, GetString ("Fax") },
                        { DataFilterLabel.PartnerEmail, GetString ("e-mail") },
                        { DataFilterLabel.PartnerTaxNumber, config.UseSalesTaxInsteadOfVAT ? GetString ("Tax number") : GetString ("VAT number") },
                        { DataFilterLabel.PartnerBulstat, GetString ("UIC") },
                        { DataFilterLabel.PartnerBankName, GetString ("Bank") },
                        { DataFilterLabel.PartnerBankCode, GetString ("BIC") },
                        { DataFilterLabel.PartnerBankAcct, GetString ("IBAN") },
                        { DataFilterLabel.PartnerBankVATName, config.UseSalesTaxInsteadOfVAT ? GetString ("Tax bank") : GetString ("VAT bank") },
                        { DataFilterLabel.PartnerBankVATCode, config.UseSalesTaxInsteadOfVAT ? GetString ("Tax BIC") : GetString ("VAT BIC") },
                        { DataFilterLabel.PartnerBankVATAcct, config.UseSalesTaxInsteadOfVAT ? GetString ("Tax IBAN") : GetString ("VAT IBAN") },
                        { DataFilterLabel.PartnerPriceGroup, GetString ("Price group") },
                        { DataFilterLabel.PartnerDiscount, GetString ("Discount (%)") },
                        { DataFilterLabel.PartnerType, GetString ("Type") },
                        { DataFilterLabel.PartnerOrder, GetString ("Order") },
                        { DataFilterLabel.PartnerDeleted, GetString ("Deleted") },
                        { DataFilterLabel.PartnerCreationTimeStamp, GetString ("Last modified") },
                        { DataFilterLabel.PartnerCardNumber, GetString ("Card No.") },
                        { DataFilterLabel.PartnerNote, GetString ("Note") },
                        { DataFilterLabel.PartnerNote2, GetString ("Note 2") },

                        #endregion

                        #region Partnersgroups filters

                        { DataFilterLabel.PartnersGroupsId, GetString ("Partner group No.") },
                        { DataFilterLabel.PartnersGroupsName, GetString ("Partner group") },
                        { DataFilterLabel.PartnersGroupsCode, GetString ("Partner group Code") },

                        #endregion

                        #region User filters

                        { DataFilterLabel.UserId, GetString ("User No.") },
                        { DataFilterLabel.UserCode, GetString ("User code") },
                        { DataFilterLabel.UserName, GetString ("User name") },
                        { DataFilterLabel.UserName2, GetString ("Display user name") },
                        { DataFilterLabel.UserOrder, GetString ("Order") },
                        { DataFilterLabel.UserDeleted, GetString ("Deleted") },
                        { DataFilterLabel.UserLevel, GetString ("Access level") },
                        { DataFilterLabel.UserCardNo, GetString ("User card No.") },

                        #endregion

                        #region Usersgroups filters

                        { DataFilterLabel.UsersGroupsId, GetString ("User group No.") },
                        { DataFilterLabel.UsersGroupsName, GetString ("User group") },
                        { DataFilterLabel.UsersGroupsCode, GetString ("User group code") },

                        #endregion

                        #region Operator filters

                        { DataFilterLabel.OperationsOperatorId, GetString ("Operator No.") },
                        { DataFilterLabel.OperationsOperatorCode, GetString ("Operator code") },
                        { DataFilterLabel.OperationsOperatorName, GetString ("Operator") },
                        { DataFilterLabel.OperationsOperatorName2, GetString ("Display operator") },
                        { DataFilterLabel.OperationsOperatorOrder, GetString ("Operator order") },
                        { DataFilterLabel.OperationsOperatorLevel, GetString ("Operator access level") },
                        { DataFilterLabel.OperationsOperatorCardNo, GetString ("Operator card No.") },

                        #endregion

                        #region Operator groups filters

                        { DataFilterLabel.OperationsOperatorsGroupsId, GetString ("Operator group No.") },
                        { DataFilterLabel.OperationsOperatorsGroupsName, GetString ("Operator group") },
                        { DataFilterLabel.OperationsOperatorsGroupsCode, GetString ("Operator group code") },

                        #endregion

                        #region Operation user filters

                        { DataFilterLabel.OperationsUserId, GetString ("Operation user No.") },
                        { DataFilterLabel.OperationsUserCode, GetString ("Operation user code") },
                        { DataFilterLabel.OperationsUserName, GetString ("Operation user") },
                        { DataFilterLabel.OperationsUserName2, GetString ("Display operation user") },
                        { DataFilterLabel.OperationsUserOrder, GetString ("Operation user order") },
                        { DataFilterLabel.OperationsUserLevel, GetString ("Operation user access level") },
                        { DataFilterLabel.OperationsUserCardNo, GetString ("Operation user card No.") },

                        #endregion

                        #region Operation user groups filters

                        { DataFilterLabel.OperationsUsersGroupsId, GetString ("Operation user group No.") },
                        { DataFilterLabel.OperationsUsersGroupsName, GetString ("Operation user group") },
                        { DataFilterLabel.OperationsUsersGroupsCode, GetString ("Operation user group code") },

                        #endregion

                        #region Location filters

                        { DataFilterLabel.LocationId, GetString ("Location No") },
                        { DataFilterLabel.LocationCode, GetString ("Code") },
                        { DataFilterLabel.LocationName, GetString ("Location") },
                        { DataFilterLabel.LocationName2, GetString ("Display location") },
                        { DataFilterLabel.LocationOrder, GetString ("Order") },
                        { DataFilterLabel.LocationDeleted, GetString ("Deleted") },
                        { DataFilterLabel.LocationPriceGroup, GetString ("Location price group") },
                        { DataFilterLabel.SourceLocationName, GetString ("From location") },
                        { DataFilterLabel.TargetLocationName, GetString ("To location") },

                        #endregion

                        #region Location group filters

                        { DataFilterLabel.LocationsGroupId, GetString ("Location group No") },
                        { DataFilterLabel.LocationsGroupName, GetString ("Location group") },
                        { DataFilterLabel.LocationsGroupCode, GetString ("Location group code") },

                        #endregion

                        #region Lot filters

                        { DataFilterLabel.LotId, GetString ("Lot No.") },
                        { DataFilterLabel.LotSerialNumber, GetString ("Serial number") },
                        { DataFilterLabel.LotExpirationDate, GetString ("Expiration date") },
                        { DataFilterLabel.LotProductionDate, GetString ("Production date") },
                        { DataFilterLabel.LotLocation, GetString ("Lot location") },

                        #endregion

                        #region Operation filters

                        { DataFilterLabel.OperationType, GetString ("Operation") },
                        { DataFilterLabel.OperationNumber, GetString ("Operation No.") },
                        { DataFilterLabel.OperationDate, GetString ("Date") },
                        { DataFilterLabel.OperationDateTime, GetString ("Date/Time") },
                        { DataFilterLabel.OperationTimeStamp, GetString ("Last modification") },
                        { DataFilterLabel.OperationDetailReferenceId, GetString ("Reference No.") },
                        { DataFilterLabel.OperationQuantity, GetString ("Quantity") },
                        { DataFilterLabel.OperationQuantitySum, GetString ("Total qtty") },
                        { DataFilterLabel.OperationQuantitySignedSum, GetString ("Total signed qtty") },
                        { DataFilterLabel.OperationSum, GetString ("Amount") },
                        { DataFilterLabel.OperationVatSum, config.UseSalesTaxInsteadOfVAT
                            ? (config.VATIncluded ? GetString ("Tax amount included") : GetString ("Tax amount"))
                            : (config.VATIncluded ? GetString ("VAT amount included") : GetString ("VAT amount"))},
                        { DataFilterLabel.OperationTotal, GetString ("Total amount") },
                        { DataFilterLabel.OperationProfit, GetString ("Profit") },

                        { DataFilterLabel.OperationDetailId, GetString ("Detail No.") },
                        { DataFilterLabel.OperationDetailQuantity, GetString ("Quantity") },
                        { DataFilterLabel.OperationDetailPriceIn, GetString ("Purchase price") },
                        { DataFilterLabel.OperationDetailPriceOut, GetString ("Sale price") },
                        { DataFilterLabel.OperationDetailDiscount, GetString ("Discount") },
                        { DataFilterLabel.OperationDetailSign, GetString ("Sign") },
                        { DataFilterLabel.OperationDetailVatIn, config.UseSalesTaxInsteadOfVAT
                            ? (config.VATIncluded ? GetString ("Tax given included") : GetString ("Tax given"))
                            : (config.VATIncluded ? GetString ("VAT given included") :GetString ("VAT given"))},
                        { DataFilterLabel.OperationDetailVatOut, config.UseSalesTaxInsteadOfVAT
                            ? (config.VATIncluded ? GetString ("Tax received included") : GetString ("Tax received"))
                            : (config.VATIncluded ? GetString ("VAT received included") :GetString ("VAT received"))},
                        { DataFilterLabel.OperationDetailVat, config.UseSalesTaxInsteadOfVAT
                            ? (config.VATIncluded ? GetString ("Tax included") : GetString ("Tax"))
                            : (config.VATIncluded ? GetString ("VAT included") :GetString ("VAT"))},
                        { DataFilterLabel.OperationDetailVatSum, config.UseSalesTaxInsteadOfVAT
                            ? (config.VATIncluded ? GetString ("Tax amount included") : GetString ("Tax amount"))
                            : (config.VATIncluded ? GetString ("VAT amount included") : GetString ("VAT amount"))},
                        { DataFilterLabel.OperationDetailSum, GetString ("Amount") },
                        { DataFilterLabel.OperationDetailTotal, GetString ("Total amount") },
                        { DataFilterLabel.OperationDetailNote, GetString ("Note") },
                        { DataFilterLabel.OperationDetailWarrantyPeriod, GetString ("Warranty Period") },
                        { DataFilterLabel.OperationDetailLot, GetString ("Lot") },
                        { DataFilterLabel.OperationDetailLotId, GetString ("Lot No.") },

                        #endregion

                        #region Payment filters

                        { DataFilterLabel.PaymentId, GetString ("Payment No.") },
                        { DataFilterLabel.PaymentOperationId, GetString ("Operation No.") },
                        { DataFilterLabel.PaymentOperationType, GetString ("Operation type") },
                        { DataFilterLabel.PaymentPartnerId, GetString ("Partner No.") },
                        { DataFilterLabel.PaymentAmount, GetString ("Amount") },
                        { DataFilterLabel.PaymentMode, GetString ("Mode") },
                        { DataFilterLabel.PaymentDate, GetString ("Date") },
                        { DataFilterLabel.PaymentOperatorId, GetString ("User No.") },
                        { DataFilterLabel.PaymentTimeStamp, GetString ("Last modification") },
                        { DataFilterLabel.PaymentTypeId, GetString ("Payment type") },
                        { DataFilterLabel.PaymentTransaction, GetString ("Transaction") },
                        { DataFilterLabel.PaymentEndDate, GetString ("Date of payment") },
                        { DataFilterLabel.PaymentLocationId, GetString ("Location No.") },
                        { DataFilterLabel.PaymentSign, GetString ("Sign") },
                        { DataFilterLabel.PaymentDueSum, GetString ("Payment due sum") },
                        { DataFilterLabel.PaymentRemainingSum, GetString ("Payment remaining sum") },
                        { DataFilterLabel.PaymentsInCash, GetString ("Payment in cash") },
                        { DataFilterLabel.PaymentsByBankOrder, GetString ("Bank payment") },
                        { DataFilterLabel.PaymentsByDebitCreditCard, GetString ("Payment by card") },
                        { DataFilterLabel.PaymentsByVoucher, GetString ("Payment by voucher") },

                        #endregion

                        #region Payment types filters

                        { DataFilterLabel.PaymentTypesId, GetString ("Payment type No.") },
                        { DataFilterLabel.PaymentTypesName, GetString ("Payment type name") },
                        { DataFilterLabel.PaymentTypesMethod, GetString ("Payment method") },

                        #endregion

                        #region VATGroups filters

                        { DataFilterLabel.VATGroupId, config.UseSalesTaxInsteadOfVAT ? GetString ("Tax group No.") : GetString ("VAT group No.") },
                        { DataFilterLabel.VATGroupCode, config.UseSalesTaxInsteadOfVAT ? GetString ("Tax group code") : GetString ("VAT group code") },
                        { DataFilterLabel.VATGroupName, config.UseSalesTaxInsteadOfVAT ? GetString ("Tax group name") : GetString ("VAT group name") },
                        { DataFilterLabel.VATGroupValue, config.UseSalesTaxInsteadOfVAT ? GetString ("Tax group value (%)") : GetString ("VAT group value (%)") },

                        #endregion

                        #region Documents filters

                        { DataFilterLabel.InvoiceId, GetString ("Document record No.") },
                        { DataFilterLabel.InvoiceOperationNumber, GetString ("Operation No.") },
                        { DataFilterLabel.InvoiceNumber, GetString ("Document No.") },
                        { DataFilterLabel.InvoiceOperationType, GetString ("Operation type") },
                        { DataFilterLabel.InvoiceDate, GetString ("Date") },
                        { DataFilterLabel.InvoiceDocumentType, GetString ("Document type") },
                        { DataFilterLabel.InvoiceReferenceDate, GetString ("Ref document date") },
                        { DataFilterLabel.InvoiceReferenceNumber, GetString ("Ref document No.") },
                        { DataFilterLabel.InvoiceRecipient, GetString ("Recipient") },
                        { DataFilterLabel.InvoiceRecipientEGN, GetString ("Recipient ID") },
                        { DataFilterLabel.InvoiceProvider, GetString ("Provider") },
                        { DataFilterLabel.InvoiceTaxDate, GetString ("Tax date") },
                        { DataFilterLabel.InvoiceReason, GetString ("Reason") },
                        { DataFilterLabel.InvoiceDescription, GetString ("Description") },
                        { DataFilterLabel.InvoiceLocation, GetString ("Point of sale") },
                        { DataFilterLabel.InvoicePaymentTypeId, GetString ("Payment type") },

                        #endregion

                    });
            }
        }

        public static string GetReportFilterName (DataFilterLabel field)
        {
            string value;
            return ReportFilterNames.TryGetValue (field, out value) ?
                value : Enum.GetName (typeof (DataFilterLabel), field);
        }
        private static Dictionary<DataFilterLabel, DataFilterLabel> migratedDataFieldMap;
        private static Dictionary<DataFilterLabel, DataFilterLabel> MigratedDataFieldMap
        {
            get
            {
                return migratedDataFieldMap ?? (migratedDataFieldMap = new Dictionary<DataFilterLabel, DataFilterLabel>
                    {
                        { DataFilterLabel.GoodsId, DataFilterLabel.ItemId },
                        { DataFilterLabel.GoodsCode, DataFilterLabel.ItemCode },
                        { DataFilterLabel.GoodsBarCode, DataFilterLabel.ItemBarcode },
                        { DataFilterLabel.GoodsCatalog, DataFilterLabel.ItemCatalog },
                        { DataFilterLabel.GoodsName, DataFilterLabel.ItemName },
                        { DataFilterLabel.GoodsName2, DataFilterLabel.ItemName2 },
                        { DataFilterLabel.GoodsMeasUnit, DataFilterLabel.ItemMeasUnit },
                        { DataFilterLabel.GoodsMeasUnit2, DataFilterLabel.ItemMeasUnit2 },
                        { DataFilterLabel.GoodsMeasRatio, DataFilterLabel.ItemMeasRatio },
                        { DataFilterLabel.GoodsTradeInPrice, DataFilterLabel.ItemTradeInPrice },
                        { DataFilterLabel.GoodsTradeOutPrice, DataFilterLabel.ItemTradeOutPrice },
                        { DataFilterLabel.GoodsTradePrice, DataFilterLabel.ItemTradePrice },
                        { DataFilterLabel.GoodsRegularPrice, DataFilterLabel.ItemRegularPrice },
                        { DataFilterLabel.GoodsPriceGroup1, DataFilterLabel.ItemPriceGroup1 },
                        { DataFilterLabel.GoodsPriceGroup2, DataFilterLabel.ItemPriceGroup2 },
                        { DataFilterLabel.GoodsPriceGroup3, DataFilterLabel.ItemPriceGroup3 },
                        { DataFilterLabel.GoodsPriceGroup4, DataFilterLabel.ItemPriceGroup4 },
                        { DataFilterLabel.GoodsPriceGroup5, DataFilterLabel.ItemPriceGroup5 },
                        { DataFilterLabel.GoodsPriceGroup6, DataFilterLabel.ItemPriceGroup6 },
                        { DataFilterLabel.GoodsPriceGroup7, DataFilterLabel.ItemPriceGroup7 },
                        { DataFilterLabel.GoodsPriceGroup8, DataFilterLabel.ItemPriceGroup8 },
                        { DataFilterLabel.GoodsMinQuantity, DataFilterLabel.ItemMinQuantity },
                        { DataFilterLabel.GoodsNomQuantity, DataFilterLabel.ItemNomQuantity },
                        { DataFilterLabel.GoodsDescription, DataFilterLabel.ItemDescription },
                        { DataFilterLabel.GoodsType, DataFilterLabel.ItemType },
                        { DataFilterLabel.GoodsOrder, DataFilterLabel.ItemOrder },
                        { DataFilterLabel.GoodsDeleted, DataFilterLabel.ItemDeleted },
                        { DataFilterLabel.GoodsGroupsId, DataFilterLabel.ItemsGroupId },
                        { DataFilterLabel.GoodsGroupsName, DataFilterLabel.ItemsGroupName },
                        { DataFilterLabel.GoodsGroupsCode, DataFilterLabel.ItemsGroupCode },
                        { DataFilterLabel.PointOfSaleId, DataFilterLabel.LocationId },
                        { DataFilterLabel.PointOfSaleCode, DataFilterLabel.LocationCode },
                        { DataFilterLabel.PointOfSaleName, DataFilterLabel.LocationName },
                        { DataFilterLabel.PointOfSaleName2, DataFilterLabel.LocationName2 },
                        { DataFilterLabel.PointOfSaleOrder, DataFilterLabel.LocationOrder },
                        { DataFilterLabel.PointOfSaleDeleted, DataFilterLabel.LocationDeleted },
                        { DataFilterLabel.PointOfSalePriceGroup, DataFilterLabel.LocationPriceGroup },
                        { DataFilterLabel.SourcePointOfSaleName, DataFilterLabel.SourceLocationName },
                        { DataFilterLabel.TargetPointOfSaleName, DataFilterLabel.TargetLocationName },
                        { DataFilterLabel.PointsOfSaleGroupId, DataFilterLabel.LocationsGroupId },
                        { DataFilterLabel.PointsOfSaleGroupName, DataFilterLabel.LocationsGroupName },
                        { DataFilterLabel.PointsOfSaleGroupCode, DataFilterLabel.LocationsGroupCode },
                        { DataFilterLabel.InvoicePointOfSale, DataFilterLabel.InvoiceLocation },
                        { DataFilterLabel.StoreGoodsAvailableQuantitySum, DataFilterLabel.StoreItemAvailableQuantitySum },
                        { DataFilterLabel.StoreGoodsCountedQuantity, DataFilterLabel.StoreItemCountedQuantity },
                    });
            }
        }

        public static DataFilterLabel GetMigratedFilterLabel (DataFilterLabel field)
        {
            DataFilterLabel ret;
            return MigratedDataFieldMap.TryGetValue (field, out ret) ? ret : field;
        }

        public static KeyValuePair<DbField, string> [] GetReportFieldColumnNames (IEnumerable<DbField> fields)
        {
            return fields.Select (field => new KeyValuePair<DbField, string> (field, GetReportFieldColumnName (field))).ToArray ();
        }

        public static string GetExchangeFieldName (DbField field)
        {
            switch (field.StrongField) {
                #region User properties

                case DataField.UserCode:
                    return GetString ("Code:");
                case DataField.UserName:
                    return GetString ("Name:");
                case DataField.UserName2:
                    return GetString ("Display name:");
                case DataField.UserPassword:
                    return GetString ("Encrypted password:");
                case DataField.UserGroupId:
                    return GetString ("Group id:");
                case DataField.UserCardNo:
                    return GetString ("Card number:");
                case DataField.UserLevel:
                    return GetString ("Access level:");
                case DataField.UsersGroupsName:
                    return GetString ("Group:");

                #endregion

                #region Location properties

                case DataField.LocationCode:
                    return GetString ("Code:");
                case DataField.LocationName:
                    return GetString ("Name:");
                case DataField.LocationName2:
                    return GetString ("Display name:");
                case DataField.LocationGroupId:
                    return GetString ("Group id:");
                case DataField.LocationPriceGroup:
                    return GetString ("Price group:");
                case DataField.LocationsGroupsName:
                    return GetString ("Group:");

                #endregion

                #region Partner properties

                case DataField.PartnerCode:
                    return GetString ("Code:");
                case DataField.PartnerName:
                    return GetString ("Name:");
                case DataField.PartnerLiablePerson:
                    return GetString ("Contact Name:");
                case DataField.PartnerCity:
                    return GetString ("City:");
                case DataField.PartnerAddress:
                    return GetString ("Address:");
                case DataField.PartnerPhone:
                    return GetString ("Phone:");
                case DataField.PartnerFax:
                    return GetString ("Fax:");
                case DataField.PartnerBulstat:
                    return GetString ("UIC:");
                case DataField.PartnerTaxNumber:
                    return config.UseSalesTaxInsteadOfVAT ? GetString ("Tax number:") : GetString ("VAT number:");
                case DataField.PartnerEmail:
                    return GetString ("e-mail:");
                case DataField.PartnerBankName:
                    return GetString ("Bank name:");
                case DataField.PartnerBankCode:
                    return GetString ("BIC:");
                case DataField.PartnerBankAcct:
                    return GetString ("IBAN:");
                case DataField.PartnerBankVATName:
                    return config.UseSalesTaxInsteadOfVAT ? GetString ("Tax bank name:") : GetString ("VAT bank name:");
                case DataField.PartnerBankVATCode:
                    return config.UseSalesTaxInsteadOfVAT ? GetString ("Tax BIC:") : GetString ("VAT BIC:");
                case DataField.PartnerBankVATAcct:
                    return config.UseSalesTaxInsteadOfVAT ? GetString ("Tax IBAN:") : GetString ("VAT IBAN:");
                case DataField.PartnerPriceGroup:
                    return GetString ("Price group:");
                case DataField.PartnerType:
                    return GetString ("Partner type:");
                case DataField.PartnerName2:
                    return GetString ("Display name:");
                case DataField.PartnerLiablePerson2:
                    return GetString ("Display contact name:");
                case DataField.PartnerCity2:
                    return GetString ("Display city:");
                case DataField.PartnerAddress2:
                    return GetString ("Display address:");
                case DataField.PartnerPhone2:
                    return GetString ("Display phone:");
                case DataField.PartnerGroupId:
                    return GetString ("Group id:");
                case DataField.PartnersGroupsName:
                    return GetString ("Group:");
                case DataField.PartnerCardNumber:
                    return GetString ("Card number:");
                case DataField.PartnerDebt:
                    return GetString ("Partner debt:");

                #endregion

                #region Item properties

                case DataField.ItemCode:
                    return GetString ("Code:");
                case DataField.ItemBarcode1:
                    return GetString ("Barcode 1:");
                case DataField.ItemBarcode2:
                    return GetString ("Barcode 2:");
                case DataField.ItemBarcode3:
                    return GetString ("Barcode 3:");
                case DataField.ItemCatalog1:
                    return GetString ("Catalogue 1:");
                case DataField.ItemCatalog2:
                    return GetString ("Catalogue 2:");
                case DataField.ItemCatalog3:
                    return GetString ("Catalogue 3:");
                case DataField.ItemName:
                    return GetString ("Name:");
                case DataField.ItemName2:
                    return GetString ("Name 2:");
                case DataField.ItemMeasUnit:
                    return GetString ("Measure:");
                case DataField.ItemMeasUnit2:
                    return GetString ("Measure 2:");
                case DataField.ItemMeasRatio:
                    return GetString ("Measure ratio:");
                case DataField.ItemPurchasePrice:
                    return GetString ("Purchase price:");
                //case DataField.ItemTradeOutPrice:
                //    return GetString ("Sale price:");
                case DataField.ItemTradePrice:
                    return GetString ("Wholesale price:");
                case DataField.ItemRegularPrice:
                    return GetString ("Retail price:");
                case DataField.ItemPriceGroup1:
                    return GetString ("Price group 1:");
                case DataField.ItemPriceGroup2:
                    return GetString ("Price group 2:");
                case DataField.ItemPriceGroup3:
                    return GetString ("Price group 3:");
                case DataField.ItemPriceGroup4:
                    return GetString ("Price group 4:");
                case DataField.ItemPriceGroup5:
                    return GetString ("Price group 5:");
                case DataField.ItemPriceGroup6:
                    return GetString ("Price group 6:");
                case DataField.ItemPriceGroup7:
                    return GetString ("Price group 7:");
                case DataField.ItemPriceGroup8:
                    return GetString ("Price group 8:");
                case DataField.ItemMinQuantity:
                    return GetString ("Minimal quantity:");
                case DataField.ItemNomQuantity:
                    return GetString ("Nominal quantity:");
                case DataField.ItemDescription:
                    return GetString ("Description:");
                case DataField.ItemType:
                    return GetString ("Type:");
                case DataField.ItemGroupId:
                    return GetString ("Group id:");
                case DataField.ItemIsRecipe:
                    return GetString ("From recipe:");
                case DataField.ItemTaxGroupId:
                    return BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ? GetString ("Tax Group Id:") : GetString ("VAT Group Id:");
                case DataField.ItemsGroupName:
                    return GetString ("Group:");
                case DataField.StoreQtty:
                    return GetString ("Available quantity:");

                #endregion

                #region VAT group properties

                case DataField.VATGroupCode:
                    return GetString ("Code:");
                case DataField.VATGroupName:
                    return GetString ("Name:");
                case DataField.VATGroupValue:
                    return GetString ("Value (%):");

                #endregion

                case DataField.OperationDetailQuantity:
                    return GetString ("Quantity:");

                default:
                    throw new ArgumentOutOfRangeException ();
            }
        }

        private static Dictionary<DbField, string> reportFieldColumnNames;
        private static Dictionary<DbField, string> ReportFieldColumnNames
        {
            get
            {
                return reportFieldColumnNames ?? (reportFieldColumnNames = new Dictionary<DbField, string>
                    {
                        #region Applicationlog table columns

                        { DataField.AppLogId, GetString ("Message No.") },
                        { DataField.AppLogMessage, GetString ("Message") },
                        { DataField.AppLogTimeStamp, GetString ("Creation Date/Time") },
                        { DataField.AppLogMessageSource, GetString ("Source") },

                        #endregion

                        #region Cashbook table columns

                        { DataField.CashEntryId, GetString ("Entry No.") },
                        { DataField.CashEntryDate, GetString ("Date") },
                        { DataField.CashEntryDescription, GetString ("Description") },
                        { DataField.CashEntryTurnoverType, GetString ("Turnover Type") },
                        { DataField.CashEntryDirection, GetString ("Direction") },
                        { DataField.CashEntryAmount, GetString ("Amount") },
                        { DataField.CashEntryTimeStamp, GetString ("Date/Time") },

                        #endregion

                        #region Documents table columns

                        { DataField.DocumentId, GetString ("Document Record No.") },
                        { DataField.DocumentOperationNumber, GetString ("Operation No.") },
                        { DataField.DocumentNumber, GetString ("Document No.") },
                        { DataField.DocumentOperationType, GetString ("Operation Type") },
                        { DataField.DocumentDate, GetString ("Date") },
                        { DataField.DocumentType, GetString ("Document Type") },
                        { DataField.DocumentReferenceDate, GetString ("Ref Document Date") },
                        { DataField.DocumentReferenceNumber, GetString ("Ref Document No.") },
                        { DataField.DocumentPaymentTypeId, GetString ("Payment Method No.") },
                        { DataField.DocumentRecipient, GetString ("Recipient") },
                        { DataField.DocumentRecipientEGN, GetString ("Recipient ID") },
                        { DataField.DocumentProvider, GetString ("Provider") },
                        { DataField.DocumentTaxDate, GetString ("Tax Date") },
                        { DataField.DocumentReason, GetString ("Reason") },
                        { DataField.DocumentDescription, GetString ("Description") },
                        { DataField.DocumentLocation, GetString ("Location") },

                        #endregion

                        #region Item table columns

                        { DataField.ItemId ,GetString ("Item No.") },
                        { DataField.ItemCode ,GetString ("Code") },
                        { DataField.ItemBarcode1, GetString ("Barcode 1") },
                        { DataField.ItemBarcode2, GetString ("Barcode 2") },
                        { DataField.ItemBarcode3, GetString ("Barcode 3") },
                        { DataField.ItemCatalog1, GetString ("Catalogue 1") },
                        { DataField.ItemCatalog2, GetString ("Catalogue 2") },
                        { DataField.ItemCatalog3, GetString ("Catalogue 3") },
                        { DataField.ItemName ,GetString ("Item") },
                        { DataField.ItemName2 ,GetString ("Item Display Name") },
                        { DataField.ItemMeasUnit, GetString ("Measure") },
                        { DataField.ItemMeasUnit2, GetString ("Measure 2") },
                        { DataField.ItemMeasRatio, GetString ("Ratio") },
                        { DataField.ItemPurchasePrice, GetString ("Purchase Price") },
                        //{ DataField.ItemTradeOutPrice, GetString ("Sale price") },
                        { DataField.ItemTradePrice, GetString ("Wholesale Price") },
                        { DataField.ItemRegularPrice, GetString ("Retail Price") },
                        { DataField.ItemPriceGroup1, GetString ("Price Group 1") },
                        { DataField.ItemPriceGroup2, GetString ("Price Group 2") },
                        { DataField.ItemPriceGroup3, GetString ("Price Group 3") },
                        { DataField.ItemPriceGroup4, GetString ("Price Group 4") },
                        { DataField.ItemPriceGroup5, GetString ("Price Group 5") },
                        { DataField.ItemPriceGroup6, GetString ("Price Group 6") },
                        { DataField.ItemPriceGroup7, GetString ("Price Group 7") },
                        { DataField.ItemPriceGroup8, GetString ("Price Group 8") },
                        { DataField.ItemMinQuantity, GetString ("Minimal Quantity") },
                        { DataField.ItemNomQuantity, GetString ("Nom. Quantity") },
                        { DataField.ItemDescription, GetString ("Description") },
                        { DataField.ItemType, GetString ("Type") },
                        { DataField.ItemOrder, GetString ("Order") },
                        { DataField.ItemDeleted, GetString ("Deleted") },
                        { DataField.ItemTradeInVAT, config.UseSalesTaxInsteadOfVAT? 
                            GetString ("Purchase Tax Amount") : GetString ("Purchase VAT Amount") },
                        { DataField.ItemTradeInSum, GetString ("Purchase Amount") },
                        { DataField.ItemTradeVAT, config.UseSalesTaxInsteadOfVAT ?
                            GetString ("Sale Tax Amount") : GetString ("Sale VAT Amount") },
                        { DataField.ItemTradeSum, GetString ("Sale Amount") },

                        #endregion

                        #region Goodsgroups table columns

                        { DataField.ItemsGroupId, GetString ("Item Group No.") },
                        { DataField.ItemsGroupName, GetString ("Item Group") },
                        { DataField.ItemsGroupCode, GetString ("Item Group Code") },

                        #endregion

                        #region Lot table columns

                        { DataField.LotId, GetString ("Lot No.") },
                        { DataField.LotSerialNumber, GetString ("Serial Number") },
                        { DataField.LotExpirationDate, GetString ("Expiration Date") },
                        { DataField.LotProductionDate, GetString ("Production Date") },
                        { DataField.LotLocation, GetString ("Lot Location") },

                        #endregion

                        #region Objects table columns

                        { DataField.LocationId, GetString ("Location No.") },
                        { DataField.LocationCode, GetString ("Code") },
                        { DataField.LocationName, GetString ("Location") },
                        { DataField.LocationName2, GetString ("Display Location") },
                        { DataField.LocationOrder, GetString ("Location Order") },
                        { DataField.LocationDeleted, GetString ("Deleted") },
                        { DataField.LocationPriceGroup, GetString ("Location Price Group") },
                        { DataField.SourceLocationName, GetString ("From location") },
                        { DataField.TargetLocationName, GetString ("To location") },

                        #endregion

                        #region Objectsgroups table columns

                        { DataField.LocationsGroupsId, GetString ("Location Group No.") },
                        { DataField.LocationsGroupsName, GetString ("Location Group") },
                        { DataField.LocationsGroupsCode, GetString ("Location Group Code") },

                        #endregion

                        #region Operations table columns

                        { DataField.OperationType, GetString ("Operation") },
                        { DataField.OperationTimeStamp, GetString ("Last Modification") },
                        { DataField.OperationNumber, GetString ("Operation No.") },
                        { DataField.OperationDetailReferenceId, GetString ("Reference No.") },
                      
                        { DataField.OperationDate, GetString ("Date") },
                        { DataField.OperationDateTime, GetString ("Date/Time") },
                        { DataField.OperationQuantitySum, GetString ("Total Qtty") },
                        { DataField.OperationQuantitySignedSum, GetString ("Total Signed Qtty") },
                        { DataField.OperationSum, GetString ("Amount") },
                        { DataField.OperationVatSum, config.UseSalesTaxInsteadOfVAT
                            ? (config.VATIncluded ? GetString ("Tax Amount Included") : GetString ("Tax Amount"))
                            : (config.VATIncluded ? GetString ("VAT Amount Included") : GetString ("VAT Amount"))},
                        { DataField.OperationTotal, GetString ("Total Amount") },
                        { DataField.OperationProfit, GetString ("Profit") },

                        { DataField.OperationDetailId, GetString ("Detail No.") },
                        { DataField.OperationDetailItemId, GetString ("Item No.") },
                        { DataField.OperationDetailAvailableQuantity, GetString ("Available") },
                        { DataField.OperationDetailQuantity, GetString ("Quantity") },
                        { DataField.OperationDetailDifference, GetString ("Difference") },
                        { DataField.OperationDetailVatIn, config.UseSalesTaxInsteadOfVAT
                            ? (config.VATIncluded ? GetString ("Tax Given Included") : GetString ("Tax Given"))
                            : (config.VATIncluded ? GetString ("VAT Given Included") : GetString ("VAT Given"))},
                        { DataField.OperationDetailVatOut, config.UseSalesTaxInsteadOfVAT
                            ? (config.VATIncluded ? GetString ("Tax Received Included") : GetString ("Tax Received"))
                            : (config.VATIncluded ? GetString ("VAT Received Included") : GetString ("VAT Received"))},
                        { DataField.OperationDetailVat, config.UseSalesTaxInsteadOfVAT
                            ? (config.VATIncluded ? GetString ("Tax Included") : GetString ("Tax"))
                            : (config.VATIncluded ? GetString ("VAT Included") :GetString ("VAT"))},
                        { DataField.OperationDetailSumVatIn, config.UseSalesTaxInsteadOfVAT
                            ? (config.VATIncluded ? GetString ("Tax Given Amount Included") : GetString ("Tax Given Amount"))
                            : (config.VATIncluded ? GetString ("VAT Given Amount Included") : GetString ("VAT Given Amount"))},
                        { DataField.OperationDetailSumVatOut, config.UseSalesTaxInsteadOfVAT
                            ? (config.VATIncluded ? GetString ("Tax Received Amount Included") : GetString ("Tax Received Amount"))
                            : (config.VATIncluded ? GetString ("VAT Received Amount Included") : GetString ("VAT Received Amount"))},
                        { DataField.OperationDetailSumVat, config.UseSalesTaxInsteadOfVAT
                            ? (config.VATIncluded ? GetString ("Tax Amount Included") : GetString ("Tax Amount"))
                            : (config.VATIncluded ? GetString ("VAT Amount Included") : GetString ("VAT Amount"))},
                        { DataField.OperationDetailLot, GetString ("Lot") },
                        { DataField.OperationDetailWarrantySerialNumber, GetString ("Serial Number") },
                        { DataField.OperationDetailLotId, GetString ("Lot No.") },
                        { DataField.OperationDetailDiscount, GetString ("Discount") },
                        { DataField.OperationDetailSign, GetString ("Sign") },
                        { DataField.OperationDetailPriceIn, GetString ("Purchase Price") },
                        { DataField.OperationDetailPriceOut, GetString ("Sale Price") },
                        { DataField.OperationDetailSumIn, GetString ("Amount") },
                        { DataField.OperationDetailSumOut, GetString ("Amount") },
                        { DataField.OperationDetailSum, GetString ("Amount") },
                        { DataField.OperationDetailTotalIn, GetString ("Total Amount") },
                        { DataField.OperationDetailTotalOut, GetString ("Total Amount") },
                        { DataField.OperationDetailTotal, GetString ("Total Amount") },
                        { DataField.OperationDetailNote, GetString ("Note") },
                        { DataField.OperationDetailWarrantyPeriod, GetString ("Warranty Period") },
                        { DataField.ConsignmentDetailRemainingQuantity, GetString ("Consigned Qty") },

                        { DataField.PurchaseSum, GetString ("Purchase Amount") },
                        { DataField.PurchaseVATSum, config.UseSalesTaxInsteadOfVAT
                            ? (config.VATIncluded ? GetString ("Purchase Tax Included") : GetString ("Purchase Tax"))
                            : (config.VATIncluded ? GetString ("Purchase VAT Included") : GetString ("Purchase VAT"))},
                        { DataField.PurchaseTotal, GetString ("Total Purchase Amount") },
                        { DataField.SaleSum, GetString ("Sale Amount") },
                        { DataField.SaleVATSum, config.UseSalesTaxInsteadOfVAT
                            ? (config.VATIncluded ? GetString ("Sale Tax Included") : GetString ("Sale Tax"))
                            : (config.VATIncluded ? GetString ("Sale VAT Included") : GetString ("Sale VAT"))},
                        { DataField.SaleTotal, GetString ("Total Sale Amount") },
                        { DataField.StockTakingDetailPrice, GetString ("Stock-taking Price") },
                        { DataField.StockTakingTotal, GetString ("Total stock-taking Amount") },
                        { DataField.StockTakingSum, GetString ("Stock-taking Amount") },
                        { DataField.PurchaseQuantitySum, GetString ("Purchase Qtty") },
                        { DataField.SaleQuantitySum, GetString ("Sale Qtty") },
                        { DataField.WasteQuantitySum, GetString ("Waste Qtty") },
                        { DataField.StockTakingQuantitySum, GetString ("Stock-taking Qtty") },
                        { DataField.TransferInQuantitySum, GetString ("Transfer to Location") },
                        { DataField.TransferOutQuantitySum, GetString ("Transfer from Location") },
                        { DataField.WriteOffQuantitySum, GetString ("Written-off Qtty") },
                        { DataField.ConsignedQuantitySum, GetString ("Consigned Qty") },
                        { DataField.ConsignedQuantitySoldSum, GetString ("Sold Qty") },
                        { DataField.ConsignedQuantityReturnedSum, GetString ("Returned Qty") },
                        { DataField.ProductionMatQuantitySum, GetString ("Production Material") },
                        { DataField.ProductionProdQuantitySum, GetString ("Production Product") },
                        { DataField.DebitNoteQuantitySum, GetString ("Debit Note Qtty") },
                        { DataField.CreditNoteQuantitySum, GetString ("Credit Note Qtty") },
                        { DataField.ReturnQuantitySum, GetString ("Returned Qtty") },
                        { DataField.PurchaseReturnQuantitySum, GetString ("Returned to Supplier Qtty") },
                        { DataField.ReturnSum, GetString ("Returned Amount") },
                        { DataField.RemainingQuantity, GetString ("Remaining Qty") },

                        #endregion

                        #region Partners table columns

                        { DataField.PartnerId, GetString ("Partner No.") },
                        { DataField.PartnerCode, GetString ("Partner Code") },
                        { DataField.PartnerName, GetString ("Partner Name") },
                        { DataField.PartnerName2, GetString ("Display Partner Name") },
                        { DataField.PartnerLiablePerson, GetString ("Contact Name") },
                        { DataField.PartnerLiablePerson2, GetString ("Contact Display Name") },
                        { DataField.PartnerCity, GetString ("City") },
                        { DataField.PartnerCity2, GetString ("Display City") },
                        { DataField.PartnerAddress, GetString ("Address") },
                        { DataField.PartnerAddress2, GetString ("Display Address") },
                        { DataField.PartnerPhone, GetString ("Phone") },
                        { DataField.PartnerPhone2, GetString ("Display Phone") },
                        { DataField.PartnerFax, GetString ("Fax") },
                        { DataField.PartnerEmail, GetString ("e-mail") },
                        { DataField.PartnerTaxNumber, config.UseSalesTaxInsteadOfVAT ? 
                            GetString ("Tax Number") : GetString ("VAT Number") },
                        { DataField.PartnerBulstat, GetString ("UIC") },
                        { DataField.PartnerBankName, GetString ("Bank") },
                        { DataField.PartnerBankCode, GetString ("BIC") },
                        { DataField.PartnerBankAcct, GetString ("IBAN") },
                        { DataField.PartnerBankVATName, config.UseSalesTaxInsteadOfVAT ? 
                            GetString ("Tax Bank") : GetString ("VAT Bank") },
                        { DataField.PartnerBankVATCode, config.UseSalesTaxInsteadOfVAT ? 
                            GetString ("Tax BIC") : GetString ("VAT BIC") },
                        { DataField.PartnerBankVATAcct, config.UseSalesTaxInsteadOfVAT ?
                            GetString ("Tax IBAN") : GetString ("VAT IBAN") },
                        { DataField.PartnerPriceGroup, GetString ("Price Group") },
                        { DataField.PartnerDiscount, GetString ("Discount (%)") },
                        { DataField.PartnerType, GetString ("Type") },
                        { DataField.PartnerOrder, GetString ("Order") },
                        { DataField.PartnerDeleted, GetString ("Deleted") },
                        { DataField.PartnerTimeStamp, GetString ("Last Modified") },
                        { DataField.PartnerCardNumber, GetString ("Card No.") },
                        { DataField.PartnerNote, GetString ("Note") },
                        { DataField.PartnerNote2, GetString ("Note 2") },
                        { DataField.PartnerDebt, GetString ("Partner Debt") },

                        #endregion

                        #region Partnersgroups table columns

                        { DataField.PartnersGroupsId, GetString ("Partner Group No.") },
                        { DataField.PartnersGroupsName, GetString ("Partner Group") },
                        { DataField.PartnersGroupsCode, GetString ("Partner Group Code") },

                        #endregion

                        #region Payments table columns

                        { DataField.PaymentId, GetString ("Payment No.") },
                        { DataField.PaymentOperationId, GetString ("Operation No.") },
                        { DataField.PaymentOperationType, GetString ("Operation Type") },
                        { DataField.PaymentPartnerId, GetString ("Partner No.") },
                        { DataField.PaymentAmount, GetString ("Amount") },
                        { DataField.PaymentMode, GetString ("Mode") },
                        { DataField.PaymentDate, GetString ("Date") },
                        { DataField.PaymentOperatorId, GetString ("User No.") },
                        { DataField.PaymentTimeStamp, GetString ("Last Modification") },
                        { DataField.PaymentTypeId, GetString ("Payment Type") },
                        { DataField.PaymentTransaction, GetString ("Transaction") },
                        { DataField.PaymentEndDate, GetString ("Date of Payment") },
                        { DataField.PaymentLocationId, GetString ("Location No.") },
                        { DataField.PaymentSign, GetString ("Sign") },
                        { DataField.PaymentDueSum, GetString ("Payment Due Sum") },
                        { DataField.PaymentRemainingSum, GetString ("Payment Remaining Sum") },
                        { DataField.PaymentsInCash, GetString ("Payments in Cash") },
                        { DataField.PaymentsByBankOrder, GetString ("Bank Payments") },
                        { DataField.PaymentsByDebitCreditCard, GetString ("Payments by Card") },
                        { DataField.PaymentsByVoucher, GetString ("Payments by Voucher") },

                        #endregion

                        #region Paymenttypes table column

                        { DataField.PaymentTypesId, GetString ("Payment Type No.") },
                        { DataField.PaymentTypesName, GetString ("Payment Type") },
                        { DataField.PaymentTypesMethod, GetString ("Payment Method") },

                        #endregion

                        #region Registration table columns

                        #endregion

                        #region Store table columns

                        { DataField.StorePrice, GetString ("Purchase Price") },
                        { DataField.StoreQtty, GetString ("Quantity") },
                        { DataField.StoreLot, GetString ("Lot") },
                        { DataField.StoreAvailableQuantity, GetString ("Quantity") },
                        { DataField.StoreCountedQuantity, GetString ("Counted") },

                        #endregion

                        #region Users table columns

                        { DataField.UserId, GetString ("User No.") },
                        { DataField.UserCode, GetString ("Code") },
                        { DataField.UserName, GetString ("User") },
                        { DataField.UserName2, GetString ("Display User") },
                        { DataField.UserOrder, GetString ("Order") },
                        { DataField.UserDeleted, GetString ("Deleted") },
                        { DataField.UserLevel, GetString ("Access Level") },
                        { DataField.UserCardNo, GetString ("Card No.") },

                        #endregion

                        #region Users groups columns

                        { DataField.UsersGroupsId, GetString ("User Group No.") },
                        { DataField.UsersGroupsName, GetString ("User Group") },
                        { DataField.UsersGroupsCode, GetString ("User Group Code") },

                        #endregion

                        #region Operator columns

                        { DataField.OperationsOperatorId, GetString ("Operator No.") },
                        { DataField.OperationsOperatorCode, GetString ("Operator Code") },
                        { DataField.OperationsOperatorName, GetString ("Operator") },
                        { DataField.OperationsOperatorName2, GetString ("Display Operator") },
                        { DataField.OperationsOperatorOrder, GetString ("Operator Order") },
                        { DataField.OperationsOperatorLevel, GetString ("Operator Access Level") },
                        { DataField.OperationsOperatorCardNo, GetString ("Operator Card No.") },

                        #endregion

                        #region Operator groups columns

                        { DataField.OperationsOperatorsGroupsId, GetString ("Operator Group No.") },
                        { DataField.OperationsOperatorsGroupsName, GetString ("Operator Group") },
                        { DataField.OperationsOperatorsGroupsCode, GetString ("Operator Group Code") },

                        #endregion

                        #region Operation user columns

                        { DataField.OperationsUserId, GetString ("Operation User No.") },
                        { DataField.OperationsUserCode, GetString ("Operation User Code") },
                        { DataField.OperationsUserName, GetString ("Operation User") },
                        { DataField.OperationsUserName2, GetString ("Display Operation User") },
                        { DataField.OperationsUserOrder, GetString ("Operation User Order") },
                        { DataField.OperationsUserLevel, GetString ("Operation User Access Level") },
                        { DataField.OperationsUserCardNo, GetString ("Operation User Card No.") },

                        #endregion

                        #region Operation user groups columns

                        { DataField.OperationsUsersGroupsId, GetString ("Operation User Group No.") },
                        { DataField.OperationsUsersGroupsName, GetString ("Operation User Group") },
                        { DataField.OperationsUsersGroupsCode, GetString ("Operation User Group Code") },

                        #endregion

                        #region VATGroups table columns

                        { DataField.VATGroupId, config.UseSalesTaxInsteadOfVAT ? GetString ("Tax Group No.") : GetString ("VAT Group No.") },
                        { DataField.VATGroupCode, config.UseSalesTaxInsteadOfVAT ? GetString ("Tax Group Code") : GetString ("VAT Group Code") },
                        { DataField.VATGroupName, config.UseSalesTaxInsteadOfVAT ? GetString ("Tax Group Name") : GetString ("VAT Group Name") },
                        { DataField.VATGroupValue, config.UseSalesTaxInsteadOfVAT ? GetString ("Tax Group Value (%)") : GetString ("VAT Group Value (%)") },

                        #endregion

                        #region Other columns

                        { DataField.PriceGroup, GetString ("Price group") },
                        { DataField.ReportDate, GetString ("Date") },
                        { DataField.NotSet, GetString ("No column") },

                        #endregion
                    });
            }
        }

        public static string GetReportFieldColumnName (DbField field)
        {
            string value;
            return ReportFieldColumnNames.TryGetValue (field, out value) ?
                value : Enum.GetName (typeof (DataField), field.StrongField);
        }

        public static string GetCommandName (DeviceCommands cmd)
        {
            switch (cmd) {
                #region Initialization commands

                case DeviceCommands.SetDateTime:
                    return GetString ("Set Date/Time");
                case DeviceCommands.SetVATRates:
                    return GetString ("Set VAT Rates");
                case DeviceCommands.SetFooterText:
                    return GetString ("Set Footer Text");
                case DeviceCommands.SetPrintingOptions:
                    return GetString ("Set Printing Options");
                case DeviceCommands.SetGraphicalLogo:
                    return GetString ("Set Graphical Logo");
                case DeviceCommands.SetAutoPaperCut:
                    return GetString ("Set Auto Paper Cut");
                case DeviceCommands.SetZeroRegisterPrint:
                    return GetString ("Set Zero Registers Print");
                case DeviceCommands.SetJournalPrintSpeed:
                    return GetString ("Set Journal Print Speed");
                case DeviceCommands.SetReceiptEndFeed:
                    return GetString ("Set Receipt End Feed");
                case DeviceCommands.SetBufferedPrint:
                    return GetString ("Set Buffered Print");
                case DeviceCommands.SetPrintContrast:
                    return GetString ("Set Print Contrast");
                case DeviceCommands.SetBarcodeHeight:
                    return GetString ("Set Barcode Height");
                case DeviceCommands.GetHeaderText:
                    return GetString ("Get Header Text");
                case DeviceCommands.GetFooterText:
                    return GetString ("Get Footer Text");
                case DeviceCommands.GetPrintingOptions:
                    return GetString ("Get Printing Options");
                case DeviceCommands.GetGraphicalLogo:
                    return GetString ("Get Graphical Logo");
                case DeviceCommands.GetAutoPaperCut:
                    return GetString ("Get Auto Paper Cut");
                case DeviceCommands.GetZeroRegisterPrint:
                    return GetString ("Get Zero Registers Print");
                case DeviceCommands.GetJournalPrintSpeed:
                    return GetString ("Get Journal Print Speed");
                case DeviceCommands.GetReceiptEndFeed:
                    return GetString ("Get Receipt End Feed");
                case DeviceCommands.GetBufferedPrint:
                    return GetString ("Get Buffered Print");
                case DeviceCommands.GetPrintContrast:
                    return GetString ("Get Print Contrast");
                case DeviceCommands.GetBarcodeHeight:
                    return GetString ("Get Barcode Height");
                case DeviceCommands.SetGraphicalLogoBitmap:
                    return GetString ("Set Graphical Logo Bitmap");
                case DeviceCommands.SetTextField:
                    return GetString ("Set Text Field");
                case DeviceCommands.GetTextField:
                    return GetString ("Get Text Field");
                case DeviceCommands.SetPaymentName:
                    return GetString ("Set Payment Name");
                case DeviceCommands.GetPaymentName:
                    return GetString ("Get Payment Name");
                case DeviceCommands.SetReceiptCode:
                    return GetString ("Set Receipt Code");
                case DeviceCommands.SetOperator:
                    return GetString ("Set Operator");
                case DeviceCommands.SetPartner:
                    return GetString ("Set Partner");
                case DeviceCommands.SetLocation:
                    return GetString ("Set Location");

                #endregion

                #region Non fiscal commands

                case DeviceCommands.OpenNonFiscal:
                    return GetString ("Open Non-Fiscal Operation");
                case DeviceCommands.PrintTitleNonFiscal:
                    return GetString ("Print Non-Fiscal Title");
                case DeviceCommands.PrintTextNonFiscal:
                    return GetString ("Print Non-Fiscal Text");
                case DeviceCommands.PrintKeyValueNonFiscal:
                    return GetString ("Print Non-Fiscal Key Value");
                case DeviceCommands.AddItemNonFiscal:
                    return GetString ("Add Item To Non-Fiscal Operation");
                case DeviceCommands.AddModifiedItemNonFiscal:
                    return GetString ("Add Modified Item To Non-Fiscal Operation");
                case DeviceCommands.CloseNonFiscal:
                    return GetString ("Close Non-Fiscal Operation");

                #endregion

                #region Fiscal commands

                case DeviceCommands.InitFiscal:
                    return GetString ("Init Fiscal Operation");
                case DeviceCommands.OpenFiscal:
                    return GetString ("Open Fiscal Operation");
                case DeviceCommands.AddItem:
                    return GetString ("Add Item To Fiscal Operation");
                case DeviceCommands.Subtotal:
                    return GetString ("Subtotal");
                case DeviceCommands.Payment:
                    return GetString ("Payment");
                case DeviceCommands.CloseFiscal:
                    return GetString ("Close Fiscal Operation");
                case DeviceCommands.CancelFiscalOperation:
                    return GetString ("Cancel Fiscal Operation");
                case DeviceCommands.PrintTextFiscal:
                    return GetString ("Print Fiscal Text");
                case DeviceCommands.PrintDuplicate:
                    return GetString ("Print Dublicate");

                #endregion

                #region Printer commands

                case DeviceCommands.PaperFeed:
                    return GetString ("Paper Feed");
                case DeviceCommands.PaperCut:
                    return GetString ("Cut The Paper");
                case DeviceCommands.PrintBarcode:
                    return GetString ("Print Barcode");
                case DeviceCommands.PrintSignature:
                    return GetString ("Print Signature");
                case DeviceCommands.SetPrinterFonts:
                    return GetString ("Set Printer Fonts");

                #endregion

                #region Information commands

                case DeviceCommands.GetFiscalReceiptStatus:
                    return GetString ("Get Fiscal Receipt Status");
                case DeviceCommands.GetDiagnosticInfo:
                    return GetString ("Get Diagnostic Info");
                case DeviceCommands.GetVATRatesInfo:
                    return GetString ("Get VAT Rates");
                case DeviceCommands.GetTaxNumber:
                    return GetString ("Get Tax Number");
                case DeviceCommands.GetLastDocumentNumber:
                    return GetString ("Get Last Document Number");
                case DeviceCommands.GetConstants:
                    return GetString ("Get Constants");
                case DeviceCommands.GetDateTime:
                    return GetString ("Get Date/Time");
                case DeviceCommands.GetMeasuringUnits:
                    return GetString ("Get Measuring Units");
                case DeviceCommands.GetSerialNumbers:
                    return GetString ("Get Serial Numbers");

                #endregion

                #region Report commands

                case DeviceCommands.DailyXReport:
                    return GetString ("Daily X Report");
                case DeviceCommands.DailyZReport:
                    return GetString ("Daily Z Report");
                case DeviceCommands.DailyEJReport:
                    return GetString ("Daily Electronic Journal Report");
                case DeviceCommands.DetailFMReportByNumbers:
                    return GetString ("Detail Fiscal Memory Report By Numbers");
                case DeviceCommands.ShortFMReportByNumbers:
                    return GetString ("Short Fiscal Memory Report By Numbers");
                case DeviceCommands.DetailFMReportByDates:
                    return GetString ("Detail Fiscal Memory Report By Dates");
                case DeviceCommands.ShortFMReportByDates:
                    return GetString ("Short Fiscal Memory Report By Dates");
                case DeviceCommands.OperatorsReport:
                    return GetString ("Operators Report");
                case DeviceCommands.RAMResetsReport:
                    return GetString ("RAM Resets Report");
                case DeviceCommands.VATRateChangesReport:
                    return GetString ("VAT Rate Changes Report");

                #endregion

                #region Display commands

                case DeviceCommands.DisplayClear:
                    return GetString ("Clear Display");
                case DeviceCommands.DisplayShowDateTime:
                    return GetString ("Display Date/Time");
                case DeviceCommands.DisplayLowerText:
                    return GetString ("Display Lower Text");
                case DeviceCommands.DisplayUpperText:
                    return GetString ("Display Upper Text");

                #endregion

                #region Kitchen Printer commands

                case DeviceCommands.PrintFreeText:
                    return GetString ("Print Free Text");

                #endregion

                #region Electronic Scale commands

                case DeviceCommands.GetWeight:
                    return GetString ("Get Weight");
                case DeviceCommands.GetPrice:
                    return GetString ("Get Price");
                case DeviceCommands.SetPrice:
                    return GetString ("Set Price");
                case DeviceCommands.GetAmount:
                    return GetString ("Get Amount");
                case DeviceCommands.GetWeightAndSetPrice:
                    return GetString ("Get Weight And Set Price");

                #endregion

                #region Meta commands

                case DeviceCommands.MetaFiscalSimpleTest:
                    return GetString ("Meta Fiscal Simple Test");
                case DeviceCommands.MetaFiscalVatGroupsTest:
                    return GetString ("Meta Fiscal Vat Groups Test");
                case DeviceCommands.MetaFiscalPaymentsTest:
                    return GetString ("Meta Fiscal Payments Test");
                case DeviceCommands.MetaFiscalCyrilicTest:
                    return GetString ("Meta Fiscal Cyrilic Test");
                case DeviceCommands.MetaFiscalItemLenghtTest:
                    return GetString ("Meta Fiscal Item Lenght Test");
                case DeviceCommands.MetaFiscalItemLoadTest:
                    return GetString ("Meta Fiscal Item Load Test");
                case DeviceCommands.MetaNonFiscalTextLengthTest:
                    return GetString ("Meta Non Fiscal Text Length Test");
                case DeviceCommands.MetaRegisterMoneyTest:
                    return GetString ("Meta Register Money Test");
                case DeviceCommands.MetaFiscalReportsTest:
                    return GetString ("Meta Fiscal Reports Test");

                #endregion

                #region Other commands

                case DeviceCommands.RegisterCash:
                    return GetString ("Register Cash");
                case DeviceCommands.OpenCashDrawer:
                    return GetString ("Open Cash Drawer");
                case DeviceCommands.GetStatus:
                    return GetString ("Get Status");
                case DeviceCommands.Ping:
                    return GetString ("Ping");
                case DeviceCommands.SignSale:
                    return GetString ("Sign Sale");

                #endregion

                default:
                    throw new ArgumentOutOfRangeException ("cmd", cmd, "Unknown command.");
            }
        }

        public static string GetDevicesGroup (DeviceType type)
        {
            switch (type) {
                case DeviceType.None:
                    return GetString ("All");

                case DeviceType.CashReceiptPrinter:
                    return GetString ("Cash receipt printers");

                case DeviceType.KitchenPrinter:
                    return GetString ("Kitchen printers");

                case DeviceType.ExternalDisplay:
                    return GetString ("External displays");

                case DeviceType.BarcodeScanner:
                    return GetString ("Barcode scanners");

                case DeviceType.ElectronicScale:
                    return GetString ("Electronic scales");

                case DeviceType.CardReader:
                    return GetString ("Card readers");

                case DeviceType.BarcodePrinter:
                    return GetString ("Barcode printers");

                case DeviceType.ProgrammableKeyboard:
                    return GetString ("Programmable keyboards");

                case DeviceType.SalesDataController:
                    return GetString ("Sales Data Controllers");

                case DeviceType.CashDrawer:
                    return GetString ("Cash drawers");

                default:
                    throw new ArgumentOutOfRangeException ("type");
            }
        }

        public static string GetOperationTypeName (OperationType operationType)
        {
            switch (operationType) {
                case OperationType.Purchase:
                    return GetString ("Purchase");
                case OperationType.Sale:
                    return GetString ("Sale");
                case OperationType.Waste:
                    return GetString ("Waste");
                case OperationType.StockTaking:
                    return GetString ("Stock-taking");
                case OperationType.ProduceIn:
                    return GetString ("Production");
                case OperationType.ProduceOut:
                    return GetString ("Production");
                case OperationType.TransferIn:
                    return GetString ("Transfer");
                case OperationType.TransferOut:
                    return GetString ("Transfer");
                case OperationType.PointOfSale:
                    return GetString ("Point of Sale");
                case OperationType.TradeMonitor:
                    return GetString ("Touch Screen");
                case OperationType.WriteOff:
                    return GetString ("Write-Off");
                case OperationType.PurchaseOrder:
                    return GetString ("Purchase Order");
                case OperationType.Offer:
                    return GetString ("Offer");
                case OperationType.ProformaInvoice:
                    return GetString ("Proforma-Invoice");
                case OperationType.Consignment:
                    return GetString ("Consignment");
                case OperationType.ConsignmentSale:
                    return GetString ("Consignment Sale");
                case OperationType.ConsignmentReturn:
                    return GetString ("Consignment Return");
                case OperationType.TakeConsignation:
                    return GetString ("Purchases on Consignment");
                case OperationType.SalesOrder:
                    return GetString ("Sales Order");
                case OperationType.RecipeMaterial:
                    return GetString ("Recipe Material");
                case OperationType.ComplexRecipeMaterial:
                    return GetString ("Recipe Material");
                case OperationType.RecipeProduct:
                    return GetString ("Recipe Product");
                case OperationType.ComplexRecipeProduct:
                    return GetString ("Recipe Product");
                case OperationType.ComplexProductionMaterial:
                    return GetString ("Production Material");
                case OperationType.ComplexProductionProduct:
                    return GetString ("Production Product");
                case OperationType.DebitNote:
                    return GetString ("Debit Note");
                case OperationType.CreditNote:
                    return GetString ("Credit Note");
                case OperationType.WarrantyCard:
                    return GetString ("Warranty Card");
                case OperationType.PackingMaterial:
                    return GetString ("Packing Raw Material");
                case OperationType.PackingProduct:
                    return GetString ("Packing Product");
                case OperationType.GivePacking:
                    return GetString ("Packing Give");
                case OperationType.ReturnPacking:
                    return GetString ("Packing Return");
                case OperationType.RestaurantOrder:
                    return GetString ("Restaurant Order");
                case OperationType.Return:
                    return GetString ("Return");
                case OperationType.Reservation:
                    return GetString ("Reservation");
                case OperationType.AdvancePayment:
                    return GetString ("Advance Payment");
                case OperationType.DebitNoteBySupplier:
                    return GetString ("Debit Note by Supplier");
                case OperationType.CreditNoteBySupplier:
                    return GetString ("Credit Note by Supplier");
                case OperationType.PurchaseReturn:
                    return GetString ("Purchase Return");
                case OperationType.Invoice:
                    return GetString ("Invoice");
                case OperationType.Barcodes:
                    return GetString ("Barcodes");
                default:
                    return string.Empty;
            }
        }

        public static string GetOperationTypeGlobalName (OperationType operationType)
        {
            switch (operationType) {
                case OperationType.TransferIn:
                case OperationType.TransferOut:
                    return GetString ("Transfer");
                case OperationType.RecipeMaterial:
                case OperationType.RecipeProduct:
                    return GetString ("Simple Recipe");
                case OperationType.ComplexRecipeMaterial:
                case OperationType.ComplexRecipeProduct:
                    return GetString ("Recipe");
                case OperationType.ProduceIn:
                case OperationType.ProduceOut:
                    return GetString ("Simple Production");
                case OperationType.ComplexProductionMaterial:
                case OperationType.ComplexProductionProduct:
                    return GetString ("Production");
                default:
                    return GetOperationTypeName (operationType);
            }
        }

        public static KeyValuePair<ItemsManagementType, string> [] GetAllItemManagementTypes ()
        {
            return new []
                {
                    new KeyValuePair<ItemsManagementType, string> (ItemsManagementType.AveragePrice, GetString ("Average price")),
                    new KeyValuePair<ItemsManagementType, string> (ItemsManagementType.QuickAveragePrice, GetString ("Quick Average price")),
                    new KeyValuePair<ItemsManagementType, string> (ItemsManagementType.LastPurchasePrice, GetString ("Last purchase price")),
                    new KeyValuePair<ItemsManagementType, string> (ItemsManagementType.FIFO, GetString ("FIFO")),
                    new KeyValuePair<ItemsManagementType, string> (ItemsManagementType.LIFO, GetString ("LIFO")),
                    new KeyValuePair<ItemsManagementType, string> (ItemsManagementType.Choice, GetString ("Choice")),
                    new KeyValuePair<ItemsManagementType, string> (ItemsManagementType.FEFO, GetString ("FEFO"))
                };
        }

        public static string GetItemManagementTypeName (ItemsManagementType type)
        {
            switch (type) {
                case ItemsManagementType.AveragePrice:
                    return GetString ("Average price");
                case ItemsManagementType.QuickAveragePrice:
                    return GetString ("Quick Average price");
                case ItemsManagementType.LastPurchasePrice:
                    return GetString ("Last purchase price");
                case ItemsManagementType.FIFO:
                    return GetString ("FIFO");
                case ItemsManagementType.LIFO:
                    return GetString ("LIFO");
                case ItemsManagementType.Choice:
                    return GetString ("Choice");
                case ItemsManagementType.FEFO:
                    return GetString ("FEFO");
                default:
                    throw new ArgumentOutOfRangeException ("type");
            }
        }

        public static string GetDayOfWeek (DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek) {
                case DayOfWeek.Sunday:
                    return GetString ("Sunday");
                case DayOfWeek.Monday:
                    return GetString ("Monday");
                case DayOfWeek.Tuesday:
                    return GetString ("Tuesday");
                case DayOfWeek.Wednesday:
                    return GetString ("Wednesday");
                case DayOfWeek.Thursday:
                    return GetString ("Thursday");
                case DayOfWeek.Friday:
                    return GetString ("Friday");
                case DayOfWeek.Saturday:
                    return GetString ("Saturday");
                default:
                    throw new ArgumentOutOfRangeException ("dayOfWeek");
            }
        }

        #region Date Time extensions

        public static string GetTimeLeftString (DateTime start, int counter, int total)
        {
            double millisecondsPerDetail = (DateTime.Now - start).TotalMilliseconds / counter;
            double estimatedMillisecondsLeft = (total - counter) * millisecondsPerDetail;
            DateTime estimatedEnd = DateTime.Now.AddMilliseconds (estimatedMillisecondsLeft);

            return estimatedEnd.ToFriendlyTimeLeftString ();
        }

        public static string ToFriendlyTimeLeftString (this DateTime time, DateTime? now = null)
        {
            now = now == null ?
                (time.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now) :
                now.Value.ToUniversalTime ();

            if (now > time)
                return GetString ("Expired");

            TimeSpan diff = time - now.Value;
            if (diff.TotalDays > 2)
                return string.Format (GetString ("{0} days left"), ((int) diff.TotalDays).ToString (CultureInfo.InvariantCulture));

            if (diff.TotalDays > 1)
                return string.Format (GetString ("1 day, {0} hours left"), ((int) diff.TotalHours - 24).ToString (CultureInfo.InvariantCulture));

            if (diff.TotalHours > 2)
                return string.Format (GetString ("{0} hours left"), ((int) diff.TotalHours).ToString (CultureInfo.InvariantCulture));

            if (diff.TotalHours > 1)
                return string.Format (GetString ("1 hour, {0} minutes left"), ((int) diff.TotalMinutes - 60).ToString (CultureInfo.InvariantCulture));

            if (diff.TotalMinutes > 2)
                return string.Format (GetString ("{0} minutes left"), ((int) diff.TotalMinutes).ToString (CultureInfo.InvariantCulture));

            if (diff.TotalMinutes > 1)
                return string.Format (GetString ("1 minute, {0} seconds left"), ((int) diff.TotalSeconds - 60).ToString (CultureInfo.InvariantCulture));

            if (diff.TotalSeconds > 5)
                return string.Format (GetString ("{0} seconds left"), ((int) diff.TotalSeconds).ToString (CultureInfo.InvariantCulture));

            return GetString ("few seconds left");
        }

        public static string ToFriendlyTimeAgoString (this DateTime time, DateTime? now = null)
        {
            TimeSpan span = (now ?? DateTime.Now) - time;
            if (span.TotalMinutes < 1)
                return string.Format (GetString ("{0} seconds ago"), (int) span.TotalSeconds);

            if (span.TotalHours < 1) {
                int totalMinutes = (int) span.TotalMinutes;
                return totalMinutes == 1 ?
                    GetString ("About a minute ago") :
                    string.Format (GetString ("{0} minutes ago"), totalMinutes);
            }

            if (span.TotalDays < 1) {
                int totalHours = (int) span.TotalHours;
                return totalHours == 1 ?
                    GetString ("About an hour ago") :
                    string.Format (GetString ("{0} hours ago"), totalHours);
            }

            if ((int) Math.Floor (span.TotalDays) == 1)
                return GetString ("Yesterday");

            if (span.TotalDays < 28)
                return string.Format (GetString ("{0} days ago"), (int) span.TotalDays);

            if (Math.Abs (span.TotalDays - 30) < 2)
                return GetString ("About a month ago");

            return BusinessDomain.GetFormattedDateTime (time);

        }

        #endregion
    }
}
