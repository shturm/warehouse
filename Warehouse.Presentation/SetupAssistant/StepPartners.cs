//
// StepPartners.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   08.16.2011
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
    public class StepPartners : StepBase
    {
        private readonly List<PartnersSetup> setups = new List<PartnersSetup> ();
        private Notebook nbkMain;
        private WrapLabel wlbError;
        private Label lblProgress;
        private ProgressBar pgbProgress;

        public Notebook Notebook
        {
            get { return nbkMain; }
        }

        public WrapLabel ErrorLabel
        {
            get { return wlbError; }
        }

        public Label ProgressLabel
        {
            get { return lblProgress; }
        }

        public ProgressBar ProgressBar
        {
            get { return pgbProgress; }
        }

        #region Overrides of StepBase

        public override string Title
        {
            get { return Translator.GetString ("Partners"); }
        }

        public override int Ordinal
        {
            get { return 40; }
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
            CreateBody (Translator.GetString ("Choose your partners"));

            WrapLabel footer = new WrapLabel
                {
                    Markup = string.Format (Translator.GetString (
                        "Partners are the companies and people you will be selling to and buying from. To modify your partners later go to:{0}{1}"),
                        Environment.NewLine,
                        new PangoStyle
                            {
                                Italic = true,
                                Bold = true,
                                Text = Translator.GetString ("Edit->Partners...")
                            })
                };
            footer.Show ();
            vboBody.PackStart (footer, false, true, 0);

            if (setups.Count == 0) {
                foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Presentation/SetupAssistant/PartnersSetup")) {
                    object instance = node.CreateInstance ();
                    PartnersSetup setup = instance as PartnersSetup;
                    if (setup != null)
                        setups.Add (setup);
                }
                setups.Sort ((p1, p2) => Math.Max (-1, Math.Min (1, p1.Ordinal - p2.Ordinal)));
            }

            nbkMain = new Notebook ();
            if (setups.Count > 0) {
                foreach (PartnersSetup setup in setups) {
                    Label lblTab = new Label (setup.Label) {Xpad = 4};
                    Widget page = setup.GetPageWidget ();
                    page.Show ();

                    Alignment alg = new Alignment (.5f, .5f, 1f, 1f) {LeftPadding = 4, RightPadding = 4, TopPadding = 4, BottomPadding = 4};
                    alg.Add (page);
                    alg.Show ();

                    EventBox evb = new EventBox {alg};
                    evb.Show ();

                    nbkMain.AppendPage (evb, lblTab);
                }
                nbkMain.Show ();
            }
            vboBody.PackStart (nbkMain, true, true, 10);

            wlbError = new WrapLabel ();
            vboBody.PackStart (wlbError, false, true, 0);

            pgbProgress = new ProgressBar { Fraction = 0.00 };
            vboBody.PackEnd (pgbProgress, false, true, 2);

            lblProgress = new Label ();
            vboBody.PackEnd (lblProgress, false, true, 4);
        }

        public override bool Complete (Assistant assistant)
        {
            PartnersSetup setup = setups [nbkMain.CurrentPage];

            return setup.Validate () && setup.CommitChanges (this, assistant);
        }

        public override void Dispose ()
        {
            base.Dispose ();

            foreach (PartnersSetup setup in setups)
                setup.Dispose ();
        }
    }
}
