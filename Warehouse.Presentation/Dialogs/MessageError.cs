//
// MessageError.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   06/24/2006
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
using Gtk;
using Warehouse.Business;

namespace Warehouse.Presentation.Dialogs
{
    public class MessageError : Message
    {
        public MessageError (string message, ErrorSeverity severity = ErrorSeverity.Warning, Exception ex = null)
        {
            InitializeForm (message, severity, ex, null);
        }

        public MessageError (string message, string dialogIcon, ErrorSeverity severity, Exception ex)
        {
            InitializeForm (message, severity, ex, dialogIcon);
        }

        private void InitializeForm (string msg, ErrorSeverity severity, Exception ex, string icon)
        {
            if (ex != null)
                ErrorHandling.LogException (ex, severity);
            else if (!string.IsNullOrEmpty (msg))
                ErrorHandling.LogError (msg, severity);

            title = GetDialogTitleFromSeverity (severity);
            dialogIcon = icon;
            message = msg;
            messageIcon = GetDialogIconFromSeverity (severity);

            base.InitializeForm ();
        }

        private static string GetDialogTitleFromSeverity (ErrorSeverity severity)
        {
            switch (severity) {
                case ErrorSeverity.Information:
                    return Translator.GetString ("Information");
                case ErrorSeverity.Warning:
                    return Translator.GetString ("Warning!");
                case ErrorSeverity.Error:
                    return Translator.GetString ("Error!");
                case ErrorSeverity.FatalError:
                    return Translator.GetString ("Fatal error!");
            }

            return string.Empty;
        }

        public static string GetDialogIconFromSeverity (ErrorSeverity severity)
        {
            switch (severity) {
                case ErrorSeverity.Information:
                    return "Icons.Info32.png";
                case ErrorSeverity.Warning:
                    return "Icons.Warning32.png";
                case ErrorSeverity.Error:
                    return "Icons.Error32.png";
                case ErrorSeverity.FatalError:
                    return "Icons.Error32.png";
            }

            return string.Empty;
        }

        public static ResponseType ShowDialog (string message, ErrorSeverity severity = ErrorSeverity.Warning, Exception ex = null, MessageButtons buttons = MessageButtons.OK)
        {
            using (MessageError msg = new MessageError (message, severity, ex) { Buttons = buttons })
                return msg.Run ();
        }

        public static ResponseType ShowDialog (string message, string dialogIcon, ErrorSeverity severity = ErrorSeverity.Warning, Exception ex = null)
        {
            using (MessageError msg = new MessageError (message, dialogIcon, severity, ex))
                return msg.Run ();
        }
    }
}
