//
// DrawingProviderCairo.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/04/2007
//
// 2006-2010 (C) Microinvest, http://www.microinvest.net
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
using System.Drawing;
using Cairo;
using Gdk;
using Pango;
using Warehouse.Component.Documenting;
using Warehouse.Data;
using CairoHelper = Gdk.CairoHelper;
using Context = Cairo.Context;
using Layout = Pango.Layout;
using WrapMode = Pango.WrapMode;

namespace Warehouse.Component.Printing
{
    public class DrawingProviderCairo : IDrawingProvider
    {
        private readonly Context cairoContext;
        private readonly Layout pangoLayout;

        private float drawingScaleX = 1;
        public float DrawingScaleX
        {
            set { drawingScaleX = value; }
            get { return drawingScaleX; }
        }

        private float drawingScaleY = 1;
        public float DrawingScaleY
        {
            set { drawingScaleY = value; }
            get { return drawingScaleY; }
        }

        public DrawingProviderCairo (Context cairoContext, Layout pangoLayout)
        {
            this.cairoContext = cairoContext;
            this.pangoLayout = pangoLayout;
        }

        public void SaveClip ()
        {
            cairoContext.Save ();
        }

        public void IntersectClip (RectangleD newClip)
        {
            cairoContext.Rectangle (newClip.X * drawingScaleX,
                newClip.Y * drawingScaleY,
                newClip.Width * drawingScaleX,
                newClip.Height * drawingScaleY);
            cairoContext.Clip ();
        }

        public void RestoreClip ()
        {
            cairoContext.Restore ();
        }

        public void DrawLine (double x1, double y1, double x2, double y2, double thickness, Cairo.Color color)
        {
            cairoContext.LineWidth = thickness * drawingScaleX;
            cairoContext.Antialias = Antialias.None;
            cairoContext.MoveTo (x1 * drawingScaleX, y1 * drawingScaleY);
            cairoContext.LineTo (x2 * drawingScaleX, y2 * drawingScaleY);
            cairoContext.Color = color;
            cairoContext.Stroke ();
        }

        public void DrawInsetRectangle (double x, double y, double width, double height, double border, Cairo.Color color)
        {
            x *= drawingScaleX;
            y *= drawingScaleY;
            width *= drawingScaleX;
            height *= drawingScaleY;
            border *= drawingScaleX;

            float posOffset = (float) Math.Floor (border / 2);
            x += posOffset;
            y += posOffset;
            width -= 2 * posOffset;
            height -= 2 * posOffset;

            if ((border % 2) == 1) {
                if (width > 1)
                    width--;

                if (height > 1)
                    height--;
            }

            cairoContext.LineWidth = border;
            cairoContext.Rectangle (x, y, width, height);
            cairoContext.Color = color;
            cairoContext.Stroke ();
        }

        public void FillRectangle (double x, double y, double width, double height, Cairo.Color color)
        {
            x *= drawingScaleX;
            y *= drawingScaleY;
            width *= drawingScaleX;
            height *= drawingScaleY;

            cairoContext.Rectangle (x, y, width, height);
            cairoContext.Color = color;
            cairoContext.Fill ();
        }

        public void DrawString (RectangleD layout, string text, Cairo.Color color, ObjectFont font, StringTrimming trimming, HorizontalAlignment hAlign = HorizontalAlignment.Left, bool wrap = false)
        {
            double x = Math.Ceiling (layout.X * drawingScaleX);
            double y = Math.Ceiling (layout.Y * drawingScaleY);
            double width = Math.Floor (layout.Width * drawingScaleX);
            double height = Math.Floor (layout.Height * drawingScaleY);

            SetupPangoLayout (text, font, trimming, wrap ? (float?) width : null);

            cairoContext.Save ();
            cairoContext.Rectangle (x, y, width, height);
            cairoContext.Clip ();
            cairoContext.Antialias = Antialias.None;

            Pango.Rectangle ink, logical;
            pangoLayout.GetPixelExtents (out ink, out logical);
            if (ink.X < 0) {
                x -= ink.X;
                logical.Width -= ink.X;
            }

            switch (hAlign) {
                case HorizontalAlignment.Left:
                    pangoLayout.Alignment = Alignment.Left;
                    break;

                case HorizontalAlignment.Center:
                    pangoLayout.Alignment = Alignment.Center;
                    if (width > logical.Width)
                        x += (width - logical.Width) / 2;
                    break;

                case HorizontalAlignment.Right:
                    pangoLayout.Alignment = Alignment.Right;
                    if (width > logical.Width)
                        x += width - logical.Width;
                    break;
            }

            cairoContext.MoveTo (x, y);
            cairoContext.Color = color;
            cairoContext.ShowLayout (pangoLayout);

            cairoContext.Restore ();
        }

