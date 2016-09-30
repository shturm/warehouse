//
// OSIntegration.cs
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
using System.IO;
using Gtk;
using Warehouse.Business;
using Warehouse.Data;
using Warehouse.Presentation.Dialogs;

namespace Warehouse.Presentation.OS
{
    public abstract class OSIntegration
    {
        private readonly ICollection<string> additionalAppMenus;

        public ICollection<string> AdditionalAppMenus
        {
            get { return additionalAppMenus; }
        }

        protected OSIntegration ()
        {
            additionalAppMenus = new List<string> ();
        }

        public virtual void Init ()
        {
        }

		public virtual void RefreshMenu ()
		{
			
		}

        public virtual void OnModalDialogRemoved ()
        {
        }

        public virtual void OnModalDialogCreated ()
        {
        }

        public virtual void OnMainFormClosed ()
        {
            PresentationDomain.MainForm.CreateExitProgram ();
        }

		public virtual void Quit ()
		{
            PresentationDomain.MainForm.CreateExitProgram ();
		}

        public virtual void Restart ()
        {
            PlatformHelper.RunApplication (Path.Combine (StoragePaths.AppFolder, StoragePaths.AppEntryFile), string.Empty);
        }

        public virtual void RestrictionsApplied ()
        {
        }

        public virtual bool ChooseFileForOpen (string title, string initialFolder, out string file, params FileChooserFilter [] filters)
        {
            bool ret;
            using (FileChooserDialog fc = new FileChooserDialog (title, null, FileChooserAction.Open,
                Translator.GetString ("Open"), ResponseType.Accept,
                Translator.GetString ("Cancel"), ResponseType.Cancel)) {
                DialogBase.PushModalDialog (fc, "ChooseInputFile");

                if (!string.IsNullOrEmpty (initialFolder) && Directory.Exists (initialFolder))
                    fc.SetCurrentFolder (initialFolder);

                if (filters != null)
                    foreach (FileChooserFilter filter in filters) {
                        FileFilter ff = new FileFilter { Name = filter.Name };
                        foreach (string mask in filter.FileMasks)
                            ff.AddPattern (mask);

                        fc.AddFilter (ff);
                    }

                if (fc.Run () == (int) ResponseType.Accept) {
                    file = fc.Filename;
                    ret = true;
                } else {
                    file = null;
                    ret = false;
                }

                fc.Destroy ();
                DialogBase.PopModalDialog (fc, "ChooseInputFile");
            }

            return ret;
        }

        public virtual bool ChooseFileForSave (string title, string initialFolder, string initialFile, out string file, params FileChooserFilter [] filters)
        {
            bool ret;
            using (FileChooserDialog fc = new FileChooserDialog (title, null, FileChooserAction.Save,
                Translator.GetString ("Save"), ResponseType.Accept,
                Translator.GetString ("Cancel"), ResponseType.Cancel)) {
                DialogBase.PushModalDialog (fc, "ChooseOutputFile");

                if (!string.IsNullOrEmpty (initialFolder) && Directory.Exists (initialFolder))
                    fc.SetCurrentFolder (initialFolder);
				fc.CurrentName = initialFile;

                if (filters != null)
                    foreach (FileChooserFilter filter in filters) {
                        FileFilter ff = new FileFilter { Name = filter.Name };
                        foreach (string mask in filter.FileMasks)
                            ff.AddPattern (mask);

                        fc.AddFilter (ff);
                    }

                if (fc.Run () == (int) ResponseType.Accept) {
                    file = fc.Filename;
                    ret = true;
                } else {
                    file = null;
                    ret = false;
                }

                fc.Destroy ();
                DialogBase.PopModalDialog (fc, "ChooseOutputFile");
            }

            return ret;
        }

        public virtual bool ChooseFolder (string title, string initialFolder, out string folder)
        {
            bool ret;
            using (FileChooserDialog fc = new FileChooserDialog (title, null, FileChooserAction.SelectFolder,
                Translator.GetString ("OK"), ResponseType.Accept,
                Translator.GetString ("Cancel"), ResponseType.Cancel)) {
                DialogBase.PushModalDialog (fc, "ChooseOutputFolder");

                if (!string.IsNullOrEmpty (initialFolder) && Directory.Exists (initialFolder))
                    fc.SetCurrentFolder (initialFolder);

                if (fc.Run () == (int) ResponseType.Accept) {
                    folder = fc.CurrentFolder;
                    ret = true;
                } else {
                    folder = null;
                    ret = false;
                }

                fc.Destroy ();
                DialogBase.PopModalDialog (fc, "ChooseOutputFolder");
            }

            return ret;
        }
    }
}

