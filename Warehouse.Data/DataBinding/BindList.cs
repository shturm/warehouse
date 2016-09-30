//
// BindList.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   03/28/2007
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Warehouse.Data.DataBinding
{
    public class BindList<T> : IBindingList, IList<T>, IDisposable, ICloneable
    {
        private bool listChanging = false;
        private List<T> internalList;

        public BindList ()
        {
            internalList = new List<T> ();
        }

        public BindList (int capacity)
        {
            internalList = new List<T> (capacity);
        }

        public BindList (IEnumerable<T> c)
        {
            internalList = new List<T> (c);
        }

        public T this [int index]
        {
            get
            {
                return internalList [index];
            }
            set
            {
                if (listChanging)
                    return;

                listChanging = true;
                internalList [index] = value;
                OnListChanged (new CustomListChangedEventArgs (ListChangedType.ItemChanged, index, index));
                listChanging = false;
            }
        }

        public void Clear ()
        {
            if (listChanging)
                return;

            listChanging = true;
            int oldCount = Count;

            List<T> backup = new List<T> (internalList);
            internalList.Clear ();

            for (int i = oldCount - 1; i >= 0; i--)
                OnListChanged (new CustomListChangedEventArgs (ListChangedType.ItemDeleted, i, i, backup [i]));
            backup.Clear ();
            listChanging = false;
        }

        public void Reverse ()
        {
            Reverse (0, Count);
        }

        public void Reverse (int index, int count)
        {
            int i, j;

            if (listChanging)
                return;

            listChanging = true;
            internalList.Reverse (index, count);
            for (i = 0, j = count - 1; i < count; i++, j--)
                if (i != j)
                    OnListChanged (new CustomListChangedEventArgs (ListChangedType.ItemMoved, i, j));
            listChanging = false;
        }

        public void AddRange (IEnumerable<T> c)
        {
            if (listChanging)
                return;

            listChanging = true;
            int oldSize = internalList.Count;
            internalList.AddRange (c);
            for (int i = oldSize; i < internalList.Count; i++)
                OnListChanged (new CustomListChangedEventArgs (ListChangedType.ItemAdded, i, i));
            listChanging = false;
        }

        public void RemoveAt (int index)
        {
            if (listChanging)
                return;

            listChanging = true;
            T deletedObject = internalList [index];
            internalList.RemoveAt (index);
            OnListChanged (new CustomListChangedEventArgs (ListChangedType.ItemDeleted, index, index, deletedObject));
            listChanging = false;
        }

        public void RemoveRange (int index, int count)
        {
            if (listChanging)
                return;

            listChanging = true;
            int oldSize = Count;

            List<T> backup = new List<T> ();
            for (int i = index; i < index + count; i++)
                backup.Add (internalList [i]);
            internalList.RemoveRange (index, count);

            for (int i = Math.Min (index + count, Count) - 1; i >= index; i--)
                OnListChanged (new CustomListChangedEventArgs (ListChangedType.ItemDeleted, i, i, backup [i]));

            backup.Clear ();

            for (int i = index + count; i < oldSize; i++)
                OnListChanged (new CustomListChangedEventArgs (ListChangedType.ItemMoved, i, i - count));

            listChanging = false;
        }

        public void Insert (int index, T value)
        {
            if (listChanging)
                return;

            listChanging = true;
            internalList.Insert (index, value);
            for (int i = Count - 1; i > index; i--)
                OnListChanged (new CustomListChangedEventArgs (ListChangedType.ItemMoved, i, i - 1));

            OnListChanged (new CustomListChangedEventArgs (ListChangedType.ItemAdded, index, index));
            listChanging = false;
        }

        public void InsertRange (int index, IEnumerable<T> c)
        {
            if (listChanging)
                return;

            listChanging = true;
            int oldSize = Count;
            internalList.InsertRange (index, c);
            //for (int i = Count - 1; i > index + c.Count; i--)
            //    OnListChanged(new CustomListChangedEventArgs(ListChangedType.ItemMoved, i, i - c.Count));

            for (int i = oldSize; i < Count; i++)
                OnListChanged (new CustomListChangedEventArgs (ListChangedType.ItemAdded, i, i));
            listChanging = false;
        }

        public T [] ToArray ()
        {
            return internalList.ToArray ();
        }

        #region IBindingList Members

        private void OnListChanged (CustomListChangedEventArgs args)
        {
            if (ListChanged != null)
                ListChanged (this, args);
        }

        public event ListChangedEventHandler ListChanged;

        public void AddIndex (PropertyDescriptor property)
        {
        }

        public object AddNew ()
        {
            if (listChanging)
                return null;

            T item = Activator.CreateInstance<T> ();

            listChanging = true;
            int ret = Count;
            internalList.Add (item);
            OnListChanged (new CustomListChangedEventArgs (ListChangedType.ItemAdded, ret, ret));
            listChanging = false;

            return item;
        }

        public bool AllowEdit
        {
            get { return true; }
        }

        public bool AllowNew
        {
            get { return true; }
        }

        public bool AllowRemove
        {
            get { return true; }
        }

        public void ApplySort (PropertyDescriptor property, ListSortDirection direction)
        {
            throw new NotSupportedException ();
        }

        public int Find (PropertyDescriptor property, object key)
        {
            throw new NotSupportedException ();
        }

        public bool IsSorted
        {
            get { throw new NotSupportedException (); }
        }

        public void RemoveIndex (PropertyDescriptor property)
        {
        }

        public void RemoveSort ()
        {
            throw new NotSupportedException ();
        }

        public ListSortDirection SortDirection
        {
            get { throw new NotSupportedException (); }
        }

        public PropertyDescriptor SortProperty
        {
            get { throw new NotSupportedException (); }
        }

        public bool SupportsChangeNotification
        {
            get { return true; }
        }

        public bool SupportsSearching
        {
            get { return false; }
        }

        public bool SupportsSorting
        {
            get { return false; }
        }

        #endregion

        #region IList<T> Members

        public int IndexOf (T item)
        {
            return internalList.IndexOf (item);
        }

        #endregion

        #region ICollection<T> Members

        public bool Contains (T item)
        {
            return internalList.Contains (item);
        }

        public void CopyTo (T [] array, int arrayIndex)
        {
            internalList.CopyTo (array, arrayIndex);
        }

        public int Count
        {
            get { return internalList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove (T item)
        {
            if (listChanging)
                return false;

            listChanging = true;
            int index = internalList.IndexOf (item);
            if (index < 0) {
                listChanging = false;
                return false;
            }

            internalList.RemoveAt (index);
            OnListChanged (new CustomListChangedEventArgs (ListChangedType.ItemDeleted, index, index, item));
            listChanging = false;

            return true;
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator ()
        {
            return ((IEnumerable<T>) internalList).GetEnumerator ();
        }

        #endregion

        #region IList Members

        public void Add (T value)
        {
            if (listChanging)
                return;

            listChanging = true;
            int ret = Count;
            internalList.Add (value);
            OnListChanged (new CustomListChangedEventArgs (ListChangedType.ItemAdded, ret, ret));
            listChanging = false;
        }

        public bool Contains (object value)
        {
            return Contains ((T) value);
        }

        public int IndexOf (object value)
        {
            return IndexOf ((T) value);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        #endregion

        #region ICollection Members

        public void CopyTo (Array array, int index)
        {
            internalList.CopyTo ((T []) array, index);
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        public object SyncRoot
        {
            get { return internalList; }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return ((IEnumerable) internalList).GetEnumerator ();
        }

        #endregion

        #region IDisposable Members

        public void Dispose ()
        {
            ListChanged = null;
            internalList = null;
        }

        #endregion

        public T Find (Predicate<T> match)
        {
            foreach (T item in internalList) {
                if (match (item))
                    return item;
            }

            return default (T);
        }

        public object Clone ()
        {
            if (typeof (T).GetInterface ("ICloneable") != null) {
                List<T> ret = new List<T> ();
                foreach (T item in internalList) {
                    ret.Add ((T) ((ICloneable) item).Clone ());
                }
                return new BindList<T> (ret);
            }

            return new BindList<T> (internalList.ToArray ());
        }

        public List<T> FindAll (Predicate<T> match)
        {
            List<T> ret = new List<T> ();

            foreach (T item in internalList) {
                if (match (item))
                    ret.Add (item);
            }

            return ret;
        }

        int IList.Add (object value)
        {
            if (listChanging)
                return -1;
            Add ((T) value);
            return Count - 1;
        }

        void IList.Insert (int index, object value)
        {
            Insert (index, (T) value);
        }

        void IList.Remove (object value)
        {
            Remove ((T) value);
        }

        object IList.this [int index]
        {
            get { return this [index]; }
            set { this [index] = (T) value; }
        }
    }

    public class BindList : BindList<object>
    {
        public BindList ()
        {
        }

        public BindList (int capacity)
            : base (capacity)
        {
        }

        public BindList (IEnumerable c)
            : base ((IEnumerable<object>) c)
        {
        }
    }
}
