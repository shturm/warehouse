//
// DriverBase.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   11/28/2007
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
using System.Text;
using Warehouse.Data;

namespace Warehouse.Business.Devices
{
    public abstract class DriverBase : IConnectableDevice
    {
        public const int DOS_MIK_CODE_PAGE = 866;
        public const int DOS_RUS_CODE_PAGE = 866111;
        public const int WINDOWS_CYR_CODE_PAGE = 1251;
        public const int WINDOWS_CEE_CODE_PAGE = 1250;
        public const int CHINESE_SIMPLIFIED_CODE_PAGE = 936;

        public const string USES_SERIAL_PORT = "USES_SERIAL_PORT";
        public const string USES_BLUETOOTH = "USES_BLUETOOTH";
        public const string USES_BAUD_RATE = "USES_BAUD_RATE";
        public const string DEFAULT_BAUD_RATE = "DEFAULT_BAUD_RATE";
        public const string USES_SERIAL_HANDSHAKING = "USES_SERIAL_HANDSHAKING";
        public const string DEFAULT_SERIAL_HANDSHAKING = "DEFAULT_SERIAL_HANDSHAKING";
        public const string USES_NETWORK_ADDRESS = "USES_NETWORK_ADDRESS";
        public const string DEFAULT_NETWORK_ADDRESS = "DEFAULT_NETWORK_ADDRESS";
        public const string USES_NETWORK_PORT = "USES_NETWORK_PORT";
        public const string DEFAULT_NETWORK_PORT = "DEFAULT_NETWORK_PORT";
        public const string USES_OPERATOR_CODE = "USES_OPERATOR_CODE";
        public const string DEFAULT_OPERATOR_CODE = "DEFAULT_OPERATOR_CODE";
        public const string USES_LOGICAL_ADDRESS = "USES_LOGICAL_ADDRESS";
        public const string DEFAULT_LOGICAL_ADDRESS = "DEFAULT_LOGICAL_ADDRESS";
        public const string USES_PASSWORD = "USES_PASSWORD";
        public const string DEFAULT_PASSWORD = "DEFAULT_PASSWORD";
        public const string USES_ADMIN_PASSWORD = "USES_ADMIN_PASSWORD";
        public const string DEFAULT_ADMIN_PASSWORD = "DEFAULT_ADMIN_PASSWORD";
        public const string USES_ENCODING = "USES_ENCODING";
        public const string DEFAULT_ENCODING = "DEFAULT_ENCODING";
        public const string USES_CHARS_IN_LINE = "USES_CHARS_IN_LINE";
        public const string DEFAULT_CHARS_IN_LINE = "DEFAULT_CHARS_IN_LINE";
        public const string USES_BLANK_LINES_BEFORE_CUT = "USES_BLANK_LINES_BEFORE_CUT";
        public const string DEFAULT_BLANK_LINES_BEFORE_CUT = "DEFAULT_BLANK_LINES_BEFORE_CUT";
        public const string USES_DOCUMENT_PRINTER = "USES_DOCUMENT_PRINTER";
        public const string USES_USE_CUSTOM_LINE_WIDTH = "USES_USE_CUSTOM_LINE_WIDTH";
        public const string DEFAULT_USE_CUSTOM_LINE_WIDTH = "DEFAULT_USE_CUSTOM_LINE_WIDTH";
        public const string USES_CUSTOM_LINE_WIDTH = "USES_CUSTOM_LINE_WIDTH";
        public const string USES_CUSTOM_LINE_CHARS_WIDTH = "USES_CUSTOM_LINE_CHARS_WIDTH";
        public const string DEFAULT_CUSTOM_LINE_WIDTH = "DEFAULT_CUSTOM_LINE_WIDTH";
        public const string DEFAULT_MIN_CUSTOM_LINE_WIDTH = "DEFAULT_MIN_CUSTOM_LINE_WIDTH";
        public const string USES_OUTPUT_FILE = "USES_OUTPUT_FILE";
        public const string DEFAULT_OUTPUT_FOLDER = "DEFAULT_OUTPUT_FOLDER";
        public const string USES_DRAWER_COMMAND = "USES_DRAWER_COMMAND";
        public const string DEFAULT_DRAWER_COMMAND = "DEFAULT_DRAWER_COMMAND";
        public const string USES_HEADER_AND_FOOTER = "USES_HEADER_AND_FOOTER";
        public const string USES_RECEIPT_DOCUMENT_TEMPLATES = "USES_RECEIPT_DOCUMENT_TEMPLATES";
        public const string CUSTOM_PORT = "CUSTOM_PORT";
        public const string USES_FRONT_OFFICE_LICENSE = "USES_FRONT_OFFICE_LICENSE";

