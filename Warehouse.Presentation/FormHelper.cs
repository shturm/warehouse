//
// FormHelper.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com
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
using System.IO;
using System.Reflection;
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Business.Devices;
using Warehouse.Business.Documenting;
using Warehouse.Business.Entities;
using Warehouse.Business.Licensing;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Component.Documenting;
using Warehouse.Data;
using Warehouse.Presentation.Dialogs;
using Item = Warehouse.Business.Entities.Item;

namespace Warehouse.Presentation
{
    public static class FormHelper
    {
        private static ResourcesProviderBase resProvider;

        public static string DefaultMonospaceFont
        {
            get
            {
                switch (PlatformHelper.Platform) {
                    case PlatformTypes.Windows:
                        // Vista has the very beautiful Consolas
                        return Environment.OSVersion.Version.Major >= 6 ? "Consolas" : "Courier New";
                    case PlatformTypes.Linux:
                        return "monospace";
                    case PlatformTypes.MacOSX:
                        return "Monaco";
                    default:
                        return string.Empty;
                }
            }
        }

        public static void Init (ResourcesProviderBase provider)
        {
            if (provider == null)
                throw new ArgumentNullException ("provider");

            resProvider = provider;
            DataHelper.ResourcesAssembly = provider.GetType ().Assembly;
        }

        public static string GetResourceName (string resource)
        {
            return resProvider.GetResourceName (resource);
        }

        public static Glade.XML LoadGladeXML (string xmlResourceFileName, string rootWidget)
        {
            return Assembly.GetExecutingAssembly ()
                .LoadGladeXML ("Warehouse.Presentation." + xmlResourceFileName, rootWidget);
        }

        public static Image LoadImage (string imgResourceFileName)
        {
            return resProvider.LoadImage (imgResourceFileName);
        }

        public static Image LoadAnimation (string imgResourceFileName)
        {
            return resProvider.LoadAnimation (imgResourceFileName);
        }

        public static Image LoadLocalImage (string imgResourceFileName)
        {
            return Assembly.GetCallingAssembly ().LoadImage (imgResourceFileName);
        }

        public static Image LoadLocalAnimation (string imgResourceFileName)
        {
            return Assembly.GetCallingAssembly ().LoadAnimation (imgResourceFileName);
        }

        public static void ShowWindowHelp (string windowName)
        {
            string eventName = windowName;
            if (eventName.EndsWith (".html"))
                eventName = eventName.Substring (0, eventName.Length - ".html".Length);

            if (eventName.EndsWith (".htm"))
                eventName = eventName.Substring (0, eventName.Length - ".htm".Length);

            BusinessDomain.FeedbackProvider.TrackEvent ("Help opened", eventName.CamelSpace ());

            resProvider.ShowWindowHelp (windowName);
        }

        public static bool CanChooseDataField (DbField field)
        {
            DataType fieldType = ReportProvider.GetDataFieldType (field);
            switch (field.StrongField) {
                case DataField.PartnerName:
                case DataField.PartnersGroupsName:
                case DataField.ItemName:
                case DataField.ItemsGroupName:
                case DataField.LocationName:
                case DataField.SourceLocationName:
                case DataField.TargetLocationName:
                case DataField.LocationsGroupsName:
                case DataField.UserName:
                case DataField.OperationsUserName:
                case DataField.OperationsOperatorName:
                case DataField.UsersGroupsName:
                case DataField.OperationsUsersGroupsName:
                case DataField.OperationsOperatorsGroupsName:
                    return true;
                default:
                    return fieldType == DataType.Date || fieldType == DataType.DateTime;
            }
        }

