//
// TableCell.cs
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
using System.Xml.Linq;
using Cairo;
using Warehouse.Data.DataBinding;

namespace Warehouse.Component.Documenting
{
    public class TableCell : FormDrawableContainer
    {
        protected override bool AllowHorizontalBreak
        {
            get { return ((FormDrawableContainer) Parent).Children.Count > 1; }
        }

        public BindList<FormObject> Template
        {
            get { return childObjects; }
        }

        public TableCell ()
        {
            SetDefaultStyle ("vfill:true;hfill:true;allowhbreak:false;allowvbreak:false;padding:2");
            childDistribution = ObjectStyle.ChildDistribution.Mixed;
        }

        public TableCell (XElement node, Hashtable boundFields)
            : this ()
        {
            CreateFromTemplate (node, boundFields);
        }

        #region FormObject Members

        public override void CreateFromTemplate (XElement node, Hashtable boundFields)
        {
            bindableFields = boundFields;

            base.CreateFromTemplate (node, boundFields);

            if (!style.Width.HasValue && !style.RelativeWidth.HasValue)
                style.HFill = true;

            if (!style.Height.HasValue && !style.RelativeHeight.HasValue)
                style.VFill = true;

            XAttribute tempAttr = node.Attribute ("text");
            if (tempAttr != null) {
                TextBox child = DocumentHelper.FormObjectCreator.CreateTextBox ();
                childObjects.Add (child);
                child.Text = tempAttr.Value;
                child.Style = style;
            }

            if (node.HasElements) {
                CreateChildrenFromTemplate (node, boundFields);
            }
        }

        #endregion

        #region FormDrawableObject Members

        public override void Draw (PointD start, int pageNumber)
        {
            if (Template.Count == 0 && !style.DrawEmpty)
                return;

            base.Draw (start, pageNumber);
        }

        public override FormDrawableContainer GetAllowedParent (FormDrawableContainer parentToCheck)
        {
            if (!(parentToCheck is TableCell))
                return null;
            FormDrawableContainer currentParent = parentToCheck;
            while (currentParent != null) {
                if (currentParent == Parent.Parent)
                    return parentToCheck;
                currentParent = (FormDrawableContainer) currentParent.Parent;
            }
            return null;
        }

        #endregion

        protected override string XmlElementName
        {
            get { return "Cell"; }
        }

        #region ICloneable Members

        public override object Clone ()
        {
            return Clone (DocumentHelper.FormObjectCreator.CreateCell ());
        }

        #endregion
    }
}
