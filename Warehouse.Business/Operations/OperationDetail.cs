//
// OperationDetail.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   07/01/2006
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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Warehouse.Business.Documenting;
using Warehouse.Business.Entities;
using Warehouse.Data;

namespace Warehouse.Business.Operations
{
    public class OperationDetail : INotifyPropertyChanged, ICloneable
    {
        private static readonly string [] defPopulateSkipFields = { "sign", "lotId" };

        #region Private members

        protected long detailId = -1;
        protected long itemId = -1;
        protected long itemGroupId = -1;
        protected string itemCode;
        protected string itemName = string.Empty;
        protected string itemName2 = string.Empty;
        protected double? originalQuantity;
        protected double maxQuantity;
        protected double quantity;
        protected int sign;
        protected long referenceDocumentId;
        protected double priceIn;
        protected double priceOut;
        protected double vatIn;
        protected double vatOut;
        protected double? originalDiscount;
        protected double discount;
        protected double discountValue;
        protected long currencyId = 1;
        protected double currencyRate = 1;
        protected string originalNote;
        protected string note = string.Empty;
        protected string lot = "NA";
        protected Lot lotObject = null;
        protected long lotId = 1;
        protected string serialNumber = string.Empty;
        protected DateTime? expirationDate;
        protected DateTime? productionDate;
        protected string lotLocation = string.Empty;

        protected string mUnitName = string.Empty;
        protected double total;
        protected double? vatRate;
        protected VATGroup vatGroup;
        protected FiscalPrinterTaxGroup vatCodeValue = FiscalPrinterTaxGroup.NotSet;
        protected double originalPriceOut = -1;
        protected double originalPriceIn = -1;
        private ItemType? itemType;
        protected bool isVATExempt;
        protected bool isDirty;

        #endregion

        #region Public properties

        [DbColumn (DataField.OperationDetailId)]
        public long DetailId
        {
            get { return detailId; }
            set
            {
                if (detailId == value)
                    return;

                detailId = value; OnPropertyChanged ("DetailId");
            }
        }

        [DbColumn (DataField.OperationDetailItemId)]
        public long ItemId
        {
            get { return itemId; }
            set
            {
                if (itemId == value)
                    return;

                vatGroup = null;
                itemId = value;
                OnPropertyChanged ("ItemId");
            }
        }

        [DbColumn (DataField.ItemGroupId)]
        public long ItemGroupId
        {
            get { return itemGroupId; }
            set
            {
                if (itemGroupId == value)
                    return;

                itemGroupId = value; OnPropertyChanged ("ItemGroupId");
            }
        }

        [DbColumn (DataField.ItemCode)]
        [FormMemberMapping ("GoodsCode")]
        public string ItemCode
        {
            get
            {
                if (itemCode == null) {
                    if (itemId >= 0) {
                        Item g = Item.Cache.GetById (itemId);
                        itemCode = g != null ? g.Code : string.Empty;
                    } else
                        return string.Empty;
                }

                return itemCode;
            }
            set
            {
                if (itemCode == value)
                    return;

                itemCode = value;
                OnPropertyChanged ("ItemCode");
            }
        }

        [DbColumn (DataField.ItemName)]
        [FormMemberMapping ("GoodsName")]
        public string ItemName
        {
            get { return itemName; }
            set
            {
                if (itemName == value)
                    return;

                itemName = value; OnPropertyChanged ("ItemName");
            }
        }

        [DbColumn (DataField.ItemName2)]
        [FormMemberMapping ("GoodsName2")]
        public string ItemName2
        {
            get { return string.IsNullOrWhiteSpace (itemName2) ? itemName : itemName2; }
            set
            {
                if (itemName2 == value)
                    return;

                itemName2 = value; OnPropertyChanged ("ItemName2");
            }
        }

        [UsedImplicitly]
        public string ItemBarcode
        {
            get { return Item == null ? string.Empty : Item.BarCode; }
        }

        public double OriginalQuantity
        {
            get { return originalQuantity ?? 0; }
            set
            {
                if (originalQuantity == value)
                    return;

                originalQuantity = value; OnPropertyChanged ("OriginalQuantity");
            }
        }

        public double MaxQuantity
        {
            get { return maxQuantity; }
            set
            {
                if (maxQuantity.IsEqualTo (value))
                    return;

                maxQuantity = value; OnPropertyChanged ("MaxQuantity");
            }
        }

