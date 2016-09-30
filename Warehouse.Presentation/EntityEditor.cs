//
// EntityEditor.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   10.06.2011
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
using Gtk;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Data;
using Warehouse.Presentation.Dialogs;
using Item = Warehouse.Business.Entities.Item;

namespace Warehouse.Presentation
{
    public static class EntityEditor
    {
        public static void OpenEntityForEdit (SourceItemId entity)
        {
            object id;
            long intId;
            object type;

            switch (entity.Table) {
                case DbTable.Unknown:
                    break;
                case DbTable.ApplicationLog:
                    break;
                case DbTable.Cashbook:
                    break;
                case DbTable.Configuration:
                    break;
                case DbTable.Currencies:
                    break;
                case DbTable.CurrenciesHistory:
                    break;
                case DbTable.Documents:
                    id = entity [DataField.DocumentOperationNumber];
                    type = entity [DataField.DocumentType];
                    if (type == null)
                        break;

                    try {
                        type = Enum.ToObject (typeof (OperationType), type);
                        intId = Convert.ToInt64 (id);
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                        break;
                    }

                    FormHelper.EditOperation ((OperationType) type, intId);
                    break;
                case DbTable.EcrReceipts:
                    break;
                case DbTable.Items:
                    id = entity [DataField.ItemId];
                    try {
                        intId = Convert.ToInt64 (id);
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                        break;
                    }

                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditGoodsbtnEdit") != UserRestrictionState.Allowed)
                        break;

                    using (EditNewItem dialog = new EditNewItem (Item.GetById (intId)))
                        if (dialog.Run () == ResponseType.Ok)
                            using (new DbMasterScope (BusinessDomain.DataAccessProvider)) 
                                dialog.GetItem ().CommitChanges ();
                    break;
                case DbTable.ItemsGroups:
                    id = entity [DataField.ItemGroupId];
                    try {
                        intId = Convert.ToInt64 (id);
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                        break;
                    }

                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditGoodsbtnEdit") != UserRestrictionState.Allowed)
                        break;

                    using (EditNewGroup<ItemsGroup> dialog = new EditNewGroup<ItemsGroup> (ItemsGroup.GetById (intId), ItemsGroup.GetAll ()))
                        if (dialog.Run () == ResponseType.Ok)
                            using (new DbMasterScope (BusinessDomain.DataAccessProvider))
                                dialog.GetGroup ().CommitChanges ();
                    break;
                case DbTable.InternalLog:
                    break;
                case DbTable.Lots:
                    break;
                case DbTable.Network:
                    break;
                case DbTable.NextAcct:
                    break;
                case DbTable.Objects:
                    id = entity [DataField.LocationId];
                    try {
                        intId = Convert.ToInt64 (id);
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                        break;
                    }

                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditObjectsbtnEdit") != UserRestrictionState.Allowed)
                        break;

                    using (EditNewLocation dialog = new EditNewLocation (Location.GetById (intId)))
                        if (dialog.Run () == ResponseType.Ok)
                            using (new DbMasterScope (BusinessDomain.DataAccessProvider))
                                dialog.GetLocation ().CommitChanges ();
                    break;
                case DbTable.ObjectsGroups:
                    id = entity [DataField.LocationsGroupsId];
                    try {
                        intId = Convert.ToInt64 (id);
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                        break;
                    }

                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditObjectsbtnEdit") != UserRestrictionState.Allowed)
                        break;

                    using (EditNewGroup<LocationsGroup> dialog = new EditNewGroup<LocationsGroup> (LocationsGroup.GetById (intId), LocationsGroup.GetAll ()))
                        if (dialog.Run () == ResponseType.Ok)
                            using (new DbMasterScope (BusinessDomain.DataAccessProvider))
                                dialog.GetGroup ().CommitChanges ();
                    break;
                case DbTable.Operations:
                    type = entity [DataField.OperationType];
                    if (type == null)
                        break;

                    id = entity [DataField.OperationNumber];
                    try {
                        type = Enum.ToObject (typeof (OperationType), type);
                        intId = Convert.ToInt64 (id);
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                        break;
                    }

                    FormHelper.EditOperation ((OperationType) type, intId);
                    break;
                case DbTable.OperationDetails:
                    break;
                case DbTable.OperationType:
                    break;
                case DbTable.Partners:
                    id = entity [DataField.PartnerId];
                    try {
                        intId = Convert.ToInt64 (id);
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                        break;
                    }

                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditPartnersbtnEdit") != UserRestrictionState.Allowed)
                        break;

                    using (EditNewPartner dialog = new EditNewPartner (Partner.GetById (intId)))
                        if (dialog.Run () == ResponseType.Ok)
                            using (new DbMasterScope (BusinessDomain.DataAccessProvider))
                                dialog.GetPartner ().CommitChanges ();
                    break;
                case DbTable.PartnersGroups:
                    id = entity [DataField.PartnersGroupsId];
                    try {
                        intId = Convert.ToInt64 (id);
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                        break;
                    }

                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditPartnersbtnEdit") != UserRestrictionState.Allowed)
                        break;

                    using (EditNewGroup<PartnersGroup> dialog = new EditNewGroup<PartnersGroup> (PartnersGroup.GetById (intId), PartnersGroup.GetAll ()))
                        if (dialog.Run () == ResponseType.Ok)
                            using (new DbMasterScope (BusinessDomain.DataAccessProvider))
                                dialog.GetGroup ().CommitChanges ();
                    break;
                case DbTable.Payments:
                    type = entity [DataField.PaymentOperationType];
                    if (type == null)
                        break;

                    id = entity [DataField.PaymentOperationId];
                    try {
                        type = Enum.ToObject (typeof (OperationType), type);
                        intId = Convert.ToInt64 (id);
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                        break;
                    }

                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditPaysPaymentsbtnEdit") != UserRestrictionState.Allowed)
                        break;

                    Operation oper = Operation.GetById ((OperationType) type, intId);
                    if (oper == null)
                        break;

                    using (EditNewPayment dialog = new EditNewPayment (oper))
                        if (dialog.Run () == ResponseType.Ok)
                            using (new DbMasterScope (BusinessDomain.DataAccessProvider))
                                oper.CommitPayments ();
                    break;
                case DbTable.PaymentTypes:
                    id = entity [DataField.PaymentTypesId];
                    try {
                        intId = Convert.ToInt64 (id);
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                        break;
                    }

                    using (EditNewPaymentType dialog = new EditNewPaymentType (PaymentType.GetById (intId)))
                        if (dialog.Run () == ResponseType.Ok)
                            using (new DbMasterScope (BusinessDomain.DataAccessProvider))
                                dialog.GetPaymentType ().CommitChanges ();
                    break;
                case DbTable.PriceRules:
                    break;
                case DbTable.Registration:
                    break;
                case DbTable.Store:
                    break;
                case DbTable.System:
                    break;
                case DbTable.Transformations:
                    break;
                case DbTable.Users:
                    EditUser (entity [DataField.UserId]);
                    break;
                case DbTable.OperationUsers:
                    EditUser (entity [DataField.OperationsUserId]);
                    break;
                case DbTable.OperationOperators:
                    EditUser (entity [DataField.OperationsOperatorId]);
                    break;
                case DbTable.UsersGroup:
                    EditUserGroup (entity [DataField.UsersGroupsId]);
                    break;
                case DbTable.OperationUsersGroup:
                    EditUserGroup (entity [DataField.OperationsUsersGroupsId]);
                    break;
                case DbTable.OperationOperatorsGroup:
                    EditUserGroup (entity [DataField.OperationsOperatorsGroupsId]);
                    break;
                case DbTable.UsersSecurity:
                    break;
                case DbTable.VatGroups:
                    id = entity [DataField.VATGroupId];
                    try {
                        intId = Convert.ToInt64 (id);
                    } catch (Exception ex) {
                        ErrorHandling.LogException (ex);
                        break;
                    }

                    if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditVATGroupsbtnEdit") != UserRestrictionState.Allowed)
                        return;

                    using (EditNewVATGroup dialog = new EditNewVATGroup (VATGroup.GetById (intId)))
                        if (dialog.Run () == ResponseType.Ok)
                            using (new DbMasterScope (BusinessDomain.DataAccessProvider))
                                dialog.GetVATGroup ().CommitChanges ();
                    break;
            }
        }

        private static void EditUser (object id)
        {
            if (id == null)
                return;

            if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditUsersbtnEdit") != UserRestrictionState.Allowed)
                return;

            User user = User.GetById ((int) id);
            if (!user.CanEdit ())
                return;

            using (EditNewUser dialog = new EditNewUser (user))
                if (dialog.Run () == ResponseType.Ok)
                    using (new DbMasterScope (BusinessDomain.DataAccessProvider))
                        dialog.GetUser ().CommitChanges ();
        }

        private static void EditUserGroup (object id)
        {
            if (id == null)
                return;

            if (BusinessDomain.RestrictionTree.GetRestriction ("mnuEditUsersbtnEdit") != UserRestrictionState.Allowed)
                return;

            using (EditNewGroup<UsersGroup> dialog = new EditNewGroup<UsersGroup> (UsersGroup.GetById ((int) id), UsersGroup.GetAll ()))
                if (dialog.Run () == ResponseType.Ok)
                    using (new DbMasterScope (BusinessDomain.DataAccessProvider))
                        dialog.GetGroup ().CommitChanges ();
        }
    }
}
