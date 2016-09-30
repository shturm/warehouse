//
// LazyListModel.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/13/2008
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

//#define DEBUG_LAZYLIST
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Reflection;
using Warehouse.Data.ThreadPool;

#if DEBUG_LAZYLIST
using ThreadState = System.Threading.ThreadState;
#endif

namespace Warehouse.Data.Model
{
    public class LazyListModel<T> : IManageableListModel<T>, IDisposable
    {
        protected const int ITEM_FETCH_DELAY = 1;

        protected readonly DataProvider provider;
        private readonly bool autoStart;
        protected readonly string commandText;
        private readonly DbParam [] commandPars;
        protected readonly Dictionary<string, DbColumnManager> dbPropManagers = new Dictionary<string, DbColumnManager> ();
        protected readonly Dictionary<string, DbColumnManager> allPropManagers = new Dictionary<string, DbColumnManager> ();
        protected DbColumnManager [] colManagers;
        protected bool translateManagers = true;

        private bool isStarted;
        protected long count;
        protected readonly List<T> innerList = new List<T> ();
        protected readonly ManualResetEvent countSet = new ManualResetEvent (false);
        protected readonly WaitingList<int> itemWaitingList = new WaitingList<int> ();

        protected ColumnBase sortColumn;
        private string lastSortKey;
        private SortDirection lastSortDirection;
        private string filter = string.Empty;
        private readonly FilterPropertiesCollection filterProperties = new FilterPropertiesCollection ();

        internal LazyListModel ()
        {
            count = 0;
            countSet.Set ();
        }

        public LazyListModel (DataProvider dp, bool autoStart, string command, params DbParam [] parameters)
        {
            provider = dp;
            this.autoStart = autoStart;
            commandText = command;
            commandPars = parameters;

            InitializeColumnManagers ();

            if (autoStart)
                Start ();
        }

        protected virtual void InitializeColumnManagers ()
        {
            colManagers = DataProvider.GetDBManagers (typeof (T));
            foreach (DbColumnManager property in colManagers)
                dbPropManagers.Add (property.Member.Name, property);

            foreach (DbColumnManager manager in DataProvider.GetAllManagers (typeof (T)))
                allPropManagers.Add (manager.Member.Name, manager);
        }

        public void Restart ()
        {
            isStarted = false;
            Start ();
        }

        protected JobWrapper job;

        public bool Start ()
        {
            if (isStarted || string.IsNullOrEmpty (commandText))
                return false;

            isStarted = true;
            if (job != null && !job.IsFinished) {
                DataHelper.ModelThreadPool.AbortJob (job);
                job.WaitFinished ();
            }

            countSet.Reset ();
            job = new JobWrapper (Worker, new object [] { TransactionContext.Current, provider.ThreadServerContext });
            job.Finished += job_Finished;
            DataHelper.ModelThreadPool.EnqueueJob (job);

            return true;
        }

