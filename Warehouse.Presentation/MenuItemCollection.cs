//
// MenuItemCollection.cs
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
using System.Collections;
using System.Collections.Generic;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;

namespace Warehouse.Presentation
{
    public class MenuItemCollection : IList<MenuItemWrapper>
    {
        private MenuShell menu;
        private readonly MenuItem parent;
		private readonly MenuItemWrapper parentWrapper;

        private List<MenuItemWrapper> items;

        public MenuShell Menu
        {
            get { return menu; }
        }

        public MenuItemWrapper this [string name]
        {
            get
            {
                LoadSubItems ();

                foreach (MenuItemWrapper menuItem in items) {
                    if (menuItem.Name == name)
                        return menuItem;
                }

                return null;
            }
        }

		public MenuItemWrapper Parent
		{
			get { return parentWrapper; }
		}

        public bool Sorted { get; set; }

		public MenuItemCollection (MenuShell menu, MenuItem parent = null, MenuItemWrapper parentWrapper = null)
        {
            this.menu = menu;
            this.parent = parent;
			this.parentWrapper = parentWrapper;

            if (menu != null) {
                menu.Shown += menu_Shown;
            }
        }

        private void LoadSubItems ()
        {
            if (items != null)
                return;

            items = new List<MenuItemWrapper> ();

            if (menu == null)
                return;

            foreach (MenuItem menuItem in menu.Children) {
                MenuItem item = menuItem;
                items.Add (new MenuItemWrapper (menuItem, () => ((Label) item.Child).Text, this));
            }
        }

        private void EnsureMenu ()
        {
            if (parent.Submenu != null)
                return;
            if (parent == null)
                return;

            menu = new Menu ();
            parent.Submenu = menu;
        }

        private void menu_Shown (object sender, EventArgs e)
        {
            FixSeparators (false);
        }

        public void FixSeparators (bool recursive)
        {
            LoadSubItems ();

            int lastVisibleIndex = -1;
            int lastSeparatorIndex = -1;
            int i;
            for (i = 0; i < items.Count; i++) {
                if (items [i].IsSeparator) {
                    items [i].Visible = true;

                    if (i == 0 || lastVisibleIndex < 0)
                        items [i].Visible = false;

                    if (lastVisibleIndex <= lastSeparatorIndex)
                        items [i].Visible = false;

                    lastSeparatorIndex = i;
                }

                if (items [i].Visible)
                    lastVisibleIndex = i;
            }

            lastVisibleIndex = -1;
            for (i = items.Count - 1; i >= 0; i--) {
                if (items [i].IsSeparator)
                    if (i == items.Count - 1 || lastVisibleIndex < 0)
                        items [i].Visible = false;

                if (items [i].Visible)
                    lastVisibleIndex = i;
            }

            if (!recursive)
                return;

            foreach (MenuItemWrapper item in items) {
                if (!item.Visible)
                    continue;

                if (item.IsSeparator)
                    continue;

                item.SubItems.FixSeparators (true);
            }
        }

        public MenuItemWrapper FindMenuItem (string itemName)
        {
            return FindMenuItem (itemName, true);
        }

        public MenuItemWrapper FindMenuItem (string itemName, bool exactMatch)
        {
            LoadSubItems ();

            foreach (MenuItemWrapper item in items) {
                if (exactMatch) {
                    if (item.Name == itemName)
                        return item;
                } else {
                    if (item.Name.Contains (itemName))
                        return item;
                }

                MenuItemWrapper menuItem = item.SubItems.FindMenuItem (itemName);
                if (menuItem != null)
                    return menuItem;
            }

            return null;
        }

        public MenuItemWrapper InsertBefore (string searchedName, MenuItem newItem, Func<string> translate, bool insertRestriction = false)
        {
            return InsertBefore (searchedName, new MenuItemWrapper (newItem, translate), insertRestriction);
        }

