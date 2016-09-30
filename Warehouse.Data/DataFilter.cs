//
// DataFilter.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   08/15/2006
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
using System.Xml.Serialization;

namespace Warehouse.Data
{
    public class DataFilter : ICloneable
    {
        #region Private fields

        private DbField [] filteredFields;
        private DataFilterLogic logic = DataFilterLogic.ExactMatch;
        private List<string> sqlTranslation = new List<string> ();
        private bool showColumns = true;
        private bool isValid = true;
        private object [] values;

        #endregion

        #region Public properties

        [XmlIgnore]
        public DbField [] FilteredFields
        {
            get { return filteredFields; }
            set { filteredFields = value; }
        }

        public DataFilterLogic Logic
        {
            get { return logic; }
            set { logic = value; }
        }

        [XmlIgnore]
        public List<string> SQLTranslation
        {
            get { return sqlTranslation; }
            set { sqlTranslation = value; }
        }

        public ConditionCombineLogic CombineLogic { get; set; }

        public bool ShowColumns
        {
            get { return showColumns; }
            set { showColumns = value; }
        }

        [XmlIgnore]
        public bool IsValid
        {
            get { return isValid; }
            set { isValid = value; }
        }

        public object [] Values
        {
            get { return values ?? (values = new object [0]); }
            set { values = value; }
        }

        #endregion

        public DataFilter ()
        {
            filteredFields = new DbField [0];
        }

        public DataFilter (params DbField [] fields)
        {
            if (fields == null || fields.Length <= 0) {
                filteredFields = new DbField [0];
                return;
            }

            filteredFields = new DbField [fields.Length];
            fields.CopyTo (filteredFields, 0);
        }

        public DataFilter (DataFilterLogic logic, params DbField [] fields)
            : this (fields)
        {
            this.logic = logic;
        }

        #region Implementation of ICloneable

        public virtual object Clone ()
        {
            return MemberwiseClone ();
        }

        #endregion
    }
}
