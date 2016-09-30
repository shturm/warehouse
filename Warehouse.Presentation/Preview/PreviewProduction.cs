//
// PreviewProduction.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   10/04/2007
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

using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component.ListView;
using Warehouse.Data.DataBinding;

namespace Warehouse.Presentation.Preview
{
    public class PreviewProduction : PreviewOperation<ComplexProduction, ComplexProductionDetail>
    {
        public PreviewProduction ()
        {
            InitializeForm ();
        }

        #region Initialization steps

        private void InitializeForm ()
        {
            LocationVisible = true;
            UserVisible = true;

            GridDescriptionVisible = true;
            GridDescription = Translator.GetString ("Materials");
            SecondGridVisible = true;
            SecondGridDescriptionVisible = true;
            SecondGridDescription = Translator.GetString ("Products");
            
            ReInitializeForm (null);
        }

        public override void LoadOperation (object newOperation)
        {
            ReInitializeForm ((ComplexProduction) newOperation);
        }

        private void ReInitializeForm (ComplexProduction production)
        {
            operation = production;

            SetLocation ();
            SetUser ();
            SetOperationTotalAndEditMode ();

            InitializeGrid ();
            InitializeSecondGrid ();
            BindGrid ();
        }

        protected override void InitializePurchasePriceColumn (ColumnController cc)
        {
            if (!BusinessDomain.LoggedUser.HideItemsPurchasePrice)
                base.InitializePurchasePriceColumn (cc);
        }

        protected override void InitializeDiscountColumn (ColumnController cc)
        {
        }

        protected override void InitializeDiscountValueColumn (ColumnController cc)
        {
        }

        private void InitializeSecondGrid ()
        {
            if (secondGrid == null) {
                secondGrid = new ListView { Name = "secondGrid" };

                ScrolledWindow sWindow = new ScrolledWindow { HscrollbarPolicy = PolicyType.Automatic, VscrollbarPolicy = PolicyType.Automatic };
                sWindow.Add (secondGrid);

                algSecondGrid.Add (sWindow);
                sWindow.Show ();
                secondGrid.Show ();
            }

            ColumnController cc = new ColumnController ();

            CellText ct = new CellText ("ItemName");
            colSecondItem = new Column (Translator.GetString ("Item"), ct, 1);
            cc.Add (colSecondItem);

            CellTextQuantity ctq = new CellTextQuantity ("Quantity");
            colSecondQuantity = new Column (Translator.GetString ("Qtty"), ctq, 0.1) { MinWidth = 70 };
            cc.Add (colSecondQuantity);

            if (!BusinessDomain.LoggedUser.HideItemsPurchasePrice) {
                CellTextCurrency ctc = new CellTextCurrency ("PriceIn", PriceType.Purchase);
                colSecondPurchaseValue = new Column (Translator.GetString ("Purchase price"), ctc, 0.1) {MinWidth = 70};
                cc.Add (colSecondPurchaseValue);
            }

            if (BusinessDomain.AppConfiguration.AllowItemLotName) {
                ct = new CellText ("Lot");
                colSecondLot = new Column (Translator.GetString ("Lot"), ct, 0.1) { MinWidth = 70 };
                cc.Add (colSecondLot);
            }

            if (BusinessDomain.AppConfiguration.AllowItemSerialNumber) {
                ct = new CellText ("SerialNumber");
                colSecondSerialNo = new Column (Translator.GetString ("Serial number"), ct, 0.1) { MinWidth = 80 };
                cc.Add (colSecondSerialNo);
            }

            CellTextDate ctd;
            if (BusinessDomain.AppConfiguration.AllowItemExpirationDate) {
                ctd = new CellTextDate ("ExpirationDate");
                colSecondExpirationDate = new Column (Translator.GetString ("Expiration date"), ctd, 0.1) { MinWidth = 70 };
                cc.Add (colSecondExpirationDate);
            }

            if (BusinessDomain.AppConfiguration.AllowItemManufacturedDate) {
                ctd = new CellTextDate ("ProductionDate");
                colSecondProductionDate = new Column (Translator.GetString ("Production date"), ctd, 0.1) { MinWidth = 70 };
                cc.Add (colSecondProductionDate);
            }

            if (BusinessDomain.AppConfiguration.AllowItemLocation) {
                ct = new CellText ("LotLocation");
                colSecondLotLocation = new Column (Translator.GetString ("Lot location"), ct, 0.1) { MinWidth = 70 };
                cc.Add (colSecondLotLocation);
            }

            secondGrid.ColumnController = cc;
            secondGrid.AllowSelect = false;
            secondGrid.CellsFucusable = true;
            secondGrid.ManualFucusChange = true;
            secondGrid.RulesHint = true;
        }

        protected override void BindGrid ()
        {
            BindGrid (grid, operation == null ? new BindList<ComplexProductionDetail> () : operation.DetailsMat);
            BindGrid (secondGrid, operation == null ? new BindList<ComplexProductionDetail> () : operation.DetailsProd);
        }

        #endregion
    }
}
