//
// EthernetDeviceConnector.cs
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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Warehouse.Business.Devices
{
    public class EthernetDeviceConnector : DeviceConnector
    {
        private Socket socket;
        private string networkAddress;
        private int networkPort;
        private bool isConnected;
        private int connectTimeout = 5000;
        private int receiveTimeout = 5000;
        private int sendTimeout = 5000;
#if DEBUG
        private const int autoDisconnectTimeout = 100000;
#else
        private const int autoDisconnectTimeout = 1000;
#endif
        private Timer autoDisconnectTimer;
        private readonly object autoDisconnectLock = new object ();
        private readonly AutoResetEvent connectEvent = new AutoResetEvent (false);
        private bool autoDisconnect = true;

        public string NetworkAddress
        {
            get { return networkAddress; }
            set { networkAddress = value; }
        }

        public int NetworkPort
        {
            get { return networkPort; }
            set { networkPort = value; }
        }

        public override bool IsConnected
        {
            get { return isConnected; }
        }

        public bool IsSocketConnected
        {
            get { return socket != null && socket.Connected; }
        }

        public override int BytesToRead
        {
            get { return socket == null ? 0 : socket.Available; }
        }

        public int ConnectTimeout
        {
            get { return connectTimeout; }
            set { connectTimeout = value; }
        }

        public override int ReadTimeout
        {
            get { return receiveTimeout; }
            set
            {
                receiveTimeout = value;
                if (socket != null)
                    socket.ReceiveTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get { return sendTimeout; }
            set
            {
                sendTimeout = value;
                if (socket != null)
                    socket.SendTimeout = value;
            }
        }

        public bool AutoDisconnect
        {
            get { return autoDisconnect; }
            set
            {
                if (autoDisconnect == value)
                    return;

                autoDisconnect = value;
                lock (autoDisconnectLock) {
                    if (value || autoDisconnectTimer == null)
                        return;
                    
                    autoDisconnectTimer.Change (Timeout.Infinite, Timeout.Infinite);
                    autoDisconnectTimer.Dispose ();
                    autoDisconnectTimer = null;
                }

            }
        }

        public EthernetDeviceConnector ()
        {
            maxSendRetries = 1;
            maxReceiveRetries = 1;
        }

        public override void Connect ()
        {
            socket = null;

            IPAddress [] ipAddresses;
            IPAddress ip;
            if (IPAddress.TryParse (networkAddress, out ip))
                ipAddresses = new [] { ip };
            else {
                // Get host related information.
                IPHostEntry hostEntry = Dns.GetHostEntry (networkAddress);
                ipAddresses = hostEntry.AddressList;
            }

            // Loop through the AddressList to obtain the supported AddressFamily. This is to avoid
            // an exception that occurs when the host IP Address is not compatible with the address family
            // (typical in the IPv6 case).
            foreach (IPAddress address in ipAddresses) {
                IPEndPoint ipe = new IPEndPoint (address, networkPort);
                Socket tempSocket = new Socket (ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                tempSocket.BeginConnect (ipe, ar => connectEvent.Set (), null);
                connectEvent.WaitOne (connectTimeout, false);

                if (tempSocket.Connected) {
                    socket = tempSocket;
                    break;
                }
                tempSocket.Close ();
            }

            if (socket != null) {
                socket.ReceiveTimeout = receiveTimeout;
                socket.SendTimeout = sendTimeout;
                RestartAutoDisconnect ();
            } else
                throw new SocketException ();

            isConnected = true;
        }

        private void RestartAutoDisconnect ()
        {
            lock (autoDisconnectLock) {
                if (!IsSocketConnected || !autoDisconnect)
                    return;

                if (autoDisconnectTimer == null)
                    autoDisconnectTimer = new Timer (AutoDisconnectCallback, null, autoDisconnectTimeout, Timeout.Infinite);
                else
                    autoDisconnectTimer.Change (autoDisconnectTimeout, Timeout.Infinite);
            }
        }

        private void AutoDisconnectCallback (object state)
        {
            lock (autoDisconnectLock) {
                autoDisconnectTimer = null;
                try {
                    Disconnect ();
                } catch (Exception) {
                    return;
                }
            }
        }

        public void SetNetworkAddress (ConnectParametersCollection parameters)
        {
            NetworkAddress = (string) parameters [ConnectParameters.NetworkAddress];
        }

        public void SetNetworkPort (ConnectParametersCollection parameters)
        {
            NetworkPort = (int) parameters [ConnectParameters.NetworkPort];
        }

        public override void Disconnect ()
        {
            if (IsSocketConnected)
                socket.Disconnect (true);
        }

        public override void SendMessage (params byte [] message)
        {
            lock (autoDisconnectLock) {
                if (!IsSocketConnected)
                    Connect ();

                if (socket != null)
                    socket.Send (message);
            }
        }

        public override byte ReceiveByte ()
        {
            lock (autoDisconnectLock) {
                if (!IsSocketConnected)
                    Connect ();

                byte [] buffer = new byte [1];
                socket.Receive (buffer, 1, SocketFlags.Partial);
                return buffer [0];
            }
        }

        public override void Dispose ()
        {
        }
    }
}
