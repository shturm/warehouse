//
// PangoCairoHelper.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Created:
//   09/23/2008
//
// Copyright (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Runtime.InteropServices;
using Warehouse.Data;

namespace Warehouse.Component
{
    public static class PangoCairoHelper
    {
        private static readonly PlatformTypes platform;

        static PangoCairoHelper ()
        {
            platform = PlatformHelper.Platform;
        }

        [DllImport ("libpangocairo-1.0.so.0", EntryPoint = "pango_cairo_show_layout")]
        private static extern void pango_cairo_show_layout (IntPtr cr, IntPtr layout);
        [DllImport ("libpangocairo-1.0.dylib", EntryPoint = "pango_cairo_show_layout")]
        private static extern void pango_cairo_show_layout_mac (IntPtr cr, IntPtr layout);
        [DllImport ("libpangocairo-1.0-0.dll", EntryPoint = "pango_cairo_show_layout", CallingConvention = CallingConvention.Cdecl)]
        private static extern void pango_cairo_show_layout_win (IntPtr cr, IntPtr layout);

        public static void ShowLayout (this Cairo.Context cr, Pango.Layout layout)
        {
            switch (platform) {
                case PlatformTypes.Windows:
                    pango_cairo_show_layout_win (cr == null ? IntPtr.Zero : cr.Handle,
                        layout == null ? IntPtr.Zero : layout.Handle);
                    break;
                case PlatformTypes.MacOSX:
                    pango_cairo_show_layout_mac (cr == null ? IntPtr.Zero : cr.Handle,
                        layout == null ? IntPtr.Zero : layout.Handle);
                    break;
                default:
                    pango_cairo_show_layout (cr == null ? IntPtr.Zero : cr.Handle,
                        layout == null ? IntPtr.Zero : layout.Handle);
                    break;
            }
        }

        [DllImport ("libpangocairo-1.0.so.0", EntryPoint = "pango_cairo_create_layout")]
        private static extern IntPtr pango_cairo_create_layout (IntPtr cr);
        [DllImport ("libpangocairo-1.0.dylib", EntryPoint = "pango_cairo_create_layout")]
        private static extern IntPtr pango_cairo_create_layout_mac (IntPtr cr);
        [DllImport ("libpangocairo-1.0-0.dll", EntryPoint = "pango_cairo_create_layout", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr pango_cairo_create_layout_win (IntPtr cr);

        public static Pango.Layout CreateLayout (this Cairo.Context cr)
        {
            IntPtr raw_ret;
            switch (platform) {
                case PlatformTypes.Windows:
                    raw_ret = pango_cairo_create_layout_win (cr == null ? IntPtr.Zero : cr.Handle);
                    break;
                case PlatformTypes.MacOSX:
                    raw_ret = pango_cairo_create_layout_mac (cr == null ? IntPtr.Zero : cr.Handle);
                    break;
                default:
                    raw_ret = pango_cairo_create_layout (cr == null ? IntPtr.Zero : cr.Handle);
                    break;
            }

            return GLib.Object.GetObject (raw_ret) as Pango.Layout;
        }

        [DllImport ("libpangocairo-1.0.so.0", EntryPoint = "pango_cairo_layout_path")]
        private static extern void pango_cairo_layout_path (IntPtr cr, IntPtr layout);
        [DllImport ("libpangocairo-1.0.dylib", EntryPoint = "pango_cairo_layout_path")]
        private static extern void pango_cairo_layout_path_mac (IntPtr cr, IntPtr layout);
        [DllImport ("libpangocairo-1.0-0.dll", EntryPoint = "pango_cairo_layout_path", CallingConvention = CallingConvention.Cdecl)]
        private static extern void pango_cairo_layout_path_win (IntPtr cr, IntPtr layout);

        public static void LayoutPath (Cairo.Context cr, Pango.Layout layout)
        {
            switch (platform) {
                case PlatformTypes.Windows:
                    pango_cairo_layout_path_win (cr == null ? IntPtr.Zero : cr.Handle,
                        layout == null ? IntPtr.Zero : layout.Handle);
                    break;
                case PlatformTypes.MacOSX:
                    pango_cairo_layout_path_mac (cr == null ? IntPtr.Zero : cr.Handle,
                        layout == null ? IntPtr.Zero : layout.Handle);
                    break;
                default:
                    pango_cairo_layout_path (cr == null ? IntPtr.Zero : cr.Handle,
                        layout == null ? IntPtr.Zero : layout.Handle);
                    break;
            }
        }

        [DllImport ("libpangocairo-1.0.so.0", EntryPoint = "pango_cairo_context_set_resolution")]
        private static extern void pango_cairo_context_set_resolution (IntPtr pango_context, double dpi);
        [DllImport ("libpangocairo-1.0.dylib", EntryPoint = "pango_cairo_context_set_resolution")]
        private static extern void pango_cairo_context_set_resolution_mac (IntPtr pango_context, double dpi);
        [DllImport ("libpangocairo-1.0-0.dll", EntryPoint = "pango_cairo_context_set_resolution", CallingConvention = CallingConvention.Cdecl)]
        private static extern void pango_cairo_context_set_resolution_win (IntPtr pango_context, double dpi);

        public static void SetResolution (this Pango.Context context, double dpi)
        {
            switch (platform) {
                case PlatformTypes.Windows:
                    pango_cairo_context_set_resolution_win (context == null ? IntPtr.Zero : context.Handle, dpi);
                    break;
                case PlatformTypes.MacOSX:
                    pango_cairo_context_set_resolution_mac (context == null ? IntPtr.Zero : context.Handle, dpi);
                    break;
                default:
                    pango_cairo_context_set_resolution (context == null ? IntPtr.Zero : context.Handle, dpi);
                    break;
            }
        }

        [DllImport ("libpangocairo-1.0.so.0", EntryPoint = "pango_layout_get_context")]
        private static extern IntPtr pango_layout_get_context (IntPtr layout);
        [DllImport ("libpangocairo-1.0.dylib", EntryPoint = "pango_layout_get_context")]
        private static extern IntPtr pango_layout_get_context_mac (IntPtr layout);
        [DllImport ("libpangocairo-1.0-0.dll", EntryPoint = "pango_layout_get_context", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr pango_layout_get_context_win (IntPtr layout);

        public static Pango.Context GetContext (this Pango.Layout layout)
        {
            IntPtr handle;
            switch (platform) {
                case PlatformTypes.Windows:
                    handle = pango_layout_get_context_win (layout.Handle);
                    break;
                case PlatformTypes.MacOSX:
                    handle = pango_layout_get_context_mac (layout.Handle);
                    break;
                default:
                    handle = pango_layout_get_context (layout.Handle);
                    break;
            }

            return handle.Equals (IntPtr.Zero) ? null : GLib.Object.GetObject (handle) as Pango.Context;
        }
    }
}
