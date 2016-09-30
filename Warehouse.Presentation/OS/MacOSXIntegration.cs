//
// MacOSXIntegration.cs
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MonoMac.AppKit;
using MonoMac.Foundation;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Data;
using Warehouse.Presentation.OS.MacOSX;

namespace Warehouse.Presentation.OS
{
    public class MacOSXIntegration : OSIntegration
    {
        private static readonly IDictionary<MenuItemWrapper, bool> menuSensitivity = new Dictionary<MenuItemWrapper, bool> ();
        MenuItemWrapper menuItemSettings;
        private static bool? snowLeopardOrAbove;

        private static bool SnowLeopardOrAbove
        {
            get
            {
                if (snowLeopardOrAbove == null) {
                    try {
                        string versionString;
                        using (Process systemVersionTool = new Process ()) {
                            systemVersionTool.StartInfo.FileName = "sw_vers";
                            systemVersionTool.StartInfo.UseShellExecute = false;
                            systemVersionTool.StartInfo.RedirectStandardOutput = true;
                            systemVersionTool.Start ();
            
                            string output = systemVersionTool.StandardOutput.ReadToEnd ();
                            versionString = Regex.Match (output, @"ProductVersion:\s*(\S+)\s*").Groups [1].Value;
                            systemVersionTool.WaitForExit (1000);
                        }
                        Version systemVersion = Version.Parse (versionString);
                        snowLeopardOrAbove = systemVersion.Major > 10 || (systemVersion.Major == 10 && systemVersion.Minor > 5);
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex, ErrorSeverity.FatalError);
                        snowLeopardOrAbove = true;
                    }
                }
                return snowLeopardOrAbove.Value;
            }
        }

        public override void Init ()
        {
			NSApplication.InitDrawingBridge ();
			NSApplication.Init ();

            IntegrateMenu ();

			NSApplication.SharedApplication.Delegate = new AppDelegate ();
            // New mono shows error when having this line but old dows not show the menu without it
            NSApplication.SharedApplication.FinishLaunching ();
        }

        private void IntegrateMenu ()
        {
            FrmMain mainForm = PresentationDomain.MainForm;

            RestrictionNode restrictionAppGroup = new RestrictionNode (DataHelper.ProductName, () => DataHelper.ProductName);

            MoveRestriction ("mnuHelpAbout", restrictionAppGroup);
            foreach (string item in AdditionalAppMenus)
                MoveRestriction (item, restrictionAppGroup);
            MoveRestriction ("mnuToolsSetup", restrictionAppGroup);
            MoveRestriction ("mnuFileExit", restrictionAppGroup);

            BusinessDomain.RestrictionTree.InsertBefore ("mnuFile", restrictionAppGroup);

            menuItemSettings = mainForm.MainMenu.FindMenuItem ("mnuToolsSetup");
            // TODO: remove when the short-cuts are fixed on the mac
            BusinessDomain.RestrictionTree.RemoveNode ("mnuToolsQuickItems");
            mainForm.MainMenu.RemoveItem ("mnuToolsQuickItems");

            //Tell the IGE library to use your GTK menu as the Mac main menu
            IgeMacMenu.MenuBar = mainForm.MainMenu.Menu;

            RefreshAppMenu (mainForm);

            //hide the menu bar so it no longer displays within the window
            mainForm.MainMenu.Menu.Hide ();
        }

		public override void RefreshMenu ()
		{
			IgeMacMenu.Sync ();
			if (menuItemSettings == null)
				return;
			menuItemSettings.Text = menuItemSettings.Translate ();
			// GTK-OSX cannot translate top menus even with its Cocoa implementation, have to do it manually
			for (int i = 0; i < PresentationDomain.MainForm.MainMenu.Count; i++)
				NSApplication.SharedApplication.MainMenu.ItemAt (i + 1).Submenu.Title = PresentationDomain.MainForm.MainMenu [i].Text;
		}

        public override void OnModalDialogRemoved ()
		{
			if (PresentationDomain.MainForm == null || ComponentHelper.Windows.Count > 1)
				return;
			EnableMenuItems (PresentationDomain.MainForm.MainMenu);
		}

		public override void OnModalDialogCreated ()
		{
			if (PresentationDomain.MainForm == null || ComponentHelper.Windows.Count > 2)
				return;
			DisableMenuItems (PresentationDomain.MainForm.MainMenu);
		}

