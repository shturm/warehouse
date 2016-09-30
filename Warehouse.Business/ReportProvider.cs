//
// ReportProvider.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   08/20/2006
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
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Data;

namespace Warehouse.Business
{
    public static class ReportProvider
    {
        public static DataType GetDataFieldType (DbField field)
        {
            if (field == null)
                throw new ArgumentNullException ("field");

            switch (field.StrongField) {
                case DataField.OperationDate:
                case DataField.DocumentDate:
                case DataField.DocumentReferenceDate:
                case DataField.DocumentTaxDate:
                case DataField.PaymentDate:
                case DataField.PaymentEndDate:
                case DataField.LotProductionDate:
                case DataField.LotExpirationDate:
                case DataField.CashEntryDate:
                    return DataType.Date;

                case DataField.AppLogTimeStamp:
                case DataField.OperationDateTime:
                case DataField.OperationTimeStamp:
                case DataField.CashEntryTimeStamp:
                case DataField.PaymentTimeStamp:
                case DataField.PartnerTimeStamp:
				case DataField.CompanyCreationTimeStamp:
                    return DataType.DateTime;

                case DataField.ItemMinQuantity:
                case DataField.ItemNomQuantity:
                case DataField.PurchaseQuantitySum:
                case DataField.SaleQuantitySum:
                case DataField.WasteQuantitySum:
                case DataField.StockTakingQuantitySum:
                case DataField.TransferInQuantitySum:
                case DataField.TransferOutQuantitySum:
                case DataField.WriteOffQuantitySum:
                case DataField.ConsignedQuantitySum:
                case DataField.ConsignedQuantitySoldSum:
                case DataField.ConsignedQuantityReturnedSum:
                case DataField.ProductionMatQuantitySum:
                case DataField.ProductionProdQuantitySum:
                case DataField.DebitNoteQuantitySum:
                case DataField.CreditNoteQuantitySum:
                case DataField.ReturnQuantitySum:
                case DataField.PurchaseReturnQuantitySum:
                case DataField.OperationQuantitySum:
                case DataField.OperationQuantitySignedSum:
                case DataField.OperationDetailAvailableQuantity:
                case DataField.OperationDetailQuantity:
                case DataField.OperationDetailDifference:
                case DataField.StoreQtty:
                case DataField.StoreAvailableQuantity:
                case DataField.RemainingQuantity:
                    return DataType.Quantity;

                case DataField.ItemPurchasePrice:
                case DataField.ItemTradeInVAT:
                case DataField.ItemTradeInSum:
                case DataField.OperationDetailPriceIn:
                case DataField.OperationDetailSumIn:
                case DataField.OperationDetailVatIn:
                case DataField.OperationDetailSumVatIn:
                case DataField.OperationDetailTotalIn:
                case DataField.StorePrice:
                case DataField.PurchaseSum:
                case DataField.PurchaseVATSum:
                case DataField.PurchaseTotal:
                    return DataType.CurrencyIn;

                //case DataField.ItemTradeOutPrice:
                case DataField.ItemTradePrice:
                case DataField.ItemTradeVAT:
                case DataField.ItemTradeSum:
                case DataField.ItemRegularPrice:
                case DataField.ItemPriceGroup1:
                case DataField.ItemPriceGroup2:
                case DataField.ItemPriceGroup3:
                case DataField.ItemPriceGroup4:
                case DataField.ItemPriceGroup5:
                case DataField.ItemPriceGroup6:
                case DataField.ItemPriceGroup7:
                case DataField.ItemPriceGroup8:
                case DataField.OperationTotal:
                case DataField.OperationSum:
                case DataField.SaleSum:
                case DataField.SaleVATSum:
                case DataField.SaleTotal:
                case DataField.StockTakingTotal:
                case DataField.StockTakingSum:
                case DataField.OperationDetailPriceOut:
                case DataField.OperationDetailSumOut:
                case DataField.OperationDetailVatOut:
                case DataField.OperationDetailSumVatOut:
                case DataField.OperationDetailTotalOut:
                    return DataType.CurrencyOut;

                case DataField.OperationVatSum:
                case DataField.OperationProfit:
                case DataField.OperationDetailDiscount:
                case DataField.OperationDetailSum:
                case DataField.OperationDetailVat:
                case DataField.OperationDetailSumVat:
                case DataField.OperationDetailTotal:
                case DataField.PartnerDebt:
                case DataField.PaymentAmount:
                case DataField.PaymentDueSum:
                case DataField.PaymentRemainingSum:
                case DataField.PaymentsInCash:
                case DataField.PaymentsByBankOrder:
                case DataField.PaymentsByDebitCreditCard:
                case DataField.PaymentsByVoucher:
                case DataField.ReturnSum:
                case DataField.CashEntryAmount:
                    return DataType.Currency;

                case DataField.VATGroupValue:
                case DataField.PartnerDiscount:
                    return DataType.Percent;

                case DataField.AppLogId:
                case DataField.CashEntryId:
                case DataField.ConfigEntryId:
                case DataField.DocumentId:
                case DataField.ItemId:
                case DataField.ItemGroupId:
                case DataField.LotId:
                case DataField.LocationId:
                case DataField.LocationsGroupsId:
                case DataField.OperationDetailId:
                case DataField.PartnerId:
                case DataField.PartnersGroupsId:
                case DataField.PaymentId:
                case DataField.PaymentTypesId:
                case DataField.UserId:
                case DataField.UsersGroupsId:
                case DataField.VATGroupId:
                    return DataType.Id;

                case DataField.OperationOperatorId:
                    return DataType.UserId;

                case DataField.DocumentPaymentTypeId:
                case DataField.PaymentTypeId:
                    return DataType.PaymentType;

                case DataField.OperationNumber:
                case DataField.DocumentNumber:
                case DataField.DocumentOperationNumber:
                case DataField.PaymentOperationId:
                case DataField.DocumentReferenceNumber:
                case DataField.OperationDetailReferenceId:
                    return DataType.DocumentNumber;

                case DataField.OperationType:
                case DataField.PaymentOperationType:
                case DataField.DocumentOperationType:
                    return DataType.OperationType;

                case DataField.PaymentTypesMethod:
                    return DataType.BasePaymentType;

                case DataField.PartnerPriceGroup:
                case DataField.LocationPriceGroup:
                    return DataType.PriceGroupType;

                case DataField.PartnerType:
                    return DataType.PartnerType;

                case DataField.ItemType:
                    return DataType.ItemType;

                case DataField.UserLevel:
                case DataField.OperationsUserLevel:
                case DataField.OperationsOperatorLevel:
                    return DataType.UserAccessLevel;

                case DataField.CashEntryTurnoverType:
                    return DataType.TurnoverType;

                case DataField.DocumentType:
                    return DataType.DocumentType;

                case DataField.CashEntryDirection:
                    return DataType.TurnoverDirection;

                case DataField.OperationDetailSign:
                case DataField.PaymentSign:
                    return DataType.Sign;

                case DataField.PaymentMode:
                    return DataType.PaymentMode;

                case DataField.VATGroupCode:
                    return DataType.TaxGroupCode;

                default:
                    return DataType.Text;
            }
        }

