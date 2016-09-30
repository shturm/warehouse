//
// DataProvider.Common.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   06/21/2006
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
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Mono.Data.Sqlite;

namespace Warehouse.Data.SQLite
{
    public class DataProvider : SQL.DataProvider
    {
        private const int BUSY_TIMEOUT = 10 * 1000;
        private const int BUSY_SLEEP = 10;
        private const string DATA_FILE_EXTENSION = ".db3";
        private bool syncDataChecked;
        private bool syncDataPresent;

        public override bool UsesDatabase
        {
            get { return true; }
        }

        protected override string InsertIgnoreSeparator
        {
            get { return "OR"; }
        }

        public override int MaxInsertedRows
        {
            get { return 25; }
        }

        // SQLITE is compiled by default with no support for DELETE... LIMIT...
        public override string LimitDelete
        {
            get { return string.Empty; }
        }

        #region Initialization

        public DataProvider (string server, string slaveServer, string user, string pass)
            : base (server, slaveServer, user, pass)
        {
        }

        protected override void InitUpgradeTable ()
        {
            //upgradeTable.Add (new UpgradeEntry ("2.06", "3.00", "Upgrade_206_300.sql"));
            //upgradeTable.Add (new UpgradeEntry ("3.00", "3.01", "Upgrade_300_301.sql"));
            //upgradeTable.Add (new UpgradeEntry ("3.01", "3.02", "Upgrade_301_302.sql"));
            //upgradeTable.Add (new UpgradeEntry ("3.02", "3.03", "Upgrade_302_303.sql"));
            //upgradeTable.Add (new UpgradeEntry ("3.03", "3.04", "Upgrade_303_304.sql"));
            //upgradeTable.Add (new UpgradeEntry ("3.04", "3.05", "Upgrade_304_305.sql"));
            //upgradeTable.Add (new UpgradeEntry ("3.05", "3.06", "Upgrade_305_306.sql"));
            //upgradeTable.Add (new UpgradeEntry ("3.06", "3.07", "Upgrade_306_307.sql"));
        }

        protected override void InitFieldNames ()
        {
            base.InitFieldNames ();
            fieldsTable.Add (DataField.OperationDateTime, "(datetime(operations.Date, '+' || SUBSTR(operations.Note,1,2) || ' hours', '+' || SUBSTR(operations.Note,4,2) || ' minutes'))");
        }

        #endregion

        #region General methods

        public override bool TryConnect ()
        {
            return true;
            //string dataFile = GetDataFile ();
            //if (string.IsNullOrWhiteSpace (dataFile) || !File.Exists (dataFile))
            //    throw new Exception ("The connection parameters are not properly set.");

            //SqliteConnection conn = null;
            //try {
            //    conn = (SqliteConnection) GetConnection (false);
            //    return true;
            //} finally {
            //    if (conn != null)
            //        conn.Close ();
            //}
        }

        public override void Disconnect ()
        {
        }

        protected override string GenerateConnectionString (bool masterServer)
        {
            syncDataChecked = false;
            SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder
                {
                    DataSource = GetDataFile (),
                    //Pooling = true,
                    Version = 3,
                    DefaultTimeout = BUSY_TIMEOUT
                };

            return builder.ConnectionString;
        }

        private string GetDataFile (string dbName = null)
        {
            if (dbName == null)
                dbName = database;

            if (!dbName.EndsWith (DATA_FILE_EXTENSION))
                dbName += DATA_FILE_EXTENSION;

            return Path.Combine (StoragePaths.CommonAppDataFolder, dbName);
        }

        #region Database management

        public override DateTime Now ()
        {
            return DateTime.Now;
        }

