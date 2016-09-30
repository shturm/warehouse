//
// PresentationDomain.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   07/01/2006
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
using System.IO;
using System.Linq;
using System.Threading;
using Gdk;
using GLib;
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Business.MOTranslator;
using Warehouse.Business.Operations;
using Warehouse.Business.Reporting;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Data.Model;
using Warehouse.Presentation.Dialogs;
using Warehouse.Presentation.OS;
using Warehouse.Presentation.SetupAssistant;
using Action = System.Action;
using Assistant = Warehouse.Presentation.SetupAssistant.Assistant;
using Item = Warehouse.Business.Entities.Item;
using Key = Gdk.Key;
using Settings = Warehouse.Presentation.Dialogs.Settings;
using Thread = System.Threading.Thread;
using Timeout = GLib.Timeout;

namespace Warehouse.Presentation
{
    public enum ScreenResolution
    {
        Low,
        Normal,
        High
    }

    public delegate void MessageHandler (string message);

    public static class PresentationDomain
    {
        private static readonly object hwErrorSyncRoot = new object ();
        private static readonly List<StartupPage> startupPages = new List<StartupPage> ();
        private static readonly ManualResetEvent mainFormLoaded = new ManualResetEvent (false);

        private static FrmMain mainForm;
        private static OSIntegration osIntegration;
        private static bool chooseDB;
        private static bool chooseUser;
        private static bool interactiveConnect;
        private static bool quitting;
        private static bool restarting;
        private static int uiThreadId;
        private static Size? screenSize;
        private static ScreenResolution? screenResolution;
        private static FileStream instanceLock;

        public static event EventHandler ShowingDialog;
        public static event EventHandler<CardReadArgs> CardRecognized;
        public static event EventHandler<QuittingEventArgs> Quitting;

        public static FrmMain MainForm
        {
            get { return mainForm; }
        }

        public static OSIntegration OSIntegration
        {
            get { return osIntegration; }
        }

        public static bool MainFormCreated
        {
            get { return mainForm != null; }
        }

        public static IList<StartupPage> StartupPages
        {
            get
            {
                InitAddins ();
                return startupPages;
            }
        }

        public static ReportEntryNode ReportsTree { get; set; }

        public static Size? ScreenSize
        {
            get
            {
                if (screenSize == null) {
                    Screen defScreen = Screen.Default;
                    if (defScreen != null) {
                        Rectangle geometry = defScreen.GetMonitorGeometry (defScreen.Number);
                        screenSize = new Size (geometry.Width, geometry.Height);
                    }
                }

                return screenSize;
            }
        }

        public static ScreenResolution ScreenResolution
        {
            get
            {
                if (screenResolution == null) {
                    Size? size = ScreenSize;
                    if (size != null) {
                        Size value = size.Value;
                        if (value.Width > 1280 && value.Height > 1024)
                            screenResolution = ScreenResolution.High;
                        else if (value.Width >= 1024 && value.Height >= 768)
                            screenResolution = ScreenResolution.Normal;
                        else
                            screenResolution = ScreenResolution.Low;
                    }
                }

                return screenResolution ?? ScreenResolution.Normal;
            }
        }

        public static ManualResetEvent MainFormLoaded
        {
            get { return mainFormLoaded; }
        }

        public static void PreInit ()
        {
            switch (PlatformHelper.Platform) {
                case PlatformTypes.Windows:
                    osIntegration = new WindowsIntegration ();
                    break;
                case PlatformTypes.Linux:
                    osIntegration = new LinuxIntegration ();
                    break;
                case PlatformTypes.MacOSX:
                    osIntegration = new MacOSXIntegration ();
                    break;
            }

            if (!GLib.Thread.Supported)
                GLib.Thread.Init ();

            Application.Init ();
        }

        public static void InitAddins ()
        {
            startupPages.Clear ();
            startupPages.Add (new StartupPage (Translator.GetString ("Sale"), typeof (WbpSale)));
            startupPages.Add (new StartupPage (Translator.GetString ("Purchase"), typeof (WbpPurchase)));
            startupPages.Add (new StartupPage (Translator.GetString ("Production"), typeof (WbpProduction)));
            startupPages.Add (new StartupPage (Translator.GetString ("Transfer"), typeof (WbpTransfer)));
            startupPages.Add (new StartupPage (Translator.GetString ("Waste"), typeof (WbpWaste)));
            startupPages.Add (new StartupPage (Translator.GetString ("Point of Sale"), typeof (WbpTradePoint)));

            startupPages.AddRange (AddinManager.GetExtensionNodes ("/Warehouse/Presentation/WorkBook")
                .Cast<TypeExtensionNode> ()
                .Select (node => node.CreateInstance ())
                .OfType<StartupPage> ());

            startupPages.Sort ((sp1, sp2) => string.Compare (sp1.Name, sp2.Name));
            startupPages.Insert (0, new StartupPage (Translator.GetString ("<none>"), string.Empty));
        }

