//
// CustomStatusBar.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   05.23.2012
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
using System.Text;
using Gdk;
using Gtk;
using Mono.Addins;
using Pango;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Data;

namespace Warehouse.Presentation.Widgets
{
    public class CustomStatusBar : Statusbar
    {
        private readonly Frame customFrame;
        private readonly Label lblInfo;
        private readonly Label lblNotification;
        private readonly IStatusWidget [] customWidgets;

        public CustomStatusBar ()
        {
            Frame originalFrame = (Frame) Children [0];
            originalFrame.HideAll ();

            customFrame = new Frame { ShadowType = ShadowType.None };
            PackStart (customFrame, true, true, 0);

            HBox contents = new HBox ();
            customFrame.Add (contents);

            lblInfo = new Label { Ellipsize = EllipsizeMode.End, Xalign = 0 };
            contents.PackStart (lblInfo, true, true, 0);

            lblNotification = new Label { Ellipsize = EllipsizeMode.End, Xalign = 0 };
            contents.PackStart (lblNotification, true, true, 0);

            customWidgets = AddinManager.GetExtensionNodes ("/Warehouse/Presentation/StatusWidget")
                .Cast<TypeExtensionNode> ()
                .Select (node => (IStatusWidget) node.CreateInstance ())
                .OrderByDescending (w => w.Order)
                .ToArray ();

            foreach (IStatusWidget widget in customWidgets)
                contents.PackEnd (widget.Widget, false, true, 2);

            customFrame.ShowAll ();
            lblNotification.Visible = false;

            if (PlatformHelper.Platform == PlatformTypes.MacOSX) {
                HasResizeGrip = true;

                SizeAllocated += delegate { QueueDraw (); };
            } else {
                if (GdkWindow != null && GdkWindow.State == WindowState.Maximized)
                    HasResizeGrip = false;

                SizeAllocated += delegate
                    {
                        if (GdkWindow != null)
                            HasResizeGrip = GdkWindow.State != WindowState.Maximized;

                        QueueDraw ();
                    };
            }
        }

        public void Refresh (bool hasDbConnection = true)
        {
            string delimiter = PresentationDomain.ScreenResolution >= ScreenResolution.Normal ?
                "\t\t" : "\t";

            StringBuilder text = new StringBuilder ();
            if (hasDbConnection) {
                CompanyRecord record = BusinessDomain.CurrentCompany;
                if (record != null)
                    text.AppendFormat ("\t{0}: {1}", Translator.GetString ("Company"), record.Name);

                User user = BusinessDomain.LoggedUser;
                if (user.IsSaved)
                    text.AppendFormat ("{2}{0}: {1}", Translator.GetString ("User"), user.Name, delimiter);

                bool usesServer = BusinessDomain.DataAccessProvider.UsesServer;
                string database = BusinessDomain.AppConfiguration.DbDatabase;
                string server = BusinessDomain.AppConfiguration.DbServer;
                text.AppendFormat ("{2}{0}: {1}", Translator.GetString ("Database"), usesServer ?
                    string.Format ("{0}@{1}", database, server) : database, delimiter);
            }

            lblInfo.Text = text.ToString ();

            foreach (IStatusWidget widget in customWidgets)
                widget.Refresh ();
        }

        public void ShowNotification (string notification)
        {
            lblNotification.Markup = string.Format ("<b>{0}</b>", notification);
            lblNotification.Visible = true;
            GLib.Timeout.Add (2000, () =>
                {
                    lblNotification.Visible = false;
                    return false;
                });
        }
    }
}
