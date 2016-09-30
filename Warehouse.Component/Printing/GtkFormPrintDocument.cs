//
// GtkFormPrintDocument.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/04/2007
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
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using Cairo;
using Gtk;
using Warehouse.Business;
using Warehouse.Component.Documenting;
using Warehouse.Data;
using PaperSize = Gtk.PaperSize;

namespace Warehouse.Component.Printing
{
    public class GtkFormPrintDocument : PrintOperation
    {
        // The original document design was using System.Drawing and it had this times more pixels in a regular document
        public const double SD_SCALE = 1.3899;
        public const double SD_MM_PER_PIXEL = 0.254;

        private readonly List<string> pageFiles = new List<string> ();
        private Surface previewSurface;

        private bool cancelled;
        public bool Cancelled
        {
            get { return cancelled; }
            set { cancelled = value; }
        }

        private int pageColumns;
        public int PageColumns
        {
            get { return pageColumns; }
            set { pageColumns = value; }
        }

        private int pageRows;
        public int PageRows
        {
            get { return pageRows; }
            set { pageRows = value; }
        }

        private int totalPages;
        public int TotalPages
        {
            get { return totalPages; }
            set { totalPages = value; }
        }

        private Form formToPrint;
        public Form FormToPrint
        {
            get { return formToPrint; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException ("value", "FormToPrint cannot be null");

                formToPrint = value;
            }
        }

        private int? pageWidth;
        public int? PageWidth
        {
            get { return pageWidth; }
            set { pageWidth = value; }
        }

        private RectangleI formArea;
        public RectangleI FormArea
        {
            get { return formArea; }
        }

        private int paperWidth;
        public int PaperWidth
        {
            get { return paperWidth; }
        }

        private int paperHeight;
        public int PaperHeight
        {
            get { return paperHeight; }
        }

        private readonly List<Context> pageContexts = new List<Context> ();
        public List<Context> PageContexts
        {
            get { return pageContexts; }
        }

        private float drawingScaleX = 1;
        public float DrawingScaleX
        {
            get { return drawingScaleX; }
        }

        private float drawingScaleY = 1;
        public float DrawingScaleY
        {
            get { return drawingScaleY; }
        }

        private bool drawingScaleIsValid;
        public bool DrawingScaleIsValid
        {
            get { return drawingScaleIsValid; }
        }

        public GtkFormPrintDocument (IntPtr raw)
            : base (raw)
        {
            ErrorHandling.LogError ("Creating GtkFormPrintDocument from pointer.", ErrorSeverity.Information);
        }

        public GtkFormPrintDocument (Form form)
        {
            if (form == null)
                throw new ArgumentNullException ("form");

            formToPrint = form;

            DefaultPageSetup = new PageSetup
                {
                    Orientation = form.Landscape ? PageOrientation.Landscape : PageOrientation.Portrait
                };

            ConfigurationHolder config = BusinessDomain.AppConfiguration;
            SetDefaultMargins (config.PrinterMarginTop, config.PrinterMarginBottom, config.PrinterMarginLeft, config.PrinterMarginRight);
        }

        public void SetPrinterSettings (string printerName = null)
        {
            if (string.IsNullOrWhiteSpace (printerName) && !BusinessDomain.AppConfiguration.UseDefaultDocumentPrinter)
                printerName = BusinessDomain.AppConfiguration.DocumentPrinterName;

            // If the setting represents a valid printer then use it
            bool isValid = !string.IsNullOrWhiteSpace (printerName) &&
                BusinessDomain.AppConfiguration.GetAllInstalledPrinters ().Any (printer => printer == printerName);

            if (PrintSettings == null)
                PrintSettings = new PrintSettings ();

            try {
                var settings = new PrinterSettings ();
                if (isValid)
                    settings.PrinterName = printerName;

                PrintSettings.Printer = settings.PrinterName;
                previewPageWidth = settings.DefaultPageSettings.PaperSize.Width;
                previewPageHeight = settings.DefaultPageSettings.PaperSize.Height;
                previewDpiX = Math.Min (settings.DefaultPageSettings.PrinterResolution.X, 300);
                if (previewDpiX == 0)
                    previewDpiX = 300;
                previewDpiY = Math.Min (settings.DefaultPageSettings.PrinterResolution.Y, 300);
                if (previewDpiY == 0)
                    previewDpiY = previewDpiX;
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                previewPageWidth = 827;
                previewPageHeight = 1169;
                previewDpiX = 300;
                previewDpiY = 300;
            }
        }

        public void SetDefaultMargins (double top, double bottom, double left, double right, Unit unit = Unit.Pixel)
        {
            DefaultPageSetup.SetTopMargin (unit == Unit.Pixel ? top / SD_SCALE : top, unit);
            DefaultPageSetup.SetBottomMargin (unit == Unit.Pixel ? bottom / SD_SCALE : bottom, unit);
            DefaultPageSetup.SetLeftMargin (unit == Unit.Pixel ? left / SD_SCALE : left, unit);
            DefaultPageSetup.SetRightMargin (unit == Unit.Pixel ? right / SD_SCALE : right, unit);
        }

        private int previewPageWidth;
        private int previewPageHeight;
        private int previewDpiX;
        private int previewDpiY;

        protected override bool OnPreview (PrintOperationPreview preview, PrintContext context, Window parent)
        {
            previewSurface = new ImageSurface (Format.ARGB32, previewPageWidth, previewPageHeight);
            context.SetCairoContext (new Context (previewSurface), previewDpiX, previewDpiY);
            ErrorHandling.LogError (string.Format ("Starting preview with page size: {0}x{1}, Dpi: {2}x{3}, Orientation: {4}",
                previewPageWidth,
                previewPageHeight,
                previewDpiX,
                previewDpiY,
                context.PageSetup.Orientation), ErrorSeverity.Information);

            return true;
        }

