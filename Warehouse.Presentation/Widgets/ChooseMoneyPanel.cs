//
// ChooseMoneyPanel.cs
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
using System.ComponentModel;
using System.Linq;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Data.Model;

namespace Warehouse.Presentation.Widgets
{
    public class ChooseMoneyPanel : Alignment
    {
        private readonly PriceType priceType;
        private readonly BindList<MoneyContainer> list = new BindList<MoneyContainer> ();
        private BindingListModel<MoneyContainer> model;
        private List<Button> allButtons;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Table tblRoot;
        [Widget]
        private Table tblGrid;

        [Widget]
        private Table tableMoney;

        [Widget]
        private Button btnChoose001;
        [Widget]
        private Button btnChoose002;
        [Widget]
        private Button btnChoose005;
        [Widget]
        private Button btnChoose01;
        [Widget]
        private Button btnChoose02;
        [Widget]
        private Button btnChoose05;

        [Widget]
        private Button btnChoose1;
        [Widget]
        private Button btnChoose2;
        [Widget]
        private Button btnChoose5;
        [Widget]
        private Button btnChoose10;
        [Widget]
        private Button btnChoose20;
        [Widget]
        private Button btnChoose50;

        [Widget]
        private Button btnChoose100;
        [Widget]
        private Button btnChoose200;
        [Widget]
        private Button btnChoose500;
        [Widget]
        private Button btnChoose1000;
        [Widget]
        private Button btnChoose2000;
        [Widget]
        private Button btnChoose5000;

        [Widget]
        private Button btnChoose10000;
        [Widget]
        private Button btnChoose20000;
        [Widget]
        private Button btnChoose50000;
        [Widget]
        private Button btnChoose100000;
        [Widget]
        private Button btnChoose200000;
        [Widget]
        private Button btnChoose500000;

#pragma warning restore 649

        #endregion

        public event EventHandler TotalChanged;

        private class MoneyContainer : INotifyPropertyChanged
        {
            private readonly double amount;
            private double quantity;

            public double Amount
            {
                get { return amount; }
            }

            public double Quantity
            {
                get { return quantity; }
                set
                {
                    quantity = value;
                    OnPropertyChanged ("Quantity");
                    OnPropertyChanged ("Total");
                }
            }

            public double Total
            {
                get { return amount * quantity; }
            }

            public MoneyContainer (double amount, double quantity)
            {
                this.amount = amount;
                this.quantity = quantity;
            }

            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged (string property)
            {
                if (PropertyChanged != null)
                    PropertyChanged (this, new PropertyChangedEventArgs (property));
            }

            #endregion
        }

        public ChooseMoneyPanel (PriceType priceType)
            : base (0, 0, 1, 1)
        {
            this.priceType = priceType;
            Initialize ();
        }