        public override void CreateDatabase (string dbName, CreateDatabaseType type, Action<double> pCallback)
        {
            dbName = FilterSqlParameter (dbName);
            string script;
            int extraCommands = 0;
            List<DbParam> parameters = new List<DbParam> ();
            switch (type) {
                case CreateDatabaseType.SampleRestaurant:
                    script = string.Format (GetScript ("SampleRestaurant_307.sql.gz", parameters), dbName);
                    extraCommands = AdjustSampleDatesCommandsNumber;
                    break;

                case CreateDatabaseType.SampleStore:
                    script = string.Format (GetScript ("SampleStore_307.sql.gz", parameters), dbName);
                    extraCommands = AdjustSampleDatesCommandsNumber;
                    break;

                default:
                    script = string.Format (GetScript ("Blank_307.sql.gz", parameters), dbName);
                    break;
            }

            int i = 0;
            string [] scriptLines = script.Split (';');
            string savedDatabase = Database;
            try {
                Database = dbName;
                using (DbTransaction transaction = new DbTransaction (this)) {
                    for (i = 0; i < scriptLines.Length; i++) {
                        string line = scriptLines [i];
                        line = line.Trim (' ', '\n', '\r', '\t', '\xA0');
                        if (line.Length > 0)
                            ExecuteNonQuery (line, parameters.ToArray ());

                        if (pCallback != null)
                            pCallback (((double) (i * 100)) / (scriptLines.Length + extraCommands - 1));
                    }

                    if (type == CreateDatabaseType.SampleRestaurant ||
                        type == CreateDatabaseType.SampleStore)
                        AdjustSampleDates (scriptLines.Length, pCallback);

                    EnsurePatchVersion (dbName);

                    transaction.Complete ();
                }
            } catch (Exception ex) {
                if (ex is DbConnectionLostException) {
                    IOException ioException = new IOException ("", ex);
                    ioException.Data.Add ("Path", GetDataFile ());
                    throw ioException;
                }

                try {
                    File.Delete (GetDataFile (dbName));
                } catch (Exception) { }
                throw new SqlSyntaxException (scriptLines [i], i, ex);
            } finally {
                Database = savedDatabase;
            }
        }

        private void AdjustSampleDates (int createScriptLines, Action<double> pCallback)
        {
            DateTime? maxDate = null;
            int i = 0;
            int extraCommands = AdjustSampleDatesCommandsNumber;
            foreach (KeyValuePair<string, string> dateFeild in DateFeilds) {
                object dateObject = ExecuteScalar (string.Format ("select MAX({1}) from {0}", dateFeild.Key, dateFeild.Value));
                if (pCallback != null)
                    pCallback (((double) ((createScriptLines + i) * 100)) / (createScriptLines + extraCommands - 1));

                i++;
                if (IsDBNull (dateObject))
                    continue;

                DateTime date = Convert.ToDateTime (dateObject);
                if (maxDate == null)
                    maxDate = date;
                else if (date > maxDate)
                    maxDate = date;
            }

            if (maxDate == null)
                return;

            int difference = (int) (Now ().Date - maxDate.Value).TotalDays - 1;

            foreach (KeyValuePair<string, string> dateFeild in DateFeilds) {
                ExecuteNonQuery (string.Format ("update {0} set `{1}` = datetime(`{1}`, '+{2} day');",
                    dateFeild.Key, dateFeild.Value, difference.ToString (CultureInfo.InvariantCulture)));

                if (pCallback != null)
                    pCallback (((double) ((createScriptLines + i) * 100)) / (createScriptLines + extraCommands - 1));

                i++;
            }
        }

        public override bool CheckDatabaseExists (string dbName)
        {
            return File.Exists (GetDataFile (dbName));
        }

        public override bool DropDatabase (string dbName)
        {
            GC.Collect ();
            GC.WaitForPendingFinalizers ();

            string dataFile = GetDataFile (dbName);
            if (!File.Exists (dataFile))
                return false;

            for (int i = 0; i < 10; i++) {
                try {
                    File.Delete (dataFile);
                    return true;
                } catch (Exception) {
                    Thread.Sleep (200);
                }
            }

            return false;
        }

