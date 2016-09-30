//
// PriceRuleAction.cs
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
using Warehouse.Data.Calculator;

namespace Warehouse.Business.Entities
{
    public class PriceRuleAction
    {
        private const string DOCUMENT_SUM = "DocumentSum";
        private static readonly IDictionary<PriceGroup, string> priceGroupAliases = new Dictionary<PriceGroup, string> {
                { PriceGroup.TradePrice, "goodprcout01" },
                { PriceGroup.RegularPrice, "goodprcout02" },
                { PriceGroup.PriceGroup1, "goodprcout03" },
                { PriceGroup.PriceGroup2, "goodprcout04" },
                { PriceGroup.PriceGroup3, "goodprcout05" },
                { PriceGroup.PriceGroup4, "goodprcout06" },
                { PriceGroup.PriceGroup5, "goodprcout07" },
                { PriceGroup.PriceGroup6, "goodprcout08" },
                { PriceGroup.PriceGroup7, "goodprcout09" },
                { PriceGroup.PriceGroup8, "goodprcout10" },
                { PriceGroup.TradeInPrice, "goodprcin" },
                { PriceGroup.RegularPriceInOperation, "price" }
            };


        public bool IsActive { get; set; }

        private readonly PriceRule.ActionType type;
        public PriceRule.ActionType Type
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

        private PriceRuleAction (PriceRule.ActionType type, string formula)
        {
            IsActive = true;
            this.type = type;
            SetFormula (formula);
        }

        public PriceRuleAction (PriceRule.ActionType type, params object [] values)
        {
            IsActive = true;
            this.type = type;
            SetValues (values);
        }

        public PriceRule.Result Apply<T> (PriceRule priceRule, Operation<T> operation) where T : OperationDetail
        {
            if (error)
                return PriceRule.Result.Success;

            string stringValue;
            switch (Type) {
                case PriceRule.ActionType.Stop:
                    BusinessDomain.OnPriceRuleMessage (new PriceRuleMessageEventArgs (values [0].ToString (), ErrorSeverity.Warning));
                    return PriceRule.Result.StopOperation;

                case PriceRule.ActionType.Exit:
                    BusinessDomain.OnPriceRuleMessage (new PriceRuleMessageEventArgs (values [0].ToString (), ErrorSeverity.Warning));
                    return PriceRule.Result.StopRules;

                case PriceRule.ActionType.Message:
                    BusinessDomain.OnPriceRuleMessage (new PriceRuleMessageEventArgs (values [0].ToString (), ErrorSeverity.Information));
                    break;

                case PriceRule.ActionType.Email:
                    BusinessDomain.WorkflowManager.SendEmailAsync (operation, values [0].ToString (), values [1].ToString ());
                    break;

                case PriceRule.ActionType.ServiceCharge:
                    List<T> detailsWithService = new List<T> ();
                    const PriceRule.AppliedActions mask = PriceRule.AppliedActions.ServiceChargeItem;
                    detailsWithService.AddRange (operation.Details.Where (d =>
                        priceRule.CheckApplicableToDetail (operation.Details, d, mask)));
                    detailsWithService.AddRange (operation.AdditionalDetails.Where (d =>
                        priceRule.CheckApplicableToDetail (operation.AdditionalDetails, d, mask)));

                    Item item = (Item) values [0];
                    PriceGroup priceGroup = operation.GetPriceGroup ();
                    double operationTotal = detailsWithService.Sum (detail => detail.Total);
                    double serviceChargeAmount;
                    if (values [1] is double) {
                        serviceChargeAmount = operationTotal * (double) values [1] / 100;
                    } else {
                        string expression = (string) values [1];
                        try {
                            serviceChargeAmount = RPNCalculator.EvaluateExpression (expression.Replace (DOCUMENT_SUM, operationTotal.ToString (CultureInfo.InvariantCulture)));
                        } catch (Exception ex) {
                            ErrorHandling.LogException (ex);
                            serviceChargeAmount = 0;
                        }
                    }

                    if (serviceChargeAmount > 0) {
                        item.SetPriceGroupPrice (priceGroup, serviceChargeAmount);
                        string note = operation.Note;
                        T detail = operation.AddNewDetail ();
                        if (detail.ItemEvaluate (item, priceGroup, true)) {
                            detail.Note = note;
                            detail.AppliedPriceRules = PriceRule.AppliedActions.ServiceChargeItem;
                        } else
                            operation.RemoveDetail (operation.Details.Count - 1, false);
                    }
                    break;

                case PriceRule.ActionType.AddGlobalGood:
                    foreach (T detail in GetDetailsForPromotionalItems<T> (formula)) {
                        if (operation.OperationType == OperationType.RestaurantOrder)
                            detail.LotId = Int32.MinValue;
                        operation.Details.Add (detail);
                    }
                    break;

                case PriceRule.ActionType.Payment:
                    Payment payment = new Payment (operation, (int) BasePaymentType.Coupon, PaymentMode.Paid);
                    stringValue = PriceRule.GetStringValue (formula, ' ');
                    try {
                        payment.Quantity = RPNCalculator.EvaluateExpression (stringValue
                            .Replace (DOCUMENT_SUM, operation.TotalPlusVAT.ToString (CultureInfo.InvariantCulture)));
                        payment.CommitAdvance ();
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                    }
                    break;

                case PriceRule.ActionType.AskAdvancePayment:
                    stringValue = PriceRule.GetStringValue (formula, ' ');
                    try {
                        BusinessDomain.OnPriceRulePriceRuleAskForAdvance (new PriceRuleAskAdvanceEventArgs (operation.PartnerId,
                            RPNCalculator.EvaluateExpression (stringValue.Replace (DOCUMENT_SUM, operation.TotalPlusVAT.ToString (CultureInfo.InvariantCulture)))));
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                    }
                    break;
            }

            return PriceRule.Result.Success;
        }

