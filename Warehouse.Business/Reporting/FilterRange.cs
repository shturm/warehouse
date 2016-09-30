//
// FilterRange.cs
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

using Warehouse.Data;

namespace Warehouse.Business.Reporting
{
    public class FilterRange : FilterBase
    {
        private string from;
        private string to;

        public string From
        {
            get { return from ?? string.Empty; }
        }

        public string To
        {
            get { return to ?? string.Empty; }
        }

        public FilterRange (bool constantVisibility, bool columnVisible, DataFilterLabel filter, params DbField [] fNames)
            : base (constantVisibility, columnVisible, filter, fNames)
        {
            FilterLogic = DataFilterLogic.InRange;
        }

        public bool ValidateFilter (string from, string to, bool skipNulls)
        {
            bool fromValid = ValidateField (from, skipNulls);
            bool toValid = ValidateField (to, skipNulls);

            if (skipNulls) {
                if (!fromValid || !toValid)
                    return false;
            } else {
                if (!fromValid && !toValid)
                    return false;
            }

            return true;
        }

        public void SetData (string from, string to)
        {
            this.from = from;
            this.to = to;
        }

        public override DataFilter GetDataFilter (params object [] objects)
        {
            DataFilter dataFilter = base.GetDataFilter (from, to);
            dataFilter.IsValid = ValidateFilter (from, to, false);

            return dataFilter;
        }

        #region Overrides of FilterBase

        public override void SetDataFilter (DataFilter dataFilter)
        {
            if (dataFilter.Values.Length < 1)
                return;
            from = GetFieldText (dataFilter.Values [0]);
            if (dataFilter.Values.Length < 2)
                return;
            to = GetFieldText (dataFilter.Values [1]);
            base.SetDataFilter (dataFilter);
        }

        public override string GetExplanation ()
        {
            if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace (to))
                return string.Format (Translator.GetString ("Between \"{0}\" and \"{1}\""), from, to);
            if (!string.IsNullOrWhiteSpace (from))
                return string.Format (Translator.GetString ("Starting from \"{0}\""), from);
            if (!string.IsNullOrWhiteSpace (to))
                return string.Format (Translator.GetString ("Up to \"{0}\""), to);
            return Translator.GetString ("Any values");
        }

        #endregion

        #region Overrides of FilterBase

        public override void Clear ()
        {
            base.Clear ();

            from = string.Empty;
            to = string.Empty;
        }

        #endregion
    }
}
