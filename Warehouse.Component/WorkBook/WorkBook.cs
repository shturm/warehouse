//
// WorkBook.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   04/04/2006
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
using GLib;
using Gtk;

namespace Warehouse.Component.WorkBook
{
    public class WorkBook : EventBox
    {
        private Notebook book;
        private WorkBookPage previousPage;

        public WorkBookPage CurrentPage
        {
            get
            {
                return (WorkBookPage) book.CurrentPageWidget;
            }
            set
            {
                for (int i = 0; i < book.NPages; i++) {
                    WorkBookPage page = (WorkBookPage) book.GetNthPage (i);
                    if (page.GetHashCode () == value.GetHashCode ()) {
                        book.CurrentPage = i;
                        page.OnPageShown ();
                        break;
                    }
                }
            }
        }

        public event EventHandler<CurrentPageChangedArgs> CurrentPageChanged;

        public int PagesCount
        {
            get
            {
                return book.NPages;
            }
        }

        public bool ShowTabs
        {
            get { return book.ShowTabs; }
            set
            {
                if (book.ShowTabs != value) {
                    book.ShowTabs = value;

                    WorkBookPage page = CurrentPage;
                    if (page != null)
                        page.OnViewModeChanged ();
                }
            }
        }

        public WorkBook ()
        {
            InitializeBook ();
            book.SwitchPage += book_SwitchPage;
        }

        private void InitializeBook ()
        {
            book = new Notebook ();
            Add (book);
            book.Show ();
            book.Scrollable = true;
        }

        private void book_SwitchPage (object o, SwitchPageArgs args)
        {
            EventHandler<CurrentPageChangedArgs> handler = CurrentPageChanged;
            if (handler != null)
                handler (this, new CurrentPageChangedArgs (previousPage));
            previousPage = CurrentPage;
        }

        public void AddPage (WorkBookPage page, bool setAsCurrent)
        {
            page.OnPageAdding ();

            HBox hbPageTab = new HBox ();

            Widget tab = CreatePageTab (page);
            hbPageTab.PackStart (tab);
            tab.Show ();

            Label pageNum = new Label ();
            pageNum.Text = page.GetHashCode ().ToString ();
            pageNum.Visible = false;
            pageNum.Name = "WorkBookPageId";
            hbPageTab.PackEnd (pageNum);

            hbPageTab.CanFocus = false;
            page.CanFocus = true;
            book.AppendPage (page, hbPageTab);
            page.Show ();

            if (setAsCurrent)
                CurrentPage = page;

            page.OnPageAdded ();
        }

        public void RemovePage (int pageNumber)
        {
            book.RemovePage (pageNumber);
            if (book.NPages == 0) {
                // the current page was changed to null
                EventHandler<CurrentPageChangedArgs> handler = CurrentPageChanged;
                if (handler != null)
                    handler (this, new CurrentPageChangedArgs (previousPage));
                previousPage = CurrentPage;
            }
        }

        public void RemovePage (WorkBookPage page)
        {
            RemovePage (GetPageIndexByHash (page.GetHashCode ()));
        }

        public WorkBookPage FindPageByName (string pageName)
        {
            for (int i = 0; i < book.NPages; i++) {
                WorkBookPage page = (WorkBookPage) book.GetNthPage (i);
                if (page.PageTitle == pageName)
                    return page;
            }

            return null;
        }

        public WorkBookPage GetPageAt (int index)
        {
            return (WorkBookPage) book.GetNthPage (index);
        }

        public WorkBookPage GetPageByHash (int hashCode)
        {
            for (int i = 0; i < book.NPages; i++) {
                WorkBookPage page = (WorkBookPage) book.GetNthPage (i);
                if (page.GetHashCode () == hashCode)
                    return page;
            }

            return null;
        }

        public int GetPageIndexByHash (int hashCode)
        {
            for (int i = 0; i < book.NPages; i++) {
                WorkBookPage page = (WorkBookPage) book.GetNthPage (i);
                if (page.GetHashCode () == hashCode)
                    return i;
            }

            return -1;
        }

        private Widget CreatePageTab (WorkBookPage child)
        {
            HBox hb = new HBox { Spacing = 0 };

            Label lbl = new Label { Markup = new PangoStyle { Size = PangoStyle.TextSize.Small, Text = child.PageTitle } };
            hb.PackStart (lbl, false, true, 0);

            Button btn = new Button { Relief = ReliefStyle.None, FocusOnClick = false };
            btn.Add (ComponentHelper.LoadImage ("Warehouse.Component.WorkBook.Icon.Close12.png"));
            btn.Clicked += CloseButton_Clicked;
            hb.PackEnd (btn, false, true, 0);

            hb.ShowAll ();
            return hb;
        }

        private void CloseButton_Clicked (object o, EventArgs args)
        {
            int pageHash = -1;

            try {
                Button tmpBtn = (Button) o;
                // get the HBox containing the tab label and the button
                HBox tmpHBox = (HBox) tmpBtn.Parent;
                // get the HBox containing the tab and the label containing the page number
                tmpHBox = (HBox) tmpHBox.Parent;

                foreach (Widget wd in tmpHBox.Children) {
                    if (wd.Name == "WorkBookPageId") {
                        pageHash = int.Parse (((Label) wd).Text);
                        break;
                    }
                }

                GetPageByHash (pageHash).RequestClose ();
            } catch (Exception) { }
        }
    }
}