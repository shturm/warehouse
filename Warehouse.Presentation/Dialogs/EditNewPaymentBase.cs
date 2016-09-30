//
// EditNewPaymentBase.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   01.06.2014
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
using System.Linq;
using Warehouse.Business;
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Data.DataBinding;

namespace Warehouse.Presentation.Dialogs
{
    public abstract class EditNewPaymentBase : DialogBase
    {
        public abstract bool PrintFiscal
        {
            get;
        }

        public abstract bool PrintDocument
        {
            get;
        }

        public void PrintPayments (IList<Payment> payments, Operation operation = null)
        {
            if (PrintFiscal)
                PrintFiscalReceipt (operation != null ? (Sale) operation : GetFakeSale (payments [0]), payments);

            if (!PrintDocument)
                return;

            if (payments.Count > 0)
                DialogControl.Hide ();

            PrintPaymentDocument (payments);
        }

        private void PrintFiscalReceipt (Sale sale, IEnumerable<Payment> payments)
        {
            FinalizeOperationOptions options = new FinalizeOperationOptions ();
            options.CashReceiptSale = sale;
            options.FinalPayments = new BindList<Payment> (payments);
            options.Action = FinalizeAction.PrintCashReceipt | FinalizeAction.CollectSaleData;

            try {
                BusinessDomain.DeviceManager.FinalizeOperation (options);
            } catch (HardwareErrorException ex) {
                FormHelper.ShowHardwareErrorMessage (ex, MessageButtons.All);
            }
        }

        private void PrintPaymentDocument (IEnumerable<Payment> payments)
        {
            foreach (PaymentReceipt paymentReceipt in payments.Select (payment => new PaymentReceipt (payment))) {
                FormHelper.PrintPreviewObject (paymentReceipt);
            }
        }

        private static Sale GetFakeSale (Payment payment)
        {
            return new Sale
            {
                PartnerId = payment.PartnerId,
                PartnerName2 = payment.PartnerName,
                LocationId = payment.LocationId,
                Location2 = payment.LocationName,
                UserId = payment.UserId,
                UserName2 = BusinessDomain.LoggedUser.Name
            };
        }
    }
}
