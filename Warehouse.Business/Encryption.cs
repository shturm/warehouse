//
// Encryption.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   06/21/2006
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
using System.IO;
using System.Security;
using System.Text;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;

namespace Warehouse.Business
{
    public class Encryption
    {
        // 6380
        private static readonly byte [] defaultRgbKey = { 0x4d, 0x69, 0x63, 0x72, 0x6f, 0x69, 0x6e, 0x76 };
        private static readonly byte [] defaultRgbIV = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xab, 0xcd, 0xef };

        internal static string EncryptUserPassword (string sourceData)
        {
            if (string.IsNullOrEmpty (sourceData))
                sourceData = " ";

            return EncryptToBase64 (sourceData);
        }

        internal static string EncryptConfigurationValue (string sourceData)
        {
            return EncryptToBase64 (sourceData);
        }

        internal static string EncryptConfigurationValue (string sourceData, string keyFile)
        {
            byte [] rgbKey;
            byte [] rgbIV;
            LoadKeysFromFile (keyFile, out rgbKey, out rgbIV);

            return EncryptToBase64 (sourceData, rgbKey, rgbIV);
        }

        public static string EncryptToBase64 (string sourceData)
        {
            return EncryptToBase64 (sourceData, defaultRgbKey, defaultRgbIV);
        }

        public static string EncryptToBase64 (string sourceData, byte [] rgbKey, byte [] rgbIV)
        {
            try {
                using (MemoryStream outStream = new MemoryStream ()) {
                    using (DESCryptoServiceProvider cProvider = new DESCryptoServiceProvider ()) {
                        using (CryptoStream cStream = new CryptoStream (outStream, cProvider.CreateEncryptor (rgbKey, rgbIV), CryptoStreamMode.Write)) {
                            byte [] data = Encoding.UTF8.GetBytes (sourceData);
                            cStream.Write (data, 0, data.Length);
                            cStream.FlushFinalBlock ();

                            return Convert.ToBase64String (outStream.ToArray ());
                        }
                    }
                }
            } catch (Exception ex) {
                ErrorHandling.LogException (new Exception (string.Format ("Error while encrypting string \"{0}\"", sourceData), ex));
            }

            return string.Empty;
        }

        public static byte [] EncryptArray (byte [] bytes, ICryptoTransform encryptor)
        {
            using (MemoryStream ms = new MemoryStream ()) {
                using (CryptoStream cStream = new CryptoStream (ms, encryptor, CryptoStreamMode.Write)) {
                    cStream.Write (bytes, 0, bytes.Length);
                    cStream.FlushFinalBlock ();
                    cStream.Close ();

                    return ms.ToArray ();
                }
            }
        }

        internal static string DecryptConfigurationValue (string sourceData)
        {
            return DecryptFromBase64 (sourceData);
        }

        internal static string DecryptConfigurationValue (string sourceData, string keyFile)
        {
            byte [] rgbKey;
            byte [] rgbIV;
            LoadKeysFromFile (keyFile, out rgbKey, out rgbIV);

            return DecryptFromBase64 (sourceData, rgbKey, rgbIV);
        }

        public static string DecryptFromBase64 (string sourceData)
        {
            return DecryptFromBase64 (sourceData, defaultRgbKey, defaultRgbIV);
        }

        public static string DecryptFromBase64 (string sourceData, byte [] rgbKey, byte [] rgbIV)
        {
            try {
                using (MemoryStream outStream = new MemoryStream ()) {
                    using (DESCryptoServiceProvider cProvider = new DESCryptoServiceProvider ()) {
                        using (CryptoStream cStream = new CryptoStream (outStream, cProvider.CreateDecryptor (rgbKey, rgbIV), CryptoStreamMode.Write)) {
                            byte [] data = Convert.FromBase64String (sourceData);
                            cStream.Write (data, 0, data.Length);
                            cStream.FlushFinalBlock ();

                            return Encoding.UTF8.GetString (outStream.ToArray ());
                        }
                    }
                }
            } catch (Exception ex) {
                ErrorHandling.LogException (new Exception (string.Format ("Error while decrypting string \"{0}\"", sourceData), ex));
            }

            return string.Empty;
        }

        public static byte [] DecryptArray (byte [] bytes, ICryptoTransform decryptor)
        {
            try {
                using (MemoryStream outStream = new MemoryStream ()) {
                    using (CryptoStream cStream = new CryptoStream (outStream, decryptor, CryptoStreamMode.Write)) {
                        cStream.Write (bytes, 0, bytes.Length);
                        cStream.FlushFinalBlock ();

                        return outStream.ToArray ();
                    }
                }
            } catch (Exception) {
                return null;
            }
        }

        public static void CreateKeyFile (string file)
        {
            DESCryptoServiceProvider cProvider = new DESCryptoServiceProvider ();
            cProvider.GenerateIV ();
            cProvider.GenerateKey ();

            string [] content =
                {
                    Convert.ToBase64String (cProvider.Key),
                    Convert.ToBase64String (cProvider.IV)
                };

            File.WriteAllLines (file, content);
        }

        public static void SaveEncryptedXML (string file, XDocument xDocument)
        {
            try {
                File.WriteAllText (file, EncryptToBase64 (xDocument.ToString ()));
            } catch (CryptographicException ex) {
                ErrorHandling.LogError (ex.Message);
            } catch (UnauthorizedAccessException ex) {
                ErrorHandling.LogError (ex.Message);
            } catch (SecurityException ex) {
                ErrorHandling.LogError (ex.Message);
            } catch (IOException ex) {
                ErrorHandling.LogError (ex.Message);
            }
        }

        public static XDocument LoadEncryptedXML (string file)
        {
            if (!File.Exists (file))
                return null;
            try {
                return XDocument.Parse (DecryptFromBase64 (File.ReadAllText (file)));
            } catch (CryptographicException ex) {
                ErrorHandling.LogError (ex.Message);
                return null;
            } catch (XmlException ex) {
                ErrorHandling.LogError (ex.Message);
                return null;
            } catch (UnauthorizedAccessException ex) {
                ErrorHandling.LogError (ex.Message);
                return null;
            } catch (SecurityException ex) {
                ErrorHandling.LogError (ex.Message);
                return null;
            } catch (IOException ex) {
                ErrorHandling.LogError (ex.Message);
                return null;
            }
        }

        private static void LoadKeysFromFile (string file, out byte [] rgbKey, out byte [] rgbIV)
        {
            string [] lines = File.ReadAllLines (file);
            if (lines.Length < 2)
                throw new ArgumentException ("file");

            rgbKey = Convert.FromBase64String (lines [0]);
            rgbIV = Convert.FromBase64String (lines [1]);
        }
    }
}
