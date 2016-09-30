//
// Quantity.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
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

using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public static class Quantity
    {
        public static bool IsValidExpression (string value)
        {
            return Number.IsValidExpression (value);
        }

        public static double ParseExpression (string value)
        {
            return Round (Number.ParseExpression (value));
        }

        public static bool TryParseExpression (string text, out double value)
        {
            return Number.TryParseExpression (text, out value);
        }

        public static double Round (double value)
        {
            return Number.Round (value, BusinessDomain.AppConfiguration.QuantityPrecision);
        }

        public static string ToString (double value)
        {
            return Number.ToString (value, BusinessDomain.AppConfiguration.QuantityPrecision);
        }

        public static string ToString (double value, int precision)
        {
            return Number.ToString (value, precision);
        }

        public static string ToEditString (double value)
        {
            return Number.ToEditString (value, BusinessDomain.AppConfiguration.QuantityPrecision);
        }
    }
}