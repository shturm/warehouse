//
// SerializableDictionary.cs
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

using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Warehouse.Data
{
    [XmlRoot ("dictionary")]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        #region IXmlSerializable Members

        public XmlSchema GetSchema ()
        {
            return null;
        }

        public void ReadXml (XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer (typeof (TKey));
            XmlSerializer valueSerializer = new XmlSerializer (typeof (TValue));

            bool wasEmpty = reader.IsEmptyElement;
            reader.Read ();

            if (wasEmpty)
                return;

            while (reader.NodeType != XmlNodeType.EndElement) {
                reader.ReadStartElement ("item");

                reader.ReadStartElement ("key");
                TKey key = (TKey) keySerializer.Deserialize (reader);
                reader.ReadEndElement ();

                reader.ReadStartElement ("value");
                TValue value = (TValue) valueSerializer.Deserialize (reader);
                reader.ReadEndElement ();

                Add (key, value);

                reader.ReadEndElement ();
                reader.MoveToContent ();
            }
            reader.ReadEndElement ();
        }

        public void WriteXml (XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer (typeof (TKey));
            XmlSerializer valueSerializer = new XmlSerializer (typeof (TValue));

            foreach (TKey key in Keys) {
                writer.WriteStartElement ("item");

                writer.WriteStartElement ("key");
                keySerializer.Serialize (writer, key);
                writer.WriteEndElement ();

                writer.WriteStartElement ("value");
                TValue value = this [key];
                valueSerializer.Serialize (writer, value);
                writer.WriteEndElement ();

                writer.WriteEndElement ();
            }
        }

        #endregion
    }
}