        public const string DEVICE_TEST_ON_CONNECT = "DEVICE_TEST_ON_CONNECT";
        public const string USES_CUSTOM_VAT_RATE = "USES_CUSTOM_VAT_RATE";
        public const string HAS_FISCAL_MEMORY = "HAS_FISCAL_MEMORY";
        public const string SEPARATOR = "--";
        public const string SEPARATOR_PATTERN = "- ";

        public enum TextAlign
        {
            Left,
            Center,
            Right
        }

        protected enum CustomEncoding
        {
            None,
            ArmenianDaisy,
            Azerbaijani,
            GeorgianDaisy,
            GeorgianPhonetic,
            GeorgianMercury,
            MIK,
        }

        private static readonly Dictionary<char, byte> armenianDaisyAlphabet = new Dictionary<char, byte>
            {
                {'Ա', 0xb2}, {'ա', 0xb3},
                {'Բ', 0xb4}, {'բ', 0xb5},
                {'Գ', 0xb6}, {'գ', 0xb7},
                {'Դ', 0xb8}, {'դ', 0xb9},
                {'Ե', 0xba}, {'ե', 0xbb},
                {'Զ', 0xbc}, {'զ', 0xbd},
                {'Է', 0xbe}, {'է', 0xbf},
                {'Ը', 0xc0}, {'ը', 0xc1},
                {'Թ', 0xc2}, {'թ', 0xc3},
                {'Ժ', 0xc4}, {'ժ', 0xc5},
                {'Ի', 0xc6}, {'ի', 0xc7},
                {'Լ', 0xc8}, {'լ', 0xc9},
                {'Խ', 0xca}, {'խ', 0xcb},
                {'Ծ', 0xcc}, {'ծ', 0xcd},
                {'Կ', 0xce}, {'կ', 0xcf},
                {'Հ', 0xd0}, {'հ', 0xd1},
                {'Ձ', 0xd2}, {'ձ', 0xd3},
                {'Ղ', 0xd4}, {'ղ', 0xd5},
                {'Ճ', 0xd6}, {'ճ', 0xd7},
                {'Մ', 0xd8}, {'մ', 0xd9},
                {'Յ', 0xda}, {'յ', 0xdb},
                {'Ն', 0xdc}, {'ն', 0xdd},
                {'Շ', 0xde}, {'շ', 0xdf},
                {'Ո', 0xe0}, {'ո', 0xe1},
                {'Չ', 0xe2}, {'չ', 0xe3},
                {'Պ', 0xe4}, {'պ', 0xe5},
                {'Ջ', 0xe6}, {'ջ', 0xe7},
                {'Ռ', 0xe8}, {'ռ', 0xe9},
                {'Ս', 0xea}, {'ս', 0xeb},
                {'Վ', 0xec}, {'վ', 0xed},
                {'Տ', 0xee}, {'տ', 0xef},
                {'Ր', 0xf0}, {'ր', 0xf1},
                {'Ց', 0xf2}, {'ց', 0xf3},
                {'Ւ', 0xf4}, {'ւ', 0xf5},
                {'Փ', 0xf6}, {'փ', 0xf7},
                {'Ք', 0xf8}, {'ք', 0xf9},
                {'Օ', 0xfa}, {'օ', 0xfb},
                {'Ֆ', 0xfc}, {'ֆ', 0xfd}
            };

