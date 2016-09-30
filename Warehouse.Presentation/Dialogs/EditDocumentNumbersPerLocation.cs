//
// EditDocumentNumbersPerLocation.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   01.12.2010
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
using Gdk;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Component.Documenting;
using Warehouse.Component.ListView;
using Warehouse.Data;
using Warehouse.Data.Model;
using Key = Gdk.Key;
using Timeout = GLib.Timeout;

namespace Warehouse.Presentation.Dialogs
{
    /// <summary>
    /// Allows the user to edit the ranges of document numbers used with the different locations.
    /// </summary>
    public class EditDocumentNumbersPerLocation : DialogBase
    {
        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEditDocumentNumbersPerLocation;
        [Widget]
        private CheckButton chkEditAllRanges;
        [Widget]
        private Label lblRangeStart;
        [Widget]
        private SpinButton spbRangeStart;
        [Widget]
        private Label lblRangeSize;
        [Widget]
        private SpinButton spbRangeSize;
        [Widget]
        private Label lblCurrentLocations;
        [Widget]
        private Label lblCurrentLocationsValue;
        [Widget]
        private Label lblMaxLocations;
        [Widget]
        private Label lblMaxLocationsValue;
        [Widget]
        private Frame frmMessage;
        [Widget]
        private Alignment algMessage;
        [Widget]
        private ScrolledWindow scwLocations;
        [Widget]
        private ScrolledWindow scwDocumentNumbers;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

#pragma warning restore 649

        #endregion

        private ListView gridLocations;
        private ListView gridDocumentNumbers;

        private LazyListModel<Location> allLocations;
        private OperationNumberingInfo [] allStartNumbers;
        private OperationNumbersUsage [] numbersUsagePerLocation;
        private List<KeyValuePair<long, BindingListModel<OperationNumberingInfo>>> allStartNumbersPerLocation = new List<KeyValuePair<long, BindingListModel<OperationNumberingInfo>>> ();
        private ObjectsContainer<OperationType, long> [] numbersUsageStarts;
        private readonly NumberFormatInfo formatBigNumber = new NumberFormatInfo { NumberGroupSeparator = " ", NumberGroupSizes = new [] { 3 }, NumberDecimalDigits = 0 };
        private bool loading = true;
        private bool hasEmptyRange;
        private string emptyRangeMessage;

        public override Dialog DialogControl
        {
            get { return dlgEditDocumentNumbersPerLocation; }
        }

        public EditDocumentNumbersPerLocation ()
        {
            Initialize ();
        }

        public EditDocumentNumbersPerLocation (long locationId)
            : this ()
        {
            SelectLocation (locationId);
        }

        /// <summary>
        /// Initializes the form by reading its *.glade file and setting other necessary properties.
        /// </summary>
        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditDocumentNumbersPerLocation.glade", "dlgEditDocumentNumbersPerLocation");
            form.Autoconnect (this);

            dlgEditDocumentNumbersPerLocation.Icon = FormHelper.LoadImage ("Icons.Location16.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();

            InitializeDocumentNumbersGrid ();
            InitializeLocationsGrid ();

            if (allStartNumbersPerLocation.Count > 0) {
                BindingListModel<OperationNumberingInfo> model = allStartNumbersPerLocation [0].Value;
                if (model.Count > 0)
                    spbRangeStart.Value = model [0].StartNumber;
            }

            if (allStartNumbersPerLocation.Count > 1) {
                BindingListModel<OperationNumberingInfo> model = allStartNumbersPerLocation [1].Value;
                if (model.Count > 0)
                    spbRangeSize.Value = model [0].StartNumber - spbRangeStart.Value;
            }

            InitializeFormStrings ();

            loading = false;
        }

        /// <summary>
        /// Initializes the strings contained in the <see cref="DialogBase"/>.
        /// </summary>
        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgEditDocumentNumbersPerLocation.Title = Translator.GetString ("Document Numbers per Location");

            chkEditAllRanges.Label = Translator.GetString ("Adjust all numbers");
            lblRangeStart.SetText (Translator.GetString ("Numbers start:"));
            lblRangeSize.SetText (Translator.GetString ("Numbers per location:"));
            lblCurrentLocations.SetText (Translator.GetString ("Number of locations:"));
            lblMaxLocations.SetText (Translator.GetString ("Max supported locations:"));

