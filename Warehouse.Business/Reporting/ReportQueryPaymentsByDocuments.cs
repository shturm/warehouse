//
// ReportQueryPaymentsByDocuments.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   09.30.2010
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

using Warehouse.Data;

namespace Warehouse.Business.Reporting
{
    public class ReportQueryPaymentsByDocuments : ReportQueryBase
    {
        public ReportQueryPaymentsByDocuments ()
        {
            Name = Translator.GetString ("Payments by Documents");

            AppendFilter (new FilterRange (true, true, DataFilterLabel.OperationNumber, DataField.PaymentOperationId));
            AppendFilter (new FilterDateRange (false, true, DataFilterLabel.PaymentDate, DataField.PaymentDate));
            AppendFilter (new FilterFind (false, true, DataFilterLabel.PartnerName, DataField.PartnerName));
            AppendFilter (new FilterGroupFind (false, true, DataFilterLabel.PartnersGroupsName, DataField.PartnersGroupsName));
            AppendFilter (new FilterFind (false, true, DataFilterLabel.LocationName, DataField.LocationName));
            AppendFilter (new FilterGroupFind (false, true, DataFilterLabel.LocationsGroupName, DataField.LocationsGroupsName));
            AppendFilter (new FilterFind (false, true, DataFilterLabel.UserName, DataField.UserName));
            AppendFilter (new FilterGroupFind (false, true, DataFilterLabel.UsersGroupsName, DataField.UsersGroupsName));
            AppendFilterForPayableOperations (true);
            AppendFilter (new FilterEmpty (false, true, DataFilterLabel.PaymentTypeId, DataField.PaymentsInCash, DataField.PaymentsByBankOrder, DataField.PaymentsByDebitCreditCard, DataField.PaymentsByVoucher));

            AppendOrder (new Order (
                DataField.PaymentOperationId,
                DataField.PaymentDate,
                DataField.PartnerName,
                DataField.LocationName,
                DataField.PaymentOperationType));
        }

        #region Overrides of ReportQueryBase

        public override DataQueryResult ExecuteReport (DataQuery dataQuery)
        {
            return BusinessDomain.DataAccessProvider.ReportPaymentsByDocuments (dataQuery);
        }

        #endregion
    }
}
