//
// PrintLayout.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   01.27.2011
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
using Gdk;
using Gtk;
using Window = Gdk.Window;
using WindowType = Gdk.WindowType;

namespace Warehouse.Component.Printing
{
    public class PrintLayout : Layout
    {
        private Window eventWindow;

        public PrintLayout (Adjustment hadjustment, Adjustment vadjustment) : base (hadjustment, vadjustment)
        {
        }

        protected override void OnRealized ()
        {
            WidgetFlags |= WidgetFlags.Realized | WidgetFlags.NoWindow;
            GdkWindow = Parent.GdkWindow;

            WindowAttr attributes = new WindowAttr ();
            attributes.WindowType = WindowType.Child;
            attributes.X = Allocation.X;
            attributes.Y = Allocation.Y;
            attributes.Width = Allocation.Width;
            attributes.Height = Allocation.Height;
            attributes.Wclass = WindowClass.InputOnly;
            attributes.EventMask = (int) (EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.ButtonMotionMask);

            const WindowAttributesType attributesMask = WindowAttributesType.X | WindowAttributesType.Y | WindowAttributesType.Wmclass;

            eventWindow = new Window (GdkWindow, attributes, attributesMask);
            eventWindow.UserData = Handle;

            base.OnRealized ();
        }

        protected override void OnUnrealized ()
        {
            base.OnUnrealized ();

            eventWindow.UserData = IntPtr.Zero;
            eventWindow.Destroy ();
            eventWindow = null;
        }

        protected override void OnMapped ()
        {
            base.OnMapped ();
            eventWindow.Show ();
        }

        protected override void OnUnmapped ()
        {
            base.OnUnmapped ();
            eventWindow.Hide ();
        }

        protected override void OnSizeAllocated (Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);

            if (IsRealized)
                eventWindow.MoveResize (allocation);
        }
    }
}
