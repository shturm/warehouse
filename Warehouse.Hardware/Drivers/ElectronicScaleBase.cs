//
// ElectronicScaleBase.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   11.07.2009
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
    /// <summary>
    /// Represents a driver for an electronic scale that connects to the PC.
    /// </summary>
    public abstract class ElectronicScaleBase<TCon> : DriverBase<TCon>, IElectronicScaleController where TCon : DeviceConnector, new ()
    {
        public override void Disconnect ()
        {
            if (Connector != null && Connector.IsConnected)
                Connector.Dispose ();
        }

        #region IElectronicScaleController Members

        public virtual void GetWeight (out double weight)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetPrice (out double price)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetAmount (out double amount)
        {
            throw new NotSupportedException ();
        }

        public virtual void SetPrice (double price)
        {
            throw new NotSupportedException ();
        }

        public virtual void GetWeightAndSetPrice (out double weight, double price)
        {
            throw new NotSupportedException ();
        }

        #endregion
    }
}
