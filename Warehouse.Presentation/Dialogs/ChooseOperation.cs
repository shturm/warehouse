//
// ChooseOperation.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   01.27.2011
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
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Presentation.Preview;
using Warehouse.Presentation.Widgets;

namespace Warehouse.Presentation.Dialogs
{
    public abstract class ChooseOperation<T> : ChooseOperationBase<T> where T : Operation
    {
        protected ChooseOperation (DocumentChoiceType choiceType)
            : base (choiceType)
        {
        }

        protected ChooseOperation (bool highlightEntities, DocumentChoiceType choiceType, OperationType operationType = OperationType.Any)
            : base (highlightEntities, choiceType, operationType)
        {
        }

        protected override void AnnulOperation ()
        {
            SelectedItem.Annul ();
        }
    }

    public abstract class ChooseOperationBase<T> : Choose<T> where T : class
    {
        private readonly bool highlightEntities;
        private PangoStyle highlight;
        private Dictionary<long, long> highlightedEntityIds;
        protected PictureButton btnAnnul;
        protected PictureToggleButton btnPreview;
        protected OperationType operationType = OperationType.Any;
        private PreviewOperation previewWidget;

        public Dictionary<long, long> HighlightedEntityIds
        {
            get { return highlightedEntityIds ?? (highlightedEntityIds = GetHighlightedEntityIds ()); }
        }

        protected ChooseOperationBase (DocumentChoiceType choiceType)
            : this (false, choiceType)
        {
        }

        protected ChooseOperationBase (bool highlightEntities, DocumentChoiceType choiceType, OperationType operationType = OperationType.Any)
            : base (choiceType)
        {
            this.highlightEntities = highlightEntities;
            this.operationType = operationType;

            Initialize ();
        }

        #region Initialization steps

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            dlgChoose.Icon = GetSmallIcon ().Pixbuf;
            Image icon = GetIcon ();
            icon.Show ();
            algDialogIcon.Add (icon);

            btnAnnul = new PictureButton ();
            btnAnnul.Show ();
            btnAnnul.SetImage (FormHelper.LoadImage ("Icons.Annul24.png"));
            btnAnnul.SetText (Translator.GetString ("Annul"));
            btnAnnul.Clicked += btnAnnul_Clicked;
            vbxAdditionalButtons.PackStart (btnAnnul, false, true, 0);

            btnPreview = new PictureToggleButton ();
            btnPreview.Show ();
            btnPreview.SetImage (FormHelper.LoadImage ("Icons.Zoom24.png"));
            btnPreview.SetText (Translator.GetString ("Preview"));
            btnPreview.Clicked += btnPreview_Clicked;
            vbxAdditionalButtons.PackStart (btnPreview, false, true, 0);

            dlgChoose.HeightRequest = 400;
            dlgChoose.WidthRequest = 800;

            InitializeFormStrings ();
            InitializeGrid ();

            UpdatePreviewWidget ();
            btnPreview.Active = GetPreviewVisible ();
            ChooseOperation.OnOpened (this);
        }

        protected virtual Image GetIcon ()
        {
            return FormHelper.LoadImage (GetIconName ());
        }

        protected virtual Image GetSmallIcon ()
        {
            return FormHelper.LoadImage (GetIconName ());
        }

        protected abstract string GetIconName ();

        protected override void InitializeGrid ()
        {
            base.InitializeGrid ();
            GetEntities ();

            RefreshGridColumns ();
            grid.Model = entities;

            grid.AllowMultipleSelect = choiceType == DocumentChoiceType.CreateChildDocument;
        }

        protected void RefreshGridColumns ()
        {
            ColumnController cc = new ColumnController ();

            AddNumberColumn (cc);

            AddPartnerColumn (cc);

            cc.Add (new Column (Translator.GetString ("Location"), "Location", 0.2, "Location") { MinWidth = 70 });

            CellTextDate ctd = new CellTextDate ("Date");
            cc.Add (new Column (Translator.GetString ("Date"), ctd, 0.1, "Date") { MinWidth = 80 });

            if (highlightEntities) {
                highlight = new PangoStyle { Color = Colors.Red };

                grid.CellStyleProvider = grid_CellStyleProvider;
            }

            grid.ColumnController = cc;
            grid.CellFocusIn += grid_CellFocusIn;
        }

        protected virtual void AddNumberColumn (ColumnController cc)
        {
            CellTextNumber ctn = new CellTextNumber ("Id") { FixedDigits = BusinessDomain.AppConfiguration.DocumentNumberLength };
            cc.Add (new Column (Translator.GetString ("Number"), ctn, 0.1, "Id") { MinWidth = 90 });
        }

        protected virtual void AddPartnerColumn (ColumnController cc)
        {
        }

