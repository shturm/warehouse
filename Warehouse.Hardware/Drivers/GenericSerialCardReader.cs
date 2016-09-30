//
// GenericSerialCardReader.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
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

using Mono.Addins;
using Warehouse.Business.Devices;
using Warehouse.Data;

namespace Warehouse.Hardware.Drivers
{
    [Extension ("/Warehouse/Business/Drivers")]
    public class GenericSerialCardReader : GenericCardReader<SerialDeviceConnector>, IAddinDriver
    {
        public override SerializableDictionary<string, object> GetAttributes ()
        {
            return new SerializableDictionary<string, object>
                {
                    { USES_SERIAL_PORT, true },
                    { USES_BAUD_RATE, true }, 
                    { DEFAULT_BAUD_RATE, 9600 }
                };
        }

        public override DeviceInfo [] GetSupportedDevices ()
        {
            return new [] { new DeviceInfo ("Serial Card Reader") };
        }

        public override void Connect (ConnectParametersCollection parameters)
        {
            if (!Connector.IsConnected) {
                Connector.SetPortName (parameters);
                Connector.SetBaudRate (parameters);

                SetEncoding (WINDOWS_CYR_CODE_PAGE);
            }

            base.Connect (parameters);
        }
    }
}
