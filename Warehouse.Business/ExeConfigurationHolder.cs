//
// ExeConfigurationHolder.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   02.28.2011
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
using System.Configuration;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Xml;
using System.Xml.Linq;
using Warehouse.Data;

namespace Warehouse.Business
{
    public class ExeConfigurationHolder : ConfigurationHolder
    {
        protected override void LoadConfigSettings ()
        {
            LoadLocalConfig ();

            if (!Directory.Exists (StoragePaths.CommonAppDataFolder)) {
                DirectoryInfo directoryInfo = Directory.CreateDirectory (StoragePaths.CommonAppDataFolder);
                DirectorySecurity directorySecurity = directoryInfo.GetAccessControl ();
                SecurityIdentifier everyone = new SecurityIdentifier (WellKnownSidType.WorldSid, null);
                directorySecurity.AddAccessRule (new FileSystemAccessRule (everyone,
                    FileSystemRights.CreateDirectories | FileSystemRights.CreateFiles |
                    FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Synchronize,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None, AccessControlType.Allow));
                directoryInfo.SetAccessControl (directorySecurity);
            }

            if (!Directory.Exists (StoragePaths.UserAppDataFolder))
                Directory.CreateDirectory (StoragePaths.UserAppDataFolder);

            configFileName = StoragePaths.ConfigFile;
            string configFolder = Path.GetDirectoryName (configFileName);
            // Just in case we have a custom config file location
            if (configFolder != null && !Directory.Exists (configFolder))
                Directory.CreateDirectory (configFolder);

            bool retry;
            do {
                retry = false;
                if (!File.Exists (configFileName)) {
                    XmlDocument xml = new XmlDocument ();
                    xml.LoadXml (@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
</configuration>");
                    xml.Save (configFileName);
                }

                ExeConfigurationFileMap map = new ExeConfigurationFileMap { ExeConfigFilename = configFileName };
                try {
                    config = ConfigurationManager.OpenMappedExeConfiguration (map, ConfigurationUserLevel.None);
                } catch (XmlException) {
                    File.Delete (configFileName);
                    retry = true;
                }
            } while (retry);

            settings = config.AppSettings.Settings;
        }

        private static void LoadLocalConfig ()
        {
            string localConfig = Path.GetFileName (StoragePaths.ConfigFile);
            if (localConfig == null)
                return;

            localConfig = Path.Combine (StoragePaths.AppFolder, localConfig);
            if (!File.Exists (localConfig))
                return;

            XDocument document = XDocument.Load (localConfig);
            if (document.Root == null || document.Root.Name != "configuration")
                return;

            IEnumerable<XElement> xElements = document.Root.Elements ("appSettings").Elements ("add");
            foreach (XElement element in xElements) {
                var key = element.Attribute ("key");
                var val = element.Attribute ("value");
                if (key == null || val == null)
                    continue;

                string folder = val.Value;
                if (!PlatformHelper.IsWindows && folder.StartsWith ("~/"))
                    folder = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), folder.Substring (2));

                switch (key.Value) {
                    case "CommonAppData":
                        StoragePaths.CommonAppDataFolder = Path.GetFullPath (folder);
                        break;

                    case "UserAppData":
                        string defaultConfigFile = StoragePaths.ConfigFile;
                        StoragePaths.UserAppDataFolder = Path.GetFullPath (folder);
                        StoragePaths.ConfigFile = null;
                        string newConfigFile = StoragePaths.ConfigFile;
                        if (!string.IsNullOrWhiteSpace (defaultConfigFile) && !string.IsNullOrWhiteSpace (newConfigFile) &&
                            defaultConfigFile != newConfigFile &&
                            File.Exists (defaultConfigFile) && !File.Exists (newConfigFile))
                            try {
                                File.Copy (defaultConfigFile, newConfigFile);
                            } catch (Exception) {
                            }
                        break;
                }
            }
        }
    }
}

