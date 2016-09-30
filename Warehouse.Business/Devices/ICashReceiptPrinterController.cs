//
// ICashReceiptPrinterController.cs
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
using Warehouse.Business.Documenting;
using Warehouse.Business.Entities;
using Warehouse.Data;

namespace Warehouse.Business.Devices
{
    public interface ICashReceiptPrinterController : IConnectableDevice
    {
        int NonFiscalTextCharsPerLine { get; }
        int FiscalTextCharsPerLine { get; }
        FiscalPrinterTaxGroup DefaultTaxGroup { get; }
        bool PrintTotalOnly { get; }

        #region Initialization

        [DeviceCommand (DeviceCommands.SetDateTime)]
        void SetDateTime (DateTime date);

        [DeviceCommand (DeviceCommands.SyncDate)]
        void SyncDate ();

        [DeviceCommand (DeviceCommands.SyncTime)]
        void SyncTime ();

        [DeviceCommand (DeviceCommands.SetVATRates)]
        void SetVATRates (double? vatA, double? vatB, double? vatC, double? vatD, double? vatE, double? vatF);

        [DeviceCommand (DeviceCommands.SetFooterText)]
        void SetFooterText (int line, string text);

        [DeviceCommand (DeviceCommands.SetPrintingOptions)]
        void SetPrintingOptions (bool headerNewLine, bool vatNewLine, bool footerNewLine, bool totalSeparator);

        [DeviceCommand (DeviceCommands.SetGraphicalLogo)]
        void SetGraphicalLogo (bool enable);

        [DeviceCommand (DeviceCommands.SetAutoPaperCut)]
        void SetAutoPaperCut (bool enable);

        [DeviceCommand (DeviceCommands.SetZeroRegisterPrint)]
        void SetZeroRegisterPrint (bool enable);

        [DeviceCommand (DeviceCommands.SetJournalPrintSpeed)]
        void SetJournalPrintSpeed (int eco, int normal);

        [DeviceCommand (DeviceCommands.SetReceiptEndFeed)]
        void SetReceiptEndFeed (int mm);

        [DeviceCommand (DeviceCommands.SetBufferedPrint)]
        void SetBufferedPrint (bool enable);

        [DeviceCommand (DeviceCommands.SetPrintContrast)]
        void SetPrintContrast (int contrast);

        [DeviceCommand (DeviceCommands.SetBarcodeHeight)]
        void SetBarcodeHeight (int height);

        [DeviceCommand (DeviceCommands.GetHeaderText)]
        void GetHeaderText (int line, out string text);

        [DeviceCommand (DeviceCommands.GetFooterText)]
        void GetFooterText (int line, out string text);

        [DeviceCommand (DeviceCommands.GetPrintingOptions)]
        void GetPrintingOptions (out bool headerNewLine, out bool vatNewLine, out bool footerNewLine, out bool totalSeparator);

        [DeviceCommand (DeviceCommands.GetGraphicalLogo)]
        void GetGraphicalLogo (out bool enable);

        [DeviceCommand (DeviceCommands.GetAutoPaperCut)]
        void GetAutoPaperCut (out bool enable);

        [DeviceCommand (DeviceCommands.GetZeroRegisterPrint)]
        void GetZeroRegisterPrint (out bool enable);

        [DeviceCommand (DeviceCommands.GetJournalPrintSpeed)]
        void GetJournalPrintSpeed (out int eco, out int normal);

        [DeviceCommand (DeviceCommands.GetReceiptEndFeed)]
        void GetReceiptEndFeed (out int mm);

        [DeviceCommand (DeviceCommands.GetBufferedPrint)]
        void GetBufferedPrint (out bool enable);

        [DeviceCommand (DeviceCommands.GetPrintContrast)]
        void GetPrintContrast (out int contrast);

        [DeviceCommand (DeviceCommands.GetBarcodeHeight)]
        void GetBarcodeHeight (out int height);