        private static readonly Dictionary<char, byte> georgianDaisyAlphabet = new Dictionary<char, byte>
            {
                {'ა', 0xd0},
                {'ბ', 0xd1},
                {'გ', 0xd2},
                {'დ', 0xd3},
                {'ე', 0xd4},
                {'ვ', 0xd5},
                {'ზ', 0xd6},
                {'თ', 0xd7},
                {'ი', 0xd8},
                {'კ', 0xd9},
                {'ლ', 0xda},
                {'მ', 0xdb},
                {'ნ', 0xdc},
                {'ო', 0xdd},
                {'პ', 0xde},
                {'ჟ', 0xdf},
                {'რ', 0xe0},
                {'ს', 0xe1},
                {'ტ', 0xe2},
                {'უ', 0xe3},
                {'ფ', 0xe4},
                {'ქ', 0xe5},
                {'ღ', 0xe6},
                {'ყ', 0xe7},
                {'შ', 0xe8},
                {'ჩ', 0xe9},
                {'ც', 0xea},
                {'ძ', 0xeb},
                {'წ', 0xec},
                {'ჭ', 0xed},
                {'ხ', 0xee},
                {'ჯ', 0xef},
                {'ჰ', 0xf0}
            };

        private static readonly Dictionary<char, byte> georgianMercuryAlphabet = new Dictionary<char, byte>
            {
                {'ა', 192},
                {'ბ', 193},
                {'გ', 194},
                {'დ', 195},
                {'ე', 196},
                {'ვ', 197},
                {'ზ', 198},
                {'თ', 199},
                {'ი', 200},
                {'კ', 201},
                {'ლ', 202},
                {'მ', 203},
                {'ნ', 204},
                {'ო', 205},
                {'პ', 206},
                {'ჟ', 207},
                {'რ', 208},
                {'ს', 209},
                {'ტ', 210},
                {'უ', 211},
                {'ფ', 212},
                {'ქ', 213},
                {'ღ', 214},
                {'ყ', 215},
                {'შ', 216},
                {'ჩ', 217},
                {'ც', 218},
                {'ძ', 219},
                {'წ', 220},
                {'ჭ', 221},
                {'ხ', 222},
                {'ჯ', 223},
                {'ჰ', 168}
            };

        private static readonly Dictionary<char, byte> georgianPhoneticAlphabet = new Dictionary<char, byte>
            {
                {'ა', 97},
                {'ბ', 98},
                {'გ', 103},
                {'დ', 100},
                {'ე', 101},
                {'ვ', 118},
                {'ზ', 122},
                {'თ', 84},
                {'ი', 105},
                {'კ', 107},
                {'ლ', 108},
                {'მ', 109},
                {'ნ', 110},
                {'ო', 111},
                {'პ', 112},
                {'ჟ', 74},
                {'რ', 114},
                {'ს', 115},
                {'ტ', 116},
                {'უ', 117},
                {'ფ', 102},
                {'ქ', 113},
                {'ღ', 82},
                {'ყ', 121},
                {'შ', 83},
                {'ჩ', 67},
                {'ც', 99},
                {'ძ', 90},
                {'წ', 119},
                {'ჭ', 87},
                {'ხ', 120},
                {'ჯ', 106},
                {'ჰ', 104}
            };

        private static readonly Dictionary<char, byte> mikAlphabet = new Dictionary<char, byte>
            {
                {'р', 176},
                {'с', 177},
                {'т', 178},
                {'у', 179},
                {'ф', 180},
                {'х', 181},
                {'ц', 182},
                {'ч', 183},
                {'ш', 184},
                {'щ', 185},
                {'ъ', 186},
                {'ы', 187},
                {'ь', 188},
                {'э', 189},
                {'ю', 190},
                {'я', 191}
            };

        private static readonly Dictionary<char, byte> azerbaijaniAlphabet = new Dictionary<char, byte>
            {
                {'Ü', 150},
                {'Ç', 151},
                {'Ş', 152},
                {'İ', 155},
                {'Ğ', 156},
                {'Ö', 158},
                {'Ə', 159},

                {'ü', 230},
                {'ç', 231},
                {'ş', 232},
                {'ı', 235},
                {'ğ', 236},
                {'ö', 238},
                {'ə', 239}
            };

        protected Encoding defaultEnc;
        protected CustomEncoding customEnc;
        protected readonly object syncRoot = new object ();
        protected readonly ErrorState lastErrorState = new ErrorState ();
        protected int referenceCount = 0;

