//
// Payment.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   07/13/2006
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
using System.ComponentModel;
using Warehouse.Business.Operations;
using Warehouse.Data;
using Warehouse.Data.Model;
using System.Linq;

namespace Warehouse.Business.Entities
{
    public enum PaymentMode
    {
        Due = -1,
        Edited = 0,
        Paid = 1
    }

    public class Payment : ICloneable, INotifyPropertyChanged
    {
        #region Private members

        private long id = -1;
        private long operationId = -1;
        private int operationType = -1;
        private long partnerId = -1;
        private double quantity;
        private double? originalQuantity;
        private PaymentMode mode = PaymentMode.Due;
        private DateTime date = BusinessDomain.Today;
        private long userId = -1;
        private long typeId = (int) BasePaymentType.Cash;
        private PaymentType type;
        private string transactionNumber = string.Empty;
        private DateTime endDate = BusinessDomain.Today;
        private Operation parentOperation;
        private long locationId = 1;
        private int sign;

        #endregion

        #region Public properties

        [DbColumn (DataField.PaymentId)]
        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        [DbColumn (DataField.PaymentOperationId)]
        public long OperationId
        {
            get { return operationId; }
            set { operationId = value; }
        }

        [DbColumn (DataField.PaymentOperationType)]
        public int OperationType
        {
            get { return operationType; }
            set { operationType = value; }
        }

        [DbColumn (DataField.PaymentPartnerId)]
        public long PartnerId
        {
            get { return partnerId; }
            set { partnerId = value; }
        }

        [DbColumn (DataField.PartnerCode, true)]
        public string PartnerCode { get; set; }

        /// <summary>
        /// Gets or sets the name of the partner used in the <see cref="Operation"/> for which the <see cref="Payment"/> is made.
        /// </summary>
        /// <value>The name of the partner.</value>
        [DbColumn (DataField.PartnerName, true)]
        public string PartnerName { get; set; }

        [DbColumn (DataField.PaymentAmount)]
        public double Quantity
        {
            get { return quantity; }
            set
            {
                if (quantity.IsEqualTo (value))
                    return;

                quantity = value;
                OnPropertyChanged ("Quantity");

                if (originalQuantity != null)
                    return;
                
                originalQuantity = value;
                OnPropertyChanged ("OriginalQuantity");
            }
        }

        public double OriginalQuantity
        {
            get
            {
                if (originalQuantity == null)
                    originalQuantity = quantity;

                return originalQuantity.Value;
            }
            set { originalQuantity = value; }
        }

        [DbColumn (DataField.PaymentMode)]
        public PaymentMode Mode
        {
            get { return mode; }
            set { mode = value; }
        }

        [DbColumn (DataField.PaymentDate)]
        public DateTime Date
        {
            get { return date; }
            set { date = value; }
        }

        [DbColumn (DataField.PaymentOperatorId)]
        public long UserId
        {
            get { return userId; }
            set { userId = value; }
        }

        [DbColumn (DataField.PaymentTimeStamp)]
        public DateTime TimeStamp { get; set; }

        [DbColumn (DataField.PaymentTypeId)]
        public long TypeId
        {
            get { return typeId; }
            set { typeId = value; }
        }

        public PaymentType Type
        {
            get
            {
                if (type == null || type.Id != typeId)
                    type = PaymentType.Cache.GetById (typeId);

                return type;
            }
        }

        [DbColumn (DataField.PaymentTransaction, 255)]
        public string TransactionNumber
        {
            get { return transactionNumber; }
            set { transactionNumber = value; }
        }

        [DbColumn (DataField.PaymentEndDate)]
        public DateTime EndDate
        {
            get { return endDate; }
            set { endDate = value; }
        }

        public Operation ParentOperation
        {
            get { return parentOperation; }
            set { parentOperation = value; }
        }

        [DbColumn (DataField.PaymentLocationId)]
        public long LocationId
        {
            get { return locationId; }
            set { locationId = value; }
        }

        /// <summary>
        /// Gets or sets the name of the location used in the <see cref="Operation"/> for which the <see cref="Payment"/> is made..
        /// </summary>
        /// <value>The name of the location.</value>
        [DbColumn (DataField.LocationName, true)]
        public string LocationName { get; set; }

        [DbColumn (DataField.LocationCode, true)]
        public string LocationCode { get; set; }

        [DbColumn (DataField.PaymentSign)]
        public int Sign
        {
            get { return sign; }
            set { sign = value; }
        }

        #endregion

        public Payment ()
        {
        }

