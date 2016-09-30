//
// WorkerWrapper.cs
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

using System;
using System.Threading;

namespace Warehouse.Data.ThreadPool
{
    internal class WorkerWrapper : IDisposable
    {
        private readonly CustomThreadPool owner;
        private readonly AutoResetEvent startWorkEvent = new AutoResetEvent (false);
        private readonly object finishingSync = new object ();
        private Thread thread;
        private DateTime sleepStartAt;
        private bool isSleeping;
        private JobWrapper currentJob;
        private bool abortingThread;
        private bool exit;

        public ThreadState ThreadState
        {
            get { return thread.ThreadState; }
        }

        public DateTime SleepStartAt
        {
            get { return sleepStartAt; }
        }

        public TimeSpan TimeSleeping
        {
            get { return isSleeping ? DateTime.Now - sleepStartAt : new TimeSpan (); }
        }

        public JobWrapper CurrentJob
        {
            get { return currentJob; }
        }

        public bool IsAlive
        {
            get { return thread != null && thread.IsAlive; }
        }

        public bool AbortingThread
        {
            get { return abortingThread; }
        }

        public WorkerWrapper (CustomThreadPool owner)
        {
            this.owner = owner;
            sleepStartAt = DateTime.Now;
        }

        private void WorkerDelegateWrapper ()
        {
            while (true) {
                isSleeping = true;
                sleepStartAt = DateTime.Now;
                startWorkEvent.WaitOne ();
                isSleeping = false;
                if (exit)
                    return;

                JobWrapper cJob = currentJob;
                if (cJob == null)
                    continue;

                cJob.Worker = this;
                cJob.StartTime = DateTime.Now;
                cJob.DoWork ();
                cJob.Worker = null;
            }
        }

        public bool StartWork (JobWrapper job)
        {
            if (job == null)
                return false;

            lock (finishingSync) {
                JobWrapper oldJob = currentJob;
                if (!abortingThread && oldJob != null && !oldJob.IsFinished)
                    return false;

                currentJob = job;
            }

            if (thread != null && abortingThread)
                thread.Join ();

            startWorkEvent.Reset ();

            if (thread == null || !thread.IsAlive) {
                thread = new Thread (WorkerDelegateWrapper) { Name = owner.Name + " Worker" };
                abortingThread = false;
                thread.Start ();
            }

            startWorkEvent.Set ();
            return true;
        }

        public void StopWork ()
        {
            lock (finishingSync) {
                JobWrapper job = currentJob;
                if (job == null || job.IsFinished)
                    return;

                if (thread == null)
                    return;

                thread.Abort ();
                abortingThread = true;
            }
        }

        public bool CheckFinished ()
        {
            JobWrapper job = currentJob;
            if (job != null && job.Aborting) {
                StopWork ();
                return true;
            }

            return job == null || job.IsFinished;
        }

        public void Dispose ()
        {
            StopWork ();

            if (thread == null || !thread.IsAlive)
                return;

            exit = true;
            startWorkEvent.Set ();
            if (thread.Join (200))
                return;

            thread.Abort ();
            abortingThread = true;
        }
    }
}
