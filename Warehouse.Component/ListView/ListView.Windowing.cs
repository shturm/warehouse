//
// ListView.Windowing.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/23/2008
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
using Warehouse.Data.Model;

namespace Warehouse.Component.ListView
{
    public partial class ListView
    {
        private Rectangle list_rendering_alloc;
        private Rectangle header_rendering_alloc;
        private Rectangle footer_rendering_alloc;
        private Rectangle list_interaction_alloc;
        private Rectangle header_interaction_alloc;
        private Rectangle footer_interaction_alloc;

        private Gdk.Window event_window;

        protected Rectangle ListAllocation
        {
            get { return list_rendering_alloc; }
        }

        protected override void OnRealized ()
        {
            WidgetFlags |= WidgetFlags.Realized | WidgetFlags.NoWindow;

            GdkWindow = Parent.GdkWindow;
            cell_context.Drawable = GdkWindow;

            WindowAttr attributes = new WindowAttr ();
            attributes.WindowType = Gdk.WindowType.Child;
            attributes.X = Allocation.X;
            attributes.Y = Allocation.Y;
            attributes.Width = Allocation.Width;
            attributes.Height = Allocation.Height;
            attributes.Wclass = WindowClass.InputOnly;
            attributes.EventMask = (int) (
                EventMask.PointerMotionMask |
                EventMask.KeyPressMask |
                EventMask.KeyReleaseMask |
                EventMask.ButtonPressMask |
                EventMask.ButtonReleaseMask |
                EventMask.LeaveNotifyMask |
                EventMask.ExposureMask);

            WindowAttributesType attributes_mask =
                WindowAttributesType.X | WindowAttributesType.Y | WindowAttributesType.Wmclass;

            event_window = new Gdk.Window (GdkWindow, attributes, attributes_mask);
            event_window.UserData = Handle;

            Style = Gtk.Rc.GetStyleByPaths (Settings, "*.GtkTreeView", "*.GtkTreeView", GType);

            OnDragSourceSet ();
            RecalculateWindowSizes (Allocation);

            //base.OnRealized ();
        }

        protected override void OnUnrealized ()
        {
            WidgetFlags &= ~WidgetFlags.Realized;

            event_window.UserData = IntPtr.Zero;
            event_window.Destroy ();
            event_window = null;

            //base.OnUnrealized ();
        }

        protected override void OnMapped ()
        {
            WidgetFlags |= WidgetFlags.Mapped;
            event_window.Show ();
        }

        protected override void OnUnmapped ()
        {
            WidgetFlags &= ~WidgetFlags.Mapped;
            event_window.Hide ();
        }

        private void RecalculateWindowSizes (Rectangle allocation)
        {
            if (Theme == null) {
                return;
            }

            header_rendering_alloc = allocation;
            header_rendering_alloc.Height = HeaderHeight;

            footer_rendering_alloc = allocation;
            footer_rendering_alloc.Y = allocation.Bottom - FooterHeight;
            footer_rendering_alloc.Height = FooterHeight;

            list_rendering_alloc.X = header_rendering_alloc.X + Theme.TotalBorderWidth;
            list_rendering_alloc.Y = header_rendering_alloc.Bottom + Theme.TotalBorderWidth;
            list_rendering_alloc.Width = allocation.Width - Theme.TotalBorderWidth * 2;
            list_rendering_alloc.Height = allocation.Height
                - HeaderHeight - FooterHeight - 2 * Theme.TotalBorderWidth;

            header_interaction_alloc = header_rendering_alloc;
            header_interaction_alloc.X = list_rendering_alloc.X;
            header_interaction_alloc.Width = list_rendering_alloc.Width;
            header_interaction_alloc.Height += Theme.BorderWidth;
            header_interaction_alloc.Offset (-allocation.X, -allocation.Y);

            footer_interaction_alloc = footer_rendering_alloc;
            footer_interaction_alloc.X = list_rendering_alloc.X;
            footer_interaction_alloc.Y -= Theme.BorderWidth;
            footer_interaction_alloc.Width = list_rendering_alloc.Width;
            footer_interaction_alloc.Height += Theme.BorderWidth;
            footer_interaction_alloc.Offset (-allocation.X, -allocation.Y);

            list_interaction_alloc = list_rendering_alloc;
            list_interaction_alloc.Offset (-allocation.X, -allocation.Y);
        }

        protected override void OnSizeRequested (ref Requisition requisition)
        {
            // TODO give the minimum height of the header
            if (Theme == null) {
                return;
            }
            requisition.Width = Theme.TotalBorderWidth * 2;
            requisition.Height = HeaderHeight + Theme.TotalBorderWidth * 2;
        }

        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);

            if (IsRealized) {
                event_window.MoveResize (allocation);
            }

            int oldHeaderAllocWidth = header_interaction_alloc.Width;
            
            RecalculateWindowSizes (allocation);
            
            if (oldHeaderAllocWidth != header_interaction_alloc.Width) {
                AutoCalculateColumnSizes ();
                UpdateColumnCache ();
            }

            if (vadjustment != null) {
                hadjustment.PageSize = header_interaction_alloc.Width;
                hadjustment.PageIncrement = header_interaction_alloc.Width;
                vadjustment.PageSize = list_rendering_alloc.Height;
                vadjustment.PageIncrement = list_rendering_alloc.Height;
                UpdateAdjustments (null, null);
            }

            ICareAboutView careAboutView = Model as ICareAboutView;
            if (careAboutView != null) {
                careAboutView.RowsInView = RowsInView;
            }

            InvalidateList ();
        }

        public int RowsInView
        {
            get { return (int) Math.Ceiling ((list_rendering_alloc.Height + RowHeight) / (double) RowHeight); }
        }
    }
}
