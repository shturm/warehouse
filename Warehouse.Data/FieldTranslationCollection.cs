//
// FieldTranslationCollection.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   03/24/2007
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
using System.Linq;

namespace Warehouse.Data
{
    public class FieldTranslationCollection : Dictionary<DbField, FieldTranslationEntry>
    {
        private readonly Dictionary<string, DbField> aliasesLookup = new Dictionary<string, DbField> ();
        private readonly string parameterPrefix;
        private readonly char [] quotationChars;
        private bool useAltNames;

        public string ParameterPrefix
        {
            get { return parameterPrefix; }
        }

        public bool UseAltNames
        {
            get { return useAltNames; }
            set { useAltNames = value; }
        }

        public FieldTranslationCollection (string parameterPrefix, params char [] quotationChars)
        {
            this.parameterPrefix = parameterPrefix;
            this.quotationChars = quotationChars;
        }

        public void Add (DataField field, string source = null, string altSource = null)
        {
            string fieldName = Enum.GetName (typeof (DataField), field);
            FieldTranslationEntry entry = new FieldTranslationEntry (source ?? fieldName, altSource, fieldName);
            DbField dbField = new DbField (field);

            this [dbField] = entry;
            aliasesLookup [entry.Alias] = dbField;
        }

        public string StripQuotationChars (string fieldName)
        {
            return quotationChars.Aggregate (fieldName, (current, c) =>
                current.Replace (c.ToString (CultureInfo.InvariantCulture), string.Empty));
        }

        public bool IsQuotationChar (char ch)
        {
            return quotationChars.Any (c => ch == c);
        }

        public string GetParameterName (string parName)
        {
            return parName.StartsWith (parameterPrefix) ?
                parName :
                parameterPrefix + parName;
        }

        public string GetParameterName (DbField field)
        {
            FieldTranslationEntry entry = GetFieldEntry (field);
            return entry.IsSimpleField ?
                parameterPrefix + StripQuotationChars (useAltNames ? entry.AltName : entry.Name) :
                GetAlternativeParameterName (field);
        }

        public string GetAlternativeParameterName (DbField field)
        {
            return parameterPrefix + field.Field;
        }

        #region DataField to String translations

        public FieldTranslationEntry GetFieldEntry (DbField field)
        {
            if (field == null)
                throw new ArgumentNullException ("field");

            return ContainsKey (field) ?
                base [field] :
                new FieldTranslationEntry (field.Field);
        }

        public string GetFieldFullName (DbField field)
        {
            FieldTranslationEntry res = base [field];

            if (res == null)
                return string.Empty;

            return useAltNames ? res.FullAltName : res.FullName;
        }

        public string GetFieldName (DbField field)
        {
            if (field == null)
                return string.Empty;

            FieldTranslationEntry value;
            if (!TryGetValue (field, out value))
                value = new FieldTranslationEntry (field.Field);

            return useAltNames ? value.AltName : value.Name;
        }

        public string GetFieldAlias (DbField field)
        {
            FieldTranslationEntry res = base [field];

            if (res == null || string.IsNullOrEmpty (res.Alias))
                return string.Empty;

            return res.Alias;
        }

        public string GetFieldTable (DbField field)
        {
            FieldTranslationEntry res = base [field];

            if (res == null || res.Table == null)
                return string.Empty;

            return res.Table;
        }

        #endregion

        #region String to DataField translations

        public DbField GetFieldByName (string name)
        {
            FieldTranslationEntry entry;
            string entName;

            name = StripQuotationChars (name);
            int dotIndex = name.IndexOf (".");
            if (dotIndex >= 0) {
                string table = name.Substring (0, dotIndex);
                string colName = name.Substring (dotIndex + 1);

                foreach (DbField key in Keys) {
                    entry = base [key];
                    entName = useAltNames ? entry.AltName : entry.Name;
                    string entTable = useAltNames ? entry.AltTable : entry.Table;

                    if (StripQuotationChars (entName) == colName && entTable == table)
                        return key;
                }
            } else {
                foreach (DbField key in Keys) {
                    entry = base [key];
                    entName = useAltNames ? entry.AltName : entry.Name;

                    if (StripQuotationChars (entName) == name)
                        return key;
                }
            }

            return new DbField (name);
        }

        public DbField [] GetFieldsByName (string name)
        {
            List<DbField> ret = new List<DbField> ();
            FieldTranslationEntry value;
            string entName;

            name = StripQuotationChars (name);
            int dotIndex = name.IndexOf (".");
            if (dotIndex >= 0) {
                string table = name.Substring (0, dotIndex);
                name = name.Substring (dotIndex + 1);

                foreach (KeyValuePair<DbField, FieldTranslationEntry> pair in this) {
                    value = pair.Value;
                    entName = useAltNames ? value.AltName : value.Name;
                    string entTable = useAltNames ? value.AltTable : value.Table;

                    if (StripQuotationChars (entName) == name && entTable == table)
                        ret.Add (pair.Key);
                }
            } else {
                foreach (KeyValuePair<DbField, FieldTranslationEntry> pair in this) {
                    value = pair.Value;
                    entName = useAltNames ? value.AltName : value.Name;

                    if (StripQuotationChars (entName) == name)
                        ret.Add (pair.Key);
                }
            }

            return ret.ToArray ();
        }

