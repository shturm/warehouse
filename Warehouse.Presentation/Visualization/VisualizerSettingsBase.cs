//
// VisualizerSettingsBase.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   06/22/2009
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
using System.Xml.Serialization;

namespace Warehouse.Presentation.Visualization
{
    public enum DateUnits
    {
        Day,
        Week,
        Month,
        Year,
        None
    }

    public abstract class VisualizerSettingsBase : ICloneable
    {
        protected bool useCustomAutoRefresh;
        protected int customAutoRefreshInterval;
        protected bool isDirty;

        public bool UseCustomAutoRefresh
        {
            get { return useCustomAutoRefresh; }
            set
            {
                if (useCustomAutoRefresh == value)
                    return;

                useCustomAutoRefresh = value;
                isDirty = true;
            }
        }

        public int CustomAutoRefreshInterval
        {
            get { return customAutoRefreshInterval; }
            set
            {
                if (customAutoRefreshInterval == value)
                    return;

                customAutoRefreshInterval = value;
                isDirty = true;
            }
        }

        [XmlIgnore]
        public bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }

        public abstract object Clone ();
    }
}
