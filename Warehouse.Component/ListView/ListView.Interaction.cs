//
// ListView.Interaction.cs
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

//#define DEBUG_LISTVIEW

using System;
#if DEBUG_LISTVIEW
using System.Diagnostics;
#endif
using GLib;
using Gdk;
using Gtk;
using Warehouse.Business;
using Warehouse.Data;
using Warehouse.Data.Model;
using RowActivatedArgs = Warehouse.Data.RowActivatedArgs;
using RowActivatedHandler = Warehouse.Data.RowActivatedHandler;

namespace Warehouse.Component.ListView
{
    public partial class ListView
    {
        private int pressed_row_index = -1;
        private int last_row_index = -1;
        private bool dragSelect;
        private bool skipLateFocusChange;

        private Adjustment hadjustment;
        public Adjustment Hadjustment
        {
            get { return hadjustment; }
        }

        private Adjustment vadjustment;
        private readonly Selection selection = new Selection ();

        public Selection Selection
        {
            get { return selection; }
        }

        public event RowActivatedHandler RowActivated;

        private bool KeyboardScroll (ModifierType modifier, int relative_row)
        {
            if (relative_row < 0 && Selection.FocusedCell.Row == -1)
                return false;

            int rowIndex = Math.Min (Model.Count - 1, Math.Max (0, Selection.FocusedCell.Row + relative_row));
            if (rowIndex < 0 || rowIndex > Model.Count - 1)
                return true;

            if (Selection != null) {
                if ((modifier & KeyShortcuts.ControlModifier) != 0 && AllowMultipleSelect) {
                    // Don't change the selection
                } else if ((modifier & ModifierType.ShiftMask) != 0 && AllowMultipleSelect) {
                    // Behave like nautilus: if and arrow key + shift is pressed and the currently focused item
                    // is not selected, select it and don't move the focus or vadjustment.
                    // Otherwise, select the new row and scroll etc as necessary.
                    if (relative_row * relative_row != 1) {
                        Selection.SelectFromFirst (rowIndex, true);
                    } else if (Selection.Contains (Selection.FocusedCell.Row)) {
                        Selection.SelectFromFirst (rowIndex, true);
                    } else {
                        Selection.Select (Selection.FocusedCell.Row);
                        return true;
                    }
                } else {
                    Selection.QuietClear ();
                    Selection.Select (rowIndex);
                }
            }

            FocusCell (selection.FocusedCell.Column, rowIndex);
            InvalidateList ();
            return true;
        }

        public void OnKeyPress (EventKey evnt)
        {
            OnKeyPressEvent (evnt);
        }

        protected override bool OnKeyPressEvent (EventKey evnt)
        {
            try {
                return OnCellKeyPressEvent (new CellKeyPressEventArgs (selection.FocusedCell.Column, selection.FocusedCell.Row, null, evnt));
            } catch (Exception ex) {
                ExceptionManager.RaiseUnhandledException (ex, false);
                return true;
            }
        }

