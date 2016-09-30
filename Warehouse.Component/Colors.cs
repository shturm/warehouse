//
// Colors.cs
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

using Cairo;

namespace Warehouse.Component
{
    public static class Colors
    {
        public static Color White
        {
            get { return new Color (1d, 1d, 1d); }
        }

        public static Color Black
        {
            get { return new Color (0d, 0d, 0d); }
        }

        public static Color Red
        {
            get { return new Color (1d, 0d, 0d); }
        }

        public static Color Green
        {
            get { return new Color (0d, 128 / 255d, 0d); }
        }

        public static Color Blue
        {
            get { return new Color (0d, 0d, 1d); }
        }

        public static Color DarkOrange
        {
            get { return new Color (1d, 140 / 255d, 0d); }
        }

        public static Color DarkGreen
        {
            get { return new Color (0d, 100 / 255d, 0d); }
        }

        public static Color DimGray
        {
            get { return new Color (105 / 255d, 105 / 255d, 105 / 255d); }
        }

        public static Color BlanchedAlmond
        {
            get { return new Color (255 / 255d, 235 / 255d, 205 / 255d); }
        }

        public static Color Honeydew
        {
            get { return new Color (240 / 255d, 255 / 255d, 240 / 255d); }
        }

        public static Color OldLace
        {
            get { return new Color (253 / 255d, 245 / 255d, 230 / 255d); }
        }
    }
}