        public void Apply<T> (T operationDetail, Operation<T> operation) where T : OperationDetail
        {
            if (error)
                return;

            switch (Type) {
                case PriceRule.ActionType.AddGood:
                    if (operation != null)
                        foreach (T detail in GetDetailsForPromotionalItems<T> (formula)) {
                            if (operation.OperationType == OperationType.RestaurantOrder)
                                detail.LotId = Int32.MinValue;
                            detail.PromotionForDetailHashCode = operationDetail.GetHashCode ();
                            operation.Details.Add (detail);
                            operationDetail.AppliedPriceRules |= PriceRule.AppliedActions.PromotionalItemSource;
                        }
                    break;

                case PriceRule.ActionType.Price:
                    PriceGroup priceGroup;
                    string [] parts = GetPriceExpressionParts (formula, out priceGroup);
                    if (parts.Length > 1) {
                        double price;
                        switch (priceGroup) {
                            case PriceGroup.TradeInPrice:
                                price = operationDetail.OriginalPriceIn;
                                break;
                            case PriceGroup.RegularPriceInOperation:
                                price = operationDetail.OriginalPriceOut;
                                break;
                            default:
                                Item item = Item.GetById (operationDetail.ItemId);
                                price = item.GetPriceGroupPrice (priceGroup);
                                break;
                        }

                        try {
                            operationDetail.OriginalPriceOut = RPNCalculator.EvaluateExpression (price + parts [1] + parts [2]);
                        } catch (Exception ex) {
                            ErrorHandling.LogException (ex);
                            break;
                        }
                    } else
                        operationDetail.OriginalPriceOut = Double.Parse (parts [0], CultureInfo.InvariantCulture);

                    operationDetail.PriceOutEvaluate ();
                    operationDetail.AppliedPriceRules |= PriceRule.AppliedActions.PriceChanged;
                    break;

                case PriceRule.ActionType.Discount:
                    operationDetail.DiscountEvaluate ((double) values [0]);
                    operationDetail.AppliedPriceRules |= PriceRule.AppliedActions.DiscountChanged;
                    break;
            }
        }

        private static string [] GetPriceExpressionParts (string actionString, out PriceGroup priceGroup)
        {
            priceGroup = PriceGroup.TradePrice;
            string priceExpression = PriceRule.GetStringValue (actionString);
            if (priceExpression == null)
                return null;

            string [] parts = priceExpression.Split (' ');
            if (parts.Length > 1) {
                // price group expression
                string priceGroupAlias = parts [0];
                priceGroup = priceGroupAliases.Where (pair => pair.Value == priceGroupAlias).Select (pair => pair.Key).FirstOrDefault ();
            }

            return parts;
        }

