//
// EditNewItemPage.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/23/2008
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

using Gtk;
using Mono.Addins;
using Item = Warehouse.Business.Entities.Item;

namespace Warehouse.Presentation.Widgets
{
    [TypeExtensionPoint ("/Warehouse/Presentation/EditNewItem")]
    public abstract class EditNewItemPage : EventBox
    {
        public abstract Widget PageLabel { get;}
        public abstract int Priority { get; }
        public int Index { get; set; }
        public abstract void LoadSettings (Item item);
        public abstract bool SaveSettings (Item item);
    }
}