        public void DrawImage (Pixbuf image, RectangleD layout, RectangleD imagePart, SizeMode sizeMode = SizeMode.Fit)
        {
            double x = layout.X * drawingScaleX;
            double y = layout.Y * drawingScaleY;
            double width = layout.Width * drawingScaleX;
            double height = layout.Height * drawingScaleY;

            if (width.IsZero () || height.IsZero ())
                return;

            double scaleX = width / imagePart.Width;
            double scaleY = height / imagePart.Height;
            using (Pixbuf pbCropped = new Pixbuf (Colorspace.Rgb, true, 8, (int) imagePart.Width, (int) imagePart.Height)) {
                image.CopyArea ((int) imagePart.X, (int) imagePart.Y, (int) imagePart.Width, (int) imagePart.Height, pbCropped, 0, 0);

                using (Pixbuf pbScaled = new Pixbuf (Colorspace.Rgb, true, 8, (int) width, (int) height)) {
                    pbCropped.Scale (pbScaled, 0, 0, pbScaled.Width, pbScaled.Height,
                        0, 0, scaleX, scaleY, InterpType.Hyper);

                    CairoHelper.SetSourcePixbuf (cairoContext, pbScaled, x, y);
                    cairoContext.Paint ();
                }
            }
        }

        public void DrawSurface (Surface surface, RectangleD layout, RectangleD imagePart, InterpType interpolation)
        {
            double x = layout.X * drawingScaleX;
            double y = layout.Y * drawingScaleY;
            double width = layout.Width * drawingScaleX;
            double height = layout.Height * drawingScaleY;

            if (width.IsZero () || height.IsZero ())
                return;

            double scaleX = width / imagePart.Width;
            double scaleY = height / imagePart.Height;
            using (Surface scaleFast = surface.ScaleSmooth ((int) imagePart.Width, (int) imagePart.Height, scaleX, scaleY, interpolation)) {
                cairoContext.SetSourceSurface (scaleFast, (int) x, (int) y);
                cairoContext.Paint ();
            }
        }

        public SizeD MeasureString (string text, ObjectFont font, double? width)
        {
            bool emptyString = string.IsNullOrWhiteSpace (text);
            if (emptyString)
                text = "W";
            SetupPangoLayout (text, font, StringTrimming.Character, width != null ? width * drawingScaleX : null);

            Pango.Rectangle ink, logical;
            pangoLayout.GetPixelExtents (out ink, out logical);
            if (ink.X < 0)
                logical.Width -= ink.X;
            if (emptyString)
                logical.Width = 0;

            return new SizeD (logical.Width / drawingScaleX, logical.Height / drawingScaleY);
        }

        private void SetupPangoLayout (string text, ObjectFont font, StringTrimming trimming, double? width)
        {
            PangoStyle style = new PangoStyle
                {
                    ExactSize = font.Size,
                    FontFamily = DataHelper.GetPreferredFont (text) ?? (font.NameSpecified ? font.Name : DataHelper.DefaultDocumentsFont)
                };
            if (font.Italic)
                style.Italic = true;
            if (font.Bold)
                style.Bold = true;
            if (font.Underline)
                style.Underline = true;
            if (font.StrikeThrought)
                style.Strikethrough = true;

            pangoLayout.SetMarkup (style.GetMarkup (text));
            pangoLayout.Ellipsize = EllipsizeMode.None;
            pangoLayout.Wrap = WrapMode.Word;
            if (width != null)
                pangoLayout.Width = (int) (width * Scale.PangoScale);
            else {
                pangoLayout.Width = -1;
                if (trimming != StringTrimming.None)
                    pangoLayout.Ellipsize = EllipsizeMode.End;
            }
        }

        #region IDisposable Members

        public void Dispose ()
        {
            pangoLayout.Dispose ();
        }

        #endregion
    }
}
