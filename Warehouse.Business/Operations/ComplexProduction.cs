//
// ComplexProduction.cs
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

using System.Collections.Generic;
using System.Linq;
using Warehouse.Business.Entities;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Data.Model;

namespace Warehouse.Business.Operations
{
    public class ComplexProduction : Operation<ComplexProductionDetail>
    {
        private DataField pGroupField = DataField.NotSet;

        #region Public properties

        public BindList<ComplexProductionDetail> DetailsMat
        {
            get
            {
                if (details.Count == 0)
                    LoadMaterialDetails ();

                return details;
            }
        }

        public BindList<ComplexProductionDetail> DetailsProd
        {
            get
            {
                if (additionalDetails.Count == 0)
                    LoadProductDetails ();

                return additionalDetails;
            }
        }

        public override double Total
        {
            get
            {
                double total = DetailsMat.Sum (t => t.Total);

                return Currency.Round (total, PriceType.Purchase);
            }
        }

        public override PriceType TotalsPriceType
        {
            get { return PriceType.PurchaseTotal; }
        }

        #endregion

        public ComplexProduction ()
        {
            partnerId = 1;
            locationId = 1;
            operationType = OperationType.ComplexProductionMaterial;
            additionalDetails = new BindList<ComplexProductionDetail> ();
        }

        public override void Commit ()
        {
            bool editMode = true;

            using (DbTransaction transaction = new DbTransaction (BusinessDomain.DataAccessProvider)) {
                transaction.SnapshotObject (this);
                if (BusinessDomain.AppConfiguration.AutoProduction)
                    if (AutomaticProduction ())
                        RecalculatePrices ();

                // Create a new operation Id if needed);
                OperationState operationState = State;
                if (operationState == OperationState.New || operationState == OperationState.NewDraft || operationState == OperationState.NewPending) {
                    id = CreateNewId ();
                    editMode = false;

                    if (BusinessDomain.AppConfiguration.ItemsManagementUseLots)
                        foreach (ComplexProductionDetail detail in DetailsProd)
                            if (detail.ProductionDate == null)
                                detail.ProductionDate = Date;
                }

                LotsEvaluate (DetailsMat);

                // Save the output ComplexProduction in the database
                OperationType = OperationType.ComplexProductionMaterial;
                foreach (ComplexProductionDetail detail in DetailsMat) {
                    detail.ReferenceDocumentId = id;
                    detail.ResetSign (-1);
                }
                BusinessDomain.DataAccessProvider.AddUpdateComplexProductionMat (this, DetailsMat.ToArray (), BusinessDomain.AppConfiguration.AllowNegativeAvailability);

                // Save the input ComplexProduction in the database
                OperationType = OperationType.ComplexProductionProduct;
                foreach (ComplexProductionDetail detail in DetailsProd) {
                    detail.ReferenceDocumentId = id;
                    detail.ResetSign (1);
                }
                BusinessDomain.DataAccessProvider.AddUpdateComplexProductionProd (this, DetailsProd.ToArray (), BusinessDomain.AppConfiguration.AllowNegativeAvailability);
                Item.Cache.Clear (DetailsProd.Select (d => d.ItemId));

                if (editMode && Total.IsZero ()) {
                    BusinessDomain.DataAccessProvider.DeleteOperationId (OperationType.ComplexProductionMaterial, id);

                    ApplicationLogEntry.AddNew (string.Format (Translator.GetString ("Void production No.{0} from {1}"), GetFormattedOperationNumber (id), BusinessDomain.GetFormattedDate (date)));
                } else if (editMode)
                    ApplicationLogEntry.AddNew (string.Format (Translator.GetString ("Edit production No.{0} from {1}"), GetFormattedOperationNumber (id), BusinessDomain.GetFormattedDate (date)));

                if (editMode)
                    RemoveAllEmptyDetails ();

                transaction.Complete ();
            }
        }

