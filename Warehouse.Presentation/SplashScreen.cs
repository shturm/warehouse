//
// SplashScreen.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   08/17/2006
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

using System.Reflection;
using Gtk;
using Warehouse.Business;
using Warehouse.Component;
using Thread = System.Threading.Thread;

namespace Warehouse.Presentation
{
    public class SplashScreen : Window
    {
        private static ProgressBar progress;
        private static VBox vbox;
        private static Gdk.Pixbuf bitmap;
        private static Label label;
        private static SplashScreen splashWindow;
        private static string colorText;

        private SplashScreen (Assembly resourceAssembly)
            : base (WindowType.Popup)
        {
            AppPaintable = true;
            Decorated = false;
            WindowPosition = WindowPosition.Center;
            TypeHint = Gdk.WindowTypeHint.Splashscreen;
            bitmap = new Gdk.Pixbuf (resourceAssembly, string.Format ("{0}.SplashScreen.png", resourceAssembly.GetName ().Name));

            progress = new ProgressBar { Fraction = 0.00, HeightRequest = 6, WidthRequest = 400 };

            HBox hbox = new HBox ();
            hbox.PackStart (progress, false, true, 0);

            vbox = new VBox { BorderWidth = 12 };
            label = new Label { Xalign = 0 };
            vbox.PackEnd (hbox, false, true, 0);
            vbox.PackEnd (label, false, true, 3);
            Add (vbox);

            Resize (bitmap.Width, bitmap.Height);
        }

        protected override bool OnExposeEvent (Gdk.EventExpose evt)
        {
            Gdk.GC gc = Style.LightGC (StateType.Normal);
            GdkWindow.DrawPixbuf (gc, bitmap, 0, 0, 0, 0, bitmap.Width, bitmap.Height, Gdk.RgbDither.None, 0, 0);
            return base.OnExposeEvent (evt);
        }

        private static bool Init (Assembly resourceAssembly)
        {
            if (splashWindow == null)
                splashWindow = new SplashScreen (resourceAssembly);

            return true;
        }

        public static void SetProgress (double percentage)
        {
            if (!BusinessDomain.AppConfiguration.ShowSplashScreen)
                return;

            SetProgressBar (percentage / 100.0d);
        }

        private static void SetProgressBar (double value)
        {
            if (splashWindow == null)
                return;

            if (value > 1.0)
                return;

            progress.Fraction = value;
            PresentationDomain.ProcessUIEvents ();
        }

        public static void SetMessage (string message)
        {
            if (splashWindow == null)
                return;

            label.Markup = new PangoStyle { Size = PangoStyle.TextSize.Small, ColorText = colorText, Text = message };
            PresentationDomain.ProcessUIEvents ();
        }

        public static void ShowSplash (Assembly resourceAssembly, string textColor)
        {
            if (BusinessDomain.AppConfiguration != null && !BusinessDomain.AppConfiguration.ShowSplashScreen)
                return;

            colorText = textColor;
            if (!Init (resourceAssembly))
                return;

            AutoStartupNotification = false;
            splashWindow.ShowAll ();
            PresentationDomain.ProcessUIEvents ();

            SetProgressBar (0.0d);
        }

        public static void HideSplash ()
        {
            if (splashWindow == null)
                return;

            if (BusinessDomain.AppConfiguration == null)
                return;

            if (!BusinessDomain.AppConfiguration.ShowSplashScreen)
                return;

            SetProgressBar (1.0d);
            PresentationDomain.ProcessUIEvents ();

            if (PresentationDomain.MainFormCreated)
                Thread.Sleep (2000);
            else
                Thread.Sleep (400);

            splashWindow.Dispose ();
            splashWindow = null;
            AutoStartupNotification = true;
        }

        #region IDisposable Members

        public override void Dispose ()
        {
            Hide ();
            base.Dispose ();
        }

        #endregion
    }
}