        public static void Init (MessageHandler msgHandler)
        {
            uiThreadId = Thread.CurrentThread.ManagedThreadId;
            BusinessDomain.DeviceManager.CardRecognized -= DeviceManager_CardRecognized;
            BusinessDomain.DeviceManager.CardRecognized += DeviceManager_CardRecognized;

            KeyShortcuts.Load ();

            InitFirstTimeUse ();
            InitStartups (msgHandler);
            msgHandler (Translator.GetString ("Connecting to database..."));

            InitReportTree ();
            InitUserSpace ();

            BusinessDomain.DeviceManager.HardwareError -= DeviceManager_HardwareError;
            BusinessDomain.DeviceManager.HardwareError += DeviceManager_HardwareError;
            BusinessDomain.DeviceManager.HardwareResponseWaitPoll -= DeviceManagerHardwareResponseWaitPoll;
            BusinessDomain.DeviceManager.HardwareResponseWaitPoll += DeviceManagerHardwareResponseWaitPoll;
            BusinessDomain.DeviceManager.KitchenPrinterError -= DeviceManager_KitchenPrinterError;
            BusinessDomain.DeviceManager.KitchenPrinterError += DeviceManager_KitchenPrinterError;
            BusinessDomain.DeviceManager.ReceiptPrintStart -= DeviceManager_ReceiptPrintStart;
            BusinessDomain.DeviceManager.ReceiptPrintStart += DeviceManager_ReceiptPrintStart;
            BusinessDomain.DeviceManager.ReceiptPrintStep -= DeviceManager_ReceiptPrintStep;
            BusinessDomain.DeviceManager.ReceiptPrintStep += DeviceManager_ReceiptPrintStep;
            BusinessDomain.DeviceManager.ReceiptPrintDialogShown -= DeviceManager_ReceiptPrintDialogShown;
            BusinessDomain.DeviceManager.ReceiptPrintDialogShown += DeviceManager_ReceiptPrintDialogShown;
            BusinessDomain.DeviceManager.ReceiptPrintEnd -= DeviceManager_ReceiptPrintEnd;
            BusinessDomain.DeviceManager.ReceiptPrintEnd += DeviceManager_ReceiptPrintEnd;
            BusinessDomain.PriceRuleMessageReceived -= BusinessDomain_PriceRuleMessageReceived;
            BusinessDomain.PriceRuleMessageReceived += BusinessDomain_PriceRuleMessageReceived;
            BusinessDomain.PriceRuleAskedForAdvance -= BusinessDomain_PriceRuleAskedForAdvance;
            BusinessDomain.PriceRuleAskedForAdvance += BusinessDomain_PriceRuleAskedForAdvance;

            ErrorHandling.ExceptionOccurred -= OnErrorHandlingOnExceptionOccurred;
            ErrorHandling.ExceptionOccurred += OnErrorHandlingOnExceptionOccurred;
        }

        private static void OnErrorHandlingOnExceptionOccurred (Exception ex, ErrorSeverity severity)
        {
            BusinessDomain.FeedbackProvider.TrackException (ex.ToString (), severity == ErrorSeverity.FatalError);
        }

        private static void BusinessDomain_PriceRuleMessageReceived (object sender, PriceRuleMessageEventArgs e)
        {
            Invoke (() => Message.ShowDialog (Translator.GetString ("Price Rules"), "Icons.PriceRules16.png",
                e.Message, MessageError.GetDialogIconFromSeverity (e.ErrorSeverity)));
        }

        private static void BusinessDomain_PriceRuleAskedForAdvance (object sender, PriceRuleAskAdvanceEventArgs e)
        {
            Invoke (() =>
                {
                    Partner partner = Partner.GetById (e.PartnerId);
                    if (partner == null)
                        return;

                    using (EditNewAdvancePayment dialog = new EditNewAdvancePayment (partner, alwaysReceive: true)) {
                        if (e.Value > 0)
                            dialog.SetReceived (e.Value);
                        if (dialog.Run () != ResponseType.Ok || dialog.Payments.Count <= 0)
                            return;

                        BindList<Payment> advances = dialog.Payments;
                        List<Payment> savedPayments = Payment.DistributeAdvances (advances, partner.Id);
                        for (int i = advances.Count - 1; i >= 0; i--) {
                            if (advances [i].Sign < 0)
                                advances.RemoveAt (i);
                            else
                                advances [i].CommitAdvance ();
                        }

                        savedPayments.AddRange (advances);
                        if (savedPayments.Count > 0)
                            dialog.PrintPayments (savedPayments);
                    }
                });
        }

        private static void InitFirstTimeUse ()
        {
            BusinessDomain.FeedbackProvider.InitFirstTimeUse ();

            if (!BusinessDomain.AppConfiguration.LocalizationSet) {
                using (ChooseLocalization dialog = new ChooseLocalization ()) {
                    OnShowingDialog ();
                    dialog.Run ();
                    BusinessDomain.AppConfiguration.LocalizationSet = true;
                }
            }

            if (!BusinessDomain.AppConfiguration.AppSetupFinished) {
                using (Assistant assistant = new Assistant (AssistType.ApplicationSetup)) {
                    OnShowingDialog ();
                    assistant.Run ();
                    BusinessDomain.AppConfiguration.AppSetupFinished = true;
                }
            }

            BusinessDomain.AppConfiguration.Save (false);
        }

