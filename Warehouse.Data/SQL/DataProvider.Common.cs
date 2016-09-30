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
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Warehouse.Data.Model;
using Warehouse.Data.ThreadPool;

namespace Warehouse.Data.SQL
{
	public abstract partial class DataProvider : Data.DataProvider
	{
		#region Private fields

		protected readonly object syncRoot = new object ();
		protected JobWrapper currentJob;

		#endregion

		#region Public Properties

		#endregion

		protected virtual string ParameterPrefix {
			get { return "@"; }
		}

		protected virtual char QuotationChar {
			get { return '`'; }
		}

		protected virtual string InsertIgnoreSeparator {
			get { return string.Empty; }
		}

		public override string LimitDelete {
			get { return string.Format ("LIMIT {0}", MaxDeletedRows); }
		}

		#region Initialization

		protected DataProvider (string server, string slaveServer, string user, string pass)
		{
			fieldsTable = new FieldTranslationCollection (ParameterPrefix, QuotationChar);
			InitFieldNames ();
			InitUpgradeTable ();

			Server = server;
			SlaveServer = slaveServer;
			User = user;
			Password = pass;
		}

		protected abstract void InitUpgradeTable ();

		#endregion

		#region General methods

		public override void Disconnect ()
		{
		}

		public override string GetAliasesString (params DataField [] fields)
		{
			StringBuilder sb = new StringBuilder ();

			foreach (DataField field in fields) {
				if (sb.Length > 0)
					sb.Append (",");

				DbField dbField = new DbField (field);
				string table = fieldsTable.GetFieldTable (field);
				sb.AppendFormat ("{0}{1}{2} as {3}", table, table.Length > 0 ? "." : string.Empty, fieldsTable.GetFieldName (dbField),
					fieldsTable.GetFieldAlias (dbField));
			}

			return sb.ToString ();
		}

		#region Database management

		public override string GetSchemaVersion (int productId = 1)
		{
			return ExecuteScalar<string> ("SELECT Version FROM system WHERE ProductID = @productId LIMIT 1",
				new DbParam ("productId", productId));
		}

		public override bool CheckCompatible (string sourceVersion)
		{
			EnsurePatchVersion ();

			return ProviderVersion == sourceVersion;
		}

		protected void EnsurePatchVersion (string dbName = null)
		{
			const string patchVersionKeyName = "WAPatchVersion";
			const int patchVersionUserId = -100;

			string value = GetConfigurationByKey (patchVersionKeyName, patchVersionUserId);
			Version patchVersion;
			if (string.IsNullOrWhiteSpace (value) || !Version.TryParse (value, out patchVersion))
				patchVersion = new Version (1, 0, 0, 0);

			Version version1 = new Version (1, 14, 9, 3);
			if (patchVersion < version1) {
				EnsureVersion1 (dbName);
				SetConfiguration (patchVersionKeyName, patchVersionUserId, version1.ToString ());
			}
		}

		protected abstract void EnsureVersion1 (string dbName = null);

		/// <summary>
		/// Gets the current date and time on the server.
		/// </summary>
		/// <returns></returns>
		public override DateTime Now ()
		{
			return ExecuteObject<DateTime> ("SELECT NOW()");
		}

		public override string GetScript (string scriptFile, IList<DbParam> parameters)
		{
			//return GetScriptFile (GetType ().Namespace + ".Scripts." + scriptFile, parameters);
			return GetScriptFileNoGzip (GetType ().Namespace + ".Scripts." + scriptFile, parameters);
		}

		protected string GetScriptFileNoGzip(string scriptFile, IList<DbParam> parameters)
		{
			Assembly resourceAssembly = Assembly.GetExecutingAssembly ();

			string script;
			scriptFile = scriptFile.Replace (".gz", "");
			using (Stream stream = resourceAssembly.GetManifestResourceStream (scriptFile)) {
				if (stream == null)
					throw new ArgumentException ("scriptFile");

				using (StreamReader sreader = new StreamReader (stream)) {
					try {
						script = sreader.ReadToEnd ();
					} catch (Exception ex) {
						throw ex;
					}
				}
			}

			foreach (string [] strings in GetParametersData ()) {
				DbParam param = new DbParam (strings [0], strings [1]);
				parameters.Add (param);
				script = script.Replace (strings [2], fieldsTable.GetParameterName (param.ParameterName));
			}

			return script;
		}

