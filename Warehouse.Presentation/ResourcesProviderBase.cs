//
// ResourcesProviderBase.cs
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
using System.IO;
using System.Reflection;
using System.Xml;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Documenting;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Component.Documenting;
using Warehouse.Component.Printing;
using Warehouse.Data;
using Warehouse.Presentation.Dialogs;

namespace Warehouse.Presentation
{
    public abstract class ResourcesProviderBase
    {
        public abstract string GetResourceName (string resource);

        public abstract void ShowWindowHelp (string windowName);

        public Image LoadImage (string imgResourceFileName)
        {
            return Assembly.GetAssembly (GetType ()).LoadImage (GetResourceName (imgResourceFileName));
        }

        public Image LoadAnimation (string imgResourceFileName)
        {
            return Assembly.GetAssembly (GetType ()).LoadAnimation (GetResourceName (imgResourceFileName));
        }

        protected static bool TryShowHelpFile (string productName, string localeName, string windowName)
        {
            string filePath = Path.Combine (StoragePaths.AppFolder, "Help");
            filePath = Path.Combine (filePath, productName, localeName);
            if (windowName != "index.htm")
                filePath = Path.Combine (filePath, "scr");
            filePath = Path.Combine (filePath, windowName);

            if (!File.Exists (filePath))
                return false;

            ComponentHelper.OpenUrlInBrowser (filePath);
            return true;
        }

        public Form LoadForm (IDocument document)
        {
            try {
                return document.LoadForm ();
            } catch (Exception exception) {
                if (exception is XmlException) {
                    MessageError.ShowDialog (string.Format (Translator.GetString ("The file \"{0}\" has an incorrect structure (invalid XML)."),
                        Path.Combine (BusinessDomain.AppConfiguration.DocumentTemplatesFolder, Path.ChangeExtension (document.FormName, "xml"))), ErrorSeverity.Error);
                }
                ErrorHandling.LogException (exception);
                return document.LoadForm (string.Empty);
            }
        }

        public void PrintForm (Form form, PrintSettings printSettings = null, MarginsI margins = null)
        {
            try {
                GtkFormPrintDocument doc = new GtkFormPrintDocument (form);
                if (margins != null)
                    doc.SetDefaultMargins (margins.Top, margins.Bottom, margins.Left, margins.Right, margins.UnitType);

                string printerName = null;
                if (printSettings != null) {
                    doc.PrintSettings = printSettings.Copy ();
                    if (!string.IsNullOrWhiteSpace (printSettings.Printer))
                        printerName = printSettings.Printer;

                    doc.DefaultPageSetup.Orientation = printSettings.Orientation;
                    if (printSettings.PaperSize != null &&
                        printSettings.PaperSize.IsCustom)
                        doc.DefaultPageSetup.PaperSize = printSettings.PaperSize;
                }

                doc.SetPrinterSettings (printerName);

                doc.Run (PrintOperationAction.Print, ComponentHelper.TopWindow);
            } catch (Exception ex) {
                ErrorHandling.LogException (ex, ErrorSeverity.FatalError);
            }
        }

        public void PrintObject (IDocument document, PrintSettings printSettings = null)
        {
            foreach (Form form in GetForms (document))
                PrintForm (form, printSettings);
        }

        public IList<Form> GetForms (IDocument document)
        {
            DocumentBase documentBase = document as DocumentBase;
            if (documentBase == null)
                return new List<Form> { LoadForm (document) };

            List<Form> forms = new List<Form> ();
            if (documentBase.PrintOriginal) {
                documentBase.IsOriginal = true;
                forms.Add (LoadForm (document));
            }

            if (documentBase.PrintInternational) {
                documentBase.IsOriginal = null;
                forms.Add (LoadForm (documentBase.GetInternational ()));
            }

            for (int i = 0; i < documentBase.PrintCopies; i++) {
                documentBase.IsOriginal = false;
                forms.Add (LoadForm (documentBase));
            }
            return forms;
        }

        public void PrintPreviewObject (IDocument document)
        {
            WbpPrintPreview printPreview = new WbpPrintPreview (document);
            printPreview.PrintDocument.SetPrinterSettings ();
            PresentationDomain.MainForm.AddNewPage (printPreview);
        }

    }
}
