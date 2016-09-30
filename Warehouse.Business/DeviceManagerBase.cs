//
// DeviceManagerBase.cs
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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Data;

namespace Warehouse.Business
{
    [Flags]
    public enum FinalizeAction
    {
        None = 0,
        PrintCashReceipt = 1,
        PrintCustomerOrder = 2,
        PrintKitchen = 4,
        PrintAny = 7,
        CommitSale = 8,
        CommitDocument = 16,
        CommitOrder = 32,
        CollectSaleData = 64,
        PrintCashReceiptInvoice = 128,
        PrintCustomerOrderInvoice = 256,
    }

    public abstract class DeviceManagerBase : IDisposable
    {
        private const int DEVICE_STATUS_POLL_INTERVAL = 3000;
        private const int DISPLAY_WELCOME_WAIT = 10000;

        private class ReceiptInfo
        {
            public IKitchenPrinterController KitchenPrinter { get; set; }
            public ICashReceiptPrinterController CustomerOrderPrinter { get; set; }
            public Sale Sale { get; set; }
            public Sale DeltaSale { get; set; }
        }

        private readonly Dictionary<string, IConnectableDevice> portAssignments = new Dictionary<string, IConnectableDevice> ();
        private readonly List<ReceiptInfo> printedKitchenReceipts = new List<ReceiptInfo> ();
        private readonly Dictionary<long, Device> failedDevices = new Dictionary<long, Device> ();
        private bool checkFailedDevices;
        private Type [] allDriverTypes;
        private DriverInfo [] allDrivers;

        public event EventHandler CashReceiptPrinterChanged;
        public event EventHandler ElectronicScaleChanged;
        public event EventHandler<CardReadArgs> CardRecognized;
        public event EventHandler<BarcodeScannedArgs> BarcodeScanned;
        public event EventHandler<KitchenPrinterErrorEventArgs> KitchenPrinterError;
        public event EventHandler<ProgressStartEventArgs> ReceiptPrintStart;
        public event EventHandler<ProgressStepEventArgs> ReceiptPrintStep;
        public event EventHandler ReceiptPrintDialogShown;
        public event EventHandler ReceiptPrintEnd;

        public DeviceManagerBase ()
        {
            DeviceWorkerStart ();
        }

        protected virtual void TrackEvent (string category, string eventName)
        {
        }

        #region Devices connect and disconnect

        public void InitBasicHardware ()
        {
            InitCardReader ();
            InitCashReceiptPrinter (true);
        }

        public void InitAllHardware ()
        {
            try {
                checkFailedDevices = true;
                failedDevices.Clear ();
                InitCashReceiptPrinter ();
                InitCustomerOrderPrinter ();
                InitExternalDisplay ();
                InitElectronicScale ();
                InitKitchenPrinters ();
                InitSalesDataController ();
                InitBarcodeScanner ();
            } finally {
                checkFailedDevices = false;
                failedDevices.Clear ();
            }
        }

        public void ReinitializeHardware (Device oldItem, Device newItem,
            Action onCashReceiptPrinterError,
            Action onCustomerOrderPrinterError,
            Action onDisplayError,
            Action onCardReaderError,
            Action onScaleError,
            Action onSDCError,
            Action onKitchenPrinterError,
            Action onBarcodeScannerError)
        {
            bool saveOk = true;

            try {
                if ((oldItem != null && oldItem.PrintCashReceipts) ||
                    (newItem != null && newItem.PrintCashReceipts)) {
                    DeinitCashReceiptPrinter ();
                    if (!InitCashReceiptPrinter ()) {
                        onCashReceiptPrinterError ();
                        saveOk = false;
                        return;
                    }
                }

                if ((oldItem != null && oldItem.PrintCustomerOrders) ||
                    (newItem != null && newItem.PrintCustomerOrders)) {
                    if (!InitCustomerOrderPrinter ()) {
                        onCustomerOrderPrinterError ();
                        saveOk = false;
                        return;
                    }
                }

                if ((oldItem != null && oldItem.DisplaySaleInfo) ||
                    (newItem != null && newItem.DisplaySaleInfo)) {
                    if (!InitExternalDisplay ()) {
                        onDisplayError ();
                        saveOk = false;
                        return;
                    }
                }

                if ((oldItem != null && oldItem.ReadIdCards) ||
                    (newItem != null && newItem.ReadIdCards)) {
                    if (!InitCardReader ()) {
                        onCardReaderError ();
                        saveOk = false;
                        return;
                    }
                }

                if ((oldItem != null && oldItem.MeasureWeight) ||
                    (newItem != null && newItem.MeasureWeight)) {
                    if (!InitElectronicScale ()) {
                        onScaleError ();
                        saveOk = false;
                        return;
                    }
                }

                if ((oldItem != null && oldItem.CollectSalesData) ||
                    (newItem != null && newItem.CollectSalesData)) {
                    if (!InitSalesDataController ()) {
                        onSDCError ();
                        saveOk = false;
                        return;
                    }
                }

                if ((oldItem != null && oldItem.PrintKitchenOrders) ||
                    (newItem != null && newItem.PrintKitchenOrders)) {
                    if (!InitKitchenPrinters ()) {
                        onKitchenPrinterError ();
                        saveOk = false;
                        return;
                    }
                }

                if ((oldItem != null && oldItem.ScanBarcodes) ||
                    (newItem != null && newItem.ScanBarcodes)) {
                    if (!InitBarcodeScanner ()) {
                        onBarcodeScannerError ();
                        saveOk = false;
                        return;
                    }
                }
            } finally {
                if (!saveOk) {
                    newItem.Enabled = false;
                    newItem.CommitChanges ();
                }
            }
        }

        public void DisconnectConflictingDevices (Device d)
        {
            string portAssignment = d.GetPortAssignment ();
            if (string.IsNullOrWhiteSpace (portAssignment))
                return;

            if (CheckForPortConflict (Device.GetDefaultCardReader (), portAssignment))
                DisconnectCardReader ();

            if (CheckForPortConflict (Device.GetDefaultDisplay (), portAssignment))
                DisconnectExternalDisplay ();

            if (CheckForPortConflict (Device.GetDefaultCustomerOrderPrinter (), portAssignment))
                DisconnectCustomerOrderPrinter ();

            if (CheckForPortConflict (Device.GetDefaultCashReceiptPrinter (), portAssignment))
                DisconnectCashReceiptPrinter ();

            if (CheckForPortConflict (Device.GetDefaultSalesDataController (), portAssignment))
                DisconnectSalesDataController ();

            if (Device.GetAllKitchenPrinters ().Any (printer => CheckForPortConflict (printer, portAssignment)))
                DisconnectKitchenPrinters ();

            if (CheckForPortConflict (Device.GetDefaultElectronicScale (), portAssignment))
                DisconnectElectronicScale ();

            if (CheckForPortConflict (Device.GetDefaultBarcodeScanner (), portAssignment))
                DisconnectBarcodeScanner ();
        }

        private static bool CheckForPortConflict (Device configuredDevice, string portAssignment)
        {
            if (configuredDevice == null || configuredDevice.DriverInfo == null)
                return false;

            return configuredDevice.GetPortAssignment () == portAssignment;
        }

        #region Cash receipt printer connection

        private static readonly object cashReceiptPrinterDriverLock = new object ();
        private ICashReceiptPrinterController cashReceiptPrinterDriver;

        public ICashReceiptPrinterController CashReceiptPrinterDriver
        {
            get { return cashReceiptPrinterDriver; }
        }

        public bool CashReceiptPrinterConnected
        {
            get { return cashReceiptPrinterDriver != null; }
        }

        public bool InitCashReceiptPrinter (bool silent = false)
        {
            try {
                if (CashReceiptPrinterConnected)
                    return true;

                BusinessDomain.AppConfiguration.CashReceiptPrinterEnabled = Device.GetDefaultCashReceiptPrinter () != null;
                if (!BusinessDomain.AppConfiguration.CashReceiptPrinterEnabled)
                    return true;

                bool retry;
                do {
                    retry = false;
                    try {
                        ConnectCashReceiptPrinter ();
                    } catch (HardwareErrorException ex) {
                        if (silent)
                            throw;

                        retry = OnHardwareError (ex);
                        if (!retry)
                            throw;
                    }
                } while (retry);

                ICashReceiptPrinterController driverCopy = cashReceiptPrinterDriver;
                if (driverCopy != null)
                    TrackEvent ("Connected cash receipt printer", driverCopy.GetType ().Name);
                return true;
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                return false;
            }
        }

        public void ConnectCashReceiptPrinter ()
        {
            Device device = Device.GetDefaultCashReceiptPrinter ();
            if (device == null || device.DriverInfo == null)
                return;

            ConnectCashReceiptPrinter (device);
            OnCashReceiptPrinterChanged ();
        }

        private void ConnectCashReceiptPrinter (Device device)
        {
            if (cashReceiptPrinterDriver != null)
                throw new ConnectFailedException ("Cash receipt printer is already connected!");

            if (!device.DriverInfo.IsCashReceiptPrinterDriver)
                throw new ConnectFailedException ("The device driver does not support fiscal printing!");

            IConnectableDevice connectable;
            try {
                connectable = ConnectDevice (device);
            } catch (SocketException socex) {
                throw new HardwareErrorException (new ErrorState (ErrorState.CashReceiptPrinterDisconnected, HardwareErrorSeverity.Error), socex);
            }

            lock (cashReceiptPrinterDriverLock)
                cashReceiptPrinterDriver = connectable as ICashReceiptPrinterController;
        }

        public void DeinitCashReceiptPrinter ()
        {
            try {
                DisconnectCashReceiptPrinter ();
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
            }
        }

        public void DisconnectCashReceiptPrinter ()
        {
            if (cashReceiptPrinterDriver == null)
                return;

            DisconnectDevice (cashReceiptPrinterDriver);
            lock (cashReceiptPrinterDriverLock)
                cashReceiptPrinterDriver = null;

            OnCashReceiptPrinterChanged ();
        }

        #endregion

        #region Customer order printer connection

        private static readonly object customerOrderDriverLock = new object ();
        private IConnectableDevice customerOrderDriver;

        public IConnectableDevice CustomerOrderPrinter
        {
            get { return customerOrderDriver; }
        }

        public bool CustomerOrderPrinterConnected
        {
            get { return customerOrderDriver != null; }
        }

        public bool InitCustomerOrderPrinter ()
        {
            try {
                DisconnectCustomerOrderPrinter ();

                BusinessDomain.AppConfiguration.CustomerOrdersPrinterEnabled = Device.GetDefaultCustomerOrderPrinter () != null;
                if (!BusinessDomain.AppConfiguration.CustomerOrdersPrinterEnabled)
                    return true;

                bool retry;
                do {
                    retry = false;
                    try {
                        ConnectCustomerOrderPrinter ();
                    } catch (HardwareErrorException ex) {
                        retry = OnHardwareError (ex);
                        if (!retry)
                            throw;
                    }
                } while (retry);

                IConnectableDevice driverCopy = customerOrderDriver;
                if (driverCopy != null)
                    TrackEvent ("Connected customer order printer", driverCopy.GetType ().Name);
                return true;
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                return false;
            }
        }

        public void ConnectCustomerOrderPrinter ()
        {
            Device device = Device.GetDefaultCustomerOrderPrinter ();
            if (device == null || device.DriverInfo == null)
                return;

            ConnectCustomerOrderPrinter (device);
        }

        private void ConnectCustomerOrderPrinter (Device device)
        {
            if (customerOrderDriver != null)
                throw new ConnectFailedException ("Customer order printer is already connected!");

            if (!device.DriverInfo.IsCustomerOrderPrinterDriver && !device.DriverInfo.IsKitchenDriver)
                throw new ConnectFailedException ("The device driver does not support printing!");

            IConnectableDevice connectable;
            try {
                connectable = ConnectDevice (device);
            } catch (SocketException socex) {
                throw new HardwareErrorException (new ErrorState (ErrorState.NonFiscalPrinterDisconnected, HardwareErrorSeverity.Error), socex);
            }

            lock (customerOrderDriverLock)
                customerOrderDriver = connectable;
        }

        public void DisconnectCustomerOrderPrinter ()
        {
            if (customerOrderDriver == null)
                return;

            DisconnectDevice (customerOrderDriver);
            lock (customerOrderDriverLock)
                customerOrderDriver = null;
        }

        #endregion

        #region External display connection

        private static readonly object displayDriverLock = new object ();
        private IExternalDisplayController displayDriver;

        public bool ExternalDisplayConnected
        {
            get { return displayDriver != null; }
        }

        public bool InitExternalDisplay ()
        {
            try {
                DisconnectExternalDisplay ();

                BusinessDomain.AppConfiguration.ExternalDisplayEnabled = Device.GetDefaultDisplay () != null;
                if (!BusinessDomain.AppConfiguration.ExternalDisplayEnabled)
                    return true;

                bool retry;
                do {
                    retry = false;
                    try {
                        ConnectExternalDisplay ();
                    } catch (HardwareErrorException ex) {
                        retry = OnHardwareError (ex);
                        if (!retry)
                            throw;
                    }
                } while (retry);

                IExternalDisplayController displayDriverCopy = displayDriver;
                if (displayDriverCopy != null)
                    TrackEvent ("Connected display", displayDriverCopy.GetType ().Name);
                return true;
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                return false;
            }
        }

