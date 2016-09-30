//
// ConfigurationHolder.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   06/26/2006
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
using System.Configuration;
using System.Drawing.Printing;
using System.Linq;
using Warehouse.Business.Entities;

namespace Warehouse.Business
{
    public abstract class ConfigurationHolder : ConfigurationHolderBase
    {
        private readonly Dictionary<Type, IConfigurationAddin> addins = new Dictionary<Type, IConfigurationAddin> ();

        public bool DisableSave { get; set; }

        public Dictionary<Type, IConfigurationAddin> Addins
        {
            get { return addins; }
        }

        #region Printing configuration

        [ConfigurationMember ("true")]
        public override bool UseDefaultDocumentPrinter
        {
            get { return useDefaultDocumentPrinter; }
            set
            {
                if (useDefaultDocumentPrinter == value)
                    return;

                if (value)
                    DocumentPrinterName = GetDefaultPrinterName ();

                SetValueConfig (() => UseDefaultDocumentPrinter, ref useDefaultDocumentPrinter, value);
            }
        }

        private int printerMarginTop;
        [ConfigurationMember ("100")]
        public int PrinterMarginTop
        {
            get { return printerMarginTop; }
            set { SetValueConfig (() => PrinterMarginTop, ref printerMarginTop, value); }
        }

        private int printerMarginBottom;
        [ConfigurationMember ("100")]
        public int PrinterMarginBottom
        {
            get { return printerMarginBottom; }
            set { SetValueConfig (() => PrinterMarginBottom, ref printerMarginBottom, value); }
        }

        private int printerMarginLeft;
        [ConfigurationMember ("100")]
        public int PrinterMarginLeft
        {
            get { return printerMarginLeft; }
            set { SetValueConfig (() => PrinterMarginLeft, ref printerMarginLeft, value); }
        }

        private int printerMarginRight;
        [ConfigurationMember ("100")]
        public int PrinterMarginRight
        {
            get { return printerMarginRight; }
            set { SetValueConfig (() => PrinterMarginRight, ref printerMarginRight, value); }
        }

        private bool alwaysPrintTransfersUsingSalePrices;
        [ConfigurationMember ("false")]
        public bool AlwaysPrintTransfersUsingSalePrices
        {
            get { return alwaysPrintTransfersUsingSalePrices; }
            set { SetValueConfig (() => AlwaysPrintTransfersUsingSalePrices, ref alwaysPrintTransfersUsingSalePrices, value); }
        }

        #endregion

        #region Last report state

        private string lastReportType;
        [ConfigurationMember ("")]
        public string LastReportType
        {
            get { return lastReportType; }
            set { SetClassConfig (() => LastReportType, ref lastReportType, value); }
        }

        private bool lastReportArgPresent;
        [ConfigurationMember ("false")]
        public bool LastReportArgPresent
        {
            get { return lastReportArgPresent; }
            set { SetValueConfig (() => LastReportArgPresent, ref lastReportArgPresent, value); }
        }

        private long lastReportArg;
        [ConfigurationMember ("0")]
        public long LastReportArg
        {
            get { return lastReportArg; }
            set { SetValueConfig (() => LastReportArg, ref lastReportArg, value); }
        }

        private string lastReportTitle;
        [ConfigurationMember ("")]
        public string LastReportTitle
        {
            get { return lastReportTitle; }
            set { SetClassConfig (() => LastReportTitle, ref lastReportTitle, value); }
        }

        #endregion

        private bool showOperationStatistics;
        [ConfigurationMember ("false")]
        public bool ShowOperationStatistics
        {
            get { return showOperationStatistics; }
            set { SetValueConfig (() => ShowOperationStatistics, ref showOperationStatistics, value); }
        }

        private bool allowMultipleInstances;
        [ConfigurationMember ("true")]
        public bool AllowMultipleInstances
        {
            get { return allowMultipleInstances; }
            set { SetValueConfig (() => AllowMultipleInstances, ref allowMultipleInstances, value); }
        }

        private ViewProfile currentViewProfile;
        public ViewProfile CurrentViewProfile
        {
            get { return currentViewProfile; }
            set { SetClassConfig (() => CurrentViewProfile, ref currentViewProfile, value); }
        }

        private bool showItemSuggestionsWhenNotFound;
        [ConfigurationMember ("true")]
        public bool ShowItemSuggestionsWhenNotFound
        {
            get { return showItemSuggestionsWhenNotFound; }
            set { SetValueConfig (() => ShowItemSuggestionsWhenNotFound, ref showItemSuggestionsWhenNotFound, value); }
        }

        private bool showPartnerSuggestionsWhenNotFound;
        [ConfigurationMember ("true")]
        public bool ShowPartnerSuggestionsWhenNotFound
        {
            get { return showPartnerSuggestionsWhenNotFound; }
            set { SetValueConfig (() => ShowPartnerSuggestionsWhenNotFound, ref showPartnerSuggestionsWhenNotFound, value); }
        }

        public List<string> GetAllInstalledPrinters ()
        {
            try {
                return PrinterSettings.InstalledPrinters.Cast<string> ().ToList ();
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                return new List<string> ();
            }
        }

        public string GetDefaultPrinterName ()
        {
            try {
                return new PrinterSettings ().PrinterName;
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                return null;
            }
        }

        public bool IsPrinterAvailable (string printerName = "")
        {
            string printer = string.IsNullOrEmpty (printerName) && !useDefaultDocumentPrinter ?
                documentPrinterName : printerName;

            List<string> printers = GetAllInstalledPrinters ();
            if (!string.IsNullOrWhiteSpace (printer) &&
                printers.Any (p => p == printer))
                return true;

            return printers.Count > 0;
        }

        public bool IsPrintingAvailable (string printerName = "")
        {
            return IsPrinterAvailable (printerName) || BusinessDomain.DocumentExporters.Count > 0;
        }

        public override void Load (bool loadLocal)
        {
            base.Load (loadLocal);

            foreach (KeyValuePair<Type, IConfigurationAddin> pair in addins)
                Load (pair.Value, loadLocal);
        }

        public override void Save (bool saveToDb)
        {
            base.Save (saveToDb);

            foreach (KeyValuePair<Type, IConfigurationAddin> pair in addins)
                SetSettings (pair.Value, saveToDb);

            SaveSettings ();
        }

        protected Configuration config;
        protected KeyValueConfigurationCollection settings;

        protected override bool TryGetSetting (string key, out string value)
        {
            KeyValueConfigurationElement configEntry = settings [key];
            if (configEntry == null || configEntry.Value == null) {
                value = null;
                return false;
            }

            value = configEntry.Value;
            return true;
        }

        protected override void SetSetting (string key, string valueString)
        {
            settings.Remove (key);
            settings.Add (key, valueString);
        }

        protected override void DeleteSetting (string key)
        {
            settings.Remove (key);
        }

        protected override void SaveSettings ()
        {
            try {
                if (config != null && !DisableSave)
                    config.Save ();
            } catch (UnauthorizedAccessException) {
            } catch (ConfigurationErrorsException) {
            }
        }
    }
}
