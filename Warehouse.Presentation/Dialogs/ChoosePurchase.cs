//
// ChoosePurchase.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/24/2006
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
using Warehouse.Business;
using Warehouse.Business.Operations;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Presentation.Preview;

namespace Warehouse.Presentation.Dialogs
{
    public class ChoosePurchase : ChooseOperation<Purchase>
    {
        public ChoosePurchase (bool showInvoiceInfo, DocumentChoiceType choiceType)
            : base (showInvoiceInfo, choiceType)
        {
        }

        protected override string GetIconName ()
        {
            return "Icons.Purchase32.png";
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChoose.Title = Translator.GetString ("Purchases - Select Document");
        }

        protected override void GetEntities ()
        {
            DataQuery filter = GetDateFilter ();

            // Skip pending and draft documents
            if (choiceType == DocumentChoiceType.CreateChildDocument)
                filter.Filters.Add (new DataFilter (DataFilterLogic.Greather, DataField.OperationNumber) { Values = new object [] { 0 } });

            entities = Purchase.GetAll (filter);
            base.GetEntities ();
            entities.FilterProperties.Add ("PartnerName");
        }

        protected override Dictionary<long, long> GetHighlightedEntityIds ()
        {
            return Purchase.GetAllIdsWithInvoices ();
        }

        protected override bool GetPreviewVisible ()
        {
            return BusinessDomain.AppConfiguration.ShowPurchasesPreview;
        }

        protected override void SetPreviewVisible (bool visible)
        {
            BusinessDomain.AppConfiguration.ShowPurchasesPreview = visible;
        }

        protected override PreviewOperation GetPreviewWidget ()
        {
            return new PreviewPurchase ();
        }

        protected override void AddPartnerColumn (ColumnController cc)
        {
            cc.Add (new Column (Translator.GetString ("Partner"), "PartnerName", 0.2, "PartnerName") { MinWidth = 70 });
        }

        protected override void btnOK_Clicked (object o, EventArgs args)
        {
            bool allowView;
            if (!CheckForInvoice (false, out allowView) && !allowView)
                return;

            readOnlyView = allowView;
            base.btnOK_Clicked (o, args);
        }

        protected override void btnAnnul_Clicked (object o, EventArgs args)
        {
            bool allowView;
            if (!CheckForInvoice (true, out allowView) && !allowView)
                return;

            readOnlyView = allowView;
            base.btnAnnul_Clicked (o, args);
        }

        private bool CheckForInvoice (bool annulling, out bool allowView)
        {
            allowView = false;
            // multiple selection allowed only when creating a document (for example, an invoice) so no error in other cases
            if (SelectedItems.Select (s => s.PartnerId).Distinct ().Count () > 1) {
                MessageError.ShowDialog (Translator.GetString ("You can create a document for multiple operations only when they use the same partner."), ErrorSeverity.Error);
                grid.GrabFocus ();
                return false;
            }

            if (!annulling && (choiceType == DocumentChoiceType.Choose || choiceType == DocumentChoiceType.Print))
                return true;

            Purchase purchase = SelectedItem;
            if (purchase == null)
                return false;

            long invoiceNumber;
            if (!HighlightedEntityIds.TryGetValue (purchase.Id, out invoiceNumber))
                return true;

            if (FormHelper.ShowMessageHasInvoice (purchase, invoiceNumber, "Icons.Purchase16.png", out allowView, annulling, choiceType))
                RefreshEntities ();

            return false;
        }
    }
}
