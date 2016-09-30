//
// ExchangeObjects.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/18/2006
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
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Gdk;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.Model;
using Warehouse.Presentation.OS;

namespace Warehouse.Presentation.Dialogs
{
    public abstract class ExchangeObjects<T> : DialogBase
    {
        private readonly DataTable data;
        protected readonly IEnumerable<IDataExchanger> exchangers;
        private readonly bool usesLocation;
        protected BindingListModel<IDataExchanger> exchangersModel;
        protected string defaultFileName;
        protected int defaultSelectedRow;
        private Location location;

        #region Public properties

        public override Dialog DialogControl
        {
            get { return dlgExchangeObjects; }
        }

        protected IDataExchanger Exchanger
        {
            get
            {
                return grid.Selection.Count == 0 ? null : exchangersModel [grid.Selection [0]];
            }
        }

        public bool? ToFile
        {
            get { return chkExchangeFile.Active ? (chkOpenFile.Active ? true : (bool?) null) : false; }
        }

        public string FileName
        {
            get
            {
                IDataExchanger exchanger = Exchanger;

                if (!exchanger.UsesFile)
                    return string.Empty;

                string filename = txtExchangeFile.Text;
                if (!IsExport && !File.Exists (filename))
                    return string.Empty;

                bool hasExtension = false;
                foreach (string mask in exchanger.FileMasks) {
                    string regexMatch = mask.Trim ()
                        .Replace ("\\", "\\\\")
                        .Replace (".", "\\.")
                        .Replace ("?", ".")
                        .Replace ("*", ".*");

                    if (!Regex.IsMatch (filename, regexMatch))
                        continue;

                    hasExtension = true;
                    break;
                }

                if (!hasExtension)
                    filename += exchanger.DefaultFileExtension;

                return filename;
            }
        }

        public string Folder
        {
            get
            {
                IDataExchanger exchanger = Exchanger;

                if (!exchanger.UsesFile)
                    return string.Empty;

                string filename = txtExchangeFile.Text;
                if (!IsExport && !File.Exists (filename))
                    return string.Empty;

                return Path.GetDirectoryName (filename);
            }
        }

        public string Email
        {
            get { return chkEmail.Active ? txtEmail.Text : string.Empty; }
        }

        public string EmailSubject
        {
            get { return chkEmail.Active ? txtEmailSubject.Text : string.Empty; }
        }

        public Location Location
        {
            get { return location; }
        }

        protected virtual bool IsExport
        {
            get { return true; }
        }

        #endregion

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        protected Dialog dlgExchangeObjects;
        [Widget]
        private Alignment algExchangeFile;
        [Widget]
        protected Frame grbFile;
        [Widget]
        protected Label lblExchangeFile;
        [Widget]
        protected HBox hboExchangeFile;
        [Widget]
        protected CheckButton chkExchangeFile;
        [Widget]
        protected Frame grbEmail;
        [Widget]
        protected CheckButton chkEmail;
        [Widget]
        protected CheckButton chkOpenFile;
        [Widget]
        protected Entry txtExchangeFile;
        [Widget]
        protected Entry txtEmail;
        [Widget]
        protected Label lblEmailSubject;
        [Widget]
        protected Entry txtEmailSubject;
        [Widget]
        private Button btnExchangeFile;
        [Widget]
        private Alignment algExchangeLocation;
        [Widget]
        protected Label lblExchangeLocation;
        [Widget]
        private Entry txtExchangeLocation;
        [Widget]
        private Button btnExchangeLocation;
        [Widget]
        private Alignment algCustomWidget;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

        [Widget]
        protected Alignment algGrid;

        protected ListView grid;

#pragma warning restore 649

        #endregion

