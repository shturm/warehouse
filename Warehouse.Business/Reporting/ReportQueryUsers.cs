//
// ReportQueryUsers.cs
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

using Warehouse.Data;

namespace Warehouse.Business.Reporting
{
    public class ReportQueryUsers : ReportQueryBase
    {
        public ReportQueryUsers ()
        {
            Name = Translator.GetString ("Users Report");

            AppendFilter (new FilterFind (true, true, DataFilterLabel.UserName, DataField.UserName));
            AppendFilter (new FilterFind (true, true, DataFilterLabel.UserCode, DataField.UserCode));
            AppendFilter (new FilterFind (false, true, DataFilterLabel.UserLevel, DataField.UserLevel));
            AppendFilter (new FilterGroupFind (false, true, DataFilterLabel.UsersGroupsName, DataField.UsersGroupsName));
            AppendOrder (new Order (
                DataField.UserName,
                DataField.UserCode,
                DataField.UserLevel));
        }

        #region Overrides of ReportQueryBase

        public override DataQueryResult ExecuteReport (DataQuery dataQuery)
        {
            return BusinessDomain.DataAccessProvider.ReportUsers (dataQuery);
        }

        #endregion
    }
}