        [DbColumn (DataField.OperationDetailQuantity)]
        public double Quantity
        {
            get { return quantity; }
            set
            {
                if (quantity.IsEqualTo (value))
                    return;

                quantity = value;
                MaxQuantity = Math.Max (MaxQuantity, value);
                double originalPriceInBackup = originalPriceIn;
                double originalPriceOutBackup = originalPriceOut;

                TotalEvaluate ();

                originalPriceIn = originalPriceInBackup;
                originalPriceOut = originalPriceOutBackup;

                OnPropertyChanged ("Quantity");
                if (originalQuantity.HasValue)
                    return;

                originalQuantity = value;
                OnPropertyChanged ("OriginalQuantity");
            }
        }

        public bool InsufficientQuantity { get; set; }

        [JsonIgnore]
        [DbColumn (DataField.OperationDetailSign)]
        public int Sign
        {
            get { return sign; }
            set
            {
                if (sign == value)
                    return;

                sign = value; OnPropertyChanged ("Sign");
            }
        }

        [DbColumn (DataField.OperationDetailReferenceId)]
        public long ReferenceDocumentId
        {
            get { return referenceDocumentId; }
            set
            {
                if (referenceDocumentId == value)
                    return;

                referenceDocumentId = value;
                OnPropertyChanged ("ReferenceDocumentId");
            }
        }

        protected DateTime timeStamp;
        [DbColumn (DataField.OperationTimeStamp)]
        public DateTime TimeStamp
        {
            get { return timeStamp; }
            set
            {
                if (timeStamp == value)
                    return;

                timeStamp = value;
                OnPropertyChanged ("TimeStamp");
            }
        }

        [JsonIgnore]
        public virtual double OriginalPricePlusVAT
        {
            get { throw new NotImplementedException (); }
        }

        [JsonIgnore]
        public virtual double OriginalPrice
        {
            get { return -1; }
        }

        [JsonIgnore]
        protected virtual double Price
        {
            get { throw new NotImplementedException (); }
        }

        public double PriceIn
        {
            get { return priceIn; }
            set
            {
                if (priceIn.IsEqualTo (value))
                    return;

                priceIn = value; OnPropertyChanged ("PriceIn");
            }
        }

        [DbColumn (DataField.OperationDetailPriceIn)]
        public double PriceInDB
        {
            get { return priceIn; }
            set
            {
                if (priceIn.IsEqualTo (value))
                    return;

                priceIn = value;
                OnPropertyChanged ("PriceIn");
            }
        }

        public virtual double PriceOut
        {
            get
            {
                return Currency.Round (priceOut);
            }
            set
            {
                if (priceOut.IsEqualTo (value))
                    return;

                priceOut = value;
                OnPropertyChanged ("PriceOut");
                OnPropertyChanged ("PriceOutPlusVAT");
            }
        }

        [DbColumn (DataField.OperationDetailPriceOut)]
        public virtual double PriceOutDB
        {
            get { return priceOut; }
            set
            {
                if (priceOut.IsEqualTo (value))
                    return;

                priceOut = value;
                OnPropertyChanged ("PriceOut");
            }
        }

        [DbColumn (DataField.OperationDetailStorePriceIn)]
        public double StorePriceInDB
        {
            get
            {
                return BusinessDomain.AppConfiguration.VATIncluded && isVATExempt ? AddVAT (priceIn) : priceIn;
            }
        }

        [DbColumn (DataField.OperationDetailStorePriceOut)]
        public double StorePriceOutDB
        {
            get
            {
                return BusinessDomain.AppConfiguration.VATIncluded && isVATExempt ? AddVAT (priceOut) : priceOut;
            }
        }

        [JsonIgnore]
        public virtual double VAT
        {
            get { throw new NotImplementedException (); }
        }

        [DbColumn (DataField.OperationDetailVatIn)]
        public double VATIn
        {
            get { return isVATExempt ? 0 : vatIn; }
            set
            {
                if (vatIn.IsEqualTo (value))
                    return;

                vatIn = value; OnPropertyChanged ("VATIn");
            }
        }

        [DbColumn (DataField.OperationDetailVatOut)]
        public double VATOut
        {
            get { return isVATExempt ? 0 : vatOut; }
            set
            {
                if (vatOut.IsEqualTo (value))
                    return;

                vatOut = value; OnPropertyChanged ("VATOut");
            }
        }

