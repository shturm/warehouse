//
// FormObject.cs
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
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Warehouse.Business.Documenting;
using Warehouse.Data.DataBinding;

namespace Warehouse.Component.Documenting
{
    public abstract class FormObject : INotifyPropertyChanged, ICloneable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected ObjectStyle style;
        private ObjectStyle defaultStyle = new ObjectStyle ();
        protected FormObject parent;
        private bool selectable = true;
        protected Hashtable bindableFields = new Hashtable ();
        private Type objectType;
        private object sourceObject;

        public virtual bool Selectable
        {
            get { return selectable; }
            set { selectable = value; }
        }

        public ObjectStyle Style
        {
            get { return style; }
            set { style = value; }
        }

        public ObjectStyle DefaultStyle
        {
            get { return defaultStyle; }
        }

        public FormObject Parent
        {
            get { return parent; }
            set
            {
                if (parent == value)
                    return;

                if (parent != null) {
                    FormDrawableContainer container = parent as FormDrawableContainer;
                    if (container != null)
                        container.Children.Remove (this);
                }
                parent = value;
                ReCheckPageDependant ();
            }
        }

        public FormObject TopMost
        {
            get
            {
                FormDrawableContainer container = parent as FormDrawableContainer;
                return container == null ? this : container.TopMost;
            }
        }

        public object SourceObject
        {
            get { return sourceObject; }
        }

        public bool IsDescendantOf (FormObject ancestor)
        {
            FormObject currentParent = this;
            while (currentParent != null) {
                if (currentParent == ancestor)
                    return true;
                currentParent = currentParent.parent;
            }
            return false;
        }

        public virtual void CreateFromTemplate (XElement node, Hashtable boundFields)
        {
            XAttribute styleAttr = node.Attribute ("style");
            if (styleAttr != null)
                style.LoadFromString (styleAttr.Value);
        }

        public virtual void Cleanup ()
        {
        }

        public virtual void RefreshBoundFields (Hashtable boundFields)
        {
            bindableFields = boundFields;
        }

        public abstract void ReCheckPageDependant ();

        protected void CreateChildrenFromTemplate (XElement node, Hashtable boundFields, ref BindList<FormObject> children)
        {
            children.Clear ();

            foreach (XElement child in node.Elements ())
                CreateChild (boundFields, children, child);
        }

        protected static void CreateChild (Hashtable boundFields, BindList<FormObject> children, XElement child)
        {
            FormObject childObj;
            switch (child.Name.LocalName.ToLowerInvariant ()) {
                case "hbox":
                    childObj = DocumentHelper.FormObjectCreator.CreateHBox (child, boundFields);
                    break;

                case "vbox":
                    childObj = DocumentHelper.FormObjectCreator.CreateVBox (child, boundFields);
                    break;

                case "textbox":
                    childObj = DocumentHelper.FormObjectCreator.CreateTextBox (child, boundFields);
                    break;

                case "picture":
                    childObj = DocumentHelper.FormObjectCreator.CreatePicture (child, boundFields);
                    break;

                case "rectangle":
                    childObj = new Rectangle (child, boundFields);
                    break;

                case "hline":
                    childObj = DocumentHelper.FormObjectCreator.CreateHLine (child);
                    break;

                case "vline":
                    childObj = DocumentHelper.FormObjectCreator.CreateVLine (child);
                    break;

                case "table":
                    childObj = DocumentHelper.FormObjectCreator.CreateTable (child, boundFields);
                    break;

                case "boxitem":
                    childObj = DocumentHelper.FormObjectCreator.CreateBox (child, boundFields);
                    break;

                case "barcode":
                    childObj = DocumentHelper.FormObjectCreator.CreateLinearBarcode (child, boundFields);
                    break;

                default:
                    return;
            }

            children.Add (childObj);
        }

        protected virtual void OnChildObjectsListChanged (object sender, ListChangedEventArgs e)
        {
            IList children = (IList) sender;
            if (e.ListChangedType == ListChangedType.ItemAdded) {
                FormObject formObject = (FormObject) children [e.NewIndex];
                formObject.Parent = this;
                formObject.Selectable = selectable;
            }
            for (int i = 0; i < children.Count; i++) {
                FormDrawableObject child = children [i] as FormDrawableObject;
                if (child != null)
                    child.ParentPosition = i;
            }
        }

        protected void SetDefaultStyle (string styleString)
        {
            defaultStyle = new ObjectStyle (styleString);
            style = new ObjectStyle (styleString);
        }

        public virtual XElement GetXmlElement ()
        {
            XElement xmlElement = new XElement (XmlElementName);
            AddAttributes (xmlElement);
            return xmlElement;
        }

        protected virtual string XmlElementName
        {
            get { return GetType ().Name; }
        }

        public Hashtable BindableFields
        {
            get { return bindableFields; }
            set { bindableFields = value; }
        }

        public Type ObjectType
        {
            get { return objectType; }
        }

        protected virtual void AddAttributes (XElement xmlElement)
        {
            if (style != null)
                AddAttribute ("style", style.ToString (defaultStyle), xmlElement);
        }

        protected static void AddAttribute (string name, object value, XElement xmlElement, bool acceptEmptyString = false)
        {
            string stringValue = value.ToString ();
            if (string.IsNullOrEmpty (stringValue) && !acceptEmptyString)
                return;

            xmlElement.Add (new XAttribute (name, stringValue));
        }

        public virtual string ToXmlString ()
        {
            return GetXmlElement ().ToString ();
        }

        public override string ToString ()
        {
            return string.Format ("{0}{1}",
                parent != null ? string.Format ("{0} >>", parent) : string.Empty,
                GetType ().Name);
        }

        protected void OnPropertyChanged (string property)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler (this, new PropertyChangedEventArgs (property));
        }

        #region ICloneable Members

        public abstract object Clone ();

        #endregion

        protected void FillBoundFields (object obj)
        {
            objectType = obj.GetType ();

            bindableFields.Clear ();
            foreach (FormMember member in GetFormMembers (objectType, obj is IDocument).Where (member => member.CanRead))
                bindableFields [member.ReportField] = GetValueFromFormMember (member, obj, member.ReportField);

            ICloneable cloneable = obj as ICloneable;
            sourceObject = cloneable != null ? cloneable.Clone () : obj;
        }

        private static FormMember [] GetFormMembers (Type objType, bool mappedOnly = true)
        {
            return objType.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .SelectMany (member => GetFormMembers (member, mappedOnly))
                .ToArray ();
        }

        public static FormMember [] GetFormMembers (MemberInfo member, bool mappedOnly = true)
        {
            FormMember [] mapped = member.GetCustomAttributes (true)
                .OfType<FormMemberMappingAttribute> ()
                .Select (attr => new FormMember (member, attr.ReportField, attr.Type))
                .ToArray ();

            if (mappedOnly || mapped.Length > 0)
                return mapped;

            return new [] { new FormMember (member, member.Name, FormMemberType.Default) };
        }

        protected virtual object GetValueFromFormMember (FormMember member, object obj, string key)
        {
            return member.GetValue (obj);
        }
    }
}
