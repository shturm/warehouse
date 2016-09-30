//
// BusinessDomain.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   06/22/2006
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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using Mono.Addins;
using Warehouse.Business.Entities;
using Warehouse.Business.Licensing;
using Warehouse.Business.Operations;
using Warehouse.Data;

namespace Warehouse.Business
{
    public class BusinessDomain
    {
        private static readonly DesktopBusinessBaseProvider baseProvider = new DesktopBusinessBaseProvider ();
        private static readonly List<ILicenseConsumer> licenseConsumers = new List<ILicenseConsumer> ();
        private static readonly Dictionary<string, object> licenseUsers = new Dictionary<string, object> ();
        private static List<IDataExchanger> dataExporters;
        private static List<IDataExchanger> dataImporters;
        private static List<IDataExchanger> documentExporters;
        private static List<ICardPaymentProcessor> cardPaymentProcessors;
        private static DeviceManagerBase deviceManager;
        private static QuickItemsCollection quickItems;
        private static IFeedbackProvider feedbackProvider;

        public static event EventHandler ChangingDatabase;

        public static event EventHandler<DatabaseConnectedArgs> DatabaseConnected;

        public static event EventHandler Deinitialized;

        #region Public properties

        public static List<IDataExchanger> DataExporters
        {
            get
            {
                if (dataExporters == null)
                    InitDataExchangers ();

                return dataExporters;
            }
        }

        public static List<IDataExchanger> DataImporters
        {
            get
            {
                if (dataImporters == null)
                    InitDataExchangers ();

                return dataImporters;
            }
        }

        public static IList<IDataExchanger> DocumentExporters
        {
            get
            {
                if (documentExporters == null) {
                    if (!AddinManager.IsInitialized)
                        return new List<IDataExchanger> ();

                    documentExporters = AddinManager.GetExtensionNodes ("/Warehouse/Component/DocumentExchange")
                        .OfType<TypeExtensionNode> ().Select (n => n.CreateInstance ())
                        .OfType<IDataExchanger> ().ToList ();
                }

                return documentExporters;
            }
        }

        public static List<ICardPaymentProcessor> CardPaymentProcessors
        {
            get
            {
                if (cardPaymentProcessors == null) {
                    if (!AddinManager.IsInitialized)
                        return new List<ICardPaymentProcessor> ();

                    cardPaymentProcessors = AddinManager.GetExtensionNodes ("/Warehouse/Business/CardPaymentAddon")
                        .OfType<TypeExtensionNode> ().Select (n => n.CreateInstance ())
                        .OfType<ICardPaymentAddon> ().Select (p => p.CreateProcessor ())
                        .Where (p => p != null).ToList ();
                }

                return cardPaymentProcessors;
            }
            set { cardPaymentProcessors = value; }
        }

        private static DataProviderDescription [] allDataAccessProviders;
        public static DataProviderDescription [] AllDataAccessProviders
        {
            get
            {
                return allDataAccessProviders ?? (allDataAccessProviders = new []
                    {
                        new DataProviderDescription
                            {
                                Name = Translator.GetString ("MySQL Server"),
                                ProviderType = typeof (Data.MySQL.DataProvider),
                                UsesServer = true,
                                UsesSlaveServer = true,
                                UsesUser = true,
                                UsesPassword = true,
                                UsesDatabase = true
                            },
                        new DataProviderDescription
                            {
                                Name = Translator.GetString ("SQLite Database"),
                                ProviderType = typeof (Data.SQLite.DataProvider),
                                UsesDatabase = true
                            }
                    });
            }
        }

        public static DataProvider DataAccessProvider
        {
            get { return baseProvider.DataAccessProvider; }
        }

        public static ConfigurationHolder AppConfiguration
        {
            get { return baseProvider.AppConfiguration; }
            set { baseProvider.AppConfiguration = value; }
        }

        public static DeviceManagerBase DeviceManager
        {
            get
            {
                if (deviceManager == null) {
                    deviceManager = new DeviceManager ();
                    deviceManager.CashReceiptPrinterChanged += DeviceManagerCashReceiptPrinterChanged;
                }

                return deviceManager;
            }
            private set
            {
                if (deviceManager != null)
                    deviceManager.CashReceiptPrinterChanged -= DeviceManagerCashReceiptPrinterChanged;
                deviceManager = value;
                if (deviceManager != null)
                    deviceManager.CashReceiptPrinterChanged += DeviceManagerCashReceiptPrinterChanged;
            }
        }

