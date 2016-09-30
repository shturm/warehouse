//
// Percent.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/12/2006
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

namespace Warehouse.Business.Entities
{
    public static class Percent
    {
        public static bool IsValidExpression (string value)
        {
            if (value == null)
                value = string.Empty;

            return Number.IsValidExpression (value.Replace ("%", string.Empty).Trim ());
        }

        public static double ParseExpression (string value)
        {
            if (value == null)
                value = string.Empty;

            return Round (Number.ParseExpression (value.Replace ("%", string.Empty).Trim ()));
        }

        public static bool TryParseExpression (string text, out double value)
        {
            if (text == null)
                text = string.Empty;

            double val;
            bool ret = Number.TryParseExpression (text.Replace ("%", string.Empty).Trim (), out val);
            value = Round (val);

            return ret;
        }

        public static double Round (double value)
        {
            return Number.Round (value, BusinessDomain.AppConfiguration.PercentPrecision);
        }

        public static string ToString (double value)
        {
            return Number.ToString (value, BusinessDomain.AppConfiguration.PercentPrecision) + " %";
        }

        public static string ToString (double value, int precision)
        {
            return Number.ToString (value, precision) + " %";
        }

        public static string ToEditString (double value)
        {
            return Number.ToEditString (value, BusinessDomain.AppConfiguration.PercentPrecision) + " %";
        }

        public static bool IsVisiblyEqual (double value1, double value2)
        {
            return Round (Math.Abs (value1 - value2)) <= 0;
        }
    }
}
