//
// PrintPreview.cs
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
using System.Linq;
using Cairo;
using Gdk;
using GLib;
using Gtk;
using Warehouse.Component.Documenting;
using Warehouse.Data;
using Alignment = Pango.Alignment;
using Point = Cairo.Point;
using VBox = Gtk.VBox;
using WrapMode = Pango.WrapMode;

namespace Warehouse.Component.Printing
{
    public class PrintPreview : VBox
    {
        public event EventHandler PagesCalculated;

        #region Private fields

        protected readonly ScrolledWindow scrollWindow;
        private readonly Layout drawingArea;
        private bool autoZoom = true;
        private bool antiAlias;
        private int rows = 1;
        private int columns = 1;
        private bool sizeIsInvalid;
        private bool virtualSizeIsInvalid;
        private int startPage;
        private int totalPages;
        private double zoom = 0.3;
        private int pageScrollGap;
        private Point drawOffset;
        private Size drawWindowSize;
        private SizeI imageSize;
        private readonly Adjustment myVAdjustment;
        private GtkFormPrintDocument document;
        private readonly ITranslationProvider translator;
        private EventHandler startPageChanged;
        private EventHandler totalPagesChanged;
        private EventHandler zoomChanged;
        private EventHandler pagesPerSheetChanged;
        private bool pageInfoCalcPending;
        private Exception exceptionPrinting;
        protected RectangleI [] visiblePages;
        protected const int HORIZONTAL_GAP = 10;
        protected const int VERTICAL_GAP = 10;
        protected readonly Dictionary<int, Surface> pageCache = new Dictionary<int, Surface> ();

        #endregion

        #region Public properties

        public bool AutoZoom
        {
            get { return autoZoom; }
            set
            {
                autoZoom = value;
                QueueRefreshLayout ();
            }
        }

        public bool UseAntiAlias
        {
            get { return antiAlias; }
            set { antiAlias = value; }
        }

        public int Rows
        {
            get { return rows; }
        }

        public int Columns
        {
            get { return columns; }
        }

        public int StartPage
        {
            get
            {
                int temp = startPage;
                if (document.PageContexts.Count > 0)
                    temp = Math.Min (temp, TotalPages - 1 /*(rows * columns)*/);

                return Math.Max (temp, 0);
            }
            set
            {
                if (startPage == value)
                    return;

                int oldValue = startPage;
                startPage = value;
                ClearPagesCache (oldValue);
                QueueRefreshLayout ();
                OnStartPageChanged (EventArgs.Empty);
            }
        }

        public int TotalPages
        {
            get
            {
                return totalPages;
            }
            set
            {
                if (totalPages != value) {
                    totalPages = value;
                    OnTotalPagesChanged (EventArgs.Empty);
                }
            }
        }

        public double Zoom
        {
            get { return zoom; }
            set
            {
                if (value <= 0) {
                    throw new ArgumentException ("zoom");
                }
                autoZoom = false;
                if (zoom == value)
                    return;

                zoom = value;
                ClearPagesCache ();
                QueueRefreshLayout ();
            }
        }

        protected double FormZoomX
        {
            get { return zoom * document.DrawingScaleX; }
        }

        protected double FormZoomY
        {
            get { return zoom * document.DrawingScaleY; }
        }

        private Size ControlSize
        {
            get
            {
                return drawingArea.Allocation.Size;
            }
        }

        private Size VirtualSize
        {
            get
            {
                return drawWindowSize;
            }
            set
            {
                drawWindowSize = value;
                drawingArea.Height = (uint) value.Height;
                drawingArea.Width = (uint) value.Width;

                ComputeAdjustments ();
            }
        }

        public virtual GtkFormPrintDocument Document
        {
            get { return document; }
            set
            {
                if (document != value) {
                    if (document != null)
                        document.Dispose ();
                    document = value;
                    QueueRefreshAll ();
                }
            }
        }

        public bool Portrait { get; set; }

        public bool IsDocumentDrawn { get { return visiblePages != null; } }

        #endregion

        #region Public events

        public event EventHandler StartPageChanged
        {
            add { startPageChanged += value; }
            remove { startPageChanged -= value; }
        }

        public event EventHandler TotalPagesChanged
        {
            add { totalPagesChanged += value; }
            remove { totalPagesChanged -= value; }
        }

