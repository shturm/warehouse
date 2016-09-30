//
// FormObjectCreator.cs
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
    public class FormObjectCreator : IFormObjectCreator
    {
        public Form CreateForm ()
        {
            return new Form ();
        }

        public Form CreateForm (XElement node, object obj)
        {
            return new Form (node, obj);
        }

        public Section CreateSection ()
        {
            return new Section ();
        }

        public Section CreateSection (XElement node, Hashtable boundFields)
        {
            return new Section (node, boundFields);
        }

        public VBox CreateVBox ()
        {
            return new VBox ();
        }

        public VBox CreateVBox (XElement node, Hashtable boundFields)
        {
            return new VBox (node, boundFields);
        }

        public HBox CreateHBox ()
        {
            return new HBox ();
        }

        public HBox CreateHBox (XElement node, Hashtable boundFields)
        {
            return new HBox (node, boundFields);
        }

        public Table CreateTable ()
        {
            return new Table ();
        }

        public Table CreateTable (XElement node, Hashtable boundFields)
        {
            return new Table (node, boundFields);
        }

        public TableRow CreateRow ()
        {
            return new TableRow ();
        }

        public TableRow CreateRow (int columns)
        {
            return new TableRow (columns);
        }

        public TableRow CreateRow (XElement node, Hashtable boundFields)
        {
            return new TableRow (node, boundFields);
        }

        public TableRow CreateRow (IEnumerable<TableColumn> columns, Hashtable boundFields)
        {
            return new TableRow (columns, boundFields);
        }

        public TableCell CreateCell ()
        {
            return new TableCell ();
        }

        public TableCell CreateCell (XElement node, Hashtable boundFields)
        {
            return new TableCell (node, boundFields);
        }

        public BoxItem CreateBox ()
        {
            return new BoxItem ();
        }

        public BoxItem CreateBox (XElement node, Hashtable boundFields)
        {
            return new BoxItem (node, boundFields);
        }

        public TextBox CreateTextBox ()
        {
            return new TextBox ();
        }

        public TextBox CreateTextBox (XElement node, Hashtable boundFields)
        {
            return new TextBox (node, boundFields);
        }

        public Picture CreatePicture ()
        {
            return new Picture ();
        }

        public Picture CreatePicture (XElement child, Hashtable boundFields)
        {
            return new Picture (child, boundFields);
        }

        public Barcode CreateLinearBarcode ()
        {
            return new Barcode ();
        }

        public Barcode CreateLinearBarcode (XElement child, Hashtable boundFields)
        {
            return new Barcode (child, boundFields);
        }

        public HLine CreateHLine ()
        {
            return new HLine ();
        }

        public HLine CreateHLine (XElement node)
        {
            return new HLine (node);
        }

        public VLine CreateVLine ()
        {
            return new VLine ();
        }

        public VLine CreateVLine (XElement node)
        {
            return new VLine (node);
        }
    }
}
