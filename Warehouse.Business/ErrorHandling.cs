//
// ErrorHandling.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   03/21/2006
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using Warehouse.Data;

namespace Warehouse.Business
{
    public enum ErrorSeverity
    {
        /// <summary>Information about certain event is being passed.</summary>
        Information,
        /// <summary>Something inappropriate happened.</summary>
        Warning,
        /// <summary>Error occurred but is still handled.</summary>
        Error,
        /// <summary>High priority error. The application needs to close.</summary>
        FatalError
    }

    public static class ErrorHandling
    {
        public static event Action<Exception, ErrorSeverity> ExceptionOccurred;

        public static event Action<string, ErrorSeverity> ErrorOccurred;

        private static readonly object sync = new object ();

        public static string ErrLogFileName
        {
            get
            {
                string erroLogFile = BusinessDomain.AppConfiguration != null ?
                    BusinessDomain.AppConfiguration.ErrorLogFileName : "errlog.xml";

                return Path.Combine (StoragePaths.UserAppDataFolder, erroLogFile);
            }
        }

        private static bool VerboseLogging
        {
            get
            {
                return BusinessDomain.AppConfiguration == null || BusinessDomain.AppConfiguration.VerboseErrorLogging;
            }
        }

        private static int LogMaxSize
        {
            get
            {
                return BusinessDomain.AppConfiguration != null ?
                    BusinessDomain.AppConfiguration.ErrorLogMaxFileSize : 1000000;
            }
        }

        public static void LogException (Exception ex, ErrorSeverity sev = ErrorSeverity.Error)
        {
            if (ex == null)
                return;

            try {
                Action<Exception, ErrorSeverity> handler = ExceptionOccurred;
                if (handler != null)
                    handler (ex, sev);

                if (sev != ErrorSeverity.FatalError && !VerboseLogging)
                    return;
                
                string fileName = ErrLogFileName;
                lock (sync) {
                    // Load the error log if exists
                    XmlDocument xml = LoadLogFile (fileName);

                    // Log the exception information
                    LogExceptionInfoToXML (xml, sev, ex);

                    // Save the log
                    xml.Save (fileName);
                }
            } catch (Exception) { }
        }

        public static void LogError (string message, ErrorSeverity sev = ErrorSeverity.Error)
        {
            if (string.IsNullOrEmpty (message))
                return;

            try {
                Action<string, ErrorSeverity> handler = ErrorOccurred;
                if (handler != null)
                    handler (message, sev);

                if (sev != ErrorSeverity.FatalError && !VerboseLogging)
                    return;
                
                string fileName = ErrLogFileName;
                lock (sync) {
                    // Load the error log if exists
                    XmlDocument xml = LoadLogFile (fileName);

                    // Log the error information
                    LogErrorInfoToXML (xml, sev, message);

                    // Save the log
                    xml.Save (fileName);
                }
            } catch { }
        }

        #region XML Logging management

        public static XmlDocument LoadLogFile (string fileName)
        {
            XmlDocument xml = new XmlDocument ();

            if (File.Exists (fileName)) {
                FileStream fileStream = File.Open (fileName, FileMode.Open);

                if (fileStream.Length >= LogMaxSize) {
                    fileStream.Close ();
                    File.Delete (fileName + ".1");
                    File.Move (fileName, fileName + ".1");
                } else {
                    fileStream.Close ();

                    try {
                        xml.Load (fileName);
                    } catch (XmlException) {
                    }
                }
            } else
                xml.LoadXml (@"<?xml version=""1.0"" encoding=""utf-8"" ?><errors/>");

            EnsureCorrectErrorsNode (xml);

            return xml;
        }

        private static XmlNode CreateXMLErrorNode (XmlDocument xml, ErrorSeverity severity)
        {
            // Create the error node and it's attributes and write it
            XmlNode errorNode = xml.CreateNode (XmlNodeType.Element, "error", null);

            XmlAttribute attrError = xml.CreateAttribute ("timestamp");
            attrError.Value = BusinessDomain.Now.ToString ("MM/dd/yyyy HH:mm:ss");
            errorNode.Attributes.Append (attrError);

            attrError = xml.CreateAttribute ("version");
            attrError.Value = BusinessDomain.ApplicationVersionString;
            errorNode.Attributes.Append (attrError);

            attrError = xml.CreateAttribute ("severity");
            attrError.Value = Enum.GetName (typeof (ErrorSeverity), severity);
            errorNode.Attributes.Append (attrError);

            EnsureCorrectErrorsNode (xml).AppendChild (errorNode);

            return errorNode;
        }

