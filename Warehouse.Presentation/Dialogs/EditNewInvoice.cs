//
// EditNewInvoice.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/21/2006
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

using System.Collections.Generic;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using System.Linq;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNewInvoice : EditNewDocument<InvoiceDetail>
    {
        public override string HelpFile
        {
            get { return sale != null ? "NewInvoice.html" : "NewReceivedInvoice.html"; }
        }

        protected override bool CheckDuplicateNumbers
        {
            get { return sale != null; }
        }

        protected override bool IsEditable
        {
            get { return operations.Count > 0; }
        }

        public EditNewInvoice (Sale sale)
            : this (new List<Sale> { sale })
        {
        }

        public EditNewInvoice (IList<Sale> sales)
            : base (new Invoice (sales), sales)
        {
            IsPrintable = true;
        }

        public EditNewInvoice (Purchase purchase)
            : this (new List<Purchase> { purchase })
        {
        }

        public EditNewInvoice (IList<Purchase> purchases)
            : base (new Invoice (purchases), purchases)
        {
        }

        public EditNewInvoice (Invoice invoice)
            : base (invoice)
        {
            IsPrintable = true;
        }

        protected override Image GetDialogIcon ()
        {
            return FormHelper.LoadImage ("Icons.Invoice16.png");
        }

        protected override void SetDialogTitle ()
        {
            if (IsEditable)
                DialogControl.Title = sale != null ?
                    Translator.GetString ("Issue Invoice") :
                    Translator.GetString ("Receive Invoice");
            else
                DialogControl.Title = Translator.GetString ("Reprint Invoice");
        }

        protected override string GetErrorWhileGeneratingMessage ()
        {
            return Translator.GetString ("An error occurred while generating invoice!");
        }

        protected virtual string GetDocumentTemplateName ()
        {
            return "Invoice";
        }

        protected override bool SaveDocument (FinalizeAction action = FinalizeAction.CommitDocument)
        {
            BusinessDomain.FeedbackProvider.TrackEvent ("Invoice", action == FinalizeAction.CommitDocument ? "New" : "Reprint");

            return base.SaveDocument (action);
        }
    }
}
