//
// GridNavigator.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   03.25.2010
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
using Gdk;
using Warehouse.Business.Operations;
using Warehouse.Component;
using Warehouse.Component.ListView;
using Warehouse.Data.DataBinding;

namespace Warehouse.Presentation
{
    public delegate bool CellValueEvaluator (int row, string value);
    public delegate void CellValueChooser (string filter);
    public delegate void CellValueContiueEdit (int row, Key keyCode);
    public delegate bool CellValueEdit (int row, int column);
    public delegate void ColumnValueEdit (int column);
    public delegate void RowDeleter (int row);

    public class GridNavigator
    {
        private readonly ListView grid;
        private readonly CellValueEdit cellEditor;
        private readonly ColumnValueEdit cellUpEditor;
        private readonly ColumnValueEdit cellDownEditor;

        public GridNavigator (ListView grid, CellValueEdit cellEditor, ColumnValueEdit cellUpEditor, ColumnValueEdit cellDownEditor)
        {
            if (grid == null)
                throw new ArgumentNullException ("grid");
            if (cellEditor == null)
                throw new ArgumentNullException ("cellEditor");
            if (cellUpEditor == null)
                throw new ArgumentNullException ("cellUpEditor");
            if (cellDownEditor == null)
                throw new ArgumentNullException ("cellDownEditor");

            this.grid = grid;
            this.cellEditor = cellEditor;
            this.cellUpEditor = cellUpEditor;
            this.cellDownEditor = cellDownEditor;
        }

        public bool ColumnKeyPress (CellKeyPressEventArgs args, int colIndex,
            CellValueEvaluator evaluator, CellValueContiueEdit editPrev, CellValueContiueEdit editNext)
        {
            return ColumnKeyPress (args, colIndex, null, evaluator, editPrev, editNext);
        }

        public bool ColumnKeyPress (CellKeyPressEventArgs args, int colIndex, CellValueChooser chooser,
            CellValueEvaluator evaluator, CellValueContiueEdit editPrev, CellValueContiueEdit editNext)
        {
            if (args == null)
                throw new ArgumentNullException ("args");

            string filter = args.Editing ? args.Entry.Text : string.Empty;

            if (KeyShortcuts.Equal (args.EventKey, KeyShortcuts.ChooseKey)) {
                args.MarkAsHandled ();
                return ChooseCellValue (evaluator, chooser, filter);
            }

            switch (args.GdkKey) {
                case Key.Tab:
                case Key.Return:
                case Key.KP_Enter:
                    if (args.Editing)
                        ContinueEditingIfValid (args, evaluator, chooser, filter, editNext);
                    break;
                case Key.Right:
                    if (args.Editing)
                        // If the cursor is at the end of the text
                        if (args.Entry.CursorPosition == args.Entry.Text.Length)
                            ContinueEditingIfValid (args, evaluator, chooser, filter, editNext);
                    break;
                case Key.Left:
                case Key.ISO_Left_Tab:
                    if (args.Editing)
                        // If the cursor is at the end of the text
                        if (args.Entry.CursorPosition == args.Entry.Text.Length ||
                            args.Entry.CursorPosition == 0 ||
                            args.GdkKey == Key.ISO_Left_Tab)
                            ContinueEditingIfValid (args, evaluator, chooser, filter, editPrev);
                    break;
                case Key.Up:
                case Key.KP_Up:
                    if (args.Editing) {
                        evaluator (grid.EditedCell.Row, args.Entry.Text);
                        cellUpEditor (colIndex);
                    }
                    break;
                case Key.Down:
                case Key.KP_Down:
                    if (args.Editing) {
                        evaluator (grid.EditedCell.Row, args.Entry.Text);
                        cellDownEditor (colIndex);
                    }
                    break;
                case Key.BackSpace:
                    if (args.Editing) {
                        if (args.Entry.Text.Length == 0) {
                            evaluator (grid.EditedCell.Row, args.Entry.Text);
                            editPrev (grid.EditedCell.Row, args.GdkKey);
                        }
                    }
                    break;
                default:
                    return false;
            }

            return true;
        }

        public bool ChooseCellValue (CellValueEvaluator evaluator,
            CellValueChooser chooser, string text)
        {
            if (chooser == null || !grid.EditedCell.IsValid)
                return false;

            chooser (evaluator (grid.EditedCell.Row, text) ? string.Empty : text);
            return true;
        }

        private void ContinueEditingIfValid (CellKeyPressEventArgs args, CellValueEvaluator evaluator,
            CellValueChooser chooser, string filter, CellValueContiueEdit contiueEdit)
        {
            if (evaluator (grid.EditedCell.Row, args.Entry.Text))
                contiueEdit (grid.EditedCell.Row, args.GdkKey);
            else if (chooser != null)
                chooser (filter);
        }

        public void EditPrev (int row, Key keyCode, Column colPrev, CellValueContiueEdit prevHandler)
        {
            if (colPrev != null && colPrev.ListCell.IsEditable)
                if (cellEditor (row, colPrev.Index))
                    return;

            prevHandler (row, keyCode);
        }

        public void EditPrevOnFirst (int row, Key keyCode, Column colPrev, CellValueContiueEdit prevHandler, RowDeleter delHandler)
        {
            if (keyCode == Key.BackSpace) {
                delHandler (row);
                return;
            }

            if (row > 0)
                row--;

            EditPrev (row, keyCode, colPrev, prevHandler);
        }

        public void EditNext (int row, Key keyCode, Column colNext, CellValueContiueEdit nextHandler)
        {
            if (colNext != null && colNext.ListCell.IsEditable)
                if (cellEditor (row, colNext.Index))
                    return;

            nextHandler (row, keyCode);
        }

        public void EditNextOnLast<T> (int row, Key keyCode, Column colNext, CellValueContiueEdit nextHandler, BindList<T> details, Func<T> addNewDetail = null, bool createRows = true) where T : OperationDetail
        {
            if (details.Count - 1 > row ||
                (details [row].ItemId > 0 && (keyCode == Key.Return || keyCode == Key.KP_Enter)))
                row++;

            if (details.Count <= row) {
                if (!createRows)
                    row = details.Count - 1;
                else if (addNewDetail != null)
                    addNewDetail ();
                else
                    details.AddNew ();
            }

            EditNext (row, keyCode, colNext, nextHandler);
        }
    }
}
