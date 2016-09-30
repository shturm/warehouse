//
// FontConverter.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   01.27.2011
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

namespace Warehouse.Component.Documenting
{
    /// <summary>
    /// Converts a font of a printable object to its <c>Pango</c> representation.
    /// </summary>
    public static class FontConverter
    {
        /// <summary>
        /// Gets the name of the specified font in the format used by <c>Pango</c>.
        /// </summary>
        /// <param name="font">The font.</param>
        /// <returns>The name of the specified font in the format used by <c>Pango</c>.</returns>
        public static string GetPangoName (ObjectFont font)
        {
            return GetPangoName (font.Name, font.Bold, font.Italic, font.Size);
        }

        /// <summary>
        /// Gets the name of the font with the specified properties in the format used by <c>Pango</c>.
        /// </summary>
        /// <param name="fontName">The name of the font.</param>
        /// <param name="bold">if set to <c>true</c> the font is bold.</param>
        /// <param name="italic">if set to <c>true</c> the font is italic.</param>
        /// <param name="size">The size of the font.</param>
        /// <returns>The name of the font with the specified properties in the format used by <c>Pango</c></returns>
        public static string GetPangoName (string fontName, bool bold, bool italic, double size)
        {
            return string.Format ("{0}, {1}{2}{3}", fontName, bold ? "Bold " : string.Empty, italic ? "Italic " : string.Empty, size);
        }
    }
}
