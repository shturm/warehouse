//
// UserLogin.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   09/20/2006
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
using System.Linq;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Presentation.Widgets;
using Alignment = Gtk.Alignment;

namespace Warehouse.Presentation.Dialogs
{
    public class UserLogin : DialogBase
    {
        private List<User> users;
        private ChooseNumberPanel pnlChooseNumber;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgUserLogin;
        [Widget]
        private Alignment algImage;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

        [Widget]
        private Label lblUserName;
        [Widget]
        private Label lblPassword;

        [Widget]
        private ComboBox cboUserName;
        [Widget]
        private Entry txtPassword;
        [Widget]
        private ToggleButton btnChoose;
        [Widget]
        private Alignment algKeypad;

#pragma warning restore 649

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgUserLogin; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        public UserLogin ()
        {
            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.UserLogin.glade", "dlgUserLogin");
            form.Autoconnect (this);

            dlgUserLogin.Icon = FormHelper.LoadImage ("Icons.User32.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            Image image = FormHelper.LoadImage ("Images.Login61.png");
            image.Show ();
            algImage.Add (image);

            pnlChooseNumber = new ChooseNumberPanel (null, txtPassword, true, false);
            pnlChooseNumber.Show ();
            algKeypad.Add (pnlChooseNumber);

            dlgUserLogin.Realized += dlgLogin_Realized;
            cboUserName.Changed += cboUserName_Changed;
            btnChoose.Toggled += btnChoose_Toggled;

            btnChoose.Active = BusinessDomain.AppConfiguration.LastKeypadOnLoginActive;

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeEntries ();
            PresentationDomain.CardRecognized += PresentationDomain_CardRecognized;
        }

        private void dlgLogin_Realized (object sender, EventArgs e)
        {
            txtPassword.GrabFocus ();
        }

        private void cboUserName_Changed (object sender, EventArgs e)
        {
            txtPassword.GrabFocus ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgUserLogin.Title = Translator.GetString ("Login");

            lblUserName.SetText (Translator.GetString ("User:"));
            lblPassword.SetText (Translator.GetString ("Password:"));

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        protected virtual void InitializeEntries ()
        {
            users = new List<User> (User.GetAll ());

            // Remove the default user from the list if there are other users added
            if (users.Count > 1) {
                for (int i = users.Count - 1; i >= 0; i--) {
                    if (users [i].Id == User.DefaultId) {
                        users.RemoveAt (i);
                    }
                }
            }

            cboUserName.Load (users.Select (u => new KeyValuePair<long, string> (u.Id, u.Name)), "Key", "Value", BusinessDomain.AppConfiguration.LastLoggedUID);
            cboUserName.SetElipsize ();
        }

        #region Event handling

        protected override void OnClosing ()
        {
            PresentationDomain.CardRecognized -= PresentationDomain_CardRecognized;
        }

        private void PresentationDomain_CardRecognized (object sender, CardReadArgs e)
        {
            User user = User.GetByCard (e.CardId);

            if (user == null) {
                MessageError.ShowDialog (Translator.GetString ("Invalid card!"));
                txtPassword.GrabFocus ();
                return;
            }

            BusinessDomain.LoggedUser = user;
            BusinessDomain.AppConfiguration.LastLoggedUID = user.Id;

            dlgUserLogin.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            User selectedUser = users [cboUserName.GetSelection ()];
            User user = selectedUser;
            if (!user.CheckPassword (txtPassword.Text))
                user = string.IsNullOrWhiteSpace (txtPassword.Text) ? null : User.GetByCard (txtPassword.Text);

            if (user == null) {
                selectedUser.LogFailedLogin ();
                MessageError.ShowDialog (Translator.GetString ("Invalid user name or password!"));
                txtPassword.GrabFocus ();
                return;
            }

            BusinessDomain.LoggedUser = user;
            BusinessDomain.AppConfiguration.LastLoggedUID = user.Id;
            user.LogSuccessfulLogin ();

            dlgUserLogin.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgUserLogin.Respond (ResponseType.Cancel);
        }

        private void btnChoose_Toggled (object sender, EventArgs e)
        {
            algKeypad.Visible = btnChoose.Active;
            dlgUserLogin.Resize (10, 10);
            BusinessDomain.AppConfiguration.LastKeypadOnLoginActive = btnChoose.Active;
            txtPassword.GrabFocus ();
            txtPassword.SelectRegion (txtPassword.Text.Length, txtPassword.Text.Length);
        }

        #endregion
    }
}
