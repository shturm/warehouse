// 
// ChooseBankNotesAndCoins.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
// 
// Created:
//    26.04.2012
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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Data.Calculator;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseBankNotesAndCoins : DialogBase
    {
        private string bankNotesAndCoins;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgChooseBankNotesAndCoins;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

        [Widget]
        private ToggleButton tbnChoose001;
        [Widget]
        private ToggleButton tbnChoose002;
        [Widget]
        private ToggleButton tbnChoose005;
        [Widget]
        private ToggleButton tbnChoose01;
        [Widget]
        private ToggleButton tbnChoose02;
        [Widget]
        private ToggleButton tbnChoose05;
        
        [Widget]
        private ToggleButton tbnChoose1;
        [Widget]
        private ToggleButton tbnChoose2;
        [Widget]
        private ToggleButton tbnChoose5;
        [Widget]
        private ToggleButton tbnChoose10;
        [Widget]
        private ToggleButton tbnChoose20;
        [Widget]
        private ToggleButton tbnChoose50;
        
        [Widget]
        private ToggleButton tbnChoose100;
        [Widget]
        private ToggleButton tbnChoose200;
        [Widget]
        private ToggleButton tbnChoose500;
        [Widget]
        private ToggleButton tbnChoose1000;
        [Widget]
        private ToggleButton tbnChoose2000;
        [Widget]
        private ToggleButton tbnChoose5000;

        [Widget]
        private ToggleButton tbnChoose10000;
        [Widget]
        private ToggleButton tbnChoose20000;
        [Widget]
        private ToggleButton tbnChoose50000;
        [Widget]
        private ToggleButton tbnChoose100000;
        [Widget]
        private ToggleButton tbnChoose200000;
        [Widget]
        private ToggleButton tbnChoose500000;

#pragma warning restore 649

        #endregion

        private List<ToggleButton> allButtons;

        public override Dialog DialogControl
        {
            get { return dlgChooseBankNotesAndCoins; }
        }

        public string BankNotesAndCoins
        {
            get { return bankNotesAndCoins; }
        }

        public ChooseBankNotesAndCoins (string bankNotesAndCoins)
        {
            this.bankNotesAndCoins = bankNotesAndCoins;

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChooseBankNotesAndCoins.glade", "dlgChooseBankNotesAndCoins");
            form.Autoconnect (this);

            dlgChooseBankNotesAndCoins.Icon = FormHelper.LoadImage ("Icons.Banknote24.png").Pixbuf;

            allButtons = new List<ToggleButton>
                {
                    tbnChoose001, tbnChoose002, tbnChoose005, tbnChoose01, tbnChoose02, tbnChoose05, 
                    tbnChoose1, tbnChoose2, tbnChoose5, tbnChoose10, tbnChoose20, tbnChoose50, 
                    tbnChoose100, tbnChoose200, tbnChoose500, tbnChoose1000, tbnChoose2000, tbnChoose5000, 
                    tbnChoose10000, tbnChoose20000, tbnChoose50000, tbnChoose100000, tbnChoose200000, tbnChoose500000
                };
            List<ToggleButton> allCoinNumbers = new List<ToggleButton>
                {
                    tbnChoose001, tbnChoose002, tbnChoose005, tbnChoose01, tbnChoose02, tbnChoose05
                };

            foreach (ToggleButton button in allButtons) {
                button.SetChildImage (allCoinNumbers.Contains (button) ?
                    FormHelper.LoadImage ("Icons.Coin24.png") :
                    FormHelper.LoadImage ("Icons.Banknote24.png"));
            }

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            string [] bankNotes = bankNotesAndCoins.Split (';');
            IList<double> allowedValues = ConfigurationHolderBase.AllowedBankNotesAndCoins;
            for (int i = 0; i < allowedValues.Count; i++) {
                ToggleButton toggleButton = allButtons [i];
                toggleButton.Data.Add ("Value", allowedValues [i]);
                foreach (string bankNote in bankNotes) {
                    double value;
                    if (Business.Entities.Currency.TryParseExpression (bankNote, out value) &&
                        allowedValues [i].IsEqualTo (value))
                        toggleButton.Active = true;
                }
            }

            base.InitializeForm ();
            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            dlgChooseBankNotesAndCoins.Title = Translator.GetString ("Choose Bank Notes and Coins");

            foreach (ToggleButton button in allButtons)
                button.SetChildLabelText (button.Data ["Value"].ToString ());

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            base.InitializeFormStrings ();
        }

        [UsedImplicitly]
        private void btnOK_Clicked (object o, EventArgs args)
        {
            StringBuilder bankNotesAndMoneyBuilder = new StringBuilder ();
            foreach (ToggleButton toggleButton in allButtons)
                if (toggleButton.Active) {
                    double value = (double) toggleButton.Data ["Value"];
                    bankNotesAndMoneyBuilder.Append (value.ToString (CultureInfo.InvariantCulture.NumberFormat) + ';');
                }

            if (string.IsNullOrEmpty (bankNotesAndMoneyBuilder.ToString ())) {
                MessageError.ShowDialog (Translator.GetString ("You must have at least one bank note or coin type selected."),
                    ErrorSeverity.Error);
                return;
            }

            bankNotesAndMoneyBuilder.Remove (bankNotesAndMoneyBuilder.Length - 1, 1);
            bankNotesAndCoins = bankNotesAndMoneyBuilder.ToString ();
            BusinessDomain.AppConfiguration.BankNotesAndCoins = bankNotesAndCoins;

            dlgChooseBankNotesAndCoins.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        private void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgChooseBankNotesAndCoins.Respond (ResponseType.Cancel);
        }
    }
}
