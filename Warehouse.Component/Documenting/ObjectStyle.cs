//
// ObjectStyle.cs
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
using System.ComponentModel;
using System.Text;
using Warehouse.Data;
using Color = Cairo.Color;

namespace Warehouse.Component.Documenting
{
    public enum HorizontalAlignment
    {
        Left,
        Center,
        Right
    }

    public enum VerticalAlignment
    {
        Top,
        Center,
        Bottom
    }
    
    public class ObjectStyle : INotifyPropertyChanged, ICloneable
    {
        public enum ChildDistribution
        {
            Vertical,
            Horizontal,
            Mixed
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region Private members

        public static readonly Color DefaultBackground = Colors.White;
        public static readonly Color DefaultForeground = Colors.Black;
        public static readonly Color DefaultBorder = Colors.Black;

        private float xStart;
        private float yStart;
        private double? width;
        private double? height;
        private double? relativeWidth;
        private double? relativeHeight;
        private bool? hFill;
        private bool? vFill;
        private float? hPadding;
        private float? vPadding;
        private float? spacing;
        private float? innerHSpacing;
        private float? innerVSpacing;
        private float? border;
        private float? innerVerticalBorder;
        private float? innerHorizontalBorder;
        private HorizontalAlignment hAlign = HorizontalAlignment.Left;
        private VerticalAlignment vAlign = VerticalAlignment.Top;
        private bool drawEmpty = true;
        private bool allowHBreak = true;
        private bool allowVBreak = true;
        private Color bgColor = DefaultBackground;
        private Color fgColor = DefaultForeground;
        private Color borderColor = DefaultBorder;

        #endregion

        #region Public properties

        #region Position

        public float XStart
        {
            get { return xStart; }
            set
            {
                if (!Number.IsEqualTo (xStart, value)) {
                    xStart = value;
                    OnPropertyChanged ("XStart");
                }
            }
        }

        public float YStart
        {
            get { return yStart; }
            set
            {
                if (!Number.IsEqualTo (yStart, value)) {
                    yStart = value;
                    OnPropertyChanged ("YStart");
                }
            }
        }

        #endregion

        #region Size

        public double? Width
        {
            get { return width; }
            set
            {
                double? finalValue = value >= 0 ? value : null;
                if (width != finalValue) {
                    width = finalValue >= 0 ? value : null;
                    OnPropertyChanged ("Width");
                }
            }
        }

        public double? Height
        {
            get { return height; }
            set
            {
                double? finalValue = value >= 0 ? value : null;
                if (height != finalValue) {
                    height = finalValue;
                    OnPropertyChanged ("Height");
                }
            }
        }

        public double? RelativeWidth
        {
            get { return relativeWidth; }
            set
            {
                double? finalValue = value >= 0 ? value : null;
                if (relativeWidth != finalValue) {
                    relativeWidth = finalValue;
                    OnPropertyChanged ("RelativeWidth");
                }
            }
        }

        public double? RelativeHeight
        {
            get { return relativeHeight; }
            set
            {
                double? finalValue = value >= 0 ? value : null;
                if (relativeHeight != finalValue) {
                    relativeHeight = finalValue;
                    OnPropertyChanged ("RelativeHeight");
                }
            }
        }

        #endregion

        #region Fill

        public bool? HFill
        {
            get { return hFill; }
            set
            {
                if (hFill != value) {
                    hFill = value;
                    OnPropertyChanged ("HFill");
                }
            }
        }

        public bool? VFill
        {
            get { return vFill; }
            set
            {
                if (vFill != value) {
                    vFill = value;
                    OnPropertyChanged ("VFill");
                }
            }
        }

        #endregion

        #region Padding

        public float? HPadding
        {
            get { return hPadding; }
            set
            {
                float? finalValue = value >= 0 ? value : null;
                if (hPadding != finalValue) {
                    hPadding = finalValue;
                    OnPropertyChanged ("HPadding");
                }
            }
        }

        public float? VPadding
        {
            get { return vPadding; }
            set
            {
                float? finalValue = value >= 0 ? value : null;
                if (vPadding != finalValue) {
                    vPadding = finalValue;
                    OnPropertyChanged ("VPadding");
                }
            }
        }

