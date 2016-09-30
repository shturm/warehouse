//
// MOTranslationProvider.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   09.22.2011
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
using System.IO;
using System.Threading;
using Warehouse.Data;

namespace Warehouse.Business.MOTranslator
{
    public class MOTranslationProvider : ITranslationProvider
    {
        private string parserLocale;
        private MOParser parser;
        private const char endOfTransmissionChar = (char) 0x04;

        public void Init (string packageName, Thread thread, CultureInfo culture)
        {
            if (thread != null && !Equals (thread.CurrentCulture, culture)) {
                thread.CurrentCulture = culture;
                thread.CurrentUICulture = culture;
            }

            if (culture == null)
                return;

            if (TryLocale (packageName, culture.Name.Replace ('-', '_')))
                return;

            TryLocale (packageName, culture.TwoLetterISOLanguageName);
        }

        private bool TryLocale (string packageName, string locale)
        {
            if (parserLocale == locale)
                return true;

            string moFile = GetMoFileLocation (packageName, locale);
            if (File.Exists (moFile)) {
                parserLocale = locale;
                using (FileStream file = File.OpenRead (moFile))
                    parser = new MOParser (file);
                return true;
            }

            parserLocale = null;
            parser = null;
            return false;
        }

        private static string GetMoFileLocation (string packageName, string locale)
        {
            string moFile = StoragePaths.LocaleFolder;
            moFile = Path.Combine (moFile, locale);
            moFile = Path.Combine (moFile, "LC_MESSAGES");
            return Path.Combine (moFile, packageName + ".mo");
        }

        public string GetString (string message, string context = null)
        {
            string value;
            if (parser == null)
                return message;
            if (context == null) {
                if (parser.Dictionary.TryGetValue (message, out value))
                    return value;
            } else {
                if (parser.Dictionary.TryGetValue (context + endOfTransmissionChar + message, out value))
                    return value;
            }

            return message;
        }

        public string GetPluralString (string singleMsg, string pluralMsg, int number)
        {
            return GetString (singleMsg);
        }
    }
}
