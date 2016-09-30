//
// CustomThreadPool.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   07/11/2009
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

//#define DEBUG_THREADPOOL

using System;
using System.Collections.Generic;
using System.Threading;
using ThreadState = System.Threading.ThreadState;

#if DEBUG_THREADPOOL
using System.Diagnostics;
#endif

namespace Warehouse.Data.ThreadPool
{
    public delegate void WorkFinished (object sender, WorkFinishedEventArgs args);

    public class CustomThreadPool : IDisposable
    {
        private readonly string name;
        private readonly object jobsSync = new object ();
        private readonly Queue<JobWrapper> jobs = new Queue<JobWrapper> ();
        private readonly AutoResetEvent jobEnqueued = new AutoResetEvent (false);

        private readonly object workersSync = new object ();
        private readonly Queue<WorkerWrapper> freeWorkers = new Queue<WorkerWrapper> ();
        private readonly List<WorkerWrapper> usedWorkers = new List<WorkerWrapper> ();

        private readonly object distributorSync = new object ();
        private Thread distributor;

        private bool lazyStart = true;
        private bool exit;
        private uint minWorkers;
        private uint maxWorkers = int.MaxValue;
        private uint maxWaitBeforeWorkerCreation;
#if DEBUG
        private uint maxWorkerSleepTime = 10 * 1000;
#else
        private uint maxWorkerSleepTime = 30 * 1000;
#endif
        private uint distributorPollWait = 100;

        #region Public properties

        public string Name
        {
            get { return name; }
        }

        public bool LazyStart
        {
            get { return lazyStart; }
            set
            {
                lazyStart = value;
                if (!lazyStart && distributor.ThreadState == ThreadState.Unstarted)
                    distributor.Start ();
            }
        }

        public uint MinWorkers
        {
            get { return minWorkers; }
            set { minWorkers = value; }
        }

        public uint MaxWorkers
        {
            get { return maxWorkers; }
            set { maxWorkers = value; }
        }

        public uint MaxWaitBeforeWorkerCreation
        {
            get { return maxWaitBeforeWorkerCreation; }
            set { maxWaitBeforeWorkerCreation = value; }
        }

        public uint MaxWorkerSleepTime
        {
            get { return maxWorkerSleepTime; }
            set { maxWorkerSleepTime = value; }
        }

        public uint DistributorPollWait
        {
            get { return distributorPollWait; }
            set { distributorPollWait = value; }
        }

        #endregion

        public CustomThreadPool (string name, bool lazyStart = true)
        {
            this.name = name;
            distributor = new Thread (DistributorThread) { Name = name + " Distributor" };
            LazyStart = lazyStart;
        }

        public JobWrapper EnqueueJob (JobWrapper jobWrapper)
        {
            if (exit)
                throw new ApplicationException ("New job enqueued while exiting.");

            lock (jobsSync) {
                jobs.Enqueue (jobWrapper);
                jobWrapper.EnqueueDate = DateTime.Now;
#if DEBUG_THREADPOOL
                Debug.WriteLine (string.Format ("{0} > Thread pool (jq:{1}, wu:{2},wf:{3}): job enqueued", DateTime.Now.ToString ("HH:mm:ss.ff"), jobs.Count, usedWorkers.Count, freeWorkers.Count));
#endif
                jobEnqueued.Set ();
            }

            lock (distributorSync) {
                if (distributor.ThreadState == ThreadState.Unstarted)
                    distributor.Start ();
            }

            return jobWrapper;
        }

        public void AbortJob (JobWrapper jobWrapper)
        {
            if (jobWrapper == null)
                throw new ArgumentNullException ("jobWrapper");

            lock (jobsSync) {
                jobWrapper.Aborting = true;
            }
        }

