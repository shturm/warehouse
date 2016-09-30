//
// ComponentHelper.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/04/2007
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Gdk;
using GLib;
using Gtk;
using Mono.Addins;
using Pango;
using Warehouse.Business;
using Warehouse.Business.Documenting;
using Warehouse.Component.Documenting;
using Warehouse.Data;
using Warehouse.Data.DataBinding;
using Image = Gtk.Image;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;
using Window = Gtk.Window;
using System.Xml.XPath;

namespace Warehouse.Component
{
    public static class ComponentHelper
    {
        private static readonly Stack<Window> windows = new Stack<Window> ();
        private static Dictionary<string, CustomFormProvider> addonForms;

        public static Dictionary<string, CustomFormProvider> AddonForms
        {
            get
            {
                if (addonForms == null) {
                    addonForms = new Dictionary<string, CustomFormProvider> ();

                    foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Component/Documenting/FromTemplate")) {
                        CustomFormProvider formTemplate = node.CreateInstance () as CustomFormProvider;
                        if (formTemplate == null)
                            continue;

                        addonForms.Add (formTemplate.FormName, formTemplate);
                    }
                }

                return addonForms;
            }
        }

        public static Stack<Window> Windows
        {
            get { return windows; }
        }

        #region Store helper methods

        private static ListStore GetListStoreFromObject (object source, out BindManager bManager, string textMember, string valueMember)
        {
            int i;
            ListStore store;

            if (string.IsNullOrEmpty (textMember) && !string.IsNullOrEmpty (valueMember))
                throw new Exception ("The text member can not be null when a value member is specified!");

            bManager = new BindManager (source);
            if (string.IsNullOrEmpty (textMember) && string.IsNullOrEmpty (valueMember)) {
                store = new ListStore (bManager.Rows.RowType);

                for (i = 0; i < bManager.Rows.Count; i++)
                    store.AppendValues (bManager.Rows [i].Value);
            } else {
                bool emptyValuePath = string.IsNullOrEmpty (valueMember);
                Type [] colTypes = bManager.Columns.GetColumnTypes ();
                int textColumn = bManager.Columns [textMember].Index;
                if (emptyValuePath) {
                    store = new ListStore (typeof (object), colTypes [textColumn]);
                    for (i = 0; i < bManager.Rows.Count; i++) {
                        BindRow<object> rowObjects = bManager.Rows [i];
                        store.AppendValues (rowObjects.Value, rowObjects [textColumn]);
                    }
                } else {
                    int valueColumn = bManager.Columns [valueMember].Index;
                    store = new ListStore (colTypes [valueColumn], colTypes [textColumn]);

                    for (i = 0; i < bManager.Rows.Count; i++) {
                        object [] rowObjects = bManager.Rows [i].ToArray ();
                        store.AppendValues (rowObjects [valueColumn], rowObjects [textColumn]);
                    }
                }
            }

            return store;
        }

        #endregion

        #region Image helper methods

        public static void SetChildImage (this Widget mainWidget, Image image, string childWidgetName = null)
        {
            if (!(mainWidget is Container))
                throw new Exception ("Can not set child widget. The main widget is not a Gtk.Container widget.");

            if (!SetChildImage (mainWidget, image, 5, childWidgetName ?? string.Empty))
                throw new Exception ("Cannot find a suitable child widget to attach the given widget.");
        }

        private static bool SetChildImage (Widget mainWidget, Image image, int currentDepth, string childWidgetName)
        {
            bool ret = false;

            if (mainWidget is Image && (childWidgetName == string.Empty || childWidgetName == mainWidget.Name)) {
                SetImage ((Image) mainWidget, image);
                ret = true;
            } else if (currentDepth > 0) {
                if (mainWidget is Container) {
                    Container cont = mainWidget as Container;
                    --currentDepth;
                    if (cont.Children.Length == 0) {
                        cont.Add (image);
                        image.Show ();
                        ret = true;
                    } else if (cont.Children.Length == 1 && cont.Children [0] is Image) {
                        cont.Remove (cont.Children [0]);
                        cont.Add (image);
                        image.Show ();
                        ret = true;
                    } else
                        foreach (Widget child in cont.Children) {
                            ret = SetChildImage (child, image, currentDepth, childWidgetName);
                            if (ret)
                                break;
                        }
                }
            }

            return ret;
        }

