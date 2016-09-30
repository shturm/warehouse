//
// TableVisualizerSettings.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   06/22/2009
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

using System.Collections.Generic;
using Warehouse.Data;

namespace Warehouse.Presentation.Visualization
{
    public class TableVisualizerSettings : VisualizerSettingsBase
    {
        private bool showTotals;
        private DbField [] skippedColumns;

        public bool ShowTotals
        {
            get { return showTotals; }
            set
            {
                if (showTotals == value)
                    return;

                showTotals = value;
                isDirty = true;
            }
        }

        public DbField [] SkippedColumns
        {
            get { return skippedColumns; }
            set
            {
                if (ReferenceEquals (skippedColumns, value))
                    return;

                skippedColumns = value;
                isDirty = true;
            }
        }

        public TableVisualizerSettings ()
        {
            skippedColumns = new DbField [0];
        }

        public override object Clone ()
        {
            TableVisualizerSettings ret = (TableVisualizerSettings) MemberwiseClone();

            List<DbField> skipped = new List<DbField> ();
            foreach (DbField dbField in skippedColumns) {
                skipped.Add (new DbField (dbField.Field));
            }
            ret.skippedColumns = skipped.ToArray ();

            return ret;
        }
    }
}
