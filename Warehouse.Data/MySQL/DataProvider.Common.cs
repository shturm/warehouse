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
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using MySql.Data.MySqlClient;

namespace Warehouse.Data.MySQL
{
    public class DataProvider : SQL.DataProvider
    {
        public override bool UsesServer
        {
            get { return true; }
        }

        public override bool UsesSlaveServer
        {
            get { return true; }
        }

        public override bool UsesUser
        {
            get { return true; }
        }

        public override bool UsesPassword
        {
            get { return true; }
        }

        public override bool UsesDatabase
        {
            get { return true; }
        }

        public override int MaxInsertedRows
        {
            get { return 5000; }
        }

        #region Initialization

        public DataProvider (string server, string slaveServer, string user, string pass)
            : base (server, slaveServer, user, pass)
        {
        }

        protected override void InitUpgradeTable ()
        {
            upgradeTable.Add (new UpgradeEntry ("2.06", "3.00", "Upgrade_206_300.sql.gz"));
            upgradeTable.Add (new UpgradeEntry ("3.00", "3.01", "Upgrade_300_301.sql.gz"));
            upgradeTable.Add (new UpgradeEntry ("3.01", "3.02", "Upgrade_301_302.sql.gz"));
            upgradeTable.Add (new UpgradeEntry ("3.02", "3.03", "Upgrade_302_303.sql.gz"));
            upgradeTable.Add (new UpgradeEntry ("3.03", "3.04", "Upgrade_303_304.sql.gz"));
            upgradeTable.Add (new UpgradeEntry ("3.04", "3.05", "Upgrade_304_305.sql.gz"));
            upgradeTable.Add (new UpgradeEntry ("3.05", "3.06", "Upgrade_305_306.sql.gz"));
            upgradeTable.Add (new UpgradeEntry ("3.06", "3.07", "Upgrade_306_307.sql.gz"));
        }

        #endregion

        #region General methods

        public override bool TryConnect ()
        {
            if (string.IsNullOrWhiteSpace (server) || string.IsNullOrWhiteSpace (user))
                throw new Exception ("The connection parameters are not properly set.");

            MySqlConnection conn = null;
            try {
                conn = (MySqlConnection) GetConnection (false);
            } finally {
                if (conn != null)
                    conn.Close ();
            }

            if (string.IsNullOrWhiteSpace (slaveServer))
                return true;

            conn = null;
            try {
                conn = (MySqlConnection) GetConnection (true);
                return true;
            } finally {
                if (conn != null)
                    conn.Close ();
            }
        }

        public override void Disconnect ()
        {
        }

        protected override string GenerateConnectionString (bool masterServer)
        {
            MySqlConnectionStringBuilder connectionString = new MySqlConnectionStringBuilder ();

            if (!masterServer && !string.IsNullOrWhiteSpace (slaveServer))
                connectionString.Server = slaveServer;
            else if (!string.IsNullOrEmpty (server))
                connectionString.Server = server;

            if (!string.IsNullOrEmpty (user))
                connectionString.UserID = user;

            if (!string.IsNullOrEmpty (password))
                connectionString.Password = password;

            // MySQL Server 5.1 requires the database paramater to be passed
            connectionString.Database = string.IsNullOrEmpty (database) ? "information_schema" : database;

            if (connectTimeout >= 0)
                connectionString.ConnectionTimeout = (uint) connectTimeout;

            if (commandTimeout >= 0)
                connectionString.DefaultCommandTimeout = (uint) commandTimeout;

            connectionString.CharacterSet = "utf8";
            connectionString.AllowUserVariables = true;

            return connectionString.GetConnectionString (true);
        }

        #region Database management

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
            try {
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
                throw new SqlSyntaxException (scriptLines [i], i, ex);
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

                DateTime date = (DateTime) dateObject;
                if (maxDate == null)
                    maxDate = date;

                if (date > maxDate)
                    maxDate = date;
            }

            if (maxDate == null)
                return;

            int difference = (int) (Now ().Date - maxDate.Value).TotalDays - 1;
            DbParam diffPar = new DbParam ("diff", difference);

            foreach (KeyValuePair<string, string> dateFeild in DateFeilds) {
                ExecuteNonQuery (string.Format ("update {0} set {1} = DATE_ADD({1}, INTERVAL @diff DAY);",
                    dateFeild.Key, dateFeild.Value), diffPar);

                if (pCallback != null)
                    pCallback (((double) ((createScriptLines + i) * 100)) / (createScriptLines + extraCommands - 1));

                i++;
            }
        }

