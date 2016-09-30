//
// NewDatabase.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   06/23/2006
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
using System.IO;
using System.Linq;
using Glade;
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public class NewDatabase : DialogBase
    {
        private WrapLabel lblDescription;
        private readonly List<KeyValuePair<RadioButton, IDatabaseCreator>> customCreators = new List<KeyValuePair<RadioButton, IDatabaseCreator>> ();

        public override Dialog DialogControl
        {
            get { return dlgNewDatabase; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgNewDatabase;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

        [Widget]
        private Label lblName;
        [Widget]
        private Entry txtName;

        [Widget]
        private RadioButton rbtBlankDb;
        [Widget]
        private RadioButton rbtSampleDb;
        [Widget]
        private Label lblSampleType;
        [Widget]
        private ComboBox cboSampleType;
        [Widget]
        private VBox vbxAdditionalTypes;
        [Widget]
        private Alignment algSampleDescription;

#pragma warning restore 649

        #endregion

        public string DatabaseName
        {
            get { return txtName.Text.Trim (); }
        }

        public CreateDatabaseType DatabaseType
        {
            get
            {
                if (rbtSampleDb.Active)
                    return (CreateDatabaseType) cboSampleType.GetSelectedValue ();

                return rbtBlankDb.Active ? CreateDatabaseType.Blank : CreateDatabaseType.Other;
            }
        }

        public NewDatabase ()
        {
            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.NewDatabase.glade", "dlgNewDatabase");
            form.Autoconnect (this);

            dlgNewDatabase.Icon = FormHelper.LoadImage ("Icons.DBNew24.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            dlgNewDatabase.WidthRequest = 400;

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeEntries ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgNewDatabase.Title = Translator.GetString ("Create New Database");

            lblName.SetText (Translator.GetString ("Name:"));
            rbtBlankDb.Label = Translator.GetString ("Blank database");
            rbtSampleDb.Label = Translator.GetString ("Sample database");
            lblSampleType.SetText (Translator.GetString ("Sample type:"));

            btnOK.SetChildLabelText (Translator.GetString ("Create"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        private void InitializeEntries ()
        {
            KeyValuePair<CreateDatabaseType, string> [] samples = new []
                {
                    new KeyValuePair<CreateDatabaseType, string> (CreateDatabaseType.SampleRestaurant, Translator.GetString ("Restaurant")),
                    new KeyValuePair<CreateDatabaseType, string> (CreateDatabaseType.SampleStore, Translator.GetString ("Store"))
                };

            lblDescription = new WrapLabel ();
            lblDescription.Show ();
            algSampleDescription.Add (lblDescription);

            cboSampleType.Load (samples, "Key", "Value");
            cboSampleType.Changed += cboSampleType_Changed;
            cboSampleType_Changed (null, EventArgs.Empty);

            rbtSampleDb.Toggled += rbtSampleDb_Toggled;
            rbtSampleDb_Toggled (null, EventArgs.Empty);

            foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Presentation/DatabaseCreator")) {
                IDatabaseCreator creator = (IDatabaseCreator) node.CreateInstance ();
                RadioButton rbtn = new RadioButton (rbtBlankDb) { Label = creator.Type };
                rbtn.Toggled += (sender, args) =>
                    {
                        if (rbtn.Active)
                            lblDescription.Text = creator.Description;
                    };

                rbtn.Show ();
                vbxAdditionalTypes.PackStart (rbtn, false, true, 0);
                customCreators.Add (new KeyValuePair<RadioButton, IDatabaseCreator> (rbtn, creator));
            }
        }

        private void cboSampleType_Changed (object sender, EventArgs e)
        {
            switch (DatabaseType) {
                case CreateDatabaseType.SampleRestaurant:
                    lblDescription.Text = string.Format (Translator.GetString ("This is a sample database that demonstrates usage of {0} as a restaurant management system. Contains predefined items, partners, users, locations and operations."),
                        DataHelper.ProductName);
                    break;

                case CreateDatabaseType.SampleStore:
                    lblDescription.Text = string.Format (Translator.GetString ("This is a sample database that demonstrates usage of {0} in a computer parts store. Contains predefined items, partners, users, locations and operations."),
                        DataHelper.ProductName);
                    break;

                default:
                    lblDescription.Text = Translator.GetString ("This is an empty database ready to be filled with your information.");
                    break;
            }
        }

        private void rbtSampleDb_Toggled (object sender, EventArgs e)
        {
            if (rbtSampleDb.Active) {
                lblSampleType.Sensitive = true;
                cboSampleType.Sensitive = true;
            } else {
                lblSampleType.Sensitive = false;
                cboSampleType.Sensitive = false;
            }
            cboSampleType_Changed (null, EventArgs.Empty);
        }

        #region Event handling

        [UsedImplicitly]
        private void btnOK_Clicked (object o, EventArgs args)
        {
            string newDb = DatabaseName;

            if (newDb.Length == 0) {
                MessageError.ShowDialog (Translator.GetString ("Please, enter the name of the new database!"),
                    "Icons.Database32.png");
                txtName.GrabFocus ();
                return;
            }

            if (!BusinessDomain.IsValidDatabaseName (newDb)) {
                MessageError.ShowDialog (Translator.GetString ("The entered database name contains invalid characters!"),
                    "Icons.Database32.png");
                txtName.GrabFocus ();
                return;
            }

            if (BusinessDomain.SetCurrentDatabase (newDb)) {
                MessageError.ShowDialog (string.Format (Translator.GetString ("A database with the name of \"{0}\" already exists!"), newDb),
                    "Icons.Database32.png");
                txtName.GrabFocus ();
                return;
            }

            // We don't need that dialog to clutter the visual space any more
            dlgNewDatabase.Hide ();

            try {
                if (DatabaseType != CreateDatabaseType.Other) {
                    using (MessageProgress dlgProg = new MessageProgress (
                        Translator.GetString ("Creating New Database"), "Icons.DBNew24.png",
                        string.Format (Translator.GetString ("Creating database \"{0}\"..."), newDb))) {
                        dlgProg.Show ();

                        BusinessDomain.CreateDatabase (newDb, DatabaseType, dlgProg.ProgressCallback);
                    }
                } else {
                    foreach (KeyValuePair<RadioButton, IDatabaseCreator> creator in customCreators.Where (creator => creator.Key.Active)) {
                        if (!creator.Value.Create (newDb)) {
                            dlgNewDatabase.Respond (ResponseType.Apply);
                            return;
                        }

                        break;
                    }
                }
            } catch (Exception ex) {
                IOException ioException = ex as IOException;
                if (ioException != null && ioException.Data.Contains ("Path"))
                    MessageError.ShowDialog (string.Format (Translator.GetString ("Error occurred while creating database \"{0}\"! Please check if you have permissions to create a new database file: \"{1}\"."), newDb, ioException.Data ["Path"]),
                        ErrorSeverity.Error, ex);
                else
                    MessageError.ShowDialog (string.Format (Translator.GetString ("Error occurred while creating database \"{0}\"! Please check the database name for invalid characters and if you have permissions to create a new database."), newDb),
                        ErrorSeverity.Error, ex);

                dlgNewDatabase.Respond (ResponseType.Cancel);
                return;
            }

            dlgNewDatabase.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        private void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgNewDatabase.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
