//
// VisualizerSettingsCollection.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   06/21/2009
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

namespace Warehouse.Presentation.Visualization
{
    public class VisualizerSettingsCollection : List<VisualizerSettingsWraper>, ICloneable
    {
        public static readonly Type DefaultVisualizerType = typeof (TableVisualizer);
        private bool isDirty;

        public bool IsDirty
        {
            get
            {
                if (isDirty)
                    return true;

                foreach (VisualizerSettingsWraper wraper in this) {
                    if (wraper.Settings.IsDirty)
                        return true;
                }

                return false;
            }
            set
            {
                isDirty = value;

                if (value)
                    return;

                foreach (VisualizerSettingsWraper wraper in this) {
                    wraper.Settings.IsDirty = false;
                }
            }
        }

        public DataQueryVisualizer GetVisualizerInstance ()
        {
            return Activator.CreateInstance (GetVisualizerType ()) as DataQueryVisualizer;
        }

        public Type GetVisualizerType ()
        {
            if (!ContainsSettings (typeof (CurrentVisualizerSettings)))
                return DefaultVisualizerType;

            CurrentVisualizerSettings curVisualizer = GetSettings<CurrentVisualizerSettings> ();
            if (curVisualizer == null || string.IsNullOrEmpty (curVisualizer.VisualizerTypeName))
                return DefaultVisualizerType;

            return Type.GetType (curVisualizer.VisualizerTypeName) ?? DefaultVisualizerType;
        }

        public bool ContainsSettings (Type settingsType)
        {
            for (int i = 0; i < Count; i++) {
                if (this [i].Settings.GetType () == settingsType)
                    return true;
            }

            return false;
        }

        public T GetSettings<T> () where T : VisualizerSettingsBase
        {
            for (int i = 0; i < Count; i++) {
                if (this [i].Settings.GetType () == typeof (T))
                    return (T) this [i].Settings;
            }

            return null;
        }

        public void SetSettings<T> (T settings) where T : VisualizerSettingsBase
        {
            for (int i = 0; i < Count; i++) {
                if (this [i].Settings.GetType () != typeof (T))
                    continue;

                if (!ReferenceEquals (this [i].Settings, settings)) {
                    this [i].Settings = settings;
                    isDirty = true;
                }
                return;
            }

            Add (new VisualizerSettingsWraper (settings));
            isDirty = true;
        }

        public object Clone ()
        {
            VisualizerSettingsCollection ret = new VisualizerSettingsCollection ();
            foreach (VisualizerSettingsWraper wraper in this) {
                ret.Add ((VisualizerSettingsWraper) wraper.Clone ());
            }
            ret.isDirty = isDirty;

            return ret;
        }
    }
}