        public override bool CheckDatabaseExists (string dbName)
        {
            DbParam param = new DbParam ("dbName", dbName);
            object result = ExecuteScalar (@"
                SELECT 1 FROM INFORMATION_SCHEMA.SCHEMATA 
                WHERE SCHEMA_NAME = @dbName", param);
            return result != null;
        }

        public override bool DropDatabase (string dbName)
        {
            int ret = ExecuteNonQuery (string.Format ("DROP DATABASE IF EXISTS `{0}`", FilterSqlParameter (dbName)));
            return ret != 0;
        }

        public override string [] GetDatabases ()
        {
            List<string> dbs = ExecuteList<string> ("SHOW DATABASES");

            dbs.Remove ("information_schema");
            dbs.Remove ("mysql");

            for (int i = dbs.Count - 1; i >= 0; i--) {
                List<string> tables = ExecuteList<string> (string.Format ("SHOW TABLES IN `{0}`", dbs [i]));

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

            return dbs.ToArray ();
        }

        public override string GetDatabaseVersion ()
        {
            Dictionary<string, string> versions = ExecuteDictionary<string, string> ("SHOW VARIABLES LIKE '%version%'");

            string value;
            return string.Format ("{0}|{1}",
                versions.TryGetValue ("version_comment", out value) ? value : "MySQL Server",
                versions.TryGetValue ("version", out value) ? value : "?");
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
            MySqlConnection conn = (MySqlConnection) GetConnection (false);

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

            try {
                using (MySqlTransaction trans = (MySqlTransaction) context.Transaction)
                    trans.Commit ();

                using (MySqlConnection conn = (MySqlConnection) context.GetConnection ())
                    if (conn != null)
                        conn.Close ();
            } catch (InvalidOperationException ex) {
                throw new DbConnectionLostException (ex);
            }

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

            try {
                using (MySqlTransaction trans = (MySqlTransaction) context.Transaction)
                    trans.Rollback ();

                using (MySqlConnection conn = (MySqlConnection) context.GetConnection ())
                    if (conn != null)
                        conn.Close ();
            } catch (InvalidOperationException ex) {
                throw new DbConnectionLostException (ex);
            }

            context.ReleaseConnection ();
            context.RollbackObjects ();

            TransactionContext.Current = null;
        }

        #endregion

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

            string query = string.Format (@"SELECT MAX(CAST(SUBSTR(LEFT(Code, LENGTH(Code) - @suffixLen), @prefixLen + 1) AS UNSIGNED)) as `MaxValue`
FROM {0}
WHERE Code REGEXP {1}", tableName, GetConcatStatement ("'^'", "@prefix", "'[0-9]+'", "@suffix", "'$'"));

            object ret = ExecuteScalar (query, helper.Parameters);
            return IsDBNull (ret) ? 0 : (ulong) ret;
        }

        public override long GetMaxOperationId ()
        {
            using (IDataReader reader = ExecuteReader (@"SHOW CREATE TABLE operations"))
                if (reader != null &&
                    reader.Read () &&
                    reader.GetString (1).ToLower ().Contains ("`acct` int(11)"))
                    return int.MaxValue;

            return long.MaxValue;
        }

        #endregion

        #region Cusom Config

        protected override void EnsureVersion1 (string dbName = null)
        {
            try {
                ExecuteNonQuery (@"ALTER TABLE `operations` ADD INDEX `OperTypeAcct` USING BTREE(OperType, ObjectID, Acct)");
            } catch (Exception) {
            }

            try {
                ExecuteNonQuery (@"ALTER TABLE `operations` ADD INDEX `AveragePriceCalculation` USING BTREE(GoodID, OperType, Qtty, Sign, PriceIn)");
            } catch (Exception) {
            }

            try {
                ExecuteNonQuery (@"ALTER TABLE `operations` ADD INDEX `AveragePriceCalculation1` USING BTREE(GoodID, PriceIn)");
            } catch (Exception) {
            }

            List<string> tables = ExecuteList<string> (string.Format ("SHOW TABLES IN `{0}`", dbName ?? database));
            if (tables.Contains ("customconfig")) {
                using (IDataReader reader = ExecuteReader (@"SHOW CREATE TABLE customconfig"))
                    if (reader != null &&
                        reader.Read () &&
                        reader.GetString (1).ToLower ().Contains ("`data` longblob"))
                        return;

                ExecuteNonQuery (@"ALTER TABLE `customconfig` CHANGE COLUMN `Data` `Data` longblob NULL DEFAULT NULL");
            } else
                ExecuteNonQuery (@"CREATE TABLE `customconfig` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Category` varchar(45) NOT NULL,
  `Profile` varchar(45) NOT NULL,
  `Data` longblob,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8");
        }

        #endregion

        #region Helper methods

        #region Execute Other

        public override IDataReader ExecuteReader (string commandText, params DbParam [] parameters)
        {
            MySqlConnection conn = null;
            MySqlCommand command = null;
            MySQLDataReaderWrapper dr = null;
            DateTime start = DateTime.Now;
            bool isReadOnly = false;

            try {
                command = GetCommand (out conn, commandText, parameters, out isReadOnly);
                if (command == null)
                    return null;

                //create a reader and use a stupid retry as sometimes the server kills the query for no good reason
                IDataReader reader = command.ExecuteReader () ?? command.ExecuteReader () ?? command.ExecuteReader ();
                if (reader == null)
                    throw new DbConnectionLostException ("Could not get a valid reader from the database");

                dr = new MySQLDataReaderWrapper (conn, TransactionContext.Current, reader, command);
                dr.Disposed += DataReader_Disposed;

                // detach the SqlParameters from the command object, so they can be used again.
                command.Parameters.Clear ();
            } catch (MySqlException ex) {
                CleanupReaderOnError (dr, conn, command);
                if (ex.HasException<SocketException> () ||
                    ex.HasException<TargetInvocationException> () ||
                    ex.HasException<TimeoutException> ())
                    throw new DbConnectionLostException (ex);

                throw new Exception ("MySql", ex);
            } catch (DbConnectionLostException) {
                CleanupReaderOnError (dr, conn, command);
                throw;
            } catch {
                CleanupReaderOnError (dr, conn, command);
            } finally {
                OnCommandExecuted (commandText, start, parameters, isReadOnly);
            }

            return dr;
        }

        private void CleanupReaderOnError (MySQLDataReaderWrapper dr, MySqlConnection conn, MySqlCommand command)
        {
            TransactionContext context = TransactionContext.Current;
            if (dr != null && !dr.IsDisposed) {
                DataReader_Disposed (dr, new DataReaderDisposedEventArgs (conn, context, command));
                return;
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
            MySqlConnection conn = e.Connection as MySqlConnection;
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
            return ExecuteScalar<long> ("SELECT CAST(IFNULL(@LAST_AUTOID_FROM_TRIGGER, LAST_INSERT_ID()) as SIGNED)");
        }

        protected override object GetConnection (bool readOnly)
        {
            try {
                MySqlConnection conn = new MySqlConnection (readOnly ? SlaveConnString : ConnString);
                conn.Open ();
                return conn;
            } catch (SocketException ex) {
                throw new DbConnectionLostException (ex);
            } catch (MySqlException ex) {
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
        private MySqlCommand GetCommand (out MySqlConnection conn, string commandText, IEnumerable<DbParam> parameters, out bool isReadOnly)
        {
            isReadOnly = !commandText.StartsWith ("INSERT ", StringComparison.InvariantCultureIgnoreCase) &&
                commandText.IndexOf (" INSERT ", StringComparison.InvariantCultureIgnoreCase) < 0 &&
                !commandText.StartsWith ("UPDATE ", StringComparison.InvariantCultureIgnoreCase) &&
                commandText.IndexOf (" UPDATE ", StringComparison.InvariantCultureIgnoreCase) < 0 &&
                !commandText.StartsWith ("DELETE ", StringComparison.InvariantCultureIgnoreCase) &&
                commandText.IndexOf (" DELETE ", StringComparison.InvariantCultureIgnoreCase) < 0;

            object trans;
            MySqlConnection connection = (MySqlConnection) GetConnection (!IsMasterScopeOpen && isReadOnly, out trans);
            if (connection == null) {
                conn = null;
                return null;
            }

            MySqlCommand command = new MySqlCommand
                {
                    Connection = connection,
                    Transaction = (MySqlTransaction) trans,
                    CommandText = commandText,
                    CommandType = CommandType.Text
                };

            if (parameters != null)
                foreach (DbParam p in parameters) {
                    command.Parameters.Add (new MySqlParameter (p.ParameterName, p.Value) { Direction = p.Direction });
                }

            conn = trans != null ? null : connection;

            return command;
        }

        #endregion

        #endregion
    }
}