        private void ConnectExternalDisplay ()
        {
            Device device = Device.GetDefaultDisplay ();
            if (device == null || device.DriverInfo == null)
                return;

            ConnectExternalDisplay (device);
        }

        private void ConnectExternalDisplay (Device device)
        {
            if (displayDriver != null)
                throw new ConnectFailedException ("External display is already connected!");

            IConnectableDevice connectable;
            try {
                connectable = ConnectDevice (device);
            } catch (SocketException socex) {
                throw new HardwareErrorException (new ErrorState (ErrorState.ExternalDisplayDisconnected, HardwareErrorSeverity.Error), socex);
            }

            lock (displayDriverLock)
                displayDriver = connectable as IExternalDisplayController;
        }

        public void DisconnectExternalDisplay ()
        {
            if (displayDriver == null)
                return;

            if (displayDriver.IsConnected && displayDriver.SupportedCommands.Contains (DeviceCommands.DisplayClear))
                TryDeviceCommand (displayDriver.DisplayClear);

            DisconnectDevice (displayDriver);
            lock (displayDriverLock)
                displayDriver = null;
        }

        #endregion

        #region Card reader connection

        private static readonly object cardReaderDriverLock = new object ();
        private ICardReaderController cardReaderDriver;

        public bool CardReaderConnected
        {
            get { return cardReaderDriver != null; }
        }

        public bool InitCardReader ()
        {
            try {
                DisconnectCardReader ();

                BusinessDomain.AppConfiguration.CardReaderEnabled = Device.GetDefaultCardReader () != null;
                if (!BusinessDomain.AppConfiguration.CardReaderEnabled)
                    return true;

                bool retry;
                do {
                    retry = false;
                    try {
                        ConnectCardReader ();
                    } catch (HardwareErrorException ex) {
                        retry = OnHardwareError (ex);
                        if (!retry)
                            throw;
                    }
                } while (retry);

                ICardReaderController cardReaderDriverCopy = cardReaderDriver;
                if (cardReaderDriverCopy != null)
                    TrackEvent ("Connected card reader", cardReaderDriverCopy.GetType ().Name);
                return true;
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                return false;
            }
        }

        private void ConnectCardReader ()
        {
            Device device = Device.GetDefaultCardReader ();
            if (device == null || device.DriverInfo == null)
                return;

            ConnectCardReader (device);
        }

        private void ConnectCardReader (Device device)
        {
            if (cardReaderDriver != null)
                throw new ConnectFailedException ("Card reader is already connected!");

            if (!device.DriverInfo.IsCardReaderDriver)
                throw new ConnectFailedException ("The device driver does not support reading cards!");

            IConnectableDevice connectable;
            try {
                connectable = ConnectDevice (device);
            } catch (SocketException socex) {
                throw new HardwareErrorException (new ErrorState (ErrorState.CardReaderDisconnected, HardwareErrorSeverity.Error), socex);
            }

            lock (cardReaderDriverLock)
                cardReaderDriver = connectable as ICardReaderController;

            if (cardReaderDriver != null)
                cardReaderDriver.CardRecognized += cardReaderDriver_CardRecognized;
        }

        public void DisconnectCardReader ()
        {
            if (cardReaderDriver == null)
                return;

            DisconnectDevice (cardReaderDriver);
            lock (cardReaderDriverLock) {
                cardReaderDriver.CardRecognized -= cardReaderDriver_CardRecognized;
                cardReaderDriver = null;
            }
        }

        private void cardReaderDriver_CardRecognized (object sender, CardReadArgs e)
        {
            if (CardRecognized != null)
                CardRecognized (sender, e);
        }

        #endregion

        #region Electronic scale connection

        private static readonly object electronicScalesLock = new object ();
        private IElectronicScaleController electronicScaleDriver;

        public bool ElectronicScaleConnected
        {
            get { return electronicScaleDriver != null; }
        }

        public bool InitElectronicScale ()
        {
            try {
                DisconnectElectronicScale ();

                BusinessDomain.AppConfiguration.ElectronicScaleEnabled = Device.GetDefaultElectronicScale () != null;
                if (!BusinessDomain.AppConfiguration.ElectronicScaleEnabled)
                    return true;

                bool retry;
                do {
                    retry = false;
                    try {
                        ConnectElectronicScale ();
                    } catch (HardwareErrorException ex) {
                        retry = OnHardwareError (ex);
                        if (!retry)
                            throw;
                    }
                } while (retry);

                IElectronicScaleController electronicScaleDriverCopy = electronicScaleDriver;
                if (electronicScaleDriverCopy != null)
                    TrackEvent ("Connected electronic scale", electronicScaleDriverCopy.GetType ().Name);
                return true;
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                return false;
            }
        }

        private void ConnectElectronicScale ()
        {
            Device device = Device.GetDefaultElectronicScale ();
            if (device == null || device.DriverInfo == null)
                return;

            ConnectElectronicScale (device);
            OnElectronicScaleChanged ();
        }

        private void ConnectElectronicScale (Device device)
        {
            if (electronicScaleDriver != null)
                throw new ConnectFailedException ("An electronic scale is already connected!");

            IConnectableDevice connectable;
            try {
                connectable = ConnectDevice (device);
            } catch (SocketException socex) {
                throw new HardwareErrorException (new ErrorState (ErrorState.ElectronicScaleDisconnected, HardwareErrorSeverity.Error), socex);
            }

            lock (electronicScalesLock)
                electronicScaleDriver = connectable as IElectronicScaleController;
        }

        public void DisconnectElectronicScale ()
        {
            if (electronicScaleDriver == null)
                return;

            DisconnectDevice (electronicScaleDriver);
            lock (electronicScalesLock)
                electronicScaleDriver = null;

            OnElectronicScaleChanged ();
        }

        #endregion

        #region Kitchen printers connection

        private static readonly object kitchenDriversLock = new object ();
        private readonly Dictionary<Device, IConnectableDevice> kitchenPrinters = new Dictionary<Device, IConnectableDevice> ();

        public Dictionary<Device, IConnectableDevice> KitchenPrinters
        {
            get { return kitchenPrinters; }
        }

        public bool InitKitchenPrinters ()
        {
            try {
                DisconnectKitchenPrinters ();

                bool retry;
                do {
                    retry = false;
                    try {
                        ConnectKitchenPrinters ();
                    } catch (HardwareErrorException ex) {
                        retry = OnHardwareError (ex);
                        if (!retry)
                            throw;
                    }
                } while (retry);

                foreach (KeyValuePair<Device, IConnectableDevice> kitchenPrinter in kitchenPrinters) {
                    IConnectableDevice kitchenPrinterCopy = kitchenPrinter.Value;
                    if (kitchenPrinterCopy == null)
                        continue;

                    TrackEvent ("Connected kitchen printer", kitchenPrinterCopy.GetType ().Name);
                }
                return true;
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                return false;
            }
        }

        private void ConnectKitchenPrinters ()
        {
            foreach (Device device in Device.GetAllKitchenPrinters ()) {
                try {
                    ConnectKitchenPrinter (device);
                } catch (ConnectFailedException ex) {
                    ErrorHandling.LogException (ex);
                }
            }
        }

        private void ConnectKitchenPrinter (Device device)
        {
            if (device.DriverInfo == null)
                throw new ConnectFailedException ("The device driver was not found!");

            if (!device.DriverInfo.IsCustomerOrderPrinterDriver && !device.DriverInfo.IsKitchenDriver)
                throw new ConnectFailedException ("The device driver does not support printing!");

            IConnectableDevice connectable;
            try {
                connectable = ConnectDevice (device);
            } catch (SocketException socex) {
                throw new HardwareErrorException (new ErrorState (ErrorState.KitchenPrinterDisconnected, HardwareErrorSeverity.Error), socex);
            }

            if (connectable == null)
                return;

            lock (kitchenDriversLock)
                kitchenPrinters.Add (device, connectable);
        }

        public void DisconnectKitchenPrinters ()
        {
            foreach (KeyValuePair<Device, IConnectableDevice> printer in kitchenPrinters) {
                DisconnectDevice (printer.Value);
            }

            lock (kitchenDriversLock)
                kitchenPrinters.Clear ();
        }

        #endregion

        #region Sales Data Controller connection

        private static readonly object salesDataControllerDriverLock = new object ();
        private ISalesDataController salesDataControllerDriver;

        public ISalesDataController SalesDataControllerDriver
        {
            get { return salesDataControllerDriver; }
        }

        public bool SalesDataControllerConnected
        {
            get { return salesDataControllerDriver != null; }
        }

        public bool InitSalesDataController ()
        {
            try {
                DeinitSalesDataController ();

                if (SalesDataControllerConnected)
                    return true;

                BusinessDomain.AppConfiguration.SalesDataControllerEnabled = Device.GetDefaultSalesDataController () != null;
                if (!BusinessDomain.AppConfiguration.SalesDataControllerEnabled)
                    return true;

                bool retry;
                do {
                    retry = false;
                    try {
                        ConnectSalesDataController ();
                    } catch (HardwareErrorException ex) {
                        retry = OnHardwareError (ex);
                        if (!retry)
                            throw;
                    }
                } while (retry);

                ISalesDataController driverCopy = salesDataControllerDriver;
                if (driverCopy != null)
                    TrackEvent ("Connected sales data controller", driverCopy.GetType ().Name);
                return true;
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                return false;
            }
        }

        private void ConnectSalesDataController ()
        {
            Device device = Device.GetDefaultSalesDataController ();
            if (device == null || device.DriverInfo == null)
                return;

            ConnectSalesDataController (device);
        }

        private void ConnectSalesDataController (Device device)
        {
            if (salesDataControllerDriver != null)
                throw new ConnectFailedException ("Sales data controller is already connected!");

            if (!device.DriverInfo.IsSalesDataControllerDriver)
                throw new ConnectFailedException ("The device driver does not support colelcting sales info!");

            IConnectableDevice connectable;
            try {
                connectable = ConnectDevice (device);
            } catch (SocketException socex) {
                throw new HardwareErrorException (new ErrorState (ErrorState.SalesDataControllerDisconnected, HardwareErrorSeverity.Error), socex);
            }

            lock (salesDataControllerDriverLock)
                salesDataControllerDriver = connectable as ISalesDataController;
        }

        public void DeinitSalesDataController ()
        {
            try {
                DisconnectSalesDataController ();
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
            }
        }

        public void DisconnectSalesDataController ()
        {
            if (salesDataControllerDriver == null)
                return;

            DisconnectDevice (salesDataControllerDriver);
            lock (salesDataControllerDriverLock)
                salesDataControllerDriver = null;
        }

        #endregion

        #region Barcode scanner connection

        private static readonly object barcodeScannerDriverLock = new object ();
        private IBarcodeScannerController barcodeScannerDriver;

        public bool BarcodeScannerConnected
        {
            get { return barcodeScannerDriver != null; }
        }

        public bool InitBarcodeScanner ()
        {
            try {
                DisconnectBarcodeScanner ();

                BusinessDomain.AppConfiguration.BarcodeScannerEnabled = Device.GetDefaultBarcodeScanner () != null;
                if (!BusinessDomain.AppConfiguration.BarcodeScannerEnabled)
                    return true;

                bool retry;
                do {
                    retry = false;
                    try {
                        ConnectBarcodeScanner ();
                    } catch (HardwareErrorException ex) {
                        retry = OnHardwareError (ex);
                        if (!retry)
                            throw;
                    }
                } while (retry);

                IBarcodeScannerController barcodeScannerDriverCopy = barcodeScannerDriver;
                if (barcodeScannerDriverCopy != null)
                    TrackEvent ("Connected barcode scanner", barcodeScannerDriverCopy.GetType ().Name);
                return true;
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                return false;
            }
        }

        private void ConnectBarcodeScanner ()
        {
            Device device = Device.GetDefaultBarcodeScanner ();
            if (device == null || device.DriverInfo == null)
                return;

            ConnectBarcodeScanner (device);
        }

        private void ConnectBarcodeScanner (Device device)
        {
            if (barcodeScannerDriver != null)
                throw new ConnectFailedException ("Barcode scanner is already connected!");

            if (!device.DriverInfo.IsBarcodeScannerDriver)
                throw new ConnectFailedException ("The device driver does not support scanning barcodes!");

            IConnectableDevice connectable;
            try {
                connectable = ConnectDevice (device);
            } catch (SocketException socex) {
                throw new HardwareErrorException (new ErrorState (ErrorState.BarcodeScannerDisconnected, HardwareErrorSeverity.Error), socex);
            }

            lock (barcodeScannerDriverLock)
                barcodeScannerDriver = connectable as IBarcodeScannerController;

            if (barcodeScannerDriver != null)
                barcodeScannerDriver.BarcodeScanned += barcodeScannerDriver_BarcodeScanned;
        }

        public void DisconnectBarcodeScanner ()
        {
            if (barcodeScannerDriver == null)
                return;

            DisconnectDevice (barcodeScannerDriver);
            lock (barcodeScannerDriverLock) {
                barcodeScannerDriver.BarcodeScanned -= barcodeScannerDriver_BarcodeScanned;
                barcodeScannerDriver = null;
            }
        }

        public void EnableBarcodeScanner ()
        {
            IBarcodeScannerController scannerDriver = barcodeScannerDriver;
            if (scannerDriver == null)
                return;

            scannerDriver.EnableBarcodeScanning ();
        }

        public void DisableBarcodeScanner ()
        {
            IBarcodeScannerController scannerDriver = barcodeScannerDriver;
            if (scannerDriver == null)
                return;

            scannerDriver.DisableBarcodeScanning ();
        }

