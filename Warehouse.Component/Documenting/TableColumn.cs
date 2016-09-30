//
// TableColumn.cs
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

using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Xml.Linq;
using Warehouse.Data.DataBinding;

namespace Warehouse.Component.Documenting
{
    public class TableColumn : FormObject
    {
        #region Private members

        private BindList<FormObject> itemTemplate = new BindList<FormObject> ();
        private BindList<FormObject> headerTemplate = new BindList<FormObject> ();
        private BindList<FormObject> footerTemplate = new BindList<FormObject> ();
        private ObjectStyle itemStyle = new ObjectStyle ("vfill:true;hfill:true;allowhbreak:false;allowvbreak:false;");
        private ObjectStyle headerStyle = new ObjectStyle ("vfill:true;hfill:true;allowhbreak:false;allowvbreak:false;");
        private ObjectStyle footerStyle = new ObjectStyle ("vfill:true;hfill:true;allowhbreak:false;allowvbreak:false;");
        private ObjectFont itemFont = new ObjectFont ("size:8");
        private ObjectFont headerFont = new ObjectFont ("size:8");
        private ObjectFont footerFont = new ObjectFont ("size:8");

        #endregion

        #region Public properties

        public BindList<FormObject> ItemTemplate
        {
            get { return itemTemplate; }
        }

        public BindList<FormObject> HeaderTemplate
        {
            get { return headerTemplate; }
        }

        public BindList<FormObject> FooterTemplate
        {
            get { return footerTemplate; }
        }

