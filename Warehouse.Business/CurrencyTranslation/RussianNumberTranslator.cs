// 
// RussianNumberTranslator.cs
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

using System.Collections.Generic;

namespace Warehouse.Business.CurrencyTranslation
{
    public class RussianNumberTranslator : NumberTranslator
    {
        protected override string GetHundreds (string strNum)
        {
            string hundreds = strNum.Substring (0, 1);
            switch (hundreds) {
                case "1":
                    return Translator.GetString ("Hundred") + GetSeparator ();
                case "2":
                    return string.Concat ("двести" + GetSeparator ());
                case "3":
                case "4":
                    return string.Concat (ConvertDigit (hundreds), "ста", GetSeparator ());
                default:
                    return string.Concat (ConvertDigit (hundreds), "сот", GetSeparator ());
            }
        }

        protected override string GetLargeUnit (string hundreds, string hundredsValue, IList<string> place, int count)
        {
            if (count < 2)
                return base.GetLargeUnit (hundreds, hundredsValue, place, count);
            int hundredsNumber = int.Parse (hundredsValue);
            string unit = place [count];
            if (count == 2) {
                if (int.Parse (hundredsValue) == 1)
                    return unit + GetSeparator ();
                unit = hundredsNumber == 1 ? unit : unit.Substring (0, unit.Length - 1) + "и";
                if (hundredsNumber % 10 == 1)
                    return string.Concat (hundreds + (hundredsNumber == 11 ? string.Empty : " одна"), unit, GetSeparator ());
                if (hundredsNumber % 10 == 2)
                    return string.Concat (hundreds + (hundredsNumber == 12 ? string.Empty : " две"), unit, GetSeparator ());
                if (hundredsNumber % 10 > 1 && hundredsNumber % 10 < 5)
                    return string.Concat (hundreds, unit, GetSeparator ());
                return string.Concat (hundreds, unit.Substring (0, unit.Length - 1), GetSeparator ());
            }
            if (hundredsNumber == 1)
                return string.Concat (hundreds, unit, GetSeparator ());
            if (hundredsNumber > 0 && hundredsNumber < 5)
                return string.Concat (hundreds, unit, "а", GetSeparator ());
            return string.Concat (hundreds, unit, "ов", GetSeparator ());
        }
    }
}