            emptyRangeMessage = Translator.GetString ("Some operation types have very little numbers left! This may be due to old operations using numbers within the same numbers range.");
            algMessage.Add (new WrapLabel { Markup = new PangoStyle { Text = emptyRangeMessage, Color = Colors.Red }, VerticalAlignment = VerticalAlignment.Center });
            algMessage.ShowAll ();

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        private void InitializeLocationsGrid ()
        {
            gridLocations = new ListView
                {
                    Name = "gridLocations",
                    WidthRequest = 250,
                    HeightRequest = 400,
                    AllowMultipleSelect = false
                };
            gridLocations.Selection.Changed += Selection_Changed;

            gridLocations.ColumnController = new ColumnController
                {
                    new Column (Translator.GetString ("Location"), "Name", 1, "Name")
                };

            scwLocations.Add (gridLocations);
            gridLocations.Show ();

            allLocations = Location.GetAll ();

            bool recreated = false;
            bool retry;
            do {
                retry = false;
                allStartNumbers = OperationNumberingInfo.Get ();
                if (allStartNumbers.Length == 0) {
                    OperationNumberingInfo.Create ();
                    recreated = true;
                    allStartNumbers = OperationNumberingInfo.Get ();
                }
                allStartNumbersPerLocation.Clear ();

                foreach (Location location in allLocations) {
                    Location l = location;
                    List<OperationNumberingInfo> operations = allStartNumbers.Where (o => o.LocationId == l.Id).ToList ();
                    if (operations.Count == 0 && !recreated) {
                        OperationNumberingInfo.Create ();
                        retry = true;
                        recreated = true;
                        break;
                    }

                    foreach (var info in operations)
                        info.UsageDescription = Translator.GetString ("Calculating...");

                    allStartNumbersPerLocation.Add (new KeyValuePair<long, BindingListModel<OperationNumberingInfo>> (location.Id,
                        new BindingListModel<OperationNumberingInfo> (operations)));
                }
            } while (retry);

            DataHelper.FireAndForget (() =>
                {
                    numbersUsagePerLocation = OperationNumbersUsage.Get ();
                    numbersUsageStarts = OperationNumbersUsage.GetUsagesStarts ();
                    Timeout.Add (0, () =>
                        {
                            try {
                                UpdateDocumentsUsage ();
                            } catch (ArgumentOutOfRangeException) { }
                            return false;
                        });
                });

            allStartNumbersPerLocation = allStartNumbersPerLocation.OrderBy (p => p.Value.Min (o => o.StartNumber)).ToList ();
            // Use a model with natural order by the operation number intervals
            gridLocations.Model = new BindingListModel<Location> (allStartNumbersPerLocation.Select (d => allLocations.Find (p => p.Id == d.Key)));
            lblCurrentLocationsValue.SetText (allLocations.Count.ToString ("N", formatBigNumber));

            if (gridLocations.Model.Count <= 0)
                return;

            gridLocations.FocusRow (0);
            gridLocations.Selection.Select (0);
            gridLocations.ScrollToV (0);
        }

        private void InitializeDocumentNumbersGrid ()
        {
            ColumnController columnController = new ColumnController ();

            CellTextLookup<OperationType> cellOperationType = new CellTextLookup<OperationType> ("OperationType")
                {
                    Lookup = Enum.GetValues (typeof (OperationType))
                        .Cast<OperationType> ()
                        .Where (operationType => operationType > 0)
                        .ToDictionary (k => k, Translator.GetOperationTypeGlobalName)
                };
            columnController.Add (new Column (Translator.GetString ("Operation"), cellOperationType, 1));

            CellTextNumber cellNum = new CellTextNumber ("StartNumber")
                {
                    IsEditable = true,
                    FixedDigits = BusinessDomain.AppConfiguration.DocumentNumberLength
                };
            Column colStart = new Column (Translator.GetString ("Start number"), cellNum, 0.1) { MinWidth = 110 };
            colStart.ButtonPressEvent += ColumnDocumentNumber_ButtonPressEvent;
            colStart.KeyPressEvent += ColumnDocumentNumber_KeyPressEvent;
            columnController.Add (colStart);

            columnController.Add (new Column (Translator.GetString ("Used numbers"), new CellProgress ("UsageDescription", "Usage"), 0.1) { MinWidth = 180 });

            gridDocumentNumbers = new ListView
                {
                    Name = "gridDocumentNumbers",
                    ColumnController = columnController,
                    WidthRequest = 400,
                    AllowMultipleSelect = false,
                    CellsFucusable = true,
                    RulesHint = true
                };

            scwDocumentNumbers.Add (gridDocumentNumbers);
            gridDocumentNumbers.Show ();
        }

