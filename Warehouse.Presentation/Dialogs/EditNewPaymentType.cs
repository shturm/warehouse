//
// EditNewPaymentType.cs
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
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNewPaymentType : DialogBase
    {
        private PaymentType editedType;

        #region Glade Widgets

        [Widget]
        protected Dialog dlgEditNewPaymentType;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;

        [Widget]
        protected Label lblName;
        [Widget]
        protected Label lblType;

        [Widget]
        protected Entry txtName;
        [Widget]
        protected ComboBox cboType;

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgEditNewPaymentType; }
        }

        public override string HelpFile
        {
            get { return "ChooseEditPaymentType.html"; }
        }

        public EditNewPaymentType (PaymentType editedType)
        {
            this.editedType = editedType;

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditNewPaymentType.glade", "dlgEditNewPaymentType");
            form.Autoconnect (this);

            dlgEditNewPaymentType.Icon = FormHelper.LoadImage ("Icons.PaymentType16.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeEntries ();
            dlgEditNewPaymentType.Shown += dlgEditNewGroup_Shown;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgEditNewPaymentType.Title = editedType != null ?
                Translator.GetString ("Edit Payment Type") :
                Translator.GetString ("New Payment Type");

            lblName.SetText (Translator.GetString ("Name:"));
            lblType.SetText (Translator.GetString ("Type:"));

            btnOK.SetChildLabelText (Translator.GetString ("Save"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        protected virtual void InitializeEntries ()
        {
            if (editedType == null) {
                editedType = new PaymentType ();
            } else {
                txtName.Text = editedType.Name;
            }

            cboType.Load (PaymentType.GetAllBaseTypePairs (), "Key", "Value", (int) editedType.BaseType);
        }

        private void dlgEditNewGroup_Shown (object sender, EventArgs e)
        {
            txtName.GrabFocus ();
        }

        public PaymentType GetPaymentType ()
        {
            editedType.Name = txtName.Text;
            editedType.BaseType = (BasePaymentType) cboType.GetSelectedValue ();

            return editedType;
        }

        private bool Validate ()
        {
            string name = txtName.Text.Trim ();

            if (name.Length == 0) {
                MessageError.ShowDialog (Translator.GetString ("Payment type name cannot be empty!"));
                return false;
            }

            return true;
        }

        #region Event handling

        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (!Validate ())
                return;

            dlgEditNewPaymentType.Respond (ResponseType.Ok);
        }

        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditNewPaymentType.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
