//
// EmailResult.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   11.13.2009
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

namespace Warehouse.Business
{
    /// <summary>
    /// Contains values representing the outcome of sending an e-mail.
    /// </summary>
    public enum EmailResult
    {
        /// <summary>
        /// The e-mail was successfully sent.
        /// </summary>
        Success,
        /// <summary>
        /// No SMTP server is specified.
        /// </summary>
        NoSmtpServer,
        /// <summary>
        /// No sender of the e-mail is specified.
        /// </summary>
        NoSender,
        /// <summary>
        /// The specified port has an invalid value.
        /// </summary>
        InvalidPort,
        /// <summary>
        /// An SMTP-related error occurred while trying to send the e-mail.
        /// </summary>
        SmtpError
    }
}
