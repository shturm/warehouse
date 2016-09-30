//
// IDataExtender.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   02/13/2009
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
using Warehouse.Business.Operations;
using Warehouse.Data;

namespace Warehouse.Business
{
    public interface IDataExtender
    {
        void Initialize (DataProvider provider);
        void Deinitialize ();
        bool CanUseLocationInOperation (OperationType operationType, long locationId, out string error, out string warning);
        bool WaitForPendingOperationToComplete (Operation operation, int msecs);
        bool WaitForPendingOperationToComplete (long detailId, int msecs);
        bool CanEditOperation (object operation, out string message, out bool readOnlyView);
        bool CanAnnulOperation (object operation, out string error, out string warning);
        void RequestOperationAnnul (object operation);
        bool CanEditPayment (object payment, out string message);
        bool CanIncreaseStoreAvailabilityOnTransfer (object transfer);
        bool CanIncreaseStoreAvailabilityOnTransferAnnull (object transfer);
        DateTime GetDatabaseLastUpdate ();
    }
}