        public override void EvaluateQuantityForLot (ComplexProductionDetail detail, double availableQuantity, double qttyToUse)
        {
            ComplexRecipe cr = detail.SourceRecipe;
            detail.SourceRecipe = null;
            detail.QuantityEvaluate (qttyToUse, this);
            detail.SourceRecipe = cr;
        }

        protected override void EvaluateQuantityForEmptyLot (ComplexProductionDetail detail, double qttyToUse)
        {
            ComplexRecipe cr = detail.SourceRecipe;
            detail.SourceRecipe = null;
            detail.QuantityEvaluate (qttyToUse, this);
            detail.SourceRecipe = cr;
        }

        protected override void LoadDetails (bool usePriceIn)
        {
            SetPriceGroup ();
            LoadMaterialDetails ();
            LoadProductDetails ();
        }

        private void LoadMaterialDetails ()
        {
            if (pGroupField == DataField.NotSet)
                return;

            OperationState state = State;
            if (state != OperationState.Saved && state != OperationState.Draft && state != OperationState.Pending)
                return;

            details.Clear ();
            details.AddRange (BusinessDomain.DataAccessProvider.GetComplexProductionMatDetailsById<ComplexProductionDetail> (id, pGroupField));
            foreach (ComplexProductionDetail detail in details) {
                referenceDocumentId = detail.ReferenceDocumentId;
                detail.IsDirty = false;
            }
        }

        private void LoadProductDetails ()
        {
            if (pGroupField == DataField.NotSet)
                return;

            OperationState state = State;
            if (state != OperationState.Saved && state != OperationState.Draft && state != OperationState.Pending)
                return;

            additionalDetails.Clear ();
            additionalDetails.AddRange (BusinessDomain.DataAccessProvider.GetComplexProductionProdDetailsById<ComplexProductionDetail> (id, pGroupField));
            foreach (ComplexProductionDetail detail in additionalDetails) {
                referenceDocumentId = detail.ReferenceDocumentId;
                detail.IsDirty = false;
            }
        }

        public override void ClearDetails (bool logChange = true)
        {
            if (State == OperationState.Saved || State == OperationState.Draft) {
                foreach (ComplexProductionDetail detail in DetailsMat)
                    detail.Quantity = 0;

                foreach (ComplexProductionDetail detail in DetailsProd)
                    detail.Quantity = 0;
            } else {
                DetailsMat.Clear ();
                DetailsProd.Clear ();
            }
        }

        public bool AddRecipe (ComplexRecipe recipe, PriceGroup pGroup, long finalProductId = -1)
        {
            int materialsRow = DetailsMat.Count;
            for (int i = DetailsMat.Count - 1; i >= 0; i--) {
                if (!string.IsNullOrWhiteSpace (DetailsMat [i].ItemName))
                    break;

                materialsRow = i;
            }

            int productsRow = DetailsProd.Count;
            for (int i = DetailsProd.Count - 1; i >= 0; i--) {
                if (!string.IsNullOrWhiteSpace (DetailsProd [i].ItemName))
                    break;

                productsRow = i;
            }

            ComplexProductionDetail prodDetail = new ComplexProductionDetail ();
            if (recipe.DetailsMat.Any (detail => !prodDetail.CheckItemCanEvaluate (Item.Cache.GetById (detail.ItemId))))
                return false;

            if (recipe.DetailsProd.Any (detail => !prodDetail.CheckItemCanEvaluate (Item.Cache.GetById (detail.ItemId))))
                return false;

            if (DetailsMat.Count <= materialsRow)
                AddNewDetail ();

            foreach (ComplexRecipeDetail detail in recipe.DetailsMat) {
                if (DetailsMat.Count <= materialsRow)
                    AddNewDetail ();

                prodDetail = DetailsMat [materialsRow];
                prodDetail.ItemEvaluate (Item.Cache.GetById (detail.ItemId), pGroup, true);
                prodDetail.Quantity = detail.Quantity;
                prodDetail.SourceRecipe = recipe;
                if (finalProductId >= 0)
                    prodDetail.FinalProductId = finalProductId;

                materialsRow++;
            }

            foreach (ComplexRecipeDetail detail in recipe.DetailsProd) {
                if (DetailsProd.Count <= productsRow)
                    AddNewAdditionalDetail ();

                prodDetail = DetailsProd [productsRow];
                prodDetail.ItemEvaluate (Item.Cache.GetById (detail.ItemId), pGroup, true);
                prodDetail.Quantity = detail.Quantity;
                prodDetail.SourceRecipe = recipe;
                if (finalProductId >= 0)
                    prodDetail.FinalProductId = finalProductId;

                productsRow++;
            }

            RecalculatePrices ();
            return true;
        }

