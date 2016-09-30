//
// BusinessBaseProvider.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   09.20.2011
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
using System.Reflection;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Data;

namespace Warehouse.Business
{
    internal class BusinessBaseProvider<T> where T : ConfigurationHolderBase
    {
        private DataProvider dataAccessProvider;
        private T appConfiguration;
        private WorkflowManagerBase workflowManager;
        private User loggedUser;
        private CompanyRecord currentCompany;
        private TimeSpan? differenceWithServer;
        private DateTime? lastServerTimeRefresh;
        private bool? hideVatColumns;
        private SerializableDictionary<string, DataQuery> reportQueryStates;
        private SerializableDictionary<string, DataQuery> documentQueryStates;

        public DataProvider DataAccessProvider
        {
            get { return dataAccessProvider; }
        }

        public string DataProviderVersion
        {
            get { return dataAccessProvider.ProviderVersion; }
        }

        public T AppConfiguration
        {
            get { return appConfiguration; }
            set { appConfiguration = value; }
        }

        public WorkflowManagerBase WorkflowManager
        {
            get { return workflowManager ?? (workflowManager = new WorkflowManagerBase ()); }
            set { workflowManager = value; }
        }

        public bool HideVATColumns
        {
            get
            {
                if (hideVatColumns == null) {
                    bool usesVat = false;
                    foreach (VATGroup vatGroup in VATGroup.GetAll ()) {
                        if (vatGroup.VatValue <= 0)
                            continue;

                        usesVat = true;
                        break;
                    }

                    hideVatColumns = !usesVat;
                }

                return hideVatColumns.Value;
            }
        }

        public CompanyRecord CurrentCompany
        {
            get { return currentCompany; }
            set { currentCompany = value; }
        }

        public RestrictionNode RestrictionTree { get; set; }

        public SerializableDictionary<string, DataQuery> ReportQueryStates
        {
            get
            {
                if (reportQueryStates == null) {
                    if (dataAccessProvider == null)
                        reportQueryStates = new SerializableDictionary<string, DataQuery> ();
                    else
                        reportQueryStates = dataAccessProvider.GetReportsQueryState<SerializableDictionary<string, DataQuery>> () ?? new SerializableDictionary<string, DataQuery> ();
                }

                return reportQueryStates;
            }
        }

        public SerializableDictionary<string, DataQuery> DocumentQueryStates
        {
            get
            {
                if (documentQueryStates == null) {
                    if (dataAccessProvider == null)
                        documentQueryStates = new SerializableDictionary<string, DataQuery> ();
                    else
                        documentQueryStates = dataAccessProvider.GetDocumentQueryStates<SerializableDictionary<string, DataQuery>> () ?? new SerializableDictionary<string, DataQuery> ();
                }

                return documentQueryStates;
            }
        }

        public User LoggedUser
        {
            get { return loggedUser ?? new User (); }
            set
            {
                if (loggedUser == value)
                    return;

                loggedUser = value;
                if (value == null || !value.IsSaved)
                    return;

                loggedUser.LoadPreferences ();
                currentCompany = CompanyRecord.GetDefault ();
            }
        }

        public Version ApplicationVersion
        {
            get
            {
                Assembly exeAssembly = Assembly.GetEntryAssembly () ?? Assembly.GetExecutingAssembly ();
                AssemblyName exeName = exeAssembly.GetName ();

                return exeName.Version;
            }
        }

        public string ApplicationVersionString
        {
            get
            {
                return ApplicationVersion.ToString (4);
            }
        }

        public DateTime ApplicationVersionDate
        {
            get
            {
                Version ver = ApplicationVersion;
                return new DateTime (2000 + ver.Minor, ver.Build, ver.Revision);
            }
        }

        protected IList<IDataExtender> dataAccessExtenders;
        public virtual IList<IDataExtender> DataAccessExtenders
        {
            get { return dataAccessExtenders; }
            set { dataAccessExtenders = value; }
        }

        public bool InitDataAccessProvider ()
        {
            foreach (IDataExtender accessExtender in DataAccessExtenders)
                accessExtender.Deinitialize ();

            T config = AppConfiguration;

            if (!TryConnect (config.DbProvider,
                config.DbServer,
                config.DbSlaveServer,
                config.DbUser,
                config.DbPassword,
                config.DbDatabase))
                throw new DBConnectException ();

            if (!SetCurrentDatabase (config.DbDatabase))
                return false;

            CheckDatabaseVersion ();

            foreach (IDataExtender accessExtender in DataAccessExtenders)
                accessExtender.Initialize (DataAccessProvider);

            return true;
        }

