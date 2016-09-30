//
// ObjectSize.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   11/22/2006
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
using System.Text;

namespace Warehouse.Component.Documenting
{
    public class ObjectSize : ICloneable
    {
        #region Private fields

        private double? width;
        private double? height;
        private double? relativeWidth;
        private double? relativeHeight;

        #endregion

        #region Public properties

        public double? Width
        {
            get { return width; }
            set
            {
                if (width == value)
                    return;

                width = value;
                OnChanged ();
            }
        }

        public double WidthValue
        {
            get { return width ?? 0f; }
        }

        public double? Height
        {
            get { return height; }
            set
            {
                if (height == value)
                    return;

                height = value;
                OnChanged ();
            }
        }

        public double HeightValue
        {
            get { return height ?? 0f; }
        }

        public double? RelativeWidth
        {
            get { return relativeWidth; }
            set
            {
                if (relativeWidth == value)
                    return;

                relativeWidth = value;
                OnChanged ();
            }
        }

        public double RelativeWidthValue
        {
            get { return relativeWidth ?? 0f; }
        }

        public double? RelativeHeight
        {
            get { return relativeHeight; }
            set
            {
                if (relativeHeight == value)
                    return;

                relativeHeight = value;
                OnChanged ();
            }
        }

        public double RelativeHeightValue
        {
            get { return relativeHeight ?? 0f; }
        }

        public SizeD StaticSize
        {
            get { return new SizeD (WidthValue, HeightValue); }
        }

        public event EventHandler Changed;

        #endregion

        public void LoadFromStyle (ObjectStyle style, bool silent = false)
        {
            if (silent) {
                width = style.Width;
                height = style.Height;
                relativeWidth = style.RelativeWidth;
                relativeHeight = style.RelativeHeight;
            } else {
                Width = style.Width;
                Height = style.Height;
                RelativeWidth = style.RelativeWidth;
                RelativeHeight = style.RelativeHeight;
            }
        }

        public void SaveToStyle (ObjectStyle style)
        {
            style.Width = Width;
            style.Height = Height;
            style.RelativeWidth = RelativeWidth;
            style.RelativeHeight = RelativeHeight;
        }

        public override string ToString ()
        {
            StringBuilder sb = new StringBuilder ();

            sb.Append ("Width = ");
            if (width.HasValue && relativeWidth.HasValue) {
                sb.Append (Width);
                sb.AppendFormat (" ({0}%)", RelativeWidth);
            } else if (relativeWidth.HasValue) {
                sb.AppendFormat ("{0}%", RelativeWidth);
            } else {
                sb.Append (Width);
            }

            sb.Append (" Height = ");
            if (height.HasValue && RelativeHeight.HasValue) {
                sb.Append (Height);
                sb.AppendFormat (" ({0}%)", RelativeHeight);
            } else if (RelativeHeight.HasValue) {
                sb.AppendFormat ("{0}%", RelativeHeight);
            } else {
                sb.Append (Height);
            }

            return sb.ToString ();
        }

        private void OnChanged ()
        {
            EventHandler onChanged = Changed;
            if (onChanged != null)
                onChanged (this, EventArgs.Empty);
        }

        #region ICloneable Members

        public object Clone ()
        {
            return MemberwiseClone ();
        }

        #endregion
    }
}
