//
// MarginsI.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   12.18.2014
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
using Gtk;
using Warehouse.Data;

namespace Warehouse.Component.Documenting
{
    public class MarginsI : ICloneable
    {
        private int left;
        private int right;
        private int top;
        private int bottom;
        private Unit unitType;

        public int Left
        {
            get { return left; }
            set
            {
                CheckMargin (value, "Left");
                left = value;
            }
        }

        public int Right
        {
            get { return right; }
            set
            {
                CheckMargin (value, "Right");
                right = value;
            }
        }

        public int Top
        {
            get { return top; }
            set
            {
                CheckMargin (value, "Top");
                top = value;
            }
        }

        public int Bottom
        {
            get { return bottom; }
            set
            {
                CheckMargin (value, "Bottom");
                bottom = value;
            }
        }

        public Unit UnitType
        {
            get { return unitType; }
            set { unitType = value; }
        }

        public MarginsI ()
            : this (100, 100, 100, 100)
        {
        }

        public MarginsI (int left, int right, int top, int bottom, Unit unit = Unit.Pixel)
        {
            CheckMargin (left, "left");
            CheckMargin (right, "right");
            CheckMargin (top, "top");
            CheckMargin (bottom, "bottom");

            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
            unitType = unit;
        }

        private void CheckMargin (int margin, string name)
        {
            if (margin < 0)
                throw new ArgumentException ("Margin value cannot be negative", name);
        }

        public object Clone ()
        {
            return MemberwiseClone ();
        }

        public override bool Equals (object obj)
        {
            MarginsI margins = obj as MarginsI;
            if (margins == this)
                return true;

            if (margins == null)
                return false;

            return margins.Left == left &&
                margins.Right == right &&
                margins.Top == top &&
                margins.Bottom == bottom;
        }

        public override int GetHashCode ()
        {
            // return HashCodes.Combine(left, right, top, bottom);
            uint left = (uint) Left;
            uint right = (uint) Right;
            uint top = (uint) Top;
            uint bottom = (uint) Bottom;

            uint result = left ^
                ((right << 13) | (right >> 19)) ^
                ((top << 26) | (top >> 6)) ^
                ((bottom << 7) | (bottom >> 25));

            return unchecked ((int) result);
        }

        public static bool operator == (MarginsI m1, MarginsI m2)
        {
            if (ReferenceEquals (m1, null) != ReferenceEquals (m2, null))
                return false;

            if (ReferenceEquals (m1, null))
                return true;
            
            return m1.Left == m2.Left &&
                m1.Top == m2.Top &&
                m1.Right == m2.Right &&
                m1.Bottom == m2.Bottom;
        }

        public static bool operator != (MarginsI m1, MarginsI m2)
        {
            return !(m1 == m2);
        }

        public override string ToString ()
        {
            return string.Format ("[Margins" + " Left={0} Right={1} Top={2} Bottom={3}]",
                left.ToString (CultureInfo.InvariantCulture),
                right.ToString (CultureInfo.InvariantCulture),
                top.ToString (CultureInfo.InvariantCulture),
                bottom.ToString (CultureInfo.InvariantCulture));
        }
    }
}
