//
// PriceRule.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   11.05.2009
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
using Warehouse.Data.Model;

namespace Warehouse.Business.Entities
{
    public class PriceRule : ICloneable
    {
        public delegate List<PriceRule> ConfirmationToApply<T> (IEnumerable<PriceRule> rulesToApply, Operation<T> operation) where T : OperationDetail;

        public const string AND = " AND ";

        public enum ConditionType
        {
            Partner,
            PartnerGroup,
            Object,
            ObjectGroup,
            User,
            UserGroup,
            Good,
            GoodGroup,
            Time,
            Date,
            DocumentSum,
            TurnoverSum,
            GoodQttySum,
            PaymentSum,
            Weekdays,
            UnpaidDocumentsSum,
            ContainsGood,
            ContainsGGroup,
            PaymentTypeUsed,
            DatabaseUpdated
        }

        public enum ActionType
        {
            Stop,
            Exit,
            Message,
            Email,
            AddGood,
            AddGlobalGood,
            Price,
            Discount,
            ServiceCharge,
            Payment,
            AskAdvancePayment
        }

        public enum Result
        {
            Success,
            StopOperation,
            StopRules,
            NoRules
        }

        [Flags]
        public enum AppliedActions
        {
            None = 0,
            PriceChanged = 1,
            DiscountChanged = 2,
            PromotionalItemSource = 4,
            PromotionalItem = 8,
            ServiceChargeItem = 16,
            All = PriceChanged | DiscountChanged | PromotionalItemSource | PromotionalItem | ServiceChargeItem
        }

        private readonly List<PriceRuleCondition> conditions;
        private readonly List<PriceRuleAction> actions;
        private readonly List<OperationType> operations;

        private bool enabled;
        private int enabledDb;

        [DbColumn (DataField.PriceRuleId)]
        public long Id { get; set; }

        [DbColumn (DataField.PriceRuleName, 255)]
        public string Name { get; set; }

        [DbColumn (DataField.PriceRuleFormula, 1000)]
        public string Formula { get; set; }

