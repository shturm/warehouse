//
// Section.cs
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
using Cairo;

namespace Warehouse.Component.Documenting
{
    public class Section : FormDrawableContainer
    {
        public enum Appearance
        {
            General,
            PageHeader,
            PageBreak,
            PageFooter
        }

        #region Private members

        private Appearance sectionType;
        private Form parentForm;

        #endregion

        #region Public properties

        protected override string XmlElementName
        {
            get { return "Section"; }
        }

        public Appearance SectionType
        {
            get
            {
                return sectionType;
            }
            set
            {
                if (sectionType != value) {
                    Form formParent = parentForm ?? (Form) Parent;
                    if (formParent != null) {
                        Appearance oldSectionType = sectionType;
                        sectionType = value;
                        switch (sectionType) {
                            case Appearance.General:
                            case Appearance.PageBreak:
                                switch (oldSectionType) {
                                    case Appearance.PageHeader:
                                        formParent.Children.Insert (0, this);
                                        break;
                                    case Appearance.PageFooter:
                                        formParent.Children.Add (this);
                                        break;
                                }
                                parentForm = null;
                                break;
                            case Appearance.PageHeader:
                                if (oldSectionType == Appearance.PageFooter)
                                    formParent.PageFooter = null;
                                formParent.PageHeader = this;
                                break;
                            case Appearance.PageFooter:
                                if (oldSectionType == Appearance.PageHeader)
                                    formParent.PageHeader = null;
                                formParent.PageFooter = this;
                                break;
                        }
                    }
                    sectionType = value;
                    OnPropertyChanged ("SectionType");
                }
            }
        }

        public Form ParentForm
        {
            get { return parentForm; }
            set { parentForm = value; }
        }

        #endregion

        public Section ()
        {
            childDistribution = ObjectStyle.ChildDistribution.Mixed;
            SetDefaultStyle ("width:100%;vfill:true;hfill:true");
            SectionType = Appearance.General;
        }

        public Section (XElement node, Hashtable boundFields)
            : this ()
        {
            CreateFromTemplate (node, boundFields);
        }

        protected override SizeD AllocateContentsSize (PointD start)
        {
            if (sectionType == Appearance.PageBreak) {
                bool isPreviousSiblingPageBreak = IsPreviousSiblingPageBreak ();
                if (start.Y > 0 || isPreviousSiblingPageBreak) {
                    double height = start.Y % DocumentHelper.CurrentPageSettings.Height;
                    if (height > 0)
                        height = DocumentHelper.CurrentPageSettings.Height - height;

                    return height > 0 || !isPreviousSiblingPageBreak ?
                        new SizeD (DocumentHelper.CurrentPageSettings.Width, height) :
                        new SizeD (DocumentHelper.CurrentPageSettings.Width, DocumentHelper.CurrentPageSettings.Height);
                }
            }

            return base.AllocateContentsSize (start);
        }

        protected override IEnumerable<Type> GetAllowedParentTypes ()
        {
            return new [] { typeof (Form) };
        }

        private bool IsPreviousSiblingPageBreak ()
        {
            FormDrawableContainer container = Parent as FormDrawableContainer;
            if (container != null) {
                int index = container.Children.IndexOf (this);
                if (index > 0) {
                    Section section = container.Children [index - 1] as Section;
                    if (section != null && section.SectionType == Appearance.PageBreak)
                        return true;
                }
            }

            return false;
        }

        #region FormObject Members

        public override void CreateFromTemplate (XElement node, Hashtable boundFields)
        {
            bindableFields = boundFields;

            base.CreateFromTemplate (node, boundFields);

            XAttribute tempAttr = node.Attribute ("type");
            if (tempAttr != null)
                SectionType = (Appearance) Enum.Parse (typeof (Appearance), tempAttr.Value, true);

            CreateChildrenFromTemplate (node, boundFields);
        }

        #endregion

        protected override void AddAttributes (XElement xmlElement)
        {
            if (sectionType != Appearance.General)
                AddAttribute ("type", sectionType, xmlElement);
            base.AddAttributes (xmlElement);
        }

        #region ICloneable Members

        public override object Clone ()
        {
            return Clone (DocumentHelper.FormObjectCreator.CreateSection ());
        }

        #endregion
    }
}
