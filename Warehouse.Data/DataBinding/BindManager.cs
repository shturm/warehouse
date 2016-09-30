//
// BindManager.cs
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
using System.Reflection;
using Warehouse.Data.Model;
using System.Linq;

namespace Warehouse.Data.DataBinding
{
    public class BindManager : BindManager<object>
    {
        public BindManager (object source, bool watchItemChanges = true, Func<MemberInfo, string> columnNameResolver = null)
            : base (source, watchItemChanges, columnNameResolver)
        {
        }
    }

    public class BindManager<T> : IDisposable
    {
        private readonly bool watchItemChanges;
        private readonly Func<MemberInfo, string> columnNameResolver;
        //private object dataSource;
        //private string dataMember;
        private IList internalIList;
        private IListModel internalIListModel;
        private BindRowList<T> rowList;
        private BindColumnList<T> colList;

        private readonly ArrayList boundLists = new ArrayList ();
        private readonly ArrayList boundMemberLists = new ArrayList ();
        private readonly ArrayList boundMembers = new ArrayList ();
        private bool membersBound;
        private bool membersAreGenerated;
        private bool listChanging;

        private object InternalList
        {
            get { return (object) internalIList ?? internalIListModel; }
        }

        public BindColumnList<T> Columns
        {
            get { return colList; }
        }

        public BindRowList<T> Rows
        {
            get { return rowList; }
        }

        public T this [int row]
        {
            get
            {
                if (internalIList != null)
                    return (T) internalIList [row];

                if (internalIListModel == null)
                    throw new Exception ("The bound object can not be indexed.");

                return (T) internalIListModel [row];
            }
            set
            {
                if (internalIList != null) {
                    internalIList [row] = value;
                    return;
                }

                if (internalIListModel == null)
                    throw new Exception ("The bound object can not be indexed.");

                internalIListModel [row] = value;
            }
        }

        public int Count
        {
            get
            {
                if (internalIList != null)
                    return internalIList.Count;

                if (internalIListModel == null)
                    throw new Exception ("The bound object can not be indexed.");

                return internalIListModel.Count;
            }
        }

        public Func<MemberInfo, string> ColumnNameResolver
        {
            get { return columnNameResolver; }
        }

        public event ListChangedEventHandler ListChanged;

        public BindManager (object source, bool watchItemChanges = true, Func<MemberInfo, string> columnNameResolver = null)
        {
            if (source == null)
                throw new ArgumentNullException ("source");

            //this.dataSource = source;
            //this.dataMember = member;

            this.watchItemChanges = watchItemChanges;
            this.columnNameResolver = columnNameResolver;
            WireList (source);
            WireListMembers ();
        }

        #region Wireing logic

        private void WireList (object source)
        {
            object res = ResolveDataSource (source);
            WireFirstDimesion (res);
        }

        private void WireFirstDimesion (object obj)
        {
            IList list = obj as IList;
            if (list != null) {
                internalIList = list;
                Type listType = list.GetType ();
                Type elementType = listType.GetElementType ();
                rowList = new BindRowList<T> (this);

                if (elementType != null)
                    rowList.RowType = elementType;
                else if (listType.IsGenericType) {
                    Type [] args = listType.GetGenericArguments ();
                    if (args.Length > 0)
                        rowList.RowType = args [0];
                } else if (list.Count > 0)
                    rowList.RowType = list [0].GetType ();

                ITypedList tList = list as ITypedList;
                if (colList == null && tList != null)
                    colList = new BindColumnList<T> (this, tList);

                IBindingList ibList = obj as IBindingList;
                if (ibList == null)
                    return;

                ibList.ListChanged += List_ListChanged;
                boundLists.Add (ibList);
                return;
            }

            IListModel bindList = obj as IListModel;
            if (bindList != null) {
                internalIListModel = bindList;
                Type listType = bindList.GetType ();
                rowList = new BindRowList<T> (this);

                if (listType.IsGenericType) {
                    Type [] args = listType.GetGenericArguments ();
                    if (args.Length > 0)
                        rowList.RowType = args [0];
                } else if (bindList.Count > 0)
                    rowList.RowType = bindList [0].GetType ();

                bindList.ListChanged += List_ListChanged;
                boundLists.Add (bindList);
                return;
            }
        }

        private void WireListMembers ()
        {
            if (watchItemChanges) {
                if (internalIList != null) {
                    foreach (object item in internalIList)
                        WireSecondDimension (ResolveDataSource (item));
                } else if (internalIListModel != null) {
                    foreach (object item in internalIListModel)
                        WireSecondDimension (ResolveDataSource (item));
                } else
                    return;
            }

            if (colList == null && rowList.RowType != null)
                colList = new BindColumnList<T> (this, rowList.RowType);
        }

