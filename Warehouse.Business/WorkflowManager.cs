//
// WorkflowManager.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   09.20.2011
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

using Warehouse.Business.Entities;
using Warehouse.Business.Operations;
using Warehouse.Data;

namespace Warehouse.Business
{
    public class WorkflowManager : WorkflowManagerBase
    {
        public WorkflowManager ()
        {
            ConfigurationProvider.CustomDefaults.Add ("EmailSubject", DataHelper.ProductName);
            // On Windows we install MySQL by default
            if (!PlatformHelper.IsWindows)
                ConfigurationProvider.CustomDefaults.Add ("DbProvider", "Warehouse.Data.SQLite.DataProvider");
        }

        public override void SendEmailAsync<T> (Operation<T> operation, string recipients, string body)
        {
            EmailServer emailServer = new EmailServer ();
            emailServer.SendCompleted += (sender, e) =>
                {
                    switch (e.EmailResult) {
                        case EmailResult.NoSmtpServer:
                            BusinessDomain.OnPriceRuleMessage (new PriceRuleMessageEventArgs (Translator.GetString (
                                "The price rule could not send an e-mail because you haven't " +
                                "specified an SMTP server in the application settings."), ErrorSeverity.Error));
                            break;
                        case EmailResult.NoSender:
                            BusinessDomain.OnPriceRuleMessage (new PriceRuleMessageEventArgs (Translator.GetString (
                                "The price rule could not send an e-mail because you haven't " +
                                "specified an email sender in the application settings."), ErrorSeverity.Error));
                            break;
                        case EmailResult.InvalidPort:
                            BusinessDomain.OnPriceRuleMessage (new PriceRuleMessageEventArgs (Translator.GetString (
                                "The price rule could not send an e-mail because the port you " +
                                "specified for the SMTP server is invalid."), ErrorSeverity.Error));
                            break;
                        case EmailResult.SmtpError:
                            BusinessDomain.OnPriceRuleMessage (new PriceRuleMessageEventArgs (Translator.GetString (
                                "The price rule could not send an e-mail because of an error in the communication " +
                                "with the SMTP server. Please check the settings of your SMTP server."), ErrorSeverity.Error));
                            break;
                    }
                };
            emailServer.SendEmail (recipients, null, body, true);
        }
    }
}
