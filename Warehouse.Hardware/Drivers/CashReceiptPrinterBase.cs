//
// CashReceiptPrinterBase.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   12.27.2006
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
using System.Text;
using Warehouse.Business.Devices;
using Warehouse.Business.Documenting;
using Warehouse.Business.Entities;
using Warehouse.Data;

namespace Warehouse.Hardware.Drivers
{
    public abstract class CashReceiptPrinterBase<TCon> : DriverBase<TCon>, ICashReceiptPrinterController where TCon : DeviceConnector, new ()
    {
        #region Private fields

        protected int nonFiscalTextCharsPerLine = 23;
        protected int fiscalTextCharsPerLine = 23;
        protected int graphicalLogoWidth = 576;
        protected int graphicalLogoHeight = 144;
        protected FiscalPrinterTaxGroup defaultTaxGroup = FiscalPrinterTaxGroup.B;
        protected bool printTotalOnly;

        #endregion

        #region Public properties

        public virtual int NonFiscalTextCharsPerLine
        {
            get { return nonFiscalTextCharsPerLine; }
        }

        public virtual int FiscalTextCharsPerLine
        {
            get { return fiscalTextCharsPerLine; }
        }

        public FiscalPrinterTaxGroup DefaultTaxGroup
        {
            get { return defaultTaxGroup; }
        }

        public bool PrintTotalOnly
        {
            get { return printTotalOnly; }
        }

        #endregion

        public override void Connect (ConnectParametersCollection parameters)
        {
            printTotalOnly = (bool) parameters [ConnectParameters.PrintTotalOnly];

            base.Connect (parameters);
        }

        #region Initialization commands

        public virtual void SetDateTime (DateTime date)
        {
            throw new NotSupportedException ();
        }

        public virtual void SyncDate ()
        {
            throw new NotSupportedException ();
        }

        public virtual void SyncTime ()
        {
            throw new NotSupportedException ();
        }

        public virtual void SetVATRates (double? vatA, double? vatB, double? vatC, double? vatD, double? vatE, double? vatF)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetFooterText (int line, string text)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetPrintingOptions (bool headerNewLine, bool vatNewLine, bool footerNewLine, bool totalSeparator)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetGraphicalLogo (bool enable)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetAutoPaperCut (bool enable)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetZeroRegisterPrint (bool enable)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetJournalPrintSpeed (int eco, int normal)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetReceiptEndFeed (int mm)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetBufferedPrint (bool enable)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetPrintContrast (int contrast)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetBarcodeHeight (int height)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetHeaderText (int line, out string text)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetFooterText (int line, out string text)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetPrintingOptions (out bool headerNewLine, out bool vatNewLine, out bool footerNewLine, out bool totalSeparator)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetGraphicalLogo (out bool enable)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetAutoPaperCut (out bool enable)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetZeroRegisterPrint (out bool enable)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetJournalPrintSpeed (out int eco, out int normal)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetReceiptEndFeed (out int mm)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetBufferedPrint (out bool enable)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetPrintContrast (out int contrast)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetBarcodeHeight (out int height)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetGraphicalLogoBitmap (string bitmapFile)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetTextField (int field, string value)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetTextField (int field, out string value)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetPaymentName (BasePaymentType method, string value)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetPaymentName (BasePaymentType method, out string value)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetReceiptCode (string code)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetOperator (string @operator)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetPartner (Partner partner)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetLocation (string location)
        {
            throw new NotSupportedException ();
        }

        #endregion

        #region Non fiscal commands

        public virtual void OpenNonFiscal (bool isKitchenReceipt)
        {
            throw new NotSupportedException ();
        }

        public virtual void PrintTitleNonFiscal (string text)
        {
            throw new NotSupportedException ();
        }

        public virtual void PrintTextNonFiscal (string text)
        {
            throw new NotSupportedException ();
        }

        public virtual void PrintKeyValueNonFiscal (string key, string value, string separator)
        {
            if (key == null)
                throw new ArgumentNullException ("key");
            if (value == null)
                throw new ArgumentNullException ("value");
            if (separator == null)
                throw new ArgumentNullException ("separator");
            if (string.IsNullOrEmpty (separator))
                throw new ArgumentException ("separator");

            if (key == SEPARATOR) {
                PrintTextNonFiscal (SEPARATOR);
                return;
            }

            if (key.Length + value.Length >= nonFiscalTextCharsPerLine) {
                PrintTextNonFiscal ((key + value).Trim (nonFiscalTextCharsPerLine));
                return;
            }

            int fillSize = nonFiscalTextCharsPerLine - value.Length;
            StringBuilder fill = new StringBuilder (key);
            while (fill.Length < fillSize) {
                fill.Append (separator);
            }

            PrintTextNonFiscal (fill.ToString ().Trim (fillSize) + value);
        }