        private static void SetImage (Image targetImage, Image sourceImage)
        {
            targetImage.SetFromImage (sourceImage.ImageProp, sourceImage.Pixmap);
            targetImage.Show ();
        }

        #endregion

        #region Label helper methods

        public static void SetChildLabelText (this Widget mainWidget, string newText, string childWidgetName = null)
        {
            if (!(mainWidget is Container))
                throw new Exception ("Can not set child widget. The main widget is not a Gtk.Container widget.");

            if (childWidgetName == null)
                childWidgetName = string.Empty;

            if (!SetChildLabelText (mainWidget, newText, 5, childWidgetName))
                throw new Exception ("Cannot find a suitable child widget to attach the given widget.");
        }

        private static bool SetChildLabelText (Widget mainWidget, string newText, int currentDepth, string childWidgetName)
        {
            bool ret = false;

            if (mainWidget is Label && (childWidgetName == string.Empty || childWidgetName == mainWidget.Name)) {
                SetText (mainWidget as Label, newText);
                ret = true;
            } else if (currentDepth > 0) {
                if (mainWidget is Container) {
                    --currentDepth;
                    foreach (Widget child in (mainWidget as Container).Children) {
                        ret = SetChildLabelText (child, newText, currentDepth, childWidgetName);
                        if (ret)
                            break;
                    }
                }
            }

            return ret;
        }

        public static void SetText (this Label label, string newValue)
        {
            if (label.UseMarkup == false) {
                label.Text = newValue;
                return;
            }

            string [] markup = label.LabelProp.Split ('<', '>');
            if (markup.Length == 5) {
                StringBuilder sb = new StringBuilder ();

                sb.Append ('<');
                sb.Append (markup [1]);
                sb.Append ('>');
                sb.Append (Markup.EscapeText (newValue));
                sb.Append ('<');
                sb.Append (markup [3]);
                sb.Append ('>');

                label.Markup = sb.ToString ();
            } else {
                label.Text = newValue;
            }
        }

        #endregion

        #region ComboBox helper methods

        private static int GetSelectedRow (BindManager bManager, string valueMember, object selectedValue)
        {
            for (int i = 0; i < bManager.Rows.Count; i++) {
                object rowObject;
                if (bManager.Columns.NamedColumns && !string.IsNullOrEmpty (valueMember))
                    rowObject = bManager.Rows [i] [valueMember];
                else
                    rowObject = bManager.Rows [i].Value;

                if (Equals (selectedValue, rowObject))
                    return i;
            }
            return 0;
        }

        public static void Load (this ComboBox combo, object source, string valueMember, string textMember, object selectedValue)
        {
            Load (combo, source, valueMember, textMember, true, selectedValue);
        }

        public static void Load (this ComboBox combo, object source, string valueMember, string textMember, bool selectValue = true)
        {
            Load (combo, source, valueMember, textMember, selectValue, Missing.Value);
        }

        public static void Load (this ComboBox combo, object source, string valueMember, string textMember, bool selectValue, object selectedValue)
        {
            if (combo == null)
                return;

            if (source == null) {
                combo.Model = null;
                return;
            }

            try {
                int textColumn = string.IsNullOrEmpty (textMember) && string.IsNullOrEmpty (valueMember) ?
                    0 : 1;

                if (combo.Model != null)
                    combo.Clear ();

                ComboBoxEntry cbe = combo as ComboBoxEntry;
                if (cbe == null) {
                    CellRendererText renderer = new CellRendererText ();
                    combo.PackStart (renderer, true);
                    combo.AddAttribute (renderer, "text", textColumn);
                }

                BindManager bManager;
                ListStore model = GetListStoreFromObject (source, out bManager, textMember, valueMember);
                combo.Model = model;

                if (cbe != null) {
                    // Dirty patch to handle the possible bad behavior of the ComboBoxEntry
                    try {
                        cbe.TextColumn = textColumn;
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                        cbe.TextColumn = textColumn;
                    }
                }

                int selectedRow = selectValue ? GetSelectedRow (bManager, valueMember, selectedValue) : int.MaxValue;
                if (bManager.Count <= selectedRow)
                    return;

                if (cbe != null) {
                    string value = string.IsNullOrEmpty (textMember) ?
                        (string) bManager.Rows [selectedRow].Value :
                        (string) bManager.Rows [selectedRow] [textMember];

                    cbe.Entry.Text = value ?? string.Empty;
                } else {
                    TreeIter iter;
                    model.GetIterFromString (out iter, selectedRow.ToString (CultureInfo.InvariantCulture));
                    combo.SetActiveIter (iter);
                }
            } catch (Exception ex) {
                ErrorHandling.LogException (ex);
                combo.Clear ();
            }
        }

