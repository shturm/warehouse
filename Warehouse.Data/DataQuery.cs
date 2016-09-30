//
// DataQuery.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/01/2009
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
using System.Xml.Serialization;

namespace Warehouse.Data
{
    public class DataQuery
    {
        #region Private fields

        protected readonly List<DataFilter> filters;
        protected readonly List<int> translatedColumns = new List<int> ();
        protected readonly List<int> hiddenColumns = new List<int> ();
        protected readonly List<int> permanentColumns = new List<int> ();
        protected readonly List<KeyValuePair<DataField, object>> idConstants = new List<KeyValuePair<DataField, object>> ();

        #endregion

        #region Public properties

        public List<DataFilter> Filters
        {
            get { return filters; }
        }

        [XmlIgnore]
        public List<int> TranslatedColumns
        {
            get { return translatedColumns; }
        }

        [XmlIgnore]
        public List<int> HiddenColumns
        {
            get { return hiddenColumns; }
        }

        [XmlIgnore]
        public List<int> PermanentColumns
        {
            get { return permanentColumns; }
        }

        [XmlIgnore]
        public IEnumerable<KeyValuePair<DataField, object>> IdConstants
        {
            get { return idConstants; }
        }

        public DbField OrderBy { get; set; }

        public SortDirection OrderDirection { get; set; }

        [XmlIgnore]
        public bool VATIsIncluded { get; set; }

        [XmlIgnore]
        public bool UseLots { get; set; }

        [XmlIgnore]
        public DbTable? Table { get; set; }

        [XmlIgnore]
        public DataField [] IdFields { get; set; }

        [XmlIgnore]
        public bool RoundPrices { get; set; }

        [XmlIgnore]
        public int CurrencyPrecission { get; set; }

        #endregion

        public DataQuery ()
        {
            filters = new List<DataFilter> ();
        }

        public DataQuery (params DataFilter [] filters)
        {
            this.filters = new List<DataFilter> (filters);
        }

        public void SetSimpleId (DbTable table, DataField idField, int fieldIndex)
        {
            Table = table;
            IdFields = new [] { idField };
            hiddenColumns.Clear ();
            hiddenColumns.Add (fieldIndex);
        }

        public void SetComplexId (DbTable table, DataField idField0, DataField idField1)
        {
            Table = table;
            IdFields = new [] { idField0, idField1 };
            hiddenColumns.Clear ();
        }

        public void SetComplexId (DbTable table, DataField idField, DataField constId, object constValue)
        {
            Table = table;
            IdFields = new [] { idField };
            hiddenColumns.Clear ();
            idConstants.Clear ();
            idConstants.Add (new KeyValuePair<DataField, object> (constId, constValue));
        }

        public DataQuery Clone ()
        {
            DataQuery ret = new DataQuery ();

            ret.filters.AddRange (filters);
            ret.OrderBy = OrderBy;
            ret.OrderDirection = OrderDirection;

            return ret;
        }
    }
}
