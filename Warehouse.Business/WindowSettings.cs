//
// WindowSettings.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   03.19.2011
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
using System.IO;
using System.Threading;
using Warehouse.Business.Entities;
using Warehouse.Data;
using Warehouse.Data.DataBinding;

namespace Warehouse.Business
{
    public class WindowSettings : XmlEntityBase<WindowSettings>
    {
        #region Overrides of XmlEntityBase<WindowSettings>

        private static readonly WindowSettings staticEntity = new WindowSettings ();
        private static readonly Mutex fileLock = new Mutex (false);
        private static WindowSettings [] entityCache;
        private static System.Timers.Timer commitTimer;

        protected override string EntitiesFile
        {
            get { return StoragePaths.WindowSettings; }
        }

        protected override WindowSettings [] EntityCache
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

        private int width;
        [DbColumn (WindowSettingsFields.Width)]
        public int Width
        {
            get { return width; }
            set
            {
                if (width == value) return;

                width = value;
                OnPropertyChanged ("Width");
            }
        }

        private int height;
        [DbColumn (WindowSettingsFields.Height)]
        public int Height
        {
            get { return height; }
            set
            {
                if (height == value) return;

                height = value;
                OnPropertyChanged ("Height");
            }
        }

        private string windowId;
        [DbColumn (WindowSettingsFields.WindowId)]
        public string WindowId
        {
            get { return windowId; }
            set
            {
                if (windowId == value) return;

                windowId = value;
                OnPropertyChanged ("WindowId");
            }
        }

        private readonly BindList<ColumnSettings> columns = new BindList<ColumnSettings> ();
        [DbColumn (WindowSettingsFields.GridColumns)]
        public BindList<ColumnSettings> Columns
        {
            get { return columns; }
        }

        private readonly BindList<PaneSettings> panes = new BindList<PaneSettings> ();
        [DbColumn (WindowSettingsFields.Panes)]
        public BindList<PaneSettings> Panes
        {
            get { return panes; }
        }

        public WindowSettings ()
        {
            columns.ListChanged += (sender, e) => OnPropertyChanged ("Columns");
            panes.ListChanged += (sender, e) => OnPropertyChanged ("Panes");
        }

        public static WindowSettings [] GetAll ()
        {
            return staticEntity.GetAllEntities (true);
        }

        public static WindowSettings [] Get (Dictionary<string, object> criteria)
        {
            return staticEntity.GetEntities (criteria);
        }

        public static WindowSettings GetSingle (Dictionary<string, object> criteria)
        {
            WindowSettings [] ret = Get (criteria);
            return ret.Length > 0 ? ret [0] : null;
        }

        public static WindowSettings GetById (long id)
        {
            return GetSingle (new Dictionary<string, object> { { XmlEntityFields.Id, id } });
        }

        public static WindowSettings GetByWindowId (string windowId)
        {
            return GetSingle (new Dictionary<string, object> { { WindowSettingsFields.WindowId, windowId } });
        }

        public static void Flush ()
        {
            staticEntity.FlushCache ();
        }
    }

    public static class WindowSettingsFields
    {
        public const string Width = "Width";
        public const string Height = "Height";
        public const string WindowId = "WindowId";
        public const string GridColumns = "GridColumns";
        public const string Panes = "Panes";
    }
}
