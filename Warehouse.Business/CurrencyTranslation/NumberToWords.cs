//
// NumberToWords.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   02.16.2010
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

using System.Globalization;
using System.Threading;

namespace Warehouse.Business.CurrencyTranslation
{
    public class NumberToWords
    {
        // The original class was written in Managed C++ and found on
        // http://www.thecodeproject.com/useritems/Convert_Currency_to_Words.asp

        /// <summary>
        /// This function will convert a currency to its word representation.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <returns>The amount in English.</returns>
        public static string Translate (double amount)
        {
            NumberTranslator currencyTranslator;
            switch (CultureInfo.CurrentCulture.TwoLetterISOLanguageName) {
                case "bg":
                    currencyTranslator = new BulgarianNumberTranslator ();
                    break;
                case "cs":
                    currencyTranslator = new CzechNumberTranslator ();
                    break;
                case "de":
                    currencyTranslator = new GeorgianNumberTranslator ();
                    break;
                case "hy":
                    currencyTranslator = new ArmenianNumberTranslator ();
                    break;
                case "ka":
                    currencyTranslator = new GeorgianNumberTranslator ();
                    break;
                case "pl":
                    currencyTranslator = new PolishNumberTranslator ();
                    break;
                case "ro":
                    currencyTranslator = new RomanianNumberTranslator ();
                    break;
                case "ru":
                    currencyTranslator = new RussianNumberTranslator ();
                    break;
                case "uk":
                    currencyTranslator = new UkrainianNumberTranslator ();
                    break;
                default:
                    currencyTranslator = new EnglishNumberTranslator ();
                    break;
            }
            return currencyTranslator.Translate (amount);
        }

        public static string TranslateInternational (double amount)
        {
            bool useSystemLocalization = BusinessDomain.AppConfiguration.UseSystemLocalization;
            BusinessDomain.AppConfiguration.UseSystemLocalization = false;
            string localization = BusinessDomain.AppConfiguration.Localization;
            BusinessDomain.AppConfiguration.Localization = "en";
            Translator.ResetCulture ();
            Translator.InitThread (Thread.CurrentThread);
            string result = Translate (amount);
            BusinessDomain.AppConfiguration.Localization = localization;
            BusinessDomain.AppConfiguration.UseSystemLocalization = useSystemLocalization;
            Translator.ResetCulture ();
            Translator.InitThread (Thread.CurrentThread);
            return result;
        }
    }
}
