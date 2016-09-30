//
// ChooseLocalization.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
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
using System.Globalization;
using System.Threading;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseLocalization : DialogBase
    {
        private readonly Dictionary<string, string> localizations = new Dictionary<string, string> ();
        private string currentLocale;

        #region Glade Widgets

        [Widget]
        protected Dialog dlgChooseLocalization;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;
        [Widget]
        protected CheckButton chkSystemLocalization;
        [Widget]
        protected Label lblLocalization;
        [Widget]
        protected ComboBox cboLocalization;

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgChooseLocalization; }
        }

        public ChooseLocalization ()
        {
            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChooseLocalization.glade", "dlgChooseLocalization");
            form.Autoconnect (this);

            dlgChooseLocalization.Icon = FormHelper.LoadImage ("Icons.User16.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            dlgChooseLocalization.Realized += dlgChooseLanguage_Realized;

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeEntries ();
        }

        private void dlgChooseLanguage_Realized (object sender, EventArgs e)
        {
            cboLocalization.GrabFocus ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseLocalization.Title = Translator.GetString ("Choose Localization");

            chkSystemLocalization.Label = Translator.GetString ("Use system localization");
            lblLocalization.SetText (Translator.GetString ("Localization:"));

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        protected virtual void InitializeEntries ()
        {
            ConfigurationHolder config = BusinessDomain.AppConfiguration;
            chkSystemLocalization.Active = config.UseSystemLocalization;
            chkSystemLocalization.Toggled += chkSystemLocalization_Toggled;
            chkSystemLocalization_Toggled (null, null);

            currentLocale = config.UseSystemLocalization ? Thread.CurrentThread.CurrentUICulture.Name : config.Localization;

            foreach (CultureInfo locale in ComponentHelper.GetAvailableLocales (config.LocalePackageName, StoragePaths.LocaleFolder)) {
                localizations.Add (locale.Name, char.ToUpper (locale.NativeName [0]) + locale.NativeName.Substring (1));
            }

            cboLocalization.Load (localizations, "Key", "Value", currentLocale);
        }

        private void chkSystemLocalization_Toggled (object sender, EventArgs e)
        {
            if (chkSystemLocalization.Active) {
                lblLocalization.Sensitive = false;
                cboLocalization.Sensitive = false;
                cboLocalization.SetSelection (localizations, "Key", "Value", currentLocale);
            } else {
                lblLocalization.Sensitive = true;
                cboLocalization.Sensitive = true;
            }
        }

        #region Event handling

        protected void btnOK_Clicked (object o, EventArgs args)
        {
            ConfigurationHolder config = BusinessDomain.AppConfiguration;
            bool newUseSystemLoalization = chkSystemLocalization.Active;
            string newLocalization = (string) cboLocalization.GetSelectedValue ();
            bool localizationChanged = newUseSystemLoalization != config.UseSystemLocalization ||
                (!newUseSystemLoalization && (newLocalization != config.Localization));

            if (localizationChanged)
                Translator.InitThread (config, Thread.CurrentThread);

            if (newUseSystemLoalization) {
                config.UseSystemLocalization = true;
                config.Localization = string.Empty;
            } else {
                config.UseSystemLocalization = false;
                config.Localization = newLocalization;
            }

            if (localizationChanged) {
                Translator.ResetCulture ();
                Translator.InitThread (config, Thread.CurrentThread);
                Translator.TranslateRestrictions ();

                FrmMain mainForm = PresentationDomain.MainForm;
                if (mainForm != null)
                    mainForm.InitializeStrings ();
            }

            dlgChooseLocalization.Respond (ResponseType.Ok);
        }

        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgChooseLocalization.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
