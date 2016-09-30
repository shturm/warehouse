//
// Table.cs
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
using System.ComponentModel;
using System.Reflection;
using System.Xml.Linq;
using Warehouse.Data.DataBinding;

namespace Warehouse.Component.Documenting
{
    public class Table : FormDrawableContainer
    {
        #region Private members

        protected readonly BindList<TableColumn> columns = new BindList<TableColumn> ();
        protected readonly BindList<FormObject> rows = new BindList<FormObject> ();
        private static readonly ObjectStyle defaultHeaderStyle = new ObjectStyle ("width:100%;vfill:true;bgcolor:#D4D0C8");
        private static readonly ObjectStyle defaultItemStyle = new ObjectStyle ("width:100%;vfill:true;allowvbreak:false");
        private static readonly ObjectStyle defaultFooterStyle = new ObjectStyle ("width:100%;vfill:true;bgcolor:#D4D0C8");

        private ObjectStyle headerStyle;
        private readonly ObjectStyle itemStyle;
        private ObjectStyle footerStyle;

        private string sourceTable = string.Empty;
        protected TableRow header;
        protected TableRow footer;
        private bool hasHeader = true;
        private bool sourceTableHasFooter;
        private string sourceTableHasFooterSource = string.Empty;
        private bool refreshingStyles;
        protected bool autoColumns;

        #endregion

        #region Public properties

        public virtual string SourceTable
        {
            get { return sourceTable; }
            set
            {
                if (sourceTable != value) {
                    sourceTable = value;
                    if (string.IsNullOrEmpty (sourceTable))
                        AutoColumns = false;
                }
            }
        }

        public ObjectStyle HeaderStyle
        {
            get { return headerStyle; }
            set { headerStyle = value; }
        }

        public ObjectStyle FooterStyle
        {
            get { return footerStyle; }
            set { footerStyle = value; }
        }

        public BindList<TableColumn> Columns
        {
            get { return columns; }
        }

        public BindList<FormObject> Rows
        {
            get { return rows; }
        }

        public TableRow Header
        {
            get { return header; }
            set
            {
                if (header == value)
                    return;

                header = value;
                hasHeader = header != null;
                OnPropertyChanged ("Header");
            }
        }

        public TableRow Footer
        {
            get { return footer; }
            set
            {
                if (footer == value)
                    return;

                footer = value;
                sourceTableHasFooter = footer != null;
                OnPropertyChanged ("Footer");
            }
        }

        public bool HasHeader
        {
            get { return hasHeader; }
            set
            {
                if (hasHeader != value) {
                    hasHeader = value;
                    if (!hasHeader) {
                        rows.Remove (header);
                        header = null;
                    }
                    OnPropertyChanged ("HasHeader");
                }
            }
        }

        public bool SourceTableHasFooter
        {
            get { return sourceTableHasFooter; }
            set
            {
                if (sourceTableHasFooter == value)
                    return;

                sourceTableHasFooter = value;
                if (!sourceTableHasFooter) {
                    rows.Remove (footer);
                    footer = null;
                }
                OnPropertyChanged ("SourceTableHasFooter");
            }
        }

        public string SourceTableHasFooterSource
        {
            get { return sourceTableHasFooterSource; }
            set
            {
                if (sourceTableHasFooterSource == value)
                    return;

                sourceTableHasFooterSource = value;
                RefreshStructure (bindableFields);
                OnPropertyChanged ("SourceTableHasFooterSource");
            }
        }

        public override BindList<FormObject> Children
        {
            get { return rows; }
        }

        public int ColumnsCount
        {
            get { return columns.Count; }
        }

        public virtual bool AutoColumns
        {
            get { return autoColumns; }
            set
            {
                if (autoColumns != value) {
                    autoColumns = value;
                    columns.Clear ();
                    foreach (FormDrawableContainer row in rows)
                        row.Children.Clear ();
                    rows.Clear ();
                    if (autoColumns)
                        RefreshStructure (bindableFields);
                    OnPropertyChanged ("AutoColumns");
                }
            }
        }

