//
// ChooseWeekDays.cs
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
using System.Globalization;
using Glade;
using Gtk;
using Warehouse.Business;
using Warehouse.Component;

namespace Warehouse.Presentation.Dialogs
{
    public class ChooseWeekDays : DialogBase
    {
        #region Glade Widgets

        [Widget]
        protected Dialog dlgChooseWeekDays;
        [Widget]
        protected TreeView treeViewWeekDays;
        [Widget]
        protected Button btnOK;
        [Widget]
        protected Button btnCancel;

        #endregion

        private readonly List<DayOfWeek> selectedWeekDays;

        public override Dialog DialogControl
        {
            get { return dlgChooseWeekDays; }
        }

        public List<DayOfWeek> SelectedWeekDays
        {
            get { return selectedWeekDays; }
        }

        public ChooseWeekDays ()
        {
            selectedWeekDays = new List<DayOfWeek> (DateTimeFormatInfo.CurrentInfo.DayNames.Length);

            Initialize ();
        }

        public ChooseWeekDays (IEnumerable<DayOfWeek> weekDays)
        {
            selectedWeekDays = new List<DayOfWeek> (weekDays);

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.ChooseWeekDays.glade", "dlgChooseWeekDays");
            form.Autoconnect (this);

            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            btnOK.SetChildLabelText (Translator.GetString ("OK"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));

            LoadWeekDays ();

            base.InitializeForm ();
            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgChooseWeekDays.Title = Translator.GetString ("Choose Days of Week");
        }

        private void LoadWeekDays ()
        {
            ListStore listStore = new ListStore (typeof (bool), typeof (string), typeof (DayOfWeek));
            List<DayOfWeek> weekDays = new List<DayOfWeek> ((IEnumerable<DayOfWeek>) Enum.GetValues (typeof (DayOfWeek)));
            // move Sunday to the end of the week
            weekDays.Add (weekDays [0]);
            weekDays.RemoveAt (0);
            foreach (DayOfWeek dayOfWeek in weekDays)
                listStore.AppendValues (selectedWeekDays.Contains (dayOfWeek), 
                    DateTimeFormatInfo.CurrentInfo.DayNames [(int) dayOfWeek], dayOfWeek);

            CellRendererToggle cellRendererToggle = new CellRendererToggle { Activatable = true };
            cellRendererToggle.Toggled += (sender, e) =>
                {
                    TreeIter row;
                    listStore.GetIter (out row, new TreePath (e.Path));
                    bool value = !(bool) listStore.GetValue (row, 0);
                    DayOfWeek dayOfWeek = (DayOfWeek) listStore.GetValue (row, 2);
                    if (value)
                        selectedWeekDays.Add (dayOfWeek);
                    else
                        selectedWeekDays.Remove (dayOfWeek);
                    listStore.SetValue (row, 0, value);
                };

            treeViewWeekDays.AppendColumn (string.Empty, cellRendererToggle, "active", 0);
            treeViewWeekDays.AppendColumn (string.Empty, new CellRendererText (), "text", 1);
            treeViewWeekDays.AppendColumn (string.Empty, new CellRendererText (), "text", 2).Visible = false;

            treeViewWeekDays.Model = listStore;
        }

        #region Event handling

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (selectedWeekDays.Count == 0) {
                MessageError.ShowDialog (Translator.GetString ("Please select at least one day of the week."), ErrorSeverity.Error);
                return;
            }
            
            dlgChooseWeekDays.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgChooseWeekDays.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
