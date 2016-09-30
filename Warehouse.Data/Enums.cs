//
// Enums.cs
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

namespace Warehouse.Data
{
    public enum OperationType
    {
        Any = -1,
        Purchase = 1,
        Sale = 2,
        Waste = 3,
        StockTaking = 4,
        ProduceIn = 5,
        ProduceOut = 6,
        TransferIn = 7,
        TransferOut = 8,
        PointOfSale = 9,
        TradeMonitor = 10,
        WriteOff = 11,
        PurchaseOrder = 12,
        Offer = 13,
        ProformaInvoice = 14,
        Consignment = 15,
        ConsignmentSale = 16,
        ConsignmentReturn = 17,
        TakeConsignation = 18,
        SalesOrder = 19,
        RecipeMaterial = 20,
        RecipeProduct = 21,
        ComplexRecipeMaterial = 22,
        ComplexRecipeProduct = 23,
        ComplexProductionMaterial = 24,
        ComplexProductionProduct = 25,
        DebitNote = 26,
        CreditNote = 27,
        WarrantyCard = 28,
        PackingMaterial = 29,
        PackingProduct = 30,
        GivePacking = 31,
        ReturnPacking = 32,
        RestaurantOrder = 33,
        Return = 34,
        Reservation = 35,
        AdvancePayment = 36,
        DebitNoteBySupplier = 37,
        CreditNoteBySupplier = 38,
        PurchaseReturn = 39,
        MoneyIn = 71,
        MoneyOut = 72,
        MoneyInBank = 73,
        MoneyOutBank = 74,
        Invoice = 101,
        Temp = 200,
        ComplexInvoicePurchase = 1007,
        ComplexInvoiceSale = 1008,
        Barcodes = 2000
    }

    public enum OperationState
    {
        Saved = 1,
        New = -1,
        NewPending = -2,
        Pending = -3,
        NewDraft = -10,
        Draft = -11,
    }

    public enum OperationDetailsType
    {
        Primary,
        Secondary,
        All,
        None
    }

    public enum DocumentType
    {
        Invoice = 0,
        DebitNote = 1,
        CreditNote = 2
    }
    
    public enum BasePaymentType
    {
        Any = -1,
        Cash = 1,
        BankTransfer = 2,
        Card = 3,
        Coupon = 4,
        Advance = 5
    }

    public enum TurnoverDirection
    {
        Expense = -1,
        Income = 1,
    }

    public enum TurnoverType
    {
        ExpensePurchase = 1,
        ExpenseConsumable = 2,
        ExpenseSalary = 3,
        ExpenseRent = 4,
        ExpenseOther = 5,
        NotDefined = 6,
        IncomeOther = 7,
        IncomeSale = 8,
        ExpenseFuel = 9,
        IncomeAdvance = 10
    }

    public enum ItemsManagementType
    {
        AveragePrice = 0,
        LastPurchasePrice = 1,
        FIFO = 2,
        LIFO = 3,
        Choice = 4,
        FEFO = 5,
        QuickAveragePrice = 10,
    }

    public enum DeletePermission
    {
        Yes,
        InUse,
        Reserved,
        No
    }

    public enum DbTable
    {
        Unknown,
        ApplicationLog,
        Cashbook,
        Configuration,
        Currencies,
        CurrenciesHistory,
        Documents,
        EcrReceipts,
        Items,
        ItemsGroups,
        InternalLog,
        Lots,
        Network,
        NextAcct,
        Objects,
        ObjectsGroups,
        Operations,
        OperationDetails,
        OperationType,
        Partners,
        PartnersGroups,
        Payments,
        PaymentTypes,
        PriceRules,
        Registration,
        Store,
        System,
        Transformations,
        Users,
        UsersGroup,
        UsersSecurity,
        VatGroups,
        OperationUsers,
        OperationUsersGroup,
        OperationOperators,
        OperationOperatorsGroup,
    }

    public enum DataField
    {
        #region ApplicationLog table fields

        AppLogId,
        AppLogMessage,
        AppLogUserId,
        AppLogTimeStamp,
        AppLogMessageSource,

        #endregion

        #region Cashbook table fields

