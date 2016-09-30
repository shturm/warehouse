//
// License.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   01.27.2012
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
using System.Xml.Serialization;
using Newtonsoft.Json;
using Warehouse.Data;

namespace Warehouse.Business.Licensing
{
    public enum LicenseStatus
    {
        Active,
        Inactive,
    }

    public class License : ICloneable
    {
        #region Private fields

        protected int id = -1;

        #endregion

        #region Public properties

        [DbColumn ("licenses.ID")]
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        [DbColumn ("licenses.Code")]
        public string Code { get; set; }

        [DbColumn ("licenses.User")]
        public string User { get; set; }

        [DbColumn ("licenses.Company")]
        public string Company { get; set; }

        [DbColumn ("licenses.Distributer")]
        public string Distributer { get; set; }

        [DbColumn ("licenses.Product")]
        public string Product { get; set; }

        [DbColumn ("licenses.Type")]
        public string Type { get; set; }

        [DbColumn ("licenses.ChallengeCode")]
        public string ChallengeCode { get; set; }

        [DbColumn ("licenses.ChallengeCodeType")]
        public int ChallengeCodeType { get; set; }

        [DbColumn ("licenses.IssuedDate")]
        public DateTime? IssuedDate { get; set; }

        [DbColumn ("licenses.SupportUntil")]
        public DateTime? SupportUntil { get; set; }

        [DbColumn ("licenses.UpdatesUntil")]
        public DateTime? UpdatesUntil { get; set; }

        [DbColumn ("licenses.ValidFrom")]
        public DateTime? ValidFrom { get; set; }

        [DbColumn ("licenses.ValidUntil")]
        public DateTime? ValidUntil { get; set; }

        [DbColumn ("licenses.OperatorId")]
        public long? OperatorId { get; set; }

        [DbColumn ("users.Name")]
        public string OperatorName { get; set; }

        [DbColumn ("licenses.CreatorHost")]
        public string CreatorHost { get; set; }

        [DbColumn ("licenses.CreatorEmail")]
        public string CreatorEmail { get; set; }

        public int? ActivationsLeft { get; set; }

        [DbColumn ("licenses.Status")]
        [XmlIgnore, JsonIgnore]
        public LicenseStatus Status { get; set; }

        [XmlIgnore, JsonIgnore]
        public string FileName { get; set; }

        [XmlIgnore, JsonIgnore]
        public byte [] Serialized { get; set; }

        #endregion

        #region Implementation of ICloneable

        public object Clone ()
        {
            return new License
                {
                    id = id,
                    Code = Code,
                    User = User,
                    Company = Company,
                    Distributer = Distributer,
                    Product = Product,
                    Type = Type,
                    ChallengeCode = ChallengeCode,
                    ChallengeCodeType = ChallengeCodeType,
                    IssuedDate = IssuedDate,
                    SupportUntil = SupportUntil,
                    UpdatesUntil = UpdatesUntil,
                    ValidFrom = ValidFrom,
                    ValidUntil = ValidUntil,
                    OperatorId = OperatorId,
                    OperatorName = OperatorName,
                    CreatorHost = CreatorHost,
                    CreatorEmail = CreatorEmail,
                    ActivationsLeft = ActivationsLeft,
                    FileName = FileName,
                    Serialized = Serialized
                };
        }

        #endregion
    }
}
