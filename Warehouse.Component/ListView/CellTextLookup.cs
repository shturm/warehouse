//
// CellTextLookup.cs
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
using System.Collections.Generic;

namespace Warehouse.Component.ListView
{
    public class CellTextLookup<T> : CellText
    {
        private Dictionary<T, string> lookup = new Dictionary<T, string> ();

        public Dictionary<T, string> Lookup
        {
            get { return lookup; }
            set { lookup = value; }
        }

        public CellTextLookup (string propertyName)
            : base (propertyName)
        {
        }

        public override string ObjectToString (object obj)
        {
            if (obj == null)
                return string.Empty;

            T key;
            try {
                key = (T) Convert.ChangeType (obj, typeof (T));
            } catch (Exception) {
                return string.Empty;
            }

            string ret;
            return lookup.TryGetValue (key, out ret) ? ret : string.Empty;
        }

        protected override string ObjectToEditString (object obj)
        {
            return ObjectToString (obj);
        }

        protected override object ParseObject (string text)
        {
            if (!lookup.ContainsValue (text))
                return default (T);

            foreach (KeyValuePair<T, string> pair in lookup) {
                if (pair.Value == text)
                    return pair.Key;
            }

            return default (T);
        }

        public CellTextLookup<T> Load (IEnumerable<KeyValuePair<T, string>> values)
        {
            foreach (KeyValuePair<T, string> pair in values)
                lookup.Add (pair.Key, pair.Value);

            return this;
        }
    }
}
