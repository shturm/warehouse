//
// Device.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   06/30/2006
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
using System.Threading;
using System.Xml.Serialization;
using Warehouse.Business.Devices;
using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public class Device : XmlEntityBase<Device>
    {
        #region Private fields

        private static readonly Device staticEntity = new Device ();
        private static readonly Mutex fileLock = new Mutex (false);
        private static Device [] entityCache;
        private static System.Timers.Timer commitTimer;

        #endregion

        #region Public properties

        protected override string EntitiesFile
        {
            get { return StoragePaths.Devices; }
        }

        protected override Device [] EntityCache
        {
            get { return entityCache; }
            set { entityCache = value; }
        }

        protected override Mutex FileLock
        {
            get { return fileLock; }
        }

        protected override bool LazyCommit
        {
            get { return true; }
        }

        protected override System.Timers.Timer CommitTimer
        {
            get { return commitTimer; }
            set { commitTimer = value; }
        }

        private string name = string.Empty;
        [DbColumn (DeviceFields.Name)]
        public string Name
        {
            get { return name; }
            set
            {
                if (name == value) return;

                name = value;
                OnPropertyChanged ("Name");
            }
        }

        private string driverName = string.Empty;
        [DbColumn (DeviceFields.DriverName)]
        public string DriverName
        {
            get
            {
                return string.IsNullOrWhiteSpace (driverName) &&
                    !string.IsNullOrWhiteSpace (deviceMake) &&
                    !string.IsNullOrWhiteSpace (deviceModel) ?
                    string.Format ("{0} {1}", deviceMake, deviceModel) : driverName;
            }
            set
            {
                if (driverName == value) return;

                driverName = value;
                OnPropertyChanged ("DriverName");
            }
        }

        private string driverTypeName = string.Empty;
        [DbColumn (DeviceFields.DriverClass)]
        public string DriverTypeName
        {
            get { return driverTypeName; }
            set
            {
                if (driverTypeName == value) return;

                driverTypeName = value;
                OnPropertyChanged ("DriverTypeName");
            }
        }

        private Type driverType;
        public Type DriverType
        {
            get { return driverType ?? (driverType = Type.GetType (driverTypeName)); }
        }

        private string deviceMake;
        [DbColumn (DeviceFields.DeviceMake)]
        public string DeviceMake
        {
            get { return deviceMake; }
            set
            {
                if (deviceMake == value) return;

                deviceMake = value;
                OnPropertyChanged ("DeviceMake");
            }
        }

        private string deviceModel;
        [DbColumn (DeviceFields.DeviceModel)]
        public string DeviceModel
        {
            get { return deviceModel; }
            set
            {
                if (deviceModel == value) return;

                deviceModel = value;
                OnPropertyChanged ("DeviceModel");
            }
        }

        private string serialPort = string.Empty;
        [DbColumn (DeviceFields.SerialPort)]
        public string SerialPort
        {
            get { return serialPort; }
            set
            {
                if (serialPort == value) return;

                serialPort = value;
                OnPropertyChanged ("SerialPort");
            }
        }

        private string macAddress = string.Empty;
        [DbColumn (DeviceFields.MacAddress)]
        public string MacAddress
        {
            get { return macAddress; }
            set
            {
                if (macAddress == value) return;

                macAddress = value;
                OnPropertyChanged ("MacAddress");
            }
        }

        private int serialPortBaudRate;
        [DbColumn (DeviceFields.SerialPortBaudRate)]
        public int SerialPortBaudRate
        {
            get { return serialPortBaudRate; }
            set
            {
                if (serialPortBaudRate == value) return;

                serialPortBaudRate = value;
                OnPropertyChanged ("SerialPortBaudRate");
            }
        }

        private int serialPortHandshaking;
        [DbColumn (DeviceFields.SerialPortHandshaking)]
        public int SerialPortHandshaking
        {
            get { return serialPortHandshaking; }
            set
            {
                if (serialPortHandshaking == value) return;

                serialPortHandshaking = value;
                OnPropertyChanged ("SerialPortHandshaking");
            }
        }

        private string networkAddress = string.Empty;
        [DbColumn (DeviceFields.NetworkAddress)]
        public string NetworkAddress
        {
            get { return networkAddress; }
            set
            {
                if (networkAddress == value) return;

                networkAddress = value;
                OnPropertyChanged ("NetworkAddress");
            }
        }

        private int networkPort = 1;
        [DbColumn (DeviceFields.NetworkPort)]
        public int NetworkPort
        {
            get { return networkPort; }
            set
            {
                if (networkPort == value) return;

                networkPort = value;
                OnPropertyChanged ("NetworkPort");
            }
        }

        private bool? useDefaultDocumentPrinter;
        [DbColumn (DeviceFields.UseDefaultDocumentPrinter)]
        public bool UseDefaultDocumentPrinter
        {
            get
            {
                return useDefaultDocumentPrinter.HasValue ?
                    useDefaultDocumentPrinter.Value :
                    BusinessDomain.AppConfiguration.UseDefaultDocumentPrinter;
            }
            set
            {
                if (useDefaultDocumentPrinter == value) return;

                useDefaultDocumentPrinter = value;
                OnPropertyChanged ("UseDefaultDocumentPrinter");
            }
        }

        private string documentPrinterName;
        [DbColumn (DeviceFields.DocumentPrinterName)]
        public string DocumentPrinterName
        {
            get
            {
                return string.IsNullOrEmpty (documentPrinterName) ?
                    BusinessDomain.AppConfiguration.DocumentPrinterName :
                    documentPrinterName;
            }
            set
            {
                if (documentPrinterName == value) return;

                documentPrinterName = value;
                OnPropertyChanged ("DocumentPrinterName");
            }
        }

        private bool useCustomLineWidth;
        [DbColumn (DeviceFields.UseCustomLineWidth)]
        public bool UseCustomLineWidth
        {
            get { return useCustomLineWidth; }
            set
            {
                if (useCustomLineWidth == value) return;

                useCustomLineWidth = value;
                OnPropertyChanged ("UseCustomLineWidth");
            }
        }

        private int lineWidth;
        [DbColumn (DeviceFields.LineWidth)]
        public int LineWidth
        {
            get { return lineWidth; }
            set
            {
                if (lineWidth == value) return;

                lineWidth = value;
                OnPropertyChanged ("LineWidth");
            }
        }

        private string imagesFolder = string.Empty;
        [DbColumn (DeviceFields.ImagesFolder)]
        public string ImagesFolder
        {
            get { return imagesFolder; }
            set
            {
                if (imagesFolder == value) return;

                imagesFolder = value;
                OnPropertyChanged ("ImagesFolder");
            }
        }

        private string logicalAddress = string.Empty;
        [DbColumn (DeviceFields.LogicalAddress)]
        public string LogicalAddress
        {
            get { return logicalAddress; }
            set
            {
                if (logicalAddress == value) return;

                logicalAddress = value;
                OnPropertyChanged ("LogicalAddress");
            }
        }

        private string password = string.Empty;
        [DbColumn (DeviceFields.Password)]
        public string Password
        {
            get { return password; }
            set
            {
                if (password == value) return;

                password = value;
                OnPropertyChanged ("Password");
            }
        }

        private string adminPassword = string.Empty;
        [DbColumn (DeviceFields.AdminPassword)]
        public string AdminPassword
        {
            get { return adminPassword; }
            set
            {
                if (adminPassword == value) return;

                adminPassword = value;
                OnPropertyChanged ("AdminPassword");
            }
        }

        private int encoding;
        [DbColumn (DeviceFields.Encoding)]
        public int Encoding
        {
            get { return encoding; }
            set
            {
                if (encoding == value) return;

                encoding = value;
                OnPropertyChanged ("Encoding");
            }
        }

        private int charactersInLine;
        [DbColumn (DeviceFields.CharactersInLine)]
        public int CharactersInLine
        {
            get { return charactersInLine; }
            set
            {
                if (charactersInLine == value) return;

                charactersInLine = value;
                OnPropertyChanged ("CharactersInLine");
            }
        }

        private int blankLinesBeforeCut;
        [DbColumn (DeviceFields.BlankLinesBeforeCut)]
        public int BlankLinesBeforeCut
        {
            get { return blankLinesBeforeCut; }
            set
            {
                if (blankLinesBeforeCut == value) return;

                blankLinesBeforeCut = value;
                OnPropertyChanged ("BlankLinesBeforeCut");
            }
        }

        private bool enabled;
        [DbColumn (DeviceFields.Enabled)]
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (enabled == value) return;

                enabled = value;
                OnPropertyChanged ("Enabled");
            }
        }

        private bool printCashReceipts;
        [DbColumn (DeviceFields.PrintFiscalReceipts)]
        public bool PrintCashReceipts
        {
            get { return printCashReceipts; }
            set
            {
                if (printCashReceipts == value) return;

                printCashReceipts = value;
                OnPropertyChanged ("PrintCashReceipts");
            }
        }

        private bool printCustomerOrders;
        [DbColumn (DeviceFields.PrintNonFiscalReceipts)]
        public bool PrintCustomerOrders
        {
            get { return printCustomerOrders; }
            set
            {
                if (printCustomerOrders == value) return;

                printCustomerOrders = value;
                OnPropertyChanged ("PrintCustomerOrders");
            }
        }

        private bool printKitchenOrders;
        [DbColumn (DeviceFields.PrintKitchenReceipts)]
        public bool PrintKitchenOrders
        {
            get { return printKitchenOrders; }
            set
            {
                if (printKitchenOrders == value) return;

                printKitchenOrders = value;
                OnPropertyChanged ("PrintKitchenOrders");
            }
        }

        private bool displaySaleInfo;
        [DbColumn (DeviceFields.DisplaySaleInfo)]
        public bool DisplaySaleInfo
        {
            get { return displaySaleInfo; }
            set
            {
                if (displaySaleInfo == value) return;

                displaySaleInfo = value;
                OnPropertyChanged ("DisplaySaleInfo");
            }
        }

        private bool readIdCards;
        [DbColumn (DeviceFields.ReadMagneticCards)]
        public bool ReadIdCards
        {
            get { return readIdCards; }
            set
            {
                if (readIdCards == value) return;

                readIdCards = value;
                OnPropertyChanged ("ReadIdCards");
            }
        }

        private bool measureWeight;
        [DbColumn (DeviceFields.MeasureWeight)]
        public bool MeasureWeight
        {
            get { return measureWeight; }
            set
            {
                if (measureWeight == value) return;

                measureWeight = value;
                OnPropertyChanged ("MeasureWeight");
            }
        }

        private bool collectSalesData;
        [DbColumn (DeviceFields.CollectSalesData)]
        public bool CollectSalesData
        {
            get { return collectSalesData; }
            set
            {
                if (collectSalesData == value) return;

                collectSalesData = value;
                OnPropertyChanged ("CollectSalesData");
            }
        }

        private bool scanBarcodes;
        [DbColumn (DeviceFields.ScanBarcodes)]
        public bool ScanBarcodes
        {
            get { return scanBarcodes; }
            set
            {
                if (scanBarcodes == value) return;

                scanBarcodes = value;
                OnPropertyChanged ("ScanBarcodes");
            }
        }

        private bool openDrawer;
        [DbColumn (DeviceFields.OpenDrawer)]
        public bool OpenDrawer
        {
            get { return openDrawer; }
            set
            {
                if (openDrawer == value) return;

                openDrawer = value;
                OnPropertyChanged ("OpenDrawer");
            }
        }

        private bool printTotalOnly;
        [DbColumn (DeviceFields.OpenDrawer)]
        public bool PrintTotalOnly
        {
            get { return printTotalOnly; }
            set
            {
                if (printTotalOnly == value) return;

                printTotalOnly = value;
                OnPropertyChanged ("PrintTotalOnly");
            }
        }

        private bool allItemGroups = true;
        [DbColumn (DeviceFields.AllItemGroups)]
        public bool AllItemGroups
        {
            get { return allItemGroups; }
            set
            {
                if (allItemGroups == value) return;

                allItemGroups = value;
                OnPropertyChanged ("AllItemGroups");
            }
        }

        private SerializableDictionary<long, bool> kitchenItemGroups = new SerializableDictionary<long, bool> ();
        [DbColumn (DeviceFields.KitchenItemGroups)]
        public SerializableDictionary<long, bool> KitchenItemGroups
        {
            get { return kitchenItemGroups; }
            set
            {
                kitchenItemGroups = value;
                OnPropertyChanged ("KitchenItemGroups");
            }
        }

        private DriverInfo driverInfo;
        [XmlIgnore]
        public DriverInfo DriverInfo
        {
            get
            {
                if (driverInfo == null && !string.IsNullOrEmpty (driverTypeName)) {
                    driverInfo = DriverHelper.GetDriverInfoByTypeName (driverTypeName);
                }
                return driverInfo;
            }
            set
            {
                driverInfo = value;
                OnPropertyChanged ("DriverInfo");
            }
        }

        private string drawerCommand;
        [DbColumn (DeviceFields.DrawerCommand)]
        public string DrawerCommand
        {
            get { return drawerCommand; }
            set
            {
                if (drawerCommand == value) return;

                drawerCommand = value;
                OnPropertyChanged ("DrawerCommand");
            }
        }

        private string headerText;
        [DbColumn (DeviceFields.HeaderText)]
        public string HeaderText
        {
            get { return headerText; }
            set
            {
                if (headerText == value) return;

                headerText = value;
                OnPropertyChanged ("HeaderText");
            }
        }

        private string footerText;
        [DbColumn (DeviceFields.FooterText)]
        public string FooterText
        {
            get { return footerText; }
            set
            {
                if (footerText == value) return;

                footerText = value;
                OnPropertyChanged ("FooterText");
            }
        }

        private string operatorCode;
        [DbColumn (DeviceFields.OperatorCode)]
        public string OperatorCode
        {
            get { return operatorCode; }
            set
            {
                if (operatorCode == value) return;

                operatorCode = value;
                OnPropertyChanged ("OperatorCode");
            }
        }

        #endregion

        public override Device CommitChanges ()
        {
            try {
                fileLock.WaitOne ();
                if (printCashReceipts) {
                    Device defFPrinter = GetDefaultCashReceiptPrinter ();
                    if (defFPrinter != null && defFPrinter.id != id) {
                        defFPrinter.PrintCashReceipts = false;
                        defFPrinter.CommitChanges ();
                    }
                }

                if (printCustomerOrders) {
                    Device defNFPrinter = GetDefaultCustomerOrderPrinter ();
                    if (defNFPrinter != null && defNFPrinter.id != id) {
                        defNFPrinter.PrintCustomerOrders = false;
                        defNFPrinter.CommitChanges ();
                    }
                }

                if (displaySaleInfo) {
                    Device defDisplay = GetDefaultDisplay ();
                    if (defDisplay != null && defDisplay.id != id) {
                        defDisplay.DisplaySaleInfo = false;
                        defDisplay.CommitChanges ();
                    }
                }

                if (readIdCards) {
                    Device defCardReader = GetDefaultCardReader ();
                    if (defCardReader != null && defCardReader.id != id) {
                        defCardReader.ReadIdCards = false;
                        defCardReader.CommitChanges ();
                    }
                }

                if (measureWeight) {
                    Device defElectronicScale = GetDefaultElectronicScale ();
                    if (defElectronicScale != null && defElectronicScale.id != id) {
                        defElectronicScale.MeasureWeight = false;
                        defElectronicScale.CommitChanges ();
                    }
                }

                if (collectSalesData) {
                    Device defSalesDataController = GetDefaultSalesDataController ();
                    if (defSalesDataController != null && defSalesDataController.id != id) {
                        defSalesDataController.CollectSalesData = false;
                        defSalesDataController.CommitChanges ();
                    }
                }

                if (scanBarcodes) {
                    Device defBarcodeScanner = GetDefaultBarcodeScanner ();
                    if (defBarcodeScanner != null && defBarcodeScanner.id != id) {
                        defBarcodeScanner.ScanBarcodes = false;
                        defBarcodeScanner.CommitChanges ();
                    }
                }

                base.CommitChanges ();
            } catch (InvalidOperationException) {
            } finally {
                fileLock.ReleaseMutex ();
            }

            BusinessDomain.AppConfiguration.CashReceiptPrinterEnabled = GetDefaultCashReceiptPrinter () != null;
            BusinessDomain.AppConfiguration.CustomerOrdersPrinterEnabled = GetDefaultCustomerOrderPrinter () != null;
            BusinessDomain.AppConfiguration.ExternalDisplayEnabled = GetDefaultDisplay () != null;
            BusinessDomain.AppConfiguration.CardReaderEnabled = GetDefaultCardReader () != null;
            BusinessDomain.AppConfiguration.ElectronicScaleEnabled = GetDefaultElectronicScale () != null;

            return this;
        }

        protected override Device [] GetAllEntities (bool clone)
        {
            List<Device> ret = new List<Device> (base.GetAllEntities (clone));
            for (int i = ret.Count - 1; i >= 0; i--) {
                if (string.IsNullOrEmpty (ret [i].driverTypeName))
                    ret.RemoveAt (i);
            }

            return ret.ToArray ();
        }

        public static DeletePermission RequestDelete (long deviceId)
        {
            if (deviceId < 0)
                return DeletePermission.Yes;

            Device device = GetById (deviceId);
            if (device == null)
                return DeletePermission.Yes;

            Device defaultDevice = GetDefaultCashReceiptPrinter ();
            if (defaultDevice != null && device.id == defaultDevice.id)
                return DeletePermission.InUse;

            defaultDevice = GetDefaultDisplay ();
            if (defaultDevice != null && device.id == defaultDevice.id)
                return DeletePermission.InUse;

            defaultDevice = GetDefaultCardReader ();
            if (defaultDevice != null && device.id == defaultDevice.id)
                return DeletePermission.InUse;

            defaultDevice = GetDefaultSalesDataController ();
            if (defaultDevice != null && device.id == defaultDevice.id)
                return DeletePermission.InUse;

            return DeletePermission.Yes;
        }

        public static void Delete (long deviceId)
        {
            staticEntity.DeleteEntity (deviceId);
        }

        public static Device [] GetAll ()
        {
            return staticEntity.GetAllEntities (false);
        }

        public static Device [] GetAllByType (DeviceType type)
        {
            List<Device> ret = new List<Device> ();

            foreach (Device device in staticEntity.GetAllEntities (true)) {
                DriverInfo dInfo = device.DriverInfo;
                if (dInfo == null)
                    continue;

                if ((dInfo.DeviceType & type) == type)
                    ret.Add (device);
            }

            return ret.ToArray ();
        }

        public static DeviceType [] GetAllUsedTypes ()
        {
            DeviceType allTypes = GetAll ()
                .Select (device => device.DriverInfo)
                .Where (dInfo => dInfo != null)
                .Aggregate (DeviceType.None, (current, dInfo) => current | dInfo.DeviceType);

            return Enum.GetValues (typeof (DeviceType))
                .Cast<DeviceType> ()
                .Where (type => (allTypes & type) != DeviceType.None).ToArray ();
        }

        public static Device GetById (long deviceId)
        {
            return staticEntity.GetSingleEntity (new Dictionary<string, object>
                {
                    { XmlEntityFields.Id, deviceId } 
                });
        }

        public static Device GetByName (string deviceName)
        {
            return staticEntity.GetSingleEntity (new Dictionary<string, object>
                {
                    { DeviceFields.Name, deviceName} 
                });
        }

        public static Device GetBySerialPort (string serialPort)
        {
            return staticEntity
                .GetEntities (new Dictionary<string, object> { { DeviceFields.SerialPort, serialPort } })
                .FirstOrDefault (d => d.DriverInfo != null && d.DriverInfo.Attributes.ContainsKey (DriverBase.USES_SERIAL_PORT));
        }

        public static Device GetDefaultCashReceiptPrinter ()
        {
            return staticEntity.GetSingleEntity (new Dictionary<string, object>
                {
                    { DeviceFields.PrintFiscalReceipts, true }, 
                    { DeviceFields.Enabled, true } 
                });
        }

        public static Device GetDefaultCustomerOrderPrinter ()
        {
            return staticEntity.GetSingleEntity (new Dictionary<string, object>
                {
                    { DeviceFields.PrintNonFiscalReceipts, true }, 
                    { DeviceFields.Enabled, true } 
                });
        }

        public static Device GetDefaultDisplay ()
        {
            return staticEntity.GetSingleEntity (new Dictionary<string, object>
                {
                    { DeviceFields.DisplaySaleInfo, true },
                    { DeviceFields.Enabled, true }
                });
        }

        public static Device GetDefaultCardReader ()
        {
            return staticEntity.GetSingleEntity (new Dictionary<string, object>
                {
                    { DeviceFields.ReadMagneticCards, true },
                    { DeviceFields.Enabled, true } 
                });
        }

        public static Device GetDefaultElectronicScale ()
        {
            return staticEntity.GetSingleEntity (new Dictionary<string, object>
                {
                    { DeviceFields.MeasureWeight, true },
                    { DeviceFields.Enabled, true } 
                });
        }

        public static Device [] GetAllKitchenPrinters ()
        {
            Dictionary<string, object> criteria = new Dictionary<string, object>
                {
                    { DeviceFields.PrintKitchenReceipts, true },
                    { DeviceFields.Enabled, true } 
                };

            return staticEntity.GetEntities (criteria);
        }

        public static Device GetDefaultSalesDataController ()
        {
            return staticEntity.GetSingleEntity (new Dictionary<string, object>
                {
                    { DeviceFields.CollectSalesData, true }, 
                    { DeviceFields.Enabled, true } 
                });
        }

        public static Device GetDefaultBarcodeScanner ()
        {
            return staticEntity.GetSingleEntity (new Dictionary<string, object>
                {
                    { DeviceFields.ScanBarcodes, true }, 
                    { DeviceFields.Enabled, true } 
                });
        }

        public ConnectParametersCollection GetParameters ()
        {
            return new ConnectParametersCollection
                {
                    { ConnectParameters.SerialPortName, serialPort },
                    { ConnectParameters.BaudRate, serialPortBaudRate },
                    { ConnectParameters.Handshaking, serialPortHandshaking },
                    { ConnectParameters.NetworkAddress, networkAddress },
                    { ConnectParameters.NetworkPort, networkPort },
                    { ConnectParameters.MacAddress, macAddress },
                    { ConnectParameters.UseDefaultDocumentPrinter, UseDefaultDocumentPrinter },
                    { ConnectParameters.DocumentPrinterName, documentPrinterName },
                    { ConnectParameters.UseCustomLineWidth, useCustomLineWidth },
                    { ConnectParameters.LineWidth, lineWidth },
                    { ConnectParameters.ImagesFolder, imagesFolder },
                    { ConnectParameters.LogicalAddress, logicalAddress },
                    { ConnectParameters.Password, password },
                    { ConnectParameters.AdminPassword, adminPassword },
                    { ConnectParameters.Encoding, encoding },
                    { ConnectParameters.CharactersInLine, charactersInLine },
                    { ConnectParameters.BlankLinesBeforeCut, blankLinesBeforeCut },
                    { ConnectParameters.DrawerCommand, drawerCommand },
                    { ConnectParameters.OpenDrawer, openDrawer },
                    { ConnectParameters.HeaderText, headerText },
                    { ConnectParameters.FooterText, footerText },
                    { ConnectParameters.PrintTotalOnly, printTotalOnly },
                    { ConnectParameters.OperatorCode, operatorCode },
                };
        }

        public string GetPortAssignment ()
        {
            SerializableDictionary<string, object> attributes = DriverInfo.Attributes;

            if (attributes.ContainsKey (DriverBase.USES_SERIAL_PORT))
                return serialPort;

            if (attributes.ContainsKey (DriverBase.USES_NETWORK_ADDRESS)) {
                string port = networkAddress;
                if (attributes.ContainsKey (DriverBase.USES_NETWORK_PORT))
                    port += ":" + networkPort;

                return port;
            }

            if (attributes.ContainsKey (DriverBase.USES_BLUETOOTH))
                return macAddress;

            object customPort;
            if (attributes.TryGetValue (DriverBase.CUSTOM_PORT, out customPort))
                return (string) customPort;

            return null;
        }

        public static void Flush ()
        {
            staticEntity.FlushCache ();
        }
    }
}