        private static void InitStartups (MessageHandler msgHandler)
        {
            foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Presentation/StartUp")) {
                object instance = node.CreateInstance ();
                IStartupLauncher startup = instance as IStartupLauncher;
                if (startup == null)
                    continue;

                startup.Launch (msgHandler);
            }
        }

        private static bool initializingUserSpace;

        private static void InitUserSpace ()
        {
            if (initializingUserSpace)
                return;

            try {
                initializingUserSpace = true;
                while (true) {
                    // Create the main form, so the menu and the restrictions are properly loaded
                    if (!MainFormCreated)
                        CreateMainForm ();

                    if (chooseDB || !TryInitDB ()) {
                        if (!BusinessDomain.AppConfiguration.AllowDbChange) {
                            OnShowingDialog ();
                            MessageError.ShowDialog (string.Format (Translator.GetString ("No connection can be made with the database! {0} will exit now."), DataHelper.ProductName), ErrorSeverity.Error);
                            throw new ApplicationAbortException ();
                        }

                        chooseDB = false;
                        CreateDBConnectDialog ();

                        // Save local configuration and possible connection config changes
                        BusinessDomain.AppConfiguration.Save (false);
                        continue;
                    }

                    if (!BusinessDomain.OnDatabaseConnected ()) {
                        chooseDB = true;
                        continue;
                    }

                    // Load the local and remote configuration
                    BusinessDomain.AppConfiguration.Load (false);
                    LoadQuickItems ();
                    if (chooseUser || !TryUserLogin ()) {
                        chooseUser = false;
                        CreateUserLoginDialog ();

                        // Save local configuration and possible last user changes
                        BusinessDomain.AppConfiguration.Save (false);
                    } else {
                        UpdateMainForm ();
                        break;
                    }
                }
            } finally {
                initializingUserSpace = false;
            }
        }

        private static void LoadQuickItems ()
        {
            AccelMap.Foreach (IntPtr.Zero, (pointer, accelPath, key, modifierType, changed) =>
                {
                    if (key == 0 || key == (uint) Key.VoidSymbol)
                        return;

                    string name = accelPath.Substring (accelPath.IndexOf ('/') + 1);
                    long itemId;
                    if (!long.TryParse (name, out itemId))
                        return;

                    Item item = Item.GetById (itemId);
                    if (item == null)
                        return;

                    string keyString = KeyShortcuts.KeyToString ((Key) key, modifierType);
                    BusinessDomain.QuickItems [keyString] = item.Name;
                });
        }

