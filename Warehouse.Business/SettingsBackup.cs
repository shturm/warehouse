// 
// SettingsBackup.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at yahoo dot com>
// 
// Created:
//    04.12.2012
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Mono.Addins;
using Warehouse.Data;

namespace Warehouse.Business
{
    public class SettingsBackup
    {
        private readonly List<string> addedFiles = new List<string> ();

        public void Save (string archive)
        {
            // make sure stuff such as last used paths are written
            BusinessDomain.AppConfiguration.Save (false);
            using (FileStream fsOut = File.Create (archive)) {
                using (ZipOutputStream zipStream = new ZipOutputStream (fsOut)) {
                    zipStream.SetLevel (9);

                    List<string> userPaths = new List<string> ();
                    List<string> commonPaths = new List<string> ();
                    GetPaths (userPaths, commonPaths);

                    SaveUserSettings (zipStream, userPaths);
                    SaveCommonSettings (zipStream, commonPaths);

                    zipStream.IsStreamOwner = true;
                }
            }
        }

        public void Load (string archive)
        {
            BusinessDomain.AppConfiguration.DisableSave = true;
            string settingsDir = ExtractSettings (archive);
            LoadSettings (settingsDir, "User", StoragePaths.UserAppDataFolder);
            LoadSettings (settingsDir, "Common", StoragePaths.CommonAppDataFolder);
        }

        private static void LoadSettings (string settingsDir, string prefix, string destination)
        {
            StringBuilder backupBuilder = new StringBuilder ();
            if (Directory.Exists (destination)) {
                string backup = string.Format ("{0}_backup", destination);
                backupBuilder = new StringBuilder (backup);
                int i = 0;
                while (Directory.Exists (backupBuilder.ToString ())) {
                    backupBuilder.Length = 0;
                    backupBuilder.Append (backup).Append (++i);
                }
                DirectoryCopy (destination, backupBuilder.ToString ());
            }
            try {
                string dir = Path.Combine (settingsDir, prefix);
                if (Directory.Exists (dir))
                    foreach (string file in Directory.GetFiles (dir, "*.*", SearchOption.AllDirectories)) {
                        string to = Path.Combine (destination, file.Substring (dir.Length + 1));
                        Directory.CreateDirectory (Path.GetDirectoryName (to));
                        File.Copy (file, to, true);
                    }

                string backup = backupBuilder.ToString ();
                if (Directory.Exists (backup))
                    Directory.Delete (backup, true);
            } catch (Exception) {
                string backup = backupBuilder.ToString ();
                if (Directory.Exists (backup)) {
                    DirectoryCopy (backup, destination);
                    Directory.Delete (backup, true);
                }
                throw;
            }
        }

        private static void DirectoryCopy (string sourceDirName, string destDirName)
        {
            DirectoryInfo dir = new DirectoryInfo (sourceDirName);
            DirectoryInfo [] dirs = dir.GetDirectories ();
            Directory.CreateDirectory (destDirName);
            FileInfo [] files = dir.GetFiles ();
            foreach (FileInfo file in files)
                file.CopyTo (Path.Combine (destDirName, file.Name), true);
            foreach (DirectoryInfo subdir in dirs)
                DirectoryCopy (subdir.FullName, Path.Combine (destDirName, subdir.Name));
        }

        private static void GetPaths (ICollection<string> userPaths, ICollection<string> commonPaths)
        {
            foreach (string path in (from TypeExtensionNode pathExtension in AddinManager.GetExtensionNodes ("/Warehouse/Data/Paths")
                                     select pathExtension.CreateInstance ()).Cast<IStoragePaths> ().SelectMany (p => p.GetAll ()))
                if (path.StartsWith (StoragePaths.UserAppDataFolder))
                    userPaths.Add (path);
                else
                    commonPaths.Add (path);
        }

        private void SaveUserSettings (ZipOutputStream zipStream, IEnumerable<string> userPaths)
        {
            CompressFiles ("User", userPaths, zipStream, StoragePaths.UserAppDataFolder);
        }

        private void SaveCommonSettings (ZipOutputStream zipStream, IEnumerable<string> commonPaths)
        {
            CompressFiles ("Common", commonPaths, zipStream, StoragePaths.CommonAppDataFolder);
        }

        private void CompressFiles (string prefix, IEnumerable<string> files, ZipOutputStream zipStream, string folderName)
        {
            string separator = Path.DirectorySeparatorChar.ToString (CultureInfo.InvariantCulture);
            int folderOffset = folderName.Length + (folderName.EndsWith (separator) ? 0 : 1);
            foreach (string file in files.Where (file => File.Exists (file) && !addedFiles.Contains (file))) {
                addedFiles.Add (file);
                FileInfo fi = new FileInfo (file);

                string entryName = Path.Combine (prefix, file.Substring (folderOffset));
                entryName = ZipEntry.CleanName (entryName);
                ZipEntry newEntry = new ZipEntry (entryName);
                newEntry.DateTime = fi.LastWriteTime;
                newEntry.IsUnicodeText = true;
                newEntry.Size = fi.Length;

                zipStream.PutNextEntry (newEntry);

                byte [] buffer = new byte [4096];
                using (FileStream streamReader = File.OpenRead (file))
                    StreamUtils.Copy (streamReader, zipStream, buffer);
                zipStream.CloseEntry ();
            }
        }

        private static string ExtractSettings (string archive)
        {
            string dir = Path.GetTempFileName () + ".settings";
            if (Directory.Exists (dir))
                Directory.Delete (dir, true);

            using (FileStream fs = File.OpenRead (archive)) {
                using (ZipFile zf = new ZipFile (fs)) {
                    foreach (ZipEntry zipEntry in zf) {
                        if (!zipEntry.IsFile)
                            continue;

                        string entryFileName = zipEntry.Name;

                        byte [] buffer = new byte [4096];
                        Stream zipStream = zf.GetInputStream (zipEntry);

                        string fullZipToPath = Path.Combine (dir, entryFileName);
                        string directoryName = Path.GetDirectoryName (fullZipToPath);
                        if (directoryName.Length > 0)
                            Directory.CreateDirectory (directoryName);

                        using (FileStream streamWriter = File.Create (fullZipToPath))
                            StreamUtils.Copy (zipStream, streamWriter, buffer);
                        zf.IsStreamOwner = true;
                    }
                }
            }
            return dir;
        }
    }
}
