//
// Cell.cs
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
using Gtk;
using Warehouse.Data.Model;

namespace Warehouse.Component.ListView
{
    public delegate void CellKeyPressEventHandler (object sender, CellKeyPressEventArgs args);

    public delegate void CellButtonPressEventHandler (object sender, CellButtonPressEventArgs args);

    public delegate void CellEditBeginHandler (object sender, CellEditBeginEventArgs args);

    public delegate void CellEditCancelHandler (object sender, CellEventArgs args);

    public delegate void CellEditEndHandler (object sender, CellEditEndEventArgs args);

    public delegate void CellFocusInHandler (object sender, CellEventArgs args);

    public delegate void CellFocusOutHandler (object sender, CellEventArgs args);

    public delegate void CellStyleProvider (object sender, CellStyleQueryEventArgs args);

    public abstract class Cell
    {
        #region Fields

        private string propertyName;
        protected Column parentColumn;
        private bool isEditable;
        protected object boundObject;
        protected ComparerBase comparer;

        #endregion

        #region Events

        public event CellKeyPressEventHandler KeyPressEvent;
        public event CellButtonPressEventHandler ButtonPressEvent;
        public event CellEditBeginHandler CellEditBegin;
        public event CellEditCancelHandler CellEditCancel;
        public event CellEditEndHandler CellEditEnd;
        public event CellFocusInHandler CellFocusIn;
        public event CellFocusOutHandler CellFocusOut;
        public event EventHandler<TextRequestEventArgs> ObjectToStringRequest;
        public event EventHandler<TextRequestEventArgs> ObjectToEditStringRequest;

        #endregion

        #region Properties

        public string PropertyName
        {
            get { return propertyName; }
            set { propertyName = value; }
        }

        public Column ParentColumn
        {
            get { return parentColumn; }
            set { parentColumn = value; }
        }

        public bool IsEditable
        {
            get { return isEditable; }
            set { isEditable = value; }
        }

        public virtual object EditValue
        {
            get { return boundObject; }
            set
            {
                if (!isEditable)
                    return;

                boundObject = value;
            }
        }

        public virtual ComparerBase Comparer
        {
            get
            {
                comparer.SortDirection = parentColumn.SortDirection;
                return comparer;
            }
            set { comparer = value; }
        }

    	#endregion

    	protected Cell (string propertyName)
        {
            this.propertyName = propertyName;
        }

        public virtual void BindListItem (int rowIndex)
        {
            ListView parentListView = parentColumn.ParentListView;
            IListModel model = parentListView.Model;

            if (string.IsNullOrEmpty (propertyName)) {
                try {
                    boundObject = model [rowIndex];
                } catch (ArgumentOutOfRangeException) {
                    throw new CellNotValidException (rowIndex);
                }
            } else {
                try {
                    boundObject = model [rowIndex, propertyName];
                } catch (ArgumentOutOfRangeException) {
                    throw new CellNotValidException (rowIndex, propertyName);
                }
            }
        }

        public abstract void Render (CellContext context, StateType state, double cellWidth, double cellHeight, CellPosition position);

        #region String to object transformations

        public virtual string ObjectToString (object obj)
        {
            if (ObjectToStringRequest != null) {
                TextRequestEventArgs args = new TextRequestEventArgs (obj);
                ObjectToStringRequest (this, args);
                return args.Text;
            }
           
            return obj != null ? obj.ToString () : string.Empty;
        }

        protected virtual string ObjectToEditString (object obj)
        {
            if (ObjectToEditStringRequest != null) {
                TextRequestEventArgs args = new TextRequestEventArgs (obj);
                ObjectToEditStringRequest (this, args);
                return args.Text;
            }

            return obj != null ? obj.ToString () : string.Empty;
        }

        protected virtual object ParseObject (string text)
        {
            return text;
        }

        #endregion

        public virtual bool BeginCellEdit (CellEventArgs args)
        {
            if (!IsEditable)
                return false;

            if (parentColumn.ParentListView.DisableEdit)
                return false;

            return OnCellEditBegin (new CellEditBeginEventArgs (args.Cell.Column, args.Cell.Row));
        }

        public virtual void EndCellEdit (object newValue)
        {
            ListView parentListView = parentColumn.ParentListView;
            CellPosition pos = parentListView.EditedCell;
            CellEditEndEventArgs args = new CellEditEndEventArgs (pos.Column, pos.Row, newValue);
            IListModel model = parentListView.Model;

            OnCellEditEnd (args);

            if (string.IsNullOrEmpty (propertyName)) {
                try {
                    model [pos.Row] = args.NewValue;
                } catch (ArgumentOutOfRangeException) {
                    throw new CellNotValidException (pos.Row);
                }
            } else {
                try {
                    model [pos.Row, propertyName] = args.NewValue;
                } catch (ArgumentOutOfRangeException) {
                    throw new CellNotValidException (pos.Row, propertyName);
                }
            }
        }

        public virtual void EndCellEdit ()
        {
            CancelCellEdit ();
        }

        public virtual void CancelCellEdit ()
        {
            CellPosition pos = ParentColumn.ParentListView.EditedCell;
            OnCellEditCancel (new CellEventArgs (pos.Column, pos.Row));
        }

        #region Event handlers

        protected internal virtual void OnKeyPressEvent (CellKeyPressEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("Cell received {0} at {1}", "OnKeyPressEvent", args.Cell));
#endif
            if (KeyPressEvent != null)
                KeyPressEvent (this, args);

            if (parentColumn != null)
                parentColumn.OnKeyPressEvent (args);
        }

        protected internal virtual void OnButtonPressEvent (CellButtonPressEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("Cell received {0} at {1}", "OnButtonPressEvent", args.Cell));
#endif
            if (ButtonPressEvent != null)
                ButtonPressEvent (this, args);

            if (parentColumn != null)
                parentColumn.OnButtonPressEvent (args);
        }

        internal virtual bool OnCellEditBegin (CellEditBeginEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("Cell received {0} at {1}", "OnCellEditBegin", args.Cell));
#endif
            if (CellEditBegin != null) {
                CellEditBegin (this, args);
                if (args.Cancel)
                    return false;
            }

            if (parentColumn != null)
                parentColumn.OnCellEditBegin (args);

            return !args.Cancel;
        }

        internal virtual void OnCellEditCancel (CellEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("Cell received {0} at {1}", "OnCellEditCancel", args.Cell));
#endif
            if (CellEditCancel != null)
                CellEditCancel (this, args);


            if (parentColumn != null)
                parentColumn.OnCellEditCancel (args);
        }

        internal virtual void OnCellEditEnd (CellEditEndEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("Cell received {0} at {1}", "OnCellEditEnd", args.Cell));
#endif
            if (CellEditEnd != null)
                CellEditEnd (this, args);

            if (parentColumn != null)
                parentColumn.OnCellEditEnd (args);
        }

        internal virtual void OnCellFocusIn (CellEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("Cell received {0} at {1}", "OnCellFocusIn", args.Cell));
#endif
            if (CellFocusIn != null)
                CellFocusIn (this, args);

            if (parentColumn != null)
                parentColumn.OnCellFocusIn (args);
        }

        internal virtual void OnCellFocusOut (CellEventArgs args)
        {
#if DEBUG_LISTVIEW
            Debug.WriteLine (string.Format ("Cell received {0} at {1}", "OnCellFocusOut", args.Cell));
#endif
            if (CellFocusOut != null)
                CellFocusOut (this, args);

            if (parentColumn != null)
                parentColumn.OnCellFocusOut (args);
        }

        #endregion
    }
}
