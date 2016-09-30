//
// DriverHelper.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   03/18/2007
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
using System.Linq;

namespace Warehouse.Business.Devices
{
    public static class DriverHelper
    {
        public static event EventHandler<DriversLoadedArgs> DriversLoaded;

        internal static void OnDriversLoaded (List<DriverInfo> allDrivers)
        {
            EventHandler<DriversLoadedArgs> handler = DriversLoaded;
            if (handler != null)
                handler (null, new DriversLoadedArgs { AllDrivers = allDrivers });
        }

        public static string [] GetAllSerialPorts ()
        {
            string [] ports = BusinessDomain.DeviceManager.GetSerialPortNames ();
            Array.Sort (ports);

            return ports;
        }

        public static List<int> GetAllSerialPortSpeeds ()
        {
            return new List<int> { 1200, 2400, 4800, 9600, 14400, 19200, 28800, 33600, 38400, 57600, 115200 };
        }

        public static KeyValuePair<string, int> [] GetAllEncodings ()
        {
            return new []
                {
                    new KeyValuePair<string, int> ("DOS (MIK)", DriverBase.DOS_MIK_CODE_PAGE), 
                    new KeyValuePair<string, int> ("DOS (866)", DriverBase.DOS_RUS_CODE_PAGE), 
                    new KeyValuePair<string, int> ("Windows (1251)", DriverBase.WINDOWS_CYR_CODE_PAGE), 
                    new KeyValuePair<string, int> ("Windows (1250)", DriverBase.WINDOWS_CEE_CODE_PAGE), 
                    new KeyValuePair<string, int> ("Chinese Simplified (GB2312)", DriverBase.CHINESE_SIMPLIFIED_CODE_PAGE)
                };
        }

        public static DriverInfo GetDriverInfoByTypeName (string typeName)
        {
            return GetDriverInfoByType (Type.GetType (typeName));
        }

        public static DriverInfo GetDriverInfoByType (Type type)
        {
            return BusinessDomain.DeviceManager.AllDrivers.FirstOrDefault (driver => Type.GetType (driver.DriverTypeName) == type);
        }
    }
}
