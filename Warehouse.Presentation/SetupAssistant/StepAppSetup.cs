//
// StepAppSetup.cs
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

using Mono.Addins;
using Warehouse.Business;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.SetupAssistant
{
    [Extension ("/Warehouse/Presentation/SetupAssistant/Step")]
    public class StepAppSetup : StepBase
    {
        #region Overrides of StepBase

        public override string Title
        {
            get { return Translator.GetString ("Welcome"); }
        }

        public override int Ordinal
        {
            get { return 0; }
        }

        public override AssistType Group
        {
            get { return AssistType.ApplicationSetup; }
        }

        #endregion

        protected override void CreateBody ()
        {
            CreateBody (string.Format (Translator.GetString ("Welcome to the {0} setup assistant"), DataHelper.ProductName));

            WrapLabel message = new WrapLabel
                {
                    Markup = new PangoStyle
                        {
                            Size = PangoStyle.TextSize.Large,
                            Text = string.Format (Translator.GetString ("This assistant will help you setup {0} so you can start working right away. If you don't need help from this assistant, you can press \"Finish\" at any time and complete the setup of {0} yourself."), DataHelper.ProductName)
                        }
                };
            message.Show ();

            vboBody.PackStart (message, true, true, 10);
        }
    }
}