        CashEntryId,
        CashEntryDate,
        CashEntryDescription,
        CashEntryTurnoverType,
        CashEntryDirection,
        CashEntryAmount,
        CashEntryOperatorId,
        CashEntryTimeStamp,
        CashEntryLocationId,

        #endregion

        #region Configuration table fields

        ConfigEntryId,
        ConfigEntryKey,
        ConfigEntryValue,
        ConfigEntryUserId,

        #endregion

        #region Currencies table fields

        CurrencyID,
        CurrencyName,
        CurrencyDescription,
        CurrencyExchangeRate,
        CurrencyDeleted,

        #endregion

        #region ECRReceipts table fields

        ECRReceiptID,
        ECRReceiptOperationType,
        ECRReceiptDate,

        #endregion

        #region Documents table fields

        DocumentId,
        DocumentOperationNumber,
        DocumentNumber,
        DocumentOperationType,
        DocumentDate,
        DocumentType,
        DocumentReferenceDate,
        DocumentReferenceNumber,
        DocumentPaymentTypeId,
        DocumentRecipient,
        DocumentRecipientEGN,
        DocumentProvider,
        DocumentTaxDate,
        DocumentReason,
        DocumentDescription,
        DocumentLocation,

        #endregion

        #region Items table fields

        ItemId,
        ItemCode,
        ItemBarcode1,
        ItemBarcode2,
        ItemBarcode3,
        ItemCatalog1,
        ItemCatalog2,
        ItemCatalog3,
        ItemName,
        ItemName2,
        ItemMeasUnit,
        ItemMeasUnit2,
        ItemMeasRatio,
        ItemPurchasePrice,
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
        ItemGroupId,
        ItemIsRecipe,
        ItemTaxGroupId,
        ItemOrder,
        ItemDeleted,

        ItemTradeInVAT,
        ItemTradeInSum,
        ItemTradeVAT,
        ItemTradeSum,
        //ItemTradeOutPrice,

        #endregion

        #region ItemsGroups table fields

        ItemsGroupId,
        ItemsGroupName,
        ItemsGroupCode,

        #endregion

        #region ApplicationLog table fields

        InternalLogId,
        InternalLogMessage,

        #endregion

        #region Lots table fields

        LotId,
        LotSerialNumber,
        LotExpirationDate,
        LotProductionDate,
        LotLocation,

        #endregion

        #region Objects table fields

        LocationId,
        LocationCode,
        LocationName,
        LocationName2,
        LocationOrder,
        LocationDeleted,
        LocationGroupId,
        LocationPriceGroup,
        SourceLocationName,
        TargetLocationName,

        #endregion

        #region ObjectsGroups table fields

        LocationsGroupsId,
        LocationsGroupsName,
        LocationsGroupsCode,

        #endregion

        #region Operations table fields

        OperationType,
        OperationNumber,
        OperationLocationId,
        OperationLocation,
        OperationLocation2,
        OperationPartnerId,
        OperationPartner,
        OperationPartner2,
        OperationOperatorId,
        OperationReferenceId,
        OperationUserId,
        OperationTimeStamp,
        OperationDetailReferenceId,
        OperationDate,
        OperationDateTime,
        OperationQuantitySum,
        OperationQuantitySignedSum,
        OperationVatSum,
        OperationSum,
        OperationTotal,
        OperationProfit,

        OperationDetailId,
        OperationDetailItemId,
        OperationDetailAvailableQuantity,
        OperationDetailQuantity,
        OperationDetailDifference,
        OperationDetailPriceIn,
        OperationDetailPriceOut,
        OperationDetailStorePriceIn,
        OperationDetailStorePriceOut,
        OperationDetailSumIn,
        OperationDetailSumOut,
        OperationDetailSum,
        OperationDetailVatIn,
        OperationDetailVatOut,
        OperationDetailVat,
        OperationDetailSumVatIn,
        OperationDetailSumVatOut,
        OperationDetailSumVat,
        OperationDetailTotalIn,
        OperationDetailTotalOut,
        OperationDetailTotal,
        OperationDetailDiscount,
        OperationDetailCurrencyId,
        OperationDetailCurrencyRate,
        OperationDetailLot,
        OperationDetailWarrantySerialNumber,
        OperationDetailLotId,
        OperationDetailNote,
        OperationDetailWarrantyPeriod,
        OperationDetailSign,
        ConsignmentDetailRemainingQuantity,
        RemainingQuantity,

