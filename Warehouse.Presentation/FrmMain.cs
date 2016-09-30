//
// FrmMain.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   03/21/2006
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
using System.ComponentModel;
using System.IO;
using Gdk;
using Glade;
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Business.Devices;
using Warehouse.Business.Documenting;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Business.Reporting;
using Warehouse.Component.WorkBook;
using Warehouse.Data;
using Warehouse.Presentation.Dialogs;
using Warehouse.Presentation.OS;
using Warehouse.Presentation.Widgets;
using Image = Gtk.Image;
using Key = Gdk.Key;
using Window = Gtk.Window;
using System.Linq;

namespace Warehouse.Presentation
{
    public class FrmMain
    {
        private readonly MenuItemCollection mainMenu;
        private readonly ToolItemCollection mainToolbar;
        private readonly WorkBook workBook;
        private readonly CustomStatusBar csbStatus;
        private bool settingProfile;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Window frmMain;

        [Widget]
        private MenuBar mbMain;
        [Widget]
        private Toolbar tbMain;
        [Widget]
        private Alignment algWorkspace;
        [Widget]
        private Alignment algStatus;

#pragma warning restore 649

        #endregion

        public static AccelGroup AccelGroup { get; private set; }

        public MenuItemCollection MainMenu
        {
            get { return mainMenu; }
        }

        public WorkBook WorkBook
        {
            get { return workBook; }
        }

        public Window Window
        {
            get { return frmMain; }
        }

        public event EventHandler RestrictionsReload;

        public FrmMain ()
        {
            XML gxml = FormHelper.LoadGladeXML ("FrmMain.glade", "frmMain");
            gxml.Autoconnect (this);

            frmMain.Icon = FormHelper.LoadImage ("Icons.AppMain32.png").Pixbuf;
            frmMain.Title = DataHelper.ProductName;

            mainMenu = new MenuItemCollection (mbMain);

            csbStatus = new CustomStatusBar ();
            csbStatus.Show ();
            algStatus.Add (csbStatus);

            SetMenuImages ();

            mainToolbar = new ToolItemCollection (tbMain);

            mainToolbar.FindToolItem ("btnQuit").Image = FormHelper.LoadImage ("Icons.Exit32.png");
            mainToolbar.FindToolItem ("btnSale").Image = FormHelper.LoadImage ("Icons.Sale32.png");
            mainToolbar.FindToolItem ("btnPurchase").Image = FormHelper.LoadImage ("Icons.Purchase32.png");
            mainToolbar.FindToolItem ("btnInvoice").Image = FormHelper.LoadImage ("Icons.Invoice32.png");
            mainToolbar.FindToolItem ("btnStockTaking").Image = FormHelper.LoadImage ("Icons.StockTaking32.png");
            mainToolbar.FindToolItem ("btnItems").Image = FormHelper.LoadImage ("Icons.Goods32.png");
            mainToolbar.FindToolItem ("btnPartners").Image = FormHelper.LoadImage ("Icons.Partner32.png");
            mainToolbar.FindToolItem ("btnSalesReport").Image = FormHelper.LoadImage ("Icons.SalesReport32.png");
            mainToolbar.FindToolItem ("btnPurchasesReport").Image = FormHelper.LoadImage ("Icons.PurchaseReport32.png");
            mainToolbar.FindToolItem ("btnItemsQtyReport").Image = FormHelper.LoadImage ("Icons.GoodsQtyReport32.png");
            mainToolbar.FindToolItem ("btnSalesByItemsReport").Image = FormHelper.LoadImage ("Icons.SalesByGoodsReport32.png");
            mainToolbar.FindToolItem ("btnPurchasesByItemsReport").Image = FormHelper.LoadImage ("Icons.PurchaseByGoodsReport32.png");
            mainToolbar.FindToolItem ("btnSalesByPartnerReport").Image = FormHelper.LoadImage ("Icons.SalesByPartnerReport32.png");
            mainToolbar.FindToolItem ("btnItemsProfitReport").Image = FormHelper.LoadImage ("Icons.GoodsProfitReport32.png");
            mainToolbar.FindToolItem ("btnSettings").Image = FormHelper.LoadImage ("Icons.Settings32.png");

            mainToolbar.FindToolItem ("btnQuit").RestrictionName = "mnuFileExit";
            mainToolbar.FindToolItem ("btnSale").RestrictionName = "mnuOperSales";
            mainToolbar.FindToolItem ("btnPurchase").RestrictionName = "mnuOperDeliveries";
            mainToolbar.FindToolItem ("btnInvoice").RestrictionName = "mnuOperInvoicePublish";
            mainToolbar.FindToolItem ("btnStockTaking").RestrictionName = "mnuEditAdminRevision";
            mainToolbar.FindToolItem ("btnItems").RestrictionName = "mnuEditGoods";
            mainToolbar.FindToolItem ("btnPartners").RestrictionName = "mnuEditPartners";
            mainToolbar.FindToolItem ("btnSalesReport").RestrictionName = "mnuRepSale";
            mainToolbar.FindToolItem ("btnPurchasesReport").RestrictionName = "mnuRepDelivery";
            mainToolbar.FindToolItem ("btnItemsQtyReport").RestrictionName = "mnuRepGoodsQtty";
            mainToolbar.FindToolItem ("btnSalesByItemsReport").RestrictionName = "mnuRepGoodsSale";
            mainToolbar.FindToolItem ("btnPurchasesByItemsReport").RestrictionName = "mnuRepGoodsDelivery";
            mainToolbar.FindToolItem ("btnSalesByPartnerReport").RestrictionName = "mnuRepPartnersSales";
            mainToolbar.FindToolItem ("btnItemsProfitReport").RestrictionName = "mnuRepGoodsBestProfit";
            mainToolbar.FindToolItem ("btnSettings").RestrictionName = "mnuToolsSetup";

            workBook = new WorkBook ();
            workBook.CurrentPageChanged += workBook_CurrentPageChanged;
            workBook.Show ();
            algWorkspace.Add (workBook);

            InitializeAddins ();
            InitializeSettings ();
            InitializeStrings ();

            frmMain.KeyPressEvent += frmMain_KeyPressEvent;
            frmMain.Shown += frmMain_Shown;

            frmMain.Maximize ();

            frmMain.AddAccelGroup (AccelGroup);
            RefreshMenuShortcuts ();

            BusinessDomain.ChangingDatabase += (sender, args) => csbStatus.Refresh (false);
        }

