//
// ListView.DragAndDrop.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Created:
//   09/23/2008
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

using Gdk;
using Gtk;
using Warehouse.Data;
using Drag = Gtk.Drag;

namespace Warehouse.Component.ListView
{
    public static class ListViewDragDropTarget
    {
        public enum TargetType
        {
            ModelSelection
        }

        public static readonly TargetEntry ModelSelection =
            new TargetEntry ("application/x-hyena-data-model-selection", TargetFlags.App,
                             (uint)TargetType.ModelSelection);
    }

    public partial class ListView
    {
        private static TargetEntry [] drag_drop_dest_entries = new []
                                                                   {
                                                                       ListViewDragDropTarget.ModelSelection
                                                                   };

        protected static TargetEntry [] DragDropDestEntries {
            get { return drag_drop_dest_entries; }
        }

        private bool rowsDraggable;
        public bool RowsDraggable
        {
            get { return rowsDraggable; }
            set
            {
                rowsDraggable = value;
                OnDragSourceSet ();
                OnDragDestSet ();
            }
        }

        private bool reorderable;
        public bool Reorderable {
            get { return reorderable; }
            set {
                reorderable = value;
                OnDragSourceSet ();
                OnDragDestSet ();
            }
        }

        private bool force_drag_source_set;
        protected bool ForceDragSourceSet {
            get { return force_drag_source_set; }
            set {
                force_drag_source_set = true;
                OnDragSourceSet ();
            }
        }

        private bool force_drag_dest_set;
        protected bool ForceDragDestSet {
            get { return force_drag_dest_set; }
            set {
                force_drag_dest_set = true;
                OnDragDestSet ();
            }
        }

        protected virtual void OnDragDestSet ()
        {
            if (ForceDragDestSet || Reorderable || RowsDraggable)
                Drag.DestSet (this, DestDefaults.All, DragDropDestEntries, DragAction.Move);
            else
                Drag.DestUnset (this);
        }

        protected virtual void OnDragSourceSet ()
        {
            if (ForceDragSourceSet || Reorderable || RowsDraggable)
                Drag.SourceSet (this, ModifierType.Button1Mask | ModifierType.Button3Mask,
                    DragDropDestEntries, DragAction.Copy | DragAction.Move);
            else
                Drag.SourceUnset (this);
        }

        private uint drag_scroll_timeout_id;
        private uint drag_scroll_timeout_duration = 50;
        private double drag_scroll_velocity;
        private double drag_scroll_velocity_max = 100.0;
        private int drag_reorder_row_index = -1;
        private int drag_reorder_motion_y = -1;

        private void StopDragScroll ()
        {
            drag_scroll_velocity = 0.0;
            
            if (drag_scroll_timeout_id > 0) {
                GLib.Source.Remove (drag_scroll_timeout_id);
                drag_scroll_timeout_id = 0;
            }
        }

        private void OnDragScroll (GLib.TimeoutHandler handler, double threshold, int total, int position)
        {
            if (position < threshold) {
                drag_scroll_velocity = -1.0 + (position / threshold);
            } else if (position > total - threshold) {
                drag_scroll_velocity = 1.0 - ((total - position) / threshold);
            } else {
                StopDragScroll ();
                return;
            }
            
            if (drag_scroll_timeout_id == 0) {
                drag_scroll_timeout_id = GLib.Timeout.Add (drag_scroll_timeout_duration, handler);
            }
        }

        protected override bool OnDragMotion (DragContext context, int x, int y, uint time)
        {
            DragSelect (y);
            if (!Reorderable) {
                StopDragScroll ();
                drag_reorder_row_index = -1;
                drag_reorder_motion_y = -1;
                InvalidateList ();
                return false;
            }
            
            drag_reorder_motion_y = y;
            DragReorderUpdateRow ();
            
            OnDragScroll (OnDragVScrollTimeout, Allocation.Height * 0.3, Allocation.Height, y);
            
            return true;
        }

        protected override void OnDragLeave (DragContext context, uint time)
        {
            StopDragScroll ();
            base.OnDragLeave (context, time);
        }

        protected override void OnDragBegin (DragContext context)
        {
            base.OnDragBegin (context);
            if (Selection.Contains (FocusedRow))
                return;
            double [] axes;
            ModifierType modifier;
            Device.CorePointer.GetState (RootWindow, out axes, out modifier);
            const ModifierType buttonMask = ModifierType.Button1Mask | ModifierType.Button2Mask | 
                ModifierType.Button3Mask | ModifierType.Button4Mask |ModifierType.Button5Mask;
            Select (modifier & ~buttonMask, (uint) (modifier & buttonMask), FocusedRow, false);
        }

        protected override void OnDragEnd (DragContext context)
        {
            dragSelect = false;
            StopDragScroll ();
            drag_reorder_row_index = -1;
            drag_reorder_motion_y = -1;
            InvalidateList ();
            base.OnDragEnd (context);
			// HACK: surprise, GTK fails again; it cannot detect the released button on the mac and drag-selection doesn't stop
			if (PlatformHelper.Platform == Warehouse.Data.PlatformTypes.MacOSX) {
				pressed_row_index = -1;
	            last_row_index = -1;
			}
        }

        private bool OnDragVScrollTimeout ()
        {
            ScrollToV (vadjustment.Value + (drag_scroll_velocity * drag_scroll_velocity_max));
            DragReorderUpdateRow ();
            return true;
        }

        private void DragReorderUpdateRow ()
        {
            int row = GetRowAtY (drag_reorder_motion_y) - 1;
            if (row != drag_reorder_row_index) {
                drag_reorder_row_index = row;
                InvalidateList ();
            }   
        }
    }
}
