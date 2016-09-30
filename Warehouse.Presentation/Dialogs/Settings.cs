//
// Settings.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   09/21/2006
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Glade;
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Business.BarcodeGenerators;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Data;
using Thread = System.Threading.Thread;

namespace Warehouse.Presentation.Dialogs
{
    public class Settings : DialogBase
    {
        public enum Page
        {
            Visual,
            Printing,
            Operations,
            Codes,
            Email,
            Special
        }

        private readonly List<string> printers = new List<string> ();
        private readonly Dictionary<string, string> localizations = new Dictionary<string, string> ();
        private readonly List<SettingsPage> addins = new List<SettingsPage> ();
        private KeyValuePair<ItemsManagementType, string> [] itemManagmentTypes;
        private bool correctDate;
        private bool oldVATIncluded;
        private bool oldAllowNegativeAvailability;
        private bool oldDocumentNumbersPerLocation;
        private ItemsManagementType oldItemManagementType;
        private ItemsManagementType lastItemManagementType;
        private string currentLocale;
        private string defaultPrinter = string.Empty;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgSettings;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

        [Widget]
        private Notebook nbMain;

        #region Visual tab

        [Widget]
        private Label lblVisual;

        [Widget]
        private Label lblApplicationLocalization;
        [Widget]
        private CheckButton chkSystemLocalization;
        [Widget]
        private Label lblLocalization;
        [Widget]
        private ComboBox cboLocalization;
        [Widget]
        private Label lblDocumentTemplates;
        [Widget]
        private ComboBox cboDocumentTemplates;

        [Widget]
        private Label lblCurrencySign;
        [Widget]
        private Alignment algCurrencySign;
        private ComboBoxEntry cbeCurrencySign;
        [Widget]
        private Label lblCurrencyPrecision;
        [Widget]
        private Label lblCurrencyPreview;
        [Widget]
        private ComboBox cboCurrencyPrecision;
        [Widget]
        private Label lblCurrencyPreviewValue;
        [Widget]
        private CheckButton chkPurchaseCurrencyPrecision;
        [Widget]
        private Label lblPurchaseCurrencyPreview;
        [Widget]
        private ComboBox cboPurchaseCurrencyPrecision;
        [Widget]
        private Label lblPurchaseCurrencyPreviewValue;

        [Widget]
        private Label lblQuantityPrecision;
        [Widget]
        private Label lblQuantityPreview;
        [Widget]
        private ComboBox cboQuantityPrecision;
        [Widget]
        private Label lblQuantityPreviewValue;

        [Widget]
        private Label lblPercentPrecision;
        [Widget]
        private Label lblPercentPreview;
        [Widget]
        private ComboBox cboPercentPrecision;
        [Widget]
        private Label lblPercentPreviewValue;

        [Widget]
        private Label lblDocumentNumbers;
        [Widget]
        private Label lblDocumentNumbersPreview;
        [Widget]
        private ComboBox cboDocumentNumbers;
        [Widget]
        private Label lblDocumentNumbersPreviewValue;

        [Widget]
        private Label lblCustomDateFormat;
        [Widget]
        private Label lblDatePreview;
        [Widget]
        private Alignment algCustomDateFormat;
        private ComboBoxEntry cbeCustomDateFormat;
        [Widget]
        private Label lblDatePreviewValue;

        #endregion

        #region Printing tab

        [Widget]
        private Label lblPrinting;
        [Widget]
        private Label lblDocumentPrint;
        [Widget]
        private CheckButton chkDefaultDocumentPrinter;
        [Widget]
        private Label lblPrinter;
        [Widget]
        private ComboBox cboDocumentPrinter;
        [Widget]
        private Label lblAskBeforePrint;
        [Widget]
        private ComboBox cboAskBeforePrint;
        [Widget]
        private Label lblMarginTop;
        [Widget]
        private SpinButton spbMarginTop;
        [Widget]
        private Label lblMarginBottom;
        [Widget]
        private SpinButton spbMarginBottom;
        [Widget]
        private Label lblMarginLeft;
        [Widget]
        private SpinButton spbMarginLeft;
        [Widget]
        private Label lblMarginRight;
        [Widget]
        private SpinButton spbMarginRight;
        [Widget]
        private CheckButton chkAlwaysPrintTransfersUsingSalePrices;

        [Widget]
        private Label lblReceiptsPrint;
        [Widget]
        private CheckButton chkPrintCashReceiptByDefault;
        [Widget]
        private CheckButton chkPrintBankCashReceiptByDefault;
        [Widget]
        private CheckButton chkPrintSaleCode;
        [Widget]
        private CheckButton chkPrintOrderCode;
        [Widget]
        private CheckButton chkPrintLocation;
        [Widget]
        private CheckButton chkPrintPartner;
        [Widget]
        private CheckButton chkPrintOperator;
        [Widget]
        private CheckButton chkPrintSaleBarCode;
        [Widget]
        private Label lblBarCodeType;
        [Widget]
        private ComboBox cboBarCodeType;
        [Widget]
        private CheckButton chkPrintSaleBarCodeNumber;

        #endregion

        #region Operations tab

        [Widget]
        private Label lblOperations;

        [Widget]
        private Label lblItemsManagementType;
        [Widget]
        private ComboBox cboItemsManagementType;
        [Widget]
        private Label lblOperationFields;
        [Widget]
        private CheckButton chkEnableLineNumber;
        [Widget]
        private CheckButton chkEnableItemCode;
        [Widget]
        private CheckButton chkEnablePercentDiscounts;
        [Widget]
        private CheckButton chkEnableValueDiscounts;
        [Widget]
        private CheckButton chkEnableLotName;
        [Widget]
        private CheckButton chkEnableSerialNumber;
        [Widget]
        private CheckButton chkEnableExpirationDate;
        [Widget]
        private CheckButton chkEnableManufacturedDate;
        [Widget]
        private CheckButton chkEnableLocation;
        [Widget]
        private CheckButton chkEnableVatRate;

        [Widget]
        private CheckButton chkAllowNegativeAvailability;
        [Widget]
        private CheckButton chkAutoCreateInvoiceOnSale;
        [Widget]
        private CheckButton chkAutoCreateInvoiceOnPurchase;
        [Widget]
        private CheckButton chkAutoProduction;
        [Widget]
        private CheckButton chkMaximumAllowedQuantity;
        [Widget]
        private SpinButton spbMaximumAllowedQuantity;
        [Widget]
        private CheckButton chkWarningMaximumQuantity;
        [Widget]
        private SpinButton spbWarningMaximumQuantity;
        [Widget]
        private CheckButton chkShowOperationStatistics;

        #endregion

        #region Codes tab

        [Widget]
        private Label lblCodes;

        [Widget]
        private CheckButton chkAutoGeneratePartnerCodes;
        [Widget]
        private Label lblPartnerCodePattern;
        [Widget]
        private Entry txtPartnerCodePattern;
        [Widget]
        private Label lblPartnerCodePatternPreview;
        [Widget]
        private Label lblPartnerCodePatternPreviewValue;

        [Widget]
        private CheckButton chkAutoGenerateItemCodes;
        [Widget]
        private Label lblItemCodePattern;
        [Widget]
        private Entry txtItemCodePattern;
        [Widget]
        private Label lblItemCodePatternPreview;
        [Widget]
        private Label lblItemCodePatternPreviewValue;

        [Widget]
        private CheckButton chkAutoGenerateUserCodes;
        [Widget]
        private Label lblUserCodePattern;
        [Widget]
        private Entry txtUserCodePattern;
        [Widget]
        private Label lblUserCodePatternPreview;
        [Widget]
        private Label lblUserCodePatternPreviewValue;

        [Widget]
        private CheckButton chkAutoGenerateLocationCodes;
        [Widget]
        private Label lblLocationCodePattern;
        [Widget]
        private Entry txtLocationCodePattern;
        [Widget]
        private Label lblLocationCodePatternPreview;
        [Widget]
        private Label lblLocationCodePatternPreviewValue;

        [Widget]
        private Label lblBarcodeType;
        [Widget]
        private ComboBox cboGeneratedBarcodeType;
        [Widget]
        private Label lblBarcodePrefix;
        [Widget]
        private Entry txtBarcodePrefix;
        [Widget]
        private Label lblBarcodePatternPreview;
        [Widget]
        private Label lblBarcodePatternPreviewValue;

        #endregion

        #region E-mail tab

