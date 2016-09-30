//
// MOParser.cs
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Warehouse.Business.MOTranslator
{
    public struct MOHeader
    {
        public UInt32 MagicNumber;
        public UInt32 Revisoin;
        public UInt32 NumberOfStrings;
        public UInt32 OrignalTableOffset;
        public UInt32 TranslatedTableOffset;
        public UInt32 HashingTableSize;
        public UInt32 HashingTableOffset;
    }

    public class IndexEntry
    {
        public UInt32 OriginalOffset;
        public UInt32 OriginalLength;
        public UInt32 TranslatedOffset;
        public UInt32 TranslatedLength;
        public string OriginalString;
        public string TranslatedString;
    }

    public class MOParser
    {
        private const UInt32 magicNumber = 0x950412de;
        private readonly MOHeader header;
        private readonly Dictionary<string, string> dict = new Dictionary<string, string> ();

        public IDictionary<string, string> Dictionary
        {
            get { return dict; }
        }

        public MOParser (Stream input)
        {
            List<IndexEntry> indexes = new List<IndexEntry> ();

            using (BinaryReader br = new BinaryReader (input)) {
                header.MagicNumber = br.ReadUInt32 ();
                header.Revisoin = br.ReadUInt32 ();
                header.NumberOfStrings = br.ReadUInt32 ();
                header.OrignalTableOffset = br.ReadUInt32 ();
                header.TranslatedTableOffset = br.ReadUInt32 ();
                header.HashingTableSize = br.ReadUInt32 ();
                header.HashingTableOffset = br.ReadUInt32 ();

                if (header.MagicNumber != magicNumber)
                    return;

                input.Seek (header.OrignalTableOffset, SeekOrigin.Begin);
                for (int i = 0; i < header.NumberOfStrings; i++)
                    indexes.Add (new IndexEntry
                        {
                            OriginalLength = br.ReadUInt32 (),
                            OriginalOffset = br.ReadUInt32 ()
                        });

                input.Seek (header.TranslatedTableOffset, SeekOrigin.Begin);
                for (int i = 0; i < header.NumberOfStrings; i++) {
                    indexes [i].TranslatedLength = br.ReadUInt32 ();
                    indexes [i].TranslatedOffset = br.ReadUInt32 ();
                }

                Encoding encoding = Encoding.UTF8;
                foreach (IndexEntry index in indexes) {
                    input.Seek (index.OriginalOffset, SeekOrigin.Begin);
                    index.OriginalString = encoding.GetString (br.ReadBytes ((int) index.OriginalLength));
                }

                foreach (IndexEntry index in indexes) {
                    input.Seek (index.TranslatedOffset, SeekOrigin.Begin);
                    index.TranslatedString = encoding.GetString (br.ReadBytes ((int) index.TranslatedLength));
                }
            }

            foreach (IndexEntry index in indexes) 
                dict.Add (index.OriginalString, index.TranslatedString);

            indexes.Clear ();
        }
    }
}
