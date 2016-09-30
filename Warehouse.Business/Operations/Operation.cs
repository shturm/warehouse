//
// Operation.cs
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Warehouse.Business.Entities;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Data.Model;

namespace Warehouse.Business.Operations
{
    public class Operation<T> : Operation where T : OperationDetail
    {
        private readonly BindList<T> removedDetails = new BindList<T> ();
        private const int MAX_AUTO_PRODUCTION_DEPTH = 5;

        private bool? usePriceIn;

        protected BindList<T> details = new BindList<T> ();
        protected BindList<T> additionalDetails = new BindList<T> ();
        private HashSet<long> appliedPriceRules = new HashSet<long> (); 

        public override BindingListModel<OperationDetail> DetailsBase
        {
            get { return detailsBase ?? (detailsBase = new BindingListModel<OperationDetail> (details)); }
        }

        public override BindingListModel<OperationDetail> AdditionalDetailsBase
        {
            get
            {
                if (additionalDetailsBase == null && additionalDetails != null)
                    additionalDetailsBase = new BindingListModel<OperationDetail> (additionalDetails);

                return additionalDetailsBase;
            }
        }

        public BindList<T> Details
        {
            get { return details; }
            protected set
            {
                if (details != null)
                    details.ListChanged -= details_ListChanged;

                details = value;
                details.ListChanged += details_ListChanged;
            }
        }

        public BindList<T> AdditionalDetails
        {
            get { return additionalDetails; }
            protected set
            {
                if (additionalDetails != null)
                    additionalDetails.ListChanged -= additionalDetails_ListChanged;

                additionalDetails = value;
                additionalDetails.ListChanged += additionalDetails_ListChanged;
            }
        }

        private void additionalDetails_ListChanged (object sender, ListChangedEventArgs e)
        {
            ListenForPropertyChange (e, additionalDetails);
            OnPropertyChanged ("AdditionalDetails");
            if (e.ListChangedType != ListChangedType.ItemAdded)
                return;

            additionalDetails [e.NewIndex].BaseTotalOnPricePlusVAT = baseTotalOnPricePlusVAT;
            additionalDetails [e.NewIndex].BaseDiscountOnPricePlusVAT = baseDiscountOnPricePlusVAT;
        }

        public HashSet<long> AppliedPriceRules
        {
            get { return appliedPriceRules; }
        }

        public bool BasePricesOnPricePlusVAT
        {
            set
            {
                baseTotalOnPricePlusVAT = value;
                baseDiscountOnPricePlusVAT = value;
                foreach (T detail in details) {
                    detail.BaseTotalOnPricePlusVAT = value;
                    detail.BaseDiscountOnPricePlusVAT = value;
                }
            }
        }

        protected bool baseTotalOnPricePlusVAT;
        protected bool baseDiscountOnPricePlusVAT;
        public bool BaseDiscountOnPricePlusVAT
        {
            get { return baseDiscountOnPricePlusVAT; }
        }

        private void details_ListChanged (object sender, ListChangedEventArgs e)
        {
            ListenForPropertyChange (e, details);
            OnPropertyChanged ("Details");
            if (e.ListChangedType != ListChangedType.ItemAdded)
                return;

            details [e.NewIndex].BaseTotalOnPricePlusVAT = baseTotalOnPricePlusVAT;
            details [e.NewIndex].BaseDiscountOnPricePlusVAT = baseDiscountOnPricePlusVAT;
        }

        private void ListenForPropertyChange (ListChangedEventArgs e, IEnumerable<T> detailsToUse)
        {
            CustomListChangedEventArgs args = (CustomListChangedEventArgs) e;
            if (args.DeletedObject != null)
                ((INotifyPropertyChanged) args.DeletedObject).PropertyChanged -= detail_PropertyChanged;
            foreach (T detail in detailsToUse) {
                detail.PropertyChanged -= detail_PropertyChanged;
                detail.PropertyChanged += detail_PropertyChanged;
            }
        }

        private void detail_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged ("DetailProperty");
        }

        protected string note;

        [JsonIgnore]
        public override string Note
        {
            get
            {
                if (note != null)
                    return note;

                string [] notes = details.Select (d => d.Note).Distinct ().ToArray ();
                return notes.Length == 1 ? notes [0] ?? string.Empty : string.Empty;
            }
            set
            {
                bool noteChanged = note != value;

                note = value;
                foreach (T detail in Details)
                    detail.Note = value;

                if (noteChanged)
                    OnPropertyChanged ("Note");
            }
        }

        public override double Total
        {
            get
            {
                double total = details.Sum (detail => detail.Total);
                return Currency.Round (total, TotalsPriceType);
            }
        }

        [DbColumn (DataField.OperationTotal)]
        public override double TotalDB { get; set; }

        public override double VAT
        {
            get
            {
                if (BusinessDomain.AppConfiguration.VATIncluded) {
                    double vat = details.Sum (detail => detail.VAT * detail.Quantity);
                    return Currency.Round (vat, TotalsPriceType);
                }
                return TotalPlusVAT - Total;
            }
        }

        [DbColumn (DataField.OperationVatSum)]
        public override double VATDB { get; set; }

        public override double TotalPlusVAT
        {
            get
            {
                double totalPlusVAT = details.Sum (detail => detail.TotalPlusVAT);
                return Currency.Round (totalPlusVAT, TotalsPriceType);
            }
        }

        public bool IsVATExempt
        {
            get { return isVATExempt; }
        }

        public override bool IsDirty
        {
            get
            {
                return isDirty || (details.Any (detail => detail.IsDirty) || additionalDetails.Any (detail => detail.IsDirty));
            }
            set
            {
                isDirty = value;
                if (value)
                    return;

                foreach (T detail in details)
                    detail.IsDirty = false;

                foreach (T detail in additionalDetails)
                    detail.IsDirty = false;
            }
        }

        public bool HasRemovedDetails
        {
            get { return removedDetails.Count > 0; }
        }

        public Operation ()
        {
            details.ListChanged += details_ListChanged;
            additionalDetails.ListChanged += additionalDetails_ListChanged;
            baseTotalOnPricePlusVAT = BusinessDomain.AppConfiguration.RoundedPrices;
        }

        public void SetUsePriceIn (bool value)
        {
            usePriceIn = value;
            foreach (T detail in details)
                detail.SetUsePriceIn (value);
        }

        public bool GetUsePriceIn ()
        {
            return usePriceIn ?? !BusinessDomain.LoggedUser.HideItemsPurchasePrice;
        }

        public override void ClearDetails (bool logChange = true)
        {
            OperationState state = State;
            if (state == OperationState.Saved || state == OperationState.Draft || state == OperationState.Pending) {
                foreach (T detail in Details)
                    detail.Quantity = 0;
            } else {
                if (logChange)
                    removedDetails.AddRange (Details);
                Details.Clear ();
            }
        }