        [DeviceCommand (DeviceCommands.SetGraphicalLogoBitmap)]
        void SetGraphicalLogoBitmap (string bitmapFile);

        [DeviceCommand (DeviceCommands.SetTextField)]
        void SetTextField (int field, string value);

        [DeviceCommand (DeviceCommands.GetTextField)]
        void GetTextField (int field, out string value);

        [DeviceCommand (DeviceCommands.SetPaymentName)]
        void SetPaymentName (BasePaymentType method, string value);

        [DeviceCommand (DeviceCommands.GetPaymentName)]
        void GetPaymentName (BasePaymentType method, out string value);

        [DeviceCommand (DeviceCommands.SetReceiptCode)]
        void SetReceiptCode (string code);

        [DeviceCommand (DeviceCommands.SetOperator)]
        void SetOperator (string @operator);

        [DeviceCommand (DeviceCommands.SetPartner)]
        void SetPartner (Partner partner);

        [DeviceCommand (DeviceCommands.SetLocation)]
        void SetLocation (string location);

        #endregion

        #region Non fiscal commands

        [DeviceCommand (DeviceCommands.OpenNonFiscal)]
        void OpenNonFiscal (bool isKitchenReceipt);

        [DeviceCommand (DeviceCommands.PrintTitleNonFiscal)]
        void PrintTitleNonFiscal (string text);

        [DeviceCommand (DeviceCommands.PrintTextNonFiscal)]
        void PrintTextNonFiscal (string text);

        [DeviceCommand (DeviceCommands.PrintKeyValueNonFiscal)]
        void PrintKeyValueNonFiscal (string key, string value, string separator);

        [DeviceCommand (DeviceCommands.AddItemNonFiscal)]
        void AddItemNonFiscal (string itemCode, string itemName, double quantity, string measUnit, double price, double vatPercent, double extraPercent, long itemGroupId, FiscalPrinterTaxGroup taxGroup, string [] modifiers);

        [DeviceCommand (DeviceCommands.AddModifiedItemNonFiscal)]
        void AddModifiedItemNonFiscal (string itemCode, string itemName, double oldQuantity, double quantity, string measUnit, double price, double vatPercent, double extraPercent, FiscalPrinterTaxGroup taxGroup, string[] oldModifiers, string[] modifiers);

        [DeviceCommand (DeviceCommands.CloseNonFiscal)]
        void CloseNonFiscal ();

        #endregion

        #region Fiscal commands

        [DeviceCommand (DeviceCommands.InitFiscal)]
        void InitFiscal (int commentsCnt, int itemsCnt, int discountedItemsCnt, int paymentsCnt);

        [DeviceCommand (DeviceCommands.OpenFiscal)]
        void OpenFiscal ();

        [DeviceCommand (DeviceCommands.AddItem)]
        void AddItem (string itemCode, string itemName, double quantity, string measUnit, double price, double vatPercent, double extraPercent, FiscalPrinterTaxGroup taxGroup);

        [DeviceCommand (DeviceCommands.Subtotal)]
        void Subtotal (bool print, bool display);

        [DeviceCommand (DeviceCommands.Payment)]
        void Payment (double amountPaid, BasePaymentType paymentMethod);

        [DeviceCommand (DeviceCommands.CloseFiscal)]
        void CloseFiscal ();

        [DeviceCommand (DeviceCommands.CancelFiscalOperation)]
        void CancelFiscalOperation (out bool annulled);

        [DeviceCommand (DeviceCommands.PrintTextFiscal)]
        void PrintTextFiscal (string text, DriverBase.TextAlign align);

        [DeviceCommand (DeviceCommands.PrintDuplicate)]
        void PrintDuplicate ();

        #endregion

        #region Printer commands

        [DeviceCommand (DeviceCommands.PaperFeed)]
        void PaperFeed (int lines);

        [DeviceCommand (DeviceCommands.PaperCut)]
        void PaperCut ();