        public static ResponseType ChooseDataFieldValue (DbField field, ref object value)
        {
            DataType fieldType = ReportProvider.GetDataFieldType (field);
            ChooseEdit dlgChooseEdit = null;
            ChooseDate dlgChooseDate = null;
            switch (field.StrongField) {
                case DataField.PartnerName:
                    dlgChooseEdit = new ChooseEditPartner (true, string.Empty);
                    break;
                case DataField.PartnersGroupsName:
                    dlgChooseEdit = new ChooseEditPartnersGroup ();
                    break;
                case DataField.ItemName:
                    dlgChooseEdit = new ChooseEditItem (true);
                    break;
                case DataField.ItemsGroupName:
                    dlgChooseEdit = new ChooseEditItemsGroup ();
                    break;
                case DataField.LocationName:
                case DataField.SourceLocationName:
                case DataField.TargetLocationName:
                    dlgChooseEdit = new ChooseEditLocation (true, string.Empty);
                    break;
                case DataField.LocationsGroupsName:
                    dlgChooseEdit = new ChooseEditLocationsGroup ();
                    break;
                case DataField.UserName:
                case DataField.OperationsUserName:
                case DataField.OperationsOperatorName:
                    dlgChooseEdit = new ChooseEditUser (true, string.Empty);
                    break;
                case DataField.UsersGroupsName:
                case DataField.OperationsUsersGroupsName:
                case DataField.OperationsOperatorsGroupsName:
                    dlgChooseEdit = new ChooseEditUsersGroup ();
                    break;
                default:
                    if (fieldType == DataType.Date || fieldType == DataType.DateTime) {
                        DateTime selectedDate = BusinessDomain.GetDateValue ((string) value);
                        dlgChooseDate = new ChooseDate { Selection = selectedDate == DateTime.MinValue ? BusinessDomain.Today : selectedDate };
                    }
                    break;
            }

            value = null;
            ResponseType ret = ResponseType.Cancel;
            if (dlgChooseEdit != null) {
                using (dlgChooseEdit) {
                    ret = dlgChooseEdit.Run ();
                    string [] selection = dlgChooseEdit.SelectedItemsText;
                    if (ret == ResponseType.Ok && selection.Length > 0)
                        value = selection [0];
                }
            } else if (dlgChooseDate != null) {
                using (dlgChooseDate) {
                    ret = dlgChooseDate.Run ();
                    value = BusinessDomain.GetFormattedDate (dlgChooseDate.Selection);
                }
            }

            return ret;
        }

        private static List<IOperationEditor> operationEditors;
        public static void EditOperation (OperationType type, long id)
        {
            string message;
            bool readOnlyView;
            if (!BusinessDomain.CanEditOperation (type, id, out message, out readOnlyView) && !readOnlyView) {
                if (!string.IsNullOrWhiteSpace (message))
                    MessageError.ShowDialog (message);

                return;
            }

            WbpOperationBase page = null;
            long invNum;
            bool allowView = false;
            switch (type) {
                case OperationType.Purchase:
                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditDocsDelivery") != UserRestrictionState.Allowed)
                        break;

                    invNum = DocumentBase.GetLastDocumentNumber (id, OperationType.Purchase);
                    if (invNum <= 0) {
                        if (!PresentationDomain.CheckPurchasePricesDisabled ())
                            page = new WbpPurchase (id);
                    } else {
                        ShowMessageHasInvoice (Purchase.GetById (id), invNum, "Icons.Purchase16.png", out allowView);
                        if (allowView)
                            page = new WbpPurchase (id);
                    }
                    break;
                case OperationType.Sale:
                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditDocsSale") != UserRestrictionState.Allowed)
                        break;

                    invNum = DocumentBase.GetLastDocumentNumber (id, OperationType.Sale);
                    if (invNum <= 0)
                        page = new WbpSale (id);
                    else {
                        ShowMessageHasInvoice (Sale.GetById (id), invNum, "Icons.Sale16.png", out allowView);
                        if (allowView)
                            page = new WbpSale (id);
                    }
                    break;
                case OperationType.Waste:
                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditDocsWaste") != UserRestrictionState.Allowed)
                        break;

                    page = new WbpWaste (id);
                    break;
                case OperationType.StockTaking:
                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditDocsRevision") != UserRestrictionState.Allowed)
                        break;

                    if (!StockTaking.GetById (id).CheckIsNewFormat ())
                        break;

                    page = new WbpStockTaking (id);
                    break;
                case OperationType.TransferIn:
                case OperationType.TransferOut:
                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditDocsTransfer") != UserRestrictionState.Allowed)
                        break;

                    page = new WbpTransfer (id);
                    break;
                case OperationType.ComplexRecipeMaterial:
                case OperationType.ComplexRecipeProduct:
                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuOperProductionComplexRecipes") != UserRestrictionState.Allowed)
                        break;

                    if (!PresentationDomain.CheckPurchasePricesDisabled ())
                        using (EditNewComplexRecipe dlgRecipe = new EditNewComplexRecipe (ComplexRecipe.GetById (id)))
                            dlgRecipe.Run ();
                    break;
                case OperationType.ComplexProductionMaterial:
                case OperationType.ComplexProductionProduct:
                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditDocsComplexProduction") != UserRestrictionState.Allowed)
                        break;

                    if (!PresentationDomain.CheckPurchasePricesDisabled ())
                        page = new WbpProduction (id);
                    break;
                default:
                    if (operationEditors == null) {
                        operationEditors = new List<IOperationEditor> ();
                        foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Presentation/OperationEditor")) {
                            IOperationEditor editor = node.CreateInstance () as IOperationEditor;
                            if (editor != null)
                                operationEditors.Add (editor);
                        }
                    }

                    foreach (IOperationEditor editor in operationEditors) {
                        if (editor.SupportedType != type)
                            continue;

                        page = editor.GetEditorPage (id);
                        break;
                    }
                    break;
            }

