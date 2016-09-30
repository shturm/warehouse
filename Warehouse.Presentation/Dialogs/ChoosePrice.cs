//
// ChoosePrice.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   10.30.2009
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
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Data.Calculator;

namespace Warehouse.Presentation.Dialogs
{
    public class ChoosePrice : DialogBase
    {
        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgChoosePrice;
        [Widget]
        private RadioButton rbValue;
        [Widget]
        private Entry txtValue;
        [Widget]
        private RadioButton rbPriceGroup;
        [Widget]
        private ComboBox cboPriceGroup;
        [Widget]
        private ComboBox cboArithmeticOperations;
        [Widget]
        private Entry txtPriceGroupValue;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

#pragma warning restore 649

        #endregion

        private bool isPriceGroupSelected;

        public override Dialog DialogControl
        {
            get { return dlgChoosePrice; }
        }

        public double Value
        {
            get { return Currency.ParseExpression (txtValue.Text); }
        }

        public bool IsPriceGroupSelected
        {
            get { return isPriceGroupSelected; }
        }

        public PriceGroup PriceGroup
        {
            get { return (PriceGroup) cboPriceGroup.GetSelectedValue (); }
        }

        public OperatorType ArithmeticOperation
        {
            get { return (OperatorType) cboArithmeticOperations.GetSelectedValue (); }
        }

        public double PriceGroupValue
        {
            get { return Number.ParseExpression (txtPriceGroupValue.Text); }
        }

        public ChoosePrice ()
        {
            Initialize ();
        }

        public ChoosePrice (double value)
            : this ()
        {
            txtValue.Text = Currency.ToEditString (value);
        }

        public ChoosePrice (PriceGroup priceGroup, OperatorType operatorType, double value)
            : this ()
        {
            rbPriceGroup.Active = true;
            cboPriceGroup.SetSelection ((int) priceGroup);
            cboArithmeticOperations.SetSelection ((int) operatorType);
            txtPriceGroupValue.Text = Number.ToEditString (value);
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChoosePrice.glade", "dlgChoosePrice");
            form.Autoconnect (this);

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            rbValue.Toggle ();

            cboPriceGroup.Load (Currency.GetAllPriceRulePriceGroups (), "Key", "Value");
            LoadArithmeticOperations ();

            base.InitializeForm ();
            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            rbValue.Label = Translator.GetString ("Value");
            rbPriceGroup.Label = Translator.GetString ("Price Group");

            dlgChoosePrice.Title = Translator.GetString ("Choose Price");
        }

        private void LoadArithmeticOperations ()
        {
            KeyValuePair<string, OperatorType> [] arithmeticOperations =
                {
                    new KeyValuePair<string, OperatorType> ("+", OperatorType.Plus), 
                    new KeyValuePair<string, OperatorType> ("-", OperatorType.Minus), 
                    new KeyValuePair<string, OperatorType> ("*", OperatorType.Multiply), 
                    new KeyValuePair<string, OperatorType> ("/", OperatorType.Divide)
                };
            cboArithmeticOperations.Load (arithmeticOperations, "Value", "Key");
        }

        private bool Validate ()
        {
            if (rbPriceGroup.Active && ArithmeticOperation == OperatorType.Divide && PriceGroupValue.IsZero ()) {
                MessageError.ShowDialog (Translator.GetString ("Division by zero is impossible."));
                txtPriceGroupValue.GrabFocus ();
                return false;
            }
            return true;
        }

        #region Event handling

        [UsedImplicitly]
        protected void OnRbValueToggled (object o, EventArgs args)
        {
            txtValue.Sensitive = rbValue.Active;
            isPriceGroupSelected = !rbValue.Active;
            cboPriceGroup.Sensitive = !rbValue.Active;
            cboArithmeticOperations.Sensitive = !rbValue.Active;
            txtPriceGroupValue.Sensitive = !rbValue.Active;
        }

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (!Validate ())
                return;
            
            dlgChoosePrice.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgChoosePrice.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
