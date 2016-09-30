//
// PictureToggleButton.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   03.22.2011
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
using Warehouse.Component;

namespace Warehouse.Presentation.Widgets
{
    public class PictureToggleButton : ToggleButton
    {
        private readonly Alignment algIcon;
        private readonly Label lblText;

        public PictureToggleButton ()
        {
            HBox hBox = new HBox { Spacing = 4 };
            hBox.Show ();
            Add (hBox);

            algIcon = new Alignment (0.5f, 0.5f, 1f, 1f);
            algIcon.Show ();
            hBox.PackStart (algIcon, false, true, 0);

            VBox vBox = new VBox ();
            vBox.Show ();
            hBox.PackStart (vBox, true, true, 0);

            lblText = new Label { Markup = "<span size=\"small\"></span>", Justify = Justification.Right, Xalign = 1f };
            lblText.Show ();
            vBox.PackStart (lblText, true, false, 6);

            Label lblFill = new Label { HeightRequest = 1, WidthRequest = 80 };
            lblFill.Show ();
            vBox.PackStart (lblFill, false, false, 0);
        }

        public void SetImage (Image img)
        {
            algIcon.DestroyChild ();
            img.Show ();
            algIcon.Add (img);
        }

        public void SetText (string text)
        {
            lblText.SetText (text);
        }
    }
}
