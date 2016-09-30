//
// TextBox.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Cairo;
using Warehouse.Data.Calculator;

namespace Warehouse.Component.Documenting
{
    public class TextBox : FormDrawableObject
    {
        #region Private members

        private ObjectFont font = new ObjectFont ();
        private string sourceField;
        private string text = string.Empty;
        private string sourceFormat = string.Empty;
        private string format = string.Empty;
        private StringTrimming trimming = StringTrimming.None;
        private bool wrap;

        #endregion

        public const string SEPARATOR = "--";

        private const string LENGTH_IN_FORMAT = "Len";

        #region Public properties

        protected override string XmlElementName
        {
            get { return "TextBox"; }
        }

        public override SizeD ActualSize
        {
            get
            {
                return visSize;
            }
        }

        protected override bool IsAllocated
        {
            get { return base.IsAllocated; }
            set
            {
                if (!disableReallocate)
                    base.IsAllocated = value;
            }
        }

        public ObjectFont Font
        {
            get { return font; }
            set
            {
                if (font != value) {
                    font = value;
                    disableReallocate = false;
                    OnPropertyChanged ("Font");
                }
            }
        }

        public bool SourceIsPageVariable
        {
            get
            {
                return (sourceField != null && sourceField.Length > 2 && sourceField.StartsWith ("%") && sourceField.EndsWith ("%"));
            }
        }

        public string SourceField
        {
            get { return sourceField; }
            set
            {
                if (sourceField == value)
                    return;

                sourceField = value;
                if (sourceField != null) {
                    object field = bindableFields [sourceField];
                    ChangeText (field != null ? field.ToString () : string.Empty);
                }
                OnPropertyChanged ("SourceField");
            }
        }

        public string UnformattedText
        {
            get
            {
                if (!SourceIsPageVariable)
                    return text;

                object textObj = DocumentHelper.Variables [sourceField];
                return textObj == null ? string.Empty : textObj.ToString ();
            }
        }

        public string Text
        {
            get { return FormatText (UnformattedText); }
            set
            {
                if (ChangeText (value))
                    OnPropertyChanged ("Text");
            }
        }

        public string SourceFormat
        {
            get { return sourceFormat; }
            set
            {
                if (sourceFormat != value) {
                    sourceFormat = value;
                    if (!string.IsNullOrEmpty (sourceFormat) && bindableFields [sourceFormat] != null)
                        format = bindableFields [sourceFormat].ToString ();
                    OnPropertyChanged ("SourceFormat");
                }
            }
        }

        public string Format
        {
            get { return format; }
            set
            {
                if (format == value)
                    return;

                format = value;
                OnPropertyChanged ("Format");
            }
        }

        public StringTrimming Trimming
        {
            get { return trimming; }
            set
            {
                if (trimming == value)
                    return;

                trimming = value;
                OnPropertyChanged ("Trimming");
            }
        }

        public bool Wrap
        {
            get { return wrap; }
            set
            {
                if (wrap == value)
                    return;

                wrap = value;
                OnPropertyChanged ("Wrap");
            }
        }

        #endregion

        public TextBox ()
        {
            SetDefaultStyle ("hfill:true;vfill:true");
        }

        public TextBox (XElement node, Hashtable boundFields)
            : this ()
        {
            CreateFromTemplate (node, boundFields);
        }

        private bool ChangeText (string value)
        {
            if (UnformattedText == value)
                return false;

            text = value;
            disableReallocate = false;
            return true;
        }

        private string FormatText (string textValue)
        {
            if (string.IsNullOrEmpty (format))
                return textValue;

            MatchCollection matches = Regex.Matches (format, @"\{\d+:(?<start>[\d\+\-\*\/(Len)\s]+):(?<count>[\d\+\-\*\/(Len)\s]+)\}");
            const string formatPattern = @"^([^{}]*(\{\d+\})*)*$";
            if (matches.Count > 0) {
                List<string> substrings = new List<string> (matches.Count);
                string valueNoSpaces = textValue.Replace (" ", string.Empty);
                StringBuilder realFormatBuilder = new StringBuilder ();

                int previousMatchEnd = 0;
                for (int i = 0; i < matches.Count; i++) {
                    substrings.Add (GetSubstring (valueNoSpaces, matches [i]));

                    int indexOfMatch = format.IndexOf (matches [i].Value, previousMatchEnd);
                    realFormatBuilder.Append (format.Substring (previousMatchEnd, indexOfMatch - previousMatchEnd));
                    realFormatBuilder.Append ("{" + i + "}");
                    previousMatchEnd = indexOfMatch + matches [i].Value.Length;
                }

                return Regex.IsMatch (realFormatBuilder.ToString (), formatPattern) ?
                    string.Format (realFormatBuilder.ToString (), substrings.ToArray ()) :
                    string.Join (" ", substrings.ToArray ());
            }

            return Regex.IsMatch (format, formatPattern) ?
                string.Format (format, textValue) :
                textValue;
        }