        private static void InitReportTree ()
        {
            ReportsTree = new ReportEntryNode ("mnuReports", null,
                new ReportEntryNode ("mnuRepSale", typeof (ReportQuerySales)),
                new ReportEntryNode ("mnuRepDelivery", typeof (ReportQueryPurchases)),
                new ReportEntryNode ("mnuReportsProduction", null,
                    new ReportEntryNode ("mnuReportsProductionComplexRecipes", typeof (ReportQueryComplexRecipes)),
                    new ReportEntryNode ("mnuReportsProductionComplexProducing", typeof (ReportQueryComplexProductions))),
                new ReportEntryNode ("mnuRepTransfer", typeof (ReportQueryTransfers)),
                new ReportEntryNode ("mnuRepWaste", typeof (ReportQueryWastes)),
                new ReportEntryNode ("mnuRepRevision", typeof (ReportQueryStockTakings)),
                new ReportEntryNode ("mnuReportsAllOperations", typeof (ReportQueryOperations)),
                new ReportEntryNode ("mnuReportsAllDrafts", typeof (ReportQueryDrafts)),
                new ReportEntryNode ("mnuRepNomenclatures", null,
                    new ReportEntryNode ("mnuRepNomenclaturesPartners", typeof (ReportQueryPartners)),
                    new ReportEntryNode ("mnuRepNomenclaturesGoodsByName", typeof (ReportQueryItems)),
                    new ReportEntryNode ("mnuRepNomenclaturesOperators", typeof (ReportQueryUsers)),
                    new ReportEntryNode ("mnuRepNomenclaturesObjects", typeof (ReportQueryLocations))),
                new ReportEntryNode ("mnuRepGoods", null,
                    new ReportEntryNode ("mnuRepGoogsPath", typeof (ReportQueryItemsFlow)),
                    new ReportEntryNode ("mnuRepGoodsQtty", typeof (ReportQueryItemsAvailability)),
                    new ReportEntryNode ("mnuRepGoodsQttyInDate", typeof (ReportQueryItemsAvailabilityAtDate)),
                    new ReportEntryNode ("mnuRepGoodsLessMinQtty", typeof (ReportQueryItemsMinimalQuantities)),
                    new ReportEntryNode ("mnuRepGoodsStockTaking", typeof (ReportQueryItemsStockTaking)),
                    new ReportEntryNode ("mnuRepGoodsInvoicedGoods", typeof (ReportQueryInvoicedItems)),
                    new ReportEntryNode ("mnuRepGoodsDelivery", typeof (ReportQueryPurchasesByItems)),
                    new ReportEntryNode ("mnuRepGoodsSale", typeof (ReportQuerySalesByItem)),
                    new ReportEntryNode ("mnuRepGoodsBestProfit", typeof (ReportQueryItemsByProfit))),
                new ReportEntryNode ("mnuRepPartners", null,
                    new ReportEntryNode ("mnuRepPartnersDeliveries", typeof (ReportQueryPurchasesByPartners)),
                    new ReportEntryNode ("mnuRepPartnersSales", typeof (ReportQuerySalesByPartners)),
                    new ReportEntryNode ("mnuRepPartnersByProfit", typeof (ReportQueryPartnersByProfit)),
                    new ReportEntryNode ("mnuRepPartnersDebt", typeof (ReportQueryPartnersDebt))),
                new ReportEntryNode ("mnuReportsObjectsSep1", null,
                    new ReportEntryNode ("mnuRepObjectsDeliveries", typeof (ReportQueryPurchasesByLocations)),
                    new ReportEntryNode ("mnuRepObjectsSales", typeof (ReportQuerySalesByLocations)),
                    new ReportEntryNode ("mnuRepObjectsByProfit", typeof (ReportQueryLocationsByProfit))),
                new ReportEntryNode ("mnuRepPayments", null,
                    new ReportEntryNode ("mnuRepPaymentsByDocuments", typeof (ReportQueryPaymentsByDocuments)),
                    new ReportEntryNode ("mnuRepPaymentsByPartners", typeof (ReportQueryPaymentsByPartners)),
                    new ReportEntryNode ("mnuRepPaymentsDueDates", typeof (ReportQueryPaymentsDueDates)),
                    new ReportEntryNode ("mnuRepPaymentsChronology", typeof (ReportQueryPaymentsHistory)),
                    new ReportEntryNode ("mnuRepPaymentsIncome", typeof (ReportQueryIncome)),
                    new ReportEntryNode ("mnuRepPaymentsAdvance", typeof (ReportQueryPaymentsAdvance))),
                new ReportEntryNode ("mnuRepDocuments", null,
                    new ReportEntryNode ("mnuRepDocumentsSalesBySum", typeof (ReportQuerySalesByTotal)),
                    new ReportEntryNode ("mnuRepDocumentsDeliveriesBySum", typeof (ReportQueryPurchasesByTotal)),
                    new ReportEntryNode ("mnuRepDocumentsRevisionsBySum", typeof (ReportQueryStockTakingsByTotal)),
                    new ReportEntryNode ("mnuRepDocumentsPublishedInvoices", typeof (ReportQueryInvoicesIssued)),
                    new ReportEntryNode ("mnuRepDocumentsReceivedInvoices", typeof (ReportQueryInvoicesReceived))),
                new ReportEntryNode ("mnuRepAdministration", null,
                    new ReportEntryNode ("mnuRepAdministrationAppLog", typeof (ReportQueryAppLogEntries))));

            ConfigurationHolder config = BusinessDomain.AppConfiguration;
            if (!string.IsNullOrWhiteSpace (config.LastReportType))
                lastReportType = Type.GetType (config.LastReportType);

            if (lastReportType == null)
                return;

            if (config.LastReportArgPresent)
                lastReportArgs = new object [] { config.LastReportArg };

            lastReportTitle = config.LastReportTitle;
        }

        #region User space initialization

        /// <summary>
        /// Checks if the current connection settings work.
        /// </summary>
        /// <returns></returns>
        private static bool TryInitDB ()
        {
            if (!interactiveConnect) {
                try {
                    return BusinessDomain.InitDataAccessProvider ();
                } catch {
                    return false;
                }
            }

            // If we are trying to connect through the dialog (at least a second try) elaborate.
            try {
                if (BusinessDomain.InitDataAccessProvider ())
                    return true;
                MessageError.ShowDialog (string.Format (Translator.GetString ("Selected database \"{0}\" is invalid."),
                    BusinessDomain.AppConfiguration.DbDatabase));
            } catch (DBConnectException) {
                MessageError.ShowDialog (Translator.GetString ("An error occurred while connecting to the server."));
            } catch (DBFormatException) {
                MessageError.ShowDialog (string.Format (Translator.GetString ("Selected database \"{0}\" is not a valid {1} database."),
                    BusinessDomain.AppConfiguration.DbDatabase, DataHelper.ProductName));
            } catch (DBVersionException ex) {
                if (!ex.Upgradeable) {
                    MessageError.ShowDialog (string.Format (
                        Translator.GetString (
                            "Current database \"{0}\" is version \"{1}\". This version of the software is compatible with database version \"{2}\" and not with the seleced database."),
                        BusinessDomain.AppConfiguration.DbDatabase,
                        ex.DatabaseVersion,
                        ex.ProviderVersion), ErrorSeverity.Information);
                    return false;
                }

                using (MessageOkCancel msgOkCancel = new MessageOkCancel (
                    Translator.GetString ("Incompatible database version!"),
                    "Icons.Database16.png",
                    string.Format (
                        Translator.GetString (
                            "Current database \"{0}\" is version \"{1}\". In order to use this database, it must be upgraded to version \"{2}\". It is possible other programs to stop working after the upgrade. Do you want to continue?"),
                        BusinessDomain.AppConfiguration.DbDatabase, ex.DatabaseVersion, ex.ProviderVersion),
                    "Icons.Database32.png")) {
                    if (msgOkCancel.Run () != ResponseType.Ok)
                        return false;
                }

                using (MessageProgress dlgProg = new MessageProgress (
                    Translator.GetString ("Database upgrade"),
                    "Icons.DBTest24.png",
                    string.Format (Translator.GetString ("Upgrading database \"{0}\"..."), BusinessDomain.AppConfiguration.DbDatabase))) {
                    dlgProg.Show ();

                    try {
                        BusinessDomain.UpgradeDatabase (ex.DatabaseVersion, dlgProg.ProgressCallback);
                    } catch (Exception upgrEx) {
                        MessageError.ShowDialog (Translator.GetString ("An error has occurred while performing upgrade. Cannot upgrade the current database!"),
                            ErrorSeverity.Error, upgrEx);
                        return false;
                    }

                    Thread.Sleep (1000);
                }

                MessageError.ShowDialog (Translator.GetString ("Database was successfully upgraded."),
                    ErrorSeverity.Information);

                try {
                    return BusinessDomain.InitDataAccessProvider ();
                } catch {
                    return false;
                }
            }
            return false;
        }

