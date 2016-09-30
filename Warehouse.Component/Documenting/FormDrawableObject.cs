//
// FormDrawableObject.cs
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
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Warehouse.Data;

namespace Warehouse.Component.Documenting
{
    public abstract class FormDrawableObject : FormObject
    {
        #region Private fields

        protected static bool reAllocating;
        protected readonly PageDistribution distribution = new PageDistribution ();
        protected readonly ObjectSize allocatedSize = new ObjectSize ();
        protected readonly ObjectSize calculatedSize = new ObjectSize ();
        private readonly ObjectSize minimalSize = new ObjectSize ();
        protected double allocatedHPageFill;
        protected double allocatedVPageFill;
        private int parentPosition;
        protected bool isPageDependant;
        private PointD drawLocation;
        protected SizeD visSize;

        #endregion

        #region Public properties

        public PageDistribution Distribution
        {
            get { return distribution; }
        }

        public ObjectSize MinimalSize
        {
            get { return minimalSize; }
        }

        public ObjectSize AllocatedSize
        {
            get { return allocatedSize; }
        }

        public ObjectSize CalculatedSize
        {
            get { return calculatedSize; }
        }

        public double AllocatedHPageFill
        {
            get { return allocatedHPageFill; }
        }

        public double AllocatedVPageFill
        {
            get { return allocatedVPageFill; }
            set { allocatedVPageFill = value; }
        }

        public double CalculatedHPageFill { get; set; }

        public double CalculatedVPageFill { get; set; }

        public int ParentPosition
        {
            get { return parentPosition; }
            set { parentPosition = value; }
        }

        protected virtual bool IsAllocated
        {
            get
            {
                return (allocatedSize.Width.HasValue && allocatedSize.Height.HasValue);
            }
            set
            {
                allocatedSize.Width = value ? 0f : (float?) null;
                allocatedSize.Height = value ? 0f : (float?) null;
            }
        }

        public bool IsPageDependant
        {
            get { return isPageDependant; }
        }

        public PointD DrawLocation
        {
            get { return drawLocation; }
        }

        public virtual SizeD ActualSize
        {
            get
            {
                return calculatedSize.StaticSize;
            }
        }

        protected virtual bool AllowVerticalBreak
        {
            get { return true; }
        }

        protected virtual bool AllowHorizontalBreak
        {
            get { return true; }
        }

        #endregion

        public virtual void Draw (PointD start, int pageNumber)
        {
            if (start.X >= 0 && start.Y >= 0 && pageNumber == distribution.GetFirstPage (DocumentHelper.CurrentPageSettings.Columns))
                drawLocation = DocumentHelper.CurrentPageSettings.GetInPageDrawLocation (start);
        }

        public virtual SizeD GetSize (PointD start)
        {
            SizeD contentsSize = AllocateSize (start);

            contentsSize.Width = Math.Max (contentsSize.Width, calculatedSize.WidthValue);
            contentsSize.Height = Math.Max (contentsSize.Height, calculatedSize.HeightValue);

            return contentsSize;
        }

        protected virtual SizeD GetContentsSize (PointD start)
        {
            PointD startOffset = GetContentStart ();
            PointD endOffset = GetContentEnd ();

            SizeD contentsSize = AllocateSize (start);
            contentsSize.Width -= startOffset.X + endOffset.X;
            contentsSize.Height -= startOffset.Y + endOffset.Y;

            return contentsSize;
        }

        public virtual SizeD AllocateSize (PointD start)
        {
            if (!IsAllocated) {
                PointD startOffset = GetContentStart (GetContentEnd ());
                SizeD contentsSize = new SizeD (-1f, -1f);

                int i;
                for (i = 0; i < 2; i++) {
                    if (i > 0) {
                        bool savedReAllocating = reAllocating;
                        reAllocating = true;
                        contentsSize = AllocateContentsSize (start);
                        reAllocating = savedReAllocating;
                    } else
                        contentsSize = AllocateContentsSize (start);

                    if (style.HFill == true || style.VFill == true) {
                        if (style.HFill == false)
                            contentsSize.Width = 0f;

                        if (style.VFill == false)
                            contentsSize.Height = 0f;
                    } else {
                        contentsSize.Width = 0f;
                        contentsSize.Height = 0f;
                    }

                    contentsSize.Width += startOffset.X;
                    contentsSize.Height += startOffset.Y;

                    contentsSize.Width = Math.Max (contentsSize.Width, minimalSize.WidthValue);
                    contentsSize.Height = Math.Max (contentsSize.Height, minimalSize.HeightValue);

                    if (i > 0)
                        break;

                    ResolveDistribution (start, contentsSize);

                    // If this object is moved to another page reallocate the children
                    if (allocatedHPageFill <= 0f && allocatedVPageFill <= 0f)
                        break;

                    start.X += allocatedHPageFill;
                    start.Y += allocatedVPageFill;
                    IsAllocated = true;
                    QueueAllocateDown ();
                }

                allocatedSize.Width = contentsSize.Width;
                allocatedSize.Height = contentsSize.Height;
                allocatedSize.RelativeWidth = minimalSize.RelativeWidth;
                allocatedSize.RelativeHeight = minimalSize.RelativeHeight;
            } else
                ResolveDistribution (start, allocatedSize.StaticSize);

            return allocatedSize.StaticSize;
        }

