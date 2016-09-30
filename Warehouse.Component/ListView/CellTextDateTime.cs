//
// CellTextDateTime.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   03.30.2010
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
using Warehouse.Business;

namespace Warehouse.Component.ListView
{
    public class CellTextDateTime : CellTextDate
    {
        public CellTextDateTime (string propertyName)
            : base (propertyName)
        {
        }

        protected override object ParseObject (string text)
        {
            try {
                DateTime date;

                if (string.IsNullOrEmpty (editDateFormat))
                    DateTime.TryParse (text, out date);
                else
                    try {
                        if (!DateTime.TryParseExact (text, editDateFormat + " HH:mm", null, DateTimeStyles.None, out date))
                            return DateTime.MinValue;
                    } catch {
                        date = DateTime.MinValue;
                    }
                return date;
            } catch {
                return new DateTime (2000, 1, 1);
            }
        }

        protected override string GetFormattedDate (DateTime date, string format)
        {
            if (string.IsNullOrEmpty (format))
                return date.ToShortDateString () + " " + date.ToShortTimeString ();

            try {
                return date.ToString (format + " HH:mm", null);
            } catch {
                return string.Empty;
            }
        }
    }
}
