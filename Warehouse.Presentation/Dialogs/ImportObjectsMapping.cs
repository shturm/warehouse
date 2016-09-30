//
// ImportObjectsMapping.cs
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
using System.Linq;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Component;
using Image = Gtk.Image;

namespace Warehouse.Presentation.Dialogs
{
    public class ImportObjectsMapping<T> : DialogBase
    {
        private class ImportProperty : ExchangePropertyInfo
        {
            public ComboBox Combo { get; set; }
        }

        private readonly IDataImporter importer;
        private readonly string file;
        private readonly List<ImportProperty> exchangeProps = new List<ImportProperty> ();
        private Dictionary<int, string> importHeaders;
        private PropertyMap propertyMap;

        public override Dialog DialogControl
        {
            get { return dlgImportObjectsMapping; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        #region Public properties

        public PropertyMap PropertyMap
        {
            get { return propertyMap; }
        }

        #endregion

        #region Glade Widgets

        [Widget]
        protected Dialog dlgImportObjectsMapping;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;
        [Widget]
        protected Button btnReset;
        [Widget]
        protected Alignment algDialogIcon;
        [Widget]
        protected Table tblEntities;

        #endregion

        public ImportObjectsMapping (IDataImporter importer, string file)
        {
            if (importer == null)
                throw new ArgumentNullException ("importer");
            if (file == null)
                throw new ArgumentNullException ("file");

            this.importer = importer;
            this.file = file;

            Initialize ();
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ImportObjectsMapping.glade", "dlgImportObjectsMapping");
            form.Autoconnect (this);

            dlgImportObjectsMapping.Icon = FormHelper.LoadImage ("Icons.Import24.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            btnReset.SetChildImage (FormHelper.LoadImage ("Icons.Clear24.png"));

            Image img = FormHelper.LoadImage ("Icons.Import24.png");
            algDialogIcon.Add (img);
            img.Show ();

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeFields ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgImportObjectsMapping.Title = Translator.GetString ("Select source columns");
            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
            btnReset.SetChildLabelText (Translator.GetString ("Reset"));
        }

        private void InitializeFields ()
        {
            importHeaders = new Dictionary<int, string> ();
            // Insert an empty mapping for properties that will not be mapped
            importHeaders.Add (-1, string.Empty);
            foreach (string header in importer.GetDataHeaders (file))
                importHeaders.Add (importHeaders.Count - 1, header);

            foreach (ImportProperty prop in ExchangeHelper.GetExchangeInfo<ImportProperty> (typeof (T)))
                AppendProperty (prop);
        }

        private void AppendProperty (ImportProperty prop)
        {
            uint rows = tblEntities.NRows;

            Label lblFieldName = new Label { Markup = new PangoStyle { Bold = prop.IsRequired, Text = prop.Name }, Xalign = 0 };
            tblEntities.Attach (lblFieldName, 0, 1, rows, rows + 1,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 1);

            VBox da = new VBox { WidthRequest = 10 };
            tblEntities.Attach (da, 1, 2, rows, rows + 1);

            prop.Combo = new ComboBox { WidthRequest = 180 };
            int selValue = -1;
            foreach (KeyValuePair<int, string> pair in importHeaders.Where (pair => pair.Value == prop.DefaultMapping)) {
                selValue = pair.Key;
                break;
            }

            prop.Combo.Load (importHeaders, "Key", "Value", selValue);
            tblEntities.Attach (prop.Combo, 2, 3, rows, rows + 1,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 1);

            exchangeProps.Add (prop);
        }

        #endregion

        public override void Show ()
        {
            tblEntities.ShowAll ();
            base.Show ();
        }

        public override ResponseType Run ()
        {
            tblEntities.ShowAll ();
            return base.Run ();
        }

        #region Event handling

        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (!ValidateMappings ())
                return;

            dlgImportObjectsMapping.Respond (ResponseType.Ok);
        }

        private bool ValidateMappings ()
        {
            foreach (ImportProperty property in exchangeProps) {
                property.MappedColumn = (int) property.Combo.GetSelectedValue ();
                if (!property.IsRequired || property.MappedColumn >= 0)
                    continue;

                MessageError.ShowDialog (
                    string.Format (Translator.GetString ("The property \"{0}\" is required and must have a valid mapping before the import process can begin!"), property.Name),
                    ErrorSeverity.Error);

                return false;
            }

            propertyMap = ExchangeHelper.GeneratePropertyMap (exchangeProps);
            return true;
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgImportObjectsMapping.Respond (ResponseType.Cancel);
        }

        [UsedImplicitly]
        protected void btnReset_Clicked (object o, EventArgs args)
        {
            foreach (ImportProperty prop in exchangeProps) {
                int selValue = -1;
                foreach (KeyValuePair<int, string> pair in importHeaders) {
                    if (pair.Value != prop.DefaultMapping)
                        continue;
                    selValue = pair.Key;
                    break;
                }

                prop.Combo.SetSelection (importHeaders, "Key", "Value", selValue);
            }
        }

        #endregion
    }
}
