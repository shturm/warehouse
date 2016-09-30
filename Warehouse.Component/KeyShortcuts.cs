//
// KeyShortcuts.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   05.10.2010
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
using System.IO;
using Gdk;
using Gtk;
using Mono.Addins;
using Warehouse.Data;
using Key = Gdk.Key;

namespace Warehouse.Component
{
    /// <summary>
    /// Contains methods and properties for working with bindings of keyboards keys to application commands.
    /// </summary>
    public static class KeyShortcuts
    {
        public const string CHOOSE_KEY = "mnuToolsSelect";
        public const string HELP_KEY = "mnuHelpHelp";
        public const string MENU_NEIGHBOURING_SHORTCUT = "m";

        public static readonly ModifierType ControlModifier = PlatformHelper.Platform == PlatformTypes.MacOSX ? ModifierType.MetaMask : ModifierType.ControlMask;
        public static readonly ModifierType ButtonControlModifier = PlatformHelper.Platform == PlatformTypes.MacOSX ? ModifierType.Mod1Mask : ModifierType.ControlMask;
        public static readonly ModifierType AltModifier = PlatformHelper.Platform == PlatformTypes.MacOSX ? ModifierType.Mod5Mask : ModifierType.Mod1Mask;
        public static readonly AccelKey DeleteKey = PlatformHelper.Platform == PlatformTypes.MacOSX ? new AccelKey (Key.BackSpace, ModifierType.Mod1Mask, AccelFlags.Visible) : new AccelKey (Key.Delete, ModifierType.None, AccelFlags.Visible);
        private static readonly Dictionary<Key, Key> groupZeroMappings = new Dictionary<Key, Key> ();
        private static List<ICustomKeyShortcut> customKeyShortcuts;

        public static AccelKey ChooseKey
        {
            get { return LookupEntry (CHOOSE_KEY); }
        }

        public static AccelKey HelpKey
        {
            get { return LookupEntry (HELP_KEY); }
        }

        public static List<ICustomKeyShortcut> CustomKeyShortcuts
        {
            get
            {
                if (customKeyShortcuts == null) {
                    customKeyShortcuts = new List<ICustomKeyShortcut> ();

                    foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Component/KeyShortcuts")) {
                        ICustomKeyShortcut shortcut = node.CreateInstance () as ICustomKeyShortcut;
                        if (shortcut == null)
                            continue;

                        customKeyShortcuts.Add (shortcut);
                    }

                }

                return customKeyShortcuts;
            }
        }

        static KeyShortcuts ()
        {
            if (PlatformHelper.Platform == PlatformTypes.MacOSX) {
                Accelerator.DefaultModMask ^= ModifierType.Mod1Mask;
                Accelerator.DefaultModMask |= AltModifier;
            }
        }

        public static string ControlModifierString
        {
            get { return ControlModifier == ModifierType.ControlMask ? "Ctrl" : "Cmd"; }
        }

        public static string GetAccelPath (string name)
        {
            return string.Format ("<Warehouse>/{0}", name);
        }

        /// <summary>
        /// Gets the string representation of the specified key and its modifiers, if any.
        /// </summary>
        /// <param name="accelKey">The key to represent as a string.</param>
        /// <returns></returns>
        public static string KeyToString (AccelKey accelKey)
        {
            return KeyToString (accelKey.Key, accelKey.AccelMods);
        }

        /// <summary>
        /// Gets the string representation of the specified key and its modifiers, if any.
        /// </summary>
        /// <param name="eventKey">The key to represent as a string.</param>
        /// <returns></returns>
        public static string KeyToString (EventKey eventKey)
        {
            return KeyToString (eventKey.Key, eventKey.State);
        }

        /// <summary>
        /// Gets the allowed modifier from the specified one.
        /// </summary>
        /// <param name="modifier">The modifier to get the allowed modifiers from.</param>
        /// <returns></returns>
        public static ModifierType GetAllowedModifier (ModifierType modifier)
        {
            return modifier & Accelerator.DefaultModMask;
        }

