//
// FormDrawableContainer.cs
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
using System.Linq;
using System.Xml.Linq;
using Cairo;
using Warehouse.Data.DataBinding;

namespace Warehouse.Component.Documenting
{
    public abstract class FormDrawableContainer : FormDrawableObject
    {
        #region Protected members

        protected BindList<FormObject> childObjects = new BindList<FormObject> ();
        protected ObjectStyle.ChildDistribution childDistribution = ObjectStyle.ChildDistribution.Mixed;
        private bool isCalculated;

        #endregion

        #region Public Members

        public override SizeD ActualSize
        {
            get { return visSize; }
        }

        public override bool Selectable
        {
            get { return base.Selectable; }
            set
            {
                if (base.Selectable == value)
                    return;

                base.Selectable = value;
                foreach (FormObject childObject in childObjects)
                    childObject.Selectable = value;
            }
        }

        public virtual BindList<FormObject> Children
        {
            get { return childObjects; }
        }

        public ObjectStyle.ChildDistribution ChildDistribution
        {
            get { return childDistribution; }
            set { childDistribution = value; }
        }

        #endregion

        protected FormDrawableContainer ()
        {
            childObjects.ListChanged += OnChildObjectsListChanged;
        }

        #region FormDrawableObject Members

        public override void Draw (PointD start, int pageNumber)
        {
            CalculateChildSizes (start);

            visSize = GetSize (start);
            SizeD contSize = GetContentsSize (start);
            PointD contStart = GetContentStart (start, visSize, contSize);
            PointD contOffset = new PointD (contStart.X - start.X, contStart.Y - start.Y);
            float innerOffset = GetInnerChildOffset ();

            PointD drawStart = DocumentHelper.CurrentPageSettings.GetInPageDrawLocation (start);

            base.Draw (start, pageNumber);

            if (style.BgColorSpecified)
                DocumentHelper.DrawingProvider.FillRectangle (
                    drawStart.X, drawStart.Y,
                    visSize.Width, visSize.Height, style.BgColor);

            if (style.Border > 0f)
                DocumentHelper.DrawingProvider.DrawInsetRectangle (
                    drawStart.X, drawStart.Y,
                    visSize.Width, visSize.Height, style.Border.Value, style.BorderColor);

            bool clip = style.HFill == false || style.VFill == false;
            if (clip) {
                DocumentHelper.DrawingProvider.SaveClip ();
                DocumentHelper.DrawingProvider.IntersectClip (new RectangleD (drawStart, visSize));
            }

            int i = 0;
            int lastPageInd = 0;
            foreach (FormDrawableObject drawable in Children) {
                bool drawThisObject = drawable.Distribution.IsInPage ();
                SizeD childSize = drawable.GetSize (contStart);

                int pageInd;
                switch (ChildDistribution) {
                    case ObjectStyle.ChildDistribution.Horizontal:
                        pageInd = drawable.Distribution.RightColumn;
                        if (lastPageInd != pageInd && drawable.Distribution.GetColumnsUsed () == 1 && i > 0)
                            contStart.X += innerOffset;

                        contStart.X += drawable.CalculatedHPageFill;

                        if (drawThisObject)
                            drawable.Draw (contStart, pageNumber);

                        contStart.X += childSize.Width;

                        if (drawThisObject && style.InnerVerticalBorder > 0f) {
                            drawStart = DocumentHelper.CurrentPageSettings.GetInPageDrawLocation (contStart);
                            // Draw a vertical border before this child
                            if (lastPageInd != pageInd && drawable.Distribution.GetColumnsUsed () == 1 && i > 0) {
                                float xPos = (float) Math.Floor (drawStart.X + contOffset.X - childSize.Width - (innerOffset / 2));
                                DocumentHelper.DrawingProvider.DrawLine (xPos, drawStart.Y,
                                    xPos, drawStart.Y + visSize.Height,
                                    style.InnerVerticalBorder.Value, style.BorderColor);
                            }

                            // Draw a vertical border after this child
                            if (i < Children.Count - 1) {
                                float xPos = (float) Math.Floor (drawStart.X + contOffset.X + (innerOffset / 2));
                                DocumentHelper.DrawingProvider.DrawLine (xPos, drawStart.Y,
                                    xPos, drawStart.Y + visSize.Height,
                                    style.InnerVerticalBorder.Value, style.BorderColor);
                            }
                        }

                        contStart.X += innerOffset;
                        lastPageInd = pageInd;
                        break;

                    case ObjectStyle.ChildDistribution.Vertical:
                        pageInd = drawable.Distribution.LowerRow;
                        if (lastPageInd != pageInd && drawable.Distribution.GetRowsUsed () == 1 && i > 0) {
                            contStart.Y += innerOffset;
                        }

                        contStart.Y += drawable.CalculatedVPageFill;

                        if (drawThisObject)
                            drawable.Draw (contStart, pageNumber);

                        contStart.Y += childSize.Height;

                        if (drawThisObject && style.InnerHorizontalBorder > 0f) {
                            drawStart = DocumentHelper.CurrentPageSettings.GetInPageDrawLocation (contStart);
                            // Draw a horizontal border before this child
                            if (lastPageInd != pageInd && drawable.Distribution.GetRowsUsed () == 1 && i > 0) {
                                float yPos = (float) Math.Floor (drawStart.Y + contOffset.Y - childSize.Height - (innerOffset / 2));
                                DocumentHelper.DrawingProvider.DrawLine (drawStart.X, yPos,
                                    drawStart.X + visSize.Width, yPos,
                                    style.InnerHorizontalBorder.Value, style.BorderColor);
                            }

                            // Draw a horizontal border after this child
                            if (i < Children.Count - 1) {
                                float yPos = (float) Math.Floor (drawStart.Y + contOffset.Y + (innerOffset / 2));
                                DocumentHelper.DrawingProvider.DrawLine (drawStart.X, yPos,
                                    drawStart.X + visSize.Width, yPos,
                                    style.InnerHorizontalBorder.Value, style.BorderColor);
                            }
                        }

                        contStart.Y += innerOffset;
                        lastPageInd = pageInd;
                        break;

                    default:
                        if (drawThisObject)
                            drawable.Draw (contStart, pageNumber);

                        break;
                }

                i++;
            }

            if (clip)
                DocumentHelper.DrawingProvider.RestoreClip ();
        }

