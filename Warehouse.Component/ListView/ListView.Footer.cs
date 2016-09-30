//
// ListView.Footer.cs
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

namespace Warehouse.Component.ListView
{
    public partial class ListView
    {
        private ListViewRowHeightHandler footer_height_handler;
        public virtual ListViewRowHeightHandler FooterHeightProvider
        {
            get { return footer_height_handler; }
            set
            {
                if (value != footer_height_handler) {
                    footer_height_handler = value;
                    footer_height = 0;
                }
            }
        }

        private int footer_height = 0;
        private int FooterHeight
        {
            get
            {
                if (!footer_visible) {
                    return 0;
                }

                if (footer_height == 0) {
                    footer_height = footer_height_handler != null
                        ? footer_height_handler (this)
                        : CellText.ComputeRowHeight (this, FooterStyleProvider) + 2;
                }

                return footer_height;
            }
        }

        private bool footer_visible = false;
        public bool FooterVisible
        {
            get { return footer_visible; }
            set
            {
                footer_visible = value;
                RecalculateWindowSizes (Allocation);
                if (vadjustment != null) {
                    vadjustment.PageSize = list_rendering_alloc.Height;
                    vadjustment.PageIncrement = list_rendering_alloc.Height;
                    UpdateAdjustments (null, null);
                }
                QueueDraw ();
            }
        }
    }
}
