//
// CompanyRecord.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/11/2006
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
using Warehouse.Data;
using Warehouse.Data.Model;

namespace Warehouse.Business.Entities
{
    public class CompanyRecord
    {
        #region Private fields

        private long id = -1;
        private string name = string.Empty;
        private string liablePerson = string.Empty;
        private string city = string.Empty;
        private string address = string.Empty;
        private string telephone = string.Empty;
        private string bulstat = string.Empty;
        private string bankAccount = string.Empty;

        private string code = string.Empty;
        private string fax = string.Empty;
        private string email = string.Empty;
        private string bankName = string.Empty;
        private string bankCode = string.Empty;
        private string bankVATAccount = string.Empty;
        private int creatorId = 1;
        private DateTime creationTimeStamp;

        private string taxNumber = string.Empty;
        private int def = 0;
        private string note1 = string.Empty;
        private string note2 = string.Empty;

        #endregion

        #region Public properties

        [DbColumn (DataField.CompanyId)]
        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        [DbColumn (DataField.CompanyCode, 255)]
        public string Code
        {
            get { return code; }
            set { code = value; }
        }

        [DbColumn (DataField.CompanyName, 255)]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [DbColumn (DataField.CompanyLiablePerson, 255)]
        public string LiablePerson
        {
            get { return liablePerson; }
            set { liablePerson = value; }
        }

        [DbColumn (DataField.CompanyCity, 255)]
        public string City
        {
            get { return city; }
            set { city = value; }
        }

        [DbColumn (DataField.CompanyAddress, 255)]
        public string Address
        {
            get { return address; }
            set { address = value; }
        }

        [DbColumn (DataField.CompanyPhone, 255)]
        public string Telephone
        {
            get { return telephone; }
            set { telephone = value; }
        }

        [DbColumn (DataField.CompanyFax, 255)]
        public string Fax
        {
            get { return fax; }
            set { fax = value; }
        }

        [DbColumn (DataField.CompanyEmail, 255)]
        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        [DbColumn (DataField.CompanyTaxNumber, 255)]
        public string TaxNumber
        {
            get { return taxNumber; }
            set { taxNumber = value; }
        }

        [DbColumn (DataField.CompanyBulstat, 255)]
        public string Bulstat
        {
            get { return bulstat; }
            set { bulstat = value; }
        }

        [DbColumn (DataField.CompanyBankName, 255)]
        public string BankName
        {
            get { return bankName; }
            set { bankName = value; }
        }

        [DbColumn (DataField.CompanyBankCode, 255)]
        public string BankCode
        {
            get { return bankCode; }
            set { bankCode = value; }
        }

        [DbColumn (DataField.CompanyBankAcct, 255)]
        public string BankAccount
        {
            get { return bankAccount; }
            set { bankAccount = value; }
        }

        [DbColumn (DataField.CompanyBankVATAcct, 255)]
        public string BankVATAccount
        {
            get { return bankVATAccount; }
            set { bankVATAccount = value; }
        }

        [DbColumn (DataField.CompanyCreatorId)]
        public int CreatorId
        {
            get { return creatorId; }
            set { creatorId = value; }
        }

        [DbColumn (DataField.CompanyCreationTimeStamp)]
        public DateTime CreationTimeStamp
        {
            get { return creationTimeStamp; }
            set { creationTimeStamp = value; }
        }

        [DbColumn (DataField.CompanyDefault)]
        public int Default
        {
            get { return def; }
            set { def = value; }
        }

        public bool IsDefault
        {
            get { return def == -1; }
            set
            {
                if (value)
                    def = -1;
                else
                    def = 0;
            }
        }

        [DbColumn (DataField.CompanyNote1, 255)]
        public string Note1
        {
            get { return note1; }
            set { note1 = value; }
        }

        [DbColumn (DataField.CompanyNote2, 255)]
        public string Note2
        {
            get { return note2; }
            set { note2 = value; }
        }

        #endregion

        public CompanyRecord CommitChanges ()
        {
            BusinessDomain.DataAccessProvider.AddUpdateCompanyRecord (this);

            return this;
        }

        public static DeletePermission CanDelete (long companyRecordId)
        {
            return BusinessDomain.DataAccessProvider.CanDeleteCompanyRecord (companyRecordId);
        }

        public static void Delete (long companyRecordId)
        {
            BusinessDomain.DataAccessProvider.DeleteCompanyRecord (companyRecordId);
        }

        public static LazyListModel<CompanyRecord> GetAll ()
        {
            return BusinessDomain.DataAccessProvider.GetAllCompanyRecords<CompanyRecord> ();
        }

        public static CompanyRecord GetById (long companyRecordId)
        {
            return BusinessDomain.DataAccessProvider.GetCompanyRecordById<CompanyRecord> (companyRecordId);
        }

        public static CompanyRecord GetByName (string companyRecordName)
        {
            return BusinessDomain.DataAccessProvider.GetCompanyRecordByName<CompanyRecord> (companyRecordName);
        }

        public static CompanyRecord GetByCode (string companyRecordCode)
        {
            return BusinessDomain.DataAccessProvider.GetCompanyRecordByCode<CompanyRecord> (companyRecordCode);
        }

        public static CompanyRecord GetDefault ()
        {
            User loggedUser = BusinessDomain.LoggedUser;
            if (loggedUser.IsSaved) {
                long companyId = loggedUser.DefaultCompanyId;
                if (companyId > 0)
                    return GetById (companyId);
            }

            return BusinessDomain.DataAccessProvider.GetDefaultCompanyRecord<CompanyRecord> ();
        }
    }
}