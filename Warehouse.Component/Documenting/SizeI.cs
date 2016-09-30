//
// SizeI.cs
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

using System.Globalization;
using Cairo;

namespace Warehouse.Component.Documenting
{
    public struct SizeI
    {
        public static readonly SizeI Empty = new SizeI ();
        private int width;
        private int height;

        public bool IsEmpty
        {
            get { return width == 0 && height == 0; }
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

        static SizeI ()
        {
        }

        public SizeI (SizeI size)
        {
            width = size.width;
            height = size.height;
        }

        //public SizeI (PointD pt)
        //{
        //    width = pt.X;
        //    height = pt.Y;
        //}

        public SizeI (int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public static explicit operator PointD (SizeI size)
        {
            return new PointD (size.Width, size.Height);
        }

        public static SizeI operator + (SizeI sz1, SizeI sz2)
        {
            return Add (sz1, sz2);
        }

        public static SizeI operator - (SizeI sz1, SizeI sz2)
        {
            return Subtract (sz1, sz2);
        }

        public static bool operator == (SizeI sz1, SizeI sz2)
        {
            return sz1.Width == sz2.Width && sz1.Height == sz2.Height;
        }

        public static bool operator != (SizeI sz1, SizeI sz2)
        {
            return !(sz1 == sz2);
        }

        public static SizeI Add (SizeI sz1, SizeI sz2)
        {
            return new SizeI (sz1.Width + sz2.Width, sz1.Height + sz2.Height);
        }

        public static SizeI Subtract (SizeI sz1, SizeI sz2)
        {
            return new SizeI (sz1.Width - sz2.Width, sz1.Height - sz2.Height);
        }

        public override bool Equals (object obj)
        {
            if (ReferenceEquals (null, obj))
                return false;

            return obj is SizeI && Equals ((SizeI) obj);
        }

        public bool Equals (SizeI other)
        {
            return width == other.width && height == other.height;
        }

        public override int GetHashCode ()
        {
            unchecked {
                return (width * 397) ^ height;
            }
        }

        public override string ToString ()
        {
            return string.Format ("{{Width={0}, Height={1}}}", width.ToString (CultureInfo.CurrentCulture), height.ToString (CultureInfo.CurrentCulture));
        }
    }
}
