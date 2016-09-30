// 
// NumberTranslator.cs
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
using System.Globalization;
using System.Text;

namespace Warehouse.Business.CurrencyTranslation
{
    public class NumberTranslator
    {
        /// <summary>
        /// This function will convert a currency to its word representation.
        /// </summary>
        /// <param name="amount">Source currency amount.</param>
        /// <param name="includeDollarVerbiage">If <c>true</c> outputs "X dollars and Y cents".</param>
        /// <param name="capitalize">Capitalizes the first word in the string.</param>
        /// <param name="allCaps">Converts the string to UPPER case. Takes precedence over <paramref name="capitalize" />.</param>
        /// <returns>The amount in English.</returns>
        public string Translate (double amount, bool includeDollarVerbiage = true, bool capitalize = false, bool allCaps = false)
        {
            bool isNegative = false;
            if (amount < 0d) {
                isNegative = true;
                amount = -amount;
            }

            string number = amount.ToString (CultureInfo.InvariantCulture);
            int decimalPlace = number.IndexOf (CultureInfo.InvariantCulture.NumberFormat.CurrencyDecimalSeparator, StringComparison.Ordinal);
            if (decimalPlace > 0 && number.Length == decimalPlace + 1)
                number = number.Substring (0, decimalPlace);

            string cents = string.Empty;
            if (decimalPlace > 0 && number.Length > decimalPlace + 1) {
                cents = ConvertCoins (number, decimalPlace);
                number = number.Substring (0, decimalPlace);
            }

            string [] place = {
                    string.Empty, 
                    string.Empty, 
                    GetSeparator () + Translator.GetString ("Thousand"),
                    GetSeparator () + Translator.GetString ("Million"),
                    GetSeparator () + Translator.GetString ("Billion"),
                    GetSeparator () + Translator.GetString ("Trillion"),
                    GetSeparator () + Translator.GetString ("Quadrillion"),
                    GetSeparator () + Translator.GetString ("Quintillion"),
                    GetSeparator () + Translator.GetString ("Sextillion")
                };

            string dollars = string.Empty;
            dollars = TranslateNumber (number, place, 1, dollars);
            if (!dollars.EndsWith (" "))
                dollars += " ";

            string zero = Translator.GetString ("Zero");

            if (includeDollarVerbiage) {
                if (cents.Length > 0) {
                    cents = GetTextDecimalSeparator () + cents;
                    dollars = dollars.TrimEnd ();
                }

                if (dollars.Trim ().Length == 0)
                    dollars = zero;
                dollars += cents + (!string.IsNullOrEmpty (cents) && cents == cents.TrimEnd () ? " " : string.Empty);
            }

            if (isNegative)
                dollars = string.Format ("{0} {1}", Translator.GetString ("minus"), dollars);

            dollars = dollars.Replace ("   ", GetSeparator ());
            if (allCaps)
                return dollars.ToUpper ();

            return (capitalize ?
                dollars.Substring (0, 1).ToUpper () + dollars.Substring (1, dollars.Length - 1).ToLower () :
                dollars.ToLower ()).Trim ();
        }

        private string ConvertCoins (string number, int decimalPlace)
        {
            string temp = number.Substring (decimalPlace + 1);
            StringBuilder centsBuilder = new StringBuilder ();
            int nonZeroIndex = 0;
            for (int i = 0; i < temp.Length; i++) {
                string digit = temp [i].ToString (CultureInfo.InvariantCulture);
                if (int.Parse (digit, CultureInfo.InvariantCulture) == 0) {
                    centsBuilder.Append (Translator.GetString ("Zero"));
                    centsBuilder.Append (this.GetSeparator ());
                }
                else {
                    nonZeroIndex = i;
                    break;
                }
            }
            centsBuilder.Append (Translate (double.Parse (temp.Substring (nonZeroIndex), CultureInfo.InvariantCulture), false));
            return centsBuilder.ToString ();
        }

        protected virtual string TranslateNumber (string number, string [] place, int count, string dollars)
        {
            while (number != string.Empty) {
                dollars = TranslatePart (number, place, count, dollars);
                number = number.Length > 3 ? number.Substring (0, number.Length - 3) : string.Empty;
                count++;
            }
            return dollars;
        }

        protected virtual string GetCurrency (double amount, RegionInfo region)
        {
            return string.Empty;
        }

        protected virtual string TranslatePart (string number, IList<string> place, int count, string dollars)
        {
            string hundredsValue = Right (number, 3);
            string hundreds = ConvertHundreds (Right (number, 3));
            if (hundreds != string.Empty)
                dollars = GetLargeUnit (hundreds, hundredsValue, place, count) + dollars;

            return dollars;
        }

