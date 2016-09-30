//
// RSAKeyInfo.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/23/2008
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

namespace Warehouse.Business.Licensing
{
    public class RSAKeyInfo
    {
        private readonly DateTime? expirationDate;
        private readonly int signLength;
        private readonly string xmlKey;

        public DateTime? ExpirationDate
        {
            get { return expirationDate; }
        }

        public int SignLength
        {
            get { return signLength; }
        }

        public string XmlKey
        {
            get { return xmlKey; }
        }

        public RSAKeyInfo(int signLen, string xmlKey, DateTime? expirationDate)
        {
            signLength = signLen;
            this.xmlKey = xmlKey;
            this.expirationDate = expirationDate;
        }
    }
}