        private void ColumnDocumentNumber_ButtonPressEvent (object sender, CellButtonPressEventArgs args)
        {
            if (args.EventButton.Type == EventType.TwoButtonPress)
                EditGridCell (args);
        }

        private void ColumnDocumentNumber_KeyPressEvent (object sender, CellKeyPressEventArgs args)
        {
            if (args.Entry == null)
                return;

            long value;
            switch (args.GdkKey) {
                case Key.Return:
                case Key.KP_Enter:
                case Key.Tab:
                    if (args.Entry.CursorPosition != args.Entry.Text.Length && args.GdkKey == Key.Right)
                        return;
                    if (long.TryParse (args.Entry.Text.Trim (), out value))
                        gridDocumentNumbers.EndCellEdit (value);
                    else
                        gridDocumentNumbers.CancelCellEdit ();
                    EditGridCell (new CellEventArgs (args.Cell.Column, args.Cell.Row + 1));
                    break;

                case Key.ISO_Left_Tab:
                    if (args.Entry.CursorPosition != 0 && args.GdkKey == Key.Left)
                        return;

                    if (long.TryParse (args.Entry.Text.Trim (), out value))
                        gridDocumentNumbers.EndCellEdit (value);
                    else
                        gridDocumentNumbers.CancelCellEdit ();
                    EditGridCell (new CellEventArgs (args.Cell.Column, args.Cell.Row - 1));
                    break;

                case Key.Escape:
                    gridDocumentNumbers.CancelCellEdit ();
                    break;
            }
        }

        private void EditGridCell (CellEventArgs args)
        {
            if (gridDocumentNumbers.EditedCell != args.Cell &&
                args.Cell.Row >= 0 && args.Cell.Row < gridDocumentNumbers.Model.Count) {
                gridDocumentNumbers.BeginCellEdit (new CellEventArgs (args.Cell.Column, args.Cell.Row));
            }
        }

        private void UpdateDocumentNumbers ()
        {
            for (int i = 0; i < allStartNumbersPerLocation.Count; i++)
                foreach (var operation in allStartNumbersPerLocation [i].Value)
                    operation.StartNumber = (long) (spbRangeStart.Value + i * spbRangeSize.Value);

            long maxId = Operation.GetMaxId ();
            long maxLocations = (long) ((maxId - spbRangeStart.Value) / spbRangeSize.Value);

            lblMaxLocationsValue.SetText (maxLocations.ToString ("N", formatBigNumber));

            try {
                if (UpdateDocumentsUsage (maxId))
                    return;

                IListModel model = gridDocumentNumbers.Model;
                gridDocumentNumbers.Model = null;
                gridDocumentNumbers.Model = model;
            } catch (ArgumentOutOfRangeException) {
            }
        }

