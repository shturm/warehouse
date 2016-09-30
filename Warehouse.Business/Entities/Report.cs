//
// Report.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   11/02/2006
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
using System.Data;
using Warehouse.Business.Documenting;

namespace Warehouse.Business.Entities
{
    public class Report : IDocument
    {
        #region Private members

        private string reportName = string.Empty;
        private string reportDate = string.Empty;
        private DataTable reportDetails = new DataTable ();
        private bool reportHasFooter;
        private string name = Translator.GetString ("Report");

        #endregion

        #region Public properties

        [FormMemberMapping ("reportName")]
        public string ReportName
        {
            get { return reportName; }
            set { reportName = value; }
        }

        [FormMemberMapping ("reportDate")]
        public string ReportDate
        {
            get { return reportDate; }
            set { reportDate = value; }
        }

        [FormMemberMapping ("dateFormat", FormMemberType.Format)]
        public static string DateFormat
        {
            get { return Translator.GetString ("Date: {0}"); }
        }

        [FormMemberMapping ("currentPageFormat", FormMemberType.Format)]
        public static string CurrentPageFormat
        {
            get { return Document.CurrentPageFormat; }
        }

        [FormMemberMapping ("totalPagesFormat", FormMemberType.Format)]
        public static string TotalPagesFormat
        {
            get { return Document.TotalPagesFormat; }
        }

        [FormMemberMapping ("reportDetails")]
        public DataTable ReportDetails
        {
            get { return reportDetails; }
            set { reportDetails = value; }
        }

        [FormMemberMapping ("reportHasFooter", FormMemberType.FooterSource)]
        public bool ReportHasFooter
        {
            get { return reportHasFooter; }
            set { reportHasFooter = value; }
        }

        [FormMemberMapping ("productId")]
        public static string ProductId
        {
            get
            {
                return Document.ProductId;
            }
        }

        #endregion

        public string Name
        {
            get { return name; }
        }

        public string FileName
        {
            get
            {
                DateTime date = BusinessDomain.GetDateValue (reportDate);
                return string.Format ("{0}-{1}_{2}_{3}", name, date.Day, date.Month, date.Year);
            }
        }

        public string FormName
        {
            get { return "Report"; }
        }

        public void SetName (string value)
        {
            name = value;
        }

        public object Clone ()
        {
            return MemberwiseClone ();
        }
    }
}