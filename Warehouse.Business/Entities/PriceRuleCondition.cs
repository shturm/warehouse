//
// PriceRuleCondition.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   12.31.2013
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
using System.Globalization;
using System.Linq;
using System.Text;
using Warehouse.Business.Operations;
using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public class PriceRuleCondition
    {
        private const string NOT = "NOT ";
        private const string DATE_FORMAT = "dd.MM.yyyy";
        private const string TIME_FORMAT = "HH:mm";

        public static Dictionary<PriceRule.ConditionType, string> AllMissingErrors
        {
            get
            {
                return new Dictionary<PriceRule.ConditionType, string> {
                        { PriceRule.ConditionType.Partner, Translator.GetString ("Missing partner") },
                        { PriceRule.ConditionType.PartnerGroup, Translator.GetString ("Missing partner group") },
                        { PriceRule.ConditionType.Object, Translator.GetString ("Missing location") },
                        { PriceRule.ConditionType.ObjectGroup, Translator.GetString ("Missing location group") },
                        { PriceRule.ConditionType.User, Translator.GetString ("Missing user") },
                        { PriceRule.ConditionType.UserGroup, Translator.GetString ("Missing user group") },
                        { PriceRule.ConditionType.Good, Translator.GetString ("Missing item") },
                        { PriceRule.ConditionType.ContainsGood, Translator.GetString ("Missing item") },
                        { PriceRule.ConditionType.GoodGroup, Translator.GetString ("Missing items group") },
                        { PriceRule.ConditionType.ContainsGGroup, Translator.GetString ("Missing items group") }
                    };
            }
        }

        public bool IsActive { get; set; }

        private bool isException;
        public bool IsException
        {
            get { return isException; }
        }

        private readonly PriceRule.ConditionType type;
        public PriceRule.ConditionType Type
        {
            get { return type; }
        }

        private string formula;
        public string Formula
        {
            get { return formula; }
        }

        private object [] values;
        public object [] Values
        {
            get { return values; }
        }

        private bool error;
        public bool Error
        {
            get { return error; }
        }

        public PriceRuleCondition (PriceRule.ConditionType type, string formula, bool isException = false)
        {
            IsActive = true;
            this.type = type;
            SetFormula (formula);
            this.isException = isException;
        }

        public PriceRuleCondition (PriceRule.ConditionType type, object [] values, bool isException = false)
        {
            IsActive = true;
            this.type = type;
            SetValues (values);
            this.isException = isException;
        }

        public bool CheckApplicableToOperation<T> (Operation<T> operation) where T : OperationDetail
        {
            if (error)
                return false;

            DateTime dateFrom;
            DateTime dateTo;
            double @from;
            double to;
            bool ret;
            string groupCode = null;
            long? intValue;
            switch (type) {
                case PriceRule.ConditionType.Partner:
                    ret = operation.PartnerId == Convert.ToInt64 (values [0]);
                    break;

                case PriceRule.ConditionType.PartnerGroup:
                    intValue = PriceRule.GetLongValue (formula);
                    if (intValue != null) {
                        PartnersGroup byId = PartnersGroup.Cache.GetById (intValue.Value);
                        if (byId != null)
                            groupCode = byId.Code;
                    }

                    if (groupCode == null)
                        groupCode = PriceRule.GetStringValue (formula);

                    Partner partner = Partner.Cache.GetById (operation.PartnerId);
                    ret = PartnersGroup.Cache.GetById (partner.GroupId).Code.StartsWith (groupCode);
                    break;

                case PriceRule.ConditionType.Object:
                    ret = operation.LocationId == Convert.ToInt64 (values [0]);
                    break;

                case PriceRule.ConditionType.ObjectGroup:
                    intValue = PriceRule.GetLongValue (formula);
                    if (intValue != null) {
                        LocationsGroup byId = LocationsGroup.Cache.GetById (intValue.Value);
                        if (byId != null)
                            groupCode = byId.Code;
                    }

                    if (groupCode == null)
                        groupCode = PriceRule.GetStringValue (formula);

                    Location location = Location.Cache.GetById (operation.LocationId);
                    ret = LocationsGroup.Cache.GetById (location.GroupId).Code.StartsWith (groupCode);
                    break;

                case PriceRule.ConditionType.User:
                    ret = operation.UserId == Convert.ToInt64 (values [0]);
                    break;

                case PriceRule.ConditionType.UserGroup:
                    intValue = PriceRule.GetLongValue (formula);
                    if (intValue != null) {
                        UsersGroup byId = UsersGroup.Cache.GetById (intValue.Value);
                        if (byId != null)
                            groupCode = byId.Code;
                    }

                    if (groupCode == null)
                        groupCode = PriceRule.GetStringValue (formula);

                    User user = User.Cache.GetById (operation.UserId);
                    ret = UsersGroup.Cache.GetById (user.GroupId).Code.StartsWith (groupCode);
                    break;

                case PriceRule.ConditionType.Time:
                    GetConditionDateTimeInterval (formula, out dateFrom, out dateTo);
                    DateTime now = BusinessDomain.Now;
                    if (dateFrom.TimeOfDay < dateTo.TimeOfDay)
                        ret = dateFrom.TimeOfDay <= now.TimeOfDay && now.TimeOfDay <= dateTo.TimeOfDay;
                    else
                        ret = dateFrom.TimeOfDay <= now.TimeOfDay || now.TimeOfDay <= dateTo.TimeOfDay;
                    break;

                case PriceRule.ConditionType.Date:
                    GetConditionDateTimeInterval (formula, out dateFrom, out dateTo);
                    ret = dateFrom <= operation.Date && operation.Date <= dateTo;
                    break;

                case PriceRule.ConditionType.DocumentSum:
                    GetConditionNumericInterval (formula, out from, out to);
                    ret = from <= operation.Total && operation.Total <= to;
                    break;

                case PriceRule.ConditionType.TurnoverSum:
                    GetConditionNumericInterval (formula, out from, out to);
                    double turnover = Partner.GetTurnover (operation.PartnerId);
                    ret = from <= turnover && turnover <= to;
                    break;

                case PriceRule.ConditionType.PaymentSum:
                    GetConditionNumericInterval (formula, out from, out to);
                    double debt = Partner.GetDebt (operation.PartnerId);
                    ret = from <= debt && debt <= to;
                    break;

                case PriceRule.ConditionType.Weekdays:
                    List<DayOfWeek> ruleWeekDays = GetRuleWeekDays ();
                    ret = ruleWeekDays.Contains (operation.Date.DayOfWeek);
                    break;

                case PriceRule.ConditionType.UnpaidDocumentsSum:
                    GetConditionNumericInterval (formula, out from, out to);
                    double sum = Partner.GetUnpaidAmountWithExpiredDueDate (operation.PartnerId, operation.Date);
                    ret = from <= sum && sum <= to;
                    break;

                case PriceRule.ConditionType.ContainsGood:
                    intValue = Convert.ToInt64 (values [0]);
                    ret = operation.Details.Any (d => d.ItemId == intValue) ||
                        operation.AdditionalDetails.Any (d => d.ItemId == intValue);
                    break;

                case PriceRule.ConditionType.GoodGroup:
                case PriceRule.ConditionType.ContainsGGroup:
                    Dictionary<long, string> codes = new Dictionary<long, string> ();

                    foreach (T detail in operation.Details)
                        AddGroupCode (codes, detail.ItemGroupId);

                    foreach (T additionalDetail in operation.AdditionalDetails)
                        AddGroupCode (codes, additionalDetail.ItemGroupId);

                    intValue = PriceRule.GetLongValue (formula);
                    if (intValue != null) {
                        ItemsGroup byId = ItemsGroup.Cache.GetById (intValue.Value);
                        if (byId != null)
                            groupCode = byId.Code;
                    }

                    if (groupCode == null)
                        groupCode = PriceRule.GetStringValue (formula);

                    ret = codes.Values.Any (c => c.StartsWith (groupCode));
                    break;

                case PriceRule.ConditionType.PaymentTypeUsed:
                    List<BasePaymentType> paymentTypes = GetConditionPaymentTypes ();
                    ret = operation.Payments.Any (payment => paymentTypes.Contains (payment.Type.BaseType));
                    break;

                case PriceRule.ConditionType.DatabaseUpdated:
                    ret = BusinessDomain.GetDatabaseLastUpdate ().AddMinutes (30) > DateTime.Now;
                    break;

                default:
                    return true;
            }

            return ret != IsException;
        }

        public bool CheckApplicableToDetail<T> (IList<T> operationDetails, OperationDetail operationDetail) where T : OperationDetail
        {
            if (error)
                return false;

            bool ret;
            switch (type) {
                case PriceRule.ConditionType.Good:
                    ret = operationDetail.ItemId == Convert.ToInt64 (values [0]);
                    break;

                case PriceRule.ConditionType.GoodGroup:
                    ItemsGroup itemsGroup = ItemsGroup.Cache.GetById (operationDetail.ItemGroupId);

                    string groupCode = null;
                    long? intValue = PriceRule.GetLongValue (formula);
                    if (intValue != null) {
                        ItemsGroup byId = ItemsGroup.Cache.GetById (intValue.Value);
                        if (byId != null)
                            groupCode = byId.Code;
                    }

                    if (groupCode == null)
                        groupCode = PriceRule.GetStringValue (formula);

                    ret = itemsGroup.Code.StartsWith (groupCode);
                    break;

                case PriceRule.ConditionType.GoodQttySum:
                    double from;
                    double to;
                    GetConditionNumericInterval (formula, out from, out to);
                    double quantity = operationDetails
                        .Where (detail => detail.ItemId == operationDetail.ItemId)
                        .Select (detail => detail.Quantity)
                        .Sum ();
                    ret = from <= quantity && quantity <= to;
                    break;

                default:
                    return true;
            }

            return ret != IsException;
        }

        private static void AddGroupCode (Dictionary<long, string> codes, long groupId)
        {
            if (!codes.ContainsKey (groupId))
                codes.Add (groupId, ItemsGroup.Cache.GetById (groupId).Code);
        }

        private List<DayOfWeek> GetRuleWeekDays ()
        {
            string weekBitMask = PriceRule.GetStringValue (Formula);
            List<DayOfWeek> ruleWeekDays = new List<DayOfWeek> (weekBitMask.Length);
            for (int i = 0; i < weekBitMask.Length; i++)
                if (weekBitMask [i] == '1')
                    // Sunday is 0 instead of 7, the remaining days start from 1
                    if (i == weekBitMask.Length - 1)
                        ruleWeekDays.Add (0);
                    else
                        ruleWeekDays.Add ((DayOfWeek) (i + 1));

            return ruleWeekDays;
        }

        private List<BasePaymentType> GetConditionPaymentTypes ()
        {
            List<BasePaymentType> ret = new List<BasePaymentType> ();
            string mask = PriceRule.GetStringValue (Formula);
            for (int i = 0; i < mask.Length && i < (int) BasePaymentType.Advance; i++) {
                if (mask [i] == '1')
                    ret.Add ((BasePaymentType) i + 1);
            }

            return ret;
        }

        private static void GetConditionNumericInterval (string condition, out double from, out double to)
        {
            bool hasFrom;
            string [] parts;
            bool hasTo;
            ParseStartEnd (condition, out hasFrom, out hasTo, out parts);

            @from = hasFrom ? Double.Parse (parts [1].Trim (), CultureInfo.InvariantCulture) : Double.MinValue;
            to = hasTo ? Double.Parse (parts [hasFrom ? 3 : 1].Trim (), CultureInfo.InvariantCulture) : Double.MaxValue;
        }

        private static void GetConditionDateTimeInterval (string condition, out DateTime from, out DateTime to)
        {
            bool hasFrom;
            string [] parts;
            bool hasTo;
            ParseStartEnd (condition, out hasFrom, out hasTo, out parts);

            DateTimeFormatInfo dateTimeFormat = new DateTimeFormatInfo { ShortDatePattern = DATE_FORMAT, ShortTimePattern = TIME_FORMAT };
            @from = hasFrom ? DateTime.Parse (parts [1].Trim (), dateTimeFormat) : DateTime.MinValue;
            to = hasTo ? DateTime.Parse (parts [hasFrom ? 3 : 1].Trim (), dateTimeFormat) : DateTime.MaxValue;
        }

        private static void ParseStartEnd (string condition, out bool hasFrom, out bool hasTo, out string [] parts)
        {
            hasFrom = condition.Contains (">=");
            hasTo = condition.Contains ("<=");

            parts = condition.Split (new [] { ">=", ")", PriceRule.AND, "(", "<=" }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void SetFormula (string value)
        {
            if (formula == value)
                return;

            formula = value;
            values = null;
            switch (type) {
                case PriceRule.ConditionType.Partner:
                case PriceRule.ConditionType.Object:
                case PriceRule.ConditionType.User:
                case PriceRule.ConditionType.Good:
                case PriceRule.ConditionType.ContainsGood:
                    values = new object [] { PriceRule.GetLongValue (formula) };
                    break;

                case PriceRule.ConditionType.PartnerGroup:
                case PriceRule.ConditionType.ObjectGroup:
                case PriceRule.ConditionType.UserGroup:
                case PriceRule.ConditionType.GoodGroup:
                case PriceRule.ConditionType.ContainsGGroup:
                    values = new object [] { PriceRule.GetStringValue (formula) };
                    break;

                case PriceRule.ConditionType.Time:
                case PriceRule.ConditionType.Date:
                    DateTime dateTimeFrom;
                    DateTime dateTimeTo;
                    GetConditionDateTimeInterval (formula, out dateTimeFrom, out dateTimeTo);
                    values = new object [] { dateTimeFrom, dateTimeTo };
                    break;

                case PriceRule.ConditionType.DocumentSum:
                case PriceRule.ConditionType.TurnoverSum:
                case PriceRule.ConditionType.GoodQttySum:
                case PriceRule.ConditionType.PaymentSum:
                case PriceRule.ConditionType.UnpaidDocumentsSum:
                    double from;
                    double to;
                    GetConditionNumericInterval (formula, out from, out to);
                    values = new [] { from.IsEqualTo (Double.MinValue) ? (object) null : from, to.IsEqualTo (Double.MaxValue) ? (object) null : to };
                    break;

                case PriceRule.ConditionType.Weekdays:
                    values = new object [] { GetRuleWeekDays () };
                    break;

                case PriceRule.ConditionType.PaymentTypeUsed:
                    values = new object [] { GetConditionPaymentTypes () };
                    break;

                case PriceRule.ConditionType.DatabaseUpdated:
                    values = new object [] { 30 };
                    break;
            }

            error = values == null || values.All (v => v == null);
        }

        public void SetValues (object [] newValues)
        {
            values = newValues;
            formula = null;
            switch (type) {
                case PriceRule.ConditionType.Partner:
                case PriceRule.ConditionType.PartnerGroup:
                case PriceRule.ConditionType.Object:
                case PriceRule.ConditionType.ObjectGroup:
                case PriceRule.ConditionType.User:
                case PriceRule.ConditionType.UserGroup:
                case PriceRule.ConditionType.Good:
                case PriceRule.ConditionType.GoodGroup:
                case PriceRule.ConditionType.ContainsGood:
                case PriceRule.ConditionType.ContainsGGroup:
                    formula = String.Format ("{0} = {1}", type, values [0]);
                    break;

                case PriceRule.ConditionType.Time:
                    if (values [0] == null && values [1] == null)
                        throw new ArgumentException ();

                    if (values [1] == null)
                        formula = String.Format ("{0} >= {1}", type,
                            ((DateTime) values [0]).ToString (TIME_FORMAT));
                    else if (values [0] == null)
                        formula = String.Format ("{0} <= {1}", type,
                            ((DateTime) values [1]).ToString (TIME_FORMAT));
                    else
                        formula = String.Format ("{0} >= {1}) AND ({0} <= {2}", type,
                            ((DateTime) values [0]).ToString (TIME_FORMAT),
                            ((DateTime) values [1]).ToString (TIME_FORMAT));
                    break;

                case PriceRule.ConditionType.Date:
                    if (values [0] == null && values [1] == null)
                        throw new ArgumentException ();

                    if (values [1] == null)
                        formula = String.Format ("{0} >= {1}", type,
                            ((DateTime) values [0]).ToString (DATE_FORMAT));
                    else if (values [0] == null)
                        formula = String.Format ("{0} <= {1}", type,
                            ((DateTime) values [1]).ToString (DATE_FORMAT));
                    else
                        formula = String.Format ("{0} >= {1}) AND ({0} <= {2}", type,
                            ((DateTime) values [0]).ToString (DATE_FORMAT),
                            ((DateTime) values [1]).ToString (DATE_FORMAT));
                    break;

                case PriceRule.ConditionType.DocumentSum:
                case PriceRule.ConditionType.TurnoverSum:
                case PriceRule.ConditionType.GoodQttySum:
                case PriceRule.ConditionType.PaymentSum:
                case PriceRule.ConditionType.UnpaidDocumentsSum:
                    if (values [0] == null && values [1] == null)
                        throw new ArgumentException ();

                    if (values [1] == null)
                        formula = String.Format (CultureInfo.InvariantCulture, "{0} >= {1}", type, values [0]);
                    else if (values [0] == null)
                        formula = String.Format (CultureInfo.InvariantCulture, "{0} <= {1}", type, values [1]);
                    else
                        formula = String.Format (CultureInfo.InvariantCulture, "{0} >= {1}) AND ({0} <= {2}",
                            type, values [0], values [1]);
                    break;

                case PriceRule.ConditionType.Weekdays:
                    List<DayOfWeek> weekDays = Enum.GetValues (typeof (DayOfWeek)).Cast<DayOfWeek> ().ToList ();
                    // move Sunday to the end of the week
                    weekDays.Add (weekDays [0]);
                    weekDays.RemoveAt (0);
                    StringBuilder weekMaskBuilder = new StringBuilder (weekDays.Count);
                    IList<DayOfWeek> selectedWeekDays = (IList<DayOfWeek>) values [0];
                    foreach (DayOfWeek dayOfWeek in weekDays)
                        weekMaskBuilder.Append (selectedWeekDays.Contains (dayOfWeek) ? 1 : 0);

                    formula = String.Format ("{0} = {1}", type, weekMaskBuilder);
                    break;

                case PriceRule.ConditionType.PaymentTypeUsed:
                    StringBuilder maskBuilder = new StringBuilder ();
                    IList<BasePaymentType> selectedPaymentTypes = (IList<BasePaymentType>) values [0];
                    for (int i = 1; i <= (int) BasePaymentType.Advance; i++)
                        maskBuilder.Append (selectedPaymentTypes.Contains ((BasePaymentType) i) ? 1 : 0);

                    formula = String.Format ("{0} = {1}", type, maskBuilder);
                    break;

                case PriceRule.ConditionType.DatabaseUpdated:
                    formula = String.Format ("{0} = {1}", type, values [0]);
                    break;

                default:
                    throw new ArgumentOutOfRangeException ("Type", type, "Unknown type");
            }
        }

        public static bool Parse (string conditionsString, List<PriceRuleCondition> conditions)
        {
            foreach (string condition in conditionsString.Split (new [] { "IF ", PriceRule.AND, "(", ")" }, StringSplitOptions.RemoveEmptyEntries)) {
                int indexOfEquals = condition.IndexOf ('=');
                if (indexOfEquals < 0)
                    return false;

                string conditionTypeName = condition.Substring (0, indexOfEquals).Trim (' ', '>', '<');
                bool isException = false;
                if (conditionTypeName.Contains (NOT)) {
                    conditionTypeName = conditionTypeName.Substring (NOT.Length);
                    isException = true;
                }

                PriceRule.ConditionType cType;
                if (!Enum.TryParse (conditionTypeName, true, out cType))
                    return false;

                int index = conditions.FindIndex (c => c.Type == cType && c.IsException == isException);

                // a duplicate type indicates a 2-sided condition: (a >= x) AND (a <= y)
                if (index > -1) {
                    string value = conditions [index].Formula + ")" + PriceRule.AND + "(" + condition;
                    conditions [index] = new PriceRuleCondition (cType, value, conditions [index].IsException);
                } else if (isException)
                    conditions.Add (new PriceRuleCondition (cType, condition.Substring (NOT.Length), true));
                else
                    conditions.Add (new PriceRuleCondition (cType, condition));
            }
            return true;
        }

        public static string GetFormula (IEnumerable<PriceRuleCondition> conditions)
        {
            return String.Join (PriceRule.AND, conditions.Select (c => String.Format (c.IsException ? "(NOT {0})" : "({0})", c.Formula)));
        }

        public PriceRuleCondition SetIsException (bool value)
        {
            isException = value;
            return this;
        }

        public static string TypeToString (PriceRule.ConditionType conditionType)
        {
            switch (conditionType) {
                case PriceRule.ConditionType.Partner:
                    return Translator.GetString ("Partner");

                case PriceRule.ConditionType.PartnerGroup:
                    return Translator.GetString ("Partner group");

                case PriceRule.ConditionType.Object:
                    return Translator.GetString ("Location");

                case PriceRule.ConditionType.ObjectGroup:
                    return Translator.GetString ("Location group");

                case PriceRule.ConditionType.User:
                    return Translator.GetString ("User");

                case PriceRule.ConditionType.UserGroup:
                    return Translator.GetString ("User group");

                case PriceRule.ConditionType.Good:
                    return Translator.GetString ("Item");

                case PriceRule.ConditionType.GoodGroup:
                    return Translator.GetString ("Item group");

                case PriceRule.ConditionType.Time:
                    return Translator.GetString ("Hour");

                case PriceRule.ConditionType.Date:
                    return Translator.GetString ("Date");

                case PriceRule.ConditionType.DocumentSum:
                    return Translator.GetString ("Document amount");

                case PriceRule.ConditionType.TurnoverSum:
                    return Translator.GetString ("Turnover");

                case PriceRule.ConditionType.GoodQttySum:
                    return Translator.GetString ("Item quantity in operation");

                case PriceRule.ConditionType.PaymentSum:
                    return Translator.GetString ("Amount due by partner");

                case PriceRule.ConditionType.Weekdays:
                    return Translator.GetString ("Days of week");

                case PriceRule.ConditionType.UnpaidDocumentsSum:
                    return Translator.GetString ("Unpaid amount with expired due date");

                case PriceRule.ConditionType.ContainsGood:
                    return Translator.GetString ("Presence of item in the operation");

                case PriceRule.ConditionType.ContainsGGroup:
                    return Translator.GetString ("Presence of item of group in the operation");

                case PriceRule.ConditionType.PaymentTypeUsed:
                    return Translator.GetString ("Payment type used");

                case PriceRule.ConditionType.DatabaseUpdated:
                    return Translator.GetString ("The database is updated");
            }
            return String.Empty;
        }

        public override string ToString ()
        {
            string condition = formula;
            long id;
            switch (type) {
                case PriceRule.ConditionType.Partner:
                    if (values [0] == null) {
                        condition = null;
                        break;
                    }
                    var partner = Partner.Cache.GetById (Convert.ToInt64 (values [0]));
                    condition = partner != null ? partner.Name : null;
                    break;

                case PriceRule.ConditionType.PartnerGroup:
                    if (values [0] == null) {
                        condition = null;
                        break;
                    }

                    var partnersGroup = Int64.TryParse ((string) values [0], out id) ?
                        PartnersGroup.Cache.GetById (id) :
                        PartnersGroup.Cache.GetByCode ((string) values [0]);

                    condition = partnersGroup != null ? partnersGroup.Name : null;
                    break;

                case PriceRule.ConditionType.Object:
                    if (values [0] == null) {
                        condition = null;
                        break;
                    }
                    var location = Location.Cache.GetById (Convert.ToInt64 (values [0]));
                    condition = location != null ? location.Name : null;
                    break;

                case PriceRule.ConditionType.ObjectGroup:
                    if (values [0] == null) {
                        condition = null;
                        break;
                    }

                    var locationsGroup = Int64.TryParse ((string) values [0], out id) ?
                        LocationsGroup.Cache.GetById (id) :
                        LocationsGroup.Cache.GetByCode ((string) values [0]);

                    condition = locationsGroup != null ? locationsGroup.Name : null;
                    break;

                case PriceRule.ConditionType.User:
                    if (values [0] == null) {
                        condition = null;
                        break;
                    }
                    var user = User.Cache.GetById (Convert.ToInt64 (values [0]));
                    condition = user != null ? user.Name : null;
                    break;

                case PriceRule.ConditionType.UserGroup:
                    if (values [0] == null) {
                        condition = null;
                        break;
                    }

                    var usersGroup = Int64.TryParse ((string) values [0], out id) ?
                        UsersGroup.Cache.GetById (id) :
                        UsersGroup.Cache.GetByCode ((string) values [0]);

                    condition = usersGroup != null ? usersGroup.Name : null;
                    break;

                case PriceRule.ConditionType.Good:
                case PriceRule.ConditionType.ContainsGood:
                    if (values [0] == null) {
                        condition = null;
                        break;
                    }
                    var item = Item.Cache.GetById (Convert.ToInt64 (values [0]));
                    condition = item != null ? item.Name : null;
                    break;

                case PriceRule.ConditionType.GoodGroup:
                case PriceRule.ConditionType.ContainsGGroup:
                    if (values [0] == null) {
                        condition = null;
                        break;
                    }

                    var itemsGroup = Int64.TryParse ((string) values [0], out id) ?
                        ItemsGroup.Cache.GetById (id) :
                        ItemsGroup.Cache.GetByCode ((string) values [0]);

                    condition = itemsGroup != null ? itemsGroup.Name : null;
                    break;

                case PriceRule.ConditionType.Time:
                    DateTime? timeFrom = (DateTime?) values [0];
                    DateTime? timeTo = (DateTime?) values [1];

                    string timeFromString = timeFrom != null ?
                        String.Format ("{0} {1}", Translator.GetString ("from"), BusinessDomain.GetFormattedTime (timeFrom.Value)) :
                        null;

                    string timeToString = timeTo != null ?
                        String.Format ("{0} {1}", Translator.GetString ("to"), BusinessDomain.GetFormattedTime (timeTo.Value)) :
                        null;

                    if (timeFrom != null && timeTo != null)
                        condition = String.Format ("{0} {1}", timeFromString, timeToString);
                    else if (timeFrom != null)
                        condition = timeFromString;
                    else if (timeTo != null)
                        condition = timeToString;
                    else
                        condition = null;

                    break;

                case PriceRule.ConditionType.Date:
                    DateTime? dateFrom = (DateTime?) values [0];
                    DateTime? dateTo = (DateTime?) values [1];

                    string dateFromString = dateFrom != null ?
                        String.Format ("{0} {1}", Translator.GetString ("from"), BusinessDomain.GetFormattedDate (dateFrom.Value)) :
                        null;

                    string dateToString = dateTo != null ?
                        String.Format ("{0} {1}", Translator.GetString ("to"), BusinessDomain.GetFormattedDate (dateTo.Value)) :
                        null;

                    if (dateFrom != null && dateTo != null)
                        condition = String.Format ("{0} {1}", dateFromString, dateToString);
                    else if (dateFrom != null)
                        condition = dateFromString;
                    else if (dateTo != null)
                        condition = dateToString;
                    else
                        condition = null;

                    break;

                case PriceRule.ConditionType.DocumentSum:
                case PriceRule.ConditionType.TurnoverSum:
                case PriceRule.ConditionType.GoodQttySum:
                case PriceRule.ConditionType.PaymentSum:
                case PriceRule.ConditionType.UnpaidDocumentsSum:
                    double? from = values [0] == null ? (double?) null : Convert.ToDouble (values [0]);
                    double? to = values [1] == null ? (double?) null : Convert.ToDouble (values [1]);
                    string fromString;
                    string toString;

                    if (from != null)
                        fromString = String.Format ("{0} {1}", Translator.GetString ("from"), type == PriceRule.ConditionType.GoodQttySum ?
                            Quantity.ToString (from.Value) : Currency.ToString (from.Value, PriceType.Unknown));
                    else
                        fromString = null;

                    if (to != null)
                        toString = String.Format ("{0} {1}", Translator.GetString ("to"), type == PriceRule.ConditionType.GoodQttySum ?
                            Quantity.ToString (to.Value) : Currency.ToString (to.Value, PriceType.Unknown));
                    else
                        toString = null;

                    if (from != null && to != null)
                        condition = String.Format ("{0} {1}", fromString, toString);
                    else if (from != null)
                        condition = fromString;
                    else if (to != null)
                        condition = toString;
                    else
                        condition = null;

                    break;

                case PriceRule.ConditionType.Weekdays:
                    condition = String.Join (" ", ((IList<DayOfWeek>) values [0])
                        .Select (DateTimeFormatInfo.CurrentInfo.GetAbbreviatedDayName)
                        .ToArray ());
                    break;

                case PriceRule.ConditionType.PaymentTypeUsed:
                    condition = String.Join (" ", ((IList<BasePaymentType>) values [0])
                        .Select (PaymentType.GetBasePaymentTypeName)
                        .ToArray ());
                    break;

                case PriceRule.ConditionType.DatabaseUpdated:
                    DateTime now = DateTime.Now;
                    condition = String.Format (Translator.GetString ("Sooner than {0}"), now.AddMinutes (-30).ToFriendlyTimeAgoString (now));
                    break;
            }

            if (condition == null)
                error = true;

            return String.Format ("{0}: {1}", TypeToString (type),
                condition ?? AllMissingErrors [type]);
        }
    }
}
