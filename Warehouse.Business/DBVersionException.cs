//
// DBVersionException.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   02/23/2007
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

namespace Warehouse.Business
{
    public class DBVersionException : Exception
    {
        private string providerVersion;
        private string databaseVersion;
        private bool upgradeable;

        public string ProviderVersion
        {
            get { return providerVersion; }
            set { providerVersion = value; }
        }

        public string DatabaseVersion
        {
            get { return databaseVersion; }
            set { databaseVersion = value; }
        }

        public bool Upgradeable
        {
            get { return upgradeable; }
            set { upgradeable = value; }
        }

        public DBVersionException (string providerVer, string databaseVer)
        {
            providerVersion = providerVer;
            databaseVersion = databaseVer;
        }

        public override string Message
        {
            get
            {
                return string.Format ("The database is version {0} but the data provider is version {1}. Upgradeable: {2}.", databaseVersion, providerVersion, upgradeable);
            }
        }
    }
}
