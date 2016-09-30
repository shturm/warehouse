//
// VisualizerSettingsDialog.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/21/2006
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
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Presentation.Dialogs;

namespace Warehouse.Presentation.Visualization
{
    public class VisualizerSettingsDialog : DialogBase
    {
        private readonly DataQueryResult result;
        private readonly VisualizerSettingsCollection settings;
        private readonly List<VisualizerSettingsPageBase> settingsPages = new List<VisualizerSettingsPageBase> ();

        #region Glade Widgets

        [Widget]
        protected Dialog dlgVisualizerSettings;
        [Widget]
        protected Button btnSave;
        [Widget]
        protected Button btnCancel;

        [Widget]
        protected Notebook nbMain;

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgVisualizerSettings; }
        }

        public override string HelpFile
        {
            get { return "VisualizerSettings.html"; }
        }

        public List<VisualizerSettingsPageBase> SettingsPages
        {
            get { return settingsPages; }
        }

        private static ExtensionNodeList settingsNodes;

        public static ExtensionNodeList SettingsNodes
        {
            get { return settingsNodes ?? (settingsNodes = AddinManager.GetExtensionNodes ("/Warehouse/Presentation/VisualizationSettings")); }
        }

        public VisualizerSettingsDialog (DataQueryResult result, VisualizerSettingsCollection settings)
        {
            this.result = result;
            this.settings = settings;

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Visualization.VisualizerSettingsDialog.glade", "dlgVisualizerSettings");
            form.Autoconnect (this);

            dlgVisualizerSettings.Icon = FormHelper.LoadImage ("Icons.Visualizer24.png").Pixbuf;
            dlgVisualizerSettings.Title = Translator.GetString ("Visualization Settings");
            btnSave.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeVisualizerPages ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            btnSave.SetChildLabelText (Translator.GetString ("Save"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        private void InitializeVisualizerPages ()
        {
            string curentVisTypeName = settings.GetVisualizerType ().AssemblyQualifiedName;

            foreach (VisualizerSettingsPageBase settingsPage in SettingsNodes.Cast<TypeExtensionNode> ()
                .Select (node => node.CreateInstance ())
                .OfType<VisualizerSettingsPageBase> ()
                .Where (settingsPage => settingsPage.LoadSettings (result, settings))) {
                settingsPage.Show ();
                nbMain.AppendPage (settingsPage, settingsPage.PageLabel);
                settingsPages.Add (settingsPage);

                if (settingsPage.VisualizerTypeName == curentVisTypeName)
                    nbMain.CurrentPage = settingsPages.Count - 1;
            }
        }

        #region Event handling

        protected virtual void btnOK_Clicked (object o, EventArgs args)
        {
            foreach (VisualizerSettingsPageBase settingsPage in settingsPages) {
                settingsPage.OnSavingSettings ();

                if (!settingsPage.SaveSettings (settings))
                    return;
            }

            settings.SetSettings (new CurrentVisualizerSettings (settingsPages [nbMain.CurrentPage].VisualizerTypeName));
            dlgVisualizerSettings.Respond (ResponseType.Ok);
        }

        protected virtual void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgVisualizerSettings.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
