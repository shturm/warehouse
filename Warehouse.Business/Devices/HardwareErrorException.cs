//
// HardwareErrorException.cs
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
    public class HardwareErrorException : SystemException
    {
        private readonly ErrorState error;

        public ErrorState Error
        {
            get { return error; }
        }

        public HardwareErrorException (ErrorState error, Exception innerException = null)
            : base (string.Format ("Hardware error occurred ({0})", error), innerException)
        {
            this.error = error;
        }

        public HardwareErrorException (ErrorState error, object command)
            : base (string.Format ("Error while sending command: {0} ({1})", command, error))
        {
            this.error = error;
        }

        public HardwareErrorException (Exception exception)
            : base (exception.Message, exception)
        {
            HardwareErrorException hwEx = exception as HardwareErrorException;
            error = hwEx != null ? hwEx.error : new ErrorState (ErrorState.GeneralError, HardwareErrorSeverity.Error);
        }
    }
}
