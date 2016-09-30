//
// Assistant.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   06/24/2006
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
using System.Threading;
using Glade;
using Gtk;
using Warehouse.Component;
using Warehouse.Business;
using Warehouse.Presentation.Dialogs;

namespace Warehouse.Presentation.SetupAssistant
{
    public class Assistant : DialogBase
    {
        private const int OPTIMAL_DIALOG_WIDTH = 550;
        private const int OPTIMAL_DIALOG_HEIGHT = 350;
        private const int MAX_VISIBLE_TOTAL = 5;
        private const int MAX_VISIBLE_NEXT = 1;

        private readonly List<StepTag> tags = new List<StepTag> ();
        private readonly List<StepBase> steps = new List<StepBase> ();
        private readonly AssistType assistType;
        private readonly List<StepBase> allSteps;
        private readonly Thread backgroundWorker;
        private readonly Queue<BackgroundJob> backgroundJobs = new Queue<BackgroundJob> ();
        private bool stopBackgroundWorker;
        private StepBase currentStep;

        #region Glade Widgets

#pragma warning disable 649

        [Widget]
        private Dialog dlgAssistant;

        [Widget]
        private HBox hboSteps;
        [Widget]
        private Label lblSteps;
        [Widget]
        private Alignment algContent;

        [Widget]
        private Button btnBack;
        [Widget]
        private Button btnNext;
        [Widget]
        private Button btnFinish;

#pragma warning restore 649

        #endregion

        public event EventHandler StatusChanged;

        public IList<StepBase> Steps
        {
            get { return steps; }
        }

        public override Dialog DialogControl
        {
            get { return dlgAssistant; }
        }

        protected override bool SaveDialogSettings
        {
            get { return false; }
        }

        public Assistant (AssistType assistType)
        {
            this.assistType = assistType;
            allSteps = StepBase.GetAllSteps (assistType);

            Initialize ();

            if (assistType == AssistType.ApplicationSetup)
                AppendStep (new StepAppSetup ());
            else if (assistType == AssistType.DatabaseSetup)
                AppendStep (new StepDBSetup ());
            else
                throw new ArgumentOutOfRangeException ("assistType");


            backgroundWorker = new Thread (delegate ()
                {
                    while (true) {
                        if (backgroundJobs.Count > 0) {
                            BackgroundJob job = backgroundJobs.Dequeue ();
                            try {
                                PresentationDomain.Invoke (() => job.Step.ChangeStatus (StepStatus.InProgress));
                                job.Action ();
                                PresentationDomain.Invoke (() => job.Step.ChangeStatus (StepStatus.Complete));
                            } catch (Exception ex) {
                                PresentationDomain.Invoke (() => job.Step.ChangeStatus (StepStatus.Failed));
                                ErrorHandling.LogException (ex, ErrorSeverity.FatalError);
                            }
                        } else
                            Thread.Sleep (100);

                        if (stopBackgroundWorker)
                            return;
                    }
                }) { IsBackground = true, Name = "Setup Assistant Worker" };
            backgroundWorker.Start ();
        }

        public Assistant (StepBase step)
        {
            assistType = AssistType.ApplicationSetup;
            allSteps = new List<StepBase> { step };

            Initialize ();

            AppendStep (step);

            backgroundWorker = new Thread (delegate ()
            {
                while (true) {
                    if (backgroundJobs.Count > 0) {
                        BackgroundJob job = backgroundJobs.Dequeue ();
                        try {
                            PresentationDomain.Invoke (() => job.Step.ChangeStatus (StepStatus.InProgress));
                            job.Action ();
                            PresentationDomain.Invoke (() => job.Step.ChangeStatus (StepStatus.Complete));
                        } catch (Exception ex) {
                            PresentationDomain.Invoke (() => job.Step.ChangeStatus (StepStatus.Failed));
                            ErrorHandling.LogException (ex, ErrorSeverity.FatalError);
                        }
                    } else
                        Thread.Sleep (100);

                    if (stopBackgroundWorker)
                        return;
                }
            }) { IsBackground = true, Name = "Setup Assistant Worker" };
            backgroundWorker.Start ();
        }

        protected override void InitializeForm ()
        {
            XML form = FormHelper.LoadGladeXML ("SetupAssistant.Assistant.glade", "dlgAssistant");
            form.Autoconnect (this);

            dlgAssistant.Icon = FormHelper.LoadImage ("Icons.AppMain32.png").Pixbuf;

            btnBack.SetChildImage (FormHelper.LoadImage ("Icons.Left24.png"));
            btnNext.SetChildImage (FormHelper.LoadImage ("Icons.Right24.png"));
            btnFinish.SetChildImage (FormHelper.LoadImage ("Icons.Ok24.png"));
            btnBack.Clicked += btnBack_Clicked;
            btnNext.Clicked += btnNext_Clicked;
            btnFinish.Clicked += btnFinish_Clicked;

            base.InitializeForm ();

            InitializeFormStrings ();
        }

        protected override void InitializeFormStrings ()
        {
            base.InitializeFormStrings ();

            dlgAssistant.Title = assistType == AssistType.ApplicationSetup ? Translator.GetString ("Setup Assistant") : Translator.GetString ("Database Assistant");
            btnBack.SetChildLabelText (Translator.GetString ("Back"));
            btnNext.SetChildLabelText (Translator.GetString ("Next"));
            btnFinish.SetChildLabelText (Translator.GetString ("Finish"));
        }