        public static object GetSelectedValue (this ComboBox combo)
        {
            TreeIter ti;

            if (combo.Model == null)
                return null;

            if (combo.Model.IterNChildren () == 0)
                return null;

            combo.GetActiveIter (out ti);
            return combo.Model.GetValue (ti, 0);
        }

        public static string GetSelectedText (this ComboBox combo)
        {
            TreeIter ti;

            if (combo.Model == null)
                return combo is ComboBoxEntry ? ((ComboBoxEntry) combo).Entry.Text : null;

            combo.GetActiveIter (out ti);
            return (string) combo.Model.GetValue (ti, combo.Model.NColumns - 1);
        }

        public static int GetSelection (this ComboBox combo)
        {
            if (combo.Model == null)
                return -1;

            TreeIter ti;
            combo.GetActiveIter (out ti);
            TreePath tp = combo.Model.GetPath (ti);

            return tp.Indices [0];
        }

        public static void SetSelection (this ComboBox combo, object source, string valueMember, string textMember, object selectedValue)
        {
            if (combo == null)
                return;

            if (source == null) {
                combo.Model = null;
                return;
            }

            BindManager bManager = new BindManager (source);
            int selectedRow = GetSelectedRow (bManager, valueMember, selectedValue);

            try {
                ComboBoxEntry cbe = combo as ComboBoxEntry;
                if (cbe != null) {
                    string value = textMember == null ?
                        (string) bManager.Rows [selectedRow].Value :
                        (string) bManager.Rows [selectedRow] [textMember];

                    cbe.Entry.Text = value ?? string.Empty;
                } else {
                    if (combo.Model == null)
                        return;

                    TreeIter iter;
                    combo.Model.GetIterFromString (out iter, selectedRow.ToString (CultureInfo.InvariantCulture));
                    combo.SetActiveIter (iter);
                }
            } catch {
                combo.Clear ();
            }
        }

        public static void SetSelection (this ComboBox combo, int selectedRow)
        {
            TreeIter iter;

            if (combo == null || combo.Model == null)
                return;

            combo.Model.GetIterFromString (out iter, selectedRow.ToString (CultureInfo.InvariantCulture));
            combo.SetActiveIter (iter);
        }

        public static void SetSelection (this ComboBox combo, object selectedItem)
        {
            combo.Model.Foreach ((model, path, iter) =>
                {
                    object value = model.GetValue (iter, 0);
                    if ((selectedItem == null && value == null) ||
                        (selectedItem != null && selectedItem.Equals (value))) {
                        combo.SetActiveIter (iter);
                        return true;
                    }
                    return false;
                });
        }

        public static void SetElipsize (this ComboBox combo, EllipsizeMode mode = EllipsizeMode.End)
        {
            if (combo == null || combo.Cells.Length == 0)
                return;

            CellRendererText cellRendererText = combo.Cells [0] as CellRendererText;
            if (cellRendererText == null)
                return;

            cellRendererText.Ellipsize = mode;
        }

        #endregion

        #region Resources management methods

        public static Glade.XML LoadGladeXML (this Assembly resourceAssembly, string resourceName, string rootWidget)
        {
            try {
                return new Glade.XML (resourceAssembly, resourceName, rootWidget, null);
            } catch (Exception ex) {
                throw new Exception ("Unable to load glade xml :" + resourceName, ex);
            }
        }

        public static Image LoadImage (string resourceName)
        {
            return new Image (Assembly.GetExecutingAssembly (), resourceName);
        }

        public static Image LoadImage (this Assembly resourceAssembly, string resourceName)
        {
            return new Image (resourceAssembly, resourceName);
        }

        public static Image LoadLocalImage (string resourceName)
        {
            return new Image (Assembly.GetCallingAssembly (), resourceName);
        }

        public static Image LoadAnimation (this Assembly resourceAssembly, string resourceName)
        {
            return new Image { PixbufAnimation = new PixbufAnimation (resourceAssembly, resourceName) };
        }