        /// <summary>
        /// Removes all material and production details which use the specified recipe.
        /// </summary>
        /// <param name="recipe">The recipe that marks the details to remove.</param>
        public void RemoveRecipe (ComplexRecipe recipe)
        {
            for (int i = DetailsMat.Count - 1; i >= 0; i--)
                if (DetailsMat [i].SourceRecipe.Id == recipe.Id)
                    DetailsMat.RemoveAt (i);

            for (int i = DetailsProd.Count - 1; i >= 0; i--)
                if (DetailsProd [i].SourceRecipe.Id == recipe.Id)
                    DetailsProd.RemoveAt (i);
        }

        public void RecalculatePrices ()
        {
            if (DetailsMat.Any (d => d.SourceRecipe != null))
                foreach (var materials in DetailsMat.ToLookup (d => d.SourceRecipe, d => d)) {
                    var g = materials;
                    var products = DetailsProd.Where (d => ReferenceEquals (g.Key, d.SourceRecipe)).ToList ();

                    RecalculateProductPrices (materials, products);
                }
            else
                foreach (var materials in DetailsMat.ToLookup (d => d.Note, d => d)) {
                    var g = materials;
                    var products = DetailsProd.Where (d => g.Key == d.Note).ToList ();

                    RecalculateProductPrices (materials, products);
                }
        }

        private static void RecalculateProductPrices (IEnumerable<ComplexProductionDetail> materials, List<ComplexProductionDetail> products)
        {
            double totalMat = materials.Sum (t => t.Total);
            double totalProd = products.Sum (t => t.Quantity * t.PriceOut);

            bool allPriceOutAreZero = false;
            if (totalProd.IsZero () && products.Count > 0) {
                allPriceOutAreZero = products.All (t => t.PriceOut <= 0);
                if (allPriceOutAreZero)
                    totalProd += products.Sum (t => t.Quantity);
            }

            foreach (ComplexProductionDetail detail in products) {
                detail.OriginalPriceInEvaluate (totalProd.IsZero () ? 0 : (allPriceOutAreZero ? 1 : detail.PriceOut) * totalMat / totalProd);
                detail.TotalEvaluate ();
            }
        }

        public static LazyListModel<ComplexProduction> GetAll (DataQuery dataQuery)
        {
            AddPartnerLocationFilters (ref dataQuery, false);
            return BusinessDomain.DataAccessProvider.GetAllComplexProductions<ComplexProduction> (dataQuery);
        }

        public static ComplexProduction GetById (long id)
        {
            ComplexProduction production = BusinessDomain.DataAccessProvider.GetOperationById<ComplexProduction> (OperationType.ComplexProductionMaterial, id);
            if (production != null) {
                production.LoadDetails ();
                production.IsDirty = false;
            }

            return production;
        }

        public static ComplexProduction GetPending (long pId, long lId)
        {
            ComplexProduction production = BusinessDomain.DataAccessProvider.GetPendingOperation<ComplexProduction> (OperationType.ComplexProductionMaterial, pId, lId);
            if (production != null) {
                production.LoadDetails ();
                production.IsDirty = false;
            }

            return production;
        }

        private void SetPriceGroup ()
        {
            pGroupField = Item.GetPriceGroupField (GetPriceGroup ());
        }
    }
}