        private static List<T> GetDetailsForPromotionalItems<T> (string actionString) where T : OperationDetail
        {
            string itemsToPromote = PriceRule.GetStringValue (actionString, ' ');
            if (itemsToPromote == null)
                return null;

            List<T> details = new List<T> ();
            foreach (string itemString in itemsToPromote.Split ('|')) {
                string [] itemData = itemString.Split (';');
                if (itemData.Length != 3)
                    continue;

                long itemId;
                if (!long.TryParse (itemData [0], out itemId))
                    continue;

                double quantity;
                if (!Double.TryParse (itemData [1], NumberStyles.Any, CultureInfo.InvariantCulture, out quantity))
                    continue;

                double price;
                if (!Double.TryParse (itemData [2], NumberStyles.Any, CultureInfo.InvariantCulture, out price))
                    continue;

                Item item = Item.Cache.GetById (itemId);
                if (item == null)
                    continue;

                T detail = (T) Activator.CreateInstance (typeof (T));
                detail.AppliedPriceRules = PriceRule.AppliedActions.PromotionalItem;
                if (!detail.ItemEvaluate (item, PriceGroup.RegularPrice))
                    continue;

                detail.Quantity = quantity;
                detail.OriginalPriceOutEvaluate (price);
                details.Add (detail);
            }

            return details;
        }

        internal static bool Parse (string actionsString, List<PriceRuleAction> actions)
        {
            string actionTypeName = actionsString.Split (' ', '=') [0];
            PriceRule.ActionType aType;
            if (!Enum.TryParse (actionTypeName, true, out aType))
                return false;

            actions.Add (new PriceRuleAction (aType, actionsString));
            return true;
        }

        internal static string GetFormula (IEnumerable<PriceRuleAction> actions)
        {
            return String.Join (PriceRule.AND, actions.Select (a => a.formula));
        }

        private void SetFormula (string value)
        {
            if (formula == value)
                return;

            formula = value;
            values = null;
            string stringValue;
            switch (Type) {
                case PriceRule.ActionType.Stop:
                case PriceRule.ActionType.Exit:
                case PriceRule.ActionType.Message:
                    values = new object [] { PriceRule.GetStringValue (formula, ' ') };
                    break;

                case PriceRule.ActionType.Email:
                    stringValue = PriceRule.GetStringValue (formula, ' ');
                    if (stringValue == null)
                        break;

                    int lastIndexOf = stringValue.LastIndexOf ('|');
                    if (lastIndexOf < 0)
                        break;

                    values = new object [] { stringValue.Substring (lastIndexOf + 1), stringValue.Substring (0, lastIndexOf) };
                    break;

                case PriceRule.ActionType.AddGood:
                case PriceRule.ActionType.AddGlobalGood:
                    List<SaleDetail> details = GetDetailsForPromotionalItems<SaleDetail> (formula);
                    if (details != null)
                        values = details.Cast<object> ().ToArray ();
                    break;

                case PriceRule.ActionType.Price:
                    PriceGroup priceGroup;
                    string [] priceParts = GetPriceExpressionParts (formula, out priceGroup);
                    if (priceParts == null || priceParts.Length == 0)
                        break;

                    double price;
                    if (priceParts.Length > 1) {
                        OperatorType operatorType;
                        switch (priceParts [1].ToLowerInvariant ()) {
                            case "+":
                            case "plus":
                                operatorType = OperatorType.Plus;
                                break;
                            case "-":
                            case "minus":
                                operatorType = OperatorType.Minus;
                                break;
                            case "*":
                                operatorType = OperatorType.Multiply;
                                break;
                            case "/":
                                operatorType = OperatorType.Divide;
                                break;
                            default:
                                operatorType = OperatorType.Unknown;
                                break;
                        }
                        if (operatorType == OperatorType.Unknown)
                            break;

                        if (Double.TryParse (priceParts [2], NumberStyles.Any, CultureInfo.InvariantCulture, out price))
                            values = new object [] { priceGroup, operatorType, price };
                    } else
                        if (Double.TryParse (priceParts [0], NumberStyles.Any, CultureInfo.InvariantCulture, out price))
                            values = new object [] { price };
                    break;

                case PriceRule.ActionType.Discount:
                    values = new object [] { PriceRule.GetDoubleValue (formula) };
                    break;

                case PriceRule.ActionType.ServiceCharge:
                    string [] itemWithPercent = formula.Split (';');
                    if (itemWithPercent.Length < 2)
                        break;

                    long? itemId = PriceRule.GetLongValue (itemWithPercent [0]);
                    if (itemId == null)
                        break;

                    Item item = Item.Cache.GetById (itemId.Value);
                    if (item == null)
                        break;

                    string s = PriceRule.GetStringValue (itemWithPercent [1]);
                    if (s == null)
                        break;

                    double result;
                    values = new [] { item,
                        Double.TryParse (s, NumberStyles.Any, CultureInfo.InvariantCulture, out result) ? (object) result : s };
                    break;

                case PriceRule.ActionType.Payment:
                case PriceRule.ActionType.AskAdvancePayment:
                    stringValue = PriceRule.GetStringValue (formula, ' ');
                    if (stringValue == null)
                        break;

                    try {
                        List<Token> tokens = RPNCalculator.ParseTokens (stringValue);
                        if (tokens.Count > 0)
                            values = new object [] { ((Operand) tokens [0]).Value };
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                    }
                    break;
            }

            error = values == null || values.All (v => v == null);
        }

