//
// GroupBase.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DbTransaction = Warehouse.Data.DbTransaction;

namespace Warehouse.Business.Entities
{
    public class GroupBase<T> : IIdentifiableEntity where T : GroupBase<T>, new ()
    {
        public const long DefaultGroupId = 1;
        public const long DeletedGroupId = int.MinValue;
        private const string pathSeparator = "|";

        #region Protected fields

        protected long id = -2;
        protected string name = string.Empty;
        protected string code = "000";
        protected T parent;

        #endregion

        public virtual long Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        public virtual string Code
        {
            get { return code; }
            set { code = value; }
        }

        public T Parent
        {
            get { return parent; }
            set
            {
                if (value == null)
                    code = "000";
                else if (value.Code != ParentCode)
                    code = value.Code + "000";

                parent = value;
            }
        }

        public string ParentCode
        {
            get
            {
                if (string.IsNullOrEmpty (code))
                    return string.Empty;

                if (code.Length < 3)
                    return string.Empty;

                return code.Substring (0, code.Length - 3);
            }
        }

        private List<T> children;
        public List<T> Children
        {
            get
            {
                if (children == null) {
                    children = new List<T> ();
                    if (id > 0)
                        GetGroupTree ();
                }

                return children;
            }
        }

        public static void CommitAll (IEnumerable<T> groups)
        {
            using (DbTransaction transaction = new DbTransaction (BusinessDomain.DataAccessProvider)) {
                foreach (T group in groups)
                    group.CommitChanges ();

                transaction.Complete ();
            }
        }

        public virtual void CommitChanges ()
        {
            CommitChildrenChanges ();
        }

        protected void CommitChildrenChanges ()
        {
            foreach (T child in Children) {
                child.Parent = (T) this;
                child.CommitChanges ();
            }
        }

        protected static void MakeGroupTree (IList<T> groups, int depth)
        {
            if (groups.Count < 2)
                return;

            List<T> subGroups = new List<T> ();

            for (int i = groups.Count - 1; i >= 0; i--) {
                T g = groups [i];
                if (g.Code == null || g.Code.Length <= 3 * (depth + 1))
                    continue;

                subGroups.Add (g);
                groups.RemoveAt (i);
            }

            MakeGroupTree (subGroups, depth + 1);

            foreach (T subGroup in subGroups) {
                string parentCode = subGroup.ParentCode;

                foreach (T group in groups) {
                    if (group.Code != parentCode)
                        continue;

                    if (group.children == null)
                        group.children = new List<T> ();

                    group.Children.Add (subGroup);
                    subGroup.Parent = group;
                    break;
                }
            }
        }

        protected virtual void GetGroupTree ()
        {
        }

        public static string GetPath<G> (long groupId, CacheEntityCollection<G> cache) where G : GroupBase<G>, ICacheableEntity<G>, new ()
        {
            if (groupId < 0)
                return null;

            cache.EnsureCompleteLoad ();
            G g = cache.GetById (groupId);

            StringBuilder ret = new StringBuilder ();
            while (g != null) {
                ret.Insert (0, g.name);
                ret.Insert (0, pathSeparator);
                g = g.parent ?? cache.GetByCode (g.ParentCode);
            }

            return ret.ToString ();
        }

        public static G GetByPath<G> (string path, CacheEntityCollection<G> cache) where G : GroupBase<G>, ICacheableEntity<G>, new ()
        {
            string [] pathParts = path.Split (new [] { pathSeparator }, StringSplitOptions.RemoveEmptyEntries);
            G g = null;

            foreach (string part in pathParts) {
                G last = g;
                g = last != null ?
                    last.Children.FirstOrDefault (c => c.name == part) :
                    cache.GetByName (part);

                if (g == null)
                    return null;
            }

            return g;
        }

        public static G EnsureByPath<G> (string path, CacheEntityCollection<G> cache) where G : GroupBase<G>, ICacheableEntity<G>, new ()
        {
            string [] pathParts = path.Split (new [] { pathSeparator }, StringSplitOptions.RemoveEmptyEntries);
            G g = null;

            foreach (string part in pathParts) {
                G last = g;
                g = last != null ?
                    last.Children.FirstOrDefault (c => c.name == part) :
                    cache.GetByName (part);

                if (g != null)
                    continue;

                g = new G { Parent = last, Name = part };
                g.CommitChanges ();
                if (last != null)
                    last.children.Add (g);
            }

            return g;
        }

        public override string ToString ()
        {
            return string.Format ("Id: '{0}', Name: '{1}', Code: '{2}'", id, name, code);
        }
    }
}