            if (page == null)
                return;

            if (readOnlyView || allowView)
                page.SetReadOnly ();

            PresentationDomain.MainForm.AddNewPage (page);
        }

        public static bool ShowMessageHasInvoice<TOper> (TOper oper, long invNum, string icon, out bool allowView, bool annulling = false, DocumentChoiceType choiceType = DocumentChoiceType.Choose) where TOper : Operation
        {
            string message = null;
            allowView = false;
            if (annulling) {
                if (oper.OperationType == OperationType.Purchase)
                    message = Translator.GetString ("Document No.{0} cannot be annulled because the received invoice " +
                        "No.{1} is attached to it. Please, void the invoice before annulling this document.");
                else
                    message = Translator.GetString ("Document No.{0} cannot be annulled because the issued invoice " +
                        "No.{1} is attached to it. Please, void the invoice before annulling this document.");
            } else if (choiceType == DocumentChoiceType.CreateChildDocument) {
                if (oper.OperationType == OperationType.Purchase)
                    message = Translator.GetString ("Document No.{0} cannot be attached to invoice because the received invoice " +
                        "No.{1} is already attached to it. Please, void the invoice before attaching another invoice to this document.");
                else
                    message = Translator.GetString ("Document No.{0} cannot be attached to another invoice because the issued invoice " +
                        "No.{1} is already attached to it. Please, void the invoice before attaching another invoice to this document.");
            }

            if (message == null) {
                allowView = true;
                if (BusinessDomain.LoggedUser.UserLevel > UserAccessLevel.Operator)
                    return ShowMessageHasInvoiceChangeUser (oper, invNum, icon, out allowView);

                if (oper.OperationType == OperationType.Consignment)
                    message = Translator.GetString ("Consignment No.{0} cannot be edited because the issued invoice " +
                        "No.{1} is attached to a sale on it. Please, void the invoice before editing this consignment.");
                else if (oper.OperationType != OperationType.Purchase)
                    message = Translator.GetString ("Document No.{0} cannot be edited because the issued invoice " +
                        "No.{1} is attached to it. Please, void the invoice before editing this document.");
                else
                    message = Translator.GetString ("Document No.{0} cannot be edited because the received invoice " +
                        "No.{1} is attached to it. Please, void the invoice before editing this document.");
            }

            using (MessageError msg = new MessageError (string.Format (message,
                Operation.GetFormattedOperationNumber (oper.Id),
                Operation.GetFormattedOperationNumber (invNum)), icon, ErrorSeverity.Warning, null))
                msg.Run ();

            return false;
        }

        private static bool ShowMessageHasInvoiceChangeUser<TOper> (TOper oper, long invNum, string icon, out bool allowView) where TOper : Operation
        {
            allowView = false;
            string message;
            if (oper.OperationType == OperationType.Consignment)
                message = Translator.GetString ("Consignment No.{0} cannot be edited because the issued invoice No.{1} is attached to a sale on it. Please, void the invoice before editing this consignment.");
            else if (oper.OperationType != OperationType.Purchase)
                message = Translator.GetString ("Document No.{0} cannot be edited because the issued invoice No.{1} is attached to it. Please, void the invoice before editing this document.");
            else
                message = Translator.GetString ("Document No.{0} cannot be edited because the received invoice No.{1} is attached to it. Please, void the invoice before editing this document.");

            message += "\n\n" + Translator.GetString ("Do you want to change the operator in this document instead?");

            using (Message msg = new Message (Translator.GetString ("Warning!"), icon, string.Format (message,
                Operation.GetFormattedOperationNumber (oper.Id),
                Operation.GetFormattedOperationNumber (invNum)), "Icons.Question32.png")) {
                msg.Buttons = MessageButtons.YesNo;
                if (msg.Run () != ResponseType.Yes) {
                    allowView = true;
                    return false;
                }
            }

            User [] users;
            using (ChooseEditUser dialog = new ChooseEditUser (true, oper.UserId)) {
                if (dialog.Run () != ResponseType.Ok)
                    return false;

                users = dialog.SelectedItems;
            }

            if (users.Length == 0)
                return false;

            Operation.ChangeUser (oper.OperationType, oper.Id, users [0].Id);
            return true;
        }