        private void barcodeScannerDriver_BarcodeScanned (object sender, BarcodeScannedArgs e)
        {
            if (BarcodeScanned != null)
                BarcodeScanned (sender, e);
        }

        #endregion

        protected abstract Type [] GetAllDriverTypes ();

        private object CreateInstance (DriverInfo driver)
        {
            string driverTypeName = driver.DriverTypeName;
            Type drType = Type.GetType (driverTypeName);
            if (drType != null)
                return Activator.CreateInstance (drType);

            return AllDriverTypes
                .Where (nodeType => nodeType.AssemblyQualifiedName == driverTypeName || nodeType.FullName == driverTypeName)
                .Select (Activator.CreateInstance)
                .FirstOrDefault ();
        }

        public IConnectableDevice ConnectDevice (Device device)
        {
            if (device == null)
                throw new ArgumentNullException ("device");

            DriverInfo driver = device.DriverInfo;
            ConnectParametersCollection parameters = device.GetParameters ();
            IConnectableDevice driverObject;

            if (checkFailedDevices && failedDevices.ContainsKey (device.Id))
                return null;

            string port = device.GetPortAssignment ();

            if (port != null && portAssignments.TryGetValue (port, out driverObject)) {
                if (driverObject.GetType ().AssemblyQualifiedName != driver.DriverTypeName)
                    throw new ConnectFailedException ("A different device is already using the specified port!");

                // No need to reconnect as there is no way for the connection parameters to be different
                //try {
                //    TryDeviceCommand (() =>
                //        {
                //            driverObject.Disconnect ();
                //            driverObject.Connect (parameters);
                //        });
                //} catch (ArgumentException arex) {
                //    TryDeviceCommand (driverObject.Disconnect);
                //    throw new HardwareErrorException (new ErrorState (ErrorState.BadSerialPort, HardwareErrorSeverity.Error), arex);
                //} catch (Exception) {
                //    TryDeviceCommand (driverObject.Disconnect);
                //    throw;
                //}
            } else {
                driverObject = CreateInstance (driver) as IConnectableDevice;
                if (driverObject == null)
                    throw new ConnectFailedException ("The device driver does not support connecting!");

                try {
                    failedDevices [device.Id] = device;
                    TryDeviceCommand (() => driverObject.Connect (parameters));
                    failedDevices.Remove (device.Id);
                } catch (ArgumentException arex) {
                    TryDeviceCommand (driverObject.Disconnect);
                    throw new HardwareErrorException (new ErrorState (ErrorState.BadConnectionParameters, HardwareErrorSeverity.Error), arex);
                } catch (HardwareErrorException ex) {
                    TryDeviceCommand (driverObject.Disconnect);
                    if (ex.InnerException is ArgumentException)
                        throw new HardwareErrorException (new ErrorState (ErrorState.BadSerialPort, HardwareErrorSeverity.Error), ex);
                    HardwareErrorException hwex = ex.InnerException as HardwareErrorException;
                    if (hwex != null)
                        throw new HardwareErrorException (hwex);

                    throw;
                } catch (Exception) {
                    TryDeviceCommand (driverObject.Disconnect);
                    throw;
                }

                if (port != null)
                    portAssignments.Add (port, driverObject);
            }
            driverObject.ReferenceCount++;

            return driverObject;
        }

        public void DisconnectDevice (IConnectableDevice connectable)
        {
            connectable.ReferenceCount = Math.Max (connectable.ReferenceCount - 1, 0);
            if (connectable.ReferenceCount != 0)
                return;

            foreach (KeyValuePair<string, IConnectableDevice> portAssignment in portAssignments) {
                if (!ReferenceEquals (connectable, portAssignment.Value))
                    continue;

                portAssignments.Remove (portAssignment.Key);
                break;
            }

            TryDeviceCommand (() =>
                {
                    try {
                        connectable.Disconnect ();
                        connectable.Dispose ();
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                    }
                });
        }

        public DriverInfo [] AllDrivers
        {
            get { return allDrivers ?? (allDrivers = GetAllDrivers ()); }
        }

        public Type [] AllDriverTypes
        {
            get { return allDriverTypes ?? (allDriverTypes = GetAllDriverTypes ()); }
            set { allDriverTypes = value; }
        }

        private DriverInfo [] GetAllDrivers ()
        {
            List<DriverInfo> ret = new List<DriverInfo> ();

            foreach (Type type in AllDriverTypes) {
                DriverBase driverInstance;
                try {
                    driverInstance = Activator.CreateInstance (type) as DriverBase;
                } catch (Exception) {
                    continue;
                }

                if (driverInstance != null)
                    ret.Add (new DriverInfo (driverInstance));
            }

            DriverHelper.OnDriversLoaded (ret);

            return ret.ToArray ();
        }

        public abstract string [] GetSerialPortNames ();

        #endregion

        #region Sales management

        private static readonly object finalizeOperationLock = new object ();

        public void FinalizeOperation (FinalizeOperationOptions options)
        {
            lock (finalizeOperationLock) {
                FinalizeAction action = options.Action;
                if (!BusinessDomain.AppConfiguration.CashReceiptPrinterEnabled) {
                    if (!BusinessDomain.WorkflowManager.AllowSaleWithoutReceipt &&
                        ((action & FinalizeAction.CommitSale) != FinalizeAction.None))
                        throw new HardwareErrorException (new ErrorState (ErrorState.CashReceiptPrinterDisconnected, HardwareErrorSeverity.Error));

                    action &= ~FinalizeAction.PrintCashReceipt;
                } else if ((action & FinalizeAction.PrintCashReceipt) != FinalizeAction.None) {
                    if (!CashReceiptPrinterConnected) {
                        ConnectCashReceiptPrinter ();

                        if (!CashReceiptPrinterConnected)
                            throw new HardwareErrorException (new ErrorState (ErrorState.CashReceiptPrinterDisconnected, HardwareErrorSeverity.Error));
                    }

                    if (cashReceiptPrinterDriver.LastErrorState.Warnings.Count > 0)
                        throw new HardwareErrorException (cashReceiptPrinterDriver.LastErrorState);
                }

                if (BusinessDomain.AppConfiguration.SalesDataControllerEnabled) {
                    if (!SalesDataControllerConnected)
                        ConnectSalesDataController ();

                    if (salesDataControllerDriver.LastErrorState.Warnings.Count > 0)
                        throw new HardwareErrorException (salesDataControllerDriver.LastErrorState);
                } else
                    action &= ~FinalizeAction.CollectSaleData;

                try {
                    printedKitchenReceipts.Clear ();
                    if ((action & (FinalizeAction.PrintCashReceipt | FinalizeAction.CollectSaleData)) != 0) {
                        CheckVATRates ();
                    }

                    using (DbTransaction transaction = new DbTransaction (BusinessDomain.DataAccessProvider)) {
                        bool commitOrder = (action & FinalizeAction.CommitOrder) != 0;
                        if (commitOrder)
                            options.RestaurantOrder.CommitChanges (true, false, options.RestaurantOrderMainLocation, options.RestaurantOrderCheckAvailability);

                        bool commitSale = (action & FinalizeAction.CommitSale) != 0;
                        if (commitOrder && commitSale) {
                            // Remove the order before saving the sale so the ordered quantity is
                            // made available for the sale
                            RestaurantOrder order = options.RestaurantOrder;
                            order.ClearDetails ();
                            order.CommitChanges ();
                        }

                        if (commitSale) {
                            transaction.SnapshotObject (options.Sale);
                            if (options.Sale.State == OperationState.NewDraft ||
                                options.Sale.State == OperationState.Draft)
                                options.Sale.SetState (OperationState.New);

                            options.Sale.Commit (commitOrder ? options.RestaurantOrder.LocationId : -1);
                            foreach (Payment payment in options.EditedAdvancePayments)
                                payment.CommitChanges ();

                            if (options.OnSaleCommitted != null)
                                options.OnSaleCommitted ();
                        }

                        if ((action & FinalizeAction.CommitDocument) != 0)
                            CommitDocument (options, transaction);

                        if ((action & FinalizeAction.PrintKitchen) != 0)
                            PrintKitchenOrder (options);

                        if ((action & FinalizeAction.PrintCustomerOrder) != 0)
                            PrintCustomerOrder (options);

                        if ((action & FinalizeAction.CollectSaleData) != 0)
                            CollectSaleData (options);

                        if ((action & FinalizeAction.PrintCashReceipt) != 0)
                            PrintCashReceiptSale (options);

                        if ((action & FinalizeAction.PrintCustomerOrderInvoice) != 0)
                            PrintCustomerOrderInvoice (options);

                        if ((action & FinalizeAction.PrintCashReceiptInvoice) != 0)
                            PrintCashReceiptInvoice (options);

                        if (commitSale && cashReceiptPrinterDriver != null)
                            OpenCashDrawer (options);

                        transaction.Complete ();
                    }
                } catch (Exception) {
                    if (printedKitchenReceipts.Count > 0) {
                        try {
                            OnReceiptPrintStart (Translator.GetString ("Printing annulled kitchen receipts..."), options);
                            for (int i = 0; i < printedKitchenReceipts.Count; i++) {
                                ReceiptInfo receipt = printedKitchenReceipts [i];
                                try {
                                    FinalizeOperationOptions anullOptions = (FinalizeOperationOptions) options.Clone ();
                                    if (receipt.KitchenPrinter != null) {
                                        anullOptions.KitchenSale = receipt.Sale;
                                        anullOptions.KitchenDeltaSale = receipt.DeltaSale;
                                        anullOptions.KitchenTitle = Translator.GetString ("RECEIPT ANNULLED");

                                        PrintKitchenOrder (anullOptions, receipt.KitchenPrinter, printedKitchenReceipts.Count, i, true);
                                    } else if (receipt.CustomerOrderPrinter != null) {
                                        anullOptions.CustomerOrderSale = receipt.Sale;
                                        anullOptions.CustomerOrderDeltaSale = receipt.DeltaSale;
                                        anullOptions.CustomerOrderTitle = Translator.GetString ("RECEIPT ANNULLED");

                                        PrintCustomerOrder (anullOptions, receipt.CustomerOrderPrinter, printedKitchenReceipts.Count, i, true);
                                    }
                                } catch (HardwareErrorException) {
                                }
                            }
                        } finally {
                            OnReceiptPrintEnd (options);
                        }
                    }

                    throw;
                }
            }
        }

        private void CommitDocument (FinalizeOperationOptions options, DbTransaction transaction)
        {
            DocumentBase document = (DocumentBase) options.Document;
            transaction.SnapshotObject (document);
            document.CommitChanges ();
        }

        private void CheckVATRates ()
        {
            // If we can, check if the VAT rates match the ones of the fiscal printer
            if (cashReceiptPrinterDriver == null || !cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.GetVATRatesInfo))
                return;

            Dictionary<FiscalPrinterTaxGroup, double> vatRates = null;
            TryDeviceCommand (() => cashReceiptPrinterDriver.GetVATRatesInfo (out vatRates), true);

            foreach (VATGroup vatGroup in VATGroup.GetAll ()) {
                FiscalPrinterTaxGroup fiscalPrinterTaxGroup = vatGroup.CodeValue;
                if (fiscalPrinterTaxGroup == FiscalPrinterTaxGroup.Default)
                    fiscalPrinterTaxGroup = cashReceiptPrinterDriver.DefaultTaxGroup;

                double printerRate;
                if (!vatRates.TryGetValue (fiscalPrinterTaxGroup, out printerRate))
                    throw new HardwareErrorException (new ErrorState (ErrorState.VATGroupsMismatch, HardwareErrorSeverity.Error));

                if (!printerRate.IsEqualTo (vatGroup.VatValue))
                    throw new HardwareErrorException (new ErrorState (ErrorState.VATGroupsMismatch, HardwareErrorSeverity.Error));
            }
        }

