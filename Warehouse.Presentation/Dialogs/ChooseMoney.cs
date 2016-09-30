//
// ChooseMoney.cs
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
using System.Collections.Generic;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Presentation.Widgets;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseMoney : DialogBase
    {
        private readonly PriceType priceType;
        private readonly ChooseMoneyPanel panel;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgChooseMoney;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;
        [Widget]
        private Label lblTotal;
        [Widget]
        private Label lblTotalValue;
        [Widget]
        private Alignment algGrid;

#pragma warning restore 649

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgChooseMoney; }
        }

        public ChooseMoney (PriceType priceType)
        {
            this.priceType = priceType;
            panel = new ChooseMoneyPanel (priceType);
            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChooseMoney.glade", "dlgChooseMoney");
            form.Autoconnect (this);

            dlgChooseMoney.Icon = FormHelper.LoadImage ("Icons.Banknote24.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();
            InitializeFormStrings ();

            InitializeGrid ();
            RefreshTotal ();
        }

        protected override void InitializeFormStrings ()
        {
            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            lblTotal.SetText (Translator.GetString ("Total"));

            dlgChooseMoney.Title = Translator.GetString ("Choose money");

            base.InitializeFormStrings ();
        }

        #region Grid handling

        private void InitializeGrid ()
        {
            panel.Show ();
            panel.TotalChanged += panel_TotalChanged;
            algGrid.Add (panel);
        }

        #endregion

        public double GetTotal ()
        {
            return panel.GetTotal ();
        }

        public Dictionary<double, double> GetAmounts ()
        {
            return panel.GetAmounts ();
        }

        private void panel_TotalChanged (object sender, EventArgs e)
        {
            RefreshTotal ();
        }

        private void RefreshTotal ()
        {
            lblTotalValue.SetText (Currency.ToString (GetTotal (), priceType));
        }
    }
}