		protected string GetScriptFile (string scriptFile, IList<DbParam> parameters)
		{
			Assembly resourceAssembly = Assembly.GetExecutingAssembly ();

			string script;
			using (Stream stream = resourceAssembly.GetManifestResourceStream (scriptFile)) {
				if (stream == null)
					throw new ArgumentException ("scriptFile");

				using (GZipStream unzipStream = new GZipStream (stream, CompressionMode.Decompress))
				using (StreamReader sreader = new StreamReader (unzipStream)) {
					try {
						script = sreader.ReadToEnd ();
					} catch (Exception ex) {
						throw ex;
					}
				}
			}

			foreach (string [] strings in GetParametersData ()) {
				DbParam param = new DbParam (strings [0], strings [1]);
				parameters.Add (param);
				script = script.Replace (strings [2], fieldsTable.GetParameterName (param.ParameterName));
			}

			return script;
		}

		protected IEnumerable<string []> GetParametersData ()
		{
			return new []
				{
					new [] { "@defaultItem", translator.GetString ("Default item"), "'Служебна стока'" },
					new [] { "@count", translator.GetString ("pcs."), "'бр.'" },
					new [] { "@defaultGroup", translator.GetString ("Default group"), "'Служебна група'" },
					new [] { "@defaultLocation", translator.GetString ("Default location"), "'Служебен обект'" },
					new [] { "@defaultPartner", translator.GetString ("Default partner"), "'Служебен партньор'" },
					new [] { "@defaultCompany", translator.GetString ("Default company"), "'Служебна фирма'" },
					new [] { "@defaultUser", translator.GetString ("Default user"), "'Служебен потребител'" },
					new [] { "@baseVATGroup", translator.GetString ("VAT"), "'Основна ДДС група'" },
					new [] { "@paymentInCash", translator.GetString ("Payment in cash"), "'Плащане в брой'" },
					new [] { "@bankOrder", translator.GetString ("Bank order"), "'Превод по сметка'" },
					new [] { "@debitCreditCard", translator.GetString ("Debit/Credit card"), "'Дебитна/Кредитна карта'" },
					new [] { "@paymentByVoucher", translator.GetString ("Payment by voucher"), "'Плащане чрез ваучер'" }
				};
		}

		private List<KeyValuePair<string, string>> dateFeilds;

		protected List<KeyValuePair<string, string>> DateFeilds {
			get {
				return dateFeilds ?? (dateFeilds = new List<KeyValuePair<string, string>>
					{
						new KeyValuePair<string, string> ("applicationlog", "UserRealTime"),
						new KeyValuePair<string, string> ("cashbook", "Date"),
						new KeyValuePair<string, string> ("cashbook", "UserRealTime"),
						new KeyValuePair<string, string> ("currencieshistory", "Date"),
						new KeyValuePair<string, string> ("documents", "InvoiceDate"),
						new KeyValuePair<string, string> ("documents", "ExternalInvoiceDate"),
						new KeyValuePair<string, string> ("documents", "TaxDate"),
						new KeyValuePair<string, string> ("ecrreceipts", "ReceiptDate"),
						new KeyValuePair<string, string> ("ecrreceipts", "UserRealTime"),
						new KeyValuePair<string, string> ("lots", "EndDate"),
						new KeyValuePair<string, string> ("lots", "ProductionDate"),
						new KeyValuePair<string, string> ("operations", "Date"),
						new KeyValuePair<string, string> ("operations", "UserRealTime"),
						new KeyValuePair<string, string> ("partners", "UserRealTime"),
						new KeyValuePair<string, string> ("payments", "Date"),
						new KeyValuePair<string, string> ("payments", "UserRealTime"),
						new KeyValuePair<string, string> ("payments", "EndDate"),
						new KeyValuePair<string, string> ("registration", "UserRealTime"),
						new KeyValuePair<string, string> ("system", "LastBackup"),
						new KeyValuePair<string, string> ("transformations", "UserRealTime")
					});
			}
		}

