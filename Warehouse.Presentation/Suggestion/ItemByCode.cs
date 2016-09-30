//
// ItemByCode.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   01.14.2014
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
using System.Linq;
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Component;

namespace Warehouse.Presentation.Suggestion
{
    [Extension ("/Warehouse/Presentation/EntityMissing")]
    public class ItemByCode : ISuggestionProvider
    {
        private string value;

        public event EventHandler<EntitySuggestedEventArgs> EntitySuggested;

        public int Order { get { return 30; } }

        public SuggestionEntityType SupportedEntities { get { return SuggestionEntityType.Item; } }

        public bool CheckAvailableFor (string value)
        {
            if (string.IsNullOrWhiteSpace (value))
                return false;

            return value.Length <= 8 && value.All (char.IsLetterOrDigit);
        }

        public Widget [] GetWidget (string value, object operation)
        {
            this.value = value;

            VBox vbo = new VBox { Spacing = 4 };

            Label lblTitle = new Label { Markup = new PangoStyle { Size = PangoStyle.TextSize.Large, Text = Translator.GetString ("Create a new item with:") }, Xalign = 0 };
            vbo.PackStart (lblTitle);

            HBox hbo = new HBox { Spacing = 4 };
            Label lblName = new Label { Markup = new PangoStyle { Bold = true, Text = Translator.GetString ("Code:") } };
            hbo.PackStart (lblName, false, true, 0);
            Label lblNameValue = new Label (value) { Xalign = 0 };
            hbo.PackStart (lblNameValue, false, true, 0);
            vbo.PackStart (hbo);

            HBox hboMain = new HBox { Spacing = 4 };
            hboMain.PackStart (FormHelper.LoadImage ("Icons.AppMain32.png"), false, true, 0);
            hboMain.PackStart (vbo, true, true, 0);

            Alignment alg = new Alignment (0, 0, 1, 1) { TopPadding = 4, BottomPadding = 4, LeftPadding = 4, RightPadding = 4 };
            alg.Add (hboMain);

            Button btn = new Button (alg);
            btn.ShowAll ();
            btn.Clicked += btn_Clicked;

            return new Widget [] { btn };
        }

        private void btn_Clicked (object sender, EventArgs e)
        {
            EventHandler<EntitySuggestedEventArgs> handler = EntitySuggested;
            if (handler != null)
                handler (this, new EntitySuggestedEventArgs (new Business.Entities.Item { Code = value }));
        }
    }
}