        #endregion

        public Table ()
        {
            SetDefaultStyle ("vfill:true;border:1;innerborder:1");
            headerStyle = (ObjectStyle) defaultHeaderStyle.Clone ();
            itemStyle = (ObjectStyle) defaultItemStyle.Clone ();
            footerStyle = (ObjectStyle) defaultFooterStyle.Clone ();

            childDistribution = ObjectStyle.ChildDistribution.Vertical;

            rows.ListChanged += OnChildObjectsListChanged;
            columns.ListChanged += Columns_ListChanged;
        }

        public Table (XElement node, Hashtable boundFields)
            : this ()
        {
            CreateFromTemplate (node, boundFields);
        }

        #region Calculate child sizes

        protected override void OnBeforeAllocateSize ()
        {
            if (string.IsNullOrEmpty (sourceTable)) {
                foreach (FormDrawableContainer row in Children)
                    while (row.Children.Count > columns.Count) {
                        TableColumn column = new TableColumn ();
                        column.HeaderTemplate.Add (DocumentHelper.FormObjectCreator.CreateTextBox ());
                        column.ItemTemplate.Add (DocumentHelper.FormObjectCreator.CreateTextBox ());
                        column.FooterTemplate.Add (DocumentHelper.FormObjectCreator.CreateTextBox ());
                        columns.Add (column);
                    }

                foreach (TableRow row in Children)
                    while (columns.Count > row.Children.Count)
                        row.CreateCell (columns [row.Children.Count], bindableFields, false);
            }

            foreach (TableRow row in Children) {
                row.MinimalSize.LoadFromStyle (row.Style);

                foreach (TableCell cell in row.Children) {
                    cell.MinimalSize.LoadFromStyle (cell.Style);
                }
            }
        }

        protected override void OnAfterAllocateSize (out bool redoAllocate)
        {
            int i;

            #region Set the maximum horizontal spacing in each row

            float spacing = 0f;
            float innerSpacing = 0f;
            foreach (TableRow row in Children) {
                spacing = Math.Max (spacing, Math.Max (row.Style.Border ?? 0, row.Style.Spacing ?? 0));
                innerSpacing = Math.Max (innerSpacing, Math.Max (row.Style.InnerVerticalBorder ?? 0, row.Style.InnerHSpacing ?? 0));
            }

            // Patch the horizontal spacing of the rows
            foreach (TableRow row in Children) {
                row.Style.Spacing = spacing;
                row.Style.InnerHSpacing = innerSpacing;
            }

            #endregion

            #region Set the maximum width and heigth in to the cells

            double? [] columnWidths = new double? [ColumnsCount];
            double? [] columnRelativeWidths = new double? [ColumnsCount];

            for (i = 0; i < columns.Count; i++) {
                columnWidths [i] = null;
                columnRelativeWidths [i] = null;
            }

            foreach (TableRow row in Children) {
                double maxHeight = -1f;
                double maxRelativeHeight = -1f;
                double maxVFill = 0f;
                i = 0;
                foreach (TableCell cell in row.Children) {
                    if (cell.AllocatedSize.Height.HasValue)
                        maxHeight = Math.Max (maxHeight, cell.AllocatedSize.HeightValue);

                    if (cell.AllocatedSize.Width.HasValue)
                        columnWidths [i] = Math.Max (columnWidths [i] ?? -1, cell.AllocatedSize.WidthValue);

                    if (cell.AllocatedSize.RelativeHeight.HasValue)
                        maxRelativeHeight = Math.Max (maxRelativeHeight, cell.AllocatedSize.RelativeHeightValue);

                    if (cell.AllocatedSize.RelativeWidth.HasValue)
                        columnRelativeWidths [i] = Math.Max (columnRelativeWidths [i] ?? -1, cell.AllocatedSize.RelativeWidthValue);

                    maxVFill = Math.Max (maxVFill, cell.AllocatedVPageFill);

                    i++;
                }

                foreach (TableCell cell in row.Children) {
                    cell.MinimalSize.Height = maxHeight >= 0 ? maxHeight : (double?) null;
                    cell.MinimalSize.RelativeHeight = maxRelativeHeight >= 0 ? maxRelativeHeight : (double?) null;
                    cell.AllocatedVPageFill = maxVFill;
                }
            }

            for (i = 0; i < ColumnsCount; i++) {
                foreach (TableRow row in Children) {
                    TableCell cell = (TableCell) row.Children [i];
                    cell.MinimalSize.Width = columnWidths [i];
                    cell.MinimalSize.RelativeWidth = columnRelativeWidths [i];
                }
            }

            #endregion

            redoAllocate = true;
        }

