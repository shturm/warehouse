//
// Str.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/23/2008
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
using System.Linq;
using System.Text;
using Warehouse.Data.Calculator;

namespace Warehouse.Data
{
    public static class Str
    {
        public const string DigitsAlphabet = "0123456789";
        public const string HexAlphabet = "0123456789ABCDEF";
        public const string LettersAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string DigitsLettersAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string DigitsLettersCasesAlphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static string Trim (this string text, int maxLength, bool ellipsize = false)
        {
            if (text == null)
                return string.Empty;

            if (text.Length <= maxLength)
                return text;

            if (!ellipsize)
                return text.Substring (0, maxLength);

            return (text.Substring (0, Math.Max (maxLength - 3, 0)) + "...").Substring (0, maxLength);
        }

        public static string Wrap (this string input, int lineLength)
        {
            return string.Join ("\n", Wrap (input, lineLength, 2));
        }

        public static string [] Wrap (this string input, int lineLength, int maxLines)
        {
            if (input == null)
                return new string [0];

            List<string> strings = new List<string> (input.Replace ("\r", string.Empty).Split ('\n'));
            List<string> ret = new List<string> ();

            for (int i = 0; i < strings.Count && i < maxLines; i++) {
                // If the string is of a correct size then just use it as it is
                if (strings [i].Length <= lineLength) {
                    if (strings [i].Length > 0)
                        ret.Add (strings [i]);
                    continue;
                }

                ret.Add (strings [i].Substring (0, lineLength));

                if (i < strings.Count - 1) {
                    strings [i + 1] = strings [i].Substring (lineLength) + " " + strings [i + 1];
                } else {
                    strings.Add (strings [i].Substring (lineLength));
                }
            }

            return ret.ToArray ();
        }

        public static string AlignCenter (this string text, int charsPerLine)
        {
            if (text == null)
                return string.Empty;

            if (text.Length > charsPerLine)
                return text.Substring (0, charsPerLine);

            return text.PadLeft (((charsPerLine - text.Length) / 2) + text.Length);
        }

        public static string AlignRight (this string text, int charsPerLine)
        {
            if (text == null)
                return string.Empty;

            if (text.Length > charsPerLine)
                return text.Substring (0, charsPerLine);

            return text.PadLeft (charsPerLine);
        }

        public static string ToKeyValue (this string key, string value, int totalChars, char separator = ' ')
        {
            return key.PadRight (totalChars - value.Length, separator) + value;
        }

        public static string GetPattern (this string pattern, int length)
        {
            StringBuilder sb = new StringBuilder ();
            while (sb.Length < length)
                sb.Append (pattern);

            return sb.ToString (0, length);
        }

        public static string GetByteSize (this long size, bool shortFormat, long unitsLimit = 1000000)
        {
            int multiplier = 0;

            if (shortFormat) {
                while (size >= unitsLimit && multiplier < 4) {
                    size /= 1024;
                    multiplier++;
                }
            }

            StringBuilder sb = new StringBuilder ();
            switch (multiplier) {
                case 0:
                    sb.Append (" B");
                    break;
                case 1:
                    sb.Append (" KB");
                    break;
                case 2:
                    sb.Append (" MB");
                    break;
                case 3:
                    sb.Append (" GB");
                    break;
                default:
                    sb.Append (" TB");
                    break;
            }

            string sizeStr = size.ToString ();

            for (int i = 0; i < sizeStr.Length; i++) {
                if (i % 3 == 0 && i > 0)
                    sb.Insert (0, ',');

                sb.Insert (0, sizeStr [sizeStr.Length - i - 1]);
            }

            return sb.ToString ();
        }

        public static string GetTimeDifference (TimeSpan span)
        {
            return new DateTime ().AddMilliseconds (span.TotalMilliseconds).ToString ("HH:mm:ss.fff");
        }

        public static string ToFormatedString (this Guid g)
        {
            return ByteArrayToFormatedString (g.ToByteArray (), DigitsLettersAlphabet);
        }

        public static string ToFormatedStringOld (this byte [] bytes, string alphabet)
        {
            return bytes.ToStringOld (alphabet).GetFormattedString ();
        }

        public static string ByteArrayToFormatedString (byte [] bytes, string alphabet)
        {
            return ToString (bytes, alphabet).GetFormattedString ();
        }

        public static string GetFormattedString (this string str, int minimalLength = 25)
        {
            while (str.Length < minimalLength)
                str = str + '0';

            StringBuilder ret = new StringBuilder ();
            for (int i = 0; i < str.Length; i++) {
                if (i % 5 == 0 && i > 0)
                    ret.Append ('-');

                ret.Append (str [i]);
            }

            return ret.ToString ();
        }

        public static string ToStringOld (this byte [] bytes, string alphabet)
        {
            int alphabetLen = alphabet.Length;

            int temp = 0;
            StringBuilder ret = new StringBuilder ();
            for (int i = bytes.Length - 1; i >= 0 || temp != 0; i--) {
                if (i >= 0) {
                    temp *= 256;
                    temp += bytes [i];
                }
                do {
                    ret.Insert (0, alphabet [temp % alphabetLen]);
                    temp /= alphabetLen;
                } while (temp > alphabetLen * alphabetLen);
            }

            return ret.ToString ();
        }

