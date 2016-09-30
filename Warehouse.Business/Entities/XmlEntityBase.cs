//
// XmlEntityBase.cs
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

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using Warehouse.Data;
using System;
using Timer = System.Timers.Timer;

namespace Warehouse.Business.Entities
{
    public abstract class XmlEntityBase<T> : INotifyPropertyChanged, ICloneable, IEqualityComparer<T> where T : XmlEntityBase<T>
    {
        protected long id = -1;
        protected bool isDirty;

        [DbColumn (XmlEntityFields.Id)]
        public long Id
        {
            get { return id; }
            set
            {
                if (id == value)
                    return;

                id = value;
                OnPropertyChanged ("Id");
            }
        }

        protected virtual bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }

        protected virtual T [] DefaultEntities
        {
            get { return new T [0]; }
        }

        protected virtual string DatabaseCategory
        {
            get { return null; }
        }

        public static string DatabaseProfile
        {
            get { return databaseProfile; }
            set { databaseProfile = value; }
        }

        public static Action SetEmptyProfile
        {
            get { return setEmptyProfile; }
            set { setEmptyProfile = value; }
        }

        public static Action<string> SetProfile
        {
            get { return setProfile; }
            set { setProfile = value; }
        }

        protected abstract string EntitiesFile
        {
            get;
        }

        protected abstract T [] EntityCache
        {
            get;
            set;
        }

        protected abstract Mutex FileLock
        {
            get;
        }

        protected virtual bool LazyCommit
        {
            get { return false; }
        }

        protected virtual long LazyCommitWait
        {
            get { return 5000; }
        }

        protected virtual Timer CommitTimer
        {
            get { return null; }
            set { }
        }

        #region INotifyPropertyChanged Members