        protected ExchangeObjects (DataTable data, IEnumerable<IDataExchanger> exchangers, bool usesLocation = false)
        {
            this.data = data;
            this.exchangers = exchangers;
            this.usesLocation = usesLocation;

            Initialize ();
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ExchangeObjects.glade", "dlgExchangeObjects");
            form.Autoconnect (this);

            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            btnExchangeFile.Clicked += btnExchangeFile_Clicked;

            base.InitializeForm ();

            algExchangeLocation.Visible = usesLocation;
            if (usesLocation) {
                Location loc = null;
                if (Location.TryGetLocked (ref loc)) {
                    txtExchangeLocation.Sensitive = false;
                    btnExchangeLocation.Sensitive = false;
                }

                if (loc != null)
                    SetLocation (loc);

                txtExchangeLocation.KeyPressEvent += txtExchangeLocation_KeyPress;
                txtExchangeLocation.ButtonPressEvent += txtExchangeLocation_ButtonPressEvent;
                txtExchangeLocation.Changed += txtExchnangeLocation_Changed;
                btnExchangeLocation.Clicked += btnExchangeLocation_Clicked;
            }

            InitializeFormStrings ();
            LoadExchangers ();

            chkExchangeFile.Active = BusinessDomain.AppConfiguration.LastExportToFile;
            chkExchangeFile.Toggle ();
            if (Exchanger != null)
                if (!Exchanger.UsesFile) {
                    chkOpenFile.Active = BusinessDomain.AppConfiguration.OpenExportedFile;
                    chkOpenFile.Toggle ();
                } else
                    chkOpenFile.Sensitive = chkOpenFile.Active = false;
            chkEmail.Active = BusinessDomain.AppConfiguration.LastExportToEmail;
            chkEmail.Toggle ();
            txtEmail.Text = BusinessDomain.AppConfiguration.LastExportEmail;
            txtEmailSubject.Text = BusinessDomain.AppConfiguration.LastExportEmailSubject;
            if (string.IsNullOrWhiteSpace (txtEmailSubject.Text))
                txtEmailSubject.Text = string.Format (Translator.GetString ("Export from {0}"), DataHelper.ProductName);
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            btnOK.SetChildLabelText (IsExport ? Translator.GetString ("Export") : Translator.GetString ("Import"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        protected abstract string GetInitialFolder ();

        private void LoadExchangers ()
        {
            IEnumerable<IDataExchanger> exchangerNames = exchangers
                .Where (exporter => exporter.SupportsType (typeof (T)) && exporter.IsAvailable);

            exchangersModel = new BindingListModel<IDataExchanger> (exchangerNames, false);
            exchangersModel.Sort ("ExchangeType", SortDirection.Ascending);

            grid = new ListView ();

            ScrolledWindow sWindow = new ScrolledWindow { HscrollbarPolicy = PolicyType.Automatic, VscrollbarPolicy = PolicyType.Automatic };
            sWindow.Add (grid);

            algGrid.Add (sWindow);
            sWindow.Show ();
            grid.Show ();

            ColumnController cc = new ColumnController ();

            Column col = new Column (IsExport ? Translator.GetString ("Export Type") : Translator.GetString ("Import Type"), "ExchangeType", 0.2, "ExchangeType") { MinWidth = 70, ListCell = { IsEditable = false } };
            cc.Add (col);

            grid.ColumnController = cc;
            grid.Model = exchangersModel;
            grid.AllowMultipleSelect = false;
            grid.CellsFucusable = true;
            grid.RulesHint = true;
            grid.SortColumnsHint = true;
            grid.Realized += grid_Realized;
            grid.Selection.Changed += grid_SelectionChanged;
        }

        private void grid_Realized (object sender, EventArgs e)
        {
            if (exchangersModel.Count > 0) {
                grid.Selection.Select (defaultSelectedRow);
                grid.FocusRow (defaultSelectedRow);
            }
        }

        private void grid_SelectionChanged (object sender, EventArgs args)
        {
            IDataExchanger exchanger = (IDataExchanger) grid.Model [grid.Selection [0]];
            bool usesFile = exchanger.UsesFile;
            algExchangeFile.Visible = usesFile;

            if (usesFile) {
                if (string.IsNullOrWhiteSpace (txtExchangeFile.Text) && !string.IsNullOrEmpty (defaultFileName))
                    SetExchangeFileName (Path.Combine (GetInitialFolder (), defaultFileName + exchanger.DefaultFileExtension));
                else if (!string.IsNullOrWhiteSpace (txtExchangeFile.Text) && !string.IsNullOrEmpty (defaultFileName)) {
                    string folder = Path.GetDirectoryName (txtExchangeFile.Text);
                    string file = Path.GetFileNameWithoutExtension (txtExchangeFile.Text);
                    SetExchangeFileName (Path.Combine (folder ?? string.Empty, file + exchanger.DefaultFileExtension));
                }
                if (chkExchangeFile.Active)
                    chkOpenFile.Sensitive = true;
                chkOpenFile.Active = BusinessDomain.AppConfiguration.OpenExportedFile;
            } else
                chkOpenFile.Active = chkOpenFile.Sensitive = false;

            algCustomWidget.DestroyChild ();

            Widget customWidget = exchanger.GetCustomWidget (data) as Widget;
            if (customWidget != null)
                algCustomWidget.Add (customWidget);
        }

        private void btnExchangeFile_Clicked (object sender, EventArgs e)
        {
            IDataExchanger exchanger = Exchanger;

            FileChooserFilter ff = new FileChooserFilter
                {
                    Name = string.Format ("{0} ({1})", exchanger.ExchangeType, string.Join ("; ", exchanger.FileMasks)),
                    FileMasks = exchanger.FileMasks
                };

            string fileName;
            if (IsExport) {
                string title = Translator.GetString ("Select Export File");
                string initialFolder = this.GetInitialFolder ();
                string initialFile = string.Format ("{0}{1}", this.defaultFileName, exchanger.DefaultFileExtension);
                if (PresentationDomain.OSIntegration.ChooseFileForSave (title, initialFolder, initialFile, out fileName, ff))
                    SetExchangeFileName (fileName);
            } else {
                if (PresentationDomain.OSIntegration.ChooseFileForOpen (Translator.GetString ("Select Import File"), GetInitialFolder (), out fileName, ff))
                    SetExchangeFileName (fileName);
            }
        }

        private void SetExchangeFileName (string fileName)
        {
            txtExchangeFile.Text = fileName;
            txtExchangeFile.Position = -1;
        }

        #endregion

        #region Location management

        [GLib.ConnectBefore]
        private void txtExchangeLocation_ButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type != EventType.TwoButtonPress)
                return;

            LocationChoose ();
            args.RetVal = true;
        }

        [GLib.ConnectBefore]
        private void txtExchangeLocation_KeyPress (object o, KeyPressEventArgs args)
        {
            if (!KeyShortcuts.Equal (args.Event, KeyShortcuts.ChooseKey))
                return;

            LocationChoose ();
            args.RetVal = true;
        }

        [GLib.ConnectBefore]
        private void txtExchnangeLocation_Changed (object sender, EventArgs e)
        {
            location = null;
        }

        private void btnExchangeLocation_Clicked (object sender, EventArgs e)
        {
            LocationChoose ();
        }

        private void LocationChoose ()
        {
            Location [] selected;

            if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditObjects") == UserRestrictionState.Allowed) {
                using (ChooseEditLocation dialog = new ChooseEditLocation (true, string.Empty)) {
                    if (dialog.Run () != ResponseType.Ok) {
                        txtExchangeLocation.GrabFocus ();
                        return;
                    }

                    selected = dialog.SelectedItems;
                }
            } else {
                selected = new Location [0];
            }

            if (selected.Length <= 0) {
                txtExchangeLocation.GrabFocus ();
                return;
            }

            SetLocation (selected [0]);
        }

