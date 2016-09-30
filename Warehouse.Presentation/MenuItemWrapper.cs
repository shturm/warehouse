//
// MenuItemWrapper.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   11/30/2007
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
using Warehouse.Business.Entities;
using Warehouse.Component;

namespace Warehouse.Presentation
{
    public class MenuItemWrapper
    {
        #region Private fields

        private readonly MenuItem item;
        private Func<string> translate;
        private readonly Dictionary<long, UserRestriction> restrictions = new Dictionary<long, UserRestriction> ();
        private MenuItemCollection parent;
        private MenuItemCollection subItems = null;

        #endregion

        #region Public properties

        public MenuItemCollection SubItems
        {
            get
            {
                if (subItems == null) {
                    //if (item.Submenu == null)
                    //    item.Submenu = new Menu ();

					subItems = new MenuItemCollection ((Menu) item.Submenu, item, this);
                }

                return subItems;
            }
        }

        public MenuItem Item
        {
            get { return item; }
        }

        public string Name
        {
            get { return item.Name; }
            set { item.Name = value; }
        }

        public string Text
        {
            get
            {
                if (item is SeparatorMenuItem)
                    return "--";

                return ((Label) item.Child).Text;
            }
            set
            {
                if (!(item is SeparatorMenuItem))
                    ((Label) item.Child).SetText (value);
            }
        }

        public Image Image
        {
            get
            {
                ImageMenuItem imgItem = item as ImageMenuItem;
                if (imgItem == null)
                    return null;

                return imgItem.Image as Image;
            }
            set
            {
                ImageMenuItem imgItem = item as ImageMenuItem;
                if (imgItem != null)
                    imgItem.Image = value;
            }
        }

        public bool IsSeparator
        {
            get { return item is SeparatorMenuItem; }
        }

        public bool Active
        {
            get
            {
                CheckMenuItem check = item as CheckMenuItem;

                if (check != null)
                    return check.Active;
                else
                    return false;
            }
            set
            {
                CheckMenuItem check = item as CheckMenuItem;

                if (check != null)
                    check.Active = value;
            }
        }

        public bool Sensitive
        {
            get { return item.Sensitive; }
            set { item.Sensitive = value; }
        }

        public bool Visible
        {
            get { return item.Visible; }
            set { item.Visible = value; }
        }

        public MenuItemCollection Parent
        {
            get { return parent; }
            internal set { parent = value; }
        }

        public Func<string> Translate
        {
            get { return translate; }
            set { translate = value; }
        }

        public event EventHandler Activated
        {
            add { item.Activated += value; }
            remove { item.Activated -= value; }
        }

        #endregion

        public MenuItemWrapper (MenuItem item, Func<string> translate, MenuItemCollection parent = null)
        {
            if (item == null)
                throw new ArgumentNullException ("item");

            this.item = item;
            this.translate = translate;
            this.parent = parent;
        }

        public static MenuItemWrapper CreateSeparator ()
        {
            SeparatorMenuItem sep = new SeparatorMenuItem ();

            return new MenuItemWrapper (sep, () => string.Empty);
        }

        #region Restrictions management

        public void ClearRestrictions ()
        {
            restrictions.Clear ();

            SubItems.ClearRestrictions ();
        }

        public void SetRestriction (long userId, UserRestrictionState state)
        {
            SetRestriction (new UserRestriction (userId, Name, state));
        }

        public void SetRestriction (UserRestriction rest)
        {
            if (rest == null)
                throw new ArgumentNullException ("rest");

            if (restrictions.ContainsKey (rest.UserId))
                restrictions [rest.UserId] = rest;
            else
                restrictions.Add (rest.UserId, rest);
        }

        public UserRestrictionState GetRestriction (long userId)
        {
            // Check for restrictions that apply to the specified user
            if (restrictions.ContainsKey (userId))
                return restrictions [userId].State;
            // Check for restrictions that apply to all the users
            if (restrictions.ContainsKey (User.AllId))
                return restrictions [User.AllId].State;

            return UserRestrictionState.Allowed;
        }

        public void ApplyRestriction (long userId)
        {
            UserRestrictionState r = GetRestriction (userId);
            item.Visible = r != UserRestrictionState.Restricted;
            item.Sensitive = r != UserRestrictionState.Disabled;

            SubItems.ApplyRestriction (userId);
        }

        public void FixSeparators ()
        {
            MenuItemCollection parent = Parent;
            if (parent.IndexOf (this) == 0 && parent.Count >= 2 && parent [1].IsSeparator) {
                parent.Menu.Remove (parent [1].Item);
                parent.RemoveAt (1);
            }
            if (parent.IndexOf (this) == parent.Count - 1 && parent.Count >= 2 && parent [parent.Count - 2].IsSeparator) {
                parent.Menu.Remove (parent [parent.Count - 2].Item);
                parent.RemoveAt (parent.Count - 2);
            }
        }

        #endregion

        public override string ToString ()
        {
            return string.Format ("{0} \"{1}\"", item.Name, Text);
        }
    }
}