        private void SetMenuImages ()
        {
            mainMenu.FindMenuItem ("mnuEditAdminUserChange").Image = FormHelper.LoadImage ("Icons.User16.png");
            mainMenu.FindMenuItem ("mnuFileOpenBase").Image = FormHelper.LoadImage ("Icons.Database16.png");
            mainMenu.FindMenuItem ("mnuFileExit").Image = FormHelper.LoadImage ("Icons.Exit16.png");

            mainMenu.FindMenuItem ("mnuOperSales").Image = GetSaleImage ();
            mainMenu.FindMenuItem ("mnuOperDeliveries").Image = GetPurchaseImage ();
            mainMenu.FindMenuItem ("mnuOperProductionComplexRecipes").Image = FormHelper.LoadImage ("Icons.Recipe16.png");
            mainMenu.FindMenuItem ("mnuOperProductionComplexProducing").Image = GetProductionImage ();
            mainMenu.FindMenuItem ("mnuOperTransfer").Image = GetTransferImage ();
            mainMenu.FindMenuItem ("mnuOperWaste").Image = GetWasteImage ();
            mainMenu.FindMenuItem ("mnuOperInvoicePublish").Image = GetSaleImage ();
            mainMenu.FindMenuItem ("mnuOperInvoiceReceive").Image = GetPurchaseImage ();
            mainMenu.FindMenuItem ("mnuOperInvoicePublishCancel").Image = GetInvoiceImage ();
            mainMenu.FindMenuItem ("mnuOperInvoiceReceiveCancel").Image = GetInvoiceImage ();
            mainMenu.FindMenuItem ("mnuOperTradeObject").Image = FormHelper.LoadImage ("Icons.TradePoint16.png");

            mainMenu.FindMenuItem ("mnuEditPartners").Image = FormHelper.LoadImage ("Icons.Partner16.png");
            mainMenu.FindMenuItem ("mnuEditGoods").Image = FormHelper.LoadImage ("Icons.Goods16.png");
            mainMenu.FindMenuItem ("mnuEditUsers").Image = FormHelper.LoadImage ("Icons.User16.png");
            mainMenu.FindMenuItem ("mnuEditObjects").Image = FormHelper.LoadImage ("Icons.Location16.png");
            mainMenu.FindMenuItem ("mnuEditVATGroups").Image = FormHelper.LoadImage ("Icons.VATGroup16.png");
            mainMenu.FindMenuItem ("mnuEditPaymentTypes").Image = FormHelper.LoadImage ("Icons.PaymentType16.png");
            mainMenu.FindMenuItem ("mnuEditDevices").Image = FormHelper.LoadImage ("Icons.Device16.png");
            mainMenu.FindMenuItem ("mnuEditVATGroups").Image = FormHelper.LoadImage ("Icons.VATGroup16.png");

            mainMenu.FindMenuItem ("mnuEditDocsSale").Image = GetSaleImage ();
            mainMenu.FindMenuItem ("mnuEditDocsDelivery").Image = GetPurchaseImage ();
            mainMenu.FindMenuItem ("mnuEditDocsComplexProduction").Image = GetProductionImage ();
            mainMenu.FindMenuItem ("mnuEditDocsTransfer").Image = GetTransferImage ();
            mainMenu.FindMenuItem ("mnuEditDocsWaste").Image = GetWasteImage ();
            mainMenu.FindMenuItem ("mnuEditDocsRevision").Image = GetStockTakingImage ();

            mainMenu.FindMenuItem ("mnuEditPrintAgainSale").Image = GetSaleImage ();
            mainMenu.FindMenuItem ("mnuEditPrintAgainDelivery").Image = GetPurchaseImage ();
            mainMenu.FindMenuItem ("mnuEditPrintAgainComplexProductions").Image = GetProductionImage ();
            mainMenu.FindMenuItem ("mnuEditPrintAgainTransfer").Image = GetTransferImage ();
            mainMenu.FindMenuItem ("mnuEditPrintAgainWaste").Image = GetWasteImage ();
            mainMenu.FindMenuItem ("mnuEditPrintAgainTaxDocumentsInvoice").Image = GetInvoiceImage ();
            mainMenu.FindMenuItem ("mnuEditPrintAgainInspection").Image = GetStockTakingImage ();

            mainMenu.FindMenuItem ("mnuEditRegObjects").Image = FormHelper.LoadImage ("Icons.CompanyRecord16.png");
            mainMenu.FindMenuItem ("mnuEditAdminPriceChange").Image = GetItemImage ();
            mainMenu.FindMenuItem ("mnuEditAdminPriceRules").Image = FormHelper.LoadImage ("Icons.PriceRules16.png");
            mainMenu.FindMenuItem ("mnuEditAdminRevision").Image = GetStockTakingImage ();
            mainMenu.FindMenuItem ("mnuEditAdminPermissions").Image = FormHelper.LoadImage ("Icons.Security16.png");
            mainMenu.FindMenuItem ("mnuEditAdminRegisterCash").Image = GetReportImage ();
            mainMenu.FindMenuItem ("mnuEditAdminFReports").Image = GetReportImage ();

            mainMenu.FindMenuItem ("mnuToolsSetup").Image = FormHelper.LoadImage ("Icons.Settings16.png");
        }

        private static Image GetSaleImage ()
        {
            return FormHelper.LoadImage ("Icons.Sale16.png");
        }

        private static Image GetPurchaseImage ()
        {
            return FormHelper.LoadImage ("Icons.Purchase16.png");
        }

        private static Image GetProductionImage ()
        {
            return FormHelper.LoadImage ("Icons.Production16.png");
        }

        private static Image GetTransferImage ()
        {
            return FormHelper.LoadImage ("Icons.Transfer16.png");
        }

        private static Image GetWasteImage ()
        {
            return FormHelper.LoadImage ("Icons.Waste16.png");
        }

        private static Image GetStockTakingImage ()
        {
            return FormHelper.LoadImage ("Icons.StockTaking16.png");
        }

        private static Image GetInvoiceImage ()
        {
            return FormHelper.LoadImage ("Icons.Invoice16.png");
        }

        private static Image GetItemImage ()
        {
            return FormHelper.LoadImage ("Icons.Goods16.png");
        }

        private static Image GetReportImage ()
        {
            return FormHelper.LoadImage ("Icons.Report16.png");
        }

        public void RefreshMenuShortcuts ()
        {
            AccelMap.Foreach (frmMain.Handle, AssignShortcutCallback);
        }

        private void AssignShortcutCallback (IntPtr pointer, string accelPath, uint key, ModifierType modifierType, bool changed)
        {
            if (key == 0 || key == (uint) Key.VoidSymbol)
                return;

            string name = accelPath.Substring (accelPath.IndexOf ('/') + 1);
            MenuItemWrapper menuItem = mainMenu.FindMenuItem (name);
            if (menuItem != null)
                menuItem.Item.SetAccelPath (accelPath, AccelGroup);
        }

        public void Show ()
        {
            frmMain.Sensitive = true;
            frmMain.Show ();
            frmMain.QueueDraw ();
        }

        private void frmMain_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (workBook.PagesCount == 0)
                return;

            if (args.Event.Key == Key.Escape) {
                args.RetVal = true;
                workBook.CurrentPage.OnOuterKeyPress (o, args);
            }
        }

        private void frmMain_Shown (object sender, EventArgs e)
        {
            RefreshStatusBar ();
            workBook.GrabFocus ();
            InitializeStartupPage ();
            PresentationDomain.MainFormLoaded.Set ();
        }

