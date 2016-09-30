//
// ViewProfile.cs
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

using System.Collections.Generic;
using System.IO;
using System.Threading;
using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public class ViewProfile : XmlEntityBase<ViewProfile>
    {
        private const string DefaultProfileName = "Default";
        private const string NameField = "ViewProfileName";
        private const string ShowToolbarField = "ViewProfileShowToolbar";
        private const string ShowTabsField = "ViewProfileShowTabs";
        private const string ShowStatusBarField = "ViewProfileShowStatusBar";

        #region Private fields

        private string name = string.Empty;
        private bool showToolbar = true;
        private bool showTabs = true;
        private bool showStatusBar = true;
        private static ViewProfile def;

        #endregion

        #region Public properties

        [DbColumn (NameField)]
        public string Name
        {
            get { return name; }
            set { name = value; isDirty = true; }
        }

        [DbColumn (ShowToolbarField)]
        public bool ShowToolbar
        {
            get { return showToolbar; }
            set { showToolbar = value; isDirty = true; }
        }

        [DbColumn (ShowTabsField)]
        public bool ShowTabs
        {
            get { return showTabs; }
            set { showTabs = value; isDirty = true; }
        }

        [DbColumn (ShowStatusBarField)]
        public bool ShowStatusBar
        {
            get { return showStatusBar; }
            set { showStatusBar = value; isDirty = true; }
        }

        public static ViewProfile Default
        {
            get
            {
                if (def == null)
                    def = GetByName (DefaultProfileName);

                return def ?? (def = new ViewProfile {Name = DefaultProfileName});
            }
        }

        #endregion

        #region Overrides of XmlEntityBase<WindowSettings>

        private static readonly ViewProfile staticEntity = new ViewProfile ();
        private static readonly Mutex fileLock = new Mutex (false);
        private static ViewProfile [] entityCache;
        private static System.Timers.Timer commitTimer;

        protected override string EntitiesFile
        {
            get
            {
                return StoragePaths.ViewProfiles;
            }
        }

        protected override ViewProfile [] EntityCache
        {
            get { return entityCache; }
            set { entityCache = value; }
        }

        protected override Mutex FileLock
        {
            get { return fileLock; }
        }

        protected override bool LazyCommit
        {
            get { return true; }
        }

        protected override System.Timers.Timer CommitTimer
        {
            get { return commitTimer; }
            set { commitTimer = value; }
        }

        #endregion

        public override ViewProfile CommitChanges ()
        {
            if (isDirty && name == DefaultProfileName)
                def = null;

            base.CommitChanges ();

            foreach (ViewProfile profile in staticEntity.GetEntities (new Dictionary<string, object> { { NameField, name } })) {
                if (profile.id == id)
                    continue;

                staticEntity.DeleteEntity (profile.id);
            }

            return this;
        }

        public static ViewProfile GetByName (string viewProfileName)
        {
            return staticEntity.GetSingleEntity (new Dictionary<string, object> { { NameField, viewProfileName } });
        }
    }
}
