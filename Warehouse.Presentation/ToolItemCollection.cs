//
// ToolItemCollection.cs
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
    public class ToolItemCollection : IList<ToolItemWrapper>
    {
        private readonly Toolbar toolbar;
        private List<ToolItemWrapper> items;

        public ToolItemWrapper this [string name]
        {
            get
            {
                return FindToolItem (name);
            }
        }

        public ToolItemCollection (Toolbar toolbar)
        {
            if (toolbar == null)
                throw new ArgumentNullException ("toolbar");

            this.toolbar = toolbar;
        }

        private void LoadSubItems ()
        {
            if (items != null)
                return;

            items = new List<ToolItemWrapper> ();

            foreach (ToolItem item in toolbar.Children) {
                items.Add (new ToolItemWrapper (item));
            }
        }

        public ToolItemWrapper FindToolItem (string itemName)
        {
            LoadSubItems ();

            foreach (ToolItemWrapper item in items) {
                if (item.Name == itemName)
                    return item;
            }

            return null;
        }

        public ToolItemWrapper FindToolItemByRestriction (string restName)
        {
            LoadSubItems ();

            foreach (ToolItemWrapper item in items) {
                if (item.RestrictionName == restName)
                    return item;
            }

            return null;
        }

        public void Translate ()
        {
            LoadSubItems ();
            RestrictionNode restrictionTree = BusinessDomain.RestrictionTree;

            foreach (ToolItemWrapper item in items) {
                if (item.IsSeparator)
                    continue;

                RestrictionNode node = restrictionTree.FindNode (item.RestrictionName);
                if (node == null)
                    throw new ApplicationException (string.Format ("No restriction node found item name \"{0}\"", item.Name));

                string text = node.Value.EndsWith ("...") ? node.Value.Substring (0, node.Value.Length - 3) : node.Value;
                item.Text = text.Trim ();
            }
        }

        #region Restrictions management

        public void ReloadRestrictions ()
        {
            LoadSubItems ();
            RestrictionNode restrictionTree = BusinessDomain.RestrictionTree;

            foreach (ToolItemWrapper item in items) {
                if (item.IsSeparator)
                    continue;

                RestrictionNode node = restrictionTree.FindNode (item.RestrictionName);
                if (node == null)
                    throw new ApplicationException (string.Format ("No restriction node found item name \"{0}\"", item.Name));

                item.ClearRestrictions ();
                foreach (KeyValuePair<long, UserRestriction> restriction in node.Restrictions)
                    item.SetRestriction (restriction.Value);
            }
        }

        public void ApplyRestriction (long userId)
        {
            LoadSubItems ();

            foreach (ToolItemWrapper item in items) {
                item.ApplyRestriction (userId);
            }
        }

        #endregion

        #region Implementation of IList<ToolButtonWraper>

        public int IndexOf (ToolItemWrapper item)
        {
            LoadSubItems ();
            return items.IndexOf (item);
        }

        public void Insert (int index, ToolItemWrapper item)
        {
            LoadSubItems ();
            items.Insert (index, item);
        }

        public void RemoveAt (int index)
        {
            LoadSubItems ();
            items.RemoveAt (index);
        }

        public ToolItemWrapper this [int index]
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

        public void Add (ToolItemWrapper item)
        {
            LoadSubItems ();
            items.Add (item);
        }

        public void Clear ()
        {
            LoadSubItems ();
            items.Clear ();
        }

        public bool Contains (ToolItemWrapper item)
        {
            LoadSubItems ();
            return items.Contains (item);
        }

        public void CopyTo (ToolItemWrapper [] array, int arrayIndex)
        {
            LoadSubItems ();
            items.CopyTo (array, arrayIndex);
        }

        public bool Remove (ToolItemWrapper item)
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

        IEnumerator<ToolItemWrapper> IEnumerable<ToolItemWrapper>.GetEnumerator ()
        {
            LoadSubItems ();
            return items.GetEnumerator ();
        }

        public IEnumerator GetEnumerator ()
        {
            return ((IEnumerable<ToolItemWrapper>) this).GetEnumerator ();
        }

        #endregion
    }
}
