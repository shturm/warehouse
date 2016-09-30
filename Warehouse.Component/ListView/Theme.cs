//
// Theme.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Created:
//   09/23/2008
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Collections.Generic;
using Gtk;
using Cairo;
using Warehouse.Data;
using Rectangle = Gdk.Rectangle;

namespace Warehouse.Component.ListView
{
    public abstract class Theme
    {
        private readonly Stack<ThemeContext> contexts = new Stack<ThemeContext> ();
        private readonly GtkColors colors;

        private Color view_fill;
        private Color view_fill_transparent;

        public GtkColors Colors
        {
            get { return colors; }
        }

        protected Theme (Widget widget)
        {
            colors = new GtkColors ();
            colors.Refreshed += delegate { OnColorsRefreshed (); };
            colors.Widget = widget;

            PushContext ();
        }

        protected virtual void OnColorsRefreshed ()
        {
            view_fill = colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal);
            view_fill_transparent = view_fill;
            view_fill_transparent.A = 0;
        }

        #region Drawing

        public abstract void DrawArrow (Context cr, Rectangle alloc, SortDirection type);

        public void DrawFrameBackground (Context cr, Rectangle alloc, bool baseColor)
        {
            DrawFrameBackground (cr, alloc, baseColor
                ? colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal)
                : colors.GetWidgetColor (GtkColorClass.Background, StateType.Normal));
        }

        protected abstract void DrawFrameBackground (Context cr, Rectangle alloc, Color color, Pattern pattern = null);

        public abstract void DrawFrameBorder (Context cr, Rectangle alloc);

        public abstract void DrawHeaderBackground (Context cr, Rectangle alloc);

        public abstract void DrawHeaderSortBackground (Context cr, Rectangle alloc);

        public abstract void DrawHeaderSeparator (Context cr, Rectangle alloc, int x);

        public abstract void DrawFooterBackground (Context cr, Rectangle alloc);

        public abstract void DrawFooterSeparator (Context cr, Rectangle alloc, int x);

        public abstract void DrawColumnHighlight (Context cr, Rectangle alloc, Color color);

        public void DrawRowSelection (Context cr, int x, int y, int width, int height, bool filled = true)
        {
            DrawRowSelection (cr, x, y, width, height, filled, true,
                colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected));
        }

        public abstract void DrawRowSelection (Context cr, int x, int y, int width, int height,
            bool filled, bool stroked, Color color, CairoCorners corners = CairoCorners.All);

        public abstract void DrawRowRule (Context cr, int x, int y, int width, int height);

        protected Color ViewFill
        {
            get { return view_fill; }
        }

        public int BorderWidth
        {
            get { return 1; }
        }

        public int InnerBorderWidth
        {
            get { return 4; }
        }

        public int TotalBorderWidth
        {
            get { return BorderWidth + InnerBorderWidth; }
        }

        #endregion

        #region Contexts

        private void PushContext ()
        {
            PushContext (new ThemeContext ());
        }

        private void PushContext (ThemeContext context)
        {
            lock (this) {
                contexts.Push (context);
            }
        }

        public ThemeContext Context
        {
            get { lock (this) { return contexts.Peek (); } }
        }

        #endregion

        #region Static Utilities

        public static double Clamp (double min, double max, double value)
        {
            return Math.Max (min, Math.Min (max, value));
        }

        #endregion

    }
}