        [DeviceCommand (DeviceCommands.PrintBarcode)]
        void PrintBarcode (GeneratedBarcodeType type, string data, bool printText);

        [DeviceCommand (DeviceCommands.PrintSignature)]
        void PrintSignature (SaleSignature signature);

        [DeviceCommand (DeviceCommands.SetPrinterFonts)]
        void SetPrintFonts (bool bigFont, bool economicalMode);

        #endregion

        #region Information commands

        [DeviceCommand (DeviceCommands.GetFiscalReceiptStatus)]
        void GetFiscalReceiptStatus (bool includePaid, out bool fiscalOpened, out int lineCount, out double amount, out double paid);

        [DeviceCommand (DeviceCommands.GetDiagnosticInfo)]
        void GetDiagnosticInfo (bool calculateCRC, out string firmwareVer, out DateTime firmwareDate, out string firmwareCRC, out int country, out string fpSerial);

        [DeviceCommand (DeviceCommands.GetVATRatesInfo)]
        void GetVATRatesInfo (out Dictionary<FiscalPrinterTaxGroup, double> vatRates);

        [DeviceCommand (DeviceCommands.GetTaxNumber)]
        void GetTaxNumber (out string taxNumber);

        [DeviceCommand (DeviceCommands.GetLastDocumentNumber)]
        void GetLastDocumentNumber (out string docNumber);

        [DeviceCommand (DeviceCommands.GetConstants)]
        void GetConstants (out Dictionary<string, string> consts);

        [DeviceCommand (DeviceCommands.GetDateTime)]
        void GetDateTime (out DateTime printerDateTime);

        [DeviceCommand (DeviceCommands.GetMeasuringUnits)]
        void GetMeasuringUnits (out Dictionary<int, string> units);

        [DeviceCommand (DeviceCommands.GetSerialNumbers)]
        void GetSerialNumbers (out string fpSerial, out string fiscalMemSerial);

        #endregion

        #region Report commands

        [DeviceCommand (DeviceCommands.DailyXReport)]
        void DailyXReport (bool resetOperations, out string closure, out Dictionary<FiscalPrinterTaxGroup, double> amounts);

        [DeviceCommand (DeviceCommands.DailyZReport)]
        void DailyZReport (bool resetOperations, out string closure, out Dictionary<FiscalPrinterTaxGroup, double> amounts);

        [DeviceCommand (DeviceCommands.DailyEJReport)]
        void DailyEJReport (bool resetOperations, out string closure, out Dictionary<FiscalPrinterTaxGroup, double> amounts);

        [DeviceCommand (DeviceCommands.DetailFMReportByNumbers)]
        void DetailFMReportByNumbers (int start, int end);

        [DeviceCommand (DeviceCommands.ShortFMReportByNumbers)]
        void ShortFMReportByNumbers (int start, int end);

        [DeviceCommand (DeviceCommands.DetailFMReportByDates)]
        void DetailFMReportByDates (DateTime start, DateTime end);

        [DeviceCommand (DeviceCommands.ShortFMReportByDates)]
        void ShortFMReportByDates (DateTime start, DateTime end);

        [DeviceCommand (DeviceCommands.OperatorsReport)]
        void OperatorsReport ();

        [DeviceCommand (DeviceCommands.RAMResetsReport)]
        void RAMResetsReport ();

        [DeviceCommand (DeviceCommands.VATRateChangesReport)]
        void VATRateChangesReport ();

        #endregion

        #region Other commands

        [DeviceCommand (DeviceCommands.RegisterCash)]
        void RegisterCash (double amount, string cashDescription, string reason);

        [DeviceCommand (DeviceCommands.OpenCashDrawer)]
        void OpenCashDrawer ();

        [DeviceCommand (DeviceCommands.PrintFormObject)]
        void PrintFormObject (IDocument formObject);

        #endregion
    }
}
