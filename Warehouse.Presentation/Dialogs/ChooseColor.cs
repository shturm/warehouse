//
// ChooseColor.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/16/2006
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
using Gdk;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Component;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseColor : DialogBase
    {
        #region Glade Widgets

        [Widget]
        protected Dialog dlgChooseColor;
        [Widget]
        protected Button btnDefault;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;

        [Widget]
        protected ColorSelection clsSelection;

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgChooseColor; }
        }


        public ColorSelection Selection
        {
            get { return clsSelection; }
        }

        public Color Color
        {
            get { return clsSelection.CurrentColor; }
        }

        public Cairo.Color CairoColor
        {
            get
            {
                Cairo.Color foreground = Selection.CurrentColor.ToCairoColor ();
                foreground.A = (Selection.CurrentAlpha >> 8) / 255d;

                return foreground;
            }
        }

        public bool DefaultButtonVisible
        {
            get { return btnDefault.Visible; }
            set { btnDefault.Visible = value; }
        }

        public ChooseColor ()
        {
            Initialize ();
        }

        public ChooseColor (string title, Cairo.Color color)
            : this ()
        {
            DialogControl.Title = title;
            Selection.CurrentColor = color.ToGDKColor ();
            Selection.CurrentAlpha = (ushort) (color.A * 255);
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChooseColor.glade", "dlgChooseColor");
            form.Autoconnect (this);

            dlgChooseColor.Icon = FormHelper.LoadImage ("Icons.Color16.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();

            InitializeFormStrings ();

            clsSelection.PreviousAlpha = ushort.MaxValue;
            clsSelection.CurrentAlpha = ushort.MaxValue;

            dlgChooseColor.Shown += dlgEditNewPartner_Shown;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseColor.Title = Translator.GetString ("Choose Color");

            btnDefault.SetChildLabelText (Translator.GetString ("Default"));
            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        private void dlgEditNewPartner_Shown (object sender, EventArgs e)
        {
            clsSelection.GrabFocus ();
        }

        #region Event handling

        protected void btnDefault_Clicked (object o, EventArgs args)
        {
            dlgChooseColor.Respond (ResponseType.Reject);
        }

        protected void btnOK_Clicked (object o, EventArgs args)
        {
            dlgChooseColor.Respond (ResponseType.Ok);
        }

        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgChooseColor.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