        protected override void GetEntities ()
        {
            entities.FilterProperties.Add ("Id");
            entities.FilterProperties.Add ("Location");
            entities.Sort ("Id", SortDirection.Descending);
            base.GetEntities ();
        }

        protected virtual Dictionary<long, long> GetHighlightedEntityIds ()
        {
            return new Dictionary<long, long> ();
        }

        private void grid_CellStyleProvider (object sender, CellStyleQueryEventArgs args)
        {
            long operationId = (long) entities [args.Cell.Row, "Id"];

            if (HighlightedEntityIds.ContainsKey (operationId))
                args.Style = highlight;
        }

        protected override void grid_RowActivated (object o, Data.RowActivatedArgs args)
        {
            if (SelectedItem == null)
                return;

            base.grid_RowActivated (o, args);
        }

        protected override void SelectFirstRow ()
        {
            base.SelectFirstRow ();

            bool hasEntities = grid.Model.Count > 0;
            vbxAdditionalButtons.Sensitive = hasEntities;
            algPreview.Visible = hasEntities && btnPreview.Active;
            if (hasEntities && btnPreview.Active)
                previewWidget.LoadOperation (SelectedItem);
        }

        #endregion

        protected override void btnAnnul_Clicked (object o, EventArgs args)
        {
            if (Message.ShowDialog (GetAnnulTitle (), string.Empty, GetAnnulQuestion (), "Icons.Question32.png",
                MessageButtons.YesNo) != ResponseType.Yes) {
                return;
            }

            string error;
            string warning;
            if (!BusinessDomain.CanAnnulOperation (SelectedItem, out error, out warning)) {
                if (warning != null) {
                    if (MessageError.ShowDialog (warning, buttons: MessageButtons.YesNo) != ResponseType.Yes)
                        return;
                } else {
                    MessageError.ShowDialog (error);
                    return;
                }
            }

            try {
                AnnulOperation ();
                GetEntities ();
                grid.Model = entities;
                if (btnPreview.Active)
                    previewWidget.LoadOperation (SelectedItem);
            } catch (ArgumentException) {
            } catch (InsufficientItemAvailabilityException ex) {
                if (btnPreview.Active)
                    previewWidget.LoadOperation (SelectedItem);

                MessageError.ShowDialog (string.Format (Translator.GetString ("The operation cannot be annulled " +
                    "because the annulment will cause negative availability of item \"{0}\"."), ex.ItemName),
                    ErrorSeverity.Warning, ex);
            }
        }

        protected virtual string GetAnnulTitle ()
        {
            return Translator.GetString ("Annul Operation");
        }

        protected virtual string GetAnnulQuestion ()
        {
            return Translator.GetString ("Are you sure you want to annul this operation?");
        }

        protected abstract void AnnulOperation ();

        #region Preview handling

        private void btnPreview_Clicked (object sender, EventArgs e)
        {
            if (btnPreview.Active) {
                previewWidget.LoadOperation (SelectedItem);
                algPreview.Show ();
            } else
                algPreview.Hide ();

            SetPreviewVisible (btnPreview.Active);
        }

        private void grid_CellFocusIn (object sender, CellEventArgs args)
        {
            if (btnPreview.Active)
                previewWidget.LoadOperation (SelectedItem);
        }

        protected virtual bool GetPreviewVisible ()
        {
            return false;
        }

        protected virtual void SetPreviewVisible (bool visible)
        {
        }

        protected void UpdatePreviewWidget ()
        {
            algPreviewWidget.DestroyChild ();

            previewWidget = GetPreviewWidget ();
            if (previewWidget != null) {
                previewWidget.Show ();
                algPreviewWidget.Add (previewWidget);
                btnPreview.Visible = true;
            } else
                btnPreview.Visible = false;
        }

        protected virtual PreviewOperation GetPreviewWidget ()
        {
            return null;
        }

        #endregion

        protected override void RefreshEntities ()
        {
            base.RefreshEntities ();

            if (entities.Count > 0 && btnPreview.Active)
                previewWidget.LoadOperation (SelectedItem);
        }

        protected override bool Validate ()
        {
            if (choiceType != DocumentChoiceType.Edit || readOnlyView)
                return true;

            string message;
            if (BusinessDomain.CanEditOperation (SelectedItem, out message, out readOnlyView))
                return true;

            if (!string.IsNullOrWhiteSpace (message))
                MessageError.ShowDialog (message);

            return readOnlyView;
        }
    }

    public class ChooseOperation
    {
        public static event EventHandler ChooseOperationOpened;

        public static void OnOpened (DialogBase dialog)
        {
            EventHandler pageOpened = ChooseOperationOpened;
            if (pageOpened != null)
                pageOpened (dialog, EventArgs.Empty);
        }
    }
}
