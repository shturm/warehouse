//
// CacheEntityCollection.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   10/03/2009
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
using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public class CacheEntityCollection<T> : Dictionary<long, CacheEntry<T>> where T : class, ICacheableEntity<T>, new ()
    {
        private readonly TimeSpan maxCompleteLoadAge = new TimeSpan (0, 1, 0);
        private string databaseName;
        private readonly T instance = new T ();
        private DateTime? completeLoad;
        private bool clearRequested;

        private bool IsValidCompleteLoad
        {
            get { return completeLoad != null && completeLoad.Value.Add (maxCompleteLoadAge) > DateTime.Now; }
        }

        public void Set (T ent)
        {
            if (DataHelper.DisableEntityCaching || ent == null)
                return;

            CacheEntry<T> oldValue;
            if (TryGetValue (ent.Id, out oldValue))
                this [ent.Id] = new CacheEntry<T> (ent, oldValue.HasHardRef);
            else
                this [ent.Id] = new CacheEntry<T> (ent);
        }

        public T GetById (long id)
        {
            if (DataHelper.DisableEntityCaching)
                return instance.GetEntityById (id);

            CheckDatabaseChanged ();
            ClearCompleteLoad ();

            CacheEntry<T> cacheEntry;
            T ent;
            if (TryGetValue (id, out cacheEntry)) {
                ent = cacheEntry.Entity;
                if (ent != null)
                    return (T) ent.Clone ();

                Remove (id);
            }

            ent = instance.GetEntityById (id);
            if (ent == null)
                return default (T);

            return (T) ent.Clone ();
        }

        public T GetByCode (string code)
        {
            if (DataHelper.DisableEntityCaching)
                return instance.GetEntityByCode (code);

            CheckDatabaseChanged ();
            ClearCompleteLoad ();

            T ent;
            KeyValuePair<long, CacheEntry<T>> pair = this.FirstOrDefault (p =>
                {
                    var entity = p.Value.Entity;
                    return entity != null && entity.Code == code;
                });
            if (pair.Key != 0) {
                CacheEntry<T> cacheEntry = pair.Value;
                ent = cacheEntry.Entity;
                if (ent != null)
                    return (T) ent.Clone ();

                Remove (pair.Key);
            }

            ent = instance.GetEntityByCode (code);
            if (ent == null)
                return default (T);

            return (T) ent.Clone ();
        }

        public T GetByName (string name)
        {
            if (DataHelper.DisableEntityCaching)
                return instance.GetEntityByName (name);

            CheckDatabaseChanged ();
            ClearCompleteLoad ();

            T ent;
            KeyValuePair<long, CacheEntry<T>> pair = this.FirstOrDefault (p =>
                {
                    var entity = p.Value.Entity;
                    return entity != null && entity.Name == name;
                });
            if (pair.Key != 0) {
                CacheEntry<T> cacheEntry = pair.Value;
                ent = cacheEntry.Entity;
                if (ent != null)
                    return (T) ent.Clone ();

                Remove (pair.Key);
            }

            ent = instance.GetEntityByName (name);
            if (ent == null)
                return default (T);

            return (T) ent.Clone ();
        }

        public void EnsureCompleteLoad ()
        {
            lock (this) {
                if (Count == 0 || !IsValidCompleteLoad) {
                    Clear ();
                    foreach (T ent in instance.GetAllEntities ())
                        this [ent.Id] = new CacheEntry<T> (ent, true);
                }

                completeLoad = DateTime.Now;
            }
        }

        private void CheckDatabaseChanged ()
        {
            string curDatabase = BusinessDomain.AppConfiguration.DbDatabase;
            if (string.IsNullOrEmpty (databaseName))
                databaseName = curDatabase;
            else if (databaseName != curDatabase) {
                Clear ();
                databaseName = curDatabase;
            }
        }

        private void ClearCompleteLoad ()
        {
            lock (this) {
                if (Count == 0 || IsValidCompleteLoad)
                    return;

                if (completeLoad != null) {
                    foreach (var pair in this)
                        pair.Value.RemoveHardRef ();

                    completeLoad = null;
                }

                if (!clearRequested)
                    return;

                Clear ();
                clearRequested = false;
            }
        }

        public void ClearRequest ()
        {
            if (Count == 0)
                return;

            lock (this) {
                if (IsValidCompleteLoad) {
                    clearRequested = true;
                    return;
                }

                Clear ();
                clearRequested = false;
            }
        }

        public void Clear (IEnumerable<long> ids)
        {
            if (Count == 0)
                return;

            lock (this) {
                if (IsValidCompleteLoad) {
                    foreach (var id in ids) {
                        T ent = instance.GetEntityById (id);
                        if (ent != null)
                            this [ent.Id] = new CacheEntry<T> (ent, true);
                    }
                    return;
                }

                foreach (var id in ids)
                    Remove (id);
            }
        }

        public override string ToString ()
        {
            return string.Format ("count: {0}, null entities: {1}, complete load: {2}, complete load is valid: {3}",
                Count,
                string.Join (",", this.Where (e => e.Value == null).Select (e => "id:" + e.Key).ToArray ()),
                completeLoad != null ? completeLoad.Value.ToString ("HH:mm:ss.fff") : "no",
                IsValidCompleteLoad);
        }
    }
}