        private void DistributorThread ()
        {
            while (!exit) {
                JobWrapper job = null;
                bool entered = false;
                try {
                    Monitor.Enter (jobsSync);
                    entered = true;
                    if (jobs.Count == 0) {
                        Monitor.Exit (jobsSync);
                        entered = false;
                        jobEnqueued.WaitOne ((int) distributorPollWait, false);
                        Monitor.Enter (jobsSync);
                        entered = true;
                        if (exit)
                            break;
                    }
                    while (jobs.Count > 0) {
                        job = jobs.Peek ();
                        if (!job.Aborting)
                            break;

                        job.Finish (new WorkFinishedEventArgs (ThreadState.Unstarted, null));
                        jobs.Dequeue ();
                        job = null;
                    }
                } finally {
                    if (entered)
                        Monitor.Exit (jobsSync);
                }

                lock (workersSync) {
                    for (int i = usedWorkers.Count - 1; i >= 0; i--) {
                        WorkerWrapper worker = usedWorkers [i];
                        if (worker.CheckFinished ()) {
                            usedWorkers.RemoveAt (i);
                            freeWorkers.Enqueue (worker);
#if DEBUG_THREADPOOL
                            Debug.WriteLine (string.Format ("{0} > Thread pool (jq:{1}, wu:{2},wf:{3}): worker finished", DateTime.Now.ToString ("HH:mm:ss.ff"), jobs.Count, usedWorkers.Count,
                                                          freeWorkers.Count));
#endif
                            continue;
                        }

                        if (worker.CurrentJob.MaxRunningTime <= worker.CurrentJob.TimeStarted) {
                            worker.StopWork ();
                            usedWorkers.RemoveAt (i);
                            freeWorkers.Enqueue (worker);
#if DEBUG_THREADPOOL
                        Debug.WriteLine (string.Format ("{0} > Thread pool (jq:{1}, wu:{2},wf:{3}): worker stopped", DateTime.Now.ToString ("HH:mm:ss.ff"), jobs.Count, usedWorkers.Count,
                                                      freeWorkers.Count));
#endif
                        }

                        if (!worker.IsAlive) {
                            if (worker.CurrentJob != null && !worker.CurrentJob.IsFinished)
                                worker.CurrentJob.Finish (new WorkFinishedEventArgs (ThreadState.Aborted, null));

                            usedWorkers.RemoveAt (i);
                            freeWorkers.Enqueue (worker);
#if DEBUG_THREADPOOL
                        Debug.WriteLine (string.Format ("{0} > Thread pool (jq:{1}, wu:{2},wf:{3}): worker died unexpectedly", DateTime.Now.ToString ("HH:mm:ss.ff"), jobs.Count, usedWorkers.Count,
                                                      freeWorkers.Count));
#endif
                        }
                    }

                    if (job != null) {
                        WorkerWrapper worker = null;
                        if (freeWorkers.Count == 0) {
                            if (maxWaitBeforeWorkerCreation <= (int) job.TimeEnqueued.TotalMilliseconds &&
                                usedWorkers.Count < maxWorkers && !exit) {
                                worker = new WorkerWrapper (this);
#if DEBUG_THREADPOOL
                                Debug.WriteLine (string.Format ("{0} > Thread pool (jq:{1}, wu:{2},wf:{3}): worker created", DateTime.Now.ToString ("HH:mm:ss.ff"), jobs.Count, usedWorkers.Count,
                                                              freeWorkers.Count));
#endif
                            }
                        } else {
                            worker = freeWorkers.Dequeue ();
                        }

                        if (worker != null) {
                            if (worker.StartWork (job)) {
                                lock (jobsSync) {
                                    jobs.Dequeue ();
#if DEBUG_THREADPOOL
                                    Debug.WriteLine (string.Format ("{0} > Thread pool (jq:{1}, wu:{2},wf:{3}): job dequeued", DateTime.Now.ToString ("HH:mm:ss.ff"), jobs.Count, usedWorkers.Count,
                                                                  freeWorkers.Count));
#endif
                                }
                                usedWorkers.Add (worker);
#if DEBUG_THREADPOOL
                                Debug.WriteLine (string.Format ("{0} > Thread pool (jq:{1}, wu:{2},wf:{3}): worker started", DateTime.Now.ToString ("HH:mm:ss.ff"), jobs.Count, usedWorkers.Count,
                                                              freeWorkers.Count));
#endif
                            } else {
                                freeWorkers.Enqueue (worker);
#if DEBUG_THREADPOOL
                                Debug.WriteLine (string.Format ("{0} > Thread pool (jq:{1}, wu:{2},wf:{3}): worker start failed", DateTime.Now.ToString ("HH:mm:ss.ff"), jobs.Count, usedWorkers.Count,
                                                              freeWorkers.Count));
#endif
                            }
                        }
                    }

                    bool workerDisposed = false;
                    do {
                        if (freeWorkers.Count <= minWorkers)
                            break;

                        WorkerWrapper worker = freeWorkers.Peek ();
                        if (maxWorkerSleepTime > (int) worker.TimeSleeping.TotalMilliseconds)
                            continue;

                        freeWorkers.Dequeue ();
                        worker.Dispose ();
                        workerDisposed = true;
#if DEBUG_THREADPOOL
                        Debug.WriteLine (string.Format ("{0} > Thread pool (jq:{1}, wu:{2},wf:{3}): worker disposed", DateTime.Now.ToString ("HH:mm:ss.ff"), jobs.Count, usedWorkers.Count,
                                                      freeWorkers.Count));
#endif
                    } while (workerDisposed);
                }
            }
        }

        public void Dispose ()
        {
            lock (workersSync) {
                exit = true;

                foreach (WorkerWrapper worker in usedWorkers) {
                    worker.Dispose ();
                }

                for (int i = 0; i < 5 && usedWorkers.Count > 0; i++) {
                    bool workerRemoved = false;
                    for (int j = usedWorkers.Count - 1; j >= 0; j--) {
                        if (usedWorkers [j].IsAlive)
                            continue;

                        usedWorkers.RemoveAt (j);
                        workerRemoved = true;
                    }

                    if (workerRemoved)
                        i = 0;
                    else
                        Thread.Sleep (100);
                }

                while (freeWorkers.Count > 0) {
                    WorkerWrapper worker = freeWorkers.Dequeue ();
                    worker.Dispose ();
                }
            }

            if (distributor != null && distributor.IsAlive) {
                if (!distributor.Join (200))
                    distributor.Abort ();
                distributor = null;
            }
        }
    }
}