        public float? Padding
        {
            get
            {
                if (!vPadding.HasValue || !hPadding.HasValue)
                    return null;

                return Math.Min (vPadding.Value, hPadding.Value);
            }
            set
            {
                if (value >= 0) {
                    VPadding = value;
                    HPadding = value;
                }
            }
        }

        #endregion

        #region Spacing

        public float? Spacing
        {
            get { return spacing; }
            set
            {
                float? finalValue = value >= 0 ? value : null;
                if (spacing != finalValue) {
                    spacing = finalValue;
                    OnPropertyChanged ("Spacing");
                }
            }
        }

        public float? InnerSpacing
        {
            get
            {
                if (!innerVSpacing.HasValue || !innerHSpacing.HasValue)
                    return null;

                return Math.Min (innerVSpacing.Value, innerHSpacing.Value);
            }
            set
            {
                if (value >= 0) {
                    InnerHSpacing = value;
                    InnerVSpacing = value;
                }
            }
        }

        public float? InnerHSpacing
        {
            get { return innerHSpacing; }
            set
            {
                float? finalValue = value >= 0 ? value : null;
                if (innerHSpacing != finalValue) {
                    innerHSpacing = finalValue;
                    OnPropertyChanged ("InnerHSpacing");
                }
            }
        }

        public float? InnerVSpacing
        {
            get { return innerVSpacing; }
            set
            {
                if (innerVSpacing != value) {
                    innerVSpacing = value;
                    OnPropertyChanged ("InnerVSpacing");
                }
            }
        }

        #endregion

        #region Border

        public float? Border
        {
            get { return border; }
            set
            {
                float? finalValue = value >= 0 ? value : null;
                if (border != finalValue) {
                    border = finalValue;
                    OnPropertyChanged ("Border");
                }
            }
        }

        public float? InnerBorder
        {
            get
            {
                if (!innerHorizontalBorder.HasValue || !innerVerticalBorder.HasValue)
                    return null;

                return Math.Min (innerVerticalBorder.Value, innerHorizontalBorder.Value);
            }
            set
            {
                if (value >= 0) {
                    InnerVerticalBorder = value;
                    InnerHorizontalBorder = value;
                }
            }
        }

        public float? InnerVerticalBorder
        {
            get { return innerVerticalBorder; }
            set
            {
                float? finalValue = value >= 0 ? value : null;
                if (innerVerticalBorder != finalValue) {
                    innerVerticalBorder = finalValue;
                    OnPropertyChanged ("InnerVerticalBorder");
                }
            }
        }

        public float? InnerHorizontalBorder
        {
            get { return innerHorizontalBorder; }
            set
            {
                float? finalValue = value >= 0 ? value : null;
                if (innerHorizontalBorder != finalValue) {
                    innerHorizontalBorder = finalValue;
                    OnPropertyChanged ("InnerHorizontalBorder");
                }
            }
        }

        #endregion

        #region Alignment

        public HorizontalAlignment HAlign
        {
            get { return hAlign; }
            set
            {
                if (hAlign != value) {
                    hAlign = value;
                    OnPropertyChanged ("HAlign");
                }
            }
        }

        public VerticalAlignment VAlign
        {
            get { return vAlign; }
            set
            {
                if (vAlign != value) {
                    vAlign = value;
                    OnPropertyChanged ("VAlign");
                }
            }
        }

        #endregion

        public bool DrawEmpty
        {
            get { return drawEmpty; }
            set { drawEmpty = value; }
        }

        #region Paging

        public bool AllowHBreak
        {
            get { return allowHBreak; }
            set
            {
                if (allowHBreak != value) {
                    allowHBreak = value;
                    OnPropertyChanged ("AllowHBreak");
                }
            }
        }

        public bool AllowVBreak
        {
            get { return allowVBreak; }
            set
            {
                if (allowVBreak != value) {
                    allowVBreak = value;
                    OnPropertyChanged ("AllowVBreak");
                }
            }
        }

        #endregion

        #region Color

        public Color BgColor
        {
            get { return bgColor; }
            set
            {
                if (bgColor.Equal (value))
                    return;
                
                bgColor = value;
                OnPropertyChanged ("BgColor");
            }
        }

        public bool BgColorSpecified
        {
            get { return !bgColor.Equal (DefaultBackground); }
        }

