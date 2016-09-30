//
// EditNewGroup.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   06/30/2006
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
using System.Linq;
using Glade;
using Gtk;

using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNewGroup<T> : DialogBase where T : GroupBase<T>, new ()
    {
        private T group;
        private readonly IEnumerable<T> allGroups;
        private readonly T allGroupsRoot;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEditNewGroup;
        [Widget]
        private Button btnOK;
        [Widget]
        private Button btnCancel;

        [Widget]
        private Label lblName;
        [Widget]
        private Label lblParentGroup;

        [Widget]
        private Entry txtName;
        [Widget]
        private ComboBox cboParentGroup;

#pragma warning restore 649

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgEditNewGroup; }
        }

        public EditNewGroup (T group, IEnumerable<T> allGroups, T allGroupsRoot = null)
        {
            this.group = group;
            this.allGroups = allGroups;
            this.allGroupsRoot = allGroupsRoot;

            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Dialogs.EditNewGroup.glade", "dlgEditNewGroup");
            form.Autoconnect (this);

            dlgEditNewGroup.Icon = FormHelper.LoadImage ("Icons.Group16.png").Pixbuf;
            btnOK.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnCancel.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();

            InitializeFormStrings ();
            InitializeEntries ();
            dlgEditNewGroup.Shown += dlgEditNewGroup_Shown;
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            if (group != null && group.Id >= 0)
                dlgEditNewGroup.Title = Translator.GetString ("Edit Group");
            else
                dlgEditNewGroup.Title = Translator.GetString ("New Group");

            lblName.SetText (Translator.GetString ("Name:"));
            lblParentGroup.SetText (Translator.GetString ("Parent group:"));

            btnOK.SetChildLabelText (Translator.GetString ("Save"));
            btnCancel.SetChildLabelText (Translator.GetString ("Cancel"));
        }

        protected virtual void InitializeEntries ()
        {
            T parent = null;

            if (group == null) {
                group = new T ();
            } else {
                txtName.Text = group.Name;
                parent = group.Parent;
            }

            cboParentGroup.Load (GetGroupsList (allGroupsRoot, 0), "Key", "Value", parent);
        }

        private KeyValuePair<T, string> [] GetGroupsList (T parentGroup, int depth)
        {
            List<KeyValuePair<T, string>> ret = new List<KeyValuePair<T, string>> ();

            if (depth == 0) {
                if (parentGroup == null)
                    ret.Add (new KeyValuePair<T, string> (null, Translator.GetString ("<none>")));
                else {
                    ret.Add (new KeyValuePair<T, string> (parentGroup, parentGroup.Name));
                    depth = 1;
                }
            }

            string prefix = string.Empty;
            for (int i = 0; i < depth; i++)
                prefix += "  ";

            IEnumerable<T> subGroups;
            if (parentGroup != null)
                subGroups = parentGroup.Children;
            else if (allGroups != null)
                subGroups = allGroups;
            else
                return ret.ToArray ();

            foreach (T subGroup in subGroups) {
                // Do not allow the default group to become a parent
                if (subGroup.Id == GroupBase<T>.DefaultGroupId)
                    continue;

                // Do not allow the same group or any of it's children to become a parent
                if (subGroup.Id == group.Id)
                    continue;

                ret.Add (new KeyValuePair<T, string> (subGroup, prefix + subGroup.Name));
                ret.AddRange (GetGroupsList (subGroup, depth + 1));
            }

            return ret.ToArray ();
        }

        private void dlgEditNewGroup_Shown (object sender, EventArgs e)
        {
            txtName.GrabFocus ();
        }

        public T GetGroup ()
        {
            group.Name = txtName.Text;
            group.Parent = (T) cboParentGroup.GetSelectedValue ();

            return group;
        }

        private bool Validate ()
        {
            string name = txtName.Text.Trim ();

            if (name.Length == 0) {
                MessageError.ShowDialog (Translator.GetString ("Group name cannot be empty!"));
                return false;
            }

            if (allGroups.Any (groupBase => groupBase.Name == name && groupBase.Id != @group.Id)) {
                if (Message.ShowDialog (
                    Translator.GetString ("Warning!"), string.Empty,
                    Translator.GetString ("Group with this name already exists! Do you want to continue?"), "Icons.Warning32.png",
                    MessageButtons.YesNo) != ResponseType.Yes)
                    return false;
            }

            return true;
        }

        #region Event handling

        [UsedImplicitly]
        protected void btnOK_Clicked (object o, EventArgs args)
        {
            if (!Validate ())
                return;

            dlgEditNewGroup.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        protected void btnCancel_Clicked (object o, EventArgs args)
        {
            dlgEditNewGroup.Respond (ResponseType.Cancel);
        }

        #endregion
    }
}
