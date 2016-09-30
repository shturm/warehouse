//
// CairoExtensions.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Created:
//   09/23/2008
//
// Copyright (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Cairo;
using Gdk;
using Color = Cairo.Color;

namespace Warehouse.Component
{
    [Flags]
    public enum CairoCorners
    {
        None = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomLeft = 4,
        BottomRight = 8,
        All = 15
    }

    public static class CairoExtensions
    {
        public static Pango.Layout CreateLayout (this Gtk.Widget widget, Context cairo_context)
        {
            Pango.Layout layout = cairo_context.CreateLayout ();
            layout.FontDescription = widget.PangoContext.FontDescription.Copy ();

            double resolution = -1;//widget.Screen.Resolution;
            if (resolution != -1) {
                Pango.Context context = layout.GetContext ();
                context.SetResolution (resolution);
                context.Dispose ();
            }

            return layout;
        }

        public static Surface CreateSurfaceForPixbuf (Cairo.Context cr, Gdk.Pixbuf pixbuf)
        {
            Surface surface = cr.Target.CreateSimilar (cr.Target.Content, pixbuf.Width, pixbuf.Height);
            Cairo.Context surface_cr = new Context (surface);
            Gdk.CairoHelper.SetSourcePixbuf (surface_cr, pixbuf, 0, 0);
            surface_cr.Paint ();
            ((IDisposable) surface_cr).Dispose ();
            return surface;
        }

        public static Pixbuf ToPixbuf (this ImageSurface surface)
        {
            byte [] cairoData = surface.Data.ToArray ();

            int i = 0;
            int pixels = surface.Height * surface.Width;

            for (int x = 0; x < pixels; x++) {
                byte alpha = cairoData [i + 3];

                byte r = ConvertColorChannel (cairoData [i + 2], alpha);
                byte g = ConvertColorChannel (cairoData [i + 1], alpha);
                byte b = ConvertColorChannel (cairoData [i + 0], alpha);

                cairoData [i + 0] = r;
                cairoData [i + 1] = g;
                cairoData [i + 2] = b;

                i += 4;
            }

            return new Pixbuf (cairoData, Colorspace.Rgb, true, 8, surface.Width,
                surface.Height, surface.Stride);
        }

        public static Surface ScaleSmooth (this Surface source, int width, int height, double zoomX, double zoomY, InterpType interpolation = InterpType.Hyper)
        {
            ImageSurface tempSurface = new ImageSurface (Format.ARGB32, width, height);
            var tempContext = new Context (tempSurface);
            tempContext.SetSourceSurface (source, 0, 0);
            tempContext.Paint ();

            Pixbuf pbSrc = tempSurface.ToPixbuf ();
            tempContext.DisposeAll ();

            using (Pixbuf pbDest = new Pixbuf (Colorspace.Rgb, true, 8,
                (int) (width * zoomX),
                (int) (height * zoomY))) {
                pbSrc.Scale (pbDest, 0, 0, pbDest.Width, pbDest.Height, 0, 0, zoomX, zoomY, interpolation);
                pbSrc.Dispose ();

                Surface cache = new ImageSurface (Format.ARGB32, pbDest.Width, pbDest.Height);
                var cacheContext = new Context (cache);
                CairoHelper.SetSourcePixbuf (cacheContext, pbDest, 0, 0);
                cacheContext.Paint ();
                cacheContext.DisposeContext ();

                return cache;
            }
        }

        public static Surface ScaleFast (this Surface source, int width, int height, double zoomX, double zoomY)
        {
            ImageSurface cacheSurface = new ImageSurface (Format.ARGB32,
                (int) (width * zoomX),
                (int) (height * zoomY));
            var cache = new Context (cacheSurface);
            cache.Scale (zoomX, zoomY);
            cache.Antialias = Antialias.Gray;
            cache.SetSourceSurface (source, 0, 0);
            cache.Paint ();
            cache.DisposeContext ();

            return cacheSurface;
        }

        private static byte ConvertColorChannel (byte src, byte alpha)
        {
            if (alpha <= 0)
                return 0;

            if (alpha == 255)
                return src;

            return (byte) (((src << 8) - src) / alpha);
        }

        public static Color AlphaBlend (Color ca, Color cb, double alpha)
        {
            return new Color (
                (1.0 - alpha) * ca.R + alpha * cb.R,
                (1.0 - alpha) * ca.G + alpha * cb.G,
                (1.0 - alpha) * ca.B + alpha * cb.B);
        }

