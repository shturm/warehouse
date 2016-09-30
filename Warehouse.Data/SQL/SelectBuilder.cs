//
// SelectBuilder.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/15/2007
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
using System.Text;
using System.Collections;

namespace Warehouse.Data.SQL
{
    public class SelectBuilder
    {
        private enum Blocks
        {
            Select,
            From,
            Where,
            GroupBy,
            Having,
            OrderBy,
            Limit,
            Count
        }

        #region Private fields

        private readonly FieldTranslationCollection fTrans;
        private readonly Dictionary<Blocks, string> blockPrefix = new Dictionary<Blocks, string> ();
        private readonly SelectColumnInfoCollection selectClause;
        private static readonly string [] aggFunctions = { "min(", "max(", "avg(", "sum(" };
        private string fromClause;
        private List<KeyValuePair<string, ConditionCombineLogic>> whereClause;
        private List<KeyValuePair<string, ConditionCombineLogic>> havingClause;
        private List<string> groupByClause;
        private List<string> orderByClause;
        private string limitClause;

        #endregion

        #region Public properties

        public SelectColumnInfoCollection SelectClause
        {
            get { return selectClause; }
        }

        public string FromClause
        {
            get { return fromClause; }
            set { fromClause = value; }
        }

        public List<KeyValuePair<string, ConditionCombineLogic>> WhereClause
        {
            get { return whereClause; }
        }

        public List<KeyValuePair<string, ConditionCombineLogic>> HavingClause
        {
            get { return havingClause; }
        }

        public List<string> GroupByClause
        {
            get { return groupByClause; }
            set { groupByClause = value; }
        }

        public List<string> OrderByClause
        {
            get { return orderByClause; }
        }

        public string LimitClause
        {
            get { return limitClause; }
            set { limitClause = value; }
        }

        #endregion

        internal SelectBuilder (FieldTranslationCollection fTrans, string query = null)
        {
            if (fTrans == null)
                throw new ArgumentNullException ("fTrans");

            this.fTrans = fTrans;
            InitPrefixes ();

            selectClause = new SelectColumnInfoCollection (fTrans);
            if (string.IsNullOrEmpty (query)) {
                fromClause = string.Empty;
                whereClause = new List<KeyValuePair<string, ConditionCombineLogic>> ();
                havingClause = new List<KeyValuePair<string, ConditionCombineLogic>> ();
                groupByClause = new List<string> ();
                orderByClause = new List<string> ();
            } else
                LoadQuery (query);
        }

        public void LoadQuery (string query)
        {
            #region Init block positions
            BitArray searchArea = GetBlockSearchArea (query);

            Dictionary<Blocks, int> blockPositions = new Dictionary<Blocks, int> ();
            blockPositions [Blocks.Select] = GetBlockLocation (query, searchArea, blockPrefix [Blocks.Select]);
            if (blockPositions [Blocks.Select] < 0)
                throw new Exception ("SELECT statement not found.");

            blockPositions [Blocks.From] = GetBlockLocation (query, searchArea, blockPrefix [Blocks.From]);
            if (blockPositions [Blocks.From] < 0)
                throw new Exception ("FROM statement not found.");

            blockPositions [Blocks.Where] = GetBlockLocation (query, searchArea, blockPrefix [Blocks.Where]);
            blockPositions [Blocks.GroupBy] = GetBlockLocation (query, searchArea, blockPrefix [Blocks.GroupBy]);
            blockPositions [Blocks.Having] = GetBlockLocation (query, searchArea, blockPrefix [Blocks.Having]);
            blockPositions [Blocks.OrderBy] = GetBlockLocation (query, searchArea, blockPrefix [Blocks.OrderBy]);
            blockPositions [Blocks.Limit] = GetBlockLocation (query, searchArea, blockPrefix [Blocks.Limit]);

            #endregion

            #region Init block values

            selectClause.Clear ();
            int startPos = blockPositions [Blocks.Select] + blockPrefix [Blocks.Select].Length;
            int len = GetBlockLen (Blocks.Select, blockPositions, query.Length);
            foreach (string statement in ParseSVs (query.Substring (startPos, len), ",")) {
                selectClause.Add (new SelectColumnInfo (statement));
            }

            startPos = blockPositions [Blocks.From] + blockPrefix [Blocks.From].Length;
            len = GetBlockLen (Blocks.From, blockPositions, query.Length);
            fromClause = query.Substring (startPos, len).Trim ();

            whereClause = GetConditionClause (query, Blocks.Where, blockPositions);
            groupByClause = GetClause (query, Blocks.GroupBy, blockPositions, ",");
            havingClause = GetConditionClause (query, Blocks.Having, blockPositions);
            orderByClause = GetClause (query, Blocks.OrderBy, blockPositions, ",");
            List<string> lClause = GetClause (query, Blocks.Limit, blockPositions, null);
            limitClause = lClause.Count > 0 ? lClause [0] : string.Empty;

            #endregion
        }