        public ObjectStyle ItemStyle
        {
            get { return itemStyle; }
            set { itemStyle = value; }
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

        public ObjectFont ItemFont
        {
            get { return itemFont; }
            set { itemFont = value; }
        }

        public ObjectFont HeaderFont
        {
            get { return headerFont; }
            set { headerFont = value; }
        }

        public ObjectFont FooterFont
        {
            get { return footerFont; }
            set { footerFont = value; }
        }

        #endregion

        public TableColumn ()
        {
            SetDefaultStyle ("vfill:true;hfill:true;allowhbreak:false;allowvbreak:false;");
        }

        public TableColumn (DataColumn column)
            : this (column.ColumnName, column.Caption)
        {
        }

        public TableColumn (string columnName, string caption)
        {
            const string styleString = "padding:4;halign:left;valign:center;width:10%;vfill:true;hfill:true;allowhbreak:false;allowvbreak:false;";

            SetDefaultStyle (styleString);

            TextBox headerTextBox = DocumentHelper.FormObjectCreator.CreateTextBox ();
            headerTextBox.Text = caption;
            headerTemplate.Add (headerTextBox);
            headerStyle = new ObjectStyle (styleString);

            TextBox itemTextBox = DocumentHelper.FormObjectCreator.CreateTextBox ();
            itemTextBox.SourceField = columnName;
            itemTemplate.Add (itemTextBox);
            itemStyle = new ObjectStyle (styleString);

            TextBox footerTextBox = DocumentHelper.FormObjectCreator.CreateTextBox ();
            footerTextBox.Text = caption;
            footerTemplate.Add (footerTextBox);
            footerStyle = new ObjectStyle (styleString);
        }

        public TableColumn (XElement node, Hashtable boundFields)
            : this ()
        {
            CreateFromTemplate (node, boundFields);
        }

        #region FormObject Members

        public override void CreateFromTemplate (XElement node, Hashtable boundFields)
        {
            XAttribute tempAttr = node.Attribute ("style");
            if (tempAttr != null) {
                style.LoadFromString (tempAttr.Value);
                itemStyle.LoadFromString (tempAttr.Value);
            }
            
            tempAttr = node.Attribute ("font");
            if (tempAttr != null)
                itemFont.LoadFromString (tempAttr.Value);

            TextBox tb;
            tempAttr = node.Attribute ("text");
            if (tempAttr != null) {
                tb = DocumentHelper.FormObjectCreator.CreateTextBox ();
                tb.Text = tempAttr.Value;
                tb.Style = (ObjectStyle) itemStyle.Clone ();
                tb.Font = (ObjectFont) itemFont.Clone ();
                itemTemplate.Clear ();
                itemTemplate.Add (tb);
            }

            tempAttr = node.Attribute ("sourceField");
            if (tempAttr != null) {
                tb = DocumentHelper.FormObjectCreator.CreateTextBox ();
                tb.SourceField = tempAttr.Value;
                if (boundFields [tempAttr.Value] != null)
                    tb.Text = boundFields [tempAttr.Value].ToString ();
                tb.Style = (ObjectStyle) itemStyle.Clone ();
                tb.Font = (ObjectFont) itemFont.Clone ();
                itemTemplate.Clear ();
                itemTemplate.Add (tb);
            }

            foreach (XElement child in node.Elements ()) {
                XAttribute styleAttr;
                switch (child.Name.LocalName.ToLowerInvariant ()) {
                    case "template":
                        styleAttr = child.Attribute ("style");
                        if (styleAttr != null)
                            itemStyle.LoadFromString (styleAttr.Value);

                        if (child.HasElements) {
                            CreateChildrenFromTemplate (child, boundFields, ref itemTemplate);
                        }
                        break;

                    case "header":
                        styleAttr = child.Attribute ("style");
                        if (styleAttr != null)
                            headerStyle.LoadFromString (styleAttr.Value);

                        tempAttr = node.Attribute ("font");
                        if (tempAttr != null)
                            headerFont.LoadFromString (tempAttr.Value);

                        tempAttr = child.Attribute ("text");
                        if (tempAttr != null) {
                            tb = DocumentHelper.FormObjectCreator.CreateTextBox ();
                            tb.Text = tempAttr.Value;
                            tb.Style = (ObjectStyle) headerStyle.Clone ();
                            tb.Font = (ObjectFont) headerFont.Clone ();
                            headerTemplate.Clear ();
                            headerTemplate.Add (tb);
                        }

                        tempAttr = child.Attribute ("sourceField");
                        if (tempAttr != null) {
                            tb = DocumentHelper.FormObjectCreator.CreateTextBox ();
                            tb.SourceField = tempAttr.Value;
                            tb.Text = boundFields [tempAttr.Value].ToString ();
                            tb.Style = (ObjectStyle) headerStyle.Clone ();
                            tb.Font = (ObjectFont) headerFont.Clone ();
                            headerTemplate.Clear ();
                            headerTemplate.Add (tb);
                        }

                        if (child.HasElements) {
                            CreateChildrenFromTemplate (child, boundFields, ref headerTemplate);
                        }
                        break;

                    case "footer":
                        styleAttr = child.Attribute ("style");
                        if (styleAttr != null)
                            footerStyle.LoadFromString (styleAttr.Value);

                        tempAttr = node.Attribute ("font");
                        if (tempAttr != null)
                            footerFont.LoadFromString (tempAttr.Value);

                        tempAttr = child.Attribute ("text");
                        if (tempAttr != null) {
                            tb = DocumentHelper.FormObjectCreator.CreateTextBox ();
                            tb.Text = tempAttr.Value;
                            tb.Style = (ObjectStyle) footerStyle.Clone ();
                            tb.Font = (ObjectFont) footerFont.Clone ();
                            footerTemplate.Clear ();
                            footerTemplate.Add (tb);
                        }

                        tempAttr = child.Attribute ("sourceField");
                        if (tempAttr != null) {
                            tb = DocumentHelper.FormObjectCreator.CreateTextBox ();
                            tb.SourceField = tempAttr.Value;
                            tb.Text = boundFields [tempAttr.Value].ToString ();
                            tb.Style = (ObjectStyle) footerStyle.Clone ();
                            tb.Font = (ObjectFont) footerFont.Clone ();
                            footerTemplate.Clear ();
                            footerTemplate.Add (tb);
                        }

                        if (child.HasElements) {
                            CreateChildrenFromTemplate (child, boundFields, ref footerTemplate);
                        }
                        break;
                }
            }
        }

        public override void RefreshBoundFields (Hashtable boundFields)
        {
            foreach (FormObject child in itemTemplate) {
                child.RefreshBoundFields (boundFields);
            }

            foreach (FormObject child in headerTemplate) {
                child.RefreshBoundFields (boundFields);
            }

            foreach (FormObject child in footerTemplate) {
                child.RefreshBoundFields (boundFields);
            }
        }

        public override void ReCheckPageDependant ()
        {
        }

        #endregion

        protected override string XmlElementName
        {
            get { return "Column"; }
        }

        public override XElement GetXmlElement ()
        {
            XElement element = base.GetXmlElement ();
            XElement item = AddTemplate (itemTemplate, "Template", itemFont, itemStyle);
            if (item != null)
                element.Add (item);
            XElement header = AddTemplate (headerTemplate, "Header", headerFont, headerStyle);
            if (header != null)
                element.Add (header);
            XElement footer = AddTemplate (footerTemplate, "Footer", footerFont, footerStyle);
            if (footer != null)
                element.Add (footer);
            return element;
        }

        private XElement AddTemplate (ICollection<FormObject> template, string templateName, ObjectFont templateFont, ObjectStyle templateStyle)
        {
            if (template.Count > 0) {
                XElement templateElement = new XElement (templateName);
                if (templateFont != null)
                    AddAttribute ("font", templateFont, templateElement);
                if (templateStyle != null)
                    AddAttribute ("style", templateStyle.ToString (DefaultStyle), templateElement);
                foreach (FormObject childObject in template)
                    templateElement.Add (childObject.GetXmlElement ());
                return templateElement;
            }
            return null;
        }

        #region ICloneable Members

        public override object Clone ()
        {
            TableColumn ret = new TableColumn ();

            ret.itemStyle = (ObjectStyle) itemStyle.Clone ();
            ret.headerStyle = (ObjectStyle) headerStyle.Clone ();
            ret.footerStyle = (ObjectStyle) footerStyle.Clone ();

            foreach (FormObject child in itemTemplate)
                ret.itemTemplate.Add ((FormObject) child.Clone ());

            foreach (FormObject child in headerTemplate)
                ret.headerTemplate.Add ((FormObject) child.Clone ());

            foreach (FormObject child in footerTemplate)
                ret.footerTemplate.Add ((FormObject) child.Clone ());

            return ret;
        }

        #endregion
    }
}
