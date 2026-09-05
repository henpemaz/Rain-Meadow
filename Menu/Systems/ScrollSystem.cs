using Menu;
using RainMeadow.UI.Interfaces;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Systems
{
    /// <summary>
    /// Helps with element positioning. <para></para>
    /// Scroller itself should handle the scrolling of the container, this class should only handle the positioning of elements and scroll offset relating to horizontal/vertical scroll<para></para>
    /// </summary>
    public abstract class ScrollSystem
    {
        public Anchor _scrollPosAnchor;
        public float _contentSize;
        public Vector2 _viewSize;
        public readonly Axis scrollingAxis;
        public event Action? MarkScrollObjectsDirty, OnViewSizeChanged;
        public bool ScrollStartsFromTop => ScrollPosAnchor is Anchor.TopLeft or Anchor.TopRight;
        public bool ScrollStartsFromLeft => ScrollPosAnchor is Anchor.TopLeft or Anchor.BottomLeft;
        public bool IsHorizontal => scrollingAxis == Axis.Horizontal;
        public Action? MarkDirtyForScrollObjects => MarkScrollObjectsDirty;
        public int IndexToRef => IsHorizontal ? 0 : 1;
        public int OppositeIndexToRef => IsHorizontal ? 1 : 0;
        public Vector2 ScrollStepDir
        {
            get
            {
                float stepX = 0;
                float stepY = 0;
                if (IsHorizontal)
                    stepX = (ScrollStartsFromLeft ? 1 : -1);
                else stepY = (ScrollStartsFromTop ? 1 : -1);
                    return new(stepX, stepY);
            }
        }
        public Vector2 ScrollPosStepDir
        {
            get
            {
                float stepX = 0;
                float stepY = 0;
                if (IsHorizontal)
                    stepX = (ScrollStartsFromLeft ? -1 : 1);
                else stepY = (ScrollStartsFromTop ? 1 : -1);
                return new(stepX, stepY);
            }
        }
        public virtual Vector2 ViewSize
        {
            get => _viewSize;
            set
            {
                if (_viewSize == value) return;
                _viewSize = value;
                MarkDirtyForScrollObjects?.Invoke();
                OnViewSizeChanged?.Invoke();
            }
        }
        /// <summary>
        /// Determines where the scroll position is anchored to, relative to the container and affects the direction of scroll<para></para>
        /// For example, at TopLeft, at 0 scroll, you will view at the top left, and scrolls down/right.<br/>
        /// </summary>
        public Anchor ScrollPosAnchor
        {
            get => _scrollPosAnchor;
            set
            {
                if (_scrollPosAnchor == value) return;
                _scrollPosAnchor = value;
                MarkDirtyForScrollObjects?.Invoke();
            }
        }
        public ScrollSystem(Axis scrollingAxis)
        {
            this.scrollingAxis = scrollingAxis;

        }
        public virtual Direction GetElementPosDirection(bool inverse = false)
        {
            if (IsHorizontal)
            {
                if (ScrollStartsFromLeft)
                    return inverse ? Direction.Left : Direction.Right;
                return inverse? Direction.Right : Direction.Left;
            }
            if (ScrollStartsFromTop)
                return inverse? Direction.Top : Direction.Bottom;
            return inverse? Direction.Bottom : Direction.Top;
        }
        public bool TryGetScrollNeededForBounds(Direction boundary, out float scrollOffset)
        {
            scrollOffset = 0;
            if (IsHorizontal && boundary is Direction.Left or Direction.Right)
            {
                if (boundary is Direction.Left != ScrollStartsFromLeft)
                    scrollOffset = GetMaxScroll();
                return true;
            }
            else if (!IsHorizontal && boundary is Direction.Top or Direction.Bottom)
            {
                if (boundary is Direction.Top != ScrollStartsFromTop)
                    scrollOffset = GetMaxScroll();
                return true;
            }

            return false;
        }
        public bool TryAddScrollThroughWheel(float scrollOffset, ref float desiredScrollPosOffset)
        {
            float scrollPosOffset = scrollOffset;
            scrollPosOffset *= ScrollStepDir[IndexToRef];
            return TryAddScroll(scrollPosOffset, ref desiredScrollPosOffset);
        }
        public bool TryAddScroll(float scrollPosOffset, ref float desiredScrollPosOffset)
        {
            if (scrollPosOffset != 0)
            {
                var maxScroll = GetMaxScroll();
                if ((scrollPosOffset < 0 && desiredScrollPosOffset > 0) || (scrollPosOffset > 0 && desiredScrollPosOffset < maxScroll))
                {
                    desiredScrollPosOffset = Mathf.Clamp(desiredScrollPosOffset + scrollPosOffset, 0, maxScroll);
                    return true;
                }
            }
            return false;

        }
        public float GetSliderValue(float origSliderValue)
        {
            if (scrollingAxis == Axis.Horizontal)
                return ScrollStartsFromLeft ? origSliderValue : 1 - origSliderValue;
            return ScrollStartsFromTop ? 1 - origSliderValue : origSliderValue;
        }
        public virtual Vector2 ScrollOffsetToPosOffset(float scrollOffset)
        {
            Vector2 scrollVec = new(scrollOffset, scrollOffset);
            return scrollVec * ScrollPosStepDir;
        }
        public Vector2 PositionOfElementWithScroll(int index, ValueTuple<Vector2, Vector2> posSizeOfElement, ValueTuple<Vector2, Vector2>? posSizeOfPrevEleemnt, float scrollOffset)
        {
            Vector2 scrollPosOffset = new(0, 0);
            if (posSizeOfPrevEleemnt == null)
                scrollPosOffset = ScrollOffsetToPosOffset(scrollOffset);
            return NormalPositionOfElement(index, posSizeOfElement, posSizeOfPrevEleemnt) + scrollPosOffset;
        }
        public abstract Vector2 NormalPositionOfElement(int index, ValueTuple<Vector2, Vector2> posSizeOfElement, ValueTuple<Vector2, Vector2>? posSizeOfPrevEleemnt);
        public abstract Vector2 SizeOfElement(int index, Vector2 origSizeOfElement);
        public abstract float GetMaxScroll();
        public enum Direction
        {
            Left, Right,
            Top, Bottom
        }
        public enum Anchor
        {
            TopLeft, TopRight,
            BottomLeft, BottomRight
        }
        public enum Axis
        {
            Horizontal = 1, 
            Vertical,
        }
    }
}
