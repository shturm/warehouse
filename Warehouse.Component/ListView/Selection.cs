//
// Selection.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Created:
//   09/23/2008
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
using System.Text;

namespace Warehouse.Component.ListView
{
    public class Selection : IEnumerable<int>
    {
        private readonly SortedList<int, bool> internalList = new SortedList<int, bool> ();
        private int first_selected_index;
        private CellPosition focused_cell = CellPosition.Empty;
        private bool allSelected = false;
        private bool disabled = false;

        public event EventHandler Changed;

        public int this [int index]
        {
            get
            {
                int i = 0;
                foreach (KeyValuePair<int, bool> pair in internalList) {
                    if (i == index)
                        return pair.Key;
                }

                throw new IndexOutOfRangeException ();
            }
        }

        public CellPosition FocusedCell
        {
            get { return focused_cell; }
            set { focused_cell = value; }
        }

        protected virtual void OnChanged ()
        {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public void ToggleSelect (int index)
        {
            if (disabled)
                return;

            if (internalList.ContainsKey (index))
                internalList.Remove (index);
            else
                internalList.Add (index, true);

            OnChanged ();
        }

        public void Select (int index)
        {
            if (disabled)
                return;

            QuietSelect (index);
            OnChanged ();
        }

        public void QuietSelect (int index)
        {
            if (disabled)
                return;

            if (!internalList.ContainsKey (index))
                internalList.Add (index, true);

            if (Count == 1)
                first_selected_index = index;
        }

        public void Unselect (int index)
        {
            if (disabled)
                return;

            if (internalList.Remove (index))
                OnChanged ();
        }

        public void QuietUnselect (int index)
        {
            if (disabled)
                return;

            internalList.Remove (index);
        }

        public bool Contains (int index)
        {
            if (disabled)
                return false;

            if (allSelected)
                return true;
            else
                return internalList.ContainsKey (index);
        }

        public void SelectFromFirst (int end, bool clear)
        {
            if (disabled)
                return;

            bool contains = Contains (first_selected_index);

            if (clear)
                QuietClear ();

            if (contains)
                SelectRange (first_selected_index, end);
            else
                Select (end);
        }

        public void SelectRange (int a, int b)
        {
            if (disabled)
                return;

            int start = Math.Min (a, b);
            int end = Math.Max (a, b);

            int i;
            for (i = start; i <= end; i++) {
                QuietSelect (i);
            }

            if (Count == (end - start + 1))
                first_selected_index = a;

            OnChanged ();
        }

        public virtual void SelectAll ()
        {
            if (disabled)
                return;

            if (allSelected)
                return;

            allSelected = true;
            if (internalList.Count == 0)
                first_selected_index = 0;

            OnChanged ();
        }

        public bool QuietUnselectRange (int a, int b)
        {
            if (disabled)
                return false;

            bool ret = false;
            int start = Math.Min (a, b);
            int end = Math.Max (a, b);

            int i;
            for (i = start; i <= end; i++) {
                QuietUnselect (i);
                ret = true;
            }

            return ret;
        }

        public bool UnselectRange (int a, int b)
        {
            if (QuietUnselectRange (a, b)) {
                OnChanged ();
                return true;
            } else
                return false;
        }

        public void Clear ()
        {
            if (QuietClear ())
                OnChanged ();
        }

        public bool QuietClear ()
        {
            allSelected = false;
            if (internalList.Count <= 0) {
                return false;
            }

            internalList.Clear ();
            return true;
        }

        public int Count
        {
            get
            {
                if (disabled)
                    return 0;

                if (allSelected)
                    return int.MaxValue;
                else
                    return internalList.Count;
            }
        }

        public virtual bool AllSelected
        {
            get
            {
                return allSelected;
            }
        }

        public bool Disabled
        {
            get { return disabled; }
            set { disabled = value; }
        }

        public override string ToString ()
        {
            StringBuilder sb = new StringBuilder ();
            sb.Append ("<Selection Count=");

            if (allSelected) {
                sb.Append ("All");
            } else {
                sb.AppendFormat ("{0} :", Count);
                int i = 0;
                foreach (KeyValuePair<int, bool> pair in internalList) {
                    if (i != 0)
                        sb.Append (", ");

                    sb.Append (pair.Key);
                    i++;
                }
            }
            sb.Append (">");

            return sb.ToString ();
        }

        #region IEnumerable<int> Members

        IEnumerator<int> IEnumerable<int>.GetEnumerator ()
        {
            foreach (KeyValuePair<int, bool> pair in internalList) {
                yield return pair.Key;
            }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator ()
        {
            return ((IEnumerable<int>) this).GetEnumerator ();
        }

        #endregion
    }
}
