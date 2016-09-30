//
// GroupsPanel.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/23/2008
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
using System.Linq;
using GLib;
using Gdk;
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Data;
using Drag = Gtk.Drag;

namespace Warehouse.Presentation.Widgets
{
    public class GroupsPanel<T> : ScrolledWindow where T : GroupBase<T>, new ()
    {
        private readonly TreeStore groupsTreeStore;
        private readonly TreeView groupsTree;
        private readonly List<T> groups = new List<T> ();
        private readonly T deletedGroup = new T { Id = int.MinValue, Name = Translator.GetString ("Deleted") };
        private int groupsCount;

        public TreeView GroupsTree
        {
            get { return groupsTree; }
        }

        public int GroupsCount
        {
            get { return groupsCount; }
        }

        public bool HideDeletedGroup { get; set; }

        public GroupsPanel ()
            : this (false)
        {
        }

        public GroupsPanel (bool multiSelect)
        {
            groupsTreeStore = new TreeStore (typeof (string), typeof (bool), typeof (T));
            groupsTree = new TreeView (groupsTreeStore) { HeadersVisible = true, RulesHint = false, SearchColumn = -1 };

            TargetEntry targetEntry = new TargetEntry { Flags = TargetFlags.Widget, Target = "application/x-hyena-data-model-selection" };
            groupsTree.EnableModelDragSource (ModifierType.Button1Mask | ModifierType.Button3Mask, new [] { targetEntry }, DragAction.Move);
            TargetEntry targetEntryTree = new TargetEntry ("application/gtk_tree_model_row", TargetFlags.Widget, 0);
            groupsTree.EnableModelDragDest (new [] { targetEntry }, DragAction.Move);
            groupsTree.EnableModelDragDest (new [] { targetEntryTree }, DragAction.Move);
            groupsTree.DragMotion += GroupsTree_DragMotion;
            groupsTree.DragDrop += GroupsTree_DragDrop;

            CellRendererText textCellRend = new CellRendererText { Editable = false };

            TreeViewColumn groupNameColumn = new TreeViewColumn { Title = Translator.GetString ("Group"), Expand = true };
            groupNameColumn.PackStart (textCellRend, true);
            groupNameColumn.AddAttribute (textCellRend, "markup", 0);

            groupsTree.AppendColumn (groupNameColumn);

            CellRendererToggle toglCellRend = new CellRendererToggle { Activatable = true };
            toglCellRend.Toggled += toglCellRend_Toggled;

            TreeViewColumn groupSelColumn = new TreeViewColumn { Title = Translator.GetString ("Selected"), Expand = true, Visible = multiSelect };
            groupSelColumn.PackStart (toglCellRend, true);
            groupSelColumn.AddAttribute (toglCellRend, "active", 1);

            groupsTree.AppendColumn (groupSelColumn);

            HscrollbarPolicy = PolicyType.Automatic;
            VscrollbarPolicy = PolicyType.Automatic;

            Add (groupsTree);
            groupsTree.Show ();
        }

        private void GroupsTree_DragMotion (object o, DragMotionArgs args)
        {
            TreePath path;
            TreeViewDropPosition pos;
            if (BusinessDomain.LoggedUser.UserLevel == UserAccessLevel.Operator ||
                !groupsTree.GetDestRowAtPos (args.X, args.Y, out path, out pos)) {
                args.RetVal = false;
                return;
            }

            if (Drag.GetSourceWidget (args.Context) == groupsTree) {
                TreeIter selectedRow;
                if (groupsTree.Selection.GetSelected (out selectedRow)) {
                    if (groupsTree.Model.GetValue (selectedRow, 2) == deletedGroup) {
                        args.RetVal = false;
                        return;
                    }
                    TreePath draggedPath = groupsTree.Model.GetPath (selectedRow);
                    if (!draggedPath.Equals (path) && !draggedPath.IsAncestor (path)) {
                        TreeIter destRow;
                        groupsTree.Model.GetIter (out destRow, path);
                        if (groupsTree.Model.GetValue (destRow, 2) == deletedGroup) {
                            args.RetVal = false;
                            return;
                        }
                        groupsTree.SetDragDestRow (path, pos);
                    }
                }
            } else
                switch (pos) {
                    case TreeViewDropPosition.Before:
                        groupsTree.SetDragDestRow (path, TreeViewDropPosition.IntoOrBefore);
                        break;
                    case TreeViewDropPosition.After:
                        groupsTree.SetDragDestRow (path, TreeViewDropPosition.IntoOrAfter);
                        break;
                }
            Gdk.Drag.Status (args.Context, args.Context.SuggestedAction, args.Time);

            args.RetVal = true;
        }

