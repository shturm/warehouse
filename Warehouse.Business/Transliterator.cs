// 
// Transliterator.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
// 
// Created:
//    06.02.2012
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

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Warehouse.Business
{
    public static class Transliterator
    {
        private static IDictionary<string, string> characterMappings;
        private static IDictionary<string, string> digraphMappings;

        public static void TransliterateProperties (object obj)
        {
            digraphMappings = GetDigraphMappings ();
            characterMappings = GetCharacterMappings ();
            Transliterate (obj);
            digraphMappings = null;
            characterMappings = null;
        }

        private static void Transliterate (object obj)
        {
            if (obj == null)
                return;
            foreach (PropertyInfo property in obj.GetType ().GetProperties ()) {
                if (!property.CanRead)
                    continue;
                object value = property.GetValue (obj, null);
                string text = value as string;
                if (text != null) {
                    if (property.CanWrite)
                        property.SetValue (obj, Transliterate (text), null);
                    continue;
                }
                IEnumerable enumerable = value as IEnumerable;
                if (enumerable != null)
                    foreach (object child in enumerable)
                        Transliterate (child);
            }
        }

        private static string Transliterate (string text)
        {
            bool allUpper = (text ?? string.Empty).All (c => !char.IsLetter (c) || char.IsUpper (c));
            StringBuilder textBuilder = new StringBuilder (text);
            foreach (KeyValuePair<string, string> digraphMapping in digraphMappings)
                textBuilder.Replace (digraphMapping.Key, digraphMapping.Value);
            string noDigraphs = textBuilder.ToString ();
            textBuilder.Clear ();
            foreach (char c in noDigraphs) {
                bool upper = char.IsUpper (c);
                string transliterated = Transliterate (char.ToLower (c));
                if (upper) {
                    if (transliterated.Length > 0)
                        textBuilder.Append (char.ToUpper (transliterated [0]));
                    if (transliterated.Length > 1)
                        if (allUpper)
                            textBuilder.Append (transliterated.Substring (1).ToUpper ());
                        else
                            textBuilder.Append (transliterated.Substring (1));
                } else
                    textBuilder.Append (transliterated);
            }
            HandleLanguageSpecifics (textBuilder);
            return textBuilder.ToString ();
        }

        private static void HandleLanguageSpecifics (StringBuilder textBuilder)
        {
            if (textBuilder.Length <= 0)
                return;
            switch (CultureInfo.CurrentCulture.TwoLetterISOLanguageName) {
                // Georgian is unicase
                case "ka":
                    if (textBuilder.Length > 0)
                        textBuilder [0] = char.ToUpper (textBuilder [0]);
                    break;
                case "bg":
                    textBuilder.Replace ("iya", "ia");
                    textBuilder.Replace ("Balgaria", "Bulgaria");
                    break;
            }
        }

        private static string Transliterate (char c)
        {
            string asString = c.ToString (CultureInfo.InvariantCulture);
            return characterMappings.ContainsKey (asString) ? characterMappings [asString] : asString;
        }

        private static IDictionary<string, string> GetDigraphMappings ()
        {
            return new Dictionary<string, string> { { "ու", "ow" } };
        } 

        private static IDictionary<string, string> GetCharacterMappings ()
        {
            bool isRussian = CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ru";
            bool isUkrainian = CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "uk";
            return new Dictionary<string, string>
                {
                    // Cyrillic
                    { "а", "a" },
                    { "б", "b" },
                    { "в", "v" },
                    // TODO: uk - h, gh (the second transliteration is used word-initially).
                    { "г", isUkrainian ? "h" : "g" },
                    { "ґ", "g" },
                    { "д", "d" },
                    { "е", "e" },
                    { "ё", "ë" },
                    // TODO: uk - ie, ye (the second transliteration is used word-initially).
                    { "є", "ie" },
                    { "ж", "ž" },
                    { "з", "z" },
                    { "и", isUkrainian ? "y" : "i" },
                    // TODO: uk - i, yi (the second transliteration is used word-initially).
                    { "ї", "yi" },
                    // TODO: uk - i, y (the second transliteration is used word-initially).
                    { "й", isRussian ? "j" : "i" },
                    { "к", "k" },
                    { "л", "l" },
                    { "м", "m" },
                    { "н", "n" },
                    { "о", "o" },
                    { "п", "p" },
                    { "р", "r" },
                    { "с", "s" },
                    { "т", "t" },
                    { "у", "u" },
                    { "ф", "f" },
                    { "х", isUkrainian ? "kh" : "h" },
                    { "ц", isRussian ? "c" : "ts" },
                    { "ч", isRussian ? "č" : "ch" },
                    { "ш", isRussian ? "š" : "sh" },
                    { "щ", isRussian ? "ŝ" : isUkrainian ? "sch" : "sht" },
                    { "ъ", isRussian ? "ʺ" : "a" },
                    { "ы", "y'" },
                    { "ь", isRussian ? "ʹ" : "y" },
                    // TODO: uk - iu, yu (the second transliteration is used word-initially).
                    { "ю", isRussian ? "û" : "yu" },
                    { "э", "è" },
                    // TODO: uk - ia, ya (the second transliteration is used word-initially).
                    { "я", isRussian ? "â" : "ya" },
                    // Georgian
                    { "ა", "a" },
                    { "ბ", "b" },
                    { "გ", "g" },
                    { "დ", "d" },
                    { "ე", "e" },
                    { "ვ", "v" },
                    { "ზ", "z" },
                    { "თ", "t" },
                    { "ი", "i" },
                    { "კ", "k'" },
                    { "ლ", "l" },
                    { "მ", "m" },
                    { "ნ", "n" },
                    { "ო", "o" },
                    { "პ", "p'" },
                    { "ჟ", "zh" },
                    { "რ", "r" },
                    { "ს", "s" },
                    { "ტ", "t'" },
                    { "უ", "u" },
                    { "ფ", "p" },
                    { "ქ", "k" },
                    { "ღ", "gh" },
                    { "ყ", "q'" },
                    { "შ", "sh" },
                    { "ჩ", "ch" },
                    { "ც", "ts" },
                    { "ძ", "dz" },
                    { "წ", "ts'" },
                    { "ჭ", "ch'" },
                    { "ხ", "kh" },
                    { "ჯ", "j" },
                    { "ჰ", "h" },
                    // Armenian
                    { "ա", "a" },
                    { "բ", "b" },
                    { "գ", "g" },
                    { "դ", "d" },
                    { "ե", "e" },
                    { "զ", "z" },
                    { "է", "ē" },
                    { "ը", "ë" },
                    { "թ", "t'" },
                    { "ժ", "ž" },
                    { "ի", "i" },
                    { "լ", "l" },
                    { "խ", "x" },
                    { "ծ", "ç" },
                    { "կ", "k" },
                    { "հ", "h" },
                    { "ձ", "j" },
                    { "ղ", "ġ" },
                    { "ճ", "č" },
                    { "մ", "m" },
                    { "յ", "y" },
                    { "ն", "n" },
                    { "շ", "š" },
                    { "ո", "o" },
                    { "չ", "č" },
                    { "պ", "p" },
                    { "ջ", "ǰ" },
                    { "ռ", "ṙ" },
                    { "ս", "s" },
                    { "վ", "v" },
                    { "տ", "t" },
                    { "ր", "r" },
                    { "ց", "c'" },
                    { "ւ", "w" },
                    { "փ", "p'" },
                    { "ք", "k'" },
                    { "օ", "ò" },
                    { "ֆ", "f" },
                    { "և", "ew" }
                };
        }
    }
}