        public void RemoveAllEmptyDetails ()
        {
            for (int i = Details.Count - 1; i >= 0; i--) {
                var detail = Details [i];
                if (detail.Quantity.IsZero ())
                    Details.RemoveAt (i);
            }
        }

        public virtual T AddNewDetail ()
        {
            BindList<T> list = Details;
            list.AddNew ();
            T ret = list [list.Count - 1];
            if (isVATExempt)
                ret.IsVATExempt = true;

            return ret;
        }

        public void RemoveDetail (int row, bool logChange = true)
        {
            if (logChange)
                removedDetails.Add (Details [row]);
            Details.RemoveAt (row);
        }

        public virtual T AddNewAdditionalDetail ()
        {
            BindList<T> list = AdditionalDetails;
            list.AddNew ();
            T ret = list [list.Count - 1];
            if (isVATExempt)
                ret.IsVATExempt = true;

            return ret;
        }

        public void RemoveAdditionalDetail (int row)
        {
            AdditionalDetails.RemoveAt (row);
        }

        protected override void LoadDetails (bool usePriceIn)
        {
            ConfigurationHolderBase config = BusinessDomain.AppConfiguration;
            details.Clear ();
            details.AddRange (BusinessDomain.DataAccessProvider.GetOperationDetailsById<T> (operationType, id, partnerId, locationId, date, userId, loggedUserId, config.CurrencyPrecision, config.RoundedPrices, usePriceIn));
            foreach (T detail in details) {
                referenceDocumentId = detail.ReferenceDocumentId;
                // Discount value is not stored in the DB so calculate it when we load
                detail.CalculateDiscountValue ();
                detail.IsDirty = false;
            }
        }

        public BindList<T> FilterDetails (bool skipOldDetails = false, bool skipNewDetails = false)
        {
            BindList<T> ret = new BindList<T> ();

            foreach (T detail in details) {
                if (detail.DetailId >= 0 && skipOldDetails)
                    continue;

                if (detail.DetailId < 0 && skipNewDetails)
                    continue;

                if (detail.DetailId < 0 && detail.Quantity.IsZero ())
                    continue;

                ret.Add (detail);
            }

            return ret;
        }

        public BindList<T> FilterChangedDetails (bool skipNoteChanges = false)
        {
            BindList<T> ret = new BindList<T> ();

            foreach (T detail in details) {
                if (detail.DetailId < 0)
                    continue;

                if (detail.OriginalQuantity.IsEqualTo (detail.Quantity) &&
                    (skipNoteChanges || detail.OriginalNote.IsEqualTo (detail.Note)))
                    continue;

                ret.Add (detail);
            }

            return ret;
        }

        public BindList<T> GetCombinedDetails (bool ignorePrice, bool ignoreDiscounts)
        {
            return GetCombinedDetails (ignorePrice, ignoreDiscounts, new BindList<T> (Details));
        }

        public BindList<T> GetCombinedDetails (bool ignorePrice, bool ignoreDiscounts, BindList<T> det)
        {
            for (int i = 0; i < det.Count; i++) {
                T det1 = (T) det [i].Clone ();

                for (int j = det.Count - 1; j > i; j--) {
                    T det2 = det [j];

                    if (det1.ItemId != det2.ItemId)
                        continue;

                    if (!ignoreDiscounts && !det1.Discount.IsEqualTo (det2.Discount))
                        continue;

                    if (!ignorePrice) {
                        if (det1.Sign < 0) {
                            if (!det1.PriceOut.IsEqualTo (det2.PriceOut))
                                continue;
                        } else
                            if (!det1.PriceIn.IsEqualTo (det2.PriceIn))
                                continue;
                    }

                    det1.Quantity = det1.Quantity + det2.Quantity;
                    det1.OriginalQuantity += det2.OriginalQuantity;
                    det.RemoveAt (j);
                }

                det [i] = det1;
            }

            return det;
        }

        public void UpdatePrices (PriceGroup oldPriceGroup, PriceGroup newPriceGroup)
        {
            if (oldPriceGroup == newPriceGroup)
                return;

            foreach (T detail in Details)
                detail.UpdatePrices (oldPriceGroup, newPriceGroup);

            foreach (T detail in AdditionalDetails)
                detail.UpdatePrices (oldPriceGroup, newPriceGroup);
        }

        protected bool AutomaticProduction ()
        {
            if (State == OperationState.New ||
                State == OperationState.Saved)
                return AutomaticProduction (locationId, -1);

            return false;
        }

        protected bool AutomaticProduction (long locId, long childLocId)
        {
            bool isTopProduction = productionChains.Count == 0;
            try {
                Dictionary<long, int> detailsRecipeIndices = new Dictionary<long, int> ();

                ComplexProduction production = new ComplexProduction
                    {
                        LocationId = locId,
                        PartnerId = partnerId,
                        UserId = userId,
                        LoggedUserId = loggedUserId
                    };

                Dictionary<long, bool> increasedDepths = productionDepths.ToDictionary (depth => depth.Key, depth => false);

                foreach (T detail in GetCombinedDetails (true, true)) {
                    if (isTopProduction) {
                        detail.FinalProductId = detail.ItemId;
                        productionChains.Add (detail.FinalProductId, new List<ComplexRecipe> ());
                        productionDepths.Add (detail.FinalProductId, 0);
                    } else if (!increasedDepths [detail.FinalProductId]) {
                        productionDepths [detail.FinalProductId]++;
                        increasedDepths [detail.FinalProductId] = true;
                    }
                    detailsRecipeIndices.Add (detail.ItemId, TryRecipe (detail, production, 0, childLocId));
                }

                if (production.DetailsProd.Count <= 0)
                    return false;

                while (true)
                    try {
                        production.Commit ();
                        foreach (T detail in details.Where (d => production.DetailsProd.Any (p => p.ItemId == d.ItemId))) {
                            detail.OriginalPriceInEvaluate (detail.GetItemPriceIn (Item.GetById (detail.ItemId)));
                            detail.TotalEvaluate ();
                        }
                        return true;
                    } catch (InsufficientItemAvailabilityException e) {
                        TryNextRecipe (production, detailsRecipeIndices, e, childLocId);
                    }
            } finally {
                if (isTopProduction) {
                    productionChains.Clear ();
                    productionDepths.Clear ();
                }
            }
        }

