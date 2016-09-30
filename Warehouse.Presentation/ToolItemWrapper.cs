//
// ToolItemWrapper.cs
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

namespace Warehouse.Presentation
{
    public class ToolItemWrapper
    {
        #region Private fields

        private readonly ToolItem item;
        private readonly Dictionary<long, UserRestriction> restrictions = new Dictionary<long, UserRestriction> ();
        private string restrictionName;

        #endregion

        #region Public properties

        public string RestrictionName
        {
            get { return restrictionName; }
            set { restrictionName = value; }
        }

        public string Name
        {
            get { return item.Name; }
        }

        public string Text
        {
            get
            {
                if (item is SeparatorToolItem)
                    return "--";

                ToolButton tb = item as ToolButton;
                if (tb == null)
                    return string.Empty;

                return tb.Label;
            }
            set
            {
                ToolButton tb = item as ToolButton;
                if (tb == null)
                    return;

                tb.Label = value;
                item.TooltipText = value;
            }
        }

        public bool IsSeparator
        {
            get { return item is SeparatorToolItem; }
        }

        public Image Image
        {
            get
            {
                ToolButton tb = item as ToolButton;
                if (tb == null)
                    return null;

                return (Image) tb.IconWidget;
            }
            set
            {
                ToolButton tb = item as ToolButton;
                if (tb == null)
                    return;

                tb.IconWidget = value;
                if (value != null)
                    value.Show ();
            }
        }

        public bool Sensitive
        {
            get { return item.Sensitive; }
            set { item.Sensitive = value; }
        }

        #endregion

        public ToolItemWrapper (ToolItem item)
        {
            if (item == null)
                throw new ArgumentNullException ("item");

            this.item = item;
        }

        #region Restrictions management

        public void ClearRestrictions ()
        {
            restrictions.Clear ();
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
            item.Sensitive = GetRestriction(userId) == UserRestrictionState.Allowed;
        }

        #endregion

        public override string ToString ()
        {
            return string.Format ("{0} \"{1}\"", item.Name, Text);
        }
    }
}
