//
// DriverInfo.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   03/06/2007
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
using Warehouse.Business.Entities;
using Warehouse.Data;

namespace Warehouse.Business.Devices
{
    public class DriverInfo
    {
        private string driverTypeName;
        private Type driverType;
        private DeviceInfo [] supportedDevices;
        private SerializableDictionary<string, object> attributes;
        private List<DeviceCommands> commands;
        private DeviceType deviceType = DeviceType.None;

        public string DriverTypeName
        {
            get { return driverTypeName; }
        }

        public Type DriverType
        {
            get
            {
                if (driverType == null)
                    driverType = Type.GetType (driverTypeName);

                return driverType;
            }
        }

        public DeviceInfo [] SupportedDevices
        {
            get { return supportedDevices; }
        }

        public SerializableDictionary<string, object> Attributes
        {
            get { return attributes; }
        }

        public List<DeviceCommands> SupportedCommands
        {
            get { return commands; }
        }

        public DeviceType DeviceType
        {
            get
            {
                if (deviceType == DeviceType.None) {
                    if (isCashReceiptPrinterDriver)
                        deviceType |= DeviceType.CashReceiptPrinter;
                    if (isDisplayDriver)
                        deviceType |= DeviceType.ExternalDisplay;
                    if (isKitchenDriver)
                        deviceType |= DeviceType.KitchenPrinter;
                    if (isCardReaderDriver)
                        deviceType |= DeviceType.CardReader;
                    if (isElectronicScaleDriver)
                        deviceType |= DeviceType.ElectronicScale;
                    if (isSalesDataControllerDriver)
                        deviceType |= DeviceType.SalesDataController;
                    if (isCashDrawerDriver)
                        deviceType |= DeviceType.CashDrawer;
                    if (isBarcodeScannerDriver)
                        deviceType |= DeviceType.BarcodeScanner;
                }

                return deviceType;
            }
        }

        public DeviceType DevicePrimaryType
        {
            get
            {
                return Enum.GetValues (typeof (DeviceType))
                    .Cast<DeviceType> ()
                    .FirstOrDefault (type => (DeviceType & type) != DeviceType.None);
            }
        }

        private bool isCashReceiptPrinterDriver;
        public bool IsCashReceiptPrinterDriver
        {
            get { return isCashReceiptPrinterDriver; }
        }

        private bool isCustomerOrderPrinterDriver;
        public bool IsCustomerOrderPrinterDriver
        {
            get { return isCustomerOrderPrinterDriver; }
        }

        private bool isDisplayDriver;
        public bool IsDisplayDriver
        {
            get { return isDisplayDriver; }
        }

        private bool isKitchenDriver;
        public bool IsKitchenDriver
        {
            get { return isKitchenDriver; }
        }

        private bool isCardReaderDriver;
        public bool IsCardReaderDriver
        {
            get { return isCardReaderDriver; }
        }

        private bool isElectronicScaleDriver;
        public bool IsElectronicScaleDriver
        {
            get { return isElectronicScaleDriver; }
        }

        private bool isSalesDataControllerDriver;
        public bool IsSalesDataControllerDriver
        {
            get { return isSalesDataControllerDriver; }
        }

        private bool isCashDrawerDriver;
        public bool IsCashDrawerDriver
        {
            get { return isCashDrawerDriver; }
        }

        private bool isBarcodeScannerDriver;
        public bool IsBarcodeScannerDriver
        {
            get { return isBarcodeScannerDriver; }
        }

        public DriverInfo ()
        {
        }

        public DriverInfo (Type driverType)
        {
            if (driverType == null)
                throw new ArgumentNullException ("driverType");

            Type driverBase = typeof (DriverBase);
            if (!driverBase.IsAssignableFrom (driverType))
                throw new ArgumentException (string.Format ("The type \"{0}\" is not a driver type!", driverType.FullName));

            using (DriverBase driverInstance = (DriverBase) Activator.CreateInstance (driverType)) {
                SetDriverInfo (driverInstance);
            }
        }

        public DriverInfo (DriverBase driverInstance)
        {
            if (driverInstance == null)
                throw new ArgumentNullException ("driverInstance");

            SetDriverInfo (driverInstance);
        }

        private void SetDriverInfo (DriverBase driverInstance)
        {
            driverTypeName = driverInstance.GetType ().AssemblyQualifiedName;
            supportedDevices = driverInstance.GetSupportedDevices ();
            attributes = driverInstance.GetAttributes ();
            commands = driverInstance.SupportedCommands;
            isCustomerOrderPrinterDriver = (driverInstance is ICashReceiptPrinterController &&
                commands.Contains (DeviceCommands.OpenNonFiscal) &&
                (commands.Contains (DeviceCommands.PrintTextNonFiscal) || commands.Contains (DeviceCommands.AddItemNonFiscal)) &&
                commands.Contains (DeviceCommands.CloseNonFiscal));

            isCashReceiptPrinterDriver = (driverInstance is ICashReceiptPrinterController &&
                commands.Contains (DeviceCommands.OpenFiscal) &&
                commands.Contains (DeviceCommands.AddItem) &&
                commands.Contains (DeviceCommands.Payment) &&
                commands.Contains (DeviceCommands.CloseFiscal));

            isDisplayDriver = driverInstance is IExternalDisplayController;
            isKitchenDriver = driverInstance is IKitchenPrinterController;
            isCardReaderDriver = driverInstance is ICardReaderController;
            isElectronicScaleDriver = driverInstance is IElectronicScaleController;
            isSalesDataControllerDriver = driverInstance is ISalesDataController;
            isCashDrawerDriver = commands.Contains (DeviceCommands.OpenCashDrawer);
            isBarcodeScannerDriver = driverInstance is IBarcodeScannerController;
        }
    }
}
