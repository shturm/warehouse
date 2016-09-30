// 
// AccountingFilters.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at yahoo dot com>
// 
// Created:
//    31.05.2012
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
using Warehouse.Data;

namespace Warehouse.Business
{
    public class AccountingFilters
    {
        public IEnumerable<DataFilter> ReceivedInvoicesFilter { get; set; }
        public IEnumerable<DataFilter> IssuedInvoicesFilter { get; set; }
        public IEnumerable<DataFilter> SalesFilter { get; set; }
        public IEnumerable<DataFilter> CashReportFilter { get; set; }
        public IEnumerable<DataFilter> CreditNotesFilter { get; set; }
        public IEnumerable<DataFilter> DebitNotesFilter { get; set; }
        public bool ExportPayments { get; set; }
        public bool CashReport { get; set; }
    }
}
