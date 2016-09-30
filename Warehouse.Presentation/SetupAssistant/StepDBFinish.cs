//
// StepDBFinish.cs
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
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.SetupAssistant
{
    [Extension ("/Warehouse/Presentation/SetupAssistant/Step")]
    public class StepDBFinish : StepBase
    {
        private WrapLabel wlbBody;
        private Table tblUnfinished;

        #region Overrides of StepBase

        public override string Title
        {
            get { return Translator.GetString ("Finished"); }
        }

        public override int Ordinal
        {
            get { return 100; }
        }

        public override AssistType Group
        {
            get { return AssistType.DatabaseSetup; }
        }

        #endregion

        protected override void CreateBody ()
        {
            vboBody = new VBox ();

            lblTitle = new Label ();
            vboBody.PackStart (lblTitle, false, true, 10);

            wlbBody = new WrapLabel ();
            vboBody.PackStart (wlbBody, true, true, 0);
            vboBody.ShowAll ();

            tblUnfinished = new Table (1, 1, false);
            vboBody.PackStart (tblUnfinished, true, true, 10);

            Assistant.StatusChanged += Assistant_StatusChanged;
            if (AllStepsFinished ())
                SetMessageCompleted ();
            else
                SetMessageIncomplete ();
        }

        private void SetMessageCompleted ()
        {
            lblTitle.Markup = new PangoStyle
                {
                    Bold = true,
                    Size = PangoStyle.TextSize.XLarge,
                    Text = Translator.GetString ("Database setup is now complete")
                };
            wlbBody.Markup = new PangoStyle
                {
                    Size = PangoStyle.TextSize.Large,
                    Markup = string.Format (Translator.GetString (
                        "The setup of your database is now complete. You can now start exploring {0}.{1}Don't forget that you can receive instant help right on the spot you need it by pressing the {2} key on your keyboard!"),
                        DataHelper.ProductName, Environment.NewLine, new PangoStyle { Text = "F1", Bold = true })
                };
            tblUnfinished.Hide ();
            ChangeStatus (StepStatus.Complete);
        }

        private void SetMessageIncomplete ()
        {
            lblTitle.Markup = new PangoStyle
                {
                    Bold = true,
                    Size = PangoStyle.TextSize.XLarge,
                    Text = Translator.GetString ("Database setup is still finishing")
                };
            wlbBody.Markup = new PangoStyle
                {
                    Size = PangoStyle.TextSize.Large,
                    Markup = string.Format (Translator.GetString ("The setup of your database is still in progress. Please wait while all the necessary actions are finalized.{0}Don't forget that when you enter {1} you can receive instant help right on the spot you need it by pressing the {2} key on your keyboard!"),
                        Environment.NewLine, DataHelper.ProductName, new PangoStyle { Text = "F1", Bold = true })
                };

            UpdateStepStatuses ();
        }

        private void UpdateStepStatuses ()
        {
            for (int i = tblUnfinished.Children.Length - 1; i >= 0; i--) {
                Widget child = tblUnfinished.Children [i];
                tblUnfinished.Remove (child);
                child.Destroy ();
            }

            uint r = 0;
            foreach (StepBase step in Assistant.Steps) {
                if (step is StepDBSetup ||
                    step is StepDBFinish)
                    continue;

                tblUnfinished.Attach (StepTag.GetStatusImage (step.Status), 1, 2, r, r + 1, AttachOptions.Fill, AttachOptions.Fill, 4, 4);
                tblUnfinished.Attach (new Label (step.Title) { Xalign = 0f }, 2, 3, r, r + 1, AttachOptions.Fill, AttachOptions.Fill, 4, 4);
                r++;
            }

            tblUnfinished.Attach (new Label (), 0, 1, 0, r, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 0);
            tblUnfinished.Attach (new Label (), 3, 4, 0, r, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 0);
            tblUnfinished.ShowAll ();
        }

        private bool AllStepsFinished ()
        {
            foreach (StepBase step in Assistant.Steps) {
                switch (step.Status) {
                    case StepStatus.Waiting:
                    case StepStatus.InProgress:
                    case StepStatus.Failed:
                        return false;
                }
            }

            return true;
        }

        private void Assistant_StatusChanged (object sender, EventArgs e)
        {
            if (!AllStepsFinished ()) {
                UpdateStepStatuses ();
                return;
            }

            PresentationDomain.Invoke (SetMessageCompleted);
        }
    }
}