        public static string DataTypeToString (object obj, DataType type)
        {
            if (obj == null)
                throw new ArgumentNullException ("obj");

            switch (type) {
                case DataType.Date:
                case DataType.DateTime:
                    if (!(obj is DateTime))
                        throw new ArgumentException ("The passed object is not System.DateTime", "obj");

                    DateTime date = (DateTime) obj;
                    return type == DataType.DateTime
                        ? BusinessDomain.GetFormattedDateTime (date)
                        : BusinessDomain.GetFormattedDate (date);

                case DataType.Quantity:
                    if (obj is double)
                        return Quantity.ToString ((double) obj);

                    throw new ArgumentException ("The passed object is not System.Double", "obj");

                case DataType.CurrencyIn:
                    if (obj is double)
                        return Currency.ToString ((double) obj, PriceType.Purchase);

                    throw new ArgumentException ("The passed object is not System.Double", "obj");

                case DataType.CurrencyOut:
                    if (obj is double)
                        return Currency.ToString ((double) obj);

                    throw new ArgumentException ("The passed object is not System.Double", "obj");

                case DataType.Currency:
                    if (obj is double)
                        return Currency.ToString ((double) obj, PriceType.Unknown);

                    throw new ArgumentException ("The passed object is not System.Double", "obj");

                case DataType.Id:
                    if ((obj is long) || (obj is int))
                        return obj.ToString ();

                    throw new ArgumentException ("The passed object is not System.Long or System.Int", "obj");

                case DataType.DocumentNumber:
                    TypeCode typeCode = Type.GetTypeCode (obj.GetType ());
                    if (TypeCode.Char <= typeCode && typeCode <= TypeCode.UInt64)
                        return Operation.GetFormattedOperationNumber (Convert.ToInt64 (obj));

                    string value = obj as string;
                    if (value != null)
                        return value;

                    throw new ArgumentException ("The passed object is not a number or System.String", "obj");

                case DataType.Percent:
                    if (obj is double)
                        return Percent.ToString ((double) obj);

                    throw new ArgumentException ("The passed object is not System.Double", "obj");

                default:
                    return obj.ToString ();
            }
        }

        public static string GetReportFieldColumnName (DataQueryResult result, int columnIndex)
        {
            DbField field = result.Columns [columnIndex].Field;

            return result.Columns [columnIndex].IsTranslated ?
                result.Result.Columns [columnIndex] :
                Translator.GetReportFieldColumnName (field);
        }

        public static DataQuery CreateDataQuery ()
        {
            ConfigurationHolderBase config = BusinessDomain.AppConfiguration;
            return new DataQuery
                {
                    VATIsIncluded = config.VATIncluded,
                    UseLots = config.ItemsManagementUseLots,
                    RoundPrices = config.RoundedPrices,
                    CurrencyPrecission = config.CurrencyPrecision
                };
        }
    }
}