        [Widget]
        private Label lblEmail;
        [Widget]
        private Label lblSmtp;
        [Widget]
        private Label lblEmailSender;
        [Widget]
        private Entry txtEmailSender;
        [Widget]
        private Label lblEmailSubject;
        [Widget]
        private Entry txtEmailSubject;
        [Widget]
        private Label lblSmtpServer;
        [Widget]
        private Entry txtSmtpServer;
        [Widget]
        private Label lblSmtpPort;
        [Widget]
        private SpinButton spbSmtpPort;
        [Widget]
        private CheckButton chkEmailUseSsl;
        [Widget]
        private CheckButton chkUseCredentials;
        [Widget]
        private Label lblSmtpUserName;
        [Widget]
        private Entry txtSmtpUserName;
        [Widget]
        private Label lblSmtpPassword;
        [Widget]
        private Entry txtSmtpPassword;

        #endregion

        #region Special tab

        [Widget]
        private Label lblSpecial;

        [Widget]
        private Label lblStartup;
        [Widget]
        private CheckButton chkShowSplashScreen;
        [Widget]
        private CheckButton chkRegisterCashAtStartup;
        [Widget]
        private Label lblStartupPage;
        [Widget]
        private ComboBox cboStartupPage;

        [Widget]
        private Label lblPrices;
        [Widget]
        private CheckButton chkUsePricesWithVATIncluded;
        [Widget]
        private CheckButton chkUseSalesTaxInsteadOfVAT;
        [Widget]
        private CheckButton chkRoundPrices;
        [Widget]
        private CheckButton chkWarnPricesSaleLowerThanPurchase;

        [Widget]
        private CheckButton chkAlwaysChooseTradePointPartner;
        [Widget]
        private CheckButton chkShowItemNameOnExtDisplay;
        [Widget]
        private CheckButton chkAllowDatabaseChange;
        [Widget]
        private CheckButton chkAllowMultipleInstances;
        [Widget]
        private CheckButton chkEnableVerboseErrorLogging;
        [Widget]
        private CheckButton chkDocumentNumbersPerLocation;
        [Widget]
        private CheckButton chkConfirmPriceRules;
        [Widget]
        private CheckButton chkLogAllChangesInOperations;
        [Widget]
        private CheckButton chkLimitDocumentNumberLength;
        [Widget]
        private CheckButton chkShowPartnerSuggestionsWhenNotFound;
        [Widget]
        private CheckButton chkShowItemSuggestionsWhenNotFound;
        [Widget]
        private Label lblBankNotesAndCoins;
        [Widget]
        private Button btnChooseMoney;
        [Widget]
        private Entry txtBankNotesAndCoins;
        [Widget]
        private Label lblDistributedChargeMethod;
        [Widget]
        private ComboBox cboDistributedChargeMethod;

        #endregion

#pragma warning restore 649

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgSettings; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        public Settings (Page startupPage = Page.Visual)
        {
            Initialize ();
            if (startupPage != Page.Visual)
                nbMain.Page = (int) startupPage;
        }

