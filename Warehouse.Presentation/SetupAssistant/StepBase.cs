//
// StepBase.cs
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
using Gtk;
using Mono.Addins;
using Warehouse.Component;

namespace Warehouse.Presentation.SetupAssistant
{
    public enum StepStatus
    {
        Incomplete,
        Waiting,
        InProgress,
        Failed,
        Complete
    }

    public enum AssistType
    {
        ApplicationSetup,
        DatabaseSetup
    }

    [TypeExtensionPoint ("/Warehouse/Presentation/SetupAssistant/Step")]
    public abstract class StepBase : IDisposable
    {
        protected VBox vboBody;
        protected Label lblTitle;
        private StepStatus status = StepStatus.Incomplete;

        public Assistant Assistant { get; set; }
        public abstract string Title { get; }
        public Widget Body
        {
            get
            {
                if (vboBody == null)
                    CreateBody ();

                return vboBody;
            }
        }

        public abstract int Ordinal { get; }
        public virtual bool IsSingleComplete
        {
            get { return false; }
        }

        public StepStatus Status
        {
            get { return status; }
        }

        public abstract AssistType Group { get; }

        public event EventHandler StatusChanged;

        protected abstract void CreateBody ();

        protected void CreateBody (string subtitle)
        {
            lblTitle = new Label
            {
                Markup = new PangoStyle
                {
                    Bold = true,
                    Size = PangoStyle.TextSize.XLarge,
                    Text = subtitle
                }
            };

            vboBody = new VBox ();
            vboBody.PackStart (lblTitle, false, true, 10);
            vboBody.ShowAll ();
        }

        public virtual StepBase GetNextStep (List<StepBase> allSteps)
        {
            return allSteps.Find (s => s.Ordinal > Ordinal);
        }

        public virtual bool Complete (Assistant assistant)
        {
            ChangeStatus (StepStatus.Complete);
            return true;
        }

        public void ChangeStatus (StepStatus newStatus)
        {
            if (status == newStatus)
                return;

            status = newStatus;
            EventHandler handler = StatusChanged;
            if (handler != null)
                handler (this, EventArgs.Empty);
        }

        public static List<StepBase> GetAllSteps (AssistType group)
        {
            List<StepBase> allSteps = new List<StepBase> ();
            foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Warehouse/Presentation/SetupAssistant/Step")) {
                object instance = node.CreateInstance ();
                StepBase step = instance as StepBase;
                if (step != null)
                    allSteps.Add (step);
            }
            allSteps.Sort ((s1, s2) => Math.Max (-1, Math.Min (1, s1.Ordinal - s2.Ordinal)));

            return allSteps.FindAll (s => s.Group == group);
        }

        #region Implementation of IDisposable

        public virtual void Dispose ()
        {
            Assistant = null;
        }

        #endregion
    }
}
