﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using SharpDX;
using System;

#if NETFX_CORE
namespace HelixToolkit.UWP.Core2D
#else
using System.Windows;
namespace HelixToolkit.Wpf.SharpDX.Core2D
#endif
{
#if NETFX_CORE
    public abstract partial class Element2DCore
#else
    public abstract partial class Element2DCore
#endif
    {
        #region layout management
        internal bool IsMeasureDirty { set; get; } = true;

        internal bool IsArrangeDirty { set; get; } = true;

        internal bool IsTransformDirty { set; get; } = true;

        private Thickness marginInternal = new Thickness();
        internal Thickness MarginInternal
        {
            set
            {
                if (Set(ref marginInternal, value))
                {
                    MarginWidthHeight = new Vector2((float)(value.Left + value.Right), (float)(value.Top + value.Bottom));
                    InvalidateMeasure();
                }
            }
            get
            {
                return marginInternal;
            }
        }

        private Vector2 MarginWidthHeight { set; get; }

        private float widthInternal = float.PositiveInfinity;
        internal float WidthInternal
        {
            set
            {
                if (Set(ref widthInternal, value))
                {
                    InvalidateMeasure();
                }
            }
            get
            {
                return widthInternal;
            }
        }


        private float heightInternal = float.PositiveInfinity;
        internal float HeightInternal
        {
            set
            {
                if (Set(ref heightInternal, value))
                {
                    InvalidateMeasure();
                }
            }
            get
            {
                return heightInternal;
            }
        }

        private float minimumWidthInternal = 0;
        internal float MinimumWidthInternal
        {
            set
            {
                if (Set(ref minimumWidthInternal, value) && value > widthInternal)
                {
                    InvalidateMeasure();
                }
            }
            get
            {
                return minimumWidthInternal;
            }
        }


        private float minimumHeightInternal = 0;
        internal float MinimumHeightInternal
        {
            set
            {
                if (Set(ref minimumHeightInternal, value) && value > heightInternal)
                {
                    InvalidateMeasure();
                }
            }
            get
            {
                return minimumHeightInternal;
            }
        }

        private float maximumWidthInternal = float.PositiveInfinity;
        internal float MaximumWidthInternal
        {
            set
            {
                if (Set(ref maximumWidthInternal, value) && value < widthInternal)
                {
                    InvalidateMeasure();
                }
            }
            get
            {
                return maximumWidthInternal;
            }
        }


        private float maximumHeightInternal = float.PositiveInfinity;
        internal float MaximumHeightInternal
        {
            set
            {
                if (Set(ref maximumHeightInternal, value) && value < heightInternal)
                {
                    InvalidateMeasure();
                }
            }
            get
            {
                return maximumHeightInternal;
            }
        }


        private HorizontalAlignment horizontalAlignmentInternal = HorizontalAlignment.Stretch;
        internal HorizontalAlignment HorizontalAlignmentInternal
        {
            set
            {
                if (Set(ref horizontalAlignmentInternal, value))
                {
                    InvalidateArrange();
                }
            }
            get
            {
                return horizontalAlignmentInternal;
            }
        }


        private VerticalAlignment verticalAlignmentInternal = VerticalAlignment.Stretch;
        internal VerticalAlignment VerticalAlignmentInternal
        {
            set
            {
                if (Set(ref verticalAlignmentInternal, value))
                {
                    InvalidateArrange();
                }
            }
            get
            {
                return verticalAlignmentInternal;
            }
        }
        private Vector2 layoutOffset = Vector2.Zero;
        public Vector2 LayoutOffsets
        {
            private set
            {
                if(Set(ref layoutOffset, value))
                {
                    IsTransformDirty = true;
                }
            }
            get { return layoutOffset; }
        }

        private Vector2 renderSize = Vector2.Zero;
        public Vector2 RenderSize
        {
            get { return renderSize; }
            private set
            {
                if (Set(ref renderSize, value))
                {
                    IsTransformDirty = true;
                }
            }
        }

        public Vector2 DesiredSize { get; private set; }
        public Vector2 UnclippedDesiredSize { get; private set; } = new Vector2(-1, -1);

        public Vector2 Size { get { return new Vector2(widthInternal, heightInternal); } }

        public bool ClipEnabled { private set; get; } = false;

        public bool ClipToBound { set; get; } = false;

        public RectangleF ClipBound
        {
            private set
            {
                RenderCore.ClippingBound = value;
            }
            get { return RenderCore.ClippingBound; }
        }

        public RectangleF Bound
        {
            private set
            {
                RenderCore.Bound = value;
            }
            get { return RenderCore.Bound; }
        }

        protected void InvalidateMeasure()
        {
            IsArrangeDirty = true;
            IsMeasureDirty = true;
            TraverseUp(this, (p) => { p.IsArrangeDirty = true; p.IsMeasureDirty = true; return true; });
        }

        protected void InvalidateArrange()
        {
            IsArrangeDirty = true;
            TraverseUp(this, (p) => { p.IsArrangeDirty = true; return true; });
        }

        protected static void TraverseUp(Element2DCore core, Func<Element2DCore, bool> action)
        {
            var ancestor = core.Parent as Element2DCore;
            while (ancestor != null)
            {
                action(ancestor);
                ancestor = ancestor.Parent as Element2DCore;
            }
        }


        public void Measure(Vector2 availableSize)
        {
            if (!IsAttached || !IsMeasureDirty)
            {
                return;
            }
            var availableSizeWithoutMargin = availableSize - MarginWidthHeight;
            Vector2 maxSize = Vector2.Zero, minSize = Vector2.Zero;
            CalculateMinMax(ref minSize, ref maxSize);

            availableSizeWithoutMargin.X = Math.Max(minSize.X, Math.Min(availableSizeWithoutMargin.X, maxSize.X));
            availableSizeWithoutMargin.Y = Math.Max(minSize.Y, Math.Min(availableSizeWithoutMargin.Y, maxSize.Y));

            var desiredSize = MeasureOverride(availableSizeWithoutMargin);

            var unclippedDesiredSize = desiredSize;

            bool clipped = false;
            if (desiredSize.X > maxSize.X)
            {
                desiredSize.X = maxSize.X;
                clipped = true;
            }

            if (desiredSize.Y > maxSize.Y)
            {
                desiredSize.Y = maxSize.Y;
                clipped = true;
            }

            var clippedDesiredSize = desiredSize + MarginWidthHeight;

            if (clippedDesiredSize.X > availableSize.X)
            {
                clippedDesiredSize.X = availableSize.X;
                clipped = true;
            }

            if (clippedDesiredSize.Y > availableSize.Y)
            {
                clippedDesiredSize.Y = availableSize.Y;
                clipped = true;
            }

            if (clipped || clippedDesiredSize.X < 0 || clippedDesiredSize.Y < 0)
            {
                UnclippedDesiredSize = unclippedDesiredSize;
            }
            else
            {
                UnclippedDesiredSize = new Vector2(-1, -1);
            }
            DesiredSize = clippedDesiredSize;
            IsMeasureDirty = false;
        }

        public void Arrange(RectangleF rect)
        {
            if (!IsAttached)
            { return; }
            bool ancestorDirty = false;
            TraverseUp(this, (parent) =>
            {
                if (parent.IsArrangeDirty)
                {
                    ancestorDirty = true;
                    return false;
                }
                else { return true; }
            });

            var rectWidthHeight = new Vector2(rect.Width, rect.Height);

            if ((!IsArrangeDirty && !ancestorDirty) || rectWidthHeight.IsZero)
                return;

            var arrangeSize = rectWidthHeight;
            

            ClipEnabled = false;
            var desiredSize = DesiredSize;

            if (float.IsNaN(DesiredSize.X) || float.IsNaN(DesiredSize.Y))
            {
                if (UnclippedDesiredSize.X == -1 || UnclippedDesiredSize.Y == -1)
                {
                    desiredSize = arrangeSize - MarginWidthHeight;
                }
                else
                {
                    desiredSize = UnclippedDesiredSize - MarginWidthHeight;
                }
            }

            if (arrangeSize.X < desiredSize.X)
            {
                ClipEnabled = true;
                arrangeSize.X = desiredSize.X;
            }

            if (arrangeSize.Y < desiredSize.Y)
            {
                ClipEnabled = true;
                arrangeSize.Y = desiredSize.Y;
            }

            if (HorizontalAlignmentInternal != HorizontalAlignment.Stretch)
            {
                arrangeSize.X = desiredSize.X;
            }

            if (VerticalAlignmentInternal != VerticalAlignment.Stretch)
            {
                arrangeSize.Y = desiredSize.Y;
            }

            Vector2 minSize = Vector2.Zero, maxSize = Vector2.Zero;

            CalculateMinMax(ref minSize, ref maxSize);

            float calcedMaxWidth = Math.Max(desiredSize.X, maxSize.X);
            if (calcedMaxWidth < arrangeSize.X)
            {
                ClipEnabled = true;
                arrangeSize.X = calcedMaxWidth;
            }

            float calcedMaxHeight = Math.Max(desiredSize.Y, maxSize.Y);
            if (calcedMaxHeight < arrangeSize.Y)
            {
                ClipEnabled = true;
                arrangeSize.Y = calcedMaxHeight;
            }

            var oldRenderSize = RenderSize;
            var arrangeResultSize = ArrangeOverride(arrangeSize);

            bool arrangeSizeChanged = arrangeResultSize != RenderSize;
            if (arrangeSizeChanged)
            {
                InvalidateVisual();
            }

            RenderSize = arrangeResultSize;

            var clippedArrangeResultSize = new Vector2(Math.Min(arrangeResultSize.X, maxSize.X), Math.Min(arrangeResultSize.Y, maxSize.Y));
            if (!ClipEnabled)
            {
                ClipEnabled = clippedArrangeResultSize.X < arrangeResultSize.X || clippedArrangeResultSize.Y < arrangeResultSize.Y;
            }

            var clientSize = new Vector2(Math.Max(0, rectWidthHeight.X - MarginWidthHeight.X), Math.Max(0, rectWidthHeight.Y - MarginWidthHeight.Y));

            if (!ClipEnabled)
            {
                ClipEnabled = clientSize.X < clippedArrangeResultSize.X || clientSize.Y < clippedArrangeResultSize.Y;
            }

            var layoutOffset = Vector2.Zero;

            var tempHorizontalAlign = HorizontalAlignmentInternal;
            var tempVerticalAlign = VerticalAlignmentInternal;

            if (tempHorizontalAlign == HorizontalAlignment.Stretch && clippedArrangeResultSize.X > clientSize.X)
            {
                tempHorizontalAlign = HorizontalAlignment.Left;
            }

            if (tempVerticalAlign == VerticalAlignment.Stretch && clippedArrangeResultSize.Y > clientSize.Y)
            {
                tempVerticalAlign = VerticalAlignment.Top;
            }

            if (tempHorizontalAlign == HorizontalAlignment.Center || tempHorizontalAlign == HorizontalAlignment.Stretch)
            {
                layoutOffset.X = (clientSize.X - clippedArrangeResultSize.X) / 2.0f;
            }
            else if (tempHorizontalAlign == HorizontalAlignment.Right)
            {
                layoutOffset.X = clientSize.X - clippedArrangeResultSize.X;// - (float)MarginInternal.Right;
            }
            else
            {
                layoutOffset.X = 0;// (float)MarginInternal.Left;
            }

            if (tempVerticalAlign == VerticalAlignment.Center || tempVerticalAlign == VerticalAlignment.Stretch)
            {
                layoutOffset.Y = (clientSize.Y - clippedArrangeResultSize.Y) / 2.0f;
            }
            else if (tempVerticalAlign == VerticalAlignment.Bottom)
            {
                layoutOffset.Y = clientSize.Y - clippedArrangeResultSize.Y;// - (float)MarginInternal.Bottom;
            }
            else
            {
                layoutOffset.Y = 0;// (float)MarginInternal.Top;
            }

            layoutOffset += new Vector2(rect.Left, rect.Top);

            if (ClipEnabled || ClipToBound)
            {
                ClipBound = new RectangleF(0, 0, clientSize.X, clientSize.Y);
            }

            LayoutOffsets = layoutOffset;
            UpdateLayoutInternal();
            IsArrangeDirty = false;
        }

        public void InvalidateVisual()
        {
            IsTransformDirty = true;
            IsMeasureDirty = true;
            IsArrangeDirty = true;
            TraverseUp(this, (p) => 
            {
                p.IsTransformDirty = true;
                p.IsMeasureDirty = true;
                p.IsArrangeDirty = true;
                return true;
            });
            if (IsAttached)
            {
                InvalidateRender();
            }
        }

        private void CalculateMinMax(ref Vector2 minSize, ref Vector2 maxSize)
        {
            maxSize.Y = MaximumHeightInternal;
            minSize.Y = MinimumHeightInternal;

            var dimensionLength = HeightInternal;

            float height = dimensionLength;

            maxSize.Y = Math.Max(Math.Min(height, maxSize.Y), minSize.Y);

            height = (float.IsInfinity(dimensionLength) ? 0 : dimensionLength);

            minSize.Y = Math.Max(Math.Min(maxSize.Y, height), minSize.Y);

            maxSize.X = MaximumWidthInternal;
            minSize.X = MinimumWidthInternal;

            dimensionLength = WidthInternal;

            float width = dimensionLength;

            maxSize.X = Math.Max(Math.Min(width, maxSize.X), minSize.X);

            width = (float.IsInfinity(dimensionLength) ? 0 : dimensionLength);

            minSize.X = Math.Max(Math.Min(maxSize.X, width), minSize.X);
        }

        private void UpdateLayoutInternal()
        {
            if (IsTransformDirty)
            {
                Bound = new RectangleF((float)MarginInternal.Left, (float)MarginInternal.Top, RenderSize.X - MarginWidthHeight.X, RenderSize.Y - MarginWidthHeight.Y);
                ClipBound = new RectangleF(0, 0, RenderSize.X, RenderSize.Y);
                LayoutTranslate = Matrix3x2.Translation(LayoutOffsets.X, LayoutOffsets.Y);
                IsTransformDirty = false;
            }
        }

        protected virtual Vector2 ArrangeOverride(Vector2 finalSize)
        {
            foreach(var item in Items)
            {
                item.Arrange(new RectangleF(0, 0,finalSize.X, finalSize.Y));
            }
            return finalSize;
        }

        protected virtual Vector2 MeasureOverride(Vector2 availableSize)
        {
            var size = Size;
            if (float.IsInfinity(size.X))
            {
                size.X = availableSize.X;
            }
            if (float.IsInfinity(size.Y))
            {
                size.Y = availableSize.Y;
            }
            foreach(var item in Items)
            {
                item.Measure(availableSize);
            }
            return availableSize;
        }
        #endregion
    }
}
