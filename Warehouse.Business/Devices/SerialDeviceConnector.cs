//
// SerialDeviceConnector.cs
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

using System.IO;
using System.IO.Ports;
using System.Text;
using Warehouse.Data;

namespace Warehouse.Business.Devices
{
    public class SerialDeviceConnector : DeviceConnector
    {
        protected const int DEFAULT_READ_TIMEOUT = 1000;
        protected const int DEFAULT_WRITE_TIMEOUT = 1000;

        private SerialPort port;

        public string PortName
        {
            get { return port.PortName; }
            set { port.PortName = value; }
        }

        public int BaudRate
        {
            get { return port.BaudRate; }
            set { port.BaudRate = value; }
        }

        public override bool IsConnected
        {
            get { return port.IsOpen; }
        }

        public override int BytesToRead
        {
            get { return port.BytesToRead; }
        }

        public override int ReadTimeout
        {
            get { return port.ReadTimeout; }
            set { port.ReadTimeout = value; }
        }

        public override int WriteTimeout
        {
            get { return port.WriteTimeout; }
            set { port.WriteTimeout = value; }
        }

        public Parity Parity
        {
            get { return port.Parity; }
            set { port.Parity = value; }
        }

        public byte ParityReplace
        {
            get { return port.ParityReplace; }
            set { port.ParityReplace = value; }
        }

        public Handshake Handshake
        {
            get { return port.Handshake; }
            set { port.Handshake = value; }
        }

        public bool BreakState
        {
            get { return port.BreakState; }
            set { port.BreakState = value; }
        }

        public SerialDeviceConnector ()
        {
            CreatePort ();
        }

        private void CreatePort ()
        {
            port = new SerialPort ();

            //connection settings
            port.Parity = Parity.None;
            port.DataBits = 8;
            port.StopBits = StopBits.One;

            //data format settings
            port.Encoding = Encoding.ASCII;
            port.ReadBufferSize = 500;
            port.WriteBufferSize = 500;

            //additional settings
            port.Handshake = Handshake.None;
            port.RtsEnable = false;
            port.ReadTimeout = DEFAULT_READ_TIMEOUT;
            port.WriteTimeout = DEFAULT_WRITE_TIMEOUT;
            port.DataReceived += port_DataReceived;
        }

        private void RecreatePort ()
        {
            try {
                if (port != null)
                    port.Dispose ();
            } catch (IOException) { }

            CreatePort ();
        }

        private void CheckPort ()
        {
            if (PlatformHelper.IsWindows)
                return;

            if (port == null ||
                string.IsNullOrWhiteSpace (port.PortName) ||
                !File.Exists (port.PortName))
                throw new IOException ("Invalid port name!");
        }

        private void port_DataReceived (object sender, SerialDataReceivedEventArgs e)
        {
            OnDataReceived ();
        }

        public override void Connect ()
        {
            CheckPort ();
            port.Open ();
        }

        public override void Disconnect ()
        {
            CheckPort ();
            port.Close ();
        }

        public void SetPortName (ConnectParametersCollection parameters)
        {
            PortName = (string) parameters [ConnectParameters.SerialPortName];
        }

        public void SetBaudRate (ConnectParametersCollection parameters)
        {
            BaudRate = (int) parameters [ConnectParameters.BaudRate];
        }

        public override void SendMessage (params byte [] message)
        {
            try {
                CheckPort ();

                port.DiscardInBuffer ();
                port.Write (message, 0, message.Length);
            } catch (IOException) {
                RecreatePort ();
                throw;
            }
        }

        public override byte ReceiveByte ()
        {
            try {
                CheckPort ();

                return (byte) port.ReadByte ();
            } catch (IOException) {
                RecreatePort ();
                throw;
            }
        }

        public override void Dispose ()
        {
            port.Dispose ();
        }
    }
}