        [ConnectBefore]
        private void GroupsTree_DragDrop (object o, DragDropArgs args)
        {
            if (Drag.GetSourceWidget (args.Context) != groupsTree)
                return;

            TreePath path;
            TreeViewDropPosition pos;
            groupsTree.GetDestRowAtPos (args.X, args.Y, out path, out pos);

            TreeIter row;
            groupsTree.Model.GetIter (out row, path);
            T group = (T) groupsTree.Model.GetValue (row, 2);

            int insertionIndex = GetInsertionIndex (pos, ref group);
            TreeIter selectedRow;
            if (!groupsTree.Selection.GetSelected (out selectedRow)) {
                args.RetVal = true;
                Drag.Finish (args.Context, false, false, args.Time);
                return;
            }

            TreePath draggedPath = groupsTree.Model.GetPath (selectedRow);
            if (draggedPath.Equals (path) || draggedPath.IsAncestor (path)) {
                args.RetVal = true;
                Drag.Finish (args.Context, false, false, args.Time);
                return;
            }

            T draggedGroup = RemoveFromOldPosition (group, selectedRow, ref insertionIndex);
            MoveGroup (draggedGroup, insertionIndex, group);
            args.RetVal = true;
            Drag.Finish (args.Context, true, true, args.Time);
            TreePath movedPath = LoadGroups (false, draggedGroup);
            groupsTree.ExpandToPath (movedPath);
        }

        private T RemoveFromOldPosition (T group, TreeIter selectedRow, ref int insertionIndex)
        {
            T draggedGroup = (T) groupsTree.Model.GetValue (selectedRow, 2);
            if (draggedGroup.Parent == null) {
                int oldIndex = groups.IndexOf (draggedGroup);
                groups.Remove (draggedGroup);
                if (oldIndex < insertionIndex)
                    --insertionIndex;
            } else {
                int oldIndex = draggedGroup.Parent.Children.IndexOf (draggedGroup);
                draggedGroup.Parent.Children.Remove (draggedGroup);
                if (group == draggedGroup.Parent && oldIndex < insertionIndex)
                    --insertionIndex;
            }
            return draggedGroup;
        }

        private void MoveGroup (T draggedGroup, int insertionIndex, T group)
        {
            draggedGroup.Parent = group;
            List<T> children = group == null ? groups : group.Children;
            children.Insert (insertionIndex, draggedGroup);
            string nextCode = children [Math.Max (0, insertionIndex - 1)].Code;
            List<T> changedGroups = new List<T> ();
            changedGroups.Add (draggedGroup);
            for (int i = insertionIndex; i < children.Count; i++) {
                children [i].Code = nextCode = DataProvider.GetNextGroupCode (nextCode);
                changedGroups.Add (children [i]);
            }
            if (group != null)
                changedGroups.Add (group);

            GroupBase<T>.CommitAll (changedGroups);
        }

        private int GetInsertionIndex (TreeViewDropPosition pos, ref T group)
        {
            int insertionIndex = group.Children.Count;
            switch (pos) {
                case TreeViewDropPosition.Before:
                    if (group.Parent == null) {
                        insertionIndex = groups.IndexOf (group);
                        group = null;
                    } else {
                        insertionIndex = group.Parent.Children.IndexOf (group);
                        group = group.Parent;
                    }
                    break;
                case TreeViewDropPosition.After:
                    if (group.Parent == null) {
                        insertionIndex = Math.Min (groups.Count, groups.IndexOf (group) + 1);
                        group = null;
                    } else {
                        insertionIndex = Math.Min (group.Parent.Children.Count, group.Parent.Children.IndexOf (group) + 1);
                        group = group.Parent;
                    }
                    break;
            }
            return insertionIndex;
        }

        private void toglCellRend_Toggled (object o, ToggledArgs args)
        {
            TreeIter iter;
            groupsTreeStore.GetIterFromString (out iter, args.Path);
            bool oldVal = (bool) groupsTreeStore.GetValue (iter, 1);
            groupsTreeStore.SetValue (iter, 1, !oldVal);
        }

        public void LoadGroups (IEnumerable<T> newGroups)
        {
            groups.Clear ();
            groups.AddRange (newGroups);
            LoadGroups (true, null);
        }

        private TreePath LoadGroups (bool expandAll, T searchedItem)
        {
            groupsCount = 0;
            groupsTreeStore.Clear ();
            TreePath treePath = CreateTreeStore (groups, TreeIter.Zero, true, searchedItem);
            if (typeof (T) != typeof (EmptyGroup) && !HideDeletedGroup)
                groupsTreeStore.AppendValues (string.Format ("<i>{0}</i>", Markup.EscapeText (deletedGroup.Name)), false, deletedGroup);

            if (expandAll)
                groupsTree.ExpandAll ();
            return treePath;
        }

