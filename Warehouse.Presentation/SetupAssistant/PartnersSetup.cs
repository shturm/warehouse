//
// PartnersSetup.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   08.16.2011
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
using Mono.Addins;

namespace Warehouse.Presentation.SetupAssistant
{
    [TypeExtensionPoint ("/Warehouse/Presentation/SetupAssistant/PartnersSetup")]
    public abstract class PartnersSetup : IDisposable
    {
        public abstract int Ordinal { get; }
        public abstract string Label { get; }

        public abstract Widget GetPageWidget ();
        public abstract bool Validate ();
        public abstract bool CommitChanges (StepPartners step, Assistant assistant);

        #region Implementation of IDisposable

        public virtual void Dispose ()
        {
        }

        #endregion
    }
}
