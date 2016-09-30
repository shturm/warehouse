//
// ReportEntryBase.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   08/15/2006
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

using System.Linq;
using Gtk;

namespace Warehouse.Presentation.Reporting
{
    public abstract class ReportEntryBase
    {
        private VBox vbxSpacer;
        protected readonly HBox label = new HBox ();
        protected readonly HBox entry = new HBox ();
        protected Label lblFieldName;
        protected CheckButton chkColumnVisible;

        public HBox Label
        {
            get
            {
                if (vbxSpacer != null)
                    label.Remove (vbxSpacer);

                return label;
            }
        }

        public HBox LabelWidget
        {
            get
            {
                if (vbxSpacer != null && label.Children.All (c => !ReferenceEquals (c, vbxSpacer))) {
                    label.PackStart (vbxSpacer);
                    label.ReorderChild (vbxSpacer, 0);
                }

                return label;
            }
        }

        public HBox EntryWidget
        {
            get { return entry; }
        }

        public virtual bool ColumnVisible
        {
            get { return true; }
            set { }
        }

        protected virtual void InitializeLabel ()
        {
            vbxSpacer = new VBox { WidthRequest = 21 };
            label.PackStart (vbxSpacer, false, true, 0);

            lblFieldName = new Label { Markup = "<span color=\"black\" ></span>" };
            label.PackStart (lblFieldName, false, true, 0);
        }
    }
}
