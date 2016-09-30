//
// PreviewOperation.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/25/2006
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

using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Warehouse.Data.Model;

namespace Warehouse.Presentation.Preview
{
    public abstract class PreviewOperation<TOper, TOperDetail> : PreviewOperation
        where TOperDetail : OperationDetail, new ()
        where TOper : Operation<TOperDetail>
    {
        #region Fields

        protected ListView grid;
        protected ListView secondGrid;

        protected TOper operation;

        #endregion

        #region Grid column handling

        protected Column colItem;
        protected Column colQuantity;
        protected Column colPurchaseValue;
        protected Column colSalePrice;
        protected Column colDiscount;
        protected Column colDiscountValue;
        protected Column colLot;
        protected Column colSerialNo;
        protected Column colExpirationDate;
        protected Column colProductionDate;
        protected Column colLotLocation;

        #endregion

        #region Second Grid column handling

        protected Column colSecondItem;
        protected Column colSecondQuantity;
        protected Column colSecondPurchaseValue;
        protected Column colSecondSalePrice;
        protected Column colSecondDiscount;
        protected Column colSecondDiscountValue;
        protected Column colSecondLot;
        protected Column colSecondSerialNo;
        protected Column colSecondExpirationDate;
        protected Column colSecondProductionDate;
        protected Column colSecondLotLocation;

        #endregion

        #region Properties

        protected bool GridDescriptionVisible
        {
            set { lblGridDescription.Visible = value; }
        }

        protected string GridDescription
        {
            set { lblGridDescription.SetText (value); }
        }

        protected bool SecondGridDescriptionVisible
        {
            set { lblSecondGridDescription.Visible = value; }
        }

        protected string SecondGridDescription
        {
            set { lblSecondGridDescription.SetText (value); }
        }

        protected bool SecondGridVisible
        {
            set { algSecondGrid.Visible = value; }
        }

        #endregion

        protected void SetPartner ()
        {
            lblPartnerValue.SetText (operation == null ? string.Empty : operation.PartnerName);
        }

        protected void SetLocation ()
        {
            lblLocationValue.SetText (operation == null ? string.Empty : operation.Location);
        }

        protected void SetUser ()
        {
            lblUserValue.SetText (operation == null ? string.Empty : operation.UserName);
        }

        protected virtual void BindGrid ()
        {
            BindGrid (grid, operation == null ? new BindList<TOperDetail> () : operation.Details);
        }

        protected void BindGrid (ListView listView, object source)
        {
            if (listView == null)
                return;

            listView.Model = new BindingListModel (source);
        }

        protected void SetOperationTotalAndEditMode ()
        {
            if (operation != null && operation.Details.Count == 0)
                operation.LoadDetails ();

            SetOperationTotal (operation);
        }

        #region Totals handling

        protected void SetOperationTotal (Operation oper)
        {
            double totalPlusVat = operation == null ? 0 : oper.TotalPlusVAT;

            if (totalPlusVat.IsZero ()) {
                lblBigTotalValue.Hide ();
                lblBigTotal.Hide ();
            } else {
                lblBigTotalValue.SetText (Currency.ToString (totalPlusVat, operation.TotalsPriceType));
                lblBigTotalValue.Show ();
                lblBigTotal.Show ();
            }
        }

        #endregion

        protected void InitializeGrid ()
        {
            if (grid == null) {
                grid = new ListView { Name = "grid" };

                ScrolledWindow sWindow = new ScrolledWindow { HscrollbarPolicy = PolicyType.Automatic, VscrollbarPolicy = PolicyType.Automatic };
                sWindow.Add (grid);

                algGrid.Add (sWindow);
                sWindow.Show ();
                grid.Show ();
            }

            ColumnController cc = new ColumnController ();

            InitializeItemColumn (cc);

            InitializeQuantityColumn (cc);

            InitializePurchasePriceColumn (cc);

            InitializeSalePriceColumn (cc);

            //if (BusinessDomain.AppConfiguration.AllowPercentDiscounts)
            //    InitializeDiscountColumn (cc);

            //if (BusinessDomain.AppConfiguration.AllowValueDiscounts)
            //    InitializeDiscountValueColumn (cc);

            if (BusinessDomain.AppConfiguration.AllowItemLotName)
                InitializeLotNameColumn (cc);
            else
                InitializeDisabledLotNameColumn (cc);

            if (BusinessDomain.AppConfiguration.AllowItemSerialNumber)
                InitializeSerialNumberColumn (cc);

            if (BusinessDomain.AppConfiguration.AllowItemExpirationDate)
                InitializeExpirationDateColumn (cc);

            if (BusinessDomain.AppConfiguration.AllowItemManufacturedDate)
                InitializeManufacturedDateColumn (cc);

            if (BusinessDomain.AppConfiguration.AllowItemLocation)
                InitializeLotLocationColumn (cc);
            else
                InitializeDisabledLotLocationColumn (cc);

            //InitializeTotalColumn (cc);

            grid.ColumnController = cc;
            grid.AllowSelect = false;
            grid.CellsFucusable = true;
            grid.ManualFucusChange = true;
            grid.RulesHint = true;
        }

        protected virtual void InitializeItemColumn (ColumnController cc)
        {
            CellText ct = new CellText ("ItemName");
            colItem = new Column (Translator.GetString ("Item"), ct, 1) { MinWidth = 70 };
            cc.Add (colItem);
        }

        protected virtual void InitializeQuantityColumn (ColumnController cc)
        {
            CellTextQuantity ctf = new CellTextQuantity ("Quantity");
            colQuantity = new Column (Translator.GetString ("Qtty"), ctf, 0.1) { MinWidth = 70 };
            cc.Add (colQuantity);
        }

        protected virtual void InitializePurchasePriceColumn (ColumnController cc)
        {
            CellTextCurrency ctf = new CellTextCurrency ("PriceIn", PriceType.Purchase);
            colPurchaseValue = new Column (Translator.GetString ("Purchase price"), ctf, 0.1) { MinWidth = 70 };
            cc.Add (colPurchaseValue);
        }

        protected virtual void InitializeSalePriceColumn (ColumnController cc)
        {
            CellTextCurrency ctf = new CellTextCurrency ("PriceOut");
            colSalePrice = new Column (Translator.GetString ("Sale price"), ctf, 0.1) { MinWidth = 70 };
            cc.Add (colSalePrice);
        }

        protected virtual void InitializeDiscountColumn (ColumnController cc)
        {
            CellTextDouble ctf = new CellTextDouble ("Discount") { FixedFaction = BusinessDomain.AppConfiguration.PercentPrecision };
            colDiscount = new Column (Translator.GetString ("Discount %"), ctf, 0.1) { MinWidth = 70 };
            cc.Add (colDiscount);
        }

        protected virtual void InitializeDiscountValueColumn (ColumnController cc)
        {
            CellTextCurrency ctf = new CellTextCurrency ("DiscountValue");
            colDiscountValue = new Column (Translator.GetString ("Discount value"), ctf, 0.1) { MinWidth = 70 };
            cc.Add (colDiscountValue);
        }

        protected virtual void InitializeLotNameColumn (ColumnController cc)
        {
            CellText ct = new CellText ("Lot");
            colLot = new Column (Translator.GetString ("Lot"), ct, 0.1) { MinWidth = 70 };
            cc.Add (colLot);
        }

        protected virtual void InitializeDisabledLotNameColumn (ColumnController cc)
        {
        }

        protected virtual void InitializeSerialNumberColumn (ColumnController cc)
        {
            CellText ct = new CellText ("SerialNumber");
            colSerialNo = new Column (Translator.GetString ("Serial number"), ct, 0.1) { MinWidth = 80 };
            cc.Add (colSerialNo);
        }

        protected virtual void InitializeExpirationDateColumn (ColumnController cc)
        {
            CellTextDate ctd = new CellTextDate ("ExpirationDate");
            colExpirationDate = new Column (Translator.GetString ("Expiration date"), ctd, 0.1) { MinWidth = 70 };
            cc.Add (colExpirationDate);
        }

        protected virtual void InitializeManufacturedDateColumn (ColumnController cc)
        {
            CellTextDate ctd = new CellTextDate ("ProductionDate");
            colProductionDate = new Column (Translator.GetString ("Production date"), ctd, 0.1) { MinWidth = 70 };
            cc.Add (colProductionDate);
        }

        protected virtual void InitializeLotLocationColumn (ColumnController cc)
        {
            CellText ct = new CellText ("LotLocation");
            colLotLocation = new Column (Translator.GetString ("Lot location"), ct, 0.1) { MinWidth = 70 };
            cc.Add (colLotLocation);
        }

        protected virtual void InitializeDisabledLotLocationColumn (ColumnController cc)
        {
        }
    }

