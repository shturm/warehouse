//
// DeviceManager.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   12/13/2007
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
using System.IO.Ports;
using System.Linq;
using Mono.Addins;
using Warehouse.Business.Entities;
using Warehouse.Data;

namespace Warehouse.Business
{
    public class DeviceManager : DeviceManagerBase
    {
        protected override void TrackEvent (string category, string eventName)
        {
            BusinessDomain.FeedbackProvider.TrackEvent (category, eventName);
        }

        protected override Type [] GetAllDriverTypes ()
        {
            return AddinManager.GetExtensionNodes ("/Warehouse/Business/Drivers")
                .Cast<TypeExtensionNode> ()
                .Select (node => node.Type)
                .ToArray ();
        }

        public override string [] GetSerialPortNames ()
        {
            if (PlatformHelper.IsWindows)
                return SerialPort.GetPortNames ();

            string [] ttys = Directory.GetFiles ("/dev/", "tty*");
            // The default mono implementation does not include the ttyACM devices, so we have to implement that method ourselves
            bool linuxStyle = ttys.Any (dev => dev.StartsWith ("/dev/ttyS") || dev.StartsWith ("/dev/ttyUSB") || dev.StartsWith ("/dev/ttyACM"));

            return linuxStyle ?
                ttys.Where (dev => dev.StartsWith ("/dev/ttyS") || dev.StartsWith ("/dev/ttyUSB") || dev.StartsWith ("/dev/ttyACM")).ToArray () :
                ttys.Where (dev => dev != "/dev/tty" && dev.StartsWith ("/dev/tty") && !dev.StartsWith ("/dev/ttyC")).ToArray ();
        }
    }
}