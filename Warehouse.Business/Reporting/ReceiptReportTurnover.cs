// 
// ReceiptReportTurnover.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at yahoo dot com>
// 
// Created:
//    29.01.2013
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
using Warehouse.Business.Entities;
using Warehouse.Data;

namespace Warehouse.Business.Reporting
{
    public class ReceiptReportTurnover : ReceiptReport
    {
        public override bool AllowOperatorsManage ()
        {
            return BusinessDomain.LoggedUser.UserLevel > UserAccessLevel.Operator;
        }

        public override string Title
        {
            get { return Translator.GetString ("Turnover"); }
        }

        public override DataFilterLabel DateLabel
        {
            get { return DataFilterLabel.PaymentDate; }
        }

        public override DataField DateField
        {
            get { return DataField.PaymentTimeStamp; }
        }

        public override DataFilterLabel UserLabel
        {
            get { return DataFilterLabel.OperationsOperatorName; }
        }

        public override DataField UserField
        {
            get { return DataField.UserId; }
        }

        protected override DataQueryResult ExecuteReport ()
        {
            return BusinessDomain.DataAccessProvider.ReportTurnover (GetDataQuery ());
        }

        protected override IList<DataField> GetReportFields ()
        {
            return new List<DataField>
                {
                    DataField.UserName,
                    DataField.PaymentTypesName,
                    DataField.PaymentAmount
                };
        }
    }
}
