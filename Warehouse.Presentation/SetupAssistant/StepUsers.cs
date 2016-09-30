//
// StepUsers.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   08.16.2011
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
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Presentation.Dialogs;

namespace Warehouse.Presentation.SetupAssistant
{
    [Extension ("/Warehouse/Presentation/SetupAssistant/Step")]
    public class StepUsers : StepBase
    {
        private readonly Dictionary<int, string> userLevelLookup = new Dictionary<int, string> ();
        private Table tbl;

        #region Overrides of StepBase

        public override string Title
        {
            get { return Translator.GetString ("Users"); }
        }

        public override int Ordinal
        {
            get { return 50; }
        }

        public override AssistType Group
        {
            get { return AssistType.DatabaseSetup; }
        }

        public StepUsers ()
        {
            foreach (KeyValuePair<int, string> pair in User.GetAllAccessLevels ())
                userLevelLookup.Add (pair.Key, pair.Value);
        }

        protected override void CreateBody ()
        {
            CreateBody (Translator.GetString ("Choose your users"));

            WrapLabel footer = new WrapLabel
                {
                    Markup = string.Format (Translator.GetString (
                        "Users are the people logging into this system. By adding separate logins you can track the activity of the individuals and restrict their access. To create your users quickly use the form bellow. To edit your users later go to:{0}{1}{0}{0}To edit the user permissions that each user has go to:{0}{2}"),
                        Environment.NewLine,
                        new PangoStyle { Italic = true, Bold = true, Text = Translator.GetString ("Edit->Users...") },
                        new PangoStyle { Italic = true, Bold = true, Text = Translator.GetString ("Edit->Administration->Permissions...") })
                };
            footer.Show ();
            vboBody.PackStart (footer, false, true, 0);

            tbl = new Table (1, 1, false) { RowSpacing = 4, ColumnSpacing = 4 };
            Alignment alignment = new Alignment (.5f, .5f, 1f, 1f) { TopPadding = 20 };
            alignment.Add (tbl);
            alignment.ShowAll ();
            vboBody.PackStart (alignment, true, true, 0);

            RefreshUsersTable ();
        }

        #endregion