    public abstract class PreviewOperation : Alignment
    {
        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private VBox vbxOperationRoot;
        [Widget]
        private Label lblPartner;
        [Widget]
        protected Label lblPartnerValue;
        [Widget]
        private Label lblLocation;
        [Widget]
        protected Label lblLocationValue;
        [Widget]
        private Label lblSrcLocation;
        [Widget]
        protected Label lblSrcLocationValue;
        [Widget]
        private Label lblDstLocation;
        [Widget]
        protected Label lblDstLocationValue;
        [Widget]
        private Label lblUser;
        [Widget]
        protected Label lblUserValue;
        [Widget]
        protected Label lblBigTotal;
        [Widget]
        protected Label lblBigTotalValue;

        [Widget]
        protected Label lblGridDescription;
        [Widget]
        protected Alignment algGrid;
        [Widget]
        protected Label lblSecondGridDescription;
        [Widget]
        protected Alignment algSecondGrid;

#pragma warning restore 649

        #endregion

        #region Properties

        protected bool PartnerVisible
        {
            set
            {
                //lblPartner.Visible = value;
                lblPartnerValue.Visible = value;
            }
        }

        protected bool LocationVisible
        {
            set
            {
                //lblLocation.Visible = value;
                lblLocationValue.Visible = value;
            }
        }

        protected bool SrcLocationVisible
        {
            set
            {
                //lblSrcLocation.Visible = value;
                lblSrcLocationValue.Visible = value;
            }
        }

        protected bool DstLocationVisible
        {
            set
            {
                //lblDstLocation.Visible = value;
                lblDstLocationValue.Visible = value;
            }
        }

        protected bool UserVisible
        {
            set
            {
                //lblUser.Visible = value;
                lblUserValue.Visible = value;
            }
        }

        #endregion

        protected PreviewOperation ()
            : base (0, 0, 1, 1)
        {
            InitializeForm ();
        }

        private void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Preview.PreviewOperation.glade", "vbxOperationRoot");
            form.Autoconnect (this);
            Add (vbxOperationRoot);

            InitializeFormStrings ();
        }

        private void InitializeFormStrings ()
        {
            lblPartner.SetText (Translator.GetString ("Partner"));
            lblLocation.SetText (Translator.GetString ("Location"));
            lblSrcLocation.SetText (Translator.GetString ("From location"));
            lblDstLocation.SetText (Translator.GetString ("To location"));
            lblUser.SetText (Translator.GetString ("User"));
            lblBigTotal.SetText (Translator.GetString ("Total:"));
        }

        public abstract void LoadOperation (object newOperation);
    }
}