        public static Form LoadForm (IDocument document)
        {
            return resProvider.LoadForm (document);
        }

        public static void PrintForm (Form form, PrintSettings printSettings = null, MarginsI margins = null)
        {
            resProvider.PrintForm (form, printSettings, margins);
        }

        public static void PrintObject (IDocument document, PrintSettings printerSettings = null)
        {
            resProvider.PrintObject (document, printerSettings);
        }

        public static void PrintPreviewObject (IDocument document)
        {
            resProvider.PrintPreviewObject (document);
        }

        public delegate void DeviceCommand (HardwareErrorResponse lastResponse);

        /// <summary>
        /// Executes the delegate passed and ensures that the fiscal printer is connected.
        /// Also handles any possible problems and errors that the printer might be causing.
        /// </summary>
        /// <param name="cmd">The delegate to be executed</param>
        /// <param name="connectFiscalDevice"></param>
        /// <param name="buttonsMask">Mask that potentially can hide buttons that we don't want to be shown in case of an error</param>
        /// <returns>True if the delegate execution succeeds</returns>
        public static bool TryReceiptPrinterCommand (DeviceCommand cmd, bool connectFiscalDevice = true, MessageButtons buttonsMask = MessageButtons.All)
        {
            HardwareErrorResponse lastResponse = new HardwareErrorResponse ();
            do {
                lastResponse.Retry = false;
                try {
                    if (connectFiscalDevice) {
                        if (!BusinessDomain.DeviceManager.CashReceiptPrinterConnected)
                            BusinessDomain.DeviceManager.ConnectCashReceiptPrinter ();
                    } else {
                        if (!BusinessDomain.DeviceManager.CustomerOrderPrinterConnected)
                            BusinessDomain.DeviceManager.ConnectCustomerOrderPrinter ();
                    }

                    cmd (lastResponse);
                } catch (HardwareErrorException ex) {
                    lastResponse = HandleHardwareError (ex, buttonsMask);
                    if (!lastResponse.Retry) {
                        BusinessDomain.DeviceManager.DeviceWorkerResume ();
                        return false;
                    }
                }
            } while (lastResponse.Retry);

            return true;
        }