        private void SetLocation (Location loc)
        {
            if (loc == null)
                return;

            txtExchangeLocation.Text = loc.Name;
            location = loc;
        }


        #endregion

        #region Event handling

        [UsedImplicitly]
        private void chkExchangeFile_Toggled (object o, EventArgs args)
        {
            hboExchangeFile.Sensitive = chkExchangeFile.Active;
            chkOpenFile.Sensitive = chkExchangeFile.Active;
            if (!chkExchangeFile.Active)
                chkEmail.Active = true;
        }

        [UsedImplicitly]
        private void chkEmail_Toggled (object o, EventArgs args)
        {
            bool emailActive = chkEmail.Active;
            txtEmail.Sensitive = emailActive;
            lblEmailSubject.Sensitive = emailActive;
            txtEmailSubject.Sensitive = emailActive;
            if (!chkEmail.Active)
                chkExchangeFile.Active = true;
        }

        protected virtual void btnOK_Clicked (object o, EventArgs args)
        {
            string locName = txtExchangeLocation.Text;
            if (usesLocation && location == null && !string.IsNullOrWhiteSpace (locName) && txtExchangeLocation.Sensitive) {
                Location loc = Location.GetByCode (locName) ?? Location.GetByName (locName);
                if (loc != null)
                    SetLocation (loc);
                else {
                    MessageError.ShowDialog (string.Format (Translator.GetString ("A location \"{0}\" cannot be found!"), locName));
                    return;
                }
            }

            if (chkExchangeFile.Active) {
                if (string.IsNullOrWhiteSpace (txtExchangeFile.Text)) {
                    MessageError.ShowDialog (Translator.GetString ("No file selected for export!"));
                    return;
                }

                string dir = Path.GetDirectoryName (txtExchangeFile.Text);
                if (!string.IsNullOrEmpty (dir) && !Directory.Exists (dir)) {
                    try {
                        Directory.CreateDirectory (dir);
                    } catch (Exception ex) {
                        MessageError.ShowDialog (string.Format (Translator.GetString ("Error occurred while creating folder \"{0}\". Please check if you have write permissions to that location."), dir), ErrorSeverity.Warning, ex);
                        return;
                    }
                }
            }

            if (chkEmail.Active) {
                if (string.IsNullOrEmpty (txtEmail.Text)) {
                    MessageError.ShowDialog (Translator.GetString ("No e-mail recipients entered!"));
                    return;
                }

                if (string.IsNullOrEmpty (txtEmailSubject.Text)) {
                    MessageError.ShowDialog (Translator.GetString ("No e-mail subject entered!"));
                    return;
                }
            }

            dlgExchangeObjects.Respond (ResponseType.Ok);
        }

        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgExchangeObjects.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