        public static IFeedbackProvider FeedbackProvider
        {
            get
            {
                if (feedbackProvider == null)
                    InitFeedback ();

                return feedbackProvider;
            }
            set { feedbackProvider = value; }
        }

        public static WorkflowManagerBase WorkflowManager
        {
            get { return baseProvider.WorkflowManager; }
            set { baseProvider.WorkflowManager = value; }
        }

        public static QuickItemsCollection QuickItems
        {
            get { return quickItems; }
        }

        public static Version ApplicationVersion
        {
            get { return baseProvider.ApplicationVersion; }
        }

        public static string ApplicationVersionString
        {
            get { return baseProvider.ApplicationVersionString; }
        }

        public static DateTime ApplicationVersionDate
        {
            get { return baseProvider.ApplicationVersionDate; }
        }

        public static string DataProviderVersion
        {
            get { return baseProvider.DataProviderVersion; }
        }

        public static User LoggedUser
        {
            get { return baseProvider.LoggedUser; }
            set { baseProvider.LoggedUser = value; }
        }

        public static bool HideVATColumns
        {
            get { return baseProvider.HideVATColumns; }
        }

        public static CompanyRecord CurrentCompany
        {
            get { return baseProvider.CurrentCompany; }
            set { baseProvider.CurrentCompany = value; }
        }

        public static RestrictionNode RestrictionTree
        {
            get { return baseProvider.RestrictionTree; }
            set { baseProvider.RestrictionTree = value; }
        }

        public static IList<ILicenseConsumer> AllLicenseConsumers
        {
            get
            {
                foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Business/LicenseConsumer")) {
                    if (licenseConsumers.Any (t => t.GetType () == node.Type))
                        continue;

                    ILicenseConsumer consumer = node.CreateInstance () as ILicenseConsumer;
                    if (consumer != null)
                        licenseConsumers.Add (consumer);
                }

                return licenseConsumers;
            }
        }

        public static Dictionary<string, object> LicenseUsers
        {
            get { return licenseUsers; }
        }

        public static SerializableDictionary<string, DataQuery> ReportQueryStates
        {
            get { return baseProvider.ReportQueryStates; }
        }

        public static SerializableDictionary<string, DataQuery> DocumentQueryStates
        {
            get { return baseProvider.DocumentQueryStates; }
        }

        #endregion

        public static void InvalidateHideVATColumns ()
        {
            baseProvider.InvalidateHideVATColumns ();
        }

        public static void InitAppConfiguration ()
        {
            bool retry;
            do {
                retry = false;
                try {
                    if (baseProvider.AppConfiguration != null)
                        throw new Exception ("Application configuration is already initialized!");

                    baseProvider.AppConfiguration = new ExeConfigurationHolder ();
                    baseProvider.AppConfiguration.Load ();
                } catch (ConfigurationErrorsException) {
                    // In some rare cases while saving the configuration the XML may get corrupted
                    // In this case it is better to delete the whole file and recreate it
                    string configFileName = StoragePaths.ConfigFile;
                    if (File.Exists (configFileName)) {
                        File.Delete (configFileName);
                        retry = true;
                    }
                } catch (Exception ex) {
                    throw new Exception ("Error occurred while initializing configuration", ex);
                }
            } while (retry);

            quickItems = new QuickItemsCollection ();
        }

        public static bool InitDataAccessProvider ()
        {
            return baseProvider.InitDataAccessProvider ();
        }

        public static void InitAddins ()
        {
            AddinManager.AddinLoadError += AddinManager_AddinLoadError;
            bool retry = false;
            bool errorDeletingFolder = false;
            AutoResetEvent initialized = new AutoResetEvent (false);
            do {
                Thread thread = new Thread (() =>
                    {
                        try {
                            AddinManager.Initialize ();
                            AddinManager.Registry.Update (null);

                            foreach (TypeExtensionNode addonOperation in AddinManager.GetExtensionNodes ("/Warehouse/Business/Operations")) {
                                IAdditionalOperation instance = (IAdditionalOperation) addonOperation.CreateInstance ();
                                Operation.GetByIdHandlers [instance.Type] = instance.GetByOperationId;
                                Operation.GetPendingHandlers [instance.Type] = instance.GetPendingOperation;
                            }

                            foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Business/Config")) {
                                IConfigurationAddin addin = (IConfigurationAddin) node.CreateInstance ();
                                AppConfiguration.Addins [addin.GetType ()] = addin;
                            }

                            retry = false;
                            initialized.Set ();
                        } catch (Exception ex) {
                            try {
                                AddinManager.Shutdown ();
                            } catch { }

                            if (Directory.Exists (StoragePaths.MonoAddinsDataFolder) && !errorDeletingFolder) {
                                try {
                                    Directory.Delete (StoragePaths.MonoAddinsDataFolder, true);
                                } catch (Exception) {
                                    errorDeletingFolder = true;
                                } finally {
                                    retry = true;
                                }
                            } else
                                ErrorHandling.LogException (ex);
                        }
                    });
                thread.Start ();

                if (!initialized.WaitOne (10000)) {
                    thread.Abort ();
                    thread.Join ();
                }
            } while (retry);

