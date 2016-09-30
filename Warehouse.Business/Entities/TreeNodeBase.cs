//
// TreeNodeBase.cs
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

namespace Warehouse.Business.Entities
{
    [Serializable]
    public abstract class TreeNodeBase<TNode, TValue> : ICloneable where TNode : TreeNodeBase<TNode, TValue>, new ()
    {
        protected string name;
        protected TValue value;
        protected List<TNode> children = new List<TNode> ();

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public TValue Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public List<TNode> Children
        {
            get { return children; }
        }

        protected TreeNodeBase (string name, TValue value, params TNode [] children)
        {
            this.name = name;
            this.value = value;
            this.children.AddRange (children);
        }

        public TNode FindNode (string nodeName)
        {
            if (name == nodeName)
                return this as TNode;

            foreach (TNode node in children) {
                if (node.Name == nodeName)
                    return node;

                TNode foundNode = node.FindNode (nodeName);
                if (!Equals (foundNode, default (TNode)))
                    return foundNode;
            }

            return null;
        }

        public bool RemoveNode (string nodeName)
        {
        	for (int i = children.Count - 1; i >= 0; i--) {
				if (children [i].Name == nodeName) {
					children.RemoveAt (i);
					return true;
				}
				if (children [i].RemoveNode (nodeName))
					return true;
			}
			return false;
        }

        public bool InsertBefore (string searchedName, params TNode [] newNodes)
        {
            for (int i = 0; i < children.Count; i++) {

                if (children [i].Name == searchedName) {
                    for (int j = 0; j < newNodes.Length; j++)
                        children.Insert (i + j, newNodes [j]);
                    
                    return true;
                }

                if (children [i].InsertBefore (searchedName, newNodes))
                    return true;
            }

            return false;
        }

        public bool InsertAfter (string searchedName, params TNode [] newNodes)
        {
            for (int i = 0; i < children.Count; i++) {

                if (children [i].Name == searchedName) {
                    for (int j = 0; j < newNodes.Length; j++)
                        children.Insert (i + j + 1, newNodes [j]);

                    return true;
                }

                if (children [i].InsertAfter (searchedName, newNodes))
                    return true;
            }

            return false;
        }

        #region Implementation of ICloneable

        public virtual object Clone ()
        {
            List<TNode> newChildren = new List<TNode> ();
            foreach (TNode child in children) {
                newChildren.Add ((TNode) child.Clone ());
            }

            return new TNode { name = name, Value = value, children = newChildren };
        }

        #endregion
    }
}