        /// <summary>
        /// Handles the hardware error passed and shows the appropriate message
        /// </summary>
        /// <param name="ex">The HardwareErrorException to be handled</param>
        /// <param name="buttonsMask">The AND mask to be applied for the Message buttons</param>
        public static HardwareErrorResponse HandleHardwareError (HardwareErrorException ex, MessageButtons buttonsMask = MessageButtons.All)
        {
            bool retry;
            HardwareErrorResponse ret;

            do {
                retry = false;
                ret = ShowHardwareErrorMessage (ex, buttonsMask);

                ICashReceiptPrinterController cashReceiptPrinter = BusinessDomain.DeviceManager.CashReceiptPrinterDriver;
                IConnectableDevice nonFiscalPrinter = BusinessDomain.DeviceManager.CustomerOrderPrinter;
                try {
                    if (ret.Retry) {
                        if (ret.RetryWithoutPrint && cashReceiptPrinter == null && nonFiscalPrinter == null) {
                            ret.RetryWithoutPrint = false;
                            ret.CancelLastReceipt = true;
                        }

                        if (ret.RetryWithoutPrint) {
                            if (cashReceiptPrinter != null &&
                                cashReceiptPrinter.SupportedCommands.Contains (DeviceCommands.GetStatus)) {
                                cashReceiptPrinter.GetStatus ();
                                if (cashReceiptPrinter.LastErrorState.CheckError (ErrorState.NoPaper) ||
                                    cashReceiptPrinter.LastErrorState.Check (ErrorState.WaitingPaperReplaceConfirmation)) {
                                    retry = true;
                                    continue;
                                }

                                if (cashReceiptPrinter.LastErrorState.Check (ErrorState.FiscalCheckOpen)) {
                                    ret.RetryWithoutPrint = false;
                                    ret.CancelLastReceipt = true;
                                } else
                                    ret.CancelLastReceipt = false;
                            }
                        }

                        if (ret.RetryWithoutPrint) {
                            if (nonFiscalPrinter != null &&
                                nonFiscalPrinter.SupportedCommands.Contains (DeviceCommands.GetStatus)) {
                                nonFiscalPrinter.GetStatus ();
                                if (nonFiscalPrinter.LastErrorState.CheckError (ErrorState.NoPaper) ||
                                    nonFiscalPrinter.LastErrorState.Check (ErrorState.WaitingPaperReplaceConfirmation)) {
                                    retry = true;
                                    continue;
                                }

                                if (nonFiscalPrinter.LastErrorState.Check (ErrorState.NonFiscalCheckOpen)) {
                                    ret.RetryWithoutPrint = false;
                                    ret.CancelLastReceipt = true;
                                } else
                                    ret.CancelLastReceipt = false;
                            }
                        }
                    }

                    if (ret.CancelLastReceipt) {
                        if (cashReceiptPrinter != null &&
                            cashReceiptPrinter.LastErrorState.Check (ErrorState.FiscalCheckOpen)) {
                            bool annulled;
                            BusinessDomain.DeviceManager.CancelFiscalReceipt (out annulled);
                            if (!annulled)
                                ret.RetryWithoutPrint = true;
                        }
                        if (nonFiscalPrinter != null &&
                            nonFiscalPrinter.LastErrorState.Check (ErrorState.NonFiscalCheckOpen)) {
                            BusinessDomain.DeviceManager.CancelNonFiscalReceipt ();
                        }
                    }
                } catch (HardwareErrorException) {
                }
            } while (retry);

            return ret;
        }