        private void InitPrefixes ()
        {
            blockPrefix.Add (Blocks.Select, "SELECT");
            blockPrefix.Add (Blocks.From, "FROM");
            blockPrefix.Add (Blocks.Where, "WHERE");
            blockPrefix.Add (Blocks.GroupBy, "GROUP BY");
            blockPrefix.Add (Blocks.Having, "HAVING");
            blockPrefix.Add (Blocks.OrderBy, "ORDER BY");
            blockPrefix.Add (Blocks.Limit, "LIMIT");
        }

        private List<string> GetClause (string query, Blocks block, IDictionary<Blocks, int> positions, string separator)
        {
            if (positions [block] < 0)
                return new List<string> ();

            int startPos = positions [block] + blockPrefix [block].Length;
            int len = GetBlockLen (block, positions, query.Length);
            if (string.IsNullOrEmpty (separator))
                return new List<string> (new [] { query.Substring (startPos, len).Trim () });

            return new List<string> (ParseSVs (query.Substring (startPos, len), separator));
        }

        private List<KeyValuePair<string, ConditionCombineLogic>> GetConditionClause (string query, Blocks block, IDictionary<Blocks, int> positions)
        {
            List<string> clause = GetClause (query, block, positions, " and ");
            List<KeyValuePair<string, ConditionCombineLogic>> ret = new List<KeyValuePair<string, ConditionCombineLogic>> ();
            foreach (string s in clause) {
                string [] orClauses = ParseSVs (s, " or ");
                if (orClauses.Length > 1)
                    ret.AddRange (orClauses.Select (orClause => new KeyValuePair<string, ConditionCombineLogic> (orClause, ConditionCombineLogic.Or)));
                else
                    ret.Add (new KeyValuePair<string, ConditionCombineLogic> (s, ConditionCombineLogic.And));
            }

            return ret;
        }

        private int GetBlockLen (Blocks block, IDictionary<Blocks, int> positions, int queryLen)
        {
            int endPos = -1;
            for (int i = (int) block + 1; i < (int) Blocks.Count; i++) {
                if (positions [(Blocks) i] >= 0) {
                    endPos = positions [(Blocks) i];
                    break;
                }
            }

            if (endPos < 0)
                endPos = queryLen;

            int startPos = positions [block] + blockPrefix [block].Length;

            return endPos - startPos;
        }

        private BitArray GetBlockSearchArea (string statement)
        {
            BitArray searchArea = new BitArray (statement.Length, true);
            int braces = 0;
            bool inQuotes = false;
            int i = 0;
            foreach (char ch in statement) {
                if (ch == '(')
                    braces++;
                else if (ch == ')')
                    braces--;
                else if (fTrans.IsQuotationChar (ch))
                    inQuotes = !inQuotes;

                if (braces != 0 || inQuotes)
                    searchArea [i] = false;

                i++;
            }

            return searchArea;
        }

        private int GetBlockLocation (string query, BitArray searchArea, string block)
        {
            string temp;

            List<int> positions = new List<int> ();
            string lcQuery = query.ToLowerInvariant ();
            block = block.ToLowerInvariant ();
            int pos = lcQuery.IndexOf (block);
            int offset = 0;

            while (pos >= 0) {
                char prefix = pos + offset > 0 ? lcQuery [pos + offset - 1] : ' ';
                char suffix = pos + offset + block.Length < lcQuery.Length - 1 ? lcQuery [pos + offset + block.Length] : ' ';
                if (!char.IsLetter (prefix) && !char.IsPunctuation (prefix) &&
                    !char.IsLetter (suffix) && !char.IsPunctuation (suffix))
                    positions.Add (pos + offset);

                offset += pos + block.Length;
                temp = lcQuery.Substring (offset);
                pos = temp.IndexOf (block);
            }

            for (int i = positions.Count - 1; i >= 0; i--) {
                pos = positions [i];
                if (!searchArea [pos]) {
                    positions.RemoveAt (i);
                    continue;
                }

                if (pos + block.Length < query.Length) {
                    if (IsBlockPart (query [pos + block.Length])) {
                        positions.RemoveAt (i);
                        continue;
                    }
                }

                if (pos > 0) {
                    if (IsBlockPart (query [pos - 1]))
                        positions.RemoveAt (i);
                }
            }

            if (positions.Count > 1)
                throw new Exception (string.Format ("The block {0} was found in more than one position in the query {1}", block, query));

            if (positions.Count == 0)
                return -1;

            return positions [0];
        }

        private bool IsBlockPart (char ch)
        {
            if (char.IsLetterOrDigit (ch))
                return true;

            if (fTrans.IsQuotationChar (ch))
                return true;

            return ch == '_';
        }

