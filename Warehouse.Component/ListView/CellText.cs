//
// CellText.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   10/18/2007
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

using Gdk;
using GLib;
using Gtk;
using Warehouse.Data.Model;
using Key = Gdk.Key;

namespace Warehouse.Component.ListView
{
    public class CellText : Cell
    {
        private Pango.EllipsizeMode ellipsize_mode = Pango.EllipsizeMode.End;
        private Pango.Alignment alignment = Pango.Alignment.Left;

        private Entry entry;
        private bool restartEdit;
        private bool eventLock;
        private int hPadding = 4;

        public Pango.Alignment Alignment
        {
            get { return alignment; }
            set { alignment = value; }
        }

        public virtual Pango.EllipsizeMode EllipsizeMode
        {
            get { return ellipsize_mode; }
            set { ellipsize_mode = value; }
        }

        internal static int ComputeRowHeight (ListView widget, CellStyleProvider styleProvider)
        {
            int w_width, row_height;
            Pango.Layout layout = new Pango.Layout (widget.PangoContext);
            if (styleProvider != null) {
                CellStyleQueryEventArgs ret = new CellStyleQueryEventArgs (StateType.Normal, new CellPosition (0, 0));
                if (widget.Model != null && widget.Model.Count > 0)
                    styleProvider (widget, ret);

                if (ret.Style != null)
                    layout.SetMarkup (ret.Style.GetMarkup ("W"));
                else
                    layout.SetText ("W");
            } else {
                layout.SetText ("W");
            }
            layout.GetPixelSize (out w_width, out row_height);
            layout.Dispose ();
            return row_height + 8;
        }

        public Entry Entry
        {
            get { return entry; }
        }

        public int HPadding
        {
            get { return hPadding; }
            set { hPadding = value; }
        }

        public override object EditValue
        {
            get
            {
                if (!IsEditable || entry == null)
                    return null;

                return ParseObject (entry.Text);
            }
            set
            {
                if (!IsEditable || entry == null)
                    return;

                entry.Text = ObjectToEditString (value);
            }
        }

        public CellText (string propertyName)
            : base (propertyName)
        {
            comparer = new BasicComparer ();
        }

        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight, CellPosition position)
        {
            CellPosition editPos = ParentColumn.ParentListView.EditedCell;
            if (editPos == position) {

                entry.SizeRequest ();
                entry.SizeAllocate (context.Area);

                if (restartEdit) {
                    entry.SelectRegion (0, -1);
                    entry.HasFocus = true;
                    entry.GrabFocus ();
                    restartEdit = false;
                }
                entry.Show ();
            } else {
                string cellText = ObjectToString (boundObject);
                if (string.IsNullOrEmpty (cellText))
                    return;

                PangoStyle style = parentColumn.ParentListView.QueryCellStyle (state, position);

                context.Layout.Width = (int) ((cellWidth - hPadding * 2) * Pango.Scale.PangoScale);
                context.Layout.Ellipsize = EllipsizeMode;
                context.Layout.Alignment = alignment;

                context.Layout.SetMarkup (style.GetMarkup (cellText));
                int text_width;
                int text_height;
                context.Layout.GetPixelSize (out text_width, out text_height);

                context.Context.MoveTo (hPadding, ((int) cellHeight - text_height) / 2);
                Cairo.Color color = context.Theme.Colors.GetWidgetColor (
                    context.TextAsForeground ? GtkColorClass.Foreground : GtkColorClass.Text, state);
                color.A = context.Sensitive ? 1.0 : 0.3;

                context.Context.Color = color;
                context.Context.ShowLayout (context.Layout);
            }
        }

        protected internal override void OnButtonPressEvent (CellButtonPressEventArgs args)
        {
            if (eventLock)
                return;

            try {
                eventLock = true;

                base.OnButtonPressEvent (args);

                if (!IsEditable)
                    return;

                if (parentColumn.ParentListView.ManualEditControl)
                    return;

                CellPosition pos = ParentColumn.ParentListView.EditedCell;
                if (pos.Row != args.Cell.Row || pos.Column != args.Cell.Column) {
                    if (args.EventButton.Type == EventType.TwoButtonPress) {
                        BeginCellEdit (args);
                    }
                }
            } finally {
                eventLock = false;
            }
        }

        protected internal override void OnKeyPressEvent (CellKeyPressEventArgs args)
        {
            if (eventLock)
                return;

            try {
                eventLock = true;

                CellPosition editPos = ParentColumn.ParentListView.EditedCell;
                if (args.Cell == editPos)
                    args.Entry = entry;

                base.OnKeyPressEvent (args);

                if (!IsEditable)
                    return;

                if (parentColumn.ParentListView.ManualEditControl)
                    return;

                switch (args.GdkKey) {
                    case Key.Return:
                    case Key.KP_Enter:
                        if (editPos == args.Cell) {
                            EndCellEdit (ParseObject (entry.Text));
                        } else {
                            BeginCellEdit (args);
                        }
                        break;
                    case Key.Escape:
                        if (editPos == args.Cell) {
                            CancelCellEdit ();
                        }
                        break;
                }
            } finally {
                eventLock = false;
            }
        }

        #region Start events

        public override bool BeginCellEdit (CellEventArgs args)
        {
            bool ret = base.BeginCellEdit (args);

            if (entry == null) {
                entry = new Entry ();
                parentColumn.ParentListView.Add (entry);
            }

            BindListItem (args.Cell.Row);
            entry.Text = ObjectToEditString (boundObject);

            entry.KeyPressEvent -= EntryKeyPressEvent;
            entry.KeyPressEvent += EntryKeyPressEvent;
            entry.ButtonPressEvent -= EntryButtonPressEvent;
            entry.ButtonPressEvent += EntryButtonPressEvent;

            restartEdit = true;

            return ret;
        }

        public override void EndCellEdit (object newValue)
        {
            if (!IsEditable)
                return;

            base.EndCellEdit (newValue);

            if (entry != null)
                entry.Hide ();
        }

        public override void EndCellEdit ()
        {
            if (entry == null)
                return;

            EndCellEdit (ParseObject (entry.Text));
        }

        public override void CancelCellEdit ()
        {
            if (!IsEditable)
                return;

            base.CancelCellEdit ();

            if (entry != null)
                entry.Hide ();
        }

        #endregion

        [ConnectBefore]
        private void EntryKeyPressEvent (object o, KeyPressEventArgs args)
        {
            switch (args.Event.Key) {
                case Key.Tab:
                case Key.ISO_Left_Tab:
                case Key.Up:
                case Key.KP_Up:
                case Key.Down:
                case Key.KP_Down:
                    return;
            }

            CellPosition pos = ParentColumn.ParentListView.EditedCell;
            OnKeyPressEvent (new CellKeyPressEventArgs (pos.Column, pos.Row, entry, args.Event) { SignalArgs = args });
        }

        [ConnectBefore]
        private void EntryButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            // single clicks move the cursor and should'n be used for outer events
            if (args.Event.Type == EventType.ButtonPress || args.Event.Type == EventType.ButtonRelease)
                return;

            CellPosition pos = ParentColumn.ParentListView.EditedCell;

            OnButtonPressEvent (new CellButtonPressEventArgs (pos.Column, pos.Row, args.Event));
        }
    }
}
