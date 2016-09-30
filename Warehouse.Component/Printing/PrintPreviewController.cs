//
// PrintPreviewController.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/04/2007
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

namespace Warehouse.Component.Printing
{
    public class PrintPreviewController : IDisposable
    {
        private readonly GtkFormPrintDocument operation;
        private StatusDialog dialog;

        public PrintPreviewController (GtkFormPrintDocument operation)
        {
            this.operation = operation;
            operation.BeginPrint += operation_BeginPrint;
            operation.DrawPage += operation_DrawPage;
            operation.EndPrint += operation_EndPrint;
        }

        void operation_BeginPrint (object o, BeginPrintArgs args)
        {
            dialog = new StatusDialog (Translator.GetString ("Generating document"), Translator.GetHelper ());
            dialog.Show ();

            if (dialog.Cancelled)
                operation.Cancelled = true;
        }

        void operation_DrawPage (object o, DrawPageArgs args)
        {
            if (dialog != null) {
                dialog.Total = operation.TotalPages;
                dialog.Current = args.PageNr + 1;
            }

            if (dialog != null && dialog.Cancelled)
                operation.Cancelled = true;
        }

        void operation_EndPrint (object o, EndPrintArgs args)
        {
            if (dialog != null)
                dialog.Hide ();
        }

        public void Dispose ()
        {
            operation.BeginPrint -= operation_BeginPrint;
            operation.DrawPage -= operation_DrawPage;
            operation.EndPrint -= operation_EndPrint;
        }
    }
}