        public void InitializeStartupPage ()
        {
            if (!frmMain.IsRealized)
                return;

            string sPageClass = BusinessDomain.AppConfiguration.StartupPageClass;

            foreach (StartupPage page in PresentationDomain.StartupPages) {
                if (page.ClassName != sPageClass || string.IsNullOrEmpty (page.ClassName))
                    continue;

                WbpBase instance = page.CreateInstance ();
                if (instance != null) {
                    AddNewPage (instance);
                    return;
                }
                break;
            }

            LoadViewProfile (ViewProfile.Default);
        }

        #region Initialization steps

        private void InitializeAddins ()
        {
            foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Presentation/Menu")) {
                IMenuLauncher menuLauncher = node.CreateInstance () as IMenuLauncher;
                if (menuLauncher != null)
                    menuLauncher.Initialize (this);
            }
        }

        private void InitializeSettings ()
        {
            RefreshRestrictions (true);
            BusinessDomain.RestrictionTree.ReloadAccessLevelRestrictions ();

            BusinessDomain.AppConfiguration.PropertyChanged += AppConfiguration_PropertyChanged;
        }

        private void AppConfiguration_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName) {
                case "DocumentPrinterEnabled":
                case "CashReceiptPrinterEnabled":
                case "ExternalDisplayEnabled":
                case "DocumentNumbersPerLocation":
                    RefreshRestrictions (false);
                    break;
                case "UseSalesTaxInsteadOfVAT":
                    TranslateTax ();
                    mainMenu.Translate ();
                    break;
            }
        }

        public void InitializeStrings ()
        {
            Gtk.Settings.Default.FontName = DataHelper.DefaultUIFont;

            TranslateTax ();

            mainMenu.Translate ();
            PresentationDomain.OSIntegration.RefreshMenu ();
            mainToolbar.Translate ();
        }

        private void TranslateTax ()
        {
            BusinessDomain.RestrictionTree.FindNode ("mnuEditVATGroups").Value = BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT ?
                Translator.GetString ("Tax Groups...") : Translator.GetString ("VAT Groups...");
        }

        #endregion

        #region Window Event handlers

        [UsedImplicitly]
        public void frmMain_Delete (object o, DeleteEventArgs args)
        {
            PresentationDomain.OSIntegration.OnMainFormClosed ();
            args.RetVal = true;
        }

        #endregion

        #region Forms Creation

        #region Common methods

        private bool closingPages;

        static FrmMain ()
        {
            AccelGroup = new AccelGroup ();
        }

        private void workBook_CurrentPageChanged (object sender, CurrentPageChangedArgs e)
        {
            WbpBase page = workBook.CurrentPage as WbpBase;

            if (page == null)
                return;

            if (!closingPages)
                LoadViewProfile (page.ViewProfile);
        }

        public bool AddNewPage (WbpBase page)
        {
            try {
                workBook.AddPage (page, true);
                page.PageClose += workBookPage_PageClose;

                RefreshWindowsList ();
                return true;
            } catch (WorkPageAddException ex) {
                PresentationDomain.OnShowingDialog ();
                MessageError.ShowDialog (ex.Message, ErrorSeverity.Error, ex);
                return false;
            }
        }

        private void workBookPage_PageClose (object sender, EventArgs e)
        {
            workBook.RemovePage ((WorkBookPage) sender);
            if (workBook.PagesCount == 0)
                LoadViewProfile (ViewProfile.Default);

            RefreshWindowsList ();
        }

        private void CloseCurrentPage ()
        {
            if (workBook.PagesCount > 0)
                workBook.CurrentPage.RequestClose ();
        }

        public bool CloseAllPages (bool force = false)
        {
            try {
                closingPages = true;
                while (workBook.PagesCount > 0) {
                    WbpBase page = (WbpBase) workBook.CurrentPage;
                    if (force) {
                        page.Close ();
                        continue;
                    }

                    if (page.RequestClose ())
                        continue;

                    LoadViewProfile (page.ViewProfile);
                    return false;
                }

                return true;
            } finally {
                closingPages = false;
            }
        }

        private void RefreshWindowsList ()
        {
            while (mainMenu.RemoveItem ("mnuWindowList", false)) { }

            for (int i = workBook.PagesCount - 1; i >= 0; i--) {
                WorkBookPage page = workBook.GetPageAt (i);

                string text = string.Format ("{0}. {1}", i + 1, page.PageTitle);
                MenuItem menuItem = new MenuItem (text) { Name = "mnuWindowList" + i };
                menuItem.Activated += menuItem_Activated;

                mainMenu.InsertAfter ("sepWindowList", new MenuItemWrapper (menuItem, () => text));
                menuItem.Show ();
            }
        }

        private void menuItem_Activated (object sender, EventArgs e)
        {
            string itemName = ((Widget) sender).Name;
            string itemNumber = itemName.Substring ("mnuWindowList".Length);
            int pageIndex = int.Parse (itemNumber);

            workBook.CurrentPage = workBook.GetPageAt (pageIndex);
        }

        #endregion

        #region File

        public void CreateExitProgram ()
        {
            if (!CloseAllPages ())
                return;

            PresentationDomain.Quit ();
        }

        #endregion

        #region Operations

        private void CreateSaleForm (int? saleId, bool readOnlyPage)
        {
            WbpSale page;
            if (saleId == null)
                page = new WbpSale ();
            else {
                page = new WbpSale (saleId);
                if (readOnlyPage)
                    page.SetReadOnly ();
            }

            AddNewPage (page);
        }

        private void CreatePurchaseForm (int? purchaseId, bool readOnlyPage)
        {
            if (PresentationDomain.CheckPurchasePricesDisabled ())
                return;

            WbpPurchase page;
            if (purchaseId == null)
                page = new WbpPurchase ();
            else {
                page = new WbpPurchase (purchaseId);
                if (readOnlyPage)
                    page.SetReadOnly ();
            }

            AddNewPage (page);
        }

        private void CreateComplexProductionForm (int? id)
        {
            if (PresentationDomain.CheckPurchasePricesDisabled ())
                return;

            AddNewPage (id == null ? new WbpProduction () : new WbpProduction (id));
        }

        private void CreateTransferForm (int? transferId)
        {
            AddNewPage (transferId == null ? new WbpTransfer () : new WbpTransfer (transferId));
        }

        private void CreateWasteForm (int? wasteId)
        {
            AddNewPage (wasteId == null ? new WbpWaste () : new WbpWaste (wasteId));
        }

        private void CreateStockTakingForm (int? stockTakingId)
        {
            AddNewPage (stockTakingId == null ? new WbpStockTaking () : new WbpStockTaking (stockTakingId));
        }

        private void CreateDlgChooseEditComplexRecipe ()
        {
            if (PresentationDomain.CheckPurchasePricesDisabled ())
                return;

            using (ChooseEditComplexRecipe dialog = new ChooseEditComplexRecipe ())
                dialog.Run ();
        }

        private void CreateDlgChooseSaleToCreateNewInvoice ()
        {
            bool repeat;

            IList<Sale> sales;
            do {
                repeat = false;
                using (ChooseSale dlgSale = new ChooseSale (true, DocumentChoiceType.CreateChildDocument)) {
                    dlgSale.DialogControl.Title = Translator.GetString ("Issue Invoices - Select Document");

                    if (dlgSale.Run () != ResponseType.Ok)
                        return;

                    sales = dlgSale.SelectedItems;
                }

                if (sales.Count == 0)
                    return;

                if (!sales.Any (s => DocumentBase.DocumentExistsForOperation (s.Id, OperationType.Sale)))
                    continue;

                using (MessageOkCancel msg = new MessageOkCancel (
                    Translator.GetString ("Create invoice"), "Icons.Invoice16.png",
                    Translator.GetString ("An invoice attached to a selected document already exists in the database! Do you want to continue?"), "Icons.Question32.png")) {
                    if (msg.Run () != ResponseType.Ok)
                        repeat = true;
                }
            } while (repeat);

            using (EditNewInvoice dlgInvoice = new EditNewInvoice (sales))
                dlgInvoice.Run ();
        }

        private void CreateDlgChoosePurchaseToReceiveNewInvoice ()
        {
            bool repeat;

            IList<Purchase> purchases;
            do {
                repeat = false;
                using (ChoosePurchase dlgPurchase = new ChoosePurchase (true, DocumentChoiceType.CreateChildDocument)) {
                    dlgPurchase.DialogControl.Title = Translator.GetString ("Receive Invoices - Select Document");

                    if (dlgPurchase.Run () != ResponseType.Ok)
                        return;

                    purchases = dlgPurchase.SelectedItems;
                }

                if (purchases.Count == 0)
                    return;

                if (!purchases.Any (p => DocumentBase.DocumentExistsForOperation (p.Id, OperationType.Purchase)))
                    continue;

                using (MessageOkCancel msg = new MessageOkCancel (
                    Translator.GetString ("Create invoice"), "Icons.Invoice16.png",
                    Translator.GetString ("An invoice attached to a selected document already exists in the database! Do you want to continue?"), "Icons.Question32.png")) {
                    if (msg.Run () != ResponseType.Ok)
                        repeat = true;
                }
            } while (repeat);

            using (EditNewInvoice dlgInvoice = new EditNewInvoice (purchases))
                dlgInvoice.Run ();
        }

        private void CreateTradeScreenForm ()
        {
            AddNewPage (new WbpTradePoint ());
        }

        #endregion

        #region Edit

        private void CreateDlgChooseEditPartners ()
        {
            using (ChooseEditPartner dialog = new ChooseEditPartner (false, string.Empty))
                dialog.Run ();
        }

        private void CreateDlgChooseEditItem ()
        {
            using (ChooseEditItem dialog = new ChooseEditItem ())
                dialog.Run ();
        }

        private void CreateDlgChooseEditUsers ()
        {
            using (ChooseEditUser dialog = new ChooseEditUser ())
                dialog.Run ();
        }

        private void CreateDlgChooseEditLocation ()
        {
            using (ChooseEditLocation dialog = new ChooseEditLocation ())
                dialog.Run ();
        }

        private void CreateDlgChooseEditVATGroups ()
        {
            using (ChooseEditVATGroup dialog = new ChooseEditVATGroup ())
                dialog.Run ();
        }

        private void CreateDlgChooseEditPaymentTypes ()
        {
            using (ChooseEditPaymentType dialog = new ChooseEditPaymentType ())
                dialog.Run ();
        }

        private static void CreateDlgChooseEditPaysPayments ()
        {
            using (ChooseEditPayment dialog = new ChooseEditPayment ())
                dialog.Run ();
        }

        private void CreateDlgChooseEditDevices ()
        {
            using (ChooseEditDevice dialog = new ChooseEditDevice ())
                dialog.Run ();
        }

        private void CreateDlgChooseEditCompanyRecord ()
        {
            using (ChooseEditCompanyRecord dialog = new ChooseEditCompanyRecord (false))
                dialog.Run ();
        }

        private void CreateDlgEditPrices ()
        {
            using (EditPrices dialog = new EditPrices ())
                dialog.Run ();
        }

        private void CreateDlgChooseEditPriceRules ()
        {
            if (WorkBook.PagesCount > 0) {
                if (Message.ShowDialog (
                    Translator.GetString ("Warning!"), null,
                    Translator.GetString ("Before editing the price rules all tabs have to be closed. Do you want to close all open tabs?"),
                    "Icons.Question32.png",
                    MessageButtons.YesNo) != ResponseType.Yes)
                    return;

                if (!CloseAllPages ())
                    return;
            }

            using (ChooseEditPriceRule chooseEditPriceRules = new ChooseEditPriceRule ())
                chooseEditPriceRules.Run ();
        }

        private void CreateDlgChooseSaleToEdit ()
        {
            long? saleId;
            bool readOnly;

            using (ChooseSale dialog = new ChooseSale (true, DocumentChoiceType.Edit, OperationType.Sale)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                saleId = dialog.SelectedItemId;
                readOnly = dialog.ReadOnlyView;
            }

            if (saleId != null)
                CreateSaleForm ((int) saleId.Value, readOnly);
        }

        private void CreateDlgChoosePurchaseToEdit ()
        {
            if (PresentationDomain.CheckPurchasePricesDisabled ())
                return;

            long? purchaseId;
            bool readOnly;

            using (ChoosePurchase dialog = new ChoosePurchase (true, DocumentChoiceType.Edit)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                purchaseId = dialog.SelectedItemId;
                readOnly = dialog.ReadOnlyView;
            }

            if (purchaseId != null)
                CreatePurchaseForm ((int) purchaseId.Value, readOnly);
        }

        private void CreateDlgChooseProductionToEdit ()
        {
            if (PresentationDomain.CheckPurchasePricesDisabled ())
                return;

            long? id;

            using (ChooseComplexProduction dialog = new ChooseComplexProduction (DocumentChoiceType.Edit)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                id = dialog.SelectedItemId;
            }

            if (id != null)
                CreateComplexProductionForm ((int) id);
        }

        private void CreateDlgChooseTransferToEdit ()
        {
            long? transferId;

            using (ChooseTransfer dialog = new ChooseTransfer (DocumentChoiceType.Edit)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                transferId = dialog.SelectedItemId;
            }

            if (transferId != null)
                CreateTransferForm ((int) transferId);
        }

        private void CreateDlgChooseWasteToEdit ()
        {
            long? wasteId;

            using (ChooseWaste dialog = new ChooseWaste (DocumentChoiceType.Edit)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                wasteId = dialog.SelectedItemId;
            }

            if (wasteId != null)
                CreateWasteForm ((int) wasteId.Value);
        }

        private void CreateDlgChooseStockTakingToEdit ()
        {
            long? stockTakingId;

            using (ChooseStockTaking dialog = new ChooseStockTaking (DocumentChoiceType.Edit)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                stockTakingId = dialog.SelectedItemId;
            }

            if (stockTakingId != null)
                CreateStockTakingForm ((int) stockTakingId.Value);
        }

        private void CreateDlgChooseSaleToPrint ()
        {
            long? saleId;

            using (ChooseSale dialog = new ChooseSale (true, DocumentChoiceType.Print, OperationType.Sale)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                saleId = dialog.SelectedItemId;
                if (saleId == null)
                    return;
            }

            try {
                Sale sale = Sale.GetById ((int) saleId.Value);
                SaleReceipt receipt = new SaleReceipt (sale);
                FormHelper.PrintPreviewObject (receipt);
            } catch (Exception ex) {
                MessageError.ShowDialog (Translator.GetString ("An error occurred while generating the stock receipt!"),
                    ErrorSeverity.Error, ex);
            }
        }

        private void CreateDlgChoosePurchaseToPrint ()
        {
            if (PresentationDomain.CheckPurchasePricesDisabled ())
                return;

            long? purchaseId;

            using (ChoosePurchase dialog = new ChoosePurchase (true, DocumentChoiceType.Print)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                purchaseId = dialog.SelectedItemId;
                if (purchaseId == null)
                    return;
            }

            try {
                Purchase purchase = Purchase.GetById ((int) purchaseId.Value);
                if (BusinessDomain.WorkflowManager.AllowPurchaseReceiptPrint (purchase, true)) {
                    PurchaseReceipt receipt = new PurchaseReceipt (purchase);
                    FormHelper.PrintPreviewObject (receipt);
                }
            } catch (Exception ex) {
                MessageError.ShowDialog (Translator.GetString ("An error occurred while generating the stock receipt!"),
                    ErrorSeverity.Error, ex);
            }
        }

        private void CreateDlgChooseProductionToPrint ()
        {
            if (PresentationDomain.CheckPurchasePricesDisabled ())
                return;

            long? id;

            using (ChooseComplexProduction dialog = new ChooseComplexProduction (DocumentChoiceType.Print)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                id = dialog.SelectedItemId;
                if (id == null)
                    return;
            }

            try {
                ComplexProduction production = ComplexProduction.GetById ((int) id.Value);
                ProductionProtocol protocol = new ProductionProtocol (production);
                FormHelper.PrintPreviewObject (protocol);
            } catch (Exception ex) {
                MessageError.ShowDialog (Translator.GetString ("An error occurred while generating the protocol!"),
                    ErrorSeverity.Error, ex);
            }
        }

        private void CreateDlgChooseTransferToPrint ()
        {
            long? transferId;

            using (ChooseTransfer dialog = new ChooseTransfer (DocumentChoiceType.Print)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                transferId = dialog.SelectedItemId;
                if (transferId == null)
                    return;
            }

            try {
                Transfer transfer = Transfer.GetById ((int) transferId.Value);
                TransferReceipt receipt = new TransferReceipt (transfer);
                FormHelper.PrintPreviewObject (receipt);
            } catch (Exception ex) {
                MessageError.ShowDialog (Translator.GetString ("An error occurred while generating the stock receipt!"),
                    ErrorSeverity.Error, ex);
            }
        }

        private void CreateDlgChooseWasteToPrint ()
        {
            long? wasteId;

            using (ChooseWaste dialog = new ChooseWaste (DocumentChoiceType.Print)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                wasteId = dialog.SelectedItemId;
                if (wasteId == null)
                    return;
            }

            try {
                Waste waste = Waste.GetById ((int) wasteId.Value);
                WasteProtocol protocol = new WasteProtocol (waste);
                FormHelper.PrintPreviewObject (protocol);
            } catch (Exception ex) {
                MessageError.ShowDialog (Translator.GetString ("An error occurred while generating the protocol!"),
                    ErrorSeverity.Error, ex);
            }
        }

        private void CreateDlgChooseStockTakingToPrint ()
        {
            long? stockTakingId;

            using (ChooseStockTaking dialog = new ChooseStockTaking (DocumentChoiceType.Print)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                stockTakingId = dialog.SelectedItemId;
                if (stockTakingId == null)
                    return;
            }

            try {
                StockTaking stockTaking = StockTaking.GetById ((int) stockTakingId.Value);
                StockTakingProtocol protocol = new StockTakingProtocol (stockTaking);
                FormHelper.PrintPreviewObject (protocol);
            } catch (Exception ex) {
                MessageError.ShowDialog (Translator.GetString ("An error occurred while generating the protocol!"),
                    ErrorSeverity.Error, ex);
            }
        }

        private void CreateDlgChooseAdvancePaymentToPrint ()
        {
            using (ChooseAdvancePayment chooseAdvancePayment = new ChooseAdvancePayment (DocumentChoiceType.Print))
                if (chooseAdvancePayment.Run () == ResponseType.Ok) {
                    PaymentReceipt paymentReceipt = new PaymentReceipt (chooseAdvancePayment.SelectedItem);
                    FormHelper.PrintPreviewObject (paymentReceipt);
                }
        }

        private void CreateDlgEditUserRestriction ()
        {
            using (EditUserRestrictions dialog = new EditUserRestrictions ()) {
                dialog.Run ();
            }
        }

        private void CreateDlgFiscalRegisterCash ()
        {
            FormHelper.TryReceiptPrinterCommand (delegate
                {
                    using (FiscalRegisterCash dialog = new FiscalRegisterCash ()) {
                        dialog.Run ();
                    }
                });
        }

        private void CreateDlgFiscalReports ()
        {
            FormHelper.TryReceiptPrinterCommand (delegate
                {
                    using (FiscalReports dialog = new FiscalReports ()) {
                        dialog.Run ();
                    }
                });
        }

        private void CreateDlgEditDocumentNumbersPerLocation ()
        {
            using (EditDocumentNumbersPerLocation editDocumentNumbersPerLocation = new EditDocumentNumbersPerLocation ())
                editDocumentNumbersPerLocation.Run ();
        }

        #endregion

        #region Others

        private void CreateDlgKeyBindings ()
        {
            using (EditKeyShortcuts dialog = new EditKeyShortcuts (mainMenu, AccelGroup)) {
                dialog.Run ();
                RefreshMenuShortcuts ();
            }
        }

        private void CreateDlgShortcutsForItems ()
        {
            using (EditQuickItems editQuickItems = new EditQuickItems (mainMenu, AccelGroup))
                editQuickItems.Run ();
        }

        private void CreateDlgAbout ()
        {
            using (About dialog = new About ()) {
                dialog.Run ();
            }
        }

        #endregion

        #endregion

        #region ToolBar Event handlers

        [UsedImplicitly]
        private void btnQuit_Click (object o, EventArgs args)
        {
            PresentationDomain.OSIntegration.Quit ();
        }

        [UsedImplicitly]
        private void btnSale_Click (object o, EventArgs args)
        {
            CreateSaleForm (null, false);
        }

        [UsedImplicitly]
        private void btnPurchase_Click (object o, EventArgs args)
        {
            CreatePurchaseForm (null, false);
        }

        [UsedImplicitly]
        private void btnInvoice_Click (object o, EventArgs args)
        {
            CreateDlgChooseSaleToCreateNewInvoice ();
        }

        [UsedImplicitly]
        private void btnStockTaking_Click (object o, EventArgs args)
        {
            CreateStockTakingForm (null);
        }

        [UsedImplicitly]
        private void btnItems_Click (object o, EventArgs args)
        {
            CreateDlgChooseEditItem ();
        }

        [UsedImplicitly]
        private void btnPartners_Click (object o, EventArgs args)
        {
            CreateDlgChooseEditPartners ();
        }

        [UsedImplicitly]
        private void btnSalesReport_Click (object o, EventArgs args)
        {
            PresentationDomain.CreateReport (o, typeof (ReportQuerySales));
        }

        [UsedImplicitly]
        private void btnPurchasesReport_Click (object o, EventArgs args)
        {
            PresentationDomain.CreateReport (o, typeof (ReportQueryPurchases));
        }

        [UsedImplicitly]
        private void btnItemsQtyReport_Click (object o, EventArgs args)
        {
            PresentationDomain.CreateReport (o, typeof (ReportQueryItemsAvailability));
        }

        [UsedImplicitly]
        private void btnSalesByItemsReport_Click (object o, EventArgs args)
        {
            PresentationDomain.CreateReport (o, typeof (ReportQuerySalesByItem));
        }

        [UsedImplicitly]
        private void btnPurchasesByItemsReport_Click (object o, EventArgs args)
        {
            PresentationDomain.CreateReport (o, typeof (ReportQueryPurchasesByItems));
        }

        [UsedImplicitly]
        private void btnSalesByPartnerReport_Click (object o, EventArgs args)
        {
            PresentationDomain.CreateReport (o, typeof (ReportQuerySalesByPartners));
        }

        [UsedImplicitly]
        private void btnItemsProfitReport_Click (object o, EventArgs args)
        {
            PresentationDomain.CreateReport (o, typeof (ReportQueryItemsByProfit));
        }

        [UsedImplicitly]
        private void btnSettings_Click (object o, EventArgs args)
        {
            PresentationDomain.ShowSettings ();
        }

        #endregion

        #region MenuBar Event handlers

        #region File

        [UsedImplicitly]
        private void on_change_active_user_1_activate (object o, EventArgs args)
        {
            if (CloseAllPages ()) {
                PresentationDomain.ChangeUser ();
            }
        }

        [UsedImplicitly]
        private void on_change_database_1_activate (object o, EventArgs args)
        {
            if (CloseAllPages ()) {
                PresentationDomain.ChangeDatabase ();
            }
        }

        [UsedImplicitly]
        private void on_load_settings_activate (object o, EventArgs args)
        {
            string text = Translator.GetString ("All of your settings will be overwritten. Are you sure you wish to continue?");
            using (Message msg = new Message (Translator.GetString ("Warning!"), "Icons.Settings16.png", text, "Icons.Question32.png")) {
                msg.Buttons = MessageButtons.YesNo;
                if (msg.Run () != ResponseType.Yes)
                    return;
            }

            PresentationDomain.ProcessUIEvents ();
            if (!CloseAllPages ())
                return;

            string title = Translator.GetString ("Load Settings");
            string initialDir = Directory.Exists (BusinessDomain.AppConfiguration.LastSettingsBackupAt) ?
                BusinessDomain.AppConfiguration.LastSettingsBackupAt :
                Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);

            string file;
            if (!PresentationDomain.OSIntegration.ChooseFileForOpen (title, initialDir, out file))
                return;

            try {
                BusinessDomain.AppConfiguration.LastSettingsBackupAt = Path.GetDirectoryName (file);
                new SettingsBackup ().Load (file);
                Message.ShowDialog (Translator.GetString ("Restart"), null,
                    string.Format (Translator.GetString ("In order for the changes to take effect {0} will be restarted."), DataHelper.ProductName), "Icons.Info32.png");
                PresentationDomain.QueueRestart ();
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                MessageError.ShowDialog (Translator.GetString ("An error occurred and your settings were not successfully loaded."),
                    ErrorSeverity.Error, ex);
            }
        }

        [UsedImplicitly]
        private void on_save_settings_activate (object o, EventArgs args)
        {
            string title = Translator.GetString ("Save Settings");
            string initialDir;
            if (Directory.Exists (BusinessDomain.AppConfiguration.LastSettingsBackupAt))
                initialDir = BusinessDomain.AppConfiguration.LastSettingsBackupAt;
            else
                initialDir = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
            const string initialFile = "Settings.zip";
            string file;
            FileChooserFilter ff = new FileChooserFilter
                {
                    Name = string.Format ("{0} ({1})", "Zip archive", string.Join ("; ", new [] { "*.zip" })),
                    FileMasks = new [] { "*.zip" }
                };
            bool result = PresentationDomain.OSIntegration.ChooseFileForSave (title, initialDir, initialFile, out file, ff);
            if (result) {
                try {
                    BusinessDomain.AppConfiguration.LastSettingsBackupAt = Path.GetDirectoryName (file);
                    new SettingsBackup ().Save (file);
                } catch (Exception ex) {
                    ErrorHandling.LogException (ex);
                    string message = Translator.GetString ("An error occurred and your settings were not successfully saved.");
                    MessageError.ShowDialog (message, ErrorSeverity.Error, ex);
                }
            }
        }

        [UsedImplicitly]
        private void on_exit2_activate (object o, EventArgs args)
        {
            PresentationDomain.OSIntegration.Quit ();
        }

        #endregion

        #region Operations

        [UsedImplicitly]
        private void on_sale_1_activate (object o, EventArgs args)
        {
            CreateSaleForm (null, false);
        }

        [UsedImplicitly]
        private void on_purchase_1_activate (object o, EventArgs args)
        {
            CreatePurchaseForm (null, false);
        }

        [UsedImplicitly]
        private void on_complex_recipies_activate (object o, EventArgs args)
        {
            CreateDlgChooseEditComplexRecipe ();
        }

        [UsedImplicitly]
        private void on_complex_production1_activate (object o, EventArgs args)
        {
            CreateComplexProductionForm (null);
        }

        [UsedImplicitly]
        private void on_transfer_1_activate (object o, EventArgs args)
        {
            CreateTransferForm (null);
        }

        [UsedImplicitly]
        private void on_waste_1_activate (object o, EventArgs args)
        {
            CreateWasteForm (null);
        }

        [UsedImplicitly]
        private void on_create_new_invoice_1_activate (object o, EventArgs args)
        {
            CreateDlgChooseSaleToCreateNewInvoice ();
        }

        [UsedImplicitly]
        private void on_receive_new_invoice_1_activate (object o, EventArgs args)
        {
            CreateDlgChoosePurchaseToReceiveNewInvoice ();
        }

        [UsedImplicitly]
        private void on_annul_created_invoice_1_activate (object o, EventArgs args)
        {
            using (ChooseIssuedInvoice dialog = new ChooseIssuedInvoice (DocumentChoiceType.Annul))
                dialog.Run ();
        }

        [UsedImplicitly]
        private void on_annul_received_invoice_1_activate (object o, EventArgs args)
        {
            using (ChooseReceivedInvoice dialog = new ChooseReceivedInvoice (DocumentChoiceType.Annul))
                dialog.Run ();
        }

        [UsedImplicitly]
        private void on_trade_screen_1_activate (object o, EventArgs args)
        {
            CreateTradeScreenForm ();
        }

        #endregion

        #region Edit

        [UsedImplicitly]
        private void on_partners_1_activate (object o, EventArgs args)
        {
            CreateDlgChooseEditPartners ();
        }

        [UsedImplicitly]
        private void on_items_1_activate (object o, EventArgs args)
        {
            CreateDlgChooseEditItem ();
        }

        [UsedImplicitly]
        private void on_users_1_activate (object o, EventArgs args)
        {
            CreateDlgChooseEditUsers ();
        }

        [UsedImplicitly]
        private void on_locations_2_activate (object o, EventArgs args)
        {
            CreateDlgChooseEditLocation ();
        }

        [UsedImplicitly]
        private void on_vat_groups_activate (object o, EventArgs args)
        {
            CreateDlgChooseEditVATGroups ();
        }

        [UsedImplicitly]
        private void on_payment_types_activate (object o, EventArgs args)
        {
            CreateDlgChooseEditPaymentTypes ();
        }

        [UsedImplicitly]
        private void OnEditPaysPaymentsActivate (object o, EventArgs args)
        {
            CreateDlgChooseEditPaysPayments ();
        }

        [UsedImplicitly]
        private void on_devices_activate (object o, EventArgs args)
        {
            CreateDlgChooseEditDevices ();
        }

        [UsedImplicitly]
        private void on_sales_2_activate (object o, EventArgs args)
        {
            CreateDlgChooseSaleToEdit ();
        }

        [UsedImplicitly]
        private void on_purchase_3_activate (object o, EventArgs args)
        {
            CreateDlgChoosePurchaseToEdit ();
        }

        [UsedImplicitly]
        private void on_complex_production_1_activate (object o, EventArgs args)
        {
            CreateDlgChooseProductionToEdit ();
        }

        [UsedImplicitly]
        private void on_transfer_2_activate (object o, EventArgs args)
        {
            CreateDlgChooseTransferToEdit ();
        }

        [UsedImplicitly]
        private void on_waste_2_activate (object o, EventArgs args)
        {
            CreateDlgChooseWasteToEdit ();
        }

        [UsedImplicitly]
        private void on_stockTaking_2_activate (object o, EventArgs args)
        {
            CreateDlgChooseStockTakingToEdit ();
        }

        [UsedImplicitly]
        private void on_sales_1_activate (object o, EventArgs args)
        {
            CreateDlgChooseSaleToPrint ();
        }

        [UsedImplicitly]
        private void on_purchase_2_activate (object o, EventArgs args)
        {
            CreateDlgChoosePurchaseToPrint ();
        }

        [UsedImplicitly]
        private void on_complex_production_2_activate (object o, EventArgs args)
        {
            CreateDlgChooseProductionToPrint ();
        }

        [UsedImplicitly]
        private void on_transfer_3_activate (object o, EventArgs args)
        {
            CreateDlgChooseTransferToPrint ();
        }

        [UsedImplicitly]
        private void on_waste_3_activate (object o, EventArgs args)
        {
            CreateDlgChooseWasteToPrint ();
        }

        [UsedImplicitly]
        private void on_reprintInvoice_activate (object o, EventArgs args)
        {
            long? invoiceNumber;
            using (ChooseIssuedInvoice dialog = new ChooseIssuedInvoice (DocumentChoiceType.Print)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                invoiceNumber = dialog.SelectedItemId;
            }

            if (invoiceNumber == null)
                return;

            Invoice invoice = Invoice.GetIssuedByNumber (invoiceNumber.Value);
            if (invoice == null)
                return;

            using (EditNewInvoice editDialog = new EditNewInvoice (invoice)) {
                editDialog.Run ();
            }
        }

        [UsedImplicitly]
        private void on_stockTaking_1_activate (object o, EventArgs args)
        {
            CreateDlgChooseStockTakingToPrint ();
        }

        [UsedImplicitly]
        private void mnuEditPrintAgainAdvancePayment_Activate (object o, EventArgs args)
        {
            CreateDlgChooseAdvancePaymentToPrint ();
        }

        [UsedImplicitly]
        private void on_company_information_1_activate (object o, EventArgs args)
        {
            CreateDlgChooseEditCompanyRecord ();
        }

        [UsedImplicitly]
        private void on_change_prices_1_activate (object o, EventArgs args)
        {
            CreateDlgEditPrices ();
        }

        [UsedImplicitly]
        private void on_price_rules_1_activate (object o, EventArgs args)
        {
            CreateDlgChooseEditPriceRules ();
        }

        [UsedImplicitly]
        private void on_delete_period_1_activate (object o, EventArgs args)
        {
            // TODO : This method is not implemented yet
        }

        [UsedImplicitly]
        private void on_stockTaking_3_activate (object o, EventArgs args)
        {
            CreateStockTakingForm (null);
        }

        [UsedImplicitly]
        private void on_user_restriction_activate (object o, EventArgs args)
        {
            CreateDlgEditUserRestriction ();
        }

        [UsedImplicitly]
        private void on_register_cash_activate (object o, EventArgs args)
        {
            CreateDlgFiscalRegisterCash ();
        }

        [UsedImplicitly]
        private void on_fiscal_reports_activate (object o, EventArgs args)
        {
            CreateDlgFiscalReports ();
        }

        [UsedImplicitly]
        private void on_print_duplicate_of_last_receipt_activate (object o, EventArgs args)
        {
            FormHelper.TryReceiptPrinterCommand (r => BusinessDomain.DeviceManager.TryDeviceCommand (
                () => BusinessDomain.DeviceManager.CashReceiptPrinterDriver.PrintDuplicate ()));
        }

        [UsedImplicitly]
        private void on_document_numbers_by_locations_activate (object o, EventArgs args)
        {
            CreateDlgEditDocumentNumbersPerLocation ();
        }

        #endregion

        #region Reports

        [UsedImplicitly]
        private void on_create_report_activate (object o, EventArgs args)
        {
            PresentationDomain.CreateReport (o);
        }

        [UsedImplicitly]
        private void on_last_report_activate (object sender, EventArgs e)
        {
            PresentationDomain.CreateLastReport ();
        }

        #endregion

        #region Other

        [UsedImplicitly]
        private void on_settings_1_activate (object o, EventArgs args)
        {
            PresentationDomain.ShowSettings ();
        }

        [UsedImplicitly]
        private void on_key_shortcuts_activate (object o, EventArgs args)
        {
            CreateDlgKeyBindings ();
        }

        [UsedImplicitly]
        private void on_quick_items_activate (object o, EventArgs args)
        {
            CreateDlgShortcutsForItems ();
        }

        [UsedImplicitly]
        private void on_tool_bar1_activate (object o, EventArgs args)
        {
            CheckMenuItem item = (CheckMenuItem) o;

            if ((settingProfile && item == null) ||
                (!settingProfile && tbMain.Visible != item.Active)) {
                if (item == null)
                    item = (CheckMenuItem) mainMenu.FindMenuItem ("mnuViewToolbar").Item;

                tbMain.Visible = item.Active;
            }

            if (settingProfile)
                return;

            try {
                settingProfile = true;
                ViewProfile viewProfile = BusinessDomain.AppConfiguration.CurrentViewProfile;
                if (viewProfile != null)
                    viewProfile.ShowToolbar = item.Active;
            } finally {
                settingProfile = false;
            }
        }

        [UsedImplicitly]
        private void on_show_tabs1_activate (object o, EventArgs args)
        {
            CheckMenuItem item = (CheckMenuItem) o;

            if ((settingProfile && item == null) ||
                (!settingProfile && workBook.ShowTabs != item.Active)) {
                if (item == null)
                    item = (CheckMenuItem) mainMenu.FindMenuItem ("mnuViewShowTabs").Item;

                workBook.ShowTabs = item.Active;
            }

            if (settingProfile)
                return;

            try {
                settingProfile = true;
                ViewProfile viewProfile = BusinessDomain.AppConfiguration.CurrentViewProfile;
                if (viewProfile != null)
                    viewProfile.ShowTabs = item.Active;
            } finally {
                settingProfile = false;
            }
        }

        [UsedImplicitly]
        private void on_status_bar1_activate (object o, EventArgs args)
        {
            CheckMenuItem item = (CheckMenuItem) o;

            if ((settingProfile && item == null) ||
                (!settingProfile && csbStatus.Visible != item.Active)) {
                if (item == null)
                    item = (CheckMenuItem) mainMenu.FindMenuItem ("mnuViewStatusbar").Item;

                csbStatus.Visible = item.Active;
            }

            if (settingProfile)
                return;

            try {
                settingProfile = true;
                ViewProfile viewProfile = BusinessDomain.AppConfiguration.CurrentViewProfile;
                if (viewProfile != null)
                    viewProfile.ShowStatusBar = item.Active;
            } finally {
                settingProfile = false;
            }
        }

        #endregion

        #region Window

        /// <summary>
        /// Closes the current workbook page.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        [UsedImplicitly]
        private void on_close_current_window1_activate (object o, EventArgs args)
        {
            CloseCurrentPage ();
        }

        /// <summary>
        /// Closes all workbook pages.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        [UsedImplicitly]
        private void on_close_all_windows1_activate (object o, EventArgs args)
        {
            CloseAllPages ();
        }

        #endregion

        #region Help

        [UsedImplicitly]
        private void on_documentation_activate (object o, EventArgs args)
        {
            FormHelper.ShowWindowHelp ("index.htm");
        }

        [UsedImplicitly]
        private void on_about1_activate (object o, EventArgs args)
        {
            CreateDlgAbout ();
        }

        #endregion

        #endregion

        private void LoadViewProfile (ViewProfile profile)
        {
            ViewProfile oldProfile = BusinessDomain.AppConfiguration.CurrentViewProfile;
            if (oldProfile != null)
                oldProfile.CommitChanges ();

            BusinessDomain.AppConfiguration.CurrentViewProfile = profile;

            if (settingProfile)
                return;

            try {
                settingProfile = true;
                mainMenu.FindMenuItem ("mnuViewToolbar").Active = profile.ShowToolbar;
                mainMenu.FindMenuItem ("mnuViewShowTabs").Active = profile.ShowTabs;
                mainMenu.FindMenuItem ("mnuViewStatusbar").Active = profile.ShowStatusBar;

                on_tool_bar1_activate (null, EventArgs.Empty);
                on_show_tabs1_activate (null, EventArgs.Empty);
                on_status_bar1_activate (null, EventArgs.Empty);
            } finally {
                settingProfile = false;
            }
        }

        public void RefreshRestrictions (bool reloadFromDB)
        {
            if (reloadFromDB)
                BusinessDomain.RestrictionTree.ReloadRestrictions ();

            mainMenu.ReloadRestrictions ();
            mainToolbar.ReloadRestrictions ();

            #region Restrict document printing menus if needed

            if (!BusinessDomain.AppConfiguration.IsPrintingAvailable ())
                mainMenu.FindMenuItem ("mnuEditPrintAgain")
                    .SetRestriction (User.AllId, UserRestrictionState.Disabled);

            #endregion

            #region Restrict sales if needed

            DeviceManagerBase deviceManager = BusinessDomain.DeviceManager;
            ICashReceiptPrinterController cashReceiptPrinter = deviceManager.SalesDataControllerDriver as ICashReceiptPrinterController
                ?? deviceManager.CashReceiptPrinterDriver;

            if (BusinessDomain.AppConfiguration.CashReceiptPrinterEnabled && cashReceiptPrinter == null)
                deviceManager.InitCashReceiptPrinter (true);

            cashReceiptPrinter = deviceManager.CashReceiptPrinterDriver;
            bool enbDisplay = BusinessDomain.AppConfiguration.ExternalDisplayEnabled;

            if (cashReceiptPrinter != null) {
                if (!cashReceiptPrinter.SupportedCommands.Contains (DeviceCommands.PrintDuplicate)) {
                    mainMenu.FindMenuItem ("mnuEditAdminPrintDuplicateOfLastReceipt")
                        .SetRestriction (User.AllId, UserRestrictionState.Disabled);
                }
            } else {
                mainMenu.FindMenuItem ("mnuEditAdminRegisterCash")
                    .SetRestriction (User.AllId, UserRestrictionState.Disabled);

                mainMenu.FindMenuItem ("mnuEditAdminFReports")
                    .SetRestriction (User.AllId, UserRestrictionState.Disabled);

                mainMenu.FindMenuItem ("mnuEditAdminPrintDuplicateOfLastReceipt")
                    .SetRestriction (User.AllId, UserRestrictionState.Disabled);
            }

            if (((!BusinessDomain.WorkflowManager.AllowSaleWithoutReceipt && cashReceiptPrinter == null) ||
                (!BusinessDomain.WorkflowManager.AllowSaleWithoutDisplay && !enbDisplay))) {
                mainMenu.FindMenuItem ("mnuOperSales")
                    .SetRestriction (User.AllId, UserRestrictionState.Disabled);

                mainMenu.FindMenuItem ("mnuOperTradeObject")
                    .SetRestriction (User.AllId, UserRestrictionState.Disabled);

                mainToolbar.FindToolItem ("btnSale")
                    .SetRestriction (User.AllId, UserRestrictionState.Disabled);
            }

            #endregion

            #region Restrict document numbering if needed

            if (!BusinessDomain.AppConfiguration.DocumentNumbersPerLocation ||
                !BusinessDomain.LoggedUser.IsSaved ||
                BusinessDomain.LoggedUser.UserLevel < UserAccessLevel.Administrator)
                mainMenu.FindMenuItem ("mnuEditAdminDocumentNumbersPerLocation")
                    .SetRestriction (User.AllId, UserRestrictionState.Disabled);

            #endregion

            if (RestrictionsReload != null)
                RestrictionsReload (this, EventArgs.Empty);

            long userId = BusinessDomain.LoggedUser.Id;
            mainMenu.ApplyRestriction (userId);
            mainToolbar.ApplyRestriction (userId);
            PresentationDomain.OSIntegration.RestrictionsApplied ();
        }

        public void ShowNotification (string notification)
        {
            csbStatus.ShowNotification (notification);
        }

        public void RefreshStatusBar ()
        {
            csbStatus.Refresh ();
        }
    }
}