        public static HardwareErrorResponse ShowHardwareErrorMessage (HardwareErrorException ex, MessageButtons buttonsMask)
        {
            HardwareErrorResponse ret = new HardwareErrorResponse { CancelLastReceipt = true };
            string message = string.Empty;
            MessageButtons buttons = MessageButtons.Retry | MessageButtons.Cancel;
            ErrorState error = ex.Error;

            if (error.Check (ErrorState.CashReceiptPrinterDisconnected))
                message = Translator.GetString ("Unable to connect to the cash receipt printer!");
            else if (error.Check (ErrorState.NonFiscalPrinterDisconnected))
                message = Translator.GetString ("Unable to connect to the receipt printer!");
            else if (error.Check (ErrorState.ExternalDisplayDisconnected))
                message = Translator.GetString ("Unable to connect to the external display!");
            else if (error.Check (ErrorState.CardReaderDisconnected))
                message = Translator.GetString ("Unable to connect to the card reader!");
            else if (error.Check (ErrorState.KitchenPrinterError)) {
                if ((buttonsMask & MessageButtons.OK) == MessageButtons.OK) {
                    message = Translator.GetString ("Unable to print on the kitchen printer! Press \"Retry\" to try again or \"OK\" to print the receipt on the printer for customer orders.");
                    buttons |= MessageButtons.OK;
                } else
                    message = Translator.GetString ("Unable to print on the kitchen printer! Press \"Retry\" to try again.");
            } else if (error.Check (ErrorState.KitchenPrinterDisconnected))
                message = Translator.GetString ("Unable to connect to the kitchen printer!");
            else if (error.Check (ErrorState.ElectronicScaleDisconnected))
                message = Translator.GetString ("Unable to connect to the electronic scale!");
            else if (error.Check (ErrorState.ElectronicScaleNotEnabled)) {
                message = Translator.GetString ("There is no electronic scale installed or the electronic scale is not enabled!");
                buttons = MessageButtons.Cancel;
            } else if (error.Check (ErrorState.SalesDataControllerDisconnected))
                message = Translator.GetString ("Unable to connect to the sales data controller!");
            else if (error.Check (ErrorState.ClockNotSet))
                message = Translator.GetString ("The clock of the fiscal printer is not set. Please set it to the correct time!");
            else if (error.Check (ErrorState.KitchenPrinterNoPaper)) {
                message = Translator.GetString ("The kitchen printer is out of paper!");
                buttons |= MessageButtons.OK;
            } else if (error.CheckError (ErrorState.NoPaper)) {
                message = Translator.GetString ("The fiscal printer is out of paper. Please replace!");
                ret.RetryWithoutPrint = true;
            } else if (error.Check (ErrorState.FiscalPrinterNotReady))
                message = Translator.GetString ("The fiscal printer is not ready to print. Please make sure that the fiscal printer is in the correct mode!");
            else if (error.Check (ErrorState.WaitingPaperReplaceConfirmation)) {
                message = Translator.GetString ("The fiscal printer waits for key combination to accept the replaced paper roll!");
                ret.RetryWithoutPrint = true;
            } else if (error.CheckError (ErrorState.LittlePaper)) {
                message = Translator.GetString ("There is a little paper in the printer. Please replace!");
                ret.RetryWithoutPrint = true;
            } else if (error.Check (ErrorState.Report24HRequired))
                message = Translator.GetString ("24 Hour report is required before you can continue working with the fiscal printer!");
            else if (error.Check (ErrorState.BadSerialPort))
                message = Translator.GetString ("There was a problem connecting with the serial port. Please check the serial port!");
            else if (error.Check (ErrorState.BadConnectionParameters))
                message = Translator.GetString ("There was a problem connecting with the device. Please check the connection parameters!");
            else if (error.Check (ErrorState.BadPassword))
                message = Translator.GetString ("The password for the device is not correct!");
            else if (error.Check (ErrorState.BadLogicalAddress))
                message = Translator.GetString ("The logical address for the device is not correct!");
            else if (error.Check (ErrorState.NotEnoughCashInTheRegister)) {
                message = Translator.GetString ("There is not enough cash in the register to return change for the payment!");
                ret.AskForPayment = true;
            } else if (error.Check (ErrorState.FiscalPrinterOverflow))
                message = Translator.GetString ("An overflow occurred while transferring data to the fiscal printer!");
            else if (error.Check (ErrorState.TooManyTransactionsInReceipt)) {
                message = Translator.GetString ("There are too many transactions in the operation for the fiscal printer to handle!");
                buttons = MessageButtons.Cancel;
            } else if (error.Check (ErrorState.VATGroupsMismatch)) {
                message = Translator.GetString ("The VAT groups defined does not match the VAT groups of the fiscal printer!");
                buttons = MessageButtons.Cancel;
            } else if (error.Check (ErrorState.EvalItemsLimitation)) {
                message = string.Format (Translator.GetString ("This is not a licensed version of {0}! Only less than 5 lines are allowed in a sale."), DataHelper.ProductName);
                buttons = MessageButtons.Cancel;
            } else if (error.Check (ErrorState.EvalPriceLimitation)) {
                message = string.Format (Translator.GetString ("This is not a licensed version of {0}! Only prices less than 10 are allowed in a sale."), DataHelper.ProductName);
                buttons = MessageButtons.Cancel;
            } else if (error.Check (ErrorState.EvalQttyLimitation)) {
                message = string.Format (Translator.GetString ("This is not a licensed version of {0}! Only quantities less than 3 are allowed in a sale."), DataHelper.ProductName);
                buttons = MessageButtons.Cancel;
            } else if (error.Check (ErrorState.EvalLimitation)) {
                message = string.Format (Translator.GetString ("This is not a licensed version of {0}! Please purchase a license to use this device."), DataHelper.ProductName);
                buttons = MessageButtons.Cancel;
            } else if (error.Check (ErrorState.DriverNotFound)) {
                message = Translator.GetString ("The device driver was not found. Please verify that it is properly installed and try again.");
                buttons = MessageButtons.Cancel;
            } else if (error.Count > 0) {
                message = Translator.GetString ("An error in a peripheral device has occurred!");
                message += "\n" + error;
            }

            using (MessageError msgDialog = new MessageError (message, ErrorSeverity.Error, ex)) {
                msgDialog.Buttons = buttons & buttonsMask;
                PresentationDomain.OnShowingDialog ();
                ret.Retry = false;
                ret.Button = MessageButtons.None;
                switch (msgDialog.Run ()) {
                    case ResponseType.Reject:
                        ret.Button |= MessageButtons.Retry;
                        ret.Retry = true;
                        break;
                    case ResponseType.Ok:
                        ret.Button |= MessageButtons.OK;
                        break;
                    case ResponseType.Cancel:
                        ret.Button |= MessageButtons.Cancel;
                        break;
                    case ResponseType.Yes:
                        ret.Button |= MessageButtons.Yes;
                        break;
                    case ResponseType.No:
                        ret.Button |= MessageButtons.No;
                        break;
                }
            }

            return ret;
        }