        private static void TryNextRecipe (ComplexProduction production, IDictionary<long, int> detailsRecipeIndices, InsufficientItemAvailabilityException e, long childLocId)
        {
            long itemId = -1;
            string itemName = string.Empty;
            foreach (ComplexRecipeDetail product in production.DetailsMat.Where (m => m.ItemId == e.ItemId).SelectMany (m => m.SourceRecipe.DetailsProd)) {
                itemId = product.ItemId;
                itemName = product.ItemName;

                ComplexRecipeDetail product1 = product;
                ComplexProductionDetail detail = production.DetailsProd.Find (d => d.ItemId == product1.ItemId);
                int recipeIndex = TryRecipe (detail, production, detailsRecipeIndices [product.ItemId] + 1, childLocId);
                if (recipeIndex != int.MaxValue) {
                    detailsRecipeIndices [product.ItemId] = recipeIndex;
                    return;
                }
            }
            throw itemId < 0 ? e : new InsufficientItemAvailabilityException (itemName, itemId);
        }

        private static int TryRecipe (OperationDetail detail, ComplexProduction production, int recipeIndex, long childLocId)
        {
            if (detail.Quantity <= 0)
                return int.MaxValue;

            double available = Item.GetAvailability (detail.ItemId, production.locationId, childLocId);
            if (available >= (detail.DetailId < 0 ? detail.Quantity : detail.Quantity - detail.OriginalQuantity))
                return int.MaxValue;

            ComplexRecipe [] recipes = ComplexRecipe.GetAllByProductId (detail.ItemId);
            if (recipes.Length == 0 || !detail.CanAutoProduceItem ()) {
                if (BusinessDomain.AppConfiguration.AllowNegativeAvailability)
                    return int.MaxValue;

                throw new InsufficientItemAvailabilityException (detail.ItemName, detail.ItemId);
            }

            ComplexRecipe usedRecipe = null;
            for (int i = recipeIndex; i < recipes.Length; i++) {
                ComplexRecipe recipe = recipes [i];

                if (productionDepths [detail.FinalProductId] >= MAX_AUTO_PRODUCTION_DEPTH ||
                    productionChains [detail.FinalProductId].Find (r => r.id == recipe.id) != null)
                    continue;

                if (recipeIndex > 0)
                    production.RemoveRecipe (recipes [recipeIndex - 1]);

                PriceGroup priceGroup = production.GetPriceGroup ();
                if (!production.AddRecipe (recipe, priceGroup, detail.FinalProductId))
                    continue;

                usedRecipe = recipe;
                productionChains [detail.FinalProductId].Add (recipe);
                recipeIndex = i;
                break;
            }

            if (usedRecipe == null)
                return int.MaxValue;

            ComplexProductionDetail prodDetail = production.DetailsProd.Find (d => d.ItemId == detail.ItemId);
            prodDetail.QuantityEvaluate (detail.Quantity - available, production);

            foreach (ComplexProductionDetail pDetail in production.DetailsMat) {
                if (!ReferenceEquals (pDetail.SourceRecipe, usedRecipe))
                    continue;

                pDetail.Note = string.Format (Translator.GetString ("Automatic production of item \"{0}\""), detail.ItemName);
            }

            foreach (ComplexProductionDetail pDetail in production.DetailsProd) {
                if (!ReferenceEquals (pDetail.SourceRecipe, usedRecipe))
                    continue;

                pDetail.Note = string.Format (Translator.GetString ("Automatic production of item \"{0}\""), detail.ItemName);
            }

            return recipeIndex;
        }

        public double [] CalculateDistributedDiscount (ref double value)
        {
            List<double> originalDiscounts = new List<double> ();
            List<double> discounts = new List<double> ();
            double originalTotal = 0;

            foreach (T detail in details) {
                originalDiscounts.Add (detail.DiscountValue);
                discounts.Add (detail.DiscountValue);
                originalTotal += baseDiscountOnPricePlusVAT ? detail.OriginalTotalPlusVAT : detail.OriginalTotal;
            }

            if (originalTotal.IsZero ())
                return new double [0];

            bool finish;
            do {
                finish = true;
                double discountRatio = value / originalTotal;

                for (int i = 0; i < discounts.Count; i++) {
                    OperationDetail det = details [i];
                    double originalTotalPlusVat = baseDiscountOnPricePlusVAT ? det.OriginalTotalPlusVAT : det.OriginalTotal;
                    double delta = Math.Min (originalTotalPlusVat - discounts [i], originalTotalPlusVat * discountRatio);
                    delta = Math.Max (-originalTotalPlusVat - discounts [i], delta);

                    if (delta.IsZero ())
                        continue;

                    discounts [i] += Currency.Round (delta);
                    value -= delta;
                    finish = false;
                }

                if (!Currency.Round (value).IsZero ())
                    continue;

                value = 0;
                finish = true;
            } while (!finish);

            return discounts.ToArray ();
        }

        public void ApplyDistributedDiscount (double [] discountValues)
        {
            if (discountValues == null)
                throw new ArgumentNullException ("discountValues");

            if (discountValues.Length != Details.Count)
                throw new ArgumentException ("Discount values are not as much as the details in the operation!", "discountValues");

            for (int i = 0; i < Details.Count; i++) {
                Details [i].DiscountValueEvaluate (discountValues [i]);
                Details [i].ManualDiscount = true;
            }
        }

        public virtual void LotsEvaluate (BindList<T> detailsList)
        {
            if (!BusinessDomain.AppConfiguration.ItemsManagementUseLots)
                return;

            for (int i = detailsList.Count - 1; i >= 0; i--) {
                LotsEvaluate (detailsList, detailsList [i]);
            }
        }

        public void LotsEvaluate (BindList<T> detailsList, T detail, string lotName = null, bool barcodeUsed = false)
        {
            if (!BusinessDomain.AppConfiguration.ItemsManagementUseLots)
                return;

            if (detail.ItemId < 0)
                return;

            if (!detail.UsesSavedLots)
                return;

            if (detail.LotId > 0)
                return;

            IEnumerable<Lot> lots = Lot.GetAvailable (detail.ItemId, locationId);
            if (!string.IsNullOrWhiteSpace (lotName))
                lots = lots.Where (l => l.Name == lotName);

            Dictionary<Lot, double> lotQtty = new Dictionary<Lot, double> ();
            foreach (T d in detailsList) {
                if (d.ItemId != detail.ItemId)
                    continue;

                if (d.LotId <= 0)
                    continue;

                Lot dLot = d.GetLot ();
                if (lotQtty.ContainsKey (dLot))
                    lotQtty [dLot] += GetDetailQuantityForLot (d);
                else
                    lotQtty.Add (dLot, GetDetailQuantityForLot (d));
            }

            if (lotQtty.Count > 0)
                foreach (Lot lot in lots) {
                    if (lotQtty.ContainsKey (lot))
                        lot.AvailableQuantity -= lotQtty [lot];
                }

            double requiredQtty = barcodeUsed ? 1 : GetDetailQuantityForLot (detail);
            T currentDetail = detail;
            foreach (Lot lot in lots) {
                double qttyToUse = Math.Min (lot.AvailableQuantity, requiredQtty);
                if (qttyToUse <= 0)
                    continue;

                double availableQuantity = lot.AvailableQuantity;
                lot.AvailableQuantity -= qttyToUse;
                requiredQtty -= qttyToUse;

                EvaluateQuantityForLot (currentDetail, barcodeUsed ? availableQuantity : qttyToUse, qttyToUse);
                currentDetail.LotEvaluate (lot);

                if (requiredQtty.IsZero ())
                    break;

                int index = detailsList.IndexOf (currentDetail);
                currentDetail = (T) currentDetail.Clone ();
                detailsList.Insert (index + 1, currentDetail);
            }

            if (requiredQtty <= 0)
                return;

            EvaluateQuantityForEmptyLot (currentDetail, requiredQtty);
            currentDetail.LotEvaluate (null);
        }