        private string [] ParseSVs (string statement, string separator)
        {
            BitArray searchArea = GetBlockSearchArea (statement);
            List<int> positions = new List<int> ();
            string lcStatement = statement.ToLowerInvariant ();
            separator = separator.ToLowerInvariant ();
            int curPos = lcStatement.IndexOf (separator);
            int offset = 0;

            while (curPos >= 0) {
                positions.Add (curPos + offset);
                offset += curPos + separator.Length;
                string temp = lcStatement.Substring (offset);
                curPos = temp.IndexOf (separator);
            }

            for (int i = positions.Count - 1; i >= 0; i--) {
                if (!searchArea [positions [i]])
                    positions.RemoveAt (i);
            }

            curPos = 0;
            List<string> ret = new List<string> ();
            foreach (int pos in positions) {
                ret.Add (statement.Substring (curPos, pos - curPos).Trim ());
                curPos = pos + separator.Length;
            }

            if (curPos < statement.Length - 1)
                ret.Add (statement.Substring (curPos).Trim ());

            return ret.ToArray ();
        }

        public void AddCondition (ConditionCombineLogic combineLogic, string statement)
        {
            string lcStatement = statement.ToLowerInvariant ();
            bool containsAggregations = aggFunctions.Any (lcStatement.Contains);
            var condition = new KeyValuePair<string, ConditionCombineLogic> (statement, combineLogic);

            if (containsAggregations)
                havingClause.Add (condition);
            else
                whereClause.Add (condition);
        }

        public override string ToString ()
        {
            StringBuilder select = new StringBuilder ();
            StringBuilder result = new StringBuilder ();

            #region Construct select

            foreach (SelectColumnInfo column in selectClause) {
                if (select.Length > 0)
                    select.Append (", ");

                select.Append (column.Statement);
            }

            if (select.Length == 0)
                throw new Exception ("SELECT statement is empty");

            #endregion

            if (string.IsNullOrEmpty (fromClause))
                throw new Exception ("FROM statement is empty");

            string where = GetConditionClauseString (whereClause);
            string groupBy = GetClauseString (groupByClause, ", ");
            string having = GetConditionClauseString (havingClause);
            string orderBy = GetClauseString (orderByClause, ", ");

            result.AppendFormat ("{0} {1}", blockPrefix [Blocks.Select], select);
            result.AppendFormat (" {0} {1}", blockPrefix [Blocks.From], fromClause);
            if (where.Length > 0)
                result.AppendFormat (" {0} {1}", blockPrefix [Blocks.Where], where);
            if (groupBy.Length > 0)
                result.AppendFormat (" {0} {1}", blockPrefix [Blocks.GroupBy], groupBy);
            if (having.Length > 0)
                result.AppendFormat (" {0} {1}", blockPrefix [Blocks.Having], having);
            if (orderBy.Length > 0)
                result.AppendFormat (" {0} {1}", blockPrefix [Blocks.OrderBy], orderBy);
            if (limitClause.Length > 0)
                result.AppendFormat (" {0} {1}", blockPrefix [Blocks.Limit], limitClause);

            return result.ToString ();
        }

        public int [] GetHiddenColumns ()
        {
            List<int> ret = new List<int> ();

            for (int i = 0; i < selectClause.Count; i++) {
                if (selectClause [i].IsHidden)
                    ret.Add (i);
            }

            return ret.ToArray ();
        }

        private static string GetClauseString (IEnumerable<string> clause, string separator)
        {
            StringBuilder sb = new StringBuilder ();

            foreach (string block in clause) {
                if (sb.Length > 0)
                    sb.Append (separator);

                sb.Append (block);
            }

            return sb.ToString ();
        }

        private static string GetConditionClauseString (List<KeyValuePair<string, ConditionCombineLogic>> conditions)
        {
            StringBuilder sb = new StringBuilder ();

            foreach (KeyValuePair<string, ConditionCombineLogic> condition in conditions.Where (c => c.Value == ConditionCombineLogic.And || c.Value == ConditionCombineLogic.AndNot)) {
                if (sb.Length > 0)
                    sb.Append (" AND ");

                sb.Append (condition.Value == ConditionCombineLogic.AndNot ?
                    string.Format (" NOT ({0})", condition.Key) : condition.Key);
            }

            StringBuilder ors = new StringBuilder ();
            foreach (KeyValuePair<string, ConditionCombineLogic> condition in conditions.Where (c => c.Value == ConditionCombineLogic.Or || c.Value == ConditionCombineLogic.OrNot)) {
                if (ors.Length > 0)
                    ors.Append (" OR ");

                ors.Append (condition.Value == ConditionCombineLogic.OrNot ?
                    string.Format (" NOT ({0})", condition.Key) : condition.Key);
            }

            if (ors.Length > 0) {
                if (sb.Length > 0)
                    sb.AppendFormat (" AND ({0})", ors);
                else
                    sb.Append (ors);
            }

            return sb.ToString ();
        }
    }
}