        private void Initialize ()
        {
            XML form = FormHelper.LoadGladeXML ("Widgets.ChooseMoneyPanel.glade", "tblRoot");
            form.Autoconnect (this);

            Add (tblRoot);

            foreach (Widget widget in tableMoney.Children)
                widget.SetChildImage (FormHelper.LoadImage ("Icons.Banknote24.png"));

            foreach (Button widget in new [] { btnChoose001, btnChoose002, btnChoose005, btnChoose01, btnChoose02, btnChoose05 })
                widget.SetChildImage (FormHelper.LoadImage ("Icons.Coin24.png"));

            btnChoose001.Data.Add ("Value", 0.01d);
            btnChoose002.Data.Add ("Value", 0.02d);
            btnChoose005.Data.Add ("Value", 0.05d);
            btnChoose01.Data.Add ("Value", 0.1d);
            btnChoose02.Data.Add ("Value", 0.2d);
            btnChoose05.Data.Add ("Value", 0.5d);
            btnChoose1.Data.Add ("Value", 1d);
            btnChoose2.Data.Add ("Value", 2d);
            btnChoose5.Data.Add ("Value", 5d);
            btnChoose10.Data.Add ("Value", 10d);
            btnChoose20.Data.Add ("Value", 20d);
            btnChoose50.Data.Add ("Value", 50d);
            btnChoose100.Data.Add ("Value", 100d);
            btnChoose200.Data.Add ("Value", 200d);
            btnChoose500.Data.Add ("Value", 500d);
            btnChoose1000.Data.Add ("Value", 1000d);
            btnChoose2000.Data.Add ("Value", 2000d);
            btnChoose5000.Data.Add ("Value", 5000d);
            btnChoose10000.Data.Add ("Value", 10000d);
            btnChoose20000.Data.Add ("Value", 20000d);
            btnChoose50000.Data.Add ("Value", 50000d);
            btnChoose100000.Data.Add ("Value", 100000d);
            btnChoose200000.Data.Add ("Value", 200000d);
            btnChoose500000.Data.Add ("Value", 500000d);

            allButtons = new List<Button>
                {
                    btnChoose001, btnChoose002, btnChoose005, btnChoose01, btnChoose02, btnChoose05, 
                    btnChoose1, btnChoose2, btnChoose5, btnChoose10, btnChoose20, btnChoose50, 
                    btnChoose100, btnChoose200, btnChoose500, btnChoose1000, btnChoose2000, btnChoose5000, 
                    btnChoose10000, btnChoose20000, btnChoose50000, btnChoose100000, btnChoose200000, btnChoose500000
                };

            List<double> values = BusinessDomain.AppConfiguration.BankNotesAndCoins.Split (';')
                .Select (value => Currency.ParseExpression (value)).ToList ();

            for (int i = tableMoney.Children.Length - 1; i >= 0; i--) {
                Button widget = (Button) tableMoney.Children [i];
                if (!values.Contains ((double) widget.Data ["Value"]))
                    allButtons.Remove (widget);

                tableMoney.Remove (widget);
            }
            int matrixSide = (int) Math.Ceiling (Math.Sqrt (allButtons.Count));
            tableMoney.NColumns = (uint) matrixSide;
            tableMoney.NRows = (uint) ((allButtons.Count + matrixSide - 1) / matrixSide);
            
            uint column = 0;
            uint row = 0;
            foreach (Button button in allButtons) {
                tableMoney.Attach (button, column, ++column, row, row + 1);
                if (column != tableMoney.NColumns)
                    continue;
                
                ++row;
                column = 0;
            }
            tableMoney.ShowAll ();

            InitializeFormStrings ();
            InitializeGrid ();
        }

        private void InitializeFormStrings ()
        {
            foreach (Button button in allButtons)
                button.SetChildLabelText (button.Data ["Value"].ToString ());
        }

        #region Grid handling

        private void InitializeGrid ()
        {
            model = new BindingListModel<MoneyContainer> (list);
            model.ListChanged += model_ListChanged;
            model.Sort ("Amount", SortDirection.Ascending);
        }

        private void model_ListChanged (object sender, ListChangedEventArgs e)
        {
            EventHandler handler = TotalChanged;
            if (handler != null)
                handler (this, EventArgs.Empty);
        }

        #endregion

        public double GetTotal ()
        {
            return list.Sum (container => container.Total);
        }

        public void SetTotal (double total)
        {
            list.Clear ();
            for (int i = allButtons.Count - 1; i >= 0; i--) {
                double value = (double) allButtons [i].Data ["Value"];
                if (value <= total) {
                    AddMoney (value, 1, false);
                    total = Math.Round (total - value, 2, MidpointRounding.AwayFromZero);
                    i++;
                }

                if (total.IsZero ())
                    break;
            }

            RefreshGrid ();
        }

        public Dictionary<double, double> GetAmounts ()
        {
            return list.ToDictionary (container => container.Amount, container => container.Quantity);
        }

