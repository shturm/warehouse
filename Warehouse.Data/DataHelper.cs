//
// DataHelper.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   05/22/2009
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Warehouse.Data.ThreadPool;

namespace Warehouse.Data
{
    public static class DataHelper
    {
        #region Fields

        private static CustomThreadPool modelThreadPool;

        #endregion

        #region Public Properties

        public static string ProductName { get; set; }

        private static string productFullName;
        public static string ProductFullName
        {
            get { return string.IsNullOrEmpty (productFullName) ? ProductName : productFullName; }
            set { productFullName = value; }
        }

        public static string ProductSupportEmail { get; set; }

        public static string CompanyName { get; set; }

        public static string CompanyAddress { get; set; }

        public static string CompanyWebSite { get; set; }

        public static string FeedbackServiceUrl { get; set; }

        public static Assembly ResourcesAssembly { get; set; }

        public static string GetDefaultKeyMap ()
        {
            string keymap = string.Format ("{0}.{1}.keymap", ResourcesAssembly.GetName ().Name, PlatformHelper.Platform);
            using (Stream resource = ResourcesAssembly.GetManifestResourceStream (keymap)) {
                if (resource == null)
                    throw new NullReferenceException ("Cannot find resource: " + keymap);
                using (StreamReader streamReader = new StreamReader (resource))
                    return streamReader.ReadToEnd ();
            }
        }

        public static bool DisableEntityCaching { get; set; }

        private static string defaultDocumentFont;
        public static string DefaultDocumentsFont
        {
            get { return defaultDocumentFont ?? (defaultDocumentFont = GetFontWithoutSize (DefaultUIFont)); }
            set { defaultDocumentFont = value; }
        }

        private static string defaultUIFont;
        public static string DefaultUIFont
        {
            get { return GetPreferredFont (null) ?? defaultUIFont; }
            set { defaultUIFont = value; }
        }

        public static CustomThreadPool ModelThreadPool
        {
            get { return modelThreadPool ?? (modelThreadPool = new CustomThreadPool ("Model Thread Pool") { MinWorkers = 1 }); }
        }

        #endregion

        public static string GetPreferredFont (string text)
        {
            string loc = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

            if (!string.IsNullOrWhiteSpace (text))
                foreach (char c in text) {
                    byte [] bytes = Encoding.UTF8.GetBytes (c.ToString ());
                    int intValue;
                    switch (bytes.Length) {
                        case 1:
                            intValue = bytes [0];
                            break;
                        case 2:
                            intValue = bytes [0] << 8 | bytes [1];
                            break;
                        case 3:
                            intValue = bytes [0] << 16 | bytes [1] << 8 | bytes [2];
                            break;
                        default:
                            intValue = -1;
                            break;
                    }

                    if (IsGeorgianChar (intValue)) {
                        loc = "ka";
                        break;
                    }

                    if (IsArmenianChar (intValue)) {
                        loc = "hy";
                        break;
                    }
                }

            switch (PlatformHelper.Platform) {
                case PlatformTypes.Windows:
                    if (loc == "ka" || loc == "hy")
                        return "Sylfaen";
                    break;
                case PlatformTypes.MacOSX:
                    if (loc == "ka" || loc == "hy")
                        return "Halvetica 12";
                    break;
            }

            return null;
        }

        private static bool IsArmenianChar (int intValue)
        {
            return (intValue >= 0xd4b1 && intValue <= 0xd687);
        }

        private static bool IsGeorgianChar (int intValue)
        {
            return (intValue >= 0xe182a0 && intValue <= 0xe183ba);
        }

        private static string GetFontWithoutSize (string font)
        {
            Match match = Regex.Match (font, "(.+)\\s+(\\d+)$");
            if (!match.Success)
                return font;

            return match.Groups [1].Value;
        }

        public static void FireAndForget (Action target)
        {
            Thread thread = new Thread (delegate ()
                {
                    try {
                        target ();
                    } catch (Exception) {
                    }
                }) { IsBackground = true };

            thread.Start ();
        }

        public static void FireAndForget<T> (Action<T> target, T argument)
        {
            Thread thread = new Thread (() =>
                {
                    try {
                        target (argument);
                    } catch (Exception) {
                    }
                }) { IsBackground = true };

            thread.Start ();
        }

        public static void Dispose ()
        {
            if (modelThreadPool == null)
                return;

            modelThreadPool.Dispose ();
            modelThreadPool = null;
        }

        #region Date management methods

        public static DateTime GetDateValue (string value, string format)
        {
            DateTime date;

            if (string.IsNullOrEmpty (format)) {
                if (!DateTime.TryParse (value, out date))
                    return DateTime.MinValue;
            } else {
                try {
                    if (!DateTime.TryParseExact (value, format, null, DateTimeStyles.None, out date))
                        return DateTime.MinValue;
                } catch {
                    date = DateTime.MinValue;
                }
            }

            return date;
        }

        public static string GetFormattedDate (DateTime date, string format)
        {
            try {
                return string.IsNullOrEmpty (format) ?
                    date.ToShortDateString () :
                    date.ToString (format, null);
            } catch {
                return string.Empty;
            }
        }

        #endregion

        private static T GetException<T> (Exception ex) where T : Exception
        {
            if (ex.InnerException == null)
                return ex as T;

            T inner = ex.InnerException as T;
            return inner ?? GetException<T> (ex.InnerException);
        }

        public static bool HasException<T> (this Exception ex) where T : Exception
        {
            return GetException<T> (ex) != null;
        }

        public static bool ContainsInnerMessaage (this Exception ex, string subMessage)
        {
            while (true) {
                if (ex.Message.Contains (subMessage))
                    return true;

                if (ex.InnerException == null)
                    return false;

                ex = ex.InnerException;
            }
        }

