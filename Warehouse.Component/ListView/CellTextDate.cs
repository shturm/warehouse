//
// CellTextDate.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   10/18/2007
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
    public class CellTextDate : CellText
    {
        private string dateFormat;
        protected string editDateFormat;

        public string DateFormat
        {
            get { return dateFormat; }
            set { dateFormat = value; }
        }

        public string EditDateFormat
        {
            get { return editDateFormat; }
            set { editDateFormat = value; }
        }

        public CellTextDate (string propertyName)
            : base (propertyName)
        {
            dateFormat = BusinessDomain.AppConfiguration.DateFormat;
        }

        public override string ObjectToString (object obj)
        {
            if (obj == null || obj.GetType () != typeof (DateTime))
                return string.Empty;

            try {
                return GetFormattedDate ((DateTime) obj, dateFormat);
            } catch {
                return "??/??/????";
            }
        }

        protected override string ObjectToEditString (object obj)
        {
            if (obj == null || obj.GetType () != typeof (DateTime))
                return string.Empty;

            try {
                return GetFormattedDate ((DateTime) obj, editDateFormat);
            } catch {
                return "??/??/????";
            }
        }

        protected override object ParseObject (string text)
        {
            try {
                DateTime date;

                if (string.IsNullOrEmpty (editDateFormat))
                    DateTime.TryParse (text, out date);
                else
                    try {
                        if (!DateTime.TryParseExact (text, editDateFormat, null, DateTimeStyles.None, out date))
                            return DateTime.MinValue;
                    } catch {
                        date = DateTime.MinValue;
                    }
                return date;
            } catch {
                return new DateTime (2000, 1, 1);
            }
        }

        protected virtual string GetFormattedDate (DateTime date, string format)
        {
            if (string.IsNullOrEmpty (format))
                return date.ToShortDateString ();

            try {
                return date.ToString (format, null);
            } catch {
                return string.Empty;
            }
        }
    }
}
