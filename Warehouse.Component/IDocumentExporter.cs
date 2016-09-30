//
// IDocumentExporter.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   03.23.2011
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

using System.Collections.Generic;
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Component.Documenting;

namespace Warehouse.Component
{
    [TypeExtensionPoint ("/Warehouse/Component/DocumentExchange")]
    public interface IDocumentExporter : IDataExchanger
    {
        void Export (string file, bool? toFile, string email, string emailSubject, string title, IList<Form> document, bool portrait, bool recalculateForm, PrintSettings printSettings, MarginsI margins);
    }
}