        private void SetValues (object [] newValues)
        {
            values = newValues;
            formula = null;
            switch (Type) {
                case PriceRule.ActionType.Stop:
                case PriceRule.ActionType.Exit:
                case PriceRule.ActionType.Message:
                    formula = String.Format ("{0} {1}", Type, newValues [0]);
                    break;

                case PriceRule.ActionType.Email:
                    formula = String.Format ("{0} {1}|{2}", Type, newValues [0], newValues [1]);
                    break;

                case PriceRule.ActionType.AddGood:
                case PriceRule.ActionType.AddGlobalGood:
                    StringBuilder itemBuilder = new StringBuilder ();
                    foreach (OperationDetail detail in newValues)
                        itemBuilder.Append (String.Format (CultureInfo.InvariantCulture, "{0};{1};{2}|", detail.ItemId, detail.Quantity, detail.PriceOut));

                    itemBuilder.Remove (itemBuilder.Length - 1, 1);
                    formula = String.Format ("{0} {1}", Type, itemBuilder);
                    break;

                case PriceRule.ActionType.Price:
                    if (newValues.Length > 1) {
                        string priceGroupAlias = priceGroupAliases [(PriceGroup) newValues [0]];
                        string operatorString = String.Empty;
                        switch ((OperatorType) newValues [1]) {
                            case OperatorType.Plus:
                                operatorString = "+";
                                break;
                            case OperatorType.Minus:
                                operatorString = "-";
                                break;
                            case OperatorType.Multiply:
                                operatorString = "*";
                                break;
                            case OperatorType.Divide:
                                operatorString = "/";
                                break;
                        }
                        formula = String.Format (CultureInfo.InvariantCulture, "{0}={1} {2} {3}", Type, priceGroupAlias, operatorString, newValues [2]);
                    } else
                        formula = String.Format (CultureInfo.InvariantCulture, "{0}={1}", Type, newValues [0]);
                    break;

                case PriceRule.ActionType.Discount:
                    formula = String.Format (CultureInfo.InvariantCulture, "{0}={1}", Type, newValues [0]);
                    break;

                case PriceRule.ActionType.ServiceCharge:
                    formula = String.Format (CultureInfo.InvariantCulture, "{0}={1};{2}", Type, ((Item) newValues [0]).Id, newValues [1]);
                    break;

                case PriceRule.ActionType.Payment:
                case PriceRule.ActionType.AskAdvancePayment:
                    formula = String.Format (CultureInfo.InvariantCulture, "{0} {1} * {2} / 100", Type, newValues [0], DOCUMENT_SUM);
                    break;

                default:
                    throw new ArgumentOutOfRangeException ("Type", Type, "Unknown type");
            }
        }

