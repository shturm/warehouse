//
// AddDiscount.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/16/2006
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
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Presentation.Widgets;

namespace Warehouse.Presentation.Dialogs
{
    public class AddDiscount : DialogBase
    {
        private readonly bool percentsOnly;
        private readonly double initialDiscount;
        private ChooseNumberPanel pnlChooseNumber;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        protected Dialog dlgAddDiscount;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

        [Widget]
        protected HBox hboDiscounts;
        [Widget]
        protected RadioButton rbnPercentDiscount;
        [Widget]
        protected RadioButton rbnValueDiscount;

        [Widget]
        private Label lblValue;
        [Widget]
        private Entry txtValue;
        [Widget]
        private Alignment algKeypad;

        [Widget]
        private ToggleButton btnChoose;

#pragma warning restore 649

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgAddDiscount; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        public double? PercentDiscount
        {
            get
            {
                return rbnPercentDiscount.Active ? Math.Min (Math.Max (0, Percent.ParseExpression (txtValue.Text)), 100) : (double?) null;
            }
        }

        public double? ValueDiscount
        {
            get
            {
                return rbnValueDiscount.Active ? Currency.ParseExpression (txtValue.Text) : (double?) null;
            }
        }

        public AddDiscount (bool percentsOnly = false, double initialDiscount = 0d, bool openKeypad = false)
        {
            this.percentsOnly = percentsOnly;
            this.initialDiscount = initialDiscount;

            Initialize ();

            NumberFormatInfo numberFormat = new NumberFormatInfo { NumberDecimalSeparator = ".", NumberGroupSeparator = string.Empty };
            txtValue.Text = initialDiscount.ToString (numberFormat);
            txtValue.GrabFocus ();

            if (openKeypad)
                btnChoose.Active = true;
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.AddDiscount.glade", "dlgAddDiscount");
            form.Autoconnect (this);

            dlgAddDiscount.Icon = FormHelper.LoadImage ("Icons.Discount24.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            if (percentsOnly) {
                rbnPercentDiscount.Active = true;
                hboDiscounts.Visible = false;
            } else {
                if (!BusinessDomain.AppConfiguration.AllowPercentDiscounts) {
                    rbnPercentDiscount.Sensitive = false;
                    rbnValueDiscount.Active = true;
                }

                if (!BusinessDomain.AppConfiguration.AllowValueDiscounts)
                    rbnValueDiscount.Sensitive = false;
            }

            pnlChooseNumber = new ChooseNumberPanel (initialDiscount, txtValue, true, false);
            pnlChooseNumber.Show ();
            algKeypad.Add (pnlChooseNumber);

            base.InitializeForm ();

            InitializeFormStrings ();

            btnChoose.Toggled += btnChoose_Toggled;
            btnChoose_Toggled (null, null);
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgAddDiscount.Title = Translator.GetString ("Add Discount");

            lblValue.SetText (Translator.GetString ("Amount:"));

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            rbnPercentDiscount.Label = Translator.GetString ("Percent discount");
            rbnValueDiscount.Label = Translator.GetString ("Value discount");
        }

        #region Event handling

        private void btnChoose_Toggled (object sender, EventArgs e)
        {
            algKeypad.Visible = btnChoose.Active;
            dlgAddDiscount.Resize (10, 10);
            txtValue.GrabFocus ();
            txtValue.SelectRegion (txtValue.Text.Length, txtValue.Text.Length);
        }

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            dlgAddDiscount.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgAddDiscount.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
