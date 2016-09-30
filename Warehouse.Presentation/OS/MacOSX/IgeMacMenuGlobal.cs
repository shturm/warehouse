//
// IgeMacMenuGlobal.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   03.23.2013
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
using System.Runtime.InteropServices;

namespace Warehouse.Presentation.OS.MacOSX
{
    public static class IgeMacMenu
    {
		private class GtkOSXApplication : GLib.Object
		{
#pragma warning disable 612,618
			public GtkOSXApplication ()
				: base (gtk_osxapplication_get_type ())
			{
			}
#pragma warning restore 612,618
		}

		[DllImport ("libigemacintegration.dylib")]
        private static extern GLib.GType gtk_osxapplication_get_type ();

        [DllImport ("libigemacintegration.dylib")]
        private static extern void gtk_osxapplication_set_menu_bar (IntPtr app, IntPtr menu_shell);

		[DllImport ("libigemacintegration.dylib")]
        static extern IntPtr gtk_osxapplication_add_app_menu_group (IntPtr app);

		[DllImport ("libigemacintegration.dylib")]
		private static extern void gtk_osxapplication_set_window_menu (IntPtr app, IntPtr menu_item);

		[DllImport ("libigemacintegration.dylib")]
        private static extern void gtk_osxapplication_ready (IntPtr app);

		[DllImport ("libigemacintegration.dylib")]
        private static extern void gtk_osxapplication_sync_menubar (IntPtr app);

        public static Gtk.MenuShell MenuBar
        {
            set
            {
				new GtkOSXApplication ();
                gtk_osxapplication_set_menu_bar (IntPtr.Zero, value == null ? IntPtr.Zero : value.Handle);
            }
        }

		public static Gtk.MenuItem WindowMenuItem
        {
            set
            {
                gtk_osxapplication_set_window_menu (IntPtr.Zero, value == null ? IntPtr.Zero : value.Handle);
            }
        }

		public static void Ready ()
		{
			gtk_osxapplication_ready (IntPtr.Zero);
		}

		public static void Sync ()
		{
			gtk_osxapplication_sync_menubar (IntPtr.Zero);
		}

        public static IgeMacMenuGroup AddAppMenuGroup ()
        {
            IntPtr raw_ret = gtk_osxapplication_add_app_menu_group (IntPtr.Zero);
            IgeMacMenuGroup ret = raw_ret == IntPtr.Zero ? null : (IgeMacMenuGroup) GLib.Opaque.GetOpaque (raw_ret, typeof (IgeMacMenuGroup), false);
            return ret;
        }
    }

    public class IgeMacMenuGroup : GLib.Opaque
    {
        private int items;

        [DllImport ("libigemacintegration.dylib")]
        static extern void gtk_osxapplication_add_app_menu_item (IntPtr app, IntPtr raw, IntPtr menu_item);

        public void AddMenuItem (Gtk.MenuItem menu_item)
        {
            gtk_osxapplication_add_app_menu_item (IntPtr.Zero, Handle, menu_item == null ? IntPtr.Zero : menu_item.Handle);
            items++;
        }

        public IgeMacMenuGroup (IntPtr raw) : base (raw) { }

        public IgeMacMenuGroup Clone ()
        {
            return (IgeMacMenuGroup) Copy (Handle);
        }
    }
}