		protected int AdjustSampleDatesCommandsNumber {
			get { return DateFeilds.Count * 2; }
		}

		#endregion

		#region Transactions management

		public override void SnapshotObject (object obj, bool replaceSnapshots = false)
		{
			TransactionContext context = TransactionContext.Current;
			if (context == null)
				return;

			context.SnapshotObject (obj, replaceSnapshots);
		}

		public override bool IsMasterScopeOpen {
			get { return ThreadServerContext != null; }
		}

		public override void BeginMasterScope ()
		{
			ServerContext context = ThreadServerContext;
			if (context != null) {
				context.BeganMasterScopes++;
				return;
			}

			int threadId = Thread.CurrentThread.ManagedThreadId;

			ThreadServerContext = new ServerContext (threadId);
		}

		public override void EndMasterScope ()
		{
			ServerContext context = ThreadServerContext;
			if (context == null)
				return;

			context.BeganMasterScopes--;
			if (context.BeganMasterScopes > 0)
				return;

			ThreadServerContext = null;
		}

		#endregion

		public override void BulkInsert (string table, string columns, IList<List<DbParam>> insertParams, string errorMessage = "")
		{
			if (insertParams.Count == 0)
				return;

			int start = 0;
			int remaining = insertParams.Count;
			while (remaining > 0) {
				int limit = Math.Min (MaxInsertedRows, remaining);
				StringBuilder insertBuilder = new StringBuilder ();
				List<DbParam> finalParameters = new List<DbParam> ();
				for (int i = start; i < start + limit; i++) {
					insertBuilder.Append ('(');
					List<DbParam> parameters = insertParams [i];
					foreach (DbParam parameter in parameters) {
						parameter.ParameterName += "x" + i.ToString (CultureInfo.InvariantCulture);
						insertBuilder.AppendFormat ("{0}, ", fieldsTable.GetParameterName (parameter.ParameterName));
					}
					finalParameters.AddRange (parameters);
					insertBuilder.Remove (insertBuilder.Length - 2, 2);
					insertBuilder.Append ("),");
				}

				insertBuilder.Remove (insertBuilder.Length - 1, 1);
				int result = ExecuteNonQuery (string.Format ("INSERT INTO {0} ({1}) VALUES {2}",
					table, columns, insertBuilder), finalParameters.ToArray ());

				if (result != limit)
					throw new Exception (errorMessage);

				start += limit;
				remaining -= limit;
			}
		}

		#endregion

		#region Cusom Config

		public override string [] GetAllCustomConfigProfiles (string category)
		{
			return ExecuteArray<string> ("SELECT DISTINCT(Profile) FROM customconfig WHERE Category = @category",
				new DbParam ("category", category)) ?? new string [0];
		}

		public override T [] GetCustomConfig<T> (string category, string profile)
		{
			List<ObjectsContainer<long, byte []>> data = ExecuteList<ObjectsContainer<long, byte []>> (@"
                SELECT ID as Value1, Data as Value2 FROM customconfig WHERE Category = @category AND Profile = @profile",
				new DbParam ("category", category), new DbParam ("profile", profile));

			if (data.Count == 0)
				return new T [0];

			if (data.Count > 1) {
				// Delete duplicates but leave the one with the longes data
				data = data.OrderByDescending (d => d.Value2.Length).ToList ();
				for (int i = 1; i < data.Count; i++)
					ExecuteNonQuery ("DELETE FROM customconfig WHERE ID = @id", new DbParam ("id", data [i].Value1));
			}

			string stringData;
			try {
				stringData = Encoding.UTF8.GetString (data [0].Value2);
			} catch (Exception ex) {
				throw new Exception (string.Format ("Error while decoding data: {0}", BitConverter.ToString (data [0].Value2)), ex);
			}

			try {
				return JsonConvert.DeserializeObject<T []> (stringData);
			} catch (Exception ex) {
				throw new Exception (string.Format ("Error while deserializing string: {0}", stringData), ex);
			}
		}

