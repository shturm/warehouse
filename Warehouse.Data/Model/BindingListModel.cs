//
// BindingListModel.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   10/19/2007
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
using System.Data;
using Warehouse.Data.DataBinding;

namespace Warehouse.Data.Model
{
    public class BindingListModel : BindingListModel<object>
    {
        public BindingListModel ()
        {
        }

        public BindingListModel (object source)
            : this ()
        {
            SetSource (source);
        }
    }

    public class BindingListModel<T> : IManageableListModel<T>
    {
        #region Fields

        private object source;
        private BindManager bManager;
        private ColumnBase sortColumn;
        private readonly List<RowMap> dataMap = new List<RowMap> ();
        private bool useClientSort = false;
        private bool useClientFilter = false;
        private bool useRowMapping = false;
        private string filter;
        private readonly FilterPropertiesCollection filterProperties = new FilterPropertiesCollection ();
        private readonly List<string> filterPhrases = new List<string> ();

        #endregion

        #region Properties

        #endregion

        public BindingListModel ()
        {
        }

        public BindingListModel (object source, bool watchItemChanges = true)
            : this ()
        {
            SetSource (source, watchItemChanges);
        }

        public void SetSource (object value, bool watchItemChanges = true)
        {
            if (bManager != null) {
                bManager.ListChanged -= bManager_ListChanged;
                bManager.Dispose ();
            }

            source = value;
            bManager = new BindManager (value, watchItemChanges);
            bManager.ListChanged += bManager_ListChanged;
            RefreshMappings ();

            bManager_ListChanged (this, new ListChangedEventArgs (ListChangedType.Reset, -1));
        }

        private void bManager_ListChanged (object sender, ListChangedEventArgs e)
        {
            RefreshMappings ();

            if (ListChanged != null)
                ListChanged (this, e);
        }

        #region IListModel Members

        public event ListChangedEventHandler ListChanged;

        public int Count
        {
            get
            {
                if (useRowMapping)
                    return dataMap.Count;
                else
                    return bManager.Count;
            }
        }

        object IListModel.this [int index]
        {
            get
            {
                return bManager [GetSourceIndex (index)];
            }
            set
            {
                bManager [GetSourceIndex (index)] = value;
            }
        }

        public T this [int index]
        {
            get
            {
                object ret = bManager [GetSourceIndex (index)];
                if (ret is T)
                    return (T) bManager [GetSourceIndex (index)];

                throw new ApplicationException (
                    string.Format ("Return type is not of the correct type. Expected type was {0} but the type available is {1}",
                        typeof (T).FullName, ret.GetType ().FullName));
            }
            set
            {
                bManager [GetSourceIndex (index)] = value;
            }
        }

        public object this [int index, string property]
        {
            get
            {
                return bManager.Rows [GetSourceIndex (index)] [property];
            }
            set
            {
                bManager.Rows [GetSourceIndex (index)] [property] = value;
            }
        }

        public int GetSourceIndex (int modelIndex)
        {
            if (useRowMapping)
                return dataMap [modelIndex].RowIndex;
            else
                return modelIndex;
        }

        #endregion

        #region ISortable Members

        public ColumnBase SortColumn
        {
            get
            {
                ISortable sortSource = source as ISortable;
                if (sortSource != null) {
                    return sortSource.SortColumn;
                }

                return sortColumn;
            }
        }

        public event EventHandler<SortChangedEventArgs> SortChanged;

        public void Sort (string key, SortDirection direction)
        {
            Sort (new DummyColumn (key, direction));
        }

        public void Sort (ColumnBase column)
        {
            //if (column != null &&
            //    sortColumn != null &&
            //    column.SortKey == sortColumn.SortKey &&
            //    column.SortDirection == sortColumn.SortDirection)
            //    return;

            sortColumn = column;
            useClientSort = false;
            if (column != null && column.IsSortable) {
                if (!(source is ISortable))
                    useClientSort = true;

            } else {
                sortColumn = null;
            }

            RefreshMappings ();

            if (SortChanged != null)
                SortChanged (this, new SortChangedEventArgs (sortColumn != null ? sortColumn.SortKey : null, sortColumn != null ? sortColumn.SortDirection : SortDirection.None));
        }

        #endregion

        #region IFilterable Members

        public string Filter
        {
            get { return filter; }
        }

        public IList<string> FilterPhrases
        {
            get { return filterPhrases; }
        }

        public FilterPropertiesCollection FilterProperties
        {
            get { return filterProperties; }
        }

        public event EventHandler<FilterChangedEventArgs> FilterChanged;

