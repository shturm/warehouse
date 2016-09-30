//
// TransactionContext.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/15/2008
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

//#define DEBUG_CONLOCK
using System;
using System.Collections;
using System.Collections.Generic;
#if DEBUG_CONLOCK
using System.Diagnostics;
#endif
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;

namespace Warehouse.Data
{
    public class TransactionContext
    {
        private readonly static Dictionary<int, TransactionContext> transactionContexts = new Dictionary<int, TransactionContext> ();

        private readonly int threadId;
        private readonly object transaction;
        private readonly object connection;
        private readonly AutoResetEvent connectionLock = new AutoResetEvent (true);
        private readonly Dictionary<object, Dictionary<string, object>> snapshots = new Dictionary<object, Dictionary<string, object>> ();
        private int beganTransactions = 1;
        private int waitingConnection;

        public int ThreadId
        {
            get { return threadId; }
        }

        public object Transaction
        {
            get { return transaction; }
        }

        public int WaitingConnection
        {
            get { return waitingConnection; }
        }

        public int BeganTransactions
        {
            get { return beganTransactions; }
            set { beganTransactions = value; }
        }

        public static TransactionContext Current
        {
            get
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                TransactionContext context;
                lock (transactionContexts)
                    return transactionContexts.TryGetValue (threadId, out context) ? context : null;
            }
            set
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                lock (transactionContexts)
                    if (transactionContexts.ContainsKey (threadId)) {
                        if (value == null) {
                            if (transactionContexts [threadId].WaitingConnection > 0)
                                transactionContexts [threadId].ReleaseConnection ();
                            transactionContexts.Remove (threadId);
                        } else
                            transactionContexts [threadId] = value;
                    } else {
                        if (value != null)
                            transactionContexts.Add (threadId, value);
                    }
            }
        }

        public TransactionContext (int threadId, object trans, object conn)
        {
            this.threadId = threadId;
            transaction = trans;
            connection = conn;
        }

        public object GetConnection ()
        {
#if DEBUG_CONLOCK
            Debug.WriteLine ("\nTransactionContext.get_Connection: TID:{0};{1} Waiting for connection lock {2}", Thread.CurrentThread.ManagedThreadId, threadId, waitingConnection);
            StackTrace st = new StackTrace ();
            foreach (StackFrame frame in st.GetFrames ()) {
                MethodBase method = frame.GetMethod ();
                Debug.WriteLine ("TransactionContext.get_Connection: Trace: {0}:{1}", method.DeclaringType, method.Name);
            }
#endif
            waitingConnection++;
            if (!connectionLock.WaitOne (10000)) {
#if DEBUG_CONLOCK
                Debug.WriteLine ("TransactionContext.get_Connection: TID:{0} Failed to get connection", Thread.CurrentThread.ManagedThreadId);
#endif
                return null;
            }

#if DEBUG_CONLOCK
            Debug.WriteLine ("TransactionContext.get_Connection: TID:{0} Got connection", Thread.CurrentThread.ManagedThreadId);
#endif
            return connection;
        }

        public void ReleaseConnection ()
        {
            waitingConnection--;
#if DEBUG_CONLOCK
            Debug.WriteLine ("\nTransactionContext.ReleaseConnection: TID:{0}:{1} Releasing connection lock {2}", Thread.CurrentThread.ManagedThreadId, threadId, waitingConnection);
            StackTrace st = new StackTrace ();
            foreach (StackFrame frame in st.GetFrames ()) {
                MethodBase method = frame.GetMethod ();
                Debug.WriteLine ("TransactionContext.ReleaseConnection: Trace: {0}:{1}", method.DeclaringType, method.Name);
            }
#endif
            connectionLock.Set ();
        }

        public void SnapshotObject (object obj, bool replaceSnapshots = true)
        {
            if (obj == null)
                throw new ArgumentNullException ("obj");

            if (!replaceSnapshots && snapshots.ContainsKey (obj))
                return;

            MemberInfo [] members = obj.GetType ().GetMembers ();
            Dictionary<string, object> values = new Dictionary<string, object> ();
            foreach (MemberInfo memberInfo in members) {
                object [] attributes = memberInfo.GetCustomAttributes (typeof (JsonIgnoreAttribute), true);
                if (attributes.Length > 0)
                    continue;

                switch (memberInfo.MemberType) {
                    case MemberTypes.Field:
                        FieldInfo field = (FieldInfo) memberInfo;
                        if (!field.IsInitOnly && !field.IsLiteral)
                            SaveValue (memberInfo, field.FieldType, values, field.GetValue (obj));

                        break;

                    case MemberTypes.Property:
                        PropertyInfo property = (PropertyInfo) memberInfo;
                        if (property.CanRead && property.CanWrite)
                            SaveValue (memberInfo, property.PropertyType, values, property.GetValue (obj, null));

                        break;
                }
            }
            snapshots [obj] = values;
        }

        private void SaveValue (MemberInfo memberInfo, Type type, IDictionary<string, object> values, object val)
        {
            if (type.IsValueType || type == typeof (string)) {
                values.Add (memberInfo.Name, val);
                return;
            }
            IEnumerable enumerable = val as IEnumerable;
            if (enumerable != null) {
                foreach (object element in enumerable)
                    if (element != null) {
                        Type elementType = element.GetType ();
                        if (!elementType.IsValueType && elementType != typeof (string))
                            SnapshotObject (element, false);
                    }
            } else if (val != null)
                SnapshotObject (val, false);
        }

        public void CommitObjects ()
        {
            snapshots.Clear ();
        }

        public void RollbackObjects ()
        {
            foreach (KeyValuePair<object, Dictionary<string, object>> snapshot in snapshots) {
                object target = snapshot.Key;
                Dictionary<string, object> values = snapshot.Value;
                MemberInfo [] members = target.GetType ().GetMembers ();

                foreach (MemberInfo memberInfo in members) {
                    if (!values.ContainsKey (memberInfo.Name))
                        continue;

                    switch (memberInfo.MemberType) {
                        case MemberTypes.Field:
                            FieldInfo fi = (FieldInfo) memberInfo;
                            fi.SetValue (target, values [memberInfo.Name]);
                            break;

                        case MemberTypes.Property:
                            PropertyInfo pi = (PropertyInfo) memberInfo;
                            pi.SetValue (target, values [memberInfo.Name], null);
                            break;
                    }
                }
            }

            snapshots.Clear ();
        }
    }
}
