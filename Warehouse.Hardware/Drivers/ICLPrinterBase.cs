//
// ICLPrinterBase.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   12/10/2006
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
using System.IO;
using System.Text;
using System.Threading;
using Warehouse.Business.Devices;
using Warehouse.Data;

#if DEBUG_PORTS
using System.Diagnostics;
#endif

namespace Warehouse.Hardware.Drivers
{
    public abstract class ICLPrinterBase<TCon> : CashReceiptPrinterBase<TCon> where TCon : DeviceConnector, new ()
    {
        private const byte MARKER_START = 0x01;
        private const byte MARKER_ARGS_SEPARATOR = 0x04;
        private const byte MARKER_ARGS_END = 0x05;
        private const byte MARKER_END = 0x03;
        private const byte MARKER_ACK = 0x06;
        private const byte MARKER_NACK = 0x15;
        private const byte MARKER_SYNC = 0x16;

        protected enum CommandCodes
        {
            UserDefined = 0,

            DisplayClear = 33,
            DisplaySetLowerText = 35,

            //Non Fiscal
            OpenNonFiscal = 38,
            CloseNonFiscal = 39,
            PrintTextNonFiscal = 42,

            //  
            PrintOptions = 43,
            PaperFeed = 44,
            PaperCut = 45,
            DisplaySetUpperText = 47,

            //Fiscal
            OpenFiscal = 48,
            AddItem = 49,
            AddDisplayItem = 52,
            SubTotal = 51,
            Payment = 53,
            PrintTextFiscal = 54,
            CloseFiscal = 56,
            PrintClientInfo = 57,
            Annul = 60,

            SetDateTime = 61,
            GetDateTime = 62,
            DisplayShowDateTime = 63,
            DailyReport = 69,
            RegisterCash = 70,
            DetailFMReportByNumbers = 73,
            GetStatus = 74,
            GetFiscalStatus = 76,
            ShortFMReportByDates = 79,
            GetDiagnosticData = 90,
            DetailFMReportByDates = 94,
            ShortFMReportByNumbers = 95,
            SetVATRates = 96,
            GetVATRates = 97,
            GetTaxNumber = 99,
            DisplaySetText = 100,
            GetCheckStatus = 103,
            OperatorsReport = 105,
            OpenCashDrawer = 106,

            PrintDuplicate = 109,
            GetLastDocumentNumber = 113,
            SetGraphicalLogoBitmap = 115,
            CloseOperation = 130,
            SetPrintFonts = 145
        }

        protected enum DailyReportType
        {
            Z,
            X,
            EJ
        }

        #region Private fields

        private List<DeviceCommands> supportedCommands;
        protected double balance = 0;
        protected uint frameSeqNumber;
        protected string operatorPassword;
        protected int itemNameCharsPerLine = 23;
        protected int totalNoteCharsPerLine = 25;
        protected int displayCharsPerLine = 16;
        protected string priceDecimalSeparator = ".";
        protected string quantityDecimalSeparator = ".";
        protected string discountDecimalSeparator = ".";
        protected string anullString = "АНУЛИРАНЕ";

        protected CommandCodes receivedCommand = CommandCodes.UserDefined;
        protected byte [] receivedMessage = null;
        protected byte [] receivedArguments = null;
        private bool openCashDrawerEnabled;

        #endregion

        public int DisplayCharsPerLine
        {
            get { return displayCharsPerLine; }
        }

