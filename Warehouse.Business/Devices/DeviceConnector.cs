//
// DeviceConnector.cs
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
    public abstract class DeviceConnector : IDisposable
    {
        public event EventHandler DataReceived;

        public abstract bool IsConnected { get; }
        public abstract int BytesToRead { get; }
        public abstract int ReadTimeout { get; set; }
        public abstract int WriteTimeout { get; set; }

        protected int maxSendRetries = 3;
        public int MaxSendRetries
        {
            get { return maxSendRetries; }
            set { maxSendRetries = value; }
        }

        protected int maxReceiveRetries = 2;
        public int MaxReceiveRetries
        {
            get { return maxReceiveRetries; }
            set { maxReceiveRetries = value; }
        }

        protected int sendRetryWait = 100;
        public int SendRetryWait
        {
            get { return sendRetryWait; }
            set { sendRetryWait = value; }
        }

        protected int receiveRetryWait = 100;
        public int ReceiveRetryWait
        {
            get { return receiveRetryWait; }
            set { receiveRetryWait = value; }
        }

        public abstract void Connect ();
        public abstract void Disconnect ();
        public abstract void SendMessage (params byte [] message);
        public abstract byte ReceiveByte ();

        protected void OnDataReceived ()
        {
            if (DataReceived != null)
                DataReceived (this, EventArgs.Empty);
        }

        #region IDisposable Members

        public abstract void Dispose ();

        #endregion
    }
}
