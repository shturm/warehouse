//
// Message.cs
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

using Warehouse.Component;
using Warehouse.Business;

namespace Warehouse.Presentation.Dialogs
{
    [Flags]
    public enum MessageButtons
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Retry = 4,
        Yes = 8,
        No = 16,
        Remember = 32,
        Restart = 64,
        OKCancel = OK | Cancel,
        YesNo = Yes | No,
        All = OK | Cancel | Retry | Yes | No | Remember | Restart
    }

    public class Message : DialogBase
    {
        protected string title;
        protected string dialogIcon;
        protected string message;
        protected string messageIcon;
        private MessageButtons buttons;
        protected WrapLabel lblDialogMessage;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgMessage;

        [Widget]
        protected Alignment algDialogMessage;
        [Widget]
        protected VBox vboContents;
        [Widget]
        private Alignment algDialogIcon;
        [Widget]
        private CheckButton chkRememberChoice;

        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnRetry;
        [Widget]
        private Button btnCancel;
        [Widget]
        protected Button btnYes;
        [Widget]
        protected Button btnNo;
        [Widget]
        private Button btnRestart;

#pragma warning restore 649

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgMessage; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        public bool RememberChoice
        {
            get { return chkRememberChoice.Active; }
        }

        public MessageButtons Buttons
        {
            get { return buttons; }
            set
            {
                buttons = value;

                btnOK.Visible = ((buttons & MessageButtons.OK) != 0);
                btnRetry.Visible = ((buttons & MessageButtons.Retry) != 0);
                btnCancel.Visible = ((buttons & MessageButtons.Cancel) != 0);
                btnYes.Visible = ((buttons & MessageButtons.Yes) != 0);
                btnNo.Visible = ((buttons & MessageButtons.No) != 0);
                btnRestart.Visible = ((buttons & MessageButtons.Restart) != 0);
                chkRememberChoice.Visible = ((buttons & MessageButtons.Remember) != 0);
            }
        }

        public Message ()
        {
        }

        public Message (string title, string dialogIcon, string message, string messageIcon)
        {
            this.title = title;
            this.dialogIcon = dialogIcon;
            this.message = message;
            this.messageIcon = messageIcon;

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.Message.glade", "dlgMessage");
            form.Autoconnect (this);

            dlgMessage.Icon = string.IsNullOrEmpty (dialogIcon) ?
                FormHelper.LoadImage ("Icons.AppMain16.png").Pixbuf :
                FormHelper.LoadImage (dialogIcon).Pixbuf;

            dlgMessage.Title = title;

            if (!string.IsNullOrEmpty (messageIcon)) {
                Image image = FormHelper.LoadImage (messageIcon);
                algDialogIcon.Add (image);
                image.Show ();
            }
            lblDialogMessage = new WrapLabel ();
            lblDialogMessage.Text = message;
            lblDialogMessage.Show ();
            algDialogMessage.Add (lblDialogMessage);

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnRetry.SetChildImage (FormHelper.LoadImage ("Icons.Retry24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            btnYes.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnNo.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            btnRestart.SetChildImage (FormHelper.LoadImage ("Icons.Retry24.png"));

            Buttons = MessageButtons.OK;

            base.InitializeForm ();

            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnRetry.SetChildLabelText (Translator.GetString ("Retry"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
            btnYes.SetChildLabelText (Translator.GetString ("Yes"));
            btnNo.SetChildLabelText (Translator.GetString ("No"));
            btnRestart.SetChildLabelText (Translator.GetString ("Restart"));
        }

        protected override void OnShown (object sender, EventArgs e)
        {
            base.OnShown (sender, e);

            if (!chkRememberChoice.Visible)
                return;

            if (((buttons & MessageButtons.OKCancel) == MessageButtons.OKCancel) ||
                ((buttons & MessageButtons.YesNo) == MessageButtons.YesNo))
                chkRememberChoice.Label = Translator.GetString ("Don\'t ask me again");
            else
                chkRememberChoice.Label = Translator.GetString ("Don\'t show this again");
        }

        public void SetButtonText (MessageButtons button, string text)
        {
            switch (button) {
                case MessageButtons.OK:
                    btnOK.SetChildLabelText (text);
                    break;
                case MessageButtons.Cancel:
                    btnCancel.SetChildLabelText (text);
                    break;
                case MessageButtons.Retry:
                    btnRetry.SetChildLabelText (text);
                    break;
                case MessageButtons.Yes:
                    btnYes.SetChildLabelText (text);
                    break;
                case MessageButtons.No:
                    btnNo.SetChildLabelText (text);
                    break;
                case MessageButtons.Remember:
                    chkRememberChoice.Label = text;
                    break;
                case MessageButtons.Restart:
                    btnRestart.SetChildLabelText (text);
                    break;
                default:
                    throw new ArgumentOutOfRangeException ("button");
            }
        }

        public void SetButtonImage (MessageButtons button, Image image)
        {
            switch (button) {
                case MessageButtons.OK:
                    btnOK.SetChildImage (image);
                    break;
                case MessageButtons.Cancel:
                    btnCancel.SetChildImage (image);
                    break;
                case MessageButtons.Retry:
                    btnRetry.SetChildImage (image);
                    break;
                case MessageButtons.Yes:
                    btnYes.SetChildImage (image);
                    break;
                case MessageButtons.No:
                    btnNo.SetChildImage (image);
                    break;
                case MessageButtons.Restart:
                    btnRestart.SetChildImage (image);
                    break;
                default:
                    throw new ArgumentOutOfRangeException ("button");
            }
        }

        [UsedImplicitly]
        private void btnOK_Clicked (object o, EventArgs args)
        {
            dlgMessage.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        private void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgMessage.Respond (ResponseType.Cancel);
        }

        [UsedImplicitly]
        private void btnNo_Clicked (object o, EventArgs args)
        {
            dlgMessage.Respond (ResponseType.No);
        }

        [UsedImplicitly]
        private void btnYes_Clicked (object o, EventArgs args)
        {
            dlgMessage.Respond (ResponseType.Yes);
        }

        [UsedImplicitly]
        private void btnRetry_Clicked (object o, EventArgs args)
        {
            dlgMessage.Respond (ResponseType.Reject);
        }

        [UsedImplicitly]
        private void btnRestart_Clicked (object o, EventArgs args)
        {
            dlgMessage.Respond (ResponseType.Apply);
        }

        public static ResponseType ShowDialog (string title, string dialogIcon, string message, string messageIcon, MessageButtons buttons = MessageButtons.OK)
        {
            using (Message msg = new Message (title, dialogIcon, message, messageIcon)) {
                msg.Buttons = buttons;
                return msg.Run ();
            }
        }
    }
}
