//
// FilterBase.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   09.30.2010
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
using Warehouse.Data.Calculator;

namespace Warehouse.Business.Reporting
{
    public abstract class FilterBase
    {
        private readonly bool constantVisibility;
        private readonly bool columnInitiallyVisible;
        private readonly DataFilterLabel displayedField = DataFilterLabel.NotSet;
        private readonly DbField [] filteredFields;
        protected bool isFrozen;
        private bool columnVisible;
        protected DataFilterLogic filterLogic = DataFilterLogic.NotSet;
        private ConditionCombineLogic combineLogic;

        public bool ConstantVisibility
        {
            get { return constantVisibility; }
        }

        public bool ColumnVisible
        {
            get { return columnVisible; }
            set { columnVisible = value; }
        }

        public bool IsFrozen
        {
            get { return isFrozen; }
        }

        public DataFilterLabel DisplayedField
        {
            get { return displayedField; }
        }

        public DbField [] FilteredFields
        {
            get { return filteredFields; }
        }

        public DataFilterLogic FilterLogic
        {
            get { return filterLogic; }
            set { filterLogic = value; }
        }

        protected FilterBase (bool constantVisibility, bool columnVisible, DataFilterLabel filter, DbField [] fNames)
        {
            this.constantVisibility = constantVisibility;
            columnInitiallyVisible = columnVisible;
            this.columnVisible = columnVisible;
            displayedField = filter;
            filteredFields = fNames;
        }

        public bool ValidateField (string value, bool skipNulls)
        {
            return ValidateField (FilteredFields [0], value, skipNulls);
        }

        private static bool ValidateField (DbField field, string value, bool skipNulls)
        {
            if (skipNulls && string.IsNullOrWhiteSpace (value))
                return true;

            return GetFieldValue (field, value) != null;
        }

        protected object GetFieldValue (object value)
        {
            return GetFieldValue (FilteredFields [0], value);
        }

        private static object GetFieldValue (DbField field, object value)
        {
            DataType fieldType = ReportProvider.GetDataFieldType (field);
            return GetFieldValue (fieldType, value);
        }

        protected static object GetFieldValue (DataType fieldType, object value)
        {
            DateTime date;
            switch (fieldType) {
                case DataType.Date:
                    if (value is DateTime)
                        return value;
                    date = BusinessDomain.GetDateValue ((string) value);
                    if (date != DateTime.MinValue)
                        return date;
                    break;

                case DataType.DateTime:
                    if (value is DateTime)
                        return value;
                    date = BusinessDomain.GetDateTimeValue ((string) value);
                    if (date != DateTime.MinValue)
                        return date;
                    break;

                case DataType.Quantity:
                    if (IsNumeric (value))
                        return value;
                    double dbl;
                    if (Quantity.TryParseExpression ((string) value, out dbl))
                        return dbl;
                    break;

                case DataType.CurrencyIn:
                    if (IsNumeric (value))
                        return value;
                    if (Currency.TryParseExpression ((string) value, out dbl, PriceType.Purchase))
                        return dbl;
                    break;

                case DataType.CurrencyOut:
                    if (IsNumeric (value))
                        return value;
                    if (Currency.TryParseExpression ((string) value, out dbl))
                        return dbl;
                    break;

                case DataType.Currency:
                    if (IsNumeric (value))
                        return value;
                    if (Currency.TryParseExpression ((string) value, out dbl, PriceType.Unknown))
                        return dbl;
                    break;

                case DataType.Percent:
                    if (IsNumeric (value))
                        return value;
                    if (Percent.TryParseExpression ((string) value, out dbl))
                        return dbl;
                    break;

                case DataType.Id:
                case DataType.UserId:
                case DataType.DocumentNumber:
                case DataType.OperationType:
                case DataType.DocumentType:
                case DataType.PriceGroupType:
                case DataType.BasePaymentType:
                case DataType.PaymentType:
                case DataType.PartnerType:
                case DataType.ItemType:
                case DataType.UserAccessLevel:
                case DataType.TurnoverType:
                case DataType.TurnoverDirection:
                case DataType.Sign:
                case DataType.PaymentMode:
                case DataType.DateTimeInterval:
                    if (IsNumeric (value))
                        return value;
                    if (Number.TryParseExpression ((string) value, out dbl))
                        return dbl;
                    break;

                default:
                    return string.IsNullOrWhiteSpace ((string) value) ? null : value;
            }

            return null;
        }

        private static bool IsNumeric (object value)
        {
            if (value == null)
                return false;
            TypeCode typeCode = Type.GetTypeCode (value.GetType ());
            return TypeCode.Char <= typeCode && typeCode <= TypeCode.Decimal;
        }

        protected string GetFieldText (object value)
        {
            return GetFieldText (FilteredFields [0], value);
        }

        private static string GetFieldText (DbField field, object value)
        {
            if (value == null)
                return string.Empty;

            string ret = value as string;
            return ret ?? GetFieldText (ReportProvider.GetDataFieldType (field), value);
        }

        protected static string GetFieldText (DataType fieldType, object value)
        {
            switch (fieldType) {
                case DataType.Date:
                    return BusinessDomain.GetFormattedDate ((DateTime) value);

                case DataType.DateTime:
                    return BusinessDomain.GetFormattedDateTime ((DateTime) value);

                case DataType.Quantity:
                    return Quantity.ToEditString ((double) value);

                case DataType.CurrencyIn:
                    return Currency.ToEditString ((double) value, PriceType.Purchase);

                case DataType.CurrencyOut:
                    return Currency.ToEditString ((double) value);

                case DataType.Currency:
                    return Currency.ToEditString ((double) value, PriceType.Unknown);

                case DataType.Percent:
                    return Percent.ToEditString ((double) value);

                default:
                    return value.ToString ();
            }
        }

        protected bool TrySetFrozenFilter (out string text)
        {
            return TrySetDefaultPartner (out text) || TrySetDefaultLocation (out text);
        }

        private bool TrySetDefaultPartner (out string text)
        {
            if (BusinessDomain.LoggedUser.LockedPartnerId > 0)
                foreach (DbField dbField in FilteredFields)
                    if (dbField.StrongField == DataField.PartnerName) {
                        Partner partner = Partner.GetById (BusinessDomain.LoggedUser.LockedPartnerId);
                        if (partner != null) {
                            text = partner.Name;
                            return true;
                        }
                    }

            text = null;
            return false;
        }

        private bool TrySetDefaultLocation (out string text)
        {
            foreach (DbField dbField in FilteredFields)
                if (dbField.StrongField == DataField.LocationName ||
                    dbField.StrongField == DataField.SourceLocationName) {
                    Location location = null;
                    if (Location.TryGetLocked (ref location)) {
                        text = location.Name;
                        return true;
                    }
                }

            text = null;
            return false;
        }

        public virtual DataFilter GetDataFilter (params object [] values)
        {
            return new DataFilter (filterLogic, FilteredFields)
                {
                    ShowColumns = columnVisible,
                    CombineLogic = combineLogic,
                    Values = values.Select (GetFieldValue).ToArray ()
                };
        }

        public virtual void Clear ()
        {
            ColumnVisible = columnInitiallyVisible;
        }

        public virtual void SetDataFilter (DataFilter dataFilter)
        {
            columnVisible = dataFilter.ShowColumns;
            combineLogic = dataFilter.CombineLogic;
        }

        public abstract string GetExplanation ();
    }
}