        protected override void OnReady (PrintContext context)
        {
            base.OnReady (context);

            for (int i = 0; i < totalPages; i++) {
                if (cancelled)
                    break;

                RenderPage (i);
            }

            EndPreview ();

            if (previewSurface != null) {
                previewSurface.Destroy ();
                previewSurface = null;
            }
        }

        protected override void OnBeginPrint (PrintContext context)
        {
            cancelled = false;
            Cleanup ();

            if (context == null)
                throw new NullReferenceException ("context is null");

            if (context.PageSetup == null)
                throw new NullReferenceException ("context.PageSetup is null");

            if (formToPrint == null)
                throw new NullReferenceException ("formToPrint is null");

            if (formToPrint.PageMargins == null)
                throw new NullReferenceException ("formToPrint.PageMargins is null");

            using (var dp = new DrawingProviderCairo (context.CairoContext, context.CreatePangoLayout ())) {
                drawingScaleX = (float) (context.Width / (context.PageSetup.GetPageWidth (Unit.Pixel) * SD_SCALE));
                drawingScaleY = (float) (context.Height / (context.PageSetup.GetPageHeight (Unit.Pixel) * SD_SCALE));
                drawingScaleIsValid = true;
                dp.DrawingScaleX = drawingScaleX;
                dp.DrawingScaleY = drawingScaleY;
                formToPrint.DrawingProvider = dp;

                if (formToPrint.PageMargins.Top == int.MaxValue) {
                    formToPrint.PageMargins.Top = UseFullPage ? 0 : (int) Math.Round (context.PageSetup.GetTopMargin (Unit.Pixel) * SD_SCALE);
                    formToPrint.PageMargins.Bottom = UseFullPage ? 0 : (int) Math.Round (context.PageSetup.GetBottomMargin (Unit.Pixel) * SD_SCALE);
                    formToPrint.PageMargins.Left = UseFullPage ? 0 : (int) Math.Round (context.PageSetup.GetLeftMargin (Unit.Pixel) * SD_SCALE);
                    formToPrint.PageMargins.Right = UseFullPage ? 0 : (int) Math.Round (context.PageSetup.GetRightMargin (Unit.Pixel) * SD_SCALE);
                }

                if (formToPrint.PageWidth <= 0)
                    formToPrint.PageWidth = pageWidth != null ?
                        pageWidth.Value :
                        (int) Math.Round (context.PageSetup.GetPageWidth (Unit.Pixel) * SD_SCALE);

                if (formToPrint.PageHeight <= 0)
                    formToPrint.PageHeight = (int) Math.Round (context.PageSetup.GetPageHeight (Unit.Pixel) * SD_SCALE);

                formArea.X = (int) (formToPrint.PageMargins.Left * drawingScaleX);
                formArea.Y = (int) (formToPrint.PageMargins.Top * drawingScaleY);
                formArea.Width = (int) (formToPrint.PageWidth * drawingScaleX);
                formArea.Height = (int) (formToPrint.PageHeight * drawingScaleY);
                paperWidth = formArea.Width + (int) ((formToPrint.PageMargins.Left + formToPrint.PageMargins.Right) * drawingScaleX);
                paperHeight = formArea.Height + (int) ((formToPrint.PageMargins.Top + formToPrint.PageMargins.Bottom) * drawingScaleY);
                ErrorHandling.LogError (string.Format ("OnBeginPrint Paper: {0}x{1}, Form Area: {2}x{3}, Form Size: {4}x{5}, Orientation: {6}",
                    paperWidth,
                    paperHeight,
                    formArea.Width,
                    formArea.Height,
                    formToPrint.PageWidth,
                    formToPrint.PageHeight,
                    context.PageSetup.Orientation), ErrorSeverity.Information);

                totalPages = formToPrint.GetTotalPages ();
                pageColumns = formToPrint.GetPageColumns ();
                pageRows = formToPrint.GetPageRows ();

                NPages = totalPages;
                formToPrint.DrawingProvider = null;
            }

            base.OnBeginPrint (context);
        }

        protected override void OnDrawPage (PrintContext context, int page_nr)
        {
            Context cairoContext;
            if (previewSurface != null) {
                var file = System.IO.Path.GetTempFileName ();
                var target = new SvgSurface (file,
                    formArea.Width,
                    formArea.Height);
                cairoContext = new Context (target);
                pageFiles.Add (file);
            } else
                cairoContext = context.CairoContext;

            pageContexts.Add (cairoContext);

            using (var dp = new DrawingProviderCairo (cairoContext, context.CreatePangoLayout ())) {
                dp.DrawingScaleX = drawingScaleX;
                dp.DrawingScaleY = drawingScaleY;
                formToPrint.DrawingProvider = dp;

                formToPrint.Draw (page_nr, new PointD ());
            }
        }

        public override void Dispose ()
        {
            Cleanup ();

            base.Dispose ();
        }

        ~GtkFormPrintDocument ()
        {
            Cleanup ();
        }

        private void Cleanup ()
        {
            foreach (var ctx in pageContexts)
                ctx.DisposeAll ();

            pageContexts.Clear ();

            foreach (var file in pageFiles)
                try {
                    File.Delete (file);
                } catch (Exception) {
                }

            pageFiles.Clear ();
        }

        public void SetCustomPaperSize (double width, double height, Unit unit)
        {
            PaperSize size = new PaperSize ("custom_barcode_827x1169mm");
            size.SetSize (width, height, unit);

            DefaultPageSetup.PaperSize = size;
            if (PrintSettings != null)
                PrintSettings.PaperSize = size;
        }
    }
}