        private bool UpdateDocumentsUsage (long? maxId = null)
        {
            if (numbersUsagePerLocation == null ||
                numbersUsageStarts == null)
                return false;

            if (maxId == null)
                maxId = Operation.GetMaxId ();

            hasEmptyRange = false;

            for (int i = 0; i < allStartNumbersPerLocation.Count; i++)
                foreach (OperationNumberingInfo numberingInfo in allStartNumbersPerLocation [i].Value) {
                    long endNumber = maxId.Value;
                    long endRange = endNumber;
                    if (i + 1 < allStartNumbersPerLocation.Count) {
                        OperationNumberingInfo nextInfo = allStartNumbersPerLocation [i + 1].Value.FirstOrDefault (n => n.OperationType == numberingInfo.OperationType);
                        if (nextInfo != null) {
                            endRange = nextInfo.StartNumber - 1;
                            if (nextInfo.StartNumber > 0)
                                endNumber = nextInfo.StartNumber - 1;
                        }
                    }

                    long usedNumbers = 0;
                    long startNumber = numberingInfo.StartNumber + 1;
                    OperationNumbersUsage usage = numbersUsagePerLocation.FirstOrDefault (n => numberingInfo.LocationId == n.LocationId && numberingInfo.OperationType == n.OperationType);
                    if (usage != null) {
                        usedNumbers = usage.UsedNumbers;
                        startNumber = Math.Max (startNumber, usage.LastUsedNumber);
                    }

                    ObjectsContainer<OperationType, long> firtNextNumber = numbersUsageStarts.FirstOrDefault (n => numberingInfo.StartNumber < n.Value2 && numberingInfo.OperationType == n.Value1);
                    if (firtNextNumber != null)
                        endNumber = Math.Min (endNumber, firtNextNumber.Value2 - 1);

                    double numbersLeft = Math.Max (endNumber - startNumber, 0);
                    if (numbersLeft < OperationNumberingInfo.MINIMAL_NUMBERS_PER_LOCATION)
                        hasEmptyRange = true;

                    long rangeSize = Math.Max (endRange - numberingInfo.StartNumber, 0);
                    if (rangeSize == 0) {
                        MessageError.ShowDialog (string.Format (Translator.GetString ("The selected numbers parameters are not valid! Please select ranges which will generate document numbers lower than {0}."), maxId.Value.ToString ("N", formatBigNumber)));
                        throw new ArgumentOutOfRangeException ();
                    }

                    numberingInfo.Usage = (rangeSize - numbersLeft) / rangeSize;
                    numberingInfo.UsageDescription = string.Format (Translator.GetString ("{0} used, {1} left"),
                        usedNumbers.ToString ("N", formatBigNumber),
                        numbersLeft.ToString ("N", formatBigNumber));
                }

            frmMessage.Visible = hasEmptyRange;

            IListModel model = gridDocumentNumbers.Model;
            gridDocumentNumbers.Model = null;
            gridDocumentNumbers.Model = model;
            return true;
        }

        private bool Validate ()
        {
            if (hasEmptyRange) {
                MessageError.ShowDialog (emptyRangeMessage);
                return false;
            }

            var maxId = Operation.GetMaxId ();
            bool userWarned = false;
            for (int i = allStartNumbersPerLocation.Count - 1; i >= 0; i--) {
                foreach (OperationNumberingInfo numberingInfo in allStartNumbersPerLocation [i].Value) {
                    long endRange = maxId;
                    OperationNumberingInfo nextInfo = null;
                    if (i + 1 < allStartNumbersPerLocation.Count) {
                        nextInfo = allStartNumbersPerLocation [i + 1].Value.FirstOrDefault (n => n.OperationType == numberingInfo.OperationType);
                        if (nextInfo != null)
                            endRange = nextInfo.StartNumber - 2;
                    }

                    long rangeSize = Math.Max (endRange - numberingInfo.StartNumber, 0);
                    if (rangeSize < OperationNumberingInfo.MINIMAL_NUMBERS_PER_LOCATION) {
                        MessageError.ShowDialog (GetShortRangeWarning (numberingInfo, nextInfo, true, maxId), ErrorSeverity.Error);
                        SelectDocumentType (numberingInfo);
                        return false;
                    }

                    if (userWarned || rangeSize >= OperationNumberingInfo.RECOMMENDED_NUMBERS_PER_LOCATION - 2)
                        continue;

                    if (Message.ShowDialog (Translator.GetString ("Document Numbers per Location"), string.Empty,
                        string.Format (GetShortRangeWarning (numberingInfo, nextInfo, false, maxId), OperationNumberingInfo.RECOMMENDED_NUMBERS_PER_LOCATION), "Icons.Warning32.png",
                        MessageButtons.YesNo) == ResponseType.No) {
                        SelectDocumentType (numberingInfo);
                        return false;
                    }
                    userWarned = true;
                }
            }
            return true;
        }