        private void PrintKitchenOrder (FinalizeOperationOptions options)
        {
            try {
                int currentPrinter = -1;
                Device [] allKitchenPrinters = Device.GetAllKitchenPrinters ();
                IList<ItemsGroup> allGroups = null;

                foreach (KeyValuePair<Device, IConnectableDevice> pair in kitchenPrinters) {
                    currentPrinter++;

                    Device device = pair.Key;
                    Sale s = (Sale) options.KitchenSale.Clone ();
                    s.Id = options.KitchenSale.Id;

                    Sale ds = null;
                    if (options.KitchenDeltaSale != null) {
                        ds = (Sale) options.KitchenDeltaSale.Clone ();
                        ds.Id = options.KitchenDeltaSale.Id;
                    }

                    if (!device.AllItemGroups) {
                        if (allGroups == null)
                            allGroups = ItemsGroup.GetAllFlat ();

                        IList<ItemsGroup> deviceGroups = null;
                        FilterKitchenSale (s, allGroups, device, ref deviceGroups);

                        if (ds != null)
                            FilterKitchenSale (ds, allGroups, device, ref deviceGroups);
                    }

                    if (s.Details.Count == 0 && (ds == null || ds.Details.Count == 0))
                        continue;

                    if (currentPrinter == 0)
                        OnReceiptPrintStart (Translator.GetString ("Printing kitchen receipts..."), options);

                    bool retry;
                    do {
                        retry = false;
                        IConnectableDevice driver = pair.Value;

                        if (device.DriverInfo.IsKitchenDriver) {
                            try {
                                IKitchenPrinterController kitchenPrinter = driver as IKitchenPrinterController;
                                FinalizeOperationOptions kitchenOptions = (FinalizeOperationOptions) options.Clone ();
                                kitchenOptions.KitchenSale = s;
                                kitchenOptions.KitchenDeltaSale = ds;

                                PrintKitchenOrder (kitchenOptions, kitchenPrinter, allKitchenPrinters.Length, currentPrinter, true);
                                printedKitchenReceipts.Add (new ReceiptInfo { KitchenPrinter = kitchenPrinter, Sale = s, DeltaSale = ds });
                            } catch (HardwareErrorException ex) {
                                if (KitchenPrinterError == null)
                                    throw;

                                OnReceiptPrintDialogShown (options);
                                KitchenPrinterErrorEventArgs args = new KitchenPrinterErrorEventArgs (ex);
                                KitchenPrinterError (this, args);
                                if (args.Retry)
                                    retry = true;
                                else if (args.TryCustomerOrderPrinter) {
                                    OnReceiptPrintEnd (options);
                                    FinalizeOperationOptions customerOrderOptions = (FinalizeOperationOptions) options.Clone ();
                                    customerOrderOptions.CustomerOrderSale = s;
                                    customerOrderOptions.CustomerOrderDeltaSale = ds;
                                    customerOrderOptions.CustomerOrderTitle = customerOrderOptions.KitchenTitle;

                                    PrintCustomerOrder (customerOrderOptions);
                                    if (allKitchenPrinters.Length - 1 > currentPrinter)
                                        OnReceiptPrintStart (Translator.GetString ("Printing kitchen receipts..."), options);
                                } else
                                    throw;
                            }
                        } else if (device.DriverInfo.IsCustomerOrderPrinterDriver) {
                            try {
                                ICashReceiptPrinterController cashReceiptPrinter = driver as ICashReceiptPrinterController;
                                FinalizeOperationOptions customerOrderOptions = (FinalizeOperationOptions) options.Clone ();
                                customerOrderOptions.CustomerOrderSale = s;
                                customerOrderOptions.CustomerOrderDeltaSale = ds;
                                customerOrderOptions.CustomerOrderTitle = customerOrderOptions.KitchenTitle;

                                PrintCustomerOrder (customerOrderOptions, cashReceiptPrinter, allKitchenPrinters.Length, currentPrinter, true);
                                printedKitchenReceipts.Add (new ReceiptInfo { CustomerOrderPrinter = cashReceiptPrinter, Sale = s, DeltaSale = ds });
                            } catch (HardwareErrorException ex) {
                                if (options.SilentMode)
                                    throw;

                                retry = OnHardwareError (ex);
                                if (!retry)
                                    throw;
                            }
                        }
                    } while (retry);
                }
            } finally {
                OnReceiptPrintEnd (options);
            }
        }

        private static void FilterKitchenSale (Sale sale, IList<ItemsGroup> allGroups, Device device, ref IList<ItemsGroup> deviceGroups)
        {
            for (int i = sale.Details.Count - 1; i >= 0; i--) {
                ItemsGroup itemsGroup = allGroups.FirstOrDefault (g => sale.Details [i].ItemGroupId == g.Id);
                if (itemsGroup == null) {
                    sale.Details.RemoveAt (i);
                    continue;
                }

                if (deviceGroups == null)
                    deviceGroups = device.KitchenItemGroups
                        .Select (gid => allGroups.FirstOrDefault (g => g.Id == gid.Key))
                        .ToList ();

                if (deviceGroups.All (dg => dg != null && !itemsGroup.Code.StartsWith (dg.Code)))
                    sale.Details.RemoveAt (i);
            }
        }

        private void PrintKitchenOrder (FinalizeOperationOptions options, IKitchenPrinterController printer, int printers, int currentPrinter, bool isKitchenReceipt)
        {
            Sale sale = options.KitchenSale;
            if (sale.State == OperationState.New &&
                options.RestaurantOrder != null &&
                options.RestaurantOrder.State == OperationState.Draft)
                sale.Id = options.RestaurantOrder.Id;

            Sale deltaSale = options.KitchenDeltaSale;
            string title = options.KitchenTitle;
            bool showPrice = !isKitchenReceipt;

            if (printer.SupportedCommands.Contains (DeviceCommands.GetStatus)) {
                TryDeviceCommand (printer.GetStatus, options.SilentMode);
                if (printer.LastErrorState.Warnings.Count > 0)
                    throw new HardwareErrorException (printer.LastErrorState);
            }

            #region Print document header

            int usedWidth;

            StringBuilder sb = new StringBuilder ();
            for (int i = 0; i < printer.TextCharsPerLine; i++)
                sb.Append (i % 2 == 0 ? "-" : " ");

            string separator = sb.ToString ();
            string saleString = Translator.GetString ("SALE").AlignCenter (printer.TextCharsPerLine);
            string documentString = Translator.GetString ("DOCUMENT");
            string orderString = string.Format (Translator.GetString ("Order code: {0}"), -10 - sale.Id);
            string locationString = string.Format (Translator.GetString ("Location: {0}"), sale.Location2);
            string partnerString = string.Format (Translator.GetString ("Partner: {0}"), sale.PartnerName2);
            string operatorString = string.Format (Translator.GetString ("Operator: {0}"), sale.UserName2);

            TryDeviceCommand (delegate
                {
                    printer.PrintFreeText (saleString);
                    printer.PaperFeed ();

                    #region Print document number

                    if (sale.State == OperationState.Saved) {
                        string docNumber = sale.FormattedOperationNumber;
                        usedWidth = documentString.Length + docNumber.Length;
                        sb = new StringBuilder (documentString);
                        for (int i = usedWidth; i < printer.TextCharsPerLine; i++) {
                            sb.Append (' ');
                        }
                        sb.Append (docNumber);
                        printer.PrintFreeText (sb.ToString ());
                    }

                    #endregion

                    #region Print document date

                    string docDate = BusinessDomain.GetFormattedDate (BusinessDomain.Today);
                    string docTime = BusinessDomain.Now.ToString ("HH:mm");
                    usedWidth = docDate.Length + docTime.Length;
                    sb = new StringBuilder (docDate);
                    for (int i = usedWidth; i < printer.TextCharsPerLine; i++) {
                        sb.Append (' ');
                    }
                    sb.Append (docTime);
                    printer.PrintFreeText (sb.ToString ());

                    #endregion

                    #region Print operator name

                    printer.PrintFreeText (separator);
                    if (title.Length > 0) {
                        printer.PrintFreeText (title.AlignCenter (printer.TextCharsPerLine));
                        printer.PrintFreeText (separator);
                    }
                    if (BusinessDomain.AppConfiguration.PrintOrderCodeOnReceipts)
                        printer.PrintFreeText (orderString);

                    if (BusinessDomain.AppConfiguration.PrintLocationOnReceipts)
                        printer.PrintFreeText (locationString);

                    if (BusinessDomain.AppConfiguration.PrintPartnerOnReceipts)
                        printer.PrintFreeText (partnerString);

                    if (BusinessDomain.AppConfiguration.PrintOperatorOnReceipts)
                        printer.PrintFreeText (operatorString);

                    printer.PrintFreeText (separator);
                    printer.PaperFeed ();

                    #endregion
                }, options.SilentMode);

            #endregion

            int totalDetails = sale.Details.Count + (deltaSale != null ? deltaSale.Details.Count : 0);
            string discountString = Translator.GetString ("Discount");
            string allowanceString = Translator.GetString ("Allowance");
            for (int i = 0; i < sale.Details.Count; i++) {
                int i1 = i;
                TryDeviceCommand (delegate
                    {
                        SaleDetail detail = sale.Details [i1];
                        PrintItemInfo (detail.ItemName2.Trim (printer.TextCharsPerLine),
                            detail.Quantity,
                            detail.MUnitName,
                            showPrice, detail.OriginalPriceOutPlusVAT, printer.TextCharsPerLine, printer.PrintFreeText);

                        if (showPrice && !detail.Discount.IsZero ())
                            printer.PrintFreeText (GetDiscountLine (printer.TextCharsPerLine, detail.Quantity, detail.OriginalPriceOutPlusVAT, detail.Discount, discountString, allowanceString));

                        foreach (string modifier in GetModifierLines (options, detail.Note, false))
                            printer.PrintFreeText (modifier);
                    }, options.SilentMode);

                OnReceiptPrintStep ((((double) currentPrinter * 100) / printers) + ((double) (i + 1) * 100) / (totalDetails * printers), options);
            }

            if (deltaSale != null) {
                string modString = Translator.GetString ("Modifications");
                string oldString = Translator.GetString ("old") + " ";
                string newString = Translator.GetString ("new") + " ";

                for (int i = 0; i < deltaSale.Details.Count; i++) {
                    int i1 = i;
                    TryDeviceCommand (delegate
                        {
                            SaleDetail detail = deltaSale.Details [i1];
                            if (i1 == 0) {
                                if (sale.Details.Count > 0)
                                    printer.PrintFreeText (separator);
                                printer.PrintFreeText (modString);
                                printer.PaperFeed ();
                            }

                            string itemName = detail.ItemName2;
                            if (itemName.Length > printer.TextCharsPerLine)
                                itemName = itemName.Substring (0, printer.TextCharsPerLine);

                            printer.PrintFreeText (itemName);

                            if (!detail.OriginalQuantity.IsEqualTo (detail.Quantity)) {
                                printer.PrintFreeText (GetQuantityLine (printer.TextCharsPerLine, oldString, detail.OriginalQuantity, detail.MUnitName, showPrice, detail.PriceOutPlusVAT));
                                printer.PaperFeed ();

                                printer.PrintFreeText (GetQuantityLine (printer.TextCharsPerLine, newString, detail.Quantity, detail.MUnitName, showPrice, detail.PriceOutPlusVAT));
                                printer.PaperFeed ();
                            }

                            if (!options.SkipPrintingNotes && !detail.OriginalNote.IsEqualTo (detail.Note)) {
                                printer.PrintFreeText (oldString);
                                foreach (string modifier in GetModifierLines (options, detail.OriginalNote, true))
                                    printer.PrintFreeText (modifier);

                                printer.PaperFeed ();

                                printer.PrintFreeText (newString);
                                foreach (string modifier in GetModifierLines (options, detail.Note, true))
                                    printer.PrintFreeText (modifier);

                                printer.PaperFeed ();
                            }
                        }, options.SilentMode);

                    OnReceiptPrintStep ((((double) currentPrinter * 100) / printers) + ((double) (i + sale.Details.Count + 1) * 100) / (totalDetails * printers), options);
                }
            }

            if (showPrice) {
                string docTotal = Translator.GetString ("TOTAL");
                TryDeviceCommand (delegate
                    {
                        string totalValue = Currency.ToString (sale.TotalPlusVAT);
                        printer.PrintFreeText (docTotal.PadRight (printer.TextCharsPerLine - totalValue.Length, '_') + totalValue);
                    }, options.SilentMode);
            }

            string nonFiscalString = Translator.GetString ("NON-FISCAL RECEIPT").AlignCenter (printer.TextCharsPerLine);
            TryDeviceCommand (delegate
                {
                    printer.PrintFreeText (separator);
                    printer.PrintFreeText (nonFiscalString);

                    if (printer.SupportedCommands.Contains (DeviceCommands.PaperCut))
                        printer.PaperCut ();
                }, options.SilentMode);
        }

        private void PrintCustomerOrder (FinalizeOperationOptions options)
        {
            if (!BusinessDomain.AppConfiguration.CustomerOrdersPrinterEnabled ||
                (options.Sale.Details.Count == 0 && (options.DeltaSale == null || options.DeltaSale.Details.Count == 0)))
                return;

            if (!CustomerOrderPrinterConnected)
                ConnectCustomerOrderPrinter ();

            if (customerOrderDriver.LastErrorState.Errors.Count > 0)
                throw new HardwareErrorException (customerOrderDriver.LastErrorState);

            FinalizeOperationOptions optionsClone = (FinalizeOperationOptions) options.Clone ();
            optionsClone.SkipPrintingNotes = true;

            try {
                OnReceiptPrintStart (Translator.GetString ("Printing receipt..."), options);
                ICashReceiptPrinterController cashReceiptPrinterController = customerOrderDriver as ICashReceiptPrinterController;
                if (cashReceiptPrinterController != null)
                    PrintCustomerOrder (optionsClone, cashReceiptPrinterController, 1, 0, false);
                else {
                    IKitchenPrinterController kitchenPrinterController = customerOrderDriver as IKitchenPrinterController;
                    if (kitchenPrinterController != null) {
                        optionsClone.KitchenSale = optionsClone.CustomerOrderSale;
                        optionsClone.KitchenDeltaSale = optionsClone.CustomerOrderDeltaSale;
                        optionsClone.KitchenTitle = optionsClone.CustomerOrderTitle;

                        PrintKitchenOrder (optionsClone, kitchenPrinterController, 1, 0, false);
                    }
                }
            } finally {
                OnReceiptPrintEnd (options);
            }
        }