        protected void OnPropertyChanged (string propertyName)
        {
            isDirty = true;
            if (PropertyChanged != null)
                PropertyChanged (this, new PropertyChangedEventArgs (propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public virtual T CommitChanges ()
        {
            if (!IsDirty)
                return (T) this;

            try {
                FileLock.WaitOne ();
                List<T> allObjects = new List<T> (GetAllEntities (false));
                DbColumnManager [] managers = DataProvider.GetDBManagers (typeof (T));
                T thisObject = (T) this;
                int index = DataProvider.FindObjectIndexByField (allObjects, XmlEntityFields.Id, id, managers);

                if (index < 0) {
                    if (!DataProvider.CreateNewObjectId (allObjects, managers, thisObject, XmlEntityFields.Id))
                        return (T) this;

                    IsDirty = false;
                    allObjects.Add ((T) thisObject.Clone ());
                } else {
                    allObjects.RemoveAt (index);
                    IsDirty = false;
                    allObjects.Insert (index, (T) thisObject.Clone ());
                }

                DoCommitChanges (allObjects.ToArray ());
#if !DEBUG
            } catch (InvalidOperationException) {
#endif
            } finally {
                FileLock.ReleaseMutex ();
            }

            return (T) this;
        }

        protected void DeleteEntity (long entId)
        {
            try {
                FileLock.WaitOne ();
                List<T> allObjects = new List<T> (GetAllEntities (false));
                int index = DataProvider.FindObjectIndexByField (allObjects, XmlEntityFields.Id, entId);

                if (index < 0)
                    return;

                allObjects.RemoveAt (index);

                DoCommitChanges (allObjects.ToArray ());
#if !DEBUG
            } catch (InvalidOperationException) {
#endif
            } finally {
                FileLock.ReleaseMutex ();
            }
        }

        private void DoCommitChanges (T [] allObjects)
        {
//            if (allObjects.Length <= 2 && GetType ().FullName == "Warehouse.TouchScreen.Entity") {
//                ErrorHandling.LogError ("Saving too few custom config entries to db:" + allObjects.Length, ErrorSeverity.FatalError);
//#if DEBUG
//                T [] all = GetAllEntities (false);
//                if (all.Length > allObjects.Length) {
//                    ErrorHandling.LogError ("Config entries in the database currently are:" + all.Length, ErrorSeverity.FatalError);
//                    System.Diagnostics.Debugger.Break ();
//                }
//#endif
//            }

            EntityCache = allObjects;

            if (LazyCommit) {
                StartLazyCommit ();
                return;
            }

            commitTimer_Elapsed (this, null);
        }

        private void StartLazyCommit ()
        {
            Timer commitTimer = CommitTimer;
            if (commitTimer == null) {
                commitTimer = new Timer { AutoReset = false };
                commitTimer.Elapsed += commitTimer_Elapsed;
                CommitTimer = commitTimer;
            }

            commitTimer.Interval = LazyCommitWait;
            commitTimer.Stop ();
            commitTimer.Start ();
        }

        private void commitTimer_Elapsed (object sender, ElapsedEventArgs e)
        {
            try {
                FileLock.WaitOne ();
                T [] entityCache = EntityCache;
                if (entityCache == null)
                    return;

                if (DatabaseCategory != null) {
                    System.Diagnostics.Debug.WriteLine ("Delayed writing changes in xml entities to database.");
                    CustomConfig.CommitChanges (PrepareToDatabase (entityCache), DatabaseCategory, databaseProfile);
                } else {
                    System.Diagnostics.Debug.WriteLine (string.Format ("Delayed writing changes in xml entities to file {0}.", EntitiesFile));
                    DataProvider.SaveAllObjectsToXML (EntitiesFile, entityCache);
                }
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
            } finally {
                FileLock.ReleaseMutex ();
            }
        }

        protected void FlushCache ()
        {
            if (!LazyCommit || CommitTimer == null)
                return;

            try {
                FileLock.WaitOne ();

                if (CommitTimer.Enabled) {
                    CommitTimer.Stop ();
                    commitTimer_Elapsed (null, null);
                }
            } finally {
                FileLock.ReleaseMutex ();
            }
        }

        private static string lastDatabaseProfile = string.Empty;
        private static string databaseProfile = string.Empty;
        private static Action setEmptyProfile = () => { };
        private static Action<string> setProfile = name => { };

        protected virtual T [] GetAllEntities (bool clone)
        {
            T [] cache = EntityCache;
            if (lastDatabaseProfile != databaseProfile) {
                lastDatabaseProfile = databaseProfile;
                EntityCache = cache = null;
            }

            if (cache == null) {
                if (DatabaseCategory != null) {
                    T [] fromDatabase = CustomConfig.Get<T> (DatabaseCategory, databaseProfile);
                    if (fromDatabase.Length == 0 && !string.IsNullOrEmpty (databaseProfile))
                        if (OnCustomProfileEmpty ())
                            fromDatabase = CustomConfig.Get<T> (DatabaseCategory, databaseProfile);

                    EntityCache = cache = OnEntitiesLoaded (PrepareFromDatabase (fromDatabase));
                }

                T [] fromFile = null;
                if (File.Exists (EntitiesFile))
                    fromFile = OnEntitiesLoaded (DataProvider.GetAllObjectsFromXML<T> (FileLock, EntitiesFile));

                if (fromFile != null && fromFile.Length > 0) {
                    T [] toDatabase = null;
                    T [] toRemoveFromFile = null;
                    if (cache == null || cache.Length == 0) {
                        EntityCache = cache = fromFile;

                        if (DatabaseCategory != null) {
                            toDatabase = PrepareToDatabase (cache);
                            toRemoveFromFile = toDatabase;
                        }
                    } else
                        toRemoveFromFile = PrepareToDatabase (fromFile);

                    if (toDatabase != null && toDatabase.Length > 0)
                        CustomConfig.CommitChanges (toDatabase, DatabaseCategory, databaseProfile);

                    if (toRemoveFromFile != null && toRemoveFromFile.Length > 0) {
                        T [] newEntitiesInFile = fromFile.Except (toRemoveFromFile, this).ToArray ();
                        if (newEntitiesInFile.Length == 0)
                            try {
                                File.Delete (EntitiesFile);
                            } catch (Exception) {
                            }

                        if (File.Exists (EntitiesFile) && toRemoveFromFile.Length > 0)
                            try {
                                FileLock.WaitOne ();
                                System.Diagnostics.Debug.WriteLine (string.Format ("Writing changes in xml entities to file {0}.", EntitiesFile));

                                DataProvider.SaveAllObjectsToXML (EntitiesFile, newEntitiesInFile);
                            } catch (UnauthorizedAccessException uaex) {
                                ErrorHandling.LogException (uaex);
                            } finally {
                                FileLock.ReleaseMutex ();
                            }
                    }
                }

                if (cache != null)
                    foreach (T entity in cache)
                        entity.isDirty = false;

                if ((cache == null || cache.Length == 0) && DefaultEntities != null && DefaultEntities.Length > 0) {
                    cache = DefaultEntities;
                    foreach (T entity in cache)
                        entity.isDirty = false;

                    DoCommitChanges (cache);
                }

                if (cache == null)
                    EntityCache = cache = new T [0];
            }

            return clone ?
                cache.Select (entity => (T) entity.Clone ()).ToArray () :
                cache;
        }

        protected virtual bool OnCustomProfileEmpty ()
        {
            return false;
        }

        protected virtual T [] PrepareToDatabase (T [] entities)
        {
            return entities;
        }

        protected virtual T [] PrepareFromDatabase (T [] entities)
        {
            return entities;
        }

        protected virtual T [] OnEntitiesLoaded (T [] entities)
        {
            return entities;
        }

        protected virtual T [] GetEntities (Dictionary<string, object> criteria)
        {
            T [] allObjects = GetAllEntities (true);
            DbColumnManager [] managers = DataProvider.GetDBManagers (typeof (T));

            foreach (KeyValuePair<string, object> criterion in criteria) {
                allObjects = DataProvider.FindObjectsByField (allObjects, criterion.Key, criterion.Value, managers);
            }

            return allObjects;
        }

        protected virtual T GetSingleEntity (Dictionary<string, object> criteria)
        {
            T [] ret = GetEntities (criteria);
            return ret.Length > 0 ? ret [0] : default (T);
        }

        protected void CreateNewProfile (string profile)
        {
            CustomConfig.CommitChanges (new T [0], DatabaseCategory, profile);
        }

        protected void DeleteExistingProfile (string profile)
        {
            CustomConfig.Delete (DatabaseCategory, profile);
        }

        #region ICloneable Members

        public virtual object Clone ()
        {
            return MemberwiseClone ();
        }

        #endregion

        #region Implementation of IEqualityComparer<in T>

        public virtual bool Equals (T x, T y)
        {
            bool xNull = x == null;
            bool yNull = y == null;
            if (xNull && yNull)
                return true;

            if (xNull || yNull)
                return false;

            return x.id == y.id;
        }

        public int GetHashCode (T obj)
        {
            return (int) obj.id;
        }

        #endregion
    }
}
