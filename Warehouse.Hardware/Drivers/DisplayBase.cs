//
// DisplayBase.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   12/11/2007
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
using System.IO;
using Warehouse.Business.Devices;

namespace Warehouse.Hardware.Drivers
{
    public abstract class DisplayBase<TCon> : DriverBase<TCon>, IExternalDisplayController where TCon : DeviceConnector, new ()
    {
        #region Public properties

        public virtual int DisplayCharsPerLine
        {
            get { return 16; }
        }

        #endregion

        public override void Connect (ConnectParametersCollection parameters)
        {
            try {
                try {
                    if (!Connector.IsConnected)
                        Connector.Connect ();

                    Initialize ();
                } catch (IOException ioex) {
                    throw new HardwareErrorException (new ErrorState (ErrorState.BadSerialPort, HardwareErrorSeverity.Error), ioex);
                } catch (UnauthorizedAccessException uaex) {
                    throw new HardwareErrorException (new ErrorState (ErrorState.BadSerialPort, HardwareErrorSeverity.Error), uaex);
                }
            } catch (Exception) {
                Disconnect ();
                throw;
            }
        }

        public override void Disconnect ()
        {
            if (Connector != null && Connector.IsConnected)
                Connector.Disconnect ();
        }

        public virtual void DisplayClear ()
        {
            throw new NotSupportedException ();
        }

        public virtual void DisplayShowDateTime ()
        {
            throw new NotSupportedException ();
        }

        public virtual void DisplayLowerText (string text)
        {
            throw new NotSupportedException ();
        }

        public virtual void DisplayUpperText (string text)
        {
            throw new NotSupportedException ();
        }
    }
}