        public void SetFilter (string filterExpression)
        {
            if (filter != null && filterExpression != null && filter == filterExpression)
                return;

            filter = filterExpression;
            filterPhrases.Clear ();
            if (!string.IsNullOrEmpty (filter)) {
                filterPhrases.AddRange (filter.Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

                //// Remove number formatting like "00000001" or "123.12300000"
                //for (int i = 0; i < filterPhrases.Count; i++) {
                //    long longRes;
                //    if (long.TryParse (filterPhrases [i], out longRes)) {
                //        filterPhrases [i] = longRes.ToString ();
                //        continue;
                //    }

                //    double doubleRes;
                //    if (double.TryParse (filterPhrases [i], out doubleRes)) {
                //        filterPhrases [i] = doubleRes.ToString ();
                //        continue;
                //    }
                //}
            }

            RefreshFilter ();
        }

        private void RefreshFilter ()
        {
            useClientFilter = false;
            if (!(source is IFilterable))
                useClientFilter = true;

            RefreshMappings ();

            if (FilterChanged != null)
                FilterChanged (this, new FilterChangedEventArgs ());
        }

        #endregion

        private void RefreshMappings ()
        {
            useRowMapping = useClientSort | useClientFilter;

            dataMap.Clear ();

            ReFilter ();
            ReSort ();
        }

        private void ReFilter ()
        {
            IFilterable filterSource = source as IFilterable;
            if (filterSource != null) {
                filterSource.SetFilter (filter);
                return;
            }

            if (!useRowMapping)
                return;

            if (useClientFilter && !string.IsNullOrEmpty (filter)) {
                FilterRowEvenArgs args = new FilterRowEvenArgs ();

                for (int i = 0; i < bManager.Count; i++) {
                    args.SourceRow = i;
                    DefaultFilterCallback (args);

                    if (args.IsMatch) {
                        if (useClientSort) {
                            if (sortColumn != null && !string.IsNullOrEmpty (sortColumn.SortKey))
                                dataMap.Add (new RowMap (bManager.Rows [i] [sortColumn.SortKey], i));
                            else
                                dataMap.Add (new RowMap (bManager.Rows [i].Value, i));
                        } else
                            dataMap.Add (new RowMap (null, i));
                    }
                }
            } else {
                if (useClientSort) {
                    if (sortColumn != null && !string.IsNullOrEmpty (sortColumn.SortKey)) {
                        for (int i = 0; i < bManager.Count; i++) {
                            dataMap.Add (new RowMap (bManager.Rows [i] [sortColumn.SortKey], i));
                        }
                    } else {
                        for (int i = 0; i < bManager.Count; i++) {
                            dataMap.Add (new RowMap (bManager.Rows [i].Value, i));
                        }
                    }
                } else {
                    useRowMapping = false;
                }
            }
        }

        private void DefaultFilterCallback (FilterRowEvenArgs args)
        {
            if (filterPhrases.Count == 0) {
                args.IsMatch = true;
                return;
            }

            List<string> phrases = new List<string> (filterPhrases);
            BindRow<object> row = bManager.Rows [args.SourceRow];
            args.IsMatch = false;
            foreach (string key in filterProperties) {
                object value = row [key];
                if (value == null)
                    continue;

                string strValue = value.ToString ();

                for (int i = phrases.Count - 1; i >= 0; i--) {
                    if (strValue.IndexOf (phrases [i], StringComparison.InvariantCultureIgnoreCase) >= 0)
                        phrases.RemoveAt (i);
                }

                if (phrases.Count == 0) {
                    args.IsMatch = true;
                    return;
                }
            }
        }

        private void ReSort ()
        {
            ISortable sortSource = source as ISortable;
            if (sortSource != null) {
                sortSource.Sort (sortColumn);
                return;
            }

            if (sortColumn == null || !useClientSort)
                return;

            dataMap.Sort (new RowComparer (sortColumn.Comparer));
        }

        public int IndexOf (object obj)
        {
            for (int i = 0; i < Count; i++) {
                if (ReferenceEquals (obj, this [i]))
                    return i;
            }

            return -1;
        }

        public T Find (Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException ("match");

            for (int i = 0; i < Count; i++) {
                T item = this [i];
                if (match (item))
                    return item;
            }

            return default (T);
        }

        public T [] FindAll (Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException ("match");

            List<T> ret = new List<T> ();

            for (int i = 0; i < Count; i++) {
                T item = this [i];
                if (match (item))
                    ret.Add (item);
            }

            return ret.ToArray ();
        }

        public int FindIndex (Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException ("match");

            for (int i = 0; i < Count; i++) {
                T item = this [i];
                if (match (item))
                    return i;
            }

            return -1;
        }

        public DataTable ToDataTable (bool stringValues)
        {
            DataTable table = new DataTable ("ListModelTable");

            int i;
            for (i = 0; i < bManager.Columns.Count; i++)
                table.Columns.Add (bManager.Columns [i].Name, stringValues ? typeof (string) : bManager.Columns [i].Type);

            for (i = 0; i < Count; i++)
                table.Rows.Add (bManager.Rows [i].ToArray ());

            return table;
        }

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return ((IEnumerable<object>) this).GetEnumerator ();
        }

        #endregion

        #region IEnumerable<object> Members

        public IEnumerator<T> GetEnumerator ()
        {
            for (int i = 0; i < Count; i++) {
                yield return this [i];
            }
        }

        #endregion
    }
}
