//
// DataProvider.Invoice.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
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

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override T [] GetIssuedInvoicesByNumber<T> (long id)
        {
            string query = string.Format (@"
                SELECT {0}, partners.Company as {1}
                FROM (documents LEFT JOIN operations ON documents.Acct = operations.Acct AND documents.OperType = operations.OperType)
                 INNER JOIN partners ON partners.ID = operations.PartnerID
                WHERE documents.OperType IN (2, 16) AND documents.DocumentType = 0 AND 
                 CASE WHEN InvoiceNumber REGEXP '^[0-9]+$' THEN CAST(documents.InvoiceNumber AS UNSIGNED) ELSE 0 END = @id
                GROUP BY documents.Acct",
                InvoiceDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.PartnerName));

            return ExecuteArray<T> (query, new DbParam ("id", id));
        }

        public override T [] GetReceivedInvoicesByNumber<T> (long id)
        {
            string query = string.Format (@"
                SELECT {0}, partners.Company as {1}
                FROM (documents LEFT JOIN operations ON documents.Acct = operations.Acct AND documents.OperType = operations.OperType)
                 INNER JOIN partners ON partners.ID = operations.PartnerID
                WHERE documents.OperType = 1 AND documents.DocumentType = 0 AND 
                 CASE WHEN InvoiceNumber REGEXP '^[0-9]+$' THEN CAST(documents.InvoiceNumber AS UNSIGNED) ELSE 0 END = @id
                GROUP BY documents.Acct",
                InvoiceDefaultAliases,
                fieldsTable.GetFieldAlias (DataField.PartnerName));

            return ExecuteArray<T> (query, new DbParam ("id", id));
        }

        public override T GetReceivedInvoiceForOperation<T> (long operationNumber)
        {
            string query = string.Format (@"
                SELECT documents.ID as {0}, documents.InvoiceNumber, documents.InvoiceDate, partners.Company as {1}, objects.Name as {2}
                FROM ((documents LEFT JOIN operations ON documents.Acct = operations.Acct AND documents.OperType = operations.OperType)
                 INNER JOIN partners ON partners.ID = operations.PartnerID)
                 INNER JOIN objects ON objects.ID = operations.ObjectID
                WHERE documents.OperType = 1 AND documents.DocumentType = 0 AND documents.Acct = @operationNumber
                GROUP BY documents.InvoiceNumber, documents.InvoiceDate, partners.Company, objects.Name",
                fieldsTable.GetFieldAlias (DataField.DocumentId),
                fieldsTable.GetFieldAlias (DataField.PartnerName),
                fieldsTable.GetFieldAlias (DataField.DocumentLocation));

            return ExecuteObject<T> (query, new DbParam ("operationNumber", operationNumber));
        }

        #endregion

        #region Save

        public override void AddInvoice (IEnumerable<object> invoiceObjects, int docNumberLen, bool createNewId)
        {
            SqlHelper helper = GetSqlHelper ();

            using (DbTransaction transaction = new DbTransaction (this)) {

                List<List<DbParam>> parameters = new List<List<DbParam>> ();
                foreach (object invoiceObject in invoiceObjects) {
                    helper.ChangeObject (invoiceObject, DocumentNonDBFields);

                    if (createNewId) {
                        string invoiceNumber = (string) helper.GetObjectValue (DataField.DocumentNumber);
                        long invoiceId = long.Parse (invoiceNumber);
                        if (IsDocumentNumberUsed (invoiceId))
                            throw new InvoiceNumberInUseException ();

                        CreateNewDocumentId (invoiceId, docNumberLen);
                        createNewId = false;
                    }
                    parameters.Add (new List<DbParam> (helper.Parameters));
                }

                BulkInsert ("documents", helper.GetColumns (DocumentNonDBFields.Select (f => new DbField (f)).ToArray ()), parameters, "Unable to create new invoice.");
                transaction.Complete ();
            }
        }

        public override void DeleteInvoice (long invoiceNumber)
        {
            DbParam par = new DbParam ("invoiceNumber", invoiceNumber);

            ExecuteNonQuery ("DELETE FROM documents WHERE OperType IN (1, 2, 16) AND (InvoiceNumber REGEXP '^[0-9]+$') AND (CAST(InvoiceNumber AS UNSIGNED) = @invoiceNumber)", par);

            ExecuteNonQuery ("DELETE FROM nextacct WHERE (NextAcct REGEXP '^INV[0-9]+$') AND (CAST(SUBSTR(NextAcct, 4) AS UNSIGNED) = @invoiceNumber)", par);
        }

        #endregion

        #region Reports

        public override DataQueryResult ReportInvoicesIssued (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT CAST(documents.InvoiceNumber as UNSIGNED) as {0},
                  documents.InvoiceDate as {1}, operations.Acct as {2},
                  goods.Code as {3}, goods.Name as {4},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.Name, partners.Company, partnersgroups.Name, users1.Name as {5}, usersgroups1.Name,
                  documents.PaymentType as {6},
                  SUM(operations.Qtty * operations.PriceOut) as {7},
                  SUM(operations.Qtty * operations.VATOut) as {8},
                  documents.Reason, documents.Description, documents.Place
                FROM ((((((documents INNER JOIN operations ON documents.Acct = operations.Acct AND documents.OperType = operations.OperType)
                  LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID
                WHERE operations.OperType IN (2, 16)
                GROUP BY goods.Code, documents.InvoiceNumber, documents.InvoiceDate, operations.Acct,
                  goods.Name, goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name, 
                  partners.Company, partnersgroups.Name, users1.Name, usersgroups1.Name,
                  documents.Reason, documents.Description, documents.Place",
                fieldsTable.GetFieldAlias (DataField.DocumentNumber),
                fieldsTable.GetFieldAlias (DataField.DocumentDate),
                fieldsTable.GetFieldAlias (DataField.DocumentOperationNumber),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.DocumentPaymentTypeId),
                fieldsTable.GetFieldAlias (DataField.OperationSum),
                fieldsTable.GetFieldAlias (DataField.OperationVatSum));

            return ExecuteDataQuery (querySet, query);
        }

        public override DataQueryResult ReportInvoicesReceived (DataQuery querySet)
        {
            string query = string.Format (@"
                SELECT CAST(documents.InvoiceNumber as UNSIGNED) as {0},
                  documents.InvoiceDate as {1}, operations.Acct as {2},
                  goods.Code as {3}, goods.Name as {4},
                  goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3,
                  goodsgroups.Name, partners.Company, partnersgroups.Name, users1.Name as {5}, usersgroups1.Name,
                  documents.PaymentType as {6},
                  SUM(operations.Qtty * operations.PriceIn) as {7},
                  SUM(operations.Qtty * operations.VATIn) as {8},
                  documents.Reason, documents.Description, documents.Place
                FROM ((((((documents INNER JOIN operations ON documents.Acct = operations.Acct AND documents.OperType = operations.OperType)
                  LEFT JOIN goods ON operations.GoodID = goods.ID)
                  LEFT JOIN goodsgroups ON ABS(goods.GroupID) = goodsgroups.ID)
                  LEFT JOIN partners ON operations.PartnerID = partners.ID)
                  LEFT JOIN partnersgroups ON ABS(partners.GroupID) = partnersgroups.ID)
                  LEFT JOIN users as users1 ON operations.OperatorID = users1.ID)
                  LEFT JOIN usersgroups as usersgroups1 ON ABS(users1.GroupID) = usersgroups1.ID
                WHERE operations.OperType = 1
                GROUP BY goods.Code, documents.InvoiceNumber, documents.InvoiceDate, operations.Acct,
                  goods.Name, goods.BarCode1, goods.BarCode2, goods.BarCode3, goods.Catalog1, goods.Catalog2, goods.Catalog3, goodsgroups.Name, 
                  partners.Company, partnersgroups.Name, users1.Name, usersgroups1.Name,
                  documents.Reason, documents.Description, documents.Place",
                fieldsTable.GetFieldAlias (DataField.DocumentNumber),
                fieldsTable.GetFieldAlias (DataField.DocumentDate),
                fieldsTable.GetFieldAlias (DataField.DocumentOperationNumber),
                fieldsTable.GetFieldAlias (DataField.ItemCode),
                fieldsTable.GetFieldAlias (DataField.ItemName),
                fieldsTable.GetFieldAlias (DataField.OperationsOperatorName),
                fieldsTable.GetFieldAlias (DataField.DocumentPaymentTypeId),
                fieldsTable.GetFieldAlias (DataField.OperationSum),
                fieldsTable.GetFieldAlias (DataField.OperationVatSum));

            return ExecuteDataQuery (querySet, query);
        }

        #endregion
    }
}