        public override SizeD GetSize (PointD start)
        {
            CalculateChildSizes (start);

            return base.GetSize (start);
        }

        protected override SizeD GetContentsSize (PointD start)
        {
            return GetChildrenSize (start, (d, s) => d.GetSize (s));
        }

        public override SizeD AllocateSize (PointD start)
        {
            SizeD ret;
            bool redoAllocate = false;

            if (!IsAllocated) {
                if (!reAllocating)
                    OnBeforeAllocateSize ();

                ret = base.AllocateSize (start);

                if (!reAllocating)
                    OnAfterAllocateSize (out redoAllocate);

                if (redoAllocate) {
                    reAllocating = true;
                    QueueAllocateDown ();
                    QueueRedistributeDown ();
                    ret = base.AllocateSize (start);
                    reAllocating = false;
                }
            } else {
                ret = allocatedSize.StaticSize;
                if (!isDistributed) {
                    AllocateContentsSize (start);
                    OnAfterAllocateSize (out redoAllocate);
                    ResolveDistribution (start, allocatedSize.StaticSize);
                }
            }

            return ret;
        }

        protected override bool ResolveDistribution (PointD start, SizeD size)
        {
            bool ret = base.ResolveDistribution (start, size);
            if (ret) {
                QueueRedistributeDown ();
                AllocateContentsSize (start);
                isDistributed = true;
            }

            return ret;
        }

        protected override SizeD AllocateContentsSize (PointD start)
        {
            return GetChildrenSize (start, (d, s) => d.AllocateSize (s));
        }

