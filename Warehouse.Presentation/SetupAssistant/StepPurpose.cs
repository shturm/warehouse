//
// StepPurpose.cs
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
using Warehouse.Data;

namespace Warehouse.Presentation.SetupAssistant
{
    [Extension ("/Warehouse/Presentation/SetupAssistant/Step")]
    public class StepPurpose : StepBase
    {
        private readonly List<PurposeBase> choices = new List<PurposeBase> ();
        private Alignment algImage;

        #region Overrides of StepBase

        public override string Title
        {
            get { return Translator.GetString ("Purpose"); }
        }

        public override int Ordinal
        {
            get { return 10; }
        }

        public override AssistType Group
        {
            get { return AssistType.ApplicationSetup; }
        }

        #endregion

        protected override void CreateBody ()
        {
            CreateBody (Translator.GetString ("What is the main purpose of this workstation?"));
        
            List<PurposeBase> allPurposes = new List<PurposeBase> ();
            foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Presentation/SetupAssistant/Purpose")) {
                object instance = node.CreateInstance ();
                PurposeBase purpose = instance as PurposeBase;
                if (purpose != null)
                    allPurposes.Add (purpose);
            }
            allPurposes.Sort ((p1, p2) => Math.Max (-1, Math.Min (1, p1.Ordinal - p2.Ordinal)));

            HBox hbo = new HBox ();
            hbo.Show ();
            vboBody.PackStart (hbo, true, true, 6);

            VBox vbo = new VBox ();
            vbo.Show ();
            hbo.PackStart (vbo, true, true, 4);

            algImage = new Alignment (.5f, .5f, 1f, 1f) { WidthRequest = 220 };
            algImage.Show ();
            hbo.PackStart (algImage, false, true, 4);

            RadioButton group = null;
            bool activeSet = false;
            foreach (PurposeBase purpose in allPurposes) {
                Widget rbtn = purpose.GetSelectionWidget (ref group);
                rbtn.Show ();
                purpose.Toggled += rbtn_Toggled;

                vbo.PackStart (rbtn, false, true, 4);
                choices.Add (purpose);

                if (activeSet)
                    continue;

                activeSet = true;
                purpose.Active = true;
                rbtn_Toggled (rbtn, EventArgs.Empty);
            }

            WrapLabel footer = new WrapLabel
                {
                    Markup = string.Format (Translator.GetString (
                        "The selection here will determine the content displayed when {1} is started. To change this later please go to:{0}{2}"),
                        Environment.NewLine,
                        DataHelper.ProductName,
                        new PangoStyle
                            {
                                Italic = true,
                                Bold = true,
                                Text = Translator.GetString ("Other->Settings->Special->Startup->Startup page")
                            })
                };
            footer.Show ();
            vboBody.PackStart (footer, true, true, 4);
        }

        private void rbtn_Toggled (object sender, EventArgs e)
        {
            RadioButton rbtn = (RadioButton) sender;
            if (!rbtn.Active)
                return;

            algImage.DestroyChild ();

            PurposeBase purpose = rbtn.Data ["PurposeBase"] as PurposeBase;
            if (purpose == null)
                return;

            Image img = purpose.Image;
            if (img == null)
                return;

            img.Show ();
            algImage.Add (img);
        }

        public override bool Complete (Assistant assistant)
        {
            foreach (PurposeBase choice in choices) {
                if (!choice.Active)
                    continue;

                choice.Select ();
                break;
            }

            return base.Complete (assistant);
        }
    }
}
