//
// CellKeyPressEventArgs.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   10/18/2007
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

using Gdk;
using GLib;
using Gtk;

namespace Warehouse.Component.ListView
{
    public class CellKeyPressEventArgs : CellEventArgs
    {
        private readonly EventKey eventKey;
        private readonly Gdk.Key gdkKey;
        private Entry entry;

        public EventKey EventKey
        {
            get { return eventKey; }
        }

        public Gdk.Key GdkKey
        {
            get { return gdkKey; }
        }

        public Entry Entry
        {
            get { return entry; }
            internal set
            {
                entry = value;
            }
        }

        public bool Editing
        {
            get { return entry != null; }
        }

        private SignalArgs signalArgs;
        public SignalArgs SignalArgs
        {
            set { signalArgs = value; }
        }

        internal CellKeyPressEventArgs (int column, int row, Entry entry, EventKey eventKey)
            : this (column, row, entry, eventKey, eventKey.Key)
        {
        }

        internal CellKeyPressEventArgs (int column, int row, Entry entry, EventKey eventKey, Gdk.Key gdkKey)
            : base (column, row)
        {
            this.entry = entry;
            this.eventKey = eventKey;
            this.gdkKey = gdkKey;
        }

        public void MarkAsHandled ()
        {
            if (signalArgs != null)
                signalArgs.RetVal = true;
        }
    }
}
