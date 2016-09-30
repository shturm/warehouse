//
// Choose.cs
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Glade;
using Gtk;
using Warehouse.Business.Reporting;
using Warehouse.Component;
using Warehouse.Business;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.Model;
using Warehouse.Presentation.Reporting;
using RowActivatedArgs = Warehouse.Data.RowActivatedArgs;

namespace Warehouse.Presentation.Dialogs
{
    public abstract class Choose<T> : Choose
    {
        protected IManageableListModel<T> entities;

        public virtual long? SelectedItemId
        {
            get
            {
                if (grid.FocusedRow < 0 || grid.FocusedRow >= entities.Count)
                    return null;

                return (long) entities [grid.FocusedRow, "Id"];
            }
        }

        public virtual T SelectedItem
        {
            get
            {
                if (grid.FocusedRow < 0 || grid.FocusedRow >= entities.Count)
                    return default (T);

                return entities [grid.FocusedRow];
            }
        }

        public virtual IList<T> SelectedItems
        {
            get
            {
                List<T> selectedItems = new List<T> ();
                foreach (int sel in grid.Selection) {
                    // HACK: an inexplicable ArgumentOutOfRangeException thrown by the indexer of LazyListModel; trying to trace it
                    if (sel < grid.Model.Count)
                        selectedItems.Add ((T) grid.Model [sel]);
                    else {
                        FieldInfo field = grid.Model.GetType ().GetField ("commandText", BindingFlags.Instance | BindingFlags.NonPublic);
                        string command = "<field commandText not found>";
                        if (field != null)
                            command = (string) field.GetValue (grid.Model);
                        ErrorHandling.LogError (
                            string.Format ("Invalid model. From screen: {0}, command: \"{1}\", count: {2}, requested index: {3}.",
                                GetType ().Name, command, grid.Model.Count, sel),
                            ErrorSeverity.FatalError);
                    }
                }
                return selectedItems;
            }
        }

        protected Choose (DocumentChoiceType choiceType)
            : base (choiceType)
        {
            queryStateKey = GetQueryStateKey (choiceType);
        }

        protected virtual string GetQueryStateKey (DocumentChoiceType choice)
        {
            return string.Format ("{0}_{1}", typeof (T).FullName, choice);
        }

        protected override void RefreshEntities ()
        {
            IDisposable oldEntities = entities as IDisposable;
            GetEntities ();
            grid.Model = entities;
            SelectFirstRow ();

            if (oldEntities != null &&
                !ReferenceEquals (oldEntities, entities))
                oldEntities.Dispose ();
        }
    }

    public abstract class Choose : DialogBase
    {
        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        protected Dialog dlgChoose;
        [Widget]
        protected Button btnOK;
        [Widget]
        private Button btnCancel;
        [Widget]
        protected VBox vbxAdditionalButtons;
        [Widget]
        protected Alignment algDialogIcon;
        [Widget]
        protected Alignment algPreview;
        [Widget]
        protected Alignment algPreviewWidget;
        [Widget]
        protected Table tableFilter;
        [Widget]
        private ScrolledWindow scwGrid;
        [Widget]
        private Label lblHelp;
        [Widget]
        protected Label lblFilter;
        [Widget]
        protected Entry txtFilter;
        [Widget]
        protected Button btnClear;
        [Widget]
        private Label lblRows;
        [Widget]
        private Label lblRowsValue;

#pragma warning restore 649

        #endregion

        protected string queryStateKey;
        protected readonly DocumentChoiceType choiceType;
        protected ListView grid;
        protected bool changingFilter;
        protected ReportFilterDateRange reportFilterDateRange;
        protected bool readOnlyView;

        public override Dialog DialogControl
        {
            get { return dlgChoose; }
        }

        public VBox AdditionalButtons
        {
            get { return vbxAdditionalButtons; }
        }

        public object SelectedObject
        {
            get
            {
                if (grid.FocusedRow < 0 || grid.FocusedRow >= grid.Model.Count)
                    return null;

                return grid.Model [grid.FocusedRow];
            }
        }

        public bool ReadOnlyView
        {
            get { return readOnlyView; }
        }

        protected Choose (DocumentChoiceType choiceType)
        {
            this.choiceType = choiceType;
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.Choose.glade", "dlgChoose");
            form.Autoconnect (this);

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            txtFilter.Changed += txtFilter_Changed;
            txtFilter.Activated += txtFilter_Activated;
            btnClear.Clicked += btnClear_Clicked;
            btnOK.Visible = choiceType != DocumentChoiceType.Annul;

            CreateDateFilter ();

            base.InitializeForm ();
        }

