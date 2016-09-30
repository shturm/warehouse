// 
// BulgarianNumberTranslator.cs
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
    public class BulgarianNumberTranslator : NumberTranslator
    {
        protected override string TranslateNumber (string number, string [] place, int count, string dollars)
        {
            string translation = base.TranslateNumber (number, place, count, dollars);
            int lastIndexOfSpace = translation.TrimEnd ().LastIndexOf (' ');
            int lastIndexOfAnd = translation.TrimEnd ().LastIndexOf (" и ");
            if (lastIndexOfSpace - 2 != lastIndexOfAnd && lastIndexOfSpace > 0)
                translation = translation.Insert (lastIndexOfSpace + 1, "и ");
            if (translation.EndsWith ("две ", StringComparison.OrdinalIgnoreCase))
                return translation.Remove (translation.Length - 2) + "а ";

            return translation;
        }

        protected override string GetHundreds (string strNum)
        {
            string hundreds = strNum.Substring (0, 1);
            switch (hundreds) {
                case "1":
                    return "сто" + GetSeparator ();
                case "2":
                case "3":
                    return string.Concat (ConvertDigit (hundreds), "ста", GetSeparator ());
                default:
                    return string.Concat (ConvertDigit (hundreds), "стотин", GetSeparator ());
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
                //if (hundredsNumber % 10 == 1)
                //    return string.Concat (hundreds, hundredsNumber == 11 ? string.Empty : " една", unit, GetSeparator ());

                //if (hundredsNumber % 10 == 2)
                //    return string.Concat (hundreds, hundredsNumber == 12 ? string.Empty : " две", unit, GetSeparator ());

                return string.Concat (hundreds, unit, GetSeparator ());
            }
            
            if (hundredsNumber == 1)
                return string.Concat (hundreds, unit, GetSeparator ());

            return string.Concat (hundreds, unit, "а", GetSeparator ());
        }

        protected override string GetTensSeparator ()
        {
            return " и ";
        }

        protected override string GetTextDecimalSeparator ()
        {
            return base.GetTextDecimalSeparator ().TrimStart ();
        }
    }
}
