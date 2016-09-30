//
// StepLocations.cs
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
using Warehouse.Component;

namespace Warehouse.Presentation.SetupAssistant
{
    [Extension ("/Warehouse/Presentation/SetupAssistant/Step")]
    public class StepLocations : StepBase
    {
        private readonly List<LocationsSetup> schemas = new List<LocationsSetup> ();
        private Notebook nbkMain;

        public Notebook Notebook
        {
            get { return nbkMain; }
        }

        #region Overrides of StepBase

        public override string Title
        {
            get { return Translator.GetString ("Locations"); }
        }

        public override int Ordinal
        {
            get { return 30; }
        }

        public override AssistType Group
        {
            get { return AssistType.DatabaseSetup; }
        }

        public override bool IsSingleComplete
        {
            get { return true; }
        }

        #endregion

        protected override void CreateBody ()
        {
            CreateBody (Translator.GetString ("Choose your locations"));

            WrapLabel footer = new WrapLabel
                {
                    Markup = string.Format (Translator.GetString (
                        "Locations are used for your physical locations where you store items, place orders, or make purchases from. To create the locations quickly use the form bellow. To edit your locations later go to:{0}{1}"),
                        Environment.NewLine,
                        new PangoStyle
                            {
                                Italic = true,
                                Bold = true,
                                Text = Translator.GetString ("Edit->Locations...")
                            })
                };
            footer.Show ();
            vboBody.PackStart (footer, false, true, 0);

            if (schemas.Count == 0) {
                foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Presentation/SetupAssistant/LocationsSetup")) {
                    object instance = node.CreateInstance ();
                    LocationsSetup schema = instance as LocationsSetup;
                    if (schema != null)
                        schemas.Add (schema);
                }
                schemas.Sort ((p1, p2) => Math.Max (-1, Math.Min (1, p1.Ordinal - p2.Ordinal)));
            }

            if (schemas.Count > 1) {
                nbkMain = new Notebook ();
                foreach (LocationsSetup schema in schemas) {
                    Label lblTab = new Label (schema.Label) { Xpad = 4 };
                    Widget page = schema.GetPageWidget ();
                    page.Show ();

                    Alignment alg = new Alignment (.5f, .5f, 1f, 1f) { LeftPadding = 4, RightPadding = 4, TopPadding = 4, BottomPadding = 4 };
                    alg.Add (page);
                    alg.Show ();

                    EventBox evb = new EventBox { alg };
                    evb.Show ();

                    nbkMain.AppendPage (evb, lblTab);
                }
                nbkMain.Show ();
                vboBody.PackStart (nbkMain, true, true, 10);
            } else if (schemas.Count == 1) {
                Widget page = schemas [0].GetPageWidget ();
                page.Show ();

                Alignment alg = new Alignment (.5f, .5f, 1f, 1f) { LeftPadding = 4, RightPadding = 4, TopPadding = 4, BottomPadding = 4 };
                alg.Add (page);
                alg.Show ();

                vboBody.PackStart (alg, true, true, 10);
            }
        }

        public override bool Complete (Assistant assistant)
        {
            LocationsSetup schema = schemas.Count == 1 ? schemas [0] : schemas [nbkMain.CurrentPage];

            return schema.Validate () && schema.CommitChanges (this, assistant);
        }

        public override void Dispose ()
        {
            base.Dispose ();

            foreach (LocationsSetup schema in schemas)
                schema.Dispose ();
        }
    }
}
