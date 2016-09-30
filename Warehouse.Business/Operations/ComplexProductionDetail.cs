//
// ComplexProductionDetail.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   07/02/2006
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

using System.Linq;
using Warehouse.Business.Entities;

namespace Warehouse.Business.Operations
{
    public class ComplexProductionDetail : PriceInOperationDetail
    {
        public ComplexRecipe SourceRecipe { get; set; }

        public override bool UsesSavedLots
        {
            get { return true; }
        }

        public ComplexProductionDetail ()
        {
            lotId = BusinessDomain.AppConfiguration.ItemsManagementUseLots ? -1 : 1;
        }

        public bool ItemEvaluate (Item item, PriceGroup priceGroup, bool updatePrice, ComplexProduction parent)
        {
            bool ret = ItemEvaluate (item, priceGroup, updatePrice);
            if (ret)
                parent.RecalculatePrices ();

            return ret;
        }

        public void QuantityEvaluate (double value, ComplexProduction parent)
        {
            Quantity = value;

            if (SourceRecipe != null) {
                ComplexRecipeDetail recDetail = GetMatchingRecipeDetail (this);
                if (recDetail != null) {
                    double coef = value / recDetail.Quantity;

                    foreach (ComplexProductionDetail detail in parent.DetailsMat) {
                        if (!ReferenceEquals (detail.SourceRecipe, SourceRecipe))
                            continue;

                        if (detail.ItemId == itemId)
                            continue;

                        recDetail = GetMatchingRecipeDetail (detail);
                        if (recDetail == null)
                            continue;

                        detail.Quantity = recDetail.Quantity * coef;
                    }

                    foreach (ComplexProductionDetail detail in parent.DetailsProd) {
                        if (!ReferenceEquals (detail.SourceRecipe, SourceRecipe))
                            continue;

                        if (detail.ItemId == itemId)
                            continue;

                        recDetail = GetMatchingRecipeDetail (detail);
                        if (recDetail == null)
                            continue;

                        detail.Quantity = recDetail.Quantity * coef;
                    }
                }
            }

            parent.RecalculatePrices ();
        }

        private ComplexRecipeDetail GetMatchingRecipeDetail (ComplexProductionDetail prodDetail)
        {
            if (SourceRecipe == null)
                return null;

            return SourceRecipe.DetailsMat.FirstOrDefault (d => d.ItemId == prodDetail.itemId) ??
                SourceRecipe.DetailsProd.FirstOrDefault (d => d.ItemId == prodDetail.itemId);
        }
    }
}