        #endregion

        #region FormObject Members

        public override void CreateFromTemplate (XElement node, Hashtable boundFields)
        {
            XAttribute tempAttr = node.Attribute ("sourceTable");
            if (tempAttr != null)
                sourceTable = tempAttr.Value;

            base.CreateFromTemplate (node, boundFields);

            tempAttr = node.Attribute ("headerStyle");
            if (tempAttr != null)
                headerStyle.LoadFromString (tempAttr.Value);

            tempAttr = node.Attribute ("itemStyle");
            if (tempAttr != null)
                ItemStyle.LoadFromString (tempAttr.Value);

            tempAttr = node.Attribute ("hasHeader");
            if (tempAttr != null)
                bool.TryParse (tempAttr.Value, out hasHeader);

            tempAttr = node.Attribute ("footerStyle");
            if (tempAttr != null)
                footerStyle.LoadFromString (tempAttr.Value);

            tempAttr = node.Attribute ("sourceTableHasFooter");
            if (tempAttr != null)
                bool.TryParse (tempAttr.Value, out sourceTableHasFooter);

            tempAttr = node.Attribute ("sourceTableHasFooterSource");
            if (tempAttr != null) {
                sourceTableHasFooterSource = tempAttr.Value;
                if (boundFields.ContainsKey (sourceTableHasFooterSource))
                    bool.TryParse (boundFields [sourceTableHasFooterSource].ToString (), out sourceTableHasFooter);
            }

            foreach (XElement child in node.Elements ()) {
                switch (child.Name.LocalName.ToLowerInvariant ()) {
                    case "columns":
                        foreach (XElement column in child.Elements ()) {
                            TableColumn tableColumn;
                            switch (column.Name.LocalName.ToLowerInvariant ()) {
                                case "column":
                                    tableColumn = new TableColumn (column, boundFields);
                                    break;
                                default:
                                    continue;
                            }

                            columns.Add (tableColumn);
                        }

                        CreateHeader (boundFields);
                        CreateFooter (boundFields);
                        break;

                    case "rows":
                        if (sourceTable.Length == 0) {
                            foreach (XElement row in child.Elements ()) {
                                TableRow tableRow;
                                switch (row.Name.LocalName.ToLowerInvariant ()) {
                                    case "row":
                                        tableRow = DocumentHelper.FormObjectCreator.CreateRow (row, boundFields);
                                        break;
                                    default:
                                        continue;
                                }
                                if (tableRow.Style.ToString (tableRow.DefaultStyle) == tableRow.DefaultStyle.ToString (tableRow.DefaultStyle))
                                    tableRow.Style = (ObjectStyle) itemStyle.Clone ();
                                rows.Add (tableRow);

                                if (style.InnerVerticalBorder.HasValue)
                                    tableRow.Style.InnerVerticalBorder = style.InnerVerticalBorder;

                                if (style.InnerHSpacing.HasValue)
                                    tableRow.Style.InnerHSpacing = style.InnerHSpacing;
                            }
                        }
                        break;
                }
            }

            RefreshStructure (boundFields);
        }

