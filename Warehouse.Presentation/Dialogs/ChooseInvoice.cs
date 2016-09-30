//
// ChooseInvoice.cs
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

using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Presentation.Preview;
using Warehouse.Presentation.Reporting;

namespace Warehouse.Presentation.Dialogs
{
    public abstract class ChooseInvoice : ChooseOperationBase<Invoice>
    {
        public override long? SelectedItemId
        {
            get
            {
                if (grid.FocusedRow < 0 || grid.FocusedRow >= entities.Count)
                    return null;

                return (long) entities [grid.FocusedRow, "Number"];
            }
        }

        protected override string GetIconName ()
        {
            return "Icons.Invoice32.png";
        }

        public override string HelpFile
        {
            get { return "ChooseInvoice.html"; }
        }

        protected ChooseInvoice (DocumentChoiceType choiceType)
            : base (choiceType)
        {
        }

        #region Initialization steps

        protected override ReportFilterDateRange GetDateRangeFilter ()
        {
            return new ReportFilterDateRange (true, true, DataFilterLabel.InvoiceDate, DataField.DocumentDate);
        }

        protected override bool GetPreviewVisible ()
        {
            return BusinessDomain.AppConfiguration.ShowInvoicesPreview;
        }

        protected override void SetPreviewVisible (bool visible)
        {
            BusinessDomain.AppConfiguration.ShowInvoicesPreview = visible;
        }

        protected override PreviewOperation GetPreviewWidget ()
        {
            return new PreviewInvoice ();
        }

        protected override void AddNumberColumn (ColumnController cc)
        {
            CellTextNumber ctn = new CellTextNumber ("Number") { FixedDigits = BusinessDomain.AppConfiguration.DocumentNumberLength };
            cc.Add (new Column (Translator.GetString ("Document No."), ctn, 0.1, "NumberString") { MinWidth = 90 });
        }

        protected override void AddPartnerColumn (ColumnController cc)
        {
            cc.Add (new Column (Translator.GetString ("Partner"), "RecipientName", 0.2, "RecipientName") { MinWidth = 70 });
        }

        protected override string GetAnnulTitle ()
        {
            return Translator.GetString ("Annul Invoice");
        }

        protected override string GetAnnulQuestion ()
        {
            return Translator.GetString ("Are you sure you want to annul this invoice?");
        }

        #endregion
    }
}