        private string GetShortRangeWarning (OperationNumberingInfo operation, OperationNumberingInfo nextOperation, bool minimal, long maxId)
        {
            BindingListModel<Location> locations = (BindingListModel<Location>) gridLocations.Model;
            string currentLocation = locations.Find (l => l.Id == operation.LocationId).Name;
            string nextLocation = string.Empty;

            string message;
            if (nextOperation != null) {
                if (minimal)
                    message = Translator.GetString ("The numbers between the start numbers for locations \"{0}\" and \"{1}\" " +
                        "for operations of type \"{2}\" are less than the minimal value of {3}.");
                else
                    message = Translator.GetString ("The numbers between the start numbers for locations \"{0}\" and \"{1}\" (possibly other ranges too) " +
                        "for operations of type \"{2}\" are less than the recommended value of {3}. " +
                        "Are you sure you want to continue?");

                nextLocation = locations.Find (l => l.Id == nextOperation.LocationId).Name;
            } else {
                if (minimal)
                    message = Translator.GetString ("The numbers between the start number for location \"{0}\" and the maximal operation number {4} " +
                        "for operations of type \"{2}\" are less than the minimal value of {3}.");
                else
                    message = Translator.GetString ("The numbers between the start number for location \"{0}\" and the maximal operation number {4} (possibly other ranges too) " +
                        "for operations of type \"{2}\" are less than the recommended value of {3}. " +
                        "Are you sure you want to continue?");
            }

            string operationType = Translator.GetOperationTypeGlobalName (operation.OperationType);
            return string.Format (message,
                currentLocation,
                nextLocation,
                operationType,
                minimal ? OperationNumberingInfo.MINIMAL_NUMBERS_PER_LOCATION : OperationNumberingInfo.RECOMMENDED_NUMBERS_PER_LOCATION,
                maxId.ToString ("N", formatBigNumber));
        }

        private void SelectDocumentType (OperationNumberingInfo operation)
        {
            SelectLocation (operation.LocationId);

            BindingListModel<OperationNumberingInfo> modelNumbers = (BindingListModel<OperationNumberingInfo>) gridDocumentNumbers.Model;
            int index = modelNumbers.FindIndex (o => o.OperationType == operation.OperationType);
            gridDocumentNumbers.Selection.Clear ();
            gridDocumentNumbers.Selection.Select (index);
            gridDocumentNumbers.ScrollToV (index);
        }

        private void SelectLocation (long locationId)
        {
            IListModel<Location> locationsModel = (IListModel<Location>) gridLocations.Model;
            int index = locationsModel.FindIndex (l => l.Id == locationId);
            gridLocations.Selection.Clear ();
            gridLocations.Selection.Select (index);
            gridLocations.ScrollToV (index);
        }

        #region Event handling

        [UsedImplicitly]
        protected void EditAllRanges_Toggled (object sender, EventArgs e)
        {
            lblRangeStart.Sensitive = chkEditAllRanges.Active;
            spbRangeStart.Sensitive = chkEditAllRanges.Active;
            lblRangeSize.Sensitive = chkEditAllRanges.Active;
            spbRangeSize.Sensitive = chkEditAllRanges.Active;

            if (chkEditAllRanges.Active)
                UpdateDocumentNumbers ();

            lblMaxLocations.Visible = chkEditAllRanges.Active;
            lblMaxLocationsValue.Visible = chkEditAllRanges.Active;
        }

        [UsedImplicitly]
        protected void Range_Focus (object sender, EventArgs e)
        {
            if (!gridDocumentNumbers.EditedCell.IsValid)
                return;

            gridDocumentNumbers.CancelCellEdit ();
            ((Widget) sender).GrabFocus ();
        }

        [UsedImplicitly]
        protected void RangeStart_ValueChanged (object sender, EventArgs e)
        {
            if (loading)
                return;

            UpdateDocumentNumbers ();
        }

        [UsedImplicitly]
        protected void RangeSize_ValueChanged (object sender, EventArgs e)
        {
            if (loading)
                return;

            UpdateDocumentNumbers ();
        }

        private void Selection_Changed (object sender, EventArgs e)
        {
            if (gridLocations.Selection.Count == 0)
                return;

            gridDocumentNumbers.CancelCellEdit ();
            Location location = (Location) gridLocations.Model [gridLocations.Selection [0]];
            gridDocumentNumbers.Model = allStartNumbersPerLocation.Find (p => p.Key == location.Id).Value;
        }

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (!Validate ())
                return;

            OperationNumberingInfo.Update (allStartNumbers);
            dlgEditDocumentNumbersPerLocation.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditDocumentNumbersPerLocation.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