        protected virtual void CreateHeader (Hashtable boundFields)
        {
            bool headerPresent = false;
            header = DocumentHelper.FormObjectCreator.CreateRow (columns.Count);
            header.BindableFields = boundFields;
            for (int i = 0; i < columns.Count; i++) {
                TableColumn tableColumn = columns [i];
                if (tableColumn.HeaderTemplate.Count != 0)
                    headerPresent = true;

                TableCell headerCell = (TableCell) header.Children [i];
                foreach (FormObject formObject in tableColumn.HeaderTemplate)
                    headerCell.Template.Add ((FormObject) formObject.Clone ());
                headerCell.Style = (ObjectStyle) tableColumn.HeaderStyle.Clone ();
            }
            if (headerPresent) {
                header.Style = (ObjectStyle) headerStyle.Clone ();
                if (!header.Style.InnerVerticalBorder.HasValue && style.InnerVerticalBorder.HasValue)
                    header.Style.InnerVerticalBorder = style.InnerVerticalBorder;

                if (!header.Style.InnerHSpacing.HasValue && style.InnerHSpacing.HasValue)
                    header.Style.InnerHSpacing = style.InnerHSpacing;
            } else {
                header = null;
            }
        }

        public virtual void CreateFooter (Hashtable boundFields)
        {
            bool footerPresent = false;
            footer = DocumentHelper.FormObjectCreator.CreateRow (columns.Count);
            footer.BindableFields = boundFields;
            for (int i = 0; i < columns.Count; i++) {
                TableColumn tableColumn = columns [i];
                if (tableColumn.FooterTemplate.Count != 0)
                    footerPresent = true;

                TableCell footerCell = (TableCell) footer.Children [i];
                foreach (FormObject formObject in tableColumn.FooterTemplate)
                    footerCell.Template.Add ((FormObject) formObject.Clone ());
                footerCell.Style = (ObjectStyle) tableColumn.FooterStyle.Clone ();
            }
            if (footerPresent) {
                footer.Style = (ObjectStyle) FooterStyle.Clone ();
                if (!footer.Style.InnerVerticalBorder.HasValue && style.InnerVerticalBorder.HasValue)
                    footer.Style.InnerVerticalBorder = style.InnerVerticalBorder;

                if (!footer.Style.InnerHSpacing.HasValue && style.InnerHSpacing.HasValue)
                    footer.Style.InnerHSpacing = style.InnerHSpacing;
            } else {
                footer = null;
            }
        }

        public override void RefreshBoundFields (Hashtable boundFields)
        {
            base.RefreshBoundFields (boundFields);
            RefreshStructure (boundFields);

            foreach (TableColumn child in Columns) {
                child.RefreshBoundFields (boundFields);
            }

            if (header != null) {
                foreach (FormObject child in header.Children) {
                    child.RefreshBoundFields (boundFields);
                }
            }

            if (footer != null) {
                foreach (FormObject child in footer.Children) {
                    child.RefreshBoundFields (boundFields);
                }
            }
        }

        protected override void OnChildObjectsListChanged (object sender, ListChangedEventArgs e)
        {
            base.OnChildObjectsListChanged (sender, e);
            if (e.ListChangedType == ListChangedType.ItemAdded) {
                IList children = (IList) sender;
                FormObject formObject = (FormObject) children [e.NewIndex];
                formObject.Selectable = !autoColumns;
            }
            OnPropertyChanged ("Rows");
        }

        private void Columns_ListChanged (object sender, ListChangedEventArgs e)
        {
            switch (e.ListChangedType) {
                case ListChangedType.ItemDeleted:
                    foreach (FormDrawableContainer row in Children)
                        row.Children.RemoveAt (e.NewIndex);
                    break;
            }
            OnPropertyChanged ("Columns");
        }