        private static bool TryUserLogin ()
        {
            // If we are already logged in then it's OK
            if (BusinessDomain.LoggedUser.IsSaved)
                return true;

            LazyListModel<User> users = User.GetAll ();

            // If we have only one user and it is the default one and it's password is blank then it's OK
            if (users != null && users.Count == 1) {
                User singleUser = users [0];

                if (singleUser.Id == User.DefaultId && singleUser.CheckPassword ("")) {
                    BusinessDomain.LoggedUser = singleUser;
                    return true;
                }
            }

            return false;
        }

        private static void CreateMainForm ()
        {
            if (MainFormCreated)
                return;

            mainForm = new FrmMain ();
            mainForm.Window.PushModal ();
        }

        private static void UpdateMainForm ()
        {
            RefreshMainFormRestrictions ();
            RefreshMainFormStatusBar ();
            mainForm.InitializeStartupPage ();

            MenuItemWrapper lastReport = mainForm.MainMenu.FindMenuItem ("mnuRepLastName");
            if (string.IsNullOrWhiteSpace (lastReportTitle)) {
                lastReport.Text = Translator.GetString ("None", "Report");
                lastReport.Sensitive = false;
            } else {
                lastReport.Text = lastReportTitle;
                lastReport.Sensitive = true;
            }
        }

        private static void CreateDBConnectDialog ()
        {
            BusinessDomain.LoggedUser = new User ();
            BusinessDomain.AppConfiguration.LastLoggedUID = -1;
            BusinessDomain.OnChangingDatabase ();

            using (EditDBConnection dbConnectDialog = new EditDBConnection ()) {
                OnShowingDialog ();
                if (dbConnectDialog.Run () != ResponseType.Ok)
                    throw new ApplicationAbortException ();

                dbConnectDialog.SetSettings ();
            }

            interactiveConnect = true;
        }

        private static void CreateUserLoginDialog ()
        {
            BusinessDomain.LoggedUser = new User ();
            RefreshMainFormStatusBar ();

            using (UserLogin userLoginDialog = new UserLogin ()) {
                OnShowingDialog ();
                if (userLoginDialog.Run () != ResponseType.Ok) {
                    if (BusinessDomain.AppConfiguration.AllowDbChange)
                        chooseDB = true;
                    else
                        AskQuit ();
                }
            }
        }

        #endregion

        #region Device events handling

        private static void DeviceManager_CardRecognized (object sender, CardReadArgs e)
        {
            Invoke (() =>
                {
                    if (CardRecognized != null)
                        CardRecognized (sender, e);
                }, false, true);
        }

        private static void DeviceManager_HardwareError (object sender, ErrorStateEventArgs e)
        {
            if (!Monitor.TryEnter (hwErrorSyncRoot))
                return;

            try {
                Invoke (() =>
                    {
                        HardwareErrorResponse res = FormHelper.HandleHardwareError (e.Exception);
                        e.Retry = res.Retry;
                    }, false, true);
            } finally {
                Monitor.Exit (hwErrorSyncRoot);
            }
        }

        private static void DeviceManagerHardwareResponseWaitPoll (object sender, EventArgs e)
        {
            if (!quitting)
                ProcessUIEvents ();
        }

        private static void DeviceManager_KitchenPrinterError (object sender, KitchenPrinterErrorEventArgs e)
        {
            MessageButtons mask = MessageButtons.All;

            if (BusinessDomain.AppConfiguration.CustomerOrdersPrinterEnabled) {
                DeviceManagerBase devMan = BusinessDomain.DeviceManager;
                if (!devMan.CustomerOrderPrinterConnected)
                    devMan.ConnectCustomerOrderPrinter ();

                ErrorState lastErr = devMan.CustomerOrderPrinter.LastErrorState;
                if (lastErr.Warnings.Count == 0) {
                    if (e.Exception.Error.Check (ErrorState.KitchenPrinterDisconnected) ||
                        e.Exception.Error.Check (ErrorState.KitchenPrinterNoPaper)) {
                        e.Exception.Error.SetError (ErrorState.KitchenPrinterError);
                    }
                }
            } else {
                mask &= ~MessageButtons.OK;
            }

            Invoke (() =>
                {
                    HardwareErrorResponse res = FormHelper.HandleHardwareError (e.Exception, mask);
                    e.Retry = (res.Button & MessageButtons.Retry) != MessageButtons.None;
                    e.TryCustomerOrderPrinter = (res.Button & MessageButtons.OK) != MessageButtons.None;
                }, true, true);

            e.Exception.Error.Unset (ErrorState.KitchenPrinterError);
        }

