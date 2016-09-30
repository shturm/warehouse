//
// ICustomKeyShortcut.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   08.16.2010
//
// 2006-2010 (C) Microinvest, http://www.microinvest.net
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

using Gdk;
using Mono.Addins;

namespace Warehouse.Presentation
{
    [TypeExtensionPoint ("/Warehouse/Presentation/KeyShortcuts")]
    public interface ICustomKeyShortcut
    {
        string Parent { get; }
        string Path { get; }
        string Label { get; }
        Key DefaultKey { get; }
        ModifierType DefaultModifier { get; }
        int Ordinal { get; }
    }
}