        public Payment (Operation operation, long paymentTypeId, PaymentMode paymentMode)
        {
            operationId = operation.Id;
            operationType = (int) operation.OperationType;
            parentOperation = operation;
            partnerId = operation.PartnerId;
            quantity = operation.TotalPlusVAT;
            userId = operation.LoggedUserId;
            date = paymentMode == PaymentMode.Due ? operation.Date : BusinessDomain.Today;
            typeId = paymentTypeId;
            mode = paymentMode;
            locationId = operation.LocationId;

            switch (operation.OperationType) {
                case Data.OperationType.Purchase:
                case Data.OperationType.CreditNote:
                case Data.OperationType.Return:
                    sign = -1;
                    break;

                case Data.OperationType.Sale:
                case Data.OperationType.StockTaking:
                case Data.OperationType.ConsignmentSale:
                case Data.OperationType.DebitNote:
                case Data.OperationType.AdvancePayment:
                case Data.OperationType.PurchaseReturn:
                    sign = 1;
                    break;
            }
        }

        public void CommitChanges (bool silent = false)
        {
            BusinessDomain.DataAccessProvider.AddUpdatePayment (this);

            if (!silent)
                BusinessDomain.OnPaymentCommited (this);
        }

        public void CommitAdvance ()
        {
            bool isNew = id < 0;
            using (DbTransaction transaction = new DbTransaction (BusinessDomain.DataAccessProvider)) {
                transaction.SnapshotObject (this);
                if (isNew) {
                    mode = PaymentMode.Paid;
                    userId = BusinessDomain.LoggedUser.Id;
                    operationType = (int) Data.OperationType.AdvancePayment;
                    sign = 1;
                    locationId = Location.DefaultId;
                }

                BusinessDomain.DataAccessProvider.AddAdvancePayment (this);

                if (isNew && Type.BaseType == BasePaymentType.Cash) {
                    CashBookEntry cashBookEntry = new CashBookEntry (this)
                        {
                            OperationNumber = OperationId,
                            PartnerName = PartnerName,
                            TurnoverType = TurnoverType.IncomeAdvance,
                            DescriptionTemplate = Translator.GetString ("Advance Payment No. {0}, {1}")
                        };
                    cashBookEntry.CommitChanges ();
                }
                transaction.Complete ();
            }

            if (isNew)
                BusinessDomain.OnPaymentCommited (this);
        }

        public void EditAdvance ()
        {
            BusinessDomain.DataAccessProvider.EditAdvancePayment (this);
        }

        public static Payment [] GetForOperation (Operation operation, PaymentMode paymentMode)
        {
            return BusinessDomain.DataAccessProvider.GetPaymentsForOperation<Payment> (
                operation.Id, (int) paymentMode, (int) operation.OperationType, operation.LocationId, operation.PartnerId);
        }

        public static LazyListModel<Payment> GetAllPerOperation (DataQuery dataQuery, bool onlyPaid = false)
        {
            if (BusinessDomain.LoggedUser.LockedLocationId > 0) {
                dataQuery.Filters.Add (new DataFilter (new DbField (DataField.PaymentLocationId))
                    {
                        Values = new object [] { BusinessDomain.LoggedUser.LockedLocationId },
                        CombineLogic = ConditionCombineLogic.Or
                    });
                dataQuery.Filters.Add (new DataFilter (new DbField (DataField.PaymentOperationType))
                    {
                        Values = new object [] { Data.OperationType.AdvancePayment },
                        CombineLogic = ConditionCombineLogic.Or
                    });
            }
            if (BusinessDomain.LoggedUser.LockedPartnerId > 0)
                dataQuery.Filters.Add (new DataFilter (new DbField (DataField.PaymentPartnerId))
                    {
                        Values = new object [] { BusinessDomain.LoggedUser.LockedPartnerId }
                    });

            return BusinessDomain.DataAccessProvider.GetAllPaymentsPerOperation<Payment> (dataQuery, onlyPaid);
        }

        public static KeyValuePair<int, string> [] GetAllSignTypes ()
        {
            return new []
                {
                    new KeyValuePair<int, string> (-2, Translator.GetString ("Any")),
                    new KeyValuePair<int, string> (1, Translator.GetString ("In")),
                    new KeyValuePair<int, string> (-1, Translator.GetString ("Out")),
                    new KeyValuePair<int, string> (0, Translator.GetString ("None", "Payment sign"))
                };
        }

        public static KeyValuePair<int, string> [] GetAllModeTypes ()
        {
            return new []
                {
                    new KeyValuePair<int, string> (-2, Translator.GetString ("Any")),
                    new KeyValuePair<int, string> ((int) PaymentMode.Due, Translator.GetString ("Due")),
                    new KeyValuePair<int, string> ((int) PaymentMode.Paid, Translator.GetString ("Paid"))
                };
        }