        #region Purchase

        PurchaseQuantitySum,
        PurchaseSum,
        PurchaseVATSum,
        PurchaseTotal,

        #endregion

        #region Sale

        SaleQuantitySum,
        SaleSum,
        SaleVATSum,
        SaleTotal,

        #endregion

        #region Waste

        WasteQuantitySum,

        #endregion

        #region Stock-taking

        StockTakingDetailPrice,
        StockTakingQuantitySum,
        StockTakingTotal,
        StockTakingSum,

        #endregion

        #region Transfer

        TransferInQuantitySum,
        TransferOutQuantitySum,

        #endregion

        #region Write-off

        WriteOffQuantitySum,

        #endregion
        
        ConsignedQuantitySum,
        ConsignedQuantitySoldSum,
        ConsignedQuantityReturnedSum,

        #region Production

        ProductionMatQuantitySum,
        ProductionProdQuantitySum,

        #endregion

        DebitNoteQuantitySum,
        CreditNoteQuantitySum,
        ReturnQuantitySum,
        ReturnSum,
        PurchaseReturnQuantitySum,

        #endregion

        #region OperationsOperators table fields

        OperationsOperatorId,
        OperationsOperatorCode,
        OperationsOperatorName,
        OperationsOperatorName2,
        OperationsOperatorOrder,
        OperationsOperatorGroupId,
        OperationsOperatorPassword,
        OperationsOperatorLevel,
        OperationsOperatorCardNo,

        #endregion

        #region OperationsOperatorsGroups table fields

        OperationsOperatorsGroupsId,
        OperationsOperatorsGroupsName,
        OperationsOperatorsGroupsCode,

        #endregion

        #region OperationsUsers table fields

        OperationsUserId,
        OperationsUserCode,
        OperationsUserName,
        OperationsUserName2,
        OperationsUserOrder,
        OperationsUserGroupId,
        OperationsUserPassword,
        OperationsUserLevel,
        OperationsUserCardNo,

        #endregion

        #region OperationsUsersGroups table fields

        OperationsUsersGroupsId,
        OperationsUsersGroupsName,
        OperationsUsersGroupsCode,

        #endregion

        #region Payments table fields

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

        #region Payment types table fields

        PaymentTypesId,
        PaymentTypesName,
        PaymentTypesMethod,

        #endregion

        #region Partners table fields

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
        PartnerCreatorId,
        PartnerGroupId,
        PartnerTimeStamp,
        PartnerCardNumber,
        PartnerNote,
        PartnerNote2,
        PartnerPaymentDays,
        PartnerDebt,

        #endregion

        #region PartnersGroups table fields

        PartnersGroupsId,
        PartnersGroupsName,
        PartnersGroupsCode,

        #endregion

        #region Price rules table fields

        PriceRuleId,
        PriceRuleName,
        PriceRuleFormula,
        PriceRuleEnabled,
        PriceRulePriority,

        #endregion

        #region Registration table fields

        CompanyId,
        CompanyCode,
        CompanyName,
        CompanyLiablePerson,
        CompanyCity,
        CompanyAddress,
        CompanyPhone,
        CompanyFax,
        CompanyEmail,
        CompanyTaxNumber,
        CompanyBulstat,
        CompanyBankName,
        CompanyBankCode,
        CompanyBankAcct,
        CompanyBankVATAcct,
        CompanyCreatorId,
        CompanyCreationTimeStamp,
        CompanyDefault,
        CompanyNote1,
        CompanyNote2,

        #endregion

        #region Store table fields

        StorePrice,
        StoreQtty,
        StoreAvailableQuantity,
        StoreCountedQuantity,
        StoreLot,

        #endregion

        #region Users table fields

        UserId,
        UserCode,
        UserName,
        UserName2,
        UserOrder,
        UserDeleted,
        UserGroupId,
        UserPassword,
        UserLevel,
        UserCardNo,