		private void EnableMenuItems (MenuItemCollection menuItems)
		{
			foreach (MenuItemWrapper item in menuItems) {
				if (menuSensitivity.ContainsKey (item)) {
					item.Sensitive = menuSensitivity[item];
					menuSensitivity.Remove (item);
				}
				EnableMenuItems (item.SubItems);
			}
		}

		private void DisableMenuItems (MenuItemCollection menuItems)
		{
			foreach (MenuItemWrapper item in menuItems) {
				menuSensitivity.Add (item, item.Sensitive);
				item.Sensitive = false;
				DisableMenuItems (item.SubItems);
			}
		}

		private static void MoveRestriction (string menuName, RestrictionNode restrictionAppGroup)
		{
			restrictionAppGroup.Children.Add (BusinessDomain.RestrictionTree.FindNode (menuName));
			BusinessDomain.RestrictionTree.RemoveNode (menuName);
		}

        public override void RestrictionsApplied ()
        {
			if (PresentationDomain.MainForm == null)
				return;
			foreach (MenuItemWrapper item in new List<MenuItemWrapper> (menuSensitivity.Keys))
				menuSensitivity [item] = item.GetRestriction (BusinessDomain.LoggedUser.Id) != UserRestrictionState.Disabled;
			PresentationDomain.MainForm.MainMenu.FixSeparators (true);
        }

        private void RefreshAppMenu (FrmMain mainForm)
        {
            if (mainForm == null)
                mainForm = PresentationDomain.MainForm;

            if (mainForm == null)
                return;
				
			int index = 0;
			MoveToAppMenu (mainForm, mainForm.MainMenu.FindMenuItem ("mnuHelpAbout"), index++);

            foreach (string item in AdditionalAppMenus)
				MoveToAppMenu (mainForm, mainForm.MainMenu.FindMenuItem (item), index++);

			NSApplication.SharedApplication.MainMenu.ItemAt (0).Submenu.InsertItem (NSMenuItem.SeparatorItem, index++);

			menuItemSettings.Translate = () => Translator.GetString ("Preferences...");
			menuItemSettings.Text = menuItemSettings.Translate ();
			MoveToAppMenu (mainForm, menuItemSettings, index++);

			MenuItemWrapper exitMenu = mainForm.MainMenu.FindMenuItem ("mnuFileExit");
			exitMenu.FixSeparators ();
			exitMenu.Parent.RemoveItem (exitMenu.Name, true);
			
			mainForm.MainMenu.FixSeparators (true);
        }

		private void MoveToAppMenu (FrmMain mainForm, MenuItemWrapper menuItemWrapper, int appMenuIndex)
		{
			var mainMenu = NSApplication.SharedApplication.MainMenu;
			var appMenu = mainMenu.ItemAt (0).Submenu;

			var itemName = menuItemWrapper.Text;
			var groupName = menuItemWrapper.Parent.Parent.Text;

			var subMenu = mainMenu.ItemWithTitle (groupName).Submenu;
			var nsMenu = subMenu.ItemWithTitle (itemName);
			subMenu.RemoveItem (nsMenu);
			appMenu.InsertItem (nsMenu, appMenuIndex);
			menuItemWrapper.FixSeparators ();
		}

        public override void OnMainFormClosed ()
        {
            PresentationDomain.MainForm.Window.Visible = false;
            PresentationDomain.MainForm.Window.Iconify ();
        }

		public override void Quit ()
		{
			NSApplication.SharedApplication.Terminate (NSApplication.SharedApplication);
		}

        public override void Restart ()
        {
            // open Program.app <- Program.app/Contents/MacOS = app dir
            PlatformHelper.RunApplication ("open", "-n \"" + Directory.GetParent (StoragePaths.AppFolder).Parent.FullName + "\"", false);
        }

        public override bool ChooseFileForSave (string title, string initialFolder, string initialFile, out string file, params FileChooserFilter [] filters)
        {
            using (NSSavePanel savePanel = new NSSavePanel ()) {
                savePanel.TreatsFilePackagesAsDirectories = true;
                savePanel.Title = title;
                if (Run (initialFolder, initialFile, out file, filters, savePanel)) 
                    return true;
            }
            file = null;
            return false;
        }

