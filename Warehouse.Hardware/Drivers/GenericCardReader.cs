//
// GenericCardReader.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   02/28/2008
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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Warehouse.Business.Devices;

#if DEBUG_PORTS
using System.Diagnostics;
#endif

namespace Warehouse.Hardware.Drivers
{
    public abstract class GenericCardReader<TCon> : CardReaderBase<TCon> where TCon : DeviceConnector, new ()
    {
        protected const int PORT_READ_TIMEOUT = 1000;
        protected const int PORT_WRITE_TIMEOUT = 1000;
        protected const int MSG_RECEIVER_POLL_WAIT = 300;
        protected const int MSG_RECEIVE_RETRIES = 10;
        protected const int MSG_RECEIVE_RETRY_WAIT = 500;
        protected const int MSG_SEND_RETRIES = 10;
        protected const int MSG_SEND_RETRY_WAIT = 50;
#if DEBUG
        protected const int MSG_SEND_TIMEOUT = 1000000;
#else
        protected const int MSG_SEND_TIMEOUT = 1000;
#endif

        protected string markerStart = ";";
        protected string markerEnd = "\r\n";
        private readonly AutoResetEvent communucationEvent = new AutoResetEvent (false);
        private Thread messageTransceiver;

        public GenericCardReader ()
        {
            Connector.ReadTimeout = PORT_READ_TIMEOUT;
            Connector.WriteTimeout = PORT_WRITE_TIMEOUT;
            Connector.DataReceived += port_DataReceived;
        }

        #region General commands

        public override void Connect (ConnectParametersCollection parameters)
        {
            try {
                if (!Connector.IsConnected)
                    Connector.Connect ();

                if (messageTransceiver == null) {
                    messageTransceiver = new Thread (MessageTransceiver);
                    messageTransceiver.Start ();
                }
                Initialize ();
            } catch (IOException ioex) {
                throw new HardwareErrorException (new ErrorState (ErrorState.BadSerialPort, HardwareErrorSeverity.Error), ioex);
            } catch (UnauthorizedAccessException uaex) {
                throw new HardwareErrorException (new ErrorState (ErrorState.BadSerialPort, HardwareErrorSeverity.Error), uaex);
            }
        }

        public override void Disconnect ()
        {
            if (Connector != null && Connector.IsConnected)
                Connector.Disconnect ();

            if (messageTransceiver != null) {
                messageTransceiver.Abort ();
                messageTransceiver = null;
            }
        }

        #endregion

        #region Receiving data

        private void port_DataReceived (object sender, EventArgs e)
        {
            communucationEvent.Set ();
        }

        private void MessageTransceiver ()
        {
#if DEBUG
            try {
#endif
            while (true) {
                try {
                    // We use timeout because mono didn't had the DataReceived event implemented
                    // at the time of writing this (mono 1.2.6) so we poll to see if there is any data
                    // At later versions set the poll to infinite.
                    communucationEvent.WaitOne (MSG_RECEIVER_POLL_WAIT, false);
#if DEBUG
                        Console.WriteLine ("> {0} MessageTransceiver activated", DateTime.Now.ToString ("HH:mm:ss"));
#endif
                    if (Connector.BytesToRead == 0)
                        continue;

                    try {
                        string message = ReceiveMessage ();
                        if (cardRecognized != null)
                            cardRecognized (this, new CardReadArgs (message));
                    } catch (CommandNotAcknowledgedException) {
                    }
                } catch (HardwareErrorException) {
                }
            }
#if DEBUG
            } finally {
                Console.WriteLine ("> {0} MessageTransceiver exiting", DateTime.Now.ToString ("HH:mm:ss"));
            }
#endif
        }

        private string ReceiveMessage ()
        {
            List<byte> answer = new List<byte> ();

#if DEBUG
            Console.WriteLine ("> {0} ReceiveMesssage started", DateTime.Now.ToString ("HH:mm:ss"));
#endif

            string ret = string.Empty;
            for (int faults = 0; faults < MSG_RECEIVE_RETRIES; faults++) {
                try {
                    byte inByte = Connector.ReceiveByte ();
#if DEBUG
                    Console.Write (string.Format (" 0x{0}", inByte.ToString ("X2")));
#endif
                    // If we receive something, zero the faults counter
                    faults = 0;
                    answer.Add (inByte);
                    ret = defaultEnc.GetString (answer.ToArray ());

                    if (ret.EndsWith (markerEnd))
                        break;
                } catch (TimeoutException) {
                    break;
                } catch (UnauthorizedAccessException) {
                    Thread.Sleep (MSG_RECEIVE_RETRY_WAIT);
                }
            }

            if (answer.Count == 0) {
                throw new CommandNotAcknowledgedException ("Unable to receive acknoledgement from printer");
            }

            if (ret.StartsWith (markerStart))
                ret = ret.Substring (markerStart.Length);

            if (ret.EndsWith (markerEnd))
                ret = ret.Substring (0, ret.Length - markerEnd.Length);

            return ret;
        }

        #endregion
    }
}
