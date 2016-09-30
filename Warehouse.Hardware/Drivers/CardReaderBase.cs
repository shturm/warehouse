//
// CardReaderBase.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   10/28/2008
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
using Warehouse.Business.Devices;

namespace Warehouse.Hardware.Drivers
{
    public abstract class CardReaderBase<TCon> : DriverBase<TCon>, ICardReaderController where TCon : DeviceConnector, new ()
    {
        #region Private fields

        protected EventHandler<CardReadArgs> cardRecognized;

        #endregion

        #region ICardReaderController Members

        public event EventHandler<CardReadArgs> CardRecognized
        {
            add { cardRecognized += value; }
            remove { cardRecognized -= value; }
        }

        #endregion
    }
}