        protected virtual void RefreshStructure (Hashtable boundFields)
        {
            bindableFields = boundFields;

            int i;

            if (boundFields.ContainsKey (sourceTableHasFooterSource))
                bool.TryParse (boundFields [sourceTableHasFooterSource].ToString (), out sourceTableHasFooter);

            if (!boundFields.ContainsKey (sourceTable))
                return;

            BindManager bManager = new BindManager (boundFields [sourceTable], true, ColumnNameResolver);

            // bManager.Columns may be null when the source table is a dictionary
            // the check below simply prevents crashing but the binding manager should be extended later
            if (columns.Count == 0 && bManager.Columns != null) {
                autoColumns = true;
                if (bManager.Columns.NamedColumns) {
                    for (i = 0; i < bManager.Columns.Count; i++)
                        columns.Add (new TableColumn (bManager.Columns [i].Name, bManager.Columns [i].Caption));
                } else {
                    for (i = 0; i < bManager.Columns.Count; i++)
                        columns.Add (new TableColumn (i.ToString (), i.ToString ()));
                }

                TableColumn tableColumn;
                if (hasHeader) {
                    header = DocumentHelper.FormObjectCreator.CreateRow (columns.Count);
                    header.BindableFields = boundFields;

                    for (i = 0; i < columns.Count; i++) {
                        tableColumn = columns [i];

                        TableCell headerCell = (TableCell) header.Children [i];
                        headerCell.Template.AddRange (tableColumn.HeaderTemplate);
                        headerCell.Style = (ObjectStyle) tableColumn.HeaderStyle.Clone ();
                    }
                }

                if (sourceTableHasFooter) {
                    footer = DocumentHelper.FormObjectCreator.CreateRow (columns.Count);
                    footer.BindableFields = boundFields;

                    BindRow<object> footerRow = null;
                    if (!string.IsNullOrEmpty (sourceTableHasFooterSource) && bManager.Rows.Count > 0)
                        footerRow = bManager.Rows [bManager.Rows.Count - 1];
                    for (i = 0; i < columns.Count; i++) {
                        tableColumn = columns [i];

                        TextBox tb = DocumentHelper.FormObjectCreator.CreateTextBox ();
                        tb.Text = footerRow == null ? string.Empty : footerRow [i].ToString ();
                        TableCell footerCell = (TableCell) footer.Children [i];
                        footerCell.Template.Add (tb);
                        footerCell.Style = (ObjectStyle) tableColumn.FooterStyle.Clone ();
                    }
                } else {
                    footer = null;
                }
            }

            RefreshRowStyles ();

            rows.Clear ();
            if (header != null)
                rows.Add (header);

            for (i = 0; i < bManager.Rows.Count - (sourceTableHasFooter && !string.IsNullOrEmpty (sourceTableHasFooterSource) ? 1 : 0); i++) {
                TableRow tableRow = DocumentHelper.FormObjectCreator.CreateRow (Columns, GetBoundFields (bManager, i));
                tableRow.Style = (ObjectStyle) itemStyle.Clone ();
                rows.Add (tableRow);
                if (style.InnerVerticalBorder.HasValue)
                    tableRow.Style.InnerVerticalBorder = style.InnerVerticalBorder;
            }

            bindableFields = boundFields;

            if (sourceTableHasFooter && footer != null)
                rows.Add (footer);
        }

        private static string ColumnNameResolver (MemberInfo memberInfo)
        {
            if (memberInfo.MemberType != MemberTypes.Property &&
                memberInfo.MemberType != MemberTypes.Field)
                return string.Empty;

            FormMember [] members = GetFormMembers (memberInfo, false);
            return members.Length > 0 ? members [0].ReportField : memberInfo.Name;
        }

        private static Hashtable GetBoundFields (BindManager manager, int row)
        {
            Hashtable ret = new Hashtable ();
            BindRow<object> rowList = manager.Rows [row];
            ret.Add (string.Empty, (rowList.Value ?? string.Empty).ToString ());

            if (manager.Columns.NamedColumns) {
                for (int i = 0; i < manager.Columns.Count; i++)
                    ret.Add (manager.Columns [i].ResolvedName, rowList [i]);
            } else if (!(rowList.Value is string)) {
                for (int i = 0; i < manager.Columns.Count; i++)
                    ret.Add (i, rowList [i]);
            }

            return ret;
        }