        public Color FgColor
        {
            get { return fgColor; }
            set
            {
                if (fgColor.Equal (value))
                    return;
                
                fgColor = value;
                OnPropertyChanged ("FgColor");
            }
        }

        private bool FgColorSpecified
        {
            get { return !fgColor.Equal (DefaultForeground); }
        }

        public Color BorderColor
        {
            get { return borderColor; }
            set
            {
                if (borderColor.Equal (value))
                    return;
                
                borderColor = value;
                OnPropertyChanged ("BorderColor");
            }
        }

        private bool BorderColorSpecified
        {
            get { return !borderColor.Equal (DefaultBorder); }
        }

        #endregion

        #endregion

        public ObjectStyle ()
        {
        }

        public ObjectStyle (string style)
        {
            LoadFromString (style);
        }

        public void LoadFromString (string style)
        {
            foreach (string pair in style.Split (new [] { ';' }, StringSplitOptions.RemoveEmptyEntries)) {

                string [] pairParts = pair.Trim ().Split (new [] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                string pixelsValue;
                if (pairParts.Length == 2) {
                    pixelsValue = pairParts [1].EndsWith ("px") ?
                        pairParts [1].Substring (0, pairParts [1].Length - 2) :
                        pairParts [1];
                } else {
                    pixelsValue = string.Empty;
                }

                float floatValue;
                bool boolValue;
                switch (pairParts [0].ToLowerInvariant ()) {
                    case "xstart":
                        if (float.TryParse (pixelsValue, out floatValue))
                            xStart = floatValue;
                        break;
                    case "ystart":
                        if (float.TryParse (pixelsValue, out floatValue))
                            yStart = floatValue;
                        break;
                    case "width":
                        if (pairParts [1].EndsWith ("%")) {
                            if (float.TryParse (pairParts [1].Substring (0, pairParts [1].Length - 1), out floatValue))
                                relativeWidth = floatValue;
                        } else {
                            if (float.TryParse (pixelsValue, out floatValue))
                                width = floatValue;
                        }
                        break;
                    case "height":
                        if (pairParts [1].EndsWith ("%")) {
                            if (float.TryParse (pairParts [1].Substring (0, pairParts [1].Length - 1), out floatValue))
                                relativeHeight = floatValue;
                        } else {
                            if (float.TryParse (pixelsValue, out floatValue))
                                height = floatValue;
                        }
                        break;
                    case "padding":
                        if (float.TryParse (pixelsValue, out floatValue))
                            hPadding = vPadding = floatValue;
                        break;
                    case "hpadding":
                        if (float.TryParse (pixelsValue, out floatValue))
                            hPadding = floatValue;
                        break;
                    case "vpadding":
                        if (float.TryParse (pixelsValue, out floatValue))
                            vPadding = floatValue;
                        break;
                    case "hfill":
                        if (bool.TryParse (pairParts [1], out boolValue))
                            hFill = boolValue;
                        break;
                    case "vfill":
                        if (bool.TryParse (pairParts [1], out boolValue))
                            vFill = boolValue;
                        break;
                    case "border":
                        if (float.TryParse (pixelsValue, out floatValue))
                            border = floatValue;
                        break;
                    case "innerborder":
                        if (float.TryParse (pixelsValue, out floatValue))
                            innerHorizontalBorder = innerVerticalBorder = floatValue;
                        break;
                    case "innerhborder":
                        if (float.TryParse (pixelsValue, out floatValue))
                            innerHorizontalBorder = floatValue;
                        break;
                    case "innervborder":
                        if (float.TryParse (pixelsValue, out floatValue))
                            innerVerticalBorder = floatValue;
                        break;
                    case "spacing":
                        if (float.TryParse (pixelsValue, out floatValue))
                            spacing = floatValue;
                        break;
                    case "innerspacing":
                        if (float.TryParse (pixelsValue, out floatValue))
                            innerHSpacing = innerVSpacing = floatValue;
                        break;
                    case "innerhspacing":
                        if (float.TryParse (pixelsValue, out floatValue))
                            innerHSpacing = floatValue;
                        break;
                    case "innervspacing":
                        if (float.TryParse (pixelsValue, out floatValue))
                            innerVSpacing = floatValue;
                        break;
                    case "halign":
                        switch (pairParts [1].ToLowerInvariant ()) {
                            case "left":
                                break;
                            case "right":
                                hAlign = HorizontalAlignment.Right;
                                break;
                            case "center":
                                hAlign = HorizontalAlignment.Center;
                                break;
                            default:
                                hAlign = HorizontalAlignment.Left;
                                break;
                        }
                        break;
                    case "valign":
                        switch (pairParts [1].ToLowerInvariant ()) {
                            case "bottom":
                                vAlign = VerticalAlignment.Bottom;
                                break;
                            case "center":
                                vAlign = VerticalAlignment.Center;
                                break;
                            default:
                                vAlign = VerticalAlignment.Top;
                                break;
                        }
                        break;
                    case "drawempty":
                        if (bool.TryParse (pairParts [1], out boolValue))
                            drawEmpty = boolValue;
                        break;
                    case "allowhbreak":
                        if (bool.TryParse (pairParts [1], out boolValue))
                            allowHBreak = boolValue;
                        break;
                    case "allowvbreak":
                        if (bool.TryParse (pairParts [1], out boolValue))
                            allowVBreak = boolValue;
                        break;
                    case "bgcolor":
                        bgColor = CairoExtensions.FromHTMLColor (pairParts [1]);
                        break;
                    case "fgcolor":
                        fgColor = CairoExtensions.FromHTMLColor (pairParts [1]);
                        break;
                    case "bordercolor":
                        borderColor = CairoExtensions.FromHTMLColor (pairParts [1]);
                        break;
                }
            }
        }

        public void Clear (ObjectStyle defaultStyle)
        {
            xStart = defaultStyle.XStart;
            yStart = defaultStyle.YStart;
            relativeWidth = defaultStyle.RelativeWidth;
            width = defaultStyle.Width;
            relativeHeight = defaultStyle.RelativeHeight;
            height = defaultStyle.Height;
            hPadding = vPadding = defaultStyle.Padding;
            hPadding = defaultStyle.HPadding;
            vPadding = defaultStyle.VPadding;
            hFill = defaultStyle.HFill;
            vFill = defaultStyle.VFill;
            border = defaultStyle.Border;
            innerHorizontalBorder = innerVerticalBorder = defaultStyle.InnerBorder;
            innerHorizontalBorder = defaultStyle.InnerHorizontalBorder;
            innerVerticalBorder = defaultStyle.InnerVerticalBorder;
            spacing = defaultStyle.Spacing;
            innerHSpacing = innerVSpacing = defaultStyle.InnerSpacing;
            innerHSpacing = defaultStyle.InnerHSpacing;
            innerVSpacing = defaultStyle.InnerVSpacing;
            hAlign = defaultStyle.HAlign;
            vAlign = defaultStyle.VAlign;
            drawEmpty = defaultStyle.DrawEmpty;
            allowHBreak = defaultStyle.AllowHBreak;
            allowVBreak = defaultStyle.AllowVBreak;
            bgColor = defaultStyle.BgColor;
            fgColor = defaultStyle.FgColor;
            borderColor = defaultStyle.BorderColor;
        }

        public string ToString (ObjectStyle defaultStyle)
        {
            StringBuilder sb = new StringBuilder ();

            if (!Number.IsEqualTo (xStart, defaultStyle.xStart))
                sb.AppendFormat ("xstart:{0};", xStart);

            if (!Number.IsEqualTo (yStart, defaultStyle.yStart))
                sb.AppendFormat ("ystart:{0};", yStart);

            if (relativeWidth != defaultStyle.relativeWidth)
                sb.AppendFormat ("width:{0}%;", relativeWidth ?? 100);
            
            if (width != defaultStyle.width)
                sb.AppendFormat ("width:{0};", width ?? 0);

            if (relativeHeight != defaultStyle.relativeHeight)
                sb.AppendFormat ("height:{0}%;", relativeHeight ?? 100);
            
            if (height != defaultStyle.height)
                sb.AppendFormat ("height:{0};", height ?? 0);

            if (hPadding == vPadding &&
                ((hPadding != defaultStyle.hPadding || vPadding != defaultStyle.vPadding)))
                sb.AppendFormat ("padding:{0};", Padding ?? 0);
            else {
                if (hPadding != defaultStyle.hPadding)
                    sb.AppendFormat ("hpadding:{0};", hPadding ?? 0);

                if (vPadding != defaultStyle.vPadding)
                    sb.AppendFormat ("vpadding:{0};", vPadding ?? 0);
            }

            if (hFill != defaultStyle.hFill)
                sb.AppendFormat ("hfill:{0};", (hFill ?? false).ToString ().ToLower ());

            if (vFill != defaultStyle.vFill)
                sb.AppendFormat ("vfill:{0};", (vFill ?? false).ToString ().ToLower ());

            if (border != defaultStyle.border)
                sb.AppendFormat ("border:{0};", border ?? 0);

            if (innerVerticalBorder == innerHorizontalBorder &&
                (innerVerticalBorder != defaultStyle.innerVerticalBorder || innerHorizontalBorder != defaultStyle.innerHorizontalBorder))
                sb.AppendFormat ("innerborder:{0};", InnerBorder ?? 0);
            else {
                if (innerHorizontalBorder != defaultStyle.innerHorizontalBorder)
                    sb.AppendFormat ("innerhborder:{0};", innerHorizontalBorder ?? 0);

                if (innerVerticalBorder != defaultStyle.innerVerticalBorder)
                    sb.AppendFormat ("innervborder:{0};", innerVerticalBorder ?? 0);
            }

            if (spacing != defaultStyle.spacing)
                sb.AppendFormat ("spacing:{0};", spacing ?? 0);

            if (innerVSpacing == innerHSpacing &&
                (innerVSpacing != defaultStyle.innerVSpacing || innerHSpacing != defaultStyle.innerHSpacing))
                sb.AppendFormat ("innerspacing:{0};", InnerSpacing ?? 0);
            else {
                if (innerHSpacing != defaultStyle.innerHSpacing)
                    sb.AppendFormat ("innerhspacing:{0};", innerHSpacing ?? 0);

                if (innerVSpacing != defaultStyle.innerVSpacing)
                    sb.AppendFormat ("innervspacing:{0};", innerVSpacing ?? 0);
            }

            if (HAlign != HorizontalAlignment.Left && HAlign != defaultStyle.HAlign) {
                switch (HAlign) {
                    case HorizontalAlignment.Right:
                        sb.Append ("halign:right;");
                        break;
                    case HorizontalAlignment.Center:
                        sb.Append ("halign:center;");
                        break;
                }
            }

            if (VAlign != VerticalAlignment.Top && VAlign != defaultStyle.VAlign)
                switch (VAlign) {
                    case VerticalAlignment.Bottom:
                        sb.Append ("valign:bottom;");
                        break;
                    case VerticalAlignment.Center:
                        sb.Append ("valign:center;");
                        break;
                }

            if (DrawEmpty != defaultStyle.DrawEmpty)
                sb.AppendFormat ("drawempty:{0};", DrawEmpty.ToString ().ToLowerInvariant ());

            if (AllowHBreak != defaultStyle.AllowHBreak)
                sb.AppendFormat ("allowhbreak:{0};", AllowHBreak.ToString ().ToLowerInvariant ());

            if (AllowVBreak != defaultStyle.AllowVBreak)
                sb.AppendFormat ("allowvbreak:{0};", AllowVBreak.ToString ().ToLowerInvariant ());

            if (!bgColor.Equal (defaultStyle.BgColor))
                sb.AppendFormat ("bgcolor:{0};", BgColor.ToHTMLColor ());

            if (!fgColor.Equal (defaultStyle.FgColor))
                sb.AppendFormat ("fgcolor:{0};", FgColor.ToHTMLColor ());

            if (!borderColor.Equals (defaultStyle.BorderColor))
                sb.AppendFormat ("bordercolor:{0};", BorderColor.ToHTMLColor ());

            return sb.ToString ().TrimEnd (';');
        }

        public override string ToString ()
        {
            return ToString (new ObjectStyle ());
        }

        private void OnPropertyChanged (string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler (this, new PropertyChangedEventArgs (propertyName));
        }

        #region ICloneable Members

        public object Clone ()
        {
            ObjectStyle clone = (ObjectStyle) MemberwiseClone ();
            clone.PropertyChanged = null;
            return clone;
        }

        #endregion
    }
}
