// 
// ReceiptReport.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at yahoo dot com>
// 
// Created:
//    30.01.2013
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
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Reporting
{
    public abstract class ReceiptReport
    {
        private readonly List<DataFilter> filters = new List<DataFilter> ();

        public List<DataFilter> Filters
        {
            get { return filters; }
        }
        public abstract string Title { get; }
        public abstract DataFilterLabel DateLabel { get; }
        public abstract DataField DateField { get; }
        public abstract DataFilterLabel UserLabel { get; }
        public abstract DataField UserField { get; }

        protected ReceiptReport ()
        {
            string reportClass = GetType ().FullName;
            DataQuery querySet;
            if (!BusinessDomain.ReportQueryStates.TryGetValue (reportClass, out querySet))
                return;

            filters.AddRange (querySet.Filters);
        }

        protected abstract DataQueryResult ExecuteReport ();

        protected abstract IList<DataField> GetReportFields ();

        public abstract bool AllowOperatorsManage ();

        public KeyValuePair<string, string> [] GetReportLines ()
        {
            DataQueryResult qSet = ExecuteReport ();
            IList<DataField> dataFields = GetReportFields ();
            List<KeyValuePair<string, string>> ret = new List<KeyValuePair<string, string>> ();
            double total = 0;

            Dictionary<DataField, int> dataFieldsIndices = new Dictionary<DataField, int> (dataFields.Count);
            foreach (DataField dataField in dataFields)
                for (int i = 0; i < qSet.Columns.Length; i++)
                    if (qSet.Columns [i].Field.StrongField == dataField) {
                        dataFieldsIndices.Add (dataField, i);
                        break;
                    }

            foreach (LazyTableDataRow row in qSet.Result) {
                foreach (KeyValuePair<DataField, int> dataFieldIndex in dataFieldsIndices) {
                    object value = row [dataFieldIndex.Value];
                    string text;
                    string textValue = null;
                    switch (dataFieldIndex.Key) {
                        case DataField.OperationNumber:
                            text = Translator.GetString ("Document:");
                            textValue = Operation.GetFormattedOperationNumber (Convert.ToInt64 (value));
                            break;

                        case DataField.OperationDateTime:
                        case DataField.OperationTimeStamp:
                            text = Translator.GetString ("Date/Time:");
                            textValue = BusinessDomain.GetFormattedDateTime (Convert.ToDateTime (value));
                            break;

                        case DataField.UserName:
                        case DataField.OperationsOperatorName2:
                            text = Translator.GetString ("Operator:");
                            break;

                        case DataField.OperationLocation2:
                            text = Translator.GetString ("Location:");
                            break;

                        case DataField.OperationPartner2:
                            text = Translator.GetString ("Client:");
                            break;

                        case DataField.OperationTotal:
                        case DataField.PaymentAmount:
                            double sum = Convert.ToDouble (value);
                            text = Translator.GetString ("Amount:");
                            textValue = Currency.ToString (sum, PriceType.Unknown);
                            total += sum;
                            break;

                        case DataField.OperationType:
                            text = Translator.GetString ("Type:");
                            textValue = Translator.GetOperationTypeName ((OperationType) value);
                            break;

                        case DataField.PaymentTypesName:
                            text = Translator.GetString ("Type:");
                            break;

                        case DataField.PartnerName:
                            text = Translator.GetString ("Partner:");
                            break;

                        default:
                            continue;
                    }
                    if (string.IsNullOrEmpty (textValue)) {
                        textValue = value.ToString ();
                        int index;
                        if (string.IsNullOrEmpty (textValue) && dataFieldsIndices.TryGetValue (dataFieldIndex.Key, out index))
                            textValue = (row [index] ?? string.Empty).ToString ();
                    }
                    ret.Add (new KeyValuePair<string, string> (text, textValue));
                }
                ret.Add (new KeyValuePair<string, string> (DriverBase.SEPARATOR, DriverBase.SEPARATOR));
            }
            if (total > 0)
                ret.Add (new KeyValuePair<string, string> (Translator.GetString ("Total:"), Currency.ToString (total, PriceType.Unknown)));

            return ret.ToArray ();
        }

        public static string GetReportLine (string key, string value, int charsPerLine)
        {
            if (key == DriverBase.SEPARATOR)
                return DriverBase.GetAlignedString (DriverBase.SEPARATOR, charsPerLine, DriverBase.TextAlign.Left);

            int valueMaxLen = charsPerLine - key.Length;
            value = value.Length > valueMaxLen ? value.Substring (0, valueMaxLen) : value.PadLeft (valueMaxLen);

            return key + value;
        }

        public string Display (IEnumerable<KeyValuePair<string, string>> reportLines)
        {
            StringBuilder sb = new StringBuilder ();
            foreach (KeyValuePair<string, string> pair in reportLines) {
                if (sb.Length > 0)
                    sb.AppendLine ();

                sb.Append (GetReportLine (pair.Key, pair.Value, 50));
            }
            return sb.ToString ();
        }

        protected DataQuery GetDataQuery ()
        {
            DataQuery dataQuery = ReportProvider.CreateDataQuery ();
            dataQuery.Filters.AddRange (filters);

            string reportClass = GetType ().FullName;
            BusinessDomain.ReportQueryStates [reportClass] = dataQuery.Clone ();

            if (!AllowOperatorsManage ()) {
                // Filter for the current user if the user cannot manage operators
                DataFilter filter = new DataFilter (DataFilterLogic.ExactMatch, UserField)
                    {
                        IsValid = true,
                        Values = new object [] { BusinessDomain.LoggedUser.Id },
                        ShowColumns = true
                    };

                dataQuery.Filters.Add (filter);
            }

            dataQuery.OrderBy = DataField.OperationTimeStamp;

            return dataQuery;
        }
    }
}
