// 
// StoragePaths.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at yahoo dot com>
// 
// Created:
//    05.12.2012
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
using System.IO;
using System.Reflection;

namespace Warehouse.Data
{
    public class StoragePaths
    {
        private const string Custom = "Custom_";
        private const string Default = "Default";

        private static string userAppDataFolder;
        private static string commonAppDataFolder;
        private static string appFolder;
        private static string appEntryFile;
        private static string configFile;
        private static string customDocumentsFolder;

        public static string UserAppDataFolder
        {
            get
            {
                if (string.IsNullOrEmpty (userAppDataFolder)) {
                    userAppDataFolder = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
                    userAppDataFolder = Path.Combine (userAppDataFolder, DataHelper.CompanyName);
                    userAppDataFolder = Path.Combine (userAppDataFolder, DataHelper.ProductName);
                }

                return userAppDataFolder;
            }
            set { userAppDataFolder = value; }
        }

        public static string CommonAppDataFolder
        {
            get
            {
                if (string.IsNullOrEmpty (commonAppDataFolder)) {
                    commonAppDataFolder = Environment.GetFolderPath (Environment.SpecialFolder.CommonApplicationData);
                    commonAppDataFolder = Path.Combine (commonAppDataFolder, DataHelper.CompanyName);
                    commonAppDataFolder = Path.Combine (commonAppDataFolder, DataHelper.ProductName);
                }

                return commonAppDataFolder;
            }
            set { commonAppDataFolder = value; }
        }

        private static string monoAddinsDataFolder;
        public static string MonoAddinsDataFolder
        {
            get
            {
                if (string.IsNullOrEmpty (monoAddinsDataFolder)) {
                    monoAddinsDataFolder = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
                    monoAddinsDataFolder = Path.Combine (monoAddinsDataFolder, "mono.addins");
                }

                return monoAddinsDataFolder;
            }
        }

        public static string AppFolder
        {
            get
            {
                if (string.IsNullOrEmpty (appFolder)) {
                    Assembly asm = Assembly.GetEntryAssembly ();
                    appFolder = asm == null ? string.Empty : Path.GetDirectoryName (asm.Location);
                }

                return appFolder;
            }
            set { appFolder = value; }
        }

        public static string AppAddInsFolder
        {
            get { return AppFolder; }
        }

        public static string LocaleFolder
        {
            get { return Path.Combine (AppFolder, "locale"); }
        }

        /// <summary>
        /// Gets the folder which contains the templates for all printable documents.
        /// </summary>
        public static string DocumentTemplatesFolder
        {
            get { return Path.Combine (AppFolder, "Documents"); }
        }

        public static string CustomDocumentsFolder
        {
            get
            {
                if (string.IsNullOrEmpty (customDocumentsFolder))
                    customDocumentsFolder = Path.Combine (CommonAppDataFolder, "Documents");

                return customDocumentsFolder;
            }
        }

        public static string AccessLevelsFile
        {
            get { return Path.Combine (CommonAppDataFolder, "accesslevels.dat"); }
        }

        public static string LicenseFolder
        {
            get { return CommonAppDataFolder; }
        }

        public static string UpdatesFolder
        {
            get { return Path.Combine (CommonAppDataFolder, "Updates"); }
        }

        public static string AppEntryFile
        {
            get
            {
                if (string.IsNullOrEmpty (appEntryFile)) {
                    Assembly asm = Assembly.GetEntryAssembly ();
                    appEntryFile = asm == null ? string.Empty : Path.GetFileName (asm.Location);
                }

                return appEntryFile;
            }
        }

        public static string ConfigFile
        {
            get
            {
                if (string.IsNullOrEmpty (configFile))
                    configFile = Path.Combine (UserAppDataFolder, AppEntryFile + ".config");

                return configFile;
            }
            set { configFile = value; }
        }

        /// <summary>
        /// Gets or sets the path to the file containing the current mappings of key bindings to application commands.
        /// </summary>
        /// <value>The new file to contain the current mappings of key bindings to application commands.</value>
        public static string KeyMapFile
        {
            get
            {
                string keyMapFile = Path.Combine (UserAppDataFolder, "Current.keymap");
                if (!File.Exists (keyMapFile))
                    File.WriteAllText (keyMapFile, string.Empty);

                return keyMapFile;
            }
        }

        public static string WindowSettings
        {
            get { return Path.Combine (UserAppDataFolder, "windows.xml"); }
        }

        public static string Devices
        {
            get { return Path.Combine (UserAppDataFolder, "devices.xml"); }
        }

        public static string ViewProfiles
        {
            get { return Path.Combine (UserAppDataFolder, "viewprofiles.xml"); }
        }

        public static string ReportQueryStateFile
        {
            get { return Path.Combine (UserAppDataFolder, "reportqueries.xml"); }
        }

        public static string DocumentQueryStateFile
        {
            get { return Path.Combine (UserAppDataFolder, "documentqueries.xml"); }
        }

        public static string GetCustomDocumentTemplatesFolder (string documentTemplatesFolder)
        {
            string folderName = Path.GetFileName (documentTemplatesFolder);
            if (folderName == null || folderName.Contains (Custom))
                return documentTemplatesFolder;

            return Path.Combine (CustomDocumentsFolder, Custom +
                (string.IsNullOrEmpty (documentTemplatesFolder) ? Default : folderName));
        }
    }
}