        private bool OnCellKeyPressEvent (CellKeyPressEventArgs args)
        {
            if (eventLock)
                return true;

            try {
                eventLock = true;
                if (cellsFucusable && args.Cell.IsValid) {
                    column_cache [args.Cell.Column].Column.ListCell.OnKeyPressEvent (args);
                }

                if (CellKeyPressEvent != null)
                    CellKeyPressEvent (this, args);

#if DEBUG_LISTVIEW
                Debug.WriteLine (string.Format ("ListView received {0}", "OnCellKeyPressEvent"));
#endif

                bool ret;
                // Don't send arrow keys to the parent because the focus gets lost
                switch (args.GdkKey) {
                    case Gdk.Key.Up:
                    case Gdk.Key.KP_Up:
                    case Gdk.Key.Down:
                    case Gdk.Key.KP_Down:
                    case Gdk.Key.Left:
                    case Gdk.Key.Right:
                        ret = true;
                        break;
                    default:
                        ret = base.OnKeyPressEvent (args.EventKey);
                        break;
                }

                switch (args.GdkKey) {
                    case Gdk.Key.Up:
                    case Gdk.Key.KP_Up:
                        if (!manualFucusChange) {
                            return KeyboardScroll (args.EventKey.State, -1);
                        }
                        break;

                    case Gdk.Key.Down:
                    case Gdk.Key.KP_Down:
                        if (!manualFucusChange) {
                            return KeyboardScroll (args.EventKey.State, 1);
                        }
                        break;

                    case Gdk.Key.Left:
                        if (!manualFucusChange && cellsFucusable) {
                            FocusCell (selection.FocusedCell.Column - 1, selection.FocusedCell.Row);
                            InvalidateList ();
                        }
                        break;

                    case Gdk.Key.Right:
                        if (!manualFucusChange && cellsFucusable) {
                            FocusCell (selection.FocusedCell.Column + 1, selection.FocusedCell.Row);
                            InvalidateList ();
                        }
                        break;

                    case Gdk.Key.Page_Up:
                    case Gdk.Key.KP_Page_Up:
                        if (!manualFucusChange) {
                            return KeyboardScroll (args.EventKey.State,
                                (int) (-vadjustment.PageIncrement / RowHeight));
                        }
                        break;

                    case Gdk.Key.Page_Down:
                    case Gdk.Key.KP_Page_Down:
                        if (!manualFucusChange) {
                            return KeyboardScroll (args.EventKey.State,
                                (int) (vadjustment.PageIncrement / RowHeight));
                        }
                        break;

                    case Gdk.Key.Home:
                    case Gdk.Key.KP_Home:
                        if (!manualFucusChange) {
                            return KeyboardScroll (args.EventKey.State, -10000000);
                        }
                        break;

                    case Gdk.Key.End:
                    case Gdk.Key.KP_End:
                        if (!manualFucusChange) {
                            return KeyboardScroll (args.EventKey.State, 10000000);
                        }
                        break;

                    case Gdk.Key.Return:
                    case Gdk.Key.KP_Enter:
                        ActivateRow ();
                        break;

                    default:
                        char keyChar = (char) Keyval.ToUnicode ((uint) args.GdkKey);

                        if (char.ToLower (keyChar) == 'a' && (args.EventKey.State & KeyShortcuts.ControlModifier) != 0) {
                            if (allowMultipleSelect) {
                                if ((args.EventKey.State & ModifierType.ShiftMask) != 0)
                                    selection.Clear ();
                                else
                                    selection.SelectAll ();
                                QueueDraw ();
                            }
                            return ret;
                        }

                        if (!editedCell.IsValid) {
                            if (keyChar != '\0') {
                                if (char.ToLower (keyChar) == 'v' && (args.EventKey.State & KeyShortcuts.ControlModifier) != 0) {
                                    Clipboard clip = Clipboard.Get (Atom.Intern ("CLIPBOARD", false));
                                    SetAutoFilter (autoFilterValue + clip.WaitForText (), true);
                                } else
                                    SetAutoFilter (autoFilterValue + keyChar, true);
                            } else if (args.GdkKey == Gdk.Key.BackSpace) {
                                SetAutoFilter (autoFilterValue.Substring (0, Math.Max (0, autoFilterValue.Length - 1)), true);
                            }
                        }

                        break;
                }

                return ret;
            } finally {
                eventLock = false;
            }
        }

        #region OnButtonPressEvent

        protected override bool OnButtonPressEvent (EventButton evnt)
        {
            try {
                HasFocus = true;
                if (header_visible && header_interaction_alloc.Contains ((int) evnt.X, (int) evnt.Y)) {
                    return OnHeaderButtonPressEvent (evnt);
                }
                if (list_interaction_alloc.Contains ((int) evnt.X, (int) evnt.Y) && model != null) {
                    return OnListButtonPressEvent (evnt);
                }
            } catch (Exception ex) {
                ExceptionManager.RaiseUnhandledException (ex, false);
            }
            return true;
        }

        private bool OnHeaderButtonPressEvent (EventButton evnt)
        {
            int x = (int) evnt.X - header_interaction_alloc.X + (int) hadjustment.Value;
            int y = (int) evnt.Y - header_interaction_alloc.Y;

            if (evnt.Button == 3 && ColumnController.EnableColumnMenu) {
                Column menu_column = GetColumnAtX (x).Column;
                if (menu_column != null) {
                    OnColumnRightClicked (menu_column, x + Allocation.X, y + Allocation.Y);
                }
                return true;
            }
            if (evnt.Button != 1) {
                return true;
            }

            Gtk.Drag.SourceUnset (this);

            Column column = GetColumnForResizeHandle (x);
            if (column != null) {
                resizing_column_index = GetCachedColumnForColumn (column).Index;
            } else {
                column = GetColumnAtX (x).Column;
                if (column != null) {
                    CachedColumn column_c = GetCachedColumnForColumn (column);
                    pressed_column_index = column_c.Index;
                    pressed_column_x_start = x;
                    pressed_column_x_offset = pressed_column_x_start - column_c.X1;
                    pressed_column_x_start_hadjustment = (int) hadjustment.Value;
                }
            }

            return true;
        }

