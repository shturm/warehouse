//
// PurposeCustom.cs
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
using Warehouse.Component;

namespace Warehouse.Presentation.SetupAssistant
{
    [Extension ("/Warehouse/Presentation/SetupAssistant/Purpose")]
    public class PurposeCustom : PurposeBase
    {
        private ComboBox cbo;

        #region Overrides of PurposeBase

        public override int Ordinal
        {
            get { return 30; }
        }

        protected override string Label
        {
            get { return Translator.GetString ("Other - Select start page:"); }
        }

        public override void Select ()
        {
            BusinessDomain.AppConfiguration.StartupPageClass = (string) cbo.GetSelectedValue ();
        }

        #endregion

        public override Widget GetSelectionWidget (ref RadioButton group)
        {
            HBox hbo = new HBox ();

            Widget ret = base.GetSelectionWidget (ref group);
            ret.Show ();
            hbo.PackStart (ret, false, true, 0);

            cbo = new ComboBox { Sensitive = false };
            cbo.Show ();
            cbo.Load (PresentationDomain.StartupPages, "ClassName", "Name");
            hbo.PackStart (cbo, false, true, 4);

            return hbo;
        }

        protected override void OnToggled (object sender, System.EventArgs e)
        {
            cbo.Sensitive = button.Active;

            base.OnToggled (sender, e);
        }
    }
}