        #region Import / Export

        public static void ExportDocument (IDocument document, bool portrait)
        {
            ExportDocument (resProvider.GetForms (document), document.Name, document.FileName, portrait, document.FormName.CamelSpace ());
        }

        public static void ExportBarcodes (Form form, bool portrait, PrintSettings printSettings, MarginsI margins)
        {
            DateTime date = DateTime.Today;
            ExportDocument (new List<Form> { form }, "Barcodes", string.Format ("Barcodes-{0}_{1}_{2}", date.Day, date.Month, date.Year), portrait, "Barcodes", typeof (Item), false, printSettings, margins);
        }

        private static void ExportDocument (IList<Form> forms, string name, string fileName, bool portrait, string type, Type sourceObjectType = null, bool recalculateForm = true, PrintSettings printSettings = null, MarginsI margins = null)
        {
            IDocumentExporter exporter;
            string filename;
            bool? toFile;
            string email;
            string subject;

            using (ExportDocuments dialog = new ExportDocuments (sourceObjectType ?? forms [0].SourceObject.GetType (), fileName)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                exporter = dialog.DocumentExporter;
                filename = dialog.FileName;
                toFile = dialog.ToFile;
                email = dialog.Email;
                subject = dialog.EmailSubject;
            }

            BusinessDomain.FeedbackProvider.TrackEvent ("Export document", type);

            try {
                exporter.Export (filename, toFile, email, subject, name, forms, portrait, recalculateForm, printSettings, margins);
            } catch (LicenseLimitationException ex) {
                MessageError.ShowDialog (ex.Message, ErrorSeverity.Warning, ex);
            } catch (IOException ex) {
                MessageError.ShowDialog (string.Format (Translator.GetString ("Error occurred while saving export to \"{0}\". Please check if you have write permissions to that location."), filename), ErrorSeverity.Warning, ex);
            } catch (Exception ex) {
                MessageError.ShowDialog (Translator.GetString ("Error occurred while performing export."), ErrorSeverity.Warning, ex);
            }
        }

        public static void ExportData (string defaultFileName, string dataTypeName, DataExchangeSet exportData)
        {
            ExportData<object> (defaultFileName, dataTypeName, exportData);
        }

        public static void ExportData<T> (string defaultFileName, string dataTypeName, DataExchangeSet exportData)
        {
            IDataExporter exporter;
            string filename;
            bool? toFile;
            string email;
            string subject;

            using (ExportObjects<T> dialog = new ExportObjects<T> (exportData.Contents, defaultFileName)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                exporter = dialog.Exporter;
                filename = dialog.FileName;
                toFile = dialog.ToFile;
                email = dialog.Email;
                subject = dialog.EmailSubject;
            }

            BusinessDomain.FeedbackProvider.TrackEvent ("Export data", dataTypeName);

            try {
                exporter.Export (filename, toFile, email, subject, exportData);
            } catch (LicenseLimitationException ex) {
                MessageError.ShowDialog (ex.Message, ErrorSeverity.Warning, ex);
            } catch (IOException ex) {
                MessageError.ShowDialog (string.Format (Translator.GetString ("Error occurred while saving export to \"{0}\". Please check if you have write permissions to that location."), filename), ErrorSeverity.Warning, ex);
            } catch (Exception ex) {
                MessageError.ShowDialog (Translator.GetString ("Error occurred while performing export."), ErrorSeverity.Warning, ex);
            }
        }

