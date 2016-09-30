//
// SelectColumnInfo.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/16/2007
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

namespace Warehouse.Data.SQL
{
    public class SelectColumnInfo
    {
        private string columnName;
        private string sourceExpression;
        private string sourceField;
        private bool isRenamed;
        private bool isGrouped;
        private bool isAggregated;

        public string Statement
        {
            get
            {
                if (isRenamed)
                    return string.Format ("{0} AS {1}", sourceExpression, columnName);
                else
                    return sourceExpression;
            }
            set
            {
                ParseStatement (value);
            }
        }

        public string ColumnName
        {
            get { return columnName; }
            set { columnName = value; }
        }

        public string SourceExpression
        {
            get { return sourceExpression; }
            set { sourceExpression = value; ParseSource (value); }
        }

        public string SourceField
        {
            get { return sourceField; }
            set { sourceField = value; }
        }

        public bool IsRenamed
        {
            get { return isRenamed; }
            set
            {
                isRenamed = value;
                if (!value) {
                    columnName = sourceExpression;
                }
            }
        }

        public bool IsGrouped
        {
            get { return isGrouped; }
        }

        public bool IsAggregated
        {
            get { return isAggregated; }
        }

        public bool IsHidden { get; set; }

        public SelectColumnInfo (string statement)
        {
            ParseStatement (statement);
        }

        private void ParseStatement (string statement)
        {
            string lcStatement = statement.ToLowerInvariant ();
            int asPos = lcStatement.LastIndexOf (" as ");

            if (asPos >= 0) {
                isRenamed = true;
                SourceExpression = statement.Substring (0, asPos).Trim ();
                columnName = statement.Substring (asPos + 4).Trim ();
            } else {
                isRenamed = false;
                SourceExpression = statement.Trim ();
                columnName = statement.Trim ();
            }
        }

        private void ParseSource (string statement)
        {
            string lcStatement = statement.ToUpperInvariant ();
            int casePos = lcStatement.IndexOf ("CASE ");
            int whenPos = lcStatement.IndexOf (" WHEN ");

            // If there is a CASE .. WHEN scenario with symbols between them
            if (casePos >= 0 && whenPos >= 0 && statement.Substring (casePos + 4, whenPos - casePos - 4).Trim ().Length != 0) {
                sourceField = statement.Substring (casePos + 5, whenPos - casePos - 5).Trim ();
            } else {
                sourceField = statement;
            }

            if (lcStatement.IndexOf ("DISTINCT ") >= 0)
                isGrouped = true;

            if (lcStatement.IndexOf ("MIN(") >= 0 ||
                lcStatement.IndexOf ("MAX(") >= 0 ||
                lcStatement.IndexOf ("AVG(") >= 0 ||
                lcStatement.IndexOf ("SUM(") >= 0 ||
                lcStatement.IndexOf ("COUNT(") >= 0)
                isAggregated = true;
        }

        public override string ToString ()
        {
            if (isRenamed) {
                return string.Format ("Column name: \"{0}\", source: \"{1}\", statement: \"{2}\"", columnName, sourceExpression, Statement);
            } else {
                return string.Format ("Column name: \"{0}\", statement: \"{1}\"", columnName, Statement);
            }
        }
    }
}
