//
// EscPOSPrinter.cs
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
using System.Collections.Generic;
using System.Threading;
using Warehouse.Business.Devices;
using Warehouse.Data;

#if DEBUG_PORTS
using System.Diagnostics;
using System.Reflection;
#endif

namespace Warehouse.Hardware.Drivers
{
    public abstract class EscPOSPrinter<TCon> : DriverBase<TCon> where TCon : DeviceConnector, new ()
    {
        protected const int MSG_SEND_RETRIES = 3;
        protected const int MSG_SEND_RETRY_WAIT = 100;
        protected const int MSG_RECEIVE_RETRIES = 2;
        protected const int MSG_RECEIVE_RETRY_WAIT = 100;

        protected int textCharsPerLine = 23;
        protected int dotsPerLine = 384;
        protected byte [] receivedMessage;

        public enum PrintingDensity
        {
            Normal,
            High
        }

        public int TextCharsPerLine
        {
            get { return textCharsPerLine; }
        }

        public int DotsPerLine
        {
            get { return dotsPerLine; }
            set { dotsPerLine = value; }
        }

        public override void Initialize ()
        {
            if (SupportedCommands.Contains (DeviceCommands.GetStatus))
                GetStatus ();
        }

        public override void Connect (ConnectParametersCollection parameters)
        {
            try {
                if (!Connector.IsConnected) {
                    Connector.Connect ();
#if DEBUG_PORTS
                    Debug.WriteLine ("Connector: OPENED:");
                    Debug.WriteLine ("Connector: start of call stack");
                    StackTrace trace = new StackTrace ();
                    foreach (StackFrame frame in trace.GetFrames ()) {
                        MethodBase meth = frame.GetMethod ();
                        Debug.WriteLine (meth.DeclaringType.FullName + "." + meth.Name);
                    }
                    Debug.WriteLine ("Connector: end of call stack");
#endif
                }

                Initialize ();
            } catch (Exception ex) {
                Disconnect ();
                throw new HardwareErrorException (new ErrorState (ErrorState.KitchenPrinterDisconnected, HardwareErrorSeverity.Error), ex);
            }
        }

        public override void GetStatus ()
        {
            SendMessage (1, 0x10, 0x04, 0x04);
            if ((receivedMessage [0] & 0x60) == 0)
                return;

            lastErrorState.SetError (ErrorState.KitchenPrinterNoPaper);
            throw new HardwareErrorException (lastErrorState);
        }

        public virtual void PrintFreeText (string text)
        {
            if (SupportedCommands.Contains (DeviceCommands.GetStatus))
                GetStatus ();

            text = text.Replace ("\r\n", "\n");
            text = text.Replace ("\r", "\n");
            text = string.Join ("\n", text.Wrap (textCharsPerLine, 20));
            text += "\n";

            SendMessage (GetTextBytes (text));
        }

        public virtual void PaperFeed ()
        {
            if (SupportedCommands.Contains (DeviceCommands.GetStatus))
                GetStatus ();

            SendMessage (defaultEnc.GetBytes (" \n"));
        }

        public virtual void PaperCut ()
        {
            if (SupportedCommands.Contains (DeviceCommands.GetStatus))
                GetStatus ();

            SendMessage (defaultEnc.GetBytes ("\n \n \n \n\x1B\x69"));
        }

        #region Messsages handling

        protected void SendMessage (params byte [] packedCommand)
        {
            SendMessage (0, packedCommand);
        }

        protected void SendMessage (int answerLen, params byte [] packedCommand)
        {
            lock (syncRoot) {
                lastErrorState.Clear ();
                for (int i = 0; i < MSG_SEND_RETRIES; i++) {
                    try {
                        Connector.SendMessage (packedCommand);

                        if (answerLen == 0)
                            return;

                        receivedMessage = ReceiveMessage (answerLen);
                        return;
                    } catch (CommandNotAcknowledgedException) {
                    } catch (TimeoutException) {
                    }

                    // NACK or no answer received so wait a little untill the next retry
                    Thread.Sleep (MSG_SEND_RETRY_WAIT);
                }
            }

            lastErrorState.SetError (ErrorState.KitchenPrinterDisconnected);
            throw new HardwareErrorException (lastErrorState);
        }

        private byte [] ReceiveMessage (int answerLen)
        {
            List<byte> answer = new List<byte> ();

            for (int faults = 0; faults < MSG_RECEIVE_RETRIES; ) {
                try {
                    byte inByte = Connector.ReceiveByte ();

                    // If we receive something, zero the faults counter
                    faults = 0;
                    answer.Add (inByte);
                } catch (TimeoutException) {
                    Thread.Sleep (MSG_RECEIVE_RETRY_WAIT);
                    faults++;
                } catch (UnauthorizedAccessException) {
                    Thread.Sleep (MSG_RECEIVE_RETRY_WAIT);
                    faults++;
                }

                if (answer.Count >= answerLen)
                    break;
            }

            if (answer.Count < answerLen) {
                throw new CommandNotAcknowledgedException ("Unable to receive acknoledgement from printer");
            }

            return answer.ToArray ();
        }

        #endregion

        public override void Disconnect ()
        {
            if (Connector != null && Connector.IsConnected) {
                Connector.Disconnect ();
#if DEBUG_PORTS
                Debug.WriteLine ("Connector: CLOSED");
                Debug.WriteLine ("Connector: start of call stack");
                StackTrace trace = new StackTrace ();
                foreach (StackFrame frame in trace.GetFrames ()) {
                    MethodBase meth = frame.GetMethod ();
                    Debug.WriteLine (meth.DeclaringType.FullName + "." + meth.Name);
                }
                Debug.WriteLine ("Connector: end of call stack");
#endif
            }
        }
    }
}
