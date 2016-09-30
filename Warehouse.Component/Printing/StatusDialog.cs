//
// StatusDialog.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   03.15.2011
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
using Warehouse.Data;

namespace Warehouse.Component.Printing
{
    public class StatusDialog
    {
        private readonly ITranslationProvider translator;
        private readonly Dialog dlgStatus;
        private readonly Alignment algProgress;
        private readonly Label lblCount;
        private ProgressBar pgbCount;
        private readonly Label lblButtonText;
        private readonly Button button;
        private bool cancelled;
        private int current;
        private int? total;

        public int Current
        {
            set
            {
                current = value;

                RefreshMessage ();
            }
        }

        public int? Total
        {
            set
            {
                if (total == value)
                    return;

                total = value;
                RefreshMessage ();
            }
        }

        private string ButtonText
        {
            set
            {
                lblButtonText.SetText (value);

                RunMainLoop ();
            }
        }

        public bool Cancelled
        {
            get
            {
                RunMainLoop ();

                return cancelled;
            }
        }

        public StatusDialog (string dialogTitle, ITranslationProvider translator)
        {
            this.translator = translator;
            lblCount = new Label { UseMarkup = true };
            lblCount.SetText (translator.GetString ("Generating..."));

            algProgress = new Alignment (0.5f, 0.5f, 1, 1)
                {
                    LeftPadding = 10,
                    RightPadding = 10
                };
            algProgress.Add (lblCount);

            VBox da = new VBox { WidthRequest = 250 };

            lblButtonText = new Label { Xalign = 1f, UseMarkup = true, Text = "<span size=\"small\">lblText</span>" };
            lblButtonText.Show ();
            pgbCount = new ProgressBar ();
            pgbCount.Show ();

            #region Stop button setup

            Alignment algButtonIcon = new Alignment (0.5f, 0.5f, 1f, 1f)
                {
                    ComponentHelper.LoadImage ("Warehouse.Component.Printing.Icon.Cancel24.png")
                };

            HBox hboButton = new HBox { WidthRequest = 100 };
            hboButton.PackStart (algButtonIcon, false, false, 0);
            hboButton.PackStart (lblButtonText, true, true, 0);

            Alignment algButton = new Alignment (0.5f, 0.5f, 0f, 0f) { hboButton };

            button = new Button { WidthRequest = 110, HeightRequest = 34 };
            button.Add (algButton);
            button.Clicked += button_Clicked;

            #endregion

            dlgStatus = new Dialog { Title = dialogTitle };
            dlgStatus.VBox.PackStart (da, true, true, 0);
            dlgStatus.VBox.PackEnd (algProgress, true, true, 20);
            dlgStatus.AddActionWidget (button, ResponseType.Cancel);
            dlgStatus.DeleteEvent += dlgStatus_DeleteEvent;
            ButtonText = translator.GetString ("Cancel");
        }

        private void RefreshMessage ()
        {
            if (current > 0) {
                if (total != null && total > 0) {
                    if (!ReferenceEquals (algProgress.Child, pgbCount)) {
                        algProgress.Remove (algProgress.Child);
                        algProgress.Add (pgbCount);
                    }
                    pgbCount.Text = string.Format (translator.GetString ("Page {0} of {1}"), current, total);
                    pgbCount.Fraction = current / (double) total;
                } else {
                    if (!ReferenceEquals (algProgress.Child, lblCount)) {
                        algProgress.Remove (algProgress.Child);
                        algProgress.Add (lblCount);
                    }
                    lblCount.SetText (string.Format (translator.GetString ("Page {0} of the document"), current));
                }
            }

            RunMainLoop ();
        }

        private void dlgStatus_DeleteEvent (object o, DeleteEventArgs args)
        {
            args.RetVal = true;
            button_Clicked (o, args);
        }

        private void button_Clicked (object sender, EventArgs e)
        {
            button.Sensitive = false;
            ButtonText = translator.GetString ("Stopping...");

            cancelled = true;
        }

        public void Show ()
        {
            dlgStatus.PushModal ();
            dlgStatus.ShowAll ();
        }

        public void Hide ()
        {
            dlgStatus.Hide ();
            dlgStatus.PopModal ();
        }

        private static void RunMainLoop ()
        {
            while (Application.EventsPending ())
                Application.RunIteration (false);
        }
    }
}
