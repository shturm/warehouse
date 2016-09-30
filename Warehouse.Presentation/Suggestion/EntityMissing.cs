//
// EntityMissing.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   06/24/2006
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
using Glade;
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Component;
using Warehouse.Presentation.Dialogs;
using Item = Warehouse.Business.Entities.Item;

namespace Warehouse.Presentation.Suggestion
{
    public class EntityMissing : DialogBase
    {
        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgEntityMissing;

        [Widget]
        private VBox vboOptions;

        [Widget]
        private Button btnClose;

#pragma warning restore 649

        #endregion

        public override Dialog DialogControl
        {
            get { return dlgEntityMissing; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        private object SuggestedEntity { get; set; }

        public EntityMissing ()
        {
            Initialize ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("Suggestion.EntityMissing.glade", "dlgEntityMissing");
            form.Autoconnect (this);

            dlgEntityMissing.Icon = FormHelper.LoadImage ("Icons.AppMain16.png").Pixbuf;

            btnClose.SetChildImage (FormHelper.LoadImage ("Icons.Cancel24.png"));

            base.InitializeForm ();

            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            btnClose.SetChildLabelText (Translator.GetString ("Close"));
        }

        private void SetSuggestions (IEnumerable<ISuggestionProvider> providers, string value, object operation)
        {
            foreach (ISuggestionProvider provider in providers) {
                foreach (Widget w in provider.GetWidget (value, operation))
                    vboOptions.PackStart (w, true, true, 0);

                provider.EntitySuggested += provider_EntitySuggested;
            }
        }

        private void provider_EntitySuggested (object sender, EntitySuggestedEventArgs e)
        {
            SuggestedEntity = e.Entity;
            dlgEntityMissing.Respond (ResponseType.Ok);
        }

        [UsedImplicitly]
        private void btnClose_Clicked (object o, EventArgs args)
        {
            dlgEntityMissing.Respond (ResponseType.Cancel);
        }

        private static List<ISuggestionProvider> allSuggestionProviders;
        private static List<ISuggestionProvider> AllSuggestionProviders
        {
            get
            {
                return allSuggestionProviders ?? (allSuggestionProviders = AddinManager
                    .GetExtensionNodes ("/Warehouse/Presentation/EntityMissing")
                    .Cast<TypeExtensionNode> ()
                    .Select (node => node.CreateInstance ())
                    .OfType<ISuggestionProvider> ()
                    .OrderBy (s => s.Order)
                    .ToList ());
            }
        }

        /// <summary>
        /// Shows suggestions for new items to be added to the system
        /// </summary>
        /// <param name="value">Returns suggested items or null if browsing for items should be used</param>
        /// <param name="operation"></param>
        /// <returns></returns>
        public static Item [] ShowItemMissing (string value, object operation)
        {
            var providers = AllSuggestionProviders.Where (s =>
                s.SupportedEntities == SuggestionEntityType.Item &&
                s.CheckAvailableFor (value)).ToList ();

            if (providers.Count == 0)
                return null;

            providers.Add (new ItemBrowse ());
            providers.Add (new ItemAlwaysBrowse ());

            Item suggestedEntity;
            using (EntityMissing dialog = new EntityMissing ()) {
                dialog.DialogControl.Title = string.Format (Translator.GetString ("Item \"{0}\" cannot be found!"), value);
                dialog.DialogControl.Icon = FormHelper.LoadImage ("Icons.Goods16.png").Pixbuf;
                dialog.SetSuggestions (providers, value, operation);
                if (dialog.Run () != ResponseType.Ok)
                    return new Item [0];

                suggestedEntity = (Item) dialog.SuggestedEntity;
                if (suggestedEntity == null)
                    return null;
            }

            if (BusinessDomain.AppConfiguration.AutoGenerateItemCodes && string.IsNullOrWhiteSpace (suggestedEntity.Code))
                suggestedEntity.AutoGenerateCode ();

            using (EditNewItem dlgItem = new EditNewItem (suggestedEntity, allowSaveAndNew: false)) {
                if (dlgItem.Run () != ResponseType.Ok)
                    return new Item [0];

                return new [] { dlgItem.GetItem ().CommitChanges () };
            }
        }

        /// <summary>
        /// Shows suggestions for new partners to be added to the system
        /// </summary>
        /// <param name="value">Returns suggested partners or null if browsing for partners should be used</param>
        /// <param name="operation"></param>
        /// <returns></returns>
        public static Partner [] ShowPartnerMissing (string value, object operation)
        {
            var providers = AllSuggestionProviders.Where (s =>
                s.SupportedEntities == SuggestionEntityType.Partner &&
                s.CheckAvailableFor (value)).ToList ();

            if (providers.Count == 0)
                return null;

            providers.Add (new PartnerBrowse ());
            providers.Add (new PartnerAlwaysBrowse ());

            Partner suggestedEntity;
            using (EntityMissing dialog = new EntityMissing ()) {
                dialog.DialogControl.Title = string.Format (Translator.GetString ("Partner \"{0}\" cannot be found!"), value);
                dialog.DialogControl.Icon = FormHelper.LoadImage ("Icons.Partner16.png").Pixbuf;
                dialog.SetSuggestions (providers, value, operation);
                if (dialog.Run () != ResponseType.Ok)
                    return new Partner [0];

                suggestedEntity = (Partner) dialog.SuggestedEntity;
                if (suggestedEntity == null)
                    return null;
            }

            // Getti may use an existing partner when a customer card is swiped
            if (suggestedEntity.Id > 0)
                return new [] { suggestedEntity };

            if (BusinessDomain.AppConfiguration.AutoGeneratePartnerCodes && string.IsNullOrWhiteSpace (suggestedEntity.Code))
                suggestedEntity.AutoGenerateCode ();

            using (EditNewPartner dlgItem = new EditNewPartner (suggestedEntity, allowSaveAndNew: false)) {
                if (dlgItem.Run () != ResponseType.Ok)
                    return new Partner [0];

                return new [] { dlgItem.GetPartner ().CommitChanges () };
            }
        }
    }
}
