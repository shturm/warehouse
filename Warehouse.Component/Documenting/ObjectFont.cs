//
// ObjectFont.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/04/2007
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
using System.Text;
using Warehouse.Data;

namespace Warehouse.Component.Documenting
{
    public class ObjectFont : ICloneable
    {
        #region Private members

        private const float defaultSize = 8f;

        #endregion

        #region Public properties

        private string name = DataHelper.DefaultDocumentsFont;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public bool NameSpecified
        {
            get { return !string.IsNullOrEmpty (name); }
        }

        private float size = defaultSize;
        public float Size
        {
            get { return size >= 0f ? size : defaultSize; }
            set { size = value; }
        }

        private bool SizeSpecified
        {
            get { return size >= 0f && size != defaultSize; }
        }

        private bool underlineSpecified;
        private bool underline;
        public bool Underline
        {
            get { return underline; }
            set
            {
                underline = value;
                underlineSpecified = true;
            }
        }

        private bool boldSpecified;
        private bool bold;
        public bool Bold
        {
            get { return bold; }
            set
            {
                bold = value;
                boldSpecified = true;
            }
        }

        private bool italicSpecified;
        private bool italic;
        public bool Italic
        {
            get { return italic; }
            set
            {
                italic = value;
                italicSpecified = true;
            }
        }

        private bool strikeTroughtSpecified;
        private bool strikeThrought;
        public bool StrikeThrought
        {
            get { return strikeThrought; }
            set
            {
                strikeThrought = value;
                strikeTroughtSpecified = true;
            }
        }

        #endregion

        public ObjectFont ()
        {
            underlineSpecified = false;
        }

        public ObjectFont (string font)
        {
            underlineSpecified = false;
            LoadFromString (font);
        }

        public void LoadFromString (string font)
        {
            foreach (string pair in font.Split (';')) {

                string [] pairParts = pair.Trim ().Split (':');
                string pixelsValue;
                if (pairParts.Length == 2) {
                    pixelsValue = pairParts [1].EndsWith ("px") ?
                        pairParts [1].Substring (0, pairParts [1].Length - 2) :
                        pairParts [1];
                } else {
                    pixelsValue = string.Empty;
                }

                switch (pairParts [0].ToLowerInvariant ()) {
                    case "name":
                        Name = pairParts [1];
                        break;
                    case "size":
                        Size = float.Parse (pixelsValue);
                        break;
                    case "underline":
                        Underline = bool.Parse (pairParts [1]);
                        break;
                    case "bold":
                        Bold = bool.Parse (pairParts [1]);
                        break;
                    case "italic":
                        Italic = bool.Parse (pairParts [1]);
                        break;
                    case "strikethrough":
                        StrikeThrought = bool.Parse (pairParts [1]);
                        break;
                }
            }
        }

        public override string ToString ()
        {
            StringBuilder sb = new StringBuilder ();

            if (NameSpecified && Name != DataHelper.DefaultDocumentsFont)
                sb.AppendFormat ("name:{0};", Name);

            if (SizeSpecified)
                sb.AppendFormat ("size:{0};", Size);

            if (underlineSpecified)
                sb.AppendFormat ("underline:{0};", underline.ToString ().ToLowerInvariant ());

            if (boldSpecified)
                sb.AppendFormat ("bold:{0};", bold.ToString ().ToLowerInvariant ());

            if (italicSpecified)
                sb.AppendFormat ("italic:{0};", italic.ToString ().ToLowerInvariant ());

            if (strikeTroughtSpecified)
                sb.AppendFormat ("italic:{0};", strikeThrought.ToString ().ToLowerInvariant ());

            return sb.ToString ().TrimEnd (';');
        }

        public string ToString (ObjectFont defaultObjectFont)
        {
            StringBuilder sb = new StringBuilder ();

            if (name != defaultObjectFont.name)
                sb.AppendFormat ("name:{0};", name);

            if (!Number.IsEqualTo (size, defaultObjectFont.size))
                sb.AppendFormat ("size:{0};", size);

            if (underline != defaultObjectFont.underline)
                sb.AppendFormat ("underline:{0};", underline.ToString ().ToLowerInvariant ());

            if (bold != defaultObjectFont.bold)
                sb.AppendFormat ("bold:{0};", bold.ToString ().ToLowerInvariant ());

            if (italic != defaultObjectFont.italic)
                sb.AppendFormat ("italic:{0};", italic.ToString ().ToLowerInvariant ());

            if (strikeThrought != defaultObjectFont.strikeThrought)
                sb.AppendFormat ("strikethrough:{0};", strikeThrought.ToString ().ToLowerInvariant ());

            return sb.ToString ().TrimEnd (';');
        }

        #region ICloneable Members

        public object Clone ()
        {
            return MemberwiseClone ();
        }

        #endregion
    }
}
