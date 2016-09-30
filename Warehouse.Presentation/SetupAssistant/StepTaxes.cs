//
// StepTaxes.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   10.03.2011
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

using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;

namespace Warehouse.Presentation.SetupAssistant
{
    [Extension ("/Warehouse/Presentation/SetupAssistant/Step")]
    public class StepTaxes : StepBase
    {
        private VATGroup defaultVatGroup;
        private RadioButton rbnVAT;
        private RadioButton rbnSalesTax;

        private Entry txtTaxRate;

        private RadioButton rbnWithVAT;
        private RadioButton rbnWithoutVAT;

        private WrapLabel lblNote;

        #region Overrides of StepBase

        public override string Title
        {
            get { return Translator.GetString ("Taxes"); }
        }

        public override int Ordinal
        {
            get { return 15; }
        }

        public override AssistType Group
        {
            get { return AssistType.DatabaseSetup; }
        }

        #endregion

        protected override void CreateBody ()
        {
            CreateBody (Translator.GetString ("How do you want to handle the taxes?"));

            #region VAT or Sales tax

            WrapLabel lblVATorTax = new WrapLabel
                {
                    Markup = new PangoStyle
                        {
                            Size = PangoStyle.TextSize.Large,
                            Text = Translator.GetString ("Are you using VAT or Sales tax?")
                        }
                };
            lblVATorTax.Show ();
            vboBody.PackStart (lblVATorTax, true, true, 5);

            rbnVAT = new RadioButton (Translator.GetString ("VAT"));
            rbnSalesTax = new RadioButton (rbnVAT, Translator.GetString ("Sales tax"));
            rbnVAT.Toggled += rbnVAT_Toggled;

            HBox hbo = new HBox { Spacing = 10 };
            hbo.PackStart (rbnVAT, false, true, 0);
            hbo.PackStart (rbnSalesTax, false, true, 0);
            hbo.ShowAll ();
            vboBody.PackStart (hbo, false, true, 5);

            #endregion

            HSeparator hs = new HSeparator ();
            hs.Show ();
            vboBody.PackStart (hs, true, true, 2);

            #region Default tax

            WrapLabel lblTaxRateTitle = new WrapLabel
                {
                    Markup = new PangoStyle
                        {
                            Size = PangoStyle.TextSize.Large,
                            Text = Translator.GetString ("What is the dafault tax rate you want to use?")
                        }
                };
            lblTaxRateTitle.Show ();
            vboBody.PackStart (lblTaxRateTitle, true, true, 5);

            Label lblTaxRate = new Label
                {
                    Markup = new PangoStyle
                        {
                            Text = Translator.GetString ("Rate:")
                        }
                };

            defaultVatGroup = VATGroup.GetById (VATGroup.DefaultVATGroupId);

            txtTaxRate = new Entry { Text = Percent.ToEditString (defaultVatGroup.VatValue) };
            txtTaxRate.WidthChars = 10;
            txtTaxRate.Alignment = 1;

            hbo = new HBox { Spacing = 10 };
            hbo.PackStart (lblTaxRate, false, true, 0);
            hbo.PackStart (txtTaxRate, false, true, 0);
            hbo.ShowAll ();
            vboBody.PackStart (hbo, false, true, 5);

            #endregion

            hs = new HSeparator ();
            hs.Show ();
            vboBody.PackStart (hs, true, true, 2);

            #region With or without VAT

            WrapLabel lblPrices = new WrapLabel
                {
                    Markup = new PangoStyle
                        {
                            Size = PangoStyle.TextSize.Large,
                            Text = Translator.GetString ("How do you want to use the prices in the application? Choose which value you want to round your prices to.")
                        }
                };
            lblPrices.Show ();
            vboBody.PackStart (lblPrices, true, true, 5);

            rbnWithVAT = new RadioButton (Translator.GetString ("With VAT"));
            rbnWithoutVAT = new RadioButton (rbnWithVAT, Translator.GetString ("Without VAT"));

            hbo = new HBox { Spacing = 10 };
            hbo.PackStart (rbnWithVAT, false, true, 0);
            hbo.PackStart (rbnWithoutVAT, false, true, 0);
            hbo.ShowAll ();
            vboBody.PackStart (hbo, false, true, 5);

            lblNote = new WrapLabel
                {
                    Text = Translator.GetString ("Note: You can easely add or subtract VAT after you enter the prices in the operation, using the buttons on the right.")
                };
            lblNote.Show ();
            vboBody.PackStart (lblNote, true, true, 5);

            #endregion
        }

        private void rbnVAT_Toggled (object sender, System.EventArgs e)
        {
            bool vat = rbnVAT.Active;

            rbnWithVAT.Label = vat ?
                Translator.GetString ("With VAT") :
                Translator.GetString ("With sales tax");
            rbnWithoutVAT.Label = vat ?
                Translator.GetString ("Without VAT") :
                Translator.GetString ("Without sales tax");
            lblNote.Text = vat ?
                Translator.GetString ("Note: You can easely add or subtract VAT after you enter the prices in the operation, using the buttons on the right.") :
                Translator.GetString ("Note: You can easely add or subtract sales tax after you enter the prices in the operation, using the buttons on the right.");
        }

        public override bool Complete (Assistant assistant)
        {
            ConfigurationHolder configuration = BusinessDomain.AppConfiguration;
            configuration.UseSalesTaxInsteadOfVAT = rbnSalesTax.Active;

            defaultVatGroup.VatValue = Percent.ParseExpression (txtTaxRate.Text);
            defaultVatGroup.CommitChanges ();

            configuration.VATIncluded = rbnWithVAT.Active;
            configuration.Save (true);

            ChangeStatus (StepStatus.Complete);
            return true;
        }
    }
}