        public static Color ToCairoColor (this Gdk.Color color, double alpha = 1.0)
        {
            return new Color (
                (color.Red >> 8) / 255.0,
                (color.Green >> 8) / 255.0,
                (color.Blue >> 8) / 255.0,
                alpha);
        }

        public static Color ToCairoColor (this System.Drawing.Color color)
        {
            return new Color (color.R / 255.0, color.G / 255.0, color.B / 255.0, color.A / 255.0);
        }

        public static System.Drawing.Color ToSDColor (this Color color)
        {
            return System.Drawing.Color.FromArgb (
                (int) (color.A * 255.0),
                (int) (color.R * 255.0),
                (int) (color.G * 255.0),
                (int) (color.B * 255.0));
        }

        public static string ToHTMLColor (this Color color, bool withAlpha = false)
        {
            if (withAlpha)
                return string.Format ("#{0:x2}{1:x2}{2:x2}{3:x2}",
                    (byte) (color.R * 255.0),
                    (byte) (color.G * 255.0),
                    (byte) (color.B * 255.0),
                    (byte) (color.A * 255.0));

            return string.Format ("#{0:x2}{1:x2}{2:x2}",
                (byte) (color.R * 255.0),
                (byte) (color.G * 255.0),
                (byte) (color.B * 255.0));
        }

        public static Color FromHTMLColor (string value)
        {
            Match match = Regex.Match (value, "#([a-fA-F0-9]{2})([a-fA-F0-9]{2})([a-fA-F0-9]{2})([a-fA-F0-9]{2})", RegexOptions.Compiled);
            if (match.Success)
                return new Color (
                    Convert.ToByte (match.Groups [1].Value, 16) / 255d,
                    Convert.ToByte (match.Groups [2].Value, 16) / 255d,
                    Convert.ToByte (match.Groups [3].Value, 16) / 255d,
                    Convert.ToByte (match.Groups [4].Value, 16) / 255d);

            match = Regex.Match (value, "#([a-fA-F0-9]{2})([a-fA-F0-9]{2})([a-fA-F0-9]{2})", RegexOptions.Compiled);
            if (match.Success)
                return new Color (
                    Convert.ToByte (match.Groups [1].Value, 16) / 255d,
                    Convert.ToByte (match.Groups [2].Value, 16) / 255d,
                    Convert.ToByte (match.Groups [3].Value, 16) / 255d);

            foreach (var property in typeof (Colors).GetProperties (BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty)) {
                if (!string.Equals (property.Name, value, StringComparison.CurrentCultureIgnoreCase))
                    continue;

                return (Color) property.GetValue (null, null);
            }

            return new Color ();
        }

        public static Gdk.Color ToGDKColor (this Color color)
        {
            return new Gdk.Color (
                (byte) (color.R * 255.0),
                (byte) (color.G * 255.0),
                (byte) (color.B * 255.0));
        }

        public static bool Equal (this Color color1, Color color2)
        {
            return color1.R == color2.R &&
                color1.G == color2.G &&
                color1.B == color2.B &&
                color1.A == color2.A;
        }

        public static System.Drawing.PointF ToSDGPoint (this PointD point)
        {
            return new System.Drawing.PointF ((float) point.X, (float) point.Y);
        }

        public static Gdk.Color GetGdkColor (this Color color)
        {
            return new Gdk.Color (
                (byte) (color.R * 255),
                (byte) (color.G * 255),
                (byte) (color.B * 255));
        }

        public static Color RgbToColor (uint rgbColor)
        {
            return RgbaToColor ((rgbColor << 8) | 0x000000ff);
        }

        public static Color RgbaToColor (uint rgbaColor)
        {
            return new Color (
                (byte) (rgbaColor >> 24) / 255.0,
                (byte) (rgbaColor >> 16) / 255.0,
                (byte) (rgbaColor >> 8) / 255.0,
                (byte) (rgbaColor & 0x000000ff) / 255.0);
        }

        public static bool IsDark (this Color color)
        {
            double h, s, b;
            GetHsb (color, out h, out s, out b);
            return b < 0.5;
        }

