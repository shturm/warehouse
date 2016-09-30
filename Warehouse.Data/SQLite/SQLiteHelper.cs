//
// SQLiteHelper.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   08.09.2012
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

namespace Warehouse.Data.SQLite
{
    public class SQLiteHelper : SqlHelper
    {
        public override string CurrentDateTimeFunction
        {
            get { return "DATETIME('now')"; }
        }

        public override string CurrentDateFunction
        {
            get { return "DATE('now')"; }
        }

        internal SQLiteHelper (FieldTranslationCollection fTrans)
            : base (fTrans)
        {
        }
    }
}
