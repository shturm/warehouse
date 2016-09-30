//
// ErrorStateEventArgs.cs
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

using System;

namespace Warehouse.Business.Devices
{
    public class ErrorStateEventArgs : EventArgs
    {
        private readonly HardwareErrorException exception;
        private readonly ErrorState errState;

        public HardwareErrorException Exception
        {
            get { return exception; }
        }

        public ErrorState ErrState
        {
            get { return errState; }
        }

        public bool Retry { get; set; }

        public ErrorStateEventArgs (ErrorState error)
        {
            errState = error;
            exception = null;
        }

        public ErrorStateEventArgs (HardwareErrorException exception)
        {
            if (exception == null)
                throw new ArgumentNullException ("exception");

            errState = exception.Error;
            this.exception = exception;
        }
    }
}