        public virtual double GetDetailQuantityForLot (T detail)
        {
            return detail.Quantity;
        }

        public virtual void EvaluateQuantityForLot (T detail, double availableQuantity, double qttyToUse)
        {
            detail.Quantity = qttyToUse;
        }

        protected virtual void EvaluateQuantityForEmptyLot (T detail, double qttyToUse)
        {
            detail.Quantity = qttyToUse;
        }

        /// <summary>
        /// Logs the changes in the Item of the <see cref="Operation"/>.
        /// </summary>
        /// <param name="logOnlyCurrentQuantities">if set to <c>true</c> only the current quantities, 
        /// as opposed to both the current quantities and their maximum values, of the details, are logged.</param>
        public void LogChanges (bool logOnlyCurrentQuantities)
        {
            if (!BusinessDomain.AppConfiguration.LogAllChangesInOperations)
                return;

            foreach (string change in GetChanges (true, logOnlyCurrentQuantities))
                ApplicationLogEntry.AddNew (change);
        }

        protected IEnumerable<string> GetChanges (bool logNewDetailChanges, bool logCurrentQuantities)
        {
            List<string> changes = new List<string> ();

            string operationTypeName = Translator.GetOperationTypeGlobalName (operationType);
            foreach (T detail in details) {
                if (!detail.Discount.IsZero ()) {
                    changes.Add (string.Format (Translator.GetString ("{0} - changed discount of item \"{1}\" to {2}."),
                        operationTypeName,
                        detail.ItemName,
                        detail.Discount));
                }

                if (!logNewDetailChanges && detail.DetailId < 0)
                    continue;

                double quantityToCompare = BusinessDomain.AppConfiguration.LogAllChangesInOperations ? detail.MaxQuantity : detail.OriginalQuantity;
                if (detail.Quantity.IsZero () && !quantityToCompare.IsZero ()) {
                    changes.Add (string.Format (Translator.GetString ("{0} - item \"{1}\" was removed. Quantity of the item was {2}."),
                        operationTypeName,
                        detail.ItemName,
                        Quantity.ToString (quantityToCompare)));
                } else {
                    if (logCurrentQuantities && detail.ItemId > 0 && detail.DetailId < 0) {
                        changes.Add (string.Format (Translator.GetString ("{0} - quantity of item \"{1}\" was {2}."),
                            operationTypeName,
                            detail.ItemName,
                            Quantity.ToString (detail.Quantity)));
                        continue;
                    }

                    if (detail.Quantity < quantityToCompare) {
                        changes.Add (string.Format (Translator.GetString ("{0} - decreased quantity of item \"{1}\" from {2} to {3}."), operationTypeName, detail.ItemName,
                            Quantity.ToString (quantityToCompare),
                            Quantity.ToString (detail.Quantity)));
                    }
                }
            }

            changes.AddRange (removedDetails.Select (d =>
                string.Format (Translator.GetString ("{0} - item \"{1}\" was removed. Quantity of the item was {2}."),
                    operationTypeName, d.ItemName,
                    BusinessDomain.AppConfiguration.LogAllChangesInOperations ? d.MaxQuantity : d.OriginalQuantity)));

            return changes;
        }

        public K Clone<K, N> ()
            where K : Operation<N>, new ()
            where N : OperationDetail, new ()
        {
            K ret = CloneOperationBody<K> ();
            ret.Details = CloneOperationDetails<N> (details);
            ret.AdditionalDetails = CloneOperationDetails<N> (additionalDetails);
            foreach (Payment payment in payments)
                ret.payments.Add ((Payment) payment.Clone ());

            return ret;
        }

        public K CloneOperationBody<K> () where K : Operation, new ()
        {
            K ret = new K ();
            ret.Populate (this);

            return ret;
        }

        public BindList<K> CloneOperationDetails<K> (BindList<T> dets) where K : OperationDetail, new ()
        {
            BindList<K> ret = new BindList<K> ();

            foreach (T detail in dets)
                ret.Add (detail.Clone<K> ());

            return ret;
        }

        public void SetVATExempt (bool isExempt)
        {
            foreach (T detail in details)
                detail.SetVATExempt (isExempt);

            foreach (T detail in additionalDetails)
                detail.SetVATExempt (isExempt);

            isVATExempt = isExempt;
        }

        public void AddVAT ()
        {
            foreach (T detail in details)
                detail.AddVAT ();

            foreach (T detail in additionalDetails)
                detail.AddVAT ();
        }

        public void SubtractVAT ()
        {
            foreach (T detail in details)
                detail.SubtractVAT ();

            foreach (T detail in additionalDetails)
                detail.SubtractVAT ();
        }

        public void ClearPromotionForDetail (OperationDetail detail)
        {
            if (details.Contains (detail)) {
                for (int i = details.Count - 1; i >= 0; i--)
                    if (details [i].PromotionForDetailHashCode == detail.GetHashCode ())
                        details.RemoveAt (i);
            } else if (additionalDetails.Contains (detail))
                for (int i = additionalDetails.Count - 1; i >= 0; i--)
                    if (additionalDetails [i].PromotionForDetailHashCode == detail.GetHashCode ())
                        additionalDetails.RemoveAt (i);
        }

        public override void SetState (OperationState state)
        {
            OperationState oldState = State;
            // Reset the sign values to the defaults if we are comming from a draft
            if (state == OperationState.New &&
                (oldState == OperationState.NewDraft || oldState == OperationState.NewPending ||
                oldState == OperationState.Draft || oldState == OperationState.Pending)) {
                T detail = Activator.CreateInstance<T> ();
                foreach (var d in details)
                    d.ResetSign (detail.Sign);

                foreach (var ad in additionalDetails)
                    ad.ResetSign (detail.Sign);
            } else if (state == OperationState.NewDraft || state == OperationState.NewPending ||
                state == OperationState.Draft || state == OperationState.Pending) {
                foreach (var d in details)
                    d.Sign = 0;

                foreach (var ad in additionalDetails)
                    ad.Sign = 0;
            }

            base.SetState (state);
        }

