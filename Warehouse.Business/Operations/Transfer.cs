//
// Transfer.cs
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
using System.Collections.Generic;
using System.Linq;
using Warehouse.Business.Entities;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Operations
{
    public class Transfer : Operation<TransferDetail>
    {
        #region Public properties

        public override PriceType TotalsPriceType
        {
            get { return GetUsePriceIn () ? PriceType.PurchaseTotal : PriceType.SaleTotal; }
        }

        private long sourceLocationId = -1;
        public long SourceLocationId
        {
            get { return sourceLocationId; }
            set
            {
                sourceLocationId = value;
                LocationId = value;
            }
        }

        private string sourceLocation = string.Empty;
        public string SourceLocation
        {
            get { return sourceLocation; }
            set { sourceLocation = value; }
        }

        private string sourceLocation2 = string.Empty;
        public string SourceLocation2
        {
            get { return string.IsNullOrWhiteSpace (sourceLocation2) ? sourceLocation : sourceLocation2; }
            set { sourceLocation2 = value; }
        }

        private long targetLocationId = -1;
        public long TargetLocationId
        {
            get { return targetLocationId; }
            set { targetLocationId = value; }
        }

        private string targetLocation = string.Empty;
        public string TargetLocation
        {
            get { return targetLocation; }
            set { targetLocation = value; }
        }

        private string targetLocation2 = string.Empty;
        public string TargetLocation2
        {
            get { return string.IsNullOrWhiteSpace (targetLocation2) ? targetLocation : targetLocation2; }
            set { targetLocation2 = value; }
        }

        #endregion

        public Transfer ()
        {
            operationType = OperationType.TransferIn;
        }

        public override void Commit ()
        {
            CommitChanges (
                BusinessDomain.CanIncreaseStoreAvailabilityOnTransfer (this),
                BusinessDomain.CanIncreaseStoreAvailabilityOnTransferAnnull (this));
        }

        public void CommitChanges (bool increaseStoreAvailability = true, bool increaseStoreAvailabilityOnAnnull = true)
        {
            bool editMode = true;

            try {
                using (DbTransaction transaction = new DbTransaction (BusinessDomain.DataAccessProvider)) {
                    transaction.SnapshotObject (this);
                    if (BusinessDomain.AppConfiguration.AutoProduction)
                        AutomaticProduction ();

                    // Create a new operation Id if needed);
                    OperationState operationState = State;
                    if (operationState == OperationState.New || operationState == OperationState.NewDraft || operationState == OperationState.NewPending) {
                        id = CreateNewId ();
                        editMode = false;
                    }

                    LotsEvaluate (Details);
                    // Save the output transfer in the database
                    OperationType = OperationType.TransferOut;
                    LocationId = SourceLocationId;
                    bool annulling = editMode && Total.IsZero ();
                    foreach (TransferDetail detail in details) {
                        detail.Sign = annulling && !increaseStoreAvailabilityOnAnnull ? 0 : -1;
                        detail.DetailId = detail.SourceDetailId;
                    }
                    BusinessDomain.DataAccessProvider.AddUpdateTransferOut (this, details.ToArray (), BusinessDomain.AppConfiguration.AllowNegativeAvailability);
                    foreach (TransferDetail detail in details)
                        detail.SourceDetailId = detail.DetailId;

                    // Save the input transfer in the database
                    OperationType = OperationType.TransferIn;
                    LocationId = TargetLocationId;
                    foreach (TransferDetail detail in details) {
                        detail.Sign = 1;
                        detail.DetailId = detail.TargetDetailId;
                    }
                    BusinessDomain.DataAccessProvider.AddUpdateTransferIn (this, details.ToArray (), BusinessDomain.AppConfiguration.AllowNegativeAvailability, increaseStoreAvailability);
                    foreach (TransferDetail detail in details)
                        detail.TargetDetailId = detail.DetailId;
                    InvalidateItemsCache ();

                    if (annulling) {
                        BusinessDomain.DataAccessProvider.DeleteOperationId (OperationType.TransferIn, id);

                        ApplicationLogEntry.AddNew (string.Format (Translator.GetString ("Void transfer No.{0} from {1}"), GetFormattedOperationNumber (id), BusinessDomain.GetFormattedDate (date)));
                    } else if (editMode) {
                        ApplicationLogEntry.AddNew (string.Format (Translator.GetString ("Edit transfer No.{0} from {1}"), GetFormattedOperationNumber (id), BusinessDomain.GetFormattedDate (date)));
                    }

                    if (editMode)
                        RemoveAllEmptyDetails ();
                    
                    transaction.Complete ();
                }
            } finally {
                LocationId = SourceLocationId;
            }
        }

        public static LazyListModel<Transfer> GetAll (DataQuery dataQuery)
        {
            AddPartnerLocationFilters (ref dataQuery, false);
            return BusinessDomain.DataAccessProvider.GetAllTransfers<Transfer> (dataQuery);
        }

        public static Transfer GetById (long transferId)
        {
            Transfer tOut = BusinessDomain.DataAccessProvider.GetOperationById<Transfer> (OperationType.TransferOut, transferId);
            if (tOut != null) {
                try {
                    tOut.LoadDetails ();
                } catch (ArgumentException) {
                    return null;
                }
                tOut.IsDirty = false;
            }

            return tOut;
        }

        public static Transfer GetPending (long pId, long lId)
        {
            Transfer tOut = BusinessDomain.DataAccessProvider.GetPendingOperation<Transfer> (OperationType.TransferOut, pId, lId);
            if (tOut != null) {
                try {
                    tOut.LoadDetails ();
                } catch (ArgumentException) {
                    return null;
                }
                tOut.IsDirty = false;
            }

            return tOut;
        }

        public override void LoadDetails ()
        {
            LoadDetails (!BusinessDomain.LoggedUser.HideItemsPurchasePrice);
        }

        /// <summary>
        /// Creates a new ID before the <see cref="Operation"/> is inserted into a storage.
        /// </summary>
        /// <returns></returns>
        protected override long CreateNewId ()
        {
            long locId = BusinessDomain.AppConfiguration.DocumentNumbersPerLocation ? sourceLocationId : 0;
            return BusinessDomain.DataAccessProvider.CreateNewOperationId (OperationType.TransferOut, locId, State);
        }

        protected override void LoadDetails (bool usePriceIn)
        {
            sourceLocationId = locationId;
            sourceLocation = location;

            if (State == OperationState.Pending)
                targetLocationId = -1;
            else {
                Transfer tIn = BusinessDomain.DataAccessProvider.GetOperationById<Transfer> (OperationType.TransferIn, id);
                if (tIn == null)
                    throw new ArgumentException (string.Format ("No transfer with the specified ID {0} found.", id));

                targetLocationId = tIn.locationId;
                targetLocation = tIn.location;
            }

            ConfigurationHolderBase config = BusinessDomain.AppConfiguration;
            details.Clear ();
            details.AddRange (BusinessDomain.DataAccessProvider.GetOperationDetailsById<TransferDetail> (OperationType.TransferIn, id, null, null, date, userId, loggedUserId, config.CurrencyPrecision, config.RoundedPrices, usePriceIn));
            List<TransferDetail> detailsOut = BusinessDomain.DataAccessProvider.GetOperationDetailsById<TransferDetail> (OperationType.TransferOut, id, null, sourceLocationId, date, userId, loggedUserId, config.CurrencyPrecision, config.RoundedPrices, usePriceIn).ToList ();
            for (int i = details.Count - 1; i >= 0; i--) {
                var detail = details [i];
                if (targetLocationId > 0 && detail.LocationId != targetLocationId) {
                    details.RemoveAt (i);
                    continue;
                }

                var matches = detailsOut.Where (d =>
                    d.ItemId == detail.ItemId &&
                    d.LotId == detail.LotId &&
                    d.Quantity.IsEqualTo (detail.Quantity) &&
                    d.PriceIn.IsEqualTo (detail.PriceIn) &&
                    d.PriceOut.IsEqualTo (detail.PriceOut)).ToList ();

                // Find the matching out detail with the closest timestamp
                TransferDetail matchingOut = null;
                double minDiff = double.MaxValue;

                for (int j = matches.Count - 1; j >= 0; j--) {
                    TransferDetail thisMatch = matches [j];
                    double diff = Math.Abs ((thisMatch.TimeStamp - detail.TimeStamp).TotalMilliseconds);

                    if (matchingOut == null) {
                        matchingOut = thisMatch;
                        minDiff = diff;
                        continue;
                    }

                    if (diff < minDiff) {
                        matchingOut = thisMatch;
                        minDiff = diff;
                    }
                }

                if (matchingOut == null) {
                    details.RemoveAt (i);
                    continue;
                }

                // Remove the match from next searches
                detailsOut.Remove (matchingOut);

                referenceDocumentId = detail.ReferenceDocumentId;
                detail.TargetDetailId = detail.DetailId;
                detail.SourceDetailId = matchingOut.DetailId;
                detail.IsDirty = false;

                if (targetLocationId < 0) {
                    targetLocationId = detail.LocationId;
                    targetLocation = Entities.Location.Cache.GetById (targetLocationId).Name;
                }
            }

            if (details.Count == 0)
                throw new ArgumentException (string.Format ("No matching transfer details for transfer with the specified ID {0} found.", id));
        }

        private enum ErrorCodes
        {
            SourceLocationEmpty,
            SourceLocationCannotBeUsed,
            TargetLocationEmpty,
            TargetLocationCannotBeUsed
        }

        public override bool Validate (ValidateCallback callback, StateHolder state)
        {
            if (callback == null)
                throw new ArgumentNullException ("callback");

            if (sourceLocationId <= 0)
                if (!callback (Translator.GetString ("Source location cannot be empty!"), ErrorSeverity.Error, (int) ErrorCodes.SourceLocationEmpty, state))
                    return false;

            if (BusinessDomain.LoggedUser.LockedLocationId > 0 && BusinessDomain.LoggedUser.LockedLocationId != sourceLocationId)
                if (!callback (Translator.GetString ("The source location cannot be used!"), ErrorSeverity.Error, (int) ErrorCodes.SourceLocationCannotBeUsed, state))
                    return false;

            string error;
            string warning;
            if (!BusinessDomain.CanUseLocationInOperation (OperationType, sourceLocationId, out error, out warning)) {
                if (error != null && !callback (error, ErrorSeverity.Error, (int) ErrorCodes.SourceLocationCannotBeUsed, state))
                    return false;

                if (warning != null && !callback (error, ErrorSeverity.Warning, (int) ErrorCodes.SourceLocationCannotBeUsed, state))
                    return false;
            }

            if (targetLocationId <= 0)
                if (!callback (Translator.GetString ("Target location cannot be empty!"), ErrorSeverity.Error, (int) ErrorCodes.TargetLocationEmpty, state))
                    return false;

            if (details.Any (d => d.Quantity.IsZero ()))
                if (!callback (Translator.GetString ("Details contain zero quantities!"), ErrorSeverity.Error, (int) ErrorCodes.TargetLocationEmpty, state))
                    return false;

            return true;
        }
    }
}