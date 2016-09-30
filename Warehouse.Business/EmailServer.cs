//
// EmailServer.cs
//
// Author:
//   Dimitar Dobrev <dpldobrev at gmail dot com>
//
// Created:
//   11.20.2009
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
using System.ComponentModel;
using System.Net;
using System.Net.Mail;

namespace Warehouse.Business
{
    public class EmailServer
    {
        public class SendCompletedEventArgs : EventArgs
        {
            public SendCompletedEventArgs (EmailResult emailResult)
            {
                EmailResult = emailResult;
            }

            public EmailResult EmailResult { get; private set; }
        }

        public event EventHandler<SendCompletedEventArgs> SendCompleted;

        public EmailResult SendEmail (string recipients, string subject, string body, bool async, params string [] attachments)
        {
            string smtpServer = BusinessDomain.AppConfiguration.SmtpServer;
            EmailResult emailResult = 0;
            if (string.IsNullOrEmpty (smtpServer))
                emailResult = EmailResult.NoSmtpServer;

            if (string.IsNullOrEmpty (BusinessDomain.AppConfiguration.EmailSender))
                emailResult = EmailResult.NoSender;

            int smtpPort = BusinessDomain.AppConfiguration.SmtpPort;
            if (smtpPort <= 0)
                emailResult = EmailResult.InvalidPort;

            if (emailResult != 0) {
                OnSendCompleted (new SendCompletedEventArgs (emailResult));
                return emailResult;
            }

            try {
                MailMessage mailMessage = new MailMessage (
                    BusinessDomain.AppConfiguration.EmailSender ?? string.Empty,
                    recipients,
                    subject ?? BusinessDomain.AppConfiguration.EmailSubject,
                    body);

                foreach (string attachment in attachments)
                    mailMessage.Attachments.Add (new Attachment (attachment));

                string smtpUserName = BusinessDomain.AppConfiguration.SmtpUserName;
                SmtpClient smtpClient = new SmtpClient (smtpServer, smtpPort)
                    {
                        Credentials = string.IsNullOrEmpty (smtpUserName) ?
                            CredentialCache.DefaultNetworkCredentials :
                            new NetworkCredential (smtpUserName, BusinessDomain.AppConfiguration.SmtpPassword),
                        EnableSsl = BusinessDomain.AppConfiguration.SmtpUseSsl,
                        Timeout = 30000
                    };

                if (async) {
                    smtpClient.SendCompleted += SmtpClient_SendCompleted;
                    smtpClient.SendAsync (mailMessage, string.Empty);
                } else {
                    smtpClient.Send (mailMessage);
                    mailMessage.Dispose ();
                }
            } catch (Exception exception) {
                ErrorHandling.LogException (exception);
                return EmailResult.SmtpError;
            }
            return EmailResult.Success;
        }

        private void SmtpClient_SendCompleted (object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
                OnSendCompleted (new SendCompletedEventArgs (EmailResult.Success));
            else {
                ErrorHandling.LogException (e.Error);
                OnSendCompleted (new SendCompletedEventArgs (EmailResult.SmtpError));
            }
        }

        private void OnSendCompleted (SendCompletedEventArgs e)
        {
            EventHandler<SendCompletedEventArgs> handler = SendCompleted;
            if (handler != null)
                handler (null, e);
        }
    }
}
