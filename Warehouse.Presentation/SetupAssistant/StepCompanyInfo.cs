//
// StepCompanyInfo.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   07.07.2011
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
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Presentation.Widgets;

namespace Warehouse.Presentation.SetupAssistant
{
    [Extension ("/Warehouse/Presentation/SetupAssistant/Step")]
    public class StepCompanyInfo : StepBase
    {
        private EditNewCompanyRecordPanel panel;

        #region Overrides of StepBase

        public override string Title
        {
            get { return Translator.GetString ("Company"); }
        }

        public override int Ordinal
        {
            get { return 10; }
        }

        public override AssistType Group
        {
            get { return AssistType.DatabaseSetup; }
        }

        #endregion

        protected override void CreateBody ()
        {
            CreateBody (Translator.GetString ("Enter your company information"));
        
            WrapLabel footer = new WrapLabel
                {
                    Markup = string.Format (Translator.GetString (
                        "This information will be printed on the documents you issue. If you want to change that information later go to:{0}{1}"),
                        Environment.NewLine,
                        new PangoStyle
                            {
                                Italic = true,
                                Bold = true,
                                Text = Translator.GetString ("Edit->Administration->Company information...")
                            })
                };
            footer.Show ();
            vboBody.PackStart (footer, false, true, 0);

            panel = new EditNewCompanyRecordPanel (CompanyRecord.GetDefault ());
            panel.Show ();
            vboBody.PackStart (panel, true, true, 10);
        }

        public override bool Complete (Assistant assistant)
        {
            if (!panel.Validate ())
                return false;

            panel.GetCompanyRecord ().CommitChanges ();
            panel.Sensitive = false;

            return base.Complete (assistant);
        }
    }
}
