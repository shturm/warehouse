//
// ChooseDateInterval.cs
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
    public class ChooseDateInterval : DialogBase
    {
        #region Glade Widgets

        [Widget]
        protected Dialog dlgChooseDateInterval;
        [Widget]
        protected CheckButton chkDateFrom;
        [Widget]
        protected Entry txtDateFrom;
        [Widget]
        protected Button btnChooseDateFrom;
        [Widget]
        protected CheckButton chkDateTo;
        [Widget]
        protected Entry txtDateTo;
        [Widget]
        protected Button btnChooseDateTo;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;

        #endregion

        private DateTime? dateFrom;
        private DateTime? dateTo;

        public override Dialog DialogControl
        {
            get { return dlgChooseDateInterval; }
        }

        public DateTime? DateFrom
        {
            get { return dateFrom; }
        }

        public DateTime? DateTo
        {
            get { return dateTo; }
        }

        public ChooseDateInterval ()
        {
            Initialize ();
        }

        public ChooseDateInterval (DateTime? dateFrom, DateTime? dateTo)
            : this ()
        {
            if (dateFrom == null)
                chkDateFrom.Active = false;
            else
                txtDateFrom.Text = BusinessDomain.GetFormattedDate (dateFrom.Value);

            if (dateTo == null)
                chkDateTo.Active = false;
            else
                txtDateTo.Text = BusinessDomain.GetFormattedDate (dateTo.Value);
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChooseDateInterval.glade", "dlgChooseDateInterval");
            form.Autoconnect (this);

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            chkDateFrom.Label = Translator.GetString ("From:");
            chkDateTo.Label = Translator.GetString ("To:");

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            base.InitializeForm ();
            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseDateInterval.Title = Translator.GetString ("Date Interval");
        }

        private bool Validate ()
        {
            if (chkDateFrom.Active && !ValidateDate (ref dateFrom, txtDateFrom))
                return false;

            if (chkDateTo.Active && !ValidateDate (ref dateTo, txtDateTo))
                return false;

            if (dateFrom == null || dateTo == null || dateFrom <= dateTo)
                return true;
            
            MessageError.ShowDialog (Translator.GetString ("The minimum date cannot be greater than the maximum date."), ErrorSeverity.Error);
            txtDateFrom.GrabFocus ();
            return false;
        }

        private static bool ValidateDate (ref DateTime? date, Entry txtDate)
        {
            DateTime result = BusinessDomain.GetDateValue (txtDate.Text.Trim ());
            if (result == DateTime.MinValue) {
                MessageError.ShowDialog (Translator.GetString ("The entered date is invalid."), ErrorSeverity.Error);

                txtDate.GrabFocus ();
                txtDate.SelectRegion (0, txtDate.Text.Length);
                return false;
            }

            date = result;
            return true;
        }

        #region Event handling

        protected void OnChkFromToggled (object o, EventArgs args)
        {
            if (!chkDateFrom.Active)
                chkDateTo.Active = true;

            txtDateFrom.Sensitive = chkDateFrom.Active;
            btnChooseDateFrom.Sensitive = chkDateFrom.Active;
        }

        protected void OnChkToToggled (object o, EventArgs args)
        {
            if (!chkDateTo.Active)
                chkDateFrom.Active = true;

            txtDateTo.Sensitive = chkDateTo.Active;
            btnChooseDateTo.Sensitive = chkDateTo.Active;
        }

        protected void OnBtnChooseDateFromClicked (object o, EventArgs args)
        {
            ChooseDate (txtDateFrom);
        }

        protected void OnBtnChooseDateToClicked (object o, EventArgs args)
        {
            ChooseDate (txtDateTo);
        }

        private static void ChooseDate (Entry txtDate)
        {
            using (ChooseDate chooseDate = new ChooseDate ()) {
                if (chooseDate.Run () != ResponseType.Ok) {
                    txtDate.GrabFocus ();
                    return;
                }
                txtDate.Text = BusinessDomain.GetFormattedDate (chooseDate.Selection);
            }
        }

        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (!Validate ())
                return;

            dlgChooseDateInterval.Respond (ResponseType.Ok);
        }

        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgChooseDateInterval.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
