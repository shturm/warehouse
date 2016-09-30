//
// ImportObjects.cs
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
using Gtk;
using Warehouse.Business;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public class ImportObjects<T> : ExchangeObjects<T>
    {
        #region Public properties

        public IDataImporter Importer
        {
            get
            {
                return (IDataImporter) Exchanger;
            }
        }

        #endregion

        protected override bool IsExport
        {
            get { return false; }
        }

        public ImportObjects (bool usesLocation)
            : base (null, BusinessDomain.DataImporters, usesLocation)
        {
            for (int i = 0; i < exchangersModel.Count; i++) {
                if (exchangersModel [i].GetType ().Name != BusinessDomain.AppConfiguration.LastImporter)
                    continue;

                defaultSelectedRow = i;
                break;
            }
        }

        #region Event handling

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            dlgExchangeObjects.Icon = FormHelper.LoadImage ("Icons.Import24.png").Pixbuf;

            hboExchangeFile.Sensitive = true;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgExchangeObjects.Title = Translator.GetString ("Import Entities");
            lblExchangeFile.SetText (Translator.GetString ("Import from:"));
            VBox parent = (VBox) grbFile.Parent;
            parent.Remove (grbFile);
            hboExchangeFile.Unparent ();
            parent.Add (hboExchangeFile);
            parent.ReorderChild (hboExchangeFile, 1);
            hboExchangeFile.ShowAll ();
            chkExchangeFile.Visible = false;
            chkOpenFile.Visible = false;
            grbEmail.Visible = false;
            txtEmail.Visible = false;
            lblEmailSubject.Visible = false;
            txtEmailSubject.Visible = false;
            lblExchangeLocation.SetText (Translator.GetString ("Import to location:"));
        }

        protected override string GetInitialFolder ()
        {
            if (string.IsNullOrEmpty (BusinessDomain.AppConfiguration.LastImportFolder))
                BusinessDomain.AppConfiguration.LastImportFolder = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);

            return BusinessDomain.AppConfiguration.LastImportFolder;
        }

        protected override void btnOK_Clicked (object o, EventArgs args)
        {
            if (Exchanger.UsesFile && string.IsNullOrWhiteSpace (txtExchangeFile.Text)) {
                MessageError.ShowDialog (Translator.GetString ("Please specify an import file before continuing."));
                return;
            }

            BusinessDomain.AppConfiguration.LastImportFolder = Folder;
            if (grid.Selection.Count > 0)
                BusinessDomain.AppConfiguration.LastImporter = exchangersModel [grid.Selection [0]].GetType ().Name;
            
            base.btnOK_Clicked (o, args);
        }

        #endregion
    }
}
