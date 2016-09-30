//
// ComplexRecipe.cs
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
using System.Linq;
using Warehouse.Business.Entities;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Data.Model;

namespace Warehouse.Business.Operations
{
    public class ComplexRecipe : Operation<ComplexRecipeDetail>, IPersistableEntity<ComplexRecipe>, IStrongEntity
    {
        #region Private members

        private string name = string.Empty;

        private BindList<ComplexRecipeDetail> detailsMat = new BindList<ComplexRecipeDetail> ();
        private BindList<ComplexRecipeDetail> detailsProd = new BindList<ComplexRecipeDetail> ();

        #endregion

        #region Public properties

        [DbColumn (DataField.OperationDetailNote)]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public BindList<ComplexRecipeDetail> DetailsMat
        {
            get
            {
                if ((State == OperationState.Saved || State == OperationState.Draft) && detailsMat.Count == 0) {
                    detailsMat = new BindList<ComplexRecipeDetail> (BusinessDomain.DataAccessProvider.GetComplexRecipeMatDetailsById<ComplexRecipeDetail> (id));
                    if (detailsMat.Count == 0)
                        AddNewDetail ();
                }

                return detailsMat;
            }
        }

        public BindList<ComplexRecipeDetail> DetailsProd
        {
            get
            {
                if ((State == OperationState.Saved || State == OperationState.Draft) && detailsProd.Count == 0) {
                    detailsProd = new BindList<ComplexRecipeDetail> (BusinessDomain.DataAccessProvider.GetComplexRecipeProdDetailsById<ComplexRecipeDetail> (id));
                    if (detailsProd.Count == 0)
                        AddNewAdditionalDetail ();
                }

                return detailsProd;
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

        protected override void LoadDetails (bool usePriceIn)
        {
        }

        #endregion

        public ComplexRecipe ()
        {
            partnerId = 1;
            locationId = 1;
            operationType = OperationType.ComplexRecipeMaterial;
            detailsProd.ListChanged += (sender, args) =>
                {
                    foreach (ComplexRecipeDetail detail in detailsProd) {
                        detail.PropertyChanged -= detailProduct_PropertyChanged;
                        detail.PropertyChanged += detailProduct_PropertyChanged;
                    }
                    RecalculatePrices ();
                };
            foreach (ComplexRecipeDetail detail in detailsProd)
                detail.PropertyChanged += detailProduct_PropertyChanged;
        }

        public ComplexRecipe (ComplexProduction production)
            : this ()
        {
            locationId = production.LocationId;
            location = production.Location;
            location2 = production.Location2;
            partnerId = production.PartnerId;
            partnerName = production.PartnerName;
            partnerName2 = production.PartnerName2;
            userId = production.UserId;
            userName = production.UserName;
            userName2 = production.UserName2;
            loggedUserId = production.LoggedUserId;
            loggedUserName = production.LoggedUserName;
            loggedUserName2 = production.LoggedUserName2;

            foreach (ComplexProductionDetail detail in production.DetailsMat)
                detailsMat.Add (new ComplexRecipeDetail (detail));

            detailsMat = GetCombinedDetails (false, true, detailsMat);

            foreach (ComplexProductionDetail detail in production.DetailsProd)
                detailsProd.Add (new ComplexRecipeDetail (detail));

            detailsProd = GetCombinedDetails (false, true, detailsProd);
        }

        public override void Commit ()
        {
            CommitChanges ();
        }

        public ComplexRecipe CommitChanges ()
        {
            bool editMode = true;

            try {
                using (DbTransaction transaction = new DbTransaction (BusinessDomain.DataAccessProvider)) {
                    // Create a new operation Id if needed
                    if (State == OperationState.New || State == OperationState.NewDraft) {
                        id = CreateNewId ();
                        editMode = false;
                    }

                    // Save the output ComplexRecipe in the database
                    OperationType = OperationType.ComplexRecipeMaterial;
                    foreach (ComplexRecipeDetail detail in DetailsMat)
                        detail.Note = name;
                    BusinessDomain.DataAccessProvider.AddUpdateComplexRecipeMat (this, DetailsMat.ToArray ());

                    // Save the input ComplexRecipe in the database
                    OperationType = OperationType.ComplexRecipeProduct;
                    foreach (ComplexRecipeDetail detail in DetailsProd)
                        detail.Note = name;
                    BusinessDomain.DataAccessProvider.AddUpdateComplexRecipeProd (this, DetailsProd.ToArray ());

                    if (editMode && Total.IsZero ()) {
                        BusinessDomain.DataAccessProvider.DeleteOperationId (OperationType.ComplexRecipeMaterial, id);

                        string format = Translator.GetString ("Void recipe No.{0} from {1}");
                        ApplicationLogEntry.AddNew (string.Format (format, GetFormattedOperationNumber (id), 
                            BusinessDomain.GetFormattedDate (date)));
                    } else if (editMode) {
                        string format = Translator.GetString ("Edit recipe No.{0} from {1}");
                        ApplicationLogEntry.AddNew (string.Format (format, GetFormattedOperationNumber (id), 
                            BusinessDomain.GetFormattedDate (date)));
                    }

                    if (editMode)
                        RemoveAllEmptyDetails ();

                    transaction.Complete ();
                }
            } catch {
                if (!editMode)
                    id = -1;

                throw;
            }

            return this;
        }

        public override void ClearDetails (bool logChange = true)
        {
            if (id < 0) {
                DetailsMat.Clear ();
                DetailsProd.Clear ();
            } else {
                foreach (ComplexRecipeDetail detail in DetailsMat)
                    detail.Quantity = 0;

                foreach (ComplexRecipeDetail detail in DetailsProd)
                    detail.Quantity = 0;
            }
        }

        public override ComplexRecipeDetail AddNewDetail ()
        {
            return (ComplexRecipeDetail) DetailsMat.AddNew ();
        }

        public override ComplexRecipeDetail AddNewAdditionalDetail ()
        {
            return (ComplexRecipeDetail) DetailsProd.AddNew ();
        }

        public static LazyListModel<ComplexRecipe> GetAll ()
        {
            return BusinessDomain.DataAccessProvider.GetAllComplexRecipes<ComplexRecipe> ();
        }

        public static ComplexRecipe [] GetAllByProductId (long id)
        {
            return BusinessDomain.DataAccessProvider.GetAllComplexRecipesByProductId<ComplexRecipe> (id);
        }

        public static ComplexRecipe GetById (long id)
        {
            ComplexRecipe ret = BusinessDomain.DataAccessProvider.GetComplexRecipeById<ComplexRecipe> (id);
            if (ret != null) {
                ret.RecalculatePrices ();
                ret.IsDirty = false;
            }
            return ret;
        }

        protected override long CreateNewId ()
        {
            return BusinessDomain.DataAccessProvider.CreateNewOperationId (operationType, 0, currentState: State);
        }

        public override bool Validate (ValidateCallback callback, StateHolder state)
        {
            if (callback == null)
                throw new ArgumentNullException ("callback");

            if (string.IsNullOrEmpty (name)) {
                callback (Translator.GetString ("Recipe name cannot be empty!"), ErrorSeverity.Error, 0, state);
                return false;
            }

            return true;
        }

        public void RecalculatePrices ()
        {
            BindList<ComplexRecipeDetail> products = detailsProd;

            double totalMat = detailsMat.Sum (t => t.Total);
            double totalProd = products.Sum (t => t.Quantity * t.PriceOut);

            bool allPriceOutAreZero = false;
            if (totalProd.IsZero () && products.Count > 0) {
                allPriceOutAreZero = products.All (t => t.PriceOut <= 0);
                if (allPriceOutAreZero)
                    totalProd += products.Sum (t => t.Quantity);
            }

            foreach (ComplexRecipeDetail detail in products.Where (p => p.Quantity != 0)) {
                detail.OriginalPriceInEvaluate (totalProd.IsZero () ? 0 : (allPriceOutAreZero ? 1 : detail.PriceOut) * totalMat / totalProd);
                detail.TotalEvaluate ();
            }
        }

        private void detailProduct_PropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RecalculatePrices ();
        }
    }
}