        public static XDocument LoadXML (this Assembly resourceAssembly, string resourceName)
        {
            using (Stream stream = resourceAssembly.GetManifestResourceStream (resourceName)) {
                if (stream == null)
                    return null;

                return XDocument.Load (stream);
            }
        }

        #endregion

        #region Language management options

        public static IEnumerable<CultureInfo> GetAvailableLocales (string packageName, string localeFolder)
        {
            List<CultureInfo> locales = new List<CultureInfo> { new CultureInfo ("en") };

            if (Directory.Exists (localeFolder)) {
                foreach (string directory in Directory.GetDirectories (localeFolder)) {
                    string dirName = Path.GetFileName (directory);
                    if (dirName == null || !IsValidLocaleFolder (dirName))
                        continue;

                    string moFilePath = Path.Combine (Path.Combine (directory, "LC_MESSAGES"), packageName + ".mo");
                    if (!File.Exists (moFilePath))
                        continue;

                    try {
                        locales.Add (new CultureInfo (dirName.Replace ("_", "-")));
                    } catch (ArgumentException) {
                    }
                }
            }

            return locales;
        }

        private static bool IsValidLocaleFolder (string dirName)
        {
            switch (dirName.Length) {
                case 2:
                    if (char.IsLetter (dirName [0]) &&
                        char.IsLetter (dirName [1])) {
                        return true;
                    }
                    break;
                case 5:
                    if (char.IsLetter (dirName [0]) &&
                        char.IsLetter (dirName [1]) &&
                        dirName [2] == '_' &&
                        char.IsLetter (dirName [3]) &&
                        char.IsLetter (dirName [4])) {
                        return true;
                    }
                    break;
            }

            return false;
        }

        #endregion

        #region Dialog helper methods

        public static Window TopWindow
        {
            get { return windows.Count > 0 ? windows.Peek () : null; }
        }

        public static void PushModal (this Window dialog)
        {
            if (windows.Count > 0) {
                // Prevent double push
                Window parentWindow = windows.Peek ();
                if (ReferenceEquals (parentWindow, dialog))
                    return;

                dialog.Modal = true;
                dialog.TransientFor = parentWindow;

                parentWindow.Sensitive = false;
            }

            windows.Push (dialog);
            dialog.FocusInEvent += dialog_FocusInEvent;
        }

        public static void PopModal (this Window dialog)
        {
            if (windows.Count == 0)
                return;

            // Prevent double pop
            if (!ReferenceEquals (windows.Peek (), dialog))
                return;

            windows.Pop ();
            dialog.FocusInEvent -= dialog_FocusInEvent;

            if (windows.Count == 0)
                return;

            Window parentWindow = windows.Peek ();
            // Don't present intentially hidden windows (like the main form)
            if (!parentWindow.Visible)
                return;

            parentWindow.Sensitive = true;
            parentWindow.Present ();
            parentWindow.GrabFocus ();
        }

        private static void dialog_FocusInEvent (object o, FocusInEventArgs args)
        {
            if (windows.Count == 0)
                return;

            Window top = windows.Peek ();
            if (ReferenceEquals (top, o))
                return;

            top.Present ();
            top.GrabFocus ();
        }

        public static bool CloseTopDialog ()
        {
            if (windows.Count <= 1)
                return false;

            Window window = windows.Peek ();
            Dialog dialog = window as Dialog;
            if (dialog != null)
                dialog.Respond (ResponseType.DeleteEvent);

            while (Application.EventsPending ())
                Application.RunIteration (false);

            window.Destroy ();

            PopModal (window);
            return true;
        }

        #endregion

        #region Other helper methods

        public static void OpenUrlInBrowser (string url, IErrorHandler errHandler = null)
        {
            Thread starter = new Thread (BrowserProcessStarter);
            starter.Start (new object [] { url, errHandler });
        }

        private static void BrowserProcessStarter (object parameters)
        {
            object [] pars = (object []) parameters;
            string url = (string) pars [0];
            IErrorHandler handler = (IErrorHandler) pars [1];

            try {
                string command = string.Empty;
                string args = string.Empty;
                switch (PlatformHelper.Platform) {
                    case PlatformTypes.Windows:
                        // Use Microsoft's way of opening sites
                        command = url;
                        break;
                    case PlatformTypes.Linux:
                        command = url;
                        break;
                    case PlatformTypes.MacOSX:
                        command = "open";
                        args = url;
                        break;
                }

                ProcessStartInfo pInfo = new ProcessStartInfo { FileName = command, Arguments = args, CreateNoWindow = false };
                Process.Start (pInfo);

                // Sleep some time to wait for the shell to return in case of error
                Thread.Sleep (250);
            } catch (Exception ex) {
                // We don't want any surprises
                if (handler != null)
                    handler.LogException (new Exception ("Exception occurred while trying to open url: " + url, ex));
            }
        }

