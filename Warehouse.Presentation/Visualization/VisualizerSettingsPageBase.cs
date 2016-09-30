//
// VisualizerSettingsPageBase.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/23/2008
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
using Gtk;
using Mono.Addins;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.Visualization
{
    [TypeExtensionPoint ("/Warehouse/Presentation/VisualizationSettings")]
    public abstract class VisualizerSettingsPageBase : EventBox
    {
        public abstract Widget PageLabel { get; }
        public abstract Notebook NotebookWidget { get; }
        public abstract string VisualizerTypeName { get; }
        public abstract VisualizerSettingsBase CurrentSettings { get; }

        public abstract bool LoadSettings (DataQueryResult result, VisualizerSettingsCollection settings);
        public abstract bool SaveSettings (VisualizerSettingsCollection settings);

        public event EventHandler SavingSettings;

        public static Widget CreateNewTabLabel (string text)
        {
            return CreateNewTabLabel (text, 4);
        }

        private static Widget CreateNewTabLabel (string text, uint padding)
        {
            Label lbl = new Label (text);
            lbl.Show ();

            Alignment alg = new Alignment (0, 0, 1, 1) { TopPadding = padding, LeftPadding = padding, RightPadding = padding, BottomPadding = padding };
            alg.Show ();
            alg.Add (lbl);
            alg.Show ();

            return alg;
        }

        protected ToggleButton CreateNewImageButton (string text, Image image, int minWidth)
        {
            image.Show ();

            Label lblText = new Label { Markup = new PangoStyle { Size = PangoStyle.TextSize.Small, Text = text }, Xalign = 1 };
            lblText.Show ();

            Label lblSpace = new Label { HeightRequest = 1, WidthRequest = minWidth };
            lblSpace.Show ();

            VBox vbxLabels = new VBox ();
            vbxLabels.PackStart (lblText, true, true, 0);
            vbxLabels.PackStart (lblSpace, false, false, 0);
            vbxLabels.Show ();

            HBox hboContents = new HBox (false, 3);
            hboContents.PackStart (image, false, true, 0);
            hboContents.PackStart (vbxLabels, true, true, 0);
            hboContents.Show ();

            ToggleButton button = new ToggleButton { hboContents };
            button.Show ();

            return button;
        }

        public void OnSavingSettings ()
        {
            if (SavingSettings != null)
                SavingSettings (this, EventArgs.Empty);
        }
    }
}
