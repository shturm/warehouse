//
// TransferDetail.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   07/02/2006
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

namespace Warehouse.Business.Operations
{
    public class TransferDetail : PriceInOperationDetail
    {
        private long sourceDetailId = -1;
        private long targetDetailId = -1;

        public long SourceDetailId
        {
            get { return sourceDetailId; }
            set { sourceDetailId = value; }
        }

        public long TargetDetailId
        {
            get { return targetDetailId; }
            set { targetDetailId = value; }
        }

        public TransferDetail ()
        {
            sign = 1;
            lotId = BusinessDomain.AppConfiguration.ItemsManagementUseLots ? -1 : 1;
        }

        public override bool UsesSavedLots
        {
            get { return true; }
        }

        protected override bool GetUsePriceIn ()
        {
            return usePriceIn ?? !BusinessDomain.LoggedUser.HideItemsPurchasePrice;
        }
    }
}