            if (AppConfiguration.Addins.Count > 0)
                AppConfiguration.Load (true);
        }

        private static void InitDataExchangers ()
        {
            dataExporters = new List<IDataExchanger> ();
            dataImporters = new List<IDataExchanger> ();

            foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Business/Exchange")) {
                object instance = node.CreateInstance ();
                IDataExporter exporter = instance as IDataExporter;
                if (exporter != null)
                    dataExporters.Add (exporter);

                IDataImporter importer = instance as IDataImporter;
                if (importer != null)
                    dataImporters.Add (importer);
            }
        }

        private static readonly object feedbackProviderSync = new object ();
        private static void InitFeedback ()
        {
            lock (feedbackProviderSync) {
                foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Business/Feedback")) {
                    feedbackProvider = node.CreateInstance () as IFeedbackProvider;
                    if (feedbackProvider != null)
                        return;
                }

                feedbackProvider = new DummyFeedbackProvider ();
            }
        }

        private static void AddinManager_AddinLoadError (object sender, AddinErrorEventArgs args)
        {
            ErrorHandling.LogException (args.Exception);
        }

        public static void InitBasicHardware ()
        {
            DeviceManager.InitBasicHardware ();
        }

        public static void InitAllHardware ()
        {
            DeviceManager.InitAllHardware ();
        }

        public static void ReinitializeHardware (Device oldItem, Device newItem,
            Action onCashReceiptPrinterError,
            Action onCustomerOrderPrinterError,
            Action onDisplayError,
            Action onCardReaderError,
            Action onScaleError,
            Action onSDCError,
            Action onKitchenPrinterError,
            Action onBarcodeScannerError)
        {
            DeviceManager.ReinitializeHardware (oldItem, newItem,
                onCashReceiptPrinterError, onCustomerOrderPrinterError, onDisplayError,
                onCardReaderError, onScaleError, onSDCError, onKitchenPrinterError, onBarcodeScannerError);
        }

        public static void OnChangingDatabase ()
        {
            EventHandler handler = ChangingDatabase;
            if (handler != null)
                handler (null, EventArgs.Empty);
        }

        public static bool OnDatabaseConnected ()
        {
            EventHandler<DatabaseConnectedArgs> handler = DatabaseConnected;
            if (handler == null)
                return true;

            DatabaseConnectedArgs args = new DatabaseConnectedArgs ();
            handler (null, args);
            return !args.Cancelled;
        }

        private static void OnDeinitialized ()
        {
            EventHandler handler = Deinitialized;
            if (handler != null)
                handler (null, EventArgs.Empty);
        }

        public static void Deinitialize ()
        {
            OnDeinitialized ();
            if (feedbackProvider != null) {
                feedbackProvider.Dispose ();
                feedbackProvider = null;
            }

            DataHelper.Dispose ();

            try {
                DeviceManager.DisconnectKitchenPrinters ();

                if (DeviceManager.CardReaderConnected)
                    DeviceManager.DisconnectCardReader ();

                if (DeviceManager.ExternalDisplayConnected)
                    DeviceManager.DisconnectExternalDisplay ();

                if (DeviceManager.CustomerOrderPrinterConnected)
                    DeviceManager.DisconnectCustomerOrderPrinter ();

                if (DeviceManager.CashReceiptPrinterConnected)
                    DeviceManager.DisconnectCashReceiptPrinter ();

                if (DeviceManager.ElectronicScaleConnected)
                    DeviceManager.DisconnectElectronicScale ();

                if (DeviceManager.SalesDataControllerConnected)
                    DeviceManager.DisconnectSalesDataController ();

                if (DeviceManager.BarcodeScannerConnected)
                    DeviceManager.DisconnectBarcodeScanner ();
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
            }

            DeviceManager.Dispose ();
            DeviceManager = null;
            Device.Flush ();
            WindowSettings.Flush ();
            baseProvider.Deinitialize ();
        }

        public static Dictionary<string, byte []> GetLicenseData ()
        {
            Dictionary<string, byte []> ret = new Dictionary<string, byte []> ();

            if (Directory.Exists (StoragePaths.LicenseFolder)) {
                try {
                    foreach (string file in Directory.GetFiles (StoragePaths.LicenseFolder, "*.lic")) {
                        ret.Add (file, File.ReadAllBytes (file));
                    }

                    // This is for backwards compatiblity
                    foreach (string file in Directory.GetFiles (StoragePaths.AppFolder, "*.lic")) {
                        ret.Add (file, File.ReadAllBytes (file));
                    }
                } catch (Exception) {
                }
            }

            return ret;
        }

        public static object GetObjectValue (Type fieldType, string text)
        {
            return baseProvider.GetObjectValue (fieldType, text);
        }

        #region Data access management

        public static bool CanUseLocationInOperation (OperationType operationType, long locationId, out string error, out string warning)
        {
            return baseProvider.CanUseLocationInOperation (operationType, locationId, out error, out warning);
        }

        public static bool WaitForPendingOperationToComplete (Operation operation, int msecs)
        {
            return baseProvider.WaitForPendingOperationToComplete (operation, msecs);
        }

        public static bool CanEditOperation (object operation, out string message, out bool readOnlyView)
        {
            return baseProvider.CanEditOperation (operation, out message, out readOnlyView);
        }

        public static bool CanEditOperation (OperationType type, long operationId, out string message, out bool readOnlyView)
        {
            return baseProvider.CanEditOperation (type, operationId, out message, out readOnlyView);
        }

        public static bool CanAnnulOperation (object operation, out string error, out string warning)
        {
            return baseProvider.CanAnnulOperation (operation, out error, out warning);
        }

        public static void RequestOperationAnnul (object operation)
        {
            baseProvider.RequestOperationAnnul (operation);
        }

        public static bool CanEditPayment (Payment payment, out string message)
        {
            return baseProvider.CanEditPayment (payment, out message);
        }

        public static bool CanIncreaseStoreAvailabilityOnTransfer (object transfer)
        {
            return baseProvider.CanIncreaseStoreAvailabilityOnTransfer (transfer);
        }

        public static bool CanIncreaseStoreAvailabilityOnTransferAnnull (object transfer)
        {
            return baseProvider.CanIncreaseStoreAvailabilityOnTransferAnnull (transfer);
        }

        public static DateTime GetDatabaseLastUpdate ()
        {
            return baseProvider.GetDatabaseLastUpdate ();
        }

        #endregion

        public static bool OnPaymentProcessed (Payment payment)
        {
            bool success = true;
            foreach (ICardPaymentProcessor processor in CardPaymentProcessors) {
                PaymentProcessorResult result = processor.ProcessPayment (payment);
                if (result == PaymentProcessorResult.Succeeded)
                    return true;

                if (result == PaymentProcessorResult.Failed)
                    success = false;
            }

            return success;
        }

        public static void OnPaymentCommited (Payment payment)
        {
            bool processingFailed = false;
            foreach (ICardPaymentProcessor processor in CardPaymentProcessors) {
                PaymentProcessorResult result = processor.PaymentCommited (payment);
                if (result == PaymentProcessorResult.Succeeded)
                    return;

                if (result == PaymentProcessorResult.Failed)
                    processingFailed = true;
            }

            if (processingFailed)
                throw new ApplicationException ("Payment completion failed!");
        }

        public static void OnPaymentDeleting (Payment payment)
        {
            bool processingFailed = false;
            foreach (ICardPaymentProcessor processor in CardPaymentProcessors) {
                PaymentProcessorResult result = processor.PaymentDeleting (payment);
                if (result == PaymentProcessorResult.Succeeded) {
                    processingFailed = false;
                    break;
                }

                if (result == PaymentProcessorResult.Failed)
                    processingFailed = true;
            }

            if (processingFailed)
                throw new ApplicationException ("Payment deletion failed!");
        }

        public static event EventHandler<PriceRuleMessageEventArgs> PriceRuleMessageReceived;
        public static void OnPriceRuleMessage (PriceRuleMessageEventArgs e)
        {
            EventHandler<PriceRuleMessageEventArgs> handler = PriceRuleMessageReceived;
            if (handler != null)
                handler (null, e);
        }

        public static event EventHandler<PriceRuleAskAdvanceEventArgs> PriceRuleAskedForAdvance;
        public static void OnPriceRulePriceRuleAskForAdvance (PriceRuleAskAdvanceEventArgs e)
        {
            EventHandler<PriceRuleAskAdvanceEventArgs> handler = PriceRuleAskedForAdvance;
            if (handler != null)
                handler (null, e);
        }

        #region Database management

        public static bool TryConnect (string provider, string server, string slaveServer, string user, string password, string database = "")
        {
            return baseProvider.TryConnect (provider, server, slaveServer, user, password, database);
        }

        public static void UpdatePurchaseCurrencyPrecision ()
        {
            baseProvider.UpdatePurchaseCurrencyPrecision ();
        }

        public static string [] GetDatabases ()
        {
            return baseProvider.GetDatabases ();
        }

        public static bool SetCurrentDatabase (string database)
        {
            return baseProvider.SetCurrentDatabase (database);
        }

        public static void CheckDatabaseVersion ()
        {
            baseProvider.CheckDatabaseVersion ();
        }

        public static void UpgradeDatabase (string sourceVersion, Action<double> callback)
        {
            baseProvider.UpgradeDatabase (sourceVersion, callback);
        }

        public static void CreateDatabase (string database, CreateDatabaseType type, Action<double> callback)
        {
            baseProvider.CreateDatabase (database, type, callback);

            foreach (IDBCustomizer node in AddinManager.GetExtensionObjects ("/Warehouse/Business/DBCustomizer"))
                node.DbCreated (database, type);
        }

        public static bool IsValidDatabaseName (string dbName)
        {
            return baseProvider.IsValidDatabaseName (dbName);
        }

        #endregion

        #region Date management

        /// <summary>
        /// Gets the current date and time on the server.
        /// </summary>
        /// <returns>The current date and time on the server.</returns>
        public static DateTime Now
        {
            get { return baseProvider.Now; }
        }

        /// <summary>
        /// Gets the current date on the server.
        /// </summary>
        public static DateTime Today
        {
            get { return baseProvider.Today; }
        }

        public static DateTime GetDateValue (string value)
        {
            return baseProvider.GetDateValue (value);
        }

        public static DateTime GetDateTimeValue (string value)
        {
            return baseProvider.GetDateTimeValue (value);
        }

        public static TimeSpan GetTimeValue (string value)
        {
            return baseProvider.GetTimeValue (value);
        }

        public static DateTime GetDateValue (string value, string format)
        {
            return baseProvider.GetDateValue (value, format);
        }

        public static string GetFormattedDate (DateTime value)
        {
            return baseProvider.GetFormattedDate (value);
        }

        public static string GetFormattedDateTime (DateTime value)
        {
            return baseProvider.GetFormattedDateTime (value);
        }

        public static string GetFormattedTime (DateTime value)
        {
            return baseProvider.GetFormattedTime (value);
        }

        public static string GetFormattedTime (TimeSpan value)
        {
            return baseProvider.GetFormattedTime (value);
        }

        public static string GetFormattedDate (DateTime value, string format)
        {
            return baseProvider.GetFormattedDate (value, format);
        }

        #endregion

        #region Licenses management

        public static T GetLicenseConsumer<T> () where T : class, ILicenseConsumer
        {
            foreach (ILicenseConsumer cons in licenseConsumers) {
                T ret = cons as T;
                if (ret != null)
                    return ret;
            }

            T lic = null;
            try {
                lic = (T) Activator.CreateInstance (typeof (T));
            } catch (Exception) {
            }

            if (lic != null)
                licenseConsumers.Add (lic);

            return lic;
        }

        public static void RegisterLicenseConsumer (ILicenseConsumer consumer)
        {
            if (licenseConsumers.Any (cons => cons.GetType () == consumer.GetType ()))
                return;

            licenseConsumers.Add (consumer);
        }

        public static void UnregisterLicenseConsumer (ILicenseConsumer consumer)
        {
            foreach (ILicenseConsumer cons in licenseConsumers.Where (cons => cons.GetType () == consumer.GetType ())) {
                licenseConsumers.Remove (cons);
                return;
            }
        }

        public static void ReinitLicenseConsumers ()
        {
            foreach (ILicenseConsumer consumer in licenseConsumers) {
                consumer.Reinit ();
            }
        }

        private static void DeviceManagerCashReceiptPrinterChanged (object sender, EventArgs e)
        {
            foreach (ILicenseConsumer consumer in licenseConsumers) {
                if (!consumer.CareAboutCashReceiptPrinter)
                    continue;

                consumer.Reinit ();
            }
        }

        #endregion
    }
}
