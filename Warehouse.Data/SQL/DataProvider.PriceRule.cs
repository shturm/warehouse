//
// DataProvider.PriceRule.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   11.04.2009
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
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        public override LazyListModel<T> GetAllPriceRules<T> ()
        {
            return ExecuteLazyModel<T> (string.Format ("SELECT {0} FROM pricerules ORDER BY Priority",
                PriceRulesDefaultAliases ()));
        }

        public override void AddUpdatePriceRule (object priceRule)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (priceRule);

            using (DbTransaction transaction = new DbTransaction (this)) {

                // Check if we already have this price rule
                long count = ExecuteScalar<long> ("SELECT count(*) FROM pricerules WHERE ID = @ID",
                    helper.Parameters);
                int affectedRows;

                switch (count) {
                    case 0:
                        affectedRows = ExecuteNonQuery (string.Format ("INSERT INTO pricerules {0}",
                            helper.GetColumnsAndValuesStatement (DataField.PriceRuleId)), helper.Parameters);

                        if (affectedRows != 1)
                            throw new Exception (string.Format ("Cannot add price rule with name=\'{0}\'",
                                helper.GetObjectValue (DataField.PriceRuleName)));

                        long insertedId = GetLastAutoId ();
                        helper.SetObjectValue (DataField.PriceRuleId, insertedId);
                        break;
                    case 1:
                        affectedRows = ExecuteNonQuery (string.Format ("UPDATE pricerules {0} WHERE ID = @ID",
                            helper.GetSetStatement (DataField.PriceRuleId)), helper.Parameters);

                        if (affectedRows != 1)
                            throw new Exception (string.Format ("Cannot update price rules with ID={0}",
                                helper.GetObjectValue (DataField.PriceRuleId)));
                        break;
                    default:
                        throw new Exception ("Too many entries with the same ID found in pricerules table.");
                }

                transaction.Complete ();
            }
        }

        public override DeletePermission CanDeletePriceRule (long id)
        {
            return DeletePermission.Yes;
        }

        public override void DeletePriceRule (long priceRuleId)
        {
            DbParam dbParameter = new DbParam ("priceRuleId", priceRuleId);

            using (DbTransaction transaction = new DbTransaction (this)) {

                int priority = ExecuteScalar<int> ("SELECT Priority FROM pricerules WHERE ID = @priceRuleId", dbParameter);
                ExecuteNonQuery ("DELETE FROM pricerules WHERE ID = @priceRuleId", dbParameter);
                ExecuteNonQuery ("UPDATE pricerules SET Priority = Priority - 1 WHERE Priority > @priority",
                    new DbParam ("priority", priority));

                transaction.Complete ();
            }
        }

        private string PriceRulesDefaultAliases ()
        {
            return GetAliasesString (DataField.PriceRuleId,
                DataField.PriceRuleName,
                DataField.PriceRuleFormula,
                DataField.PriceRuleEnabled,
                DataField.PriceRulePriority);
        }
    }
}