		public override void AddUpdateCustomConfig<T> (T [] configs, string category, string profile)
		{
			string dataString = JsonConvert.SerializeObject (configs);
			byte [] data = Encoding.UTF8.GetBytes (dataString);

			long? dataId = ExecuteScalar<long?> (string.Format ("SELECT ID FROM customconfig WHERE Category = @category AND Profile = @profile"),
				new DbParam ("category", category),
				new DbParam ("profile", profile));

			if (dataId == null)
				ExecuteNonQuery ("INSERT INTO customconfig (Category, Profile, Data) VALUES (@category, @profile, @data)",
					new DbParam ("category", category),
					new DbParam ("profile", profile),
					new DbParam ("data", data));
			else
				ExecuteNonQuery ("UPDATE customconfig SET Data = @data WHERE ID = @id", new DbParam ("id", dataId.Value), new DbParam ("data", data));
		}

		public override void DeleteCustomConfig (string category, string profile)
		{
			ExecuteNonQuery ("DELETE FROM customconfig WHERE Category = @category AND Profile = @profile",
				new DbParam ("category", category), new DbParam ("profile", profile));
		}

		#endregion

		#region Helper methods

		#region Helper methods

		public override long GetQueryRowsCount (string commandText, params DbParam [] parameters)
		{
			SelectBuilder sBuilder = new SelectBuilder (fieldsTable, commandText);

			// Optimize the query for fast execution
			for (int i = sBuilder.SelectClause.Count - 1; i >= 0; i--) {
				SelectColumnInfo cInfo = sBuilder.SelectClause [i];
				if (cInfo.IsGrouped || cInfo.IsAggregated ||
					sBuilder.HavingClause.Any (having => Regex.IsMatch (having.Key, string.Format (@"([^\w]*)(\w\.)*{0}([^\w]*)", cInfo.ColumnName))))
					continue;

				sBuilder.SelectClause.RemoveAt (i);
			}

			if (sBuilder.SelectClause.Count == 0)
				sBuilder.SelectClause.Add (new SelectColumnInfo ("1"));

			sBuilder.OrderByClause.Clear ();

			return ExecuteScalar<long> (string.Format ("SELECT COUNT(*) FROM ({0}) as CountsTable", sBuilder), parameters);
		}

		public override string GetQueryWithSort (string commandText, string sortKey, DbField sortField, SortDirection direction)
		{
			sortKey = fieldsTable.StripQuotationChars (sortKey);
			SelectBuilder sBuilder = new SelectBuilder (fieldsTable, commandText);

			foreach (SelectColumnInfo columnInfo in sBuilder.SelectClause) {
				if (sortField != null) {
					if (fieldsTable.GetFieldByAny (columnInfo.ColumnName) != sortField)
						continue;
				} else if (fieldsTable.StripQuotationChars (columnInfo.ColumnName) != sortKey)
					continue;

				sBuilder.OrderByClause.Insert (0, string.Format ("{0} {1}", columnInfo.SourceExpression, direction == SortDirection.Ascending ? "ASC" : "DESC"));
				break;
			}

			return sBuilder.ToString ();
		}