        /// <summary>
        /// Gets the string representation of the specified key and modifier.
        /// </summary>
        /// <param name="key">The key to represent as a string.</param>
        /// <param name="modifierType">The modifier to represent as a string.</param>
        /// <param name="isButton">Shows if the widget the short-cut is applied to is a button.</param>
        /// <returns></returns>
        public static string KeyToString (Key key, ModifierType modifierType, bool isButton = false)
        {
            ModifierType modifier = GetAllowedModifier (modifierType);
            if (PlatformHelper.Platform == PlatformTypes.MacOSX) {
                if (isButton && (modifier & ButtonControlModifier) != 0) {
                    modifier ^= ButtonControlModifier;
                    modifier |= ControlModifier;
                }
                if ((modifier & AltModifier) != 0) {
                    modifier ^= AltModifier;
                    modifier |= ModifierType.Mod1Mask;
                }
            }
            return Accelerator.GetLabel (key == Key.VoidSymbol ? 0 : (uint) key, modifier);
        }

        public static bool Equal (EventKey eventKey, AccelKey accelKey)
        {
            return GetAllowedModifier (eventKey.State) == GetAllowedModifier (accelKey.AccelMods) &&
                KeyEqual (eventKey.KeyValue, (uint) accelKey.Key);
        }

        public static bool KeyEqual (uint left, uint right)
        {
            KeymapKey [] eventKeys = left > 0 ? Keymap.Default.GetEntriesForKeyval (left) : new KeymapKey [0];
            if (eventKeys.Length < 1 || eventKeys.Length > 5)
                return false;

            if (eventKeys [0].Keycode == Keyval.ToUpper (right))
                return true;

            KeymapKey [] keys = right > 0 ? Keymap.Default.GetEntriesForKeyval (right) : new KeymapKey [0];
            return keys.Length > 0 && eventKeys [0].Keycode == keys [0].Keycode;
        }

        public static bool CombinationValid (Key key, ModifierType mod)
        {
            return CombinationValid (new AccelKey (key, mod, AccelFlags.Visible));
        }

        public static bool CombinationValid (AccelKey accelKey)
        {
            char key = (char) Keyval.ToUnicode ((uint) accelKey.Key);
            ModifierType modifier = GetAllowedModifier (accelKey.AccelMods);
            return (IsFunctional (accelKey.Key) || accelKey.Key == Key.Delete ||
                (((modifier & ControlModifier) != 0 || (modifier & AltModifier) != 0) && (key != char.MinValue || accelKey.Key == Key.BackSpace)));
        }

        public static bool CombinationValidForItem (EventKey eventKey)
        {
            return CombinationValidForItem (new AccelKey (eventKey.Key, eventKey.State, AccelFlags.Visible));
        }

        public static bool CombinationValidForItem (AccelKey accelKey)
        {
            char key = (char) Keyval.ToUnicode ((uint) accelKey.Key);
            return (IsFunctional (accelKey.Key) || key != char.MinValue);
        }

        /// <summary>
        /// Determines whether the specified key is functional (F1, F2, etc.).
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        /// 	<c>true</c> if the specified key is functional; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunctional (Key key)
        {
            return key >= Key.F1 && key <= Key.F12;
        }

        public static void Save (IEnumerable<KeyValuePair<string, AccelKey>> changedShortcuts)
        {
            AccelMap.Foreach (IntPtr.Zero,
                (pointer, accelPath, key, modifierType, changed) => AccelMap.ChangeEntry (accelPath, 0, 0, true));
            AccelMap.Load (StoragePaths.KeyMapFile);
            foreach (KeyValuePair<string, AccelKey> shortcut in changedShortcuts)
                AccelMap.ChangeEntry (shortcut.Key, (uint) shortcut.Value.Key, shortcut.Value.AccelMods, true);
            AccelMap.Save (StoragePaths.KeyMapFile);
            Load ();
        }

        public static void Load ()
        {
            string tempKeyMap = Path.GetTempFileName ();
            File.WriteAllText (tempKeyMap, DataHelper.GetDefaultKeyMap ());

            AccelMap.Load (tempKeyMap);
            foreach (ICustomKeyShortcut shortcut in CustomKeyShortcuts)
                AccelMap.ChangeEntry (GetAccelPath (shortcut.Path), (uint) shortcut.DefaultKey, GetAllowedModifier (shortcut.DefaultModifier), true);

            File.Delete (tempKeyMap);

            AccelMap.Load (StoragePaths.KeyMapFile);
        }