        /// <summary>
        /// Refreshes the styles of the header and footer cells of this <see cref="Table"/>.
        /// </summary>
        public void RefreshRowStyles ()
        {
            if (refreshingStyles)
                return;

            refreshingStyles = true;

            if (header != null) {
                header.Style = (ObjectStyle) headerStyle.Clone ();
                if (!header.Style.InnerVerticalBorder.HasValue && style.InnerVerticalBorder.HasValue)
                    header.Style.InnerVerticalBorder = style.InnerVerticalBorder;
            }

            foreach (FormObject row in rows)
                if (row != header && row != footer) {
                    row.Style = (ObjectStyle) itemStyle.Clone ();
                    if (!row.Style.InnerVerticalBorder.HasValue && style.InnerVerticalBorder.HasValue)
                        row.Style.InnerVerticalBorder = style.InnerVerticalBorder;
                }

            if (footer != null) {
                footer.Style = (ObjectStyle) footerStyle.Clone ();
                if (!footer.Style.InnerVerticalBorder.HasValue && style.InnerVerticalBorder.HasValue)
                    footer.Style.InnerVerticalBorder = style.InnerVerticalBorder;
            }
            refreshingStyles = false;
        }

        #endregion

        public override XElement GetXmlElement ()
        {
            XElement element = base.GetXmlElement ();
            if (string.IsNullOrEmpty (sourceTable))
                AddElements (element, "Rows", rows);
            else if (!autoColumns)
                AddElements (element, "Columns", columns);
            return element;
        }

        protected override string XmlElementName
        {
            get { return "Table"; }
        }

        public ObjectStyle ItemStyle
        {
            get { return itemStyle; }
        }

        protected override void AddAttributes (XElement xmlElement)
        {
            if (!string.IsNullOrEmpty (sourceTable))
                AddAttribute ("sourceTable", sourceTable, xmlElement);
            if (!hasHeader)
                AddAttribute ("hasHeader", hasHeader, xmlElement);
            if (string.IsNullOrEmpty (sourceTableHasFooterSource)) {
                if (sourceTableHasFooter)
                    AddAttribute ("sourceTableHasFooter", sourceTableHasFooter, xmlElement);
            } else AddAttribute ("sourceTableHasFooterSource", sourceTableHasFooterSource, xmlElement);
            base.AddAttributes (xmlElement);
            if (headerStyle != null)
                AddAttribute ("headerStyle", headerStyle.ToString (defaultHeaderStyle), xmlElement);
            if (itemStyle != null)
                AddAttribute ("itemStyle", itemStyle.ToString (defaultItemStyle), xmlElement);
            if (footerStyle != null)
                AddAttribute ("footerStyle", footerStyle.ToString (defaultFooterStyle), xmlElement);
        }

        private static void AddElements (XElement table, string name, IEnumerable elements)
        {
            XElement elementsParent = new XElement (name);
            foreach (FormObject formObject in elements)
                elementsParent.Add (formObject.GetXmlElement ());
            table.Add (elementsParent);
        }

        #region ICloneable Members

        public override object Clone ()
        {
            Table ret = DocumentHelper.FormObjectCreator.CreateTable ();

            return CopyDataTo (ret);
        }

        protected object CopyDataTo (Table result)
        {
            result.style = (ObjectStyle) style.Clone ();
            result.sourceTable = sourceTable;
            result.bindableFields = new Hashtable (bindableFields);

            result.columns.Clear ();
            result.rows.Clear ();

            foreach (TableColumn child in columns)
                result.columns.Add ((TableColumn) child.Clone ());

            foreach (TableRow child in rows)
                result.rows.Add ((FormObject) child.Clone ());

            if (header != null)
                if (header.ParentPosition < rows.Count)
                    result.header = (TableRow) result.rows [header.ParentPosition];
                else
                    result.header = (TableRow) header.Clone ();

            if (footer != null)
                if (footer.ParentPosition < rows.Count)
                    result.footer = (TableRow) result.Rows [footer.ParentPosition];
                else
                    result.footer = (TableRow) footer.Clone ();

            return result;
        }

        #endregion
    }
}