        private SizeD GetChildrenSize (PointD start, Func<FormDrawableObject, PointD, SizeD> function)
        {
            int i;
            int pageInd;
            int lastPageInd;
            float offset;
            SizeD contentsSize = new SizeD (0f, 0f);
            SizeD childSize;

            start.X += allocatedHPageFill;
            start.Y += allocatedVPageFill;
            start = GetContentStart (start);
            switch (ChildDistribution) {
                case ObjectStyle.ChildDistribution.Horizontal:
                    offset = GetInnerChildOffset ();
                    i = 0;
                    lastPageInd = 0;

                    foreach (FormDrawableObject drawable in Children) {
                        PointD childStart = new PointD (start.X + contentsSize.Width, start.Y);
                        childSize = function (drawable, childStart);

                        contentsSize.Width += drawable.Style.XStart;
                        contentsSize.Width += drawable.AllocatedHPageFill;
                        contentsSize.Width += childSize.Width;
                        contentsSize.Height = Math.Max (contentsSize.Height, childSize.Height);

                        pageInd = drawable.Distribution.RightColumn;
                        if (lastPageInd != pageInd && drawable.Distribution.GetColumnsUsed () == 1 && i > 0)
                            contentsSize.Width += offset;

                        if (i < Children.Count - 1)
                            contentsSize.Width += offset;

                        lastPageInd = pageInd;
                        i++;
                    }
                    break;

                case ObjectStyle.ChildDistribution.Vertical:
                    offset = GetInnerChildOffset ();
                    i = 0;
                    lastPageInd = 0;

                    foreach (FormDrawableObject drawable in Children) {
                        PointD childStart = new PointD (start.X, start.Y + contentsSize.Height);
                        childSize = function (drawable, childStart);

                        contentsSize.Width = Math.Max (contentsSize.Width, childSize.Width);
                        contentsSize.Height += drawable.Style.YStart;
                        contentsSize.Height += drawable.AllocatedVPageFill;
                        contentsSize.Height += childSize.Height;

                        pageInd = drawable.Distribution.LowerRow;
                        if (lastPageInd != pageInd && drawable.Distribution.GetRowsUsed () == 1 && i > 0)
                            contentsSize.Height += offset;

                        if (i < Children.Count - 1)
                            contentsSize.Height += offset;

                        lastPageInd = pageInd;
                        i++;
                    }
                    break;

                case ObjectStyle.ChildDistribution.Mixed:
                    foreach (FormDrawableObject drawable in Children) {
                        childSize = function (drawable, start);

                        contentsSize.Width = Math.Max (contentsSize.Width, drawable.Style.XStart + childSize.Width + drawable.AllocatedHPageFill);
                        contentsSize.Height = Math.Max (contentsSize.Height, drawable.Style.YStart + childSize.Height + drawable.AllocatedVPageFill);
                    }
                    break;
            }

            return contentsSize;
        }

        protected virtual void OnBeforeAllocateSize ()
        {
            foreach (FormDrawableObject drawable in Children)
                drawable.MinimalSize.LoadFromStyle (drawable.Style);
        }

        protected virtual void OnAfterAllocateSize (out bool redoAllocate)
        {
            redoAllocate = false;
        }

        public override void QueueAllocateDown ()
        {
            if (!IsAllocated)
                return;

            foreach (FormDrawableObject drawable in Children)
                drawable.QueueAllocateDown ();

            IsAllocated = false;
            isCalculated = false;
            calculatedSize.Width = null;
            calculatedSize.Height = null;
        }

        public virtual void QueueCalculateDown (bool annulCalculatedSize = false)
        {
            if (!isCalculated)
                return;

            foreach (FormDrawableObject drawable in Children) {
                if (annulCalculatedSize)
                    drawable.CalculatedSize.Width = drawable.CalculatedSize.Height = 0;

                FormDrawableContainer container = drawable as FormDrawableContainer;
                if (container != null)
                    container.QueueCalculateDown (annulCalculatedSize);
            }

            isCalculated = false;
        }

        public override void QueueRedistributeDown ()
        {
            foreach (FormDrawableObject drawable in Children)
                drawable.QueueRedistributeDown ();

            isDistributed = false;
        }

        public override void ReCheckPageDependant ()
        {
            bool oldPageDependant = IsPageDependant;

            isPageDependant = Children.Cast<FormDrawableObject> ().Any (drawable => drawable.IsPageDependant);
            if (isPageDependant == oldPageDependant)
                return;

            FormDrawableObject drawableParent = Parent as FormDrawableObject;
            if (drawableParent != null)
                drawableParent.ReCheckPageDependant ();
        }

