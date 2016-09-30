//
// BoxItem.cs
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

namespace Warehouse.Component.Documenting
{
    public class BoxItem : FormDrawableContainer
    {
        protected override string XmlElementName
        {
            get { return "BoxItem"; }
        }

        public BoxItem ()
        {
            SetDefaultStyle ("vfill:true");
            childDistribution = ObjectStyle.ChildDistribution.Mixed;
        }

        public BoxItem (XElement node, Hashtable boundFields)
            : this ()
        {
            CreateFromTemplate (node, boundFields);
        }

        #region FormObject Members

        public override void CreateFromTemplate (XElement node, Hashtable boundFields)
        {
            bindableFields = boundFields;

            base.CreateFromTemplate (node, boundFields);

            if (!style.Width.HasValue && !style.RelativeWidth.HasValue &&
                ((style.HFill.HasValue && !style.HFill.Value) || !style.HFill.HasValue))
                style.RelativeWidth = 100f;

            CreateChildrenFromTemplate (node, boundFields);
        }

        #endregion

        #region ICloneable Members

        public override object Clone ()
        {
            return Clone (DocumentHelper.FormObjectCreator.CreateBox ());
        }

        #endregion
    }
}