        private static string GetSubstring (string textValue, Match match)
        {
            string startString = match.Groups ["start"].Value.Replace (LENGTH_IN_FORMAT, textValue.Length.ToString ());
            int start = Convert.ToInt32 (RPNCalculator.EvaluateExpression (startString));
            if (start < 0 || start >= textValue.Length)
                return string.Empty;

            string countString = match.Groups ["count"].Value.Replace (LENGTH_IN_FORMAT, textValue.Length.ToString ());
            int count = Convert.ToInt32 (RPNCalculator.EvaluateExpression (countString));
            if (count < 0 || start + count > textValue.Length)
                return string.Empty;

            return textValue.Substring (start, count);
        }

        #region FormObject Members

        public override void CreateFromTemplate (XElement node, Hashtable boundFields)
        {
            base.CreateFromTemplate (node, boundFields);

            XAttribute tempAttr = node.Attribute ("font");
            if (tempAttr != null)
                font.LoadFromString (tempAttr.Value);

            tempAttr = node.Attribute ("text");
            if (tempAttr != null)
                Text = tempAttr.Value;

            tempAttr = node.Attribute ("sourceField");
            if (tempAttr != null)
                SourceField = tempAttr.Value;

            tempAttr = node.Attribute ("format");
            if (tempAttr != null)
                Format = tempAttr.Value;

            tempAttr = node.Attribute ("sourceFormat");
            if (tempAttr != null)
                SourceFormat = tempAttr.Value;

            tempAttr = node.Attribute ("trimming");
            if (tempAttr != null)
                try {
                    Trimming = (StringTrimming) Enum.Parse (typeof (StringTrimming), tempAttr.Value, true);
                } catch {
                    Trimming = StringTrimming.None;
                }

            tempAttr = node.Attribute ("wrap");
            bool value;
            if (tempAttr != null && bool.TryParse (tempAttr.Value, out value))
                Wrap = value;

            RefreshBoundFields (boundFields);
        }

        public override void RefreshBoundFields (Hashtable boundFields)
        {
            base.RefreshBoundFields (boundFields);
            if (sourceField != null && bindableFields [SourceField] != null)
                Text = bindableFields [SourceField].ToString ();

            if (!string.IsNullOrEmpty (SourceFormat) && bindableFields [SourceFormat] != null)
                Format = bindableFields [SourceFormat].ToString ();

            ReCheckPageDependant ();
        }

        #endregion

        #region FormDrawableObject Members

        public override void Draw (PointD start, int pageNumber)
        {
            base.Draw (start, pageNumber);

            start = DocumentHelper.CurrentPageSettings.GetInPageDrawLocation (start);
            visSize = GetSize (start);
            SizeD contSize = GetContentsSize (start);
            if (contSize.Width > visSize.Width)
                contSize.Width = visSize.Width;

            PointD contStart = GetContentStart (start, visSize, contSize);

            if (style.BgColorSpecified)
                DocumentHelper.DrawingProvider.FillRectangle (
                    start.X, start.Y,
                    visSize.Width, visSize.Height, style.BgColor);

            if (style.Border > 0f)
                DocumentHelper.DrawingProvider.DrawInsetRectangle (
                    start.X, start.Y,
                    visSize.Width, visSize.Height, style.Border.Value, style.BorderColor);

            if (Text == SEPARATOR)
                DocumentHelper.DrawingProvider.DrawLine (contStart.X, contStart.Y, contStart.X + visSize.Width, contStart.Y, 1, style.FgColor);
            else
                DocumentHelper.DrawingProvider.DrawString (new RectangleD (contStart, contSize), Text, style.FgColor, font, trimming, style.HAlign, wrap);
        }

        public override SizeD AllocateSize (PointD start)
        {
            bool reallocating = !IsAllocated;
            SizeD size = base.AllocateSize (start);
            if (reallocating && (Text == SEPARATOR || trimming != StringTrimming.None || wrap) &&
                allocatedSize.RelativeWidth == null)
                allocatedSize.RelativeWidth = 100;

            ResolveDistribution (start, allocatedSize.StaticSize);

            return size;
        }

        protected override SizeD AllocateContentsSize (PointD start)
        {
            SizeD size = (Text == SEPARATOR) ?
                new SizeD (10, 5) :
                MeasureString (null);

            size.Width = (float) Math.Ceiling (size.Width);
            size.Height = (float) Math.Ceiling (size.Height);

            if (trimming != StringTrimming.None || wrap)
                return new SizeD (10, size.Height);

            return calculatedSize.WidthValue > 0 ?
                new SizeD (Math.Min (calculatedSize.WidthValue, size.Width), size.Height) :
                size;
        }