        private bool OnListButtonPressEvent (EventButton evnt)
        {
            if (eventLock)
                return true;

            try {
                eventLock = true;

                int x = (int) evnt.X - list_interaction_alloc.X + (int) hadjustment.Value;
                int y = (int) evnt.Y - list_interaction_alloc.Y;

                GrabFocus ();

                int row_index = GetRowAtY (y);
                int col_index = GetColumnAtX (x).Index;

                if (row_index < 0 || row_index >= Model.Count) {
                    return true;
                }

                CellButtonPressEventArgs args = new CellButtonPressEventArgs (col_index, row_index, evnt);
                if (col_index >= 0 && row_index >= 0) {
                    column_cache [col_index].Column.ListCell.OnButtonPressEvent (args);
                }

                if (CellButtonPressEvent != null)
                    CellButtonPressEvent (this, args);

                if ((evnt.Button != 1 || evnt.Type != EventType.TwoButtonPress) && Selection != null &&
                    ((evnt.State & KeyShortcuts.ControlModifier) == 0 || !AllowMultipleSelect) &&
                    (((evnt.State & ModifierType.ShiftMask) == 0 || !AllowMultipleSelect) && AllowSelect) &&
                    evnt.Button != 3) {
                    pressed_row_index = row_index;
                    last_row_index = row_index;
                }

                if (!ManualFucusChange)
                    if (cellsFucusable)
                        FocusCell (col_index, row_index);
                    else
                        FocusRow (row_index);

                if (evnt.Button == 1 && evnt.Type == EventType.TwoButtonPress) {
                    pressed_row_index = -1;
                    last_row_index = -1;
                    OnRowActivated ();
                }

                if (evnt.Button == 3)
                    OnPopupMenu ();

                return true;
            } finally {
                eventLock = false;
            }
        }

        #endregion

        #region OnButtonRelease

        protected override bool OnButtonReleaseEvent (EventButton evnt)
        {
            try {
                OnDragSourceSet ();
                StopDragScroll ();
                pressed_row_index = -1;
                last_row_index = -1;

                if (resizing_column_index >= 0) {
                    pressed_column_index = -1;
                    resizing_column_index = -1;
                    GdkWindow.Cursor = null;
                    return true;
                }

                if (pressed_column_index >= 0 && pressed_column_is_dragging) {
                    pressed_column_is_dragging = false;
                    pressed_column_index = -1;
                    GdkWindow.Cursor = null;
                    QueueDraw ();
                    return true;
                }

                if (header_visible && header_interaction_alloc.Contains ((int) evnt.X, (int) evnt.Y)) {
                    bool result = OnHeaderButtonRelease (evnt);
                    dragSelect = false;
                    return result;
                }
                if (list_interaction_alloc.Contains ((int) evnt.X, (int) evnt.Y) && model != null) {
                    bool result = OnListButtonRelease (evnt);
                    dragSelect = false;
                    return result;
                }
                dragSelect = false;
            } catch (Exception ex) {
                ExceptionManager.RaiseUnhandledException (ex, false);
            }
            return true;
        }

        private bool OnHeaderButtonRelease (EventButton evnt)
        {
            int x = (int) evnt.X - header_interaction_alloc.X + (int) hadjustment.Value;

            if (pressed_column_index < 0 || pressed_column_index >= column_cache.Length)
                return false;

            Column column = GetColumnAtX (x).Column;
            if (sortModel != null && column != null && column.IsSortable) {
                if (sortModel.SortColumn == column) {
                    switch (column.SortDirection) {
                        case SortDirection.Ascending:
                            column.SortDirection = SortDirection.Descending;
                            break;
                        case SortDirection.Descending:
                            column.SortDirection = SortDirection.None;
                            column = null;
                            break;
                        default:
                            column.SortDirection = SortDirection.Ascending;
                            break;
                    }
                } else {
                    column.SortDirection = SortDirection.Ascending;
                    foreach (CachedColumn cachedColumn in column_cache) {
                        if (ReferenceEquals (cachedColumn.Column, column))
                            continue;
                        cachedColumn.Column.SortDirection = SortDirection.None;
                    }
                    if (sortModel.SortColumn != null)
                        sortModel.SortColumn.SortDirection = SortDirection.None;
                }

                CancelCellEdit ();
                sortModel.Sort (column);
                AutoCalculateColumnSizes ();
                UpdateColumnCache ();
                InvalidateHeader ();
            }

            pressed_column_index = -1;
            return true;
        }

