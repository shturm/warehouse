//
// WrapLabel.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Created:
//   05/22/2009
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using Gtk;
using Warehouse.Business.Documenting;
using Warehouse.Component.Documenting;
using Rectangle = Gdk.Rectangle;
using WrapMode = Pango.WrapMode;

namespace Warehouse.Component
{
    public class WrapLabel : Widget
    {
        private string text;
        private bool useMarkup;
        private bool wrap = true;
        private WrapMode wrapMode = WrapMode.Word;
        private bool limitHeight;
        private Pango.Layout layout;
        private HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left;
        private VerticalAlignment verticalAlignment = VerticalAlignment.Top;

        public bool Wrap
        {
            get { return wrap; }
            set
            {
                wrap = value;
                UpdateLayout ();
            }
        }

        public WrapMode WrapMode
        {
            get { return wrapMode; }
            set
            {
                wrapMode = value;
                UpdateLayout ();
            }
        }

        public string Markup
        {
            get { return text; }
            set
            {
                useMarkup = true;
                text = value;
                UpdateLayout ();
            }
        }

        public string Text
        {
            get { return text; }
            set
            {
                useMarkup = false;
                text = value;
                UpdateLayout ();
            }
        }

        public bool LimitHeight
        {
            get { return limitHeight; }
            set
            {
                limitHeight = value;
                UpdateLayout ();
            }
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get { return horizontalAlignment; }
            set
            {
                if (horizontalAlignment == value)
                    return;

                horizontalAlignment = value;
                UpdateLayout ();
            }
        }

        public VerticalAlignment VerticalAlignment
        {
            get { return verticalAlignment; }
            set
            {
                if (verticalAlignment == value)
                    return;

                verticalAlignment = value;
                QueueDraw ();
            }
        }

        public WrapLabel ()
        {
            WidgetFlags |= WidgetFlags.NoWindow;
        }

        private void CreateLayout ()
        {
            if (layout != null) {
                layout.Dispose ();
            }

            layout = new Pango.Layout (PangoContext);
        }

        private void UpdateLayout ()
        {
            if (layout == null) {
                CreateLayout ();
            }

            layout.Wrap = wrapMode;
            layout.Ellipsize = wrap ? Pango.EllipsizeMode.None : Pango.EllipsizeMode.End;
            switch (horizontalAlignment) {
                case HorizontalAlignment.Center:
                    layout.Alignment = Pango.Alignment.Center;
                    break;
                case HorizontalAlignment.Right:
                    layout.Alignment = Pango.Alignment.Right;
                    break;
                default:
                    layout.Alignment = Pango.Alignment.Left;
                    break;
            }

            if (useMarkup) {
                layout.SetMarkup (text);
            } else {
                layout.SetText (text);
            }

            QueueResize ();
        }

        protected override void OnStyleSet (Style previous_style)
        {
            CreateLayout ();
            UpdateLayout ();
            base.OnStyleSet (previous_style);
        }

        protected override void OnRealized ()
        {
            GdkWindow = Parent.GdkWindow;
            base.OnRealized ();
        }

        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            int lw, lh;

            layout.Width = (int) (allocation.Width * Pango.Scale.PangoScale);
            layout.GetPixelSize (out lw, out lh);

            Alignment alg = Parent as Alignment;
            if (alg != null && limitHeight) {
                HeightRequest = Math.Min ((int) (alg.Allocation.Height - alg.TopPadding - alg.BottomPadding), lh);
            } else {
                HeightRequest = lh;
            }

            base.OnSizeAllocated (allocation);
        }

        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            if (evnt.Window == GdkWindow) {
                int yPos = Allocation.Y;
                Rectangle area = evnt.Area;
                switch (verticalAlignment) {
                    case VerticalAlignment.Center:
                        yPos += Math.Max (area.Height - HeightRequest, 0) / 2;
                        break;
                    case VerticalAlignment.Bottom:
                        yPos += Math.Max (area.Height - HeightRequest, 0);
                        break;
                }

                Style.PaintLayout (Style, GdkWindow, State, false,
                    area, this, null, Allocation.X, yPos, layout);
            }

            return true;
        }

        public void MarkupFormat (string format, params object [] args)
        {
            if (args == null || args.Length == 0) {
                Markup = format;
                return;
            }

            for (int i = 0; i < args.Length; i++) {
                if (args [i] is string) {
                    args [i] = GLib.Markup.EscapeText ((string) args [i]);
                }
            }

            Markup = string.Format (format, args);
        }
    }
}