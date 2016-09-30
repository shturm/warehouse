//
// DialogBase.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   02/28/2007
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
using Warehouse.Business;
using Warehouse.Component;
using Warehouse.Data;

namespace Warehouse.Presentation.Dialogs
{
    public abstract class DialogBase : IDisposable
    {
        private bool hidden = true;
        private int responceHandlers;

        /// <summary>
        /// Gets the dialog widget used by the <see cref="DialogBase"/>.
        /// </summary>
        /// <value>The dialog widget used by the <see cref="DialogBase"/>.</value>
        public abstract Dialog DialogControl
        {
            get;
        }

        public virtual string HelpFile
        {
            get { return GetType ().Name + ".html"; }
        }

        public event ResponseHandler Response
        {
            add
            {
                DialogControl.Response += value;
                responceHandlers++;
                OnResponseHandlerChanged (true);
            }
            remove
            {
                DialogControl.Response -= value;
                responceHandlers--;
                OnResponseHandlerChanged (responceHandlers > 0);
            }
        }

        protected void Initialize ()
        {
            try {
                InitializeForm ();
            } catch {
                if (DialogControl != null) {
                    PopModalDialog ();
                    Hide ();
                }
                throw;
            }
        }

        /// <summary>
        /// Initializes the form by reading its *.glade file and setting other necessary properties.
        /// </summary>
        protected virtual void InitializeForm ()
        {
            DialogControl.KeyPressEvent += DialogControl_KeyPressEvent;
            // HACK: GTK Mac has a bug which causes clicking on widgets not to work when a window is maximized
            if (PlatformHelper.Platform == PlatformTypes.MacOSX)
                DialogControl.EnterNotifyEvent += DialogControl_WidgetEvent;
        }

        /// <summary>
        /// Initializes the strings contained in the <see cref="DialogBase"/>.
        /// </summary>
        protected virtual void InitializeFormStrings ()
        {
        }

        private void DialogControl_KeyPressEvent (object o, KeyPressEventArgs args)
        {
            if (!KeyShortcuts.Equal (args.Event, KeyShortcuts.HelpKey))
                return;

            if (string.IsNullOrEmpty (HelpFile))
                return;

            FormHelper.ShowWindowHelp (HelpFile);
        }

        private void DialogControl_WidgetEvent (object o, EnterNotifyEventArgs args)
        {
            int x;
            int y;
            DialogControl.GetPosition (out x, out y);
            Gdk.Window drawable = DialogControl.GdkWindow;
            int titleBarHeight = drawable.FrameExtents.Height - drawable.ClipRegion.Clipbox.Height;
            DialogControl.Move (x, y + titleBarHeight);
        }

        public virtual void Show ()
        {
            DialogControl.Shown -= OnShown;
            DialogControl.Shown += OnShown;
            DialogControl.DeleteEvent += DialogControl_DeleteEvent;
            if (hidden)
                PushModalDialog ();
            DialogControl.Show ();
        }

        public virtual void Hide ()
        {
            DialogControl.Hidden -= DialogControl_Hidden;
            DialogControl.Hidden += DialogControl_Hidden;
            DialogControl.Hide ();
        }

        public virtual ResponseType Run ()
        {
            try {
                DialogControl.Shown -= OnShown;
                DialogControl.Shown += OnShown;
                DialogControl.Hidden -= DialogControl_Hidden;
                DialogControl.Hidden += DialogControl_Hidden;
                DialogControl.DeleteEvent += DialogControl_DeleteEvent;
                if (hidden)
                    PushModalDialog ();

                ResponseType ret = (ResponseType) DialogControl.Run ();
                Hide ();

                return ret;
#if !DEBUG
            } catch {
                ComponentHelper.PopModal (DialogControl);
                Hide ();
                throw;
#endif
            } finally {
                DialogControl.DeleteEvent -= DialogControl_DeleteEvent;
                DialogControl.Hidden -= DialogControl_Hidden;
                DialogControl.Shown -= OnShown;
            }
        }

        protected virtual void OnShown (object sender, EventArgs e)
        {
            hidden = false;
        }

        private void DialogControl_Hidden (object sender, EventArgs e)
        {
            PopModalDialog ();
            hidden = true;
            OnClosing ();
        }

        private void DialogControl_DeleteEvent (object o, DeleteEventArgs args)
        {
            PopModalDialog ();
            hidden = true;
            OnClosing ();
        }

        protected virtual void OnResponseHandlerChanged (bool hasHandler)
        {
        }

        protected virtual void OnClosing ()
        {
        }

        protected virtual bool SaveDialogSettings
        {
            get { return true; }
        }

        protected string GetDialogId ()
        {
            return GetType ().Name;
        }

        private void PopModalDialog ()
        {
            PopModalDialog (DialogControl, SaveDialogSettings ? GetDialogId () : null);
            PresentationDomain.OSIntegration.OnModalDialogRemoved ();
        }

        public static void PopModalDialog (Window window, string windowId)
        {
            window.PopModal ();
            if (string.IsNullOrEmpty (windowId))
                return;

            WindowSettings settings = WindowSettings.GetByWindowId (windowId) ?? new WindowSettings { WindowId = windowId };
            settings.Width = window.Allocation.Width;
            settings.Height = window.Allocation.Height;
            settings.Panes.Clear ();
            foreach (Paned pane in ComponentHelper.GetChildWidgetsByType<Paned> (window))
                settings.Panes.Add (new PaneSettings { Name = pane.Name, Position = pane.Position });

            ColumnSerializer.SaveColumnSettings (window, settings);
        }

        private void PushModalDialog ()
        {
            PushModalDialog (DialogControl, SaveDialogSettings ? GetDialogId () : null);
            PresentationDomain.OSIntegration.OnModalDialogCreated ();
        }

        public static void PushModalDialog (Window window, string windowId)
        {
            window.PushModal ();
            if (string.IsNullOrEmpty (windowId))
                return;

            WindowSettings settings = WindowSettings.GetByWindowId (windowId);
            if (settings == null)
                return;

            window.Resize (settings.Width, settings.Height);
            foreach (Paned pane in ComponentHelper.GetChildWidgetsByType<Paned> (window)) {
                PaneSettings pair = settings.Panes.FirstOrDefault (p => p.Name == pane.Name);
                if (pair != null)
                    pane.Position = pair.Position;
            }

            ColumnSerializer.LoadColumnSettings (window, settings);
        }

        #region IDisposable Members

        public virtual void Dispose ()
        {
            Dialog dialog = DialogControl;
            if (!hidden) {
                DialogControl_Hidden (null, null);
                dialog.Hide ();
            }

            dialog.Dispose ();
            dialog.Destroy ();
            Disposing ();
        }

        protected virtual void Disposing ()
        {
        }

        #endregion
    }
}
