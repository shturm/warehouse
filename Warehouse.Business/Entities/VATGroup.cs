//
// VATGroup.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   06/30/2006
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
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Entities
{
    public class VATGroup : IStrongEntity, IPersistableEntity<VATGroup>, ICacheableEntity<VATGroup>
    {
        #region Private fields

        private long id = -1;
        private string name = string.Empty;
        private string code = string.Empty;
        private double vatValue;
        private static Dictionary<FiscalPrinterTaxGroup, string> vatCodes;

        #endregion

        #region Public properties

        [DbColumn (DataField.VATGroupId)]
        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        [ExchangeProperty ("Name", true)]
        [DbColumn (DataField.VATGroupName, 255)]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [ExchangeProperty ("Code")]
        [DbColumn (DataField.VATGroupCode, 255)]
        public string Code
        {
            get { return code; }
            set { code = value; }
        }

        public string CodeName
        {
            get { return GetVATGroupCodeName (code); }
        }

        public FiscalPrinterTaxGroup CodeValue
        {
            get
            {
                return GetVATCodeValue (code);
            }
        }

        [ExchangeProperty ("Value")]
        [DbColumn (DataField.VATGroupValue)]
        public double VatValue
        {
            get { return vatValue; }
            set { vatValue = value; }
        }

        public static int DefaultVATGroupId
        {
            get { return 1; }
        }

        public static Dictionary<FiscalPrinterTaxGroup, string> VatCodes
        {
            get
            {
                return vatCodes ?? (vatCodes = new Dictionary<FiscalPrinterTaxGroup, string>
                    {
                        {FiscalPrinterTaxGroup.A, "A"},
                        {FiscalPrinterTaxGroup.B, "B"},
                        {FiscalPrinterTaxGroup.C, "C"},
                        {FiscalPrinterTaxGroup.D, "D"},
                        {FiscalPrinterTaxGroup.E, "E"},
                        {FiscalPrinterTaxGroup.F, "F"},
                        {FiscalPrinterTaxGroup.G, "G"},
                        {FiscalPrinterTaxGroup.H, "H"},
                        {FiscalPrinterTaxGroup.I, "I"},
                        {FiscalPrinterTaxGroup.J, "J"},
                        {FiscalPrinterTaxGroup.K, "K"},
                        {FiscalPrinterTaxGroup.L, "L"},
                        {FiscalPrinterTaxGroup.M, "M"},
                        {FiscalPrinterTaxGroup.N, "N"},
                        {FiscalPrinterTaxGroup.O, "O"},
                        {FiscalPrinterTaxGroup.P, "P"},
                        {FiscalPrinterTaxGroup.Q, "Q"},
                        {FiscalPrinterTaxGroup.R, "R"},
                        {FiscalPrinterTaxGroup.S, "S"},
                        {FiscalPrinterTaxGroup.TaxExempt, "Exempt"},
                        {FiscalPrinterTaxGroup.Default, string.Empty}
                    });
            }
        }

        #endregion

        private static readonly CacheEntityCollection<VATGroup> cache = new CacheEntityCollection<VATGroup> ();
        public static CacheEntityCollection<VATGroup> Cache
        {
            get { return cache; }
        }

        public static FiscalPrinterTaxGroup GetVATCodeValue (string vatCode)
        {
            foreach (KeyValuePair<FiscalPrinterTaxGroup, string> pair in VatCodes) {
                if (pair.Value == vatCode)
                    return pair.Key;
            }

            return FiscalPrinterTaxGroup.Default;
        }

        public static string GetVATCode (FiscalPrinterTaxGroup vatCode)
        {
            string ret;

            return VatCodes.TryGetValue (vatCode, out ret) ? ret : string.Empty;
        }

        public static VATGroup GetExemptGroup (IEnumerable<VATGroup> allGroups = null)
        {
            List<VATGroup> allEmpty = new List<VATGroup> ();
            foreach (VATGroup vatGroup in allGroups ?? GetAll ()) {
                if (vatGroup.VatValue.IsZero ())
                    allEmpty.Add (vatGroup);
            }

            if (allEmpty.Count == 0)
                return null;
            
            if (allEmpty.Count == 1)
                return allEmpty [0];

            foreach (VATGroup vatGroup in allEmpty) {
                if (vatGroup.CodeValue == FiscalPrinterTaxGroup.TaxExempt)
                    return vatGroup;
            }

            return allEmpty [0];
        }

        public VATGroup CommitChanges ()
        {
            BusinessDomain.DataAccessProvider.AddUpdateVATGroup (this);
            cache.Set (this);

            return this;
        }

        public static DeletePermission RequestDelete (long vatGroupId)
        {
            return BusinessDomain.DataAccessProvider.CanDeleteVATGroup (vatGroupId);
        }

        public static void Delete (long vatGroupId)
        {
            BusinessDomain.DataAccessProvider.DeleteVATGroup (vatGroupId);
            cache.Remove (vatGroupId);
        }

        public static LazyListModel<VATGroup> GetAll ()
        {
            return BusinessDomain.DataAccessProvider.GetAllVATGroups<VATGroup> ();
        }

        public static VATGroup GetById (long vatGroupId)
        {
            VATGroup ret = BusinessDomain.DataAccessProvider.GetVATGroupById<VATGroup> (vatGroupId);
            cache.Set (ret);
            return ret;
        }

        public static VATGroup GetByCode (string vatGroupCode)
        {
            VATGroup ret = BusinessDomain.DataAccessProvider.GetVATGroupByCode<VATGroup> (vatGroupCode);
            cache.Set (ret);
            return ret;
        }

        public static VATGroup GetByName (string vatGroupName)
        {
            VATGroup ret = BusinessDomain.DataAccessProvider.GetVATGroupByName<VATGroup> (vatGroupName);
            cache.Set (ret);
            return ret;
        }

        private static KeyValuePair<string, string> [] allCodes;
        public static KeyValuePair<string, string> [] AllCodes
        {
            get
            {
                return allCodes ?? (allCodes = new []
                    {
                        new KeyValuePair<string, string> (string.Empty, Translator.GetString ("Default")),
                        new KeyValuePair<string, string> ("A", "A"),
                        new KeyValuePair<string, string> ("B", "B"),
                        new KeyValuePair<string, string> ("C", "C"),
                        new KeyValuePair<string, string> ("D", "D"),
                        new KeyValuePair<string, string> ("E", "E"),
                        new KeyValuePair<string, string> ("F", "F"),
                        new KeyValuePair<string, string> ("G", "G"),
                        new KeyValuePair<string, string> ("H", "H"),
                        new KeyValuePair<string, string> ("I", "I"),
                        new KeyValuePair<string, string> ("J", "J"),
                        new KeyValuePair<string, string> ("K", "K"),
                        new KeyValuePair<string, string> ("L", "L"),
                        new KeyValuePair<string, string> ("M", "M"),
                        new KeyValuePair<string, string> ("N", "N"),
                        new KeyValuePair<string, string> ("O", "O"),
                        new KeyValuePair<string, string> ("P", "P"),
                        new KeyValuePair<string, string> ("Q", "Q"),
                        new KeyValuePair<string, string> ("R", "R"),
                        new KeyValuePair<string, string> ("S", "S"),
                        new KeyValuePair<string, string> ("Exempt", Translator.GetString ("Tax Exempt"))
                    });
            }
        }

        private static Dictionary<string, string> vatGroupCodeNames;
        public static string GetVATGroupCodeName (string code)
        {
            if (vatGroupCodeNames == null) {
                vatGroupCodeNames = new Dictionary<string, string> ();
                foreach (KeyValuePair<string, string> pair in AllCodes) {
                    vatGroupCodeNames.Add (pair.Key, pair.Value);
                }
            }

            string value;
            return vatGroupCodeNames.TryGetValue (code.Trim (), out value) ? value : string.Empty;
        }

        public bool Validate (ValidateCallback callback, StateHolder state)
        {
            if (callback == null)
                throw new ArgumentNullException ("callback");

            if (name.Length == 0) {
                if (!callback (Translator.GetString ("VAT group name cannot be empty!"), ErrorSeverity.Error, 0, state))
                    return false;
            }

            if (vatValue == double.MinValue || vatValue < 0 || vatValue >= 100) {
                if (!callback (string.Format (Translator.GetString ("VAT group \"{0}\" has invalid percent value!"), name),
                    ErrorSeverity.Error, 1, state))
                    return false;
            }

            VATGroup p = GetByName (name);
            if (p != null && p.Id != id) {
                if (!callback (string.Format (Translator.GetString ("VAT group with the name \"{0}\" already exists! Do you want to save the vat group anyway?"), name),
                    ErrorSeverity.Warning, 2, state))
                    return false;
            }

            p = GetByCode (code);
            if (p != null && p.Id != id) {
                if (!callback (string.Format (Translator.GetString ("VAT group with this code \"{0}\" already exists! Do you want to save the vat group anyway?"), code),
                    ErrorSeverity.Warning, 3, state))
                    return false;
            }

            return true;
        }

        #region Overrides of ICacheableEntityBase<VATGroup>

        public VATGroup GetEntityById (long entityId)
        {
            return GetById (entityId);
        }

        public VATGroup GetEntityByCode (string entityCode)
        {
            return GetByCode (entityCode);
        }

        public VATGroup GetEntityByName (string entityName)
        {
            return GetByName (entityName);
        }

        public IEnumerable<VATGroup> GetAllEntities ()
        {
            return GetAll ();
        }

        #endregion

        public object Clone ()
        {
            return MemberwiseClone ();
        }
    }
}
