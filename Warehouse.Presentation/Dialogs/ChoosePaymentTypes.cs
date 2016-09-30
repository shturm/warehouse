//
// ChoosePaymentTypes.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   10.27.2009
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
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public class ChoosePaymentTypes : DialogBase
    {
        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgChoosePaymentTypes;
        [Widget]
        private TreeView treeView;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

#pragma warning restore 649

        #endregion

        private readonly List<BasePaymentType> selected;

        public override Dialog DialogControl
        {
            get { return dlgChoosePaymentTypes; }
        }

        public List<BasePaymentType> Selected
        {
            get { return selected; }
        }

        public ChoosePaymentTypes ()
        {
            selected = new List<BasePaymentType> ();

            Initialize ();
        }

        public ChoosePaymentTypes (IEnumerable<BasePaymentType> paymentTypes)
        {
            selected = new List<BasePaymentType> (paymentTypes);

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChoosePaymentTypes.glade", "dlgChoosePaymentTypes");
            form.Autoconnect (this);

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            Load ();

            base.InitializeForm ();
            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChoosePaymentTypes.Title = Translator.GetString ("Choose Payment Types");
        }

        private void Load ()
        {
            ListStore listStore = new ListStore (typeof (bool), typeof (string), typeof (BasePaymentType));
            for (int i = (int) BasePaymentType.Cash; i <= (int) BasePaymentType.Advance; i++) {
                BasePaymentType paymentType = (BasePaymentType) i;
                listStore.AppendValues (selected.Contains (paymentType),
                    PaymentType.GetBasePaymentTypeName (paymentType), paymentType);
            }

            CellRendererToggle cellRendererToggle = new CellRendererToggle { Activatable = true };
            cellRendererToggle.Toggled += (sender, e) =>
                {
                    TreeIter row;
                    listStore.GetIter (out row, new TreePath (e.Path));
                    bool value = !(bool) listStore.GetValue (row, 0);
                    BasePaymentType paymentType = (BasePaymentType) listStore.GetValue (row, 2);
                    if (value)
                        selected.Add (paymentType);
                    else
                        selected.Remove (paymentType);
                    listStore.SetValue (row, 0, value);
                };

            treeView.AppendColumn (string.Empty, cellRendererToggle, "active", 0);
            treeView.AppendColumn (string.Empty, new CellRendererText (), "text", 1);
            treeView.AppendColumn (string.Empty, new CellRendererText (), "text", 2).Visible = false;

            treeView.Model = listStore;
        }

        #region Event handling

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (selected.Count == 0) {
                MessageError.ShowDialog (Translator.GetString ("Please select at least one payment type."), ErrorSeverity.Error);
                return;
            }
            
            dlgChoosePaymentTypes.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgChoosePaymentTypes.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
