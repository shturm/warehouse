//
// FormMemberMappingAttribute.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/04/2007
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

namespace Warehouse.Business.Documenting
{
    /// <summary>
    /// This is a Business Object DataBase Mepped Member attribute
    /// it is used to map certain business object field to a column in a database.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property | AttributeTargets.Field)]
    public class FormMemberMappingAttribute : Attribute
    {
        private readonly string reportField;
        private readonly FormMemberType type;

        public string ReportField
        {
            get { return reportField; }
        }

        public FormMemberType Type
        {
            get { return type; }
        }

        public FormMemberMappingAttribute ([NotNull] string reportField, FormMemberType type = FormMemberType.Default)
        {
            if (string.IsNullOrWhiteSpace (reportField))
                throw new ArgumentNullException ("reportField");

            this.reportField = reportField;
            this.type = type;
        }
    }
}