        protected abstract SizeD AllocateContentsSize (PointD start);

        protected virtual bool ResolveDistribution (PointD start, SizeD size)
        {
            PageSettings pSettings = DocumentHelper.CurrentPageSettings;
            if (isDistributed || pSettings.Width.IsZero ())
                return false;

            SizeD startOffset = new SizeD ();
            SizeD endOffset = new SizeD ();

            if (Parent != null) {
                FormDrawableContainer container = Parent as FormDrawableContainer;
                if (container == null)
                    throw new Exception ("The parent control is not a container.");

                PointD contentStart = container.GetContentStart ();
                startOffset.Width = contentStart.X;
                startOffset.Height = contentStart.Y;

                // If this is not the last child in the parent object
                // add the inner offset else add the ending offset
                if (ParentPosition < container.Children.Count - 1) {
                    switch (container.ChildDistribution) {
                        case ObjectStyle.ChildDistribution.Horizontal:
                            endOffset.Width = Math.Max (container.Style.InnerVerticalBorder ?? 0, container.Style.InnerHSpacing ?? 0);
                            break;
                        case ObjectStyle.ChildDistribution.Vertical:
                            endOffset.Height = Math.Max (container.Style.InnerHorizontalBorder ?? 0,
                                container.Style.InnerVSpacing ?? 0);
                            break;
                    }
                } else {
                    PointD contentEnd = container.GetContentEnd ();
                    endOffset.Width = contentEnd.X;
                    endOffset.Height = contentEnd.Y;
                }
            }

            bool ret = false;
            allocatedHPageFill = 0f;
            distribution.LeftColumn = Math.Max ((int) start.X + 1, 0) / (int) pSettings.Width;
            distribution.RightColumn = Math.Max ((int) (start.X + size.Width + endOffset.Width - 1), 0) / (int) pSettings.Width;
            if (distribution.LeftColumn != distribution.RightColumn && AllowHorizontalBreak && !style.AllowHBreak) {
                distribution.LeftColumn++;
                allocatedHPageFill = pSettings.Width - start.X % pSettings.Width;
                ret = true;
            }
            if (distribution.LeftColumn > distribution.RightColumn)
                distribution.LeftColumn = distribution.RightColumn;

            allocatedVPageFill = 0f;
            distribution.UpperRow = Math.Max ((int) start.Y + 1, 0) / (int) pSettings.Height;
            distribution.LowerRow = Math.Max ((int) (start.Y + size.Height + endOffset.Height - 1), 0) / (int) pSettings.Height;
            if (distribution.UpperRow != distribution.LowerRow && AllowVerticalBreak && !style.AllowVBreak) {
                distribution.UpperRow++;
                allocatedVPageFill = pSettings.Height - start.Y % pSettings.Height;
                ret = true;
            }
            if (distribution.UpperRow > distribution.LowerRow)
                distribution.UpperRow = distribution.LowerRow;

            isDistributed = true;
            return ret;
        }

        public virtual void QueueAllocate ()
        {
            FormDrawableObject drawableParent = Parent as FormDrawableObject;

            if (drawableParent != null)
                drawableParent.QueueAllocate ();
            else
                QueueAllocateDown ();
        }

        public virtual void QueueAllocateDown ()
        {
            IsAllocated = false;
            calculatedSize.Width = null;
            calculatedSize.Height = null;
        }

        protected virtual void QueueAllocateUp (bool reallocateCurrent = true)
        {
            if (reallocateCurrent)
                IsAllocated = false;

            FormDrawableContainer container = parent as FormDrawableContainer;
            if (container != null)
                container.QueueAllocateUp ();
        }

        protected bool isDistributed;

        public virtual void QueueRedistributeDown ()
        {
            isDistributed = false;
        }

