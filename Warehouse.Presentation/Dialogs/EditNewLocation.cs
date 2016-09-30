//
// EditNewLocation.cs
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
using Warehouse.Presentation.Widgets;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNewLocation : DialogBase
    {
        private Location location;
        private LocationsGroupsEditPanel gEditPanel;
        private long? defaultGroupId;

        private string oldName;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Label lblBasicInfoTab;
        [Widget]
        private Dialog dlgEditNewLocation;
        [Widget]
        private Button btnSaveAndNew;
        [Widget]
        private Button btnSave;
        [Widget]
        private Button btnCancel;

        [Widget]
        private Label lblCode;
        [Widget]
        private Button btnGenerateCode;
        [Widget]
        private Label lblName;
        [Widget]
        private Label lblDisplayName;
        [Widget]
        private Label lblPriceGroup;

        [Widget]
        private Entry txtName;
        [Widget]
        private Entry txtDisplayName;
        [Widget]
        private Entry txtCode;
        [Widget]
        private ComboBox cboPriceGroup;

        #region Groups

        [Widget]
        private Label lblGroupsTab;
        [Widget]
        private Alignment algGroups;

        #endregion

#pragma warning restore 649

        #endregion

        public override string HelpFile
        {
            get { return "EditNewPOS.html"; }
        }

        public override Dialog DialogControl
        {
            get { return dlgEditNewLocation; }
        }

        public EditNewLocation (Location location, long? defaultGroupId = null)
        {
            this.location = location;
            this.defaultGroupId = defaultGroupId;

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditNewLocation.glade", "dlgEditNewLocation");
            form.Autoconnect (this);

            dlgEditNewLocation.Icon = FormHelper.LoadImage ("Icons.Location16.png").Pixbuf;
            btnSaveAndNew.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnSave.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            gEditPanel = new LocationsGroupsEditPanel ();
            algGroups.Add (gEditPanel);
            gEditPanel.Show ();

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeEntries ();
            btnGenerateCode.Clicked += btnGenerateCode_Clicked;
            dlgEditNewLocation.Shown += dlgEditNewLocation_Shown;

            oldName = txtName.Text;
            txtName.Changed += txtName_Changed;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgEditNewLocation.Title = location != null ?
                Translator.GetString ("Edit Location") :
                Translator.GetString ("New Location");

            lblBasicInfoTab.SetText (Translator.GetString ("General information"));
            lblName.SetText (Translator.GetString ("Name:"));
            lblDisplayName.SetText (Translator.GetString ("Display name:"));
            lblCode.SetText (Translator.GetString ("Code:"));
            lblPriceGroup.SetText (Translator.GetString ("Price group:"));

            lblGroupsTab.SetText (Translator.GetString ("Groups"));

            btnSaveAndNew.SetChildLabelText (location != null ?
                Translator.GetString ("Save as New", "Location") : Translator.GetString ("Save and New", "Location"));
            btnSave.SetChildLabelText (Translator.GetString ("Save"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        protected virtual void InitializeEntries ()
        {
            if (location == null) {
                location = new Location ();

                if (defaultGroupId.HasValue)
                    gEditPanel.SelectGroupId ((int) defaultGroupId);

                if (BusinessDomain.AppConfiguration.AutoGenerateLocationCodes)
                    location.AutoGenerateCode ();
            } else
                gEditPanel.SelectGroupId (location.GroupId);

            txtName.Text = location.Name;
            txtDisplayName.Text = location.Name2;
            txtCode.Text = location.Code;
            cboPriceGroup.Load (Currency.GetAllSalePriceGroups (), "Key", "Value", (int) location.PriceGroup);
        }

        private void btnGenerateCode_Clicked (object sender, EventArgs e)
        {
            location.AutoGenerateCode ();
            txtCode.Text = location.Code;
        }

        private void dlgEditNewLocation_Shown (object sender, EventArgs e)
        {
            if (BusinessDomain.AppConfiguration.AutoGenerateLocationCodes)
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

        public Location GetLocation ()
        {
            location.Name = txtName.Text.Trim ();
            location.Name2 = txtDisplayName.Text.Trim ();
            location.Code = txtCode.Text.Trim ();
            location.PriceGroup = (PriceGroup) cboPriceGroup.GetSelectedValue ();
            long selectedGroupId = gEditPanel.GetSelectedGroupId ();
            if (selectedGroupId == PartnersGroup.DeletedGroupId)
                location.Deleted = true;
            else
                location.GroupId = selectedGroupId;

            return location;
        }

        private bool Validate ()
        {
            return GetLocation ().Validate ((message, severity, code, state) =>
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
        private void btnSaveAndNew_Clicked (object o, EventArgs args)
        {
            long oldId = location.Id;
            location.Id = -1;
            if (!Validate ()) {
                location.Id = oldId;
                return;
            }

            var saved = GetLocation ().CommitChanges ();

            if (BusinessDomain.AppConfiguration.DocumentNumbersPerLocation)
                using (EditDocumentNumbersPerLocation dialog = new EditDocumentNumbersPerLocation (saved.Id))
                    dialog.Run ();
            
            if (oldId > 0) {
                dlgEditNewLocation.Respond (ResponseType.Ok);
                return;
            }

            location = null;
            InitializeEntries ();

            txtName.Text = saved.Name;
        }

        [UsedImplicitly]
        protected void btnSave_Clicked (object o, EventArgs args)
        {
            if (!Validate ())
                return;

            dlgEditNewLocation.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditNewLocation.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
