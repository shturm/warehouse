//
// Picture.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   11.05.2010
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
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using Cairo;
using Gdk;
using Warehouse.Business;
using Path = System.IO.Path;

namespace Warehouse.Component.Documenting
{
    public class Picture : FormDrawableObject
    {
        private const string defaultPicture = "Warehouse.Component;Documenting.PicturePlaceholder.jpg";

        private Pixbuf image;
        private string fileName;
        private string resource;
        private SizeMode sizeMode;

        public Picture ()
        {
            SetDefaultStyle ("allowhbreak:false;allowvbreak:false");
        }

        public Picture (XElement child, Hashtable boundFields)
            : this ()
        {
            CreateFromTemplate (child, boundFields);
        }

        ~Picture ()
        {
            DisposeImage ();
        }

        public override SizeD ActualSize
        {
            get { return visSize; }
        }

        protected override string XmlElementName
        {
            get { return "Picture"; }
        }

        public string FileName
        {
            get { return fileName; }
            set
            {
                // backward compatibility
                string file = Path.GetFileName (value);
                if (fileName == file)
                    return;

                fileName = file;
                if (string.IsNullOrEmpty (fileName)) {
                    OnPropertyChanged ("FileName");
                    return;
                }

                string path = Path.Combine (BusinessDomain.AppConfiguration.DocumentTemplatesFolder, fileName);
                if (File.Exists (path)) {
                    DisposeImage ();
                    try {
                        image = new Pixbuf (path);
                    } catch (Exception) {
                        LoadFromResource (true);
                    }
                } else
                    LoadFromResource (true);

                OnPropertyChanged ("FileName");
            }
        }

        public string Resource
        {
            get { return resource; }
            set
            {
                if (resource == value)
                    return;

                resource = value;
                OnPropertyChanged ("Resource");

                LoadFromResource ();
            }
        }

        public SizeMode SizeMode
        {
            get { return sizeMode; }
            set
            {
                if (sizeMode != value) {
                    sizeMode = value;
                    OnPropertyChanged ("SizeMode");
                }
            }
        }

        protected void LoadFromResource (bool loadDefault = false)
        {
            DisposeImage ();

            if (!loadDefault && string.IsNullOrWhiteSpace (resource)) {
                LoadFromResource (true);
            } else {
                string [] strings = (loadDefault ? defaultPicture : resource).Split (';');
                try {
                    image = new Pixbuf (Assembly.Load (strings [0]), strings [0] + '.' + strings [1]);
                } catch (Exception) {
                }
            }
        }

        private void DisposeImage ()
        {
            if (image != null) {
                image.Dispose ();
                image = null;
            }
        }

        public override void CreateFromTemplate (XElement node, Hashtable boundFields)
        {
            base.CreateFromTemplate (node, boundFields);

            XAttribute attr = node.Attribute ("resource");
            if (attr != null)
                Resource = attr.Value;

            attr = node.Attribute ("fileName");
            if (attr != null)
                FileName = attr.Value;

            attr = node.Attribute ("sizeMode");
            SizeMode value;
            if (attr != null && Enum.TryParse (attr.Value, true, out value))
                sizeMode = value;

            RefreshBoundFields (boundFields);
        }

        public override void RefreshBoundFields (Hashtable boundFields)
        {
        }

        public override void ReCheckPageDependant ()
        {
            FormDrawableObject drawableParent = Parent as FormDrawableObject;
            if (drawableParent == null)
                return;

            drawableParent.ReCheckPageDependant ();
        }

        public override object Clone ()
        {
            Picture picture = DocumentHelper.FormObjectCreator.CreatePicture ();
            picture.bindableFields = new Hashtable (bindableFields);
            picture.FileName = FileName;
            picture.sizeMode = sizeMode;
            picture.style = (ObjectStyle) style.Clone ();
            return picture;
        }

        protected override SizeD AllocateContentsSize (PointD start)
        {
            float width = image == null ? 0 : image.Width;
            float height = image == null ? 0 : image.Height;
            return new SizeD (calculatedSize.WidthValue > 0 ? calculatedSize.WidthValue : width,
                calculatedSize.HeightValue > 0 ? calculatedSize.HeightValue : height);
        }

