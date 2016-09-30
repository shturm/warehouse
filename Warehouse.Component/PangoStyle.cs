//
// PangoStyle.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   11/18/2007
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
using System.Text;
using System.Text.RegularExpressions;
using Cairo;

namespace Warehouse.Component
{
    public class PangoStyle : ICloneable
    {
        public enum TextSize
        {
            XXSmall,
            XSmall,
            Small,
            Medium,
            Large,
            XLarge,
            XXLarge,
        }

        private bool underline;
        private bool bold;
        private bool italic;
        private bool strikethrough;
        private Color color = Colors.Black;
        private string markupTemplate = string.Empty;
        private string text;
        private string markup;

        public bool Underline
        {
            get { return underline; }
            set
            {
                underline = value;
                markupTemplate = string.Empty;
            }
        }

        public bool Bold
        {
            get { return bold; }
            set
            {
                bold = value;
                markupTemplate = string.Empty;
            }
        }

        public bool Italic
        {
            get { return italic; }
            set
            {
                italic = value;
                markupTemplate = string.Empty;
            }
        }

        public bool Strikethrough
        {
            get { return strikethrough; }
            set
            {
                strikethrough = value;
                markupTemplate = string.Empty;
            }
        }

        public Color Color
        {
            get { return color; }
            set
            {
                color = value;
                markupTemplate = string.Empty;
            }
        }

        public string ColorText
        {
            get
            {
                return color.ToHTMLColor ();
            }
            set
            {
                Regex rxColor = new Regex (@"#?(?<r>[a-fA-F0-9]{2})(?<g>[a-fA-F0-9]{2})(?<b>[a-fA-F0-9]{2})");
                Match m = rxColor.Match (value);
                if (!m.Success)
                    return;

                int r = int.Parse (m.Groups ["r"].Value, NumberStyles.AllowHexSpecifier);
                int g = int.Parse (m.Groups ["g"].Value, NumberStyles.AllowHexSpecifier);
                int b = int.Parse (m.Groups ["b"].Value, NumberStyles.AllowHexSpecifier);
                color = new Color (r / 255d, g / 255d, b / 255d);
            }
        }

        private float? exactSize;
        public float? ExactSize
        {
            get { return exactSize; }
            set
            {
                exactSize = value;
                size = TextSize.Medium;
                markupTemplate = string.Empty;
            }
        }

        private TextSize size = TextSize.Medium;
        public TextSize Size
        {
            get { return size; }
            set
            {
                size = value;
                markupTemplate = string.Empty;
            }
        }

        private string fontFamily;
        public string FontFamily
        {
            get { return fontFamily; }
            set
            {
                fontFamily = value;
                markupTemplate = string.Empty;
            }
        }

        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                markup = GLib.Markup.EscapeText (value);
            }
        }

        public string Markup
        {
            get { return markup; }
            set { markup = value; }
        }

        public string GetMarkup (string value)
        {
            return string.Format (GetTemplate (), GLib.Markup.EscapeText (value));
        }

        private string GetTemplate ()
        {
            if (!string.IsNullOrEmpty (markupTemplate))
                return markupTemplate;

            StringBuilder spanAttribs = new StringBuilder ();

            if (underline)
                spanAttribs.Append (" underline=\"single\"");

            if (bold)
                spanAttribs.Append (" weight=\"bold\"");

            if (italic)
                spanAttribs.Append (" style=\"italic\"");

            if (strikethrough)
                spanAttribs.Append (" strikethrough=\"true\"");

            if (!color.Equal (Colors.Black))
                spanAttribs.AppendFormat (" foreground=\"{0}\"", color.ToHTMLColor ());

            if (size != TextSize.Medium) {
                switch (size) {
                    case TextSize.XXSmall:
                        spanAttribs.Append (" size=\"xx-small\"");
                        break;
                    case TextSize.XSmall:
                        spanAttribs.Append (" size=\"x-small\"");
                        break;
                    case TextSize.Small:
                        spanAttribs.Append (" size=\"small\"");
                        break;
                    case TextSize.Large:
                        spanAttribs.Append (" size=\"large\"");
                        break;
                    case TextSize.XLarge:
                        spanAttribs.Append (" size=\"x-large\"");
                        break;
                    case TextSize.XXLarge:
                        spanAttribs.Append (" size=\"xx-large\"");
                        break;
                }
            } else if (exactSize != null) {
                NumberFormatInfo nfi = new NumberFormatInfo
                    {
                        NumberGroupSeparator = string.Empty,
                        NumberDecimalSeparator = "."
                    };
                spanAttribs.AppendFormat (" font_desc=\"{0}\"", exactSize.Value.ToString (nfi));
            }

            if (!string.IsNullOrWhiteSpace (fontFamily))
                spanAttribs.AppendFormat (" font_family=\"{0}\"", fontFamily);

            markupTemplate = spanAttribs.Length > 0 ? string.Format ("<span{0}>{{0}}</span>", spanAttribs) : "{0}";
            return markupTemplate;
        }

        public object Clone ()
        {
            return MemberwiseClone ();
        }

        public override string ToString ()
        {
            return string.Format (GetTemplate (), markup);
        }

        public static implicit operator string (PangoStyle operand)
        {
            return operand.ToString ();
        }
    }
}
