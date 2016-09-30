//
// Order.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   09.30.2010
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

using System.Collections.Generic;
using Warehouse.Data;

namespace Warehouse.Business.Reporting
{
    public class Order
    {
        private KeyValuePair<DbField, string> [] choices;
        private DbField selection;
        private SortDirection direction;

        public KeyValuePair<DbField, string> [] Choices
        {
            get { return choices; }
            set
            {
                if (value != null) {
                    List<KeyValuePair<DbField, string>> newChoices = new List<KeyValuePair<DbField, string>> (value);
                    if (newChoices.FindIndex (i => i.Key == DataField.NotSet) == -1) {
                        newChoices.Insert (0, new KeyValuePair<DbField, string> (DataField.NotSet, Translator.GetReportFieldColumnName (DataField.NotSet)));
                        choices = newChoices.ToArray ();
                        return;
                    }
                }

                choices = value;
            }
        }

        public DbField Selection
        {
            get { return selection; }
            set { selection = value; }
        }

        public SortDirection Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        public Order (params DbField [] fields)
        {
            Choices = Translator.GetReportFieldColumnNames (fields);
        }

        public void LoadOrder (KeyValuePair<DbField, string> [] allChoices, DbField currentSelection, SortDirection currentDirection)
        {
            Choices = allChoices;
            selection = currentSelection;
            direction = currentDirection;
        }

        public void ClearOrder ()
        {
            selection = choices [0].Key;
            direction = SortDirection.Ascending;
        }
    }
}