        protected byte [] GetTextBytes (string text)
        {
            Dictionary<char, byte> customEncDict;

            switch (customEnc) {
                case CustomEncoding.ArmenianDaisy:
                    customEncDict = armenianDaisyAlphabet;
                    break;
                case CustomEncoding.GeorgianDaisy:
                    customEncDict = georgianDaisyAlphabet;
                    break;
                case CustomEncoding.GeorgianPhonetic:
                    customEncDict = georgianPhoneticAlphabet;
                    break;
                case CustomEncoding.GeorgianMercury:
                    customEncDict = georgianMercuryAlphabet;
                    break;
                case CustomEncoding.Azerbaijani:
                    customEncDict = azerbaijaniAlphabet;
                    break;
                case CustomEncoding.MIK:
                    customEncDict = mikAlphabet;
                    break;
                default:
                    customEncDict = null;
                    break;
            }

            if (customEncDict != null) {
                List<byte> ret = new List<byte> ();
                foreach (char c in text) {
                    byte val;
                    if (customEncDict.TryGetValue (c, out val))
                        ret.Add (val);
                    else
                        ret.AddRange (defaultEnc.GetBytes (c.ToString ()));
                }

                return ret.ToArray ();
            }

            return defaultEnc.GetBytes (text);
        }

        protected void SetEncoding (ConnectParametersCollection parameters)
        {
            SetEncoding ((int) parameters [ConnectParameters.Encoding]);
        }

        protected void SetEncoding (int encoding)
        {
            switch (encoding) {
                case DOS_RUS_CODE_PAGE:
                    encoding = DOS_MIK_CODE_PAGE;
                    customEnc = CustomEncoding.None;
                    break;
                case DOS_MIK_CODE_PAGE:
                    customEnc = CustomEncoding.MIK;
                    break;
            }

            defaultEnc = Encoding.GetEncoding (encoding);
        }

        public static string GetAlignedString (string text, int charsPerLine, TextAlign align)
        {
            if (text == null)
                return string.Empty;

            if (text == SEPARATOR) {
                return GetSeparatorLine (charsPerLine);
            }

            if (text.Length > charsPerLine)
                return text.Substring (0, charsPerLine);

            switch (align) {
                case TextAlign.Center:
                    return text.PadLeft (((charsPerLine - text.Length) / 2) + text.Length);
                case TextAlign.Right:
                    return text.PadLeft (charsPerLine);
                default:
                    return text;
            }
        }

        protected static string GetSeparatorLine (int charsPerLine)
        {
            StringBuilder sb = new StringBuilder ();
            while (sb.Length < charsPerLine) {
                sb.Append (SEPARATOR_PATTERN);
            }

            return sb.ToString (0, charsPerLine);
        }

        public abstract void Dispose ();

        public abstract bool IsConnected { get; }

        public int ReferenceCount
        {
            get { return referenceCount; }
            set { referenceCount = value; }
        }

        public virtual ErrorState LastErrorState
        {
            get { return lastErrorState; }
        }

        public virtual List<DeviceCommands> SupportedCommands
        {
            get { return new List<DeviceCommands> (); }
        }

        public virtual SerializableDictionary<string, object> GetAttributes ()
        {
            throw new NotImplementedException ();
        }

        public virtual DeviceInfo [] GetSupportedDevices ()
        {
            throw new NotImplementedException ();
        }

        public virtual void Initialize ()
        {
        }

        public virtual void Connect (ConnectParametersCollection parameters)
        {
        }

        public virtual void Disconnect ()
        {
            throw new NotImplementedException ();
        }

        public virtual void GetStatus ()
        {
            throw new NotSupportedException ();
        }

        public virtual void Ping ()
        {
            GetStatus ();
        }
    }

    public abstract class DriverBase<TCon> : DriverBase where TCon : DeviceConnector, new ()
    {
        #region Private fields

        private TCon connector;

        #endregion

        protected DriverBase ()
        {
            connector = new TCon ();
        }

        public override void Dispose ()
        {
            if (connector == null)
                return;

            if (connector.IsConnected)
                connector.Disconnect ();

            connector.Dispose ();
            connector = null;
        }

        #region IConnectableDevice Members

        #region Public properties

        public override bool IsConnected
        {
            get { return connector != null && connector.IsConnected; }
        }

        public virtual TCon Connector
        {
            get { return connector; }
        }

        #endregion

        #endregion
    }
}