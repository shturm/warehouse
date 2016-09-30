//
// DesktopBusinessBaseProvider.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   10.23.2013
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
using Mono.Addins;
using Warehouse.Data;

namespace Warehouse.Business
{
    internal class DesktopBusinessBaseProvider : BusinessBaseProvider<ConfigurationHolder>
    {
        public override IList<IDataExtender> DataAccessExtenders
        {
            get
            {
                if (dataAccessExtenders == null) {
                    if (!AddinManager.IsInitialized)
                        return new List<IDataExtender> ();

                    dataAccessExtenders = AddinManager.GetExtensionNodes ("/Warehouse/Business/Extend")
                        .OfType<TypeExtensionNode> ().Select (n => n.CreateInstance ())
                        .OfType<IDataExtender> ().ToList ();
                }

                return dataAccessExtenders;
            }
        }
    }
}