        public void Deinitialize ()
        {
            if (appConfiguration != null) {
                try {
                    if (dataAccessProvider != null) {
                        appConfiguration.DbProvider = dataAccessProvider.GetType ().FullName;
                        appConfiguration.DbServer = dataAccessProvider.Server;
                        appConfiguration.DbUser = dataAccessProvider.User;
                        appConfiguration.DbPassword = dataAccessProvider.Password;
                        appConfiguration.DbDatabase = dataAccessProvider.Database;
                    }

                    appConfiguration.Save (false);
                } catch (Exception ex) {
                    ErrorHandling.LogException (ex);
                } finally {
                    AppConfiguration = null;
                }
            }

            if (dataAccessProvider != null) {
                try {
                    if (reportQueryStates != null)
                        dataAccessProvider.SaveReportsQueryState (reportQueryStates);
                    if (documentQueryStates != null)
                        dataAccessProvider.SaveDocumentQueryStates (documentQueryStates);
                    dataAccessProvider.Disconnect ();
                } catch (Exception ex) {
                    ErrorHandling.LogException (ex);
                } finally {
                    dataAccessProvider = null;
                }
            }
        }

        #region Database management

        public bool TryConnect (string provider, string server, string slaveServer, string user, string password, string database)
        {
            if (dataAccessProvider != null) {
                if (dataAccessProvider.GetType ().FullName == provider) {
                    dataAccessProvider.Server = server;
                    dataAccessProvider.SlaveServer = slaveServer;
                    dataAccessProvider.User = user;
                    dataAccessProvider.Password = password;
                    dataAccessProvider.Database = database;
                } else {
                    dataAccessProvider.Disconnect ();
                    dataAccessProvider = null;
                }
            }

            if (dataAccessProvider == null) {
                dataAccessProvider = DataProvider.CreateProvider (provider, Translator.GetHelper (), server, slaveServer, user, password);
                dataAccessProvider.ConnectTimeout = AppConfiguration.DbConnectTimeout;
                dataAccessProvider.CommandTimeout = AppConfiguration.DbCommandTimeout;
                dataAccessProvider.LogFile = appConfiguration.DbLogFile;
                UpdatePurchaseCurrencyPrecision ();
            }

            try {
                return dataAccessProvider.TryConnect ();
            } catch (Exception ex) {
                dataAccessProvider = null;
                ErrorHandling.LogException (ex);
                return false;
            }
        }

        public void UpdatePurchaseCurrencyPrecision ()
        {
            dataAccessProvider.PurchaseCurrencyPrecision = appConfiguration.PurchaseCurrencyPrecision;
        }

        public string [] GetDatabases ()
        {
            if (dataAccessProvider == null)
                throw new DataProviderNotInitializedException ();

            try {
                return dataAccessProvider.GetDatabases ();
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
            }

            return new string [0];
        }

        public bool SetCurrentDatabase (string database)
        {
            if (dataAccessProvider == null)
                throw new DataProviderNotInitializedException ();

            if (string.IsNullOrEmpty (database))
                return false;

            try {
                if (dataAccessProvider.CheckDatabaseExists (database)) {
                    dataAccessProvider.Database = database;
                    return true;
                }
                return false;
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                dataAccessProvider.Database = string.Empty;
                return false;
            }
        }

        public string GetCurrentDatabase ()
        {
            if (dataAccessProvider == null)
                throw new DataProviderNotInitializedException ();

            return dataAccessProvider.Database;
        }

        public void CheckDatabaseVersion ()
        {
            string dbVer;

            if (dataAccessProvider == null)
                throw new DataProviderNotInitializedException ();

            try {
                dbVer = dataAccessProvider.GetSchemaVersion ();
                if (dataAccessProvider.CheckCompatible (dbVer))
                    return;

                if (!AppConfiguration.CheckDbVersion)
                    return;
            } catch (Exception ex1) {
                throw new DBFormatException (ex1);
            }

            throw new DBVersionException (dataAccessProvider.ProviderVersion, dbVer)
                {
                    Upgradeable = dataAccessProvider.CheckUpgradeable (dbVer)
                };
        }

        public void UpgradeDatabase (string sourceVersion, Action<double> callback)
        {
            if (dataAccessProvider == null)
                throw new DataProviderNotInitializedException ();

            dataAccessProvider.UpgradeDatabase (sourceVersion, callback);
        }