        public override string [] GetDatabases ()
        {
            if (!Directory.Exists (StoragePaths.CommonAppDataFolder))
                return new string [0];

            List<string> dbs = Directory.GetFiles (StoragePaths.CommonAppDataFolder)
                .Where (f => f.EndsWith (DATA_FILE_EXTENSION))
                .Select (Path.GetFileNameWithoutExtension)
                .Where (f => !string.IsNullOrWhiteSpace (f))
                .ToList ();
            string savedDb = database;

            try {
                for (int i = dbs.Count - 1; i >= 0; i--) {
                    Database = dbs [i];
                    List<string> tables;
                    try {
                        tables = ExecuteList<string> (@"SELECT name FROM sqlite_master WHERE type='table'");
                    } catch (SqliteException) {
                        dbs.RemoveAt (i);
                        continue;
                    }

                    if (!tables.Contains ("applicationlog") || !tables.Contains ("cashbook") ||
                        !tables.Contains ("configuration") || !tables.Contains ("currencies") ||
                        !tables.Contains ("currencieshistory") || !tables.Contains ("documents") ||
                        !tables.Contains ("goods") || !tables.Contains ("goodsgroups") ||
                        !tables.Contains ("lots") || !tables.Contains ("nextacct") ||
                        !tables.Contains ("objects") || !tables.Contains ("objectsgroups") ||
                        !tables.Contains ("operations") || !tables.Contains ("operationtype") ||
                        !tables.Contains ("partners") || !tables.Contains ("partnersgroups") ||
                        !tables.Contains ("payments") || !tables.Contains ("paymenttypes") ||
                        !tables.Contains ("store") || !tables.Contains ("system") ||
                        !tables.Contains ("users") || !tables.Contains ("usersgroups") ||
                        !tables.Contains ("userssecurity"))
                        dbs.RemoveAt (i);
                }
            } finally {
                Database = savedDb;
            }

            return dbs.ToArray ();
        }

        public override string GetDatabaseVersion ()
        {
            string version = ExecuteScalar<string> ("select sqlite_version()");

            return string.Format ("SQLite|{0}", version);
        }

        #endregion

        #region Transactions management

        public override void BeginTransaction ()
        {
            TransactionContext context = TransactionContext.Current;
            if (context != null) {
                context.BeganTransactions++;
                return;
            }

            int threadId = Thread.CurrentThread.ManagedThreadId;
            SqliteConnection conn = (SqliteConnection) GetConnection (false);

            TransactionContext.Current = new TransactionContext (threadId, conn.BeginTransaction (IsolationLevel.ReadCommitted), conn);
        }

        public override void CommitTransaction ()
        {
            TransactionContext context = TransactionContext.Current;
            if (context == null)
                return;

            context.BeganTransactions--;
            if (context.BeganTransactions > 0)
                return;

            DateTime end = DateTime.Now.AddMilliseconds (BUSY_TIMEOUT);
            using (SqliteTransaction trans = (SqliteTransaction) context.Transaction) {
                bool retry;
                do {
                    retry = false;
                    try {
                        trans.Commit ();
                    } catch (SqliteException ex) {
                        if ((ex.ErrorCode != SQLiteErrorCode.Busy && ex.ErrorCode != SQLiteErrorCode.Locked) || end < DateTime.Now)
                            throw;

                        retry = true;
                        Thread.Sleep (BUSY_SLEEP);
                    }
                } while (retry);
            }

            using (SqliteConnection conn = (SqliteConnection) context.GetConnection ())
                if (conn != null)
                    conn.Close ();

            context.ReleaseConnection ();
            context.CommitObjects ();

            TransactionContext.Current = null;

            OnCommandExecuted ("COMMIT", DateTime.Now, new DbParam [0], false);
        }

