//
// WorkBookPage.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/11/2006
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

namespace Warehouse.Component.WorkBook
{
    public abstract class WorkBookPage : EventBox
    {
        public abstract string PageTitle
        {
            get;
        }

        public event KeyPressEventHandler OuterKeyPressed;
        public event EventHandler PageAdding;
        public event EventHandler PageAdded;
        public event EventHandler PageShown;
        public event EventHandler PageClose;
        public event EventHandler ViewModeChanged;

        public void OnOuterKeyPress (object o, KeyPressEventArgs args)
        {
            if (OuterKeyPressed != null)
                OuterKeyPressed (o, args);
        }

        public bool RequestClose ()
        {
            WorkBookPageCloseArgs args = new WorkBookPageCloseArgs ();
            OnPageCloseRequested (this, args);

            bool ret = !args.Canceled;

            if (ret)
                OnPageClose ();

            return ret;
        }

        protected virtual void OnPageCloseRequested (object sender, WorkBookPageCloseArgs args)
        {
        }

        public void Close ()
        {
            OnPageClose ();
        }

        protected virtual void OnPageClose ()
        {
            if (PageClose != null)
                PageClose (this, null);
        }

        public virtual void OnPageAdding ()
        {
            OnPageAddingFinish ();
        }

        protected virtual void OnPageAddingFinish ()
        {
            if (PageAdding != null)
                PageAdding (this, null);
        }

        public virtual void OnPageAdded ()
        {
            if (PageAdded != null)
                PageAdded (this, null);
        }

        public virtual void OnPageShown ()
        {
            if (PageShown != null)
                PageShown (this, null);
        }

        public virtual void OnViewModeChanged ()
        {
            if (ViewModeChanged != null)
                ViewModeChanged (this, null);
        }
    }
}