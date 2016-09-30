//
// FieldTranslationEntry.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/02/2007
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
using System.Text.RegularExpressions;

namespace Warehouse.Data
{
    public class FieldTranslationEntry
    {
        private readonly string table = string.Empty;
        private readonly string name;
        private readonly bool isSimpleField;
        private readonly string altTable = string.Empty;
        private readonly string altName;
        private readonly bool isAltSimpleField;
        private readonly string alias = string.Empty;
        private readonly Regex simpleFieldMatch = new Regex (@"^([a-zA-Z0-9_-`]+)\.([a-zA-Z0-9_-`]+)$", RegexOptions.Compiled);

        public string Table
        {
            get { return table; }
        }

        public string Name
        {
            get { return name; }
        }

        public string FullName
        {
            get { return isSimpleField ? string.Format ("{0}.{1}", table, name) : name; }
        }

        public bool IsSimpleField
        {
            get { return isSimpleField; }
        }

        public string AltTable
        {
            get { return altTable; }
        }

        public string AltName
        {
            get { return altName; }
        }

        public string FullAltName
        {
            get { return isAltSimpleField ? string.Format ("{0}.{1}", altTable, altName) : altName; }
        }

        public bool IsAltSimpleField
        {
            get { return isAltSimpleField; }
        }

        public string Alias
        {
            get { return alias; }
        }

        public FieldTranslationEntry (string source, string altSource, string alias)
            : this (source, altSource)
        {
            if (alias == null)
                throw new ArgumentNullException ("alias");

            this.alias = alias;
        }

        public FieldTranslationEntry (string source, string altSource = null)
        {
            if (source == null)
                throw new ArgumentNullException ("source");

            Match match = simpleFieldMatch.Match (source);
            if (match.Success) {
                table = match.Groups [1].Value;
                name = match.Groups [2].Value;
                isSimpleField = isAltSimpleField = true;
            } else {
                table = string.Empty;
                name = source;
            }

            if (altSource != null) {
                match = simpleFieldMatch.Match (altSource);
                if (match.Success) {
                    altTable = match.Groups [1].Value;
                    altName = match.Groups [2].Value;
                    isAltSimpleField = true;
                } else {
                    altTable = string.Empty;
                    altName = altSource;
                }
            } else {
                altTable = table;
                altName = name;
            }
        }
    }
}
