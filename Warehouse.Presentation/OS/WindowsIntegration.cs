//
// WindowsIntegration.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   01.27.2011
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
using System.Text;
using System.Windows.Forms;

namespace Warehouse.Presentation.OS
{
    public class WindowsIntegration : OSIntegration
    {
        public WindowsIntegration ()
        {
            try {
                // This is needed in order for GTK# 2.12.8 to get the proper theming
                Application.EnableVisualStyles ();
                Application.DoEvents ();
            } catch { }
        }

        public override bool ChooseFileForOpen (string title, string initialFolder, out string file, params FileChooserFilter [] filters)
        {
            using (OpenFileDialog dialog = new OpenFileDialog ())
                return ChooseFile (dialog, title, initialFolder, string.Empty, out file, filters);
        }

        public override bool ChooseFileForSave (string title, string initialFolder, string initialFile, out string file, params FileChooserFilter [] filters)
        {
            using (SaveFileDialog dialog = new SaveFileDialog ())
                return ChooseFile (dialog, title, initialFolder, initialFile, out file, filters);
        }

        private static bool ChooseFile (FileDialog dialog, string title, string initialFolder, string initialFile, out string file, IEnumerable<FileChooserFilter> filters)
        {
            dialog.InitialDirectory = initialFolder;
        	dialog.FileName = initialFile;
            dialog.Title = title;

            StringBuilder filterBuilder = new StringBuilder ();
            foreach (FileChooserFilter fileChooserFilter in filters) {
                filterBuilder.Append (fileChooserFilter.Name);
                filterBuilder.Append ('|');
                foreach (string fileMask in fileChooserFilter.FileMasks) {
                    filterBuilder.Append (fileMask);
                    filterBuilder.Append (';');
                }
            }

            if (filterBuilder.Length > 0)
                filterBuilder.Remove (filterBuilder.Length - 1, 1);

            dialog.Filter = filterBuilder.ToString ();
            if (dialog.ShowDialog () == DialogResult.OK) {
                file = dialog.FileName;
                return true;
            }
            file = null;
            return false;
        }

        public override bool ChooseFolder (string title, string initialFolder, out string folder)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog { Description = title, SelectedPath = initialFolder })
                if (dialog.ShowDialog () == DialogResult.OK) {
                    folder = dialog.SelectedPath;
                    return true;
                }

            folder = null;
            return false;
        }
    }
}

