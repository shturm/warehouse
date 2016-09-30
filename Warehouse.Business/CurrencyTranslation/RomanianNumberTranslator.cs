// 
// RomanianNumberTranslator.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
// 
// Created:
//   6.6.2010
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

namespace Warehouse.Business.CurrencyTranslation
{
    public class RomanianNumberTranslator : NumberTranslator
    {
        protected override string GetHundreds (string strNum)
        {
            string hundreds = strNum.Substring (0, 1);
            switch (hundreds) {
                case "1":
                    return string.Format ("o {0}{1}", Translator.GetString ("Hundred"), GetSeparator ());
                default:
                    return string.Concat (ConvertDigit (hundreds), GetSeparator (), 
                        Translator.GetString ("Hundred").TrimEnd ('a'), 'e', GetSeparator ());
            }
        }

        protected override string GetLargeUnit (string hundreds, string hundredsValue, IList<string> place, int count)
        {
            if (count < 2)
                return base.GetLargeUnit (hundreds, hundredsValue, place, count);
            int hundredsNumber = int.Parse (hundredsValue);
            string unit = place [count];
            if (hundredsNumber > 1)
                if (count == 2)
                    unit = unit.Substring (0, unit.Length - 1) + "і";
                else
                    unit = unit.Substring (0, unit.Length - 1) + "ane";
            string preposition = GetPreposition (hundredsNumber);
            if (string.IsNullOrEmpty (preposition))
                return string.Concat (hundreds, unit, GetSeparator ());
            return string.Concat (hundreds, GetSeparator (), preposition.Trim (), unit, GetSeparator ());
        }

        private static string GetPreposition (double amount)
        {
            return amount < 19 || Math.Floor (amount) != amount ? string.Empty : "de ";
        }

        protected override string GetTensSeparator ()
        {
            return string.Format (" {0} ", Translator.GetString ("and"));
        }
    }
}
