//
// Form.cs
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using Cairo;

namespace Warehouse.Component.Documenting
{
    public class Form : FormDrawableContainer
    {
        #region Private members

        private float pageWidth = -1f;
        private float pageHeight = -1f;
        private string formName = string.Empty;
        private Section pageHeader;
        private Section pageFooter;
        private double headerHeight = -1f;
        private double footerHeight = -1f;
        private MarginsI pageMargins = new MarginsI (int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);
        private bool landscape = false;

        #endregion

        #region Public properties

        public float PageWidth
        {
            get { return pageWidth; }
            set { pageWidth = value; }
        }

        public float PageHeight
        {
            get { return pageHeight; }
            set { pageHeight = value; }
        }

        public string FormName
        {
            get { return formName; }
            set { formName = value; }
        }

        public Section PageHeader
        {
            get { return pageHeader; }
            set
            {
                if (pageHeader == value)
                    return;

                if (pageHeader != null)
                    pageHeader.CalculatedSize.Changed -= HeaderSize_Changed;

                pageHeader = value;
                childObjects.Remove (pageHeader);
                if (pageHeader == null) {
                    OnPropertyChanged ("PageHeader");
                    return;
                }

                pageHeader.CalculatedSize.Changed += HeaderSize_Changed;
                pageHeader.ParentForm = this;
                OnPropertyChanged ("PageHeader");
            }
        }

        public Section PageFooter
        {
            get { return pageFooter; }
            set
            {
                if (pageFooter == value)
                    return;

                if (pageFooter != null)
                    pageFooter.CalculatedSize.Changed -= FooterSize_Changed;

                pageFooter = value;
                childObjects.Remove (pageFooter);
                if (pageFooter == null) {
                    OnPropertyChanged ("PageFooter");
                    return;
                }

                pageFooter.CalculatedSize.Changed += FooterSize_Changed;
                pageFooter.ParentForm = this;
                OnPropertyChanged ("PageFooter");
            }
        }

        public MarginsI PageMargins
        {
            get { return pageMargins; }
            set { pageMargins = value; }
        }

        public bool Landscape
        {
            get { return landscape; }
            set
            {
                if (landscape != value) {
                    landscape = value;
                    OnPropertyChanged ("Landscape");
                }
            }
        }

        public IDrawingProvider DrawingProvider
        {
            set
            {
                DocumentHelper.DrawingProvider = value;
            }
        }

        #endregion

        public Form ()
        {
            SetDefaultStyle ("vfill:true;hfill:true");
            childDistribution = ObjectStyle.ChildDistribution.Vertical;
            calculatedSize.Changed += CalculatedSize_Changed;
            DocumentHelper.TotalPages = -1;
        }

        private void CalculatedSize_Changed (object sender, EventArgs e)
        {
            //QueueAllocate ();
            QueueCalculateDown ();
        }

        public Form (XElement node, object obj)
            : this ()
        {
            if (obj == null)
                throw new ArgumentNullException ("obj");

            FillBoundFields (obj);

            CreateFromTemplate (node, bindableFields);
        }

        #region FormObject Members

        public override void CreateFromTemplate (XElement node, Hashtable boundFields)
        {
            bindableFields = boundFields;

            XAttribute tempAttr = node.Attribute ("formName");
            if (tempAttr != null)
                formName = tempAttr.Value;

            XAttribute attributeLanscape = node.Attribute ("landscape");

            bool value;
            if (attributeLanscape != null && bool.TryParse (attributeLanscape.Value, out value))
                landscape = value;

            base.CreateFromTemplate (node, boundFields);

            bindableFields = boundFields;

            foreach (XElement child in node.Elements ()) {
                switch (child.Name.LocalName.ToLowerInvariant ()) {
                    case "section":
                        Section childObj = DocumentHelper.FormObjectCreator.CreateSection (child, boundFields);
                        switch (childObj.SectionType) {
                            case Section.Appearance.General:
                            case Section.Appearance.PageBreak:
                                childObjects.Add (childObj);
                                break;
                            case Section.Appearance.PageHeader:
                                PageHeader = childObj;
                                break;
                            case Section.Appearance.PageFooter:
                                PageFooter = childObj;
                                break;
                        }
                        break;
                    default:
                        CreateChild (boundFields, childObjects, child);
                        break;
                }
            }
        }

