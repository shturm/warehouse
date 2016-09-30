//
// ErrorState.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Warehouse.Business.Devices
{
    public enum HardwareErrorSeverity
    {
        Information,
        Warning,
        Error
    }

    public class ErrorState
    {
        public const string SyntaxError = "SyntaxError";
        public const string InvalidOperationCode = "InvalidOperationCode";
        public const string ClockNotSet = "ClockNotSet";
        public const string DateNotSet = "DateNotSet";
        public const string ClockHardwareError = "ClockHardwareError";
        public const string PrintHeadFault = "PrintHeadFault";
        public const string PrintHeadOverheat = "PrintHeadOverheat";
        public const string GeneralError = "GeneralError";

        public const string SumOverflow = "SumOverflow";
        public const string CmdNotAllowed = "CmdNotAllowed";
        public const string RAM_IsReset = "RAM_IsReset";
        public const string RAM_Corrupted = "RAM_Corrupted";
        public const string LidOpen = "LidOpen";

        public const string NoPaper = "NoPaper";
        public const string LittlePaper = "LittlePaper";
        public const string EKL_NoPaper = "EKL_NoPaper";
        public const string FiscalCheckOpen = "FiscalCheckOpen";
        public const string EKL_LittlePaper = "EKL_LittlePaper";
        public const string EKL_MemoryLow = "EKL_MemoryLow";
        public const string EKL_Overflow = "EKL_Overflow";
        public const string EKL_CompactFormat = "EKL_CompactFormat";
        public const string EKL_DetailedFormat = "EKL_DetailedFormat";
        public const string NonFiscalCheckOpen = "NonFiscalCheckOpen";

        public const string Switch1IsOn = "Switch1IsOn";
        public const string Switch2IsOn = "Switch2IsOn";
        public const string Switch3IsOn = "Switch3IsOn";
        public const string Switch4IsOn = "Switch4IsOn";
        public const string Switch5IsOn = "Switch5IsOn";
        public const string Switch6IsOn = "Switch6IsOn";
        public const string Switch7IsOn = "Switch7IsOn";
        public const string Switch8IsOn = "Switch8IsOn";

        public const string FiscalMemReadError = "FiscalMemReadError";
        public const string FiscalMemWriteError = "FiscalMemWriteError";
        public const string NoFiscalMemInstalled = "NoFiscalMemInstalled";
        public const string FiscalMemLowSpace = "FiscalMemLowSpace";
        public const string FiscalMemFull = "FiscalMemFull";
        public const string FiscalMemGeneralError = "FiscalMemGeneralError";

        public const string FiscalMemIsReadOnly = "FiscalMemIsReadOnly";
        public const string FiscalMemIsFormatted = "FiscalMemIsFormatted";
        public const string FiscalMode = "FiscalMode";
        public const string TaxValuesAreSet = "TaxValuesAreSet";
        public const string SerialNumbersAreSet = "SerialNumbersAreSet";

        public const string Report24HRequired = "Report24HRequired";
        public const string WaitingPaperReplaceConfirmation = "WaitingPaperReplaceConfirmation";
        public const string IllegalTaxGroup = "IllegalTaxGroup";
        public const string NoValidTaxGroup = "NoValidTaxGroup";
        public const string LoginFailed3Times = "LoginFailed3Times";
        public const string BadPrinterStatusReceived = "BadPrinterStatusReceived";
        public const string BadSerialPort = "BadSerialPort";
        public const string BadConnectionParameters = "BadCommunicationParameters";
        public const string NotEnoughCashInTheRegister = "NotEnoughCashInTheRegister";
        public const string FiscalPrinterOverflow = "FiscalPrinterOverflow";
        public const string FiscalPrinterNotReady = "FiscalPrinterNotReady";
        public const string TooManyTransactionsInReceipt = "TooManyTransactionsInReceipt";
        public const string VATGroupsMismatch = "VATGroupsMismatch";
        public const string InvalidInputData = "InvalidInputData";
        public const string EvalPriceLimitation = "EvalPriceLimitation";
        public const string EvalQttyLimitation = "EvalQttyLimitation";
        public const string EvalItemsLimitation = "EvalItemsLimitation";
        public const string EvalLimitation = "EvalLimitation";
        public const string CommandNotAcknoledged = "CommandNotAcknoledged";
        public const string PowerInterruption = "PowerInterruption";
        public const string DailyReportNotEmpty = "DailyReportNotEmpty";
        public const string ItemsReportNotEmpty = "ItemsReportNotEmpty";
        public const string OperatorsReportNotEmpty = "OperatorsReportNotEmpty";
        public const string ReportsOverflow = "ReportsOverflow";
        public const string DuplicateNotPrinted = "DuplicateNotPrinted";
        public const string RegularReceipt = "RegularReceipt";
        public const string PrintVATInReceipt = "PrintVATInReceipt";
        public const string ReceiptIsInvoice = "ReceiptIsInvoice";
        public const string UsingNonIntegerNumbers = "UsingNonIntegerNumbers";
        public const string UsingIntegerNumbers = "UsingIntegerNumbers";
        public const string AutoCutPaper = "AutoCutPaper";
        public const string TransparentDisplay = "TransparentDisplay";
        public const string CommunicationSpeed9600 = "CommunicationSpeed9600";
        public const string CommunicationSpeed19200 = "CommunicationSpeed19200";
        public const string AutoOpenSafe = "AutoOpenSafe";
        public const string PrintLogoOnReceipts = "PrintLogoOnReceipts";
        public const string PrintTotalInForeignCurrency = "PrintTotalInForeignCurrency";
        public const string BadPassword = "BadPassword";
        public const string BadLogicalAddress = "BadLogicalAddress";

        public const string KitchenPrinterError = "KitchenPrinterError";
        public const string KitchenPrinterNoPaper = "KitchenPrinterNoPaper";

        public const string CashReceiptPrinterDisconnected = "CashReceiptPrinterDisconnected";
        public const string FiscalPrinterBusy = "FiscalPrinterBusy";
        public const string NonFiscalPrinterDisconnected = "NonFiscalPrinterDisconnected";
        public const string ExternalDisplayDisconnected = "ExternalDisplayDisconnected";
        public const string KitchenPrinterDisconnected = "KitchenPrinterDisconnected";
        public const string CardReaderDisconnected = "CardReaderDisconnected";
        public const string ElectronicScaleDisconnected = "ElectronicScaleDisconnected";
        public const string ElectronicScaleNotEnabled = "ElectronicScaleNotEnabled";
        public const string SalesDataControllerDisconnected = "SalesDataControllerDisconnected";
        public const string BarcodeScannerDisconnected = "BarcodeScannerDisconnected";
        public const string DriverNotFound = "DriverNotFound";

        private readonly Dictionary<string, bool> informations = new Dictionary<string, bool> ();
        private readonly Dictionary<string, bool> warnings = new Dictionary<string, bool> ();
        private readonly Dictionary<string, bool> errors = new Dictionary<string, bool> ();
        private byte? command;

        public Dictionary<string, bool> Informations
        {
            get { return informations; }
        }

        public Dictionary<string, bool> Warnings
        {
            get { return warnings; }
        }

        public Dictionary<string, bool> Errors
        {
            get { return errors; }
        }

        public int Count
        {
            get
            {
                return informations.Count + warnings.Count + errors.Count;
            }
        }

        public byte? Command
        {
            get { return command; }
            set { command = value; }
        }

        public ErrorState ()
        {
        }

        public ErrorState (string error, HardwareErrorSeverity sev)
        {
            Set (sev, error);
        }

        public void Set (HardwareErrorSeverity sev, string error)
        {
            switch (sev) {
                case HardwareErrorSeverity.Information:
                    if (!informations.ContainsKey (error))
                        informations.Add (error, true);
                    break;
                case HardwareErrorSeverity.Warning:
                    if (!warnings.ContainsKey (error))
                        warnings.Add (error, true);
                    break;
                case HardwareErrorSeverity.Error:
                    if (!errors.ContainsKey (error))
                        errors.Add (error, true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException ("sev");
            }
        }

        public void SetInformation (string error)
        {
            if (!informations.ContainsKey (error))
                informations.Add (error, true);
        }

        public void SetWarning (string error)
        {
            if (!warnings.ContainsKey (error))
                warnings.Add (error, true);
        }

        public void SetError (string error)
        {
            if (!errors.ContainsKey (error))
                errors.Add (error, true);
        }

        public bool Check (string error)
        {
            return CheckError (error) || CheckWarning (error) || CheckInformation (error);
        }

        public bool CheckInformation (string error)
        {
            return informations.ContainsKey (error);
        }

        public bool CheckWarning (string error)
        {
            return warnings.ContainsKey (error);
        }

        public bool CheckError (string error)
        {
            return errors.ContainsKey (error);
        }

        public bool Unset (string error)
        {
            if (informations.ContainsKey (error)) {
                informations.Remove (error);
                return true;
            }

            if (warnings.ContainsKey (error)) {
                warnings.Remove (error);
                return true;
            }

            if (errors.ContainsKey (error)) {
                errors.Remove (error);
                return true;
            }

            return false;
        }

        public void Clear ()
        {
            informations.Clear ();
            warnings.Clear ();
            errors.Clear ();
            command = null;
        }

        public override string ToString ()
        {
            StringBuilder err = new StringBuilder ();
            foreach (KeyValuePair<string, bool> pair in errors) {
                if (err.Length > 0)
                    err.Append (", ");

                err.Append (pair.Key);
            }

            StringBuilder warn = new StringBuilder ();
            foreach (KeyValuePair<string, bool> pair in warnings) {
                if (warn.Length > 0)
                    warn.Append (", ");

                warn.Append (pair.Key);
            }

            StringBuilder info = new StringBuilder ();
            foreach (KeyValuePair<string, bool> pair in informations) {
                if (info.Length > 0)
                    info.Append (", ");

                info.Append (pair.Key);
            }

            StringBuilder ret = new StringBuilder ();
            if (command != null)
                ret.AppendFormat ("Command: {0}", command);

            if (err.Length > 0) {
                if (ret.Length > 0)
                    ret.Append ("\n");

                ret.AppendFormat ("Errors: {0}", err);
            }

            if (warn.Length > 0) {
                if (ret.Length > 0)
                    ret.Append ("\n");

                ret.AppendFormat ("Warnings: {0}", warn);
            }

            if (info.Length > 0) {
                if (ret.Length > 0)
                    ret.Append ("\n");

                ret.AppendFormat ("Information: {0}", info);
            }

            return ret.ToString ();
        }
    }
}