        public static string TypeToString (PriceRule.ActionType actionType)
        {
            switch (actionType) {
                case PriceRule.ActionType.Stop:
                    return Translator.GetString ("Stop operation");

                case PriceRule.ActionType.Exit:
                    return Translator.GetString ("Stop price rules");

                case PriceRule.ActionType.Message:
                    return Translator.GetString ("Message");

                case PriceRule.ActionType.Email:
                    return Translator.GetString ("Send e-mail");

                case PriceRule.ActionType.AddGood:
                    return Translator.GetString ("Add promotional item to the operation");

                case PriceRule.ActionType.AddGlobalGood:
                    return Translator.GetString ("Add promotional item once for the operation");

                case PriceRule.ActionType.Price:
                    return Translator.GetString ("Change price");

                case PriceRule.ActionType.Discount:
                    return Translator.GetString ("Change discount");

                case PriceRule.ActionType.ServiceCharge:
                    return Translator.GetString ("Service charge");

                case PriceRule.ActionType.Payment:
                    return Translator.GetString ("Add advance");

                case PriceRule.ActionType.AskAdvancePayment:
                    return Translator.GetString ("Ask for advance deposit");
            }
            return String.Empty;
        }

        public override string ToString ()
        {
            string action = null;
            Item item;
            if (!error)
                switch (Type) {
                    case PriceRule.ActionType.Stop:
                    case PriceRule.ActionType.Exit:
                    case PriceRule.ActionType.Message:
                        action = values.Length > 0 ? values [0].ToString () : null;
                        break;

                    case PriceRule.ActionType.Email:
                        action = values.Length > 1 ? String.Format ("{0}: {1}; {2}: {3}",
                            Translator.GetString ("Message"), values [0],
                            Translator.GetString ("Receiver"), values [1]) : null;
                        break;

                    case PriceRule.ActionType.AddGood:
                    case PriceRule.ActionType.AddGlobalGood:
                        List<string> ret = new List<string> ();
                        if (values != null) {
                            foreach (SaleDetail saleDetail in values.OfType<SaleDetail> ()) {
                                item = Item.Cache.GetById (saleDetail.ItemId);
                                if (item == null)
                                    continue;

                                ret.Add (String.Format ("{0}: {1}; {2}: {3}; {4}: {5}",
                                    Translator.GetString ("Item"),
                                    item.Name,
                                    Translator.GetString ("Quantity"),
                                    Quantity.ToString (saleDetail.Quantity),
                                    Translator.GetString ("Price"),
                                    Currency.ToString (saleDetail.PriceOut)));
                            }
                        }

                        action = ret.Count > 0 ? String.Join (Environment.NewLine, ret) : null;
                        break;

                    case PriceRule.ActionType.Price:
                        if (values == null || values.Length == 0)
                            break;

                        if (values.Length == 1) {
                            action = Currency.ToString ((double) values [0]);
                            break;
                        }

                        if (values.Length < 3)
                            break;

                        string priceGroupString = String.Empty;
                        foreach (KeyValuePair<int, string> keyValuePair in Currency.GetAllPriceRulePriceGroups ())
                            if (keyValuePair.Key == (int) values [0]) {
                                priceGroupString = keyValuePair.Value;
                                break;
                            }

                        string operatorString = String.Empty;
                        switch ((OperatorType) values [1]) {
                            case OperatorType.Plus:
                                operatorString = "+";
                                break;
                            case OperatorType.Minus:
                                operatorString = "-";
                                break;
                            case OperatorType.Multiply:
                                operatorString = "*";
                                break;
                            case OperatorType.Divide:
                                operatorString = "/";
                                break;
                        }
                        return String.Format ("{0} {1} {2}", priceGroupString, operatorString, (double) values [2]);

                    case PriceRule.ActionType.Discount:
                        return Percent.ToString ((double) values [0]);

                    case PriceRule.ActionType.ServiceCharge:
                        item = (Item) values [0];
                        return String.Format ("{0}: {1}; {2}: {3}",
                            Translator.GetString ("Item"),
                            item.Name,
                            values [1] is double ? Translator.GetString ("Percent") : Translator.GetString ("Value"),
                            values [1] is double ? Percent.ToString ((double) values [1]) : values [1]);

                    case PriceRule.ActionType.Payment:
                    case PriceRule.ActionType.AskAdvancePayment:
                        return String.Format ("{0} {1}", Percent.ToString ((double) values [0]), Translator.GetString ("of operation total"));

                    default:
                        action = formula;
                        break;
                }

            return action ?? Translator.GetString ("Error!");
        }

        public PriceRuleAction Clone ()
        {
            return new PriceRuleAction (type, formula) { IsActive = IsActive };
        }
    }
}
