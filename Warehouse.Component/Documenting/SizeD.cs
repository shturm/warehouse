//
// SizeD.cs
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
using Warehouse.Data;

namespace Warehouse.Component.Documenting
{
    public struct SizeD
    {
        public static readonly SizeD Empty = new SizeD ();
        private double width;
        private double height;

        public bool IsEmpty
        {
            get { return width.IsZero () && height.IsZero (); }
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

        static SizeD ()
        {
        }

        public SizeD (SizeD size)
        {
            width = size.width;
            height = size.height;
        }

        public SizeD (PointD pt)
        {
            width = pt.X;
            height = pt.Y;
        }

        public SizeD (double width, double height)
        {
            this.width = width;
            this.height = height;
        }

        public static explicit operator PointD (SizeD size)
        {
            return new PointD (size.Width, size.Height);
        }

        public static SizeD operator + (SizeD sz1, SizeD sz2)
        {
            return Add (sz1, sz2);
        }

        public static SizeD operator - (SizeD sz1, SizeD sz2)
        {
            return Subtract (sz1, sz2);
        }

        public static bool operator == (SizeD sz1, SizeD sz2)
        {
            return sz1.Width.IsEqualTo (sz2.Width) && sz1.Height.IsEqualTo (sz2.Height);
        }

        public static bool operator != (SizeD sz1, SizeD sz2)
        {
            return !(sz1 == sz2);
        }

        public static SizeD Add (SizeD sz1, SizeD sz2)
        {
            return new SizeD (sz1.Width + sz2.Width, sz1.Height + sz2.Height);
        }

        public static SizeD Subtract (SizeD sz1, SizeD sz2)
        {
            return new SizeD (sz1.Width - sz2.Width, sz1.Height - sz2.Height);
        }

        public override bool Equals (object obj)
        {
            if (ReferenceEquals (null, obj))
                return false;

            return obj is SizeD && Equals ((SizeD) obj);
        }

        public bool Equals (SizeD other)
        {
            return width.Equals (other.width) && height.Equals (other.height);
        }

        public override int GetHashCode ()
        {
            unchecked {
                return (width.GetHashCode () * 397) ^ height.GetHashCode ();
            }
        }

        public override string ToString ()
        {
            return string.Format ("{{Width={0}, Height={1}}}", width.ToString (CultureInfo.CurrentCulture), height.ToString (CultureInfo.CurrentCulture));
        }
    }
}