        public static void GetHsb (this Color color,
            out double hue,
            out double saturation,
            out double brightness)
        {
            double min, max;
            double red = color.R;
            double green = color.G;
            double blue = color.B;


            if (red > green) {
                max = Math.Max (red, blue);
                min = Math.Min (green, blue);
            } else {
                max = Math.Max (green, blue);
                min = Math.Min (red, blue);
            }

            brightness = (max + min) / 2;

            hue = 0;
            if (Math.Abs (max - min) < 0.0001)
                saturation = 0;
            else {
                saturation = brightness <= 0.5
                    ? (max - min) / (max + min)
                    : (max - min) / (2 - max - min);

                double delta = max - min;

                if (red == max)
                    hue = (green - blue) / delta;
                else if (green == max)
                    hue = 2 + (blue - red) / delta;
                else if (blue == max)
                    hue = 4 + (red - green) / delta;

                hue *= 60;
                if (hue < 0)
                    hue += 360;
            }
        }

        private static double Modula (double number, double divisor)
        {
            return ((int) number % divisor) + (number - (int) number);
        }

        public static Color ColorFromHsb (double hue, double saturation, double brightness)
        {
            double [] hue_shift = { 0, 0, 0 };
            double [] color_shift = { 0, 0, 0 };

            double m2 = brightness <= 0.5
                ? brightness * (1 + saturation)
                : brightness + saturation - brightness * saturation;

            double m1 = 2 * brightness - m2;

            hue_shift [0] = hue + 120;
            hue_shift [1] = hue;
            hue_shift [2] = hue - 120;

            color_shift [0] = color_shift [1] = color_shift [2] = brightness;

            int i = saturation == 0 ? 3 : 0;

            for (; i < 3; i++) {
                double m3 = hue_shift [i];

                if (m3 > 360)
                    m3 = Modula (m3, 360);
                else if (m3 < 0)
                    m3 = 360 - Modula (Math.Abs (m3), 360);

                if (m3 < 60)
                    color_shift [i] = m1 + (m2 - m1) * m3 / 60;
                else if (m3 < 180)
                    color_shift [i] = m2;
                else if (m3 < 240)
                    color_shift [i] = m1 + (m2 - m1) * (240 - m3) / 60;
                else
                    color_shift [i] = m1;
            }

            return new Color (color_shift [0], color_shift [1], color_shift [2]);
        }

        public static Color ColorShade (this Color @base, double ratio)
        {
            double h, s, b;

            GetHsb (@base, out h, out s, out b);

            b = Math.Max (Math.Min (b * ratio, 1), 0);
            s = Math.Max (Math.Min (s * ratio, 1), 0);

            Color color = ColorFromHsb (h, s, b);
            color.A = @base.A;
            return color;
        }

        public static Color ColorAdjustBrightness (Color @base, double br)
        {
            double h, s, b;
            GetHsb (@base, out h, out s, out b);
            b = Math.Max (Math.Min (br, 1), 0);
            return ColorFromHsb (h, s, b);
        }

        public static void RoundedRectangle (this Context cr, double x, double y, double w, double h, double r, CairoCorners corners = CairoCorners.All, bool topBottomFallsThrough = false)
        {
            if (topBottomFallsThrough && corners == CairoCorners.None) {
                cr.MoveTo (x, y - r);
                cr.LineTo (x, y + h + r);
                cr.MoveTo (x + w, y - r);
                cr.LineTo (x + w, y + h + r);
                return;
            }

            if (r < 0.0001 || corners == CairoCorners.None) {
                cr.Rectangle (x, y, w, h);
                return;
            }

            if ((corners & (CairoCorners.TopLeft | CairoCorners.TopRight)) == 0 && topBottomFallsThrough) {
                y -= r;
                h += r;
                cr.MoveTo (x + w, y);
            } else {
                if ((corners & CairoCorners.TopLeft) != 0)
                    cr.MoveTo (x + r, y);
                else
                    cr.MoveTo (x, y);

                if ((corners & CairoCorners.TopRight) != 0)
                    cr.Arc (x + w - r, y + r, r, Math.PI * 1.5, Math.PI * 2);
                else
                    cr.LineTo (x + w, y);
            }

            if ((corners & (CairoCorners.BottomLeft | CairoCorners.BottomRight)) == 0 && topBottomFallsThrough) {
                h += r;
                cr.LineTo (x + w, y + h);
                cr.MoveTo (x, y + h);
                cr.LineTo (x, y + r);
                cr.Arc (x + r, y + r, r, Math.PI, Math.PI * 1.5);
            } else {
                if ((corners & CairoCorners.BottomRight) != 0)
                    cr.Arc (x + w - r, y + h - r, r, 0, Math.PI * 0.5);
                else
                    cr.LineTo (x + w, y + h);

                if ((corners & CairoCorners.BottomLeft) != 0)
                    cr.Arc (x + r, y + h - r, r, Math.PI * 0.5, Math.PI);
                else
                    cr.LineTo (x, y + h);

                if ((corners & CairoCorners.TopLeft) != 0)
                    cr.Arc (x + r, y + r, r, Math.PI, Math.PI * 1.5);
                else
                    cr.LineTo (x, y);
            }
        }