		public override string GetQueryWithFilter (string commandText, IList<DbField> filterFields, string filter, out DbParam [] pars)
		{
			SelectBuilder sBuilder = new SelectBuilder (fieldsTable, commandText);

			string [] filterKeys = filter.Split (new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			List<DbParam> p = new List<DbParam> ();

			foreach (string key in filterKeys) {
				StringBuilder condition = new StringBuilder ();

				DbParam par = GetFilterParam (p, key);
				p.Add (par);

				foreach (DbField field in filterFields) {
					SelectColumnInfo colInfo = sBuilder.SelectClause [field];
					if (colInfo == null)
						throw new ArgumentException (string.Format ("Cannot search for field {0} as it is not populated from the database", field));

					if (condition.Length > 0)
						condition.Append (" OR ");

					condition.AppendFormat (GetFilterCondition (colInfo.SourceExpression, par));
				}

				sBuilder.WhereClause.Add (new KeyValuePair<string, ConditionCombineLogic> (string.Format ("({0})", condition), ConditionCombineLogic.And));
			}

			pars = p.ToArray ();
			return sBuilder.ToString ();
		}

		protected virtual DbParam GetFilterParam (List<DbParam> p, string key)
		{
			return new DbParam ("fPar" + p.Count, key);
		}

		protected virtual string GetFilterCondition (string source, DbParam parameter)
		{
			return string.Format ("({0} LIKE {1})",
				source, GetConcatStatement ("'%'", fieldsTable.GetParameterName (parameter.ParameterName), "'%'"));
		}

		public override SqlHelper GetSqlHelper ()
		{
			return new SqlHelper (fieldsTable);
		}

		public override string GetSelect (IEnumerable<DataField> fields, IDictionary<DataField, DataField> aliases = null)
		{
			fieldsTable.UseAltNames = true;
			StringBuilder selectBuilder = new StringBuilder ("SELECT ");
			foreach (DataField field in fields) {
				selectBuilder.Append (fieldsTable.GetFieldFullName (field));
				selectBuilder.Append (" AS ");
				selectBuilder.Append (fieldsTable.GetFieldAlias (aliases != null && aliases.ContainsKey (field) ? aliases [field] : field));
				selectBuilder.Append (", ");
			}
			selectBuilder.Remove (selectBuilder.Length - 2, 2);
			fieldsTable.UseAltNames = false;
			return selectBuilder.ToString ();
		}

		public override SelectBuilder GetSelectBuilder (string select = null)
		{
			return new SelectBuilder (fieldsTable, select);
		}

		public virtual string GetConcatStatement (params string [] values)
		{
			return values.Length == 0 ?
				"''" :
				string.Format ("concat({0})", string.Join (", ", values));
		}

		#endregion

		#region Execute DataQuery

		public override DataQueryResult ExecuteDataQuery (DataQuery querySet, string query, params DbParam [] pars)
		{
			SqlHelper helper = GetSqlHelper ();
			helper.AddParameters (pars);

			DataQueryResult queryResult;
			query = GetQuery (helper, querySet, query, out queryResult, true);

			int sortColumnOrder = -1;
			SortDirection sortDirection = SortDirection.Ascending;
			if (querySet.OrderBy != null) {
				for (int i = 0; i < queryResult.Columns.Length; i++) {
					if (queryResult.Columns [i].Field != querySet.OrderBy)
						continue;

					sortColumnOrder = i;
					sortDirection = querySet.OrderDirection;
					break;
				}
			}

			try {
				LazyTableModel res = new LazyTableModel (this, false, query, helper.Parameters);
				if (sortColumnOrder >= 0)
					res.Sort (res.Columns [sortColumnOrder], sortDirection);

				queryResult.Result = res;
			} catch (DbConnectionLostException) {
				throw;
			} catch (Exception ex) {
				throw new Exception (string.Format ("Error occurred while executing DataQuerySet with query: \"{0}\"", query), ex);
			}

			return queryResult;
		}

		public override LazyListModel<T> ExecuteDataQuery<T> (DataQuery querySet, string query, params DbParam [] pars)
		{
			SqlHelper helper = GetSqlHelper ();
			helper.AddParameters (pars);

			DataQueryResult queryResult;
			query = GetQuery (helper, querySet, query, out queryResult, false);

			try {
				return new LazyListModel<T> (this, false, query, helper.Parameters);
			} catch (DbConnectionLostException) {
				throw;
			} catch (Exception ex) {
				throw new Exception (string.Format ("Error occurred while executing DataQuerySet with query: \"{0}\"", query), ex);
			}
		}

		private string GetQuery (SqlHelper helper, DataQuery querySet, string query, out DataQueryResult queryResult, bool removeHidden)
		{
			queryResult = new DataQueryResult (querySet);
			SelectBuilder sBuilder = new SelectBuilder (fieldsTable, query);
			Dictionary<DbField, SelectColumnInfo> selectDictionary = CreateDictionaryFromSelect (queryResult, sBuilder);
			helper.AddParameters (GenerateConditionStatements (queryResult, selectDictionary));
			if (removeHidden)
				sBuilder = RemoveHiddenColumns (queryResult, sBuilder, selectDictionary);

			return GenerateConditionString (queryResult, sBuilder);
		}

		#region Report query generation

		/// <summary>
		/// Generates a dictionary containing all the columns from the select that are renamed (using AS)
		/// the hashtable has the original values as values and the translated values as keys.
		/// </summary>
		/// <param name="querySet"></param>
		/// <param name="sBuilder"></param>
		/// <returns></returns>
		private Dictionary<DbField, SelectColumnInfo> CreateDictionaryFromSelect (DataQueryResult querySet, SelectBuilder sBuilder)
		{
			Dictionary<DbField, SelectColumnInfo> translation = new Dictionary<DbField, SelectColumnInfo> ();

			string [] columnNames = sBuilder.SelectClause.Select (cInfo => cInfo.ColumnName).ToArray ();

			querySet.Columns = fieldsTable.TranslateToColumnInfo (columnNames);
			querySet.MarkTranslated ();
			querySet.MarkHidden ();
			querySet.MarkPermanent ();

			for (int i = 0; i < querySet.Columns.Length; i++) {
				if (querySet.Columns [i].Field == null) {
					if (!querySet.Columns [i].IsTranslated)
						throw new Exception ("Unable to translate column " + columnNames [i]);
				} else {
					SelectColumnInfo cInfo = sBuilder.SelectClause [i];

					if (cInfo.IsRenamed)
						translation.Add (querySet.Columns [i].Field, cInfo);
				}
			}

			return translation;
		}

		/// <summary>
		/// Helper function for translating a filter to SQL sintax by using a template provided
		/// </summary>
		/// <param name="filter"></param>
		/// <param name="template"></param>
		/// <param name="dictionary"></param>
		private void TranslateFilter (DataFilter filter, string template, IDictionary<DbField, SelectColumnInfo> dictionary)
		{
			filter.SQLTranslation.Clear ();

			foreach (DbField field in filter.FilteredFields) {
				SelectColumnInfo cInfo;
				string translation = dictionary.TryGetValue (field, out cInfo) ?
					cInfo.SourceExpression :
					fieldsTable.GetFieldFullName (field);

				filter.SQLTranslation.Add (string.Format (template, translation));
			}
		}

		/// <summary>
		/// Translates the condition statements (filters) inside the query set to a sql and stores them
		/// inside the query set.
		/// </summary>
		/// <param name="querySet"></param>
		/// <param name="dictionary"></param>
		private DbParam [] GenerateConditionStatements (DataQueryResult querySet, IDictionary<DbField, SelectColumnInfo> dictionary)
		{
			List<DbParam> pars = new List<DbParam> ();

			foreach (DataFilter filter in querySet.Filters) {
				if (!filter.IsValid || filter.Values == null || filter.Values.Length == 0)
					continue;

				object [] values = filter.Values;
				string templateSource;
				switch (filter.Logic) {
				case DataFilterLogic.Less:
					templateSource = "({{0}} < @wPar{0})";
					break;

				case DataFilterLogic.LessOrEqual:
					templateSource = "({{0}} <= @wPar{0})";
					break;

				case DataFilterLogic.Greather:
					templateSource = "({{0}} > @wPar{0})";
					break;

				case DataFilterLogic.GreatherOrEqual:
					templateSource = "({{0}} >= @wPar{0})";
					break;

				case DataFilterLogic.ExactMatch:
					templateSource = "({{0}} = @wPar{0})";

					if (filter.FilteredFields.Any (f =>
						f == DataField.OperationType ||
						f == DataField.DocumentOperationType ||
						f == DataField.PaymentOperationType) &&
						values.Length > 0 && values [0] != null) {
						OperationType operType = (OperationType)values [0];
						switch (operType) {
						case OperationType.TransferIn:
						case OperationType.TransferOut:
							values = new object [] { OperationType.TransferIn, OperationType.TransferOut };
							break;

						case OperationType.ComplexRecipeMaterial:
						case OperationType.ComplexRecipeProduct:
							values = new object [] { OperationType.ComplexRecipeMaterial, OperationType.ComplexRecipeProduct };
							break;

						case OperationType.ComplexProductionMaterial:
						case OperationType.ComplexProductionProduct:
							values = new object [] { OperationType.ComplexProductionMaterial, OperationType.ComplexProductionProduct };
							break;
						}

						if (values.Length > 1)
							templateSource = "(({{0}} = @wPar{0}) OR ({{0}} = @wPar{1}))";
					}
					break;

				case DataFilterLogic.Contains:
					if (values [0] != null)
						values = values [0].ToString ().Split (new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Cast<object> ().ToArray ();

					List<string> sources = new List<string> ();
					for (int i = 0; i < values.Length; i++)
						sources.Add (string.Format ("({{{{0}}}} LIKE {0})", GetConcatStatement ("'%'", string.Format ("@wPar{{{0}}}", i), "'%'")));

					templateSource = sources.Count > 1 ? string.Format ("({0})", string.Join (" AND ", sources)) : sources [0];
					break;

				case DataFilterLogic.In:
					templateSource = string.Format ("({{{{0}}}} IN ({0}))",
						string.Join (", ", values.Select (o => o.ToString ())));
					break;

				case DataFilterLogic.StartsWith:
					templateSource = string.Format ("({{{{0}}}} LIKE {0})", GetConcatStatement ("@wPar{0}", "'%'"));
					break;

				case DataFilterLogic.EndsWith:
					templateSource = string.Format ("({{{{0}}}} LIKE {0})", GetConcatStatement ("'%'", "@wPar{0}"));
					break;

				case DataFilterLogic.InRange:
					if (values.Length < 2)
						continue;

					if (values [0] != null && values [1] != null)
						templateSource = "({{0}} >= @wPar{0}) AND ({{0}} <= @wPar{1})";
					else if (values [0] != null)
						templateSource = "({{0}} >= @wPar{0})";
					else
						templateSource = "({{0}} <= @wPar{0})";

					if (values.Length > 2)
						values = values.Take (2).ToArray ();

					break;

				case DataFilterLogic.InEntityGroup:
					// filtering by groups must include the subgroups
					// look for groups which code starts with the code of the group with a name that starts with the parameter
					string groupsTable = fieldsTable.GetFieldTable (filter.FilteredFields [0]);
					string originalTable = groupsTable.TrimEnd ('1', '2');
					List<string> codes = ExecuteList<string> (string.Format ("SELECT {0}.Code FROM {0} WHERE {0}.Name LIKE {1}",
						originalTable, GetConcatStatement ("'%'", "@Name", "'%'")),
						new DbParam ("Name", values [0]));

					if (codes.Count > 0) {
						StringBuilder temp = new StringBuilder ();
						foreach (string code in codes) {
							if (temp.Length > 0)
								temp.Append (" OR ");
							temp.AppendFormat ("({0}.Code LIKE '{1}%')", groupsTable, code);
						}

						templateSource = codes.Count > 1 ? string.Format ("({0})", temp) : temp.ToString ();
					} else
						templateSource = "1 = 0";
					break;

				case DataFilterLogic.MoreThanMinutesAgo:
					templateSource = GetMoreThanMinuteAgoFilter ();
					break;

				case DataFilterLogic.LessThanMinutesAgo:
					templateSource = GetLessThanMinuteAgoFilter ();
					break;

				default:
					continue;
				}

				TranslateFilter (filter, GetFilterTemplate (templateSource, pars, values), dictionary);
			}

			return pars.ToArray ();
		}

		protected virtual string GetMoreThanMinuteAgoFilter ()
		{
			return "DATE_SUB(NOW(), INTERVAL @wPar{0} MINUTE) > {{0}}";
		}

		protected virtual string GetLessThanMinuteAgoFilter ()
		{
			return "DATE_SUB(NOW(), INTERVAL @wPar{0} MINUTE) < {{0}}";
		}

		private string GetFilterTemplate (string source, List<DbParam> pars, params object [] values)
		{
			int countBase = pars.Count;
			List<int> counts = new List<int> ();
			for (int i = 0; i < values.Length; i++) {
				if (values [i] == null) {
					countBase--;
					continue;
				}

				counts.Add (countBase + i);
				pars.Add (new DbParam (string.Format ("@wPar{0}", countBase + i), values [i]));
			}

			return string.Format (source, counts.Cast<object> ().ToArray ());
		}

		/// <summary>
		/// Remove all the columns that are marked for removal from the select statement, the group by statement
		/// and the Columns array in the data query
		/// </summary>
		/// <param name="querySet"></param>
		/// <param name="sBuilder"></param>
		/// <param name="dictionary"></param>
		/// <returns></returns>
		private SelectBuilder RemoveHiddenColumns (DataQueryResult querySet, SelectBuilder sBuilder, Dictionary<DbField, SelectColumnInfo> dictionary)
		{
			List<DbField> toRemove = new List<DbField> ();
			List<DbField> toRemoveExtension = new List<DbField> ();
			SelectColumnInfo cInfo;
			int i;

			#region Generate a list with all the fields to remove

			foreach (DataFilter filter in querySet.Filters) {
				if (filter.ShowColumns)
					continue;

				foreach (DbField field in filter.FilteredFields) {
					toRemove.Add (field);

					// Add the source of that field using the SELECT dictionary if available
					// This is used to remove the column from the GROUP BY statement
					DbField foundDbField;
					if (dictionary.TryGetValue (field, out cInfo)) {
						foundDbField = fieldsTable.GetFieldByAny (cInfo.SourceField);
						if (foundDbField != field)
							toRemoveExtension.Add (foundDbField);
					}

					// Add all the fields that have this field for a source from the SELECT dictionary
					foreach (DbField dField in dictionary.Keys) {
						cInfo = dictionary [dField];
						foundDbField = fieldsTable.GetFieldByAny (cInfo.SourceField);
						if (foundDbField == field)
							toRemove.Add (dField);
					}
				}
			}

			#endregion

			#region Remove all the hidden columns from the select statement

			List<ColumnInfo> newColumns = new List<ColumnInfo> (querySet.Columns);
			for (i = sBuilder.SelectClause.Count - 1; i >= 0; i--) {
				DbField field = newColumns [i].Field;
				if (field == null || !toRemove.Contains (field))
					continue;

				newColumns.RemoveAt (i);
				sBuilder.SelectClause.RemoveAt (i);
			}
			querySet.Columns = newColumns.ToArray ();

			#endregion

			if (sBuilder.GroupByClause.Count > 0) {
				#region Remove all the hidden columns from the group by clause

				for (i = sBuilder.GroupByClause.Count - 1; i >= 0; i--) {
					DbField [] fields = fieldsTable.GetFieldsByName (sBuilder.GroupByClause [i]);

					if (fields.Length == 0) {
						DbField dbField = fieldsTable.GetFieldByAlias (sBuilder.GroupByClause [i]);
						if (dbField != null)
							fields = new [] { dbField };
						else {
							foreach (KeyValuePair<DbField, SelectColumnInfo> selectColumnInfo in dictionary) {
								if (selectColumnInfo.Value.SourceExpression != sBuilder.GroupByClause [i])
									continue;

								fields = new [] { selectColumnInfo.Key };
								break;
							}
							if (fields.Length == 0)
								throw new Exception (string.Format ("Unable to parse column \"{0}\" in GROUP BY statement in the query \"{1}\"", sBuilder.GroupByClause [i], sBuilder));
						}
					}

					foreach (DbField field in fields) {
						if (!toRemove.Contains (field) && !toRemoveExtension.Contains (field))
							continue;

						sBuilder.GroupByClause.RemoveAt (i);
						break;
					}
				}

				#endregion
			}

			return sBuilder;
		}

		/// <summary>
		/// Generate the final where clause from the already translated conditions in the query set.
		/// </summary>
		/// <param name="querySet"></param>
		/// <param name="sBuilder"></param>
		/// <returns></returns>
		private static string GenerateConditionString (DataQueryResult querySet, SelectBuilder sBuilder)
		{
			foreach (DataFilter filter in querySet.Filters) {
				if (!filter.IsValid)
					continue;

				if (filter.SQLTranslation.Count == 0)
					continue;

				StringBuilder condition = new StringBuilder ();
				for (int j = 0; j < filter.SQLTranslation.Count; j++) {
					if (j > 0)
						condition.Append (" OR ");

					condition.Append (filter.SQLTranslation [j]);
				}

				sBuilder.AddCondition (filter.CombineLogic, filter.SQLTranslation.Count > 1 ?
					string.Format ("({0})", condition) : condition.ToString ());
			}

			return sBuilder.ToString ();
		}

		#endregion

		#endregion

		#endregion
	}
}
