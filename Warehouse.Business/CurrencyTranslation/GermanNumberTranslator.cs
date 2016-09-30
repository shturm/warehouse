// 
// GermanNumberTranslator.cs
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
    public class GermanNumberTranslator : NumberTranslator
    {
        protected override string GetSeparator ()
        {
            return string.Empty;
        }

        protected override string TranslatePart (string number, IList<string> place, int count, string dollars)
        {
            string right = Right (number, 3);
            if (right == "1" && count > 2)
                return string.Concat ("eine", place [count], dollars);

            return base.TranslatePart (number, place, count, dollars);
        }

        protected override string GetOne ()
        {
            return "ein";
        }

        protected override string GetTensAndOnes (string strTens, string tens)
        {
            string ones = ConvertDigit (Right (strTens, 1));
            if (string.IsNullOrEmpty (ones))
                return tens;

            if (string.IsNullOrEmpty (tens))
                return ones;

            return string.Concat (ones, Translator.GetString ("and"), tens);
        }
    }
}
