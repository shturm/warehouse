//
// DataQueryResult.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   08/15/2006
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

using Warehouse.Data.Model;

namespace Warehouse.Data
{
    public class DataQueryResult : DataQuery
    {
        #region Private fields

        private ColumnInfo [] columns;

        #endregion

        #region Public properties

        public ColumnInfo [] Columns
        {
            get { return columns; }
            set { columns = value; }
        }

        public LazyTableModel Result { get; set; }

        #endregion

        public DataQueryResult (DataQuery dataQuery)
        {
            filters.AddRange (dataQuery.Filters);
            translatedColumns.AddRange (dataQuery.TranslatedColumns);
            hiddenColumns.AddRange (dataQuery.HiddenColumns);
            permanentColumns.AddRange (dataQuery.PermanentColumns);
            idConstants.AddRange (dataQuery.IdConstants);
            OrderBy = dataQuery.OrderBy;
            OrderDirection = dataQuery.OrderDirection;
            VATIsIncluded = dataQuery.VATIsIncluded;
            UseLots = dataQuery.UseLots;
            Table = dataQuery.Table;
            IdFields = dataQuery.IdFields;
        }

        public void MarkTranslated ()
        {
            if (columns == null || translatedColumns.Count == 0)
                return;

            foreach (int translated in translatedColumns) {
                if (translated < columns.Length)
                    columns [translated].IsTranslated = true;
            }

            translatedColumns.Clear ();
        }

        public void MarkHidden ()
        {
            if (columns == null || hiddenColumns.Count == 0)
                return;

            foreach (int column in hiddenColumns) {
                if (column < columns.Length)
                    columns [column].IsHidden = true;
            }

            hiddenColumns.Clear ();
        }

        public void MarkPermanent ()
        {
            if (columns == null || permanentColumns.Count == 0)
                return;

            foreach (int column in permanentColumns) {
                if (column < columns.Length)
                    columns [column].IsPermanent = true;
            }

            permanentColumns.Clear ();
        }
    }
}
