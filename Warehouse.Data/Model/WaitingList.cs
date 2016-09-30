//
// WaitingList.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/23/2008
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

using System.Collections.Generic;
using System.Threading;

namespace Warehouse.Data.Model
{
    public class WaitingList<T> : SortedList<T, ManualResetEvent>
    {
        private readonly object syncRoot = new object ();
        private T lastSignal;
        private bool lastSignalValid;
        private bool isReleased;

        public void WaitForItem (T item)
        {
            ManualResetEvent ev;

            if (isReleased)
                return;

            lock (syncRoot) {
                if (lastSignalValid && Comparer.Compare (lastSignal, item) >= 0)
                    return;

                if (ContainsKey (item)) {
                    ev = base [item];
                } else {
                    ev = new ManualResetEvent (false);
                    Add (item, ev);
                }
            }

            ev.WaitOne ();
        }

        public void SignalItem (T item)
        {
            lock (syncRoot) {
                lastSignal = item;
                lastSignalValid = true;

                while (true) {
                    if (Count == 0)
                        return;

                    T i = Keys [0];

                    if (Comparer.Compare (i, item) <= 0) {
                        base [i].Set ();
                        Remove (i);
                    } else
                        break;
                }
            }
        }

        public void Reset ()
        {
            lock (syncRoot) {
                lastSignal = default (T);
                lastSignalValid = false;
                isReleased = false;
            }
        }

        public void SignalAll ()
        {
            lock (syncRoot) {
                foreach (KeyValuePair<T, ManualResetEvent> valuePair in this) {
                    valuePair.Value.Set ();
                }

                Clear ();

                isReleased = true;
            }
        }
    }
}