        public Settings (Type startupPage)
        {
            Initialize ();
            foreach (SettingsPage settingsPage in addins.Where (settingsPage => settingsPage.GetType () == startupPage))
                for (int i = 0; i < nbMain.NPages; i++)
                    if (nbMain.GetNthPage (i) == settingsPage) {
                        nbMain.Page = i;
                        break;
                    }
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.Settings.glade", "dlgSettings");
            form.Autoconnect (this);

            btnChooseMoney.Image = FormHelper.LoadImage ("Icons.Banknote24.png");

            dlgSettings.Title = Translator.GetString ("Settings");
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeEntries ();
            InitializeAddins ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            #region Visual

            lblVisual.SetText (Translator.GetString ("Visual"));
            lblApplicationLocalization.SetText (Translator.GetString ("Application localization"));
            lblLocalization.SetText (Translator.GetString ("Localization"));
            lblDocumentTemplates.SetText (Translator.GetString ("Document templates"));
            lblCurrencySign.SetText (Translator.GetString ("Currency sign"));
            cbeCurrencySign = new ComboBoxEntry ();
            algCurrencySign.Add (cbeCurrencySign);
            cbeCurrencySign.Show ();
            lblCurrencyPrecision.SetText (Translator.GetString ("Prices format"));
            lblCurrencyPreview.SetText (Translator.GetString ("Prices view"));
            chkPurchaseCurrencyPrecision.Label = Translator.GetString ("Different purchase pices format");
            lblPurchaseCurrencyPreview.SetText (Translator.GetString ("Purchase prices view"));
            lblQuantityPrecision.SetText (Translator.GetString ("Quantities format"));
            lblQuantityPreview.SetText (Translator.GetString ("Quantities view"));
            lblPercentPrecision.SetText (Translator.GetString ("Percents format"));
            lblPercentPreview.SetText (Translator.GetString ("Percents view"));
            lblDocumentNumbers.SetText (Translator.GetString ("Document numbers format"));
            lblDocumentNumbersPreview.SetText (Translator.GetString ("Document numbers view"));
            lblCustomDateFormat.SetText (Translator.GetString ("Dates format"));
            lblDatePreview.SetText (Translator.GetString ("Dates view"));
            chkSystemLocalization.Label = Translator.GetString ("Use system localization");
            cbeCustomDateFormat = new ComboBoxEntry ();
            algCustomDateFormat.Add (cbeCustomDateFormat);
            cbeCustomDateFormat.Show ();

            #endregion

            #region Printing

            lblPrinting.SetText (Translator.GetString ("Printing"));
            lblDocumentPrint.SetText (Translator.GetString ("Documents"));
            lblAskBeforePrint.SetText (Translator.GetString ("Print documents:"));
            chkDefaultDocumentPrinter.Label = Translator.GetString ("Use default printer");
            lblPrinter.SetText (Translator.GetString ("Printer:"));
            lblMarginTop.SetText (Translator.GetString ("Margin top:"));
            lblMarginBottom.SetText (Translator.GetString ("Margin bottom:"));
            lblMarginLeft.SetText (Translator.GetString ("Margin left:"));
            lblMarginRight.SetText (Translator.GetString ("Margin right:"));
            chkAlwaysPrintTransfersUsingSalePrices.Label = Translator.GetString ("Always print transfers using sale prices");

            lblReceiptsPrint.SetText (Translator.GetString ("Receipts"));
            chkPrintCashReceiptByDefault.Label = Translator.GetString ("Print cash receipt by default");
            chkPrintBankCashReceiptByDefault.Label = Translator.GetString ("Print cash receipt for bank payments by default");
            chkPrintSaleCode.Label = Translator.GetString ("Print sale code on receipts");
            chkPrintOrderCode.Label = Translator.GetString ("Print order code on receipts");
            chkPrintLocation.Label = Translator.GetString ("Print location on receipts");
            chkPrintPartner.Label = Translator.GetString ("Print partner on receipts");
            chkPrintOperator.Label = Translator.GetString ("Print operator on receipts");
            chkPrintSaleBarCode.Label = Translator.GetString ("Print sale bar code on receipts (if supported)");
            lblBarCodeType.SetText (Translator.GetString ("Bar code type:"));
            chkPrintSaleBarCodeNumber.Label = Translator.GetString ("Print sale bar code number");

            #endregion

            #region Operations

            lblOperations.SetText (Translator.GetString ("Operations"));
            lblItemsManagementType.SetText (Translator.GetString ("Item management:"));
            lblOperationFields.SetText (Translator.GetString ("Operation fields"));
            chkEnableLineNumber.Label = Translator.GetString ("Enable line numbers");
            chkEnableItemCode.Label = Translator.GetString ("Enable item code");
            chkEnablePercentDiscounts.Label = Translator.GetString ("Allow percent discounts");
            chkEnableValueDiscounts.Label = Translator.GetString ("Allow value discounts");
            chkEnableLotName.Label = Translator.GetString ("Enable lot name");
            chkEnableSerialNumber.Label = Translator.GetString ("Enable serial number");
            chkEnableExpirationDate.Label = Translator.GetString ("Enable expiration date");
            chkEnableManufacturedDate.Label = Translator.GetString ("Enable manufactured date");
            chkEnableLocation.Label = Translator.GetString ("Enable lot location");
            chkEnableVatRate.Label = BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                Translator.GetString ("Enable tax rate") :
                Translator.GetString ("Enable VAT rate");

            chkAllowNegativeAvailability.Label = Translator.GetString ("Work with negative availability");
            chkAutoCreateInvoiceOnSale.Label = Translator.GetString ("Auto create invoice on sale");
            chkAutoCreateInvoiceOnPurchase.Label = Translator.GetString ("Auto create invoice on purchase");
            chkAutoProduction.Label = Translator.GetString ("Auto production");
            chkMaximumAllowedQuantity.Label = Translator.GetString ("Maximum allowed quantity");
            chkWarningMaximumQuantity.Label = Translator.GetString ("Warn if the quantity is larger than");
            chkLogAllChangesInOperations.Label = Translator.GetString ("Log all changes in operations");
            chkLimitDocumentNumberLength.Label = Translator.GetString ("Limit the length of document numbers");
            chkShowOperationStatistics.Label = Translator.GetString ("Show operation sums in footer");

            #endregion

            #region Codes

            lblCodes.SetText (Translator.GetString ("Codes"));

            chkAutoGeneratePartnerCodes.Label = Translator.GetString ("Auto generate partner codes");
            lblPartnerCodePattern.SetText (Translator.GetString ("Partner code pattern:"));
            lblPartnerCodePatternPreview.SetText (Translator.GetString ("Partner code preview:"));

            chkAutoGenerateItemCodes.Label = Translator.GetString ("Auto generate item codes");
            lblItemCodePattern.SetText (Translator.GetString ("Item code pattern:"));
            lblItemCodePatternPreview.SetText (Translator.GetString ("Item code preview:"));

            chkAutoGenerateUserCodes.Label = Translator.GetString ("Auto generate user codes");
            lblUserCodePattern.SetText (Translator.GetString ("User code pattern:"));
            lblUserCodePatternPreview.SetText (Translator.GetString ("User code preview:"));

            chkAutoGenerateLocationCodes.Label = Translator.GetString ("Auto generate location codes");
            lblLocationCodePattern.SetText (Translator.GetString ("Location code pattern:"));
            lblLocationCodePatternPreview.SetText (Translator.GetString ("Location code preview:"));

            #endregion

            #region Barcodes

            lblBarcodeType.SetText (Translator.GetString ("Barcode type:"));
            lblBarcodePrefix.SetText (Translator.GetString ("Barcode prefix:"));
            lblBarcodePatternPreview.SetText (Translator.GetString ("Barcode preview:"));

            #endregion

            #region Email

            lblEmail.SetText (Translator.GetString ("E-mail"));
            lblSmtp.SetText ("SMTP");
            lblSmtpServer.SetText (Translator.GetString ("Server:"));
            lblSmtpPort.SetText (Translator.GetString ("Port:"));
            lblEmailSender.SetText (Translator.GetString ("Sender address:"));
            lblEmailSubject.SetText (Translator.GetString ("Subject:"));
            chkEmailUseSsl.Label = Translator.GetString ("Use SSL");
            chkUseCredentials.Label = Translator.GetString ("Use username and password");
            lblSmtpUserName.SetText (Translator.GetString ("User:"));
            lblSmtpPassword.SetText (Translator.GetString ("Password:"));

            #endregion

            #region Special

            lblSpecial.SetText (Translator.GetString ("Special"));
            lblStartup.SetText (Translator.GetString ("Startup"));
            chkShowSplashScreen.Label = Translator.GetString ("Show splash screen");
            chkRegisterCashAtStartup.Label = Translator.GetString ("Register cash at startup");
            lblStartupPage.SetText (Translator.GetString ("Startup page"));

            lblPrices.SetText (Translator.GetString ("Prices"));
            chkUsePricesWithVATIncluded.Label = BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                Translator.GetString ("Use prices with included sales tax") : Translator.GetString ("Use prices with included VAT");
            chkRoundPrices.Label = Translator.GetString ("Round prices (better compatibility with fiscal devices)");
            chkWarnPricesSaleLowerThanPurchase.Label = Translator.GetString ("Warn if the sale price is lower than the purchase price");
            chkAlwaysChooseTradePointPartner.Label = Translator.GetString ("Selectable partner at Point of Sale");
            chkShowItemNameOnExtDisplay.Label = Translator.GetString ("Show item names on external display");
            chkAllowDatabaseChange.Label = Translator.GetString ("Allow changing the database");
            chkAllowMultipleInstances.Label = Translator.GetString ("Allow multiple instances of the application to run at the same time");
            chkEnableVerboseErrorLogging.Label = Translator.GetString ("Enable verbose error logging");
            chkDocumentNumbersPerLocation.Label = Translator.GetString ("Document numbers per location");
            chkConfirmPriceRules.Label = Translator.GetString ("Confirm price rules");
            chkUseSalesTaxInsteadOfVAT.Label = Translator.GetString ("Use sales tax instead of VAT");
            chkShowPartnerSuggestionsWhenNotFound.Label = Translator.GetString ("Show partner suggestions when a partner is not found");
            chkShowItemSuggestionsWhenNotFound.Label = Translator.GetString ("Show item suggestions when an item is not found");
            lblBankNotesAndCoins.SetText (Translator.GetString ("Bank notes and coins:"));
            lblDistributedChargeMethod.SetText (Translator.GetString ("Distributed charge algorithm:"));

            #endregion

            btnOK.SetChildLabelText (Translator.GetString ("Save"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        private void InitializeEntries ()
        {
            int i;
            ConfigurationHolder config = BusinessDomain.AppConfiguration;

            // Database hosted settings can be changed and if not reloaded will be overwritten
            config.Load (false);
            oldVATIncluded = config.VATIncluded;
            oldAllowNegativeAvailability = config.AllowNegativeAvailability;
            oldDocumentNumbersPerLocation = config.DocumentNumbersPerLocation;
            oldItemManagementType = config.ItemsManagementType;
            lastItemManagementType = config.ItemsManagementType;

            #region Visual settings

            chkSystemLocalization.Active = config.UseSystemLocalization;
            chkSystemLocalization.Toggled += chkSystemLocalization_Toggled;
            chkSystemLocalization_Toggled (null, null);

            currentLocale = config.UseSystemLocalization ?
                Thread.CurrentThread.CurrentUICulture.Name : config.Localization;

            foreach (CultureInfo locale in ComponentHelper.GetAvailableLocales (config.LocalePackageName, StoragePaths.LocaleFolder)) {
                localizations.Add (locale.Name, char.ToUpper (locale.NativeName [0]) + locale.NativeName.Substring (1));
            }

            cboLocalization.Load (localizations, "Key", "Value", currentLocale);

            LoadDocumentTemplates ();

            string currencyFormat = string.Empty;
            if (!string.IsNullOrWhiteSpace (config.CurrencySymbol)) {
                if (config.CurrencySymbolPosition < 0)
                    currencyFormat = config.CurrencySymbol + "###";
                else if (config.CurrencySymbolPosition > 0)
                    currencyFormat = "###" + config.CurrencySymbol;
            }

            cbeCurrencySign.Load (new [] {
                    string.Empty,
                    "$###",
                    "€ ###",
                    "### €",
                    "£###",
                    "### лв.",
                    "### р.",
                    "¥###",
                }, null, null, false);
            cbeCurrencySign.Entry.Text = currencyFormat;

            List<KeyValuePair<int, string>> currencyPrecisionValues = new List<KeyValuePair<int, string>> ();
            for (i = -3; i <= 6; i++)
                currencyPrecisionValues.Add (new KeyValuePair<int, string> (i, ((decimal) Math.Pow (10, -i)).ToString (CultureInfo.InvariantCulture)));

            cboCurrencyPrecision.Load (currencyPrecisionValues, "Key", "Value", config.CurrencyPrecision);
            chkPurchaseCurrencyPrecision.Active = config.UsePurchaseCurrencyPrecision;
            cboPurchaseCurrencyPrecision.Load (currencyPrecisionValues, "Key", "Value", config.PurchaseCurrencyPrecision);

            List<KeyValuePair<int, string>> numberPrecisionValues = new List<KeyValuePair<int, string>> ();
            for (i = 0; i <= 6; i++)
                numberPrecisionValues.Add (new KeyValuePair<int, string> (i, ((decimal) Math.Pow (10, -i)).ToString (CultureInfo.InvariantCulture)));

            cboQuantityPrecision.Load (numberPrecisionValues, "Key", "Value", config.QuantityPrecision);
            cboPercentPrecision.Load (numberPrecisionValues, "Key", "Value", config.PercentPrecision);

            List<KeyValuePair<int, string>> documentFormatValues = new List<KeyValuePair<int, string>> ();
            for (i = 1; i <= 15; i++) {
                StringBuilder ret = new StringBuilder ();
                for (int j = 1; j <= i; j++)
                    ret.Append ((j % 10).ToString (CultureInfo.InvariantCulture));

                documentFormatValues.Add (new KeyValuePair<int, string> (i, ret.ToString ()));
            }
            cboDocumentNumbers.Load (documentFormatValues, "Key", "Value", config.DocumentNumberLength);

            cbeCustomDateFormat.Load (new [] {
                    string.Empty,
                    "M/d/yyyy",
                    "M/d/yy",
                    "MM/dd/yy",
                    "MM/dd/yyyy",
                    "yy/MM/dd",
                    "yyyy-MM-dd",
                    "dd-MMM-yyyy",
                    "dd-MMMM-yyyy",
                    "dd.M.yyyy",
                    "d.M.yyyy",
                    "dd.MM.yyyy",
                    "dd.MMM.yyyy",
                    "dd.MMMM.yyyy"
                }, null, null, config.DateFormat);

            cbeCurrencySign.Changed += cbeCurrencySign_Changed;
            cboCurrencyPrecision.Changed += cboCurrencyPrecision_Changed;
            chkPurchaseCurrencyPrecision.Toggled += chkPurchaseCurrencyPrecision_Toggled;
            cboPurchaseCurrencyPrecision.Changed += cboPurchaseCurrencyPrecision_Changed;
            cboQuantityPrecision.Changed += cboQuantityPrecision_Changed;
            cboPercentPrecision.Changed += cboPercentPrecision_Changed;
            cboDocumentNumbers.Changed += cboDocumentNumbers_Changed;
            cbeCustomDateFormat.Changed += txtCustomDateFormat_Changed;

            RefreshCurrencyPreview ();
            RefreshPurchaseCurrencySensitivity ();
            RefreshPurchaseCurrencyPreview ();
            RefreshQuantityPreview ();
            RefreshPercentPreview ();
            RefreshDocumentNumbersPreview ();
            RefreshDatePreview ();

            #endregion

            #region Operations settings

            itemManagmentTypes = Translator.GetAllItemManagementTypes ();

            cboItemsManagementType.Load (itemManagmentTypes, "Key", "Value", config.ItemsManagementType);
            chkEnableLineNumber.Active = config.EnableLineNumber;
            chkEnableItemCode.Active = config.EnableItemCode;
            chkEnablePercentDiscounts.Active = config.AllowPercentDiscounts;
            chkEnableValueDiscounts.Active = config.AllowValueDiscounts;
            chkEnableLotName.Active = config.AllowItemLotName;
            chkEnableSerialNumber.Active = config.AllowItemSerialNumber;
            chkEnableExpirationDate.Active = config.AllowItemExpirationDate;
            chkEnableManufacturedDate.Active = config.AllowItemManufacturedDate;
            chkEnableLocation.Active = config.AllowItemLocation;
            chkEnableVatRate.Active = config.EnableItemVatRate;

            chkAllowNegativeAvailability.Active = config.AllowNegativeAvailability;
            chkAutoCreateInvoiceOnSale.Active = config.AutoCreateInvoiceOnSale;
            chkAutoCreateInvoiceOnPurchase.Active = config.AutoCreateInvoiceOnPurchase;
            chkAutoProduction.Active = config.AutoProduction;
            chkMaximumAllowedQuantity.Active = config.MaximumAllowedQuantity > 0;
            spbMaximumAllowedQuantity.Value = config.MaximumAllowedQuantity;
            chkWarningMaximumQuantity.Active = config.WarningMaximumQuantity > 0;
            spbWarningMaximumQuantity.Value = config.WarningMaximumQuantity;
            chkLogAllChangesInOperations.Active = config.LogAllChangesInOperations;
            chkLimitDocumentNumberLength.Active = config.LimitDocumentNumber;
            chkShowOperationStatistics.Active = config.ShowOperationStatistics;

            cboItemsManagementType.Changed += cboItemsManagementType_Changed;
            cboItemsManagementType_Changed (null, null);

            #endregion

            #region Codes settings

            chkAutoGeneratePartnerCodes.Active = config.AutoGeneratePartnerCodes;
            chkAutoGenerateItemCodes.Active = config.AutoGenerateItemCodes;
            chkAutoGenerateUserCodes.Active = config.AutoGenerateUserCodes;
            chkAutoGenerateLocationCodes.Active = config.AutoGenerateLocationCodes;

            txtPartnerCodePattern.Text = config.PartnerCodePattern;
            txtItemCodePattern.Text = config.ItemCodePattern;
            txtUserCodePattern.Text = config.UserCodePattern;
            txtLocationCodePattern.Text = config.LocationCodePattern;

            txtPartnerCodePattern.Changed += txtPartnerCodePattern_Changed;
            txtItemCodePattern.Changed += txtItemCodePattern_Changed;
            txtUserCodePattern.Changed += txtUserCodePattern_Changed;
            txtLocationCodePattern.Changed += txtLocationCodePattern_Changed;

            RefreshPartnerCodesPreview ();
            RefreshItemCodesPreview ();
            RefreshUserCodesPreview ();
            RefreshLocationCodesPreview ();

            cboGeneratedBarcodeType.Load (Translator.GetBarcodeTypes (), "Key", "Value", (int) config.GeneratedBarcodeType);
            cboGeneratedBarcodeType.Changed += (sender, args) => RefreshBarcodePreview ();
            txtBarcodePrefix.Text = config.GeneratedBarcodePrefix;
            txtBarcodePrefix.Changed += (sender, args) => RefreshBarcodePreview ();
            RefreshBarcodePreview ();

            #endregion

            #region Printing settings

            chkDefaultDocumentPrinter.Active = config.UseDefaultDocumentPrinter;
            chkDefaultDocumentPrinter.Toggled += chkDefaultDocumentPrinter_Toggled;
            chkDefaultDocumentPrinter_Toggled (null, null);

            printers.AddRange (BusinessDomain.AppConfiguration.GetAllInstalledPrinters ());
            defaultPrinter = BusinessDomain.AppConfiguration.GetDefaultPrinterName ();

            string docPrinter = config.DocumentPrinterName;
            cboDocumentPrinter.Load (printers, null, null, docPrinter);

            cboAskBeforePrint.Load (Translator.GetAskDialogStates (), "Key", "Value", (int) config.AskBeforeDocumentPrint);

            spbMarginTop.Value = config.PrinterMarginTop;
            spbMarginBottom.Value = config.PrinterMarginBottom;
            spbMarginLeft.Value = config.PrinterMarginLeft;
            spbMarginRight.Value = config.PrinterMarginRight;
            chkAlwaysPrintTransfersUsingSalePrices.Active = config.AlwaysPrintTransfersUsingSalePrices;

            chkPrintCashReceiptByDefault.Active = config.PrintFinalReceiptByDefault;
            chkPrintBankCashReceiptByDefault.Active = config.PrintBankCashReceiptByDefault;
            chkPrintSaleCode.Active = config.PrintSaleCode;
            chkPrintOrderCode.Active = config.PrintOrderCodeOnReceipts;
            chkPrintLocation.Active = config.PrintLocationOnReceipts;
            chkPrintPartner.Active = config.PrintPartnerOnReceipts;
            chkPrintOperator.Active = config.PrintOperatorOnReceipts;
            chkPrintSaleBarCode.Active = config.PrintSaleBarCode;
            chkPrintSaleBarCode.Toggled += chkPrintSaleBarCode_Toggled;
            chkPrintSaleBarCode_Toggled (null, null);

            List<KeyValuePair<object, string>> barCodeTypes = Enum.GetValues (typeof (GeneratedBarcodeType))
                .Cast<object> ()
                .Select (value => new KeyValuePair<object, string> (value, value.ToString ()))
                .ToList ();
            cboBarCodeType.Load (barCodeTypes, "Key", "Value", config.SaleBarCodeType);
            chkPrintSaleBarCodeNumber.Active = config.PrintSaleBarCodeNumber;

            #endregion

            #region E-mail settings

            txtSmtpServer.Text = config.SmtpServer;
            spbSmtpPort.Value = config.SmtpPort;
            chkEmailUseSsl.Active = config.SmtpUseSsl;
            if (!string.IsNullOrEmpty (config.SmtpUserName)) {
                chkUseCredentials.Active = true;
                txtSmtpUserName.Text = config.SmtpUserName;
                txtSmtpPassword.Text = config.SmtpPassword;
            }
            txtEmailSender.Text = config.EmailSender;
            txtEmailSubject.Text = config.EmailSubject;

            #endregion

            #region Advanced settings

            chkShowSplashScreen.Active = config.ShowSplashScreen;
            chkRegisterCashAtStartup.Active = config.RegisterCashAtStartup;

            cboStartupPage.Load (PresentationDomain.StartupPages, "ClassName", "Name", BusinessDomain.AppConfiguration.StartupPageClass);

            chkUsePricesWithVATIncluded.Active = config.VATIncluded;
            chkRoundPrices.Active = config.RoundedPrices;
            chkWarnPricesSaleLowerThanPurchase.Active = config.WarnPricesSaleLowerThanPurchase;
            chkAlwaysChooseTradePointPartner.Active = config.AlwaysChoosePartnerInTradePoint;
            chkShowItemNameOnExtDisplay.Active = !config.ExtDisplayDigitsOnly;
            chkAllowDatabaseChange.Active = config.AllowDbChange;
            chkAllowMultipleInstances.Active = config.AllowMultipleInstances;
            chkEnableVerboseErrorLogging.Active = config.VerboseErrorLogging;
            chkDocumentNumbersPerLocation.Active = config.DocumentNumbersPerLocation;
            chkDocumentNumbersPerLocation.Sensitive = BusinessDomain.LoggedUser.UserLevel >= UserAccessLevel.Administrator;
            if (!chkDocumentNumbersPerLocation.Sensitive)
                chkDocumentNumbersPerLocation.TooltipText = Translator.GetString ("Administrators and Owners can change this setting");
            chkConfirmPriceRules.Active = config.ConfirmPriceRules;
            chkUseSalesTaxInsteadOfVAT.Active = config.UseSalesTaxInsteadOfVAT;
            chkShowPartnerSuggestionsWhenNotFound.Active = config.ShowPartnerSuggestionsWhenNotFound;
            chkShowItemSuggestionsWhenNotFound.Active = config.ShowItemSuggestionsWhenNotFound;
            txtBankNotesAndCoins.Text = FilterBankNotesAndCoins (config.BankNotesAndCoins);
            cboDistributedChargeMethod.Load (Business.Entities.Item.GetAllDistributedChargeMethods (), "Key", "Value", config.DistributedChargeMethod);

            #endregion
        }

        private static string FilterBankNotesAndCoins (string bankNotesAndCoins)
        {
            StringBuilder ret = new StringBuilder ();
            foreach (string bankNote in bankNotesAndCoins.Split (';')) {
                double value;
                if (!Currency.TryParseExpression (bankNote, out value))
                    continue;

                ret.Append (Currency.ToEditString (value) + ';');
            }

            return ret.Length > 0 ? ret.ToString (0, ret.Length - 1) : string.Empty;
        }

        private void LoadDocumentTemplates ()
        {
            ConfigurationHolder config = BusinessDomain.AppConfiguration;

            Dictionary<string, string> documentTemplatesDirectories = new Dictionary<string, string>
                {
                    { string.Empty, "Default" }
                };

            if (Directory.Exists (StoragePaths.DocumentTemplatesFolder))
                foreach (string directory in Directory.GetDirectories (StoragePaths.DocumentTemplatesFolder))
                    documentTemplatesDirectories.Add (directory, Path.GetFileName (directory));

            if (Directory.Exists (StoragePaths.CustomDocumentsFolder))
                foreach (string directory in Directory.GetDirectories (StoragePaths.CustomDocumentsFolder))
                    documentTemplatesDirectories.Add (directory, Path.GetFileName (directory));

            cboDocumentTemplates.Load (documentTemplatesDirectories, "Key", "Value", config.DocumentTemplatesFolder);
        }

        private void InitializeAddins ()
        {
            foreach (SettingsPage settingsPage in AddinManager.GetExtensionNodes ("/Warehouse/Presentation/Settings")
                .Cast<TypeExtensionNode> ()
                .Select (node => node.CreateInstance () as SettingsPage)
                .Where (page => page != null)
                .OrderBy (page => page.Priority)) {
                settingsPage.Index = nbMain.AppendPage (settingsPage, settingsPage.PageLabel);
                settingsPage.Show ();
                addins.Add (settingsPage);
            }
        }

        #region Visual settings

        private void chkSystemLocalization_Toggled (object sender, EventArgs e)
        {
            if (chkSystemLocalization.Active) {
                lblLocalization.Sensitive = false;
                cboLocalization.Sensitive = false;
                cboLocalization.SetSelection (localizations, "Key", "Value", currentLocale);
            } else {
                lblLocalization.Sensitive = true;
                cboLocalization.Sensitive = true;
            }
        }

        private void cbeCurrencySign_Changed (object sender, EventArgs e)
        {
            RefreshCurrencyPreview ();
            RefreshPurchaseCurrencyPreview ();
        }

        private void cboCurrencyPrecision_Changed (object sender, EventArgs e)
        {
            RefreshCurrencyPreview ();
        }

        private void chkPurchaseCurrencyPrecision_Toggled (object sender, EventArgs e)
        {
            RefreshPurchaseCurrencySensitivity ();
        }

        private void cboPurchaseCurrencyPrecision_Changed (object sender, EventArgs e)
        {
            RefreshPurchaseCurrencyPreview ();
        }

        private void cboQuantityPrecision_Changed (object sender, EventArgs e)
        {
            RefreshQuantityPreview ();
        }

        private void cboPercentPrecision_Changed (object sender, EventArgs e)
        {
            RefreshPercentPreview ();
        }

        private void cboDocumentNumbers_Changed (object sender, EventArgs e)
        {
            RefreshDocumentNumbersPreview ();
        }

        private void txtCustomDateFormat_Changed (object sender, EventArgs e)
        {
            RefreshDatePreview ();
        }

        private void GetCurrencyFormat (out string currencySymbol, out int currencySymbolPosition)
        {
            currencySymbol = null;
            currencySymbolPosition = 0;
            string value = cbeCurrencySign.Entry.Text.Trim ();
            if (value.StartsWith ("#") == value.EndsWith ("#"))
                return;

            if (value.StartsWith ("#")) {
                currencySymbol = value.Substring (value.LastIndexOf ('#') + 1);
                currencySymbolPosition = 1;
            } else {
                currencySymbol = value.Substring (0, value.IndexOf ('#'));
                currencySymbolPosition = -1;
            }
        }

        private void RefreshCurrencyPreview ()
        {
            string currencySymbol;
            int currencySymbolPosition;
            GetCurrencyFormat (out currencySymbol, out currencySymbolPosition);

            int selection = (int) cboCurrencyPrecision.GetSelectedValue ();
            string value = Currency.ToString (1234.123456789d, selection, currencySymbol, currencySymbolPosition);

            lblCurrencyPreviewValue.SetText (value);
            if (!chkPurchaseCurrencyPrecision.Active)
                cboPurchaseCurrencyPrecision.SetSelection ((object) selection);
        }

        private void RefreshPurchaseCurrencySensitivity ()
        {
            if (chkPurchaseCurrencyPrecision.Active) {
                cboPurchaseCurrencyPrecision.Sensitive = true;
                lblPurchaseCurrencyPreview.Sensitive = true;
                lblPurchaseCurrencyPreviewValue.Sensitive = true;
            } else {
                cboPurchaseCurrencyPrecision.Sensitive = false;
                lblPurchaseCurrencyPreview.Sensitive = false;
                lblPurchaseCurrencyPreviewValue.Sensitive = false;
            }
        }

        private void RefreshPurchaseCurrencyPreview ()
        {
            string currencySymbol;
            int currencySymbolPosition;
            GetCurrencyFormat (out currencySymbol, out currencySymbolPosition);

            string value = Currency.ToString (1234.123456789d, (int) cboPurchaseCurrencyPrecision.GetSelectedValue (), currencySymbol, currencySymbolPosition);

            lblPurchaseCurrencyPreviewValue.SetText (value);
        }

        private void RefreshQuantityPreview ()
        {
            string value = Quantity.ToString (1234.123456789d, (int) cboQuantityPrecision.GetSelectedValue ());

            lblQuantityPreviewValue.SetText (value);
        }

        private void RefreshPercentPreview ()
        {
            string value = Percent.ToString (1234.123456789d, (int) cboPercentPrecision.GetSelectedValue ());

            lblPercentPreviewValue.SetText (value);
        }

        private void RefreshDocumentNumbersPreview ()
        {
            string value = Operation.GetFormattedOperationNumber (1234, (int) cboDocumentNumbers.GetSelectedValue ());

            lblDocumentNumbersPreviewValue.SetText (value);
        }

        private void RefreshDatePreview ()
        {
            DateTime date = BusinessDomain.Today;
            string enteredValue = cbeCustomDateFormat.Entry.Text;
            string value = BusinessDomain.GetFormattedDate (date, enteredValue);
            DateTime parsedDate = BusinessDomain.GetDateValue (value, enteredValue);

            if (date != parsedDate || (enteredValue.Length > 0 &&
                (!enteredValue.Contains ("d") || !enteredValue.Contains ("M") || !enteredValue.Contains ("y") ||
                enteredValue.Contains ("H") || enteredValue.Contains ("h") || enteredValue.Contains ("m")))) {
                lblDatePreviewValue.SetText (Translator.GetString ("Error!"));
                correctDate = false;
            } else {
                lblDatePreviewValue.SetText (value);
                correctDate = true;
            }
        }

        #endregion

        #region Operation settings

        private void cboItemsManagementType_Changed (object sender, EventArgs e)
        {
            ItemsManagementType currentManagmentType = (ItemsManagementType) cboItemsManagementType.GetSelectedValue ();
            bool lastUseLots = lastItemManagementType != ItemsManagementType.AveragePrice &&
                lastItemManagementType != ItemsManagementType.QuickAveragePrice &&
                lastItemManagementType != ItemsManagementType.LastPurchasePrice;
            bool nowUseLots = currentManagmentType != ItemsManagementType.AveragePrice &&
                currentManagmentType != ItemsManagementType.QuickAveragePrice &&
                currentManagmentType != ItemsManagementType.LastPurchasePrice;

            if (lastUseLots != nowUseLots && sender != null) {
                if (Message.ShowDialog (
                    Translator.GetString ("Change item management method?"), "Icons.Question32.png",
                    Translator.GetString ("Warning! Changing this setting causes the use of a new method for data storage. There is a small possibility of data loss during the conversion process. This change should be done only on a database without any saved operations. Do you want to continue with this change?"), "Icons.Question32.png",
                    MessageButtons.YesNo) != ResponseType.Yes) {
                    cboItemsManagementType.Load (itemManagmentTypes, "Key", "Value", lastItemManagementType);
                    return;
                }
            }

            if (currentManagmentType == ItemsManagementType.QuickAveragePrice &&
                lastItemManagementType != ItemsManagementType.QuickAveragePrice &&
                sender != null) {
                if (Message.ShowDialog (
                    Translator.GetString ("Change item management method?"), "Icons.Question32.png",
                    Translator.GetString ("Warning! Quick average purchase price calculation will increase the speed with which income operations are saved, but may produce less accurate average prices when old operations are editied or annulled or when sales are made before purchases! Do you want to continue with this change?"), "Icons.Question32.png",
                    MessageButtons.YesNo) != ResponseType.Yes) {
                    cboItemsManagementType.Load (itemManagmentTypes, "Key", "Value", lastItemManagementType);
                    return;
                }
            }

            chkEnableLotName.Sensitive = true;
            chkEnableSerialNumber.Sensitive = true;
            chkEnableExpirationDate.Sensitive = true;
            chkEnableManufacturedDate.Sensitive = true;
            chkEnableLocation.Sensitive = true;
            chkEnableExpirationDate.Sensitive = true;
            chkAllowNegativeAvailability.Sensitive = true;

            switch (currentManagmentType) {
                case ItemsManagementType.AveragePrice:
                case ItemsManagementType.QuickAveragePrice:
                case ItemsManagementType.LastPurchasePrice:
                    chkEnableLotName.Active = false;
                    chkEnableSerialNumber.Active = false;
                    chkEnableExpirationDate.Active = false;
                    chkEnableManufacturedDate.Active = false;
                    chkEnableLocation.Active = false;

                    chkEnableLotName.Sensitive = false;
                    chkEnableSerialNumber.Sensitive = false;
                    chkEnableExpirationDate.Sensitive = false;
                    chkEnableManufacturedDate.Sensitive = false;
                    chkEnableLocation.Sensitive = false;
                    break;
                case ItemsManagementType.FIFO:
                case ItemsManagementType.LIFO:
                case ItemsManagementType.Choice:
                    chkAllowNegativeAvailability.Active = false;
                    chkAllowNegativeAvailability.Sensitive = false;
                    break;
                case ItemsManagementType.FEFO:
                    chkEnableExpirationDate.Active = true;
                    chkAllowNegativeAvailability.Active = false;

                    chkEnableExpirationDate.Sensitive = false;
                    chkAllowNegativeAvailability.Sensitive = false;
                    break;
            }

            lastItemManagementType = currentManagmentType;
        }

        [UsedImplicitly]
        private void MaximumAllowedQuantity_Toggled (object o, EventArgs args)
        {
            AdjustSpinButton (spbMaximumAllowedQuantity, chkMaximumAllowedQuantity.Active);
        }

        [UsedImplicitly]
        private void WarningMaximumQuantity_Toggled (object o, EventArgs args)
        {
            AdjustSpinButton (spbWarningMaximumQuantity, chkWarningMaximumQuantity.Active);
        }

        private static void AdjustSpinButton (SpinButton spinButton, bool active)
        {
            spinButton.Sensitive = active;
            spinButton.Adjustment.Lower = spinButton.Sensitive ? 1 : 0;
            if (spinButton.Value < spinButton.Adjustment.Lower)
                spinButton.Value = spinButton.Adjustment.Lower;
        }

        #endregion

        #region Entity settings

        private void txtPartnerCodePattern_Changed (object sender, EventArgs e)
        {
            RefreshPartnerCodesPreview ();
        }

        private void txtItemCodePattern_Changed (object sender, EventArgs e)
        {
            RefreshItemCodesPreview ();
        }

        private void txtUserCodePattern_Changed (object sender, EventArgs e)
        {
            RefreshUserCodesPreview ();
        }

        private void txtLocationCodePattern_Changed (object sender, EventArgs e)
        {
            RefreshLocationCodesPreview ();
        }

        private void RefreshPartnerCodesPreview ()
        {
            lblPartnerCodePatternPreviewValue.SetText (GetCodePreview (txtPartnerCodePattern.Text));
        }

        private void RefreshItemCodesPreview ()
        {
            lblItemCodePatternPreviewValue.SetText (GetCodePreview (txtItemCodePattern.Text));
        }

        private void RefreshUserCodesPreview ()
        {
            lblUserCodePatternPreviewValue.SetText (GetCodePreview (txtUserCodePattern.Text));
        }

        private void RefreshLocationCodesPreview ()
        {
            lblLocationCodePatternPreviewValue.SetText (GetCodePreview (txtLocationCodePattern.Text));
        }

        private static string GetCodePreview (string pattern)
        {
            string newCode;
            try {
                newCode = CodeGenerator.GenerateCode (pattern, 123);
            } catch (Exception) {
                newCode = Translator.GetString ("Invalid pattern!");
            }
            return newCode;
        }

        private void RefreshBarcodePreview ()
        {
            try {
                lblBarcodePatternPreviewValue.SetText (BarcodeGenerator.Generate ((GeneratedBarcodeType) cboGeneratedBarcodeType.GetSelectedValue (), txtBarcodePrefix.Text));
            } catch (InvalidDataException) {
                lblBarcodePatternPreviewValue.SetText (Translator.GetString ("Invalid bar-code"));
            }
        }

        #endregion

        #region Printing settings

        private void chkDefaultDocumentPrinter_Toggled (object sender, EventArgs e)
        {
            if (chkDefaultDocumentPrinter.Active) {
                lblPrinter.Sensitive = false;
                cboDocumentPrinter.Sensitive = false;
                cboDocumentPrinter.SetSelection (printers, null, null, defaultPrinter);
            } else {
                lblPrinter.Sensitive = true;
                cboDocumentPrinter.Sensitive = true;
            }
        }

        private void chkPrintSaleBarCode_Toggled (object sender, EventArgs e)
        {
            if (chkPrintSaleBarCode.Active) {
                cboBarCodeType.Sensitive = true;
                chkPrintSaleBarCodeNumber.Sensitive = true;
            } else {
                cboBarCodeType.Sensitive = false;
                chkPrintSaleBarCodeNumber.Sensitive = false;
            }
        }

        #endregion

        #region Event handling

        [UsedImplicitly]
        private void OnChkUseCredentials_Toggled (object o, EventArgs args)
        {
            lblSmtpUserName.Sensitive = chkUseCredentials.Active;
            txtSmtpUserName.Sensitive = chkUseCredentials.Active;
            lblSmtpPassword.Sensitive = chkUseCredentials.Active;
            txtSmtpPassword.Sensitive = chkUseCredentials.Active;
        }

        [UsedImplicitly]
        private void btnChooseMoney_Clicked (object o, EventArgs args)
        {
            string bankNotesAndCoins = FilterBankNotesAndCoins (txtBankNotesAndCoins.Text.Trim ());
            using (ChooseBankNotesAndCoins dialog = new ChooseBankNotesAndCoins (bankNotesAndCoins))
                if (dialog.Run () == ResponseType.Ok)
                    txtBankNotesAndCoins.Text = FilterBankNotesAndCoins (dialog.BankNotesAndCoins);
        }

        [UsedImplicitly]
        private void btnOK_Clicked (object o, EventArgs args)
        {
            foreach (SettingsPage settingsPage in addins.Where (page => !page.SaveSettings ())) {
                nbMain.Page = settingsPage.Index;
                return;
            }

            if (!ValidateSettings ())
                return;

            ConfigurationHolder config = BusinessDomain.AppConfiguration;

            // save before a possible change to the current culture
            config.BankNotesAndCoins = FilterBankNotesAndCoins (txtBankNotesAndCoins.Text.Trim ());

            #region Save Visual settings

            bool newUseSystemLocalization = chkSystemLocalization.Active;
            string newLocalization = (string) cboLocalization.GetSelectedValue ();

            bool localizationChanged = newUseSystemLocalization != config.UseSystemLocalization ||
                (!newUseSystemLocalization && (newLocalization != config.Localization));

            if (newUseSystemLocalization) {
                config.UseSystemLocalization = true;
                config.Localization = string.Empty;
            } else {
                config.UseSystemLocalization = false;
                config.Localization = newLocalization;
            }

            if (localizationChanged) {
                Translator.ResetCulture ();
                Translator.InitThread (config, Thread.CurrentThread);
                Translator.TranslateRestrictions ();
                PresentationDomain.MainForm.InitializeStrings ();
                PresentationDomain.MainForm.RefreshStatusBar ();
            }

            config.DocumentTemplatesFolder = (string) cboDocumentTemplates.GetSelectedValue ();
            string currencySymbol;
            int currencySymbolPosition;
            GetCurrencyFormat (out currencySymbol, out currencySymbolPosition);

            config.CurrencySymbol = currencySymbol;
            config.CurrencySymbolPosition = currencySymbolPosition;
            config.CurrencyPrecision = (int) cboCurrencyPrecision.GetSelectedValue ();
            config.UsePurchaseCurrencyPrecision = chkPurchaseCurrencyPrecision.Active;
            config.PurchaseCurrencyPrecision = (int) cboPurchaseCurrencyPrecision.GetSelectedValue ();
            config.QuantityPrecision = (int) cboQuantityPrecision.GetSelectedValue ();
            config.PercentPrecision = (int) cboPercentPrecision.GetSelectedValue ();
            config.DocumentNumberLength = (int) cboDocumentNumbers.GetSelectedValue ();
            config.DateFormat = cbeCustomDateFormat.Entry.Text;
            BusinessDomain.UpdatePurchaseCurrencyPrecision ();

            #endregion

            #region Save Operations settings

            config.ItemsManagementType = (ItemsManagementType) cboItemsManagementType.GetSelectedValue ();
            config.EnableLineNumber = chkEnableLineNumber.Active;
            config.EnableItemCode = chkEnableItemCode.Active;
            config.AllowPercentDiscounts = chkEnablePercentDiscounts.Active;
            config.AllowValueDiscounts = chkEnableValueDiscounts.Active;
            config.EnableItemLotName = chkEnableLotName.Active;
            config.EnableItemSerialNumber = chkEnableSerialNumber.Active;
            config.EnableItemExpirationDate = chkEnableExpirationDate.Active;
            config.EnableItemManufacturedDate = chkEnableManufacturedDate.Active;
            config.EnableItemLocation = chkEnableLocation.Active;
            config.EnableItemVatRate = chkEnableVatRate.Active;

            config.AllowNegativeAvailability = chkAllowNegativeAvailability.Active;
            config.AutoCreateInvoiceOnSale = chkAutoCreateInvoiceOnSale.Active;
            config.AutoCreateInvoiceOnPurchase = chkAutoCreateInvoiceOnPurchase.Active;
            config.AutoProduction = chkAutoProduction.Active;
            spbMaximumAllowedQuantity.Update ();
            config.MaximumAllowedQuantity = chkMaximumAllowedQuantity.Active ? spbMaximumAllowedQuantity.Value : 0;
            spbWarningMaximumQuantity.Update ();
            config.WarningMaximumQuantity = chkWarningMaximumQuantity.Active ? spbWarningMaximumQuantity.Value : 0;
            config.LogAllChangesInOperations = chkLogAllChangesInOperations.Active;
            config.LimitDocumentNumber = chkLimitDocumentNumberLength.Active;
            config.ShowOperationStatistics = chkShowOperationStatistics.Active;

            #endregion

            #region Save Codes settings

            config.AutoGeneratePartnerCodes = chkAutoGeneratePartnerCodes.Active;
            config.PartnerCodePattern = txtPartnerCodePattern.Text;
            config.AutoGenerateItemCodes = chkAutoGenerateItemCodes.Active;
            config.ItemCodePattern = txtItemCodePattern.Text;
            config.AutoGenerateUserCodes = chkAutoGenerateUserCodes.Active;
            config.UserCodePattern = txtUserCodePattern.Text;
            config.AutoGenerateLocationCodes = chkAutoGenerateLocationCodes.Active;
            config.LocationCodePattern = txtLocationCodePattern.Text;

            #endregion

            #region Save Barcodes settings

            config.GeneratedBarcodeType = (GeneratedBarcodeType) cboGeneratedBarcodeType.GetSelectedValue ();
            config.GeneratedBarcodePrefix = txtBarcodePrefix.Text.Trim ();
            int length = config.GeneratedBarcodeType == GeneratedBarcodeType.EAN8 ? 8 : 13;
            if (config.GeneratedBarcodePrefix.Length > length - 2) {
                using (Message warning = new Message (null, null, Translator.GetString ("Your prefix is too long and there are too few bar-code combinations. Are you sure you want to continue?"), "Icons.Question32.png")) {
                    warning.Buttons = MessageButtons.YesNo;
                    if (warning.Run () != ResponseType.Yes)
                        return;
                }
            }

            #endregion

            #region Save Printing settings

            config.AskBeforeDocumentPrint = (AskDialogState) cboAskBeforePrint.GetSelectedValue ();

            if (chkDefaultDocumentPrinter.Active) {
                config.UseDefaultDocumentPrinter = true;
                config.DocumentPrinterName = string.Empty;
            } else {
                config.UseDefaultDocumentPrinter = false;
                config.DocumentPrinterName = (string) cboDocumentPrinter.GetSelectedValue ();
            }

            spbMarginTop.Update ();
            spbMarginBottom.Update ();
            spbMarginLeft.Update ();
            spbMarginRight.Update ();
            config.PrinterMarginTop = spbMarginTop.ValueAsInt;
            config.PrinterMarginBottom = spbMarginBottom.ValueAsInt;
            config.PrinterMarginLeft = spbMarginLeft.ValueAsInt;
            config.PrinterMarginRight = spbMarginRight.ValueAsInt;
            config.AlwaysPrintTransfersUsingSalePrices = chkAlwaysPrintTransfersUsingSalePrices.Active;

            config.PrintFinalReceiptByDefault = chkPrintCashReceiptByDefault.Active;
            config.PrintBankCashReceiptByDefault = chkPrintBankCashReceiptByDefault.Active;
            config.PrintSaleCode = chkPrintSaleCode.Active;
            config.PrintOrderCodeOnReceipts = chkPrintOrderCode.Active;
            config.PrintLocationOnReceipts = chkPrintLocation.Active;
            config.PrintPartnerOnReceipts = chkPrintPartner.Active;
            config.PrintOperatorOnReceipts = chkPrintOperator.Active;
            config.PrintSaleBarCode = chkPrintSaleBarCode.Active;
            config.SaleBarCodeType = (GeneratedBarcodeType) cboBarCodeType.GetSelectedValue ();
            config.PrintSaleBarCodeNumber = chkPrintSaleBarCodeNumber.Active;

            #endregion

            #region Save E-mail settings

            config.SmtpServer = txtSmtpServer.Text.Trim ();
            spbSmtpPort.Update ();
            config.SmtpPort = spbSmtpPort.ValueAsInt;
            config.SmtpUseSsl = chkEmailUseSsl.Active;
            if (chkUseCredentials.Active) {
                config.SmtpUserName = txtSmtpUserName.Text.Trim ();
                config.SmtpPassword = txtSmtpPassword.Text.Trim ();
            } else {
                config.SmtpUserName = string.Empty;
                config.SmtpPassword = string.Empty;
            }
            config.EmailSender = txtEmailSender.Text.Trim ();
            config.EmailSubject = txtEmailSubject.Text.Trim ();

            #endregion

            #region Save Advanced settings

            config.ShowSplashScreen = chkShowSplashScreen.Active;
            config.RegisterCashAtStartup = chkRegisterCashAtStartup.Active;
            config.StartupPageClass = (string) cboStartupPage.GetSelectedValue ();

            config.VATIncluded = chkUsePricesWithVATIncluded.Active;
            config.RoundedPrices = chkRoundPrices.Active;
            config.WarnPricesSaleLowerThanPurchase = chkWarnPricesSaleLowerThanPurchase.Active;
            config.AlwaysChoosePartnerInTradePoint = chkAlwaysChooseTradePointPartner.Active;
            config.ExtDisplayDigitsOnly = !chkShowItemNameOnExtDisplay.Active;
            config.AllowDbChange = chkAllowDatabaseChange.Active;
            config.AllowMultipleInstances = chkAllowMultipleInstances.Active;
            config.VerboseErrorLogging = chkEnableVerboseErrorLogging.Active;
            config.DocumentNumbersPerLocation = chkDocumentNumbersPerLocation.Active;
            config.ConfirmPriceRules = chkConfirmPriceRules.Active;
            config.UseSalesTaxInsteadOfVAT = chkUseSalesTaxInsteadOfVAT.Active;
            config.ShowPartnerSuggestionsWhenNotFound = chkShowPartnerSuggestionsWhenNotFound.Active;
            config.ShowItemSuggestionsWhenNotFound = chkShowItemSuggestionsWhenNotFound.Active;
            config.DistributedChargeMethod = (int) cboDistributedChargeMethod.GetSelectedValue ();

            #endregion

            bool oldUseLots = oldItemManagementType != ItemsManagementType.AveragePrice &&
                oldItemManagementType != ItemsManagementType.QuickAveragePrice &&
                oldItemManagementType != ItemsManagementType.LastPurchasePrice;
            bool nowUseLots = config.ItemsManagementUseLots;

            if (oldUseLots != nowUseLots) {
                try {
                    if (nowUseLots)
                        Lot.EnableLots ();
                    else
                        Lot.DisableLots ();
                } catch (InsufficientItemAvailabilityException) {
                    MessageError.ShowDialog (Translator.GetString ("There are items with negative availability! Make sure that there are no items with negative availability before changing the item management method!"));
                    return;
                } catch (MixedPriceInItemException ex) {
                    MessageError.ShowDialog (string.Format (Translator.GetString ("There are items like \"{0}\" used in operations with different purchase prices! Make sure that there are no operations which use the same item with different purchase prices before changing the item management method!"), ex.ItemName));
                    return;
                } catch (Exception ex) {
                    MessageError.ShowDialog (Translator.GetString ("An error occurred while converting the database to use the new item management method!"), ErrorSeverity.Error, ex);
                    return;
                }
            }

            config.Save (true);

            if (oldVATIncluded != config.VATIncluded) {
                if (BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT) {
                    ApplicationLogEntry.AddNew (config.VATIncluded ?
                        Translator.GetString ("Sales tax included changed to enabled") :
                        Translator.GetString ("Sales tax included changed to disabled"));
                } else {
                    ApplicationLogEntry.AddNew (config.VATIncluded ?
                        Translator.GetString ("VAT included changed to enabled") :
                        Translator.GetString ("VAT included changed to disabled"));
                }
            }

            if (oldAllowNegativeAvailability != config.AllowNegativeAvailability) {
                ApplicationLogEntry.AddNew (config.AllowNegativeAvailability ?
                    Translator.GetString ("Allow negative availability changed to enabled") :
                    Translator.GetString ("Allow negative availability changed to disabled"));
            }

            if (oldItemManagementType != config.ItemsManagementType)
                ApplicationLogEntry.AddNew (string.Format (Translator.GetString ("Item management type changed to: {0}"), config.ItemsManagementType));

            dlgSettings.Respond (ResponseType.Ok);

            if (oldDocumentNumbersPerLocation != config.DocumentNumbersPerLocation)
                if (config.DocumentNumbersPerLocation)
                    using (EditDocumentNumbersPerLocation editDocumentNumbersPerLocation = new EditDocumentNumbersPerLocation ())
                        editDocumentNumbersPerLocation.Run ();
                else
                    OperationNumberingInfo.Delete ();
        }

        private bool ValidateSettings ()
        {
            if (!correctDate) {
                MessageError.ShowDialog (
                    Translator.GetString ("The custom date format entered is not valid. Please choose a valid date format. The format should contain day (\"d\"), month (\"M\") and year (\"y\") symbols. Or leave blank to use the default format."),
                    ErrorSeverity.Information);
                nbMain.Page = (int) Page.Visual;
                cbeCustomDateFormat.GrabFocus ();
                return false;
            }

            string selectedDocPrinter = (string) cboDocumentPrinter.GetSelectedValue ();
            if (!chkDefaultDocumentPrinter.Active) {
                if (string.IsNullOrEmpty (selectedDocPrinter)) {
                    MessageError.ShowDialog (Translator.GetString ("There is no printer selected for document printing!"),
                        ErrorSeverity.Error);
                    return false;
                }
            }

            if (!string.IsNullOrEmpty (txtSmtpServer.Text.Trim ()) &&
                !Validator.CheckInternetAddress (txtSmtpServer.Text.Trim ())) {
                MessageError.ShowDialog (Translator.GetString ("Please enter a valid IP or a valid internet address for the SMTP server."),
                    ErrorSeverity.Error);
                nbMain.Page = (int) Page.Email;
                txtSmtpServer.GrabFocus ();
                return false;
            }

            if (!string.IsNullOrEmpty (txtEmailSender.Text.Trim ()) &&
                !Validator.CheckEmail (txtEmailSender.Text.Trim ())) {
                MessageError.ShowDialog (Translator.GetString ("The entered e-mail address is invalid."), ErrorSeverity.Error);
                nbMain.Page = (int) Page.Email;
                txtEmailSender.GrabFocus ();
                return false;
            }

            if (chkUseCredentials.Active) {
                if (string.IsNullOrEmpty (txtSmtpUserName.Text.Trim ())) {
                    MessageError.ShowDialog (Translator.GetString ("User name cannot be empty!"), ErrorSeverity.Error);
                    nbMain.Page = (int) Page.Email;
                    txtSmtpUserName.GrabFocus ();
                    return false;
                }
            }

            if (chkAutoGeneratePartnerCodes.Active && !CodeGenerator.PatternIsValid (txtPartnerCodePattern.Text)) {
                MessageError.ShowDialog (Translator.GetString ("The partner code pattern is not valid. The pattern should be of the following format: <prefix>#<suffix>"), ErrorSeverity.Error);
                nbMain.Page = (int) Page.Codes;
                txtPartnerCodePattern.GrabFocus ();
                return false;
            }

            if (chkAutoGenerateItemCodes.Active && !CodeGenerator.PatternIsValid (txtItemCodePattern.Text)) {
                MessageError.ShowDialog (Translator.GetString ("The item code pattern is not valid. The pattern should be of the following format: <prefix>#<suffix>"), ErrorSeverity.Error);
                nbMain.Page = (int) Page.Codes;
                txtItemCodePattern.GrabFocus ();
                return false;
            }

            if (chkAutoGenerateUserCodes.Active && !CodeGenerator.PatternIsValid (txtUserCodePattern.Text)) {
                MessageError.ShowDialog (Translator.GetString ("The user code pattern is not valid. The pattern should be of the following format: <prefix>#<suffix>"), ErrorSeverity.Error);
                nbMain.Page = (int) Page.Codes;
                txtUserCodePattern.GrabFocus ();
                return false;
            }

            if (chkAutoGenerateLocationCodes.Active && !CodeGenerator.PatternIsValid (txtLocationCodePattern.Text)) {
                MessageError.ShowDialog (Translator.GetString ("The location code pattern is not valid. The pattern should be of the following format: <prefix>#<suffix>"), ErrorSeverity.Error);
                nbMain.Page = (int) Page.Codes;
                txtLocationCodePattern.GrabFocus ();
                return false;
            }

            if (!Validator.ValidateBankNotesAndCoins (txtBankNotesAndCoins.Text.Trim (), true)) {
                MessageError.ShowDialog (Translator.GetString ("The list of bank notes and coins is invalid. The list must contain only the " +
                    "allowed currency values, separated by ';'."), ErrorSeverity.Error);
                nbMain.Page = (int) Page.Special;
                txtBankNotesAndCoins.GrabFocus ();
                return false;
            }

            if (oldDocumentNumbersPerLocation &&
                !chkDocumentNumbersPerLocation.Active &&
                MessageError.ShowDialog (string.Format (Translator.GetString ("You are disabling the option \"{0}\"! If you disable this option now after new operations are made it may be impossible to turn the option back on. Are you sure you want to disable it?"),
                    Translator.GetString ("Document numbers per location")), ErrorSeverity.Warning, null, MessageButtons.YesNo) != ResponseType.Yes) {
                nbMain.Page = (int) Page.Special;
                chkDocumentNumbersPerLocation.GrabFocus ();
                return false;
            }

            return true;
        }

        [UsedImplicitly]
        private void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgSettings.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
