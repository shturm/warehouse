//
// ChooseStockTaking.cs
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

using Warehouse.Business;
using Warehouse.Business.Operations;
using Warehouse.Data;
using Warehouse.Presentation.Preview;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseStockTaking : ChooseOperation<StockTaking>
    {
        public ChooseStockTaking (DocumentChoiceType choiceType) : base (choiceType) { }

        protected override string GetIconName ()
        {
            return "Icons.StockTaking32.png";
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChoose.Title = Translator.GetString ("Stock-takings - Select Document");
        }

        protected override void GetEntities ()
        {
            entities = StockTaking.GetAll (GetDateFilter ());
            base.GetEntities ();
        }

        protected override bool GetPreviewVisible ()
        {
            return BusinessDomain.AppConfiguration.ShowStockTakingsPreview;
        }

        protected override void SetPreviewVisible (bool visible)
        {
            BusinessDomain.AppConfiguration.ShowStockTakingsPreview = visible;
        }

        protected override PreviewOperation GetPreviewWidget ()
        {
            return new PreviewStockTaking ();
        }

        protected override void AnnulOperation ()
        {
            StockTaking stockTaking = SelectedItem;
            if (!stockTaking.CheckIsNewFormat ()) {
                MessageError.ShowDialog (string.Format (Translator.GetString ("This stock-taking cannot be annulled because it is created with an older version of {0}! Only new stock-taking operations can be annulled."), DataHelper.ProductName));
                return;
            }

            stockTaking.Annul ();
        }

        protected override void btnOK_Clicked (object o, System.EventArgs args)
        {
            if (!SelectedItem.CheckIsNewFormat ()) {
                MessageError.ShowDialog (string.Format (Translator.GetString ("This stock-taking cannot be edited because it is created with an older version of {0}! Only new stock-taking operations can be edited."), DataHelper.ProductName));
                return;
            }

            base.btnOK_Clicked (o, args);
        }
    }
}
