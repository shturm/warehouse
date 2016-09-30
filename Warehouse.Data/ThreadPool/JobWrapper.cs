//
// JobWrapper.cs
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
    public class JobWrapper
    {
        private readonly ParameterizedThreadStart jobDelegate;
        private readonly object parameter;
        private readonly ManualResetEvent finishedEvent = new ManualResetEvent (false);
        private WorkFinishedEventArgs finishedArgs;
        private WorkerWrapper worker;
        private DateTime? enqueueDate;
        private DateTime? startTime;
        private TimeSpan maxRunningTime = TimeSpan.MaxValue;
        private bool isFinished;

        public event EventHandler<WorkFinishedEventArgs> Finished;

        public ParameterizedThreadStart JobDelegate
        {
            get { return jobDelegate; }
        }

        public object Parameter
        {
            get { return parameter; }
        }

        public WorkFinishedEventArgs FinishedArgs
        {
            get { return finishedArgs; }
        }

        internal WorkerWrapper Worker
        {
            get { return worker; }
            set { worker = value; }
        }

        public DateTime? EnqueueDate
        {
            get { return enqueueDate; }
            internal set { enqueueDate = value; }
        }

        public TimeSpan TimeEnqueued
        {
            get { return enqueueDate.HasValue ? DateTime.Now - enqueueDate.Value : TimeSpan.Zero; }
        }

        public DateTime? StartTime
        {
            get { return startTime; }
            internal set { startTime = value; }
        }

        public TimeSpan TimeStarted
        {
            get { return startTime.HasValue ? DateTime.Now - startTime.Value : TimeSpan.Zero; }
        }

        public TimeSpan MaxRunningTime
        {
            get { return maxRunningTime; }
            set { maxRunningTime = value; }
        }

        public bool Aborting { get; set; }

        public bool IsFinished
        {
            get { return isFinished; }
            set { isFinished = value; }
        }

        public JobWrapper (ParameterizedThreadStart jobDelegate, object parameter)
        {
            this.jobDelegate = jobDelegate;
            this.parameter = parameter;
        }

        internal void DoWork ()
        {
            Exception lastException = null;
            try {
                try {
                    jobDelegate (parameter);
                } catch (ThreadAbortException) {
                    throw;
                } catch (Exception ex) {
                    lastException = ex;
                } finally {
                    Finish (new WorkFinishedEventArgs (Thread.CurrentThread.ThreadState, lastException));
                }
            } finally {
                if (!isFinished)
                    Finish (new WorkFinishedEventArgs (Thread.CurrentThread.ThreadState, lastException));
            }
        }

        internal void Finish (WorkFinishedEventArgs args)
        {
            finishedArgs = args;
            finishedEvent.Set ();
            if (Finished != null)
                Finished (this, args);
            isFinished = true;
        }

        public bool WaitFinished ()
        {
            return finishedEvent.WaitOne ();
        }

        public bool WaitFinished (int miliseconds)
        {
            return finishedEvent.WaitOne (miliseconds, false);
        }
    }
}