        public event EventHandler ZoomChanged
        {
            add { zoomChanged += value; }
            remove { zoomChanged -= value; }
        }

        public event EventHandler PagesPerSheetChanged
        {
            add { pagesPerSheetChanged += value; }
            remove { pagesPerSheetChanged -= value; }
        }

        #endregion

        public PrintPreview (ITranslationProvider translator)
        {
            scrollWindow = new ScrolledWindow ();
            Add (scrollWindow);

            myVAdjustment = new Adjustment (0f, 0f, 1f, 0f, 0f, 0f);
            drawingArea = new PrintLayout (scrollWindow.Hadjustment, myVAdjustment);
            drawingArea.ExposeEvent += OnDrawingAreaExposeEvent;
            scrollWindow.Add (drawingArea);
            drawingArea.Vadjustment = myVAdjustment;

            scrollWindow.VscrollbarPolicy = PolicyType.Automatic;
            scrollWindow.HscrollbarPolicy = PolicyType.Automatic;
            scrollWindow.Vadjustment.ValueChanged += Vadjustment_Changed;
            this.translator = translator;
        }

        public void Initialize ()
        {
            ComputePagesInfo ();
            SizeAllocated += (o, args) => ComputeVirtualSize ();
            ComputeVirtualSize ();
            QueueDraw ();
        }

        public void SetSize (int c, int r)
        {
            if (columns == c && rows == r && !sizeIsInvalid)
                return;

            columns = c;
            rows = r;
            sizeIsInvalid = false;
            OnPagesPerSheetChanged (EventArgs.Empty);
            QueueRefreshLayout ();
        }

        private void ComputePagesInfo ()
        {
            if (pageInfoCalcPending)
                return;

            pageInfoCalcPending = true;

            if (document.PageContexts.Count == 0)
                PrintDocument ();

            pageInfoCalcPending = false;
        }

        private bool printingDocument;
        public void PrintDocument ()
        {
            if (printingDocument)
                return;

            try {
                printingDocument = true;
                exceptionPrinting = null;
                if (document != null) {
                    var newDocument = new GtkFormPrintDocument (document.FormToPrint) { UseFullPage = document.UseFullPage };
                    newDocument.DefaultPageSetup = document.DefaultPageSetup.Copy ();
                    newDocument.DefaultPageSetup.Orientation = document.FormToPrint.Landscape
                        ? PageOrientation.Landscape
                        : PageOrientation.Portrait;
                    if (document.PrintSettings != null) {
                        newDocument.PrintSettings = document.PrintSettings.Copy ();
                        newDocument.PrintSettings.Orientation = newDocument.DefaultPageSetup.Orientation;
                    }
                    string printerName = null;
                    if (document.PrintSettings != null && !string.IsNullOrWhiteSpace (document.PrintSettings.Printer))
                        printerName = document.PrintSettings.Printer;

                    newDocument.SetPrinterSettings (printerName);

                    ClearPagesCache ();

                    document = newDocument;
                    sizeIsInvalid = true;
                    virtualSizeIsInvalid = true;

                    document.DrawPage += document_DrawPage;
                    using (new PrintPreviewController (document))
                        document.Run (PrintOperationAction.Preview, ComponentHelper.TopWindow);
                    document.DrawPage -= document_DrawPage;

                    document.PageRows = document.FormToPrint.GetPageRows ();
                    document.PageColumns = document.FormToPrint.GetPageColumns ();
                    StartPage = Math.Min (startPage, TotalPages - 1);
                    OnPagesCalculated (EventArgs.Empty);
                }
                QueueDraw ();
            } catch (GException ex) {
                exceptionPrinting = ex;
            } catch (AccessViolationException ex) {
                exceptionPrinting = ex;
            } finally {
                printingDocument = false;
            }
        }

