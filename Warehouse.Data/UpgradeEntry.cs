//
// UpgradeEntry.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   02/22/2007
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

namespace Warehouse.Data
{
    public class UpgradeEntry
    {
        private readonly string sourceVersion;
        private readonly string targetVersion;
        private readonly string script;

        public string SourceVersion
        {
            get { return sourceVersion; }
        }

        public string TargetVersion
        {
            get { return targetVersion; }
        }

        internal UpgradeEntry (string sourceVersion, string targetVersion, string script)
        {
            this.sourceVersion = sourceVersion;
            this.targetVersion = targetVersion;
            this.script = script;
        }

        public void Execute (DataProvider dProvider, Action<double> callback)
        {
            List<DbParam> parameters = new List<DbParam> ();
            string s = dProvider.GetScript (script, parameters);
            string [] scriptLines = s.Split (new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            int j = 0;

            try {
                using (DbTransaction transaction = new DbTransaction (dProvider)) {
                    for (; j < scriptLines.Length; j++) {
                        string line = scriptLines [j];
                        line = line.Trim (' ', '\n', '\r', '\t', '\xA0', '\xD0');
                        if (line.Length > 0)
                            dProvider.ExecuteNonQuery (line, parameters.ToArray ());

                        if (callback != null)
                            callback (((double) (j * 100)) / (scriptLines.Length - 1));
                    }
                    transaction.Complete ();
                }
            } catch (Exception ex) {
                throw new SqlSyntaxException (scriptLines [j], j + 1, ex);
            }
        }
    }
}
