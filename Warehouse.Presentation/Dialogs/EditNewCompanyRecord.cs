//
// EditNewCompanyRecord.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/16/2006
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
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Presentation.Widgets;

namespace Warehouse.Presentation.Dialogs
{
    public class EditNewCompanyRecord : EditNew
    {
        private readonly EditNewCompanyRecordPanel panel;

        public EditNewCompanyRecord (CompanyRecord companyRecord)
        {
            panel = new EditNewCompanyRecordPanel (companyRecord);
            panel.ShowAll ();
            algContents.Add (panel);

            dlgEditNew.Title = companyRecord != null ?
                Translator.GetString ("Edit company") :
                Translator.GetString ("New company record");
        }

        protected override void InitializeForm ()
        {
            base.InitializeForm ();

            dlgEditNew.Icon = FormHelper.LoadImage ("Icons.CompanyRecord16.png").Pixbuf;
        }

        #region Event handling

        protected override void btnOK_Clicked (object o, EventArgs args)
        {
            if (!panel.Validate ())
                return;

            base.btnOK_Clicked (o, args);
        }

        #endregion

        public CompanyRecord GetCompanyRecord ()
        {
            return panel.GetCompanyRecord ();
        }
    }
}
