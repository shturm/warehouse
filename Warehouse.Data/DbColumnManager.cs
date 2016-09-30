//
// DbColumnManager.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   06/19/2006
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
using System.Reflection;

namespace Warehouse.Data
{
    public class DbColumnManager
    {
        #region Private fields

        protected DbColumnAttribute dbAttribute;
        private readonly MemberInfo member;
        private readonly PropertyInfo propInfo;
        private readonly FieldInfo fieldInfo;
        private readonly bool isProperty;

        #endregion

        #region Public properties

        public MemberInfo Member
        {
            get { return member; }
        }

        public virtual DbField DbField
        {
            get
            {
                return dbAttribute != null ? dbAttribute.DbField : null;
            }
        }

        public int DbPosition { get; set; }

        public virtual bool CanWrite
        {
            get
            {
                return !isProperty || propInfo.CanWrite;
            }
        }

        public virtual bool CanRead
        {
            get
            {
                return !isProperty || propInfo.CanRead;
            }
        }

        public virtual Type MemberType
        {
            get
            {
                return isProperty ? propInfo.PropertyType : fieldInfo.FieldType;
            }
        }

        #endregion

        public DbColumnManager ()
        {
        }

        public DbColumnManager (DbColumnAttribute dbAttribute, MemberInfo member)
        {
            propInfo = member as PropertyInfo;
            fieldInfo = member as FieldInfo;
            isProperty = propInfo != null;

            this.member = member;
            this.dbAttribute = dbAttribute;
        }

        public virtual void SetValue (object obj, object value)
        {
            if (isProperty)
                propInfo.SetValue (obj, value, null);
            else
                fieldInfo.SetValue (obj, value);
        }

        public virtual object GetValue (object obj, bool applyRestrictions = false)
        {
            object ret = isProperty ? propInfo.GetValue (obj, null) : fieldInfo.GetValue (obj);

            if (applyRestrictions) {
                string retString = ret as string;
                if (retString != null && dbAttribute != null && dbAttribute.MaxLength != null) {
                    if (retString.Length > dbAttribute.MaxLength)
                        ret = retString.Substring (0, dbAttribute.MaxLength.Value);
                }
            }

            return ret;
        }
    }
}