        protected virtual string GetLargeUnit (string hundreds, string hundredsValue, IList<string> place, int count)
        {
            return string.Concat (hundreds, place [count], GetSeparator ());
        }

        protected virtual string GetTextDecimalSeparator ()
        {
            return string.Format (" {0} ", Translator.GetString ("point"));
        }

        protected string Right (string numStr, int length)
        {
            if (numStr.Length < length)
                length = numStr.Length;
            return numStr.Substring (numStr.Length - length);
        }

        protected string ConvertHundreds (string strNum)
        {
            string result = string.Empty;
            if (Convert.ToDecimal (strNum) == 0)
                return string.Empty;

            strNum = Right ("000" + strNum, 3);
            if (strNum.Substring (0, 1) != "0")
                result = GetHundreds (strNum);

            if (strNum.Substring (1, 1) != "0")
                result += ConvertTens (strNum.Substring (1, 2));
            else
                result += ConvertDigit (strNum.Substring (2, 1));

            return result;
        }

        protected virtual string GetHundreds (string strNum)
        {
            return ConvertDigit (strNum.Substring (0, 1)) + GetSeparator () + Translator.GetString ("Hundred") + GetSeparator ();
        }

        protected virtual string GetSeparator ()
        {
            return " ";
        }

        protected virtual string ConvertTens (string strTens)
        {
            string sTens = string.Empty;
            if (Convert.ToInt16 (strTens.Substring (0, 1)) == 1) {
                int nTens = Convert.ToInt16 (strTens);
                switch (nTens) {
                    case 10:
                        sTens = Translator.GetString ("Ten");
                        break;
                    case 11:
                        sTens = Translator.GetString ("Eleven");
                        break;
                    case 12:
                        sTens = Translator.GetString ("Twelve");
                        break;
                    case 13:
                        sTens = Translator.GetString ("Thirteen");
                        break;
                    case 14:
                        sTens = Translator.GetString ("Fourteen");
                        break;
                    case 15:
                        sTens = Translator.GetString ("Fifteen");
                        break;
                    case 16:
                        sTens = Translator.GetString ("Sixteen");
                        break;
                    case 17:
                        sTens = Translator.GetString ("Seventeen");
                        break;
                    case 18:
                        sTens = Translator.GetString ("Eighteen");
                        break;
                    case 19:
                        sTens = Translator.GetString ("Nineteen");
                        break;
                }
            } else {
                switch (Convert.ToInt16 (strTens.Substring (0, 1))) {
                    case 2:
                        sTens = Translator.GetString ("Twenty");
                        break;

                    case 3:
                        sTens = Translator.GetString ("Thirty");
                        break;

                    case 4:
                        sTens = Translator.GetString ("Forty");
                        break;

                    case 5:
                        sTens = Translator.GetString ("Fifty");
                        break;

                    case 6:
                        sTens = Translator.GetString ("Sixty");
                        break;

                    case 7:
                        sTens = Translator.GetString ("Seventy");
                        break;

                    case 8:
                        sTens = Translator.GetString ("Eighty");
                        break;

                    case 9:
                        sTens = Translator.GetString ("Ninety");
                        break;
                }
                sTens = GetTensAndOnes (strTens, sTens);
            }
            return sTens;
        }

        protected virtual string GetTensAndOnes (string strTens, string tens)
        {
            string ones = ConvertDigit (Right (strTens, 1));
            if (string.IsNullOrEmpty (ones)) 
                return tens;
            if (string.IsNullOrEmpty (tens))
                return ones;
            return string.Concat (tens, this.GetTensSeparator (), ones);
        }

        protected virtual string GetTensSeparator ()
        {
            return " ";
        }

        protected string ConvertDigit (string strDigit)
        {
            string sDigit;
            int nDigit = Convert.ToInt16 (strDigit);
            switch (nDigit) {
                case 1:
                    sDigit = GetOne ();
                    break;

                case 2:
                    sDigit = Translator.GetString ("Two");
                    break;

                case 3:
                    sDigit = Translator.GetString ("Three");
                    break;

                case 4:
                    sDigit = Translator.GetString ("Four");
                    break;

                case 5:
                    sDigit = Translator.GetString ("Five");
                    break;

                case 6:
                    sDigit = Translator.GetString ("Six");
                    break;

                case 7:
                    sDigit = Translator.GetString ("Seven");
                    break;

                case 8:
                    sDigit = Translator.GetString ("Eight");
                    break;

                case 9:
                    sDigit = Translator.GetString ("Nine");
                    break;

                default:
                    sDigit = string.Empty;
                    break;
            }
            return sDigit;
        }

        protected virtual string GetOne ()
        {
            return Translator.GetString ("One");
        }
    }
}