        public static void ImportData<T> (EventHandler<ValidateEventArgs> validateCallback = null, EventHandler<ImportEventArgs> commitCallback = null, bool usesLocation = false) where T : IStrongEntity, IPersistableEntity<T>
        {
            Dictionary<int, bool> responses = new Dictionary<int, bool> ();
            using (MessageProgress progress = new MessageProgress (Translator.GetString ("Importing..."), "Icons.Import24.png", Translator.GetString ("Importing in progress..."))) {
                bool cancelImport = false;
                progress.Response += delegate { cancelImport = true; };
                progress.CustomProgressText = true;

                StateHolder state = new StateHolder ();
                state ["responses"] = responses;
                state ["messageProgress"] = progress;

                ImportData (delegate (T entity, long? locationId, int current, int total, out bool cancel)
                    {
                        progress.Show ();
                        progress.Progress = ((double) current * 100) / (total - 1);
                        progress.ProgressText = string.Format (Translator.GetString ("{0} of {1}"), current + 1, total);
                        PresentationDomain.ProcessUIEvents ();
                        cancel = cancelImport;
                        if (validateCallback != null) {
                            ValidateEventArgs args = new ValidateEventArgs (InteractiveValidationCallback, state);
                            validateCallback (entity, args);
                            if (!args.IsValid)
                                return;
                        }

                        if (!entity.Validate (InteractiveValidationCallback, state))
                            return;

                        entity.CommitChanges ();

                        if (commitCallback != null)
                            commitCallback (entity, new ImportEventArgs (locationId));
                    }, usesLocation);
            }
        }

        public static bool InteractiveValidationCallback (string message, ErrorSeverity severity, int code, StateHolder state)
        {
            Dictionary<int, bool> responses = (Dictionary<int, bool>) state ["responses"];
            MessageProgress messageProgress = null;
            if (state.ContainsKey ("messageProgress"))
                messageProgress = (MessageProgress) state ["messageProgress"];

            if (responses.ContainsKey (code)) {
                return severity == ErrorSeverity.Warning && responses [code];
            }

            try {
                if (messageProgress != null)
                    messageProgress.Hide ();
                using (MessageError dlgError = new MessageError (message, severity)) {
                    if (severity == ErrorSeverity.Warning) {
                        dlgError.Buttons = MessageButtons.YesNo | MessageButtons.Remember;
                        ResponseType resp = dlgError.Run ();
                        if (dlgError.RememberChoice)
                            responses [code] = resp == ResponseType.Yes;

                        return (resp == ResponseType.Yes);
                    } else {
                        dlgError.Buttons = MessageButtons.OK | MessageButtons.Remember;
                        dlgError.Run ();
                        if (dlgError.RememberChoice)
                            responses [code] = true;
                        return false;
                    }
                }
            } finally {
                if (messageProgress != null)
                    messageProgress.Show ();
            }
        }

        public static bool SilentValidationCallback (string message, ErrorSeverity severity, int code, StateHolder state)
        {
            return severity < ErrorSeverity.Error;
        }

        private static void ImportData<T> (ImporterCallback<T> callback, bool usesLocation)
        {
            IDataImporter importer;
            string filename;
            long? locationId;

            BusinessDomain.FeedbackProvider.TrackEvent ("Import data", typeof (T).Name.CamelSpace ());

            using (ImportObjects<T> dialog = new ImportObjects<T> (usesLocation)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                importer = dialog.Importer;
                filename = dialog.FileName;
                locationId = dialog.Location != null ? dialog.Location.Id : (long?) null;
            }

            if (importer.UsesFile && !File.Exists (filename)) {
                MessageError.ShowDialog (Translator.GetString ("Please select a valid source file to import!"));
                return;
            }

            try {
                PropertyMap propertyMap;
                using (ImportObjectsMapping<T> dialog = new ImportObjectsMapping<T> (importer, filename)) {
                    if (dialog.Run () != ResponseType.Ok)
                        return;

                    propertyMap = dialog.PropertyMap;
                }

                ExchangeHelper.ImportObjects (importer, filename, propertyMap, locationId, callback);
                MessageError.ShowDialog (Translator.GetString ("Import completed successfully."), ErrorSeverity.Information);
            } catch (LicenseLimitationException ex) {
                MessageError.ShowDialog (ex.Message, ErrorSeverity.Warning, ex);
            } catch (IOException ex) {
                MessageError.ShowDialog (Translator.GetString ("The selected source file is in use by another application! Please close all other applications and try again."), ErrorSeverity.Warning, ex);
            } catch (Exception ex) {
                MessageError.ShowDialog (Translator.GetString ("Error occurred while performing import."), ErrorSeverity.Warning, ex);
            }
        }

        #endregion
    }
}
