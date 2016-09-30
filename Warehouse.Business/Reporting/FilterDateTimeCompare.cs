//
// FilterDateTimeCompare.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   09.30.2010
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
using Warehouse.Data;

namespace Warehouse.Business.Reporting
{
    public class FilterDateTimeCompare : FilterBase
    {
        private string text;

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        public FilterDateTimeCompare (bool constantVisibility, bool columnVisible, DataFilterLabel filter, params DbField [] fNames)
            : base (constantVisibility, columnVisible, filter, fNames)
        {
        }

        public bool ValidateFilter (string value, bool skipNulls)
        {
            if (skipNulls && string.IsNullOrWhiteSpace (value))
                return true;

            return GetFieldValue (DataType.DateTimeInterval, value) != null;
        }

        public void SetData (DataFilterLogic logic, string value)
        {
            filterLogic = logic;
            text = value;
        }

        public override DataFilter GetDataFilter (params object [] objects)
        {
            DataFilter dataFilter = base.GetDataFilter (text);
            dataFilter.Values = new [] { GetFieldValue (DataType.DateTimeInterval, text) };
            dataFilter.IsValid = ValidateFilter (text, false);

            return dataFilter;
        }

        #region Overrides of FilterBase

        public override void SetDataFilter (DataFilter dataFilter)
        {
            text = dataFilter.Values.Length > 0 && dataFilter.Values [0] != null ?
                GetFieldText (DataType.DateTimeInterval, dataFilter.Values [0]) : string.Empty;

            filterLogic = dataFilter.Logic;
            base.SetDataFilter (dataFilter);
        }

        public override string GetExplanation ()
        {
            switch (filterLogic) {
                case DataFilterLogic.MoreThanMinutesAgo:
                    return string.Format (Translator.GetString ("More than \"{0}\" minutes ago"), text);
                case DataFilterLogic.LessThanMinutesAgo:
                    return string.Format (Translator.GetString ("Less than \"{0}\" minutes ago"), text);
                default:
                    throw new ArgumentOutOfRangeException ();
            }
        }

        public override void Clear ()
        {
            base.Clear ();

            text = string.Empty;
            filterLogic = DataFilterLogic.MoreThanMinutesAgo;
        }

        #endregion
    }
}
