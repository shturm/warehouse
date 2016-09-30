//
// ListView.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Created:
//   10/18/2007
//
// Copyright (C) 2007 Novell, Inc.
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
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Timers;
using Gtk;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Component.ListView
{
    public partial class ListView : Gtk.Container
    {
        private const double autoFilterWait = 500;

        #region Fields

        private IListModel model;
        private ISortable sortModel;
        private IFilterable filterModel;
        private Timer filterTimer;

        private CellPosition editedCell = CellPosition.Empty;
        private bool sortColumnsHint = true;
        private bool cellsFucusable;
        private bool manualFucusChange;
        private bool manualEditControl = true;
        private bool allowMultipleSelect = true;
        private bool allowScrollSelect = true;
        private bool allowHorizontalScroll = true;
        private bool disableEdit;
        private bool eventLock;
        private PangoStyle defaultCellStyle = new PangoStyle ();
        private bool enableAutoFilter;
        private string autoFilterValue = string.Empty;
        private bool autoFilterDebouncing;
        private bool autoFilterActivateRow;

        #endregion

        public CellStyleProvider CellStyleProvider;
        public CellStyleProvider HeaderStyleProvider;
        public CellStyleProvider FooterStyleProvider;
        public event CellKeyPressEventHandler CellKeyPressEvent;
        public event CellButtonPressEventHandler CellButtonPressEvent;
        public event CellEditBeginHandler CellEditBegin;
        public event CellEditCancelHandler CellEditCancel;
        public event CellEditEndHandler CellEditEnd;
        public event CellFocusInHandler CellFocusIn;
        public event CellFocusOutHandler CellFocusOut;
        public event EventHandler AutoFilterChanged;
        public event EventHandler AutoFilterApplied;

        #region Properties

        public CellPosition FocusedCell
        {
            get { return selection.FocusedCell; }
        }

        public int FocusedRow
        {
            get { return selection.FocusedCell.Row; }
        }

        public CellPosition EditedCell
        {
            get { return editedCell; }
        }

        public object EditedCellValue
        {
            get
            {
                if (!editedCell.IsValid)
                    return null;

                return column_cache [editedCell.Column].Column.ListCell.EditValue;
            }
            set
            {
                if (!editedCell.IsValid)
                    return;

                column_cache [editedCell.Column].Column.ListCell.EditValue = value;
            }
        }

        public bool SortColumnsHint
        {
            get { return sortColumnsHint; }
            set { sortColumnsHint = value; }
        }

        public bool CellsFucusable
        {
            get { return cellsFucusable; }
            set { cellsFucusable = value; }
        }

        public virtual IListModel Model
        {
            get { return model; }
            set
            {
                #region Manage list changes

                if (model != null)
                    model.ListChanged -= OnModelListChanged;

                model = value;
                if (model != null)
                    model.ListChanged += OnModelListChanged;

                #endregion

                #region Manage sort

                ColumnBase oldSort = null;

                if (sortModel != null) {
                    sortModel.SortChanged -= ModelSortChanged;
                    oldSort = sortModel.SortColumn;
                }

                sortModel = model as ISortable;
                if (sortModel != null) {
                    sortModel.SortChanged += ModelSortChanged;
                    ColumnBase modelSortCol = sortModel.SortColumn;

                    if (oldSort != null) {
                        // If we have a previous sort set in the grid apply it to the new model
                        sortModel.Sort (oldSort);
                        ApplySortToColumns (oldSort);
                    } else if (modelSortCol != null && modelSortCol.SortDirection != SortDirection.None) {
                        // If we have a sort in the model apply it to the grid
                        ApplySortToColumns (modelSortCol);
                    }
                }

                #endregion

                #region Manage filter

                if (filterModel != null)
                    filterModel.FilterChanged -= ModelFilterChanged;

                filterModel = model as IFilterable;
                if (filterModel != null) {
                    filterModel.FilterChanged += ModelFilterChanged;
                    if (!string.IsNullOrEmpty (autoFilterValue))
                        SetAutoFilter (autoFilterValue, false);
                }

                #endregion

                RefreshViewForModel ();
            }
        }

        public bool ManualFucusChange
        {
            get { return manualFucusChange; }
            set { manualFucusChange = value; }
        }

        public bool ManualEditControl
        {
            get { return manualEditControl; }
            set { manualEditControl = value; }
        }

        public bool AllowSelect
        {
            get { return !selection.Disabled; }
            set { selection.Disabled = !value; }
        }

        public bool AllowMultipleSelect
        {
            get { return allowMultipleSelect; }
            set { allowMultipleSelect = value; }
        }

        public bool AllowScrollSelect
        {
            get { return allowScrollSelect; }
            set { allowScrollSelect = value; }
        }

        public bool AllowHorizontalScroll
        {
            get { return allowHorizontalScroll; }
            set { allowHorizontalScroll = value; }
        }

        public bool DisableEdit
        {
            get { return disableEdit; }
            set
            {
                if (value)
                    CancelCellEdit ();

                disableEdit = value;
            }
        }

        public PangoStyle DefaultCellStyle
        {
            get { return defaultCellStyle; }
            set { defaultCellStyle = value; }
        }

        public bool EnableAutoFilter
        {
            get { return enableAutoFilter; }
            set { enableAutoFilter = value; }
        }

        public string AutoFilterValue
        {
            get { return autoFilterValue; }
            set
            {
                SetAutoFilter (value, true);
            }
        }

        public bool AutoFilterDebouncing
        {
            get { return autoFilterDebouncing; }
        }

        #endregion

        public ListView ()
        {
            Initialize ();
        }

        public ListView (IntPtr raw)
            : base (raw)
        {
            Initialize ();
        }

        private void Initialize ()
        {
            column_layout = new Pango.Layout (PangoContext);
            CanFocus = true;

            filterTimer = new Timer { AutoReset = false, Interval = autoFilterWait };
            filterTimer.Elapsed += filterTimer_Elapsed;
        }

        #region Various Utilities

        public DataTable ToDataTable (bool stringValues)
        {
            DataTable ret = new DataTable ("ListViewTable");
            int i;
            List<string> lookupCells = new List<string> ();

            for (i = 0; i < column_cache.Length; i++) {
                Column column = column_cache [i].Column;
                if (!column.Visible)
                    continue;

                DataColumn dataColumn = new DataColumn (column.ListCell.PropertyName);
                if (stringValues)
                    dataColumn.DataType = typeof (string);
                else if (model.Count > 0) {
                    object value = model [0, column.ListCell.PropertyName];
                    dataColumn.DataType = value != null ? value.GetType () : typeof (string);
                    Type listCellType = column.ListCell.GetType ();
                    if (listCellType.IsGenericType && listCellType.GetGenericTypeDefinition () == typeof (CellTextLookup<>)) {
                        dataColumn.DataType = typeof (string);
                        lookupCells.Add (column.ListCell.PropertyName);
                    }
                }

                dataColumn.Caption = column.HeaderText;
                ret.Columns.Add (dataColumn);
            }

            for (i = 0; i < model.Count; i++) {
                object [] row = new object [ret.Columns.Count];
                int j = 0;

                foreach (CachedColumn cachedColumn in column_cache) {
                    Column column = cachedColumn.Column;
                    if (!column.Visible)
                        continue;

                    Cell cell = column.ListCell;
                    object value = model [i, cell.PropertyName];
                    row [j] = stringValues || lookupCells.Contains (cell.PropertyName) ? cell.ObjectToString (value) : value;

                    j++;
                }
                ret.Rows.Add (row);
            }

            if (footer_visible) {
                object [] row = new object [ret.Columns.Count];
                int j = 0;

                foreach (CachedColumn cachedColumn in column_cache) {
                    if (!cachedColumn.Column.Visible)
                        continue;

                    row [j] = cachedColumn.Column.FooterValue;
                    j++;
                }
                ret.Rows.Add (row);
            }

            return ret;
        }

        #endregion

        #region Model Interaction

        private void RefreshViewForModel ()
        {
            UpdateAdjustments (null, null);

            if (Parent is ScrolledWindow) {
                Parent.QueueDraw ();
            }
        }

        private void ApplySortToColumns (ColumnBase modelSortCol)
        {
            if (column_controller != null) {
                foreach (Column column in column_controller) {
                    if (column.SortKey == modelSortCol.SortKey) {
                        column.SortDirection = modelSortCol.SortDirection;
                        break;
                    }
                }
            }

            if (column_cache != null) {
                foreach (CachedColumn column in column_cache) {
                    if (column.Column.SortKey == modelSortCol.SortKey) {
                        column.Column.SortDirection = modelSortCol.SortDirection;
                        break;
                    }
                }
            }
        }

        private ListChangedEventArgs modelListChangedEventArgs;
        private readonly object listResetSync = new object ();
        private bool listResetRized;

        private void OnModelListChanged (object sender, ListChangedEventArgs e)
        {
            lock (listResetSync) {
                modelListChangedEventArgs = e;
                if (listResetRized)
                    return;

                skipLateFocusChange = false;
                GLib.Timeout.Add (0, ListReset_GlibHandler);
                listResetRized = true;
            }
        }

        private bool ListReset_GlibHandler ()
        {
            ModelListChanged (modelListChangedEventArgs);

            listResetRized = false;

            return false;
        }

        private void ModelListChanged (ListChangedEventArgs e)
        {
            int first_row = (int) vadjustment.Value / RowHeight;
            int last_row = Math.Min (model.Count, first_row + RowsInView);

            if ((first_row <= e.OldIndex && e.OldIndex <= last_row) ||
                (first_row <= e.NewIndex && e.NewIndex <= last_row) ||
                (e.NewIndex < 0 && e.OldIndex < 0) ||
                e.ListChangedType == ListChangedType.Reset) {
                RefreshViewForModel ();
                InvalidateList ();
            }

            if (e.ListChangedType == ListChangedType.ItemDeleted &&
                selection.FocusedCell.IsValid &&
                selection.FocusedCell.Row >= e.NewIndex &&
                !skipLateFocusChange) {
                DefocusCell ();
            }
        }

        private void ModelSortChanged (object sender, SortChangedEventArgs e)
        {
            InvalidateHeader ();
            InvalidateList ();
        }

        private void ModelFilterChanged (object sender, FilterChangedEventArgs e)
        {
            selection.Clear ();
            DefocusCell ();
            InvalidateHeader ();
            InvalidateList ();
            UpdateAdjustments (null, null);
        }

        public void SetAutoFilter (string value, bool debounce)
        {
            autoFilterValue = value;

            if (!enableAutoFilter)
                return;

            if (AutoFilterChanged != null)
                AutoFilterChanged (this, EventArgs.Empty);

            if (debounce) {
                filterTimer.Stop ();
                autoFilterDebouncing = true;
                filterTimer.Start ();
            } else {
                filterTimer_GlibHandler ();
            }
        }

        private void filterTimer_Elapsed (object sender, EventArgs e)
        {
            GLib.Timeout.Add (0, filterTimer_GlibHandler);
        }

        private bool filterTimer_GlibHandler ()
        {
            if (filterModel != null) {
                DateTime startTime = DateTime.Now;
                Debug.WriteLine ("ListView: Setting filter \"{0}\"", autoFilterValue);
                filterModel.SetFilter (autoFilterValue);
                Debug.WriteLine ("ListView: Finished setting filter in \"{0}\"", DateTime.Now - startTime);
            }

            autoFilterDebouncing = false;
            if (AutoFilterApplied != null)
                AutoFilterApplied (this, EventArgs.Empty);

            if (autoFilterActivateRow) {
                autoFilterActivateRow = false;
                OnRowActivated ();
            }

            return false;
        }

        #endregion

        #region Cell Utilities

        public bool BeginCellEdit (CellEventArgs args)
        {
            bool ret = false;
            if (args.Cell.Column < column_cache.Length && column_cache [args.Cell.Column].Column.Visible)
                ret = column_cache [args.Cell.Column].Column.ListCell.BeginCellEdit (args);

            if (ret)
                FocusCell (args.Cell.Column, args.Cell.Row);

            return ret;
        }

        public void EndCellEdit (object newValue)
        {
            if (editedCell.IsValid)
                column_cache [editedCell.Column].Column.ListCell.EndCellEdit (newValue);
        }

        public void EndCellEdit ()
        {
            if (editedCell.IsValid)
                column_cache [editedCell.Column].Column.ListCell.EndCellEdit ();
        }

        public void CancelCellEdit ()
        {
            if (editedCell.IsValid)
                column_cache [editedCell.Column].Column.ListCell.CancelCellEdit ();
        }

        public PangoStyle QueryCellStyle (StateType state, CellPosition cell)
        {
            if (cell.Column >= 0) {
                CellStyleQueryEventArgs ret = new CellStyleQueryEventArgs (state, cell);

                if (cell.Row >= 0 && CellStyleProvider != null) {
                    if (Model != null && Model.Count > 0)
                        CellStyleProvider (this, ret);

                    if (ret.Style != null)
                        return ret.Style;
                } else if (cell.Row == -1 && HeaderStyleProvider != null) {
                    HeaderStyleProvider (this, ret);

                    if (ret.Style != null)
                        return ret.Style;
                } else if (cell.Row == -2 && FooterStyleProvider != null) {
                    FooterStyleProvider (this, ret);

                    if (ret.Style != null)
                        return ret.Style;
                }
            }

            return defaultCellStyle;
        }

        #endregion

        #region Event handlers

        internal virtual void OnCellKeyPress (CellKeyPressEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("ListView received {0} at {1}", "OnCellKeyPress", args.Cell));
#endif

            OnCellKeyPressEvent (args);
        }

        internal virtual void OnCellButtonPress (CellButtonPressEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("ListView received {0} at {1}", "OnCellButtonPress", args.Cell));
#endif
            OnListButtonPressEvent (args.EventButton);
            //OnButtonPressEvent (args.EventButton);
        }

        internal virtual void OnCellEditBegin (CellEditBeginEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("ListView received {0} at {1}", "OnCellEditBegin", args.Cell));
