//
// Number.cs
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
using System.Globalization;
using Warehouse.Data.Calculator;

namespace Warehouse.Data
{
    public static class Number
    {
        private static readonly NumberFormatInfo numberFormat = new NumberFormatInfo { NumberGroupSeparator = string.Empty };
        private const double MinimalDifference = 0.000001;

        public static bool IsValidExpression (string text)
        {
            try {
                if (string.IsNullOrEmpty (text))
                    return false;

                RPNCalculator.EvaluateExpression (text);
                return true;
            } catch (ExpressionErrorException) {
                return false;
            }
        }

        public static double ParseExpression (string text)
        {
            try {
                if (string.IsNullOrEmpty (text))
                    return 0;

                return RPNCalculator.EvaluateExpression (text);
            } catch (ExpressionErrorException) {
                return 0;
            }
        }

        public static bool TryParseExpression (string text, out double value)
        {
            try {
                if (string.IsNullOrEmpty (text)) {
                    value = 0;
                    return false;
                }

                value = RPNCalculator.EvaluateExpression (text);
                return true;
            } catch (ExpressionErrorException) {
                value = 0;
                return false;
            }
        }

        public static double Round (double value, int precision)
        {
            if (double.IsNaN (value) || double.IsInfinity (value))
                return 0;

            try {
                if (precision >= 0)
                    return (double) Math.Round ((decimal) value, precision, MidpointRounding.AwayFromZero);

                double power = Math.Pow (10, Math.Abs (precision));
                return (int) Math.Round ((decimal) (value / power), MidpointRounding.AwayFromZero) * power;
            } catch (OverflowException) {
                if (precision >= 0)
                    return Math.Round (value, precision, MidpointRounding.AwayFromZero);

                double power = Math.Pow (10, Math.Abs (precision));
                return (int) Math.Round (value / power, MidpointRounding.AwayFromZero) * power;
            }
        }

        public static string ToString (double value)
        {
            return value.ToString ("N");
        }

        public static string ToString (double value, int precision)
        {
            return precision >= 0 ?
                value.ToString ("N" + precision) :
                Round (value, precision).ToString ("N0");
        }

        public static string ToIntString (double value)
        {
            numberFormat.NumberDecimalSeparator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
            return value.ToString ("N0", numberFormat);
        }

        public static string ToEditString (int value)
        {
            numberFormat.NumberDecimalSeparator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
            return value.ToString (numberFormat);
        }

        public static string ToEditString (double value)
        {
            numberFormat.NumberDecimalSeparator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
            return value.ToString (numberFormat);
        }

        public static string ToEditString (double value, int precision)
        {
            if (precision < 0)
                return ToEditString (value);

            numberFormat.NumberDecimalSeparator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
            return value.ToString ("N" + precision, numberFormat);
        }

        public static int GetNumberOfSignificantDigits (double value)
        {
            // Make sure there are no decimal digits
            while (Math.Abs (Math.Round (value) - value) > 0.000001)
                value *= 10;

            // Make sure there are no ending zeroes
            while (Math.Abs (Math.Round (value / 10) - value / 10) < 0.000001 && value > 0)
                value /= 10;

            int count = 0;
            while (!IsZero (value)) {
                value = Math.Round (value / 10);
                count++;
            }

            return count;
        }

        public static bool IsEqualTo (this double value1, double value2)
        {
            return Math.Abs (value1 - value2) < MinimalDifference;
        }

        public static bool IsZero (this double value)
        {
            return Math.Abs (value) < MinimalDifference;
        }
    }
}
