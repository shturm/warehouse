//
// PartnersRetail.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   07.07.2011
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
using Gtk;
using Mono.Addins;
using Warehouse.Business;
using Warehouse.Business.Entities;
using Warehouse.Presentation.Dialogs;

namespace Warehouse.Presentation.SetupAssistant
{
    [Extension ("/Warehouse/Presentation/SetupAssistant/PartnersSetup")]
    internal class CustomersRetail : PartnersSetup
    {
        private class CustomerWrapper
        {
            private readonly Partner customer;

            public Partner Customer
            {
                get { return customer; }
            }

            public CustomerWrapper (Partner customer)
            {
                this.customer = customer;
            }

            public void CommitChanges ()
            {
                customer.CommitChanges ();
            }

            public bool Validate (ValidateCallback callback, StateHolder state)
            {
                return customer.Validate (callback, state);
            }
        }

        private Table tbl;
        private Label lblNumCustomers;
        private SpinButton spbNumCustomers;
        private Table tblCustomers;

        private readonly List<Partner> customers = new List<Partner> ();
        private readonly Dictionary<Partner, Entry> entries = new Dictionary<Partner, Entry> ();
        private readonly Dictionary<Partner, SpinButton> spins = new Dictionary<Partner, SpinButton> ();

        #region Overrides of LocationsSet

        public override int Ordinal
        {
            get { return 10; }
        }

        public override string Label
        {
            get { return Translator.GetString ("Retail store"); }
        }

        public override Widget GetPageWidget ()
        {
            if (tbl != null)
                return tbl;

            tbl = new Table (1, 2, false) { RowSpacing = 4 };

            lblNumCustomers = new Label (Translator.GetString ("Number of separate customer profiles to create:")) { Xalign = 0f };
            tbl.Attach (lblNumCustomers, 0, 1, 0, 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 10, 2);

            spbNumCustomers = new SpinButton (1d, 20d, 1d) { Alignment = 1f };
            spbNumCustomers.ValueChanged += spbNumCustomers_Changed;
            tbl.Attach (spbNumCustomers, 1, 2, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 10, 2);

            Label lblCustomersList = new Label (Translator.GetString ("Customers to be created:")) { Xalign = 0f };
            tbl.Attach (lblCustomersList, 0, 3, 1, 2, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 10, 2);

            tblCustomers = new Table (1, 1, false) { RowSpacing = 2 };
            tbl.Attach (tblCustomers, 0, 3, 2, 3, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill | AttachOptions.Expand, 0, 0);
            tbl.ShowAll ();

            RecreateCustomers (true);

            return tbl;
        }

        #region Save changes

        private IList<CustomerWrapper> GetAllCustomers ()
        {
            List<CustomerWrapper> ret = new List<CustomerWrapper> ();

            foreach (Partner customer in customers) {
                Entry oldEntry;
                if (entries.TryGetValue (customer, out oldEntry))
                    customer.Name = oldEntry.Text;

                ret.Add (new CustomerWrapper (customer));
            }

            return ret;
        }

        public override bool Validate ()
        {
            IList<CustomerWrapper> allCustomers = GetAllCustomers ();

            for (int i = 0; i < allCustomers.Count - 2; i++) {
                for (int j = i + 1; j < allCustomers.Count - 1; j++) {
                    if (allCustomers [i].Customer.Name != allCustomers [j].Customer.Name)
                        continue;

                    if (MessageError.ShowDialog (string.Format (Translator.GetString ("Customer with the name \"{0}\" is used more than once! Are you sure you want to continue?"), allCustomers [i].Customer.Name),
                        buttons: MessageButtons.YesNo) != ResponseType.Yes)
                        return false;
                }
            }

            return allCustomers.All (customer => customer.Validate ((message, severity, code, state) =>
                {
                    using (MessageError dlgError = new MessageError (message, severity))
                        if (severity == ErrorSeverity.Warning) {
                            dlgError.Buttons = MessageButtons.YesNo;
                            if (dlgError.Run () != ResponseType.Yes)
                                return false;
                        } else {
                            dlgError.Run ();
                            return false;
                        }

                    return true;
                }, null));
        }

        public override bool CommitChanges (StepPartners step, Assistant assistant)
        {
            BackgroundJob job = new BackgroundJob (step);
            job.Action += () =>
                {
                    PresentationDomain.Invoke (() => { step.Notebook.Sensitive = false; });
                    IList<CustomerWrapper> allCustomers = GetAllCustomers ();
                    if (allCustomers.Count <= 0)
                        return;

                    CustomerWrapper def = allCustomers [0];
                    // Substitute the default customer
                    def.Customer.Id = Partner.DefaultId;
                    def.CommitChanges ();

                    foreach (CustomerWrapper customer in allCustomers)
                        customer.CommitChanges ();
                };
            assistant.EnqueueBackgroundJob (job);

            return true;
        }

        #endregion

        private void spbNumCustomers_Changed (object sender, EventArgs e)
        {
            RecreateCustomers (false);
        }

        private void RecreateCustomers (bool removeAll)
        {
            int i;

            int removeUntil = spbNumCustomers.ValueAsInt;
            if (removeAll)
                removeUntil = 0;

            for (i = customers.Count - 1; i >= removeUntil; i--) {
                Partner customer = customers [i];
                entries.Remove (customer);
                spins.Remove (customer);
                customers.Remove (customer);
            }

            if (customers.Count == 0)
                customers.Add (new Partner { Name = Translator.GetString ("Default customer") });
            else {
                int j;
                for (i = customers.Count, j = customers.Count; i < spbNumCustomers.ValueAsInt; i++) {
                    string newName = GetNewCustomerName (j++);
                    while (!customers.TrueForAll (l => l.Name != newName))
                        newName = GetNewCustomerName (j++);

                    customers.Add (new Partner { Name = newName });
                }
            }

            RecreateCustomersTable ();
        }

        private static string GetNewCustomerName (int index)
        {
            return string.Format (Translator.GetString ("Customer {0}"), index + 1);
        }

        private void RecreateCustomersTable ()
        {
            tblCustomers.Hide ();
            for (int i = tblCustomers.Children.Length - 1; i >= 0; i--)
                tblCustomers.Remove (tblCustomers.Children [i]);

            uint index = 0;
            foreach (Partner customer in customers) {
                string name = customer.Name;
                Entry oldEntry;
                if (entries.TryGetValue (customer, out oldEntry))
                    name = oldEntry.Text;

                Entry txtName = new Entry (name) { WidthChars = 10 };
                entries [customer] = txtName;

                tblCustomers.Attach (txtName, index % 2, (index % 2) + 1, index / 2, (index / 2) + 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 10, 0);
                index++;
            }

            tblCustomers.ShowAll ();
        }

        #endregion
    }
}
