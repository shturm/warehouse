//
// EditNewPartner.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
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
using System.Timers;
using Glade;
using Gtk;

using Warehouse.Business;
using Warehouse.Business.Devices;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Presentation.Widgets;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNewPartner : DialogBase
    {
        private Partner partner;
        private PartnersGroupsEditPanel gEditPanel;
        private long? defaultGroupId;
        private bool allowSaveAndNew;

        private string oldName;
        private string oldLiablePerson;
        private string oldTelephone;
        private string oldAddress;
        private string oldCity;

        private readonly Timer cardNumberTimer;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEditNewPartner;
        [Widget]
        private Button btnSaveAndNew;
        [Widget]
        private Button btnSave;
        [Widget]
        private Button btnCancel;

        [Widget]
        private Label lblBasicInfoTab;
        [Widget]
        private Label lblAditionalInfoTab;
        [Widget]
        private Label lblDisplayInfoTab;
        [Widget]
        private Label lblGroupsTab;

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
        private Label lblLiablePerson;
        [Widget]
        private Entry txtLiablePerson;
        [Widget]
        private Label lblCity;
        [Widget]
        private Entry txtCity;
        [Widget]
        private Label lblAddress;
        [Widget]
        private Entry txtAddress;
        [Widget]
        private Label lblTelephone;
        [Widget]
        private Entry txtTelephone;
        [Widget]
        private Label lblFax;
        [Widget]
        private Entry txtFax;
        [Widget]
        private Label lblBulstat;
        [Widget]
        private Entry txtBulstat;
        [Widget]
        private Label lblTaxNo;
        [Widget]
        private Entry txtTaxNo;
        [Widget]
        private Label lblCardNo;
        [Widget]
        private Entry txtCardNo;
        [Widget]
        private Label lblEmail;
        [Widget]
        private Entry txtEmail;
        [Widget]
        private Label lblBankName;
        [Widget]
        private Entry txtBankName;
        [Widget]
        private Label lblBankCode;
        [Widget]
        private Entry txtBankCode;
        [Widget]
        private Label lblBankAccount;
        [Widget]
        private Entry txtBankAccount;
        [Widget]
        private Label lblBankVATName;
        [Widget]
        private Entry txtBankVATName;
        [Widget]
        private Label lblBankVATCode;
        [Widget]
        private Entry txtBankVATCode;
        [Widget]
        private Label lblBankVATAccount;
        [Widget]
        private Entry txtBankVATAccount;
        [Widget]
        private Label lblPriceGroup;
        [Widget]
        private ComboBox cboPriceGroup;
        [Widget]
        private Label lblType;
        [Widget]
        private ComboBox cboType;
        [Widget]
        private Label lblNote;
        [Widget]
        private TextView txvNote;

        [Widget]
        private Label lblDisplayName;
        [Widget]
        private Label lblDisplayLiablePerson;
        [Widget]
        private Label lblDisplayCity;
        [Widget]
        private Label lblDisplayAddress;
        [Widget]
        private Label lblDisplayTelephone;

        [Widget]
        private Entry txtDisplayName;
        [Widget]
        private Entry txtDisplayLiablePerson;
        [Widget]
        private Entry txtDisplayCity;
        [Widget]
        private Entry txtDisplayAddress;
        [Widget]
        private Entry txtDisplayTelephone;

        #region Groups

        [Widget]
        private Alignment algGroups;

        #endregion

#pragma warning restore 649

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgEditNewPartner; }
        }

        public EditNewPartner (Partner partner, long? defaultGroupId = null, bool allowSaveAndNew = true)
        {
            this.partner = partner;
            this.defaultGroupId = defaultGroupId;
            this.allowSaveAndNew = allowSaveAndNew;
            cardNumberTimer = new Timer (200) { AutoReset = false };

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditNewPartner.glade", "dlgEditNewPartner");
            form.Autoconnect (this);

            dlgEditNewPartner.Icon = FormHelper.LoadImage ("Icons.Partner16.png").Pixbuf;
            btnSaveAndNew.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnSave.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();

            gEditPanel = new PartnersGroupsEditPanel ();
            algGroups.Add (gEditPanel);
            gEditPanel.Show ();

            InitializeFormStrings ();
            InitializeEntries ();
            dlgEditNewPartner.Shown += dlgEditNewPartner_Shown;
            btnGenerateCode.Clicked += btnGenerateCode_Clicked;

            oldName = txtName.Text;
            txtName.Changed += txtName_Changed;

            oldLiablePerson = txtLiablePerson.Text;
            txtLiablePerson.Changed += txtLiablePerson_Changed;

            oldTelephone = txtTelephone.Text;
            txtTelephone.Changed += txtTelephone_Changed;

            oldAddress = txtAddress.Text;
            txtAddress.Changed += txtAddress_Changed;

            oldCity = txtCity.Text;
            txtCity.Changed += txtCity_Changed;
            txtCardNo.Changed += txtCardNo_Changed;
            PresentationDomain.CardRecognized += PresentationDomain_CardRecognized;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgEditNewPartner.Title = partner != null && partner.Id > 0 ?
                Translator.GetString ("Edit Partner") : Translator.GetString ("New Partner");

            lblBasicInfoTab.SetText (Translator.GetString ("General information"));
            lblAditionalInfoTab.SetText (Translator.GetString ("Additional information"));
            lblDisplayInfoTab.SetText (Translator.GetString ("Display information"));
            lblGroupsTab.SetText (Translator.GetString ("Groups"));

            lblCode.SetText (Translator.GetString ("Code:"));
            lblName.SetText (Translator.GetString ("Company:"));
            lblLiablePerson.SetText (Translator.GetString ("Contact Name:"));
            lblCity.SetText (Translator.GetString ("City:"));
            lblAddress.SetText (Translator.GetString ("Address:"));
            lblTelephone.SetText (Translator.GetString ("Phone:"));
            lblFax.SetText (Translator.GetString ("Fax:"));
            lblBulstat.SetText (Translator.GetString ("UIC:"));
            lblTaxNo.SetText (Translator.GetString ("VAT number:"));
            lblCardNo.SetText (Translator.GetString ("Card number:"));
            lblEmail.SetText (Translator.GetString ("e-mail:"));
            lblBankName.SetText (Translator.GetString ("Bank name:"));
            lblBankCode.SetText (Translator.GetString ("BIC:"));
            lblBankAccount.SetText (Translator.GetString ("IBAN:"));
            lblBankVATName.SetText (Translator.GetString ("VAT bank name:"));
            lblBankVATCode.SetText (Translator.GetString ("VAT BIC:"));
            lblBankVATAccount.SetText (Translator.GetString ("VAT IBAN:"));
            lblPriceGroup.SetText (Translator.GetString ("Price group:"));
            lblType.SetText (Translator.GetString ("Partner type:"));
            lblNote.SetText (Translator.GetString ("Note:"));

            lblDisplayName.SetText (Translator.GetString ("Display name:"));
            lblDisplayLiablePerson.SetText (Translator.GetString ("Display contact name:"));
            lblDisplayCity.SetText (Translator.GetString ("Display city:"));
            lblDisplayAddress.SetText (Translator.GetString ("Display address:"));
            lblDisplayTelephone.SetText (Translator.GetString ("Display phone:"));

            btnSaveAndNew.SetChildLabelText (partner != null ?
                Translator.GetString ("Save as New", "Partner") : Translator.GetString ("Save and New", "Partner"));
            btnSaveAndNew.Visible = allowSaveAndNew;
            btnSave.SetChildLabelText (Translator.GetString ("Save"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        protected virtual void InitializeEntries ()
        {
            if (partner == null) {
                partner = new Partner ();

                if (defaultGroupId.HasValue)
                    gEditPanel.SelectGroupId ((int) defaultGroupId);

                if (BusinessDomain.AppConfiguration.AutoGeneratePartnerCodes)
                    partner.AutoGenerateCode ();
            } else
                gEditPanel.SelectGroupId (partner.GroupId);

            txtCode.Text = partner.Code;
            txtName.Text = partner.Name;
            txtDisplayName.Text = partner.Name2;
            txtLiablePerson.Text = partner.LiablePerson;
            txtDisplayLiablePerson.Text = partner.LiablePerson2;
            txtCity.Text = partner.City;
            txtDisplayCity.Text = partner.City2;
            txtAddress.Text = partner.Address;
            txtDisplayAddress.Text = partner.Address2;
            txtTelephone.Text = partner.Telephone;
            txtDisplayTelephone.Text = partner.Telephone2;
            txtFax.Text = partner.Fax;
            txtBulstat.Text = partner.Bulstat;
            txtTaxNo.Text = partner.TaxNumber;
            txtCardNo.Text = partner.CardNumber;
            txtEmail.Text = partner.Email;
            txtBankName.Text = partner.BankName;
            txtBankCode.Text = partner.BankCode;
            txtBankAccount.Text = partner.BankAccount;
            txtBankVATName.Text = partner.BankVATName;
            txtBankVATCode.Text = partner.BankVATCode;
            txtBankVATAccount.Text = partner.BankVATAccount;

            cboPriceGroup.Load (Currency.GetAllSalePriceGroups (), "Key", "Value", (int) partner.PriceGroup);
            cboType.Load (Partner.GetAllTypes (), "Key", "Value", (int) partner.BusinessType);
            txvNote.Buffer.Text = partner.Note;
        }

        private void dlgEditNewPartner_Shown (object sender, EventArgs e)
        {
            if (BusinessDomain.AppConfiguration.AutoGeneratePartnerCodes)
                txtName.GrabFocus ();
            else
                txtCode.GrabFocus ();
        }

        private void btnGenerateCode_Clicked (object sender, EventArgs e)
        {
            partner.AutoGenerateCode ();
            txtCode.Text = partner.Code;
        }

        private void txtName_Changed (object sender, EventArgs e)
        {
            if (txtDisplayName.Text == oldName)
                txtDisplayName.Text = txtName.Text;

            oldName = txtName.Text;
        }

        private void txtLiablePerson_Changed (object sender, EventArgs e)
        {
            if (txtDisplayLiablePerson.Text == oldLiablePerson)
                txtDisplayLiablePerson.Text = txtLiablePerson.Text;

            oldLiablePerson = txtLiablePerson.Text;
        }

        private void txtTelephone_Changed (object sender, EventArgs e)
        {
            if (txtDisplayTelephone.Text == oldTelephone)
                txtDisplayTelephone.Text = txtTelephone.Text;

            oldTelephone = txtTelephone.Text;
        }

        private void txtAddress_Changed (object sender, EventArgs e)
        {
            if (txtDisplayAddress.Text == oldAddress)
                txtDisplayAddress.Text = txtAddress.Text;

            oldAddress = txtAddress.Text;
        }

        private void txtCity_Changed (object sender, EventArgs e)
        {
            if (txtDisplayCity.Text == oldCity)
                txtDisplayCity.Text = txtCity.Text;

            oldCity = txtCity.Text;
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

        public Partner GetPartner ()
        {
            partner.Code = txtCode.Text.Trim ();
            partner.Name = txtName.Text.Trim ();
            partner.Name2 = txtDisplayName.Text.Trim ();
            partner.LiablePerson = txtLiablePerson.Text.Trim ();
            partner.LiablePerson2 = txtDisplayLiablePerson.Text.Trim ();
            partner.City = txtCity.Text.Trim ();
            partner.City2 = txtDisplayCity.Text.Trim ();
            partner.Address = txtAddress.Text.Trim ();
            partner.Address2 = txtDisplayAddress.Text.Trim ();
            partner.Telephone = txtTelephone.Text.Trim ();
            partner.Telephone2 = txtDisplayTelephone.Text.Trim ();
            partner.Fax = txtFax.Text.Trim ();
            partner.Bulstat = txtBulstat.Text.Trim ();
            partner.TaxNumber = txtTaxNo.Text.Trim ();
            partner.CardNumber = txtCardNo.Text.Trim ();
            partner.Email = txtEmail.Text.Trim ();
            partner.BankName = txtBankName.Text.Trim ();
            partner.BankCode = txtBankCode.Text.Trim ();
            partner.BankAccount = txtBankAccount.Text.Trim ();
            partner.BankVATName = txtBankVATName.Text.Trim ();
            partner.BankVATCode = txtBankVATCode.Text.Trim ();
            partner.BankVATAccount = txtBankVATAccount.Text.Trim ();
            partner.PriceGroup = (PriceGroup) cboPriceGroup.GetSelectedValue ();
            partner.BusinessType = (PartnerType) cboType.GetSelectedValue ();
            partner.Note = txvNote.Buffer.Text.Trim ();

            long selectedGroupId = gEditPanel.GetSelectedGroupId ();
            if (selectedGroupId == PartnersGroup.DeletedGroupId)
                partner.Deleted = true;
            else
                partner.GroupId = selectedGroupId;

            return partner;
        }

        private bool Validate ()
        {
            return GetPartner ().Validate ((message, severity, code, state) =>
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

        protected override void OnClosing ()
        {
            PresentationDomain.CardRecognized -= PresentationDomain_CardRecognized;
            cardNumberTimer.Dispose ();
        }

        [UsedImplicitly]
        private void btnSaveAndNew_Clicked (object o, EventArgs args)
        {
            long oldId = partner.Id;
            partner.Id = -1;
            if (!Validate ()) {
                partner.Id = oldId;
                return;
            }

            Partner saved = GetPartner ().CommitChanges ();
            if (oldId > 0) {
                dlgEditNewPartner.Respond (ResponseType.Ok);
                return;
            }

            partner = null;
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

            dlgEditNewPartner.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditNewPartner.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
