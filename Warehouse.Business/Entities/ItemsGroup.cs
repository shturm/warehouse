//
// ItemsGroup.cs
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
using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public class ItemsGroup : GroupBase<ItemsGroup>, ICacheableEntity<ItemsGroup>
    {
        #region Public fields

        [DbColumn (DataField.ItemsGroupId)]
        public override long Id
        {
            get { return id; }
            set { id = value; }
        }

        [DbColumn (DataField.ItemsGroupName, 255)]
        public override string Name
        {
            get { return name; }
            set { name = value; }
        }

        [DbColumn (DataField.ItemsGroupCode, 255)]
        public override string Code
        {
            get { return code; }
            set { code = value; }
        }

        #endregion

        private static readonly CacheEntityCollection<ItemsGroup> cache = new CacheEntityCollection<ItemsGroup> ();
        public static CacheEntityCollection<ItemsGroup> Cache
        {
            get { return cache; }
        }

        public override void CommitChanges ()
        {
            BusinessDomain.DataAccessProvider.AddUpdateItemsGroup (this);
            cache.Set (this);

            CommitChildrenChanges ();
        }

        public static DeletePermission RequestDelete (long groupId)
        {
            return BusinessDomain.DataAccessProvider.CanDeleteItemsGroup (groupId);
        }

        public static void Delete (long groupId)
        {
            BusinessDomain.DataAccessProvider.DeleteItemsGroup (groupId);
            cache.Remove (groupId);
        }

        public static ItemsGroup [] GetAll ()
        {
            List<ItemsGroup> groups = GetAllFlat ();
            MakeGroupTree (groups, 0);

            return groups.ToArray ();
        }

        public static List<ItemsGroup> GetAllFlat ()
        {
            if (BusinessDomain.DataAccessProvider == null)
                return new List<ItemsGroup> ();

            return BusinessDomain.DataAccessProvider.GetAllItemsGroups<ItemsGroup> ().ToList ();
        }

        public static ItemsGroup GetById (long groupID)
        {
            ItemsGroup ret = BusinessDomain.DataAccessProvider.GetItemsGroupById<ItemsGroup> (groupID);
            cache.Set (ret);
            return ret;
        }

        public static ItemsGroup GetByCode (string code)
        {
            ItemsGroup ret = BusinessDomain.DataAccessProvider.GetItemsGroupByCode<ItemsGroup> (code);
            cache.Set (ret);
            return ret;
        }

        public static ItemsGroup GetByName (string groupName)
        {
            ItemsGroup ret = BusinessDomain.DataAccessProvider.GetItemsGroupByName<ItemsGroup> (groupName);
            cache.Set (ret);
            return ret;
        }

        protected override void GetGroupTree ()
        {
            if (!string.IsNullOrEmpty (code)) {
                List<ItemsGroup> groups = new List<ItemsGroup> { this };

                StringBuilder error = new StringBuilder ();
                try {
                    error.Append ("\nBefore complete load: " + cache);
                    cache.EnsureCompleteLoad ();
                    error.Append ("\nAfter complete load: " + cache);
                    groups.AddRange (cache.Select (ce => ce.Value.Entity).Where (g => g.ParentCode == code));
                } catch (Exception ex) {
                    throw new Exception ("Error while getting the group tree: " + error, ex);
                }

                MakeGroupTree (groups, (code.Length / 3) - 1);
            }
        }

        public object Clone ()
        {
            return MemberwiseClone ();
        }

        public ItemsGroup GetEntityById (long entityId)
        {
            return GetById (entityId);
        }

        public ItemsGroup GetEntityByCode (string entityCode)
        {
            return GetByCode (entityCode);
        }

        public ItemsGroup GetEntityByName (string entityName)
        {
            return GetByName (entityName);
        }

        public IEnumerable<ItemsGroup> GetAllEntities ()
        {
            return GetAllFlat ();
        }
    }
}
