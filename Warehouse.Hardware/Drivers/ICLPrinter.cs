//
// ICLPrinter.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   12/10/2006
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
using System.Drawing;
using System.Globalization;
using Warehouse.Business.Devices;

namespace Warehouse.Hardware.Drivers
{
    public abstract class ICLPrinter<TCon> : ICLPrinterBase<TCon> where TCon : DeviceConnector, new ()
    {
        #region Initialization commands

        public override void SetGraphicalLogoBitmap (string bitmapFile)
        {
            if (string.IsNullOrEmpty (bitmapFile))
                throw new HardwareErrorException (new ErrorState (ErrorState.InvalidInputData, HardwareErrorSeverity.Error));

            Bitmap bitmap;
            try {
                bitmap = new Bitmap (bitmapFile);
            } catch (ArgumentException argex) {
                throw new HardwareErrorException (new ErrorState (ErrorState.InvalidInputData, HardwareErrorSeverity.Error), argex);
            } catch (OutOfMemoryException oomex) {
                throw new HardwareErrorException (new ErrorState (ErrorState.InvalidInputData, HardwareErrorSeverity.Error), oomex);
            }

            if (bitmap == null ||
                bitmap.Width > graphicalLogoWidth ||
                bitmap.Height > graphicalLogoHeight)
                throw new HardwareErrorException (new ErrorState (ErrorState.InvalidInputData, HardwareErrorSeverity.Error));

            SetGraphicalLogoBitmap (bitmap);
        }

        protected virtual void SetGraphicalLogoBitmap (Bitmap bitmap)
        {
            for (int y = 0; y < bitmap.Height; y++) {
                List<byte> line = new List<byte> ();
                line.AddRange (defaultEnc.GetBytes (y.ToString (CultureInfo.InvariantCulture)));
                line.AddRange (defaultEnc.GetBytes (","));

                int lineInfoLen = line.Count;

                for (int x = 0; x < ((graphicalLogoWidth - bitmap.Width) / 2) / 4; x++) {
                    line.Add (0x30);
                }

                for (int x = 0; x < bitmap.Width / 4; x++) {
                    byte pack = 0;

                    for (int b = 0; b < 4; b++) {
                        Color pixel = bitmap.GetPixel (x * 4 + b, y);
                        if (pixel.GetBrightness () <= 0.5)
                            pack |= (byte) (1 << (3 - b));

                    }

                    line.AddRange (defaultEnc.GetBytes (pack.ToString ("X")));
                }

                for (int x = line.Count; x < (graphicalLogoWidth / 4) + lineInfoLen; x++) {
                    line.Add (0x30);
                }

                int oldTimeout = Connector.ReadTimeout;
                try {
                    Connector.ReadTimeout = 10000;
                    SendMessage (CommandCodes.SetGraphicalLogoBitmap, line.ToArray ());
                } finally {
                    Connector.ReadTimeout = oldTimeout;
                }
            }
        }

        #endregion
    }
}