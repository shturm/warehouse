//
// CacheEntry.cs
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

namespace Warehouse.Business.Entities
{
    public class CacheEntry<T> where T : class, ICacheableEntity<T>, new ()
    {
        private readonly T wRef;
        private readonly DateTime timeStamp;
        private T hRef;

        public bool HasHardRef
        {
            get { return hRef != null; }
        }

        public T Entity
        {
            get
            {
                return hRef ??
                    (timeStamp.AddHours (1) >= DateTime.Now ? wRef : null);
            }
        }

        public CacheEntry (T ent, bool hardRef = false)
        {
            wRef = ent;
            if (hardRef)
                hRef = ent;

            timeStamp = DateTime.Now;
        }

        public void RemoveHardRef ()
        {
            hRef = null;
        }
    }
}