        private void PrintCustomerOrder (FinalizeOperationOptions options, ICashReceiptPrinterController printer, int printers, int currentPrinter, bool isKitchenReceipt)
        {
            Sale sale = options.CustomerOrderSale;
            if (sale.State == OperationState.New &&
                options.RestaurantOrder != null &&
                options.RestaurantOrder.State == OperationState.Draft)
                sale.Id = options.RestaurantOrder.Id;

            Sale deltaSale = options.CustomerOrderDeltaSale;
            string title = options.CustomerOrderTitle;
            bool showPrice = !isKitchenReceipt;
            List<DeviceCommands> supportedCommands = printer.SupportedCommands;

            if (!supportedCommands.Contains (DeviceCommands.OpenNonFiscal) ||
                (!supportedCommands.Contains (DeviceCommands.PrintTextNonFiscal) && !supportedCommands.Contains (DeviceCommands.AddItemNonFiscal)) ||
                !supportedCommands.Contains (DeviceCommands.CloseNonFiscal))
                return;

            string orderString = string.Format (Translator.GetString ("Order code: {0}"), -10 - sale.Id);
            string locationString = string.Format (Translator.GetString ("Location: {0}"), sale.Location2);
            string partnerString = string.Format (Translator.GetString ("Partner: {0}"), sale.PartnerName2);
            string operatorString = string.Format (Translator.GetString ("Operator: {0}"), sale.UserName2);

            TryDeviceCommand (delegate
                {
                    printer.OpenNonFiscal (isKitchenReceipt);

                    if (title.Length > 0) {
                        if (supportedCommands.Contains (DeviceCommands.PrintTitleNonFiscal))
                            printer.PrintTitleNonFiscal (title);
                        else if (supportedCommands.Contains (DeviceCommands.PrintTextNonFiscal)) {
                            printer.PrintTextNonFiscal (title.AlignCenter (printer.NonFiscalTextCharsPerLine));
                            printer.PrintTextNonFiscal (DriverBase.SEPARATOR);
                        }
                    }

                    if (supportedCommands.Contains (DeviceCommands.PrintTextNonFiscal) && BusinessDomain.AppConfiguration.PrintOrderCodeOnReceipts)
                        printer.PrintTextNonFiscal (orderString);

                    if (supportedCommands.Contains (DeviceCommands.SetLocation))
                        printer.SetLocation (sale.Location2);

                    if (supportedCommands.Contains (DeviceCommands.PrintTextNonFiscal) && BusinessDomain.AppConfiguration.PrintLocationOnReceipts)
                        printer.PrintTextNonFiscal (locationString);

                    if (supportedCommands.Contains (DeviceCommands.SetPartner))
                        printer.SetPartner (Partner.Cache.GetById (sale.PartnerId));

                    if (supportedCommands.Contains (DeviceCommands.PrintTextNonFiscal) && BusinessDomain.AppConfiguration.PrintPartnerOnReceipts)
                        printer.PrintTextNonFiscal (partnerString);

                    if (supportedCommands.Contains (DeviceCommands.SetOperator))
                        printer.SetOperator (sale.UserName2);

                    if (supportedCommands.Contains (DeviceCommands.PrintTextNonFiscal) && BusinessDomain.AppConfiguration.PrintOperatorOnReceipts)
                        printer.PrintTextNonFiscal (operatorString);

                    if (supportedCommands.Contains (DeviceCommands.PrintTextNonFiscal))
                        printer.PrintTextNonFiscal (DriverBase.SEPARATOR);
                }, options.SilentMode);

            int totalDetails = sale.Details.Count + (deltaSale != null ? deltaSale.Details.Count : 0);
            string discountString = Translator.GetString ("Discount");
            string allowanceString = Translator.GetString ("Allowance");
            for (int i = 0; i < sale.Details.Count; i++) {
                int i1 = i;
                TryDeviceCommand (delegate
                    {
                        SaleDetail detail = sale.Details [i1];
                        if (supportedCommands.Contains (DeviceCommands.AddItemNonFiscal)) {
                            printer.AddItemNonFiscal (detail.ItemCode, detail.ItemName2,
                                detail.Quantity,
                                detail.MUnitName,
                                detail.OriginalPriceOutPlusVAT,
                                detail.VatRate,
                                -detail.Discount,
                                detail.ItemGroupId,
                                detail.VatGroup != null ? detail.VatGroup.CodeValue : FiscalPrinterTaxGroup.Default,
                                GetModifierLines (options, detail.Note, false));
                            return;
                        }

                        PrintItemInfo (detail.ItemName2.Trim (printer.NonFiscalTextCharsPerLine),
                            detail.Quantity,
                            detail.MUnitName,
                            showPrice, detail.OriginalPriceOutPlusVAT, printer.NonFiscalTextCharsPerLine, printer.PrintTextNonFiscal);

                        if (showPrice && !detail.Discount.IsZero ())
                            printer.PrintTextNonFiscal (GetDiscountLine (printer.NonFiscalTextCharsPerLine, detail.Quantity, detail.OriginalPriceOutPlusVAT, detail.Discount, discountString, allowanceString));

                        foreach (string modifier in GetModifierLines (options, detail.Note, false))
                            printer.PrintTextNonFiscal (modifier);
                    }, options.SilentMode);

                OnReceiptPrintStep ((((double) currentPrinter * 100) / printers) + ((double) (i + 1) * 100) / (totalDetails * printers), options);
            }

            if (deltaSale != null) {
                string modString = Translator.GetString ("Modifications");
                string oldString = Translator.GetString ("old") + " ";
                string newString = Translator.GetString ("new") + " ";

                for (int i = 0; i < deltaSale.Details.Count; i++) {
                    int i1 = i;
                    TryDeviceCommand (delegate
                        {
                            SaleDetail detail = deltaSale.Details [i1];
                            if (supportedCommands.Contains (DeviceCommands.AddModifiedItemNonFiscal)) {
                                string [] oldModifiers;
                                string [] modifiers;
                                if (options.SkipPrintingNotes || detail.OriginalNote == detail.Note) {
                                    oldModifiers = new string [0];
                                    modifiers = new string [0];
                                } else {
                                    oldModifiers = GetModifierLines (options, detail.OriginalNote, true);
                                    modifiers = GetModifierLines (options, detail.Note, true);
                                }

                                printer.AddModifiedItemNonFiscal (detail.ItemCode, detail.ItemName2, detail.OriginalQuantity, detail.Quantity,
                                    detail.MUnitName, detail.PriceOutPlusVAT, -detail.Discount, detail.VatRate,
                                    detail.VatGroup.CodeValue, oldModifiers, modifiers);
                                return;
                            }

                            if (!supportedCommands.Contains (DeviceCommands.PrintTextNonFiscal))
                                return;

                            if (i1 == 0) {
                                if (sale.Details.Count > 0)
                                    printer.PrintTextNonFiscal (DriverBase.SEPARATOR);
                                printer.PrintTextNonFiscal (modString);
                            } else
                                printer.PrintTextNonFiscal (" ");

                            printer.PrintTextNonFiscal (detail.ItemName2.Trim (printer.NonFiscalTextCharsPerLine));

                            if (!detail.OriginalQuantity.IsEqualTo (detail.Quantity)) {
                                printer.PrintTextNonFiscal (GetQuantityLine (printer.NonFiscalTextCharsPerLine, oldString, detail.OriginalQuantity, detail.MUnitName, showPrice, detail.PriceOutPlusVAT));
                                printer.PrintTextNonFiscal (GetQuantityLine (printer.NonFiscalTextCharsPerLine, newString, detail.Quantity, detail.MUnitName, showPrice, detail.PriceOutPlusVAT));
                            }

                            if (!options.SkipPrintingNotes && !detail.OriginalNote.IsEqualTo (detail.Note)) {
                                printer.PrintTextNonFiscal (oldString);
                                foreach (string modifier in GetModifierLines (options, detail.OriginalNote, true)) {
                                    printer.PrintTextNonFiscal (modifier);
                                }

                                printer.PrintTextNonFiscal (newString);
                                foreach (string modifier in GetModifierLines (options, detail.Note, true)) {
                                    printer.PrintTextNonFiscal (modifier);
                                }
                            }
                        }, options.SilentMode);

                    OnReceiptPrintStep ((((double) currentPrinter * 100) / printers) + ((double) (i + sale.Details.Count + 1) * 100) / (totalDetails * printers), options);
                }
            }

            if (showPrice && !supportedCommands.Contains (DeviceCommands.AddItemNonFiscal)) {
                string docTotal = Translator.GetString ("TOTAL");
                TryDeviceCommand (delegate
                    {
                        printer.PrintTextNonFiscal (" ");
                        string totalValue = Currency.ToString (sale.TotalPlusVAT);
                        printer.PrintTextNonFiscal (docTotal.PadRight (printer.NonFiscalTextCharsPerLine - totalValue.Length, '.') + totalValue);
                    }, options.SilentMode);
            }

            TryDeviceCommand (printer.CloseNonFiscal, options.SilentMode);
        }

        private void PrintCustomerOrderInvoice (FinalizeOperationOptions options)
        {
            if (!BusinessDomain.AppConfiguration.CustomerOrdersPrinterEnabled ||
                options.CashReceiptSale.Details.Count == 0)
                return;

            if (!CustomerOrderPrinterConnected)
                ConnectCustomerOrderPrinter ();

            if (customerOrderDriver.LastErrorState.Errors.Count > 0)
                throw new HardwareErrorException (customerOrderDriver.LastErrorState);

            FinalizeOperationOptions optionsClone = (FinalizeOperationOptions) options.Clone ();
            optionsClone.SkipPrintingNotes = true;

            try {
                OnReceiptPrintStart (Translator.GetString ("Printing invoice..."), options);
                ICashReceiptPrinterController cashReceiptPrinterController = customerOrderDriver as ICashReceiptPrinterController;
                if (cashReceiptPrinterController != null) {
                    Invoice invoice = options.CustomerOrderInvoice;

                    if (invoice.PrintOriginal)
                        PrintCustomerOrderInvoice (options, cashReceiptPrinterController, true);

                    for (int i = 0; i < invoice.PrintCopies; i++)
                        PrintCustomerOrderInvoice (options, cashReceiptPrinterController, false);
                } else {
                    IKitchenPrinterController kitchenPrinterController = customerOrderDriver as IKitchenPrinterController;
                    if (kitchenPrinterController != null) {
                        //optionsClone.KitchenSale = optionsClone.CustomerOrderSale;
                        //optionsClone.KitchenDeltaSale = optionsClone.CustomerOrderDeltaSale;
                        //optionsClone.KitchenTitle = optionsClone.CustomerOrderTitle;

                        //PrintKitchenOrder (optionsClone, kitchenPrinterController, 1, 0, false);
                    }
                }
            } finally {
                OnReceiptPrintEnd (options);
            }

        }