        #endregion

        protected void CreateChildrenFromTemplate (XElement node, Hashtable boundFields)
        {
            bindableFields = boundFields;

            CreateChildrenFromTemplate (node, boundFields, ref childObjects);
        }

        protected void CalculateChildSizes (PointD start)
        {
            while (true) {
                AllocateSize (start);

                if (isCalculated)
                    return;

                if (ResolveChildSizes (start))
                    break;

                isCalculated = true;
                QueueCalculateDown (true);
                QueueRedistributeDown ();
            }
            isCalculated = true;
        }

        private bool ResolveChildSizes (PointD start)
        {
            if (!allocatedSize.Height.HasValue && !allocatedSize.Width.HasValue)
                return true;

            int i;
            double calcSize;
            double percentRatio;
            bool optimized;
            start.X += allocatedHPageFill;
            start.Y += allocatedVPageFill;
            PointD childStart = GetContentStart (start);
            PointD startOffset = GetContentStart ();
            PointD endOffset = GetContentEnd ();
            float innerOffset = GetInnerChildOffset ();

            ObjectStyle.ChildDistribution childDist = ChildDistribution;
            double [] totalSpace = null;
            double [] absoluteSpace = null;
            double [] relativeSpace = null;
            int childCount = Children.Count;
            int pageCount;
            int lastPageInd;
            int pageInd;
            List<PointD> childPositions = new List<PointD> ();

            if (childDist == ObjectStyle.ChildDistribution.Horizontal) {
                try {
                    #region Initialize arrays and the total space used by the object in each page

                    pageCount = Math.Max (distribution.RightColumn + 1, Children
                        .Cast<FormDrawableObject> ()
                        .Select (child => child.Distribution.RightColumn + 1)
                        .DefaultIfEmpty ().Max ());
                    totalSpace = new double [pageCount];
                    absoluteSpace = new double [pageCount];
                    relativeSpace = new double [pageCount];

                    for (i = 0; i <= distribution.RightColumn; i++) {
                        totalSpace [i] = GetObjectSizeInPage (i, distribution.UpperRow, start).Width;
                    }

                    #endregion
                } catch (Exception ex) {
                    throw new Exception ("Exception while initializing the total space used by the horizontally aligned object", ex);
                }

                try {
                    #region Calculate the space used in each page by each child object

                    i = 0;
                    PointD cursor = childStart;
                    lastPageInd = distribution.LeftColumn;
                    foreach (FormDrawableObject drawable in Children) {
                        childPositions.Add (cursor);
                        pageInd = drawable.Distribution.LeftColumn;

                        if (drawable.AllocatedSize.RelativeWidth.HasValue)
                            relativeSpace [pageInd] += drawable.AllocatedSize.RelativeWidth.Value;
                        else if (drawable.AllocatedSize.Width.HasValue)
                            absoluteSpace [pageInd] += drawable.AllocatedSize.Width.Value;

                        cursor.X += drawable.AllocatedSize.WidthValue;

                        if (lastPageInd != pageInd) {
                            absoluteSpace [pageInd] += innerOffset;
                            cursor.X += innerOffset;
                            cursor.X += drawable.AllocatedHPageFill;
                        }

                        if (drawable.Distribution.GetColumnsUsed () > 1) {
                            totalSpace [pageInd + 1] -= cursor.X % DocumentHelper.CurrentPageSettings.Width;
                        } else if (i < childCount - 1) {
                            absoluteSpace [pageInd] += innerOffset;
                            cursor.X += innerOffset;
                        }

                        i++;
                        lastPageInd = pageInd;
                    }

                    #endregion
                } catch (Exception ex) {
                    throw new Exception ("Exception while calculating the space used in each page by the horizontally aligned children", ex);
                }

                try {
                    #region Check if we have to treat the object as "Mixed mode"

                    if (!allocatedSize.Width.HasValue) {
                        foreach (float rel in relativeSpace)
                            if (rel > 0f)
                                childDist = ObjectStyle.ChildDistribution.Mixed;
                    }

                    #endregion
                } catch (Exception ex) {
                    throw new Exception ("Exception while checking if we have to treat the horizontally aligned object as mixed mode", ex);
                }
            }

            if (childDist == ObjectStyle.ChildDistribution.Vertical) {
                try {
                    #region Initialize arrays and the total space used by the object in each page

                    pageCount = Math.Max (distribution.LowerRow + 1, Children
                        .Cast<FormDrawableObject> ()
                        .Select (child => child.Distribution.LowerRow + 1)
                        .DefaultIfEmpty ().Max ());
                    totalSpace = new double [pageCount];
                    absoluteSpace = new double [pageCount];
                    relativeSpace = new double [pageCount];

                    for (i = 0; i <= distribution.LowerRow; i++) {
                        totalSpace [i] = GetObjectSizeInPage (Distribution.LeftColumn, i, start).Height;
                    }

                    #endregion
                } catch (Exception ex) {
                    throw new Exception ("Exception while initializing the total space used by the vertically aligned object", ex);
                }

                try {
                    #region Calculate the space used in each page by each child object

                    i = 0;
                    PointD cursor = childStart;
                    lastPageInd = distribution.UpperRow;
                    foreach (FormDrawableObject drawable in Children) {
                        childPositions.Add (cursor);
                        pageInd = drawable.Distribution.UpperRow;

                        if (drawable.AllocatedSize.RelativeHeight.HasValue)
                            relativeSpace [pageInd] += drawable.AllocatedSize.RelativeHeight.Value;
                        else if (drawable.AllocatedSize.Height.HasValue)
                            absoluteSpace [pageInd] += drawable.AllocatedSize.Height.Value;

                        cursor.Y += drawable.AllocatedSize.HeightValue;

                        if (lastPageInd != pageInd) {
                            absoluteSpace [pageInd] += innerOffset;
                            cursor.Y += innerOffset;
                            cursor.Y += drawable.AllocatedVPageFill;
                        }

                        if (drawable.Distribution.GetRowsUsed () > 1) {
                            totalSpace [pageInd + 1] -= cursor.Y % DocumentHelper.CurrentPageSettings.Height;
                        } else if (i < childCount - 1) {
                            absoluteSpace [pageInd] += innerOffset;
                            cursor.Y += innerOffset;
                        }

                        i++;
                        lastPageInd = pageInd;
                    }

                    #endregion
                } catch (Exception ex) {
                    throw new Exception ("Exception while calculating the space used in each page by the vertically aligned children", ex);
                }

                try {
                    #region Check if we have to treat the object as "Mixed mode"

                    if (!allocatedSize.Height.HasValue) {
                        foreach (float rel in relativeSpace)
                            if (rel > 0f)
                                childDist = ObjectStyle.ChildDistribution.Mixed;
                    }

                    #endregion
                } catch (Exception ex) {
                    throw new Exception ("Exception while checking if we have to treat the vertically aligned object as mixed mode", ex);
                }
            }

            switch (childDist) {
                case ObjectStyle.ChildDistribution.Horizontal:

                    try {
                        #region Optimize relative sizes

                        for (i = 0, optimized = false; i < childCount && !optimized; i++) {
                            optimized = true;
                            foreach (FormDrawableObject drawable in Children) {
                                if (!drawable.AllocatedSize.RelativeWidth.HasValue || !drawable.AllocatedSize.Width.HasValue)
                                    continue;

                                pageInd = drawable.Distribution.LeftColumn;

                                // Find out the percentage of the space left for relative sizing
                                percentRatio = (totalSpace [pageInd] - absoluteSpace [pageInd]) * 100f / totalSpace [pageInd];
                                // Calculate the coeficient for percent translation
                                percentRatio /= relativeSpace [pageInd];
                                percentRatio = Math.Min (percentRatio, 1f);

                                calcSize = (float) Math.Round (totalSpace [pageInd] *
                                    Math.Min (drawable.AllocatedSize.RelativeWidthValue * percentRatio, 100f) / 100f, MidpointRounding.AwayFromZero);

                                if (calcSize <= drawable.AllocatedSize.Width) {
                                    absoluteSpace [pageInd] += drawable.AllocatedSize.WidthValue;
                                    relativeSpace [pageInd] -= drawable.AllocatedSize.RelativeWidthValue;
                                    drawable.AllocatedSize.RelativeWidth = null;
                                    optimized = false;
                                }
                            }
                        }

                        #endregion
                    } catch (Exception ex) {
                        throw new Exception ("Exception while optimizing the relative sizes of the horizontally aligned object", ex);
                    }

                    try {
                        #region Calculate relative sizes

                        lastPageInd = 0;
                        i = 0;
                        foreach (FormDrawableObject drawable in Children) {
                            if (drawable.AllocatedSize.RelativeHeight.HasValue && calculatedSize.Height.HasValue) {
                                drawable.CalculatedSize.Height = (float) Math.Round (
                                    Math.Max (calculatedSize.HeightValue - startOffset.Y - endOffset.Y, 0f) *
                                    Math.Min (drawable.AllocatedSize.RelativeHeightValue, 100f) / 100f, MidpointRounding.AwayFromZero);
                            } else {
                                drawable.CalculatedSize.Height = drawable.AllocatedSize.Height;
                            }

                            pageInd = drawable.Distribution.LeftColumn;

                            if (totalSpace [pageInd] == 0f) {
                                i++;
                                continue;
                            }

                            if (drawable.AllocatedSize.RelativeWidth.HasValue) {
                                // Find out the percentage of the space left for relative sizing
                                percentRatio = (totalSpace [pageInd] - absoluteSpace [pageInd]) * 100f / totalSpace [pageInd];
                                // Calculate the coeficient for percent translation
                                percentRatio /= relativeSpace [pageInd];
                                percentRatio = Math.Min (percentRatio, 1f);

                                relativeSpace [pageInd] -= drawable.AllocatedSize.RelativeWidthValue;

                                if (relativeSpace [pageInd] > 0f || relativeSpace.Length - 1 == pageInd) {
                                    calcSize = (float) Math.Round ((totalSpace [pageInd] *
                                        Math.Min (drawable.AllocatedSize.RelativeWidthValue * percentRatio, 100f) / 100f), MidpointRounding.AwayFromZero);

                                    drawable.CalculatedSize.Width = calcSize;
                                } else
                                    drawable.CalculatedSize.Width = totalSpace [pageInd] - absoluteSpace [pageInd];

                                absoluteSpace [pageInd] += drawable.CalculatedSize.WidthValue;
                            } else
                                drawable.CalculatedSize.Width = drawable.AllocatedSize.Width;

                            if (i > 0 && lastPageInd != pageInd)
                                drawable.CalculatedHPageFill = totalSpace [lastPageInd] - absoluteSpace [lastPageInd];
                            else
                                drawable.CalculatedHPageFill = 0f;

                            if (!drawable.OnSizeCalculated (childPositions [i]))
                                return false;

                            lastPageInd = drawable.Distribution.RightColumn;
                            i++;
                        }

                        #endregion
                    } catch (Exception ex) {
                        throw new Exception ("Exception while calculating the relative sizes of the horizontally aligned object", ex);
                    }

                    break;

                case ObjectStyle.ChildDistribution.Vertical:

                    try {
                        #region Optimize relative sizes

                        for (i = 0, optimized = false; i < childCount && !optimized; i++) {
                            optimized = true;
                            foreach (FormDrawableObject drawable in Children) {
                                if (!drawable.AllocatedSize.RelativeHeight.HasValue || !drawable.AllocatedSize.Height.HasValue)
                                    continue;

                                pageInd = drawable.Distribution.UpperRow;

                                // Find out the percentage of the space left for relative sizing
                                percentRatio = (totalSpace [pageInd] - absoluteSpace [pageInd]) * 100f / totalSpace [pageInd];
                                // Calculate the coeficient for percent translation
                                percentRatio /= relativeSpace [pageInd];
                                percentRatio = Math.Min (percentRatio, 1f);

                                calcSize = (float) Math.Round (totalSpace [pageInd] *
                                    Math.Min (drawable.AllocatedSize.RelativeHeightValue * percentRatio, 100f) / 100f, MidpointRounding.AwayFromZero);

                                if (calcSize <= drawable.AllocatedSize.Height) {
                                    absoluteSpace [pageInd] += drawable.AllocatedSize.HeightValue;
                                    relativeSpace [pageInd] -= drawable.AllocatedSize.RelativeHeightValue;
                                    drawable.AllocatedSize.RelativeHeight = null;
                                    optimized = false;
                                }
                            }
                        }

                        #endregion
                    } catch (Exception ex) {
                        throw new Exception ("Exception while optimizing the relative sizes of the vertically aligned object", ex);
                    }

                    try {
                        #region Calculate relative sizes

                        lastPageInd = 0;
                        i = 0;
                        foreach (FormDrawableObject drawable in Children) {
                            if (drawable.AllocatedSize.RelativeWidth.HasValue && calculatedSize.Width.HasValue) {
                                drawable.CalculatedSize.Width = (float) Math.Round (
                                    Math.Max (calculatedSize.WidthValue - startOffset.X - endOffset.X, 0f) *
                                    Math.Min (drawable.AllocatedSize.RelativeWidthValue, 100f) / 100f, MidpointRounding.AwayFromZero);
                            } else
                                drawable.CalculatedSize.Width = drawable.AllocatedSize.Width;

                            pageInd = drawable.Distribution.UpperRow;

                            if (totalSpace [pageInd] == 0f) {
                                i++;
                                continue;
                            }

                            if (drawable.AllocatedSize.RelativeHeight.HasValue) {
                                // Find out the percentage of the space left for relative sizing
                                percentRatio = (totalSpace [pageInd] - absoluteSpace [pageInd]) * 100f / totalSpace [pageInd];
                                // Calculate the coeficient for percent translation
                                percentRatio /= relativeSpace [pageInd];
                                percentRatio = Math.Min (percentRatio, 1f);

                                relativeSpace [pageInd] -= drawable.AllocatedSize.RelativeHeightValue;

                                if (relativeSpace [pageInd] > 0f || relativeSpace.Length - 1 == pageInd) {
                                    calcSize = (float) Math.Round ((totalSpace [pageInd] *
                                        Math.Min (drawable.AllocatedSize.RelativeHeightValue * percentRatio, 100f) / 100f), MidpointRounding.AwayFromZero);

                                    drawable.CalculatedSize.Height = calcSize;
                                } else {
                                    drawable.CalculatedSize.Height = totalSpace [pageInd] - absoluteSpace [pageInd];
                                }
                                absoluteSpace [pageInd] += drawable.CalculatedSize.HeightValue;
                            } else
                                drawable.CalculatedSize.Height = drawable.AllocatedSize.Height;

                            if (i > 0 && lastPageInd != pageInd)
                                drawable.CalculatedVPageFill = totalSpace [lastPageInd] - absoluteSpace [lastPageInd];
                            else
                                drawable.CalculatedVPageFill = 0f;

                            if (!drawable.OnSizeCalculated (childPositions [i]))
                                return false;

                            lastPageInd = drawable.Distribution.LowerRow;
                            i++;
                        }

                        #endregion
                    } catch (Exception ex) {
                        throw new Exception ("Exception while calculating the relative sizes of the vertically aligned object", ex);
                    }

                    break;

                case ObjectStyle.ChildDistribution.Mixed:

                    try {
                        #region Calculate relative sizes

                        foreach (FormDrawableObject drawable in Children) {
                            PageSettings pSettings = (PageSettings) DocumentHelper.CurrentPageSettings.Clone ();
                            pSettings.Column = drawable.Distribution.LeftColumn;
                            pSettings.Row = drawable.Distribution.UpperRow;
                            RectangleD pRect = pSettings.GetPageRectangle ();
                            pRect.Inflate (-endOffset.X, -endOffset.Y);

                            if (drawable.Distribution.GetColumnsUsed () == 1 &&
                                drawable.AllocatedSize.RelativeWidth.HasValue && calculatedSize.Width.HasValue) {

                                calcSize = (float) Math.Round (
                                    Math.Max (calculatedSize.WidthValue - startOffset.X - endOffset.X, 0f) *
                                    Math.Min (drawable.AllocatedSize.RelativeWidthValue, 100f) / 100f, MidpointRounding.AwayFromZero);

                                RectangleD temp = new RectangleD (childStart, new SizeD (calcSize, 1f));
                                temp.X += drawable.AllocatedHPageFill;
                                temp.Intersect (pRect);
                                calcSize = temp.Width;
                            } else {
                                calcSize = drawable.AllocatedSize.WidthValue;
                            }
                            drawable.CalculatedSize.Width = calcSize;
                            drawable.CalculatedHPageFill = drawable.AllocatedHPageFill;

                            if (drawable.Distribution.GetRowsUsed () == 1 &&
                                drawable.AllocatedSize.RelativeHeight.HasValue && calculatedSize.Height.HasValue) {

                                calcSize = (float) Math.Round (
                                    Math.Max (calculatedSize.HeightValue - startOffset.Y - endOffset.Y, 0f) *
                                    Math.Min (drawable.AllocatedSize.RelativeHeightValue, 100f) / 100f, MidpointRounding.AwayFromZero);

                                RectangleD temp = new RectangleD (childStart, new SizeD (1f, calcSize));
                                temp.Y += drawable.AllocatedVPageFill;
                                temp.Intersect (pRect);
                                calcSize = temp.Height;
                            } else {
                                calcSize = drawable.AllocatedSize.HeightValue;
                            }
                            drawable.CalculatedSize.Height = calcSize;
                            drawable.CalculatedVPageFill = drawable.AllocatedVPageFill;
                            if (!drawable.OnSizeCalculated (childStart))
                                return false;
                        }

                        #endregion
                    } catch (Exception ex) {
                        throw new Exception ("Exception while calculating the relative sizes of the mixed mode object", ex);
                    }

                    break;
            }

            return true;
        }

