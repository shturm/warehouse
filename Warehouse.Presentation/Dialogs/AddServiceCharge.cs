//
// AddServiceCharge.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   01.27.2011
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
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Item = Warehouse.Business.Entities.Item;

namespace Warehouse.Presentation.Dialogs
{
    public class AddServiceCharge : DialogBase
    {
        #region Glade Widgets

        [Widget]
        protected Dialog dlgAddServiceCharge;
        [Widget]
        protected Label lblItem;
        [Widget]
        protected Entry txtItem;
        [Widget]
        protected Button chooseItem;
        [Widget]
        protected Label lblPercent;
        [Widget]
        protected SpinButton sbAmount;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgAddServiceCharge; }
        }

        public Item Item
        {
            get; private set;
        }

        public double Amount
        {
            get
            {
                return sbAmount.Value;
            }
        }
        
        public AddServiceCharge ()
        {
            Initialize ();
        }

        public AddServiceCharge (Item item, double initialServiceCharge)
            : this ()
        {
            Item = item;
            if (Item != null)
                txtItem.Text = Item.Name;

            sbAmount.Value = initialServiceCharge;
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.AddServiceCharge.glade", "dlgAddServiceCharge");
            form.Autoconnect (this);

            dlgAddServiceCharge.Icon = FormHelper.LoadImage ("Icons.PriceRules16.png").Pixbuf;

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();

            InitializeFormStrings ();

            sbAmount.Value = 0;
            sbAmount.Digits = (uint) BusinessDomain.AppConfiguration.PercentPrecision;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgAddServiceCharge.Title = Translator.GetString ("Service Charge");

            lblItem.SetText (Translator.GetString ("Item:"));
            lblPercent.SetText (Translator.GetString ("Percent:"));

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        #region Event handling

        [UsedImplicitly]
        protected void ChooseItem_Clicked (object o, EventArgs args)
        {
            using (ChooseEditItem dialog = new ChooseEditItem (true, Item != null && Item.Name == txtItem.Text ? string.Empty : txtItem.Text)) {
                if (dialog.Run () != ResponseType.Ok || dialog.SelectedItems.Length <= 0)
                    return;

                Item = dialog.SelectedItems [0];
                txtItem.Text = Item.Name;
                sbAmount.GrabFocus ();
            }
        }

        [UsedImplicitly]
        protected void btnChoose_Clicked (object o, EventArgs args)
        {
            using (ChooseNumber dialog = new ChooseNumber (sbAmount.Value)) {
                if (dialog.Run () != ResponseType.Ok) {
                    sbAmount.GrabFocus ();
                    return;
                }

                sbAmount.Value = dialog.GetNumber ();
            }
        }

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            Item = Business.Entities.Item.GetByAny (txtItem.Text);
            if (Item == null) {
                MessageError.ShowDialog (Translator.GetString ("Please select a valid item."), ErrorSeverity.Error);
                txtItem.GrabFocus ();
                return;
            }
            dlgAddServiceCharge.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgAddServiceCharge.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