        private static MessageProgress receiptProgress;

        private static void DeviceManager_ReceiptPrintStart (object sender, ProgressStartEventArgs e)
        {
            Invoke (() =>
                {
                    receiptProgress = new MessageProgress (Translator.GetString ("Printing..."), null, e.Message);
                    receiptProgress.Show ();
                }, true);
        }

        private static void DeviceManager_ReceiptPrintStep (object sender, ProgressStepEventArgs e)
        {
            if (receiptProgress == null)
                return;

            Invoke (() =>
                {
                    receiptProgress.Progress = e.Percent;
                    receiptProgress.Show ();
                }, true);
        }

        private static void DeviceManager_ReceiptPrintDialogShown (object sender, EventArgs e)
        {
            if (receiptProgress == null)
                return;

            Invoke (() => receiptProgress.Hide (), true);
        }

        private static void DeviceManager_ReceiptPrintEnd (object sender, EventArgs e)
        {
            if (receiptProgress == null)
                return;

            Invoke (() =>
                {
                    receiptProgress.Dispose ();
                    receiptProgress = null;
                }, true);
        }

        #endregion

        #region Reports handling

        private static Type lastReportType;
        private static object lastReportArgs;
        private static string lastReportTitle;

        public static void CreateLastReport ()
        {
            if (lastReportType == null)
                return;

            CreateReport (null, lastReportType, lastReportArgs, lastReportTitle);
        }

        public static void CreateReport (object sender, Type reportType = null, object args = null, string title = null)
        {
            if (reportType == null || title == null) {
                MenuItem mItem = sender as MenuItem;
                if (mItem != null) {
                    title = ((Label) mItem.Child).Text;
                    if (reportType == null) {
                        ReportEntryNode repNode = ReportsTree.FindNode (mItem.Name);
                        if (repNode != null) {
                            reportType = repNode.Value;
                            args = repNode.Args;
                        }
                    }
                } else {
                    ToolButton tButton = sender as ToolButton;
                    if (tButton != null) {
                        title = tButton.Label;
                        if (!title.EndsWith ("..."))
                            title += "...";
                    }
                }
            }

            if (reportType == null)
                throw new ArgumentNullException ("reportType");

            if (!string.IsNullOrEmpty (title)) {
                mainForm.MainMenu.FindMenuItem ("mnuRepLastName").Text = title;
                mainForm.MainMenu.FindMenuItem ("mnuRepLastName").Sensitive = true;
                lastReportType = reportType;
                lastReportArgs = args;

                ConfigurationHolder config = BusinessDomain.AppConfiguration;
                config.LastReportTitle = title;
                config.LastReportType = reportType.AssemblyQualifiedName;
                config.LastReportArgPresent = args != null;
                if (args != null)
                    config.LastReportArg = (long) ((object []) args) [0];
            }

            object [] argsArray = args as object [];
            ReportQueryBase reportQuery = argsArray != null ?
                (ReportQueryBase) Activator.CreateInstance (reportType, argsArray) :
                (ReportQueryBase) Activator.CreateInstance (reportType);

            WbpReportResult form;
            using (ReportFilter dialog = new ReportFilter (reportQuery)) {
                dialog.SetDataQuery ();

                if (dialog.Run () != ResponseType.Ok)
                    return;

                DataQueryResult qSet = dialog.ExecuteReport (true);
                form = new WbpReportResult (qSet) { Title = dialog.Title, ReportTypeName = reportQuery.ReportTypeName };
            }
            mainForm.AddNewPage (form);
        }

        #endregion

        #region Common steps

        public static void Invoke (Action hnd, bool processUi = false, bool blocking = false)
        {
            if (Thread.CurrentThread.ManagedThreadId == uiThreadId)
                hnd ();
            else {
                bool exit = false;
                Timeout.Add (0, () =>
                    {
                        hnd ();
                        exit = true;
                        return false;
                    });

                while (!exit && blocking) {
                    if (processUi)
                        ProcessUIEvents ();
                    Thread.Sleep (100);
                }
            }

            if (processUi)
                ProcessUIEvents ();
        }

        public static void OnShowingDialog ()
        {
            if (ShowingDialog != null)
                ShowingDialog (null, null);
        }

        public static void Show ()
        {
            osIntegration.Init ();
            if (BusinessDomain.AppConfiguration.RegisterCashAtStartup &&
                BusinessDomain.DeviceManager.CashReceiptPrinterConnected &&
                BusinessDomain.DeviceManager.CashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.RegisterCash)) {
                OnShowingDialog ();
                FormHelper.TryReceiptPrinterCommand (delegate
                    {
                        using (FiscalRegisterCash dialog = new FiscalRegisterCash ()) {
                            dialog.Run ();
                        }
                    });
            }

