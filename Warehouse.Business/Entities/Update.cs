//
// Update.cs
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
using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public enum ReleaseTypes
    {
        Stable = 0,
        Beta = 1,
        Alpha = 2,
    }

    public class Update
    {
        #region Private fields

        private string version;
        protected string path;

        #endregion

        #region Public properties

        [DbColumn ("updates.ID")]
        public int Id { get; set; }

        [DbColumn ("updates.Product")]
        public string Product { get; set; }

        [DbColumn ("updates.Version")]
        public string Version
        {
            get { return version; }
            set { version = value; }
        }

        [DbColumn ("updates.Platform")]
        public PlatformTypes Platform { get; set; }

        [DbColumn ("updates.ReleaseType")]
        public ReleaseTypes ReleaseType { get; set; }

        [DbColumn ("updates.Size")]
        public long Size { get; set; }

        [DbColumn ("updates.Path")]
        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        [DbColumn ("updates.Date")]
        public DateTime Date { get; set; }

        [DbColumn ("updates.Description")]
        public string Description { get; set; }

        public bool IsNewerVersion
        {
            get
            {
                Version v = GetVersion ();
                return v != null && v > BusinessDomain.ApplicationVersion;
            }
        }

        #endregion

        public Version GetVersion ()
        {
            if (string.IsNullOrEmpty (version))
                return null;

            string [] verParts = version.Split ('.');
            if (verParts.Length < 4)
                return null;

            int major, minor, build, revision;
            if (!int.TryParse (verParts [0], out major) ||
                !int.TryParse (verParts [1], out minor) ||
                !int.TryParse (verParts [2], out build) ||
                !int.TryParse (verParts [3], out revision))
                return null;

            return new Version (major, minor, build, revision);
        }

        public string GetLocalFileLocation ()
        {
            if (string.IsNullOrEmpty (path))
                return string.Empty;

            string fName = path.Contains ("/") ? path.Substring (path.LastIndexOf ('/') + 1) : path;

            return System.IO.Path.Combine (StoragePaths.UpdatesFolder, fName);
        }
    }
}
