//
// SizeChooser.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   03/21/2007
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
using System.Globalization;
using System.Reflection;
using GLib;
using Gtk;

namespace Warehouse.Component
{
    public class SizeChooser : HBox
    {

        #region Private fields

        private string labelFormat = "{0} x {1}";
        private int columns = -1;
        private int rows = -1;
        private uint maxColumns;
        private uint maxRows;
        private int outerSpace = 2;
        private int innerSpace = 2;
        private Gdk.Color innerSelectedColor = new Gdk.Color (255, 226, 148);
        private Gdk.Color outerSelectedColor = new Gdk.Color (239, 72, 16);
        private EventHandler sizeChanged;

        private bool selectMode;
        private Button btnChoose;
        private Button [,] btnsSelect;
        private EventBox [,] dasSelect;
        private Table tblSelect;
        private Label lblMessage;

        #endregion

        #region Public properties

        public uint MaxColumns
        {
            get { return maxColumns; }
        }

        public uint MaxRows
        {
            get { return maxRows; }
        }

        public int Columns
        {
            get { return columns; }
            set { columns = value; UpdateLabel (); OnSizeChanged (); }
        }

        public int Rows
        {
            get { return rows; }
            set { rows = value; UpdateLabel (); OnSizeChanged (); }
        }

        public Gdk.Color InnerSelectedColor
        {
            get { return innerSelectedColor; }
            set { innerSelectedColor = value; }
        }

        public Gdk.Color OuterSelectedColor
        {
            get { return outerSelectedColor; }
            set { outerSelectedColor = value; }
        }

        public Label Label
        {
            get { return lblMessage; }
        }

        public string LabelFormat
        {
            get { return labelFormat; }
            set { labelFormat = value; UpdateLabel (); }
        }

        #endregion

        #region Public events

        public event EventHandler SizeChanged
        {
            add { sizeChanged += value; }
            remove { sizeChanged -= value; }
        }

        #endregion

        public SizeChooser (uint maxCols, uint maxRows, int cols, int rows)
            : this (maxCols, maxRows, null, null)
        {
            Columns = cols;
            Rows = rows;
        }