        private TreePath CreateTreeStore (IEnumerable<T> newGroups, TreeIter parentIter, bool root, T searchedItem)
        {
            TreePath treePath = null;
            foreach (T child in newGroups) {
                string childName = Markup.EscapeText (child.Name);
                TreeIter iter = root ?
                    groupsTreeStore.AppendValues (childName, false, child) :
                    groupsTreeStore.AppendValues (parentIter, childName, false, child);

                groupsCount++;
                TreePath newTreePath = CreateTreeStore (child.Children, iter, false, searchedItem);
                if (treePath != null)
                    continue;

                treePath = newTreePath;
                if (treePath == null && child == searchedItem)
                    treePath = groupsTreeStore.GetPath (iter);
            }
            return treePath;
        }

        public bool SelectFirstGroup ()
        {
            if (groupsCount == 0)
                return false;

            TreeIter iter;
            groupsTreeStore.GetIterFirst (out iter);

            TreePath path = groupsTreeStore.GetPath (iter);
            groupsTree.Selection.UnselectAll ();
            groupsTree.Selection.SelectPath (path);
            groupsTree.ScrollToCell (path, groupsTree.Columns [0], true, 0.5f, 0.5f);

            return true;
        }

        public bool SelectGroupId (long id)
        {
            TreeIter firstIter;
            return groupsTreeStore.GetIterFirst (out firstIter) && SearchTreeStore (firstIter, id);
        }

        public bool SearchTreeStore (TreeIter iter, long id)
        {
            do {
                T g = (T) groupsTreeStore.GetValue (iter, 2);
                if (g.Id == id) {
                    TreePath path = groupsTreeStore.GetPath (iter);
                    groupsTree.Selection.UnselectAll ();
                    groupsTree.Selection.SelectPath (path);
                    groupsTree.ScrollToCell (path, groupsTree.Columns [0], true, 0.5f, 0.5f);

                    return true;
                }

                TreeIter child;
                if (!groupsTreeStore.IterChildren (out child, iter))
                    continue;

                if (SearchTreeStore (child, id))
                    return true;
            } while (groupsTreeStore.IterNext (ref iter));

            return false;
        }

        public T [] GetAllGroups ()
        {
            return groups.ToArray ();
        }

        public T GetSelectedGroup ()
        {
            TreePath [] selected = groupsTree.Selection.GetSelectedRows ();
            TreeIter selectedIter;
            if (selected.Length == 0)
                groupsTreeStore.GetIter (out selectedIter, new TreePath ("0"));
            else
                groupsTreeStore.GetIter (out selectedIter, selected [0]);

            return (T) groupsTreeStore.GetValue (selectedIter, 2);
        }

        public long GetSelectedGroupId ()
        {
            return GetSelectedGroup ().Id;
        }

        public void EnableGroups (IEnumerable<long> ids)
        {
            TreeIter firstIter;
            groupsTreeStore.GetIterFirst (out firstIter);

            EnableTreeStore (firstIter, ids.ToDictionary (id => id, id => true));
        }

        private void EnableTreeStore (TreeIter iter, Dictionary<long, bool> ids)
        {
            do {
                T g = (T) groupsTreeStore.GetValue (iter, 2);
                groupsTreeStore.SetValue (iter, 1, ids.ContainsKey (g != null ? g.Id : -1));

                TreeIter child;
                if (groupsTreeStore.IterChildren (out child, iter)) {
                    EnableTreeStore (child, ids);
                }
            } while (groupsTreeStore.IterNext (ref iter));
        }

        public long [] GetEnabledGroupIds ()
        {
            TreeIter firstIter;
            groupsTreeStore.GetIterFirst (out firstIter);

            return GetEnabledTreeStoreIds (firstIter).ToArray ();
        }

        public List<long> GetEnabledTreeStoreIds (TreeIter iter)
        {
            List<long> ret = new List<long> ();
            do {
                T g = (T) groupsTreeStore.GetValue (iter, 2);
                object en = groupsTreeStore.GetValue (iter, 1);
                bool enabled = en != null && (bool) en;
                if (enabled)
                    ret.Add (g.Id);

                TreeIter child;
                if (groupsTreeStore.IterChildren (out child, iter)) {
                    ret.AddRange (GetEnabledTreeStoreIds (child));
                }
            } while (groupsTreeStore.IterNext (ref iter));

            return ret;
        }

        public override void Dispose ()
        {
            base.Dispose ();

            groupsTreeStore.Dispose ();
            groupsTree.Dispose ();
        }
    }
}
