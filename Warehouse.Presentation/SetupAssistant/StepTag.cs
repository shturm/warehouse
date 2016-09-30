//
// StepTag.cs
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
using Warehouse.Component;

namespace Warehouse.Presentation.SetupAssistant
{
    public class StepTag : ToggleButton
    {
        private readonly Alignment algStatus;
        private readonly Label lblTitle;
        private readonly StepBase step;

        public StepBase Step
        {
            get { return step; }
        }

        public StepTag (StepBase step)
        {
            this.step = step;
            step.StatusChanged += step_StatusChanged;

            Relief = ReliefStyle.None;
            HBox hbo = new HBox { Spacing = 2 };
            Add (hbo);

            algStatus = new Alignment (0.5f, 0.5f, 1f, 1f) { WidthRequest = 16, HeightRequest = 16 };
            hbo.PackStart (algStatus, false, true, 0);

            lblTitle = new Label { Markup = new PangoStyle { Italic = true, Text = step.Title }, Xpad = 4 };
            hbo.PackStart (lblTitle, true, true, 0);
            hbo.ShowAll ();

            RefreshStatus ();
        }

        private void step_StatusChanged (object sender, EventArgs e)
        {
            RefreshStatus ();
        }

        private void RefreshStatus ()
        {
            algStatus.DestroyChild ();

            Image image = GetStatusImage (step.Status);
            if (image == null)
                return;

            image.Show ();
            algStatus.Add (image);
        }

        public static Image GetStatusImage (StepStatus status)
        {
            switch (status) {
                case StepStatus.Incomplete:
                    return FormHelper.LoadLocalImage ("Warehouse.Presentation.SetupAssistant.Icons.Question16.png");
                case StepStatus.Waiting:
                    return FormHelper.LoadLocalImage ("Warehouse.Presentation.SetupAssistant.Icons.Pause16.png");
                case StepStatus.InProgress:
                    return FormHelper.LoadLocalAnimation ("Warehouse.Presentation.SetupAssistant.Icons.Working16.gif");
                case StepStatus.Failed:
                    return FormHelper.LoadLocalImage ("Warehouse.Presentation.SetupAssistant.Icons.Failed16.png");
                case StepStatus.Complete:
                    return FormHelper.LoadLocalImage ("Warehouse.Presentation.SetupAssistant.Icons.Completed16.png");
                default:
                    return null;
            }
        }
    }
}
