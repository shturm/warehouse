//
// CashBookEntry.cs
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
using Warehouse.Business.Operations;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Entities
{
    public class CashBookEntry
    {
        #region Private members

        private long number = -1;
        private DateTime date = BusinessDomain.Today;
        private string description = string.Empty;
        private TurnoverType turnoverType = TurnoverType.IncomeSale;
        private TurnoverDirection paymentDirection = TurnoverDirection.Income;
        private long userId = -1;
        private DateTime timeStamp = BusinessDomain.Now;
        private long locationId = -1;
        private long operationNumber = -1;
        private string descriptionTemplate = string.Empty;

        private string partnerName;

        #endregion

        #region Public properties

        [DbColumn (DataField.CashEntryId)]
        public long Number
        {
            get { return number; }
            set { number = value; }
        }

        [DbColumn (DataField.CashEntryDate)]
        public DateTime Date
        {
            get { return date; }
            set { date = value; }
        }

        [DbColumn (DataField.CashEntryDescription, 255)]
        public string Description
        {
            get
            {
                return string.IsNullOrEmpty (descriptionTemplate) ?
                    description :
                    string.Format (descriptionTemplate, Operation.GetFormattedOperationNumber (operationNumber), partnerName);
            }
            set { description = value; }
        }

        [DbColumn (DataField.CashEntryTurnoverType)]
        public TurnoverType TurnoverType
        {
            get { return turnoverType; }
            set
            {
                if (turnoverType != value) {
                    turnoverType = value;
                    switch (turnoverType) {
                        case TurnoverType.ExpensePurchase:
                        case TurnoverType.ExpenseConsumable:
                        case TurnoverType.ExpenseSalary:
                        case TurnoverType.ExpenseRent:
                        case TurnoverType.ExpenseOther:
                        case TurnoverType.ExpenseFuel:
                            paymentDirection = TurnoverDirection.Expense;
                            break;
                        case TurnoverType.IncomeOther:
                        case TurnoverType.IncomeSale:
                        case TurnoverType.IncomeAdvance:
                            paymentDirection = TurnoverDirection.Income;
                            break;
                    }
                }
            }
        }

        [DbColumn (DataField.CashEntryDirection)]
        public TurnoverDirection PaymentDirection
        {
            get { return paymentDirection; }
            set { paymentDirection = value; }
        }

        [DbColumn (DataField.CashEntryAmount)]
        public double Amount { get; set; }

        public double Income
        {
            get { return PaymentDirection == TurnoverDirection.Expense ? 0 : Amount; }
        }

        public double Expense
        {
            get { return PaymentDirection == TurnoverDirection.Income ? 0 : Amount; }
        }

        [DbColumn (DataField.CashEntryOperatorId)]
        public long UserId
        {
            get { return userId; }
            set { userId = value; }
        }

        [DbColumn (DataField.CashEntryTimeStamp)]
        public DateTime TimeStamp
        {
            get { return timeStamp; }
            set { timeStamp = value; }
        }

        [DbColumn (DataField.CashEntryLocationId)]
        public long LocationId
        {
            get { return locationId; }
            set { locationId = value; }
        }

        [DbColumn (DataField.LocationCode, true)]
        public string LocationCode { get; set; }

        [DbColumn (DataField.LocationName, true)]
        public string LocationName { get; set; }

        [DbColumn (DataField.UserName, true)]
        public string OperatorName { get; set; }

        public long OperationNumber
        {
            get { return operationNumber; }
            set { operationNumber = value; }
        }

        public string DescriptionTemplate
        {
            set { descriptionTemplate = value; }
        }

        public string PartnerName
        {
            set { partnerName = value; }
        }

        #endregion

        public void CommitChanges ()
        {
            BusinessDomain.DataAccessProvider.AddUpdateCashBookEntry (this);
        }

        public CashBookEntry ()
        {
        }

        public CashBookEntry (Payment payment)
        {
            Date = payment.Date;
            Amount = payment.Quantity;
            UserId = payment.UserId;

            bool isSale = payment.ParentOperation != null && InteritsRawGeneric (payment.ParentOperation.GetType (), typeof (Sale<>));
            if (isSale) {
                PaymentDirection = TurnoverDirection.Income;
                LocationId = payment.ParentOperation.LocationId;
                TurnoverType = TurnoverType.IncomeSale;
                DescriptionTemplate = Translator.GetString ("Sale No. {0}, {1}");
            }

            Purchase purchase = payment.ParentOperation as Purchase;
            if (purchase != null) {
                PaymentDirection = TurnoverDirection.Expense;
                LocationId = purchase.LocationId;
                TurnoverType = TurnoverType.ExpensePurchase;
                DescriptionTemplate = Translator.GetString ("Purchase No. {0}, {1}");
            }
        }

        public static LazyListModel<CashBookEntry> GetAll (DataQuery dataQuery)
        {
            return BusinessDomain.DataAccessProvider.GetAllCashBookEntries<CashBookEntry> (dataQuery);
        } 

        public static KeyValuePair<int, string> [] GetAllTurnoverDirections ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) TurnoverDirection.Expense, Translator.GetString ("Expense")),
                    new KeyValuePair<int, string> ((int) TurnoverDirection.Income, Translator.GetString ("Income"))
                };
        }

        public static KeyValuePair<int, string> [] GetAllTurnoverDirectionFilters ()
        {
            List<KeyValuePair<int, string>> filters = new List<KeyValuePair<int, string>>
                {
                    new KeyValuePair<int, string> (-1, Translator.GetString ("All"))
                };

            filters.AddRange (GetAllTurnoverDirections ());

            return filters.ToArray ();
        }

        public static KeyValuePair<int, string> [] GetAllTurnoverTypes ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) TurnoverType.ExpensePurchase, Translator.GetString ("Expense (purchase)")),
                    new KeyValuePair<int, string> ((int) TurnoverType.ExpenseConsumable, Translator.GetString ("Expense (consumation)")),
                    new KeyValuePair<int, string> ((int) TurnoverType.ExpenseSalary, Translator.GetString ("Expense (salary)")),
                    new KeyValuePair<int, string> ((int) TurnoverType.ExpenseRent, Translator.GetString ("Expense (rent)")),
                    new KeyValuePair<int, string> ((int) TurnoverType.ExpenseOther, Translator.GetString ("Expense")),
                    new KeyValuePair<int, string> ((int) TurnoverType.ExpenseFuel, Translator.GetString ("Expense (fuel)")),
                    new KeyValuePair<int, string> ((int) TurnoverType.IncomeOther, Translator.GetString ("Income")),
                    new KeyValuePair<int, string> ((int) TurnoverType.IncomeSale, Translator.GetString ("Income (sale)")),
                    new KeyValuePair<int, string> ((int) TurnoverType.IncomeAdvance, Translator.GetString ("Income (advance deposit)")),
                    new KeyValuePair<int, string> ((int) TurnoverType.NotDefined, Translator.GetString ("Unknown"))
                };
        }

        public static KeyValuePair<int, string> [] GetAllTurnoverTypeFilters ()
        {
            List<KeyValuePair<int, string>> filters = new List<KeyValuePair<int, string>>
                {
                    new KeyValuePair<int, string> (-1, Translator.GetString ("All"))
                };

            filters.AddRange (GetAllTurnoverTypes ());

            return filters.ToArray ();
        }

        public static void DeleteBefore (DateTime date, long locationId)
        {
            BusinessDomain.DataAccessProvider.DeleteCashBookEntries (date, locationId);
        }

        public static bool Delete (long id)
        {
            return BusinessDomain.DataAccessProvider.DeleteCashBookEntry (id);
        }

        public static IEnumerable<string> GetRecentDescriptions ()
        {
            return BusinessDomain.DataAccessProvider.GetRecentDescriptions ();
        }

        private static bool InteritsRawGeneric (Type derived, Type genericBase)
        {
            Type type = typeof (object);
            while (derived != type && derived != null) {
                Type current = derived.IsGenericType ? derived.GetGenericTypeDefinition () : derived;
                if (genericBase == current)
                    return true;
                derived = derived.BaseType;
            }
            return false;
        }

        public static IEnumerable<long> GetUsedLocationIdsBefore (DateTime date)
        {
            DataQuery filter = new DataQuery (new DataFilter (DataFilterLogic.LessOrEqual, DataField.CashEntryDate) 
                { Values = new object [] { date } });
            return GetUsedLocationIds (filter);
        }

        public static IEnumerable<long> GetUsedLocationIds (DataQuery dataQuery)
        {
            return BusinessDomain.DataAccessProvider.GetCashBookLocationIds (dataQuery);
        }

        public static IDictionary<int, double> GetCashBookBalancesForLocation (long locationID)
        {
            DataQuery filter = new DataQuery (new DataFilter (DataFilterLogic.ExactMatch, DataField.CashEntryLocationId) 
                { Values = new object [] { locationID } });
            return GetCashBookBalances (filter);
        }

        public static IDictionary<int, double> GetCashBookBalances (DataQuery dataQuery)
        {
            return BusinessDomain.DataAccessProvider.GetCashBookBalances<CashBookEntry> (dataQuery);
        }
    }
}