        public SizeChooser (uint maxCols, uint maxRows, Assembly imageAssembly, string imageResource)
        {
            maxColumns = maxCols;
            this.maxRows = maxRows;

            lblMessage = new Label { Markup = new PangoStyle { Bold = true, Size = PangoStyle.TextSize.Large, Text = labelFormat } };

            btnChoose = new Button (lblMessage);
            btnChoose.Clicked += btnChoose_Clicked;
            PackStart (btnChoose, true, true, 0);
            UpdateLabel ();

            tblSelect = new Table (maxRows * 2 + 1, maxCols * 2 + 1, false);
            PackStart (tblSelect, true, true, 0);

            btnsSelect = new Button [maxCols, maxRows];
            dasSelect = new EventBox [maxCols * 2 + 1, maxRows * 2 + 1];

            for (uint i = 0; i < maxCols; i++) {
                for (uint j = 0; j < maxRows; j++) {
                    EventBox daUpperLeft = new EventBox ();
                    EventBox daUpper = new EventBox ();
                    EventBox daLeft = new EventBox ();

                    daUpperLeft.WidthRequest = i == 0 ? outerSpace : innerSpace;

                    daUpperLeft.HeightRequest = j == 0 ? outerSpace : innerSpace;

                    tblSelect.Attach (daUpperLeft, i * 2, i * 2 + 1, j * 2, j * 2 + 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                    tblSelect.Attach (daLeft, i * 2, i * 2 + 1, j * 2 + 1, j * 2 + 2, AttachOptions.Fill, AttachOptions.Fill | AttachOptions.Expand, 0, 0);
                    tblSelect.Attach (daUpper, i * 2 + 1, i * 2 + 2, j * 2, j * 2 + 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 0);
                    dasSelect [i * 2, j * 2] = daUpperLeft;
                    dasSelect [i * 2 + 1, j * 2] = daUpper;
                    dasSelect [i * 2, j * 2 + 1] = daLeft;

                    Button btn;
                    if (imageAssembly != null && imageResource != null && imageResource.Length > 0)
                        btn = new Button (imageAssembly.LoadImage (imageResource));
                    else
                        btn = new Button ();

                    btn.HeightRequest = 5;
                    btn.WidthRequest = 5;
                    tblSelect.Attach (btn, i * 2 + 1, i * 2 + 2, j * 2 + 1, j * 2 + 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                    btnsSelect [i, j] = btn;
                    btn.Clicked += btnSelect_Clicked;
                    btn.Entered += btn_Entered;

                    if (maxCols == i + 1) {
                        EventBox daUpperRight = new EventBox ();
                        EventBox daRight = new EventBox ();
                        daUpperRight.WidthRequest = outerSpace;
                        daRight.WidthRequest = outerSpace;
                        tblSelect.Attach (daUpperRight, i * 2 + 2, i * 2 + 3, j * 2, j * 2 + 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                        tblSelect.Attach (daRight, i * 2 + 2, i * 2 + 3, j * 2 + 1, j * 2 + 2, AttachOptions.Fill, AttachOptions.Fill | AttachOptions.Expand, 0, 0);
                        dasSelect [i * 2 + 2, j * 2] = daUpperRight;
                        dasSelect [i * 2 + 2, j * 2 + 1] = daRight;
                    }

                    if (maxRows == j + 1) {
                        EventBox daBottomLeft = new EventBox ();
                        EventBox daBottom = new EventBox ();
                        daBottomLeft.HeightRequest = outerSpace;
                        daBottom.HeightRequest = outerSpace;
                        tblSelect.Attach (daBottomLeft, i * 2, i * 2 + 1, j * 2 + 2, j * 2 + 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                        tblSelect.Attach (daBottom, i * 2 + 1, i * 2 + 2, j * 2 + 2, j * 2 + 3, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 0);
                        dasSelect [i * 2, j * 2 + 2] = daBottomLeft;
                        dasSelect [i * 2 + 1, j * 2 + 2] = daBottom;
                    }

                    if ((maxCols == i + 1) && (maxRows == j + 1)) {
                        EventBox daBottomRight = new EventBox { WidthRequest = outerSpace, HeightRequest = outerSpace };
                        tblSelect.Attach (daBottomRight, i * 2 + 2, i * 2 + 3, j * 2 + 2, j * 2 + 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                        dasSelect [i * 2 + 2, j * 2 + 2] = daBottomRight;
                    }
                }
            }
        }

        private void btn_Entered (object sender, EventArgs e)
        {
            uint i, j;
            uint selCol = 0;
            uint selRow = 0;

            for (i = 0; i < maxColumns; i++) {
                for (j = 0; j < maxRows; j++) {
                    if (!ReferenceEquals (sender, btnsSelect [i, j]))
                        continue;
                    
                    selCol = i * 2 + 2;
                    selRow = j * 2 + 2;
                    break;
                }
            }

            for (i = 0; i < maxColumns * 2 + 1; i++) {
                for (j = 0; j < maxRows * 2 + 1; j++) {
                    EventBox da = dasSelect [i, j];
                    if (da == null)
                        continue;

                    if (i > 0 && j > 0 && i < selCol && j < selRow)
                        da.ModifyBg (StateType.Normal, innerSelectedColor);
                    else if (i >= 0 && j >= 0 && i <= selCol && j <= selRow)
                        da.ModifyBg (StateType.Normal, outerSelectedColor);
                    else
                        da.ModifyBg (StateType.Normal);
                }
            }
        }

        private void btnSelect_Clicked (object sender, EventArgs args)
        {
            for (int i = 0; i < maxColumns; i++) {
                for (int j = 0; j < maxRows; j++) {
                    if (!ReferenceEquals (sender, btnsSelect [i, j]))
                        continue;
                    
                    columns = i + 1;
                    rows = j + 1;

                    UpdateLabel ();
                    selectMode = false;
                    tblSelect.Hide ();
                    btnChoose.Show ();
                    OnSizeChanged ();
                    break;
                }
            }
        }

        private void btnChoose_Clicked (object sender, EventArgs e)
        {
            selectMode = true;
            btnChoose.Hide ();
            tblSelect.ShowAll ();
        }

        private void UpdateLabel ()
        {
            string columnsString = columns > 0 ? columns.ToString (CultureInfo.InvariantCulture) : "?";
            string rowsString = rows > 0 ? rows.ToString (CultureInfo.InvariantCulture) : "?";

            lblMessage.SetText (string.Format (labelFormat, columnsString, rowsString));
        }

        private void OnSizeChanged ()
        {
            if (sizeChanged != null)
                sizeChanged (this, EventArgs.Empty);
        }

        protected override void OnShown ()
        {
            if (selectMode)
                btnChoose.Hide ();
            else
                tblSelect.Hide ();

            base.OnShown ();
        }
    }
}
