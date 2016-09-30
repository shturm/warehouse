//
// DbParam.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   10.12.2007
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

using System.Data;

namespace Warehouse.Data
{
    public class DbParam
    {
        private string parameterName;
        private object value;
        private ParameterDirection direction = ParameterDirection.Input;

        public string ParameterName
        {
            get { return parameterName; }
            set { parameterName = value; }
        }

        public object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public ParameterDirection Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        public DbParam (string parameterName, object value)
        {
            this.parameterName = parameterName;
            this.value = value;
        }

        public override string ToString ()
        {
            return string.Format ("{0} = {1}", parameterName, value);
        }
    }
}