        private void ClearPagesCache (int? oldStartPage = null)
        {
            if (oldStartPage == null) {
                int [] allPages = pageCache.Keys.ToArray ();
                foreach (var page in allPages) {
                    Surface cache;
                    if (!pageCache.TryGetValue (page, out cache))
                        continue;

                    cache.DisposeSurface ();
                    pageCache.Remove (page);
                }
            } else if (oldStartPage.Value < startPage) {
                for (int i = startPage - 1; i >= oldStartPage.Value; i--) {
                    Surface cache;
                    if (!pageCache.TryGetValue (i, out cache))
                        continue;

                    cache.DisposeSurface ();
                    pageCache.Remove (i);
                }
            } else if (startPage < oldStartPage.Value) {
                int count = columns * rows;
                for (int i = count + oldStartPage.Value; i > count + startPage; i--) {
                    Surface cache;
                    if (!pageCache.TryGetValue (i, out cache))
                        continue;

                    cache.DisposeSurface ();
                    pageCache.Remove (i);
                }
            }
        }

        private void document_DrawPage (object sender, DrawPageArgs args)
        {
            TotalPages = document.TotalPages;

            ComputeAdjustments ();
            QueueDraw ();
            OnPagesCalculated (EventArgs.Empty);
        }

        private void ComputeVirtualSize ()
        {
            if (TotalPages == 0) {
                VirtualSize = ControlSize;
            } else {
                Gdk.Size visibleSize = ControlSize;
                // Invalid values may cause invalid values for zoom and lead to flickering and useless computations
                if (document.FormArea.Width <= 0 || document.FormArea.Height <= 0 || visibleSize.Width <= 1 || visibleSize.Height <= 1)
                    return;

                SizeI pageSize = new SizeI (document.PaperWidth, document.PaperHeight);

                if (autoZoom) {
                    // Adjust the zoom ratio in respect to the number of columns and rows
                    double widthRatio = (visibleSize.Width - (HORIZONTAL_GAP * (columns + 1))) / ((double) (columns * pageSize.Width));
                    double heightRatio = (visibleSize.Height - (VERTICAL_GAP * (rows + 1))) / ((double) (rows * pageSize.Height));
                    double newZoom = Math.Min (widthRatio, heightRatio);
                    if (newZoom != zoom) {
                        zoom = newZoom;
                        ClearPagesCache ();
                        OnZoomChanged (EventArgs.Empty);
                    }
                } else {
                    // Adjust the number of columns and rows in respect to the zoom ratio
                    columns = 1;
                    int i;
                    for (i = 1; i < 5; i++) {
                        double calcWidth = i * pageSize.Width * zoom + 10 * (i + 1);
                        if (calcWidth > visibleSize.Width)
                            break;

                        columns = Math.Min (i, document.PageColumns);
                    }

                    rows = 1;
                    for (i = 1; i < 5; i++) {
                        double calcHeight = i * pageSize.Height * zoom + 10 * (i + 1);
                        if (calcHeight > visibleSize.Height)
                            break;

                        rows = Math.Min (i, document.PageRows);
                    }
                    OnPagesPerSheetChanged (EventArgs.Empty);
                }

                imageSize = new SizeI ((int) (zoom * pageSize.Width), (int) (zoom * pageSize.Height));
                int virtualWidth = (imageSize.Width * columns) + HORIZONTAL_GAP * (columns + 1);
                int virtualHeight = (imageSize.Height * rows) + VERTICAL_GAP * (rows + 1);

                if (visibleSize.Width > virtualWidth) {
                    drawOffset.X = (visibleSize.Width - virtualWidth) / 2;
                    virtualWidth = visibleSize.Width;
                } else {
                    drawOffset.X = 0;
                }

                if (visibleSize.Height > virtualHeight) {
                    drawOffset.Y = (visibleSize.Height - virtualHeight) / 2;
                    virtualHeight = visibleSize.Height;
                } else {
                    drawOffset.Y = 0;
                }

                VirtualSize = new Size (virtualWidth, virtualHeight);
            }

            virtualSizeIsInvalid = false;
        }