        public void CreateDatabase (string database, CreateDatabaseType type, Action<double> callback)
        {
            if (dataAccessProvider == null)
                throw new DataProviderNotInitializedException ();

            dataAccessProvider.CreateDatabase (database, type, callback);
            dataAccessProvider.Database = database;
            appConfiguration.DbDatabase = database;

            WorkflowManager.OnDataBaseCreated (database, type);
        }

        public bool IsValidDatabaseName (string dbName)
        {
            if (dataAccessProvider == null)
                throw new DataProviderNotInitializedException ();

            return DataProvider.IsValidDatabaseName (dbName);
        }

        #endregion

        #region Date management

        private TimeSpan DifferenceWithServer
        {
            get
            {
                if (dataAccessProvider == null)
                    return new TimeSpan ();

                if (differenceWithServer == null ||
                    lastServerTimeRefresh == null ||
                    lastServerTimeRefresh.Value.AddHours (4) < DateTime.Now) {
                    DateTime serverNow = DataAccessProvider.Now ();
                    if (serverNow == DateTime.MinValue)
                        return new TimeSpan ();

                    differenceWithServer = serverNow - DateTime.Now;
                    lastServerTimeRefresh = DateTime.Now;
                }

                return differenceWithServer.Value;
            }
        }

        /// <summary>
        /// Gets the current date and time on the server.
        /// </summary>
        /// <returns>The current date and time on the server.</returns>
        public DateTime Now
        {
            get { return DateTime.Now + DifferenceWithServer; }
        }

        /// <summary>
        /// Gets the current date on the server.
        /// </summary>
        public DateTime Today
        {
            get { return Now.Date; }
        }

        public DateTime GetDateValue (string value)
        {
            string format = appConfiguration.DateFormat;

            DateTime ret = DataHelper.GetDateValue (value, format);
            if (ret == DateTime.MinValue)
                DateTime.TryParse (value, out ret);

            return ret;
        }

        public DateTime GetDateTimeValue (string value)
        {
            string format = appConfiguration.DateFormat;

            DateTime ret = DataHelper.GetDateValue (value, format + " HH:mm");
            return ret != DateTime.MinValue ? ret : DataHelper.GetDateValue (value, format);
        }

        public TimeSpan GetTimeValue (string value)
        {
            string format = appConfiguration.DateFormat;

            DateTime ret = DataHelper.GetDateValue (value, format + " HH:mm");
            if (ret == DateTime.MinValue)
                ret = DataHelper.GetDateValue (value, "HH:mm");

            return new TimeSpan (ret.Hour, ret.Minute, 0);
        }

        public DateTime GetDateValue (string value, string format)
        {
            return DataHelper.GetDateValue (value, format);
        }

        public string GetFormattedDate (DateTime value)
        {
            string format = appConfiguration.DateFormat;

            return DataHelper.GetFormattedDate (value, format);
        }

        public string GetFormattedDateTime (DateTime value)
        {
            string format = appConfiguration.DateFormat;

            return string.IsNullOrEmpty (format) ?
                DataHelper.GetFormattedDate (value, format) + DataHelper.GetFormattedDate (value, " HH:mm") :
                DataHelper.GetFormattedDate (value, format + " HH:mm");
        }

        public string GetFormattedTime (DateTime value)
        {
            return value.ToString ("HH:mm");
        }

        public string GetFormattedTime (TimeSpan value)
        {
            return string.Format ("{0}:{1}", value.Hours, value.Minutes);
        }

        public string GetFormattedDate (DateTime value, string format)
        {
            return DataHelper.GetFormattedDate (value, format);
        }

        #endregion

        public void InvalidateHideVATColumns ()
        {
            hideVatColumns = null;
        }

