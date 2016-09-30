//
// ChooseMessage.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   10.26.2009
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
using Warehouse.Component;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseMessage : DialogBase
    {
        #region Glade Widgets

        [Widget]
        protected Dialog dlgChooseMessage;
        [Widget]
        protected Label lblEmail;
        [Widget]
        protected Entry txtEmail;
        [Widget]
        protected Label lblEnterMessage;
        [Widget]
        protected TextView txtViewMessage;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgChooseMessage; }
        }

        public string Email
        {
            get { return txtEmail.Text.Trim (); }
        }

        public string Message
        {
            get { return txtViewMessage.Buffer.Text.Trim (); }
        }

        public ChooseMessage (string message)
        {
            Initialize ();

            txtViewMessage.Buffer.Text = message;
        }

        public ChooseMessage (string emailAddress, string message) :
            this (message)
        {
            txtEmail.Text = emailAddress;

            lblEmail.Visible = true;
            txtEmail.Visible = true;
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChooseMessage.glade", "dlgChooseMessage");
            form.Autoconnect (this);

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            base.InitializeForm ();
            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            lblEmail.SetText (Translator.GetString ("E-mail:"));
            lblEnterMessage.SetText (Translator.GetString ("Message:"));

            dlgChooseMessage.Title = Translator.GetString ("Enter Message");
        }

        private bool Validate ()
        {
            if (txtEmail.Visible && !Validator.CheckEmail (txtEmail.Text.Trim ())) {
                MessageError.ShowDialog (Translator.GetString ("The entered e-mail address is invalid."));
                txtEmail.GrabFocus ();
                return false;
            }
            return true;
        }

        #region Event handling

        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (!Validate ())
                return;
            
            dlgChooseMessage.Respond (ResponseType.Ok);
        }

        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgChooseMessage.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