        private void ComputeAdjustments ()
        {
            int sheets = 1;

            if (document.PageContexts.Count > 0)
                sheets = (int) Math.Ceiling (TotalPages / ((double) (columns * rows)));

            float horizontalUpperBound = VirtualSize.Width;
            if (scrollWindow.VScrollbar.Visible && horizontalUpperBound > 0)
                horizontalUpperBound -= scrollWindow.VScrollbar.Allocation.Width;

            float verticalUpperBound = (float) VirtualSize.Height / rows * (float) Math.Ceiling (TotalPages / (float) columns);
            if (scrollWindow.HScrollbar.Visible && verticalUpperBound > 0)
                verticalUpperBound -= scrollWindow.HScrollbar.Allocation.Height;

            // Add free movement space before switching pages
            if (sheets > 1) {
                pageScrollGap = Math.Max (VirtualSize.Height * 5 / 100, 5);
                verticalUpperBound += 2 * pageScrollGap * sheets;
            }

            if (Math.Abs (scrollWindow.Hadjustment.Upper - horizontalUpperBound) > scrollWindow.VScrollbar.Allocation.Width)
                if (horizontalUpperBound <= ControlSize.Width)
                    scrollWindow.Hadjustment.SetBounds (0d, horizontalUpperBound, 1d, 1d, ControlSize.Width);
                else
                    scrollWindow.Hadjustment.SetBounds (0d, horizontalUpperBound, 1d, 1d, ControlSize.Width / (float) columns);

            if (Math.Abs (scrollWindow.Vadjustment.Upper - verticalUpperBound) > scrollWindow.HScrollbar.Allocation.Height)
                if (verticalUpperBound <= ControlSize.Height)
                    scrollWindow.Vadjustment.SetBounds (0d, verticalUpperBound, 1d, 1d, ControlSize.Height);
                else
                    scrollWindow.Vadjustment.SetBounds (0d, verticalUpperBound, 1d, 1d, ControlSize.Height / (float) rows);
        }

        private void Vadjustment_Changed (object sender, EventArgs e)
        {
            int sheets = 1;

            if (document.PageContexts.Count > 0)
                sheets = (int) Math.Ceiling (TotalPages / ((double) (columns * rows)));

            if (sheets > 1) {
                int sheetHeight = VirtualSize.Height / rows;
                int currentSheet = ((int) scrollWindow.Vadjustment.Value - 1) / (sheetHeight + 2 * pageScrollGap);
                StartPage = columns * currentSheet;

                double value = scrollWindow.Vadjustment.Value % (sheetHeight + 2 * pageScrollGap);
                value = Math.Max (value - pageScrollGap, 0d);
                value = Math.Min (value, sheetHeight);

                myVAdjustment.Upper = sheetHeight;
                myVAdjustment.Value = value;
            } else {
                myVAdjustment.Upper = scrollWindow.Vadjustment.Upper;
                myVAdjustment.Value = scrollWindow.Vadjustment.Value;
            }

            myVAdjustment.Lower = scrollWindow.Vadjustment.Lower;
            myVAdjustment.StepIncrement = scrollWindow.Vadjustment.StepIncrement;
            myVAdjustment.PageSize = scrollWindow.Vadjustment.PageSize;
            myVAdjustment.PageIncrement = scrollWindow.Vadjustment.PageIncrement;
            myVAdjustment.Change ();
            myVAdjustment.ChangeValue ();
        }

        protected virtual void OnDrawingAreaExposeEvent (object o, ExposeEventArgs args)
        {
            DrawDocument (args.Event.Window);
        }

        private void DrawDocument (Drawable drawable)
        {
            using (Context context = CairoHelper.Create (drawable))
                DrawDocument (context, GetPrintingError ());
        }

        private void DrawDocument (Context context, string error)
        {
            if (document.PageContexts.Count == 0)
                return;

            if (virtualSizeIsInvalid)
                ComputeVirtualSize ();

            PreparePlot (context);

            if (TotalPages == 0) {
                if (document.PageContexts.Count > 0 || exceptionPrinting != null)
                    ShowError (context, error);
            } else {
                visiblePages = new RectangleI [rows * columns];

                for (int i = 0; i < rows; i++) {
                    for (int j = 0; j < columns; j++) {
                        int pageNumber = (StartPage + j) + (i * columns);

                        if (pageNumber >= TotalPages)
                            break;

                        int pageXOffset = drawOffset.X + (HORIZONTAL_GAP * (j + 1)) + (imageSize.Width * j);
                        int pageYOffset = drawOffset.Y + (VERTICAL_GAP * (i + 1)) + (imageSize.Height * i);
                        visiblePages [pageNumber - StartPage] =
                            new RectangleI (pageXOffset, pageYOffset, imageSize.Width, imageSize.Height);
                    }
                }

                for (int i = 0; i < visiblePages.Length && (i + StartPage) < TotalPages; i++) {
                    RectangleI currentPage = visiblePages [i];

                    context.LineWidth = 2;
                    context.Color = new Cairo.Color (0, 0, 0);
                    context.Rectangle (currentPage.X + 2, currentPage.Y + 2, currentPage.Width, currentPage.Height);
                    context.Stroke ();

                    context.Color = new Cairo.Color (10 / 255d, 36 / 255d, 106 / 255d);
                    context.Rectangle (currentPage.X, currentPage.Y, currentPage.Width, currentPage.Height);
                    context.Stroke ();

                    currentPage.Inflate (-1, -1);
                    context.Color = Style.Foreground (StateType.Normal).ToCairoColor ();
                    context.Rectangle (currentPage.X, currentPage.Y, currentPage.Width, currentPage.Height);
                    context.Fill ();

                    DrawPrinterPreview (context, i, currentPage);
                }
            }
        }

