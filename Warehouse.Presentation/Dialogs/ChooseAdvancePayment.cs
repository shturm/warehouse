// 
// ChooseAdvancePayment.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
// 
// Created:
//   26.04.2010
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
using Warehouse.Presentation.Reporting;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseAdvancePayment : Choose<Payment>
    {
        public ChooseAdvancePayment (DocumentChoiceType choiceType)
            : base (choiceType)
        {
            Initialize ();
        }

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeGrid ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChoose.Title = string.Format (Translator.GetString ("Advance Payments - Select Document"));
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();

            grid.HeightRequest = 300;
            grid.WidthRequest = 500;

            GetEntities ();

            grid.Model = entities;

            ColumnController columnController = new ColumnController ();

            CellTextNumber cellTextNumber = new CellTextNumber ("Id") { FixedDigits = BusinessDomain.AppConfiguration.DocumentNumberLength };
            string documentNumber = Translator.GetString ("Payment No.");
            columnController.Add (new Column (documentNumber, cellTextNumber, 1, cellTextNumber.PropertyName) { MinWidth = 70 });

            string partner = Translator.GetString ("Partner");
            columnController.Add (new Column (partner, "PartnerName", 2, "PartnerName") { MinWidth = 100 });

            CellTextCurrency cellTextDouble = new CellTextCurrency ("Quantity");
            string balance = Translator.GetString ("Balance");
            columnController.Add (new Column (balance, cellTextDouble, 1, cellTextDouble.PropertyName) { MinWidth = 70 });

            grid.ColumnController = columnController;
        }

        protected override ReportFilterDateRange GetDateRangeFilter ()
        {
            return new ReportFilterDateRange (true, true, DataFilterLabel.PaymentDate, DataField.PaymentDate);
        }

        protected override void GetEntities ()
        {
            entities = Payment.GetAdvances (GetDateFilter ());
            base.GetEntities ();
        }
    }
}