        private bool OnListButtonRelease (EventButton evnt)
        {
            if (eventLock)
                return true;

            try {
                eventLock = true;

                int y = (int) evnt.Y - list_interaction_alloc.Y;

                //GrabFocus ();

                int row_index = GetRowAtY (y);

                if (row_index < 0 || row_index >= Model.Count)
                    return true;

                object item = model [row_index];
                if (item == null) {
                    return true;
                }

                Select (evnt.State, evnt.Button, row_index, true);

                InvalidateList ();
                return true;
            } finally {
                eventLock = false;
            }
        }

        private void Select (ModifierType modifier, uint button, int rowIndex, bool endDragSelection)
        {
            if (Selection != null) {
                if ((modifier & KeyShortcuts.ControlModifier) != 0 && AllowMultipleSelect) {
                    if (AllowSelect) {
                        if (button == 3) {
                            if (!Selection.Contains (rowIndex)) {
                                Selection.Select (rowIndex);
                            }
                        } else {
                            Selection.ToggleSelect (rowIndex);
                        }
                    }
                } else if ((modifier & ModifierType.ShiftMask) != 0 && AllowMultipleSelect) {
                    if (AllowSelect)
                        Selection.SelectFromFirst (rowIndex, true);
                } else {
                    if (AllowSelect) {
                        if (button == 3) {
                            if (!Selection.Contains (rowIndex) && dragSelect) {
                                Selection.QuietClear ();
                                Selection.Select (rowIndex);
                            }
                        } else {
                            if (!dragSelect) {
                                Selection.QuietClear ();
                                Selection.Select (rowIndex);
                            }
                            if (endDragSelection) {
                                pressed_row_index = -1;
                                last_row_index = -1;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        protected override bool OnMotionNotifyEvent (EventMotion evnt)
        {
            try {
                DragSelect (evnt.Y);

                int x = (int) evnt.X - header_interaction_alloc.X + (int) hadjustment.Value;

                if (pressed_column_index >= 0 && !pressed_column_is_dragging &&
                    Gtk.Drag.CheckThreshold (this, pressed_column_x_start, 0, x, 0) &&
                    Reorderable) {
                    pressed_column_is_dragging = true;
                    InvalidateHeader ();
                    InvalidateFooter ();
                    InvalidateList ();
                }

                pressed_column_x = x;

                if (OnMotionNotifyEvent (x)) {
                    return true;
                }

                GdkWindow.Cursor = (resizing_column_index >= 0 || GetColumnForResizeHandle (x) != null) &&
                    header_interaction_alloc.Contains ((int) evnt.X, (int) evnt.Y)
                    ? resize_x_cursor
                    : null;

                if (resizing_column_index >= 0) {
                    ResizeColumn (x);
                }
            } catch (Exception ex) {
                ExceptionManager.RaiseUnhandledException (ex, false);
            }

            return true;
        }

        private void DragSelect (double eventY)
        {
            if ((Selection.Count <= 1 || !RowsDraggable || dragSelect) &&
                pressed_row_index >= 0 && allowMultipleSelect && allowScrollSelect) {
                int y = (int) eventY - list_interaction_alloc.Y;
                int rowIndex = GetRowAtY (y);
                if (rowIndex >= 0 && rowIndex < Model.Count) {
                    if (rowIndex != last_row_index) {
                        if (!dragSelect)
                            selection.QuietClear ();
                        dragSelect = true;
                        selection.QuietUnselectRange (pressed_row_index, last_row_index);
                        selection.SelectRange (pressed_row_index, rowIndex);
                        InvalidateList ();
                    }
                    last_row_index = rowIndex;
                }
            }
        }

        private bool OnMotionNotifyEvent (int x)
        {
            if (!pressed_column_is_dragging) {
                return false;
            }

            OnDragScroll (OnDragHScrollTimeout, header_interaction_alloc.Width * 0.1, header_interaction_alloc.Width, x);

            GdkWindow.Cursor = drag_cursor;

            Column swap_column = GetColumnAtX (x).Column;

            if (swap_column != null) {
                CachedColumn swap_column_c = GetCachedColumnForColumn (swap_column);
                bool reorder = false;

                if (swap_column_c.Index < pressed_column_index) {
                    // Moving from right to left
                    reorder = pressed_column_x_drag <= swap_column_c.X1 + swap_column_c.Width / 2;
                } else if (swap_column_c.Index > pressed_column_index) {
                    // Moving from left to right
                    reorder = pressed_column_x_drag + column_cache [pressed_column_index].Width >=
                        swap_column_c.X1 + swap_column_c.Width / 2;
                }

                if (reorder) {
                    int actual_pressed_index = column_cache [pressed_column_index].Column.Index;
                    int actual_swap_index = swap_column_c.Column.Index;
                    ColumnController.Reorder (actual_pressed_index, actual_swap_index);
                    pressed_column_index = swap_column_c.Index;
                    UpdateColumnCache ();
                }
            }

            pressed_column_x_drag = x - pressed_column_x_offset - (pressed_column_x_start_hadjustment - (int) hadjustment.Value);

            QueueDraw ();
            return true;
        }

        private bool OnDragHScrollTimeout ()
        {
            ScrollTo (hadjustment, hadjustment.Value + (drag_scroll_velocity * drag_scroll_velocity_max));
            OnMotionNotifyEvent (pressed_column_x);
            return true;
        }

        protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
        {
            GdkWindow.Cursor = null;
            return base.OnLeaveNotifyEvent (evnt);
        }

        public void ActivateRow ()
        {
            if (Selection == null || Selection.FocusedCell.Row == -1)
                return;

            if (autoFilterDebouncing)
                autoFilterActivateRow = true;
            else
                OnRowActivated ();
        }

        protected virtual void OnRowActivated ()
        {
            if (Selection.FocusedCell.Row >= 0) {
                RowActivatedHandler handler = RowActivated;
                if (handler != null) {
                    handler (this, new RowActivatedArgs (Selection.FocusedCell.Row));
                }
            }
        }

        protected int GetRowAtY (int y)
        {
            if (y < 0) {
                return -1;
            }

            return ((int) (vadjustment.Value + y)) / RowHeight;
        }

        private CachedColumn GetColumnAtX (int x)
        {
            if (column_cache == null)
                return CachedColumn.Zero;

            foreach (CachedColumn column in column_cache) {
                if (!column.Column.Visible)
                    continue;

                if (column.X1 <= x && x <= column.X2)
                    return column;
            }

            return CachedColumn.Zero;
        }

        protected double GetYAtRow (int row)
        {
            double y = (double) RowHeight * row;
            return y;
        }

        protected double GetXAtColumn (int column)
        {
            if (column_cache.Length > column)
                return column_cache [column].X1;

            return 0;
        }

        public void FocusRow (int index)
        {
            FocusCell (-1, index, true);
        }

        public void FocusRow (int index, bool ensureVisible)
        {
            FocusCell (-1, index, ensureVisible);
        }

        public void FocusCell (int column, int row)
        {
            FocusCell (column, row, true);
        }

        public void FocusCell (int column, int row, bool ensureVisible)
        {
            if (column_cache == null)
                return;

            if (row < 0 || row >= Model.Count)
                return;

            if (column < -1 || column >= column_cache.Length)
                return;

            if (ensureVisible) {
                AllignToV (row);
                if (column >= 0)
                    AllignToH (column);
            }

            skipLateFocusChange = true;
            if (Selection.FocusedCell.Column == column &&
                Selection.FocusedCell.Row == row)
                return;

            MoveFocus (new CellPosition (column, row));

            CellPosition fCell = Selection.FocusedCell;
            if (fCell.IsValid)
                column_cache [fCell.Column].Column.ListCell.OnCellFocusIn (new CellEventArgs (fCell.Column, fCell.Row));
            else
                OnCellFocusIn (new CellEventArgs (fCell.Column, fCell.Row));

            QueueDraw ();
        }

        public void DefocusCell ()
        {
            MoveFocus (CellPosition.Empty);

            QueueDraw ();
        }

        private void MoveFocus (CellPosition newFocusedCell)
        {
            CellPosition fCell = Selection.FocusedCell;
            if (fCell.IsValid && fCell.Column < column_cache.Length)
                column_cache [fCell.Column].Column.ListCell.OnCellFocusOut (new CellEventArgs (fCell.Column, fCell.Row));
            else
                OnCellFocusOut (new CellEventArgs (fCell.Column, fCell.Row));

            if (editedCell != newFocusedCell)
                CancelCellEdit ();

            selection.FocusedCell = newFocusedCell;
        }

        #region Adjustments & Scrolling

        private void UpdateAdjustments (Adjustment hadj, Adjustment vadj)
        {
            bool vchange = false;
            bool hchange = false;
            if (hadj != null) {
                hadjustment = hadj;
            }

            if (vadj != null) {
                vadjustment = vadj;
            }

            if (hadjustment != null) {
                if (hadjustment.Upper != header_width) {
                    hadjustment.Upper = header_width;
                    hchange = true;
                }
                if (hadjustment.StepIncrement != 10.0) {
                    hadjustment.StepIncrement = 10.0;
                    hchange = true;
                }
                if (hadjustment.Value + hadjustment.PageSize > hadjustment.Upper) {
                    hadjustment.Value = hadjustment.Upper - hadjustment.PageSize;
                    hchange = true;
                }
            }

            if (vadjustment != null && model != null) {
                if (vadjustment.Upper != RowHeight * model.Count) {
                    vadjustment.Upper = RowHeight * model.Count;
                    vchange = true;
                }
                if (vadjustment.StepIncrement != RowHeight) {
                    vadjustment.StepIncrement = RowHeight;
                    vchange = true;
                }
                if (vadjustment.Value + vadjustment.PageSize > vadjustment.Upper) {
                    vadjustment.Value = vadjustment.Upper - vadjustment.PageSize;
                    vchange = true;
                }
            }

            if (hadjustment != null && hchange) {
                hadjustment.Change ();
            }

            if (vadjustment != null && vchange) {
                vadjustment.Change ();
            }
        }

        private bool autoAligning;
        private void OnHadjustmentChanged (object o, EventArgs args)
        {
            if (autoAligning)
                return;

            CancelCellEdit ();
            InvalidateHeader ();
            InvalidateFooter ();
            InvalidateList ();
        }

        private void OnVadjustmentChanged (object o, EventArgs args)
        {
            if (autoAligning)
                return;

            CancelCellEdit ();
            InvalidateList ();
        }

        public void AllignToV (int row_index)
        {
            double y_at_row = GetYAtRow (row_index);

            autoAligning = true;
            try {
                if (y_at_row < vadjustment.Value) {
                    ScrollToV (y_at_row);
                } else if ((y_at_row + RowHeight) > (vadjustment.Value + vadjustment.PageSize)) {
                    ScrollToV (y_at_row + RowHeight - (vadjustment.PageSize));
                }
            } finally {
                autoAligning = false;
            }
        }

        public void AllignToH (int col_index)
        {
            if (column_cache == null || col_index >= column_cache.Length) {
                return;
            }

            CachedColumn column = column_cache [col_index];

            autoAligning = true;
            try {
                if (column.X1 < hadjustment.Value) {
                    ScrollToH ((double) column.X1);
                } else if ((column.X1 + column.Width) > (hadjustment.Value + hadjustment.PageSize)) {
                    ScrollToH (column.X1 + column.Width - hadjustment.PageSize);
                }
            } finally {
                autoAligning = false;
            }
        }

        public void ScrollToV (int index)
        {
            ScrollToV (GetYAtRow (index));
        }

        private void ScrollToV (double val)
        {
            if (vadjustment == null)
                return;

            if (model != null)
                vadjustment.Upper = RowHeight * model.Count;
            ScrollTo (vadjustment, val);
        }

        public void ScrollToH (int index)
        {
            ScrollToH (GetXAtColumn (index));
        }

        private void ScrollToH (double val)
        {
            ScrollTo (hadjustment, val);
        }

        private static void ScrollTo (Adjustment adjustment, double val)
        {
            if (adjustment == null)
                return;

            double newValue = Math.Max (0.0, Math.Min (val, adjustment.Upper - adjustment.PageSize));
            adjustment.Value = newValue;
        }

        public void CenterOn (int index)
        {
            ScrollToV (index - RowsInView / 2 + 1);
        }

        protected override void OnSetScrollAdjustments (Adjustment hadj, Adjustment vadj)
        {
            if (hadj == null || vadj == null) {
                return;
            }

            hadj.ValueChanged += OnHadjustmentChanged;
            vadj.ValueChanged += OnVadjustmentChanged;

            try {
                UpdateAdjustments (hadj, vadj);
            } catch (Exception ex) {
                ExceptionManager.RaiseUnhandledException (ex, false);
            }
        }

        #endregion

    }
}
