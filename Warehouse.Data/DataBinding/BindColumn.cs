//
// BindColumn.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   05/22/2009
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

namespace Warehouse.Data.DataBinding
{
    public class BindColumn
    {
        private readonly int index;
        private readonly string name;
        private readonly Type type;
        private readonly string caption;
        private readonly string resolvedName;

        public int Index
        {
            get { return index; }
        }

        public string Name
        {
            get { return name; }
        }

        public Type Type
        {
            get { return type; }
        }

        public string Caption
        {
            get { return string.IsNullOrEmpty (caption) ? name : caption; }
        }

        public string ResolvedName
        {
            get { return string.IsNullOrEmpty (resolvedName) ? name : resolvedName; }
        }

        internal BindColumn (int index, string name, Type type, string caption, string resolvedName)
        {
            this.index = index;
            this.type = type;
            this.name = name;
            this.caption = caption;
            this.resolvedName = resolvedName;
        }
    }
}