        #endregion

        #region UsersGroups table fields

        UsersGroupsId,
        UsersGroupsName,
        UsersGroupsCode,

        #endregion

        #region UsersSecurity table fields

        UsersSecurityId,
        UsersSecurityUserId,
        UsersSecurityControlName,
        UsersSecurityState,

        #endregion

        #region VATGroups table fields

        VATGroupId,
        VATGroupCode,
        VATGroupName,
        VATGroupValue,

        #endregion

        #region Other fields

        PriceGroup,
        ReportDate,
        NotSet,

        #endregion

        #region Legacy fields

        CashEntryPointOfSaleId,
        DocumentPointOfSale,

        #region Goods table fields

        GoodsId,
        GoodsCode,
        GoodsBarCode1,
        GoodsBarCode2,
        GoodsBarCode3,
        GoodsCatalog1,
        GoodsCatalog2,
        GoodsCatalog3,
        GoodsName,
        GoodsName2,
        GoodsMeasUnit,
        GoodsMeasUnit2,
        GoodsMeasRatio,
        GoodsTradeInPrice,
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
        GoodsGroupId,
        GoodsIsRecipe,
        GoodsTaxGroupId,
        GoodsOrder,
        GoodsDeleted,

        GoodsTradeInVAT,
        GoodsTradeInSum,
        GoodsTradeVAT,
        GoodsTradeSum,

        #endregion

        #region GoodsGroups table fields

        GoodsGroupsId,
        GoodsGroupsName,
        GoodsGroupsCode,

        #endregion

        #region Objects table fields

        PointOfSaleId,
        PointOfSaleCode,
        PointOfSaleName,
        PointOfSaleName2,
        PointOfSaleOrder,
        PointOfSaleDeleted,
        PointOfSaleGroupId,
        PointOfSalePriceGroup,
        SourcePointOfSaleName,
        TargetPointOfSaleName,

        #endregion

        #region ObjectsGroups table fields

        PointsOfSaleGroupsId,
        PointsOfSaleGroupsName,
        PointsOfSaleGroupsCode,

        #endregion

        OperationPointOfSaleId,
        OperationPointOfSale,
        OperationPointOfSale2,
        OperationDetailGoodsId,
        OperationDetailVatInSum,
        OperationDetailVatOutSum,
        OperationDetailVatSum,

        #endregion
    }

    public enum DataFilterLogic
    {
        Contains,
        In,
        StartsWith,
        EndsWith,
        ExactMatch,
        Greather,
        GreatherOrEqual,
        Less,
        LessOrEqual,
        InRange,
        InEntityGroup,
        MoreThanMinutesAgo,
        LessThanMinutesAgo,
        NotSet
    }

    public enum CreateDatabaseType
    {
        Blank,
        SampleRestaurant,
        SampleStore,
        Other,
    }

    public enum AggregationType
    {
        None,
        GroupBy,
        Min,
        Max,
        Average,
        Sum,
        Count,
    }

    public enum DataType
    {
        Date,
        DateTime,
        DateTimeInterval,
        Quantity,
        CurrencyIn,
        CurrencyOut,
        Currency,
        DocumentNumber,
        Text,
        Percent,
        OperationType,
        DocumentType,
        PriceGroupType,
        BasePaymentType,
        PartnerType,
        ItemType,
        UserAccessLevel,
        TaxGroupCode,
        Id,
        UserId,
        PaymentType,
        TurnoverType,
        TurnoverDirection,
        Sign,
        PaymentMode
    }

    public enum ConditionCombineLogic
    {
        And,
        Or,
        AndNot,
        OrNot,
    }

    public enum SortDirection
    {
        None,
        Ascending,
        Descending
    }

    public enum GeneratedBarcodeType
    {
        EAN8,
        UPCA,
        EAN13
    }

    [Flags]
    public enum DeviceType
    {
        None = 0,
        CashReceiptPrinter = 1,
        KitchenPrinter = 2,
        ExternalDisplay = 4,
        BarcodeScanner = 8,
        ElectronicScale = 16,
        CardReader = 32,
        BarcodePrinter = 64,
        ProgrammableKeyboard = 128,
        SalesDataController = 256,
        CashDrawer = 512,
    }

