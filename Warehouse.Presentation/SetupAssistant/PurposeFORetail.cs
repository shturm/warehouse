//
// PurposeFORetail.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   07.07.2011
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

using Gtk;
using Mono.Addins;
using Warehouse.Business;

namespace Warehouse.Presentation.SetupAssistant
{
    [Extension ("/Warehouse/Presentation/SetupAssistant/Purpose")]
    public class PurposeFORetail : PurposeBase
    {
        #region Implementation of IPurpose

        public override int Ordinal
        {
            get { return 20; }
        }

        protected override string Label
        {
            get { return Translator.GetString ("Front Office - Retail sales"); }
        }

        public override Image Image
        {
            get { return FormHelper.LoadLocalImage ("Warehouse.Presentation.SetupAssistant.Icons.RetailPurpose170.png"); }
        }

        public override void Select ()
        {
            BusinessDomain.AppConfiguration.StartupPageClass = typeof (WbpTradePoint).FullName;
        }

        #endregion
    }
}
