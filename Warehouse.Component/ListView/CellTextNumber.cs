//
// CellTextNumber.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   10/18/2007
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
using Warehouse.Business;
using Warehouse.Data;
using Warehouse.Data.Calculator;

namespace Warehouse.Component.ListView
{
    public class CellTextNumber : CellText
    {
        private int fixedDigits;
        private int editFixedDigits;

        public int FixedDigits
        {
            get { return fixedDigits; }
            set { fixedDigits = value; }
        }

        public int EditFixedDigits
        {
            get { return editFixedDigits; }
            set { editFixedDigits = value; }
        }

        public CellTextNumber (string propertyName)
            : base (propertyName)
        {
            Alignment = Pango.Alignment.Right;
        }

        public override string ObjectToString (object obj)
        {
            long value = ParseLong (obj);
            if (value < -10)
                return Translator.GetString ("Draft");

            if (value == (int) OperationState.Pending)
                return Translator.GetString ("Pending");

            return Number.ToIntString (value).PadLeft (fixedDigits, '0');
        }

        protected override string ObjectToEditString (object obj)
        {
            return Number.ToEditString (ParseLong (obj)).PadLeft (editFixedDigits, '0');
        }

        private static long ParseLong (object obj)
        {
            if (obj is string && string.IsNullOrWhiteSpace ((string) obj))
                return 0;
            
            return Convert.ToInt64 (obj);
        }

        protected override object ParseObject (string text)
        {
            return string.IsNullOrEmpty (text) ? 0 : Number.ParseExpression (text);
        }
    }
}