        public static Form LoadForm (this IDocument document, string documentTemplatesFolder = null, bool ignoreExisting = false)
        {
            CustomFormProvider customForm;
            var formRoot = GetFormXmlRoot (document, out customForm, documentTemplatesFolder, ignoreExisting);

            return document.LoadForm (formRoot, customForm);
        }

        public static Form LoadForm (this IDocument document, XElement formRoot, CustomFormProvider customForm)
        {
            Form ret = DocumentHelper.FormObjectCreator.CreateForm (formRoot, document);
            if (customForm != null) {
                ret.PageWidth = Math.Max (customForm.PageSize.Width, 0);
                ret.PageHeight = Math.Max (customForm.PageSize.Height, 0);
                ret.PageMargins = customForm.PageMargins;
            }

            return ret;
        }

        public static XElement GetFormXmlRoot (this IDocument document, out CustomFormProvider customForm, string documentTemplatesFolder = null, bool ignoreExisting = false)
        {
            if (documentTemplatesFolder == null)
                documentTemplatesFolder = BusinessDomain.AppConfiguration.DocumentTemplatesFolder;

            string templatesDir = Path.GetFileName (documentTemplatesFolder);
            XDocument templatesXml = null;
            if (Directory.Exists (documentTemplatesFolder) && (string.IsNullOrWhiteSpace (templatesDir) || !templatesDir.StartsWith ("Custom_") || !ignoreExisting)) {
                string templateFile = Path.Combine (documentTemplatesFolder, Path.ChangeExtension (document.FormName, "xml"));
                if (File.Exists (templateFile))
                    templatesXml = XDocument.Load (templateFile);
            }

            if (templatesXml == null) {
                string dir = Path.GetFileName (documentTemplatesFolder);
                int index = dir.IndexOf ("_");
                if (index >= 0) {
                    string name = dir.Substring (index + 1);
                    foreach (string directory in Directory.GetDirectories (StoragePaths.DocumentTemplatesFolder)) {
                        if (Path.GetFileName (directory) == name) {
                            string templateFile = Path.Combine (directory, Path.ChangeExtension (document.FormName, "xml"));
                            if (File.Exists (templateFile))
                                templatesXml = XDocument.Load (templateFile);
                            break;
                        }
                    }
                }
            }

            if (templatesXml == null)
                templatesXml = LoadXML (Assembly.GetExecutingAssembly (), "Warehouse.Component.Printing." + Path.ChangeExtension (document.FormName, "xml"));

            // Get the coresponding report form template
            XElement formRoot = null;
            if (templatesXml != null)
                formRoot = templatesXml.XPathSelectElement (string.Format (@"//Form [@formName='{0}']", document.FormName));

            customForm = null;
            if (formRoot == null && AddonForms.TryGetValue (document.FormName, out customForm))
                formRoot = customForm.GetFormRoot ();

            if (formRoot == null)
                throw new ArgumentException (string.Format ("The document template form \"{0}\" could not be found", document.FormName));

            return formRoot;
        }

        public static void LogDocumenting (string message)
        {
#if false
            Debug.WriteLine (message);
#endif
        }

        #endregion

        public static List<TWidget> GetChildWidgetsByType<TWidget> (Container container) where TWidget : class
        {
            List<TWidget> widgets = new List<TWidget> ();
            foreach (Widget child in container.Children) {
                TWidget listView = child as TWidget;
                if (listView != null) {
                    widgets.Add (listView);
                    continue;
                }

                Container cont = child as Container;
                if (cont != null)
                    widgets.AddRange (GetChildWidgetsByType<TWidget> (cont));
            }

            return widgets;
        }

        public static void DestroyChild (this Bin parent)
        {
            if (parent.Child == null)
                return;

            Widget child = parent.Child;
            parent.Remove (child);
            child.Destroy ();
        }

        public static void RemoveChild (this Bin parent)
        {
            if (parent.Child != null)
                parent.Remove (parent.Child);
        }
    }
}
