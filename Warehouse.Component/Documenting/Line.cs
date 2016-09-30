//
// Line.cs
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
using Warehouse.Data;

namespace Warehouse.Component.Documenting
{
    public abstract class Line : FormDrawableObject
    {
        #region Private fields

        private double? length;
        private double thickness = 1;
        protected bool horizontal = true;

        #endregion

        #region Public properties

        public override SizeD ActualSize
        {
            get
            {
                return visSize;
            }
        }

        public double Thickness
        {
            get { return thickness; }
            set
            {
                if (thickness != value) {
                    thickness = value;
                    OnPropertyChanged ("Thickness");
                }
            }
        }

        public double LengthValue
        {
            get
            {
                if (length.HasValue)
                    return length.Value;

                return horizontal ? calculatedSize.WidthValue : calculatedSize.HeightValue;
            }
        }

        public double? Length
        {
            get { return length; }
            set
            {
                double? finalValue = value >= 0 ? value : null;
                if (length != finalValue) {
                    length = finalValue;
                    OnPropertyChanged ("Length");
                }
            }
        }

        #endregion

        protected Line ()
        {
            style = new ObjectStyle ();
            SetDefaultStyle ("hfill:true;vfill:true");
        }

        protected Line (XElement node)
            : this ()
        {
            CreateFromTemplate (node, null);
        }

        #region FormObject Members

        public override void CreateFromTemplate (XElement node, Hashtable boundFields)
        {
            base.CreateFromTemplate (node, boundFields);

            XAttribute tempAttr;

            tempAttr = node.Attribute ("length");
            if (tempAttr != null)
                Length = float.Parse (tempAttr.Value);

            tempAttr = node.Attribute ("thickness");
            if (tempAttr != null)
                Thickness = float.Parse (tempAttr.Value);
        }

        public override void RefreshBoundFields (Hashtable boundFields)
        {
        }

        public override void ReCheckPageDependant ()
        {
        }

        #endregion

        #region FormDrawableObject Members

        public override void Draw (PointD start, int pageNumber)
        {
            AllocateSize (start);

            base.Draw (start, pageNumber);

            start = DocumentHelper.CurrentPageSettings.GetInPageDrawLocation (start);
            visSize = GetSize (start);
            SizeD contSize = GetContentsSize (start);
            PointD contStart = GetContentStart (start, visSize, contSize);

            DocumentHelper.DrawingProvider.DrawLine (
                contStart.X, contStart.Y,
                contStart.X + (horizontal ? LengthValue : 0f),
                contStart.Y + (horizontal ? 0f : LengthValue),
                Thickness, style.FgColor);
        }

        protected override SizeD AllocateContentsSize (PointD start)
        {
            return horizontal ?
                new SizeD (LengthValue, Thickness) :
                new SizeD (Thickness, LengthValue);
        }

        #endregion

        protected override void AddAttributes (XElement xmlElement)
        {
            if (!thickness.IsEqualTo (1))
                AddAttribute ("thickness", thickness, xmlElement);

            if (length.HasValue)
                AddAttribute ("length", length.Value, xmlElement);

            base.AddAttributes (xmlElement);
        }

        public abstract Line GetClone ();

        public override object Clone ()
        {
            Line ret = GetClone ();

            ret.Style = (ObjectStyle) style.Clone ();
            ret.bindableFields = new Hashtable (bindableFields);
            ret.Length = Length;
            ret.Thickness = Thickness;
            ret.horizontal = horizontal;

            return ret;
        }
    }
}