        public static string Int32ToString (int value, string alphabet)
        {
            byte [] arr = new byte [4];
            arr [0] = (byte) ((value >> 24) & 0xFF);
            arr [1] = (byte) ((value >> 16) & 0xFF);
            arr [2] = (byte) ((value >> 8) & 0xFF);
            arr [3] = (byte) ((value >> 0) & 0xFF);

            return ToString (arr, alphabet, true);
        }

        /// <summary>
        /// Method to convert a byte array to string
        /// </summary>
        /// <param name="arr">The input array</param>
        /// <param name="alphabet">The alphabet to be used for string generation</param>
        /// <param name="cleanInsignificant">Set to <see langword="true"/> to remove the insignificant characters from the left</param>
        /// <returns></returns>
        public static string ToString (this byte [] arr, string alphabet, bool cleanInsignificant = false)
        {
            int bitLen = (int) Math.Log (alphabet.Length, 2);
            StringBuilder ret = new StringBuilder ();

            for (int i = 0; i < arr.Length * 8; i++) {
                byte t = 0;
                for (int j = 0; i < arr.Length * 8 && j < bitLen; j++, i++) {
                    bool bit = (arr [i / 8] & (1 << 7 - (i % 8))) != 0;

                    t |= (byte) ((bit ? 1 : 0) << (bitLen - j - 1));
                }

                i--;

                if (cleanInsignificant && ret.Length == 0 && t == 0)
                    continue;

                ret.Append (alphabet [t]);
            }

            if (ret.Length == 0)
                ret.Append (alphabet [0]);

            return ret.ToString ();
        }

        public static byte [] ToByteArray (this string str, string alphabet)
        {
            int alphabetLen = alphabet.Length;

            int temp = 0;
            List<byte> ret = new List<byte> ();
            for (int i = 0; i < str.Length; i++) {
                int index = alphabet.IndexOf (str [i]);
                if (index < 0)
                    continue;

                temp *= alphabetLen;
                temp += index;

                while (temp > 256 || (temp > 0 && i == str.Length - 1)) {
                    ret.Add ((byte) (temp % 256));
                    temp /= 256;
                }
            }

            return ret.ToArray ();
        }

        public static bool IsEqualTo (this string a, string b, bool ignoreNulls = false, Func<string, string> filter = null)
        {
            if (filter == null)
                filter = EmptyFilter;

            if (ignoreNulls)
                return filter ((a ?? string.Empty).Trim ()) == filter ((b ?? string.Empty).Trim ());

            if (a == null && b == null)
                return true;

            if (a == null || b == null)
                return false;

            return filter (a.Trim ()) == filter (b.Trim ());
        }

        private static string EmptyFilter (string input)
        {
            return input;
        }

        public static string Random (string alphabet, int length)
        {
            Random r = new Random ();
            StringBuilder ret = new StringBuilder ();
            for (int i = 0; i < length; i++)
                ret.Append (alphabet [r.Next (alphabet.Length - 1)]);

            return ret.ToString ();
        }

        /// <summary>
        /// Adds space between the camel case words in the input
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string CamelSpace (this string value)
        {
            return string.Join (" ", CamelSplit (value));
        }

        public static string [] CamelSplit (string value)
        {
            List<string> ret = new List<string> ();
            StringBuilder sb = new StringBuilder ();
            foreach (char c in value) {
                if (char.IsUpper (c) && sb.Length > 0) {
                    ret.Add (sb.ToString ());
                    sb = new StringBuilder ();
                }

                sb.Append (c);
            }

            if (sb.Length > 0)
                ret.Add (sb.ToString ());

            return ret.ToArray ();
        }

        public static string ToFirstUpper (this string value)
        {
            if (string.IsNullOrEmpty (value))
                return value;

            return char.ToUpperInvariant (value [0]) + (value.Length > 1 ? value.Substring (1) : string.Empty);
        }

        public static string ToFirstLower (this string value)
        {
            if (string.IsNullOrEmpty (value))
                return value;

            return char.ToLowerInvariant (value [0]) + (value.Length > 1 ? value.Substring (1) : string.Empty);
        }

        public static string ToFirstUpperWord (this string value)
        {
            if (string.IsNullOrEmpty (value))
                return value;

            StringBuilder ret = new StringBuilder ();
            bool wordStarted = false;
            foreach (char c in value) {
                if (char.IsWhiteSpace (c)) {
                    ret.Append (c);
                    wordStarted = false;
                } else if (wordStarted) {
                    ret.Append (char.ToLower (c));
                } else {
                    ret.Append (char.ToUpper (c));
                    wordStarted = true;
                }
            }

            return ret.ToString ();
        }

        public static void WriteDebugMessage (string message, params object [] args)
        {
            System.Diagnostics.Debug.WriteLine ("[{0}] {1}", DateTime.Now.ToString ("HH:mm:ss.fff"), string.Format (message, args));
        }

        public static string FilterControlCharacters (this string value)
        {
            StringBuilder res = new StringBuilder ();

            foreach (char ch in value.Where (ch => !Char.IsControl (ch)))
                res.Append (ch);

            return res.ToString ();
        }
    }
}
