//
// ChooseNumberPanel.cs
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

using System;
using System.Globalization;
using Glade;
using Gtk;
using Warehouse.Component;

namespace Warehouse.Presentation.Widgets
{
    public class ChooseNumberPanel : Alignment
    {
        private readonly NumberFormatInfo numberFormat;
        private readonly bool allowStrings;
        private readonly bool textVisible;
        private readonly Widget valueWidget;
        private string entryValue;
        private bool isNumber = true;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Table tblRoot;
        [Widget]
        private Button btnBackspace;
        [Widget]
        private Button btn0;
        [Widget]
        private Button btn1;
        [Widget]
        private Button btn2;
        [Widget]
        private Button btn3;
        [Widget]
        private Button btn4;
        [Widget]
        private Button btn5;
        [Widget]
        private Button btn6;
        [Widget]
        private Button btn7;
        [Widget]
        private Button btn8;
        [Widget]
        private Button btn9;
        [Widget]
        private Button btnDot;

#pragma warning restore 649

        #endregion

        public bool IsNumber
        {
            get { return isNumber; }
            set { isNumber = value; }
        }

        public ChooseNumberPanel (double? value, Widget valueWidget, bool allowStrings = false, bool textVisible = true)
            : base (0.5f, 0.5f, 1, 1)
        {
            if (valueWidget == null)
                throw new ArgumentNullException ("valueWidget");

            this.allowStrings = allowStrings;
            this.valueWidget = valueWidget;
            this.textVisible = textVisible;

            InitializeForm ();

            numberFormat = new NumberFormatInfo { NumberDecimalSeparator = ".", NumberGroupSeparator = string.Empty };
            if (value.HasValue)
                entryValue = value.Value.ToString (numberFormat);
            else if (allowStrings)
                entryValue = string.Empty;
            else
                entryValue = "0";

            SetRefreshEntryValue ();
        }

        private void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Widgets.ChooseNumberPanel.glade", "tblRoot");
            form.Autoconnect (this);

            Add (tblRoot);

            btnBackspace.SetChildImage (FormHelper.LoadImage ("Icons.Left24.png"));

            btnBackspace.Clicked += btnBackspace_Clicked;
            btn0.Clicked += btn0_Clicked;
            btn1.Clicked += btn1_Clicked;
            btn2.Clicked += btn2_Clicked;
            btn3.Clicked += btn3_Clicked;
            btn4.Clicked += btn4_Clicked;
            btn5.Clicked += btn5_Clicked;
            btn6.Clicked += btn6_Clicked;
            btn7.Clicked += btn7_Clicked;
            btn8.Clicked += btn8_Clicked;
            btn9.Clicked += btn9_Clicked;
            btnDot.Clicked += btnDot_Clicked;
        }

        public string GetString ()
        {
            return entryValue;
        }

        public double GetNumber ()
        {
            double ret;
            return double.TryParse (entryValue, NumberStyles.Any, numberFormat, out ret) ? ret : 0;
        }

        public int GetInteger ()
        {
            return (int) Math.Round (GetNumber (), MidpointRounding.AwayFromZero);
        }

        #region Money buttons handling

        private void btn0_Clicked (object sender, EventArgs e)
        {
            AddNumber ("0");
        }

        private void btn1_Clicked (object sender, EventArgs e)
        {
            AddNumber ("1");
        }

        private void btn2_Clicked (object sender, EventArgs e)
        {
            AddNumber ("2");
        }

        private void btn3_Clicked (object sender, EventArgs e)
        {
            AddNumber ("3");
        }

        private void btn4_Clicked (object sender, EventArgs e)
        {
            AddNumber ("4");
        }

        private void btn5_Clicked (object sender, EventArgs e)
        {
            AddNumber ("5");
        }

        private void btn6_Clicked (object sender, EventArgs e)
        {
            AddNumber ("6");
        }

        private void btn7_Clicked (object sender, EventArgs e)
        {
            AddNumber ("7");
        }

        private void btn8_Clicked (object sender, EventArgs e)
        {
            AddNumber ("8");
        }

        private void btn9_Clicked (object sender, EventArgs e)
        {
            AddNumber ("9");
        }

        private void btnDot_Clicked (object sender, EventArgs e)
        {
            if (entryValue.Contains ("."))
                return;

            entryValue += ".";
            SetRefreshEntryValue ();
        }

        private void btnBackspace_Clicked (object sender, EventArgs e)
        {
            if (!allowStrings && entryValue == "0")
                return;

            if ((!allowStrings && entryValue.Length > 1) || (allowStrings && entryValue.Length > 0))
                entryValue = entryValue.Substring (0, entryValue.Length - 1);
            else if (allowStrings)
                entryValue = string.Empty;
            else
                entryValue = "0";

            SetRefreshEntryValue ();
        }

        private void AddNumber (string value)
        {
            if (!allowStrings && !entryValue.Contains ("."))
                entryValue = entryValue.TrimStart ('0');

            entryValue += value;

            SetRefreshEntryValue ();
        }

        private void SetRefreshEntryValue ()
        {
            Label lblValue = valueWidget as Label;
            if (lblValue != null) {
                lblValue.SetText (textVisible ?
                    entryValue :
                    string.Empty.PadRight (entryValue.Length, '*'));
                return;
            }

            Entry txtValue = valueWidget as Entry;
            if (txtValue == null)
                return;

            txtValue.Text = entryValue;
            txtValue.GrabFocus ();
            txtValue.SelectRegion (entryValue.Length, entryValue.Length);
        }

        #endregion
    }
}