        private void PrintCustomerOrderInvoice (FinalizeOperationOptions options, ICashReceiptPrinterController printer, bool isOriginal)
        {
            Sale sale = options.CustomerOrderSale;
            Invoice invoice = options.CustomerOrderInvoice;

            List<DeviceCommands> supportedCommands = printer.SupportedCommands;
            if (!supportedCommands.Contains (DeviceCommands.OpenNonFiscal) ||
                !supportedCommands.Contains (DeviceCommands.PrintTextNonFiscal) ||
                !supportedCommands.Contains (DeviceCommands.CloseNonFiscal))
                return;

            string invoiceTitle = Translator.GetString ("INVOICE");
            string invoiceNumber = string.Format (Translator.GetString ("Number: {0}"), invoice.DocumentNumber);
            string invoiceDate = string.Format (Translator.GetString ("Date: {0}"), invoice.DateString);
            string invoiceOriginal = isOriginal ?
                Translator.GetString ("ORIGINAL") :
                Translator.GetString ("COPY");
            string invoiceSuppNumber = Translator.GetString ("UIC:") + " " + invoice.SupplierNumber;
            string invoiceSuppTax = BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                Translator.GetString ("Tax number:") + " " + invoice.SupplierTaxNumber :
                Translator.GetString ("VAT number:") + " " + invoice.SupplierTaxNumber;

            string operatorString = string.Format (Translator.GetString ("Operator: {0}"), sale.UserName2);

            TryDeviceCommand (delegate
                {
                    printer.OpenNonFiscal (false);

                    if (supportedCommands.Contains (DeviceCommands.PrintTitleNonFiscal))
                        printer.PrintTitleNonFiscal (invoiceTitle);
                    else if (supportedCommands.Contains (DeviceCommands.PrintTextNonFiscal))
                        printer.PrintTextNonFiscal (invoiceTitle.AlignCenter (printer.NonFiscalTextCharsPerLine));

                    printer.PrintTextNonFiscal (invoiceNumber.AlignCenter (printer.NonFiscalTextCharsPerLine));
                    printer.PrintTextNonFiscal (invoiceDate.AlignCenter (printer.NonFiscalTextCharsPerLine));
                    printer.PrintTextNonFiscal (invoiceOriginal.AlignCenter (printer.NonFiscalTextCharsPerLine));
                    printer.PrintTextNonFiscal (string.Empty);
                    printer.PrintTextNonFiscal (invoice.SupplierName.AlignCenter (printer.NonFiscalTextCharsPerLine));
                    if (!string.IsNullOrWhiteSpace (invoice.SupplierAddress))
                        printer.PrintTextNonFiscal (invoice.SupplierAddress.AlignCenter (printer.NonFiscalTextCharsPerLine));
                    if (!string.IsNullOrWhiteSpace (invoice.SupplierTaxNumber))
                        printer.PrintTextNonFiscal (invoiceSuppTax.AlignCenter (printer.NonFiscalTextCharsPerLine));
                    if (!string.IsNullOrWhiteSpace (invoice.SupplierNumber))
                        printer.PrintTextNonFiscal (invoiceSuppNumber.AlignCenter (printer.NonFiscalTextCharsPerLine));
                    printer.PrintTextNonFiscal (string.Empty);
                }, options.SilentMode);

            int totalDetails = sale.Details.Count;
            string discountString = Translator.GetString ("Discount");
            string allowanceString = Translator.GetString ("Allowance");
            for (int i = 0; i < sale.Details.Count; i++) {
                int i1 = i;
                TryDeviceCommand (delegate
                    {
                        SaleDetail detail = sale.Details [i1];
                        if (supportedCommands.Contains (DeviceCommands.AddItemNonFiscal)) {
                            printer.AddItemNonFiscal (detail.ItemCode, detail.ItemName2,
                                detail.Quantity,
                                detail.MUnitName,
                                detail.OriginalPriceOutPlusVAT,
                                detail.VatRate,
                                -detail.Discount,
                                detail.ItemGroupId,
                                detail.VatGroup != null ? detail.VatGroup.CodeValue : FiscalPrinterTaxGroup.Default,
                                GetModifierLines (options, detail.Note, false));
                            return;
                        }

                        PrintItemInfo (detail.ItemName2.Trim (printer.NonFiscalTextCharsPerLine),
                            detail.Quantity,
                            detail.MUnitName,
                            true, detail.OriginalPriceOutPlusVAT, printer.NonFiscalTextCharsPerLine, printer.PrintTextNonFiscal);

                        if (!detail.Discount.IsZero ())
                            printer.PrintTextNonFiscal (GetDiscountLine (printer.NonFiscalTextCharsPerLine, detail.Quantity, detail.OriginalPriceOutPlusVAT, detail.Discount, discountString, allowanceString));
                    }, options.SilentMode);

                OnReceiptPrintStep (((double) (i + 1) * 100) / totalDetails, options);
            }

            if (!supportedCommands.Contains (DeviceCommands.AddItemNonFiscal)) {
                string docTotal = Translator.GetString ("TOTAL");
                string totalValue = Currency.ToString (sale.TotalPlusVAT);
                TryDeviceCommand (delegate
                    {
                        printer.PrintTextNonFiscal (" ");
                        printer.PrintTextNonFiscal (docTotal.PadRight (printer.NonFiscalTextCharsPerLine - totalValue.Length, '.') + totalValue);
                    }, options.SilentMode);
            }

            string netLabel = Translator.GetString ("Net Amount") + ": " + invoice.TotalString;
            SortedDictionary<string, string> vatGrouped = invoice.VatGrouped;
            string totalLabel = Translator.GetString ("Total") + ": " + invoice.TotalPlusVatString;

            string recName = Translator.GetString ("Recipient:") + " " + invoice.RecipientName;
            string recNumber = Translator.GetString ("UIC:") + " " + invoice.RecipientNumber;
            string recTax = BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                Translator.GetString ("Tax number:") + " " + invoice.RecipientTaxNumber :
                Translator.GetString ("VAT number:") + " " + invoice.RecipientTaxNumber;
            string recAddress = Translator.GetString ("Address:") + " " + invoice.RecipientAddress;
            string recContact = Translator.GetString ("Contact Name:") + " " + invoice.RecipientLiablePerson;

            string recBy = Translator.GetString ("Received by:") + " " + invoice.Recipient;
            string issBy = Translator.GetString ("Issued by:") + " " + invoice.Recipient;
            string sign = Translator.GetString ("Signature:");
            string finEvent = Translator.GetString ("Financial event:") + " " + invoice.TaxDateString;
            string payment = Translator.GetString ("Payment:") + " " + invoice.PaymentMethodString;

            TryDeviceCommand (delegate
                {
                    printer.PrintTextNonFiscal (string.Empty);
                    printer.PrintTextNonFiscal (netLabel.AlignRight (printer.NonFiscalTextCharsPerLine));
                    foreach (var vat in vatGrouped)
                        printer.PrintTextNonFiscal ((vat.Key + ": " + vat.Value).AlignRight (printer.NonFiscalTextCharsPerLine));
                    printer.PrintTextNonFiscal (totalLabel.AlignRight (printer.NonFiscalTextCharsPerLine));

                    printer.PrintTextNonFiscal (string.Empty);

                    printer.PrintTextNonFiscal (recName);
                    printer.PrintTextNonFiscal (recAddress);
                    printer.PrintTextNonFiscal (recTax);
                    printer.PrintTextNonFiscal (recNumber);
                    printer.PrintTextNonFiscal (recContact);

                    printer.PrintTextNonFiscal (DriverBase.SEPARATOR);

                    printer.PrintTextNonFiscal (recBy);
                    printer.PrintTextNonFiscal (sign);
                    printer.PrintTextNonFiscal (string.Empty);
                    printer.PrintTextNonFiscal (issBy);
                    printer.PrintTextNonFiscal (sign);

                    printer.PrintTextNonFiscal (DriverBase.SEPARATOR);

                    printer.PrintTextNonFiscal (finEvent);
                    printer.PrintTextNonFiscal (payment);
                }, options.SilentMode);

            TryDeviceCommand (printer.CloseNonFiscal, options.SilentMode);
        }

        private void CollectSaleData (FinalizeOperationOptions options)
        {
            if (salesDataControllerDriver == null || !salesDataControllerDriver.SupportedCommands.Contains (DeviceCommands.SignSale))
                return;

            try {
                OnReceiptPrintStart (Translator.GetString ("Recording sale to fiscal device..."), options);
                TryDeviceCommand (() => salesDataControllerDriver.SignSale (options.Sale, options.FinalPayments), options.SilentMode);
            } finally {
                OnReceiptPrintEnd (options);
            }
        }

        #region Cash receipts

        private void PrintCashReceiptSale (FinalizeOperationOptions options)
        {
            Sale sale = options.CashReceiptSale;

            try {
                OnReceiptPrintStart (Translator.GetString ("Printing cash receipt..."), options);
                string saleCode = string.Format (Translator.GetString ("Sale code: {0}"), sale.Id);
                string orderString = options.RestaurantOrder != null ?
                    string.Format (Translator.GetString ("Order code: {0}"), -10 - options.RestaurantOrder.Id) : string.Empty;
                string location = string.Format (Translator.GetString ("Location: {0}"), sale.Location2);
                string partner = string.Format (Translator.GetString ("Partner: {0}"), sale.PartnerName2);
                string oper = string.Format (Translator.GetString ("Operator: {0}"), sale.UserName2);
                bool printSaleCode = BusinessDomain.AppConfiguration.PrintSaleCode && sale.State == OperationState.Saved;

                TryDeviceCommand (delegate
                    {
                        int comments = 0;
                        if (cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.PrintTextFiscal)) {
                            if (printSaleCode)
                                comments++;
                            if (BusinessDomain.AppConfiguration.PrintOrderCodeOnReceipts)
                                comments++;
                            if (BusinessDomain.AppConfiguration.PrintLocationOnReceipts)
                                comments++;
                            if (BusinessDomain.AppConfiguration.PrintPartnerOnReceipts)
                                comments++;
                            if (BusinessDomain.AppConfiguration.PrintOperatorOnReceipts)
                                comments++;

                            comments++;
                        }

                        InitCashReceipt (sale, comments);

                        cashReceiptPrinterDriver.OpenFiscal ();
                        if (!cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.PrintTextFiscal))
                            return;

                        if (printSaleCode)
                            cashReceiptPrinterDriver.PrintTextFiscal (saleCode, DriverBase.TextAlign.Left);

                        if (BusinessDomain.AppConfiguration.PrintOrderCodeOnReceipts)
                            cashReceiptPrinterDriver.PrintTextFiscal (orderString, DriverBase.TextAlign.Left);

                        if (BusinessDomain.AppConfiguration.PrintLocationOnReceipts)
                            cashReceiptPrinterDriver.PrintTextFiscal (location, DriverBase.TextAlign.Left);

                        if (BusinessDomain.AppConfiguration.PrintPartnerOnReceipts)
                            cashReceiptPrinterDriver.PrintTextFiscal (partner, DriverBase.TextAlign.Left);

                        if (BusinessDomain.AppConfiguration.PrintOperatorOnReceipts)
                            cashReceiptPrinterDriver.PrintTextFiscal (oper, DriverBase.TextAlign.Left);

                        cashReceiptPrinterDriver.PrintTextFiscal (DriverBase.SEPARATOR, DriverBase.TextAlign.Left);
                    }, options.SilentMode);

                PrintCashReceiptBody (options);

