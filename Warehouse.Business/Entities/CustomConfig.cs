//
// CustomConfig.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   10.10.2013
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
using System.Globalization;
using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public class CustomConfig
    {
        public static DateTime LastUpdateFromSync { get; set; }

        static CustomConfig ()
        {
            LastUpdateFromSync = DateTime.MinValue;
        }

        public static void CommitChanges<T> (T [] configs, string category, string profile) where T : XmlEntityBase<T>
        {
//            if (configs.Length <= 2) {
//                ErrorHandling.LogError ("Saving too few custom config entries to db:" + configs.Length, ErrorSeverity.FatalError);
//#if DEBUG
//                T [] all = Get<T> (category, profile);
//                if (all.Length > configs.Length) {
//                    ErrorHandling.LogError ("Config entries in the database currently are:" + all.Length, ErrorSeverity.FatalError);
//                    System.Diagnostics.Debugger.Break ();
//                }
//#endif
//            }
            BusinessDomain.DataAccessProvider.AddUpdateCustomConfig (configs, category, profile);
        }

        public static void Delete (string category, string profile)
        {
            BusinessDomain.DataAccessProvider.DeleteCustomConfig (category, profile);
        }

        public static T [] Get<T> (string category, string profile) where T : XmlEntityBase<T>
        {
            T [] ret;
            try {
                ret = BusinessDomain.DataAccessProvider.GetCustomConfig<T> (category, profile);
            } catch (Exception ex) {
                ErrorHandling.LogException (ex, ErrorSeverity.FatalError);
                throw new DbConnectionLostException (ex);
            }

//            if (ret == null || ret.Length <= 2) {
//                ErrorHandling.LogError ("Read too few custom config entries from db: " + (ret != null ? ret.Length.ToString (CultureInfo.InvariantCulture) : "null"), ErrorSeverity.FatalError);
//#if DEBUG
//                System.Diagnostics.Debugger.Break ();
//#endif
//            }

            return ret ?? new T [0];
        }

        public static string [] GetAllProfiles (string category)
        {
            return BusinessDomain.DataAccessProvider.GetAllCustomConfigProfiles (category);
        }
    }
}