        private void RefreshGrid ()
        {
            for (int i = tblGrid.Children.Length - 1; i >= 0; i--) {
                Widget child = tblGrid.Children [i];
                tblGrid.Remove (child);
                child.Destroy ();
            }

            foreach (MoneyContainer container in model) {
                uint rows = tblGrid.NRows;
                Label lblValue = new Label (Currency.ToString (container.Amount, priceType)) { Xalign = 1f };
                tblGrid.Attach (lblValue, 0, 1, rows, rows + 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 1, 1);

                Label lblX = new Label ("x");
                tblGrid.Attach (lblX, 1, 2, rows, rows + 1, AttachOptions.Expand, AttachOptions.Fill, 4, 1);

                Button btnLess = new Button (FormHelper.LoadImage ("Icons.Down16.png"));
                btnLess.Data.Add ("Container", container);
                btnLess.Clicked += btnLess_Clicked;
                tblGrid.Attach (btnLess, 2, 3, rows, rows + 1, AttachOptions.Fill, AttachOptions.Fill, 1, 1);

                Label lblQtty = new Label (Quantity.ToString (container.Quantity)) { Xalign = 1f };
                Button btnQtty = new Button (lblQtty);
                tblGrid.Attach (btnQtty, 3, 4, rows, rows + 1, AttachOptions.Fill, AttachOptions.Fill, 0, 1);

                Button btnMore = new Button (FormHelper.LoadImage ("Icons.Up16.png"));
                btnMore.Data.Add ("Container", container);
                btnMore.Clicked += btnMore_Clicked;
                tblGrid.Attach (btnMore, 4, 5, rows, rows + 1, AttachOptions.Fill, AttachOptions.Fill, 1, 1);

                Label lblEq = new Label ("=");
                tblGrid.Attach (lblEq, 5, 6, rows, rows + 1, AttachOptions.Expand, AttachOptions.Fill, 4, 1);

                Label lblTotal = new Label (Currency.ToString (container.Total, priceType)) { Xalign = 1f };
                tblGrid.Attach (lblTotal, 6, 7, rows, rows + 1, AttachOptions.Fill, AttachOptions.Fill, 4, 1);

                Button btnDelete = new Button (FormHelper.LoadImage ("Icons.Delete16.png"));
                btnDelete.Data.Add ("Container", container);
                btnDelete.Clicked += btnDelete_Clicked;
                tblGrid.Attach (btnDelete, 7, 8, rows, rows + 1, AttachOptions.Fill, AttachOptions.Fill, 1, 1);
            }

            tblGrid.ShowAll ();
        }

        #region Money buttons handling

        private void btnMore_Clicked (object sender, EventArgs e)
        {
            Button btn = (Button) sender;
            MoneyContainer container = btn.Data ["Container"] as MoneyContainer;
            if (container == null)
                return;

            container.Quantity++;
            RefreshGrid ();
        }

        private void btnLess_Clicked (object sender, EventArgs e)
        {
            Button btn = (Button) sender;
            MoneyContainer container = btn.Data ["Container"] as MoneyContainer;
            if (container == null)
                return;

            container.Quantity--;
            if (container.Quantity <= 0)
                list.Remove (container);
            RefreshGrid ();
        }

        private void btnDelete_Clicked (object sender, EventArgs e)
        {
            Button btn = (Button) sender;
            MoneyContainer container = btn.Data ["Container"] as MoneyContainer;
            if (container == null)
                return;

            list.Remove (container);
            RefreshGrid ();
        }

        [UsedImplicitly]
        private void btnChoose_Clicked (object sender, EventArgs e)
        {
            Button btn = (Button) sender;
            double value = (double) btn.Data ["Value"];
            AddMoney (value, 1);
        }

        private void AddMoney (double amount, int quantity, bool refreshGrid = true)
        {
            MoneyContainer mc = list.Find (m => m.Amount.IsEqualTo (amount));

            if (mc != null)
                mc.Quantity += quantity;
            else
                list.Add (new MoneyContainer (amount, quantity));

            if (refreshGrid)
                RefreshGrid ();
        }

        #endregion
    }
}
