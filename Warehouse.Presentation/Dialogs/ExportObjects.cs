//
// ExportObjects.cs
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
using System.Data;
using Warehouse.Business;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Presentation.Dialogs
{
    public class ExportObjects : ExportObjects<object>
    {
        public ExportObjects (string fileName, params IDataExchanger [] dataExchangers)
            : base (null, fileName)
        {
            grid.Model = exchangersModel = new BindingListModel<IDataExchanger> (dataExchangers);
            grid.Selection.Select (0);
        }
    }

    public class ExportObjects<T> : ExchangeObjects<T>
    {
        #region Public properties

        public IDataExporter Exporter
        {
            get { return (IDataExporter) Exchanger; }
        }

        #endregion

        public ExportObjects (DataTable data, string fileName)
            : base (data, BusinessDomain.DataExporters)
        {
            defaultFileName = fileName;

            for (int i = 0; i < exchangersModel.Count; i++) {
                if (exchangersModel [i].GetType ().Name != BusinessDomain.AppConfiguration.LastExporter)
                    continue;

                defaultSelectedRow = i;
                break;
            }
        }

        #region Event handling

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            dlgExchangeObjects.Icon = FormHelper.LoadImage ("Icons.Export24.png").Pixbuf;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgExchangeObjects.Title = Translator.GetString ("Export Entities");
            chkExchangeFile.Label = Translator.GetString ("Export to:");
            chkEmail.Label = Translator.GetString ("Send to e-mail:");
            chkOpenFile.Label = Translator.GetString ("Open file after export");
            lblEmailSubject.SetText (Translator.GetString ("E-mail subject:"));
            lblExchangeFile.Visible = false;
            lblExchangeLocation.SetText (Translator.GetString ("Export from location:"));
        }

        protected override string GetInitialFolder ()
        {
            if (string.IsNullOrEmpty (BusinessDomain.AppConfiguration.LastExportFolder))
                BusinessDomain.AppConfiguration.LastExportFolder = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);

            return BusinessDomain.AppConfiguration.LastExportFolder;
        }

        protected override void btnOK_Clicked (object o, EventArgs args)
        {
            if (Exchanger.UsesFile && string.IsNullOrWhiteSpace (txtExchangeFile.Text)) {
                MessageError.ShowDialog (Translator.GetString ("Please specify an export file before continuing."));
                return;
            }

        	BusinessDomain.AppConfiguration.LastExportToFile = chkExchangeFile.Active;
            if (chkOpenFile.Sensitive)
                BusinessDomain.AppConfiguration.OpenExportedFile = chkOpenFile.Active;
            BusinessDomain.AppConfiguration.LastExportToEmail = chkEmail.Active;
        	BusinessDomain.AppConfiguration.LastExportEmail = txtEmail.Text;
            BusinessDomain.AppConfiguration.LastExportEmailSubject = txtEmailSubject.Text;
            BusinessDomain.AppConfiguration.LastExportFolder = Folder;
            if (grid.Selection.Count > 0)
                BusinessDomain.AppConfiguration.LastExporter = exchangersModel [grid.Selection [0]].GetType ().Name;
            
            base.btnOK_Clicked (o, args);
        }

        #endregion
    }
}