        protected void InvalidateItemsCache ()
        {
            Item.Cache.Clear (details.Select (d => d.ItemId));
        }

        #region ICloneable Members

        public override object Clone ()
        {
            Operation<T> ret = (Operation<T>) MemberwiseClone ();
            ret.ClearPropertyChangedHandlers ();

            ret.Details = new BindList<T> ();
            foreach (T detail in details)
                ret.details.Add ((T) detail.Clone ());

            if (additionalDetails != null) {
                ret.AdditionalDetails = new BindList<T> ();
                foreach (T detail in additionalDetails)
                    ret.additionalDetails.Add ((T) detail.Clone ());
            }

            return ret;
        }

        #endregion
    }

    public class Operation : INotifyPropertyChanged, ICloneable
    {
        protected static readonly Dictionary<long, List<ComplexRecipe>> productionChains = new Dictionary<long, List<ComplexRecipe>> ();
        protected static readonly Dictionary<long, int> productionDepths = new Dictionary<long, int> ();

        #region Fields

        protected long id = -1;
        protected OperationType operationType;
        protected long partnerId = -1;
        protected string partnerName = string.Empty;
        protected string partnerName2 = string.Empty;
        protected long locationId = -1;
        protected string location = string.Empty;
        protected string location2 = string.Empty;
        protected long userId = -1;
        protected string userName = string.Empty;
        protected string userName2 = string.Empty;
        protected long loggedUserId = -1;
        protected string loggedUserName = string.Empty;
        protected string loggedUserName2 = string.Empty;
        protected long referenceDocumentId;
        protected DateTime date = BusinessDomain.Today;
        protected readonly BindList<Payment> payments = new BindList<Payment> ();
        protected bool isVATExempt;
        protected bool isDirty;

        #endregion

        #region Public properties

        [DbColumn (DataField.OperationNumber)]
        public long Id
        {
            get { return id; }
            set
            {
                if (id == value)
                    return;

                id = value;
                OnPropertyChanged ("Id");
            }
        }

        [JsonIgnore]
        public OperationState State
        {
            get
            {
                if (id >= 0)
                    return OperationState.Saved;

                if (id == (int) OperationState.NewPending)
                    return OperationState.NewPending;

                if (id == (int) OperationState.Pending)
                    return OperationState.Pending;

                if (id == (int) OperationState.NewDraft)
                    return OperationState.NewDraft;

                if (id < -10)
                    return OperationState.Draft;

                return OperationState.New;
            }
        }

        [JsonIgnore]
        [DbColumn (DataField.OperationType)]
        public OperationType OperationType
        {
            get { return operationType; }
            set
            {
                if (operationType == value)
                    return;

                operationType = value;
                OnPropertyChanged ("OperationType");
            }
        }

        [DbColumn (DataField.OperationPartnerId)]
        public long PartnerId
        {
            get { return partnerId; }
            set
            {
                if (partnerId == value)
                    return;

                partnerId = value;
                OnPropertyChanged ("PartnerId");
            }
        }

        [DbColumn (DataField.OperationPartner)]
        public string PartnerName
        {
            get { return partnerName; }
            set
            {
                if (partnerName == value)
                    return;

                partnerName = value;
                OnPropertyChanged ("PartnerName");
            }
        }

        [DbColumn (DataField.OperationPartner2)]
        public string PartnerName2
        {
            get { return partnerName2; }
            set
            {
                if (partnerName2 == value)
                    return;

                partnerName2 = value;
                OnPropertyChanged ("PartnerName2");
            }
        }

        [DbColumn (DataField.PartnerCode, true)]
        public string PartnerCode { get; set; }

        [DbColumn (DataField.OperationLocationId)]
        public long LocationId
        {
            get { return locationId; }
            set
            {
                if (locationId == value)
                    return;

                locationId = value;
                OnPropertyChanged ("LocationId");
            }
        }

        [DbColumn (DataField.OperationLocation)]
        public string Location
        {
            get { return location; }
            set
            {
                if (location == value)
                    return;

                location = value;
                OnPropertyChanged ("Location");
            }
        }

        [DbColumn (DataField.OperationLocation2)]
        public string Location2
        {
            get { return string.IsNullOrWhiteSpace (location2) ? location : location2; }
            set
            {
                if (location2 == value)
                    return;

                location2 = value;
                OnPropertyChanged ("Location2");
            }
        }

        [DbColumn (DataField.LocationCode, true)]
        public string LocationCode { get; set; }

        [DbColumn (DataField.OperationOperatorId)]
        public long UserId
        {
            get { return userId; }
            set
            {
                if (userId == value)
                    return;

                userId = value;
                OnPropertyChanged ("UserId");
            }
        }

        [DbColumn (DataField.OperationsOperatorName)]
        public string UserName
        {
            get { return userName; }
            set
            {
                if (userName == value)
                    return;

                userName = value;
                OnPropertyChanged ("UserName");
            }
        }

        [DbColumn (DataField.OperationsOperatorName2)]
        public string UserName2
        {
            get { return string.IsNullOrWhiteSpace (userName2) ? userName : userName2; }
            set
            {
                if (userName2 == value)
                    return;

                userName2 = value;
                OnPropertyChanged ("UserName2");
            }
        }

        [DbColumn (DataField.OperationUserId)]
        public long LoggedUserId
        {
            get { return loggedUserId; }
            set
            {
                if (loggedUserId == value)
                    return;

                loggedUserId = value;
                OnPropertyChanged ("LoggedUserId");
            }
        }

        [DbColumn (DataField.OperationsUserName)]
        public string LoggedUserName
        {
            get { return loggedUserName; }
            set
            {
                if (loggedUserName == value)
                    return;

                loggedUserName = value;
                OnPropertyChanged ("LoggedUserName");
            }
        }

        [DbColumn (DataField.OperationsUserName2)]
        public string LoggedUserName2
        {
            get { return loggedUserName2; }
            set
            {
                if (loggedUserName2 == value)
                    return;

                loggedUserName2 = value;
                OnPropertyChanged ("LoggedUserName2");
            }
        }

        public long ReferenceDocumentId
        {
            get { return referenceDocumentId; }
            set
            {
                if (referenceDocumentId == value)
                    return;

                referenceDocumentId = value;
                OnPropertyChanged ("ReferenceDocumentId");
            }
        }

        [DbColumn (DataField.OperationDate)]
        public DateTime Date
        {
            get { return date; }
            set
            {
                if (date == value)
                    return;

                date = value;
                OnPropertyChanged ("Date");
            }
        }

        protected DateTime timeStamp;
        [DbColumn (DataField.OperationTimeStamp)]
        public DateTime TimeStamp
        {
            get { return timeStamp; }
            set
            {
                if (timeStamp == value)
                    return;

                timeStamp = value;
                OnPropertyChanged ("TimeStamp");
            }
        }

        public virtual string Note
        {
            get { return null; }
            set { }
        }

        [JsonIgnore]
        public Payment Debt { get; set; }

        [JsonIgnore]
        public BindList<Payment> Payments
        {
            get { return payments; }
        }

        [JsonIgnore]
        public string FormattedOperationNumber
        {
            get { return GetFormattedOperationNumber (id); }
        }

        [JsonIgnore]
        public virtual double Total
        {
            get { throw new NotImplementedException (); }
        }

        public virtual double TotalDB
        {
            get { throw new NotImplementedException (); }
            set { throw new NotImplementedException (); }
        }

        [JsonIgnore]
        public virtual double VAT
        {
            get { throw new NotImplementedException (); }
        }

        public virtual double VATDB
        {
            get { throw new NotImplementedException (); }
            set { throw new NotImplementedException (); }
        }

        [JsonIgnore]
        public virtual double TotalPlusVAT
        {
            get { throw new NotImplementedException (); }
        }

        [JsonIgnore]
        public double DueAmount
        {
            get
            {
                return payments.Sum (payment => payment.Quantity * (double) payment.Mode * -1);
            }
        }

        [JsonIgnore]
        public virtual PriceType TotalsPriceType
        {
            get { return PriceType.SaleTotal; }
        }

        public virtual bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }

        protected BindingListModel<OperationDetail> detailsBase;
        [JsonIgnore]
        public virtual BindingListModel<OperationDetail> DetailsBase
        {
            get { throw new NotImplementedException (); }
        }

        protected BindingListModel<OperationDetail> additionalDetailsBase;
        [JsonIgnore]
        public virtual BindingListModel<OperationDetail> AdditionalDetailsBase
        {
            get { throw new NotImplementedException (); }
        }

        public virtual bool AllowFiscal
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether it makes sense for this <see cref="Operation"/> to use money change.
        /// </summary>
        /// <value>
        ///   <c>true</c> if change; otherwise, <c>false</c>.
        /// </value>
        public virtual bool UseChange
        {
            get { return true; }
        }

        public virtual bool UseAdvancePayments
        {
            get { return false; }
        }

        #endregion

        public delegate Operation GetByIdHandler (long id);
        public delegate Operation GetPendingHandler (long partnerId, long locationId);

        private static readonly Dictionary<OperationType, GetByIdHandler> getByIdHandlers;
        private static readonly Dictionary<OperationType, GetPendingHandler> getPendingHandlers;

        public static Dictionary<OperationType, GetByIdHandler> GetByIdHandlers
        {
            get { return getByIdHandlers; }
        }

        public static Dictionary<OperationType, GetPendingHandler> GetPendingHandlers
        {
            get { return getPendingHandlers; }
        }

        static Operation ()
        {
            getByIdHandlers = new Dictionary<OperationType, GetByIdHandler>
                {
                    { OperationType.ComplexProductionMaterial, ComplexProduction.GetById },
                    { OperationType.ComplexProductionProduct, ComplexProduction.GetById },
                    { OperationType.ComplexRecipeMaterial, ComplexRecipe.GetById },
                    { OperationType.ComplexRecipeProduct, ComplexRecipe.GetById },
                    { OperationType.Purchase, Purchase.GetById },
                    { OperationType.Sale, Sale.GetById },
                    { OperationType.StockTaking, StockTaking.GetById },
                    { OperationType.TransferIn, Transfer.GetById },
                    { OperationType.TransferOut, Transfer.GetById },
                    { OperationType.Waste, Waste.GetById }
                };

            getPendingHandlers = new Dictionary<OperationType, GetPendingHandler>
                {
                    { OperationType.ComplexProductionMaterial, ComplexProduction.GetPending },
                    { OperationType.ComplexProductionProduct, ComplexProduction.GetPending },
                    { OperationType.Purchase, Purchase.GetPending },
                    { OperationType.Sale, Sale.GetPending },
                    { OperationType.StockTaking, StockTaking.GetPending },
                    { OperationType.TransferIn, Transfer.GetPending },
                    { OperationType.TransferOut, Transfer.GetPending },
                    { OperationType.Waste, Waste.GetPending }
                };
        }

        public static Operation GetById (OperationType type, long operationId)
        {
            GetByIdHandler handler;
            return getByIdHandlers.TryGetValue (type, out handler) ? handler (operationId) : null;
        }

        public static Operation GetPending (OperationType type, long partnerId, long locationId)
        {
            GetPendingHandler handler;
            return getPendingHandlers.TryGetValue (type, out handler) ? handler (partnerId, locationId) : null;
        }

        public static Operation GetFirst ()
        {
            return BusinessDomain.DataAccessProvider.GetFirstOperation<Operation> ();
        }

        public static Operation [] GetAllAfter (DateTime operationDate)
        {
            return BusinessDomain.DataAccessProvider.GetAllAfter<Operation> (operationDate);
        }

        public static ObjectsContainer<OperationType, long, long> [] GetAllPending ()
        {
            return BusinessDomain.DataAccessProvider.GetAllPendingOperations ();
        }

        public static string GetFormattedOperationNumber (long operationNumber)
        {
            return GetFormattedOperationNumber (operationNumber, BusinessDomain.AppConfiguration.DocumentNumberLength);
        }

        public static string GetFormattedOperationNumber (long operationNumber, int numberLength)
        {
            if (operationNumber > 0)
                return Math.Abs (operationNumber).ToString (CultureInfo.InvariantCulture).PadLeft (numberLength, '0');

            if (operationNumber < -10)
                return Translator.GetString ("Draft");

            if (operationNumber == (int) OperationState.Pending)
                return Translator.GetString ("Pending");

            return Translator.GetString ("New");
        }

        public PriceGroup GetPriceGroup ()
        {
            return GetPriceGroup (partnerId, locationId);
        }

        public static PriceGroup GetPriceGroup (long partnerId, long locationId)
        {
            PriceGroup partnerPriceGroup = partnerId > -1 ? Partner.GetById (partnerId).PriceGroup : PriceGroup.RegularPrice;
            PriceGroup locationPriceGroup = locationId > -1 ? Entities.Location.GetById (locationId).PriceGroup : PriceGroup.RegularPrice;

            return GetPriceGroup (partnerPriceGroup, locationPriceGroup);
        }

        public static PriceGroup GetPriceGroup (PriceGroup partnerPriceGroup, PriceGroup locationPriceGroup)
        {
            return partnerPriceGroup != PriceGroup.RegularPrice ? partnerPriceGroup : locationPriceGroup;
        }

        public bool RefreshIDByDetail ()
        {
            OperationDetail detail = DetailsBase.FirstOrDefault ();
            if (detail == null)
                return false;

            long? newId = BusinessDomain.DataAccessProvider.GetOperationId (detail.DetailId);
            if (newId == null)
                return false;

            Id = newId.Value;
            return true;
        }

        /// <summary>
        /// Creates a new ID before the <see cref="Operation"/> is inserted into a storage.
        /// </summary>
        /// <returns></returns>
        protected virtual long CreateNewId ()
        {
            long locId = BusinessDomain.AppConfiguration.DocumentNumbersPerLocation ? locationId : 0;
            return BusinessDomain.DataAccessProvider.CreateNewOperationId (operationType, locId, State);
        }

        protected void CreatePayments (out Payment [] duePayments, out Payment [] paidPayments)
        {
            if (State == OperationState.NewDraft ||
                State == OperationState.Draft) {
                duePayments = new Payment [0];
                paidPayments = new Payment [0];
                return;
            }

            DateTime endDate = DateTime.MinValue;
            if (Debt != null)
                endDate = Debt.EndDate;
            else if (payments.Count > 0)
                endDate = payments [0].EndDate;

            // Generate due amount payment
            duePayments = Payment.GetForOperation (this, PaymentMode.Due);
            if (duePayments.Length == 0) {
                duePayments = new [] { new Payment (this, (int) BasePaymentType.Cash, PaymentMode.Due) };
            } else {
                foreach (Payment payment in duePayments) {
                    payment.PartnerId = PartnerId;
                    payment.Quantity = TotalPlusVAT;
                }
            }

            if (payments.Count == 0)
                paidPayments = Payment.GetForOperation (this, PaymentMode.Paid);
            else {
                for (int i = payments.Count - 1; i >= 0; i--)
                    if (payments [i].Quantity <= 0 && payments [i].Id < 0)
                        payments.RemoveAt (i);
                paidPayments = payments.ToArray ();
            }

            foreach (Payment payment in paidPayments)
                payment.PartnerId = PartnerId;

            if (endDate != DateTime.MinValue) {
                duePayments [0].EndDate = endDate;
                foreach (Payment paidPayment in paidPayments)
                    paidPayment.EndDate = endDate;
            }
        }

        public virtual void Commit ()
        {
            throw new NotImplementedException ();
        }

        public static void ChangeUser (OperationType operationType, long operationId, long userId)
        {
            BusinessDomain.DataAccessProvider.UpdateOperationUser (operationType, operationId, userId);
        }

        #region INotifyPropertyChanged Members

        protected void OnPropertyChanged (string property)
        {
            isDirty = true;
            if (PropertyChanged != null)
                PropertyChanged (this, new PropertyChangedEventArgs (property));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public static bool IdExists (long id)
        {
            return BusinessDomain.DataAccessProvider.OperationIdExists (id);
        }

        public static bool NoteExists (string note)
        {
            return BusinessDomain.DataAccessProvider.OperationNoteExists (note);
        }

        public static long? GetIdByDetailId (long detailId)
        {
            return BusinessDomain.DataAccessProvider.GetOperationId (detailId);
        }

        public static long GetMaxId ()
        {
            return BusinessDomain.DataAccessProvider.GetMaxOperationId ();
        }

        public static LazyListModel<long> GetUsedLocationIdsBefore (DateTime date)
        {
            DataQuery filter = new DataQuery (new DataFilter (DataFilterLogic.LessOrEqual, DataField.OperationDate) { Values = new object [] { date } });
            return GetUsedLocationIds (filter);
        }

        public static LazyListModel<long> GetUsedLocationIds (DataQuery filter)
        {
            return BusinessDomain.DataAccessProvider.GetOperationLocationIds (filter);
        }

        /// <summary>
        /// Gets the last operation numbers per operation type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="groupByLocation"></param>
        /// <returns>Value1 - Operation type, Value2 - Last number, Value3 - Location Id (if grouping by id)</returns>
        public static List<ObjectsContainer<int, long, long>> GetLastNumbers ()
        {
            return BusinessDomain.DataAccessProvider.GetLastOperationNumbers (BusinessDomain.AppConfiguration.DocumentNumbersPerLocation);
        }

        public static LazyListModel<OperationDetail> GetQuantitiesBefore (DateTime date, IList<long> locations)
        {
            return BusinessDomain.DataAccessProvider.GetQuantities<OperationDetail> (new DataQuery (
                new DataFilter (DataFilterLogic.LessOrEqual, DataField.OperationDate)
                    {
                        Values = new object [] { date }
                    },
                new DataFilter (DataFilterLogic.In, DataField.OperationLocationId)
                    {
                        Values = locations.Count > 0 ? locations.Select (id => (object) id).ToArray () : new object [] { int.MinValue }
                    }), BusinessDomain.AppConfiguration.ItemsManagementUseLots);
        }

        public static void AddIdCodes (IEnumerable<string> operationCodes)
        {
            BusinessDomain.DataAccessProvider.AddOperationIdCodes (operationCodes);
        }

        public static void DeleteIdCodes ()
        {
            BusinessDomain.DataAccessProvider.DeleteOperationIdCodes ();
        }

        public static void DeleteNonPayableOperationsBefore (DateTime date)
        {
            BusinessDomain.DataAccessProvider.DeleteNonPayableOperations (date);
        }

        public static void DeletePayableOperationsBefore (DateTime date, bool onlyPaid)
        {
            BusinessDomain.DataAccessProvider.DeletePayableOperationsBefore (date, onlyPaid);
        }

        public virtual void ClearDetails (bool logChange = true)
        {
        }

        public virtual void LoadDetails ()
        {
            LoadDetails (true);
        }

        protected virtual void LoadDetails (bool usePriceIn)
        {
            throw new NotImplementedException ();
        }

        public virtual void Annul ()
        {
            string error;
            string warning;
            if (BusinessDomain.CanAnnulOperation (this, out error, out warning)) {
                LoadDetails ();
                ClearDetails ();
                Commit ();
            } else if (warning != null)
                BusinessDomain.RequestOperationAnnul (this);
        }

        public void Populate (Operation oper)
        {
            if (oper == null)
                return;

            operationType = oper.operationType;
            locationId = oper.locationId;
            location = oper.location;
            location2 = oper.location2;
            partnerId = oper.partnerId;
            partnerName = oper.partnerName;
            partnerName2 = oper.partnerName2;
            userId = oper.userId;
            userName = oper.userName;
            userName2 = oper.userName2;
            loggedUserId = oper.loggedUserId;
            loggedUserName = oper.loggedUserName;
            loggedUserName2 = oper.loggedUserName2;
            referenceDocumentId = oper.referenceDocumentId;
            date = oper.date;
            timeStamp = oper.timeStamp;
            isVATExempt = oper.isVATExempt;
            isDirty = oper.isDirty;
        }

        public static void AddPartnerLocationFilters (ref DataQuery dataQuery, bool addPartnerFilter = true, bool addLocationFilter = true)
        {
            if (BusinessDomain.LoggedUser.LockedPartnerId > 0 && addPartnerFilter)
                dataQuery.Filters.Add (new DataFilter (new DbField (DataField.OperationPartnerId))
                    {
                        Values = new object [] { BusinessDomain.LoggedUser.LockedPartnerId }
                    });

            if (BusinessDomain.LoggedUser.LockedLocationId > 0 && addLocationFilter)
                dataQuery.Filters.Add (new DataFilter (new DbField (DataField.OperationLocationId))
                    {
                        Values = new object [] { BusinessDomain.LoggedUser.LockedLocationId }
                    });
        }

        public virtual void CommitPayments ()
        {
            CommitPayments (true);
        }

        protected void CommitPayments (bool updateCashBook)
        {
            Payment [] duePayments;
            Payment [] paidPayments;
            CreatePayments (out duePayments, out paidPayments);

            using (DbTransaction transaction = new DbTransaction (BusinessDomain.DataAccessProvider)) {
                CommitPayments (duePayments, paidPayments, false, updateCashBook);

                transaction.Complete ();
            }
        }

        protected void CommitPayments (IEnumerable<Payment> duePayments, Payment [] paidPayments, bool editMode, bool updateCashBook)
        {
            // We have to delete the payment records if there is nothing left
            if (editMode && Total.IsZero ()) {
                foreach (Payment payment in duePayments)
                    BusinessDomain.DataAccessProvider.DeletePayment (payment.Id);

                foreach (Payment payment in paidPayments) {
                    BusinessDomain.OnPaymentDeleting (payment);
                    BusinessDomain.DataAccessProvider.DeletePayment (payment.Id);
                }

                BusinessDomain.DataAccessProvider.DeleteOperationId (operationType, id);

                ApplicationLogEntry.AddNew (string.Format (LogVoidOperation (), GetFormattedOperationNumber (id), BusinessDomain.GetFormattedDate (date)));
            } else {
                if (!editMode && updateCashBook && State == OperationState.Saved)
                    UpdateCashBook (paidPayments);

                // Save the due payment in the database
                foreach (Payment payment in duePayments)
                    payment.CommitChanges ();

                // Save the paid payments in the database
                foreach (Payment payment in paidPayments)
                    payment.CommitChanges ();

                // Don't update the amount paid if this is not a new operation
                if (editMode)
                    ApplicationLogEntry.AddNew (string.Format (LogEditOperation (), GetFormattedOperationNumber (id), BusinessDomain.GetFormattedDate (date)));
            }
        }

        public virtual bool Validate (ValidateCallback callback, StateHolder state)
        {
            return true;
        }

        protected virtual void UpdateCashBook (IEnumerable<Payment> paidPayments)
        {
            // Generate cash book entry
            // Compatibility: cash entries are added only for new payments
            CashBookEntry [] cashEntries = paidPayments
                .Where (payment => payment.Id < 0 && payment.Type.BaseType == BasePaymentType.Cash)
                .Select (payment => new CashBookEntry (payment))
                .ToArray ();

            UpdateCashBook (cashEntries);
        }

        protected void UpdateCashBook (CashBookEntry [] cashEntries, int? operationNumber = null)
        {
            foreach (CashBookEntry cashEntry in cashEntries) {
                BusinessDomain.DataAccessProvider.SnapshotObject (cashEntry);
                cashEntry.OperationNumber = operationNumber ?? id;
                cashEntry.PartnerName = partnerName;
            }

            // Create a cashbook entry in the database
            foreach (CashBookEntry cashEntry in cashEntries)
                cashEntry.CommitChanges ();
        }

        protected virtual string LogVoidOperation ()
        {
            return Translator.GetString ("Void operation No.{0} from {1}");
        }

        protected virtual string LogEditOperation ()
        {
            return Translator.GetString ("Edit operation No.{0} from {1}");
        }

        public static KeyValuePair<int, string> [] GetAllTypeFilters ()
        {
            return GetTypeFilters (OperationType.Sale,
                OperationType.Purchase,
                OperationType.Waste,
                OperationType.StockTaking,
                OperationType.TransferIn,
                OperationType.WriteOff,
                OperationType.PurchaseOrder,
                OperationType.Offer,
                OperationType.ProformaInvoice,
                OperationType.Consignment,
                OperationType.ConsignmentSale,
                OperationType.ConsignmentReturn,
                OperationType.SalesOrder,
                OperationType.ComplexRecipeMaterial,
                OperationType.ComplexProductionMaterial,
                OperationType.DebitNote,
                OperationType.CreditNote,
                OperationType.WarrantyCard,
                OperationType.RestaurantOrder,
                OperationType.Return,
                OperationType.PurchaseReturn);
        }

        public static KeyValuePair<int, string> [] GetAllWithPaymentFilters ()
        {
            return GetTypeFilters (OperationType.Sale,
                OperationType.Purchase,
                OperationType.StockTaking,
                OperationType.DebitNote,
                OperationType.CreditNote,
                OperationType.Return);
        }

        private static KeyValuePair<int, string> [] GetTypeFilters (params OperationType [] types)
        {
            List<KeyValuePair<int, string>> pairs = new List<KeyValuePair<int, string>>
                {
                    new KeyValuePair<int, string> (-1, Translator.GetString ("All"))
                };
            pairs.AddRange (types.Select (type => new KeyValuePair<int, string> ((int) type, Translator.GetOperationTypeGlobalName (type))));

            return pairs.ToArray ();
        }

        public virtual object Clone ()
        {
            Operation ret = (Operation) MemberwiseClone ();
            ret.ClearPropertyChangedHandlers ();

            return ret;
        }

        protected void ClearPropertyChangedHandlers ()
        {
            PropertyChanged = null;
        }

        public virtual void SetState (OperationState state)
        {
            switch (state) {
                case OperationState.New:
                    id = -1;
                    break;

                case OperationState.NewDraft:
                    id = -10;
                    break;

                case OperationState.NewPending:
                    id = -2;
                    break;
            }
        }
    }
}