//
// DataProvider.CashBook.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   10.13.2007
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
using System.Text;
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrive

        public override LazyListModel<T> GetAllCashBookEntries<T> (DataQuery dataQuery)
        {
            string query = string.Format (@"
                {0}
                FROM (cashbook LEFT JOIN objects ON cashbook.ObjectID = objects.ID) LEFT JOIN users ON cashbook.UserID = users.ID
                ORDER BY cashbook.`Date` DESC", 
                GetSelect (new [] { DataField.CashEntryId, DataField.CashEntryAmount, DataField.CashEntryOperatorId, 
                    DataField.CashEntryDescription, DataField.CashEntryDirection, DataField.CashEntryTurnoverType, 
                    DataField.CashEntryLocationId, DataField.CashEntryDate, 
                    DataField.LocationName, DataField.LocationCode,
                    DataField.UserName }));

            return ExecuteDataQuery<T> (dataQuery, query);
        }

        public override IEnumerable<long> GetCashBookLocationIds (DataQuery dataQuery)
        {
            return ExecuteDataQuery<long> (dataQuery, 
                string.Format (@"{0} FROM cashbook GROUP BY cashbook.ObjectID", 
                    GetSelect (new [] { DataField.CashEntryLocationId })));
        }

        public override IDictionary<int, double> GetCashBookBalances<T> (DataQuery dataQuery)
        {
            LazyListModel<T> model = ExecuteDataQuery<T> (dataQuery,
                string.Format (@"SELECT cashbook.Sign AS {0}, SUM(cashbook.Profit) AS {1} FROM cashbook GROUP BY cashbook.Sign",
                    fieldsTable.GetFieldAlias (DataField.CashEntryDirection),
                    fieldsTable.GetFieldAlias (DataField.CashEntryAmount)));
            Dictionary<int, double> balances = new Dictionary<int, double> ();
            switch (model.Count) {
                case 0:
                    balances.Add (-1, 0);
                    balances.Add (1, 0);
                    break;
                case 1:
                    int existingSign = (int) model [0, "PaymentDirection"];
                    balances.Add (-existingSign, 0);
                    balances.Add (existingSign, (double) model [0, "Amount"]);
                    break;
                default:
                    balances.Add ((int) model [0, "PaymentDirection"], (double) model [0, "Amount"]);
                    balances.Add ((int) model [1, "PaymentDirection"], (double) model [1, "Amount"]);
                    break;
            }
            return balances;
        }

        #endregion

        #region Save / Delete

        public override bool AddUpdateCashBookEntry (object chashBookEntryObject)
        {
            SqlHelper helper = GetSqlHelper ();
            helper.AddObject (chashBookEntryObject);

            bool ret = false;

            // Check if we already have that entry
            long temp = ExecuteScalar<long> ("SELECT count(*) FROM cashbook WHERE ID = @ID", helper.Parameters);

            // We are updating cash book information
            if (temp == 1) {
                temp = ExecuteNonQuery (string.Format ("UPDATE cashbook {0} WHERE ID = @ID",
                    helper.GetSetStatement (DataField.CashEntryId, DataField.LocationCode, DataField.LocationName, DataField.UserName)), helper.Parameters);

                if (temp != 1)
                    throw new Exception ("Unable to update cashbook entry.");
            } // We are creating new cash book entry information
            else if (temp == 0) {
                temp = ExecuteNonQuery (string.Format ("INSERT INTO cashbook {0}",
                    helper.GetColumnsAndValuesStatement (DataField.CashEntryId, DataField.LocationCode, DataField.LocationName, DataField.UserName)), helper.Parameters);

                if (temp != 1)
                    throw new Exception ("Unable to insert cashbook entry.");

                ret = true;
            } else
                throw new Exception ("Wrong number of cashbook entries found with the given Id.");

            return ret;
        }

        public override void AddCashBookEntries (IEnumerable<object> cashBookEntries)
        {
            SqlHelper helper = GetSqlHelper ();

            List<List<DbParam>> parameters = new List<List<DbParam>> ();
            foreach (object payment in cashBookEntries) {
                helper.ChangeObject (payment, DataField.CashEntryId);
                parameters.Add (new List<DbParam> (helper.Parameters));
            }

            BulkInsert ("cashbook", helper.GetColumns (DataField.CashEntryId), parameters, "Unable to create cash book entry.");
        }

        public override bool DeleteCashBookEntry (long id)
        {
            return ExecuteNonQuery ("DELETE FROM cashbook WHERE ID = @ID", new DbParam ("ID", id)) == 1;
        }

        public override void DeleteCashBookEntries (DateTime date, long locationId)
        {
            DbParam paramDate = new DbParam ("date", date);
            DbParam paramLocationID = new DbParam ("locationID", locationId);
            while (ExecuteNonQuery (string.Format (@"
                DELETE FROM cashbook 
                WHERE cashbook.Date <= @date AND cashbook.ObjectID = @locationID
                {0}", LimitDelete), paramDate, paramLocationID) > 0) {
            }
        }

        public override IEnumerable<string> GetRecentDescriptions ()
        {
            return ExecuteList<string> (string.Format (@"
                {0}
                FROM cashbook
                ORDER BY `Date`
                LIMIT 10", GetSelect (new [] { DataField.CashEntryDescription })));
        }

        #endregion
    }
}
