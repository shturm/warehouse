//
// QuickItemsCollection.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   12/27/2007
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
using System.IO;
using System.Xml;

namespace Warehouse.Business
{
    public class QuickItemsCollection : Dictionary<string, string>
    {
        public void Load (string fileName)
        {
            if (string.IsNullOrEmpty (fileName))
                return;

            if (!File.Exists (fileName))
                return;

            XmlDocument xmlDoc = new XmlDocument ();
            xmlDoc.Load (fileName);

            XmlNodeList nodes = xmlDoc.SelectNodes (@"/quickgoods/shortcut");

            if (nodes == null || nodes.Count == 0)
                return;

            foreach (XmlNode node in nodes)
            {
                XmlNode keyNode = node.Attributes.GetNamedItem ("key");
                if (keyNode == null)
                    continue;

                XmlNode itemNode = node.Attributes.GetNamedItem ("goods");
                if (itemNode == null)
                    continue;

                if (ContainsKey (keyNode.Value))
                    continue;

                Add (keyNode.Value, itemNode.Value);
            }
        }
    }
}