        private void WireSecondDimension (object obj)
        {
            if (internalIList == null && internalIListModel == null)
                return;

            if (!watchItemChanges)
                return;

            IList list = obj as IList;
            if (list != null) {
                if (colList == null)
                    colList = new BindColumnList<T> (this, list);

                IBindingList ibList = obj as IBindingList;
                if (ibList != null) {
                    ibList.ListChanged += ListMember_ListChanged;
                    boundMemberLists.Add (ibList);
                    membersBound = true;
                    return;
                }
            }
            if (colList == null)
                colList = new BindColumnList<T> (this, obj.GetType ());

            INotifyPropertyChanged inpChanged = obj as INotifyPropertyChanged;
            if (inpChanged == null)
                return;

            inpChanged.PropertyChanged += ListMember_PropertyChanged;
            boundMembers.Add (inpChanged);
            membersBound = true;
        }

        internal object ResolveDataSource (object source)
        {
            if (source == null)
                throw new Exception ("Data source is null");

            if (source is IList)
                return ResolveIList (source as IList);

            if (source is IListSource)
                return ResolveIListSource (source as IListSource);

            if (source is IListModel)
                return ResolveIListModel (source as IListModel);

            if (source is IEnumerable)
                return ResolveIEnumerable (source as IEnumerable);

            return ResolveCommonObject (source);
        }

        private static object ResolveIList (IEnumerable source)
        {
            return source;
        }

        private object ResolveIListSource (IListSource source)
        {
            if (internalIList != null || internalIListModel != null)
                membersAreGenerated = true;

            return ResolveDataSource (source.GetList ());
        }

        private object ResolveIListModel (IListModel source)
        {
            return source;
        }

        private object ResolveIEnumerable (IEnumerable source)
        {
            Type elementType = typeof (object);
            Type sourceType = source.GetType ();
            Type @interface = sourceType.GetInterface (typeof (IEnumerable<>).FullName);
            // HACK: a Mono bug with SortedDictionary: the declared interface type is KeyValuePair<> while the contained elements are of DictionaryEntry
            if (@interface == null || sourceType.FullName.StartsWith (typeof (SortedDictionary<,>).FullName, StringComparison.InvariantCulture)) {
                object first = source.Cast<object> ().FirstOrDefault ();
                if (first != null)
                    elementType = first.GetType ();
            } else elementType = @interface.GetGenericArguments () [0];
            IList list = (IList) Activator.CreateInstance (typeof (List<>).MakeGenericType (elementType));

            foreach (object item in source)
                list.Add (item);

            return list;
        }

        private static object ResolveCommonObject (object source)
        {
            return source;
        }

        #endregion

        #region Unwireing logic

        private void UnwireDataSource ()
        {
            foreach (object list in boundLists) {
                IBindingList ibList = list as IBindingList;
                if (ibList != null)
                    ibList.ListChanged -= List_ListChanged;
                else {
                    IListModel listModel = list as IListModel;
                    if (listModel != null)
                        listModel.ListChanged -= List_ListChanged;
                }
            }

            foreach (IBindingList list in boundMemberLists)
                list.ListChanged -= ListMember_ListChanged;

            foreach (INotifyPropertyChanged obj in boundMembers)
                obj.PropertyChanged -= ListMember_PropertyChanged;
        }

        #endregion

        #region Items change logic

        private void List_ListChanged (object sender, ListChangedEventArgs e)
        {
            if (listChanging)
                return;

            if (membersBound && e.ListChangedType == ListChangedType.ItemChanged)
                return;

            if (e.ListChangedType == ListChangedType.ItemAdded)
                WireSecondDimension (ResolveDataSource (this [e.NewIndex]));

            listChanging = true;
            if (ListChanged != null)
                ListChanged (sender, e);
            listChanging = false;
        }

        private void ListMember_ListChanged (object sender, ListChangedEventArgs e)
        {
            if (listChanging)
                return;

            if (!membersAreGenerated) {
                for (int i = 0; i < Count; i++) {
                    if (!ReferenceEquals (this [i], sender))
                        continue;

                    listChanging = true;
                    if (ListChanged != null)
                        ListChanged (InternalList, new ListChangedEventArgs (ListChangedType.ItemChanged, i, i));
                    listChanging = false;
                    return;
                }
            }

            listChanging = true;
            if (ListChanged != null)
                ListChanged (InternalList, new ListChangedEventArgs (ListChangedType.ItemChanged, -1));
            listChanging = false;
        }

        private void ListMember_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            if (listChanging)
                return;

            if (!membersAreGenerated) {
                for (int i = 0; i < Count; i++) {
                    if (!ReferenceEquals (this [i], sender))
                        continue;

                    listChanging = true;
                    if (ListChanged != null)
                        ListChanged (InternalList, new ListChangedEventArgs (ListChangedType.ItemChanged, i, i));
                    listChanging = false;
                    return;
                }
            }

            listChanging = true;
            if (ListChanged != null)
                ListChanged (InternalList, new ListChangedEventArgs (ListChangedType.ItemChanged, -1));
            listChanging = false;
        }

        #endregion

        #region IDisposable Members

        public void Dispose ()
        {
            UnwireDataSource ();

            boundLists.Clear ();
            boundMemberLists.Clear ();
            boundMembers.Clear ();
            //dataSource = null;
            //dataMember = null;
            internalIList = null;
            internalIListModel = null;
        }

        #endregion
    }
}