        public double OriginalDiscount
        {
            get { return originalDiscount ?? 0; }
            set
            {
                if (originalDiscount == value)
                    return;

                originalDiscount = value;
            }
        }

        [DbColumn (DataField.OperationDetailDiscount)]
        public virtual double Discount
        {
            get { return Percent.Round (discount); }
            set
            {
                if (discount.IsEqualTo (value))
                    return;

                discount = value;
                if (!originalDiscount.HasValue)
                    originalDiscount = value;
                OnPropertyChanged ("Discount");
            }
        }

        public string DiscountString
        {
            get { return Percent.ToString (Discount); }
        }

        public double DiscountValue
        {
            get { return Currency.Round (discountValue, TotalsPriceType); }
            set
            {
                if (discountValue.IsEqualTo (value))
                    return;

                discountValue = value; OnPropertyChanged ("DiscountValue");
            }
        }

        public string DiscountValueString
        {
            get { return Currency.ToString (DiscountValue); }
        }

        [DbColumn (DataField.OperationDetailCurrencyId)]
        public long CurrencyId
        {
            get { return currencyId; }
            set
            {
                if (currencyId == value)
                    return;

                currencyId = value; OnPropertyChanged ("CurrencyId");
            }
        }

        [DbColumn (DataField.OperationDetailCurrencyRate)]
        public double CurrencyRate
        {
            get { return currencyRate; }
            set
            {
                if (currencyRate == value)
                    return;

                currencyRate = value; OnPropertyChanged ("CurrencyRate");
            }
        }

        public string OriginalNote
        {
            get { return originalNote; }
            set
            {
                if (originalNote == value)
                    return;

                originalNote = value;
            }
        }

        [DbColumn (DataField.OperationDetailNote, 255)]
        public string Note
        {
            get { return note; }
            set
            {
                if (originalNote == null)
                    originalNote = value;

                if (note == value)
                    return;

                note = value;
                OnPropertyChanged ("Note");
            }
        }

        [DbColumn (DataField.ItemMeasUnit)]
        public string MUnitName
        {
            get { return mUnitName; }
            set
            {
                if (mUnitName == value)
                    return;

                mUnitName = value; OnPropertyChanged ("MUnitName");
            }
        }

        [DbColumn (DataField.OperationDetailTotal)]
        public double Total
        {
            get { return Currency.Round (total, TotalsPriceType); }
            set
            {
                if (total.IsEqualTo (value))
                    return;

                total = value;
                OnPropertyChanged ("Total");
                OnPropertyChanged ("TotalPlusVAT");
            }
        }

        [DbColumn (DataField.OperationDetailLot, 50)]
        public string Lot
        {
            get { return lot; }
            set
            {
                if (lot == value)
                    return;

                lot = value; OnPropertyChanged ("Lot");
            }
        }

        [DbColumn (DataField.OperationDetailLotId)]
        public long LotId
        {
            get { return lotId; }
            set
            {
                if (lotId == value)
                    return;

                lotId = value; OnPropertyChanged ("LotId");
            }
        }

        [DbColumn (DataField.LotSerialNumber)]
        public string SerialNumber
        {
            get { return serialNumber; }
            set
            {
                if (serialNumber == value)
                    return;

                serialNumber = value; OnPropertyChanged ("SerialNumber");
            }
        }

        [DbColumn (DataField.LotExpirationDate)]
        public DateTime? ExpirationDate
        {
            get { return expirationDate; }
            set
            {
                if (expirationDate == value)
                    return;

                expirationDate = value; OnPropertyChanged ("ExpirationDate");
            }
        }

        [DbColumn (DataField.LotProductionDate)]
        public DateTime? ProductionDate
        {
            get { return productionDate; }
            set
            {
                if (productionDate == value)
                    return;

                productionDate = value; OnPropertyChanged ("ProductionDate");
            }
        }

        [DbColumn (DataField.LotLocation)]
        public string LotLocation
        {
            get { return lotLocation; }
            set
            {
                if (lotLocation == value)
                    return;

                lotLocation = value; OnPropertyChanged ("LotLocation");
            }
        }

        [DbColumn (DataField.OperationLocationId, true)]
        public int LocationId { get; set; }

        public double VatRate
        {
            get
            {
                VatRateEvaluate ();

                return vatRate ?? 0;
            }
            set
            {
                if (vatRate != null && vatRate.Value.IsEqualTo (value))
                    return;

                vatRate = value; OnPropertyChanged ("VatRate");
            }
        }

