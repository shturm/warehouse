// 
// Barcode.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
// 
// Created:
//    14.02.2012
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
using System.ComponentModel;
using System.Xml.Linq;
using Cairo;
using Gdk;
using NBarCodes;
using NBarCodes.BarCodes;

namespace Warehouse.Component.Documenting
{
    public class Barcode : FormDrawableObject
    {
        private BarCodeSettings settings;
        private string sourceField;
        private BarCodeImage image;

        protected override string XmlElementName
        {
            get { return "BarCode"; }
        }

        public BarCodeSettings Settings
        {
            get { return settings; }
            set
            {
                if (settings != value) {
                    if (settings != null)
                        settings.PropertyChanged -= Settings_PropertyChanged;
                    settings = value;
                    if (settings != null)
                        settings.PropertyChanged += Settings_PropertyChanged;
                }
            }
        }

        public override SizeD ActualSize
        {
            get { return visSize; }
        }

        public string SourceField
        {
            get { return sourceField; }
            set
            {
                if (sourceField != value) {
                    sourceField = value;
                    OnPropertyChanged ("SourceField");
                }
            }
        }

        public Barcode ()
        {
            SetDefaultStyle ("allowhbreak:false;allowvbreak:false");
            settings = new BarCodeSettings ();
            settings.PropertyChanged += Settings_PropertyChanged;
        }

        public Barcode (XElement child, Hashtable boundFields)
            : this ()
        {
            CreateFromTemplate (child, boundFields);
        }

        private void GetImage (out bool formatError, out SizeD size, float requestedHeight)
        {
            formatError = false;
            settings.RequestedTotalHeight = requestedHeight;
            BarCodeGenerator barCodeGenerator = new BarCodeGenerator (settings);
            string data = settings.Data;
            if (barCodeGenerator.AssembleBarCode ().Validate (ref data) == BarCodeFormatValidationResult.Success) {
                image = barCodeGenerator.GenerateImage (ComponentHelper.TopWindow.CreatePangoLayout (""));
                size = new SizeD (image.Width, image.Height);
            } else {
                formatError = true;
                size = SetSizeOnError ();
            }
        }

        protected virtual SizeD SetSizeOnError ()
        {
            return visSize = new SizeD (50, 50);
        }

        public override void RefreshBoundFields (Hashtable boundFields)
        {
            base.RefreshBoundFields (boundFields);

            if (SourceField != null && bindableFields [SourceField] != null)
                settings.Data = bindableFields [SourceField].ToString ();
        }

        protected override void AddAttributes (XElement xmlElement)
        {
            base.AddAttributes (xmlElement);

            if (string.IsNullOrEmpty (SourceField))
                AddAttribute ("Data", settings.Data, xmlElement);
            else
                AddAttribute ("sourceField", SourceField, xmlElement);

            AddAttribute ("BackColor", settings.BackColor.ToHTMLColor (), xmlElement);
            AddAttribute ("BarColor", settings.BarColor.ToHTMLColor (), xmlElement);
            AddAttribute ("FontColor", settings.FontColor.ToHTMLColor (), xmlElement);
            AddAttribute ("FontName", settings.FontName, xmlElement);
            AddAttribute ("FontSize", settings.FontSize, xmlElement);
            AddAttribute ("TextPosition", settings.TextPosition, xmlElement);
            AddAttribute ("Type", settings.Type, xmlElement);
            AddAttribute ("UseChecksum", settings.UseChecksum, xmlElement);
        }

        public override object Clone ()
        {
            Barcode barcode = DocumentHelper.FormObjectCreator.CreateLinearBarcode ();
            barcode.bindableFields = new Hashtable (bindableFields);
            barcode.settings = (BarCodeSettings) settings.Clone ();
            barcode.style = (ObjectStyle) style.Clone ();
            return barcode;
        }

        private void Settings_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged ("Settings");
        }

