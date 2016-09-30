//
// ChooseLot.cs
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

using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component.ListView;
using Warehouse.Presentation.Reporting;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseLot : Choose<Lot>
    {
        private readonly long itemId;
        private readonly string itemName;
        private readonly long? locationId;

        public ChooseLot (long itemId, string itemName, long? locationId)
            : base (DocumentChoiceType.Choose)
        {
            this.itemId = itemId;
            this.itemName = itemName;
            this.locationId = locationId;

            Initialize ();
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            Image icon = FormHelper.LoadImage ("Icons.Goods32.png");
            dlgChoose.Icon = icon.Pixbuf;
            algDialogIcon.Add (icon);
            icon.Show ();

            dlgChoose.HeightRequest = 300;
            dlgChoose.WidthRequest = 800;

            InitializeFormStrings ();
            InitializeGrid ();
        }

        protected override ReportFilterDateRange GetDateRangeFilter ()
        {
            return null;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChoose.Title = string.Format (Translator.GetString ("{0} - Select Lot"), itemName);
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();
            GetEntities ();

            ColumnController cc = new ColumnController ();

            CellTextEnumerator cte = new CellTextEnumerator ();
            Column colNumber = new Column ("№", cte, 0.01) { MinWidth = 25, Visible = BusinessDomain.AppConfiguration.EnableLineNumber };
            cc.Add (colNumber);

            CellTextDouble ctf;
            Column col = null;
            if (!BusinessDomain.LoggedUser.HideItemsPurchasePrice) {
                ctf = new CellTextCurrency ("PriceIn", PriceType.Purchase);
                col = new Column (Translator.GetString ("Purchase price"), ctf, 0.1) { MinWidth = 70 };
                cc.Add (col);
            }

            if (!BusinessDomain.LoggedUser.HideItemsAvailability) {
                ctf = new CellTextQuantity ("AvailableQuantity");
                col = new Column (Translator.GetString ("Qtty"), ctf, 0.1, "AvailableQuantity") { MinWidth = 70 };
                cc.Add (col);
            }

            CellText ct;
            if (BusinessDomain.AppConfiguration.AllowItemLotName) {
                ct = new CellText ("Name") { IsEditable = true };
                col = new Column (Translator.GetString ("Lot"), ct, 0.1) { MinWidth = 70 };
                cc.Add (col);
                entities.FilterProperties.Add ("Name");
            }

            if (BusinessDomain.AppConfiguration.AllowItemSerialNumber) {
                ct = new CellText ("SerialNumber");
                col = new Column (Translator.GetString ("Serial number"), ct, 0.1) { MinWidth = 80 };
                cc.Add (col);
                entities.FilterProperties.Add ("SerialNumber");
            }

            CellTextDate ctd;
            if (BusinessDomain.AppConfiguration.AllowItemExpirationDate) {
                ctd = new CellTextDate ("ExpirationDate");
                col = new Column (Translator.GetString ("Expiration date"), ctd, 0.1) { MinWidth = 70 };
                cc.Add (col);
            }

            if (BusinessDomain.AppConfiguration.AllowItemManufacturedDate) {
                ctd = new CellTextDate ("ProductionDate");
                col = new Column (Translator.GetString ("Production date"), ctd, 0.1) { MinWidth = 70 };
                cc.Add (col);
            }

            if (BusinessDomain.AppConfiguration.AllowItemLocation) {
                ct = new CellText ("Location");
                col = new Column (Translator.GetString ("Lot location"), ct, 0.1) { MinWidth = 70 };
                cc.Add (col);
                entities.FilterProperties.Add ("Location");
            }

            // If no columns are visible show the number at least
            if (col == null)
                colNumber.Visible = true;

            grid.ColumnController = cc;
            grid.Model = entities;
        }

        protected override void GetEntities ()
        {
            entities = Lot.GetAvailable (itemId, locationId);
            base.GetEntities ();
        }

        #endregion
    }
}
