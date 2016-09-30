//
// ChooseNumericInterval.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   11.04.2009
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
    public class ChooseNumericInterval : DialogBase
    {
        #region Glade Widgets

        [Widget]
        protected Dialog dlgChooseNumericInterval;
        [Widget]
        protected CheckButton chkValueFrom;
        [Widget]
        protected SpinButton spbValueFrom;
        [Widget]
        protected CheckButton chkValueTo;
        [Widget]
        protected SpinButton spbValueTo;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;

        #endregion

        private double? valueFrom;
        private double? valueTo;

        public override Dialog DialogControl
        {
            get { return dlgChooseNumericInterval; }
        }

        public double? ValueFrom
        {
            get { return valueFrom; }
        }

        public double? ValueTo
        {
            get { return valueTo; }
        }

        public ChooseNumericInterval ()
        {
            Initialize ();
        }

        public ChooseNumericInterval (double? valueFrom, double? valueTo)
        {
            Initialize ();

            if (valueFrom == null)
                chkValueFrom.Active = false;
            else
                spbValueFrom.Value = valueFrom.Value;

            if (valueTo == null)
                chkValueTo.Active = false;
            else
                spbValueTo.Value = valueTo.Value;
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChooseNumericInterval.glade", "dlgChooseNumericInterval");
            form.Autoconnect (this);

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            chkValueFrom.Label = Translator.GetString ("From:");
            chkValueTo.Label = Translator.GetString ("To:");

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            base.InitializeForm ();
            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseNumericInterval.Title = Translator.GetString ("Numeric Interval");
        }

        private bool Validate ()
        {
            if (chkValueFrom.Active)
                valueFrom = spbValueFrom.Value;
            
            if (chkValueTo.Active)
                valueTo = spbValueTo.Value;

            if (!valueFrom.HasValue || !valueTo.HasValue || !(valueFrom.Value > valueTo.Value))
                return true;
            
            MessageError.ShowDialog (Translator.GetString ("The minimum number cannot be greater than the maximum number."), ErrorSeverity.Error);
            spbValueFrom.GrabFocus ();
            return false;
        }

        #region Event handling

        protected void OnChkFromToggled (object o, EventArgs args)
        {
            if (!chkValueFrom.Active)
                chkValueTo.Active = true;
            spbValueFrom.Sensitive = chkValueFrom.Active;
        }

        protected void OnChkToToggled (object o, EventArgs args)
        {
            if (!chkValueTo.Active)
                chkValueFrom.Active = true;
            spbValueTo.Sensitive = chkValueTo.Active;
        }

        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (!Validate ())
                return;
            
            dlgChooseNumericInterval.Respond (ResponseType.Ok);
        }

        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgChooseNumericInterval.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
