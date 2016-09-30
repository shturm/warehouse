//
// FilterChoose.cs
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
using System.Collections.Generic;
using Warehouse.Data;

namespace Warehouse.Business.Reporting
{
    public class FilterChoose : FilterBase
    {
        private readonly KeyValuePair<int, string> [] values;
        private object value;
        private long invalidValue = -1;

        public KeyValuePair<int, string> [] Values
        {
            get { return values; }
        }

        public object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public long InvalidValue
        {
            get { return invalidValue; }
            set { invalidValue = value; }
        }

        public FilterChoose (bool constantVisibility, bool columnVisible, KeyValuePair<int, string> [] values, DataFilterLabel filter, params DbField [] fNames)
            : base (constantVisibility, columnVisible, filter, fNames)
        {
            this.values = values;
            filterLogic = DataFilterLogic.ExactMatch;
        }

        public void SetData (object value)
        {
            this.value = value;
        }

        public override DataFilter GetDataFilter (params object [] objects)
        {
            if (value != null) {
                // If the value is numeric type and is less than 0 invalidate the filter
                TypeCode typeCode = Type.GetTypeCode (value.GetType ());
                if (TypeCode.Char <= typeCode && typeCode <= TypeCode.UInt64 && Convert.ToInt64 (value) == invalidValue)
                    value = null;
            }

            DataFilter dataFilter = base.GetDataFilter (value);
            dataFilter.IsValid = value != null;

            return dataFilter;
        }

        #region Overrides of FilterBase

        public override void SetDataFilter (DataFilter dataFilter)
        {
            value = dataFilter.Values.Length > 0 ? dataFilter.Values [0] : null;

            base.SetDataFilter (dataFilter);
        }

        public override string GetExplanation ()
        {
            string text = value != null ? value.ToString () : string.Empty;
            int intValue = Convert.ToInt32 (value);
            foreach (KeyValuePair<int, string> pair in values) {
                if (pair.Key != intValue)
                    continue;

                text = pair.Value;
                break;
            }

            return string.Format (Translator.GetString ("Values equal to \"{0}\""), text);
        }

        public override void Clear ()
        {
            base.Clear ();

            if (values != null && values.Length > 0)
                value = values [0].Key;
            else
                value = null;
        }

        #endregion
    }
}
