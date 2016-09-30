//
// Column.cs
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
using System.Collections;
using Warehouse.Data;
using Warehouse.Data.Model;

#if DEBUG_LISTVIEW
using System.Diagnostics;
#endif

namespace Warehouse.Component.ListView
{
    public class Column : ColumnBase
    {
        #region Fields

        private Cell headerCell;
        private Cell footerCell;
        private Cell listCell;
        private ListView parentListView;

        private double width;
        private bool visible = true;
        private bool isSortable = true;
        private string sortKey;
        private int maxWidth = 1000;
        private ColumnController controller;
        private int cachedIndex = -1;

        #endregion

        public event EventHandler VisibilityChanged;
        public event EventHandler WidthChanged;
        public event CellKeyPressEventHandler KeyPressEvent;
        public event CellButtonPressEventHandler ButtonPressEvent;
        public event CellEditBeginHandler CellEditBegin;
        public event CellEditCancelHandler CellEditCancel;
        public event CellEditEndHandler CellEditEnd;
        public event CellFocusInHandler CellFocusIn;
        public event CellFocusOutHandler CellFocusOut;

        #region Properties

        public string HeaderText { get; set; }

        public object FooterValue { get; set; }

        public string FooterText { get; set; }

        public Cell HeaderCell
        {
            get { return headerCell; }
            set
            {
                headerCell = value;
                if (headerCell != null)
                    headerCell.ParentColumn = this;
            }
        }

        public Cell FooterCell
        {
            get { return footerCell; }
            set
            {
                footerCell = value;
                if (footerCell != null)
                    footerCell.ParentColumn = this;
            }
        }

        public Cell ListCell
        {
            get { return listCell; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException ("value");

                listCell = value;
                listCell.ParentColumn = this;
            }
        }

        public ListView ParentListView
        {
            get { return parentListView; }
            set { parentListView = value; }
        }

        public int MinWidth { get; set; }

        public int MaxWidth
        {
            get { return maxWidth; }
            set { maxWidth = value; }
        }

        internal double MinRelativeWidth { get; set; }

        public double Width
        {
            get { return width; }
            set
            {
                double old = width;
                //Console.WriteLine ("Changing width of {0} from {1} to {2}", Title, Width, value);
                width = value;

                if (value != old) {
                    OnWidthChanged ();
                }
            }
        }

        public bool Visible
        {
            get { return visible; }
            set
            {
                bool old = visible;
                visible = value;

                if (value != old) {
                    OnVisibilityChanged ();
                }
            }
        }

        public override bool IsSortable
        {
            get { return isSortable; }
            set { isSortable = value; }
        }

        public override string SortKey
        {
            get { return sortKey; }
            set { sortKey = value; }
        }

        public override string Binding
        {
            get { return listCell.PropertyName; }
            set { listCell.PropertyName = value; }
        }

        public override SortDirection SortDirection { get; set; }

        public override IComparer Comparer
        {
            get { return listCell.Comparer; }
        }

        public ColumnController Controller
        {
            get { return controller; }
            set
            {
                controller = value;
                cachedIndex = -1;
            }
        }

        public int Index
        {
            get
            {
                if (cachedIndex >= 0)
                    return cachedIndex;

                if (Controller == null)
                    return -1;

                cachedIndex = Controller.IndexOf (this);
                return cachedIndex;
            }
            set { cachedIndex = value; }
        }

        #endregion

        public Column (string headerText, Cell cell, double width)
            : this (headerText, string.Empty, cell, width, null)
        {
        }

        public Column (string headerText, Cell cell, double width, string sortKey)
            : this (headerText, string.Empty, cell, width, sortKey)
        {
        }

        public Column (string headerText, string propertyName, double width)
            : this (headerText, string.Empty, new CellText (propertyName), width, null)
        {
        }

        public Column (string headerText, string propertyName, double width, string sortKey)
            : this (headerText, string.Empty, new CellText (propertyName), width, sortKey)
        {
        }

        public Column (string headerText, string footerText, Cell cell, double width, string sortKey)
        {
            HeaderText = headerText;
            FooterText = footerText;
            HeaderCell = new CellTextHeader ();
            FooterCell = new CellTextFooter ();
            ListCell = cell;
            this.width = width;
            this.sortKey = sortKey;
            isSortable = !string.IsNullOrEmpty (sortKey);
        }

        #region Event handlers

        protected virtual void OnVisibilityChanged ()
        {
            if (VisibilityChanged != null) {
                VisibilityChanged (this, new EventArgs ());
            }
        }

        protected virtual void OnWidthChanged ()
        {
            EventHandler handler = WidthChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        internal virtual void OnKeyPressEvent (CellKeyPressEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("Column received {0} at {1}", "OnKeyPressEvent", args.Cell));
#endif
            if (KeyPressEvent != null)
                KeyPressEvent (this, args);

            if (parentListView != null)
                parentListView.OnCellKeyPress (args);
        }

        internal virtual void OnButtonPressEvent (CellButtonPressEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("Column received {0} at {1}", "OnButtonPressEvent", args.Cell));
#endif
            if (ButtonPressEvent != null)
                ButtonPressEvent (this, args);

            if (parentListView != null)
                parentListView.OnCellButtonPress (args);
        }

        internal virtual void OnCellEditBegin (CellEditBeginEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("Column received {0} at {1}", "OnCellEditBegin", args.Cell));
#endif
            if (CellEditBegin != null)
                CellEditBegin (this, args);

            if (parentListView != null)
                parentListView.OnCellEditBegin (args);
        }

        internal virtual void OnCellEditCancel (CellEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("Column received {0} at {1}", "OnCellEditCancel", args.Cell));
#endif
            if (CellEditCancel != null)
                CellEditCancel (this, args);

            if (parentListView != null)
                parentListView.OnCellEditCancel (args);
        }

        internal virtual void OnCellEditEnd (CellEditEndEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("Column  received {0} at {1}", "OnCellEditEnd", args.Cell));
#endif
            if (CellEditEnd != null)
                CellEditEnd (this, args);

            if (parentListView != null)
                parentListView.OnCellEditEnd (args);
        }

        internal virtual void OnCellFocusIn (CellEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("Column  received {0} at {1}", "OnCellFocusIn", args.Cell));
#endif
            if (CellFocusIn != null)
                CellFocusIn (this, args);

            if (parentListView != null)
                parentListView.OnCellFocusIn (args);
        }

        internal virtual void OnCellFocusOut (CellEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("Column  received {0} at {1}", "OnCellFocusOut", args.Cell));
#endif
            if (CellFocusOut != null)
                CellFocusOut (this, args);

            if (parentListView != null)
                parentListView.OnCellFocusOut (args);
        }

        #endregion
    }
}
