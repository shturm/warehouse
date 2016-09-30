//
// IFormObjectCreator.cs
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

using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Warehouse.Component.Documenting
{
    public interface IFormObjectCreator
    {
        Form CreateForm ();

        Form CreateForm (XElement node, object obj);

        Section CreateSection ();

        Section CreateSection (XElement node, Hashtable boundFields);

        VBox CreateVBox ();

        VBox CreateVBox (XElement node, Hashtable boundFields);

        HBox CreateHBox ();

        HBox CreateHBox (XElement node, Hashtable boundFields);

        Table CreateTable ();

        Table CreateTable (XElement node, Hashtable boundFields);

        TableRow CreateRow ();

        TableRow CreateRow (int columns);

        TableRow CreateRow (XElement node, Hashtable boundFields);

        TableRow CreateRow (IEnumerable<TableColumn> columns, Hashtable boundFields);

        TableCell CreateCell ();

        TableCell CreateCell (XElement node, Hashtable boundFields);

        BoxItem CreateBox ();

        BoxItem CreateBox (XElement node, Hashtable boundFields);

        TextBox CreateTextBox ();

        TextBox CreateTextBox (XElement node, Hashtable boundFields);

        Picture CreatePicture ();

        Picture CreatePicture (XElement child, Hashtable boundFields);

        Barcode CreateLinearBarcode ();

        Barcode CreateLinearBarcode (XElement child, Hashtable boundFields);

        HLine CreateHLine ();

        HLine CreateHLine (XElement node);

        VLine CreateVLine ();

        VLine CreateVLine (XElement node);
    }
}
