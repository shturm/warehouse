//
// EmptyDeviceConnector.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   09.02.2013
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
    public class EmptyDeviceConnector : DeviceConnector
    {
        private bool isConnected;

        public override bool IsConnected
        {
            get { return isConnected; }
        }

        public override int BytesToRead
        {
            get { throw new NotImplementedException (); }
        }

        public override int ReadTimeout { get; set; }

        public override int WriteTimeout { get; set; }

        public override void Connect ()
        {
            isConnected = true;
        }

        public override void Disconnect ()
        {
            isConnected = false;
        }

        public override void SendMessage (params byte[] message)
        {
            throw new NotImplementedException ();
        }

        public override byte ReceiveByte ()
        {
            throw new NotImplementedException ();
        }

        public override void Dispose ()
        {
        }
    }
}