        protected override void OnChildObjectsListChanged (object sender, ListChangedEventArgs e)
        {
            base.OnChildObjectsListChanged (sender, e);
            if (e.ListChangedType == ListChangedType.ItemAdded) {
                if (childObjects [e.NewIndex] == pageHeader)
                    PageHeader = null;
                if (childObjects [e.NewIndex] == pageFooter)
                    PageFooter = null;
            }
        }

        private void HeaderSize_Changed (object sender, EventArgs e)
        {
            pageHeader.QueueAllocate ();
        }

        private void FooterSize_Changed (object sender, EventArgs e)
        {
            pageFooter.QueueAllocate ();
        }

        #endregion

        public override void QueueAllocateDown ()
        {
            base.QueueAllocateDown ();

            if (pageHeader != null)
                pageHeader.QueueAllocateDown ();

            if (pageFooter != null)
                pageFooter.QueueAllocateDown ();
        }

        public override void QueueCalculateDown (bool annulCalculatedSize = false)
        {
            base.QueueCalculateDown (annulCalculatedSize);

            if (pageHeader != null)
                pageHeader.QueueCalculateDown (annulCalculatedSize);

            if (pageFooter != null)
                pageFooter.QueueCalculateDown (annulCalculatedSize);
        }

        public override void QueueRedistributeDown ()
        {
            DocumentHelper.TotalPages = -1;

            base.QueueRedistributeDown ();

            if (pageHeader != null)
                pageHeader.QueueRedistributeDown ();

            if (pageFooter != null)
                pageFooter.QueueRedistributeDown ();
        }

        private void AllocateFormSize (int currentPage)
        {
            PageSettings page = new PageSettings { Width = PageWidth, Height = PageHeight };
            DocumentHelper.CurrentPageSettings = page;

            if (currentPage > 0)
                DocumentHelper.CurrentPage = currentPage;

            double pHeight = PageHeight;
            if (pageHeader != null) {
                if (DocumentHelper.ReallocatePageDependent && pageHeader.IsPageDependant) {
                    pageHeader.QueueCalculateDown (true);
                    pageHeader.QueueAllocateDown ();
                }

                headerHeight = pageHeader.GetSize (new PointD ()).Height;
                pHeight -= headerHeight;
            } else
                headerHeight = 0f;

            if (pageFooter != null) {
                if (DocumentHelper.ReallocatePageDependent && pageFooter.IsPageDependant) {
                    pageFooter.QueueCalculateDown (true);
                    pageFooter.QueueAllocateDown ();
                }

                footerHeight = pageFooter.GetSize (new PointD ()).Height;
                pHeight -= footerHeight;
            } else
                footerHeight = 0f;

            DocumentHelper.CurrentPageSettings.Height = pHeight;

            if (DocumentHelper.ReallocatePageDependent && IsPageDependant) {
                QueueCalculateDown (true);
                QueueAllocateDown ();
            }

            AllocateSize (new PointD ());

            if (DocumentHelper.TotalPages <= 0) {
                DocumentHelper.TotalPages = distribution.GetPagesUsed ();

                AllocateFormSize (currentPage);
            }

            DocumentHelper.ReallocatePageDependent = false;
        }

        public int GetTotalPages ()
        {
            if (pageWidth < 1 || pageHeight < 1)
                return 0;

            DistributeForm ();

            return distribution.GetPagesUsed ();
        }

        public int GetPageColumns ()
        {
            if (pageWidth < 1 || pageHeight < 1)
                return 0;

            DistributeForm ();

            return distribution.GetColumnsUsed ();
        }

        public int GetPageRows ()
        {
            if (pageWidth < 1 || pageHeight < 1)
                return 0;

            DistributeForm ();

            return distribution.GetRowsUsed ();
        }

        private void DistributeForm ()
        {
            AllocateFormSize (-1);
            int pagesUsed = distribution.GetPagesUsed ();

            do {
                calculatedSize.Width = DocumentHelper.CurrentPageSettings.Width * distribution.GetColumnsUsed ();
                calculatedSize.Height = DocumentHelper.CurrentPageSettings.Height * distribution.GetRowsUsed ();
                CalculateChildSizes (new PointD ());

                int newPagesUsed = distribution.GetPagesUsed ();
                // Calculation may cause the number of pages to change as some content may wrap and use more space
                if (pagesUsed >= newPagesUsed)
                    return;

                QueueCalculateDown ();
                DocumentHelper.TotalPages = newPagesUsed;
                pagesUsed = newPagesUsed;
            } while (true);
        }