        public bool ManualSalePrice { get; set; }
        public bool ManualPurchasePrice { get; set; }
        public bool ManualDiscount { get; set; }

        protected void VatRateEvaluate ()
        {
            if (vatRate != null)
                return;

            if (vatOut.IsZero () && priceOut > 0 && detailId > 0)
                vatRate = 0;
            else if (vatGroup != null)
                vatRate = vatGroup.VatValue;
            else if (priceOut > 0 && priceOut >= priceIn) // Prefer the higher price to calculated the rate more accuratelly
                vatRate = CalculateVatRate (vatOut, priceOut);
            else if (priceIn > 0)
                vatRate = CalculateVatRate (vatIn, priceIn);
            else if (VatGroup != null) // In the end load the vat group from DB
                vatRate = VatGroup.VatValue;
        }

        private double CalculateVatRate (double vat, double price)
        {
            if (BusinessDomain.AppConfiguration.VATIncluded)
                return vat * 100 / (price - vat);

            return vat * 100 / price;
        }

        [JsonIgnore]
        public VATGroup VatGroup
        {
            set
            {
                if (vatGroup == null || value == null || vatGroup.Id != value.Id)
                    vatRate = null;

                vatGroup = value;
                vatCodeValue = FiscalPrinterTaxGroup.NotSet;
            }
            get
            {
                if (vatGroup == null && itemId >= 0) {
                    Item g = Item ?? Item.Cache.GetById (itemId);
                    if (g == null)
                        return null;

                    vatGroup = g.GetVATGroup ();
                }

                return vatGroup;
            }
        }

        [JsonIgnore]
        public FiscalPrinterTaxGroup VatCodeValue
        {
            get
            {
                if (vatCodeValue == FiscalPrinterTaxGroup.NotSet && VatGroup != null)
                    vatCodeValue = VatGroup.CodeValue;

                return vatCodeValue;
            }
        }

        public double OriginalPriceIn
        {
            get
            {
                if (originalPriceIn < 0 && !Discount.IsEqualTo (100)) {
                    originalPriceIn = priceIn * 100 / (100 - Discount);
                }

                return Currency.Round (originalPriceIn, PriceType.Purchase);
            }
            set
            {
                if (originalPriceIn.IsEqualTo (value))
                    return;

                originalPriceIn = value; OnPropertyChanged ("OriginalPriceIn");
            }
        }

        public double OriginalPriceInPlusVAT
        {
            get { return GetWithVAT (OriginalPriceIn); }
        }

        [JsonIgnore]
        public double PriceInWithoutVAT
        {
            get { return Currency.Round (GetWithoutVAT (priceIn)); }
        }

        public double OriginalPriceOut
        {
            get
            {
                if (originalPriceOut < 0 && !discount.IsEqualTo (100)) {
                    originalPriceOut = priceOut * 100 / (100 - Discount);
                }

                return Currency.Round (originalPriceOut);
            }
            set
            {
                if (originalPriceOut.IsEqualTo (value))
                    return;

                originalPriceOut = value;
                OnPropertyChanged ("OriginalPriceOut");
                OnPropertyChanged ("OriginalPriceOutPlusVAT");
            }
        }

        private double? originalPriceOutPlusVAT;
        public double OriginalPriceOutPlusVAT
        {
            get { return originalPriceOutPlusVAT ?? Currency.Round (GetWithVAT (OriginalPriceOut)); }
        }

        [JsonIgnore]
        public double PriceOutPlusVAT
        {
            get { return Currency.Round (GetWithVAT (priceOut)); }
        }

        [JsonIgnore]
        public double PriceOutWithoutVAT
        {
            get { return Currency.Round (GetWithoutVAT (priceOut)); }
        }

        [JsonIgnore]
        public virtual double OriginalTotal
        {
            get { return Currency.Round (Quantity * OriginalPriceIn, TotalsPriceType); }
        }

        [JsonIgnore]
        public double OriginalTotalPlusVAT
        {
            get { return Currency.Round (GetWithVAT (OriginalTotal), TotalsPriceType); }
        }

        [JsonIgnore]
        public virtual double TotalPlusVAT
        {
            get { return Currency.Round (GetWithVAT (Total), TotalsPriceType); }
        }

        [JsonIgnore]
        public virtual double TotalWithoutVAT
        {
            get { return Currency.Round (GetWithoutVAT (Total, TotalsPriceType), TotalsPriceType); }
        }