        protected virtual void Worker (object context)
        {
#if DEBUG_LAZYLIST
            Str.WriteDebugMessage ("LazyListModel.Worker: Launching data load");
            DateTime startTime = DateTime.Now;
#endif
            object [] pars = (object []) context;
            TransactionContext.Current = (TransactionContext) pars [0];
            provider.ThreadServerContext = (ServerContext) pars [1];

            string query = commandText;
            SqlHelper sqlHelper = provider.GetSqlHelper ();
            sqlHelper.AddParameters (commandPars);

            if (sortColumn != null) {
#if DEBUG_LAZYLIST
                Str.WriteDebugMessage ("LazyListModel.Worker: Setting data sort by \"{0}\" {1}", sortColumn.SortKey,
                    sortColumn.SortDirection);
#endif
                DbColumnManager sortManager;
                string sortKey = sortColumn.SortKey;
                DbField sortField = null;
                if (dbPropManagers.TryGetValue (sortKey, out sortManager))
                    sortField = sortManager.DbField;

                query = provider.GetQueryWithSort (query, sortKey, sortField, sortColumn.SortDirection);
            }

            if (!string.IsNullOrEmpty (filter) && filterProperties.Count > 0) {
#if DEBUG_LAZYLIST
                Str.WriteDebugMessage ("LazyListModel.Worker: Setting data filter \"{0}\"", filter);
#endif
                List<DbField> filterFields = new List<DbField> ();
                foreach (string property in filterProperties) {
                    DbColumnManager manager;
                    if (dbPropManagers.TryGetValue (property, out manager))
                        filterFields.Add (manager.DbField);
                }

                DbParam [] filterPars;
                query = provider.GetQueryWithFilter (query, filterFields, filter, out filterPars);
                sqlHelper.AddParameters (filterPars);
            }

#if DEBUG_LAZYLIST
            Str.WriteDebugMessage ("LazyListModel.Worker: Inner list cleared");
#endif
            itemWaitingList.Reset ();
            count = 0;
            innerList.Clear ();

#if DEBUG_LAZYLIST
            Str.WriteDebugMessage ("LazyListModel.Worker: Starting items counting in {0}",
                DateTime.Now - startTime);
#endif

            count = provider.GetQueryRowsCount (query, sqlHelper.Parameters);
            countSet.Set ();
            OnListReset ();

#if DEBUG_LAZYLIST
            Str.WriteDebugMessage ("LazyListModel.Worker: Counted {0} items of type \"{1}\" in {2}", count, typeof (T).FullName,
                DateTime.Now - startTime);
#endif
            using (IDataReader reader = provider.ExecuteReader (query, sqlHelper.Parameters)) {
                if (translateManagers)
                    provider.FieldsTable.FindOrdinals (reader, colManagers);

#if DEBUG_LAZYLIST
                Str.WriteDebugMessage ("LazyListModel.Worker: Started load of \"{0}\" items. Using transaction: {1}", typeof (T).FullName, TransactionContext.Current != null && TransactionContext.Current.Transaction != null);
#endif
                int c = 0;
                while (reader != null && reader.Read ()) {
                    innerList.Add ((T) provider.FillObject (DataProvider.CreateObject (typeof (T)), reader, ref colManagers));

                    JobWrapper thisJob = job;
                    if (thisJob != null && thisJob.Aborting) {
#if DEBUG_LAZYLIST
                        Str.WriteDebugMessage ("LazyListModel.Worker: Aborting items load");
#endif
                        return;
                    }

                    itemWaitingList.SignalItem (innerList.Count - 1);
                    OnItemAdded ();

                    // Give away some CPU time every now and then
                    if (c >= 100) {
                        Thread.Sleep (ITEM_FETCH_DELAY);
                        c = 0;
                    }
                    c++;
                }
#if DEBUG_LAZYLIST
                Str.WriteDebugMessage ("LazyListModel.Worker: Loaded {0} \"{1}\" items in {2}", innerList.Count, typeof (T).FullName,
                    DateTime.Now - startTime);
#endif
            }
        }

        private void job_Finished (object sender, WorkFinishedEventArgs e)
        {
            if (e.State != ThreadState.AbortRequested &&
                e.State != ThreadState.Aborted) {
                count = innerList.Count;
                countSet.Set ();
            }

            OnListReset ();
#if DEBUG_LAZYLIST
            Str.WriteDebugMessage ("LazyListModel.Worker: Finished load of {0} \"{1}\" items.", typeof (T).FullName, count);
#endif
            itemWaitingList.SignalAll ();
#if DEBUG_LAZYLIST
            Str.WriteDebugMessage ("LazyListModel.Worker: Sending join signal");
#endif
        }

        protected void OnListReset ()
        {
            if (ListChanged != null)
                ListChanged (this, new ListChangedEventArgs (ListChangedType.Reset, innerList.Count - 1));
        }

        protected void OnItemAdded ()
        {
            if (ListChanged != null)
                ListChanged (this, new ListChangedEventArgs (ListChangedType.ItemAdded, innerList.Count - 1));
        }

        #region IListModel members

        public event ListChangedEventHandler ListChanged;

        public int Count
        {
            get
            {
                if (!isStarted)
                    Start ();

                countSet.WaitOne ();
                CheckLastError ();

                return (int) count;
            }
        }

        object IListModel.this [int index]
        {
            get
            {
                return ((IListModel<T>) this) [index];
            }
            set
            {
                ((IListModel<T>) this) [index] = (T) value;
            }
        }

        public T this [int index]
        {
            get
            {
                WaitForItem (index, null);

                return innerList [index];
            }
            set
            {
                WaitForItem (index, null);

                innerList [index] = value;
            }
        }

        public object this [int index, string property]
        {
            get
            {
                DbColumnManager man = WaitForItem (index, property);
                if (man == null)
                    throw new ArgumentException ("No manager found for property " + property);

                return man.GetValue (innerList [index]);
            }
            set
            {
                DbColumnManager man = WaitForItem (index, property);
                if (man == null)
                    throw new ArgumentException ("No manager found for property " + property);

                man.SetValue (innerList [index], value);
            }
        }