        public override List<DeviceCommands> SupportedCommands
        {
            get
            {
                if (supportedCommands == null) {
                    supportedCommands = new List<DeviceCommands> ();

                    #region Initialization commands

                    supportedCommands.Add (DeviceCommands.SetDateTime);
                    supportedCommands.Add (DeviceCommands.SetFooterText);
                    supportedCommands.Add (DeviceCommands.SetPrintingOptions);
                    supportedCommands.Add (DeviceCommands.SetGraphicalLogo);
                    supportedCommands.Add (DeviceCommands.SetAutoPaperCut);
                    supportedCommands.Add (DeviceCommands.SetZeroRegisterPrint);
                    supportedCommands.Add (DeviceCommands.SetJournalPrintSpeed);
                    supportedCommands.Add (DeviceCommands.SetReceiptEndFeed);
                    supportedCommands.Add (DeviceCommands.SetBufferedPrint);
                    supportedCommands.Add (DeviceCommands.SetPrintContrast);
                    supportedCommands.Add (DeviceCommands.GetHeaderText);
                    supportedCommands.Add (DeviceCommands.GetFooterText);
                    supportedCommands.Add (DeviceCommands.GetPrintingOptions);
                    supportedCommands.Add (DeviceCommands.GetGraphicalLogo);
                    supportedCommands.Add (DeviceCommands.GetAutoPaperCut);
                    supportedCommands.Add (DeviceCommands.GetZeroRegisterPrint);
                    supportedCommands.Add (DeviceCommands.GetJournalPrintSpeed);
                    supportedCommands.Add (DeviceCommands.GetReceiptEndFeed);
                    supportedCommands.Add (DeviceCommands.GetBufferedPrint);
                    supportedCommands.Add (DeviceCommands.GetPrintContrast);

                    #endregion

                    #region Display commands

                    #endregion

                    #region Non fiscal commands

                    supportedCommands.Add (DeviceCommands.OpenNonFiscal);
                    supportedCommands.Add (DeviceCommands.PrintKeyValueNonFiscal);
                    supportedCommands.Add (DeviceCommands.PrintTextNonFiscal);
                    supportedCommands.Add (DeviceCommands.CloseNonFiscal);

                    #endregion

                    #region Fiscal commands

                    supportedCommands.Add (DeviceCommands.OpenFiscal);
                    supportedCommands.Add (DeviceCommands.AddItem);
                    supportedCommands.Add (DeviceCommands.Subtotal);
                    supportedCommands.Add (DeviceCommands.Payment);
                    supportedCommands.Add (DeviceCommands.CloseFiscal);
                    supportedCommands.Add (DeviceCommands.CancelFiscalOperation);
                    supportedCommands.Add (DeviceCommands.PrintTextFiscal);

                    #endregion

                    #region Printer commands

                    supportedCommands.Add (DeviceCommands.PaperFeed);
                    supportedCommands.Add (DeviceCommands.PaperCut);

                    #endregion

                    #region Information commands

                    supportedCommands.Add (DeviceCommands.GetFiscalReceiptStatus);
                    supportedCommands.Add (DeviceCommands.GetDiagnosticInfo);
                    supportedCommands.Add (DeviceCommands.GetTaxNumber);
                    supportedCommands.Add (DeviceCommands.GetLastDocumentNumber);
                    supportedCommands.Add (DeviceCommands.GetDateTime);
                    supportedCommands.Add (DeviceCommands.GetSerialNumbers);

                    #endregion

                    #region Report commands

                    supportedCommands.Add (DeviceCommands.DailyXReport);
                    supportedCommands.Add (DeviceCommands.DailyZReport);
                    supportedCommands.Add (DeviceCommands.DetailFMReportByNumbers);
                    supportedCommands.Add (DeviceCommands.ShortFMReportByNumbers);
                    supportedCommands.Add (DeviceCommands.DetailFMReportByDates);
                    supportedCommands.Add (DeviceCommands.ShortFMReportByDates);
                    supportedCommands.Add (DeviceCommands.OperatorsReport);

                    #endregion

                    #region Other commands

                    supportedCommands.Add (DeviceCommands.GetStatus);
                    supportedCommands.Add (DeviceCommands.Ping);
                    supportedCommands.Add (DeviceCommands.OpenCashDrawer);

                    #endregion
                }

                return supportedCommands;
            }
        }

        protected void SetPassword (ConnectParametersCollection parameters)
        {
            operatorPassword = (string) parameters [ConnectParameters.Password];
        }

        #region General commands

        public override void Initialize ()
        {
            GetStatus ();

            if (lastErrorState.Check (ErrorState.FiscalCheckOpen)) {
                bool annulled;
                CancelFiscalOperation (out annulled);
            }

            if (lastErrorState.Check (ErrorState.NonFiscalCheckOpen))
                CloseNonFiscal ();
        }

        public override void Connect (ConnectParametersCollection parameters)
        {
            try {
                try {
                    if (!Connector.IsConnected) {
                        frameSeqNumber = 0x20;

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

                    openCashDrawerEnabled = (bool) parameters [ConnectParameters.OpenDrawer];
                } catch (IOException ioex) {
                    throw new HardwareErrorException (new ErrorState (ErrorState.BadSerialPort, HardwareErrorSeverity.Error), ioex);
                } catch (UnauthorizedAccessException uaex) {
                    throw new HardwareErrorException (new ErrorState (ErrorState.BadSerialPort, HardwareErrorSeverity.Error), uaex);
                }
            } catch (Exception) {
                Disconnect ();
                throw;
            }

            base.Connect (parameters);
        }

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

        #endregion

        #region Initialization commands

        public override void SetDateTime (DateTime date)
        {
            SendMessage (CommandCodes.SetDateTime, defaultEnc.GetBytes (date.ToString ("dd-MM-yy HH:mm:ss")));
        }

        public override void SetFooterText (int line, string text)
        {
            line = Math.Min (line, 1);
            line = Math.Max (line, 0);
            line += 5;

            StringBuilder sb = new StringBuilder (line.ToString (CultureInfo.InvariantCulture));
            sb.Append (text);

            SendMessage (CommandCodes.PrintOptions, GetTextBytes (sb.ToString ()));
        }

        public override void SetPrintingOptions (bool headerNewLine, bool vatNewLine, bool footerNewLine, bool totalSeparator)
        {
            StringBuilder sb = new StringBuilder ("P");
            sb.Append (headerNewLine ? '1' : '0');
            sb.Append (",");
            sb.Append (vatNewLine ? '1' : '0');
            sb.Append (",");
            sb.Append (footerNewLine ? '1' : '0');
            sb.Append (",");
            sb.Append (totalSeparator ? '1' : '0');

            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes (sb.ToString ()));
        }

        public override void SetGraphicalLogo (bool enable)
        {
            StringBuilder sb = new StringBuilder ("L");
            sb.Append (enable ? '1' : '0');

            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes (sb.ToString ()));
        }

