//
// ChooseDate.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
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

using System;
using Glade;
using Gtk;

using Warehouse.Component;
using Warehouse.Business;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseDate : DialogBase
    {
        private enum DateField
        {
            Day,
            Month,
            Year
        }

        private DateField editedField = DateField.Day;
        private int editedCharacter;
        private int editCharactersLeft = 2;
        private bool nowRefreshing;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgChooseDate;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;
        [Widget]
        private Button btnToday;
        [Widget]
        private Calendar calMain;

        [Widget]
        private Label lblDay;
        [Widget]
        private Label lblMonth;
        [Widget]
        private Label lblYear;

        [Widget]
        private SpinButton spbDay;
        [Widget]
        private SpinButton spbMonth;
        [Widget]
        private SpinButton spbYear;

#pragma warning restore 649

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgChooseDate; }
        }

        public DateTime Selection
        {
            get { return calMain.Date; }
            set
            {
                calMain.Date = value;
                RefreshTextFields ();
            }
        }

        public ChooseDate ()
            : this (BusinessDomain.Today)
        {
        }

        public ChooseDate (DateTime dateToSelect)
        {
            Initialize ();

            calMain.Date = dateToSelect;
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChooseDate.glade", "dlgChooseDate");
            form.Autoconnect (this);

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));
            btnToday.SetChildImage (FormHelper.LoadImage ("Icons.Today24.png"));

            calMain.KeyPressEvent += calMain_KeyPressEvent;
            calMain.DaySelected += calMain_DaySelected;
            calMain.DaySelectedDoubleClick += calMain_DaySelectedDoubleClick;
            spbDay.ValueChanged += SpinButton_Changed;
            spbMonth.ValueChanged += SpinButton_Changed;
            spbYear.ValueChanged += SpinButton_Changed;
            btnToday.Clicked += btnToday_Clicked;

            base.InitializeForm ();

            InitializeFormStrings ();
            RefreshTextFields ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseDate.Title = Translator.GetString ("Select date");

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
            btnToday.SetChildLabelText (Translator.GetString ("Today"));
            lblDay.SetText (Translator.GetString ("Day:"));
            lblMonth.SetText (Translator.GetString ("Month:"));
            lblYear.SetText (Translator.GetString ("Year:"));
        }

        [GLib.ConnectBefore]
        private void calMain_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            int digit = -1;

            switch (args.Event.Key) {
                case Gdk.Key.Up:
                case Gdk.Key.KP_Up:
                    calMain.Date = calMain.Date.AddDays (-7.0d);
                    break;
                case Gdk.Key.Down:
                case Gdk.Key.KP_Down:
                    calMain.Date = calMain.Date.AddDays (7.0d);
                    break;
                case Gdk.Key.Left:
                    calMain.Date = calMain.Date.AddDays (-1.0d);
                    break;
                case Gdk.Key.Right:
                    calMain.Date = calMain.Date.AddDays (1.0d);
                    break;
                case Gdk.Key.Key_0:
                    digit = 0;
                    break;
                case Gdk.Key.Key_1:
                    digit = 1;
                    break;
                case Gdk.Key.Key_2:
                    digit = 2;
                    break;
                case Gdk.Key.Key_3:
                    digit = 3;
                    break;
                case Gdk.Key.Key_4:
                    digit = 4;
                    break;
                case Gdk.Key.Key_5:
                    digit = 5;
                    break;
                case Gdk.Key.Key_6:
                    digit = 6;
                    break;
                case Gdk.Key.Key_7:
                    digit = 7;
                    break;
                case Gdk.Key.Key_8:
                    digit = 8;
                    break;
                case Gdk.Key.Key_9:
                    digit = 9;
                    break;
            }

            if (digit >= 0) {
                int newValue;
                switch (editedField) {
                    case DateField.Day:
                        if (editedCharacter == 0) {
                            try {
                                calMain.Date = new DateTime (calMain.Date.Year, calMain.Date.Month, digit);
                                editedCharacter = 1;
                            } catch { }

                            if (--editCharactersLeft == 0) {
                                editCharactersLeft = 2;
                                editedField = DateField.Month;
                                editedCharacter = 0;
                            }
                        } else {
                            editCharactersLeft = 2;
                            editedField = DateField.Month;
                            editedCharacter = 0;
                            newValue = (calMain.Date.Day * 10) + digit;

                            try {
                                calMain.Date = new DateTime (calMain.Date.Year, calMain.Date.Month, newValue);
                            } catch {
                                calMain_KeyPressEvent (o, args);
                            }
                        }
                        break;

                    case DateField.Month:
                        if (editedCharacter == 0) {
                            try {
                                calMain.Date = new DateTime (calMain.Date.Year, digit, calMain.Date.Day);
                                spbDay.Adjustment.Upper = DateTime.DaysInMonth (calMain.Date.Year, calMain.Date.Month);
                                editedCharacter = 1;
                            } catch { }

                            if (--editCharactersLeft == 0) {
                                editCharactersLeft = 4;
                                editedField = DateField.Year;
                                editedCharacter = 0;
                            }
                        } else {
                            editCharactersLeft = 4;
                            editedField = DateField.Year;
                            editedCharacter = 0;
                            newValue = (calMain.Date.Month * 10) + digit;

                            try {
                                calMain.Date = new DateTime (calMain.Date.Year, newValue, calMain.Date.Day);
                            } catch {
                                calMain_KeyPressEvent (o, args);
                            }
                        }
                        break;

                    case DateField.Year:
                        newValue = (calMain.Date.Year / (int) Math.Pow (10d, 3 - (double) editedCharacter));
                        newValue = newValue % 10;
                        newValue = (calMain.Date.Year - (int) Math.Pow (10d, 3 - (double) editedCharacter) * newValue);
                        newValue += (int) Math.Pow (10d, 3 - (double) editedCharacter) * digit;

                        try {
                            calMain.Date = new DateTime (newValue, calMain.Date.Month, calMain.Date.Day);
                        } catch { }

                        editedCharacter++;
                        if (--editCharactersLeft == 0) {
                            editCharactersLeft = 2;
                            editedField = DateField.Day;
                            editedCharacter = 0;
                        }

                        break;
                }
            }

            RefreshTextFields ();
        }

        private void calMain_DaySelected (object sender, EventArgs e)
        {
            RefreshTextFields ();
        }

        private void SpinButton_Changed (object sender, EventArgs e)
        {
            RefreshCallendar ();
        }

        private void RefreshTextFields ()
        {
            if (nowRefreshing)
                return;

            nowRefreshing = true;

            double days = DateTime.DaysInMonth (calMain.Date.Day, calMain.Date.Month);

            spbDay.Value = Math.Min (days, calMain.Date.Day);
            spbDay.Adjustment.Upper = days;
            spbMonth.Value = calMain.Date.Month;
            spbYear.Value = calMain.Date.Year;

            nowRefreshing = false;
        }

        private void RefreshCallendar ()
        {
            if (nowRefreshing)
                return;

            nowRefreshing = true;

            double days = DateTime.DaysInMonth ((int) spbYear.Value, (int) spbMonth.Value);

            spbDay.Value = Math.Min (days, spbDay.Value);
            spbDay.Adjustment.Upper = days;
            calMain.Date = new DateTime ((int) spbYear.Value, (int) spbMonth.Value, (int) spbDay.Value);

            nowRefreshing = false;
        }

        #region Event handling

        private void btnOK_Clicked (object o, EventArgs args)
        {
            dlgChooseDate.Respond (ResponseType.Ok);
        }

        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgChooseDate.Respond (ResponseType.Cancel);
        }

        private void btnToday_Clicked (object sender, EventArgs e)
        {
            Selection = BusinessDomain.Today;
            btnOK_Clicked (this, EventArgs.Empty);
        }

        private void calMain_DaySelectedDoubleClick (object sender, EventArgs e)
        {
            btnOK_Clicked (this, EventArgs.Empty);
        }

        #endregion
    }
}
