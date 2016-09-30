//
// Validator.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   11.04.2009
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
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Warehouse.Business
{
    /// <summary>
    /// Contains methods that check specific types of values for validity.
    /// </summary>
    public static class Validator
    {
        /// <summary>
        /// Checks if the specified parameter is a valid e-mail address.
        /// </summary>
        /// <param name="email">The e-mail to check for validity.</param>
        /// <returns>A value indicating if the specified parameter is a valid e-mail address.</returns>
        public static bool CheckEmail (string email)
        {
            return Regex.IsMatch (email, @"^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Checks if the specified parameter is a valid internet address.
        /// </summary>
        /// <param name="internetAddress">The internet address to check for validity.</param>
        /// <returns>A value indicating if the specified parameter is a valid internet address.</returns>
        public static bool CheckInternetAddress (string internetAddress)
        {
            Uri uri;
            return Uri.TryCreate (internetAddress, UriKind.RelativeOrAbsolute, out uri);
        }

        /// <summary>
        /// Checks if the specified parameter is a valid Internet protocol.
        /// </summary>
        /// <param name="ip">The IP to check for validity.</param>
        /// <returns>A value indicating if the specified parameter is a valid IP.</returns>
        public static bool CheckIP (string ip)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse (ip, out ipAddress)) {
                AddressFamily addressFamily = ipAddress.AddressFamily;
                return addressFamily == AddressFamily.InterNetwork || addressFamily == AddressFamily.InterNetworkV6;
            } 
            return false;
        }

        /// <summary>
        /// Validates the string setting containing the used bank notes and coins.
        /// </summary>
        /// <param name="bankNotesAndCoins">The bank notes and coins to validate.</param>
        /// <param name="userInput">Determines if the value to check is user input.</param>
        /// <returns>
        /// A value indicating if the string containing the used bank notes and coins is valid.
        /// </returns>
        public static bool ValidateBankNotesAndCoins (string bankNotesAndCoins, bool userInput = false)
        {
            string [] values = bankNotesAndCoins.Split (new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length == 0)
                return false;

            foreach (string value in values) {
                double result;
                if (!Entities.Currency.TryParseExpression(value, out result))
                    return false;

                if (!ConfigurationHolderBase.AllowedBankNotesAndCoins.Contains (result))
                    return false;
            }
            return true;
        }
    }
}
