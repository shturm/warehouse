//
// FinalizeOperationOptions.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   02/26/2009
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
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Data.DataBinding;

namespace Warehouse.Business
{
    public class FinalizeOperationOptions : ICloneable
    {
        private Sale sale;
        private Sale deltaSale;
        private string title;

        private Sale kitchenSale;
        private Sale kitchenDeltaSale;
        private string kitchenTitle;

        private Sale customerOrderSale;
        private Sale customerOrderDeltaSale;
        private string customerOrderTitle;

        private Sale cashReceiptSale;
        private string cashReceiptTitle;

        private BindList<Payment> finalPayments;
        private readonly BindList<Payment> editedAdvancePayments = new BindList<Payment> ();

        public Sale Sale
        {
            get { return sale; }
            set { sale = value; }
        }

        public Sale DeltaSale
        {
            get { return deltaSale; }
            set { deltaSale = value; }
        }

        public string Title
        {
            get { return title ?? string.Empty; }
            set { title = value; }
        }

        public Sale KitchenSale
        {
            get { return kitchenSale ?? sale; }
            set { kitchenSale = value; }
        }

        public Sale KitchenDeltaSale
        {
            get { return kitchenDeltaSale ?? deltaSale; }
            set { kitchenDeltaSale = value; }
        }

        public string KitchenTitle
        {
            get { return kitchenTitle ?? Title; }
            set { kitchenTitle = value; }
        }

        public Sale CustomerOrderSale
        {
            get { return customerOrderSale ?? sale; }
            set { customerOrderSale = value; }
        }

        public Sale CustomerOrderDeltaSale
        {
            get { return customerOrderDeltaSale ?? deltaSale; }
            set { customerOrderDeltaSale = value; }
        }

        public string CustomerOrderTitle
        {
            get { return customerOrderTitle ?? Title; }
            set { customerOrderTitle = value; }
        }

        public Invoice CustomerOrderInvoice { get; set; }

        public Sale CashReceiptSale
        {
            get { return cashReceiptSale ?? sale; }
            set { cashReceiptSale = value; }
        }

        public Invoice CashReceiptInvoice { get; set; }

        public RestaurantOrder RestaurantOrder { get; set; }

        public long? RestaurantOrderMainLocation { get; set; }

        public bool RestaurantOrderCheckAvailability { get; set; }

        public object Document { get; set; }

        public FinalizeAction Action { get; set; }

        public bool SkipPrintingNotes { get; set; }

        public bool SilentMode { get; set; }

        public Action OnSaleCommitted { get; set; }

        public BindList<Payment> FinalPayments
        {
            get { return finalPayments ?? (CashReceiptSale != null ? CashReceiptSale.Payments : null); }
            set { finalPayments = value; }
        }

        public BindList<Payment> EditedAdvancePayments
        {
            get { return editedAdvancePayments; }
        }

        public object Clone ()
        {
            return MemberwiseClone ();
        }
    }
}
