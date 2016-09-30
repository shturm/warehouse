//
// ChooseEditPriceRule.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   11.04.2009
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
using System.Linq;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Alignment = Pango.Alignment;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseEditPriceRule : ChooseEdit<PriceRule, EmptyGroup>
    {
        private Button btnUp;
        private Button btnDown;
        private TextView preview;
        private readonly List<string> errors = PriceRuleCondition.AllMissingErrors
            .Select (errorKey => string.Format ("{0}: {1}", PriceRuleCondition.TypeToString (errorKey.Key), errorKey.Value))
            .Union (new [] { Translator.GetString ("Error!") })
            .ToList ();

        private readonly PangoStyle highlight = new PangoStyle { Color = Colors.Red };

        public ChooseEditPriceRule ()
        {
            Initialize ();

            lblFilter.Visible = false;
            btnClear.Visible = false;
            txtFilter.Visible = false;

            algGridGroups.Visible = false;
            btnImport.Visible = false;
            btnExport.Visible = false;
            btnGroups.Visible = false;
        }

        public override string [] SelectedItemsText
        {
            get { throw new NotImplementedException (); }
        }

        protected override void GetAllEntities ()
        {
            entities = PriceRule.GetAll ();
        }

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            dlgChooseEdit.Icon = FormHelper.LoadImage ("Icons.PriceRules16.png").Pixbuf;
            Image icon = FormHelper.LoadImage ("Icons.PriceRules32.png");
            algDialogIcon.Add (icon);
            icon.Show ();

            InitializeGrid ();

            hpnMain.Remove (algGrid);

            Table table = new Table (2, 2, false);
            table.ColumnSpacing = 8;
            table.Attach (algGrid, 0, 1, 0, 2);

            btnUp = new Button ();
            btnUp.Clicked += (sender, e) => MovePriceRule (true);
            btnUp.SetChildImage (FormHelper.LoadImage ("Icons.Up24.png"));
            table.Attach (btnUp, 1, 2, 0, 1, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            btnUp.Show ();

            btnDown = new Button ();
            btnDown.Clicked += (sender, e) => MovePriceRule (false);
            btnDown.SetChildImage (FormHelper.LoadImage ("Icons.Down24.png"));
            table.Attach (btnDown, 1, 2, 1, 2, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            btnDown.Show ();

            dlgChooseEdit.VBox.Spacing = 8;

            preview = new TextView { Editable = false };
            preview.Show ();

            Viewport viewport = new Viewport { ShadowType = ShadowType.EtchedIn };
            viewport.Add (preview);
            viewport.Show ();

            ScrolledWindow scrolledWindowPreview = new ScrolledWindow {
                HeightRequest = 250,
                HscrollbarPolicy = PolicyType.Never,
                VscrollbarPolicy = PolicyType.Automatic
            };
            scrolledWindowPreview.Add (viewport);
            scrolledWindowPreview.Show ();

            HBox hBoxPreview = new HBox ();
            hBoxPreview.PackStart (scrolledWindowPreview, true, true, 4);
            hBoxPreview.Show ();

            dlgChooseEdit.VBox.Add (hBoxPreview);

            vboxGrid.Remove (viewportHelp);
            HBox hBoxHelp = new HBox ();
            hBoxHelp.PackStart (viewportHelp, true, true, 4);
            dlgChooseEdit.VBox.PackStart (hBoxHelp, false, false, 0);
            hBoxHelp.Show ();

            hpnMain.Pack2 (table, false, false);
            table.Show ();

            ReinitializeGrid (true, null);

            InitializeFormStrings ();
        }

        private void MovePriceRule (bool up)
        {
            if ((up && grid.FocusedRow < 1) || (!up && grid.FocusedRow > grid.Model.Count - 2))
                return;

            int newRowIndex = up ? grid.FocusedRow - 1 : grid.FocusedRow + 1;

            object swap = grid.Model [newRowIndex];
            grid.Model [newRowIndex] = grid.Model [grid.FocusedRow];
            grid.Model [grid.FocusedRow] = swap;

            PriceRule priceRuleAtNewIndex = (PriceRule) grid.Model [newRowIndex];
            PriceRule priceRuleAtOldIndex = (PriceRule) grid.Model [grid.FocusedRow];
            priceRuleAtNewIndex.Priority = newRowIndex;
            priceRuleAtNewIndex.CommitChanges ();
            priceRuleAtOldIndex.Priority = grid.FocusedRow;
            priceRuleAtOldIndex.CommitChanges ();

            OnEntitiesChanged ();

            grid.FocusRow (newRowIndex);
            grid.Selection.Clear ();
            grid.Selection.Select (newRowIndex);
        }

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();

            grid.CellStyleProvider = grid_CellStyleProvider;

            grid.WidthRequest = 600;
            grid.HeightRequest = 250;

            ColumnController columnController = new ColumnController ();

            Dictionary<bool, string> lookup = new Dictionary<bool, string> { { false, Translator.GetString ("No") }, { true, Translator.GetString ("Yes") } };
            CellTextLookup<bool> cellTextLookup = new CellTextLookup<bool> ("Enabled") { Lookup = lookup, Alignment = Alignment.Center };
            columnController.Add (new Column (Translator.GetString ("Active"), cellTextLookup, 0.1, null) { MinWidth = 70 });
            columnController.Add (new Column (Translator.GetString ("Rule Name"), "Name", 1, null) { MinWidth = 70 });
            columnController.Add (new Column (Translator.GetString ("Priority"), "Priority", 0.1, null) { MinWidth = 90 });

            grid.ColumnController = columnController;
            grid.Selection.Changed += Selection_Changed;
        }

        private void grid_CellStyleProvider (object sender, CellStyleQueryEventArgs args)
        {
            PriceRule priceRule = entities [args.Cell.Row];
            if (priceRule.Conditions.Any (c => c.Error) || priceRule.Actions.Any (a => a.Error))
                args.Style = highlight;
        }

        protected override string KeyForKeyboardBingdings
        {
            get { return "mnuEditAdminPriceRules"; }
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseEdit.Title = Translator.GetString ("Price Rules");

            btnNew.SetChildLabelText (Translator.GetString ("New", "Price rule"));
        }

        private void Selection_Changed (object sender, EventArgs e)
        {
            if (grid.Selection.Count != 1) {
                btnUp.Sensitive = false;
                btnDown.Sensitive = false;
                preview.Buffer.Text = string.Empty;
                return;
            }

            int selected = grid.Selection [0];

            if (grid.Model.Count > 1) {
                btnUp.Sensitive = selected > 0;
                btnDown.Sensitive = selected < grid.Model.Count - 1;
            } else {
                btnUp.Sensitive = false;
                btnDown.Sensitive = false;
            }

            preview.Buffer.Text = string.Empty;
            preview.Buffer.RemoveAllTags (preview.Buffer.StartIter, preview.Buffer.EndIter);
            if (grid.Model.Count > selected) {
                string description = grid.Model [selected].ToString ();
                string [] lines = description.Split (new [] { Environment.NewLine }, StringSplitOptions.None);
                TextIter startIter = preview.Buffer.StartIter;
                for (int i = 0; i < lines.Length; i++) {
                    string line = lines [i];
                    if (errors.Any (line.Contains)) {
                        TextTag tag = new TextTag (i.ToString (CultureInfo.InvariantCulture)) { Foreground = "red" };
                        preview.Buffer.TagTable.Add (tag);
                        preview.Buffer.InsertWithTagsByName (ref startIter, line + Environment.NewLine, tag.Name);
                    } else
                        preview.Buffer.Insert (ref startIter, line + Environment.NewLine);
                }
            } else preview.Buffer.Text = string.Empty;
        }

        #region Button Events

        protected override void btnNew_Clicked (object o, EventArgs args)
        {
            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                using (EditNewPriceRule editNewPriceRule = new EditNewPriceRule (grid.Model.Count))
                    if (editNewPriceRule.Run () == ResponseType.Ok) {
                        selectedId = editNewPriceRule.GetPriceRule ().CommitChanges ().Id;
                        OnEntitiesChanged ();
                    }
            }
        }

        protected override void btnEdit_Clicked (object o, EventArgs args)
        {
            if (grid.FocusedRow < 0)
                return;

            PriceRule selectedPriceRule = (PriceRule) entities [grid.FocusedRow].Clone ();
            selectedId = (int?) selectedPriceRule.Id;

            // Added transaction to ensure that we are connected to the same server in case of
            // master-slave replication
            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) {
                using (EditNewPriceRule editNewPriceRule = new EditNewPriceRule (selectedPriceRule))
                    if (editNewPriceRule.Run () == ResponseType.Ok) {
                        editNewPriceRule.GetPriceRule ().CommitChanges ();
                        OnEntitiesChanged ();
                    }
            }
        }

        protected override void btnDelete_Clicked (object o, EventArgs args)
        {
            DeleteSelectedEntities ();
        }

        protected override bool DeleteEntity (PriceRule entity)
        {
            if (PriceRule.RequestDelete (entity.Id) != DeletePermission.Yes)
                return false;

            PriceRule.Delete (entity.Id);
            return true;
        }

        protected override bool AskDeleteSingleEntity (PriceRule entity)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Price Rule"), string.Empty,
                string.Format (Translator.GetString ("Do you want to delete the price rule \"{0}\"?"), entity.Name),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        protected override bool AskDeleteMultipleEntities (int count)
        {
            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete Price Rules"), string.Empty,
                string.Format (Translator.GetString ("Do you want to delete the selected {0} price rules?"), count),
                "Icons.Delete32.png"))
                return dialog.Run () == ResponseType.Ok;
        }

        #endregion
    }
}
