//
// IDrawingProvider.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   08/03/2006
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
using System.Drawing;
using Cairo;
using Gdk;
using Color = Cairo.Color;

namespace Warehouse.Component.Documenting
{
    public interface IDrawingProvider : IDisposable
    {
        float DrawingScaleX { get; set; }
        float DrawingScaleY { get; set; }
        void SaveClip ();
        void RestoreClip ();
        void IntersectClip (RectangleD newClip);
        void DrawLine (double x1, double y1, double x2, double y2, double thickness, Color color);
        void DrawInsetRectangle (double x, double y, double width, double height, double border, Color color);
        void FillRectangle (double x, double y, double width, double height, Color color);
        void DrawString (RectangleD layout, string text, Color color, ObjectFont font, StringTrimming trimming, HorizontalAlignment hAlign = HorizontalAlignment.Left, bool wrap = false);
        void DrawImage (Pixbuf image, RectangleD layout, RectangleD imagePart, SizeMode sizeMode = SizeMode.Fit);
        void DrawSurface (Surface surface, RectangleD layout, RectangleD imagePart, InterpType interpolation);
        SizeD MeasureString (string text, ObjectFont font, double? width);
    }
}
