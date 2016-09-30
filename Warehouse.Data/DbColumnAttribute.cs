//
// DbColumnAttribute.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   06/19/2006
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

namespace Warehouse.Data
{
    /// <summary>
    /// This is a Business Object DataBase Mepped Member attribute
    /// it is used to map certain business object field to a column in a database.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property | AttributeTargets.Field)]
    public class DbColumnAttribute : Attribute
    {
        private readonly int? maxLength;
        private readonly DbField dbField;

        public DbField DbField
        {
            get { return dbField; }
        }

        public int? MaxLength
        {
            get { return maxLength; }
        }

        public bool ReadOnly { get; set; }

        public DbColumnAttribute (DataField strongField)
        {
            dbField = new DbField (strongField);
        }

        public DbColumnAttribute (DataField strongField, bool readOnly)
        {
            dbField = new DbField (strongField);
            ReadOnly = readOnly;
        }

        public DbColumnAttribute (DataField strongField, int maxLength)
        {
            this.maxLength = maxLength;
            dbField = new DbField (strongField);
        }

        public DbColumnAttribute (string field)
        {
            dbField = new DbField (field);
        }

        public DbColumnAttribute (string field, int maxLength)
        {
            this.maxLength = maxLength;
            dbField = new DbField (field);
        }
    }
}
