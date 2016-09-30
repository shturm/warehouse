//
// ChooseTimeInterval.cs
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
    public class ChooseTimeInterval : DialogBase
    {
        #region Glade Widgets

        [Widget]
        protected Dialog dlgChooseTimeInterval;
        [Widget]
        protected CheckButton chkTimeFrom;
        [Widget]
        protected SpinButton spbHoursFrom;
        [Widget]
        protected SpinButton spbMinutesFrom;
        [Widget]
        protected CheckButton chkTimeTo;
        [Widget]
        protected SpinButton spbHoursTo;
        [Widget]
        protected SpinButton spbMinutesTo;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;

        #endregion

        private DateTime? timeFrom;
        private DateTime? timeTo;

        public override Dialog DialogControl
        {
            get { return dlgChooseTimeInterval; }
        }

        public DateTime? TimeFrom
        {
            get
            {
                return timeFrom;
            }
        }

        public DateTime? TimeTo
        {
            get
            {
                return timeTo;
            }
        }

        public ChooseTimeInterval ()
        {
            Initialize ();
        }

        public ChooseTimeInterval (DateTime? timeFrom, DateTime? timeTo)
            : this ()
        {
            if (timeFrom == null)
                chkTimeFrom.Active = false;
            else {
                spbHoursFrom.Value = timeFrom.Value.Hour;
                spbMinutesFrom.Value = timeFrom.Value.Minute;
            }

            if (timeTo == null)
                chkTimeTo.Active = false;
            else {
                spbHoursTo.Value = timeTo.Value.Hour;
                spbMinutesTo.Value = timeTo.Value.Minute;
            }
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChooseTimeInterval.glade", "dlgChooseTimeInterval");
            form.Autoconnect (this);

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            chkTimeFrom.Label = Translator.GetString ("From:");
            chkTimeTo.Label = Translator.GetString ("To:");

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            base.InitializeForm ();
            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseTimeInterval.Title = Translator.GetString ("Time Interval");
        }

        private bool Validate ()
        {
            if (chkTimeFrom.Active)
                timeFrom = new DateTime (DateTime.MinValue.Year, DateTime.MinValue.Month, DateTime.MinValue.Day,
                    spbHoursFrom.ValueAsInt, spbMinutesFrom.ValueAsInt, 0);
            
            if (chkTimeTo.Active)
                timeTo = new DateTime (DateTime.MinValue.Year, DateTime.MinValue.Month, DateTime.MinValue.Day,
                    spbHoursTo.ValueAsInt, spbMinutesTo.ValueAsInt, 0);

            if (timeFrom == null || timeTo == null || timeFrom <= timeTo)
                return true;
            
            MessageError.ShowDialog (Translator.GetString ("The minimum time cannot be greater than the maximum time."), ErrorSeverity.Error);
            spbHoursFrom.GrabFocus ();
            return false;
        }

        #region Event handling

        protected void OnChkFromToggled (object o, EventArgs args)
        {
            if (!chkTimeFrom.Active)
                chkTimeTo.Active = true;
            spbHoursFrom.Sensitive = chkTimeFrom.Active;
            spbMinutesFrom.Sensitive = chkTimeFrom.Active;
        }

        protected void OnChkToToggled (object o, EventArgs args)
        {
            if (!chkTimeTo.Active)
                chkTimeFrom.Active = true;
            spbHoursTo.Sensitive = chkTimeTo.Active;
            spbMinutesTo.Sensitive = chkTimeTo.Active;
        }

        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (!Validate ())
                return;
            
            dlgChooseTimeInterval.Respond (ResponseType.Ok);
        }

        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgChooseTimeInterval.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