                TryDeviceCommand (cashReceiptPrinterDriver.CloseFiscal, options.SilentMode);
            } finally {
                OnReceiptPrintEnd (options);
            }
        }

        private void PrintCashReceiptInvoice (FinalizeOperationOptions options)
        {
            Invoice invoice = options.CashReceiptInvoice;

            if (invoice.PrintOriginal)
                PrintCashReceiptInvoice (options, true);

            for (int i = 0; i < invoice.PrintCopies; i++)
                PrintCashReceiptInvoice (options, false);
        }

        private void PrintCashReceiptInvoice (FinalizeOperationOptions options, bool isOriginal)
        {
            Sale sale = options.CashReceiptSale;
            Invoice invoice = options.CashReceiptInvoice;

            try {
                OnReceiptPrintStart (Translator.GetString ("Printing invoice..."), options);

                string invoiceTitle = Translator.GetString ("INVOICE");
                string invoiceNumber = string.Format (Translator.GetString ("Number: {0}"), invoice.DocumentNumber);
                string invoiceDate = string.Format (Translator.GetString ("Date: {0}"), invoice.DateString);
                string invoiceOriginal = isOriginal ?
                    Translator.GetString ("ORIGINAL") :
                    Translator.GetString ("COPY");
                string invoiceSuppNumber = Translator.GetString ("UIC:") + " " + invoice.SupplierNumber;
                string invoiceSuppTax = BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                    Translator.GetString ("Tax number:") + " " + invoice.SupplierTaxNumber :
                    Translator.GetString ("VAT number:") + " " + invoice.SupplierTaxNumber;

                TryDeviceCommand (delegate
                    {
                        int comments = 0;
                        if (cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.PrintTextFiscal))
                            comments = 22;

                        InitCashReceipt (sale, comments);

                        cashReceiptPrinterDriver.OpenFiscal ();
                        if (!cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.PrintTextFiscal))
                            return;

                        cashReceiptPrinterDriver.PrintTextFiscal (invoiceTitle, DriverBase.TextAlign.Center);
                        cashReceiptPrinterDriver.PrintTextFiscal (invoiceNumber, DriverBase.TextAlign.Center);
                        cashReceiptPrinterDriver.PrintTextFiscal (invoiceDate, DriverBase.TextAlign.Center);
                        cashReceiptPrinterDriver.PrintTextFiscal (invoiceOriginal, DriverBase.TextAlign.Center);
                        object hasFiscal;
                        if (!cashReceiptPrinterDriver.GetAttributes ().TryGetValue (DriverBase.HAS_FISCAL_MEMORY, out hasFiscal) ||
                            (bool) hasFiscal)
                            return;

                        cashReceiptPrinterDriver.PrintTextFiscal (string.Empty, DriverBase.TextAlign.Center);
                        cashReceiptPrinterDriver.PrintTextFiscal (invoice.SupplierName, DriverBase.TextAlign.Center);
                        if (!string.IsNullOrWhiteSpace (invoice.SupplierAddress))
                            cashReceiptPrinterDriver.PrintTextFiscal (invoice.SupplierAddress, DriverBase.TextAlign.Center);
                        if (!string.IsNullOrWhiteSpace (invoice.SupplierTaxNumber))
                            cashReceiptPrinterDriver.PrintTextFiscal (invoiceSuppTax, DriverBase.TextAlign.Center);
                        if (!string.IsNullOrWhiteSpace (invoice.SupplierNumber))
                            cashReceiptPrinterDriver.PrintTextFiscal (invoiceSuppNumber, DriverBase.TextAlign.Center);
                        cashReceiptPrinterDriver.PrintTextFiscal (string.Empty, DriverBase.TextAlign.Center);
                    }, options.SilentMode);

                PrintCashReceiptBody (options);

                if (cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.PrintTextFiscal)) {
                    string netLabel = Translator.GetString ("Net Amount") + ": " + invoice.TotalString;
                    SortedDictionary<string, string> vatGrouped = invoice.VatGrouped;
                    string totalLabel = Translator.GetString ("Total") + ": " + invoice.TotalPlusVatString;

                    string recName = Translator.GetString ("Recipient:") + " " + invoice.RecipientName;
                    string recNumber = Translator.GetString ("UIC:") + " " + invoice.RecipientNumber;
                    string recTax = BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                        Translator.GetString ("Tax number:") + " " + invoice.RecipientTaxNumber :
                        Translator.GetString ("VAT number:") + " " + invoice.RecipientTaxNumber;
                    string recAddress = Translator.GetString ("Address:") + " " + invoice.RecipientAddress;
                    string recContact = Translator.GetString ("Contact Name:") + " " + invoice.RecipientLiablePerson;

                    string recBy = Translator.GetString ("Received by:") + " " + invoice.Recipient;
                    string issBy = Translator.GetString ("Issued by:") + " " + invoice.Provider;
                    string sign = Translator.GetString ("Signature:");
                    string finEvent = Translator.GetString ("Financial event:") + " " + invoice.TaxDateString;
                    
                    TryDeviceCommand (delegate
                        {
                            cashReceiptPrinterDriver.PrintTextFiscal (string.Empty, DriverBase.TextAlign.Left);
                            cashReceiptPrinterDriver.PrintTextFiscal (netLabel, DriverBase.TextAlign.Right);
                            foreach (var vat in vatGrouped)
                                cashReceiptPrinterDriver.PrintTextFiscal (vat.Key + ": " + vat.Value, DriverBase.TextAlign.Right);
                            cashReceiptPrinterDriver.PrintTextFiscal (totalLabel, DriverBase.TextAlign.Right);
                            
                            cashReceiptPrinterDriver.PrintTextFiscal (string.Empty, DriverBase.TextAlign.Left);

                            cashReceiptPrinterDriver.PrintTextFiscal (recName, DriverBase.TextAlign.Left);
                            cashReceiptPrinterDriver.PrintTextFiscal (recAddress, DriverBase.TextAlign.Left);
                            cashReceiptPrinterDriver.PrintTextFiscal (recTax, DriverBase.TextAlign.Left);
                            cashReceiptPrinterDriver.PrintTextFiscal (recNumber, DriverBase.TextAlign.Left);
                            cashReceiptPrinterDriver.PrintTextFiscal (recContact, DriverBase.TextAlign.Left);

                            cashReceiptPrinterDriver.PrintTextFiscal (DriverBase.SEPARATOR, DriverBase.TextAlign.Left);

                            cashReceiptPrinterDriver.PrintTextFiscal (recBy, DriverBase.TextAlign.Left);
                            cashReceiptPrinterDriver.PrintTextFiscal (sign, DriverBase.TextAlign.Left);
                            cashReceiptPrinterDriver.PrintTextFiscal (string.Empty, DriverBase.TextAlign.Left);
                            cashReceiptPrinterDriver.PrintTextFiscal (issBy, DriverBase.TextAlign.Left);
                            cashReceiptPrinterDriver.PrintTextFiscal (sign, DriverBase.TextAlign.Left);

                            cashReceiptPrinterDriver.PrintTextFiscal (DriverBase.SEPARATOR, DriverBase.TextAlign.Left);

                            cashReceiptPrinterDriver.PrintTextFiscal (finEvent, DriverBase.TextAlign.Left);
                        }, options.SilentMode);
                }

                TryDeviceCommand (cashReceiptPrinterDriver.CloseFiscal, options.SilentMode);
            } finally {
                OnReceiptPrintEnd (options);
            }
        }

        private void InitCashReceipt (Sale sale, int comments)
        {
            if (cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.InitFiscal)) {
                int discounted = sale.Details.Count (detail => !detail.Discount.IsZero ());
                cashReceiptPrinterDriver.InitFiscal (comments, sale.Details.Count, discounted, sale.Payments.Count);
            }

            if (cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.SetPartner))
                cashReceiptPrinterDriver.SetPartner (Partner.Cache.GetById (sale.PartnerId));

            if (cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.SetReceiptCode)) {
                if (sale.State == OperationState.Saved)
                    cashReceiptPrinterDriver.SetReceiptCode (sale.Id.ToString (CultureInfo.InvariantCulture));
                else
                    cashReceiptPrinterDriver.SetReceiptCode ("D" + sale.Details.First ().DetailId.ToString (CultureInfo.InvariantCulture));
            }

            if (cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.SetOperator))
                cashReceiptPrinterDriver.SetOperator (sale.UserName2);
        }

        private void PrintCashReceiptBody (FinalizeOperationOptions options)
        {
            Sale sale = options.CashReceiptSale;
            if (sale.State == OperationState.Saved && options.FinalPayments.Count == 0) {
                Payment [] paidPayments = Payment.GetForOperation (sale, PaymentMode.Paid);
                if (paidPayments.Length == 0)
                    paidPayments = new [] { new Payment (sale, (int) BasePaymentType.Cash, PaymentMode.Paid) };

                options.FinalPayments.AddRange (paidPayments);
            }

            double? globalVatRate = null;
            FiscalPrinterTaxGroup? globalTaxGroup = null;
            if (sale.IsVATExempt || (sale.VAT.IsZero () && sale.Total > 0)) {
                VATGroup exGroup = VATGroup.GetExemptGroup ();
                globalVatRate = 0;
                globalTaxGroup = exGroup != null ? exGroup.CodeValue : FiscalPrinterTaxGroup.TaxExempt;
            }

            int totalDetails = sale.Details.Count + options.FinalPayments.Count;
            if (cashReceiptPrinterDriver.PrintTotalOnly) {
                string totalString = Translator.GetString ("Total");
                double vatRate = globalVatRate ?? sale.Details.Last ().VatRate;
                FiscalPrinterTaxGroup taxGroup = globalTaxGroup ?? sale.Details.Last ().VatCodeValue;

                TryDeviceCommand (() => cashReceiptPrinterDriver.AddItem ("1", totalString, 1d, string.Empty,
                    sale.TotalPlusVAT, vatRate, 0, taxGroup), options.SilentMode);

                OnReceiptPrintStep (((double) (sale.Details.Count) * 100) / totalDetails, options);
            } else {
                for (int i = 0; i < sale.Details.Count; i++) {
                    SaleDetail detail = sale.Details [i];
                    if (detail.Quantity <= 0)
                        continue;

                    TryDeviceCommand (() => cashReceiptPrinterDriver.AddItem (detail.ItemCode, detail.ItemName2, detail.Quantity, detail.MUnitName,
                        detail.OriginalPriceOutPlusVAT, globalVatRate ?? detail.VatRate, -detail.Discount,
                        globalTaxGroup ?? detail.VatCodeValue), options.SilentMode);

                    OnReceiptPrintStep (((double) (i + 1) * 100) / totalDetails, options);
                }
            }

            // Some fiscal printers return change only out of the last payment and
            // we want to make sure that cash ones are at the end
            List<Payment> payments = new List<Payment> (options.FinalPayments);
            payments.Sort ((a, b) =>
                {
                    if (a.Type.BaseType > b.Type.BaseType)
                        return -1;

                    return a.Type.BaseType < b.Type.BaseType ? 1 : 0;
                });

            for (int i = 0; i < payments.Count; i++) {
                Payment payment = payments [i];
                TryDeviceCommand (() => cashReceiptPrinterDriver.Payment (payment.Quantity, payment.Type.BaseType), options.SilentMode);

                OnReceiptPrintStep (((double) (i + sale.Details.Count + 1) * 100) / totalDetails, options);
            }

            if (sale.Signature != null &&
                cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.PrintSignature)) {
                TryDeviceCommand (() => cashReceiptPrinterDriver.PrintSignature (sale.Signature), options.SilentMode);
            }

            if (sale.State == OperationState.Saved &&
                BusinessDomain.AppConfiguration.PrintSaleBarCode &&
                cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.PrintBarcode)) {
                GeneratedBarcodeType barType = BusinessDomain.AppConfiguration.SaleBarCodeType;
                int numberLength = barType == GeneratedBarcodeType.EAN8 ? 7 : 12;
                string numberText = Math.Abs (sale.Id).ToString ();

                numberText = numberText.Length > numberLength ? numberText.Substring (0, numberLength) : numberText.PadLeft (numberLength, '0');
                TryDeviceCommand (() => cashReceiptPrinterDriver.PrintBarcode (barType, numberText, BusinessDomain.AppConfiguration.PrintSaleBarCodeNumber), options.SilentMode);
            }
        }

        #endregion

        private void OpenCashDrawer (FinalizeOperationOptions options)
        {
            if (cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.OpenCashDrawer))
                TryDeviceCommand (cashReceiptPrinterDriver.OpenCashDrawer, options.SilentMode);
        }

        private static void PrintItemInfo (string itemName, double qty, string measUnit, bool showPrice, double price, int maxChars, Action<string> printLine)
        {
            string quantityLine = GetQuantityLine (maxChars, string.Empty, qty, measUnit, showPrice, price, false);

            if (itemName.Length + quantityLine.Length + 1 <= maxChars) {
                printLine (itemName.PadRight (maxChars - quantityLine.Length) + quantityLine);
                return;
            }

            printLine (itemName);
            printLine (quantityLine.PadLeft (maxChars));
        }

        private static string GetQuantityLine (int maxChars, string prexif, double qty, string measUnit, bool showPrice, double price, bool align = true)
        {
            string itemQty = Quantity.ToString (qty);
            string itemPrice = Currency.ToString (price);
            string itemTotal = Currency.ToString (qty * price);
            if (string.IsNullOrEmpty (prexif))
                prexif = string.Empty;

            string line;
            if (showPrice) {
                line = string.Format ("{0}{1} {2} x {3} = {4}", prexif, itemQty, measUnit, itemPrice, itemTotal);
                if (line.Length > maxChars) {
                    line = string.Format ("{0}{1} {2} = {3}", prexif, itemQty, measUnit, itemTotal);
                    if (line.Length > maxChars) {
                        line = string.Format ("{0}{1}", prexif, itemTotal);
                    }
                }
            } else {
                line = string.Format ("{0}{1} {2}", prexif, itemQty, measUnit);
                if (line.Length > maxChars) {
                    line = string.Format ("{0}{1}", prexif, itemQty);
                }
            }

            return align ? line.PadLeft (maxChars) : line;
        }

        private static string GetDiscountLine (int maxChars, double qty, double price, double discount, string discountString, string allowanceString)
        {
            if (discount.IsZero ())
                return string.Empty;

            double value = Currency.Round (Currency.Round (price * -discount / 100) * qty);

            string line = discount > 0
                ? string.Format ("{0} {1} = {2}", Percent.ToString (discount), discountString, Currency.ToString (value))
                : string.Format ("{0} {1} = {2}", Percent.ToString (-discount), allowanceString, Currency.ToString (value));

            return line.PadLeft (maxChars);
        }

        private static string [] GetModifierLines (FinalizeOperationOptions options, string note, bool addEmpty)
        {
            if (options.SkipPrintingNotes || string.IsNullOrWhiteSpace (note)) {
                return addEmpty ? new [] { "> ---" } : new string [0];
            }

            return note.Trim ().Split (',').Select (s => string.Format ("> {0}", s.Trim ())).ToArray ();
        }

        public void CancelFiscalReceipt (out bool annulled)
        {
            if (cashReceiptPrinterDriver == null ||
                !cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.CancelFiscalOperation)) {
                annulled = false;
                return;
            }

            bool a = false;
            TryDeviceCommand (() => cashReceiptPrinterDriver.CancelFiscalOperation (out a));
            annulled = a;
        }

        public void CancelNonFiscalReceipt ()
        {
            ICashReceiptPrinterController driver = customerOrderDriver as ICashReceiptPrinterController;

            if (driver == null ||
                !driver.SupportedCommands.Contains (DeviceCommands.CloseNonFiscal))
                return;

            TryDeviceCommand (driver.CloseNonFiscal);
        }

        public void OnReceiptPrintStart (string message, FinalizeOperationOptions options)
        {
            if (options.SilentMode)
                return;

            if (ReceiptPrintStart != null)
                ReceiptPrintStart (this, new ProgressStartEventArgs (message));
        }

        private void OnReceiptPrintStep (double progress, FinalizeOperationOptions options)
        {
            if (options.SilentMode)
                return;

            if (ReceiptPrintStep != null)
                ReceiptPrintStep (this, new ProgressStepEventArgs (progress));
        }

        private void OnReceiptPrintDialogShown (FinalizeOperationOptions options)
        {
            if (options.SilentMode)
                return;

            if (ReceiptPrintDialogShown != null)
                ReceiptPrintDialogShown (this, EventArgs.Empty);
        }

        public void OnReceiptPrintEnd (FinalizeOperationOptions options)
        {
            if (options.SilentMode)
                return;

            if (ReceiptPrintEnd != null)
                ReceiptPrintEnd (this, EventArgs.Empty);
        }

        #endregion

        #region Display management

        public void DisplaySaleDetail (Operation operation, SaleDetail detail)
        {
            if (!BusinessDomain.AppConfiguration.ExternalDisplayEnabled) {
                if (!BusinessDomain.WorkflowManager.AllowSaleWithoutDisplay)
                    throw new HardwareErrorException (new ErrorState (ErrorState.ExternalDisplayDisconnected, HardwareErrorSeverity.Error));

                return;
            }

            if (!ExternalDisplayConnected)
                ConnectExternalDisplay ();

            if (!displayDriver.SupportedCommands.Contains (DeviceCommands.DisplayUpperText) ||
                !displayDriver.SupportedCommands.Contains (DeviceCommands.DisplayLowerText))
                return;

            TryDeviceCommand (delegate
                {
                    string itemPrice = string.Empty;
                    double quantity = Quantity.Round (detail.Quantity);
                    if (!quantity.IsZero () && !quantity.IsEqualTo (1)) {
                        if (!detail.PriceOutPlusVAT.IsZero ()) {
                            itemPrice = string.Format ("{0} * {1} = ",
                                Currency.ToString (detail.PriceOutPlusVAT),
                                Number.ToEditString (quantity));
                        } else {
                            itemPrice = string.Format (" * {0} = ",
                                Number.ToEditString (quantity));
                        }
                    }

                    double total = detail.PriceOutPlusVAT;
                    if (!quantity.IsZero ())
                        total *= quantity;

                    int displayCharsPerLine = displayDriver.DisplayCharsPerLine;

                    itemPrice += Currency.ToString (total);
                    itemPrice = itemPrice.PadLeft (displayCharsPerLine);
                    if (BusinessDomain.AppConfiguration.ExtDisplayDigitsOnly) {
                        displayDriver.DisplayUpperText (itemPrice);
                        displayDriver.DisplayLowerText (Currency.ToString (operation.TotalPlusVAT).PadLeft (displayCharsPerLine));
                    } else {
                        string itemName = detail.ItemName2;
                        if (itemName.Length > displayCharsPerLine)
                            itemName = itemName.Substring (0, displayCharsPerLine);
                        else if (itemName.Length < displayCharsPerLine)
                            itemName = itemName.PadRight (displayCharsPerLine);

                        displayDriver.DisplayUpperText (itemName);
                        displayDriver.DisplayLowerText (itemPrice);
                    }
                });
        }

        public void DisplayTotal (double total)
        {
            if (!BusinessDomain.AppConfiguration.ExternalDisplayEnabled) {
                if (!BusinessDomain.WorkflowManager.AllowSaleWithoutDisplay)
                    throw new HardwareErrorException (new ErrorState (ErrorState.ExternalDisplayDisconnected, HardwareErrorSeverity.Error));

                return;
            }

            if (!ExternalDisplayConnected)
                ConnectExternalDisplay ();

            if (displayDriver == null ||
                !displayDriver.SupportedCommands.Contains (DeviceCommands.DisplayUpperText) ||
                !displayDriver.SupportedCommands.Contains (DeviceCommands.DisplayClear))
                return;

            string totalValue = Currency.ToString (total);
            int maxLineLen = displayDriver.DisplayCharsPerLine - totalValue.Length;
            string totalString = Translator.GetString ("TOTAL");
            if (totalString.Length > maxLineLen)
                totalString = totalString.Substring (0, maxLineLen);

            totalString = totalString.PadRight (maxLineLen) + totalValue;

            TryDeviceCommand (delegate
                {
                    displayDriver.DisplayClear ();
                    displayDriver.DisplayUpperText (totalString);
                });
        }

        public void DisplayPayment (double total, double totalReceived)
        {
            if (!BusinessDomain.AppConfiguration.ExternalDisplayEnabled) {
                if (!BusinessDomain.WorkflowManager.AllowSaleWithoutDisplay)
                    throw new HardwareErrorException (new ErrorState (ErrorState.ExternalDisplayDisconnected, HardwareErrorSeverity.Error));

                return;
            }

            if (!ExternalDisplayConnected)
                ConnectExternalDisplay ();

            if (!displayDriver.SupportedCommands.Contains (DeviceCommands.DisplayUpperText) ||
                !displayDriver.SupportedCommands.Contains (DeviceCommands.DisplayLowerText))
                return;

            int displayCharsPerLine = displayDriver.DisplayCharsPerLine;
            string recString = Currency.ToString (totalReceived);
            int maxLineLen = displayCharsPerLine - recString.Length;
            string receivedLine = Translator.GetString ("RECEIVED");
            if (receivedLine.Length > maxLineLen)
                receivedLine = receivedLine.Substring (0, maxLineLen);

            receivedLine = receivedLine.PadRight (maxLineLen) + recString;

            string changeLine;
            if (total.IsZero () || total.IsEqualTo (totalReceived)) {
                changeLine = string.Empty;
            } else {
                string changeString = Currency.ToString (Math.Abs (totalReceived - total));
                maxLineLen = displayCharsPerLine - changeString.Length;
                changeLine = Translator.GetString ("CHANGE");
                if (changeLine.Length > maxLineLen)
                    changeLine = changeLine.Substring (0, maxLineLen);

                changeLine = changeLine.PadRight (maxLineLen) + changeString;
            }

            TryDeviceCommand (delegate
                {
                    displayDriver.DisplayUpperText (receivedLine);
                    displayDriver.DisplayLowerText (changeLine);

                    lastPayment = BusinessDomain.Now;
                    welcomeDisplayed = false;
                });
        }

        #endregion

        #region Electronic scale management

        /// <summary>
        /// Sets the specified price to the connnected electronic scale, if any, 
        /// and returns the weight displayed on the scale.
        /// </summary>
        /// <param name="price">The price to set to to the connnected electronic scale, if any.</param>
        /// <returns>The weight displayed on the scale.</returns>
        public double GetWeightFromElectronicScale (double price)
        {
            if (!BusinessDomain.AppConfiguration.ElectronicScaleEnabled)
                throw new HardwareErrorException (new ErrorState (ErrorState.ElectronicScaleNotEnabled, HardwareErrorSeverity.Warning));
            //return 1;

            if (!ElectronicScaleConnected)
                ConnectElectronicScale ();

            if (electronicScaleDriver == null)
                throw new HardwareErrorException (new ErrorState ("Electonic Scale Driver Not Initialized", HardwareErrorSeverity.Warning));

            if (!electronicScaleDriver.SupportedCommands.Contains (DeviceCommands.GetWeight) &&
                !electronicScaleDriver.SupportedCommands.Contains (DeviceCommands.GetWeightAndSetPrice))
                throw new HardwareErrorException (new ErrorState ("Electonic Scale Cannot Measure Weight", HardwareErrorSeverity.Warning));
            //return 1;

            if (electronicScaleDriver.LastErrorState != null &&
                electronicScaleDriver.LastErrorState.Errors.Count > 0)
                throw new HardwareErrorException (electronicScaleDriver.LastErrorState);

            double weight = 1;
            if (electronicScaleDriver.SupportedCommands.Contains (DeviceCommands.GetWeightAndSetPrice)) {
                TryDeviceCommand (() => electronicScaleDriver.GetWeightAndSetPrice (out weight, price));
            } else {
                TryDeviceCommand (() => electronicScaleDriver.GetWeight (out weight));
                if (electronicScaleDriver.SupportedCommands.Contains (DeviceCommands.SetPrice)) {
                    TryDeviceCommand (() => electronicScaleDriver.SetPrice (price));
                }
            }

            return weight;
        }

        #endregion

        public delegate void CommandWrapper ();

        private static readonly object deviceWorkerLock = new object ();
        private static readonly object deviceCommandLock = new object ();
        private readonly AutoResetEvent deviceCommandStartEvent = new AutoResetEvent (false);
        private readonly AutoResetEvent deviceCommandEndEvent = new AutoResetEvent (false);
        private CommandWrapper deviceCommand;
        private TransactionContext deviceCommandTransactionContext;
        private Thread deviceWorkerThread;
        private bool deviceWorkerExit;
        private Exception lastException;
        private DateTime lastPayment = DateTime.Now;
        private DateTime lastStatusPoll = DateTime.Now;
        private bool welcomeDisplayed;

        public event EventHandler<ErrorStateEventArgs> HardwareError;
        public event EventHandler HardwareResponseWaitPoll;

        public void TryDeviceCommand (CommandWrapper cwDelegate, bool silent = false)
        {
            lock (deviceWorkerLock) {
                if (deviceWorkerThread == null)
                    return;

                lock (deviceCommandLock) {
                    lastException = null;
                    deviceCommand = cwDelegate;
                    deviceCommandTransactionContext = TransactionContext.Current;

                    deviceCommandEndEvent.Reset ();
                    deviceCommandStartEvent.Set ();
                }

                while (!deviceCommandEndEvent.WaitOne (100, false)) {
                    if (HardwareResponseWaitPoll != null && !silent)
                        HardwareResponseWaitPoll (this, EventArgs.Empty);
                }

                if (lastException == null)
                    return;

                ErrorHandling.LogException (lastException);
                throw new HardwareErrorException (lastException);
            }
        }

        private void DeviceWorkerStart ()
        {
            lock (deviceWorkerLock) {
                if (deviceWorkerThread != null)
                    return;

                deviceWorkerExit = false;
                deviceWorkerThread = new Thread (DeviceWorker) { Name = "Device Manager Worker" };
                deviceWorkerThread.Start ();
            }
        }

        private void DeviceWorkerExit ()
        {
            lock (deviceWorkerLock) {
                if (deviceWorkerThread == null)
                    return;

                deviceWorkerExit = true;
                deviceCommandStartEvent.Set ();
                deviceWorkerThread.Join ();
                deviceWorkerThread = null;
            }
        }

        public void DeviceWorkerResume ()
        {
            lock (deviceWorkerLock) {
                if (deviceWorkerThread == null)
                    return;

                deviceCommandStartEvent.Set ();
            }
        }

        private void DeviceWorker ()
        {
            bool error = false;
            bool lastError = false;
            bool commandExecuted = false;

            while (true) {
                try {
                    commandExecuted = false;
                    if (lastError != error)
                        OnCashReceiptPrinterChanged ();

                    lastError = error;
                    if (error) {
                        deviceCommandStartEvent.WaitOne ();
                    } else {
                        deviceCommandStartEvent.WaitOne (DEVICE_STATUS_POLL_INTERVAL, false);
                    }

                    if (deviceWorkerExit)
                        return;

                    lock (deviceCommandLock) {
                        try {
                            if (deviceCommand != null) {
                                try {
                                    TransactionContext.Current = deviceCommandTransactionContext;
                                    deviceCommand ();
                                    // if the command passes ok then clear the error flag if it was previously set
                                    error = false;
                                } catch (HardwareErrorException ex) {
                                    error = true;
                                    lastException = ex;
                                } finally {
                                    deviceCommand = null;
                                    deviceCommandTransactionContext = null;
                                    commandExecuted = true;
                                }
                            } else
                                error = CheckDeviceStatuses ();

                            DisplayAdvertismentMessage ();
                        } catch (ObjectDisposedException ex) {
                            lastException = ex;
                        } catch (Exception ex) {
                            lastException = ex;
                            error = true;
                        }
                    }
                } finally {
                    if (commandExecuted || deviceWorkerExit)
                        deviceCommandEndEvent.Set ();
                }
            }
        }

        private bool CheckDeviceStatuses ()
        {
            // If there is a printing operation stop the check
            // If we start checking the device status prevent starting of any printing operations
            if (!Monitor.TryEnter (finalizeOperationLock))
                return false;

            try {
                DateTime now = DateTime.Now;
                if ((now - lastStatusPoll).TotalMilliseconds > DEVICE_STATUS_POLL_INTERVAL) {
                    lastStatusPoll = now;
                    do {
                        try {
                            lock (cashReceiptPrinterDriverLock) {
                                if (CashReceiptPrinterConnected &&
                                    cashReceiptPrinterDriver.SupportedCommands.Contains (DeviceCommands.Ping)) {
                                    cashReceiptPrinterDriver.Ping ();
                                    if (cashReceiptPrinterDriver.LastErrorState.CheckError (ErrorState.ExternalDisplayDisconnected) ||
                                        cashReceiptPrinterDriver.LastErrorState.CheckWarning (ErrorState.ExternalDisplayDisconnected))
                                        throw new HardwareErrorException (cashReceiptPrinterDriver.LastErrorState);
                                }

                                lock (customerOrderDriverLock) {
                                    if (CustomerOrderPrinterConnected &&
                                        customerOrderDriver.SupportedCommands.Contains (DeviceCommands.Ping)) {
                                        if (!ReferenceEquals (cashReceiptPrinterDriver, customerOrderDriver))
                                            customerOrderDriver.Ping ();
                                    }
                                }

                                lock (displayDriverLock) {
                                    if (ExternalDisplayConnected &&
                                        displayDriver.SupportedCommands.Contains (DeviceCommands.Ping)) {
                                        if (!ReferenceEquals (cashReceiptPrinterDriver, displayDriver))
                                            displayDriver.Ping ();
                                    }
                                }
                            }
                            return false;
                        } catch (HardwareErrorException ex) {
                            bool retry = OnHardwareError (ex);
                            if (retry)
                                continue;

                            lastException = ex;
                            break;
                        }
                    } while (true);
                }
            } finally {
                Monitor.Exit (finalizeOperationLock);
            }

            return true;
        }

        private void DisplayAdvertismentMessage ()
        {
            if (welcomeDisplayed)
                return;

            if ((DateTime.Now - lastPayment).TotalMilliseconds <= DISPLAY_WELCOME_WAIT)
                return;

            if (!ExternalDisplayConnected)
                return;

            if (!displayDriver.SupportedCommands.Contains (DeviceCommands.DisplayUpperText) ||
                !displayDriver.SupportedCommands.Contains (DeviceCommands.DisplayClear))
                return;

            lock (displayDriverLock) {
                displayDriver.DisplayClear ();
                displayDriver.DisplayUpperText (Translator.GetString ("Welcome").AlignCenter (displayDriver.DisplayCharsPerLine));
                welcomeDisplayed = true;
            }
        }

        private bool OnHardwareError (HardwareErrorException ex)
        {
            bool retry = false;
            if (HardwareError != null) {
                ErrorStateEventArgs args = new ErrorStateEventArgs (ex);
                HardwareError (this, args);
                retry = args.Retry;
            }
            return retry;
        }

        private void OnCashReceiptPrinterChanged ()
        {
            if (CashReceiptPrinterChanged != null)
                CashReceiptPrinterChanged (this, EventArgs.Empty);
        }

        private void OnElectronicScaleChanged ()
        {
            EventHandler handler = ElectronicScaleChanged;
            if (handler != null)
                handler (this, EventArgs.Empty);
        }

        public void Dispose ()
        {
            DeviceWorkerExit ();
        }
    }
}
