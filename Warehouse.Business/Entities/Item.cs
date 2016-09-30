//
// Item.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/12/2006
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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Entities
{
    public class Item : ICacheableEntity<Item>, IPersistableEntity<Item>, INotifyPropertyChanged, IDisposable, IStrongEntity, IHirarchicalEntity
    {
        public enum ErrorCodes
        {
            NameEmpty,
            NameInUse,
            CodeInUse,
            BarcodeInUse,
            TooManyBarcodes,
            MeasUnitEmpty,
        }

        #region Private fields

        private long id = -1;
        private string name = string.Empty;
        private string name2 = string.Empty;
        private string code = string.Empty;
        private string barCode = string.Empty;
        private string barCode2 = string.Empty;
        private string barCode3 = string.Empty;
        private string catalog = string.Empty;
        private string catalog2 = string.Empty;
        private string catalog3 = string.Empty;
        private string description = string.Empty;
        private string mUnit = string.Empty;
        private string mUnit2 = string.Empty;
        private double mUnitRatio = 1;
        private double minimalQuantity;
        private double nominalQuantity;
        private ItemType type;
        private double quantity;
        private double tradeInPrice;
        private double tradePrice;
        private double regularPrice;
        private double priceGroup1;
        private double priceGroup2;
        private double priceGroup3;
        private double priceGroup4;
        private double priceGroup5;
        private double priceGroup6;
        private double priceGroup7;
        private double priceGroup8;
        private long groupId = 1;
        private int order = -1;
        private int deletedDb;
        private string groupName;
        private long vatGroupId = 1;
        private VATGroup vatGroup;
        private bool useMeasUnit2;

        #endregion

        public const long DefaultId = 1;

        #region Public properties

        [DbColumn (DataField.ItemId)]
        public long Id
        {
            get { return id; }
            set { id = value; OnPropertyChanged ("Id"); }
        }

        [ExchangeProperty ("Name", true)]
        [DbColumn (DataField.ItemName, 255)]
        public string Name
        {
            get { return name; }
            set { name = value; OnPropertyChanged ("Name"); }
        }

        [ExchangeProperty ("Name 2")]
        [DbColumn (DataField.ItemName2, 255)]
        public string Name2
        {
            get { return string.IsNullOrWhiteSpace (name2) ? name : name2; }
            set { name2 = value; }
        }

        [ExchangeProperty ("Code")]
        [DbColumn (DataField.ItemCode, 255)]
        public string Code
        {
            get { return code; }
            set { code = value; OnPropertyChanged ("Code"); }
        }

        [ExchangeProperty ("BarCode")]
        [DbColumn (DataField.ItemBarcode1, 255)]
        public string BarCode
        {
            get { return barCode; }
            set { barCode = value; OnPropertyChanged ("BarCode"); }
        }

        [ExchangeProperty ("BarCode 2")]
        [DbColumn (DataField.ItemBarcode2, 255)]
        public string BarCode2
        {
            get { return barCode2; }
            set { barCode2 = value; OnPropertyChanged ("BarCode2"); }
        }

        [ExchangeProperty ("BarCode 3")]
        [DbColumn (DataField.ItemBarcode3, 255)]
        public string BarCode3
        {
            get { return barCode3; }
            set { barCode3 = value; OnPropertyChanged ("BarCode3"); }
        }

        [ExchangeProperty ("Catalog")]
        [DbColumn (DataField.ItemCatalog1, 255)]
        public string Catalog
        {
            get { return catalog; }
            set { catalog = value; OnPropertyChanged ("Catalog"); }
        }

        [ExchangeProperty ("Catalog 2")]
        [DbColumn (DataField.ItemCatalog2, 255)]
        public string Catalog2
        {
            get { return catalog2; }
            set { catalog2 = value; OnPropertyChanged ("Catalog2"); }
        }

        [ExchangeProperty ("Catalog 3")]
        [DbColumn (DataField.ItemCatalog3, 255)]
        public string Catalog3
        {
            get { return catalog3; }
            set { catalog3 = value; OnPropertyChanged ("Catalog3"); }
        }

        [DbColumn (DataField.ItemDescription, 255)]
        public string Description
        {
            get { return description; }
            set { description = value; OnPropertyChanged ("Description"); }
        }

        [ExchangeProperty ("Measuring Unit")]
        [DbColumn (DataField.ItemMeasUnit, 255)]
        public string MUnit
        {
            get { return mUnit; }
            set { mUnit = value; OnPropertyChanged ("MUnit"); }
        }

        [ExchangeProperty ("Measuring Unit 2")]
        [DbColumn (DataField.ItemMeasUnit2, 255)]
        public string MUnit2
        {
            get { return mUnit2; }
            set { mUnit2 = value; OnPropertyChanged ("MUnit2"); }
        }

        [ExchangeProperty ("Measuring Unit Ratio")]
        [DbColumn (DataField.ItemMeasRatio)]
        public double MUnitRatio
        {
            get { return mUnitRatio; }
            set { mUnitRatio = value; OnPropertyChanged ("MUnitRatio"); }
        }

        [ExchangeProperty ("Available Quantity")]
        [DbColumn (DataField.StoreQtty)]
        public double Quantity
        {
            get { return quantity; }
            set { quantity = value; OnPropertyChanged ("Quantity"); }
        }

        [ExchangeProperty ("Minimal Quantity")]
        [DbColumn (DataField.ItemMinQuantity)]
        public double MinimalQuantity
        {
            get { return minimalQuantity; }
            set { minimalQuantity = value; OnPropertyChanged ("MinimalQuantity"); }
        }

        [ExchangeProperty ("Nominal Quantity")]
        [DbColumn (DataField.ItemNomQuantity)]
        public double NominalQuantity
        {
            get { return nominalQuantity; }
            set { nominalQuantity = value; OnPropertyChanged ("NominalQuantity"); }
        }

        [ExchangeProperty ("Item Type")]
        [DbColumn (DataField.ItemType)]
        public ItemType Type
        {
            get { return type; }
            set { type = value; OnPropertyChanged ("Type"); }
        }

        public double QuantityIncrement
        {
            get
            {
                return (Type & ItemType.Discount) == ItemType.Discount ?
                    -1 : 1;
            }
        }

        [ExchangeProperty ("Purchase Price")]
        [DbColumn (DataField.ItemPurchasePrice)]
        public double TradeInPrice
        {
            get { return Currency.Round (tradeInPrice, PriceType.Purchase); }
            set { tradeInPrice = value; OnPropertyChanged ("TradeInPrice"); }
        }

        [ExchangeProperty ("Wholesale Price")]
        [DbColumn (DataField.ItemTradePrice)]
        public double TradePrice
        {
            get { return Currency.Round (tradePrice); }
            set { tradePrice = value; OnPropertyChanged ("TradePrice"); }
        }

        [ExchangeProperty ("Retail Price")]
        [DbColumn (DataField.ItemRegularPrice)]
        public double RegularPrice
        {
            get { return Currency.Round (regularPrice); }
            set { regularPrice = value; OnPropertyChanged ("RegularPrice"); }
        }

        [ExchangeProperty ("Price Group 1")]
        [DbColumn (DataField.ItemPriceGroup1)]
        public double PriceGroup1
        {
            get { return Currency.Round (priceGroup1); }
            set { priceGroup1 = value; OnPropertyChanged ("PriceGroup1"); }
        }

        [ExchangeProperty ("Price Group 2")]
        [DbColumn (DataField.ItemPriceGroup2)]
        public double PriceGroup2
        {
            get { return Currency.Round (priceGroup2); }
            set { priceGroup2 = value; OnPropertyChanged ("PriceGroup2"); }
        }

        [ExchangeProperty ("Price Group 3")]
        [DbColumn (DataField.ItemPriceGroup3)]
        public double PriceGroup3
        {
            get { return Currency.Round (priceGroup3); }
            set { priceGroup3 = value; OnPropertyChanged ("PriceGroup3"); }
        }

        [ExchangeProperty ("Price Group 4")]
        [DbColumn (DataField.ItemPriceGroup4)]
        public double PriceGroup4
        {
            get { return Currency.Round (priceGroup4); }
            set { priceGroup4 = value; OnPropertyChanged ("PriceGroup4"); }
        }

        [ExchangeProperty ("Price Group 5")]
        [DbColumn (DataField.ItemPriceGroup5)]
        public double PriceGroup5
        {
            get { return Currency.Round (priceGroup5); }
            set { priceGroup5 = value; OnPropertyChanged ("PriceGroup5"); }
        }

        [ExchangeProperty ("Price Group 6")]
        [DbColumn (DataField.ItemPriceGroup6)]
        public double PriceGroup6
        {
            get { return Currency.Round (priceGroup6); }
            set { priceGroup6 = value; OnPropertyChanged ("PriceGroup6"); }
        }

        [ExchangeProperty ("Price Group 7")]
        [DbColumn (DataField.ItemPriceGroup7)]
        public double PriceGroup7
        {
            get { return Currency.Round (priceGroup7); }
            set { priceGroup7 = value; OnPropertyChanged ("PriceGroup7"); }
        }

        [ExchangeProperty ("Price Group 8")]
        [DbColumn (DataField.ItemPriceGroup8)]
        public double PriceGroup8
        {
            get { return Currency.Round (priceGroup8); }
            set { priceGroup8 = value; OnPropertyChanged ("PriceGroup8"); }
        }

        [DbColumn (DataField.ItemGroupId)]
        public long GroupId
        {
            get { return groupId; }
            set { groupId = value; }
        }

        [DbColumn (DataField.ItemOrder)]
        public int Order
        {
            get { return order; }
            set { order = value; }
        }

        public bool Deleted
        {
            get { return deletedDb == -1; }
            set { deletedDb = value ? -1 : 0; }
        }

        [DbColumn (DataField.ItemDeleted)]
        public int DeletedDb
        {
            get { return deletedDb; }
            set { deletedDb = value; }
        }

        [ExchangeProperty ("Group Name", false, DataField.ItemsGroupName)]
        public string GroupName
        {
            get
            {
                if (!string.IsNullOrEmpty (groupName))
                    return groupName;

                return ItemsGroup.GetPath (Math.Abs (groupId), ItemsGroup.Cache);
            }
            set { groupName = value; }
        }

        public bool IsRecipe
        {
            get { return IsRecipeDB == -1; }
            set { IsRecipeDB = value ? -1 : 0; }
        }

        [DbColumn (DataField.ItemIsRecipe)]
        public int IsRecipeDB { get; set; }

        [ExchangeProperty ("VAT Group Id")]
        [DbColumn (DataField.ItemTaxGroupId)]
        public long VatGroupId
        {
            get { return vatGroupId; }
            set
            {
                if (value != vatGroupId)
                    vatGroup = null;

                vatGroupId = value;
            }
        }

        public string LotNumber { get; set; }

        public double MUnit2Price
        {
            get
            {
                double ratio = mUnitRatio;
                if (ratio.IsZero ())
                    ratio = 1;

                return Currency.Round (tradePrice * ratio);
            }
        }

        public string MUnit1PriceFormatted
        {
            get { return string.Format ("{0}/{1}", Currency.ToString (tradePrice), mUnit); }
        }

        public string MUnit2PriceFormatted
        {
            get
            {
                if (string.IsNullOrWhiteSpace (mUnit2))
                    return string.Empty;

                return string.Format ("{0}/{1}", Currency.ToString (MUnit2Price), mUnit2);
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged (string property)
        {
            if (PropertyChanged != null)
                PropertyChanged (this, new PropertyChangedEventArgs (property));
        }

        #endregion

        private static readonly CacheEntityCollection<Item> cache = new CacheEntityCollection<Item> ();
        public static CacheEntityCollection<Item> Cache
        {
            get { return cache; }
        }

        public Item CommitChanges ()
        {
            if (barCode3 != null && barCode3.Length > 254) {
                while (barCode3.Length > 254 && barCode3.Contains (",")) {
                    barCode3 = barCode3.Substring (0, barCode3.LastIndexOf (','));
                }

                if (barCode3.Length > 254)
                    barCode3 = barCode3.Substring (0, 254);
            }

            if (BusinessDomain.AppConfiguration.AutoGenerateItemCodes && string.IsNullOrWhiteSpace (code))
                AutoGenerateCode ();

            if (!string.IsNullOrEmpty (groupName) && groupId <= 1) {
                ItemsGroup g = ItemsGroup.EnsureByPath (groupName, ItemsGroup.Cache);
                groupId = g.Id;
            }

            BusinessDomain.DataAccessProvider.AddUpdateItem (this);
            cache.Set (this);

            return this;
        }

        public double GetPriceGroupPrice (PriceGroup pGroup)
        {
            switch (pGroup) {
                case PriceGroup.TradeInPrice:
                    return TradeInPrice;
                case PriceGroup.TradePrice:
                    return TradePrice;
                case PriceGroup.PriceGroup1:
                    return PriceGroup1;
                case PriceGroup.PriceGroup2:
                    return PriceGroup2;
                case PriceGroup.PriceGroup3:
                    return PriceGroup3;
                case PriceGroup.PriceGroup4:
                    return PriceGroup4;
                case PriceGroup.PriceGroup5:
                    return PriceGroup5;
                case PriceGroup.PriceGroup6:
                    return PriceGroup6;
                case PriceGroup.PriceGroup7:
                    return PriceGroup7;
                case PriceGroup.PriceGroup8:
                    return PriceGroup8;
                default:
                    return RegularPrice;
            }
        }

        public void SetPriceGroupPrice (PriceGroup pGroup, double value)
        {
            switch (pGroup) {
                case PriceGroup.TradeInPrice:
                    TradeInPrice = value;
                    break;
                case PriceGroup.TradePrice:
                    TradePrice = value;
                    break;
                case PriceGroup.RegularPrice:
                    RegularPrice = value;
                    break;
                case PriceGroup.PriceGroup1:
                    PriceGroup1 = value;
                    break;
                case PriceGroup.PriceGroup2:
                    PriceGroup2 = value;
                    break;
                case PriceGroup.PriceGroup3:
                    PriceGroup3 = value;
                    break;
                case PriceGroup.PriceGroup4:
                    PriceGroup4 = value;
                    break;
                case PriceGroup.PriceGroup5:
                    PriceGroup5 = value;
                    break;
                case PriceGroup.PriceGroup6:
                    PriceGroup6 = value;
                    break;
                case PriceGroup.PriceGroup7:
                    PriceGroup7 = value;
                    break;
                case PriceGroup.PriceGroup8:
                    PriceGroup8 = value;
                    break;
            }
        }

        public VATGroup GetVATGroup ()
        {
            return vatGroup ?? (vatGroup = VATGroup.Cache.GetById (vatGroupId));
        }

        public double GetPriceWithoutVAT (double value, PriceType priceType = PriceType.Sale)
        {
            if (BusinessDomain.AppConfiguration.VATIncluded) {
                double vatRate = GetVATGroup ().VatValue;
                double vatValue = Currency.Round ((value * vatRate) / (100 + vatRate), priceType);

                value -= vatValue;
            }

            return Currency.Round (value, priceType);
        }

        public void AutoGenerateCode ()
        {
            string pattern = BusinessDomain.AppConfiguration.ItemCodePattern;
            ulong lastCode = BusinessDomain.DataAccessProvider.GetMaxCodeValue (DbTable.Items, pattern);
            code = CodeGenerator.GenerateCode (pattern, lastCode + 1);
        }

        public static string GetPriceGroupProperty (PriceGroup pGroup)
        {
            switch (pGroup) {
                case PriceGroup.TradeInPrice:
                    return "TradeInPrice";
                case PriceGroup.TradePrice:
                    return "TradePrice";
                case PriceGroup.PriceGroup1:
                    return "PriceGroup1";
                case PriceGroup.PriceGroup2:
                    return "PriceGroup2";
                case PriceGroup.PriceGroup3:
                    return "PriceGroup3";
                case PriceGroup.PriceGroup4:
                    return "PriceGroup4";
                case PriceGroup.PriceGroup5:
                    return "PriceGroup5";
                case PriceGroup.PriceGroup6:
                    return "PriceGroup6";
                case PriceGroup.PriceGroup7:
                    return "PriceGroup7";
                case PriceGroup.PriceGroup8:
                    return "PriceGroup8";
                default:
                    return "RegularPrice";
            }
        }

        public static DataField GetPriceGroupField (PriceGroup pGroup)
        {
            switch (pGroup) {
                case PriceGroup.TradeInPrice:
                    return DataField.ItemPurchasePrice;
                case PriceGroup.TradePrice:
                    return DataField.ItemTradePrice;
                case PriceGroup.PriceGroup1:
                    return DataField.ItemPriceGroup1;
                case PriceGroup.PriceGroup2:
                    return DataField.ItemPriceGroup2;
                case PriceGroup.PriceGroup3:
                    return DataField.ItemPriceGroup3;
                case PriceGroup.PriceGroup4:
                    return DataField.ItemPriceGroup4;
                case PriceGroup.PriceGroup5:
                    return DataField.ItemPriceGroup5;
                case PriceGroup.PriceGroup6:
                    return DataField.ItemPriceGroup6;
                case PriceGroup.PriceGroup7:
                    return DataField.ItemPriceGroup7;
                case PriceGroup.PriceGroup8:
                    return DataField.ItemPriceGroup8;
                default:
                    return DataField.ItemRegularPrice;
            }
        }

        public static DeletePermission RequestDelete (long itemId)
        {
            return BusinessDomain.DataAccessProvider.CanDeleteItem (itemId);
        }

        public static void Delete (long itemId)
        {
            BusinessDomain.DataAccessProvider.DeleteItem (itemId);
            cache.Remove (itemId);
        }

        public static LazyListModel<Item> GetAll (long? groupId = null, bool onlyAvailable = false,
            bool autoStart = true, bool includeDeleted = false)
        {
            return BusinessDomain.DataAccessProvider.GetAllItems<Item> (groupId, onlyAvailable, autoStart, includeDeleted);
        }

        public static LazyListModel<Item> GetAllByLocation (long locationId, long? groupId, bool onlyAvailable = false, bool autoStart = true)
        {
            return BusinessDomain.DataAccessProvider.GetAllItemsByLocation<Item> (locationId, groupId, onlyAvailable, autoStart);
        }

        public static LazyListModel<Item> GetAllAvailableAtLocation (long locationId, long? groupId)
        {
            return BusinessDomain.DataAccessProvider.GetAllAvailableItemsAtLocation<Item> (locationId, groupId);
        }

        public static ItemType? GetItemType (long itemId)
        {
            return BusinessDomain.DataAccessProvider.GetItemType (itemId);
        }

        public static double GetAvailability (long itemId, long locationId, long childLocationId = -1)
        {
            if ((GetItemType (itemId) & ItemType.NonInventory) == ItemType.NonInventory)
                return double.MaxValue;

            return BusinessDomain.DataAccessProvider.GetItemAvailability (itemId, locationId, childLocationId);
        }

        public static double GetAvailabilityAtDate (long itemId, long locationId, DateTime date)
        {
            if ((GetItemType (itemId) & ItemType.NonInventory) == ItemType.NonInventory)
                return double.MaxValue;

            return BusinessDomain.DataAccessProvider.GetItemAvailabilityAtDate (itemId, locationId, date);
        }

        public static Item GetById (long itemId)
        {
            Item ret = BusinessDomain.DataAccessProvider.GetItemById<Item> (itemId);
            cache.Set (ret);
            return ret;
        }

        public static Item GetByName (string itemName)
        {
            Item ret = BusinessDomain.DataAccessProvider.GetItemByName<Item> (itemName);
            cache.Set (ret);
            return ret;
        }

        public static Item GetByCode (string itemCode)
        {
            Item ret = BusinessDomain.DataAccessProvider.GetItemByCode<Item> (itemCode);
            cache.Set (ret);
            return ret;
        }

        public static Item GetByBarCode (string itemBarcode)
        {
            if (string.IsNullOrEmpty (itemBarcode))
                return null;

            Item ret = BusinessDomain.DataAccessProvider.GetItemByBarcode<Item> (itemBarcode);
            if (ret == null)
                return null;

            if (IsBarcodeMatching (itemBarcode, ret.barCode))
                ret.SetParamsFromBarcode (itemBarcode, ret.barCode);
            else if (IsBarcodeMatching (itemBarcode, ret.barCode2))
                ret.SetParamsFromBarcode (itemBarcode, ret.barCode2);
            else if (IsBarcodeMatching (itemBarcode, ret.barCode3))
                ret.SetParamsFromBarcode (itemBarcode, ret.barCode3);

            cache.Set (ret);
            return ret;
        }

        public static Item GetByCatalog (string catalog)
        {
            Item ret = BusinessDomain.DataAccessProvider.GetItemByCatalog<Item> (catalog);
            cache.Set (ret);
            return ret;
        }

        public static Item GetBySerial (string serial, long locationId)
        {
            Item ret = BusinessDomain.DataAccessProvider.GetItemBySerialNumber<Item> (serial, locationId, BusinessDomain.AppConfiguration.ItemsManagementType);
            cache.Set (ret);
            return ret;
        }

        public static long GetMaxBarcodeSubNumber (string prefix, int length, int subNumberStart, int subNumberLen)
        {
            return BusinessDomain.DataAccessProvider.GetMaxBarcodeSubNumber (prefix, length, subNumberStart, subNumberLen);
        }

        public bool CheckForLotInBarCodes ()
        {
            return new [] { barCode, barCode2, barCode3 }.Any (b => Regex.IsMatch (b, "l+", RegexOptions.IgnoreCase));
        }

        public string GetRealBarCodeFromPattern (string pattern)
        {
            Match lotMask = Regex.Match (pattern, "l+", RegexOptions.IgnoreCase);
            return lotMask.Success ? pattern.Replace (lotMask.Value, LotNumber.PadLeft (lotMask.Value.Length, '0')) : pattern;
        }

        private static bool IsBarcodeMatching (string barCode, string mask)
        {
            mask = mask.Replace (".", string.Empty);
            if (mask.Length != barCode.Length)
                return false;

            for (int i = 0; i < barCode.Length; i++) {
                char lowerMask = char.ToLower (mask [i]);
                if (lowerMask == 'w' ||
                    lowerMask == 'c' ||
                    lowerMask == 'l')
                    continue;

                if (mask [i] != barCode [i])
                    return false;
            }

            return true;
        }

        private void SetParamsFromBarcode (string scannedBarCode, string mask)
        {
            int dotIndex = mask.IndexOf ('.');
            if (dotIndex < 0)
                dotIndex = int.MaxValue;

            mask = mask.Replace (".", string.Empty);
            if (mask.Length != scannedBarCode.Length) {
                quantity = 0;
                return;
            }

            double weight = 0;
            int delimiter = 1;
            int lot = 0;

            for (int i = 0; i < scannedBarCode.Length; i++) {
                switch (char.ToLower (mask [i])) {
                    case 'w':
                        weight *= 10;
                        weight += scannedBarCode [i] - '0';
                        if (i >= dotIndex)
                            delimiter *= 10;
                        break;
                    case 'l':
                        lot *= 10;
                        lot += scannedBarCode [i] - '0';
                        break;
                }
            }

            quantity = weight / delimiter;
            LotNumber = lot > 0 ? lot.ToString (CultureInfo.InvariantCulture) : null;
            useMeasUnit2 = barCode2 == mask;
        }

        public static Item GetByAny (string input)
        {
            bool barcodeUsed;
            double quantity;
            string codeLot;
            long storeId;
            return GetByAny (input, out barcodeUsed, out quantity, out codeLot, out storeId);
        }

        public static Item GetByAny (string input, out bool barCodeUsed, out double qtty, out string lot, out long storeId, long? locationId = null)
        {
            barCodeUsed = false;
            qtty = 0;
            lot = null;
            storeId = -1;

            // Empty field is never correct
            if (input.Length == 0)
                return null;

            // Check if we have a qtty*item record
            qtty = GetSearchQuantity (ref input);

            Item item = GetByBarCode (input);
            if (item != null) {
                barCodeUsed = true;
                if (!item.quantity.IsZero ()) {
                    if (qtty.IsZero ())
                        qtty = item.quantity;
                    else
                        qtty *= item.quantity;
                }

                if (item.useMeasUnit2 && !item.mUnitRatio.IsZero ()) {
                    if (qtty.IsZero ())
                        qtty = item.mUnitRatio;
                    else
                        qtty *= item.mUnitRatio;
                }

                item.quantity = qtty;
                lot = item.LotNumber;
            }

            // If we can't find Item with such a barcode try to search for such a name or code
            item = item ?? cache.GetByName (input) ?? cache.GetByCode (input);

            if (item == null && BusinessDomain.AppConfiguration.ItemsManagementUseLots && locationId != null) {
                item = GetBySerial (input, locationId.Value);
                if (item != null) {
                    storeId = (int) item.quantity;
                    item.quantity = 0;
                }
            }

            return item;
        }

        public static double GetSearchQuantity (ref string input)
        {
            double qtty = 0;
            int starIndex = input.IndexOf ("*");
            if (starIndex > 0 && starIndex < input.Length - 1) {
                string qttyString = input.Substring (0, starIndex);
                if (Entities.Quantity.TryParseExpression (qttyString, out qtty))
                    input = input.Substring (starIndex + 1);
            }
            return qtty;
        }

        #region IDisposable Members

        public void Dispose ()
        {
        }

        #endregion

        public bool Validate (ValidateCallback callback, StateHolder state)
        {
            if (callback == null)
                throw new ArgumentNullException ("callback");

            if (string.IsNullOrWhiteSpace (name))
                if (!callback (Translator.GetString ("Item name cannot be empty!"), ErrorSeverity.Error, (int) ErrorCodes.NameEmpty, state))
                    return false;

            if (string.IsNullOrWhiteSpace (mUnit)) {
                if (!callback (string.Format (Translator.GetString ("Measurement unit for item \"{0}\" is empty! Do you want to use the default one?"), name),
                       ErrorSeverity.Warning, (int) ErrorCodes.MeasUnitEmpty, state))
                    return false;

                mUnit = Translator.GetString ("pcs.");
            }

            if (barCode3 != null && barCode3.Length > 254) {
                if (!callback (string.Format (Translator.GetString ("There are too many additional barcodes! The excess barcodes will not be saved. Do you want to save the item anyway?"), name),
                    ErrorSeverity.Warning, (int) ErrorCodes.TooManyBarcodes, state))
                    return false;
            }

            Item g = GetByName (name);
            if (g != null && g.Id != id) {
                if (!callback (string.Format (Translator.GetString ("Item with the name \"{0}\" already exists! Do you want to save the item anyway?"), name),
                    ErrorSeverity.Warning, (int) ErrorCodes.NameInUse, state))
                    return false;
            }

            if (!string.IsNullOrEmpty (code)) {
                g = GetByCode (code);
                if (g != null && g.Id != id) {
                    if (!callback (string.Format (Translator.GetString ("Item with the code \"{0}\" already exists. Do you want to save the item anyway?"), code),
                        ErrorSeverity.Warning, (int) ErrorCodes.CodeInUse, state))
                        return false;
                }
            }

            bool isDupl = false;
            string duplCode = string.Empty;
            if (barCode.Length > 0) {
                g = GetByBarCode (barCode);
                if (g != null && g.Id != id)
                    isDupl = true;

                if (!isDupl && barCode2.Length > 0 && barCode == barCode2)
                    isDupl = true;

                if (!isDupl && barCode3.Length > 0 && barCode == barCode3)
                    isDupl = true;

                if (isDupl)
                    duplCode = barCode;
            }

            if (!isDupl && barCode2.Length > 0) {
                g = GetByBarCode (barCode2);
                if (g != null && g.Id != id)
                    isDupl = true;

                if (!isDupl && barCode3.Length > 0 && barCode2 == barCode3)
                    isDupl = true;

                if (isDupl)
                    duplCode = barCode2;
            }

            if (!isDupl && barCode3.Length > 0) {
                g = GetByBarCode (barCode3);
                if (g != null && g.Id != id) {
                    isDupl = true;
                    duplCode = barCode;
                }
            }

            if (isDupl) {
                if (!callback (string.Format (Translator.GetString ("The barcode \"{0}\" already exists. Do you want to save the item anyway?"), duplCode),
                    ErrorSeverity.Warning, (int) ErrorCodes.BarcodeInUse, state))
                    return false;
            }

            return true;
        }

        #region Overrides of ICacheableEntityBase<Item>

        public Item GetEntityById (long entityId)
        {
            return GetById (entityId);
        }

        public Item GetEntityByCode (string entityCode)
        {
            return GetByCode (entityCode);
        }

        public Item GetEntityByName (string entityName)
        {
            return GetByName (entityName);
        }

        public IEnumerable<Item> GetAllEntities ()
        {
            return GetAll ();
        }

        public object Clone ()
        {
            return MemberwiseClone ();
        }

        #endregion

        public static KeyValuePair<int, string> [] GetAllTypes ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) ItemType.Standard, Translator.GetString ("Standard")),
                    new KeyValuePair<int, string> ((int) ItemType.FixedPrice, Translator.GetString ("Fixed price")),
                    new KeyValuePair<int, string> ((int) ItemType.VariablePrice, Translator.GetString ("Variable price"))
                };
        }

        public static KeyValuePair<int, string> [] GetAllTypeFilters ()
        {
            List<KeyValuePair<int, string>> filters = new List<KeyValuePair<int, string>>
                {
                    new KeyValuePair<int, string> (-1, Translator.GetString ("All"))
                };

            filters.AddRange (GetAllTypes ());

            return filters.ToArray ();
        }

        public static KeyValuePair<int, string> [] GetAllDistributedChargeMethods ()
        {
            return new []
                {
                    new KeyValuePair<int, string> ((int) DistributedChargeMethods.Total, Translator.GetString ("Total")),
                    new KeyValuePair<int, string> ((int) DistributedChargeMethods.Quantity, Translator.GetString ("Quantity")),
                    new KeyValuePair<int, string> ((int) DistributedChargeMethods.Price, Translator.GetString ("Purchase price"))
                };
        }
    }

    public enum DistributedChargeMethods
    {
        Total,
        Quantity,
        Price
    }
}