        [JsonIgnore]
        public double TotalVAT
        {
            get { return isVATExempt ? 0 : Currency.Round (VAT * quantity, TotalsPriceType); }
        }

        public ItemType? ItemType
        {
            get
            {
                if (itemType == null && itemId >= 0)
                    itemType = Item.GetItemType (itemId);

                return itemType;
            }
        }

        public double QuantityIncrement
        {
            get
            {
                return (itemType & (Data.ItemType.Discount | Data.ItemType.DistributedCharge)) != Data.ItemType.Standard ?
                    -1 : 1;
            }
        }

        public bool IsVATExempt
        {
            get { return isVATExempt; }
            set { isVATExempt = value; }
        }

        public bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }

        public long FinalProductId { get; set; }

        [JsonIgnore]
        public virtual bool UsesSavedLots
        {
            get { return false; }
        }

        [JsonIgnore]
        public virtual PriceType TotalsPriceType
        {
            get { return PriceType.Sale; }
        }

        protected bool baseTotalOnPricePlusVAT;
        public bool BaseTotalOnPricePlusVAT
        {
            set { baseTotalOnPricePlusVAT = value; }
        }

        private bool baseDiscountOnPricePlusVAT;

        public bool BaseDiscountOnPricePlusVAT
        {
            set { baseDiscountOnPricePlusVAT = value; }
            get { return baseDiscountOnPricePlusVAT; }
        }

        [JsonIgnore]
        public Item Item { get; set; }

        public PriceRule.AppliedActions AppliedPriceRules { get; set; }

        public int PromotionForDetailHashCode { get; set; }

        #endregion

        public OperationDetail ()
        {
            baseTotalOnPricePlusVAT = BusinessDomain.AppConfiguration.RoundedPrices;
        }

        #region INotifyPropertyChanged Members