        public override void SetAutoPaperCut (bool enable)
        {
            StringBuilder sb = new StringBuilder ("C");
            sb.Append (enable ? '1' : '0');

            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes (sb.ToString ()));
        }

        public override void SetZeroRegisterPrint (bool enable)
        {
            StringBuilder sb = new StringBuilder ("R");
            sb.Append (enable ? '1' : '0');

            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes (sb.ToString ()));
        }

        public override void SetJournalPrintSpeed (int eco, int normal)
        {
            eco = Math.Min (eco, 240);
            eco = Math.Max (eco, 5);
            normal = Math.Min (normal, 240);
            normal = Math.Max (normal, 5);

            StringBuilder sb = new StringBuilder ("S");
            sb.Append (eco);
            sb.Append (",");
            sb.Append (normal);

            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes (sb.ToString ()));
        }

        public override void SetReceiptEndFeed (int mm)
        {
            mm = Math.Max (mm, 0);

            StringBuilder sb = new StringBuilder ("E");
            sb.Append (mm);

            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes (sb.ToString ()));
        }

        public override void SetBufferedPrint (bool enable)
        {
            StringBuilder sb = new StringBuilder ("W");
            sb.Append (enable ? '1' : '0');

            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes (sb.ToString ()));
        }

        public override void SetPrintContrast (int contrast)
        {
            contrast = Math.Min (contrast, 8);
            contrast = Math.Max (contrast, 0);

            StringBuilder sb = new StringBuilder ("T");
            sb.Append (contrast);

            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes (sb.ToString ()));
        }

        public override void GetHeaderText (int line, out string text)
        {
            line = Math.Min (line, 4);
            line = Math.Max (line, 0);

            StringBuilder sb = new StringBuilder ("I");
            sb.Append (line.ToString (CultureInfo.InvariantCulture));

            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes (sb.ToString ()));

            text = defaultEnc.GetString (receivedArguments);
        }

        public override void GetFooterText (int line, out string text)
        {
            line = Math.Min (line, 1);
            line = Math.Max (line, 0);
            line += 5;

            StringBuilder sb = new StringBuilder ("I");
            sb.Append (line.ToString (CultureInfo.InvariantCulture));

            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes (sb.ToString ()));

            text = defaultEnc.GetString (receivedArguments);
        }

        public override void GetPrintingOptions (out bool headerNewLine, out bool vatNewLine, out bool footerNewLine, out bool totalSeparator)
        {
            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes ("IP"));

            string values = defaultEnc.GetString (receivedArguments);
            headerNewLine = values [0] == '1';
            vatNewLine = values [1] == '1';
            footerNewLine = values [2] == '1';
            totalSeparator = values [3] == '1';
        }

        public override void GetGraphicalLogo (out bool enable)
        {
            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes ("IL"));

            string value = defaultEnc.GetString (receivedArguments);
            enable = value == "1";
        }

        public override void GetAutoPaperCut (out bool enable)
        {
            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes ("IC"));

            string value = defaultEnc.GetString (receivedArguments);
            enable = value == "1";
        }

        public override void GetZeroRegisterPrint (out bool enable)
        {
            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes ("IR"));

            string value = defaultEnc.GetString (receivedArguments);
            enable = value == "1";
        }

        public override void GetJournalPrintSpeed (out int eco, out int normal)
        {
            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes ("IS"));

            string [] values = defaultEnc.GetString (receivedArguments).Split (',');
            int.TryParse (values [0], out eco);
            int.TryParse (values [1], out normal);
        }

        public override void GetReceiptEndFeed (out int mm)
        {
            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes ("IE"));

            string value = defaultEnc.GetString (receivedArguments);
            int.TryParse (value, out mm);
        }

        public override void GetBufferedPrint (out bool enable)
        {
            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes ("IW"));

            string value = defaultEnc.GetString (receivedArguments);
            enable = value == "1";
        }

        public override void GetPrintContrast (out int contrast)
        {
            SendMessage (CommandCodes.PrintOptions, defaultEnc.GetBytes ("IT"));

            string value = defaultEnc.GetString (receivedArguments);
            int.TryParse (value, out contrast);
        }

        #endregion

        #region Non fiscal commands

        public override void OpenNonFiscal (bool isKitchenReceipt)
        {
            SendMessage (CommandCodes.OpenNonFiscal);
        }

        public override void PrintTextNonFiscal (string text)
        {
            text = GetAlignedString (text, nonFiscalTextCharsPerLine, TextAlign.Left);
            if (string.IsNullOrEmpty (text))
                text = " ";

            SendMessage (CommandCodes.PrintTextNonFiscal, GetTextBytes (text));
        }

        public override void CloseNonFiscal ()
        {
            SendMessage (CommandCodes.CloseNonFiscal);
        }

        #endregion

        #region Fiscal commands

        public override void OpenFiscal ()
        {
            OpenFiscal (1, 1, false);
        }

        private void OpenFiscal (int operatorCode, int tillNumber, bool invoice)
        {
            List<byte> res = new List<byte> ();

            if ((operatorCode < 1) || (operatorCode > 8))
                operatorCode = 1;

            if ((tillNumber < 1) || (tillNumber >= 100000))
                tillNumber = 1;

            res.AddRange (defaultEnc.GetBytes (operatorCode.ToString (CultureInfo.InvariantCulture)));
            res.AddRange (defaultEnc.GetBytes (","));
            res.AddRange (defaultEnc.GetBytes (operatorPassword));
            res.AddRange (defaultEnc.GetBytes (","));
            res.AddRange (defaultEnc.GetBytes (tillNumber.ToString (CultureInfo.InvariantCulture)));

            if (invoice)
                res.AddRange (defaultEnc.GetBytes (",I"));

            SendMessage (CommandCodes.OpenFiscal, res.ToArray ());

            balance = 0d;
        }

        public void PrintClientInfo (string customerTaxNo)
        {
            PrintClientInfo (customerTaxNo, null, null, null, null);
        }

        public void PrintClientInfo (string customerTaxNo, string salesPersonName, string recipientName, string customerName, string customerAddress)
        {
            StringBuilder sb = new StringBuilder ();

            sb.Append (customerTaxNo);
            sb.AppendFormat ("\t{0}", salesPersonName.Trim (30));
            sb.AppendFormat ("\t{0}", recipientName.Trim (25));
            sb.AppendFormat ("\t{0}", customerName.Trim (27));
            sb.Append ("\t"); // Customer bulstat
            sb.AppendFormat ("\t{0}", customerAddress.Trim (24));

            SendMessage (CommandCodes.PrintClientInfo, defaultEnc.GetBytes (sb.ToString ()));
        }

        public override void AddItem (string itemCode, string itemName, double quantity, string measUnit, double price, double vatPercent, double extraPercent, FiscalPrinterTaxGroup taxGroup)
        {
            itemName = itemName.FilterControlCharacters ();
            itemName = itemName.Wrap (itemNameCharsPerLine);
            StringBuilder res = new StringBuilder (itemName);

            res.Append ("\t");
            res.Append (TaxGroupToString (taxGroup));

            NumberFormatInfo numberFormat = new NumberFormatInfo { NumberDecimalSeparator = priceDecimalSeparator, NumberGroupSeparator = string.Empty };
            price = Math.Round (price, 2, MidpointRounding.AwayFromZero);
            price = Math.Min (price, 99999999);
            res.Append (price.ToString ("G8", numberFormat));

            quantity = Math.Round (quantity, 3, MidpointRounding.AwayFromZero);
            quantity = Math.Min (quantity, 99999999);
            quantity = Math.Max (quantity, 0);
            if (!quantity.IsEqualTo (1d)) {
                numberFormat.NumberDecimalSeparator = quantityDecimalSeparator;
                res.AppendFormat ("*{0}", quantity.ToString ("G8", numberFormat));
            }

            extraPercent = Math.Round (extraPercent, 2, MidpointRounding.AwayFromZero);
            extraPercent = Math.Min (extraPercent, 99.99);
            extraPercent = Math.Max (extraPercent, -99.99);
            if (!extraPercent.IsZero ()) {
                numberFormat.NumberDecimalSeparator = priceDecimalSeparator;
                res.AppendFormat (",{0}", extraPercent.ToString ("G2", numberFormat));
            }

            IncrementBalance (quantity, price, extraPercent);

            SendMessage (CommandCodes.AddItem, GetTextBytes (res.ToString ()));
        }

        protected void IncrementBalance (double quantity, double price, double extraPercent)
        {
            double lineTotal = price * quantity;
            balance = Math.Round (balance + lineTotal + lineTotal * (extraPercent / 100), 2, MidpointRounding.AwayFromZero);
        }

        public override void Subtotal (bool print, bool display)
        {
            List<byte> data = new List<byte> ();

            data.AddRange (defaultEnc.GetBytes (print ? "1" : "0"));
            data.AddRange (defaultEnc.GetBytes (display ? "1" : "0"));

            SendMessage (CommandCodes.SubTotal, data.ToArray ());
        }

        protected void Payment ()
        {
            Payment (balance, BasePaymentType.Cash, string.Empty);
        }

        public override void Payment (double amountPaid, BasePaymentType paymentMethod)
        {
            Payment (amountPaid, paymentMethod, string.Empty);
        }

        public virtual void Payment (double amountPaid, BasePaymentType paymentMethod, string info)
        {
            StringBuilder res = new StringBuilder ();

            res.Append (info.Wrap (totalNoteCharsPerLine));
            res.Append ('\t');
            res.Append (PaymentTypeToString (paymentMethod));

            NumberFormatInfo numberFormat = new NumberFormatInfo { NumberDecimalSeparator = priceDecimalSeparator, NumberGroupSeparator = string.Empty };
            res.Append (amountPaid.ToString ("G9", numberFormat));

            SendMessage (CommandCodes.Payment, defaultEnc.GetBytes (res.ToString ()));
            balance -= amountPaid;
        }

        protected virtual string PaymentTypeToString (BasePaymentType paymentMethod)
        {
            switch (paymentMethod) {
                case BasePaymentType.BankTransfer:
                    return "N";
                case BasePaymentType.Card:
                    return "D";
                case BasePaymentType.Coupon:
                    return "C";
                default:
                    return "P";
            }
        }

        public override void CloseFiscal ()
        {
            if (balance > 0)
                Payment ();

            SendMessage (CommandCodes.CloseFiscal);
        }

        public override void CancelFiscalOperation (out bool annulled)
        {
            bool fiscalOpened;
            int lineCount;
            double currentBalance;
            double paid;

            annulled = false;
            GetFiscalReceiptStatus (false, out fiscalOpened, out lineCount, out currentBalance, out paid);
            if (currentBalance > 0) {
                SendMessage (CommandCodes.GetCheckStatus);

                string [] checkStatus = defaultEnc.GetString (receivedArguments).Split (',');
                bool canVoid = int.Parse (checkStatus [0]) == 1;
                NumberFormatInfo n = new NumberFormatInfo { NumberDecimalSeparator = ".", NumberGroupSeparator = string.Empty };

                double groupABalance = double.Parse (checkStatus [1], n) / 100;
                double groupBBalance = double.Parse (checkStatus [2], n) / 100;
                double groupCBalance = double.Parse (checkStatus [3], n) / 100;
                double groupDBalance = double.Parse (checkStatus [4], n) / 100;

                if (canVoid) {
                    if (groupABalance > 0)
                        AddItem (null, anullString, 1, null, -groupABalance, 20, 0, FiscalPrinterTaxGroup.A);

                    if (groupBBalance > 0)
                        AddItem (null, anullString, 1, null, -groupBBalance, 20, 0, FiscalPrinterTaxGroup.B);

                    if (groupCBalance > 0)
                        AddItem (null, anullString, 1, null, -groupCBalance, 20, 0, FiscalPrinterTaxGroup.C);

                    if (groupDBalance > 0)
                        AddItem (null, anullString, 1, null, -groupDBalance, 20, 0, FiscalPrinterTaxGroup.D);

                    currentBalance = 0;
                    annulled = true;
                }
            }

            balance = currentBalance;
            Payment ();
            CloseFiscal ();
        }

        public override void PrintTextFiscal (string text, TextAlign align)
        {
            text = GetAlignedString (text, fiscalTextCharsPerLine, align);
            if (string.IsNullOrEmpty (text))
                text = " ";

            SendMessage (CommandCodes.PrintTextFiscal, GetTextBytes (text));
        }

        public override void PrintDuplicate ()
        {
            byte [] result = new byte [1];
            result [0] = (byte) '1';

            SendMessage (CommandCodes.PrintDuplicate, result);
        }

        #endregion

        #region Printer commands

        public override void GetStatus ()
        {
            SendMessage (CommandCodes.GetStatus, defaultEnc.GetBytes ("W"));
        }

        public override void PaperFeed (int lines)
        {
            byte [] result;

            if (lines < 1)
                lines = 1;
            else if (lines > 99)
                lines = 99;

            if (lines == 1)
                result = new byte [0];
            else {
                result = new byte [1];
                result [0] = (byte) lines;
            }

            SendMessage (CommandCodes.PaperFeed, result);
        }

        public override void PaperCut ()
        {
            SendMessage (CommandCodes.PaperCut);
        }

        #endregion

        #region Information commands

        public override void GetFiscalReceiptStatus (bool includePaid, out bool fiscalOpened, out int lineCount, out double amount, out double paid)
        {
            SendMessage (CommandCodes.GetFiscalStatus, includePaid ? defaultEnc.GetBytes ("T") : new byte [0]);

            string [] fiscalStatus = defaultEnc.GetString (receivedArguments).Split (',');
            if (fiscalStatus.Length < 3)
                throw new HardwareErrorException (new ErrorState (ErrorState.BadPrinterStatusReceived, HardwareErrorSeverity.Error));

            fiscalOpened = fiscalStatus [0] == "1";
            lineCount = int.Parse (fiscalStatus [1]);

            NumberFormatInfo n = new NumberFormatInfo { NumberDecimalSeparator = ".", NumberGroupSeparator = string.Empty };

            amount = double.Parse (fiscalStatus [2], n) / 100;
            if (includePaid && fiscalStatus.Length > 3)
                paid = double.Parse (fiscalStatus [3], n) / 100;
            else
                paid = 0;
        }

        public override void GetDiagnosticInfo (bool calculateCRC, out string firmwareVer, out DateTime firmwareDate, out string firmwareCRC, out int country, out string fpSerial)
        {
            SendMessage (CommandCodes.GetDiagnosticData, defaultEnc.GetBytes (calculateCRC ? "1" : "0"));

            string receivedString = defaultEnc.GetString (receivedArguments);

            int delimiter = receivedString.IndexOf (' ');
            if (delimiter > 0) {
                firmwareVer = receivedString.Substring (0, delimiter);
                receivedString = receivedString.Substring (Math.Min (delimiter + 1, receivedString.Length));
            } else
                firmwareVer = string.Empty;

            string [] data = receivedString.Split (',');
            DateTime.TryParseExact (data [0].Replace ("  ", " "), "MMM d yyyy HH:mm", null, DateTimeStyles.None, out firmwareDate);

            firmwareCRC = data [1];
            country = int.Parse (data [3]);
            fpSerial = data [4];
        }

        public override void GetTaxNumber (out string taxNumber)
        {
            SendMessage (CommandCodes.GetTaxNumber);

            string [] args = defaultEnc.GetString (receivedArguments).Split (',');
            taxNumber = args [0];
        }

        public override void GetLastDocumentNumber (out string docNumber)
        {
            SendMessage (CommandCodes.GetLastDocumentNumber);

            docNumber = defaultEnc.GetString (receivedArguments);
        }

        public override void GetDateTime (out DateTime printerDateTime)
        {
            SendMessage (CommandCodes.GetDateTime);

            DateTime.TryParseExact (defaultEnc.GetString (receivedArguments), "dd-MM-yy HH:mm:ss", null, DateTimeStyles.None, out printerDateTime);
        }

        public override void GetSerialNumbers (out string fpSerial, out string fiscalMemSerial)
        {
            SendMessage (CommandCodes.GetDiagnosticData, defaultEnc.GetBytes ("0"));

            string receivedString = defaultEnc.GetString (receivedArguments);

            int delimiter = receivedString.IndexOf (' ');
            if (delimiter > 0) {
                receivedString = receivedString.Substring (Math.Min (delimiter + 1, receivedString.Length));
            }

            string [] data = receivedString.Split (',');
            fpSerial = data [4];
            fiscalMemSerial = string.Empty;
        }

        #endregion

        #region Report commands

        public override void DailyXReport (bool resetOperations, out string closure, out Dictionary<FiscalPrinterTaxGroup, double> amounts)
        {
            DailyReport (DailyReportType.X, resetOperations, out closure, out amounts);
        }

        public override void DailyZReport (bool resetOperations, out string closure, out Dictionary<FiscalPrinterTaxGroup, double> amounts)
        {
            DailyReport (DailyReportType.Z, resetOperations, out closure, out amounts);
        }

        private void DailyReport (DailyReportType type, bool resetOperations, out string closure, out Dictionary<FiscalPrinterTaxGroup, double> amounts)
        {
            List<byte> pars = new List<byte> ();
            switch (type) {
                case DailyReportType.Z:
                    pars.AddRange (defaultEnc.GetBytes ("0"));
                    if (!resetOperations)
                        pars.AddRange (defaultEnc.GetBytes ("N"));
                    break;
                case DailyReportType.X:
                    pars.AddRange (defaultEnc.GetBytes ("2"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException ("type");
            }

            SendMessage (CommandCodes.DailyReport, pars.ToArray ());

            string [] ret = defaultEnc.GetString (receivedArguments).Split (',');

            closure = ret.Length > 0 ? ret [0] : string.Empty;

            if (ret.Length != 6) {
                amounts = new Dictionary<FiscalPrinterTaxGroup, double> ();
                return;
            }

            NumberFormatInfo n = new NumberFormatInfo { NumberDecimalSeparator = ".", NumberGroupSeparator = string.Empty };
            amounts = new Dictionary<FiscalPrinterTaxGroup, double>
                {
                    {FiscalPrinterTaxGroup.A, double.Parse (ret [2], n)},
                    {FiscalPrinterTaxGroup.B, double.Parse (ret [3], n)},
                    {FiscalPrinterTaxGroup.C, double.Parse (ret [4], n)},
                    {FiscalPrinterTaxGroup.D, double.Parse (ret [5], n)}
                };
        }

        public override void DetailFMReportByNumbers (int start, int end)
        {
            FMReportByNumbers (CommandCodes.DetailFMReportByNumbers, start, end);
        }

        public override void ShortFMReportByNumbers (int start, int end)
        {
            FMReportByNumbers (CommandCodes.ShortFMReportByNumbers, start, end);
        }

        protected virtual void FMReportByNumbers (CommandCodes command, int start, int end)
        {
            start = Math.Min (start, 9999);
            start = Math.Max (start, 0);

            end = Math.Min (end, 9999);
            end = Math.Max (end, 0);

            int validStart = Math.Min (end, start);
            int validEnd = Math.Max (end, start);
            NumberFormatInfo n = new NumberFormatInfo { NumberDecimalSeparator = ".", NumberGroupSeparator = string.Empty };

            SendMessage (command, defaultEnc.GetBytes (
                string.Format ("{0},{1}", validStart.ToString (n), validEnd.ToString (n))));
        }

        public override void DetailFMReportByDates (DateTime start, DateTime end)
        {
            SendMessage (CommandCodes.DetailFMReportByDates, defaultEnc.GetBytes (
                string.Format ("{0},{1}", start.ToString ("ddMMyy"), end.ToString ("ddMMyy"))));
        }

        public override void ShortFMReportByDates (DateTime start, DateTime end)
        {
            SendMessage (CommandCodes.ShortFMReportByDates, defaultEnc.GetBytes (
                string.Format ("{0},{1}", start.ToString ("ddMMyy"), end.ToString ("ddMMyy"))));
        }

        public override void OperatorsReport ()
        {
            SendMessage (CommandCodes.OperatorsReport);
        }

        #endregion

        #region Other commands

        public override void OpenCashDrawer ()
        {
            if (openCashDrawerEnabled)
                SendMessage (CommandCodes.OpenCashDrawer);
        }

        public override void RegisterCash (double amount, string cashDescription, string reason)
        {
            List<byte> data = new List<byte> ();

            amount = Math.Min (999999999, amount);
            amount = Math.Max (-999999999, amount);

            NumberFormatInfo n = new NumberFormatInfo { NumberDecimalSeparator = ".", NumberGroupSeparator = string.Empty };
            data.AddRange (defaultEnc.GetBytes (amount.ToString (n)));

            SendMessage (CommandCodes.RegisterCash, data.ToArray ());
        }

        #endregion

        #region Display commands

        public virtual void DisplayClear ()
        {
            SendMessage (CommandCodes.DisplayClear);
        }

        public virtual void DisplayShowDateTime ()
        {
            SendMessage (CommandCodes.DisplayShowDateTime);
        }

        public virtual void DisplayLowerText (string text)
        {
            text = text.Trim (displayCharsPerLine);
            if (text.Length == 0)
                text = " ";

            SendMessage (CommandCodes.DisplaySetLowerText, GetTextBytes (text));
        }

        public virtual void DisplayUpperText (string text)
        {
            text = text.Trim (displayCharsPerLine);
            if (text.Length == 0)
                text = " ";

            SendMessage (CommandCodes.DisplaySetUpperText, GetTextBytes (text));
        }

        #endregion

        #region Private methods

        protected virtual string TaxGroupToString (FiscalPrinterTaxGroup group)
        {
            switch (group) {
                case FiscalPrinterTaxGroup.A:
                    return "А";
                case FiscalPrinterTaxGroup.B:
                    return "Б";
                case FiscalPrinterTaxGroup.C:
                    return "В";
                case FiscalPrinterTaxGroup.D:
                    return "Г";
                default:
                    return "Б";
            }
        }

        #region Sending data

        private byte [] PackMessage (byte command, byte [] commandArgs)
        {
            List<byte> res = new List<byte> ();

            #region Write header data

            int argsLen = commandArgs != null ? commandArgs.Length : 0;

            res.Add (MARKER_START);
            res.Add ((byte) (argsLen + 4 + 32)); //4 = <Preamble> + <LEN> + <SEQ> + <CMD> 32 = 0x20, 
            res.Add ((byte) frameSeqNumber);
            res.Add (command);

            #endregion

            // Write operation data
            if (commandArgs != null)
                res.AddRange (commandArgs);

            #region Write footer data

            res.Add (MARKER_ARGS_END);

            // Calculate checksum
            int checkSum = 0;
            for (int i = 1; i < res.Count; i++) {
                checkSum += res [i];
            }

            // Write the check sum
            res.Add ((byte) (((checkSum >> 0xC) & 0xf) + '0'));
            res.Add ((byte) (((checkSum >> 0x8) & 0xf) + '0'));
            res.Add ((byte) (((checkSum >> 0x4) & 0xf) + '0'));
            res.Add ((byte) (((checkSum >> 0x0) & 0xf) + '0'));

            // Write terminator
            res.Add (MARKER_END);

            #endregion

            return res.ToArray ();
        }

        protected void SendMessage (CommandCodes command, params byte [] commandArgs)
        {
            SendMessage ((byte) command, commandArgs);
        }

        protected void SendMessage (byte command, params byte [] commandArgs)
        {
            byte [] packedCommand = PackMessage (command, commandArgs);

            lock (syncRoot) {
                for (int i = 0; i < Connector.MaxSendRetries; i++) {
                    if (!Connector.IsConnected)
                        return;

                    Connector.SendMessage (packedCommand);

                    try {
                        receivedMessage = ReceiveMessage ();
                        if (receivedMessage [0] == MARKER_START) {
                            UnpackMessage (receivedMessage);
                            lastErrorState.Command = command;

                            frameSeqNumber++;
                            if (frameSeqNumber == 0x20 + 30)
                                frameSeqNumber = 0x20;

                            if (lastErrorState.Errors.Count > 0)
                                throw new HardwareErrorException (lastErrorState, command);

                            return;
                        }
                    } catch (CommandNotAcknowledgedException) {
                    }

                    // NACK or no answer received so wait a little untill the next retry
                    Thread.Sleep (Connector.SendRetryWait);
                }
            }

            lastErrorState.SetError (ErrorState.CashReceiptPrinterDisconnected);

            throw new HardwareErrorException (lastErrorState);
        }

        #endregion

        #region Receiving data

        private byte [] ReceiveMessage ()
        {
            List<byte> answer = new List<byte> ();
            for (int faults = 0; faults < Connector.MaxReceiveRetries; ) {
                try {
                    byte inByte = Connector.ReceiveByte ();

                    // If we receive something, zero the faults counter
                    faults = 0;
                    if (inByte == MARKER_SYNC ||
                        inByte == MARKER_ACK)
                        continue;

                    if (answer.Count != 0 ||
                        inByte == MARKER_START)
                        answer.Add (inByte);

                    if (inByte == MARKER_END)
                        break;
                } catch (TimeoutException) {
                    Thread.Sleep (Connector.ReceiveRetryWait);
                    faults++;
                } catch (UnauthorizedAccessException) {
                    Thread.Sleep (Connector.ReceiveRetryWait);
                    faults++;
                }
            }

            if (answer.Count == 0 ||
                (answer.Count > 4 && answer [4] == MARKER_NACK)) {
                throw new CommandNotAcknowledgedException ("Unable to receive command confirmation from printer");
            }

            return answer.ToArray ();
        }

        private void UnpackMessage (byte [] data)
        {
            List<byte> args = new List<byte> ();
            List<byte> status = new List<byte> ();
            bool separatorFound = false;

            if (data == null)
                throw new ArgumentNullException ("data");

            if (data.Length < 5)
                throw new InvalidDataException ();

            if (data [0] != MARKER_START)
                throw new InvalidDataException ();

            receivedCommand = (CommandCodes) data [3];
            for (int i = 4; i < data.Length; i++) {
                if (data [i] == MARKER_ARGS_END)
                    break;

                if (data [i] == MARKER_ARGS_SEPARATOR && !separatorFound) {
                    separatorFound = true;
                    continue;
                }

                if (separatorFound)
                    status.Add (data [i]);
                else
                    args.Add (data [i]);
            }

            receivedArguments = args.ToArray ();
            ParseStatus (status.ToArray ());
        }

        protected virtual void ParseStatus (byte [] statusData)
        {
            lastErrorState.Clear ();

            if (statusData == null || statusData.Length != 6) {
                lastErrorState.SetWarning (ErrorState.BadPrinterStatusReceived);
                return;
            }

            byte b = statusData [0];
            if ((b & 0x01) != 0)
                lastErrorState.SetError (ErrorState.SyntaxError);
            if ((b & 0x02) != 0)
                lastErrorState.SetError (ErrorState.InvalidOperationCode);
            if ((b & 0x04) != 0)
                lastErrorState.SetWarning (ErrorState.ClockNotSet);
            if ((b & 0x10) != 0)
                lastErrorState.SetError (ErrorState.PrintHeadFault);
            if ((b & 0x20) != 0)
                lastErrorState.SetError (ErrorState.GeneralError);

            b = statusData [1];
            if ((b & 0x01) != 0)
                lastErrorState.SetError (ErrorState.SumOverflow);
            if ((b & 0x02) != 0)
                lastErrorState.SetError (ErrorState.CmdNotAllowed);
            if ((b & 0x04) != 0)
                lastErrorState.SetError (ErrorState.RAM_IsReset);
            if ((b & 0x10) != 0)
                lastErrorState.SetError (ErrorState.RAM_Corrupted);
            if ((b & 0x20) != 0)
                lastErrorState.SetError (ErrorState.LidOpen);

            b = statusData [2];
            if ((b & 0x01) != 0)
                lastErrorState.SetError (ErrorState.NoPaper);
            if ((b & 0x02) != 0)
                lastErrorState.SetWarning (ErrorState.LittlePaper);
            if ((b & 0x04) != 0)
                lastErrorState.SetError (ErrorState.EKL_NoPaper);
            if ((b & 0x08) != 0)
                lastErrorState.SetInformation (ErrorState.FiscalCheckOpen);
            if ((b & 0x10) != 0)
                lastErrorState.SetWarning (ErrorState.EKL_LittlePaper);
            if ((b & 0x20) != 0)
                lastErrorState.SetInformation (ErrorState.NonFiscalCheckOpen);

            b = statusData [3];
            if ((b & 0x01) != 0)
                lastErrorState.SetInformation (ErrorState.Switch1IsOn);
            if ((b & 0x02) != 0)
                lastErrorState.SetInformation (ErrorState.Switch2IsOn);
            if ((b & 0x04) != 0)
                lastErrorState.SetInformation (ErrorState.Switch3IsOn);
            if ((b & 0x08) != 0)
                lastErrorState.SetInformation (ErrorState.Switch4IsOn);

            b = statusData [4];
            if ((b & 0x01) != 0)
                lastErrorState.SetError (ErrorState.FiscalMemWriteError);
            if ((b & 0x08) != 0)
                lastErrorState.SetInformation (ErrorState.FiscalMemLowSpace);
            if ((b & 0x10) != 0)
                lastErrorState.SetError (ErrorState.FiscalMemFull);
            if ((b & 0x20) != 0)
                lastErrorState.SetError (ErrorState.FiscalMemGeneralError);

            b = statusData [5];
            if ((b & 0x01) != 0)
                lastErrorState.SetError (ErrorState.FiscalMemIsReadOnly);
            if ((b & 0x02) != 0)
                lastErrorState.SetInformation (ErrorState.FiscalMemIsFormatted);
            if ((b & 0x08) != 0)
                lastErrorState.SetInformation (ErrorState.FiscalMode);
            if ((b & 0x10) != 0)
                lastErrorState.SetInformation (ErrorState.TaxValuesAreSet);
            if ((b & 0x20) != 0)
                lastErrorState.SetInformation (ErrorState.SerialNumbersAreSet);
        }

        #endregion

        #endregion
    }
}
