//
// CodeGenerator.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   03.11.2011
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
using System.Text.RegularExpressions;
using Warehouse.Data;

namespace Warehouse.Business
{
    public static class CodeGenerator
    {
        private static readonly Regex rex = new Regex ("^([\\w-+=\\.\\*&]*)(#+)([\\w-+=\\.\\*&]*)$", RegexOptions.Compiled);

        public static string GenerateCode (string pattern, ulong number)
        {
            Match match = rex.Match (pattern);
            if (!match.Success)
                throw new ArgumentException ("codeMask");

            string prefix = match.Groups [1].Value;
            string digits = match.Groups [2].Value;
            string suffix = match.Groups [3].Value;

            return string.Format ("{0}{1}{2}", prefix, Number.ToEditString (number).PadLeft (digits.Length, '0'), suffix);
        }

        public static bool PatternIsValid (string pattern)
        {
            return rex.IsMatch(pattern);
        }
    }
}