        public static LazyListModel<Payment> GetAdvances (long partnerId)
        {
            return BusinessDomain.DataAccessProvider.GetAdvancePayments<Payment> (partnerId);
        }

        public static LazyListModel<Payment> GetAdvances (DataQuery dataQuery)
        {
            if (BusinessDomain.LoggedUser.LockedPartnerId > 0)
                dataQuery.Filters.Add (new DataFilter (new DbField (DataField.PaymentPartnerId))
                {
                    Values = new object [] { BusinessDomain.LoggedUser.LockedPartnerId }
                });
            return BusinessDomain.DataAccessProvider.GetAdvancePayments<Payment> (dataQuery);
        }

        #region Implementation of ICloneable

        public object Clone ()
        {
            return MemberwiseClone ();
        }

        #endregion

        public static List<Payment> DistributeAdvances (IList<Payment> advances, long partnerId)
        {
            List<Payment> payments = new List<Payment> ();
            Payment [] duePayments = Partner.GetDuePayments (partnerId).OrderBy (p => p.EndDate).ToArray ();
            DistributeAdvances (advances, duePayments, payments);

            // Check if there is room for settlements and optimization of debt
            double owedToUs = duePayments.Where (p => p.Sign > 0).Sum (p => p.Quantity);
            double owedByUs = duePayments.Where (p => p.Sign < 0).Sum (p => p.Quantity);
            if (owedToUs.IsEqualTo (owedByUs) && owedByUs > 0) {
                Payment fakeAdvance = new Payment
                    {
                        Date = BusinessDomain.Today,
                        Sign = 1,
                        Quantity = owedByUs
                    };
                advances.Add (fakeAdvance);

                fakeAdvance = (Payment) fakeAdvance.Clone ();
                fakeAdvance.Sign = -1;
                advances.Add (fakeAdvance);

                foreach (Payment payment in GetAdvances (partnerId))
                    advances.Add (payment);

                DistributeAdvances (advances, duePayments, payments);
            }

            if (owedByUs.IsZero () && owedToUs.IsZero () && payments.Count == 0) {
                foreach (Payment advance in advances) {
                    advance.Sign = 1;
                    payments.Add (advance);
                }
            }

            return payments;
        }

        private static void DistributeAdvances (IList<Payment> advances, IList<Payment> duePayments, ICollection<Payment> payments)
        {
            for (int i = 0; i < advances.Count; i++) {
                Payment advance = advances [i];
                foreach (Payment duePayment in duePayments) {
                    if (duePayment.Quantity.IsZero () || duePayment.Sign != advance.Sign)
                        continue;

                    Payment payment = (Payment) duePayment.Clone ();
                    payment.Id = -1;
                    payment.Mode = PaymentMode.Paid;
                    payment.Date = advance.Date;
                    payment.Quantity = Math.Min (advance.Quantity, duePayment.Quantity);
                    payment.TypeId = advance.TypeId;
                    payment.TransactionNumber = advance.TransactionNumber;
                    payment.CommitChanges ();
                    payments.Add (payment);

                    if (payment.Type.BaseType == BasePaymentType.Cash) {
                        CashBookEntry entry = new CashBookEntry (payment)
                            {
                                OperationNumber = payment.OperationId,
                                PartnerName = payment.PartnerName,
                                LocationId = payment.LocationId,
                                Description = string.Format ("{0} No. {1}, {2}",
                                    Translator.GetOperationTypeGlobalName ((OperationType) payment.OperationType),
                                    Operation.GetFormattedOperationNumber (payment.OperationId),
                                    payment.PartnerName)
                            };
                        if (payment.Sign > 0) {
                            entry.PaymentDirection = TurnoverDirection.Income;
                            entry.TurnoverType = TurnoverType.IncomeOther;
                        } else {
                            entry.PaymentDirection = TurnoverDirection.Expense;
                            entry.TurnoverType = TurnoverType.ExpenseOther;
                        }
                        entry.CommitChanges ();
                    }

                    advance.Quantity -= payment.Quantity;
                    duePayment.Quantity -= payment.Quantity;

                    if (advance.Id > 0)
                        advance.CommitChanges ();

                    if (advance.Quantity.IsZero ())
                        break;
                }

                if (advance.Quantity.IsZero ()) {
                    advances.RemoveAt (i);
                    i--;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged (string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler (this, new PropertyChangedEventArgs (propertyName));
        }
    }
}