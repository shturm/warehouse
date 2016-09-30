// 
// AppDelegate.cs
// 
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
// 
// Created:
//    26.04.2012
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

using MonoMac.AppKit;
using MonoMac.Foundation;

namespace Warehouse.Presentation.OS.MacOSX
{
	public class AppDelegate : NSApplicationDelegate
	{
		public override NSApplicationTerminateReply ApplicationShouldTerminate (NSApplication sender)
		{
			if (PresentationDomain.MainForm == null)
				return NSApplicationTerminateReply.Now;
			return PresentationDomain.MainForm.CloseAllPages () ? NSApplicationTerminateReply.Now : NSApplicationTerminateReply.Cancel;
		}
		
		public override void WillTerminate (NSNotification notification)
		{
			PresentationDomain.Quit ();
		}

		public override bool ApplicationShouldHandleReopen (NSApplication sender, bool hasVisibleWindows)
		{
			if (hasVisibleWindows || PresentationDomain.MainForm == null)
				return false;
			PresentationDomain.MainForm.Window.Deiconify ();
			PresentationDomain.MainForm.Window.Visible = true;
			return true;
		}
	}
}