        private float GetInnerChildOffset ()
        {
            switch (ChildDistribution) {
                case ObjectStyle.ChildDistribution.Horizontal:
                    return Math.Max (style.InnerVerticalBorder ?? 0, style.InnerHSpacing ?? 0);
                case ObjectStyle.ChildDistribution.Vertical:
                    return Math.Max (style.InnerHorizontalBorder ?? 0, style.InnerVSpacing ?? 0);
                default:
                    return 0f;
            }
        }

        protected T Clone<T> (T ret) where T : FormDrawableContainer
        {
            ret.Style = (ObjectStyle) style.Clone ();
            ret.bindableFields = new Hashtable (bindableFields);
            foreach (FormObject child in childObjects)
                ret.Children.Add ((FormObject) child.Clone ());

            return ret;
        }

        public override void RefreshBoundFields (Hashtable boundFields)
        {
            base.RefreshBoundFields (boundFields);
            foreach (FormObject child in childObjects)
                child.RefreshBoundFields (boundFields);
        }

        public override XElement GetXmlElement ()
        {
            XElement element = base.GetXmlElement ();
            foreach (FormObject childObject in childObjects)
                element.Add (childObject.GetXmlElement ());

            return element;
        }

        public override bool OnSizeCalculated (PointD childPosition)
        {
            AllocateSize (childPosition);

            if (isCalculated)
                return true;

            if (!ResolveChildSizes (childPosition))
                return false;

            isCalculated = true;
            return true;
        }

        public override void Cleanup ()
        {
            foreach (FormObject formObject in Children)
                formObject.Cleanup ();

            base.Cleanup ();
        }

        /// <summary>
        /// This method is for debugging purposes
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public FormObject GetByPath (string path)
        {
            if (string.IsNullOrWhiteSpace (path))
                return this;

            int divider = path.IndexOf ('|');
            string index = divider >= 0 ? path.Substring (0, divider) : path;

            int i;
            if (!int.TryParse (index, out i) || i >= Children.Count)
                return null;

            FormObject child = Children [i];
            FormDrawableContainer container = child as FormDrawableContainer;
            if (container != null)
                return container.GetByPath (divider >= 0 && divider < path.Length - 1 ?
                    path.Substring (divider + 1) : string.Empty);

            return child;
        }
    }
}
