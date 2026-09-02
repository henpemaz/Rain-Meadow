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
        public event Action? MarkScrollObjectsDirty, OnViewSizeChanged, OnContentSizeChanged;
        public bool ScrollStartsFromTop => ScrollPosAnchor is Anchor.TopLeft or Anchor.TopRight;
        public bool ScrollStartsFromLeft => ScrollPosAnchor is Anchor.TopLeft or Anchor.BottomLeft;
        public bool IsHorizontal => scrollingAxis == Axis.Horizontal;
        public Action? MarkDirtyForScrollObjects => MarkScrollObjectsDirty;
        public int IndexToRef => IsHorizontal ? 0 : 1;
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
        public virtual float ContentSize
        {
            get => _contentSize;
            set
            {
                if (_contentSize == value) return;
                _contentSize = value;
                MarkDirtyForScrollObjects?.Invoke();
                OnContentSizeChanged?.Invoke();
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
        /// For example, at TopLeft, 0 scroll starts at top left, and scrolls down/right.<br/>
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
        public void SetContentSize(Vector2 size)
        {
            ContentSize = size[IndexToRef];
        }
        public float GetScrollNeededForScrollAnchor(Anchor anchor)
        {
            bool anchorStartsFromLeft = anchor is Anchor.TopLeft or Anchor.BottomLeft;
            bool anchorStartsFromTop = anchor is Anchor.TopLeft or Anchor.TopRight;
            if (IsHorizontal && ScrollStartsFromLeft != anchorStartsFromLeft)
            {
                return GetMaxScroll();
            }
            else if (!IsHorizontal && ScrollStartsFromTop != anchorStartsFromTop)
                return GetMaxScroll();

            return 0;
        }
        public virtual float GetMaxScroll()
        {
            return Mathf.Max(ContentSize - ViewSize[IndexToRef], 0);
        }
        public bool AddScroll(float scrollOffset, ref float desiredScrollPosOffset)
        {
            float scrollPosOffset = scrollOffset;
            scrollPosOffset *= ScrollStepDir[IsHorizontal? 0 : 1];
            return TryAddScroll(scrollPosOffset, ref desiredScrollPosOffset);
        }
        public bool TryAddScroll(float scrollPosOffset, ref float desiredScrollPosOffset)
        {
            if (scrollPosOffset != 0)
            {
                var horiMaxScroll = GetMaxScroll();
                if ((scrollPosOffset < 0 && desiredScrollPosOffset > 0) || (scrollPosOffset > 0 && desiredScrollPosOffset < horiMaxScroll))
                {
                    desiredScrollPosOffset = Mathf.Clamp(desiredScrollPosOffset + scrollPosOffset, 0, horiMaxScroll);
                    return true;
                }
            }
            return false;

        }
        public float GetSliderValue(float origSliderValue)
        {
            if (scrollingAxis == Axis.Horizontal)
            {
                return ScrollStartsFromLeft ? origSliderValue : 1 - origSliderValue;
            }
            return ScrollStartsFromTop ? 1 - origSliderValue : origSliderValue;
        }
        public Vector2 ScrollOffsetToDirection(float scrollOffset)
        {
            Vector2 scrollVec = new(scrollOffset, scrollOffset);
            return scrollVec * ScrollPosStepDir;
        }
        public abstract Vector2 NormalPositionOfElement(int index, ValueTuple<Vector2, Vector2> posSizeOfElement, ValueTuple<Vector2, Vector2>? posSizeOfPrevEleemnt);
        public abstract Vector2 SizeOfElement(int index, Vector2 origSizeOfElement);
        public virtual void SetElementCount(int elementCount)
        {

        }
        public enum Anchor
        {
            TopLeft, TopRight,
            BottomLeft, BottomRight
        }
        public enum Axis
        {
            Horizontal = 1, 
            Vertical = 2,
        }
    }
}
