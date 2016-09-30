//
// WbpPrintPreview.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   11/08/2006
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
using Gdk;
using Glade;
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Business.Documenting;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Component.Documenting;
using Warehouse.Component.Printing;
using Warehouse.Component.WorkBook;
using Warehouse.Data;
using Warehouse.Presentation.Dialogs;
using Image = Gtk.Image;
using Key = Gdk.Key;
using Timeout = GLib.Timeout;
using VBox = Gtk.VBox;

namespace Warehouse.Presentation
{
    public class WbpPrintPreview : WbpBase
    {
        protected readonly IDocument document;
        protected PrintPreview currentPreview;
        protected PrintPreview previewControl;
        private SizeChooser tcPages;
        private bool changingStartPage;
        private bool changingSheetSize;
        private const int maxAutoColumns = 3;
        private const int maxAutoRows = 2;
        private static readonly IDocumentDesigner documentDesigner;
        protected bool portrait = true;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        protected Gtk.HBox hboPrintPreviewRoot;
        [Widget]
        protected Alignment algPrintPreview;
        [Widget]
        protected VBox vboxControls;
        [Widget]
        protected Button btnSave;
        [Widget]
        protected Button btnClose;
        [Widget]
        protected Button btnPrint;
        [Widget]
        protected Button btnExport;
        [Widget]
        protected Label lblZoom;
        [Widget]
        protected ComboBox cboZoom;
        [Widget]
        protected Label lblPaging;
        [Widget]
        protected Alignment algPages;
        [Widget]
        protected Label lblPage;
        [Widget]
        protected SpinButton spbPage;
        [Widget]
        protected Button btnDocumentDesigner;
        [Widget]
        private ToggleButton tbtnPortrait;
        [Widget]
        private ToggleButton tbtnLandscape;
        [Widget]
        protected Alignment algPrintPreviewIcon;

#pragma warning restore 649

        #endregion

        #region Public properties

        public Form DocumentForm
        {
            get { return PrintDocument.FormToPrint; }
        }

        public GtkFormPrintDocument PrintDocument
        {
            get { return currentPreview.Document; }
        }

        public virtual string Printer
        {
            get { return string.Empty; }
        }

        public virtual string HelpFile
        {
            get { return "PrintPreview.html"; }
        }

        #endregion

        static WbpPrintPreview ()
        {
            foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Presentation/DocumentDesigner")) {
                documentDesigner = (IDocumentDesigner) node.CreateInstance ();
                break;
            }
        }

        public WbpPrintPreview ()
        {
            InitializeForm ();
        }

        public WbpPrintPreview (IDocument document)
        {
            this.document = document;

            InitializeForm ();
        }

        protected virtual void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("WbpPrintPreview.glade", "hboPrintPreviewRoot");
            form.Autoconnect (this);

            btnSave.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnClose.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            btnPrint.SetChildImage (FormHelper.LoadImage ("Icons.Print24.png"));
            tcPages = new SizeChooser (maxAutoColumns, maxAutoRows,
                DataHelper.ResourcesAssembly,
                FormHelper.GetResourceName ("Icons.Page24.png"));
            tcPages.SizeChanged += tcPages_SizeChanged;
            algPages.Add (tcPages);

            btnDocumentDesigner.SetChildImage (FormHelper.LoadImage ("Icons.DesignDoc24.png"));
            btnExport.SetChildImage (FormHelper.LoadImage ("Icons.Export24.png"));

            Image icon = FormHelper.LoadImage ("Icons.Report32.png");
            algPrintPreviewIcon.Add (icon);
            icon.Show ();

            Add (hboPrintPreviewRoot);
            hboPrintPreviewRoot.KeyPressEvent += WbpPrintPreview_KeyPressEvent;
            OuterKeyPressed += WbpPrintPreview_KeyPressEvent;

            cboZoom.Changed += cboZoom_Changed;
            spbPage.ValueChanged += spbPage_Changed;
            spbPage.Adjustment.Lower = 1d;

