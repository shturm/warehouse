//
// ChooseEditGroup.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/16/2006
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
using Warehouse.Business.Entities;
using Warehouse.Presentation.Widgets;

namespace Warehouse.Presentation.Dialogs
{
    public abstract class ChooseEditGroup<T> : ChooseEdit<GroupBase<T>, T> where T : GroupBase<T>, new ( )
    {
        private GroupsEditPanel<T> groupsEditPanel;

        public override string [] SelectedItemsText
        {
            get
            {
                T selectedGroup = GetSelectedGroup ();
                return new [] { selectedGroup != null ? selectedGroup.Name : string.Empty };
            }
        }

        protected ChooseEditGroup (long? groupId = null)
        {
            Initialize ();
            if (groupId.HasValue)
                groupsEditPanel.SelectGroupId (groupId.Value);
        }

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            dlgChooseEdit.Icon = FormHelper.LoadImage ("Icons.Group16.png").Pixbuf;

            dlgChooseEdit.HeightRequest = 310;
            dlgChooseEdit.WidthRequest = 520;

            InitializeFormStrings ();
            InitializeEntries ();
            dlgChooseEdit.Shown += dlgChooseEditGroup_Shown;

            tblFilter.Visible = false;
            btnGroups.Visible = false;
            algGridGroups.Visible = false;
            lblRows.Visible = lblRowsValue.Visible = false;
        }

        protected override string KeyForKeyboardBingdings
        {
            get { return "editGroups"; }
        }

        private void InitializeEntries ()
        {
            groupsEditPanel = CreateGroupsEditPanel ();
            groupsEditPanel.HideButtons ();
            groupsEditPanel.GroupsPanel.GroupsTree.RowActivated += GroupsTree_RowActivated;
            algGrid.Add (groupsEditPanel);
            groupsEditPanel.Show ();
        }

        protected virtual GroupsEditPanel<T> CreateGroupsEditPanel ()
        {
            return null;
        }

        private void GroupsTree_RowActivated (object o, Gtk.RowActivatedArgs args)
        {
            btnOK_Clicked (btnOK, EventArgs.Empty);
        }

        private void dlgChooseEditGroup_Shown (object sender, EventArgs e)
        {
            groupsEditPanel.GrabFocus ();
        }

        public T GetSelectedGroup ()
        {
            return groupsEditPanel.GetSelectedGroup();
        }

        #region Event handling

        protected override void btnNew_Clicked (object o, EventArgs args)
        {
            groupsEditPanel.btnNew_Clicked (o, args);
        }

        protected override void btnEdit_Clicked (object o, EventArgs args)
        {
            groupsEditPanel.btnEdit_Clicked (o, args);
        }

        protected override void btnDelete_Clicked (object o, EventArgs args)
        {
            groupsEditPanel.btnDelete_Clicked (o, args);
        }

        protected override bool DeleteEntity (GroupBase<T> entity)
        {
            return false;
        }

        protected override bool AskDeleteSingleEntity (GroupBase<T> entity)
        {
            throw new NotImplementedException ();
        }

        protected override bool AskDeleteMultipleEntities (int count)
        {
            throw new NotImplementedException ();
        }

        protected override void ReinitializeGrid (bool reinitGroups, long? selectedGroup)
        {
            if (!reinitializing) {
                reinitializing = true;
                groupsEditPanel.ReloadGroups ();
                reinitializing = false;
            }
        }

        #endregion

        protected override void GetAllEntities ()
        {
            
        }
    }
}
