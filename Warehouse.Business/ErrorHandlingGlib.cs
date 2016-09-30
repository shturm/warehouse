//
// ErrorHandlingGlib.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   03/21/2006
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

using GLib;

namespace Warehouse.Business
{
    public static class ErrorHandlingGlib
    {
        private static bool glibHooked;
        private static uint gtkLogHandle;
        private static uint gdkLogHandle;
        private static uint glibLogHandle;
        private static UnhandledExceptionHandler exceptionHandler;

        #region GLib errors management

        public static void HookErrors (UnhandledExceptionHandler handler)
        {
            if (glibHooked)
                return;

            glibHooked = true;
            exceptionHandler = handler;
            ExceptionManager.UnhandledException += OnUnhandledException;
            gtkLogHandle = Log.SetLogHandler ("Gtk", LogLevelFlags.All, LogFunc);
            gdkLogHandle = Log.SetLogHandler ("Gdk", LogLevelFlags.All, LogFunc);
            glibLogHandle = Log.SetLogHandler ("GLib", LogLevelFlags.All, LogFunc);
        }

        public static void UnhookErrors ()
        {
            if (!glibHooked)
                return;

            glibHooked = false;
            ExceptionManager.UnhandledException -= OnUnhandledException;
            Log.RemoveLogHandler ("Gtk", gtkLogHandle);
            Log.RemoveLogHandler ("Gdk", gdkLogHandle);
            Log.RemoveLogHandler ("GLib", glibLogHandle);
        }

        private static void LogFunc (string logDomain, LogLevelFlags logLevel, string message)
        {
            // Known messages we don't care about
            if (message.Contains ("Unable to locate theme engine in module_path:") ||
                message.Contains ("Unsupported unit"))
                return;

            string msg = string.Format ("{0}-{1}: {2}",
                logDomain, logLevel, message);

            switch (logLevel) {
                case LogLevelFlags.Debug:
                case LogLevelFlags.Info:
                    ErrorHandling.LogError (msg, ErrorSeverity.Information);
                    break;
                case LogLevelFlags.Warning:
                    ErrorHandling.LogError (msg, ErrorSeverity.Warning);
                    break;
                default:
                    ErrorHandling.LogError (msg);
                    break;
            }
        }

        private static void OnUnhandledException (UnhandledExceptionArgs args)
        {
            if (exceptionHandler != null)
                exceptionHandler (args);
            else
                args.ExitApplication = true;
        }

        #endregion
    }
}