        public void Draw (int pageNumber, PointD start)
        {
            AllocateFormSize (pageNumber + 1);

            PageSettings contentSettings = (PageSettings) DocumentHelper.CurrentPageSettings.Clone ();
            contentSettings.SetPageNumber (distribution, pageNumber);
            contentSettings.SetDrawStart (new PointD (start.X, start.Y + headerHeight));

            PageSettings pageSettings = DocumentHelper.CurrentPageSettings;
            pageSettings.Height = PageHeight;
            pageSettings.SetPageNumber (1, 0);
            pageSettings.SetDrawStart (start);

            if (pageHeader != null) {
                pageHeader.QueueAllocate ();
                pageHeader.CalculatedSize.Changed -= HeaderSize_Changed;
                pageHeader.CalculatedSize.Width = pageSettings.Width;
                pageHeader.CalculatedSize.Height = headerHeight;
                pageHeader.CalculatedSize.Changed += HeaderSize_Changed;
                
                pageHeader.Draw (new PointD (), pageNumber);
            }

            if (pageFooter != null) {
                pageFooter.QueueAllocate ();
                pageFooter.CalculatedSize.Changed -= FooterSize_Changed;
                pageFooter.CalculatedSize.Width = pageSettings.Width;
                pageFooter.CalculatedSize.Height = footerHeight;
                pageFooter.CalculatedSize.Changed += FooterSize_Changed;
                
                pageSettings.SetDrawStart (new PointD (start.X, start.Y + PageHeight - footerHeight));
                pageFooter.Draw (new PointD (), pageNumber);
            }

            DocumentHelper.DrawingProvider.SaveClip ();

            RectangleD pageClip = contentSettings.GetPageRectangle ();
            pageClip.X = start.X;
            pageClip.Y = start.Y + headerHeight;
            DocumentHelper.DrawingProvider.IntersectClip (pageClip);

            DocumentHelper.CurrentPageSettings = contentSettings;
            Draw (new PointD (), pageNumber);

            DocumentHelper.DrawingProvider.RestoreClip ();
        }

        #region FormDrawableObject Members

        public override void Draw (PointD start, int pageNumber)
        {
            calculatedSize.Width = DocumentHelper.CurrentPageSettings.Width * distribution.GetColumnsUsed ();
            calculatedSize.Height = DocumentHelper.CurrentPageSettings.Height * distribution.GetRowsUsed ();

            base.Draw (start, pageNumber);
        }

        public override SizeD GetSize (PointD start)
        {
            calculatedSize.Width = DocumentHelper.CurrentPageSettings.Width * distribution.GetColumnsUsed ();
            calculatedSize.Height = DocumentHelper.CurrentPageSettings.Height * distribution.GetRowsUsed ();

            return base.GetSize (start);
        }

        protected override IEnumerable<Type> GetAllowedParentTypes ()
        {
            return new Type [0];
        }

        #endregion

        public override XElement GetXmlElement ()
        {
            XElement element = base.GetXmlElement ();
            if (pageHeader != null)
                element.Elements ().First ().AddBeforeSelf (pageHeader.GetXmlElement ());
            if (pageFooter != null)
                element.Add (pageFooter.GetXmlElement ());
            return element;
        }

        protected override string XmlElementName
        {
            get { return "Form"; }
        }

        protected override void AddAttributes (XElement xmlElement)
        {
            if (!string.IsNullOrEmpty (formName))
                AddAttribute ("formName", formName, xmlElement);
            if (Landscape)
                AddAttribute ("landscape", true, xmlElement);
            base.AddAttributes (xmlElement);
        }

        #region ICloneable Members

        public override object Clone ()
        {
            Form ret = Clone (DocumentHelper.FormObjectCreator.CreateForm ());
            if (pageHeader != null)
                ret.pageHeader = (Section) pageHeader.Clone ();
            if (pageFooter != null)
                ret.pageFooter = (Section) pageFooter.Clone ();
            ret.formName = formName;
            ret.landscape = landscape;

            return ret;
        }

        #endregion
    }
}
