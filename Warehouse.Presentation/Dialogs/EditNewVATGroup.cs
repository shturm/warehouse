//
// EditNewVATGroup.cs
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
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNewVATGroup : DialogBase
    {
        private VATGroup vatGroup;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEditNewVATGroup;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

        [Widget]
        private Label lblCode;
        [Widget]
        private Label lblName;
        [Widget]
        private Label lblValue;

        [Widget]
        private ComboBox cboCode;
        [Widget]
        private Entry txtName;
        [Widget]
        private Entry txtValue;

#pragma warning restore 649

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgEditNewVATGroup; }
        }

        public EditNewVATGroup (VATGroup vatGroup)
        {
            this.vatGroup = vatGroup;

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditNewVATGroup.glade", "dlgEditNewVATGroup");
            form.Autoconnect (this);

            dlgEditNewVATGroup.Icon = FormHelper.LoadImage ("Icons.VATGroup16.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeEntries ();
            dlgEditNewVATGroup.Shown += dlgEditNewVATGroup_Shown;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            if (BusinessDomain.AppConfiguration.UseSalesTaxInsteadOfVAT) {
                dlgEditNewVATGroup.Title = vatGroup != null ?
                    Translator.GetString ("Edit Tax Group") : Translator.GetString ("New Tax Group");
            } else {
                dlgEditNewVATGroup.Title = vatGroup != null ?
                    Translator.GetString ("Edit VAT Group") : Translator.GetString ("New VAT Group");
            }

            lblName.SetText (Translator.GetString ("Name:"));
            lblCode.SetText (Translator.GetString ("Code:"));
            lblValue.SetText (Translator.GetString ("Value:"));

            btnOK.SetChildLabelText (Translator.GetString ("Save"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        protected virtual void InitializeEntries ()
        {
            if (vatGroup == null) {
                vatGroup = new VATGroup ();
            } else {
                txtName.Text = vatGroup.Name;
                txtValue.Text = Percent.ToEditString (vatGroup.VatValue);
            }

            cboCode.Load (VATGroup.AllCodes, "Key", "Value", vatGroup.Code);
        }

        private void dlgEditNewVATGroup_Shown (object sender, EventArgs e)
        {
            txtName.GrabFocus ();
        }

        public VATGroup GetVATGroup ()
        {
            vatGroup.Name = txtName.Text.Trim ();
            vatGroup.Code = (string) cboCode.GetSelectedValue ();
            vatGroup.VatValue = Percent.ParseExpression (txtValue.Text);

            return vatGroup;
        }

        private bool Validate ()
        {
            if (!Percent.IsValidExpression (txtValue.Text)) {
                MessageError.ShowDialog (Translator.GetString ("Invalid percent value!"));
                txtValue.GrabFocus ();
                return false;
            }

            return GetVATGroup ().Validate ((message, severity, code, state) =>
                {
                    using (MessageError dlgError = new MessageError (message, severity))
                        if (severity == ErrorSeverity.Warning) {
                            dlgError.Buttons = MessageButtons.YesNo;
                            if (dlgError.Run () != ResponseType.Yes)
                                return false;
                        } else {
                            dlgError.Run ();
                            return false;
                        }

                    return true;
                }, null);
        }

        #region Event handling

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (!Validate ())
                return;

            dlgEditNewVATGroup.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditNewVATGroup.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