        private DbColumnManager WaitForItem (int index, string property)
        {
            if (!isStarted)
                Start ();

            countSet.WaitOne ();
            DbColumnManager man = null;
            if (!string.IsNullOrEmpty (property)) {
                man = GetColumnManager (property);
                if (man == null)
                    throw new ArgumentException ("No manager found for property " + property);
            }

            if (index >= count)
                throw new ArgumentOutOfRangeException ("index");

            itemWaitingList.WaitForItem (index);

            CheckLastError ();

            if (index >= count)
                throw new ArgumentOutOfRangeException ("index");

            return man;
        }

        private void CheckLastError ()
        {
            if (job == null)
                return;

            WorkFinishedEventArgs args = job.FinishedArgs;
            if (args == null)
                return;

            Exception lastException = args.Exception;
            if (lastException == null)
                return;

            if (!(lastException is DbConnectionLostException) || !provider.DisableConnectionLostErrors)
                throw new Exception ("LazyListModel encountered an exception!", lastException);
        }


        private DbColumnManager GetColumnManager (string property)
        {
            DbColumnManager value;
            return allPropManagers.TryGetValue (property, out value) ? value : null;
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
            SortedList<string, PropertyInfo> props = new SortedList<string, PropertyInfo> ();
            foreach (PropertyInfo property in typeof (T).GetProperties ()) {
                ExchangePropertyAttribute exAttribute = null;
                foreach (object attrib in property.GetCustomAttributes (true)) {
                    exAttribute = attrib as ExchangePropertyAttribute;
                    if (exAttribute != null)
                        break;
                }

                if (exAttribute == null)
                    continue;

                props.Add (exAttribute.DefaultExchangeName, property);
            }

            DataTable ret = new DataTable ();
            foreach (KeyValuePair<string, PropertyInfo> pair in props)
                ret.Columns.Add (pair.Key, stringValues ? typeof (string) : pair.Value.PropertyType);

            foreach (T obj in this)
                ret.Rows.Add (props.Select (prop => prop.Value.GetValue (obj, null)).ToArray ());

            return ret;
        }

        #endregion

        #region ISortable members

        public ColumnBase SortColumn
        {
            get { return sortColumn; }
        }

        public event EventHandler<SortChangedEventArgs> SortChanged;

        public void Sort (string key, SortDirection direction)
        {
            Sort (new DummyColumn (key, direction));
        }

        public virtual void Sort (ColumnBase column)
        {
            string newSortKey;
            SortDirection newSortDirection;

            if (column != null) {
                newSortKey = column.SortKey;
                newSortDirection = column.SortDirection;
            } else {
                newSortKey = null;
                newSortDirection = SortDirection.None;
            }

            if (lastSortKey == newSortKey &&
                lastSortDirection == newSortDirection)
                return;

            sortColumn = column;
            lastSortKey = newSortKey;
            lastSortDirection = newSortDirection;

            if (autoStart || isStarted)
                Restart ();

            if (SortChanged != null)
                SortChanged (this, new SortChangedEventArgs (newSortKey, newSortDirection));
        }

        #endregion

        #region IFilterable members

        public string Filter
        {
            get { return filter; }
        }

        public FilterPropertiesCollection FilterProperties
        {
            get { return filterProperties; }
        }

        public event EventHandler<FilterChangedEventArgs> FilterChanged;

        public void SetFilter (string filterExpression)
        {
            if (filter == filterExpression)
                return;

            filter = filterExpression;

            if (autoStart || isStarted)
                Restart ();

            if (FilterChanged != null)
                FilterChanged (this, new FilterChangedEventArgs ());
        }

        #endregion

        public void Dispose ()
        {
            if (job != null && !job.IsFinished) {
                DataHelper.ModelThreadPool.AbortJob (job);
                job.Finished -= job_Finished;
                job = null;
            }

#if DEBUG_LAZYLIST
            Str.WriteDebugMessage ("LazyListModel: Inner list cleared (dispose)");
#endif
            count = 0;
            innerList.Clear ();
            countSet.Set ();
        }

        #region IEnumerable Members

        ///<summary>
        ///Returns an enumerator that iterates through a collection.
        ///</summary>
        ///
        ///<returns>
        ///An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        ///</returns>
        ///<filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator ()
        {
            return ((IEnumerable<T>) this).GetEnumerator ();
        }

        #endregion

        #region IEnumerable<T> Members

        ///<summary>
        ///Returns an enumerator that iterates through the collection.
        ///</summary>
        ///
        ///<returns>
        ///A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
        ///</returns>
        ///<filterpriority>1</filterpriority>
        public IEnumerator<T> GetEnumerator ()
        {
            for (int i = 0; i < Count; i++) {
                yield return this [i];
            }
        }

        #endregion
    }
}
