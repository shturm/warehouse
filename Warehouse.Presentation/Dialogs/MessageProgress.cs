//
// MessageProgress.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   06/24/2006
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

using Glade;
using Gtk;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Business;

namespace Warehouse.Presentation.Dialogs
{
    public class MessageProgress : DialogBase
    {
        private readonly string title;
        private readonly string dialogIcon;
        private string message;
        private bool customProgressText;

        #region Glade Widgets

        [Widget]
        protected Dialog dlgMessageProgress;
        [Widget]
        protected Button btnStop;

        [Widget]
        protected Label lblDialogMessage;
        [Widget]
        protected ProgressBar prgDialogProgress;

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgMessageProgress; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        public double Progress
        {
            set
            {
                double val = Math.Min (value, 100.0d);
                val = Math.Max (val, -100.0d);

                prgDialogProgress.Fraction = val / 100.0d;
                if (!customProgressText)
                    prgDialogProgress.Text = Percent.ToString (val, 1);
            }
            get
            {
                return prgDialogProgress.Fraction * 100.0d;
            }
        }

        public string ProgressText
        {
            get { return prgDialogProgress.Text; }
            set { prgDialogProgress.Text = value; }
        }

        public double PulseStep
        {
            get { return prgDialogProgress.PulseStep; }
            set { prgDialogProgress.PulseStep = value; }
        }

        public bool CustomProgressText
        {
            get { return customProgressText; }
            set { customProgressText = value; }
        }

        public string Message
        {
            get { return message; }
            set
            {
                if (message == value)
                    return;

                message = value;
                lblDialogMessage.SetText (message);
            }
        }

        public MessageProgress (string title, string dialogIcon, string message)
        {
            this.title = title;
            this.dialogIcon = dialogIcon;
            this.message = message;

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.MessageProgress.glade", "dlgMessageProgress");
            form.Autoconnect (this);

            if (!string.IsNullOrEmpty (dialogIcon))
                dlgMessageProgress.Icon = FormHelper.LoadImage (dialogIcon).Pixbuf;

            dlgMessageProgress.Title = title;
            prgDialogProgress.PulseStep = 0.01;

            lblDialogMessage.SetText (message);
            btnStop.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            btnStop.Sensitive = false;

            base.InitializeForm ();

            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            btnStop.SetChildLabelText (Translator.GetString ("Stop"));
        }

        public void ProgressCallback (double percent)
        {
            Progress = percent;

            PresentationDomain.ProcessUIEvents ();
        }

        public void PulseCallback ()
        {
            prgDialogProgress.Pulse ();
            if (!customProgressText)
                prgDialogProgress.Text = string.Empty;

            PresentationDomain.ProcessUIEvents ();
        }

        #region Event handling

        protected override void OnResponseHandlerChanged (bool hasHandler)
        {
            btnStop.Sensitive = hasHandler;
        }

        #endregion
    }
}