            CreatePreview ();
            algPrintPreview.Add (currentPreview);

            hboPrintPreviewRoot.ShowAll ();

            btnPrint.Sensitive = BusinessDomain.AppConfiguration.IsPrinterAvailable (Printer);
            btnExport.Visible = BusinessDomain.DocumentExporters.Count > 0;
            btnSave.Visible = false;
            btnDocumentDesigner.Visible = documentDesigner != null;

            tbtnPortrait.Toggled -= tbtnPortrait_Toggled;
            tbtnLandscape.Active = BusinessDomain.AppConfiguration.IsPrinterAvailable (Printer) && PrintDocument.FormToPrint.Landscape;

            tbtnPortrait.Active = !tbtnLandscape.Active;
            tbtnPortrait.Toggled += tbtnPortrait_Toggled;

            tbtnPortrait.Image = FormHelper.LoadImage ("Icons.Portrait.png");
            tbtnLandscape.Image = FormHelper.LoadImage ("Icons.Landscape.png");

            InitializeStrings ();
        }

        protected void CreatePreview ()
        {
            if (previewControl == null)
                CreatePreviewControl ();

            if (currentPreview.Document != null)
                return;

            currentPreview.Document = GetPrintDocument ();
            portrait = !BusinessDomain.AppConfiguration.IsPrinterAvailable (Printer) || !PrintDocument.FormToPrint.Landscape;
            currentPreview.Portrait = portrait;
            currentPreview.ModifyBg (StateType.Normal, new Color (220, 220, 220));
            currentPreview.ModifyFg (StateType.Normal, new Color (255, 255, 255));
            currentPreview.StartPageChanged += CurrentPreview_StartPageChanged;
            currentPreview.TotalPagesChanged += CurrentPreview_TotalPagesChanged;
            currentPreview.PagesPerSheetChanged += CurrentPreview_PagesPerSheetChanged;
            currentPreview.PagesCalculated += CurrentPreview_PagesCalculated;
        }

        protected virtual GtkFormPrintDocument GetPrintDocument ()
        {
            return new GtkFormPrintDocument (FormHelper.LoadForm (document));
        }

        protected virtual void CreatePreviewControl ()
        {
            previewControl = new PrintPreview (Translator.GetHelper ());
            currentPreview = previewControl;
        }

        private void WbpPrintPreview_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.HelpKey) && !string.IsNullOrEmpty (HelpFile)) {
                FormHelper.ShowWindowHelp (HelpFile);
                return;
            }

            switch (args.Event.Key) {
                case Key.Escape:
                    RequestClose ();
                    return;

                case Key.F9:
                    btnPrint_Clicked (null, EventArgs.Empty);
                    return;

                case Key.Left:
                    currentPreview.StartPage--;
                    return;

                case Key.Right:
                    currentPreview.StartPage++;
                    return;

                case Key.End:
                    if (KeyShortcuts.GetAllowedModifier (args.Event.State) == KeyShortcuts.ControlModifier)
                        currentPreview.StartPage = currentPreview.TotalPages - 1;

                    return;

                case Key.Home:
                    if (KeyShortcuts.GetAllowedModifier (args.Event.State) == KeyShortcuts.ControlModifier)
                        currentPreview.StartPage = 0;

                    return;
            }
        }

        public override void OnPageAdded ()
        {
            base.OnPageAdded ();

            currentPreview.Initialize ();
        }

        public override void OnViewModeChanged ()
        {
            base.OnViewModeChanged ();

            currentPreview.Refresh ();
        }

        protected void InitializeStrings ()
        {
            btnSave.SetChildLabelText (Translator.GetString ("Save"));
            btnClose.SetChildLabelText (Translator.GetString ("Close"));
            btnPrint.SetChildLabelText (Translator.GetString ("Print"));
            lblZoom.SetText (Translator.GetString ("Zoom:"));
            lblPaging.SetText (Translator.GetString ("Pages shown:"));
            lblPage.SetText (Translator.GetString ("Page:"));

            KeyValuePair<float, string> [] values =
                {
                    new KeyValuePair<float, string> (-1f, Translator.GetString ("Auto")),
                    new KeyValuePair<float, string> (5f, "500%"),
                    new KeyValuePair<float, string> (2f, "200%"),
                    new KeyValuePair<float, string> (1.5f, "150%"),
                    new KeyValuePair<float, string> (1f, "100%"),
                    new KeyValuePair<float, string> (0.75f, "75%"),
                    new KeyValuePair<float, string> (0.5f, "50%"),
                    new KeyValuePair<float, string> (0.25f, "25%"),
                    new KeyValuePair<float, string> (0.1f, "10%")
                };
            cboZoom.Load (values, "Key", "Value");

            btnDocumentDesigner.SetChildLabelText (Translator.GetString ("Design"));
            btnExport.SetChildLabelText (Translator.GetString ("Export"));
        }

        private void CurrentPreview_PagesCalculated (object sender, EventArgs e)
        {
            if (changingSheetSize)
                return;

            changingSheetSize = true;
            int cols = Math.Min (currentPreview.Document.PageColumns, maxAutoColumns);
            int rows = Math.Min (currentPreview.Document.PageRows, maxAutoRows);
            // achieve a more "symmetric" look
            if (cols == 1)
                rows = 1;
            SyncRowsColumns (cols, rows);
            changingSheetSize = false;

            // HACK: The ... GTK sends key events not to the current tab unless it's "focused"
            Timeout.Add (0, () =>
                {
                    (btnPrint.Sensitive ? btnPrint : btnClose).GrabFocus ();
                    return false;
                });
        }

        protected virtual void SyncRowsColumns (int cols, int rows)
        {
            //currentPreview.Columns = cols;
            //currentPreview.Rows = rows;
            currentPreview.SetSize (cols, rows);
            tcPages.Columns = cols;
            tcPages.Rows = rows;
        }

        protected void btnClose_Clicked (object o, EventArgs args)
        {
            RequestClose ();
        }

        protected virtual void btnPrint_Clicked (object sender, EventArgs e)
        {
            try {
                if (PrintDocument.PrintSettings != null)
                    PrintDocument.PrintSettings.Orientation = currentPreview.Portrait ? PageOrientation.Portrait : PageOrientation.Landscape;

                FormHelper.PrintObject (document, PrintDocument.PrintSettings);
            } catch (Exception ex) {
                MessageError.ShowDialog (
                    Translator.GetString ("An error occurred while printing document!"),
                    ErrorSeverity.Error, ex);
            }
        }

        [UsedImplicitly]
        protected virtual void btnExport_Clicked (object sender, EventArgs e)
        {
            FormHelper.ExportDocument (document, portrait);
        }

        private void cboZoom_Changed (object sender, EventArgs e)
        {
            float zoomFactor = (float) cboZoom.GetSelectedValue ();

            if (zoomFactor <= 0f)
                currentPreview.AutoZoom = true;
            else
                currentPreview.Zoom = zoomFactor;
        }

        private void tcPages_SizeChanged (object sender, EventArgs e)
        {
            if (changingSheetSize)
                return;

            changingSheetSize = true;
            //currentPreview.Columns = tcPages.Columns;
            //currentPreview.Rows = tcPages.Rows;
            currentPreview.SetSize (tcPages.Columns, tcPages.Rows);
            cboZoom.SetSelection (0);
            changingSheetSize = false;
        }

        protected void CurrentPreview_TotalPagesChanged (object sender, EventArgs e)
        {
            spbPage.Adjustment.Upper = currentPreview.TotalPages;
        }

        protected void CurrentPreview_StartPageChanged (object sender, EventArgs e)
        {
            if (changingStartPage)
                return;

            changingStartPage = true;
            spbPage.Value = (double) currentPreview.StartPage + 1;
            changingStartPage = false;
        }

        protected void CurrentPreview_PagesPerSheetChanged (object sender, EventArgs e)
        {
            if (changingSheetSize)
                return;

            changingSheetSize = true;
            tcPages.Columns = currentPreview.Columns;
            tcPages.Rows = currentPreview.Rows;
            changingSheetSize = false;
        }

        private void spbPage_Changed (object sender, EventArgs e)
        {
            if (changingStartPage)
                return;

            changingStartPage = true;
            spbPage.Update ();
            currentPreview.StartPage = spbPage.ValueAsInt - 1;
            changingStartPage = false;
        }

        [UsedImplicitly]
        protected virtual void btnDocumentDesigner_Clicked (object sender, EventArgs e)
        {
            PresentationDomain.MainForm.WorkBook.CurrentPageChanged += WorkBook_CurrentPageChanged;
            PresentationDomain.MainForm.AddNewPage (documentDesigner.GetPage (document));
        }

        [UsedImplicitly]
        private void tbtnPortrait_Toggled (object sender, EventArgs e)
        {
            portrait = tbtnPortrait.Active;
            currentPreview.Portrait = portrait;
            tbtnLandscape.Active = !portrait;
            PrintDocument.FormToPrint.Landscape = !portrait;
            PrintDocument.FormToPrint.PageWidth = -1;
            PrintDocument.FormToPrint.PageHeight = -1;
            PrintDocument.FormToPrint.QueueCalculateDown (true);
            PrintDocument.FormToPrint.QueueAllocateDown ();
            PrintDocument.FormToPrint.QueueRedistributeDown ();

            currentPreview.PrintDocument ();
        }

        [UsedImplicitly]
        private void tbtnLandscape_Toggled (object sender, EventArgs e)
        {
            tbtnPortrait.Active = !tbtnLandscape.Active;
        }

        private void WorkBook_CurrentPageChanged (object sender, CurrentPageChangedArgs e)
        {
            if (PresentationDomain.MainForm.WorkBook.CurrentPage != this)
                return;
            
            PresentationDomain.MainForm.WorkBook.CurrentPageChanged -= WorkBook_CurrentPageChanged;
            Form form = FormHelper.LoadForm (document);
            form.Landscape = PrintDocument.FormToPrint.Landscape;

            if (PrintDocument.FormToPrint.ToXmlString () == form.ToXmlString ())
                return;
            
            PrintDocument.FormToPrint = form;
            currentPreview.PrintDocument ();
        }

        protected PrintPreview CreatePrintPreview (GtkFormPrintDocument formPrintDocument, Container container)
        {
            PrintPreview printPreview = new PrintPreview (Translator.GetHelper ());
            printPreview.Document = formPrintDocument;
            printPreview.Portrait = portrait;
            printPreview.ModifyBg (StateType.Normal, new Color (220, 220, 220));
            printPreview.ModifyFg (StateType.Normal, new Color (255, 255, 255));
            printPreview.StartPageChanged += CurrentPreview_StartPageChanged;
            printPreview.TotalPagesChanged += CurrentPreview_TotalPagesChanged;
            printPreview.PagesPerSheetChanged += CurrentPreview_PagesPerSheetChanged;

            if (container.Children.Length > 0)
                container.Remove (container.Children [0]);

            container.Add (printPreview);
            printPreview.Initialize ();

            container.ShowAll ();

            return printPreview;
        }

        #region WorkBookPage Members

        public override ViewProfile ViewProfile
        {
            get
            {
                return ViewProfile.GetByName ("PrintPreview") ?? new ViewProfile
                    {
                        Name = "PrintPreview",
                        ShowToolbar = false,
                        ShowTabs = false,
                        ShowStatusBar = false
                    };
            }
        }

        protected override string PageDescription
        {
            get { return "Print Preview"; }
        }

        public override string PageTitle
        {
            get { return document.Name; }
        }

        #endregion
    }
}