        public MenuItemWrapper InsertBefore (string searchedName, MenuItemWrapper newItem, bool insertRestriction = false)
        {
            LoadSubItems ();

            for (int i = 0; i < items.Count; i++) {

                if (items [i].Name == searchedName) {
                    InsertAt (newItem, i);
                    if (insertRestriction && !newItem.IsSeparator)
                        BusinessDomain.RestrictionTree.InsertBefore (searchedName, GetRestrictionTreeFromMenuItem (newItem));

                    return newItem;
                }

                MenuItemWrapper ret = items [i].SubItems.InsertBefore (searchedName, newItem, insertRestriction);
                if (ret != null)
                    return ret;
            }

            return null;
        }

        public MenuItemWrapper InsertAfter (string searchedName, MenuItem newItem, Func<string> translate, bool insertRestriction = false)
        {
            return InsertAfter (searchedName, new MenuItemWrapper (newItem, translate), insertRestriction);
        }

        public MenuItemWrapper InsertAfter (string searchedName, MenuItemWrapper newItem, bool insertRestriction = false)
        {
            LoadSubItems ();

            for (int i = 0; i < items.Count; i++) {

                if (items [i].Name == searchedName) {
                    InsertAt (newItem, i + 1);
                    if (insertRestriction && !newItem.IsSeparator)
                        BusinessDomain.RestrictionTree.InsertAfter (searchedName, GetRestrictionTreeFromMenuItem (newItem));

                    return newItem;
                }

                MenuItemWrapper ret = items [i].SubItems.InsertAfter (searchedName, newItem, insertRestriction);
                if (ret != null)
                    return ret;
            }

            return null;
        }

        private static RestrictionNode GetRestrictionTreeFromMenuItem (MenuItemWrapper item)
        {
            RestrictionNode ret = new RestrictionNode (item.Name, item.Translate);
            foreach (MenuItemWrapper subItem in item.SubItems) {
                if (subItem.IsSeparator)
                    continue;

                ret.Children.Add (GetRestrictionTreeFromMenuItem (subItem));
            }

            return ret;
        }

        public void InsertAt (MenuItemWrapper newItem, int position)
        {
            if (newItem == null)
                throw new ArgumentNullException ("newItem");

            newItem.Parent = this;

            LoadSubItems ();

            if (menu != null)
                menu.Insert (newItem.Item, position);

            items.Insert (position, newItem);
        }

        public bool RemoveItem (string searchedName, bool exactMatch = true)
        {
            LoadSubItems ();

            for (int i = 0; i < items.Count; i++) {
                bool found = exactMatch ?
                    items [i].Name == searchedName :
                    items [i].Name.Contains (searchedName);

                if (found) {
                    if (menu != null) {
                        menu.Remove (items [i].Item);
						items [i].Visible = false;
                        items.RemoveAt (i);
                        return true;
                    }
                }

                if (items [i].SubItems.RemoveItem (searchedName, exactMatch))
                    return true;
            }

            return false;
        }

        public void Sort (Comparison<MenuItemWrapper> comparison)
        {
            items.Sort (comparison);
        }

        public void Translate ()
        {
            LoadSubItems ();
            RestrictionNode restrictionTree = BusinessDomain.RestrictionTree;

            foreach (MenuItemWrapper item in items) {
                if (item.IsSeparator)
                    continue;

                RestrictionNode node = restrictionTree.FindNode (item.Name);
                if (node == null)
                    throw new ApplicationException (string.Format ("No restriction node found item name \"{0}\"", item.Name));

                item.Text = node.Value;
                item.SubItems.Translate ();

                if (item.SubItems.Sorted && item.Item.Submenu != null) {
                    item.SubItems.Sort ((m1, m2) => string.Compare (m1.Text, m2.Text));
                    RestrictionNode restrictionNode = BusinessDomain.RestrictionTree.FindNode (item.Name);
                    restrictionNode.Children.Sort ((r1, r2) => string.Compare (r1.Value, r2.Value));
                    Container container = (Container) item.Item.Submenu;
                    List<Widget> children = new List<Widget> (container.Children);
                    foreach (Widget widget in container.Children)
                        container.Remove (widget);
                    children.Sort ((w1, w2) => string.Compare (((Label) ((Bin) w1).Child).Text, (((Label) ((Bin) w2).Child).Text)));
                    foreach (Widget widget in children)
                        container.Add (widget);
                }
            }
        }

