//
// DataCommandEventArgs.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   09.25.2007
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

namespace Warehouse.Data
{
    public class DataCommandEventArgs : EventArgs
    {
        private readonly string command;
        private readonly DateTime startAt;
        private readonly DbParam [] parameters;
        private readonly bool isReadOnly;

        public bool IsReadOnly
        {
            get { return isReadOnly; }
        }

        public string Command
        {
            get { return command; }
        }

        public DateTime StartAt
        {
            get { return startAt; }
        }

        public DbParam [] Parameters
        {
            get { return parameters; }
        }

        public DataCommandEventArgs (string cmd, DateTime start, DbParam[] pars, bool isReadOnly)
        {
            command = cmd;
            startAt = start;
            parameters = pars;
            this.isReadOnly = isReadOnly;
        }
    }
}
