//
// UserRestriction.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   12/01/2007
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

using System.Collections.Generic;
using Warehouse.Data;
using System.Linq;

namespace Warehouse.Business.Entities
{
    public enum UserRestrictionState
    {
        Allowed = 0,
        Disabled = 1,
        Restricted = 2
    }

    public class UserRestriction
    {
        #region Private fields

        private long id = -1;
        private long userId = -1;
        private string name = string.Empty;
        private UserRestrictionState state = UserRestrictionState.Restricted;
        private bool dirty = true;

        #endregion

        #region Public properties

        [DbColumn (DataField.UsersSecurityId)]
        public long Id
        {
            get { return id; }
            set { id = value; dirty = true; }
        }

        [DbColumn (DataField.UsersSecurityUserId)]
        public long UserId
        {
            get { return userId; }
            set { userId = value; dirty = true; }
        }

        [DbColumn (DataField.UsersSecurityControlName, 100)]
        public string Name
        {
            get { return name; }
            set { name = value; dirty = true; }
        }

        [DbColumn (DataField.UsersSecurityState)]
        public UserRestrictionState State
        {
            get { return state; }
            set { state = value; dirty = true; }
        }

        #endregion

        public UserRestriction ()
        {
        }

        public UserRestriction (long userId, string name, UserRestrictionState state)
        {
            this.userId = userId;
            this.name = name;
            this.state = state;
        }

        public static void CommitChanges (IEnumerable<UserRestriction> restrictions)
        {
            BusinessDomain.DataAccessProvider.AddUpdateUserRestriction (restrictions.Where (r => r.dirty));
        }

        public void CommitChanges ()
        {
            if (!dirty)
                return;

            BusinessDomain.DataAccessProvider.AddUpdateUserRestriction (new object[] { this });

            dirty = false;
        }

        public static UserRestriction [] GetAll ()
        {
            UserRestriction [] ret = BusinessDomain.DataAccessProvider.GetAllRestrictions<UserRestriction> ();
            foreach (UserRestriction restriction in ret) {
                restriction.dirty = false;
            }

            return ret;
        }

        public static UserRestriction [] GetByUser (long userId)
        {
            UserRestriction [] ret = BusinessDomain.DataAccessProvider.GetRestrictionsByUser<UserRestriction> (userId);
            foreach (UserRestriction restriction in ret) {
                restriction.dirty = false;
            }

            return ret;
        }

        public static UserRestriction [] GetByName (string name)
        {
            UserRestriction [] ret = BusinessDomain.DataAccessProvider.GetRestrictionsByName<UserRestriction> (name);
            foreach (UserRestriction restriction in ret) {
                restriction.dirty = false;
            }

            return ret;
        }
    }
}