        #region Restrictions management

        public void ClearRestrictions ()
        {
            LoadSubItems ();

            foreach (MenuItemWrapper item in items) {
                item.ClearRestrictions ();
            }
        }

        public void ReloadRestrictions ()
        {
            LoadSubItems ();
            RestrictionNode restrictionTree = BusinessDomain.RestrictionTree;

            foreach (MenuItemWrapper item in items) {
                if (item.IsSeparator)
                    continue;

                RestrictionNode node = restrictionTree.FindNode (item.Name);
                if (node == null)
                    continue;

                item.ClearRestrictions ();
                foreach (KeyValuePair<long, UserRestriction> restriction in node.Restrictions)
                    item.SetRestriction (restriction.Value);

                item.SubItems.ReloadRestrictions ();
            }
        }

        public void ApplyRestriction (long userId)
        {
            LoadSubItems ();

            foreach (MenuItemWrapper item in items) {
                item.ApplyRestriction (userId);
            }
        }

        #endregion

        #region Implementation of IList<MenuItemWraper>

        public int IndexOf (MenuItemWrapper item)
        {
            LoadSubItems ();
            return items.IndexOf (item);
        }

        public void Insert (int index, MenuItemWrapper item)
        {
            LoadSubItems ();
            items.Insert (index, item);
        }

        public void RemoveAt (int index)
        {
            LoadSubItems ();
            items.RemoveAt (index);
        }

        public MenuItemWrapper this [int index]
        {
            get
            {
                LoadSubItems ();
                return items [index];
            }
            set
            {
                LoadSubItems ();
                items [index] = value;
            }
        }

        public MenuItemWrapper Add (MenuItem item, Func<string> translate, bool createRestriction = false)
        {
            return Add (new MenuItemWrapper (item, translate), createRestriction);
        }

        public void Add (MenuItemWrapper item)
        {
            Add (item, false);
        }

        public MenuItemWrapper Add (MenuItemWrapper item, bool createRestriction)
        {
            LoadSubItems ();
            EnsureMenu ();

            items.Add (item);
            if (menu != null)
                menu.Append (item.Item);

            if (!createRestriction)
                return item;

            RestrictionNode node = BusinessDomain.RestrictionTree.FindNode (parent.Name);
            node.Children.Add (new RestrictionNode (item.Name, item.Translate));
            return item;
        }

        public void Clear ()
        {
            LoadSubItems ();
            items.Clear ();
            if (menu == null)
                return;

            for (int i = menu.Children.Length - 1; i >= 0; i--) {
                Widget child = menu.Children [i];
                menu.Remove (child);
            }
        }

        public bool Contains (MenuItemWrapper item)
        {
            LoadSubItems ();
            return items.Contains (item);
        }

        public void CopyTo (MenuItemWrapper [] array, int arrayIndex)
        {
            LoadSubItems ();
            items.CopyTo (array, arrayIndex);
        }

        public bool Remove (MenuItemWrapper item)
        {
            LoadSubItems ();
            return items.Remove (item);
        }

        public int Count
        {
            get
            {
                LoadSubItems ();
                return items.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        IEnumerator<MenuItemWrapper> IEnumerable<MenuItemWrapper>.GetEnumerator ()
        {
            LoadSubItems ();
            return items.GetEnumerator ();
        }

        public IEnumerator GetEnumerator ()
        {
            return ((IEnumerable<MenuItemWrapper>) this).GetEnumerator ();
        }

        #endregion
    }
}
