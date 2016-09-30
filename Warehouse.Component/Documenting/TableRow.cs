//
// TableRow.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/04/2007
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
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Warehouse.Data.DataBinding;

namespace Warehouse.Component.Documenting
{
    public class TableRow : FormDrawableContainer
    {
        public override BindList<FormObject> Children
        {
            get { return childObjects; }
        }

        public TableRow ()
        {
            SetDefaultStyle ("width:100%;vfill:true;allowvbreak:false");
            childDistribution = ObjectStyle.ChildDistribution.Horizontal;
        }

        public TableRow (int cellCount)
            : this ()
        {
            for (int i = 0; i < cellCount; i++) {
                TableCell cell = DocumentHelper.FormObjectCreator.CreateCell ();
                childObjects.Add (cell);
            }
        }

        public TableRow (IEnumerable<TableColumn> columns)
            : this (columns, null)
        {

        }

        public TableRow (IEnumerable<TableColumn> columns, Hashtable boundFields)
            : this ()
        {
            bindableFields = boundFields;
            foreach (TableColumn column in columns)
                CreateCell (column, boundFields, false);
        }

        /// <summary>
        /// Creates a cell for this row by using the specified column and data.
        /// </summary>
        /// <param name="column">The column to which the cell belongs.</param>
        /// <param name="boundFields">The bound fields to use as data.</param>
        /// <param name="rowType">The type of the row: <c>true</c> for a header, <c>false</c> for a data row and <c>null</c> for a footer.</param>
        /// <returns>The newly created cell.</returns>
        public TableCell CreateCell (TableColumn column, Hashtable boundFields, bool? rowType)
        {
            return CreateCell (column, boundFields, rowType, null);
        }

        /// <summary>
        /// Creates a cell for this row by using the specified column and data.
        /// </summary>
        /// <param name="column">The column to which the cell belongs.</param>
        /// <param name="boundFields">The bound fields to use as data.</param>
        /// <param name="rowType">The type of the row: <c>true</c> for a header, <c>false</c> for a data row and <c>null</c> for a footer.</param>
        /// <param name="changedElement">The form object that was changed in a bound table; its reference must be kept.</param>
        /// <returns>The newly created cell.</returns>
        public TableCell CreateCell (TableColumn column, Hashtable boundFields, bool? rowType, FormObject changedElement)
        {
            TableCell cell = DocumentHelper.FormObjectCreator.CreateCell ();

            InitializeCell (cell, column, boundFields, rowType, changedElement);
            childObjects.Add (cell);
            return cell;
        }

        public void InitializeCell (TableCell cell, TableColumn column, Hashtable boundFields, bool? rowType, FormObject changedElement)
        {
            cell.Template.Clear ();
            ObjectStyle cellStyle;
            BindList<FormObject> template;
            switch (rowType) {
                case true:
                    cellStyle = column.HeaderStyle;
                    template = column.HeaderTemplate;
                    break;
                case false:
                    cellStyle = column.ItemStyle;
                    template = column.ItemTemplate;
                    break;
                case null:
                    cellStyle = column.FooterStyle;
                    template = column.FooterTemplate;
                    break;
                default:
                    throw new ArgumentException ("Invalid row type", "rowType");
            }
            cell.Style.Clear (cell.DefaultStyle);
            cell.Style.LoadFromString (cellStyle.ToString (cell.DefaultStyle));

            foreach (FormObject child in template) {
                FormObject newChild = changedElement != null &&
                    (child.IsDescendantOf (changedElement) || changedElement.IsDescendantOf (child)) ? 
                    child : (FormObject) child.Clone ();
                cell.Template.Add (newChild);
                newChild.RefreshBoundFields (boundFields);
            }
            cell.BindableFields = boundFields;
        }

        public TableRow (XElement node, Hashtable boundFields)
            : this ()
        {
            CreateFromTemplate (node, boundFields);
        }

        protected override void OnBeforeAllocateSize ()
        {
        }

        protected override void QueueAllocateUp (bool reallocateCurrent = true)
        {
            base.QueueAllocateUp (reallocateCurrent);

            foreach (TableCell child in Children)
                child.QueueAllocateDown ();
        }

        #region FormObject Members

        public override void CreateFromTemplate (XElement node, Hashtable boundFields)
        {
            base.CreateFromTemplate (node, boundFields);

            bindableFields = boundFields;

            foreach (XElement child in node.Elements ()) {
                FormDrawableObject childObj;
                switch (child.Name.LocalName.ToLowerInvariant ()) {
                    case "cell":
                        childObj = DocumentHelper.FormObjectCreator.CreateCell (child, boundFields);
                        break;
                    default:
                        continue;
                }

                childObjects.Add (childObj);
            }
        }

        #endregion

        protected override IEnumerable<Type> GetAllowedParentTypes ()
        {
            // rows are allowed in tables but currently rows be dropped in them
            return new Type [0];
        }

        protected override string XmlElementName
        {
            get { return "Row"; }
        }

        #region ICloneable Members

        public override object Clone ()
        {
            return Clone (DocumentHelper.FormObjectCreator.CreateRow ());
        }

        #endregion
    }
}
