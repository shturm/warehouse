//
// Program.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   08/17/2006
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
using Warehouse.Business;
using Warehouse.Data;
using Warehouse.Presentation;
using WarehouseOpen.Resources;
#if !DEBUG
using System.Diagnostics;
#endif

namespace WarehouseOpen
{
    public class Program
    {
        [STAThread]
        public static void Main (string [] args)
        {
            DataHelper.ProductName = "Warehouse Open";
            DataHelper.ProductSupportEmail = "support@microinvest.net";
            DataHelper.CompanyName = "Microinvest";
            DataHelper.CompanyAddress = "Tsar Boris III 215, floor 12, Sofia, Bulgaria";
            DataHelper.CompanyWebSite = "http://www.microinvest.net";
            BusinessDomain.WorkflowManager = new WorkflowManager ();

            PresentationDomain.Run (new ResourceHandler (), "#707070");
        }
    }
}