        public override bool OnSizeCalculated (PointD childPosition)
        {
            if (!wrap && trimming == StringTrimming.None) {
                if (Text == SEPARATOR) {
                    allocatedSize.Width = calculatedSize.Width;
                    allocatedSize.Height = calculatedSize.Height;
                }
                return true;
            }

            PointD contentStart = GetContentStart (GetContentEnd ());
            SizeD size = MeasureString (wrap ? calculatedSize.Width - contentStart.X : null);
            size.Width = (float) Math.Ceiling (size.Width) + contentStart.X;
            size.Height = (float) Math.Ceiling (size.Height) + contentStart.Y;
            if (!wrap) {
                allocatedSize.Width = size.Width > calculatedSize.WidthValue ? calculatedSize.Width : size.Width;
                allocatedSize.Height = size.Height > calculatedSize.HeightValue ? calculatedSize.Height : size.Height;
                return true;
            }

            // if the new calculated width is more than the last one recalculate to save height
            // if the new measured height is more than the calculated reallocate to gain height
            if (size.Width <= allocatedSize.Width &&
                size.Height <= calculatedSize.Height)
                return true;

            allocatedSize.Width = size.Width;
            allocatedSize.Height = size.Height;
            disableReallocate = true;
            QueueAllocateUp (false);
            return false;
        }

        public override void ReCheckPageDependant ()
        {
            if (isPageDependant == SourceIsPageVariable)
                return;

            FormDrawableObject drawableParent = Parent as FormDrawableObject;
            if (drawableParent == null)
                return;

            isPageDependant = SourceIsPageVariable;
            drawableParent.ReCheckPageDependant ();
        }

        #endregion

        protected override void AddAttributes (XElement xmlElement)
        {
            if (sourceField == null && !string.IsNullOrEmpty (text))
                AddAttribute ("text", text, xmlElement);

            if (sourceField != null)
                AddAttribute ("sourceField", sourceField, xmlElement, true);

            if (string.IsNullOrEmpty (sourceFormat) && !string.IsNullOrEmpty (format))
                AddAttribute ("format", format, xmlElement);

            if (!string.IsNullOrEmpty (sourceFormat))
                AddAttribute ("sourceFormat", sourceFormat, xmlElement);

            var defaultFont = new ObjectFont ();
            if (font != defaultFont)
                AddAttribute ("font", font.ToString (defaultFont), xmlElement);

            if (Trimming != StringTrimming.None)
                AddAttribute ("trimming", trimming.ToString (), xmlElement);

            if (wrap)
                AddAttribute ("wrap", "true", xmlElement);

            base.AddAttributes (xmlElement);
        }

        #region ICloneable Members

        public override object Clone ()
        {
            TextBox ret = DocumentHelper.FormObjectCreator.CreateTextBox ();

            ret.bindableFields = new Hashtable (bindableFields);
            ret.style = (ObjectStyle) style.Clone ();
            ret.font = (ObjectFont) font.Clone ();
            ret.wrap = wrap;
            ret.trimming = trimming;

            ret.format = format;
            ret.sourceFormat = sourceFormat;
            ret.text = UnformattedText;
            ret.sourceField = SourceField;

            return ret;
        }

        #endregion

        private string lastText;
        private ObjectFont lastFont;
        private SizeD? lastSize;

        private string lastWrappedText;
        private ObjectFont lastWrappedFont;
        private SizeD? lastWrappedSize;
        private double? lastWidth;
        private bool disableReallocate;

        /// <summary>
        /// Implements caching of the measured sizes as this operation is one of the slowest
        /// and is likelly to be repeatedly called with same values
        /// </summary>
        /// <param name="width">Horizontal space available</param>
        /// <returns></returns>
        private SizeD MeasureString (double? width)
        {
            if (width == null) {
                if (lastText != Text || lastFont != font || lastSize == null) {
                    lastText = Text;
                    lastFont = font;
                    lastSize = DocumentHelper.DrawingProvider.MeasureString (Text, font, null);
                }
                return lastSize.Value;
            }

            if (lastWrappedText != Text || lastWrappedFont != font || lastWidth != width || lastWrappedSize == null) {
                lastWrappedText = Text;
                lastWrappedFont = font;
                lastWidth = width;
                lastWrappedSize = DocumentHelper.DrawingProvider.MeasureString (Text, font, width);
            }
            return lastWrappedSize.Value;
        }
    }
}
