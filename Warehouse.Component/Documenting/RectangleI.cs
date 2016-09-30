//
// RectangleI.cs
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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Cairo;
using Warehouse.Data;

namespace Warehouse.Component.Documenting
{
    public struct RectangleI
    {
        public static readonly RectangleI Empty = new RectangleI ();
        private int x;
        private int y;
        private int width;
        private int height;

        public Point Location
        {
            get
            {
                return new Point (x, y);
            }
            set
            {
                x = value.X;
                y = value.Y;
            }
        }

        public SizeI Size
        {
            get
            {
                return new SizeI (width, height);
            }
            set
            {
                width = value.Width;
                height = value.Height;
            }
        }

        public int X
        {
            get { return x; }
            set { x = value; }
        }

        public int Y
        {
            get { return y; }
            set { y = value; }
        }

        public int Width
        {
            get { return width; }
            set { width = value; }
        }

        public int Height
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

        public RectangleI (int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public RectangleI (Point location, SizeI size)
        {
            x = location.X;
            y = location.Y;
            width = size.Width;
            height = size.Height;
        }

        public static bool operator == (RectangleI left, RectangleI right)
        {
            return left.X == right.X && left.Y == right.Y && left.Width == right.Width && left.Height == right.Height;
        }

        public static bool operator != (RectangleI left, RectangleI right)
        {
            return !(left == right);
        }

        public override bool Equals (object obj)
        {
            if (!(obj is RectangleI))
                return false;

            RectangleI rectangleI = (RectangleI) obj;
            return rectangleI.X == x && rectangleI.Y == y && rectangleI.Width == width && rectangleI.Height == height;
        }

        public bool Contains (double x, double y)
        {
            return this.x <= x && x < this.x + width && this.y <= y && y < this.y + height;
        }

        public bool Contains (Point pt)
        {
            return Contains (pt.X, pt.Y);
        }

        public bool Contains (RectangleI rect)
        {
            return x <= rect.X && rect.X + rect.Width <= X + Width && Y <= rect.Y && rect.Y + rect.Height <= Y + Height;
        }

        public override int GetHashCode ()
        {
            return (int) (uint) x ^ ((int) (uint) y << 13 | (int) ((uint) y >> 19)) ^ ((int) (uint) width << 26 | (int) ((uint) width >> 6)) ^ ((int) (uint) height << 7 | (int) ((uint) height >> 25));
        }

        public void Inflate (int x, int y)
        {
            this.x -= x;
            this.y -= y;
            width += 2 * x;
            height += 2 * y;
        }

        public void Inflate (SizeI size)
        {
            Inflate (size.Width, size.Height);
        }

        public static RectangleI Inflate (RectangleI rect, int x, int y)
        {
            RectangleI rectangleI = rect;
            rectangleI.Inflate (x, y);
            return rectangleI;
        }

        public void Intersect (RectangleI rect)
        {
            RectangleI RectangleI = Intersect (rect, this);
            x = RectangleI.X;
            y = RectangleI.Y;
            width = RectangleI.Width;
            height = RectangleI.Height;
        }

        public static RectangleI Intersect (RectangleI a, RectangleI b)
        {
            int x = Math.Max (a.X, b.X);
            int num1 = Math.Min (a.X + a.Width, b.X + b.Width);
            int y = Math.Max (a.Y, b.Y);
            int num2 = Math.Min (a.Y + a.Height, b.Y + b.Height);
            return num1 >= x && num2 >= y ?
                new RectangleI (x, y, num1 - x, num2 - y) :
                Empty;
        }

        public bool IntersectsWith (RectangleI rect)
        {
            return rect.X < x + width && x < rect.X + rect.Width && rect.Y < y + height && y < rect.Y + rect.Height;
        }

        public static RectangleI Union (RectangleI a, RectangleI b)
        {
            int x = Math.Min (a.X, b.X);
            int num1 = Math.Max (a.X + a.Width, b.X + b.Width);
            int y = Math.Min (a.Y, b.Y);
            int num2 = Math.Max (a.Y + a.Height, b.Y + b.Height);
            return new RectangleI (x, y, num1 - x, num2 - y);
        }

        public void Offset (Point pos)
        {
            this.Offset (pos.X, pos.Y);
        }

        public void Offset (int x, int y)
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
