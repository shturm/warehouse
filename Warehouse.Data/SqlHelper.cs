//
// SqlHelper.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   10.12.2007
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
using System.Text;

namespace Warehouse.Data
{
    public class SqlHelper : IDisposable
    {
        private static readonly DbField [] defaultTimeStamps = new []
            {
                new DbField (DataField.AppLogTimeStamp),
                new DbField (DataField.CashEntryTimeStamp),
                new DbField (DataField.CompanyCreationTimeStamp),
                new DbField (DataField.OperationTimeStamp),
                new DbField (DataField.PartnerTimeStamp),
                new DbField (DataField.PaymentTimeStamp)
            };

        private readonly List<DataField> skippedFields = new List<DataField> ();
        private List<KeyValuePair<object, object> []> tables = new List<KeyValuePair<object, object> []> ();
        private List<object> objects = new List<object> ();
        private List<List<DbColumnManager>> columnManagers = new List<List<DbColumnManager>> ();
        private List<DbParam> parameters = new List<DbParam> ();
        private FieldTranslationCollection fTrans;
        private List<DbField> timeStamps;
        private bool updateTimeStamp = true;

        public DbParam [] Parameters
        {
            get
            {
                if (parameters.Count == 0)
                    LoadObjectParameters ();

                return parameters.ToArray ();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to update the time stamp of the saved object.
        /// </summary>
        /// <value><c>true</c> if the time stamp of the saved object must be updated; otherwise, <c>false</c>.</value>
        public bool UpdateTimeStamp
        {
            get { return updateTimeStamp; }
            set { updateTimeStamp = value; }
        }

        public List<DbField> TimeStamps
        {
            get { return timeStamps ?? (timeStamps = new List<DbField> (defaultTimeStamps)); }
        }

        public bool UseAltNames
        {
            get { return fTrans.UseAltNames; }
            set { fTrans.UseAltNames = value; }
        }

        public virtual string CurrentDateTimeFunction
        {
            get { return "NOW()"; }
        }

        public virtual string CurrentDateFunction
        {
            get { return "CURDATE()"; }
        }

        public SqlHelper (FieldTranslationCollection fTrans)
        {
            if (fTrans == null)
                throw new ArgumentNullException ("fTrans");

            this.fTrans = fTrans;
        }

        public void AddObject (object obj)
        {
            if (obj == null)
                throw new ArgumentNullException ("obj");

            objects.Add (obj);
            LoadManagers (obj);
            ResetParameters ();
        }

        public void ChangeObject (object obj, params DataField [] skippedFieldsArray)
        {
            if (obj == null)
                throw new ArgumentNullException ("obj");

            if (objects.Count > 1)
                throw new InvalidOperationException ("You can only change a single object");

            skippedFields.Clear ();
            skippedFields.AddRange (skippedFieldsArray);
            if (objects.Count == 0 || obj.GetType () != objects [0].GetType ()) {
                columnManagers.Clear ();
                LoadManagers (obj);
            }
            objects.Clear ();
            objects.Add (obj);
            ResetParameters ();
        }

        public void AddParameters (params DbParam [] pars)
        {
            parameters.AddRange (pars);
        }

        private void LoadManagers (object obj)
        {
            Type objType = obj.GetType ();

            List<DbColumnManager> ret = (
                from propInfo in objType.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                from attrib in propInfo.GetCustomAttributes (true)
                let colAttrib = attrib as DbColumnAttribute
                where colAttrib != null && !colAttrib.ReadOnly
                select new DbColumnManager (colAttrib, propInfo) into manager
                where manager.CanRead
                select manager).ToList ();

            columnManagers.Add (ret);
        }

        private void LoadObjectParameters ()
        {
            for (int i = 0; i < columnManagers.Count; i++) {
                for (int j = 0; j < columnManagers [i].Count; j++) {
                    DbColumnManager manager = columnManagers [i] [j];
                    if (skippedFields.Contains (manager.DbField.StrongField))
                        continue;

                    string paramName = fTrans.GetParameterName (manager.DbField);
                    int ind = parameters.FindIndex (p => fTrans.GetParameterName (p.ParameterName) == paramName);
                    if (ind < 0)
                        parameters.Add (GetParameter (paramName, manager, objects [i]));
                    else {
                        string altParamName = fTrans.GetAlternativeParameterName (manager.DbField);
                        ind = parameters.FindIndex (p => fTrans.GetParameterName (p.ParameterName) == altParamName);
                        DbParam par = GetParameter (altParamName, manager, objects [i]);
                        if (ind < 0)
                            parameters.Add (par);
                        else
                            parameters [ind] = par;
                    }
                }
            }
        }

        protected virtual DbParam GetParameter (string paramName, DbColumnManager manager, object obj)
        {
            return new DbParam (paramName, manager.GetValue (obj, true));
        }

        public void ResetParameters ()
        {
            parameters.Clear ();
        }

        public void SetSkippedFields (params DataField [] fields)
        {
            skippedFields.Clear ();
            skippedFields.AddRange (fields);
            ResetParameters ();
        }

        public string GetSetStatement (params DataField [] skipFields)
        {
            return GetSetStatement (skipFields.Select (dataField => new DbField (dataField)).ToArray ());
        }

        public string GetSetStatement (params DbField [] skipFields)
        {
            StringBuilder ret = new StringBuilder ();
            List<DbField> fields = new List<DbField> ();

            foreach (List<DbColumnManager> managersList in columnManagers) {
                foreach (DbColumnManager manager in managersList) {
                    if (skipFields.Any (field => manager.DbField == field))
                        continue;

                    if (fields.Contains (manager.DbField))
                        continue;

                    ret.Append (ret.Length == 0 ? " SET " : ", ");

                    string fieldName = fTrans.GetFieldName (manager.DbField);
                    if (TimeStamps.Contains (manager.DbField) && updateTimeStamp)
                        ret.AppendFormat ("{0} = {1}", fieldName, CurrentDateTimeFunction);
                    else {
                        string paramName = fTrans.StripQuotationChars (fieldName);
                        ret.AppendFormat ("{0} = @{1}", fieldName, paramName);
                    }
                    fields.Add (manager.DbField);
                }
            }

            return ret.ToString ();
        }

        public string GetSelectStatement (params DbField [] skipFields)
        {
            StringBuilder ret = new StringBuilder ();
            List<DbField> usedFields = new List<DbField> ();

            foreach (List<DbColumnManager> managersList in columnManagers) {
                foreach (DbColumnManager manager in managersList) {
                    DbField field = manager.DbField;

                    bool skip = skipFields.Any (skipField => field == skipField);

                    if (skip)
                        continue;

                    if (usedFields.Contains (field))
                        continue;

                    if (ret.Length > 0)
                        ret.Append (",");

                    ret.Append (GetSelect (field));
                    usedFields.Add (manager.DbField);
                }
            }

            return ret.ToString ();
        }

        public string GetSelect (DbField field, AggregationType aggregation = AggregationType.None, bool useAlias = true)
        {
            string fieldName = fTrans.GetFieldName (field);

            string template = null;
            if (GetAggregationType (fieldName) == AggregationType.None)
                switch (aggregation) {
                    case AggregationType.Min:
                        template = "MIN({0})";
                        break;
                    case AggregationType.Max:
                        template = "MAX({0})";
                        break;
                    case AggregationType.Average:
                        template = "AVG({0})";
                        break;
                    case AggregationType.Sum:
                        template = "SUM({0})";
                        break;
                    case AggregationType.Count:
                        template = "COUNT({0})";
                        break;
                }

            if (template == null)
                template = "{0}";

            if (!fTrans.ContainsKey (field))
                return string.Format (template, field.Field);

            string table = fTrans.GetFieldTable (field);
            string name = string.Format (template, string.Format ("{0}{1}{2}", table, table.Length > 0 ? "." : string.Empty, fieldName));

            return useAlias ?
                string.Format ("{0} as {1}", name, fTrans.GetFieldAlias (field)) :
                name;
        }

        public string GetColumnsAndValuesStatement (params DataField [] skipFields)
        {
            return GetColumnsAndValuesStatement (skipFields.Select (f => new DbField (f)).ToArray ());
        }

        public string GetColumnsAndValuesStatement (params DbField [] skipFields)
        {
            StringBuilder values;
            StringBuilder columns;
            GetColumnsAndValues (out columns, out values, skipFields);

            return string.Format (" ({0}) VALUES ({1})", columns, values);
        }

        private void GetColumnsAndValues (out StringBuilder columns, out StringBuilder values, params DbField[] skipFields)
        {
            columns = new StringBuilder ();
            values = new StringBuilder ();
            List<DbField> fields = new List<DbField> ();

            foreach (List<DbColumnManager> managersList in columnManagers) {
                foreach (DbColumnManager manager in managersList) {
                    bool skip = skipFields.Any (field => manager.DbField == field);

                    if (skip)
                        continue;

                    if (fields.Contains (manager.DbField))
                        continue;

                    if (columns.Length > 0) {
                        columns.Append (", ");
                        values.Append (", ");
                    }

                    columns.Append (fTrans.GetFieldName (manager.DbField));
                    values.Append (TimeStamps.Contains (manager.DbField) && updateTimeStamp ?
                        CurrentDateTimeFunction : fTrans.GetParameterName (manager.DbField));
                    fields.Add (manager.DbField);
                }
            }
        }

        public string GetColumns (params DbField [] skipFields)
        {
            StringBuilder columns = new StringBuilder ();
            List<DbField> fields = new List<DbField> ();

            foreach (List<DbColumnManager> managersList in columnManagers) {
                foreach (DbColumnManager manager in managersList) {
                    bool skip = skipFields.Any (field => manager.DbField == field);

                    if (skip)
                        continue;

                    if (fields.Contains (manager.DbField))
                        continue;

                    if (columns.Length > 0)
                        columns.Append (", ");

                    columns.Append (fTrans.GetFieldName (manager.DbField));
                    fields.Add (manager.DbField);
                }
            }

            return columns.ToString ();
        }

        public AggregationType GetAggregationType (DbField field)
        {
            string select = GetSelect (field, AggregationType.None, false);

            return GetAggregationType (select);
        }

        private AggregationType GetAggregationType (string select)
        {
            select = select.Trim (' ', '\t', '(', ')').ToUpperInvariant ();
            if (select.StartsWith ("MIN("))
                return AggregationType.Min;
            if (select.StartsWith ("MAX("))
                return AggregationType.Max;
            if (select.StartsWith ("AVG("))
                return AggregationType.Average;
            if (select.StartsWith ("SUM("))
                return AggregationType.Sum;
            if (select.StartsWith ("COUNT("))
                return AggregationType.Count;
            return AggregationType.None;
        }

        public object GetParameterValue (string parName)
        {
            parName = fTrans.GetParameterName (parName);
            return Parameters
                .Where (par => fTrans.GetParameterName (par.ParameterName) == parName)
                .Select (par => par.Value)
                .FirstOrDefault ();
        }

        public object GetObjectValue (DataField field)
        {
            for (int i = 0; i < columnManagers.Count; i++) {
                foreach (DbColumnManager manager in columnManagers [i]) {
                    if (manager.DbField == field)
                        return manager.GetValue (objects [i], true);
                }
            }

            return null;
        }

        public void SetParameterValue (DataField field, object value)
        {
            SetParameterValue (fTrans.GetFieldName (field), value);
        }

        public void SetParameterValue (string parName, object value)
        {
            parName = fTrans.GetParameterName (parName);

            foreach (DbParam par in Parameters.Where (par => fTrans.GetParameterName (par.ParameterName) == parName))
                par.Value = value;
        }

        public void SetObjectValue (DataField field, object value)
        {
            for (int i = 0; i < columnManagers.Count; i++) {
                for (int j = 0; j < columnManagers [i].Count; j++) {
                    DbColumnManager manager = columnManagers [i] [j];
                    if (field != manager.DbField)
                        continue;

                    Type targetType = Nullable.GetUnderlyingType (manager.MemberType) ?? manager.MemberType;
                    object targetValue = value == null ? null : Convert.ChangeType (value, targetType);

                    manager.SetValue (objects [i], targetValue);
                    return;
                }
            }
        }

        public void SetObjectValue (string parName, object value)
        {
            parName = fTrans.GetParameterName (parName);

            for (int i = 0; i < columnManagers.Count; i++) {
                for (int j = 0; j < columnManagers [i].Count; j++) {
                    string fieldName = fTrans.GetParameterName (columnManagers [i] [j].DbField);
                    if (fieldName != parName)
                        continue;

                    columnManagers [i] [j].SetValue (objects [i], value);
                    return;
                }
            }
        }

        public string GetFieldAlias (DbField field)
        {
            return fTrans.GetFieldAlias (field);
        }

        public void Dispose ()
        {
            tables.Clear ();
            objects.Clear ();
            columnManagers.Clear ();
            parameters.Clear ();

            tables = null;
            objects = null;
            columnManagers = null;
            parameters = null;

            fTrans = null;
        }
    }
}
