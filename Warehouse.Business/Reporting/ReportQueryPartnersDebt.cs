//
// ReportQueryPartnersDebt.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   06.03.2013
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
    public class ReportQueryPartnersDebt : ReportQueryBase
    {
        public ReportQueryPartnersDebt ()
        {
            Name = Translator.GetString ("Partners Debt Report");

            AppendFilter (new FilterFind (false, true, DataFilterLabel.PartnerCode, DataField.PartnerCode));
            AppendFilter (new FilterFind (true, true, DataFilterLabel.PartnerName, DataField.PartnerName));
            AppendFilter (new FilterFind (false, true, DataFilterLabel.PartnerCity, DataField.PartnerCity));
            AppendFilter (new FilterFind (false, true, DataFilterLabel.PartnerLiablePerson, DataField.PartnerLiablePerson));
            AppendFilter (new FilterGroupFind (false, true, DataFilterLabel.PartnersGroupsName, DataField.PartnersGroupsName));
            AppendOrder (new Order (
                DataField.PartnerName,
                DataField.PartnerCode,
                DataField.PartnerBulstat));
        }

        #region Overrides of ReportQueryBase

        public override DataQueryResult ExecuteReport (DataQuery dataQuery)
        {
            return BusinessDomain.DataAccessProvider.ReportPartnersDebt (dataQuery);
        }

        #endregion
    }
}
