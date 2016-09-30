//
// EditNewCompanyRecordPanel.cs
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

using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Presentation.Dialogs;

namespace Warehouse.Presentation.Widgets
{
    public class EditNewCompanyRecordPanel : Alignment
    {
        private CompanyRecord companyRecord;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Notebook nbkRoot;
        [Widget]
        private CheckButton chkIsDefault;

        [Widget]
        private Label lblBasicInfoTab;
        [Widget]
        private Label lblAditionalInfoTab;

        [Widget]
        private Label lblCode;
        [Widget]
        private Label lblName;
        [Widget]
        private Label lblLiablePerson;
        [Widget]
        private Label lblCity;
        [Widget]
        private Label lblAddress;
        [Widget]
        private Label lblTelephone;
        [Widget]
        private Label lblFax;
        [Widget]
        private Label lblEmail;
        [Widget]
        private Label lblBulstat;
        [Widget]
        private Label lblTaxNo;
        [Widget]
        private Label lblBankName;
        [Widget]
        private Label lblBankCode;
        [Widget]
        private Label lblBankAccount;
        [Widget]
        private Label lblBankVATAccount;

        [Widget]
        private Entry txtCode;
        [Widget]
        private Entry txtName;
        [Widget]
        private Entry txtLiablePerson;
        [Widget]
        private Entry txtCity;
        [Widget]
        private Entry txtAddress;
        [Widget]
        private Entry txtTelephone;
        [Widget]
        private Entry txtFax;
        [Widget]
        private Entry txtBulstat;
        [Widget]
        private Entry txtTaxNo;
        [Widget]
        private Entry txtEmail;
        [Widget]
        private Entry txtBankName;
        [Widget]
        private Entry txtBankCode;
        [Widget]
        private Entry txtBankAccount;
        [Widget]
        private Entry txtBankVATAccount;

#pragma warning restore 649

        #endregion

        public EditNewCompanyRecordPanel (CompanyRecord companyRecord)
            : base (.5f, .5f, 1f, 1f)
        {
            this.companyRecord = companyRecord;

            InitializeForm ();
        }

        private void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Widgets.EditNewCompanyRecordPanel.glade", "nbkRoot");
            form.Autoconnect (this);

            Add (nbkRoot);

            InitializeFormStrings ();
            InitializeEntries ();
        }

        private void InitializeFormStrings ()
        {
            lblBasicInfoTab.SetText (Translator.GetString ("General information"));
            lblAditionalInfoTab.SetText (Translator.GetString ("Additional information"));

            lblCode.SetText (Translator.GetString ("Code:"));
            lblName.SetText (Translator.GetString ("Company:"));
            lblLiablePerson.SetText (Translator.GetString ("Contact Name:"));
            lblCity.SetText (Translator.GetString ("City:"));
            lblAddress.SetText (Translator.GetString ("Address:"));
            lblTelephone.SetText (Translator.GetString ("Phone:"));
            lblFax.SetText (Translator.GetString ("Fax:"));
            lblBulstat.SetText (Translator.GetString ("UIC:"));
            lblTaxNo.SetText (Translator.GetString ("VAT number:"));
            lblEmail.SetText (Translator.GetString ("e-mail:"));
            lblBankName.SetText (Translator.GetString ("Bank name:"));
            lblBankCode.SetText (Translator.GetString ("BIC:"));
            lblBankAccount.SetText (Translator.GetString ("IBAN:"));
            lblBankVATAccount.SetText (Translator.GetString ("VAT IBAN:"));

            chkIsDefault.Label = Translator.GetString ("This information is used by default");
        }

        protected virtual void InitializeEntries ()
        {
            if (companyRecord == null) {
                companyRecord = new CompanyRecord ();
                chkIsDefault.Active = true;
            } else {
                txtCode.Text = companyRecord.Code;
                txtName.Text = companyRecord.Name;
                txtLiablePerson.Text = companyRecord.LiablePerson;
                txtCity.Text = companyRecord.City;
                txtAddress.Text = companyRecord.Address;
                txtTelephone.Text = companyRecord.Telephone;
                txtFax.Text = companyRecord.Fax;
                txtBulstat.Text = companyRecord.Bulstat;
                txtTaxNo.Text = companyRecord.TaxNumber;
                txtEmail.Text = companyRecord.Email;
                txtBankName.Text = companyRecord.BankName;
                txtBankCode.Text = companyRecord.BankCode;
                txtBankAccount.Text = companyRecord.BankAccount;
                txtBankVATAccount.Text = companyRecord.BankVATAccount;
                chkIsDefault.Active = companyRecord.IsDefault;
            }
        }

        protected override void OnShown ()
        {
            base.OnShown ();

            txtCode.GrabFocus ();
        }

        public CompanyRecord GetCompanyRecord ()
        {
            companyRecord.Code = txtCode.Text;
            companyRecord.Name = txtName.Text;
            companyRecord.LiablePerson = txtLiablePerson.Text;
            companyRecord.City = txtCity.Text;
            companyRecord.Address = txtAddress.Text;
            companyRecord.Telephone = txtTelephone.Text;
            companyRecord.Fax = txtFax.Text;
            companyRecord.Bulstat = txtBulstat.Text;
            companyRecord.TaxNumber = txtTaxNo.Text;
            companyRecord.Email = txtEmail.Text;
            companyRecord.BankName = txtBankName.Text;
            companyRecord.BankCode = txtBankCode.Text;
            companyRecord.BankAccount = txtBankAccount.Text;
            companyRecord.BankVATAccount = txtBankVATAccount.Text;
            companyRecord.IsDefault = chkIsDefault.Active;

            return companyRecord;
        }

        public bool Validate ()
        {
            string name = txtName.Text.Trim ();

            if (name.Length == 0) {
                MessageError.ShowDialog (Translator.GetString ("Company name cannot be empty!"));
                return false;
            }

            CompanyRecord c = CompanyRecord.GetByName (name);
            if (c != null && c.Id != companyRecord.Id) {
                if (Message.ShowDialog (Translator.GetString ("Warning!"), string.Empty,
                    Translator.GetString ("Company with this name already exists! Do you want to continue?"), "Icons.Warning32.png",
                    MessageButtons.YesNo) != ResponseType.Yes)
                    return false;
            }

            string code = txtCode.Text.Trim ();
            c = CompanyRecord.GetByCode (name);

            if (!string.IsNullOrEmpty (code) && c != null && c.Id != companyRecord.Id) {
                if (Message.ShowDialog (Translator.GetString ("Warning!"), string.Empty,
                    Translator.GetString ("Company with this code already exists! Do you want to continue?"), "Icons.Warning32.png",
                    MessageButtons.YesNo) != ResponseType.Yes)
                    return false;
            }

            return true;
        }
    }
}
