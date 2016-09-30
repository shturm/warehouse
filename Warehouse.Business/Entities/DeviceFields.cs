//
// DeviceFields.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   09/09/2009
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

namespace Warehouse.Business.Entities
{
    public class DeviceFields : XmlEntityFields
    {
        public const string Name = "DeviceName";
        public const string DriverName = "DeviceDriverName";
        public const string DriverClass = "DeviceDriverClass";
        public const string DeviceMake = "DeviceMake";
        public const string DeviceModel = "DeviceModel";
        public const string SerialPort = "DeviceSerialPort";
        public const string MacAddress = "DeviceMacAddress";
        public const string SerialPortBaudRate = "DeviceSerialPortBaudRate";
        public const string SerialPortHandshaking = "SerialPortHandshaking";
        public const string NetworkAddress = "DeviceNetworkAddress";
        public const string NetworkPort = "DeviceNetworkPort";
        public const string UseDefaultDocumentPrinter = "DeviceUseDefaultDocumentPrinter";
        public const string DocumentPrinterName = "DeviceDocumentPrinterName";
        public const string UseCustomLineWidth = "UseCustomLineWidth";
        public const string LineWidth = "DeviceLineWidth";
        public const string ImagesFolder = "DeviceImagesFolder";
        public const string LogicalAddress = "DeviceLogicalAddress";
        public const string Password = "DevicePassword";
        public const string AdminPassword = "DeviceAdminPassword";
        public const string Encoding = "DeviceEncoding";
        public const string Enabled = "DeviceEnabled";
        public const string PrintFiscalReceipts = "DevicePrintFiscalReceipts";
        public const string PrintNonFiscalReceipts = "DevicePrintNonFiscalReceipts";
        public const string PrintKitchenReceipts = "DevicePrintKitchenReceipts";
        public const string DisplaySaleInfo = "DeviceDisplaySaleInfo";
        public const string ReadMagneticCards = "DeviceReadMagneticCards";
        public const string MeasureWeight = "DeviceMeasureWeight";
        public const string CollectSalesData = "DeviceCollectSalesData";
        public const string ScanBarcodes = "DeviceScanBarcodes";
        public const string AllItemGroups = "DeviceAllItemGroups";
        public const string KitchenItemGroups = "DeviceKitchenItemGroups";
        public const string CharactersInLine = "CharactersInLine";
        public const string BlankLinesBeforeCut = "BlankLinesBeforeCut";
        public const string DrawerCommand = "DrawerCommand";
        public const string OpenDrawer = "OpenDrawer";
        public const string HeaderText = "HeaderText";
        public const string FooterText = "FooterText";
        public const string OperatorCode = "OperatorCode";
    }
}