        protected void OnPropertyChanged (string property)
        {
            isDirty = true;
            if (PropertyChanged != null)
                PropertyChanged (this, new PropertyChangedEventArgs (property));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region ICloneable Members

        public virtual object Clone ()
        {
            return MemberwiseClone ();
        }

        public K Clone<K> (bool useAbsQtty = false) where K : OperationDetail, new ()
        {
            K ret = new K ();
            ret.Populate (this);

            if (useAbsQtty && ret.Quantity < 0)
                ret.Quantity = -ret.Quantity;

            return ret;
        }

        protected virtual void Populate (OperationDetail detail, params string [] skipFields)
        {
            if (detail == null)
                return;

            List<string> skip = new List<string> (defPopulateSkipFields);
            skip.AddRange (skipFields);

            foreach (FieldInfo fieldInfo in typeof (OperationDetail).GetFields (BindingFlags.NonPublic | BindingFlags.Instance)) {
                if (!fieldInfo.FieldType.IsValueType && fieldInfo.FieldType != typeof (string))
                    continue;

                if (skip.Contains (fieldInfo.Name))
                    continue;

                object val = fieldInfo.GetValue (detail);
                fieldInfo.SetValue (this, val);
            }

            Item = detail.Item;

            var afterHandler = AfterItemEvaluate;
            if (afterHandler != null)
                afterHandler (this, new AfterItemEvaluateArgs { Item = Item });
        }

        #endregion

        #region VAT Calculations

        public double GetWithVAT (double value)
        {
            return BusinessDomain.AppConfiguration.VATIncluded || isVATExempt ? value : AddVAT (value);
        }

        public double GetWithoutVAT (double value, PriceType priceType = PriceType.Sale)
        {
            if (BusinessDomain.AppConfiguration.VATIncluded) {
                double vatValue = Currency.Round ((value * VatRate) / (100 + VatRate), priceType);

                value -= vatValue;
            }

            return value;
        }

        public double SetWithVAT (double value)
        {
            return BusinessDomain.AppConfiguration.VATIncluded ? value : SubtractVAT (value);
        }

        public double SetWithoutVAT (double value)
        {
            return BusinessDomain.AppConfiguration.VATIncluded ? AddVAT (value) : value;
        }

        public double GetVAT (double value)
        {
            if (BusinessDomain.AppConfiguration.VATIncluded)
                return (value * VatRate) / (100 + VatRate);

            return value * VatRate / 100;
        }

        private double AddVAT (double value)
        {
            return value * (100 + VatRate) / 100;
        }

        private double SubtractVAT (double value)
        {
            return (value * 100) / (100 + VatRate);
        }

        public void SetVATExempt (bool value)
        {
            if (isVATExempt == value)
                return;

            isVATExempt = value;
            if (BusinessDomain.AppConfiguration.VATIncluded) {
                double newPriceIn = value ? SubtractVAT (priceIn) : AddVAT (priceIn);
                OriginalPriceInEvaluate (newPriceIn);

                double newPriceOut = value ? SubtractVAT (priceOut) : AddVAT (priceOut);
                OriginalPriceOutEvaluate (newPriceOut);
            } else {
                OnPropertyChanged ("OriginalPriceOutPlusVAT");
            }

            VATEvaluate ();
            TotalEvaluate ();
        }

        public void AddVAT ()
        {
            OriginalPriceEvaluate (AddVAT (OriginalPrice));

            VATEvaluate ();
            TotalEvaluate ();
        }

        public void SubtractVAT ()
        {
            OriginalPriceEvaluate (SubtractVAT (OriginalPrice));

            VATEvaluate ();
            TotalEvaluate ();
        }

        #endregion

        public void SetOriginalPriceOutPlusVAT (double value)
        {
            // don't use a property setter because a transaction rollback may assign an invalid default value of 0 as the property type is double and not double?
            originalPriceOutPlusVAT = value;
        }

        public static event EventHandler<AfterItemEvaluateArgs> AfterItemEvaluate;

        public static event EventHandler<BeforeItemEvaluateArgs> BeforeItemEvaluate;
        public bool CheckItemCanEvaluate (Item newItem)
        {
            if (newItem == null)
                return false;

            var beforeHandler = BeforeItemEvaluate;
            if (beforeHandler != null) {
                BeforeItemEvaluateArgs args = new BeforeItemEvaluateArgs { Item = newItem };
                beforeHandler (this, args);
                if (args.Cancelled)
                    return false;
            }

            return true;
        }

        public bool ItemEvaluate (string name, PriceGroup priceGroup)
        {
            double qtty;
            bool barCodeUsed;
            string codeLot;

            long i;
            Item item = Item.GetByAny (name, out barCodeUsed, out qtty, out codeLot, out i);
            if (!ItemEvaluate (item, priceGroup))
                return false;

            Quantity = qtty.IsZero () ? 1 : qtty;
            return true;
        }

        public bool ItemEvaluate (Item newItem, PriceGroup priceGroup, bool updatePrice = false)
        {
            if (!CheckItemCanEvaluate (newItem))
                return false;

            Item oldItem = itemId >= 0 ? Item.GetById (itemId) : null;
            if (ItemName != newItem.Name) {
                ItemCode = newItem.Code;
                ItemName = newItem.Name;
                ItemName2 = newItem.Name2;
            }

            Item = newItem;
            if (oldItem == null || newItem.Id != oldItem.Id) {
                ItemId = newItem.Id;
                ItemGroupId = newItem.GroupId;
                VatGroup = newItem.GetVATGroup ();
                itemType = newItem.Type;

                if (string.IsNullOrEmpty (newItem.MUnit)) {
                    MesUnit [] units = MesUnit.GetByItem (newItem);
                    MUnitName = units == null ? Translator.GetString ("pcs.") : units [0].Name;
                } else
                    MUnitName = newItem.MUnit;

                if (oldItem == null)
                    ResetQuantity ();
            }

            if (oldItem == null || newItem.Id != oldItem.Id || updatePrice) {
                UpdatePrices (newItem, priceGroup);
                VATEvaluate ();
                TotalEvaluate ();
            }

            var afterHandler = AfterItemEvaluate;
            if (afterHandler != null)
                afterHandler (this, new AfterItemEvaluateArgs { Item = newItem });

            return true;
        }

        protected virtual void ResetQuantity ()
        {
            OriginalQuantity = 0;
            Quantity = 1;
        }

        private void UpdatePrices (Item item, PriceGroup priceGroup)
        {
            OriginalPriceInEvaluate (GetItemPriceIn (item));
            OriginalPriceOutEvaluate (GetItemPriceOut (item, priceGroup));
            TotalEvaluate ();
        }

        public void UpdatePrices (PriceGroup oldPriceGroup, PriceGroup newPriceGroup)
        {
            Item item = Item.GetById (ItemId);
            if (item == null)
                return;

            if (item.GetPriceGroupPrice (oldPriceGroup).IsEqualTo (OriginalPriceOut))
                OriginalPriceOutEvaluate (GetItemPriceOut (item, newPriceGroup));

            TotalEvaluate ();
        }

        public double GetItemPriceIn (Item item)
        {
            return isVATExempt ?
                GetWithoutVAT (item.TradeInPrice, PriceType.Purchase) :
                item.TradeInPrice;
        }

        private double GetItemPriceOut (Item item, PriceGroup priceGroup)
        {
            return isVATExempt ?
                GetWithoutVAT (item.GetPriceGroupPrice (priceGroup)) :
                item.GetPriceGroupPrice (priceGroup);
        }

        public virtual void DiscountEvaluate (double discountPercent)
        {
        }

        public virtual void DiscountValueEvaluate (double value)
        {
        }

        public virtual void OriginalPriceEvaluate (double value)
        {
        }

        public virtual void PriceEvaluate ()
        {
        }

        public virtual void OriginalPriceInEvaluate (double value, Operation operation = null)
        {
            var oldValue = OriginalPriceIn;

            OriginalPriceIn = value;
            PriceInEvaluate ();

            OnAfterPriceInEvaluate (operation, oldValue);
        }

        public static event EventHandler<AfterPriceInEvaluateArgs> AfterPriceInEvaluate;
        protected void OnAfterPriceInEvaluate (Operation operation, double oldValue)
        {
            var afterHandler = AfterPriceInEvaluate;
            if (afterHandler != null)
                afterHandler (this, new AfterPriceInEvaluateArgs { Operation = operation, OldValue = oldValue });
        }

        public virtual void PriceInEvaluate ()
        {
            VatRateEvaluate ();
            PriceIn = OriginalPriceIn;
            VATEvaluate ();
        }

        public virtual void OriginalPriceOutEvaluate (double value)
        {
            OriginalPriceOut = value;
            PriceOutEvaluate ();
        }

        public virtual void PriceOutEvaluate ()
        {
            VatRateEvaluate ();
            PriceOut = OriginalPriceOut;
            VATEvaluate ();
        }

        public virtual void VATEvaluate ()
        {
            double vat = 0;
            if (priceIn > 0 && !BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT && !isVATExempt)
                vat = CalculateVAT (priceIn, PriceType.Purchase);

            VATIn = vat;

            vat = 0;
            if (priceOut > 0 && !isVATExempt)
                vat = CalculateVAT (priceOut, PriceType.Sale);

            VATOut = vat;
        }

        private double CalculateVAT (double price, PriceType type)
        {
            if (BusinessDomain.AppConfiguration.RoundedPrices && !BusinessDomain.AppConfiguration.VATIncluded)
                price = Currency.Round (price, type);

            return BusinessDomain.AppConfiguration.VATIncluded ?
                (price * VatRate) / (100d + VatRate) :
                (price * VatRate) / 100d;
        }

        public void LotEvaluate (Lot l)
        {
            lotObject = l;
            if (l != null) {
                lotId = l.ID;
                lot = l.Name;
                serialNumber = l.SerialNumber;
                productionDate = l.ProductionDate;
                expirationDate = l.ExpirationDate;
                lotLocation = l.Location;
                OriginalPriceInEvaluate (l.PriceIn);
            } else {
                lotId = -1;
                lot = "NA";
                serialNumber = null;
                productionDate = null;
                expirationDate = null;
                lotLocation = null;
            }
        }

        public Lot GetLot ()
        {
            return new Lot
                {
                    PriceIn = priceIn,
                    ID = lotId,
                    Name = lot,
                    SerialNumber = serialNumber,
                    ProductionDate = productionDate,
                    ExpirationDate = expirationDate,
                    Location = lotLocation
                };
        }

        public void CheckForInsufficiency (Operation operation, long locationId)
        {
            if (!BusinessDomain.AppConfiguration.AllowNegativeAvailability && !BusinessDomain.AppConfiguration.AutoProduction &&
                sign != 0 && (sign < 0 || detailId > 0) && itemId > 0)
                InsufficientQuantity = Item.GetAvailability (itemId, locationId) +
                    operation.DetailsBase.Where (d => d.ItemId == itemId).Sum (d => sign * (d.Quantity - d.OriginalQuantity)) < 0;
        }

        public void TotalEvaluate ()
        {
            double originalTotal = Quantity * OriginalPrice;

            Total = originalTotal - Currency.Round (originalTotal * Discount / 100, TotalsPriceType);
        }

        protected void CalculateDiscount (double discountPercent)
        {
            discountPercent = Math.Min (discountPercent, 99);
            discountPercent = Math.Max (discountPercent, -99);
            discountPercent = Percent.Round (discountPercent);
            Discount = discountPercent;

            CalculateDiscountValue ();
            PriceEvaluate ();
            TotalEvaluate ();
        }

        public void CalculateDiscountValue ()
        {
            double originalTotal = baseDiscountOnPricePlusVAT ?
                Currency.Round (GetWithVAT (OriginalPrice), TotalsPriceType) * quantity :
                Currency.Round (OriginalPrice * quantity, TotalsPriceType);

            DiscountValue = originalTotal * discount / 100;
        }

        protected void CalculateValueDiscount (double value)
        {
            double price = (baseDiscountOnPricePlusVAT ? GetWithVAT (OriginalPrice) : OriginalPrice) * quantity;
            double discountPercent = price.IsZero () ? 0 : (value * 100) / price;

            DiscountEvaluate (discountPercent);
        }

        public bool ApplyPriceRules<T> (IEnumerable<PriceRule> priceRules, Operation<T> operation,
            PriceGroup partnerPriceGroup, PriceGroup locationPriceGroup) where T : OperationDetail, new ()
        {
            if (operation.PartnerId < 0 || operation.LocationId < 0 || itemId < 0)
                return false;

            if (PromotionForDetailHashCode != 0)
                return false;

            AppliedPriceRules = PriceRule.AppliedActions.None;
            if (!ManualDiscount)
                DiscountEvaluate (0);

            PriceGroup priceGroup = Operation.GetPriceGroup (partnerPriceGroup, locationPriceGroup);
            if (!ManualSalePrice && Item != null) {
                OriginalPriceOutEvaluate (GetItemPriceOut (Item, priceGroup));
                TotalEvaluate ();
            }

            if (!ManualPurchasePrice && Item != null) {
                OriginalPriceInEvaluate (lotObject == null ? GetItemPriceIn (Item) : lotObject.PriceIn, operation);
                TotalEvaluate ();
            }

            operation.ClearPromotionForDetail (this);
            return PriceRule.ApplyBeforeOperationSaved (operation, null, false, priceRules);
        }

        protected bool? usePriceIn;

        public void SetUsePriceIn (bool value)
        {
            usePriceIn = value;
        }

        public static event EventHandler<OperationDetailArgs> OnQuickSaleChangePriceRequest;
        public bool CanChangePriceFromQuickSale ()
        {
            var handler = OnQuickSaleChangePriceRequest;
            if (handler != null) {
                OperationDetailArgs args = new OperationDetailArgs ();
                handler (this, args);
                if (args.Cancelled)
                    return false;
            }

            return true;
        }

        public static event EventHandler<OperationDetailArgs> OnAutoProduceRequest;
        public bool CanAutoProduceItem ()
        {
            var handler = OnAutoProduceRequest;
            if (handler != null) {
                OperationDetailArgs args = new OperationDetailArgs ();
                handler (this, args);
                if (args.Cancelled)
                    return false;
            }

            return true;
        }

        public static event EventHandler<OperationDetailArgs> OnCheckPricesRequest;
        public bool CheckPurchaseSalePrices (double purchasePrice, double salePrice)
        {
            var handler = OnCheckPricesRequest;
            if (handler != null) {
                OperationDetailArgs args = new OperationDetailArgs ();
                handler (this, args);
                if (args.Result != null)
                    return args.Result.Value;
            }

            if (!BusinessDomain.AppConfiguration.WarnPricesSaleLowerThanPurchase || purchasePrice <= salePrice)
                return true;

            return false;
        }

        public static event EventHandler OnSignReset;
        public void ResetSign (int value)
        {
            Sign = value;

            EventHandler handler = OnSignReset;
            if (handler == null)
                return;

            handler (this, null);
        }

        public static event EventHandler<OperationDetailArgs> OnValidateQuantity;
        public bool ValidateQuantity ()
        {
            var handler = OnValidateQuantity;
            if (handler != null) {
                OperationDetailArgs args = new OperationDetailArgs ();
                handler (this, args);
                if (args.Result != null)
                    return args.Result.Value;
            }

            return ValidateQuantityValue ();
        }

        protected virtual bool ValidateQuantityValue ()
        {
            return (detailId > 0 || (quantity > 0)) && (detailId < 0 || (quantity >= 0));
        }
    }
}
