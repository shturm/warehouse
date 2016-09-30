//
// About.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   06/24/2006
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
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public class About : DialogBase
    {
        #region Glade Widgets

        [Widget]
        protected Dialog dlgAbout;
        [Widget]
        protected Button btnOK;

        [Widget]
        protected Alignment algAboutLogo;
        [Widget]
        protected Label lblProductName;
        [Widget]
        protected Label lblVersion;
        [Widget]
        protected Label lblLicenseLine1;
        [Widget]
        protected Label lblLicenseLine2;
        [Widget]
        protected Label lblCopyright;
        [Widget]
        protected Label lblContactAddress;
        [Widget]
        protected Button btnContactWebAddress;
        [Widget]
        protected Label lblContactEmail;
        [Widget]
        protected EventBox eventboxSeparator;
        [Widget]
        protected EventBox evbContactWebAddress;

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgAbout; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        public About ()
        {
            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.About.glade", "dlgAbout");
            form.Autoconnect (this);

            dlgAbout.Title = string.Format (Translator.GetString ("About {0}"), DataHelper.ProductName);

            Image image = FormHelper.LoadImage ("Images.About150.png");
            algAboutLogo.Add (image);
            image.Show ();

            eventboxSeparator.ModifyBg (StateType.Normal, new Gdk.Color (10, 10, 10));

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));

            btnContactWebAddress.Clicked += btnContactWebAddress_Clicked;

            base.InitializeForm ();

            InitializeFormStrings ();
            dlgAbout.Realized += dlgAbout_Realized;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            lblProductName.SetText (string.Format (Translator.GetString ("{0} by {1}"), DataHelper.ProductFullName, DataHelper.CompanyName));
            lblVersion.SetText (string.Format (Translator.GetString ("Version: {0} (database version: {1})"), BusinessDomain.ApplicationVersionString, BusinessDomain.DataProviderVersion));
            lblLicenseLine1.SetText (Translator.GetString ("This product is released under the terms of"));
            lblLicenseLine2.SetText (Translator.GetString ("GNU General Public License version 2."));
            lblCopyright.SetText (string.Format (Translator.GetString ("Copyright© 2006-2015 by {0}"), DataHelper.CompanyName));
            lblContactAddress.SetText (DataHelper.CompanyAddress);
            lblContactEmail.SetText (string.Format (Translator.GetString ("e-mail: {0}"), DataHelper.ProductSupportEmail));
            btnContactWebAddress.SetChildLabelText (DataHelper.CompanyWebSite);
            btnOK.SetChildLabelText (Translator.GetString ("OK"));
        }

        private void dlgAbout_Realized (object sender, EventArgs e)
        {
            btnOK.GrabFocus ();
        }

        private static void btnContactWebAddress_Clicked (object sender, EventArgs e)
        {
            ComponentHelper.OpenUrlInBrowser (DataHelper.CompanyWebSite, ErrorHandling.GetHelper ());
        }
    }
}
