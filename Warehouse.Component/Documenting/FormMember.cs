//
// FormMember.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
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
using Warehouse.Business.Documenting;

namespace Warehouse.Component.Documenting
{
    public class FormMember
    {
        #region Private fields

        private readonly PropertyInfo propertyInfo;
        private readonly FieldInfo fieldInfo;
        private readonly FormMemberType type;
        private readonly bool isProperty;
        private readonly bool isStatic;

        #endregion

        #region Public properties

        public string ReportField { get; set; }

        public bool IsProperty
        {
            get { return isProperty; }
        }

        public bool IsStatic
        {
            get { return isStatic; }
        }

        public bool CanWrite
        {
            get { return !isProperty || propertyInfo.CanWrite; }
        }

        public bool CanRead
        {
            get { return !isProperty || propertyInfo.CanRead; }
        }

        public Type MemberType
        {
            get
            {
                return isProperty ?
                    propertyInfo.PropertyType :
                    fieldInfo.FieldType;
            }
        }

        public FormMemberType Type
        {
            get { return type; }
        }

        #endregion

        public FormMember (MemberInfo member, string reportField, FormMemberType type)
        {
            ReportField = reportField;
            this.type = type;

            propertyInfo = member as PropertyInfo;
            fieldInfo = member as FieldInfo;
            if (propertyInfo != null) {
                isProperty = true;
                MethodInfo methodInfo = propertyInfo.GetGetMethod () ?? propertyInfo.GetSetMethod ();
                isStatic = methodInfo != null && methodInfo.IsStatic;
            } else {
                isProperty = false;
                if (fieldInfo != null)
                    isStatic = fieldInfo.IsStatic;
            }
        }

        public void SetValue (object obj, object value)
        {
            if (isProperty)
                propertyInfo.SetValue (obj, value, null);
            else
                fieldInfo.SetValue (obj, value);
        }

        public object GetValue (object obj)
        {
            return isProperty ?
                propertyInfo.GetValue (obj, null) :
                fieldInfo.GetValue (obj);
        }
    }
}
