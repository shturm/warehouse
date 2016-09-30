//
// RestrictionNode.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   03/09/2008
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
using System.Xml.Linq;
using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public class RestrictionNode : TreeNodeBase<RestrictionNode, string>
    {
        private Dictionary<long, UserRestriction> restrictions = new Dictionary<long, UserRestriction> ();
        private readonly Func<string> translate;

        public Dictionary<long, UserRestriction> Restrictions
        {
            get { return restrictions; }
        }

        public RestrictionNode ()
            : base (null, null)
        {
        }

        public RestrictionNode (string name, Func<string> translate, params RestrictionNode [] children)
            : base (name, translate (), children)
        {
            this.translate = translate;
        }

        public void Translate ()
        {
            Value = translate ();
        }

        private void ClearRestrictions ()
        {
            restrictions = restrictions.Where (pair => pair.Key < 0).ToDictionary (r => r.Key, r => r.Value);

            foreach (RestrictionNode child in children)
                child.ClearRestrictions ();
        }

        public void ReloadRestrictions ()
        {
            ClearRestrictions ();
            if (BusinessDomain.DataAccessProvider != null)
                SetRestrictions (UserRestriction.GetAll ());
        }

        private void SetRestrictions (params UserRestriction [] rests)
        {
            foreach (UserRestriction rest in rests) {
                if (rest.Name == name) {
                    if (restrictions.ContainsKey (rest.UserId))
                        restrictions [rest.UserId] = rest;
                    else
                        restrictions.Add (rest.UserId, rest);

                    continue;
                }

                RestrictionNode node = FindNode (rest.Name);
                if (node == null)
                    continue;

                node.SetRestrictions (rest);
            }
        }

        public void SetRestriction (UserAccessLevel level, string restName, UserRestrictionState state)
        {
            if (restName == name) {
                int levelId = GetAccessLevelId (level);
                if (restrictions.ContainsKey (levelId))
                    restrictions [levelId].State = state;
                else
                    restrictions.Add (levelId, new UserRestriction (levelId, restName, state));

                return;
            }

            RestrictionNode node = FindNode (restName);
            if (node == null)
                return;

            node.SetRestriction (level, restName, state);
        }

        public void SetRestriction (long userId, string restName, UserRestrictionState state)
        {
            if (restName == name) {
                if (restrictions.ContainsKey (userId))
                    restrictions [userId].State = state;
                else
                    restrictions.Add (userId, new UserRestriction (userId, restName, state));

                return;
            }

            RestrictionNode node = FindNode (restName);
            if (node == null)
                return;

            node.SetRestriction (userId, restName, state);
        }

        public RestrictionNode RestrictByDefaultFor (params UserAccessLevel [] levels)
        {
            foreach (UserAccessLevel level in levels) {
                int levelId = GetDefaultAccessLevelId (level);
                if (restrictions.ContainsKey (levelId))
                    restrictions [levelId].State = UserRestrictionState.Restricted;
                else
                    restrictions.Add (levelId, new UserRestriction (levelId, name, UserRestrictionState.Restricted));
            }

            foreach (RestrictionNode child in children)
                child.RestrictByDefaultFor (levels);

            return this;
        }

        public void ResetLevelRestrictions (long userId, UserAccessLevel level)
        {
            UserRestrictionState state = GetRestriction (level);
            if (restrictions.ContainsKey (userId))
                restrictions [userId].State = state;
            else if (state != UserRestrictionState.Allowed)
                restrictions.Add (userId, new UserRestriction (userId, name, state));

            foreach (RestrictionNode child in Children)
                child.ResetLevelRestrictions (userId, level);
        }

        private static int GetAccessLevelId (UserAccessLevel level)
        {
            return (int) level - 10;
        }

        private static int GetDefaultAccessLevelId (UserAccessLevel level)
        {
            return (int) level - 20;
        }

        public UserRestrictionState GetRestriction (string nodeName)
        {
            User loggedUser = BusinessDomain.LoggedUser;
            return loggedUser.IsSaved ? GetRestriction (nodeName, loggedUser.Id) : UserRestrictionState.Allowed;
        }

        private UserRestrictionState GetRestriction (string nodeName, long userId)
        {
            RestrictionNode node = FindNode (nodeName);

            return node == null ? UserRestrictionState.Allowed : node.GetRestriction (userId);
        }

        public UserRestrictionState GetRestriction (UserAccessLevel level)
        {
            int levelId = GetAccessLevelId (level);
            return restrictions.ContainsKey (levelId) ?
                restrictions [levelId].State :
                UserRestrictionState.Allowed;
        }

        public UserRestrictionState GetRestriction (long userId)
        {
            // Check for restrictions that apply to the specified user
            if (restrictions.ContainsKey (userId))
                return restrictions [userId].State;

            // Check for restrictions that apply to all the users
            if (restrictions.ContainsKey (User.AllId))
                return restrictions [User.AllId].State;

            return UserRestrictionState.Allowed;
        }

        private UserRestrictionState GetDefaultRestriction (UserAccessLevel level)
        {
            int levelId = GetDefaultAccessLevelId (level);
            return restrictions.ContainsKey (levelId) ?
                restrictions [levelId].State :
                UserRestrictionState.Allowed;
        }

        public void SaveRestrictions ()
        {
            UserRestriction.CommitChanges (GetAllRestrictions (this));
        }

        private static IEnumerable<UserRestriction> GetAllRestrictions (RestrictionNode restrictionNode)
        {
            List<UserRestriction> allRestrictions = restrictionNode.restrictions
                .Where (restriction => restriction.Key >= 0)
                .Select (restriction => restriction.Value).ToList ();

            foreach (RestrictionNode child in restrictionNode.Children)
                allRestrictions.AddRange (GetAllRestrictions (child));

            return allRestrictions;
        }

        public override object Clone ()
        {
            RestrictionNode clone = (RestrictionNode) base.Clone ();
            clone.restrictions = new Dictionary<long, UserRestriction> ();
            foreach (KeyValuePair<long, UserRestriction> restriction in restrictions) {
                UserRestriction userRestriction = new UserRestriction (restriction.Value.UserId, restriction.Value.Name, restriction.Value.State);
                clone.restrictions.Add (restriction.Key, userRestriction);
            }
            return clone;
        }

        public void ReloadAccessLevelRestrictions ()
        {
            foreach (UserAccessLevel level in Enum.GetValues (typeof (UserAccessLevel)))
                ReloadAccessLevelDefaults (level);

            XDocument xDocument = Encryption.LoadEncryptedXML (StoragePaths.AccessLevelsFile);
            if (xDocument == null || xDocument.Root == null)
                return;

            foreach (XElement accessLevelNode in xDocument.Root.Elements ()) {
                XAttribute attributeName = accessLevelNode.Attribute ("name");
                if (attributeName == null)
                    continue;

                UserAccessLevel accessLevel;
                if (!Enum.TryParse (attributeName.Value, out accessLevel))
                    continue;

                foreach (XElement restrictionElement in accessLevelNode.Elements ("restriction"))
                    ReadRestrictionFromXML (accessLevel, restrictionElement);
            }
        }

        private void ReloadAccessLevelDefaults (UserAccessLevel level)
        {
            UserRestrictionState state = GetDefaultRestriction (level);
            int levelId = GetAccessLevelId (level);
            if (restrictions.ContainsKey (levelId))
                restrictions [levelId].State = state;
            else if (state != UserRestrictionState.Allowed)
                restrictions.Add (levelId, new UserRestriction (levelId, name, state));

            foreach (RestrictionNode child in Children)
                child.ReloadAccessLevelDefaults (level);
        }

        private void ReadRestrictionFromXML (UserAccessLevel accessLevel, XElement restrictionElement)
        {
            XAttribute attributeName = restrictionElement.Attribute ("name");
            if (attributeName == null)
                return;

            RestrictionNode node = FindNode (attributeName.Value);
            if (node == null)
                return;

            XAttribute attributeValue = restrictionElement.Attribute ("value");
            UserRestrictionState state;
            if (attributeValue == null || !Enum.TryParse (attributeValue.Value, out state))
                state = node.GetDefaultRestriction (accessLevel);

            node.SetRestriction (accessLevel, node.name, state);
        }

        public void SaveAccessLevelRestrictions ()
        {
            XDocument xDocument = new XDocument (new XElement ("accessLevels"));
            foreach (UserAccessLevel level in Enum.GetValues (typeof (UserAccessLevel))) {
                XElement restrictionElement = new XElement ("accessLevel");
                restrictionElement.Add (new XAttribute ("name", level));
                foreach (RestrictionNode child in children)
                    child.WriteRestrictionToXML (restrictionElement, level);

                xDocument.Root.Add (restrictionElement);
            }

            Encryption.SaveEncryptedXML (StoragePaths.AccessLevelsFile, xDocument);
        }

        private void WriteRestrictionToXML (XContainer parent, UserAccessLevel accessLevel)
        {
            UserRestrictionState state = GetRestriction (accessLevel);
            if (state != GetDefaultRestriction (accessLevel)) {
                XElement restrictionElement = new XElement ("restriction");
                restrictionElement.Add (new XAttribute ("name", name));
                restrictionElement.Add (new XAttribute ("value", state));
                parent.Add (restrictionElement);
            }

            foreach (RestrictionNode child in children)
                child.WriteRestrictionToXML (parent, accessLevel);
        }
    }
}
