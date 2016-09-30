//
// EditNew.cs
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

using Glade;
using Gtk;

using Warehouse.Business;
using Warehouse.Component;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNew : DialogBase
    {
        #region Glade Widgets

        [Widget]
        protected Dialog dlgEditNew;
        [Widget]
        protected Alignment algContents;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgEditNew; }
        }

        public EditNew ()
        {
            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditNew.glade", "dlgEditNew");
            form.Autoconnect (this);

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();

            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            btnOK.SetChildLabelText (Translator.GetString ("Save"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        #region Event handling

        protected virtual void btnOK_Clicked (object o, EventArgs args)
        {
            dlgEditNew.Respond (ResponseType.Ok);
        }

        protected virtual void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditNew.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
