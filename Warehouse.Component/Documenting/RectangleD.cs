//
// RectangleD.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   12.09.2014
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
using Cairo;
using Warehouse.Data;

namespace Warehouse.Component.Documenting
{
    public struct RectangleD
    {
        public static readonly RectangleD Empty = new RectangleD ();
        private double x;
        private double y;
        private double width;
        private double height;

        public PointD Location
        {
            get
            {
                return new PointD (x, y);
            }
            set
            {
                x = value.X;
                y = value.Y;
            }
        }

        public SizeD Size
        {
            get
            {
                return new SizeD (width, height);
            }
            set
            {
                width = value.Width;
                height = value.Height;
            }
        }

        public double X
        {
            get { return x; }
            set { x = value; }
        }

        public double Y
        {
            get { return y; }
            set { y = value; }
        }

        public double Width
        {
            get { return width; }
            set { width = value; }
        }

        public double Height
        {
            get { return height; }
            set { height = value; }
        }

        public double Left
        {
            get { return x; }
        }

        public double Top
        {
            get { return y; }
        }

        public double Right
        {
            get { return x + width; }
        }

        public double Bottom
        {
            get { return y + height; }
        }

        public bool IsEmpty
        {
            get { return width <= 0d || height <= 0d; }
        }

        public RectangleD (double x, double y, double width, double height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public RectangleD (PointD location, SizeD size)
        {
            x = location.X;
            y = location.Y;
            width = size.Width;
            height = size.Height;
        }

        public static bool operator == (RectangleD left, RectangleD right)
        {
            return left.X.IsEqualTo (right.X) && left.Y.IsEqualTo (right.Y) && left.Width.IsEqualTo (right.Width) && left.Height.IsEqualTo (right.Height);
        }

        public static bool operator != (RectangleD left, RectangleD right)
        {
            return !(left == right);
        }

        public override bool Equals (object obj)
        {
            if (!(obj is RectangleD))
                return false;

            RectangleD rectangleD = (RectangleD) obj;
            return rectangleD.X.IsEqualTo (x) && rectangleD.Y.IsEqualTo (y) && rectangleD.Width.IsEqualTo (width) && rectangleD.Height.IsEqualTo (height);
        }

        public bool Contains (double x, double y)
        {
            return this.x <= x && x < this.x + width && this.y <= y && y < this.y + height;
        }

        public bool Contains (PointD pt)
        {
            return Contains (pt.X, pt.Y);
        }

        public bool Contains (RectangleD rect)
        {
            return x <= rect.X && rect.X + rect.Width <= X + Width && Y <= rect.Y && rect.Y + rect.Height <= Y + Height;
        }

        public override int GetHashCode ()
        {
            return (int) (uint) x ^ ((int) (uint) y << 13 | (int) ((uint) y >> 19)) ^ ((int) (uint) width << 26 | (int) ((uint) width >> 6)) ^ ((int) (uint) height << 7 | (int) ((uint) height >> 25));
        }

        public void Inflate (double x, double y)
        {
            this.x -= x;
            this.y -= y;
            width += 2d * x;
            height += 2d * y;
        }

        public void Inflate (SizeD size)
        {
            Inflate (size.Width, size.Height);
        }

        public static RectangleD Inflate (RectangleD rect, double x, double y)
        {
            RectangleD rectangleD = rect;
            rectangleD.Inflate (x, y);
            return rectangleD;
        }

        public void Intersect (RectangleD rect)
        {
            RectangleD rectangleD = Intersect (rect, this);
            x = rectangleD.X;
            y = rectangleD.Y;
            width = rectangleD.Width;
            height = rectangleD.Height;
        }

        public static RectangleD Intersect (RectangleD a, RectangleD b)
        {
            double x = Math.Max (a.X, b.X);
            double num1 = Math.Min (a.X + a.Width, b.X + b.Width);
            double y = Math.Max (a.Y, b.Y);
            double num2 = Math.Min (a.Y + a.Height, b.Y + b.Height);
            return num1 >= x && num2 >= y ?
                new RectangleD (x, y, num1 - x, num2 - y) :
                Empty;
        }

        public bool IntersectsWith (RectangleD rect)
        {
            return rect.X < x + width && x < rect.X + rect.Width && rect.Y < y + height && y < rect.Y + rect.Height;
        }

        public static RectangleD Union (RectangleD a, RectangleD b)
        {
            double x = Math.Min (a.X, b.X);
            double num1 = Math.Max (a.X + a.Width, b.X + b.Width);
            double y = Math.Min (a.Y, b.Y);
            double num2 = Math.Max (a.Y + a.Height, b.Y + b.Height);
            return new RectangleD (x, y, num1 - x, num2 - y);
        }

        public void Offset (PointD pos)
        {
            this.Offset (pos.X, pos.Y);
        }

        public void Offset (double x, double y)
        {
            X += x;
            Y += y;
        }

        public override string ToString ()
        {
            return string.Format ("{{X={0},Y={1},Width={2},Height={3}}}", X.ToString (CultureInfo.CurrentCulture), Y.ToString (CultureInfo.CurrentCulture), Width.ToString (CultureInfo.CurrentCulture), Height.ToString (CultureInfo.CurrentCulture));
        }
    }
}