        public override void Draw (PointD start, int pageNumber)
        {
            base.Draw (start, pageNumber);

            start = DocumentHelper.CurrentPageSettings.GetInPageDrawLocation (start);
            visSize = GetSize (start);
            SizeD contSize = GetContentsSize (start);
            if (contSize.Width > visSize.Width)
                contSize.Width = visSize.Width;

            PointD contStart = GetContentStart (start, visSize, contSize);

            if (style.BgColorSpecified)
                DocumentHelper.DrawingProvider.FillRectangle (
                    start.X, start.Y,
                    visSize.Width, visSize.Height, style.BgColor);

            if (image != null && image.Width > 0 && image.Height > 0) {
                RectangleD targetArea = new RectangleD (contStart, visSize);
                RectangleD sourceArea = new RectangleD (new PointD (), new SizeD (image.Width, image.Height));

                SizeD size;
                double width;
                switch (sizeMode) {
                    case SizeMode.Stretch:
                        DocumentHelper.DrawingProvider.DrawImage (image, targetArea, sourceArea, sizeMode);
                        break;
                    case SizeMode.Fit:
                        size = targetArea.Size;
                        width = size.Height * image.Width / image.Height;
                        if (width > size.Width)
                            size.Height = size.Width * image.Height / image.Width;
                        else
                            size.Width = width;

                        PointD imageStart = contStart;
                        if (targetArea.Width > size.Width)
                            imageStart.X += (targetArea.Width - size.Width) / 2;

                        if (targetArea.Height > size.Height)
                            imageStart.Y += (targetArea.Height - size.Height) / 2;

                        DocumentHelper.DrawingProvider.DrawImage (image, new RectangleD (imageStart, size), sourceArea, sizeMode);
                        break;
                    case SizeMode.Fill:
                        size = targetArea.Size;
                        width = size.Height * image.Width / image.Height;
                        if (width > size.Width) {
                            double scale = size.Height / image.Height;
                            sourceArea.X = (width - size.Width) / (2 * scale);
                            sourceArea.Width -= sourceArea.X * 2;
                        } else {
                            double height = size.Width * image.Height / image.Width;
                            double scale = size.Width / image.Width;
                            sourceArea.Y = (height - size.Height) / (2 * scale);
                            sourceArea.Height -= sourceArea.Y * 2;
                        }

                        DocumentHelper.DrawingProvider.DrawImage (image, targetArea, sourceArea, sizeMode);
                        break;
                    case SizeMode.Crop:
                        if (targetArea.Width > image.Width) {
                            targetArea.X += (targetArea.Width - image.Width) / 2;
                            targetArea.Width = image.Width;
                        } else {
                            sourceArea.X = (image.Width - targetArea.Width) / 2;
                            sourceArea.Width = targetArea.Width;
                        }

                        if (targetArea.Height > image.Height) {
                            targetArea.Y += (targetArea.Height - image.Height) / 2;
                            targetArea.Height = image.Height;
                        } else {
                            sourceArea.Y = (image.Height - targetArea.Height) / 2;
                            sourceArea.Height = targetArea.Height;
                        }

                        DocumentHelper.DrawingProvider.DrawImage (image, targetArea, sourceArea, sizeMode);
                        break;
                }
            }

            if (style.Border > 0f)
                DocumentHelper.DrawingProvider.DrawInsetRectangle (
                    start.X, start.Y,
                    visSize.Width, visSize.Height, style.Border.Value, style.BorderColor);
        }

        protected override void AddAttributes (XElement xmlElement)
        {
            if (!string.IsNullOrEmpty (resource))
                AddAttribute ("resource", resource, xmlElement);

            if (!string.IsNullOrEmpty (fileName))
                AddAttribute ("fileName", fileName, xmlElement);

            AddAttribute ("sizeMode", sizeMode.ToString (), xmlElement);
            base.AddAttributes (xmlElement);
        }
    }
}
