//
// EditNewUser.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
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
using System.Collections.Generic;
using System.Timers;
using Glade;
using GLib;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Data.Model;
using Warehouse.Presentation.Widgets;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNewUser : DialogBase
    {
        private User user;
        private UsersGroupsEditPanel gEditPanel;
        private bool passwordChanged;
        private long? defaultGroupId;

        private Location lockedLocation;
        private Partner lockedPartner;
        private Partner defaultPartner;
        private CompanyRecord defaultCompany;

        private string oldName;

        private readonly Timer cardNumberTimer;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEditNewUser;
        [Widget]
        private Button btnSaveAndNew;
        [Widget]
        private Button btnSave;
        [Widget]
        private Button btnCancel;

        #region Basic Info

        [Widget]
        private Label lblBasicInfoTab;
        [Widget]
        private Label lblCode;
        [Widget]
        private Entry txtCode;
        [Widget]
        private Button btnGenerateCode;
        [Widget]
        private Label lblName;
        [Widget]
        private Entry txtName;
        [Widget]
        private Label lblDisplayName;
        [Widget]
        private Entry txtDisplayName;
        [Widget]
        private Label lblPassword1;
        [Widget]
        private Entry txtPassword1;
        [Widget]
        private Label lblPassword2;
        [Widget]
        private Entry txtPassword2;
        [Widget]
        private Label lblAccessLevel;
        [Widget]
        private ComboBox cboAccessLevel;
        [Widget]
        private Label lblCardNo;
        [Widget]
        private Entry txtCardNo;

        #endregion

        #region Additional Info

        [Widget]
        private Label lblAditionalInfoTab;
        [Widget]
        private Label lblPartner;
        [Widget]
        private Entry txtPartner;
        [Widget]
        private Button btnPartner;
        [Widget]
        private Label lblDefaultPartner;
        [Widget]
        private Entry txtDefaultPartner;
        [Widget]
        private Button btnDefaultPartner;
        [Widget]
        private Label lblLocation;
        [Widget]
        private Entry txtLocation;
        [Widget]
        private Button btnLocation;
        [Widget]
        private Label lblCompany;
        [Widget]
        private Entry txtCompany;
        [Widget]
        private Button btnCompany;
        [Widget]
        private CheckButton chkShowItemsPurchasePrice;
        [Widget]
        private CheckButton chkShowItemsAvailability;
        [Widget]
        private CheckButton chkAllowZeroPrices;

        #endregion

        #region Groups

        [Widget]
        private Label lblGroupsTab;
        [Widget]
        private Alignment algGroups;

        #endregion

#pragma warning restore 649

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgEditNewUser; }
        }

        public EditNewUser (User user, long? defaultGroupId = null)
        {
            passwordChanged = false;
            this.user = user;
            this.defaultGroupId = defaultGroupId;
            cardNumberTimer = new Timer (200) { AutoReset = false };

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditNewUser.glade", "dlgEditNewUser");
            form.Autoconnect (this);

            dlgEditNewUser.Icon = FormHelper.LoadImage ("Icons.User16.png").Pixbuf;
            btnSaveAndNew.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnSave.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            gEditPanel = new UsersGroupsEditPanel ();
            algGroups.Add (gEditPanel);
            gEditPanel.Show ();

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeEntries ();

            txtPartner.Changed += (sender, args) => { lockedPartner = null; };
            txtPartner.ButtonPressEvent += txtPartner_ButtonPressEvent;
            txtPartner.KeyPressEvent += txtPartner_KeyPressEvent;
            btnPartner.Clicked += (sender, args) => ChangePartner ();

            txtDefaultPartner.Changed += (sender, args) => { defaultPartner = null; };
            txtDefaultPartner.ButtonPressEvent += txtDefaultPartner_ButtonPressEvent;
            txtDefaultPartner.KeyPressEvent += txtDefaultPartner_KeyPressEvent;
            btnDefaultPartner.Clicked += (sender, args) => ChangeDefaultPartner ();

            txtLocation.Changed += (sender, args) => { lockedLocation = null; };
            txtLocation.ButtonPressEvent += txtLocation_ButtonPressEvent;
            txtLocation.KeyPressEvent += txtLocation_KeyPressEvent;
            btnLocation.Clicked += (sender, args) => ChangeLocation ();

            txtCompany.Changed += (sender, args) => { defaultCompany = null; };
            txtCompany.ButtonPressEvent += txtCompany_ButtonPressEvent;
            txtCompany.KeyPressEvent += txtCompany_KeyPressEvent;
            btnCompany.Clicked += (sender, args) => ChangeCompany ();

            txtPassword1.Changed += txtPassword_Changed;
            txtPassword2.Changed += txtPassword_Changed;
            btnGenerateCode.Clicked += btnGenerateCode_Clicked;
            dlgEditNewUser.Shown += dlgEditNewUser_Shown;

            oldName = txtName.Text;
            txtName.Changed += txtName_Changed;
            txtCardNo.Changed += txtCardNo_Changed;
            PresentationDomain.CardRecognized += PresentationDomain_CardRecognized;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgEditNewUser.Title = user != null ?
                Translator.GetString ("Edit User") : Translator.GetString ("New User");

            lblBasicInfoTab.SetText (Translator.GetString ("General information"));
            lblCode.SetText (Translator.GetString ("Code:"));
            lblName.SetText (Translator.GetString ("Name:"));
            lblDisplayName.SetText (Translator.GetString ("Display name:"));
            lblPassword1.SetText (Translator.GetString ("Password:"));
            lblPassword2.SetText (Translator.GetString ("Confirm password:"));
            lblAccessLevel.SetText (Translator.GetString ("Access level:"));
            lblCardNo.SetText (Translator.GetString ("Card number:"));

            lblAditionalInfoTab.SetText (Translator.GetString ("Additional information"));
            lblPartner.SetText (Translator.GetString ("Locked partner:"));
            lblDefaultPartner.SetText (Translator.GetString ("Default partner:"));
            lblLocation.SetText (Translator.GetString ("Locked location:"));
            lblCompany.SetText (Translator.GetString ("Default company:"));
            chkShowItemsPurchasePrice.Label = Translator.GetString ("Show items purchase price");
            chkShowItemsAvailability.Label = Translator.GetString ("Show items available quantity");
            chkAllowZeroPrices.Label = Translator.GetString ("Allow sales using zero prices");

            lblGroupsTab.SetText (Translator.GetString ("Groups"));

            btnSaveAndNew.SetChildLabelText (user != null ?
                Translator.GetString ("Save as New", "User") : Translator.GetString ("Save and New", "User"));
            btnSave.SetChildLabelText (Translator.GetString ("Save"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        protected virtual void InitializeEntries ()
        {
            LazyListModel<User> allUsers = User.GetAll ();
            List<KeyValuePair<int, string>> levels = new List<KeyValuePair<int, string>> (User.GetAllAccessLevels ());

            if (user == null) {
                user = new User { Password = string.Empty };
                // If we are creating the first user after the default user then he can only be owner
                if (allUsers.Count <= 1)
                    ClearAllButOwner (levels);
                else
                    ClearUnavailableAccessLevels (levels);

                cboAccessLevel.Load (levels, "Key", "Value", (int) UserAccessLevel.Operator);

                if (defaultGroupId.HasValue)
                    gEditPanel.SelectGroupId ((int) defaultGroupId);

                if (BusinessDomain.AppConfiguration.AutoGenerateUserCodes)
                    user.AutoGenerateCode ();
            } else {
                // If we are editing the first user after the default user then he can only be owner
                if (allUsers.Count <= 2)
                    ClearAllButOwner (levels);
                else
                    ClearUnavailableAccessLevels (levels);

                cboAccessLevel.Load (levels, "Key", "Value", (int) user.UserLevel);
                gEditPanel.SelectGroupId (user.GroupId);
            }

            txtName.Text = user.Name;
            txtDisplayName.Text = user.Name2;
            txtPassword1.Text = "******";
            txtPassword2.Text = "******";
            txtCardNo.Text = user.CardNo;
            txtCode.Text = user.Code;

            if (user.LockedLocationId > 0) {
                lockedLocation = Location.GetById (user.LockedLocationId);
                if (lockedLocation != null)
                    txtLocation.Text = lockedLocation.Name;
            }

            if (user.LockedPartnerId > 0) {
                lockedPartner = Partner.GetById (user.LockedPartnerId);
                if (lockedPartner != null)
                    txtPartner.Text = lockedPartner.Name;
            }

            if (user.DefaultPartnerId > 0) {
                defaultPartner = Partner.GetById (user.DefaultPartnerId);
                if (defaultPartner != null)
                    txtDefaultPartner.Text = defaultPartner.Name;
            }

            if (user.DefaultCompanyId > 0) {
                defaultCompany = CompanyRecord.GetById (user.DefaultCompanyId);
                if (defaultCompany != null)
                    txtCompany.Text = defaultCompany.Name;
            }

            chkShowItemsPurchasePrice.Active = !user.HideItemsPurchasePrice;
            chkShowItemsAvailability.Active = !user.HideItemsAvailability;
            chkAllowZeroPrices.Active = user.AllowZeroPrices;
        }

        private static void ClearAllButOwner (IList<KeyValuePair<int, string>> levels)
        {
            for (int i = levels.Count - 1; i >= 0; i--)
                if (levels [i].Key != (int) UserAccessLevel.Owner)
                    levels.RemoveAt (i);
        }

        private void ClearUnavailableAccessLevels (IList<KeyValuePair<int, string>> levels)
        {
            User loggedUser = BusinessDomain.LoggedUser;
            int maxLevel = (int) (loggedUser.IsSaved ? loggedUser.UserLevel : UserAccessLevel.Owner);
            // Allow Owners to edit all access levels and others to edit themselves
            if (maxLevel == (int) UserAccessLevel.Owner || (loggedUser.IsSaved && user.Id == loggedUser.Id))
                maxLevel++;

            for (int i = levels.Count - 1; i >= 0; i--)
                if (levels [i].Key >= maxLevel)
                    levels.RemoveAt (i);
        }

        private void btnGenerateCode_Clicked (object sender, EventArgs e)
        {
            user.AutoGenerateCode ();
            txtCode.Text = user.Code;
        }

        private void dlgEditNewUser_Shown (object sender, EventArgs e)
        {
            if (BusinessDomain.AppConfiguration.AutoGenerateUserCodes)
                txtName.GrabFocus ();
            else
                txtCode.GrabFocus ();
        }

        private void txtName_Changed (object sender, EventArgs e)
        {
            if (txtDisplayName.Text == oldName)
                txtDisplayName.Text = txtName.Text;

            oldName = txtName.Text;
        }

        private void txtPassword_Changed (object sender, EventArgs e)
        {
            passwordChanged = true;
        }

        private void txtCardNo_Changed (object sender, EventArgs e)
        {
            cardNumberTimer.Stop ();
            cardNumberTimer.Start ();
        }

        private void PresentationDomain_CardRecognized (object sender, CardReadArgs e)
        {
            txtCardNo.Text = e.CardId;
        }

        public User GetUser ()
        {
            user.Code = txtCode.Text.Trim ();
            user.Name = txtName.Text.Trim ();
            user.Name2 = txtDisplayName.Text.Trim ();
            if (passwordChanged)
                user.Password = txtPassword1.Text.Trim ();

            user.UserLevel = (UserAccessLevel) cboAccessLevel.GetSelectedValue ();
            user.CardNo = txtCardNo.Text.Trim ();

            if (lockedLocation == null) {
                if (!string.IsNullOrWhiteSpace (txtLocation.Text)) {
                    Location resolved = Location.GetByName (txtLocation.Text);
                    if (resolved != null)
                        EvaluateLocation (resolved);
                }
            }
            user.LockedLocationId = lockedLocation != null ? lockedLocation.Id : -1;

            if (defaultPartner == null) {
                if (!string.IsNullOrWhiteSpace (txtDefaultPartner.Text)) {
                    Partner resolved = Partner.GetByName (txtDefaultPartner.Text);
                    if (resolved != null)
                        EvaluateDefaultPartner (resolved);
                }
            }
            user.DefaultPartnerId = defaultPartner != null ? defaultPartner.Id : -1;

            if (lockedPartner == null) {
                if (!string.IsNullOrWhiteSpace (txtPartner.Text)) {
                    Partner resolved = Partner.GetByName (txtPartner.Text);
                    if (resolved != null)
                        EvaluatePartner (resolved);
                }
            }
            user.LockedPartnerId = lockedPartner != null ? lockedPartner.Id : -1;

            if (defaultCompany == null) {
                if (!string.IsNullOrWhiteSpace (txtCompany.Text)) {
                    CompanyRecord resolved = CompanyRecord.GetByName (txtCompany.Text);
                    if (resolved != null)
                        EvaluateCompany (resolved);
                }
            }
            user.DefaultCompanyId = defaultCompany != null ? defaultCompany.Id : -1;

            user.HideItemsPurchasePrice = !chkShowItemsPurchasePrice.Active;
            user.HideItemsAvailability = !chkShowItemsAvailability.Active;
            user.AllowZeroPrices = chkAllowZeroPrices.Active;

            long selectedGroupId = gEditPanel.GetSelectedGroupId ();
            if (selectedGroupId == PartnersGroup.DeletedGroupId)
                user.Deleted = true;
            else
                user.GroupId = selectedGroupId;

            return user;
        }

        private bool Validate ()
        {
            if (!GetUser ().Validate ((message, severity, code, state) =>
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
                }, null))
                return false;

            if (txtPassword1.Text != txtPassword2.Text) {
                MessageError.ShowDialog (Translator.GetString ("Wrong password confirmation!"));
                return false;
            }

            return true;
        }

        #region Event handling

        [ConnectBefore]
        private void txtPartner_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type == Gdk.EventType.TwoButtonPress)
                ChangePartner ();
        }

        [ConnectBefore]
        private void txtPartner_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                ChangePartner ();
        }

        private void ChangePartner ()
        {
            using (ChooseEditPartner dialog = new ChooseEditPartner (true, lockedPartner != null ? lockedPartner.Id : (long?) null)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                Partner [] partners = dialog.SelectedItems;
                if (partners.Length == 0)
                    return;

                EvaluatePartner (partners [0]);
            }

            txtPartner.GrabFocus ();
        }

        private void EvaluatePartner (Partner newPartner)
        {
            txtPartner.Text = newPartner.Name;
            if (lockedPartner != null && newPartner.Id == lockedPartner.Id)
                return;

            lockedPartner = newPartner;
        }

        [ConnectBefore]
        private void txtDefaultPartner_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type == Gdk.EventType.TwoButtonPress)
                ChangeDefaultPartner ();
        }

        [ConnectBefore]
        private void txtDefaultPartner_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                ChangeDefaultPartner ();
        }

        private void ChangeDefaultPartner ()
        {
            using (ChooseEditPartner dialog = new ChooseEditPartner (true, defaultPartner != null ? defaultPartner.Id : (long?) null)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                Partner [] partners = dialog.SelectedItems;
                if (partners.Length == 0)
                    return;

                EvaluateDefaultPartner (partners [0]);
            }

            txtDefaultPartner.GrabFocus ();
        }

        private void EvaluateDefaultPartner (Partner newDefaultPartner)
        {
            txtDefaultPartner.Text = newDefaultPartner.Name;
            if (defaultPartner != null && newDefaultPartner.Id == defaultPartner.Id)
                return;

            defaultPartner = newDefaultPartner;
        }

        [ConnectBefore]
        private void txtLocation_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type == Gdk.EventType.TwoButtonPress)
                ChangeLocation ();
        }

        [ConnectBefore]
        private void txtLocation_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                ChangeLocation ();
        }

        private void ChangeLocation ()
        {
            using (ChooseEditLocation dialog = new ChooseEditLocation (true, lockedLocation != null ? lockedLocation.Id : (long?) null)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                Location [] locations = dialog.SelectedItems;
                if (locations.Length == 0)
                    return;

                EvaluateLocation (locations [0]);
            }

            txtLocation.GrabFocus ();
        }

        private void EvaluateLocation (Location newLocation)
        {
            txtLocation.Text = newLocation.Name;
            if (lockedLocation != null && newLocation.Id == lockedLocation.Id)
                return;

            lockedLocation = newLocation;
        }

        [ConnectBefore]
        private void txtCompany_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type == Gdk.EventType.TwoButtonPress)
                ChangeCompany ();
        }

        [ConnectBefore]
        private void txtCompany_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                ChangeCompany ();
        }

        private void ChangeCompany ()
        {
            using (ChooseEditCompanyRecord dialog = new ChooseEditCompanyRecord (true)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                CompanyRecord [] companyRecords = dialog.SelectedItems;
                if (companyRecords.Length == 0)
                    return;

                EvaluateCompany (companyRecords [0]);
            }

            txtCompany.GrabFocus ();
        }

        private void EvaluateCompany (CompanyRecord newCompany)
        {
            txtCompany.Text = newCompany.Name;
            if (defaultCompany != null && newCompany.Id == defaultCompany.Id)
                return;

            defaultCompany = newCompany;
        }

        protected override void OnClosing ()
        {
            PresentationDomain.CardRecognized -= PresentationDomain_CardRecognized;
            cardNumberTimer.Dispose ();
        }

        [UsedImplicitly]
        protected void btnSaveAndNew_Clicked (object o, EventArgs args)
        {
            long oldId = user.Id;
            user.Id = -1;
            if (!Validate ()) {
                user.Id = oldId;
                return;
            }

            var saved = GetUser ().CommitChanges ();
            if (oldId > 0) {
                dlgEditNewUser.Respond (ResponseType.Ok);
                return;
            }

            user = null;
            InitializeEntries ();

            txtName.Text = saved.Name;
        }

        [UsedImplicitly]
        protected void btnSave_Clicked (object o, EventArgs args)
        {
            // if the timer is still running then we are most probably here because of a new line in the input of a card
            if (cardNumberTimer.Enabled) {
                txtCardNo.GrabFocus ();
                return;
            }

            if (!Validate ())
                return;

            dlgEditNewUser.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditNewUser.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