        public static void MapRawKeys (EventKey evt, out Key key, out ModifierType mod)
        {
            mod = GetAllowedModifier (evt.State);
            key = evt.Key;
            if (PlatformHelper.Platform != PlatformTypes.MacOSX)
                return;

            uint keyval;
            int effectiveGroup, level;
            ModifierType consumedModifiers;
            Keymap.Default.TranslateKeyboardState (evt.HardwareKeycode, evt.State, evt.Group, out keyval, out effectiveGroup,
                out level, out consumedModifiers);

            key = (Key) keyval;
            mod = evt.State & ~consumedModifiers;

            AccelKey accelKey = MapRawKeys (new AccelKey (key, mod, AccelFlags.Visible));
            key = accelKey.Key;
            mod = accelKey.AccelMods;

            // When opt modifier is active, we need to decompose this to make the command appear correct for Mac.
            // In addition, we can only inspect whether the opt/alt key is pressed by examining
            // the key's "group", because the Mac GTK+ treats opt as a group modifier and does
            // not expose it as an actual GDK modifier.
            if (evt.Group == (byte) 1) {
                mod |= AltModifier;
                key = GetGroupZeroKey (key, evt);
            }
        }

        public static AccelKey MapRawKeys (AccelKey accelKey)
        {
            if (PlatformHelper.Platform != PlatformTypes.MacOSX)
                return accelKey;

            // HACK: the MAC GTK+ port currently does some horrible, un-GTK-ish key mappings
            // so we work around them by playing some tricks to remap and decompose modifiers.
            // We also decompose keys to the root physical key so that the Mac command
            // combinations appear as expected, e.g. shift-{ is treated as shift-[.
            // Mac GTK+ maps the command key to the Mod1 modifier, which usually means alt/
            // We map this instead to meta, because the Mac GTK+ has mapped the cmd key
            // to the meta key (yay inconsistency!). IMO super would have been saner.
            if ((accelKey.AccelMods & ModifierType.Mod1Mask) != 0) {
                accelKey.AccelMods ^= ModifierType.Mod1Mask;
                accelKey.AccelMods |= ControlModifier;
            }

            //fix shift-tab weirdness. There isn't a nice name for untab, so make it shift-tab
            if (accelKey.Key == Key.ISO_Left_Tab) {
                accelKey.Key = Key.Tab;
                accelKey.AccelMods |= ModifierType.ShiftMask;
            }
            return accelKey;
        }

        private static Key GetGroupZeroKey (Key mappedKey, EventKey evt)
        {
            Key ret;
            if (groupZeroMappings.TryGetValue (mappedKey, out ret))
                return ret;

            //LookupKey isn't implemented on Mac, so we have to use this workaround
            uint [] keyvals;
            KeymapKey [] keys;
            Keymap.Default.GetEntriesForKeycode (evt.HardwareKeycode, out keys, out keyvals);

            //find the key that has the same level (so we preserve shift) but with group 0
            for (uint i = 0; i < keyvals.Length; i++)
                if (keyvals [i] == (uint) mappedKey)
                    for (uint j = 0; j < keys.Length; j++)
                        if (keys [j].Group == 0 && keys [j].Level == keys [i].Level)
                            return groupZeroMappings [mappedKey] = (Key) keyvals [j];

            //failed, but avoid looking it up again
            return groupZeroMappings [mappedKey] = mappedKey;
        }

        public static bool IsScreenModifierControl (ModifierType modifier)
        {
            return (modifier & ButtonControlModifier) == ButtonControlModifier;
        }

        public static void SetAccelPath (Widget widget, AccelGroup accelGroup, string accelPath)
        {
            widget.SetAccelPath (GetAccelPath (accelPath), accelGroup);
            AccelKey key = LookupEntry (accelPath);
            string keyToString = KeyToString (key.Key, key.AccelMods, true);
            if (!string.IsNullOrWhiteSpace (keyToString))
                widget.TooltipText = string.Format (" {0} ", keyToString);
        }

        public static AccelKey LookupEntry (string accelPath)
        {
            AccelKey result = new AccelKey ();
            AccelMap.Foreach (IntPtr.Zero, (data, path, key, mods, changed) =>
                {
                    if (path == GetAccelPath (accelPath))
                        result = new AccelKey ((Key) key, mods, AccelFlags.Visible);
                });
            AccelKey accelKey = result;
            return accelKey;
        }
    }
}
