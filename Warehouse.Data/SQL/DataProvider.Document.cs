//
// DataProvider.Document.cs
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Warehouse.Data.Model;

namespace Warehouse.Data.SQL
{
    public abstract partial class DataProvider
    {
        #region Retrieve

        public override long GetNextDocumentId (long locationId)
        {
            if (locationId > 0) {
                long max = 1;
                long operationDocumentNumber = long.MinValue;
                object scalar = ExecuteScalar (@"
                    SELECT MAX(CASE WHEN InvoiceNumber REGEXP '^[0-9]+$' THEN CAST(InvoiceNumber AS UNSIGNED) ELSE 0 END) as LastInvoice
                    FROM documents
                    WHERE OperType IN (2, 14, 16, 26, 27) AND
                        (SELECT operations.ObjectID FROM operations 
                        WHERE operations.OperType = documents.OperType AND operations.Acct = documents.Acct
                        LIMIT 1) = @locationID",
                    new DbParam ("locationID", locationId));

                if (!IsDBNull (scalar))
                    max += Convert.ToInt64 (scalar);

                // 101 is the "operation type" of an invoice
                scalar = ExecuteScalar (@"
                    SELECT MAX(Acct) FROM operations 
                    WHERE OperType = 101 AND ObjectID = @locationID",
                    new DbParam ("locationID", locationId));

                if (!IsDBNull (scalar))
                    operationDocumentNumber = Convert.ToInt64 (scalar) + 1;

                return Math.Max (max, operationDocumentNumber);
            }

            object res = ExecuteScalar (@"
                SELECT MAX(CASE WHEN InvoiceNumber REGEXP '^[0-9]+$' THEN CAST(InvoiceNumber AS UNSIGNED) ELSE 0 END) as LastInvoice
                FROM documents
                WHERE OperType IN (2, 14, 16, 27, 26)");

            return IsDBNull (res) ? 1 : 1 + Convert.ToInt64 (res);
        }

        public override long GetLastDocumentNumber (long operationNumber, long operationType)
        {
            // Check if we have any operations that match
            object res = ExecuteScalar (@"
                SELECT MAX(CASE WHEN InvoiceNumber REGEXP '^[0-9]+$' THEN CAST(InvoiceNumber AS UNSIGNED) ELSE -1 END) as InvNumber
                FROM documents
                WHERE OperType = @operationType AND Acct = @operationNumber",
                new DbParam ("operationNumber", operationNumber),
                new DbParam ("operationType", (int) operationType));

            return IsDBNull (res) ? -1 : Convert.ToInt64 (res);
        }

        public override bool IsDocumentNumberUsed (long number)
        {
            // Check if we have any sales
            object res = ExecuteScalar (@"
                SELECT 1 FROM documents WHERE EXISTS (
                    SELECT * FROM documents
                    WHERE OperType IN (2, 14, 16, 26, 27) AND DocumentType IN (0,2,1) AND
                    CASE WHEN InvoiceNumber REGEXP '^[0-9]+$' THEN CAST(InvoiceNumber AS UNSIGNED) ELSE 0 END = @number)",
                new DbParam ("number", number));

            return res != null;
        }

        public override bool DocumentExistsForOperation (long operationNumber, long operationType)
        {
            DbParam [] pars = new DbParam [2];
            pars [0] = new DbParam ("operationNumber", operationNumber);
            pars [1] = new DbParam ("operationType", (int) operationType);

            // Check if we have any operations that match
            object res = ExecuteScalar (@"
                SELECT COUNT(*)
                FROM documents
                WHERE OperType = @operationType AND Acct = @operationNumber", pars);

            return !IsDBNull (res) && Convert.ToInt64 (res) > 0;
        }

        public override LazyListModel<T> GetAllDocuments<T> (DataQuery dataQuery, DocumentType documentType,
            bool extendedInfo = false, params OperationType [] operationTypes)
        {
            StringBuilder operationTypeConditionBuilder = new StringBuilder (" IN (");
            foreach (OperationType operationType in operationTypes) {
                operationTypeConditionBuilder.Append ((int) operationType);
                operationTypeConditionBuilder.Append (", ");
            }
            operationTypeConditionBuilder.Remove (operationTypeConditionBuilder.Length - 2, 2);
            operationTypeConditionBuilder.Append (')');
            fieldsTable.UseAltNames = true;
            string query = string.Format (@"
                {0}{1}
                FROM ((documents LEFT JOIN operations ON documents.Acct = operations.Acct AND documents.OperType = operations.OperType)
                 INNER JOIN partners ON partners.ID = operations.PartnerID)
                 INNER JOIN objects ON objects.ID = operations.ObjectID
                WHERE documents.DocumentType = {2} AND documents.OperType {3}
                GROUP BY documents.InvoiceNumber, documents.InvoiceDate, partners.Company",
                GetSelect (new [] { DataField.DocumentId, DataField.DocumentNumber, DataField.DocumentDate, DataField.DocumentDescription, 
                    DataField.PartnerName, DataField.PartnerLiablePerson, DataField.PartnerCity, 
                    DataField.PartnerAddress, DataField.PartnerTaxNumber, DataField.PartnerBulstat, 
                    DataField.PartnerBankName, DataField.PartnerBankAcct, DataField.PartnerCode, 
                    DataField.DocumentLocation, DataField.DocumentOperationType, DataField.DocumentOperationNumber, 
                    DataField.OperationDate, DataField.OperationTotal, DataField.OperationVatSum }),
                extendedInfo ?
                    string.Format (", SUM(operations.PriceIn * operations.Qtty) AS {0}, " +
                        "SUM(operations.VatIn * operations.Qtty) AS {1}, operations.Note AS {2}",
                        fieldsTable.GetFieldAlias (DataField.PurchaseTotal), fieldsTable.GetFieldAlias (DataField.PurchaseVATSum),
                        fieldsTable.GetFieldAlias (DataField.OperationDetailNote))
                    : string.Empty,
                (int) documentType, operationTypeConditionBuilder);
            fieldsTable.UseAltNames = false;

            return ExecuteDataQuery<T> (dataQuery, query);
        }

        #endregion

        #region Suggestions

        public override List<string> GetDocumentRecipientSuggestions (long partnerId)
        {
            DbParam par = new DbParam ("partnerId", partnerId);

            string mol = ExecuteScalar<string> (@"
                SELECT MOL
                FROM partners
                WHERE partners.ID = @partnerId", par);

            List<string> suggestions = new List<string> ();
            if (!string.IsNullOrWhiteSpace (mol))
                suggestions.Add (mol);

            using (IDataReader reader = ExecuteReader (@"
                SELECT COUNT(*) as RecpCnt, t.Recipient as Recipient
                FROM
                (SELECT documents.Recipient
                 FROM documents INNER JOIN operations ON documents.Acct = operations.Acct
                 WHERE documents.Recipient <> '' AND documents.Recipient <> ' ' AND documents.OperType IN (2, 14, 16, 27, 26) AND operations.PartnerID = @partnerId
                 GROUP BY operations.Acct) as t
                GROUP BY t.Recipient
                ORDER BY 1 DESC", par))
                while (reader.Read ())
                    suggestions.Add ((string) reader.GetValue (1));

            return suggestions;
        }

        public override List<string> GetDocumentEGNSuggestions (long partnerId)
        {
            List<string> suggestions = new List<string> ();
            using (IDataReader reader = ExecuteReader (@"
                SELECT COUNT(*) as Cnt, t.EGN as EGN
                FROM
                (SELECT documents.EGN
                 FROM documents INNER JOIN operations ON documents.Acct = operations.Acct
                 WHERE documents.EGN <> '' AND documents.EGN <> ' ' AND documents.OperType IN (2, 14, 16, 27, 26) AND operations.PartnerID = @partnerId
                 GROUP BY operations.Acct) as t
                GROUP BY t.EGN
                ORDER BY 1 DESC
                LIMIT 15", new DbParam ("partnerId", partnerId)))
                while (reader.Read ())
                    suggestions.Add ((string) reader.GetValue (1));

            return suggestions;
        }

        public override List<string> GetDocumentProviderSuggestions ()
        {
            List<string> suggestions = new List<string> ();
            using (IDataReader reader = ExecuteReader (@"
                SELECT COUNT(documents.Provider), documents.Provider
                FROM documents
                WHERE documents.Provider <> '' AND documents.Provider <> ' '
                GROUP BY Provider
                ORDER BY 1 DESC
                LIMIT 15"))
                while (reader.Read ())
                    suggestions.Add ((string) reader.GetValue (1));

            return suggestions;
        }

        public override List<string> GetDocumentReasonSuggestions ()
        {
            List<string> suggestions = new List<string> { string.Empty };
            using (IDataReader reader = ExecuteReader (@"
                SELECT COUNT(documents.Reason), documents.Reason
                FROM documents
                WHERE documents.Reason <> '' AND documents.Reason <> ' '
                GROUP BY Reason
                ORDER BY 1 DESC
                LIMIT 15"))
                while (reader.Read ())
                    suggestions.Add ((string) reader.GetValue (1));

            return suggestions;
        }

        public override List<string> GetDocumentDescriptionSuggestions ()
        {
            List<string> suggestions = new List<string> { string.Empty };
            using (IDataReader reader = ExecuteReader (@"
                SELECT COUNT(documents.Description), documents.Description
                FROM documents
                WHERE documents.Description <> '' AND documents.Description <> ' '
                GROUP BY Description
                ORDER BY 1 DESC
                LIMIT 15"))
                while (reader.Read ()) 
                    suggestions.Add ((string) reader.GetValue (1));

            return suggestions;
        }

        public override IEnumerable<string> GetDocumentLocationSuggestions ()
        {
            List<string> suggestions = new List<string> { string.Empty };
            using (IDataReader reader = ExecuteReader (@"
                SELECT COUNT(documents.Place), documents.Place
                FROM documents
                WHERE documents.Place <> '' AND documents.Place <> ' '
                GROUP BY Place
                ORDER BY 1 DESC
                LIMIT 15"))
                while (reader.Read ())
                    suggestions.Add ((string) reader.GetValue (1));

            return suggestions;
        }

        #endregion

        #region Save

        private static string GetFormattedDocumentNumber (long operationNumber, int docNumberLen)
        {
            return Math.Abs (operationNumber).ToString ().PadLeft (docNumberLen, '0');
        }

        public override void CreateNewDocumentId (long invoiceId, int docNumberLen)
        {
            // This is needed since someone might be creating an operation at this moment and
            // also have reserved only an id in nextacct table. Try 10 more ids before giving up.
            DbParam pars = new DbParam ("NextAcct", "INV" + GetFormattedDocumentNumber (invoiceId, docNumberLen));

            if (ExecuteScalar<long> ("SELECT count(*) FROM nextacct WHERE NextAcct = @NextAcct", pars) != 0)
                throw new InvoiceNumberInUseException ();

            if (ExecuteNonQuery ("INSERT INTO nextacct (NextAcct) VALUES(@NextAcct)", pars) != 1)
                throw new Exception ("Unable to insert new record into nextacct table.");
        }

        #endregion
    }
}
