//
// Waste.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/18/2006
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
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Operations
{
    public class Waste : Operation<WasteDetail>
    {
        public override PriceType TotalsPriceType
        {
            get { return BusinessDomain.LoggedUser.HideItemsPurchasePrice ? PriceType.SaleTotal : PriceType.PurchaseTotal; }
        }
        
        public Waste ()
        {
            operationType = OperationType.Waste;
        }

        public override void Commit ()
        {
            bool editMode = true;
            using (DbTransaction transaction = new DbTransaction (BusinessDomain.DataAccessProvider)) {
                transaction.SnapshotObject (this);
                // Create a new operation Id if needed);
                OperationState operationState = State;
                if (operationState == OperationState.New || operationState == OperationState.NewDraft || operationState == OperationState.NewPending) {
                    id = CreateNewId ();
                    editMode = false;
                }

                LotsEvaluate (Details);
                BusinessDomain.DataAccessProvider.AddUpdateWaste (this, details.ToArray (), BusinessDomain.AppConfiguration.AllowNegativeAvailability);

                if (editMode && Total.IsZero ()) {
                    BusinessDomain.DataAccessProvider.DeleteOperationId (Data.OperationType.Waste, id);

                    ApplicationLogEntry.AddNew (string.Format (Translator.GetString ("Void waste No.{0} from {1}"), GetFormattedOperationNumber (id), BusinessDomain.GetFormattedDate (date)));
                } else if (editMode) {
                    ApplicationLogEntry.AddNew (string.Format (Translator.GetString ("Edit waste No.{0} from {1}"), GetFormattedOperationNumber (id), BusinessDomain.GetFormattedDate (date)));
                }

                if (editMode)
                    RemoveAllEmptyDetails ();
                
                transaction.Complete ();
            }
        }

        public static LazyListModel<Waste> GetAll (DataQuery dataQuery)
        {
            AddPartnerLocationFilters (ref dataQuery, false);
            return BusinessDomain.DataAccessProvider.GetAllWastes<Waste> (dataQuery);
        }

        public static Waste GetById (long wasteId)
        {
            Waste waste = BusinessDomain.DataAccessProvider.GetOperationById<Waste> (OperationType.Waste, wasteId);
            if (waste != null) {
                waste.LoadDetails ();
                waste.IsDirty = false;
            }

            return waste;
        }

        public static Waste GetPending (long pId, long lId)
        {
            Waste waste = BusinessDomain.DataAccessProvider.GetPendingOperation<Waste> (OperationType.Waste, pId, lId);
            if (waste != null) {
                waste.LoadDetails ();
                waste.IsDirty = false;
            }

            return waste;
        }

        public override void LoadDetails ()
        {
            LoadDetails (!BusinessDomain.LoggedUser.HideItemsPurchasePrice);
        }
    }
}