        public override void CreateFromTemplate (XElement node, Hashtable boundFields)
        {
            base.CreateFromTemplate (node, boundFields);

            XAttribute attr = node.Attribute ("sourceField");
            if (attr != null)
                SourceField = attr.Value;

            if (string.IsNullOrEmpty (SourceField)) {
                attr = node.Attribute ("Data");
                if (attr != null)
                    settings.Data = attr.Value;
            }

            attr = node.Attribute ("BackColor");
            if (attr != null)
                settings.BackColor = CairoExtensions.FromHTMLColor (attr.Value);

            attr = node.Attribute ("BarColor");
            if (attr != null)
                settings.BarColor = CairoExtensions.FromHTMLColor (attr.Value);

            float floatValue;
            attr = node.Attribute ("FontColor");
            if (attr != null)
                settings.FontColor = CairoExtensions.FromHTMLColor (attr.Value);

            attr = node.Attribute ("FontName");
            if (attr != null)
                settings.FontName = attr.Value;

            attr = node.Attribute ("FontSize");
            if (attr != null && float.TryParse (attr.Value, out floatValue))
                settings.FontSize = floatValue;

            attr = node.Attribute ("TextPosition");
            TextPosition textPosition;
            if (attr != null && Enum.TryParse (attr.Value, out textPosition))
                settings.TextPosition = textPosition;

            attr = node.Attribute ("Type");
            BarCodeType barcodeType;
            if (attr != null && Enum.TryParse (attr.Value, out barcodeType))
                settings.Type = barcodeType;

            attr = node.Attribute ("UseChecksum");
            bool boolValue;
            if (attr != null && bool.TryParse (attr.Value, out boolValue))
                settings.UseChecksum = boolValue;

            RefreshBoundFields (boundFields);
        }

        public override void ReCheckPageDependant ()
        {
        }

        protected override SizeD AllocateContentsSize (PointD start)
        {
            bool formatError;
            SizeD size;
            GetImage (out formatError, out size, 10);
            return new SizeD (calculatedSize.WidthValue > 0 ? calculatedSize.WidthValue : size.Width,
                calculatedSize.HeightValue > 0 ? calculatedSize.HeightValue : size.Height);
        }

        public override void Draw (PointD start, int pageNumber)
        {
            base.Draw (start, pageNumber);

            start = DocumentHelper.CurrentPageSettings.GetInPageDrawLocation (start);
            visSize = GetSize (start);

            PointD contStart = GetContentStart (start);
            IDrawingProvider provider = DocumentHelper.DrawingProvider;

            if (style.BgColorSpecified)
                provider.FillRectangle (
                    start.X, start.Y,
                    visSize.Width, visSize.Height, style.BgColor);

            SizeD scaledVisSize = new SizeD (
                visSize.Width * provider.DrawingScaleX,
                visSize.Height * provider.DrawingScaleY);

            bool formatError;
            SizeD size;
            GetImage (out formatError, out size, (float) scaledVisSize.Height);

            if (formatError)
                DrawError (contStart);
            else {
                double targetWidth;
                if (image.Width > scaledVisSize.Width)
                    targetWidth = scaledVisSize.Width;
                else {
                    int expand = (int) (scaledVisSize.Width / image.Width);
                    if (expand > 1)
                        GetImage (out formatError, out size, (float) scaledVisSize.Height / expand);

                    // scale with whole factor lower the blurring
                    targetWidth = image.Width * expand;
                    // center the barcode in the new spot
                    contStart.X += (int) ((scaledVisSize.Width - targetWidth) / (2 * provider.DrawingScaleX));
                }

                RectangleD target = new RectangleD (contStart, new SizeD (targetWidth / provider.DrawingScaleX, visSize.Height));
                RectangleD source = new RectangleD (0, 0, image.Width, image.Height);
                provider.DrawSurface (image.Surface, target, source, InterpType.Nearest);
            }

            if (style.Border > 0f)
                provider.DrawInsetRectangle (
                    start.X, start.Y,
                    visSize.Width, visSize.Height, style.Border.Value, style.BorderColor);
        }

        protected virtual void DrawError (PointD contStart)
        {
        }
    }
}
