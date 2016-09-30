// 
// BarcodeEANGenerator.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at yahoo dot com>
// 
// Created:
//    14.01.2013
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
using System.IO;
using System.Text.RegularExpressions;
using NBarCodes;
using Warehouse.Business.Entities;

namespace Warehouse.Business.BarcodeGenerators
{
    public abstract class BarcodeEANGenerator : IBarcodeGenerator
    {
        public abstract int Length { get; }

        public string Generate (string prefix = "")
        {
            if (string.IsNullOrEmpty (prefix) || !Regex.IsMatch (prefix, @"^\d+$"))
                prefix = "20";
            else
                prefix = prefix.Substring (0, Math.Min (prefix.Length, Length - 1));

            long maxBarcode = Item.GetMaxBarcodeSubNumber (prefix, Length, prefix.Length, Length - prefix.Length - 1) + 1;

            string generated = prefix + maxBarcode.ToString (CultureInfo.InvariantCulture).PadLeft (Length - prefix.Length - 1, '0');
            if (generated.Length + 1 > Length)
                throw new InvalidDataException ("All bar-code combinations used.");

            return generated + new Modulo10Checksum ().Calculate (generated);
        }

        public string GenerateCustom (string format)
        {
            string filteredFormat = format.Replace (".", "");

            string prefix = string.Empty;
            int start = -1;
            int end = -1;
            for (int i = 0; i < filteredFormat.Length; i++) {
                char c = filteredFormat [i];

                if (c == '#') {
                    if (start < 0)
                        start = i;
                } else {
                    if (start >= 0) {
                        end = i;
                        break;
                    }

                    if (!char.IsDigit (c)) {
                        if (prefix.Length == 0)
                            throw new InvalidDataException (string.Format (Translator.GetString ("Please enter a valid prefix to generate {0} symbol barcode!"), Length));

                        break;
                    }

                    prefix += c;
                }
            }

            int generatedLength = end - start;
            string suffix = null;
            if (generatedLength < 1) {
                if (prefix.Length == 0)
                    throw new InvalidDataException (Translator.GetString ("Please enter at least one digit to be generated using the # (pound) sign!"));

                start = prefix.Length;
                generatedLength = Length - prefix.Length - 1;
            } else {
                if (filteredFormat.Length != Length)
                    throw new InvalidDataException (string.Format (Translator.GetString ("Please enter a valid format to generate {0} symbol barcode!"), Length));

                suffix = format.Substring (end);
            }

            long maxBarcode = Item.GetMaxBarcodeSubNumber (prefix, Length, start, generatedLength) + 1;

            string generated = prefix + maxBarcode.ToString (CultureInfo.InvariantCulture).PadLeft (generatedLength, '0');
            if (suffix == null)
                generated += new Modulo10Checksum ().Calculate (generated);
            else
                generated += suffix;

            if (generated.Replace (".", "").Length > Length)
                throw new InvalidDataException (Translator.GetString ("Barcode cannot be generated with the current settings. All barcode numbers with the specified prefix and type are in use."));

            return generated;
        }
    }
}
