//
// Currency.cs
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
using System.Collections.Generic;
using Warehouse.Data;
using Warehouse.Data.Calculator;

namespace Warehouse.Business.Entities
{
    public enum PriceGroup
    {
        TradePrice = 0,
        RegularPrice = 1,
        PriceGroup1 = 2,
        PriceGroup2 = 3,
        PriceGroup3 = 4,
        PriceGroup4 = 5,
        PriceGroup5 = 6,
        PriceGroup6 = 7,
        PriceGroup7 = 8,
        PriceGroup8 = 9,
        TradeInPrice = 10,
        RegularPriceInOperation = 11
    }

    public enum PriceType
    {
        Sale,
        SaleTotal,
        Purchase,
        PurchaseTotal,
        Unknown
    }

    public static class Currency
    {
        public static bool IsValidExpression (string value)
        {
            return Number.IsValidExpression (value);
        }

        public static double ParseExpression (string value, PriceType type = PriceType.Sale)
        {
            return Round (Number.ParseExpression (value), type);
        }

        public static bool TryParseExpression (string text, out double value, PriceType type = PriceType.Sale)
        {
            double val;
            bool ret = Number.TryParseExpression (text, out val);
            value = Round (val, type);

            return ret;
        }

        public static double Round (double value, PriceType type = PriceType.Sale)
        {
            return Number.Round (value, GetPrecision (type));
        }

        public static string ToString (double value, PriceType type = PriceType.Sale)
        {
            return ToString (value, GetPrecision (type));
        }

        public static string ToString (double value, int precision, string currencySymbol = null, int currencySymbolPosition = 0)
        {
            value = Number.Round (value, precision);
            string ret = Number.ToString (Math.Abs (value), precision);
            if (currencySymbol == null || currencySymbolPosition == 0) {
                ConfigurationHolderBase config = BusinessDomain.AppConfiguration;
                currencySymbol = config.CurrencySymbol;
                currencySymbolPosition = config.CurrencySymbolPosition;
            }

            if (currencySymbol != null && currencySymbolPosition != 0)
                ret = currencySymbolPosition < 0 ?
                    currencySymbol + ret :
                    ret + currencySymbol;

            return value < 0 ?
                "-" + ret : ret;
        }

        public static string BankNotesString (double value)
        {
            return ToString (Math.Floor (value), 0);            
        }

        public static string CoinsString (double value)
        {
            return ToString ((value - Math.Floor (value)) * 100, 0);
        }

        public static string ToEditString (double value, PriceType type = PriceType.Sale)
        {
            return Number.ToEditString (value, GetPrecision (type));
        }

        public static int GetPrecision (PriceType type)
        {
            if (BusinessDomain.AppConfiguration == null)
                return 2;

            switch (type) {
                case PriceType.Sale:
                case PriceType.SaleTotal:
                    return BusinessDomain.AppConfiguration.CurrencyPrecision;
                case PriceType.Purchase:
                case PriceType.PurchaseTotal:
                    return BusinessDomain.AppConfiguration.PurchaseCurrencyPrecision;
                default:
                    return Math.Max (BusinessDomain.AppConfiguration.CurrencyPrecision, BusinessDomain.AppConfiguration.PurchaseCurrencyPrecision);
            }
        }

        public static KeyValuePair<int, string> [] GetAllSalePriceGroups ()
        {
            List<KeyValuePair<int, string>> salePriceGroups = new List<KeyValuePair<int, string>> (GetAllPriceGroups ());
            salePriceGroups.Remove (salePriceGroups.Find (k => k.Key == (int) PriceGroup.TradeInPrice));
            return salePriceGroups.ToArray ();
        }

        public static KeyValuePair<int, string> [] GetAllPriceGroups ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) PriceGroup.TradePrice, Translator.GetString ("Wholesale price")),
                    new KeyValuePair<int, string> ((int) PriceGroup.RegularPrice, Translator.GetString ("Retail price")),
                    new KeyValuePair<int, string> ((int) PriceGroup.PriceGroup1, Translator.GetString ("Price group 1")),
                    new KeyValuePair<int, string> ((int) PriceGroup.PriceGroup2, Translator.GetString ("Price group 2")),
                    new KeyValuePair<int, string> ((int) PriceGroup.PriceGroup3, Translator.GetString ("Price group 3")),
                    new KeyValuePair<int, string> ((int) PriceGroup.PriceGroup4, Translator.GetString ("Price group 4")),
                    new KeyValuePair<int, string> ((int) PriceGroup.PriceGroup5, Translator.GetString ("Price group 5")),
                    new KeyValuePair<int, string> ((int) PriceGroup.PriceGroup6, Translator.GetString ("Price group 6")),
                    new KeyValuePair<int, string> ((int) PriceGroup.PriceGroup7, Translator.GetString ("Price group 7")),
                    new KeyValuePair<int, string> ((int) PriceGroup.PriceGroup8, Translator.GetString ("Price group 8")),
                    new KeyValuePair<int, string> ((int) PriceGroup.TradeInPrice, Translator.GetString ("Purchase Price"))
                };
        }

        public static KeyValuePair<int, string> [] GetAllPriceRulePriceGroups ()
        {
            List<KeyValuePair<int, string>> salePriceGroups = new List<KeyValuePair<int, string>> (GetAllPriceGroups ())
                {
                    new KeyValuePair<int, string> ((int) PriceGroup.RegularPriceInOperation, Translator.GetString ("Sale price of the operation"))
                };
            return salePriceGroups.ToArray ();
        }

        public static KeyValuePair<int, string> [] GetAllPriceGroupFilters ()
        {
            List<KeyValuePair<int, string>> filters = new List<KeyValuePair<int, string>>
                {
                    new KeyValuePair<int, string> (-1, Translator.GetString ("All"))
                };

            filters.AddRange (GetAllPriceGroups ());

            return filters.ToArray ();
        }
    }
}
