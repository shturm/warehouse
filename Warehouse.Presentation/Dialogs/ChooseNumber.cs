//
// ChooseNumber.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/24/2006
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

using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Component;
using Warehouse.Presentation.Widgets;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseNumber : DialogBase
    {
        private readonly ChooseNumberPanel pnlChooseNumber;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgChooseNumber;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

        [Widget]
        private Alignment algPanel;

        [Widget]
        private Label lblValue;

#pragma warning restore 649

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgChooseNumber; }
        }

        public ChooseNumber (double? value, bool allowEmpty = false, bool textVisible = true)
        {
            Initialize ();

            pnlChooseNumber = new ChooseNumberPanel (value, lblValue, allowEmpty, textVisible);
            pnlChooseNumber.Show ();
            algPanel.Add (pnlChooseNumber);
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChooseNumber.glade", "dlgChooseNumber");
            form.Autoconnect (this);

            //dlgChooseNumber.Icon = FormHelper.LoadImage ("Icons.Banknote24.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();
            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            dlgChooseNumber.Title = Translator.GetString ("Choose number");

            base.InitializeFormStrings ();
        }

        public string GetString ()
        {
            return pnlChooseNumber.GetString ();
        }

        public double GetNumber ()
        {
            return pnlChooseNumber.GetNumber ();
        }

        public int GetInteger ()
        {
            return pnlChooseNumber.GetInteger ();
        }
    }
}
