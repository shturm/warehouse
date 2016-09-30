//
// PlatformHelper.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   12.15.2014
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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Warehouse.Data
{
    public enum PlatformTypes
    {
        Windows = 0,
        Linux = 1,
        MacOSX = 2,
        Android
    }

    public static class PlatformHelper
    {
        private static PlatformTypes? platform;

        public static PlatformTypes Platform
        {
            get
            {
                if (platform.HasValue)
                    return platform.Value;

                if (Path.DirectorySeparatorChar == '\\') {
                    platform = PlatformTypes.Windows;
                    return platform.Value;
                }

                StringBuilder ret = new StringBuilder ();
                if (RunApplication ("uname", "-s", false, null, ret, true) != "0") {
                    platform = PlatformTypes.Linux;
                    return platform.Value;
                }

                platform = ret.ToString ().StartsWith ("Darwin") ? PlatformTypes.MacOSX : PlatformTypes.Linux;
                return platform.Value;
            }
            set { platform = value; }
        }

        public static bool IsWindows
        {
            get { return Path.DirectorySeparatorChar == '\\'; }
        }

        private static string runtimeVersion;

        public static string RuntimeVersion
        {
            get
            {
                if (runtimeVersion == null) {
                    Type type = Type.GetType ("Mono.Runtime");
                    if (type != null) {
                        MethodInfo displayName = type.GetMethod ("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                        if (displayName != null)
                            runtimeVersion = displayName.Invoke (null, null).ToString ();
                        else
                            runtimeVersion = "Mono " + Environment.Version;
                    } else
                        runtimeVersion = "MS.NET " + Environment.Version;
                }

                return runtimeVersion;
            }
        }

        private static readonly AutoResetEvent appStartEvent = new AutoResetEvent (false);
        private static readonly AutoResetEvent appFinishEvent = new AutoResetEvent (false);
        private static string appFinishResult;
        private static StringBuilder appFinishOutput;

        public static string RunApplication (string fileName, string args, bool isDotNet = true, string workingDir = null, StringBuilder output = null, bool blocking = false)
        {
            if (string.IsNullOrEmpty (workingDir))
                workingDir = Path.GetDirectoryName (Path.GetFullPath (fileName));

            Thread starter = new Thread (ProcessStarter);
            appStartEvent.Reset ();
            appFinishEvent.Reset ();
            appFinishOutput = output;
            appFinishResult = "0";
            starter.Start (new object [] { fileName, args, workingDir, blocking, isDotNet });
            appStartEvent.WaitOne ();
            if (blocking)
                appFinishEvent.WaitOne ();

            return appFinishResult;
        }

        private static void ProcessStarter (object parameters)
        {
            object [] pars = (object []) parameters;
            string fileName = (string) pars [0];
            string args = (string) pars [1];
            string workingDir = (string) pars [2];
            bool blocking = (bool) pars [3];
            bool isDotNet = (bool) pars [4];

            try {
                Process process = new Process ();
                if (IsWindows)
                    process.StartInfo = new ProcessStartInfo (fileName, args);
                else if (isDotNet && Platform == PlatformTypes.Linux)
                    process.StartInfo = new ProcessStartInfo ("mono", "--debug " + fileName.Replace (" ", @"\ ") + " " + args);
                else
                    process.StartInfo = new ProcessStartInfo (fileName, args);
                process.StartInfo.WorkingDirectory = workingDir;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.CreateNoWindow = true;

                if (blocking) {
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.OutputDataReceived += outputDataReceived_static;
                    process.ErrorDataReceived += errorDataReceived_static;
                    process.EnableRaisingEvents = true;
                }

                try {
                    process.Start ();
                } catch (Exception ex) {
                    appFinishResult = ex.ToString ();
                    throw;
                } finally {
                    appStartEvent.Set ();
                }

                if (blocking) {
                    process.BeginErrorReadLine ();
                    process.BeginOutputReadLine ();
                    process.WaitForExit ();
                    appFinishResult = process.ExitCode.ToString ();
                    process.Close ();
                }
            } catch (Exception) {
            } finally {
                appFinishEvent.Set ();
            }
        }

        private static void errorDataReceived_static (object sender, DataReceivedEventArgs e)
        {
            if (String.IsNullOrEmpty (e.Data))
                return;

            if (appFinishOutput == null)
                return;

            appFinishOutput.AppendFormat ("{0}{1}", e.Data, Environment.NewLine);
        }

        private static void outputDataReceived_static (object sender, DataReceivedEventArgs e)
        {
            if (String.IsNullOrEmpty (e.Data))
                return;

            if (appFinishOutput == null)
                return;

            appFinishOutput.AppendFormat ("{0}{1}", e.Data, Environment.NewLine);
        }
    }
}