    public enum DeviceCommands
    {
        #region Initialization

        SetDateTime,
        SyncDate,
        SyncTime,
        SetVATRates,
        SetFooterText,
        SetPrintingOptions,
        SetGraphicalLogo,
        SetAutoPaperCut,
        SetZeroRegisterPrint,
        SetJournalPrintSpeed,
        SetReceiptEndFeed,
        SetBufferedPrint,
        SetPrintContrast,
        SetBarcodeHeight,
        GetHeaderText,
        GetFooterText,
        GetPrintingOptions,
        GetGraphicalLogo,
        GetAutoPaperCut,
        GetZeroRegisterPrint,
        GetJournalPrintSpeed,
        GetReceiptEndFeed,
        GetBufferedPrint,
        GetPrintContrast,
        GetBarcodeHeight,
        SetGraphicalLogoBitmap,
        SetTextField,
        GetTextField,
        SetPaymentName,
        GetPaymentName,
        SetReceiptCode,
        SetOperator,
        SetPartner,
        SetLocation,

        #endregion

        #region Non fiscal commands

        OpenNonFiscal,
        PrintTitleNonFiscal,
        PrintTextNonFiscal,
        PrintKeyValueNonFiscal,
        AddItemNonFiscal,
        AddModifiedItemNonFiscal,
        CloseNonFiscal,

        #endregion

        #region Fiscal commands

        InitFiscal,
        OpenFiscal,
        AddItem,
        Subtotal,
        Payment,
        CloseFiscal,
        CancelFiscalOperation,
        PrintTextFiscal,
        PrintDuplicate,

        #endregion

        #region Printer commands

        PaperFeed,
        PaperCut,
        PrintBarcode,
        PrintSignature,
        SetPrinterFonts,

        #endregion

        #region Information commands

        GetFiscalReceiptStatus,
        GetDiagnosticInfo,
        GetVATRatesInfo,
        GetTaxNumber,
        GetLastDocumentNumber,
        GetConstants,
        GetDateTime,
        GetMeasuringUnits,
        GetSerialNumbers,

        #endregion

        #region Report commands

        DailyXReport,
        DailyZReport,
        DailyEJReport,
        DetailFMReportByNumbers,
        ShortFMReportByNumbers,
        DetailFMReportByDates,
        ShortFMReportByDates,
        OperatorsReport,
        RAMResetsReport,
        VATRateChangesReport,

        #endregion

        #region Display commands

        DisplayClear,
        DisplayShowDateTime,
        DisplayLowerText,
        DisplayUpperText,

        #endregion

        #region Kitchen Printer commands

        PrintFreeText,

        #endregion

        #region Electronic Scale commands

        GetWeight,
        GetPrice,
        SetPrice,
        GetAmount,
        GetWeightAndSetPrice,

        #endregion

        #region Meta commands

        MetaFiscalSimpleTest,
        MetaFiscalVatGroupsTest,
        MetaFiscalPaymentsTest,
        MetaFiscalCyrilicTest,
        MetaFiscalItemLenghtTest,
        MetaFiscalItemLoadTest,
        MetaNonFiscalTextLengthTest,
        MetaRegisterMoneyTest,
        MetaFiscalReportsTest,

        #endregion

        #region Other commands

        RegisterCash,
        OpenCashDrawer,
        GetStatus,
        Ping,
        SignSale,
        PrintFormObject,
        None,

        #endregion
    }

    public enum FiscalPrinterTaxGroup
    {
        A,
        B,
        C,
        D,
        E,
        F,
        G,
        H,
        I,
        J,
        K,
        L,
        M,
        N,
        O,
        P,
        Q,
        R,
        S,
        TaxExempt,
        Default,
        NotSet
    }

    [Flags]
    public enum ItemType
    {
        Standard = 0,
        FixedPrice = 1,
        VariablePrice = 2,
        ManualProductionOnly = 4,
        NonInventory = 8,
        DistributedCharge = 16,
        Discount = 32,
        PriceFlags = FixedPrice | VariablePrice
    }
}