#endif

            if (CellEditBegin != null)
                CellEditBegin (this, args);

            if (!args.Cancel) {
                CancelCellEdit ();
                editedCell = new CellPosition (args.Cell.Column, args.Cell.Row);
            }

            InvalidateList ();
        }

        internal virtual void OnCellEditCancel (CellEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("ListView received {0} at {1}", "OnCellEditCancel", args.Cell));
#endif

            HasFocus = true;
            GrabFocus ();

            if (CellEditCancel != null)
                CellEditCancel (this, args);

            editedCell = CellPosition.Empty;
            InvalidateList ();
        }

        internal virtual void OnCellEditEnd (CellEditEndEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("ListView received {0} at {1}", "OnCellEditEnd", args.Cell));
#endif
            HasFocus = true;
            GrabFocus ();

            if (CellEditEnd != null)
                CellEditEnd (this, args);

            editedCell = CellPosition.Empty;
            InvalidateList ();
        }

        internal virtual void OnCellFocusIn (CellEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("ListView received {0} at {1}", "OnCellFocusIn", args.Cell));
#endif
            if (CellFocusIn != null)
                CellFocusIn (this, args);
        }

        internal virtual void OnCellFocusOut (CellEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("ListView received {0} at {1}", "OnCellFocusOut", args.Cell));
#endif
            if (CellFocusOut != null)
                CellFocusOut (this, args);
        }

        #endregion
    }
}