        public static int IndexOf<T> (this T [] array, Predicate<T> condition)
        {
            for (int i = 0; i < array.Length; i++) {
                if (condition (array [i]))
                    return i;
            }

            return -1;
        }

        private static Dictionary<DataField, DataField> migratedDataFieldMap;
        private static Dictionary<DataField, DataField> MigratedDataFieldMap
        {
            get
            {
                return migratedDataFieldMap ?? (migratedDataFieldMap = new Dictionary<DataField, DataField>
                    {
                        { DataField.CashEntryPointOfSaleId, DataField.CashEntryLocationId },
                        { DataField.DocumentPointOfSale, DataField.DocumentLocation },
                        { DataField.GoodsId, DataField.ItemId },
                        { DataField.GoodsCode, DataField.ItemCode },
                        { DataField.GoodsBarCode1, DataField.ItemBarcode1 },
                        { DataField.GoodsBarCode2, DataField.ItemBarcode2 },
                        { DataField.GoodsBarCode3, DataField.ItemBarcode3 },
                        { DataField.GoodsCatalog1, DataField.ItemCatalog1 },
                        { DataField.GoodsCatalog2, DataField.ItemCatalog2 },
                        { DataField.GoodsCatalog3, DataField.ItemCatalog3 },
                        { DataField.GoodsName, DataField.ItemName },
                        { DataField.GoodsName2, DataField.ItemName2 },
                        { DataField.GoodsMeasUnit, DataField.ItemMeasUnit },
                        { DataField.GoodsMeasUnit2, DataField.ItemMeasUnit2 },
                        { DataField.GoodsMeasRatio, DataField.ItemMeasRatio },
                        { DataField.GoodsTradeInPrice, DataField.ItemTradeInSum },
                        { DataField.GoodsTradePrice, DataField.ItemTradePrice },
                        { DataField.GoodsRegularPrice, DataField.ItemRegularPrice },
                        { DataField.GoodsPriceGroup1, DataField.ItemPriceGroup1 },
                        { DataField.GoodsPriceGroup2, DataField.ItemPriceGroup2 },
                        { DataField.GoodsPriceGroup3, DataField.ItemPriceGroup3 },
                        { DataField.GoodsPriceGroup4, DataField.ItemPriceGroup4 },
                        { DataField.GoodsPriceGroup5, DataField.ItemPriceGroup5 },
                        { DataField.GoodsPriceGroup6, DataField.ItemPriceGroup6 },
                        { DataField.GoodsPriceGroup7, DataField.ItemPriceGroup7 },
                        { DataField.GoodsPriceGroup8, DataField.ItemPriceGroup8 },
                        { DataField.GoodsMinQuantity, DataField.ItemMinQuantity },
                        { DataField.GoodsNomQuantity, DataField.ItemNomQuantity },
                        { DataField.GoodsDescription, DataField.ItemDescription },
                        { DataField.GoodsType, DataField.ItemType },
                        { DataField.GoodsGroupId, DataField.ItemGroupId },
                        { DataField.GoodsIsRecipe, DataField.ItemIsRecipe },
                        { DataField.GoodsTaxGroupId, DataField.ItemTaxGroupId },
                        { DataField.GoodsOrder, DataField.ItemOrder },
                        { DataField.GoodsDeleted, DataField.ItemDeleted },
                        { DataField.GoodsTradeInVAT, DataField.ItemTradeInVAT },
                        { DataField.GoodsTradeInSum, DataField.ItemTradeInSum },
                        { DataField.GoodsTradeVAT, DataField.ItemTradeVAT },
                        { DataField.GoodsTradeSum, DataField.ItemTradeSum },
                        { DataField.GoodsGroupsId, DataField.ItemsGroupId },
                        { DataField.GoodsGroupsName, DataField.ItemsGroupName },
                        { DataField.GoodsGroupsCode, DataField.ItemsGroupCode },
                        { DataField.PointOfSaleId, DataField.LocationId },
                        { DataField.PointOfSaleCode, DataField.LocationCode },
                        { DataField.PointOfSaleName, DataField.LocationName },
                        { DataField.PointOfSaleName2, DataField.LocationName2 },
                        { DataField.PointOfSaleOrder, DataField.LocationOrder },
                        { DataField.PointOfSaleDeleted, DataField.LocationDeleted },
                        { DataField.PointOfSaleGroupId, DataField.LocationGroupId },
                        { DataField.PointOfSalePriceGroup, DataField.LocationPriceGroup },
                        { DataField.SourcePointOfSaleName, DataField.SourceLocationName },
                        { DataField.TargetPointOfSaleName, DataField.TargetLocationName },
                        { DataField.PointsOfSaleGroupsId, DataField.LocationsGroupsId },
                        { DataField.PointsOfSaleGroupsName, DataField.LocationsGroupsName },
                        { DataField.PointsOfSaleGroupsCode, DataField.LocationsGroupsCode },
                        { DataField.OperationPointOfSaleId, DataField.OperationLocationId },
                        { DataField.OperationPointOfSale, DataField.OperationLocation },
                        { DataField.OperationPointOfSale2, DataField.OperationLocation2 },
                        { DataField.OperationDetailGoodsId, DataField.OperationDetailItemId },
                        { DataField.OperationDetailSumVatIn, DataField.OperationDetailVatInSum },
                        { DataField.OperationDetailSumVatOut, DataField.OperationDetailVatOutSum },
                        { DataField.OperationDetailSumVat, DataField.OperationDetailVatSum },
                    });
            }
        }

        public static DbField GetMigratedDataField (DbField field)
        {
            DataField ret;
            return MigratedDataFieldMap.TryGetValue (field.StrongField, out ret) ? ret : field;
        }
    }
}