        private void RefreshUsersTable ()
        {
            for (int i = tbl.Children.Length - 1; i >= 0; i--) {
                Widget child = tbl.Children [i];
                tbl.Remove (child);
                child.Destroy ();
            }


            tbl.Attach (GetAddWidget (), 0, 2, 0, 1, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            uint index = 0;
            foreach (User user in User.GetAll ()) {
                if (user.Id == User.DefaultId)
                    continue;

                tbl.Attach (GetUserWidget (user), index % 2, (index % 2) + 1, (index / 2) + 1, (index / 2) + 2, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                index++;
            }

            if (index == 0) {
                WrapLabel wlbNoUsers = new WrapLabel
                    {
                        Markup = Translator.GetString ("Currently no users are added, which means that no user or password will be required at startup!")
                    };
                tbl.Attach (wlbNoUsers, 0, 2, 1, 2, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 4);
            }

            tbl.ShowAll ();
        }

        private Widget GetAddWidget ()
        {
            Table tblButton = new Table (1, 1, false) { RowSpacing = 2, ColumnSpacing = 4 };

            Gtk.Image imgAdd = FormHelper.LoadImage ("Icons.Add24.png");
            tblButton.Attach (imgAdd, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, 2, 0);

            Label lblName = new Label { Markup = new PangoStyle { Text = Translator.GetString ("Add"), Bold = true, Size = PangoStyle.TextSize.Large }, Xalign = 0.5f };
            tblButton.Attach (lblName, 1, 2, 0, 1, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, 0, 0);

            Button btnAdd = new Button { tblButton };
            btnAdd.Clicked += btnAdd_Clicked;
            return btnAdd;
        }

        private Widget GetUserWidget (User user)
        {
            Table tblButton = new Table (1, 1, false) { RowSpacing = 2, ColumnSpacing = 4 };

            Gtk.Image imgUser = FormHelper.LoadImage ("Icons.User24.png");
            tblButton.Attach (imgUser, 0, 1, 0, 2, AttachOptions.Fill, AttachOptions.Fill, 2, 0);

            Label lblName = new Label { Markup = new PangoStyle { Text = user.Name, Bold = true, Size = PangoStyle.TextSize.Large }, Xalign = 0f };
            tblButton.Attach (lblName, 1, 2, 0, 1, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 2, 0);

            Label lblLevel = new Label { Markup = new PangoStyle { Text = userLevelLookup [(int) user.UserLevel] }, Xalign = 0f };
            tblButton.Attach (lblLevel, 1, 2, 1, 2, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 2, 0);

            Button btnEdit = new Button { tblButton };
            btnEdit.Data ["User"] = user;
            btnEdit.Clicked += btnEdit_Clicked;

            Alignment algDelete = new Alignment (0.5f, 0.5f, 1f, 1f) { LeftPadding = 2, RightPadding = 2 };
            algDelete.Add (FormHelper.LoadImage ("Icons.Delete24.png"));

            Button btnRemove = new Button { algDelete };
            btnRemove.Data ["User"] = user;
            btnRemove.Clicked += btnRemove_Clicked;

            HBox hbo = new HBox ();
            hbo.PackStart (btnEdit, true, true, 0);
            hbo.PackStart (btnRemove, false, true, 0);

            return hbo;
        }

        private void btnEdit_Clicked (object sender, EventArgs e)
        {
            Button btn = (Button) sender;
            User user = (User) btn.Data ["User"];

            using (EditNewUser choose = new EditNewUser (user, UsersGroup.DefaultGroupId)) {
                if (choose.Run () != ResponseType.Ok)
                    return;

                using (new DbMasterScope (BusinessDomain.DataAccessProvider))
                    choose.GetUser ().CommitChanges ();
            }

            RefreshUsersTable ();
        }

        private void btnRemove_Clicked (object sender, EventArgs e)
        {
            Button btn = (Button) sender;
            User user = (User) btn.Data ["User"];

            switch (User.RequestDelete (user.Id)) {
                case DeletePermission.InUse:
                    MessageError.ShowDialog (
                        Translator.GetString ("This user cannot be deleted, because it is used in operations. Please, delete the corresponding operations in order to delete this user!"),
                        "Icons.User16.png");
                    return;

                case DeletePermission.Reserved:
                case DeletePermission.No:
                    MessageError.ShowDialog (string.Format (Translator.GetString ("Cannot delete user \"{0}\"!"), user.Name),
                        "Icons.User16.png");
                    return;

                default:
                    if (user.UserLevel == UserAccessLevel.Owner) {
                        bool foundOwner = false;
                        bool foundUser = false;
                        foreach (User u in User.GetAll ()) {
                            if (u.Id == User.DefaultId || u.Id == user.Id)
                                continue;

                            if (u.UserLevel == UserAccessLevel.Owner) 
                                foundOwner = true;
                            else
                            foundUser = true;
                        }

                        if (!foundOwner && foundUser) {
                            MessageError.ShowDialog (Translator.GetString ("The last owner cannot be deleted before the rest of the users!"),
                                "Icons.User16.png");
                            return;
                        }
                    }
                    break;
            }

            User.Delete (user.Id);
            RefreshUsersTable ();
        }

        private void btnAdd_Clicked (object sender, EventArgs e)
        {
            using (EditNewUser choose = new EditNewUser (null, UsersGroup.DefaultGroupId)) {
                if (choose.Run () != ResponseType.Ok)
                    return;

                using (new DbMasterScope (BusinessDomain.DataAccessProvider))
                    choose.GetUser ().CommitChanges ();
            }

            RefreshUsersTable ();
        }
    }
}