        public override bool ChooseFileForOpen (string title, string initialFolder, out string file, params FileChooserFilter [] filters)
        {
            return ChooseFile (title, initialFolder, out file, filters, true);
        }

        public override bool ChooseFolder (string title, string initialFolder, out string folder)
        {
            return ChooseFile (title, initialFolder, out folder, null, false);
        }

        private static bool ChooseFile (string title, string initialFolder, out string file, IList<FileChooserFilter> filters, bool chooseFile)
        {
            using (NSOpenPanel openPanel = new NSOpenPanel ()) {
                openPanel.TreatsFilePackagesAsDirectories = true;
                openPanel.Title = title;
                openPanel.CanChooseFiles = chooseFile;
                openPanel.CanChooseDirectories = !chooseFile;
                if (Run (initialFolder, null, out file, filters, openPanel)) 
                    return true;
            }
            file = null;
            return false;
        }

        private static bool Run (string initialFolder, string initialFile, out string file, IList<FileChooserFilter> filters, NSSavePanel savePanel)
        {
            NSPopUpButton popup = CreateFileFilterPopup (filters, savePanel);
            if (popup != null)
                using (popup) {
                    savePanel.AccessoryView = popup;
                    if (Run (initialFolder, initialFile, out file, savePanel))
                        return true;
                }
            else if (Run (initialFolder, initialFile, out file, savePanel))
                return true;
            return false;
        }

        private static NSPopUpButton CreateFileFilterPopup (IList<FileChooserFilter> filters, NSSavePanel panel)
        {
            //no filtering
            if (filters == null || filters.Count == 0)
                return null;

            //filter, but no choice
            if (filters.Count == 1) {
                panel.ShouldEnableUrl = GetFileFilter (filters [0]);
                return null;
            }

            NSPopUpButton popup = new NSPopUpButton (new RectangleF (0, 6, 200, 18), false);
            popup.SizeToFit ();
            RectangleF rect = popup.Frame;
            popup.Frame = new RectangleF (rect.X, rect.Y, 200, rect.Height);

            foreach (var filter in filters)
                popup.AddItem (filter.Name);

            panel.ShouldEnableUrl = GetFileFilter (filters [0]);

            popup.Activated += delegate
                {
                    panel.ShouldEnableUrl = GetFileFilter (filters [popup.IndexOfSelectedItem]);
                    panel.Display ();
                };

            return popup;
        }

        private static NSOpenSavePanelUrl GetFileFilter (FileChooserFilter filter)
        {
            Regex globRegex = CreateGlobRegex (filter.FileMasks);

            return (sender, url) =>
                {
                    //never show non-file URLs
                    if (!url.IsFileUrl)
                        return false;

                    string path = url.Path;

                    //always make directories selectable, unless they're app bundles
                    if (Directory.Exists (path))
                        return !path.EndsWith (".app", StringComparison.OrdinalIgnoreCase);

                    return globRegex != null && globRegex.IsMatch (path);
                };
        }

        private static Regex CreateGlobRegex (IEnumerable<string> globs)
        {
            StringBuilder globalPattern = new StringBuilder ();

            foreach (string glob in globs) {
                string pattern = Regex.Escape (glob);
                pattern = pattern.Replace ("\\*", ".*");
                pattern = pattern.Replace ("\\?", ".");
                pattern = pattern.Replace ("\\|", "$|^");
                pattern = "^" + pattern + "$";
                if (globalPattern.Length > 0)
                    globalPattern.Append ('|');
                globalPattern.Append (pattern);
            }
            return new Regex (globalPattern.ToString (), RegexOptions.Compiled);
        }

        private static bool Run (string initialFolder, string initialFile, out string file, NSSavePanel savePanel)
        {
            using (NSUrl url = new NSUrl (initialFolder, true)) {
                int result;
                if (SnowLeopardOrAbove) {
                    savePanel.DirectoryUrl = url;
					if (!string.IsNullOrEmpty (initialFile))
                        savePanel.NameFieldStringValue = initialFile;
                    result = savePanel.RunModal ();
                } else {
#pragma warning disable 612,618
                    result = savePanel.RunModal (initialFolder, initialFile);
#pragma warning restore 612,618
                }
                if (result != 0) {
                    file = savePanel.Url.Path;
                    return true;
                }
            }
            file = null;
            return false;
        }
    }
}