        private void CreateDateFilter ()
        {
            reportFilterDateRange = GetDateRangeFilter ();
            if (reportFilterDateRange == null)
                return;

            DataFilter dataFilter = null;
            DataQuery dataQuery;
            if (BusinessDomain.DocumentQueryStates.TryGetValue (queryStateKey, out dataQuery))
                dataFilter = dataQuery.Filters.Count > 0 ? dataQuery.Filters [0] : null;

            if (dataFilter == null)
                dataFilter = GetDefaultDateRangeFilter ();

            reportFilterDateRange.SetDataFilter (dataFilter);
            reportFilterDateRange.FilterChanged += (sender, e) => RefreshEntities ();

            tableFilter.Attach (reportFilterDateRange.Label, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            tableFilter.Attach (reportFilterDateRange.EntryWidget, 1, 3, 1, 2, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            tableFilter.ShowAll ();
        }

        protected abstract void RefreshEntities ();

        protected virtual ReportFilterDateRange GetDateRangeFilter ()
        {
            return new ReportFilterDateRange (true, true, DataFilterLabel.OperationDate, DataField.OperationDate);
        }

        protected virtual DataFilter GetDefaultDateRangeFilter ()
        {
            return new DataFilter { Values = new object [] { null, null, FilterDateRange.DateRanges.Today } };
        }

        protected override void InitializeFormStrings ()
        {
            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
            btnClear.SetChildLabelText (Translator.GetString ("Clear"));

            lblFilter.SetText (Translator.GetString ("Filter"));
            lblRows.SetText (Translator.GetString ("Rows"));
            lblRowsValue.SetText (string.Empty);
            lblHelp.SetText (Translator.GetString ("Arrows to select, OK to confirm, Cancel to exit"));

            base.InitializeFormStrings ();
        }

        protected virtual void InitializeGrid ()
        {
            grid = new ListView ();

            scwGrid.Add (grid);
            grid.Show ();

            grid.AllowSelect = true;
            grid.AllowMultipleSelect = false;
            grid.CellsFucusable = false;
            grid.RulesHint = true;
            grid.SortColumnsHint = true;
            grid.RowActivated += grid_RowActivated;
            grid.EnableAutoFilter = true;
            grid.AutoFilterChanged += grid_AutoFilterChanged;
            grid.AutoFilterApplied += grid_AutoFilterApplied;
            grid.Realized += grid_Realized;
            grid.GrabFocus ();
        }

        protected virtual void GetEntities ()
        {
            BusinessDomain.DocumentQueryStates [queryStateKey] = GetDateFilter ();
        }

        protected DataQuery GetDateFilter ()
        {
            return reportFilterDateRange != null ? new DataQuery (reportFilterDateRange.GetDataFilter ()) : new DataQuery ();
        }

        protected virtual void grid_RowActivated (object o, RowActivatedArgs args)
        {
            if (choiceType == DocumentChoiceType.Annul)
                btnAnnul_Clicked (null, null);
            else
                btnOK_Clicked (null, null);
        }

        private void grid_AutoFilterChanged (object sender, EventArgs e)
        {
            if (changingFilter)
                return;

            changingFilter = true;
            txtFilter.Text = grid.AutoFilterValue;
            changingFilter = false;
        }

        private void grid_AutoFilterApplied (object sender, EventArgs e)
        {
            SelectFirstRow ();
        }

        private void grid_Realized (object sender, EventArgs e)
        {
            SelectFirstRow ();
        }

        private void txtFilter_Changed (object sender, EventArgs e)
        {
            if (changingFilter)
                return;

            changingFilter = true;
            grid.AutoFilterValue = txtFilter.Text;
            changingFilter = false;
        }

        private void txtFilter_Activated (object sender, EventArgs e)
        {
            grid.ActivateRow ();
        }

        private void btnClear_Clicked (object sender, EventArgs e)
        {
            if (reportFilterDateRange != null) {
                grid.EnableAutoFilter = false;
                grid.SetAutoFilter (string.Empty, false);
                grid.EnableAutoFilter = true;
                changingFilter = true;
                txtFilter.Text = string.Empty;
                changingFilter = false;
                reportFilterDateRange.SetDataFilter (GetDefaultDateRangeFilter ());
            } else
                grid.SetAutoFilter (string.Empty, false);
        }

        protected virtual void SelectFirstRow ()
        {
            grid.Selection.Clear ();
            int count = grid.Model.Count;
            if (count > 0) {
                grid.Selection.Select (0);
                grid.FocusRow (0);
                btnOK.Sensitive = true;
            } else {
                grid.DefocusCell ();
                btnOK.Sensitive = false;
            }

            lblRowsValue.SetText (Translator.GetString (count.ToString (CultureInfo.InvariantCulture)));
        }

        #region Event handling

        protected virtual void btnOK_Clicked (object o, EventArgs args)
        {
            if (!Validate ())
                return;

            dlgChoose.Respond (ResponseType.Ok);
        }

        protected virtual bool Validate ()
        {
            return true;
        }

        protected virtual void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgChoose.Respond (ResponseType.Cancel);
        }

        protected virtual void btnAnnul_Clicked (object o, EventArgs args)
        {
        }

        #endregion
    }
}