            if (mainForm != null)
                mainForm.Show ();
        }

        public static void Run (ResourcesProviderBase resourcesProvider, string splashTextColor)
        {
            try {
                PreInit ();

                bool hasConfig = File.Exists (StoragePaths.ConfigFile);
                if (!hasConfig) {
                    SplashScreen.ShowSplash (resourcesProvider.GetType ().Assembly, splashTextColor);
                    SplashScreen.SetMessage ("Loading initial configuration...");
                }
                BusinessDomain.InitAppConfiguration ();
                ErrorHandlingGlib.HookErrors (HandleApplicationException);

                DataHelper.DefaultUIFont = Gtk.Settings.Default.FontName;
                Gtk.Settings.Default.FontName = DataHelper.DefaultUIFont;

                if (hasConfig)
                    SplashScreen.ShowSplash (resourcesProvider.GetType ().Assembly, splashTextColor);

                SplashScreen.SetMessage ("Loading translation...");
                SplashScreen.SetProgress (20);
                Translator.Init (new MOTranslationProvider ());

                SplashScreen.SetMessage (Translator.GetString ("Loading resources..."));
                SplashScreen.SetProgress (30);
                FormHelper.Init (resourcesProvider);

                if (!BusinessDomain.AppConfiguration.AllowMultipleInstances) {
                    string path = Path.Combine (Path.GetTempPath (), DataHelper.ProductName + "-il");
                    try {
                        instanceLock = File.Open (path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    } catch (Exception) {
                        SplashScreen.HideSplash ();
                        MessageError.ShowDialog (string.Format (Translator.GetString ("Another copy of {0} is already running!"), DataHelper.ProductName));
                        instanceLock = null;
                        throw new ApplicationAbortException ();
                    }
                }

                SplashScreen.SetMessage (Translator.GetString ("Loading addins..."));
                SplashScreen.SetProgress (40);
                BusinessDomain.InitAddins ();

                SplashScreen.SetMessage (Translator.GetString ("Connecting card readers..."));
                SplashScreen.SetProgress (65);
                BusinessDomain.InitBasicHardware ();

                SplashScreen.SetMessage (Translator.GetString ("Loading user interface..."));
                SplashScreen.SetProgress (70);
                ShowingDialog += PresentationDomain_ShowingDialog;
                Init (SplashScreen.SetMessage);

                SplashScreen.SetMessage (Translator.GetString ("Connecting periferial devices..."));
                SplashScreen.SetProgress (80);
                BusinessDomain.InitAllHardware ();

                SplashScreen.SetMessage (Translator.GetString ("Starting..."));
                SplashScreen.SetProgress (100);
                Show ();

                SplashScreen.HideSplash ();

                while (!quitting)
                    Application.Run ();

                if (restarting)
                    Restart ();
            } catch (ApplicationAbortException) {
                Quit ();
#if !DEBUG
            } catch (Exception ex) {
                HandleApplicationException (new UnhandledExceptionArgs (ex, true));
#endif
            }
        }

        private static void PresentationDomain_ShowingDialog (object sender, EventArgs e)
        {
            SplashScreen.HideSplash ();
        }

        private static void HandleApplicationException (UnhandledExceptionArgs args)
        {
            SplashScreen.HideSplash ();

            HandleApplicationException (args, (s, a) =>
                PlatformHelper.RunApplication (Path.Combine (StoragePaths.AppAddInsFolder, "Feedback.exe"),
                    string.Format ("-p \"{0}\" -pf \"{1}\" -c \"{2}\" -ec \"{3}\" -fs \"{4}\"",
                    DataHelper.ProductName,
                    DataHelper.ProductFullName,
                    DataHelper.CompanyName,
                    Path.GetFileName (StoragePaths.ConfigFile),
                    DataHelper.FeedbackServiceUrl)));
        }

        public static void ProcessUIEvents ()
        {
            while (Application.EventsPending ()) {
                Application.RunIteration (false);
            }
        }

        private static bool exitFlag;

        private static void HandleApplicationException (UnhandledExceptionArgs args, EventHandler feedbackHandler)
        {
            if (quitting) {
                args.ExitApplication = true;
                ErrorHandling.LogException ((Exception) args.ExceptionObject, ErrorSeverity.FatalError);
                return;
            }

            if (HandleDatabaseConnectionException ((Exception) args.ExceptionObject))
                return;

            quitting = true;
            try {
                using (MessageError dialog = new MessageError (
                    Translator.GetString ("The program must be stopped due to a fatal error. Sorry for the inconvenience."),
                    ErrorSeverity.FatalError, (Exception) args.ExceptionObject)) {
                    dialog.Response += delegate { exitFlag = true; };
                    dialog.Show ();

                    // Kill the application in 5 seconds
                    exitFlag = false;
                    new Timer (dialog_Timeout, dialog, 5000, System.Threading.Timeout.Infinite);

                    while (Application.EventsPending () || !exitFlag) {
                        Application.RunIteration (false);
                    }

                    if (feedbackHandler != null)
                        feedbackHandler (null, null);

                    args.ExitApplication = true;
                }
            } catch (Exception ex) {
                ErrorHandling.LogException (ex, ErrorSeverity.FatalError);
                ErrorHandling.LogException ((Exception) args.ExceptionObject, ErrorSeverity.FatalError);
            } finally {
                ErrorHandlingGlib.UnhookErrors ();
                ConfigurationHolder config = BusinessDomain.AppConfiguration;
                if (config != null)
                    config.Save (false);
            }
        }

        private static bool HandleDatabaseConnectionException (Exception ex)
        {
            if (!ex.HasException<DbConnectionLostException> ())
                return false;

            try {
                if (BusinessDomain.DataAccessProvider != null)
                    BusinessDomain.DataAccessProvider.DisableConnectionLostErrors = true;

                CloseAllDialogs ();

                MessageError.ShowDialog (Translator.GetString ("The connection with database was interrupted!"), ErrorSeverity.Error, ex);
                if (BusinessDomain.AppConfiguration.AllowDbChange)
                    ChangeDatabase ();
                else
                    AskQuit ();

                return true;
            } finally {
                if (BusinessDomain.DataAccessProvider != null)
                    BusinessDomain.DataAccessProvider.DisableConnectionLostErrors = false;
            }
        }

        private static void dialog_Timeout (object state)
        {
            MessageError dialog = (MessageError) state;

            dialog.Hide ();
            exitFlag = true;
        }

        public static void AskQuit ()
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Exit"),
                string.Empty,
                Translator.GetString ("Do you want to quit the application?"),
                "Icons.Exit32.png")) {
                if (dialog.Run () == ResponseType.Ok) {
                    Quit ();
                }
            }
        }

        public static void Quit (bool isUrgent = false)
        {
            Invoke (() =>
                {
                    User loggedUser = BusinessDomain.LoggedUser;
                    if (loggedUser.IsSaved && loggedUser.Id != User.DefaultId)
                        loggedUser.LogSuccessfulLogout ();

                    Application.Quit ();
                    quitting = true;
                    OnQuitting (isUrgent);
                    BusinessDomain.Deinitialize ();
                    if (instanceLock == null)
                        return;

                    instanceLock.Close ();
                    instanceLock = null;
                });
        }

        private static void OnQuitting (bool isUrgent)
        {
            EventHandler<QuittingEventArgs> handler = Quitting;
            if (handler != null)
                handler (Application.CurrentEvent, new QuittingEventArgs { IsUrgent = isUrgent });
        }

        public static void QueueRestart ()
        {
            Quit ();
            restarting = true;
        }

        private static void Restart ()
        {
            quitting = true;
            osIntegration.Restart ();
        }

        public static void ChangeDatabase ()
        {
            if (BusinessDomain.AppConfiguration.AllowDbChange) {
                chooseDB = true;
                try {
                    InitUserSpace ();
                } catch (ApplicationAbortException) {
                    Quit ();
                }
            } else {
                AskQuit ();
            }
        }

        public static void ChangeUser ()
        {
            chooseUser = true;
            BusinessDomain.LoggedUser.LogSuccessfulLogout ();

            try {
                InitUserSpace ();
            } catch (ApplicationAbortException) {
                Quit ();
            }
        }

        public static void RefreshMainFormRestrictions ()
        {
            if (MainFormCreated)
                mainForm.RefreshRestrictions (true);
        }

        public static void RefreshMainFormStatusBar ()
        {
            if (MainFormCreated)
                mainForm.RefreshStatusBar ();
        }

        public static void ShowSettings (Settings.Page startupPage = Settings.Page.Visual)
        {
            if (!CloseAllPages ())
                return;

            using (Settings dialog = new Settings (startupPage))
                dialog.Run ();
        }

        #endregion

        public static void ShowSettings (Type startupPage)
        {
            if (!CloseAllPages ())
                return;

            using (Settings dialog = new Settings (startupPage))
                dialog.Run ();
        }

        private static bool CloseAllPages ()
        {
            if (mainForm.WorkBook.PagesCount > 0) {
                if (Message.ShowDialog (
                    Translator.GetString ("Warning!"), null,
                    Translator.GetString (
                        "Before editing the settings all tabs have to be closed. Do you want to close all open tabs?"),
                    "Icons.Question32.png",
                    MessageButtons.YesNo) != ResponseType.Yes)
                    return false;

                if (!mainForm.CloseAllPages ())
                    return false;
            }
            return true;
        }

        private static bool retValue;

        public static void CloseAllDialogs ()
        {
            do {
                retValue = false;
                Invoke (() =>
                    {
                        if (ComponentHelper.CloseTopDialog ())
                            retValue = true;
                    }, true, true);
            } while (retValue);
        }

        public static void ForceCloseAllPages ()
        {
            Invoke (() =>
                {
                    if (mainForm != null)
                        mainForm.CloseAllPages (true);
                }, true, true);
        }

        public static bool CheckPurchasePricesDisabled ()
        {
            if (!BusinessDomain.LoggedUser.HideItemsPurchasePrice)
                return false;

            MessageError.ShowDialog (Translator.GetString ("This operation is not available because the purchase prices are hidden for the current user and they are mandatory for this operation. Please contact your administrator if you need to use this operation."));
            return true;
        }
    }
}