        protected PointD GetContentStart ()
        {
            PointD ret = new PointD ();

            ret.X += style.XStart;
            ret.X += style.HPadding ?? 0;
            ret.X += Math.Max (style.Border ?? 0, style.Spacing ?? 0);

            ret.Y += style.YStart;
            ret.Y += style.VPadding ?? 0;
            ret.Y += Math.Max (style.Border ?? 0, style.Spacing ?? 0);

            return ret;
        }

        public PointD GetContentStart (PointD start)
        {
            PointD startOffset = GetContentStart ();

            start.X += startOffset.X;
            start.Y += startOffset.Y;

            return start;
        }

        protected PointD GetContentStart (PointD start, SizeD visibleSize, SizeD contentsSize)
        {
            PointD startOffset = GetContentStart ();
            PointD endOffset = GetContentEnd ();

            Cairo.Rectangle contentArea = new Cairo.Rectangle
                (
                    start.X + startOffset.X,
                    start.Y + startOffset.Y,
                    visibleSize.Width - startOffset.X - endOffset.X,
                    visibleSize.Height - startOffset.Y - endOffset.Y
                );

            HorizontalAlignment hAlign = distribution.GetColumnsUsed () > 1 ?
                HorizontalAlignment.Left : style.HAlign;
            VerticalAlignment vAlign = distribution.GetRowsUsed () > 1 ?
                VerticalAlignment.Top : style.VAlign;

            PointD ret = new PointD (0f, 0f);
            switch (hAlign) {
                case HorizontalAlignment.Right:
                    ret.X = contentArea.X + (contentArea.Width - contentsSize.Width);
                    break;
                case HorizontalAlignment.Center:
                    ret.X = contentArea.X + (contentArea.Width - contentsSize.Width) / 2;
                    break;
                default:
                    ret.X = contentArea.X;
                    break;
            }

            switch (vAlign) {
                case VerticalAlignment.Bottom:
                    ret.Y = contentArea.Y + (contentArea.Height - contentsSize.Height);
                    break;
                case VerticalAlignment.Center:
                    ret.Y = contentArea.Y + (contentArea.Height - contentsSize.Height) / 2;
                    break;
                default:
                    ret.Y = contentArea.Y;
                    break;
            }

            return ret;
        }

        protected PointD GetContentEnd ()
        {
            PointD ret = new PointD { X = style.HPadding ?? 0, Y = style.VPadding ?? 0 };
            ret.X += Math.Max (style.Border ?? 0, style.Spacing ?? 0);
            ret.Y += Math.Max (style.Border ?? 0, style.Spacing ?? 0);

            return ret;
        }

        public virtual FormDrawableContainer GetAllowedParent (FormDrawableContainer parentToCheck)
        {
            FormDrawableContainer currentParent = parentToCheck;
            while (currentParent != null) {
                Type type = currentParent.GetType ();
                if (GetAllowedParentTypes ().Any (allowedParentType => type == allowedParentType || type.IsSubclassOf (allowedParentType)))
                    return currentParent;

                currentParent = (FormDrawableContainer) currentParent.Parent;
            }
            return null;
        }

        public FormDrawableObject GetParentBelow (FormDrawableContainer newParent)
        {
            FormObject currentParent = this;
            while (currentParent != null) {
                if (newParent.Children.Contains (currentParent))
                    return (FormDrawableObject) currentParent;
                currentParent = currentParent.Parent;
            }
            return this;
        }

        protected virtual IEnumerable<Type> GetAllowedParentTypes ()
        {
            return new [] { typeof (Form), typeof (Section), typeof (VBox), typeof (HBox), typeof (BoxItem), typeof (TableCell) };
        }

        protected SizeD GetObjectSizeInPage (int pageColumn, int pageRow, PointD start)
        {
            PageSettings page = (PageSettings) DocumentHelper.CurrentPageSettings.Clone ();
            page.Column = pageColumn;
            page.Row = pageRow;

            PointD startOffset = GetContentStart ();
            PointD endOffset = GetContentEnd ();

            RectangleD objectRec = new RectangleD
                (
                    start.X + startOffset.X,
                    start.Y + startOffset.Y,
                    calculatedSize.WidthValue - startOffset.X - endOffset.X,
                    calculatedSize.HeightValue - startOffset.Y - endOffset.Y
                );
            objectRec.Intersect (page.GetPageRectangle ());

            return objectRec.Size;
        }

        public virtual bool OnSizeCalculated (PointD childPosition)
        {
            return true;
        }

        public override string ToString ()
        {
            return string.Format ("{0}{1}",
                parent != null ? string.Format ("{0}[{1}] >>", parent, parentPosition) : string.Empty,
                GetType ().Name);
        }
    }
}