        [DbColumn (DataField.PriceRuleEnabled)]
        public int EnabledDB
        {
            get { return enabledDb; }
            set
            {
                if (enabledDb != value) {
                    enabledDb = value;
                    // not comparing to -1 for backward compatibility
                    enabled = value != 0;
                }
            }
        }

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (enabled != value) {
                    enabled = value;
                    enabledDb = value ? -1 : 0;
                }
            }
        }

        [DbColumn (DataField.PriceRulePriority)]
        public int Priority { get; set; }

        public List<PriceRuleCondition> Conditions
        {
            get { return conditions; }
        }

        public List<PriceRuleAction> Actions
        {
            get { return actions; }
        }

        public List<OperationType> Operations
        {
            get { return operations; }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="PriceRule"/> applies to the 
        /// whole operation as opposed to on some or all of its details.
        /// </summary>
        /// <value><c>true</c> if this <see cref="PriceRule"/> applies to the whole operation; otherwise, <c>false</c>.</value>
        public bool IsGlobal
        {
            get
            {
                return !actions.Any (a => a.Type == ActionType.AddGood ||
                    a.Type == ActionType.Price ||
                    a.Type == ActionType.Discount);
            }
        }

        public PriceRule ()
        {
            Enabled = true;

            conditions = new List<PriceRuleCondition> ();
            actions = new List<PriceRuleAction> ();
            operations = new List<OperationType> ();
        }

        public override string ToString ()
        {
            StringBuilder ret = new StringBuilder ();
            List<PriceRuleCondition> cond = conditions.FindAll (c => !c.IsException);
            if (cond.Count > 0)
                ret.AppendFormat ("{0}:{1}{2}{1}",
                    Translator.GetString ("Conditions"),
                    Environment.NewLine,
                    string.Join (Environment.NewLine, cond));

            List<PriceRuleCondition> exceptions = conditions.FindAll (c => c.IsException);
            if (exceptions.Count > 0) {
                if (ret.Length > 0)
                    ret.Append (Environment.NewLine);

                ret.AppendFormat ("{0}:{1}{2}{1}",
                    Translator.GetString ("Exceptions"),
                    Environment.NewLine,
                    string.Join (Environment.NewLine, exceptions));
            }

            ret.AppendFormat ("{1}{0}:{1}{2}{1}",
                Translator.GetString ("Actions"),
                Environment.NewLine,
                string.Join (Environment.NewLine, actions.Select (a => string.Format ("{0}: {1}", PriceRuleAction.TypeToString (a.Type), a))));

            ret.AppendFormat ("{1}{0}:{1}{2}",
                Translator.GetString ("Operations"),
                Environment.NewLine,
                string.Join (Environment.NewLine, operations.Select (Translator.GetOperationTypeGlobalName)));

            return ret.ToString ();
        }

        private bool DependsOnDocumentSum ()
        {
            return conditions.Any (c => c.Type == ConditionType.DocumentSum);
        }

        private void BuildFormula ()
        {
            if (conditions.Count == 0)
                throw new InvalidOperationException ("A price rule must contain at least one condition.");
            if (actions.Count == 0)
                throw new InvalidOperationException ("A price rule must contain at least one action.");
            if (operations.Count == 0)
                throw new InvalidOperationException ("A price rule must apply to at least one operation.");

            Formula = string.Format ("IF {0} AND (operation = {1}) THEN {2}",
                PriceRuleCondition.GetFormula (conditions),
                string.Join (",", operations.Cast<int> ()),
                PriceRuleAction.GetFormula (actions));
        }

        private bool ParseFormula ()
        {
            string [] conditionsAndActions = Formula.Split (new [] { " THEN " }, StringSplitOptions.RemoveEmptyEntries);
            if (conditionsAndActions.Length != 2)
                return false;

            string prerequisitesString = conditionsAndActions [0];
            int operationsStart = prerequisitesString.IndexOf ("(operation", StringComparison.Ordinal);
            string conditionsString = prerequisitesString.Substring (0, operationsStart);
            string operationsString = prerequisitesString.Substring (operationsStart);

            string actionsString = conditionsAndActions [1];

            return PriceRuleCondition.Parse (conditionsString, conditions) &&
                ParseOperations (operationsString) &&
                PriceRuleAction.Parse (actionsString, actions);
        }

        public static long? GetLongValue (string expression)
        {
            string stringValue = GetStringValue (expression);
            if (stringValue == null)
                return null;

            long result;
            return long.TryParse (stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result) ?
                result : (long?) null;
        }

        public static double? GetDoubleValue (string expression)
        {
            string stringValue = GetStringValue (expression);
            if (stringValue == null)
                return null;

            double result;
            return double.TryParse (stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result) ?
                result : (double?) null;
        }

        public static string GetStringValue (string expression, char separator = '=')
        {
            if (expression == null)
                return null;

            return expression.Substring (expression.IndexOf (separator) + 1).Trim ();
        }

        private bool ParseOperations (string operationsString)
        {
            string operationTypes = operationsString.Substring (operationsString.IndexOf ('=') + 1);
            foreach (string operationType in operationTypes.Split (new [] { ',', ')' }, StringSplitOptions.RemoveEmptyEntries)) {
                OperationType type;
                if (!Enum.TryParse (operationType, true, out type))
                    return false;

                operations.Add (type);
            }
            return true;
        }

        public static IManageableListModel<PriceRule> GetAll ()
        {
            LazyListModel<PriceRule> allPriceRules = BusinessDomain.DataAccessProvider.GetAllPriceRules<PriceRule> ();
            return new BindingListModel<PriceRule> (allPriceRules.Where (p => p.ParseFormula ()));
        }

        public PriceRule CommitChanges ()
        {
            BuildFormula ();
            BusinessDomain.DataAccessProvider.AddUpdatePriceRule (this);
            return this;
        }

        public PriceRuleCondition AddCondition (ConditionType conditionType, params object [] values)
        {
            return AddCondition (conditionType, false, values);
        }

        public PriceRuleCondition AddCondition (ConditionType conditionType, bool isException, params object [] values)
        {
            PriceRuleCondition condition = conditions.Find (c => c.Type == conditionType && c.IsException == isException);
            try {
                if (condition == null) {
                    condition = new PriceRuleCondition (conditionType, values, isException);
                    conditions.Add (condition);
                } else {
                    condition.IsActive = true;
                    condition.SetValues (values);
                }
                return condition;
            } catch (Exception) {
                return null;
            }
        }

        public PriceRuleAction AddAction (ActionType actionType, params object [] values)
        {
            try {
                PriceRuleAction action = new PriceRuleAction (actionType, values);
                actions.Clear ();
                actions.Add (action);

                return action;
            } catch (ArgumentOutOfRangeException) {
                return null;
            }
        }

        public static bool ApplyBeforeOperationSaved<T> (Operation<T> operation,
            ConfirmationToApply<T> confirmationToApply = null,
            bool operationComplete = true,
            IEnumerable<PriceRule> allPriceRules = null) where T : OperationDetail
        {
            if (allPriceRules == null)
                allPriceRules = GetAll ();

            IDictionary<PriceRule, IList<T>> rulesToApply = new Dictionary<PriceRule, IList<T>> ();

            foreach (PriceRule priceRule in allPriceRules.Where (p => p.actions.All (a =>
                a.Type != ActionType.Email &&
                a.Type != ActionType.Payment &&
                a.Type != ActionType.AskAdvancePayment &&
                a.Type != ActionType.ServiceCharge) &&
                p.CheckApplicableToOperation (operation)))
                if (priceRule.IsGlobal) {
                    if (operationComplete)
                        rulesToApply.Add (priceRule, null);
                } else if (operationComplete == priceRule.DependsOnDocumentSum ()) {
                    priceRule.CheckApplicableDetails (operation.Details, ref rulesToApply);
                    priceRule.CheckApplicableDetails (operation.AdditionalDetails, ref rulesToApply);
                }

            List<PriceRule> selectedRulesToApply;
            if (BusinessDomain.AppConfiguration.ConfirmPriceRules &&
                confirmationToApply != null &&
                rulesToApply.Any (r => r.Key.actions.All (a => a.Type != ActionType.AddGlobalGood) && r.Value != null && r.Value.Count > 0))
                selectedRulesToApply = confirmationToApply (rulesToApply.Keys, operation);
            else
                selectedRulesToApply = new List<PriceRule> (rulesToApply.Keys);

            if (selectedRulesToApply.Count == 0)
                return true;

            foreach (KeyValuePair<PriceRule, IList<T>> ruleToApply in rulesToApply) {
                PriceRule priceRule = ruleToApply.Key;
                if (!selectedRulesToApply.Contains (priceRule))
                    continue;

                if (priceRule.IsGlobal) {
                    Result result = priceRule.Apply (operation);
                    if (result == Result.StopOperation)
                        return false;

                    if (result == Result.StopRules)
                        break;
                } else
                    foreach (T detail in ruleToApply.Value)
                        priceRule.Apply (detail, operation);
            }

            return true;
        }

        private void CheckApplicableDetails<T> (IList<T> details, ref IDictionary<PriceRule, IList<T>> rulesToApply) where T : OperationDetail
        {
            foreach (T detail in details)
                if (CheckApplicableToDetail (details, detail) &&
                    ((detail.DetailId < 0 && detail.Quantity > 0) || DependsOnDocumentSum ())) {
                    if (!rulesToApply.ContainsKey (this))
                        rulesToApply.Add (this, new List<T> ());

                    rulesToApply [this].Add (detail);
                }
        }

        public static void ReapplyOnLocationChanged<T> (Operation<T> operation, Action<string, Action> reapplyConfirmation, IListModel<PriceRule> allRules = null) where T : OperationDetail, new ()
        {
            if (operation.Details.Count == 0)
                return;

            if (allRules == null)
                allRules = GetAll ();

            if (!allRules.Where (p => p.Enabled).SelectMany (r => r.Conditions).Any (c =>
                {
                    if (c.Error || c.Values == null || c.Values.Length <= 0 || c.Values [0] == null)
                        return false;

                    switch (c.Type) {
                        case ConditionType.Object:
                            return (long) c.Values [0] == operation.LocationId;

                        case ConditionType.ObjectGroup:
                            string stringValue = (string) c.Values [0];

                            long longValue;
                            LocationsGroup locationsGroup = long.TryParse (stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out longValue) ?
                                LocationsGroup.Cache.GetById (longValue) :
                                LocationsGroup.Cache.GetByCode (stringValue);

                            if (locationsGroup == null)
                                return false;

                            Location location = Location.Cache.GetById (operation.LocationId);
                            if (location == null)
                                return false;

                            return locationsGroup.Id == location.GroupId;
                    }

                    return false;
                }))
                return;

            reapplyConfirmation (Translator.GetString ("You have changed the location. Would you like to reapply your price rules?"), () =>
                {
                    for (int i = operation.Details.Count - 1; i >= 0; i--)
                        if ((operation.Details [i].AppliedPriceRules & AppliedActions.PromotionalItem) == AppliedActions.PromotionalItem)
                            operation.RemoveDetail (i, false);
                        else
                            operation.Details [i].AppliedPriceRules = AppliedActions.None;

                    ApplyBeforeOperationSaved (operation, null, false, allRules);
                });
        }

        public static void ReapplyOnPartnerChanged<T> (Operation<T> operation, Action<string, Action> reapplyConfirmation, IListModel<PriceRule> allRules = null) where T : OperationDetail, new ()
        {
            if (operation.Details.Count == 0)
                return;

            if (allRules == null)
                allRules = GetAll ();

            if (!allRules.Where (p => p.Enabled).SelectMany (r => r.Conditions).Any (c =>
                {
                    if (c.Error || c.Values == null || c.Values.Length <= 0 || c.Values [0] == null)
                        return false;

                    switch (c.Type) {
                        case ConditionType.Partner:
                            return (long) c.Values [0] == operation.PartnerId;

                        case ConditionType.PartnerGroup:
                            string stringValue = (string) c.Values [0];

                            long longValue;
                            PartnersGroup partnersGroup = long.TryParse (stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out longValue) ?
                                PartnersGroup.Cache.GetById (longValue) :
                                PartnersGroup.Cache.GetByCode (stringValue);

                            if (partnersGroup == null)
                                return false;

                            Partner partner = Partner.Cache.GetById (operation.PartnerId);
                            if (partner == null)
                                return false;

                            return partnersGroup.Id == partner.GroupId;
                    }

                    return false;
                }))
                return;

            reapplyConfirmation (Translator.GetString ("You have changed the partner. Would you like to reapply your price rules?"), () =>
                {
                    for (int i = operation.Details.Count - 1; i >= 0; i--)
                        if ((operation.Details [i].AppliedPriceRules & AppliedActions.PromotionalItem) == AppliedActions.PromotionalItem)
                            operation.RemoveDetail (i, false);
                        else
                            operation.Details [i].AppliedPriceRules = AppliedActions.None;

                    ApplyBeforeOperationSaved (operation, null, false, allRules);
                });
        }

        public static void ReapplyOnUserChanged<T> (Operation<T> operation, Action<string, Action> reapplyConfirmation, IListModel<PriceRule> allRules = null) where T : OperationDetail, new ()
        {
            if (operation.Details.Count == 0)
                return;

            if (allRules == null)
                allRules = GetAll ();

            if (!allRules.Where (p => p.Enabled).SelectMany (r => r.Conditions).Any (c =>
                {
                    if (c.Error || c.Values == null || c.Values.Length <= 0 || c.Values [0] == null)
                        return false;

                    switch (c.Type) {
                        case ConditionType.User:
                            return (long) c.Values [0] == operation.PartnerId;

                        case ConditionType.UserGroup:
                            string stringValue = (string) c.Values [0];
                         
                            long longValue;
                            UsersGroup usersGroup = long.TryParse (stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out longValue) ?
                                UsersGroup.Cache.GetById (longValue) :
                                UsersGroup.Cache.GetByCode (stringValue);

                            if (usersGroup == null)
                                return false;

                            User user = User.Cache.GetById (operation.UserId);
                            if (user == null)
                                return false;

                            return usersGroup.Id == user.GroupId;
                    }

                    return false;
                }))
                return;

            reapplyConfirmation (Translator.GetString ("You have changed the operator. Would you like to reapply your price rules?"), () =>
                {
                    for (int i = operation.Details.Count - 1; i >= 0; i--)
                        if ((operation.Details [i].AppliedPriceRules & AppliedActions.PromotionalItem) == AppliedActions.PromotionalItem)
                            operation.RemoveDetail (i, false);
                        else
                            operation.Details [i].AppliedPriceRules = AppliedActions.None;

                    ApplyBeforeOperationSaved (operation, null, false, allRules);
                });
        }

        public static void ReapplyOnDateChanged<T> (Operation<T> operation, Action<string, Action> reapplyConfirmation, IListModel<PriceRule> allRules = null) where T : OperationDetail, new ()
        {
            if (operation.Details.Count == 0)
                return;

            if (allRules == null)
                allRules = GetAll ();

            if (allRules.Where (p => p.Enabled).SelectMany (r => r.Conditions).All (c => c.Type != ConditionType.Date))
                return;

            reapplyConfirmation (Translator.GetString ("You have changed the date. Would you like to reapply your price rules?"), () =>
                {
                    for (int i = operation.Details.Count - 1; i >= 0; i--)
                        if ((operation.Details [i].AppliedPriceRules & AppliedActions.PromotionalItem) == AppliedActions.PromotionalItem)
                            operation.RemoveDetail (i, false);
                        else
                            operation.Details [i].AppliedPriceRules = AppliedActions.None;

                    ApplyBeforeOperationSaved (operation, null, false, allRules);
                });
        }

        public static bool TryAddServiceCharge<T, TDetail> (T operation,
            ConfirmationToApply<TDetail> confirmationToApply = null,
            IEnumerable<PriceRule> allRules = null)
            where T : Operation<TDetail>, new ()
            where TDetail : OperationDetail, new ()
        {
            List<PriceRule> serviceRules = new List<PriceRule> ();
            foreach (PriceRule priceRule in (allRules ?? GetAll ())
                .Where (p => p.actions.Any (a => a.Type == ActionType.ServiceCharge) &&
                    p.CheckApplicableToOperation (operation)))
                if (BusinessDomain.AppConfiguration.ConfirmPriceRules)
                    serviceRules.Add (priceRule);
                else {
                    Result result = priceRule.Apply (operation);
                    if (result == Result.StopOperation)
                        return false;

                    if (result == Result.StopRules)
                        break;
                }

            if (!BusinessDomain.AppConfiguration.ConfirmPriceRules)
                return true;

            T clone = operation.Clone<T, TDetail> ();
            int countNoService = clone.Details.Count;
            foreach (PriceRule priceRule in serviceRules) {
                Result result = priceRule.Apply (clone);
                if (result == Result.StopOperation)
                    return false;

                if (result == Result.StopRules)
                    break;
            }
            List<TDetail> serviceCharges = new List<TDetail> (clone.Details.Count - countNoService);
            for (int i = countNoService; i < clone.Details.Count; i++)
                serviceCharges.Add (clone.Details [i]);

            if (serviceCharges.Count > 0)
                foreach (PriceRule priceRule in confirmationToApply != null ?
                    confirmationToApply (serviceRules, operation) : serviceRules)
                    priceRule.Apply (operation);

            return true;
        }

        public static bool ApplyOnPaymentSet<T> (Operation<T> operation, IEnumerable<PriceRule> allRules = null) where T : OperationDetail, new ()
        {
            foreach (PriceRule priceRule in (allRules ?? GetAll ()).Where (p =>
                !operation.AppliedPriceRules.Contains (p.Id) && p.conditions.Any (c => c.Type == ConditionType.PaymentTypeUsed)))
                if (priceRule.CheckApplicableToOperation (operation) && priceRule.Apply (operation) != Result.Success)
                    return false;

            return true;
        }

        public static bool ApplyOnOperationSaved<T> (Operation<T> operation, IEnumerable<PriceRule> allRules = null) where T : OperationDetail, new ()
        {
            foreach (PriceRule priceRule in (allRules ?? GetAll ()).Where (p =>
                !operation.AppliedPriceRules.Contains (p.Id) && p.actions.Any (a => a.Type == ActionType.Payment)))
                if (priceRule.CheckApplicableToOperation (operation) && priceRule.Apply (operation) != Result.Success)
                    return false;

            return true;
        }

        public static void ApplyAfterOperationSaved<T> (Operation<T> operation, IEnumerable<PriceRule> allRules = null) where T : OperationDetail, new ()
        {
            foreach (PriceRule priceRule in (allRules ?? GetAll ()).Where (p =>
                !operation.AppliedPriceRules.Contains (p.Id) && p.actions.Any (a =>
                a.Type == ActionType.Email ||
                a.Type == ActionType.AskAdvancePayment)))
                if (priceRule.CheckApplicableToOperation (operation) && priceRule.Apply (operation) != Result.Success)
                    break;
        }

        public static void RemoveAddedDetails<T> (Operation<T> operation) where T : OperationDetail, new ()
        {
            for (int i = operation.Details.Count - 1; i >= 0; i--) {
                T detail = operation.Details [i];
                detail.AppliedPriceRules &= ~AppliedActions.PromotionalItemSource;
                if ((detail.AppliedPriceRules & (AppliedActions.PromotionalItem | AppliedActions.ServiceChargeItem))
                    != AppliedActions.None)
                    operation.Details.RemoveAt (i);
            }
        }

        public static void RollbackOnOperationSaveFailed<T> (Operation<T> operation) where T : OperationDetail, new ()
        {
            operation.AppliedPriceRules.Clear ();
        }

        public static List<KeyValuePair<object, string>> GetAllConditions ()
        {
            return Enum.GetValues (typeof (ConditionType))
                .Cast<ConditionType> ()
                .Select (c => new KeyValuePair<object, string> (c, PriceRuleCondition.TypeToString (c)))
                .ToList ();
        }

        public static List<KeyValuePair<object, string>> GetAllActions ()
        {
            return Enum.GetValues (typeof (ActionType))
                .Cast<ActionType> ()
                .Select (a => new KeyValuePair<object, string> (a, PriceRuleAction.TypeToString (a)))
                .ToList ();
        }

        public static List<KeyValuePair<object, string>> GetAllExceptions ()
        {
            return new []
                {
                    ConditionType.Partner,
                    ConditionType.PartnerGroup,
                    ConditionType.Object,
                    ConditionType.ObjectGroup,
                    ConditionType.User,
                    ConditionType.UserGroup,
                    ConditionType.Good,
                    ConditionType.GoodGroup,
                    ConditionType.PaymentTypeUsed,
                    ConditionType.DatabaseUpdated
                }
                .Select (c => new KeyValuePair<object, string> (c, PriceRuleCondition.TypeToString (c)))
                .ToList ();
        }

        public static DeletePermission RequestDelete (long priceRuleId)
        {
            return BusinessDomain.DataAccessProvider.CanDeletePriceRule (priceRuleId);
        }

        public static void Delete (long priceRuleId)
        {
            BusinessDomain.DataAccessProvider.DeletePriceRule (priceRuleId);
        }

        public bool Validate (ValidateCallback callback, StateHolder state)
        {
            if (callback == null)
                throw new ArgumentNullException ("callback");

            if (string.IsNullOrEmpty (Name) &&
                !callback (Translator.GetString ("The name of a price rule must not be empty."),
                    ErrorSeverity.Error, 0, state)) {
                return false;
            }

            if (conditions.FindAll (condition => condition.IsActive).Count == 0 &&
                !callback (Translator.GetString ("A price rule must contain at least one condition."),
                    ErrorSeverity.Error, 0, state))
                return false;

            if (actions.FindAll (action => action.IsActive).Count == 0 &&
                !callback (Translator.GetString ("A price rule must contain at least one action."),
                    ErrorSeverity.Error, 0, state))
                return false;

            if (operations.Count == 0 &&
                !callback (Translator.GetString ("A price rule must contain at least one operation."),
                    ErrorSeverity.Error, 0, state))
                return false;

            return true;
        }

        public bool CheckApplicableToOperation<T> (Operation<T> operation) where T : OperationDetail
        {
            return Enabled &&
                (operations.Contains (operation.OperationType) || (operations.Contains (OperationType.Sale) && operation.OperationType == OperationType.RestaurantOrder)) &&
                conditions.All (condition => condition.CheckApplicableToOperation (operation));
        }

        public bool CheckApplicableToDetail<T> (IList<T> operationDetails, OperationDetail operationDetail, AppliedActions maskApplied = AppliedActions.All) where T : OperationDetail
        {
            return (operationDetail.AppliedPriceRules & maskApplied) == AppliedActions.None &&
                conditions.All (condition => condition.CheckApplicableToDetail (operationDetails, operationDetail));
        }

        public Result Apply<T> (Operation<T> operation) where T : OperationDetail
        {
            operation.AppliedPriceRules.Add (Id);
            foreach (PriceRuleAction action in Actions) {
                Result ret = action.Apply (this, operation);
                if (ret != Result.Success)
                    return ret;
            }
            return Result.Success;
        }

        public void Apply<T> (T operationDetail, Operation<T> operation) where T : OperationDetail
        {
            operation.AppliedPriceRules.Add (Id);
            foreach (PriceRuleAction action in actions)
                action.Apply (operationDetail, operation);
        }

        public object Clone ()
        {
            PriceRule priceRule = new PriceRule { Id = Id, Name = Name, Formula = Formula, Enabled = Enabled, Priority = Priority };

            foreach (PriceRuleCondition newCondition in conditions.Select (c => new PriceRuleCondition (c.Type, c.Formula) { IsActive = c.IsActive }.SetIsException (c.IsException)))
                priceRule.conditions.Add (newCondition);

            foreach (PriceRuleAction newAction in actions.Select (a => a.Clone ()))
                priceRule.actions.Add (newAction);

            foreach (OperationType operationType in operations)
                priceRule.operations.Add (operationType);

            return priceRule;
        }
    }
}
