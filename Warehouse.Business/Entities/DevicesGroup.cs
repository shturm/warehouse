//
// DevicesGroup.cs
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

using System.Linq;
using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public class DevicesGroup : GroupBase<DevicesGroup>
    {
        #region Public fields

        public override long Id
        {
            get { return id; }
            set { id = value; }
        }

        public override string Name
        {
            get { return name; }
            set { name = value; }
        }

        public override string Code
        {
            get { return code; }
            set { code = value; }
        }

        #endregion

        public DevicesGroup ()
        {
        }

        private DevicesGroup (DeviceType type)
        {
            id = (int) type;
            name = Translator.GetDevicesGroup (type);
        }

        public static DevicesGroup [] GetAll ()
        {
            return Device.GetAllUsedTypes ().Select (type => new DevicesGroup (type)).ToArray ();
        }
    }
}