        public object GetObjectValue (Type fieldType, string text)
        {
            switch (fieldType.FullName) {
                case "System.Int16":
                    Int16 i16Val;
                    if (Int16.TryParse (text, out i16Val))
                        return i16Val;
                    break;
                case "System.Int32":
                    Int32 i32Val;
                    if (Int32.TryParse (text, out i32Val))
                        return i32Val;
                    break;
                case "System.Int64":
                    Int64 i64Val;
                    if (Int64.TryParse (text, out i64Val))
                        return i64Val;
                    break;
                case "System.Single":
                    Double dblVal;
                    if (Number.TryParseExpression (text, out dblVal))
                        return (Single) dblVal;
                    break;
                case "System.Double":
                    if (Number.TryParseExpression (text, out dblVal))
                        return dblVal;
                    break;
                case "System.Decimal":
                    Decimal decVal;
                    if (Decimal.TryParse (text, out decVal))
                        return decVal;
                    break;
                case "System.DateTime":
                    DateTime date = GetDateValue (text);
                    if (date != DateTime.MinValue)
                        return date;
                    break;
                case "System.String":
                    return text;
                case "System.Char":
                    Char chrVal;
                    if (Char.TryParse (text, out chrVal))
                        return chrVal;
                    break;
                case "System.Boolean":
                    Boolean bolVal;
                    if (Boolean.TryParse (text, out bolVal))
                        return bolVal;
                    break;
                default:
                    // Enumerations default to the first entry
                    if (fieldType.BaseType == typeof (Enum)) {
                        try {
                            return Enum.Parse (fieldType, text, true);
                        } catch {
                            Array objEnumValues = Enum.GetValues (fieldType);
                            Array.Sort (objEnumValues);
                            return Enum.ToObject (fieldType, objEnumValues.GetValue (0));
                        }
                    }

                    if (fieldType.IsGenericType &&
                        fieldType.FullName.StartsWith ("System.Nullable`1[[")) {
                        Type [] args = fieldType.GetGenericArguments ();
                        return GetObjectValue (args [0], text);
                    }

                    return null;
            }

            return null;
        }

        #region Data access management

        public bool CanUseLocationInOperation (OperationType operationType, long locationId, out string error, out string warning)
        {
            foreach (IDataExtender dataExtender in DataAccessExtenders) {
                if (!dataExtender.CanUseLocationInOperation (operationType, locationId, out error, out warning))
                    return false;
            }

            error = null;
            warning = null;
            return true;
        }

        public bool WaitForPendingOperationToComplete (Operation operation, int msecs)
        {
            foreach (IDataExtender dataExtender in DataAccessExtenders) {
                if (!dataExtender.WaitForPendingOperationToComplete (operation, msecs))
                    return false;
            }

            return true;
        }

        public bool WaitForPendingOperationToComplete (long detailId, int msecs)
        {
            foreach (IDataExtender dataExtender in DataAccessExtenders) {
                if (!dataExtender.WaitForPendingOperationToComplete (detailId, msecs))
                    return false;
            }

            return true;
        }

        public bool CanEditOperation (object operation, out string message, out bool readOnlyView)
        {
            foreach (IDataExtender dataExtender in DataAccessExtenders) {
                if (!dataExtender.CanEditOperation (operation, out message, out readOnlyView))
                    return false;
            }

            message = null;
            readOnlyView = false;
            return true;
        }

        public bool CanEditOperation (OperationType type, long operationId, out string message, out bool readOnlyView)
        {
            Operation operation = null;
            readOnlyView = false;
            foreach (IDataExtender dataExtender in DataAccessExtenders) {
                if (operation == null) {
                    operation = Operation.GetById (type, operationId);
                    if (operation == null) {
                        message = Translator.GetString ("Cannot find the selected operation!");
                        return false;
                    }
                }

                if (!dataExtender.CanEditOperation (operation, out message, out readOnlyView))
                    return false;
            }

            message = null;
            return true;
        }

        public bool CanAnnulOperation (object operation, out string error, out string warning)
        {
            foreach (IDataExtender dataExtender in DataAccessExtenders) {
                if (!dataExtender.CanAnnulOperation (operation, out error, out warning))
                    return false;
            }

            error = null;
            warning = null;
            return true;
        }

        public bool RequestOperationAnnul (object operation)
        {
            foreach (IDataExtender dataExtender in DataAccessExtenders)
                dataExtender.RequestOperationAnnul (operation);

            return true;
        }

        public bool CanEditPayment (Payment payment, out string message)
        {
            foreach (IDataExtender dataExtender in DataAccessExtenders) {
                if (!dataExtender.CanEditPayment (payment, out message))
                    return false;
            }

            message = null;
            return true;
        }

        public bool CanIncreaseStoreAvailabilityOnTransfer (object transfer)
        {
            return DataAccessExtenders.All (dataExtender => dataExtender.CanIncreaseStoreAvailabilityOnTransfer (transfer));
        }

        public bool CanIncreaseStoreAvailabilityOnTransferAnnull (object transfer)
        {
            return DataAccessExtenders.All (dataExtender => dataExtender.CanIncreaseStoreAvailabilityOnTransferAnnull (transfer));
        }

        public DateTime GetDatabaseLastUpdate ()
        {
            foreach (IDataExtender dataExtender in DataAccessExtenders)
                return dataExtender.GetDatabaseLastUpdate ();

            return DateTime.Now;
        }

        #endregion
    }
}
