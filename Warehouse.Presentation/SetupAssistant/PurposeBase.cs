//
// PurposeBase.cs
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
using Gtk;
using Mono.Addins;

namespace Warehouse.Presentation.SetupAssistant
{
    [TypeExtensionPoint ("/Warehouse/Presentation/SetupAssistant/Purpose")]
    public abstract class PurposeBase
    {
        protected RadioButton button;

        public abstract int Ordinal { get; }
        protected abstract string Label { get; }
        public virtual Image Image { get { return null; } }
        
        public bool Active
        {
            get { return button != null ? button.Active : false; }
            set
            {
                if (button != null)
                    button.Active = value;
            }
        }

        public event EventHandler Toggled;

        public virtual Widget GetSelectionWidget (ref RadioButton group)
        {
            button = new RadioButton (group, Label);
            button.Data ["PurposeBase"] = this;
            button.Toggled += OnToggled;
            if (group == null)
                group = button;

            return button;
        }

        protected virtual void OnToggled (object sender, EventArgs e)
        {
            EventHandler handler = Toggled;
            if (handler != null)
                handler (sender, e);
        }

        public abstract void Select ();
    }
}