        public static XmlNode EnsureCorrectErrorsNode (XmlDocument xml)
        {
            XmlNode errorsNode = xml.SelectSingleNode (@"/errors");

            // If the root node is missing create the node
            if (errorsNode == null) {
                errorsNode = xml.CreateNode (XmlNodeType.Element, "errors", null);
                xml.AppendChild (errorsNode);
            }
            
            XmlAttribute attr;
            if (errorsNode.Attributes ["os"] == null) {
                attr = xml.CreateAttribute ("os");
                attr.Value = PlatformHelper.Platform.ToString ();
                errorsNode.Attributes.Append (attr);
            }

            if (errorsNode.Attributes ["product"] == null) {
                attr = xml.CreateAttribute ("product");
                attr.Value = DataHelper.ProductFullName;
                errorsNode.Attributes.Append (attr);
            }

            return errorsNode;
        }

        private static void LogExceptionInfoToXML (XmlDocument xml, ErrorSeverity sev, Exception ex)
        {
            // Create child node for exception description
            XmlNode exceptionsNode = xml.CreateNode (XmlNodeType.Element, "exceptions", null);
            // Create an error node to hold the information
            XmlNode rootNode = CreateXMLErrorNode (xml, sev);
            rootNode.AppendChild (exceptionsNode);

            for (int i = 0; i < 10 && ex != null; i++, ex = ex.InnerException) {
                XmlNode exceptionNode = xml.CreateNode (XmlNodeType.Element, "exception", null);

                // Add the type attribute to the exception node
                XmlAttribute attrExceptionType = xml.CreateAttribute ("type");
                attrExceptionType.Value = ex.GetType ().FullName;
                exceptionNode.Attributes.Append (attrExceptionType);

                // Add the message attribute to the exception node
                XmlAttribute attrExceptionMessage = xml.CreateAttribute ("message");
                attrExceptionMessage.Value = ex.Message;
                exceptionNode.Attributes.Append (attrExceptionMessage);

                // Attach the exception node
                exceptionsNode.AppendChild (exceptionNode);

                // Add stack trace information
                LogStackTraceToXML (xml, exceptionNode, new StackTrace (ex, true));
            }
        }

        private static void LogStackTraceToXML (XmlDocument xml, XmlNode rootNode, StackTrace stackTrace)
        {
            // Create child node for stack trace
            XmlNode stackTraceNode = xml.CreateNode (XmlNodeType.Element, "stackTrace", null);
            rootNode.AppendChild (stackTraceNode);

            for (int i = 0; i < stackTrace.FrameCount; i++) {
                XmlNode frameNode = xml.CreateNode (XmlNodeType.Element, "frame", null);
                StackFrame frame = stackTrace.GetFrame (i);
                MethodBase method = frame.GetMethod ();

                //attrFrame = xml.CreateAttribute ("assembly");
                //attrFrame.Value = method.Module.Name;
                //frameNode.Attributes.Append (attrFrame);

                XmlAttribute attrFrame = xml.CreateAttribute ("class");
                attrFrame.Value = method.DeclaringType.ToString ();
                frameNode.Attributes.Append (attrFrame);

                attrFrame = xml.CreateAttribute ("method");
                attrFrame.Value = method.Name;
                frameNode.Attributes.Append (attrFrame);

                attrFrame = xml.CreateAttribute ("line");
                attrFrame.Value = frame.GetFileLineNumber ().ToString (CultureInfo.InvariantCulture);
                frameNode.Attributes.Append (attrFrame);

                stackTraceNode.AppendChild (frameNode);
            }
        }

        private static void LogErrorInfoToXML (XmlDocument xml, ErrorSeverity sev, string error)
        {
            XmlNode errorNode = xml.CreateNode (XmlNodeType.Element, "error", null);

            // Add the message attribute to the error node
            XmlAttribute attrError = xml.CreateAttribute ("message");
            attrError.Value = error;
            errorNode.Attributes.Append (attrError);

            // Create an error node to hold the information
            XmlNode rootNode = CreateXMLErrorNode (xml, sev);
            // Attach the error node
            rootNode.AppendChild (errorNode);

            // Add stack trace information
            LogStackTraceToXML (xml, errorNode, new StackTrace (true));
        }

        #endregion

        public static IErrorHandler GetHelper ()
        {
            return new ErrorHandlingHelper ();
        }
    }

    public class ErrorHandlingHelper : IErrorHandler
    {
        #region IErrorHandler Members

        public void LogException (Exception ex)
        {
            ErrorHandling.LogException (ex);
        }

        public void LogError (string message)
        {
            ErrorHandling.LogError (message);
        }

        #endregion
    }
}
