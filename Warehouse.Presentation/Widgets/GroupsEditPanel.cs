//
// GroupsEditPanel.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/23/2008
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
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Presentation.Dialogs;

namespace Warehouse.Presentation.Widgets
{
    public abstract class GroupsEditPanel<T> : HBox where T : GroupBase<T>, new ( )
    {
        private readonly GroupsPanel<T> gPanel;
        private readonly VBox buttonsBox = new VBox ();
        private readonly Button btnNew;
        private readonly Button btnEdit;
        private readonly Button btnDelete;
        private T [] allGroups;

        public GroupsPanel<T> GroupsPanel
        {
            get { return gPanel; }
        }

        protected GroupsEditPanel (bool multiSelect = false)
        {
            gPanel = new GroupsPanel<T> (multiSelect);
            Viewport vPort = new Viewport { ShadowType = ShadowType.EtchedIn };
            vPort.Add (gPanel);
            PackStart (vPort, true, true, 0);
            PackStart (buttonsBox, false, true, 4);

            buttonsBox.Spacing = 2;
            btnNew = CreateButton (Translator.GetString ("New", "Group"), "Icons.New24.png");
            btnEdit = CreateButton (Translator.GetString ("Edit"), "Icons.Edit24.png");
            btnDelete = CreateButton (Translator.GetString ("Delete"), "Icons.Delete24.png");

            btnNew.Clicked += btnNew_Clicked;
            btnEdit.Clicked += btnEdit_Clicked;
            btnDelete.Clicked += btnDelete_Clicked;

            buttonsBox.PackStart (btnNew, false, false, 0);
            buttonsBox.PackStart (btnEdit, false, false, 0);
            buttonsBox.PackStart (btnDelete, false, false, 0);

            vPort.ShowAll ();
            buttonsBox.ShowAll ();

            ReloadGroups ();
        }

        public void ReloadGroups ()
        {
            allGroups = GetAllGroups ();

            gPanel.LoadGroups (allGroups);
            gPanel.GroupsTree.GrabFocus ();
        }

        protected abstract T [] GetAllGroups ();

        protected virtual T GetAllGroupsRoot (T [] groups)
        {
            return null;
        }

        public void btnNew_Clicked (object sender, EventArgs e)
        {
            T group = CreateNewGroup ();
            group.Parent = gPanel.GetSelectedGroup ();

            using (EditNewGroup<T> dialog = new EditNewGroup<T> (group, allGroups, GetAllGroupsRoot (allGroups))) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                group = dialog.GetGroup ();
            }
            group.CommitChanges ();
            ReloadGroups ();
            gPanel.SelectGroupId (group.Id);
            OnGroupCreated ();
        }

        protected abstract T CreateNewGroup ();

        protected virtual void OnGroupCreated ()
        {
        }

        public void btnEdit_Clicked (object sender, EventArgs e)
        {
            T group = gPanel.GetSelectedGroup ();
            using (EditNewGroup<T> dialog = new EditNewGroup<T> (group, allGroups)) {
                if (dialog.Run () != ResponseType.Ok)
                    return;

                group = dialog.GetGroup ();
            }
            group.CommitChanges ();
            ReloadGroups ();
            gPanel.SelectGroupId (group.Id);
        }

        public void btnDelete_Clicked (object sender, EventArgs e)
        {
            T group = gPanel.GetSelectedGroup ();
            DeletePermission permission = GetDeletePermission (group);

            switch (permission) {
                case DeletePermission.InUse:
                    MessageError.ShowDialog (
                        Translator.GetString ("This group cannot be deleted, because it is not empty. Please, delete or move to another group the containing items in order to delete this group!"),
                        "Icons.Group16.png");
                    return;

                case DeletePermission.Reserved:
                case DeletePermission.No:
                    MessageError.ShowDialog (
                        string.Format (Translator.GetString ("Cannot delete group \"{0}\"!"), group.Name),
                        "Icons.Group16.png");
                    return;
            }

            using (MessageOkCancel dialog = new MessageOkCancel (
                Translator.GetString ("Delete group"), "Icons.Group16.png",
                string.Format (Translator.GetString ("Do you want to delete group with name \"{0}\"?"), group.Name),
                "Icons.Delete32.png")) {
                if (dialog.Run () != ResponseType.Ok)
                    return;
            }

            DeleteGroup (group);
            ReloadGroups ();
            if (group.Parent != null)
                gPanel.SelectGroupId (group.Parent.Id);
            else
                gPanel.SelectGroupId (-1);
            OnGroupDeleted ();
        }

        protected abstract DeletePermission GetDeletePermission (T group);

        protected abstract void DeleteGroup (T group);

        protected virtual void OnGroupDeleted ()
        {
        }

        private static Button CreateButton (string text, string iconName)
        {
            Button btn = new Button ();
            Alignment alg = new Alignment (0.5f, 0.5f, 0, 0);
            HBox hbo = new HBox { WidthRequest = 100 };

            Alignment iconAlg = new Alignment (0.5f, 0.5f, 0, 0);
            Image icon = FormHelper.LoadImage (iconName);
            iconAlg.Add (icon);
            Label btnText = new Label { Markup = new PangoStyle { Size = PangoStyle.TextSize.Small, Text = text }, Xalign = 1 };

            hbo.PackStart (iconAlg, false, false, 0);
            hbo.PackStart (btnText, true, true, 0);
            alg.Add (hbo);
            btn.Add (alg);

            return btn;
        }

        public T GetSelectedGroup ()
        {
            return gPanel.GetSelectedGroup ();
        }

        public long GetSelectedGroupId ()
        {
            return gPanel.GetSelectedGroupId ();
        }

        public void SelectGroupId (long id)
        {
            gPanel.SelectGroupId (id);
        }

        public void HideButtons ()
        {
            buttonsBox.Visible = false;
        }
    }
}