        public virtual void AddItemNonFiscal (string itemCode, string itemName, double quantity, string measUnit, double price, double vatPercent, double extraPercent, long itemGroupId, FiscalPrinterTaxGroup taxGroup, string [] modifiers)
        {
            throw new NotSupportedException ();
        }

        public virtual void AddModifiedItemNonFiscal (string itemCode, string itemName, double oldQuantity, double quantity, string measUnit, double price, double vatPercent, double extraPercent, FiscalPrinterTaxGroup taxGroup, string[] oldModifiers, string[] modifiers)
        {
            throw new NotSupportedException ();
        }

        public virtual void CloseNonFiscal ()
        {
            throw new NotSupportedException ();
        }

        #endregion

        #region Fiscal commands

        public virtual void InitFiscal (int commentsCnt, int itemsCnt, int discountedItemsCnt, int paymentsCnt)
        {
            throw new NotSupportedException ();
        }

        public virtual void OpenFiscal ()
        {
            throw new NotSupportedException ();
        }

        public virtual void AddItem (string itemCode, string itemName, double quantity, string measUnit, double price, double vatPercent, double extraPercent, FiscalPrinterTaxGroup taxGroup)
        {
            throw new NotSupportedException ();
        }

        public virtual void Subtotal (bool print, bool display)
        {
            throw new NotSupportedException ();
        }

        public virtual void Payment (double amountPaid, BasePaymentType paymentMethod)
        {
            throw new NotSupportedException ();
        }

        public virtual void CloseFiscal ()
        {
            throw new NotSupportedException ();
        }

        public virtual void CancelFiscalOperation (out bool annulled)
        {
            throw new NotSupportedException ();
        }

        public virtual void PrintTextFiscal (string text, TextAlign align)
        {
            throw new NotSupportedException ();
        }

        public virtual void PrintDuplicate ()
        {
            throw new NotSupportedException ();
        }

        #endregion

        #region Printer commands

        public virtual void PaperFeed (int lines)
        {
            throw new NotSupportedException ();
        }

        public virtual void PaperCut ()
        {
            throw new NotSupportedException ();
        }

        public virtual void PrintBarcode (GeneratedBarcodeType type, string data, bool printText)
        {
            throw new NotSupportedException ();
        }

        public virtual void PrintSignature (SaleSignature signature)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetPrintFonts (bool bigFont, bool economicalMode)
        {
            throw new NotSupportedException ();
        }

        #endregion

        #region Information commands

        public virtual void GetFiscalReceiptStatus (bool includePaid, out bool fiscalOpened, out int lineCount, out double amount, out double paid)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetDiagnosticInfo (bool calculateCRC, out string firmwareVer, out DateTime firmwareDate, out string firmwareCRC, out int country, out string fpSerial)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetVATRatesInfo (out Dictionary<FiscalPrinterTaxGroup, double> vatRates)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetTaxNumber (out string taxNumber)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetLastDocumentNumber (out string docNumber)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetConstants (out Dictionary<string, string> consts)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetDateTime (out DateTime printerDateTime)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetMeasuringUnits (out Dictionary<int, string> units)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetSerialNumbers (out string fpSerial, out string fiscalMemSerial)
        {
            throw new NotSupportedException ();
        }

        #endregion

        #region Report commands

        public virtual void DailyXReport (bool resetOperations, out string closure, out Dictionary<FiscalPrinterTaxGroup, double> amounts)
        {
            throw new NotSupportedException ();
        }

        public virtual void DailyZReport (bool resetOperations, out string closure, out Dictionary<FiscalPrinterTaxGroup, double> amounts)
        {
            throw new NotSupportedException ();
        }

        public virtual void DailyEJReport (bool resetOperations, out string closure, out Dictionary<FiscalPrinterTaxGroup, double> amounts)
        {
            throw new NotSupportedException ();
        }

        public virtual void DetailFMReportByNumbers (int start, int end)
        {
            throw new NotSupportedException ();
        }

        public virtual void ShortFMReportByNumbers (int start, int end)
        {
            throw new NotSupportedException ();
        }

        public virtual void DetailFMReportByDates (DateTime start, DateTime end)
        {
            throw new NotSupportedException ();
        }

        public virtual void ShortFMReportByDates (DateTime start, DateTime end)
        {
            throw new NotSupportedException ();
        }

        public virtual void OperatorsReport ()
        {
            throw new NotSupportedException ();
        }

        public virtual void RAMResetsReport ()
        {
            throw new NotSupportedException ();
        }

        public virtual void VATRateChangesReport ()
        {
            throw new NotSupportedException ();
        }

        #endregion

        #region Other commands

        public virtual void RegisterCash (double amount, string cashDescription, string reason)
        {
            throw new NotSupportedException ();
        }

        public virtual void OpenCashDrawer ()
        {
            throw new NotSupportedException ();
        }

        public virtual void PrintFormObject (IDocument formObject)
        {
            throw new NotSupportedException ();
        }

        #endregion
    }
}