        public static void DisposeAll (this Context context)
        {
            ((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();
        }

        public static void DisposeContext (this Context context)
        {
            ((IDisposable) context).Dispose ();
        }

        public static void DisposeSurface (this Surface surface)
        {
            ((IDisposable) surface).Dispose ();
        }

        private struct CairoInteropCall
        {
            public string Name;
            public MethodInfo ManagedMethod;
            public bool CallNative;

            public CairoInteropCall (string name)
            {
                Name = name;
                ManagedMethod = null;
                CallNative = false;
            }
        }

        private static bool CallCairoMethod (Context cr, ref CairoInteropCall call)
        {
            if (call.ManagedMethod == null && !call.CallNative) {
                MemberInfo [] members = typeof (Context).GetMember (call.Name, MemberTypes.Method,
                    BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public);

                if (members != null && members.Length > 0 && members [0] is MethodInfo) {
                    call.ManagedMethod = (MethodInfo) members [0];
                } else {
                    call.CallNative = true;
                }
            }

            if (call.ManagedMethod != null) {
                call.ManagedMethod.Invoke (cr, null);
                return true;
            }

            return false;
        }

        private static bool native_push_pop_exists = true;

        [DllImport ("libcairo.so.2")]
        private static extern void cairo_push_group (IntPtr ptr);
        private static CairoInteropCall cairo_push_group_call = new CairoInteropCall ("PushGroup");

        public static void PushGroup (Cairo.Context cr)
        {
            if (!native_push_pop_exists) {
                return;
            }

            try {
                if (!CallCairoMethod (cr, ref cairo_push_group_call)) {
                    cairo_push_group (cr.Handle);
                }
            } catch {
                native_push_pop_exists = false;
            }
        }

        [DllImport ("libcairo.so.2")]
        private static extern void cairo_pop_group_to_source (IntPtr ptr);
        private static CairoInteropCall cairo_pop_group_to_source_call = new CairoInteropCall ("PopGroupToSource");

        public static void PopGroupToSource (Cairo.Context cr)
        {
            if (!native_push_pop_exists) {
                return;
            }

            try {
                if (!CallCairoMethod (cr, ref cairo_pop_group_to_source_call)) {
                    cairo_pop_group_to_source (cr.Handle);
                }
            } catch (EntryPointNotFoundException) {
                native_push_pop_exists = false;
            }
        }

        public static bool Same (this Color a, Color b)
        {
            return Math.Abs (a.R - b.R) < 0.00001 &&
                Math.Abs (a.G - b.G) < 0.00001 &&
                Math.Abs (a.B - b.B) < 0.00001 &&
                Math.Abs (a.A - b.A) < 0.00001;
        }

        public static Color GetPixel (this ImageSurface sf, int x, int y)
        {
            int pixelSize;
            switch (sf.Format) {
                case Format.Argb32:
                    pixelSize = 4;
                    break;
                case Format.Rgb24:
                    pixelSize = 3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException ();
            }

            sf.Flush ();
            int start = x * pixelSize + y * sf.Stride;
            byte [] data = sf.Data;

            switch (sf.Format) {
                case Format.Argb32:
                    return new Color (
                        data [start + 1] / 255.0,
                        data [start + 2] / 255.0,
                        data [start + 3] / 255.0,
                        data [start + 0] / 255.0);
                case Format.Rgb24:
                    return new Color (
                        data [start + 0] / 255.0,
                        data [start + 1] / 255.0,
                        data [start + 2] / 255.0);
                default:
                    throw new ArgumentOutOfRangeException ();
            }
        }
    }
}
