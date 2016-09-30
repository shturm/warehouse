//
// VisualizerSettingsWraper.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   07/10/2009
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
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Warehouse.Presentation.Visualization
{
    public class VisualizerSettingsWraper : ICloneable, IXmlSerializable
    {
        public VisualizerSettingsBase Settings { get; set; }

        public VisualizerSettingsWraper ()
        {
        }

        public VisualizerSettingsWraper (VisualizerSettingsBase settings)
        {
            if (settings == null)
                throw new ArgumentNullException ("settings");

            Settings = settings;
        }

        public object Clone ()
        {
            if (Settings == null)
                return new VisualizerSettingsWraper ();
            
            return new VisualizerSettingsWraper ((VisualizerSettingsBase) Settings.Clone ());
        }

		#region IXmlSerializable Implementation

		public XmlSchema GetSchema ()
		{
			return null;
		}

		public void ReadXml (XmlReader reader)
		{
			Type type = null;
			string typeName = reader.GetAttribute ("type");
			if (!string.IsNullOrEmpty (typeName))
				type = Type.GetType (typeName);
			
			reader.ReadStartElement ();
			// backward compatibility
			bool oldFormat = type == null;
			if (oldFormat) {
				type = Type.GetType (reader.GetAttribute ("type"));
				reader.ReadStartElement ();
			}
			Settings = (VisualizerSettingsBase) new XmlSerializer (type).Deserialize (reader);
			if (oldFormat)
				reader.ReadEndElement ();
			reader.ReadEndElement ();
		}

		public void WriteXml (XmlWriter writer)
		{
			writer.WriteAttributeString ("type", Settings.GetType ().AssemblyQualifiedName);
			new XmlSerializer (Settings.GetType ()).Serialize (writer, Settings);
		}

		#endregion IXmlSerializable Implementation

		public static implicit operator VisualizerSettingsWraper (VisualizerSettingsBase p)
		{
			return p == null ? null : new VisualizerSettingsWraper (p);
		}

		public static implicit operator VisualizerSettingsBase (VisualizerSettingsWraper p)
		{
			return p == null ? null : p.Settings;
		}
    }
}