        public override void RollbackTransaction ()
        {
            TransactionContext context = TransactionContext.Current;
            if (context == null)
                return;

            context.BeganTransactions--;
            if (context.BeganTransactions > 0)
                return;

            DateTime end = DateTime.Now.AddMilliseconds (BUSY_TIMEOUT);
            using (SqliteTransaction trans = (SqliteTransaction) context.Transaction) {
                bool retry;
                do {
                    retry = false;
                    try {
                        trans.Rollback ();
                    } catch (SqliteException ex) {
                        if ((ex.ErrorCode != SQLiteErrorCode.Busy && ex.ErrorCode != SQLiteErrorCode.Locked) || end < DateTime.Now)
                            throw;

                        retry = true;
                        Thread.Sleep (BUSY_SLEEP);
                    }
                } while (retry);
            }

            using (SqliteConnection conn = (SqliteConnection) context.GetConnection ())
                if (conn != null)
                    conn.Close ();

            context.ReleaseConnection ();
            context.RollbackObjects ();

            TransactionContext.Current = null;
        }

        #endregion

        protected override DbParam GetFilterParam (List<DbParam> p, string key)
        {
            return new DbParam ("fPar" + p.Count, "(?i)" + Regex.Escape (key));
        }

        protected override string GetFilterCondition (string source, DbParam parameter)
        {
            return string.Format ("({0} REGEXP {1})",
                source, fieldsTable.GetParameterName (parameter.ParameterName));
        }

