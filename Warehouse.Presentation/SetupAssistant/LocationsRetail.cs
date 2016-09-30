//
// LocationsRetail.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   07.07.2011
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
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Presentation.Dialogs;

namespace Warehouse.Presentation.SetupAssistant
{
    [Extension ("/Warehouse/Presentation/SetupAssistant/LocationsSetup")]
    public class LocationsRetail : LocationsSetup
    {
        private Table tbl;
        private RadioButton rbnSingleStore;
        private RadioButton rbnMultiStore;
        private Label lblNumStores;
        private SpinButton spbNumStores;
        private Table tblLocations;

        private readonly List<Location> locations = new List<Location> ();
        private readonly Dictionary<Location, Entry> entries = new Dictionary<Location, Entry> ();

        #region Overrides of LocationsSet

        public override int Ordinal
        {
            get { return 10; }
        }

        public override string Label
        {
            get { return Translator.GetString ("Retail store"); }
        }

        public override Widget GetPageWidget ()
        {
            if (tbl != null)
                return tbl;

            rbnSingleStore = new RadioButton (Translator.GetString ("Single store"));
            rbnMultiStore = new RadioButton (rbnSingleStore, Translator.GetString ("Multiple stores"));
            rbnMultiStore.Toggled += rbnMultiStore_Toggled;
            lblNumStores = new Label (Translator.GetString ("Number of stores:")) { Xalign = 1f };
            spbNumStores = new SpinButton (1d, 20d, 1d) { Alignment = 1f };
            spbNumStores.ValueChanged += spbNumStores_Changed;

            Label lblStoresList = new Label (Translator.GetString ("Locations to be created:")) { Xalign = 0f };
            tblLocations = new Table (1, 1, false) { RowSpacing = 2 };

            tbl = new Table (1, 2, false) { RowSpacing = 4 };
            tbl.Attach (rbnSingleStore, 0, 3, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 2);
            tbl.Attach (rbnMultiStore, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 2);
            tbl.Attach (lblNumStores, 1, 2, 1, 2, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 4, 0);
            tbl.Attach (spbNumStores, 2, 3, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            tbl.Attach (lblStoresList, 0, 3, 2, 3, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 10, 2);
            tbl.Attach (tblLocations, 0, 3, 3, 4, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill | AttachOptions.Expand, 0, 0);
            tbl.ShowAll ();

            rbnMultiStore_Toggled (rbnMultiStore, EventArgs.Empty);

            return tbl;
        }

        public override bool Validate ()
        {
            foreach (Location location in locations)
                location.Name = entries [location].Text;

            for (int i = 0; i < locations.Count - 2; i++) {
                for (int j = i + 1; j < locations.Count - 1; j++) {
                    if (locations [i].Name != locations [j].Name)
                        continue;

                    if (MessageError.ShowDialog (string.Format (Translator.GetString ("Location with the name \"{0}\" is used more than once! Are you sure you want to continue?"), locations [i].Name),
                        buttons: MessageButtons.YesNo) != ResponseType.Yes)
                        return false;
                }
            }

            foreach (Location location in locations) {
                if (!location.Validate ((message, severity, code, state) =>
                    {
                        using (MessageError dlgError = new MessageError (message, severity))
                            if (severity == ErrorSeverity.Warning) {
                                dlgError.Buttons = MessageButtons.YesNo;
                                if (dlgError.Run () != ResponseType.Yes)
                                    return false;
                            } else {
                                dlgError.Run ();
                                return false;
                            }

                        return true;
                    }, null))
                    return false;
            }

            return true;
        }

        public override bool CommitChanges (StepLocations step, Assistant assistant)
        {
            BackgroundJob job = new BackgroundJob (step);
            job.Action += () =>
                {
                    PresentationDomain.Invoke (() =>
                        {
                            if (step.Notebook != null)
                                step.Notebook.Sensitive = false;
                            else
                                tbl.Sensitive = false;
                        });
                    // Substitute the default location
                    if (locations.Count > 0)
                        locations [0].Id = Location.DefaultId;

                    foreach (Location location in locations)
                        location.CommitChanges ();
                };
            assistant.EnqueueBackgroundJob (job);

            return true;
        }

        private void spbNumStores_Changed (object sender, EventArgs e)
        {
            RecreateLocations (false);
        }

        private void rbnMultiStore_Toggled (object sender, System.EventArgs e)
        {
            if (rbnMultiStore.Active) {
                lblNumStores.Sensitive = true;
                spbNumStores.Sensitive = true;
            } else {
                lblNumStores.Sensitive = false;
                spbNumStores.Sensitive = false;
            }

            RecreateLocations (true);
        }

        private void RecreateLocations (bool toggle)
        {
            foreach (Location location in locations)
                location.Name = entries [location].Text;

            int i, j;

            if (rbnSingleStore.Active) {
                int removeUntil = 1;
                if (toggle)
                    removeUntil = 0;

                for (i = locations.Count - 1; i >= removeUntil; i--) {
                    Location location = locations [i];
                    entries.Remove (location);
                    locations.Remove (location);
                }

                if (locations.Count == 0)
                    locations.Add (new Location { Name = Translator.GetString ("Store") });
            } else {
                int removeUntil = spbNumStores.ValueAsInt;
                if (toggle)
                    removeUntil = 0;

                for (i = locations.Count - 1; i >= removeUntil; i--) {
                    Location location = locations [i];
                    entries.Remove (location);
                    locations.Remove (location);
                }

                for (i = locations.Count, j = locations.Count; i < spbNumStores.ValueAsInt; i++) {
                    string [] newName = { GetNewName (j++) };
                    while (!locations.TrueForAll (l => l.Name != newName [0]))
                        newName [0] = GetNewName (j++);

                    locations.Add (new Location { Name = newName [0] });
                }
            }

            RecreateLocationsTable ();
        }

        private static string GetNewName (int index)
        {
            return string.Format ("{0} {1}", Translator.GetString ("Store"), index + 1);
        }

        private void RecreateLocationsTable ()
        {
            for (int i = tblLocations.Children.Length - 1; i >= 0; i--)
                tblLocations.Remove (tblLocations.Children [i]);

            uint index = 0;
            foreach (Location location in locations) {
                string name = location.Name;
                Entry oldEntry;
                if (entries.TryGetValue (location, out oldEntry))
                    name = oldEntry.Text;

                Entry txtName = new Entry (name) { WidthChars = 30 };
                txtName.Show ();
                entries [location] = txtName;

                tblLocations.Attach (txtName, index % 2, (index % 2) + 1, index / 2, (index / 2) + 1, AttachOptions.Fill, AttachOptions.Fill, 10, 0);

                index++;
            }
        }

        #endregion
    }
}
