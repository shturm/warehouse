//
// Reservation.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/11/2006
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
    public class Reservation : RestaurantOrder
    {
        private int detailId = -1;
        private double persons;
        private int currencyId = 1;
        private double currencyRate = 1;
        private string description = " ";
        private int lotId = 1;
        private DateTime dateTime;

        #region Public properties

        [DbColumn (DataField.OperationDetailId)]
        public virtual int DetailId
        {
            get { return detailId; }
            set { detailId = value; OnPropertyChanged ("DetailId"); }
        }

        [DbColumn (DataField.OperationDetailQuantity)]
        public double Persons
        {
            get { return persons; }
            set { persons = value; OnPropertyChanged ("Persons"); }
        }

        [DbColumn (DataField.OperationDetailCurrencyId)]
        public int CurrencyId
        {
            get { return currencyId; }
            set { currencyId = value; OnPropertyChanged ("CurrencyId"); }
        }

        [DbColumn (DataField.OperationDetailCurrencyRate)]
        public double CurrencyRate
        {
            get { return currencyRate; }
            set { currencyRate = value; OnPropertyChanged ("CurrencyRate"); }
        }

        [DbColumn (DataField.OperationDetailLot)]
        public string Description
        {
            get { return description; }
            set { description = value; OnPropertyChanged ("Description"); }
        }

        [DbColumn (DataField.OperationDetailLotId)]
        public int LotId
        {
            get { return lotId; }
            set { lotId = value; OnPropertyChanged ("LotId"); }
        }

        [DbColumn (DataField.OperationDateTime)]
        public DateTime DateTime
        {
            get { return dateTime; }
            set
            {
                dateTime = value; OnPropertyChanged ("DateTime");
                Date = value.Date;
                note = value.ToString ("HH:mm");
            }
        }

        #endregion

        public Reservation ()
        {
            operationType = OperationType.Reservation;
        }

        public override void CommitChanges (bool logChanges = false, bool logNewDetailChanges = false, long? mainAvailabilityLocation = null, bool checkAvailability = false)
        {
            using (DbTransaction transaction = new DbTransaction (BusinessDomain.DataAccessProvider)) {
                // Create a new operation Id if needed);
                bool editMode = true;
                OperationState operationState = State;
                if (operationState == OperationState.New || operationState == OperationState.NewDraft || operationState == OperationState.NewPending) {
                    id = CreateNewId ();

                    // Set the new operation id
                    editMode = false;
                }

                // Save the order in the database
                RestaurantOrderDetail reservationDetail = new RestaurantOrderDetail { ReferenceDocumentId = referenceDocumentId, Note = Note };
                BusinessDomain.DataAccessProvider.AddUpdateReservation (this, reservationDetail);

                RestaurantOrder order = GetRestaurantOrder ();
                if (order.Details.Count > 0) {
                    order.Id = id;
                    order.CommitChanges (logChanges, logNewDetailChanges, mainAvailabilityLocation, checkAvailability);
                }

                // We have to delete the payment records if there is nothing left
                if (editMode && Persons.IsZero ()) {
                    BusinessDomain.DataAccessProvider.DeleteOperationId (OperationType.Reservation, id);

                    ApplicationLogEntry.AddNew (string.Format (Translator.GetString ("Void reservation No.{0} from {1}"), GetFormattedOperationNumber (id), BusinessDomain.GetFormattedDate (date)));
                } else if (!Persons.IsZero ()) {
                    // Don't update the amount paid if this is not a new sale
                    if (editMode) {
                        ApplicationLogEntry.AddNew (string.Format (Translator.GetString ("Edit reservation No.{0} from {1}"), GetFormattedOperationNumber (id), BusinessDomain.GetFormattedDate (date)));
                    }
                }
                IsDirty = false;

                transaction.Complete ();
            }
        }

        protected override void LoadDetails (bool usePriceIn)
        {
            ConfigurationHolder config = BusinessDomain.AppConfiguration;
            details.Clear ();
            details.AddRange (BusinessDomain.DataAccessProvider.GetOperationDetailsById<RestaurantOrderDetail> (operationType, id, partnerId, locationId, date, userId, loggedUserId, config.CurrencyPrecision, config.RoundedPrices, usePriceIn));
            if (details.Count > 0)
                referenceDocumentId = details [0].ReferenceDocumentId;

            base.LoadDetails (usePriceIn);
        }

        public override RestaurantOrder GetRestaurantOrder ()
        {
            RestaurantOrder ret = base.GetRestaurantOrder ();
            ret.OperationType = OperationType.RestaurantOrder;
            foreach (RestaurantOrderDetail detail in ret.Details) {
                detail.OriginalQuantity = 0;
            }

            ret.IsDirty = true;

            return ret;
        }

        public static LazyListModel<Reservation> GetReservations (long? locationId, long? customerId, long? userId, DateTime? from, DateTime? to)
        {
            return BusinessDomain.DataAccessProvider.GetReservations<Reservation> (locationId, customerId, userId, @from, to);
        }
    }
}
