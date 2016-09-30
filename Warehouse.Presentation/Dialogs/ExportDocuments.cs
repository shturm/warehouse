//
// ExportDocuments.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   02.24.2011
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
using System.Linq;
using Warehouse.Business;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Presentation.Dialogs
{
    public class ExportDocuments : ExchangeObjects<object>
    {
        public IDocumentExporter DocumentExporter
        {
            get { return (IDocumentExporter) Exchanger; }
        }

        public ExportDocuments (Type sourceObjectType, string fileName)
            : base (null, BusinessDomain.DocumentExporters)
        {
            defaultFileName = fileName;

            exchangersModel = new BindingListModel<IDataExchanger> (exchangers
                .Where (exporter => exporter.SupportsType (sourceObjectType) && exporter.IsAvailable), false);
            exchangersModel.Sort ("ExchangeType", SortDirection.Ascending);
            grid.Model = exchangersModel;
            
            for (int i = 0; i < exchangersModel.Count; i++) {
                if (exchangersModel [i].GetType ().Name != BusinessDomain.AppConfiguration.LastDocsExporter)
                    continue;

                defaultSelectedRow = i;
                break;
            }
        }

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            dlgExchangeObjects.Icon = FormHelper.LoadImage ("Icons.Export24.png").Pixbuf;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgExchangeObjects.Title = Translator.GetString ("Export Document");
            chkExchangeFile.Label = Translator.GetString ("Export to:");
            chkEmail.Label = Translator.GetString ("Send to e-mail:");
            chkOpenFile.Label = Translator.GetString ("Open file after export");
            lblEmailSubject.SetText (Translator.GetString ("E-mail subject:"));
            lblExchangeFile.Visible = false;
            lblExchangeLocation.SetText (Translator.GetString ("Export from location:"));
        }

        protected override string GetInitialFolder ()
        {
        	if (string.IsNullOrEmpty (BusinessDomain.AppConfiguration.LastExportDocsFolder))
				BusinessDomain.AppConfiguration.LastExportDocsFolder = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);

            return BusinessDomain.AppConfiguration.LastExportDocsFolder;
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
            BusinessDomain.AppConfiguration.LastExportDocsFolder = Folder;
            if (grid.Selection.Count > 0)
                BusinessDomain.AppConfiguration.LastDocsExporter = exchangersModel [grid.Selection [0]].GetType ().Name;
            
            base.btnOK_Clicked (o, args);
        }
    }
}
