//
// EditDBConnection.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
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
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Presentation.SetupAssistant;
using Assistant = Warehouse.Presentation.SetupAssistant.Assistant;

namespace Warehouse.Presentation.Dialogs
{
    public class EditDBConnection : DialogBase
    {
        private string [] dataBases;

        public override Dialog DialogControl
        {
            get { return dlgEditDBConnection; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        #region Public properties

        public string DbProvider
        {
            get { return ((DataProviderDescription) cboType.GetSelectedValue ()).ProviderType.FullName; }
        }

        public string Server
        {
            get { return txtServer.Text; }
        }

        public string SlaveServer
        {
            get { return chkUseReplicatingServers.Active ? txtSlaveServer.Text : string.Empty; }
        }

        public string User
        {
            get { return txtUsername.Text; }
        }

        public string Password
        {
            get { return txtPassword.Text; }
        }

        public string Database
        {
            get { return (string) cboDatabase.GetSelectedValue (); }
        }

        #endregion

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEditDBConnection;
        [Widget]
        private Button btnTest;
        [Widget]
        private Button btnNew;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

        [Widget]
        private Label lblType;
        [Widget]
        private ComboBox cboType;

        [Widget]
        private Label lblServer;
        [Widget]
        private Label lblSlaveServer;
        [Widget]
        private Label lblUsername;
        [Widget]
        private Label lblPassword;
        [Widget]
        private Label lblDatabase;

        [Widget]
        private Entry txtServer;
        [Widget]
        private CheckButton chkUseReplicatingServers;
        [Widget]
        private Entry txtSlaveServer;
        [Widget]
        private Entry txtUsername;
        [Widget]
        private Entry txtPassword;
        [Widget]
        private ComboBox cboDatabase;

#pragma warning restore 649

        #endregion

        public EditDBConnection ()
        {
            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditDBConnection.glade", "dlgEditDBConnection");
            form.Autoconnect (this);

            dlgEditDBConnection.Icon = FormHelper.LoadImage ("Icons.Database32.png").Pixbuf;
            btnTest.SetChildImage (FormHelper.LoadImage ("Icons.DBTest24.png"));
            btnNew.SetChildImage (FormHelper.LoadImage ("Icons.DBNew24.png"));
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            btnTest.Clicked += btnTest_Clicked;
            btnNew.Clicked += btnNew_Clicked;

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeEntries ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgEditDBConnection.Title = Translator.GetString ("Connecting To Database");

            lblType.SetText (Translator.GetString ("Type:"));
            lblServer.SetText (Translator.GetString ("Server:"));
            lblSlaveServer.SetText (Translator.GetString ("Slave server:"));
            lblUsername.SetText (Translator.GetString ("User:"));
            lblPassword.SetText (Translator.GetString ("Password:"));
            lblDatabase.SetText (Translator.GetString ("Database:"));

            chkUseReplicatingServers.Label = Translator.GetString ("Use replicating slave server");

            btnTest.SetChildLabelText (Translator.GetString ("Test"));
            btnNew.SetChildLabelText (Translator.GetString ("Create"));
            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        protected virtual void InitializeEntries ()
        {
            cboType.Load (BusinessDomain.AllDataAccessProviders, null, "Name");
            cboType.SetSelection (Array.FindIndex (BusinessDomain.AllDataAccessProviders, p => p.ProviderType.FullName == BusinessDomain.AppConfiguration.DbProvider));
            cboType.Changed += cboType_Changed;

            txtServer.Text = BusinessDomain.AppConfiguration.DbServer;
            txtSlaveServer.Text = BusinessDomain.AppConfiguration.DbSlaveServer;
            txtUsername.Text = BusinessDomain.AppConfiguration.DbUser;
            txtPassword.Text = BusinessDomain.AppConfiguration.DbPassword;

            chkUseReplicatingServers.Active = !string.IsNullOrWhiteSpace (BusinessDomain.AppConfiguration.DbSlaveServer);
            chkUseReplicatingServers.Toggled += chkUseReplicatingServers_Toggled;
            chkUseReplicatingServers_Toggled (null, EventArgs.Empty);

            cboType_Changed (null, EventArgs.Empty);
        }

        private void cboType_Changed (object sender, EventArgs e)
        {
            DataProviderDescription description = (DataProviderDescription) cboType.GetSelectedValue ();
            lblServer.Visible = txtServer.Visible = description.UsesServer;
            lblSlaveServer.Visible = txtSlaveServer.Visible = chkUseReplicatingServers.Visible = description.UsesSlaveServer;
            lblUsername.Visible = txtUsername.Visible = description.UsesUser;
            lblPassword.Visible = txtPassword.Visible = description.UsesPassword;
            lblDatabase.Visible = cboDatabase.Visible = description.UsesDatabase;
            btnTest.Visible = description.UsesServer;

            dlgEditDBConnection.Resize (10, 10);
            ReLoadDatabases ();
        }

        private void chkUseReplicatingServers_Toggled (object sender, EventArgs e)
        {
            bool active = chkUseReplicatingServers.Active;
            lblSlaveServer.Sensitive = active;
            txtSlaveServer.Sensitive = active;
        }

        private void btnTest_Clicked (object sender, EventArgs e)
        {
            if (ReLoadDatabases ()) {
                MessageError.ShowDialog (Translator.GetString ("The connection to the server was successful."),
                    "Icons.Database32.png", ErrorSeverity.Information);
            } else {
                MessageError.ShowDialog (Translator.GetString ("The connection to the server was not successful!"),
                    "Icons.Database32.png");
            }
        }

        private void btnNew_Clicked (object sender, EventArgs e)
        {
            if (!BusinessDomain.TryConnect (DbProvider, Server, SlaveServer, User, Password)) {
                MessageError.ShowDialog (Translator.GetString ("The connection to the server was not successful!"),
                    "Icons.Database32.png");
                return;
            }

            CreateDatabaseType dbType;
            using (NewDatabase dlgNewDB = new NewDatabase ()) {
                ResponseType response = dlgNewDB.Run ();
                if (response == ResponseType.Apply) {
                    BusinessDomain.AppConfiguration.DbDatabase = dlgNewDB.DatabaseName;
                    ReLoadDatabases ();
                }

                if (response != ResponseType.Ok)
                    return;

                dbType = dlgNewDB.DatabaseType;

                // Set the new database as the only one as at this point we are closing the dialog and we don't nee the full list
                cboDatabase.Load (new [] { dlgNewDB.DatabaseName }, null, null, null);
            }

            // Unclutter the screen by hiding the background dialog which is no longer needed anyway
            dlgEditDBConnection.Hide ();
            if (dbType == CreateDatabaseType.Blank) {
                SetSettings ();
                BusinessDomain.AppConfiguration.Load (false);
                
                using (Assistant assistant = new Assistant (AssistType.DatabaseSetup))
                    assistant.Run ();

                BusinessDomain.AppConfiguration.Save (true);
            }

            dlgEditDBConnection.Respond (ResponseType.Ok);
        }

        private bool reloadingDatabases;
        private bool ReLoadDatabases ()
        {
            if (reloadingDatabases)
                return true;

            dataBases = null;

            try {
                cboDatabase.Sensitive = false;
                btnOK.Sensitive = false;
                reloadingDatabases = true;
                cboDatabase.Load (new [] { Translator.GetString ("Loading...") }, null, null, null);
                PresentationDomain.ProcessUIEvents ();

                string dbProviderName = DbProvider;
                if (!BusinessDomain.TryConnect (dbProviderName, Server, SlaveServer, User, Password)) {
                    cboDatabase.Load (new [] { Translator.GetString ("None", "Database") }, null, null, null);
                    reloadingDatabases = false;
                    return false;
                }

                DataHelper.FireAndForget (startedProvider =>
                    {
                        try {
                            dataBases = BusinessDomain.GetDatabases ();
                            // If the provider changed after we started to look for databases, don't show them
                            if (startedProvider != DbProvider)
                                return;

                            PresentationDomain.Invoke (() =>
                                {
                                    bool hasDbs = dataBases.Length > 0;
                                    if (hasDbs)
                                        cboDatabase.Load (dataBases, null, null, BusinessDomain.AppConfiguration.DbDatabase);
                                    else
                                        cboDatabase.Load (new [] { Translator.GetString ("None", "Database") }, null, null, null);
                                    cboDatabase.Sensitive = hasDbs;
                                    btnOK.Sensitive = hasDbs;
                                    reloadingDatabases = false;
                                });
                        } catch {
                            cboDatabase.Load (new [] { Translator.GetString ("None", "Database") }, null, null, null);
                            reloadingDatabases = false;
                        }
                    }, dbProviderName);
                return true;
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                return false;
            }
        }

        public void SetSettings ()
        {
            ConfigurationHolder config = BusinessDomain.AppConfiguration;
            config.DbProvider = DbProvider;
            config.DbServer = Server;
            config.DbSlaveServer = SlaveServer;
            config.DbUser = User;
            config.DbPassword = Password;
            config.DbDatabase = Database;
        }
    }
}
