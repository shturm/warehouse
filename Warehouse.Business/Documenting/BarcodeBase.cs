// 
// BarcodeBase.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
// 
// Created:
//    14.02.2012
// 
// 2006-2012 (C) Microinvest, http://www.microinvest.net
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
using System.Drawing;
using System.Xml.Linq;

namespace Warehouse.Business.Documenting
{
    public abstract class BarcodeBase : FormDrawableObject
    {
        private string sourceField;
        protected Image image;

        public override SizeF ActualSize
        {
            get { return visSize; }
        }

        public string SourceField
        {
            get { return sourceField; }
            set
            {
                if (sourceField != value) {
                    sourceField = value;
                    OnPropertyChanged ("SourceField");
                }
            }
        }

        public override void ReCheckPageDependant ()
        {
        }

        protected override SizeF AllocateContentsSize (PointF start)
        {
            bool formatError;
            SizeF size;
            GetImage (out formatError, out size);
            float width = style.Width.HasValue ? style.Width.Value : size.Width;
            float height = style.Height.HasValue ? style.Height.Value : size.Height;
            return new SizeF (width, height);
        }

        public override void Draw (PointF start, int pageNumber)
        {
            base.Draw (start, pageNumber);

            start = DocumentHelper.CurrentPageSettings.GetInPageDrawLocation (start);
            visSize = GetSize (start);
            SizeF contSize = GetContentsSize (start);
            if (contSize.Width > visSize.Width)
                contSize.Width = visSize.Width;

            PointF contStart = GetContentStart (start, visSize, contSize);

            if (style.BgColorSpecified)
                DocumentHelper.DrawingProvider.FillRectangle (
                    start.X, start.Y,
                    visSize.Width, visSize.Height, style.BgColor);

            if (style.Border > 0f)
                DocumentHelper.DrawingProvider.DrawInsetRectangle (
                    start.X, start.Y,
                    visSize.Width, visSize.Height, style.Border.Value, style.BorderColor);

            bool formatError;
            SizeF size;
            GetImage (out formatError, out size);
            if (formatError) {
                DrawError (contStart);
                return;
            }

            if (visSize.Width == 0)
                visSize.Width = image.Width;
            if (visSize.Height == 0)
                visSize.Height = image.Height;

            DocumentHelper.DrawingProvider.DrawImage (new RectangleF (contStart, calculatedSize.StaticSize), image);
        }

        protected virtual void DrawError (PointF contStart)
        {
        }

        protected abstract void GetImage (out bool formatError, out SizeF size);

        public override void CreateFromTemplate (XElement node, Hashtable boundFields)
        {
            base.CreateFromTemplate (node, boundFields);

            XAttribute sourceFieldAttribute = node.Attribute ("sourceField");
            if (sourceFieldAttribute != null)
                SourceField = sourceFieldAttribute.Value;

            RefreshBoundFields (boundFields);
        }
    }
}
