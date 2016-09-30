//
// CellStyleQueryEventArgs.cs
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

using Gtk;

namespace Warehouse.Component.ListView
{
    public class CellStyleQueryEventArgs : CellEventArgs
    {
        private readonly StateType state;

        public StateType State
        {
            get { return state; }
        }

        public PangoStyle Style { get; set; }

        public CellStyleQueryEventArgs (StateType state, CellPosition cell)
            : base (cell)
        {
            this.state = state;
        }
    }
}
