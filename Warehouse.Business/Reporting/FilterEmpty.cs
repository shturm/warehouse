//
// FilterEmpty.cs
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
    public class FilterEmpty : FilterBase
    {
        private string text;

        public FilterEmpty (bool constantVisibility, bool columnVisible, DataFilterLabel filter, params DbField [] fNames)
            : base (constantVisibility, columnVisible, filter, fNames)
        {
        }

        public bool ValidateFilter (string text, bool skipNulls)
        {
            return ValidateField (text, skipNulls);
        }

        public void SetData (string text)
        {
            this.text = text;
        }

        public override DataFilter GetDataFilter (params object [] objects)
        {
            DataFilter dataFilter = base.GetDataFilter (text);
            dataFilter.IsValid = ValidateField (text, false);

            return dataFilter;
        }

        #region Overrides of FilterBase

        public override string GetExplanation ()
        {
            return Translator.GetString ("No filter");
        }

        #endregion

        #region Overrides of FilterBase

        public override void Clear ()
        {
            base.Clear ();

            text = string.Empty;
        }

        #endregion
    }
}