        public DbField GetFieldByAlias (string alias)
        {
            DbField ret;
            return aliasesLookup.TryGetValue (alias, out ret) ? ret : null;
        }

        public DbField GetFieldByAny (string name)
        {
            // If no alias match is found fall back to names
            return GetFieldByAlias (name) ?? GetFieldByName (name);
        }

        #endregion

        public DbField [] TranslateToDbField (string [] columns, IList<DbColumnManager> columnManagers = null)
        {
            if (columns == null)
                throw new ArgumentNullException ("columns");

            DbField [] translation = new DbField [columns.Length];

            int i;
            for (i = 0; i < columns.Length; i++) {
                translation [i] = GetFieldByAlias (columns [i]);
            }

            for (i = 0; i < columns.Length; i++) {
                if (translation [i] != null)
                    continue;

                // Get all the possible fields that translate to that column name
                DbField [] fields = GetFieldsByName (columns [i]);

                // Filter the fields so only the ones present in the object are left
                if (fields.Length > 1 && columnManagers != null)
                    fields = fields.Where (f => columnManagers.Any (c => c.DbField == f)).ToArray ();

                if (fields.Length == 0)
                    continue;

                // Check for possible duplication in the translation
                bool duplicateFound = false;
                for (int j = 0; j < translation.Length && !duplicateFound; j++) {
                    for (int k = 0; k < fields.Length && !duplicateFound; k++) {
                        if (translation [j] == fields [k])
                            duplicateFound = true;
                    }
                }

                // If a duplicate is found don't translate the column as the alias translation is considered more correct
                if (duplicateFound)
                    continue;

                // If no duplicate is found and there is a more than one translation then raise an exception
                if (fields.Length > 1)
                    throw new Exception (string.Format ("Ambiguous translation of column {0}.", columns [i]));

                translation [i] = fields [0];
            }

            // Try to resolve the unresolved fields by alias if table name was added
            for (i = 0; i < columns.Length; i++) {
                if (translation [i] != null)
                    continue;

                string column = columns [i];
                int dotIndex = column.IndexOf (".", StringComparison.Ordinal);
                if (dotIndex <= 0 || dotIndex == column.Length - 1)
                    continue;

                column = column.Substring (dotIndex + 1).Trim (quotationChars);
                translation [i] = GetFieldByAlias (column);
            }

            return translation;
        }

        public ColumnInfo [] TranslateToColumnInfo (string [] columns)
        {
            return TranslateToDbField (columns)
                .Select (field => new ColumnInfo { Field = field })
                .ToArray ();
        }

        public void FindOrdinals (IDataReader dr, DbColumnManager [] columnManagers)
        {
            if (dr == null)
                return;

            string [] columns = new string [dr.FieldCount];
            for (int i = 0; i < dr.FieldCount; i++) {
                columns [i] = dr.GetName (i);
            }

            FindOrdinals (columns, columnManagers);
        }

        private void FindOrdinals (string [] columns, IList<DbColumnManager> columnManagers)
        {
            bool allFieldsAreStrong = true;
            int i;
            for (i = 0; i < columnManagers.Count; i++) {
                columnManagers [i].DbPosition = -1;
                if (columnManagers [i].DbField.StrongField == DataField.NotSet)
                    allFieldsAreStrong = false;
            }

            if (allFieldsAreStrong) {
                DbField [] translation = TranslateToDbField (columns, columnManagers);

                for (i = 0; i < translation.Length; i++) {
                    if (translation [i] == null)
                        continue;

                    int i1 = i;
                    foreach (DbColumnManager manager in columnManagers.Where (m => m.DbField == translation [i1])) {
                        manager.DbPosition = i;
                        break;
                    }
                }
            } else {
                foreach (DbColumnManager columnManager in columnManagers) {
                    FieldTranslationEntry fte = new FieldTranslationEntry (columnManager.DbField.Field);
                    for (i = 0; i < columns.Length; i++) {
                        string entName = useAltNames ? fte.AltName : fte.Name;
                        if (entName != columns [i])
                            continue;

                        columnManager.DbPosition = i;
                        break;
                    }
                }
            }

        }

        public void CheckAliases ()
        {
            FieldTranslationEntry entry1, entry2;
            DbField [] keys = new DbField [Keys.Count];

            Keys.CopyTo (keys, 0);

            for (int i = 0; i < keys.Length; i++) {
                entry1 = base [keys [i]];
                string ent1Name = useAltNames ? entry1.AltName : entry1.Name;
                for (int j = i + 1; j < keys.Length; j++) {
                    entry2 = base [keys [j]];
                    string ent2Name = useAltNames ? entry2.AltName : entry2.Name;
                    if (ent1Name != ent2Name)
                        continue;

                    if (entry1.Alias == entry2.Alias)
                        throw new Exception (string.Format ("Matching keys({0} and {1}) with same aliases found.", keys [i], keys [j]));
                }
            }
        }
    }
}