        public override ulong GetMaxCodeValue (DbTable table, string pattern)
        {
            string tableName;
            switch (table) {
                case DbTable.Items:
                    tableName = "goods";
                    break;

                case DbTable.Objects:
                    tableName = "objects";
                    break;

                case DbTable.Users:
                    tableName = "users";
                    break;

                case DbTable.Partners:
                    tableName = "partners";
                    break;

                default:
                    throw new ArgumentException ("table");
            }

            Regex rex = new Regex ("^([\\w-+=\\.\\*&]*)#+([\\w-+=\\.\\*&]*)$", RegexOptions.Compiled);
            Match match = rex.Match (pattern);
            if (!match.Success)
                throw new ArgumentException ("codeMask");

            string prefix = match.Groups [1].Value;
            string suffix = match.Groups [2].Value;

            SqlHelper helper = GetSqlHelper ();
            helper.AddParameters (
                new DbParam ("prefixLen", prefix.Length),
                new DbParam ("suffixLen", suffix.Length),
                new DbParam ("prefix", prefix),
                new DbParam ("suffix", suffix));

            string query = string.Format (@"SELECT MAX(CAST(SUBSTR(Code, @prefixLen + 1, LENGTH(Code) - @suffixLen - @prefixLen) AS UNSIGNED)) as `MaxValue`
FROM {0}
WHERE Code REGEXP {1}", tableName, GetConcatStatement ("'^'", "@prefix", "'[0-9]+'", "@suffix", "'$'"));

            object ret = ExecuteScalar (query, helper.Parameters);
            return IsDBNull (ret) ? 0 : Convert.ToUInt64 (ret);
        }

        public override long GetMaxOperationId ()
        {
            return long.MaxValue;
        }

        public override void BulkInsert (string table, string columns, IList<List<DbParam>> insertParams, string errorMessage = "")
        {
            if (insertParams.Count == 0)
                return;

            int start = 0;
            int remaining = insertParams.Count;
            string [] columnNames = columns.Split (new [] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            while (remaining > 0) {
                int limit = Math.Min (MaxInsertedRows, remaining);
                StringBuilder insertBuilder = new StringBuilder (string.Format ("({0}) SELECT ", columns));
                List<DbParam> finalParameters = new List<DbParam> (insertParams [start]);
                for (int i = 0; i < insertParams [start].Count; i++) {
                    DbParam param = insertParams [start] [i];
                    param.ParameterName += "x" + start.ToString (CultureInfo.InvariantCulture);
                    insertBuilder.AppendFormat ("{0} AS {1}, ", fieldsTable.GetParameterName (param.ParameterName), columnNames [i]);
                }
                insertBuilder.Remove (insertBuilder.Length - 2, 2);

                for (int i = start + 1; i < start + limit; i++) {
                    insertBuilder.Append (" UNION ALL SELECT ");
                    List<DbParam> parameters = insertParams [i];
                    foreach (DbParam param in parameters) {
                        param.ParameterName += "x" + i.ToString (CultureInfo.InvariantCulture);
                        insertBuilder.AppendFormat ("{0}, ", fieldsTable.GetParameterName (param.ParameterName));
                    }
                    insertBuilder.Remove (insertBuilder.Length - 2, 2);
                    finalParameters.AddRange (parameters);
                }

                int result = ExecuteNonQuery (string.Format ("INSERT INTO {0} {1}",
                    table, insertBuilder), finalParameters.ToArray ());

                if (result != limit)
                    throw new Exception (errorMessage);
                start += limit;
                remaining -= limit;
            }
        }

        #endregion

        #region Cusom Config

        protected override void EnsureVersion1 (string dbName = null)
        {
            try {
                ExecuteNonQuery (@"CREATE INDEX IF NOT EXISTS `OperTypeAcct_idx` ON `operations` (OperType, ObjectID, Acct)");
            } catch (Exception) {
            }

            try {
                ExecuteNonQuery (@"CREATE INDEX IF NOT EXISTS `AveragePriceCalculation_idx` ON `operations` (GoodID, OperType, Qtty, Sign, PriceIn)");
            } catch (Exception) {
            }

            try {
                ExecuteNonQuery (@"CREATE INDEX IF NOT EXISTS `AveragePriceCalculation1_idx` ON `operations` (GoodID, PriceIn)");
            } catch (Exception) {
            }
        
            if (ExecuteScalar<int> (@"SELECT count(1) FROM sqlite_master WHERE type='table' AND name='customconfig'") > 0)
                return;

            ExecuteNonQuery (@"CREATE TABLE `customconfig` (
    `ID` INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    `Category` TEXT NOT NULL,
    `Profile` TEXT NOT NULL,
    `Data` BLOB)");
        }

        #endregion

        #region Helper methods

        protected override string GetMoreThanMinuteAgoFilter ()
        {
            return "datetime('now','-@wPar{0} minutes') > {{0}}";
        }

        protected override string GetLessThanMinuteAgoFilter ()
        {
            return "datetime('now','-@wPar{0} minutes') < {{0}}";
        }

        #region Execute Other

        public override IDataReader ExecuteReader (string commandText, params DbParam [] parameters)
        {
            SqliteConnection conn = null;
            SqliteCommand command = null;
            SQLiteDataReaderWrapper dr = null;
            DateTime start = DateTime.Now;
            bool isReadOnly = false;
            bool retry;
            DateTime end = DateTime.Now.AddMilliseconds (BUSY_TIMEOUT);

            do {
                retry = false;
                try {
                    command = GetCommand (out conn, commandText, parameters, out isReadOnly);
                    
                    //create a reader
                    SqliteDataReader reader = command.ExecuteReader ();
                    if (reader == null)
                        throw new DbConnectionLostException ("Could not get a valid reader from the database");

                    dr = new SQLiteDataReaderWrapper (conn, TransactionContext.Current, reader, command);
                    dr.Disposed += DataReader_Disposed;

                    // detach the SqlParameters from the command object, so they can be used again.
                    command.Parameters.Clear ();
                } catch (SqliteException ex) {
                    CleanupReaderOnError (dr, conn, command);
                    if ((ex.ErrorCode == SQLiteErrorCode.Busy || ex.ErrorCode == SQLiteErrorCode.Locked) && end < DateTime.Now) {
                        retry = true;
                        Thread.Sleep (BUSY_SLEEP);
                    } else if (ex.InnerException is TargetInvocationException)
                        throw new DbConnectionLostException (ex);
                    else
                        throw;
                } catch (DbConnectionLostException) {
                    CleanupReaderOnError (dr, conn, command);
                    throw;
                } catch {
                    CleanupReaderOnError (dr, conn, command);
                } finally {
                    OnCommandExecuted (commandText, start, parameters, isReadOnly);
                }
            } while (retry);

            return dr;
        }

        private void CleanupReaderOnError (SQLiteDataReaderWrapper dr, SqliteConnection conn, SqliteCommand command)
        {
            TransactionContext context = TransactionContext.Current;
            if (dr != null) {
                if (!dr.IsClosed)
                    dr.Close ();

                if (!dr.IsDisposed) {
                    DataReader_Disposed (dr, new DataReaderDisposedEventArgs (conn, context, command));
                    return;
                }
            }

            if (command != null && context != null)
                context.ReleaseConnection ();

            if (command != null)
                command.Dispose ();
        }

        private static void DataReader_Disposed (object sender, DataReaderDisposedEventArgs e)
        {
            IDbCommand command = e.DbCommand;
            if (command != null)
                command.Dispose ();

            // If we have a connection passed then we should close it now
            SqliteConnection conn = e.Connection as SqliteConnection;
            if (conn != null) {
                conn.Close ();
                conn.Dispose ();
            }

            // If we have a TransactionContext passed then we are in a transaction and we should release the connection
            if (e.Context != null)
                e.Context.ReleaseConnection ();
        }

        public override long GetLastAutoId ()
        {
            if (!syncDataChecked) {
                syncDataPresent = ExecuteScalar<int> (@"SELECT count(1) FROM sqlite_master WHERE type='table' AND name='syncdata'") > 0;
                syncDataChecked = true;
            }

            return ExecuteScalar<long> (syncDataPresent ?
                @"SELECT IFNULL((SELECT IntValue FROM `syncdata` WHERE Key = 'LastAutoID'), last_insert_rowid())" :
                @"SELECT last_insert_rowid()");
        }

        protected override object GetConnection (bool readOnly)
        {
            try {
                SqliteConnection conn = new SqliteConnection (ConnString);
                conn.Open ();
                return conn;
            } catch (SqliteException ex) {
                throw new DbConnectionLostException (ex);
            }
        }

        /// <summary>
        /// Creates and sets up a command object
        /// </summary>
        /// <param name="conn">Connection object used in the command in case we are not in a transaction</param>
        /// <param name="commandText">Command to be executed</param>
        /// <param name="parameters">Command paramaters</param>
        /// <returns></returns>
        private SqliteCommand GetCommand (out SqliteConnection conn, string commandText, IEnumerable<DbParam> parameters, out bool isReadOnly)
        {
            isReadOnly = !commandText.StartsWith ("INSERT ", StringComparison.InvariantCultureIgnoreCase) &&
                commandText.IndexOf (" INSERT ", StringComparison.InvariantCultureIgnoreCase) < 0 &&
                !commandText.StartsWith ("UPDATE ", StringComparison.InvariantCultureIgnoreCase) &&
                commandText.IndexOf (" UPDATE ", StringComparison.InvariantCultureIgnoreCase) < 0 &&
                !commandText.StartsWith ("DELETE ", StringComparison.InvariantCultureIgnoreCase) &&
                commandText.IndexOf (" DELETE ", StringComparison.InvariantCultureIgnoreCase) < 0;

            if (commandText.IndexOf ("syncdata", StringComparison.InvariantCultureIgnoreCase) > 0)
                syncDataChecked = false;

            object trans;
            SqliteConnection connection = (SqliteConnection) GetConnection (!IsMasterScopeOpen && isReadOnly, out trans);
            if (connection == null) {
                conn = null;
                return null;
            }

            SqliteCommand command = new SqliteCommand
                {
                    Connection = connection,
                    Transaction = (SqliteTransaction) trans,
                    CommandText = commandText,
                    CommandType = CommandType.Text
                };

            if (parameters != null)
                foreach (DbParam p in parameters) {
                    command.Parameters.Add (new SqliteParameter (p.ParameterName, p.Value) { Direction = p.Direction });
                }

            conn = trans != null ? null : connection;

            return command;
        }

        #endregion

        public override SqlHelper GetSqlHelper ()
        {
            return new SQLiteHelper (fieldsTable);
        }

        public override string GetConcatStatement (params string [] values)
        {
            return values.Length == 0 ?
                "''" :
                string.Format ("({0})", string.Join (" || ", values));
        }

        #endregion
    }
}
