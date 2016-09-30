//
// ChooseIssuedInvoice.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
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

using System.Collections.Generic;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseIssuedInvoice : ChooseInvoice
    {
        public ChooseIssuedInvoice (DocumentChoiceType choiceType)
            : base (choiceType)
        {
        }

        #region Initialization steps

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            switch (choiceType) {
                case DocumentChoiceType.Annul:
                    dlgChoose.Title = Translator.GetString ("Void Issued Invoice - Select Document");
                    break;
                case DocumentChoiceType.Print:
                    dlgChoose.Title = Translator.GetString ("Issued Invoice - Select Document");
                    break;
                default:
                    dlgChoose.Title = Translator.GetString ("Invoices - Select Document");
                    break;
            }
        }

        protected override void GetEntities ()
        {
            entities = Invoice.GetAllIssued (GetDateFilter ());
            entities.FilterProperties.Add ("NumberString");
            entities.FilterProperties.Add ("RecipientName");
            entities.FilterProperties.Add ("Location");
            entities.Sort ("NumberString", SortDirection.Descending);

            BusinessDomain.DocumentQueryStates [queryStateKey] = GetDateFilter ();
        }

        protected override void AnnulOperation ()
        {
            Invoice invoice = SelectedItem;
            KeyValuePair<int, OperationType> [] operations = null;
            if (BusinessDomain.AppConfiguration.AutoCreateInvoiceOnSale) {
                if (Message.ShowDialog (Translator.GetString ("Annul Sales Too?"), null,
                    Translator.GetString ("Do you want to annul the sales for that invoice too?"), "Icons.Question32.png",
                    MessageButtons.YesNo) == ResponseType.Yes) {
                    operations = Invoice.GetOperationsByIssuedNumber (invoice.Number);
                }
            }

            invoice.Annul ();

            if (operations == null)
                return;

            foreach (var operation in operations) {
                Operation oper = Operation.GetById (operation.Value, operation.Key);
                if (oper != null)
                    oper.Annul ();
            }
        }

        #endregion
    }
}
