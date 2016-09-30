//
// CellTextDouble.cs
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

using Warehouse.Data;
using Warehouse.Data.Calculator;

namespace Warehouse.Component.ListView
{
    public class CellTextDouble : CellText
    {
        private int fixedFaction = -1;
        private int editFixedFraction = -1;

        public int FixedFaction
        {
            get { return fixedFaction; }
            set { fixedFaction = value; }
        }

        public int EditFixedFraction
        {
            get { return editFixedFraction; }
            set { editFixedFraction = value; }
        }

        public CellTextDouble (string propertyName)
            : base (propertyName)
        {
            Alignment = Pango.Alignment.Right;
        }

        public override string ObjectToString (object obj)
        {
            double number = ObjectToDouble (obj);

            return Number.ToString (number, fixedFaction);
        }

        protected override string ObjectToEditString (object obj)
        {
            double number = ObjectToDouble (obj);

            return Number.ToEditString (number, editFixedFraction);
        }

        protected static double ObjectToDouble (object obj)
        {
            if (obj == null)
                return 0;

            string source = obj.ToString ();
            if (string.IsNullOrEmpty (source))
                return 0;

            double number;
            double.TryParse (source, out number);
            return number;
        }

        protected override object ParseObject (string text)
        {
            return string.IsNullOrEmpty (text) ? 0 : Number.ParseExpression (text);
        }
    }
}