        private void PreparePlot (Context context)
        {
            context.Rectangle (0, 0, VirtualSize.Width, VirtualSize.Height);
            context.Color = Style.Background (StateType.Normal).ToCairoColor ();
            context.Fill ();
        }

        private void ShowError (Context context, string error)
        {
            PreparePlot (context);

            Pango.Layout layout = context.CreateLayout ();
            layout.SetText (error);
            layout.Alignment = Alignment.Center;
            layout.Width = (int) (VirtualSize.Width * Pango.Scale.PangoScale);
            layout.Wrap = WrapMode.Word;
            int text_width;
            int text_height;
            layout.GetPixelSize (out text_width, out text_height);
            context.MoveTo (0, (VirtualSize.Height - text_height) / 2);
            context.Color = new Cairo.Color (0, 0, 0);
            context.ShowLayout (layout);
        }

        private string GetPrintingError ()
        {
            string message;
            if (exceptionPrinting != null) {
                if (exceptionPrinting.Message.IndexOf ("No printers", StringComparison.OrdinalIgnoreCase) >= 0)
                    message = string.Format (translator.GetString ("The print preview could not be created " +
                        "because there are no printers installed.{0}" +
                        "Please install at least a virtual (PDF) printer " +
                        "in order to be able to use the preview."),
                        Environment.NewLine);
                else
                    message = translator.GetString ("An error occurred while generating document.");
            } else
                message = translator.GetString ("The document contains no pages.");
            return message;
        }

        private void DrawPrinterPreview (Context context, int i, RectangleI currentPage)
        {
            if (i + StartPage >= document.PageContexts.Count)
                return;

            var page = document.PageContexts [i + StartPage];
            int left = document.FormArea.X;
            int top = document.FormArea.Y;

            Surface cache;
            if (!pageCache.TryGetValue (i + startPage, out cache)) {
                cache = zoom < 0.5 ?
                    page.Target.ScaleSmooth (document.FormArea.Width, document.FormArea.Height, zoom, zoom) :
                    page.Target.ScaleFast (document.FormArea.Width, document.FormArea.Height, zoom, zoom);

                pageCache [i + startPage] = cache;
            }

            context.SetSourceSurface (cache,
                (int) Math.Round (currentPage.X + left * zoom),
                (int) Math.Round (currentPage.Y + top * zoom));
            context.Paint ();
        }

        private void OnStartPageChanged (EventArgs eventArgs)
        {
            if (startPageChanged != null)
                startPageChanged (this, eventArgs);
        }

        private void OnTotalPagesChanged (EventArgs eventArgs)
        {
            if (totalPagesChanged != null)
                totalPagesChanged (this, eventArgs);
        }

        private void OnZoomChanged (EventArgs eventArgs)
        {
            if (zoomChanged != null)
                zoomChanged (this, eventArgs);
        }

        private void OnPagesPerSheetChanged (EventArgs eventArgs)
        {
            if (pagesPerSheetChanged != null)
                pagesPerSheetChanged (this, eventArgs);
        }

        public void Refresh ()
        {
            QueueRefreshLayout ();
        }

        private void QueueRefreshAll ()
        {
            QueueRefreshLayout ();
        }

        private void QueueRefreshLayout ()
        {
            ComputeVirtualSize ();
            QueueDraw ();
        }

        private void OnPagesCalculated (EventArgs e)
        {
            EventHandler handler = PagesCalculated;
            if (handler != null)
                handler (this, e);
        }
    }
}