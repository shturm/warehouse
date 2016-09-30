//
// DataProvider.ComplexRecipe.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   10.12.2007
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

using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override LazyListModel<T> GetAllComplexRecipes<T> ()
        {
            string query = string.Format (@"
                SELECT {0}, users1.Name as {1}, users1.Name2 as {2}, users2.Name as {3}, users2.Name2 as {4}
                FROM (((operations INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.OperType = 22
                GROUP BY Acct", ComplexRecipeDefaultAliases (),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2));

            return ExecuteLazyModel<T> (query);
        }

        public override T [] GetAllComplexRecipesByProductId<T> (long id)
        {
            string query = string.Format (@"
                SELECT {0}, users1.Name as {1}, users1.Name2 as {2}, users2.Name as {3}, users2.Name2 as {4}
                FROM (((operations INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.OperType = 23 AND operations.GoodID = @ItemId
                GROUP BY Acct", ComplexRecipeDefaultAliases (),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2));

            return ExecuteArray<T> (query, new DbParam ("ItemId", id));
        }

        public override T GetComplexRecipeById<T> (long id)
        {
            string query = string.Format (@"
                SELECT {0}, users1.Name as {1}, users1.Name2 as {2}, users2.Name as {3}, users2.Name2 as {4}
                FROM (((operations INNER JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN users as users2 ON operations.UserID = users2.ID)
                WHERE operations.OperType = 22 AND Acct = @ComplexRecipeId
                GROUP BY Acct", ComplexRecipeDefaultAliases (),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName2),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName),
                fieldsTable.GetFieldAlias (DataField.OperationsUserName2));

            return ExecuteObject<T> (query, new DbParam ("ComplexRecipeId", id));
        }

        public override LazyListModel<T> GetComplexRecipeMatDetailsById<T> (long id)
        {
            string query = string.Format (@"
                SELECT {0}, goods.Measure1 as {1}, goods.Name as {2}, goods.Name2 as {3}, operations.PriceIn * operations.Qtty as {4}, goods.PriceIn AS {5}
                FROM operations LEFT JOIN goods ON operations.GoodID = goods.ID
                WHERE operations.OperType = 22 AND Acct = @ComplexRecipeId", ComplexRecipeDefaultAliases (),
                fieldsTable.GetFieldAlias (DataField.ItemMeasUnit),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.ItemName2),
                fieldsTable.GetFieldAlias (DataField.OperationDetailTotalIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailPriceIn));

            return ExecuteLazyModel<T> (query, new DbParam ("ComplexRecipeId", id));
        }

        public override LazyListModel<T> GetComplexRecipeProdDetailsById<T> (long id)
        {
            string query = string.Format (@"
                SELECT {0}, goods.Measure1 as {1}, goods.Name as {2}, goods.Name2 as {3}, operations.PriceIn * operations.Qtty as {4}
                FROM operations LEFT JOIN goods ON operations.GoodID = goods.ID
                WHERE operations.OperType = 23 AND Acct = @ComplexRecipeId", ComplexRecipeDefaultAliases (),
                fieldsTable.GetFieldAlias (DataField.ItemMeasUnit),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.ItemName2),
                fieldsTable.GetFieldAlias (DataField.OperationDetailTotalIn));

            return ExecuteLazyModel<T> (query, new DbParam ("ComplexRecipeId", id));
        }

        #endregion

        #region Save

        public override void AddUpdateComplexRecipeProd (object obj, object [] detailObj)
        {
            foreach (object detail in detailObj)
                AddUpdateDetail (obj, detail);
        }

        public override void AddUpdateComplexRecipeMat (object obj, object [] detailObj)
        {
            foreach (object detail in detailObj)
                AddUpdateDetail (obj, detail);
        }

        #endregion

        #region Reports

        public override DataQueryResult ReportComplexRecipes (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT operations.Acct as {0}, operations.Date as {1}, goods.Code as {2}, goods.Name as {3},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.name as {4}, users1.Name as {5}, usersgroups1.Name AS {6},
                  operations.Qtty as {7}, goods.Measure1, operations.PriceIn as {8},
                  (operations.Qtty * operations.PriceIn) as {9},
                  operations.OperType as {10},
                  operations.Note as {11}
                FROM ((((((operations LEFT JOIN goods ON operations.GoodID=goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN objects ON operations.ObjectID = objects.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID)
                  LEFT JOIN operationtype ON operations.OperType = operationtype.ID
                WHERE (operations.OperType = 22 OR operations.OperType = 23)",
                fieldsTable.GetFieldAlias (DataField.OperationNumber),
                fieldsTable.GetFieldAlias (DataField.OperationDate),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.ItemsGroupName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorsGroupsName),
                fieldsTable.GetFieldAlias (DataField.OperationDetailQuantity),
                fieldsTable.GetFieldAlias (DataField.OperationDetailPriceIn),
                fieldsTable.GetFieldAlias (DataField.OperationDetailSumIn),
                fieldsTable.GetFieldAlias (DataField.OperationType),
                fieldsTable.GetFieldAlias (DataField.OperationDetailNote));

            querySet.SetComplexId (DbTable.Operations, DataField.OperationNumber, DataField.OperationType, OperationType.ComplexRecipeMaterial);

            return ExecuteDataQuery (querySet, query);
        }

        #endregion

        private string ComplexRecipeDefaultAliases ()
        {
            return GetAliasesString (DataField.OperationType,
                DataField.OperationNumber,
                DataField.OperationLocationId,
                DataField.OperationPartnerId,
                DataField.OperationOperatorId,
                DataField.OperationUserId,
                DataField.OperationTimeStamp,
                DataField.OperationDate,
                DataField.OperationDetailId,
                DataField.OperationDetailItemId,
                DataField.OperationDetailQuantity,
                DataField.OperationDetailPriceIn,
                DataField.OperationDetailPriceOut,
                DataField.OperationDetailDiscount,
                DataField.OperationDetailCurrencyId,
                DataField.OperationDetailCurrencyRate,
                DataField.OperationDetailReferenceId,
                DataField.OperationDetailLot,
                DataField.OperationDetailNote,
                DataField.OperationDetailVatIn,
                DataField.OperationDetailVatOut,
                DataField.OperationDetailSign,
                DataField.OperationDetailLotId);
        }
    }
}
