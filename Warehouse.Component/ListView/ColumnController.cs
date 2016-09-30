//
// ColumnController.cs
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
using System.Collections.Generic;

namespace Warehouse.Component.ListView
{
    public class ColumnController : IEnumerable<Column>
    {
        private readonly List<Column> columns = new List<Column> ();

        public event EventHandler Updated;

        protected virtual void OnVisibilitiesChanged ()
        {
            OnUpdated ();
        }

        protected virtual void OnWidthsChanged ()
        {
        }

        protected void OnUpdated ()
        {
            EventHandler handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public void Clear ()
        {
            lock (this) {
                foreach (Column column in columns) {
                    column.VisibilityChanged -= OnColumnVisibilityChanged;
                    column.WidthChanged -= OnColumnWidthChanged;
                    column.Controller = null;
                }
                columns.Clear ();
            }

            OnUpdated ();
        }

        public void AddRange (params Column [] range)
        {
            lock (this) {
                foreach (Column column in range) {
                    column.VisibilityChanged += OnColumnVisibilityChanged;
                    column.WidthChanged += OnColumnWidthChanged;
                    column.Controller = this;
                }
                columns.AddRange (range);
            }

            OnUpdated ();
        }

        public void Add (Column column)
        {
            lock (this) {
                column.VisibilityChanged += OnColumnVisibilityChanged;
                column.WidthChanged += OnColumnWidthChanged;
                column.Controller = this;
                columns.Add (column);
            }

            OnUpdated ();
        }

        public void Insert (Column column, int index)
        {
            lock (this) {
                column.VisibilityChanged += OnColumnVisibilityChanged;
                column.WidthChanged += OnColumnWidthChanged;
                column.Controller = this;
                columns.Insert (index, column);
            }

            OnUpdated ();
        }

        public void Remove (Column column)
        {
            lock (this) {
                column.VisibilityChanged -= OnColumnVisibilityChanged;
                column.WidthChanged -= OnColumnWidthChanged;
                column.Controller = null;
                columns.Remove (column);
            }

            OnUpdated ();
        }

        public void Remove (int index)
        {
            lock (this) {
                Column column = columns [index];
                column.VisibilityChanged -= OnColumnVisibilityChanged;
                column.WidthChanged -= OnColumnWidthChanged;
                column.Controller = null;
                columns.RemoveAt (index);
            }

            OnUpdated ();
        }

        public void Reorder (int index, int newIndex)
        {
            lock (this) {
                Column column = columns [index];
                columns.RemoveAt (index);
                columns.Insert (newIndex, column);
                for (int i = Math.Min (index, newIndex); i <= Math.Max (index, newIndex); i++) {
                    columns [i].Index = -1;
                }
            }

            OnUpdated ();
        }
        IEnumerator IEnumerable.GetEnumerator ()
        {
            return columns.GetEnumerator ();
        }

        IEnumerator<Column> IEnumerable<Column>.GetEnumerator ()
        {
            return columns.GetEnumerator ();
        }

        public int IndexOf (Column column)
        {
            lock (this) {
                for (int i = 0; i < columns.Count; i++) {
                    if (ReferenceEquals (column, columns [i]))
                        return i;
                }
                return -1;
            }
        }

        public Column [] ToArray ()
        {
            return columns.ToArray ();
        }

        private void OnColumnVisibilityChanged (object o, EventArgs args)
        {
            OnVisibilitiesChanged ();
        }

        private void OnColumnWidthChanged (object o, EventArgs args)
        {
            OnWidthsChanged ();
        }

        public Column this [int index]
        {
            get { return columns [index]; }
        }

        public int Count
        {
            get { return columns.Count; }
        }

        public virtual bool EnableColumnMenu
        {
            get { return false; }
        }

        public int GetColumnOrdinal (string boundProperty)
        {
            int i = 0;
            foreach (Column column in columns) {
                if (column.ListCell.PropertyName == boundProperty)
                    return i;

                i++;
            }

            return -1;
        }
    }
}