        private void AppendStep (StepBase step)
        {
            int curIndex = steps.IndexOf (currentStep);
            for (int i = steps.Count - 1; i > curIndex; i--) {
                StepTag t = tags [curIndex];
                hboSteps.Remove (t);
                tags.RemoveAt (i);
                StepBase s = steps [i];
                s.StatusChanged -= step_StatusChanged;
                steps.RemoveAt (i);
            }

            steps.Add (step);
            step.Assistant = this;
            step.StatusChanged += step_StatusChanged;

            StepTag tag = new StepTag (step);
            tag.Show ();
            tag.Toggled += Tag_Toggled;
            hboSteps.PackStart (tag, false, true, 0);
            tags.Add (tag);

            SetActiveStep (step);
        }

        private void step_StatusChanged (object sender, EventArgs e)
        {
            RefreshForwardButtonsSensitivity ();

            EventHandler handler = StatusChanged;
            if (handler != null)
                handler (sender, e);
        }

        private void SetActiveStep (StepBase step)
        {
            if (algContent.Child != null)
                algContent.Remove (algContent.Child);

            algContent.Add (step.Body);
            currentStep = step;

            int newIndex = steps.IndexOf (step);
            int totalVisible = 0;
            for (int i = tags.Count - 1; i >= 0; i--) {
                StepTag tag = tags [i];
                bool visible = i <= newIndex + MAX_VISIBLE_NEXT;
                if (visible)
                    totalVisible++;

                tag.Visible = visible && totalVisible <= MAX_VISIBLE_TOTAL;

                if (ReferenceEquals (tag.Step, step))
                    tag.Active = true;
            }

            for (int i = newIndex + MAX_VISIBLE_NEXT + 1; i < tags.Count && totalVisible < MAX_VISIBLE_TOTAL; i++) {
                tags [i].Visible = true;
                totalVisible++;
            }

            btnBack.Visible = newIndex != 0;
            lblSteps.SetText (string.Format ("{0}/{1}", newIndex + 1, allSteps.Count));
            dlgAssistant.Resize (OPTIMAL_DIALOG_WIDTH, OPTIMAL_DIALOG_HEIGHT);
            RefreshForwardButtonsSensitivity ();
        }

        private void RefreshForwardButtonsSensitivity ()
        {
            StepBase next = currentStep.GetNextStep (allSteps);
            btnNext.Sensitive = next != null;

            foreach (StepBase step in steps) {
                switch (step.Status) {
                    case StepStatus.Waiting:
                    case StepStatus.InProgress:
                    case StepStatus.Failed:
                        btnFinish.Sensitive = false;
                        return;
                }
            }

            btnFinish.Sensitive = true;
        }

        private bool handlingTagToggle;
        private void Tag_Toggled (object sender, EventArgs e)
        {
            if (handlingTagToggle)
                return;

            StepTag tag = (StepTag) sender;
            try {
                handlingTagToggle = true;
                if (tag.Active) {
                    foreach (StepTag stepTag in tags) {
                        if (!ReferenceEquals (stepTag, tag))
                            stepTag.Active = false;
                    }
                    SetActiveStep (tag.Step);
                } else
                    tag.Active = true;
            } finally {
                handlingTagToggle = false;
            }
        }

        private void Finish ()
        {
            dlgAssistant.Respond (ResponseType.Ok);
        }

        public void EnqueueBackgroundJob (BackgroundJob job)
        {
            PresentationDomain.Invoke (() => job.Step.ChangeStatus (StepStatus.Waiting), false, true);
            backgroundJobs.Enqueue (job);
        }

        #region Event handling

        private void btnBack_Clicked (object sender, EventArgs e)
        {
            int curIndex = steps.IndexOf (currentStep);
            SetActiveStep (steps [curIndex - 1]);
        }

        private void btnNext_Clicked (object sender, EventArgs e)
        {
            if ((!currentStep.IsSingleComplete || currentStep.Status != StepStatus.Complete) && !currentStep.Complete (this))
                return;

            StepBase next = currentStep.GetNextStep (allSteps);
            if (next != null) {
                int curIndex = steps.IndexOf (currentStep);
                StepBase availNext = null;
                if (curIndex >= 0 && curIndex < steps.Count - 1)
                    availNext = steps [curIndex + 1];

                if (ReferenceEquals (availNext, next))
                    SetActiveStep (next);
                else
                    AppendStep (next);
            } else
                Finish ();
        }

        private void btnFinish_Clicked (object sender, EventArgs e)
        {
            if (btnNext.Sensitive)
                Finish ();
            else
                btnNext_Clicked (sender, e);
        }

        #endregion

        public override void Dispose ()
        {
            stopBackgroundWorker = true;
            base.Dispose ();

            for (int i = hboSteps.Children.Length - 1; i >= 0; i--)
                hboSteps.Remove (hboSteps.Children [i]);

            if (algContent.Child != null)
                algContent.Remove (algContent.Child);

            foreach (StepBase step in allSteps)
                step.Dispose ();

            backgroundWorker.Join ();
        }
    }
}
