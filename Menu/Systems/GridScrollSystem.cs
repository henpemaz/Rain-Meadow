using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Systems
{
    public class GridScrollSystem : ScrollSystem
    {
        public int elementCountInOppositeAxis = 1;
        public Vector2 _elementSpacing, _elementSize;
        public bool _startEndWithSpacingInAxis, _automaticallyCalculateElementOppositeSize;
        public float cachedVisibleItemsShown;
        public int elementCount;
        public float ElementSpacingInAxis => ElementSpacing[IndexToRef];
        public float ElementSizeInAxis => ElementSize[IndexToRef];
        public Vector2 ElementSpacing
        {
            get => _elementSpacing;
            set
            {
                if (_elementSpacing == value) return;
                bool shouldRefresh = _elementSpacing[IndexToRef] != value[IndexToRef];
                _elementSpacing = value;
                if (shouldRefresh)
                    CacheMaxVisibleItemsShownInAxis();
                MarkDirtyForScrollObjects?.Invoke();
            }
        }
        public Vector2 ElementSize
        {
            get => _elementSize;
            set
            {
                if (_elementSize == value) return;
                bool shouldRefresh = ElementCountInOppositeAxis != -1 ||  _elementSize[IndexToRef] != value[IndexToRef];
                _elementSize = value;
                if (shouldRefresh)
                    CacheMaxVisibleItemsShownInAxis();
                MarkDirtyForScrollObjects?.Invoke();
            }
        }
        public bool AutomaticallyCalculateElementOppositeSize
        {
            get => _automaticallyCalculateElementOppositeSize;
            set
            {
                if (_automaticallyCalculateElementOppositeSize == value) return;
                _automaticallyCalculateElementOppositeSize = value;
                if (ElementCountInOppositeAxis != 1 && AutomaticallyCalculateElementOppositeSize)
                    AdjustElementOppositeAxisSize();
            }
        }
        public bool StartEndWithSpacingInAxis
        {
            get =>_startEndWithSpacingInAxis;
            set
            {
                if (_startEndWithSpacingInAxis == value) return;
                _startEndWithSpacingInAxis = value;
                CacheMaxVisibleItemsShownInAxis();
                MarkDirtyForScrollObjects?.Invoke();
            }
        }
        public int ElementCountInOppositeAxis
        {
            get => elementCountInOppositeAxis;
            set
            {
                int newVal = Mathf.Max(1, value);
                if (elementCountInOppositeAxis != newVal)
                elementCountInOppositeAxis = newVal;
                CacheMaxVisibleItemsShownInAxis();
                MarkDirtyForScrollObjects?.Invoke();
            }
        }
        public GridScrollSystem(Vector2 elementSize, Vector2 elementSpacing, int visibleElementsShownInAxis, int visibleElementsInOppositeAxis = 1, bool startEndWithSpacing = false, Axis scrollingAxis = Axis.Vertical) : this(scrollingAxis)
        {
            _elementSize = elementSize;
            _elementSpacing = elementSpacing;
            _startEndWithSpacingInAxis = startEndWithSpacing;
            elementCount = Mathf.Max(1, visibleElementsShownInAxis);
            elementCountInOppositeAxis = Mathf.Max(1, visibleElementsInOppositeAxis);
        }
        public GridScrollSystem(Axis scrollingAxis = Axis.Vertical) : base(scrollingAxis)
        {
            OnViewSizeChanged += ViewSizeChanged;
        }
        public Vector2 CalculateCustomViewSize()
        {
            Vector2 newViewSize = new(0, 0);
            int index = IndexToRef, oppIndex = OppositeIndexToRef;
            newViewSize[index] = ButtonScroller.CalculateHeightBasedOnAmtOfButtons(elementCount, ElementSize[index], ElementSpacing[index], StartEndWithSpacingInAxis);
            newViewSize[oppIndex] = ButtonScroller.CalculateHeightBasedOnAmtOfButtons(ElementCountInOppositeAxis, ElementSize[oppIndex], ElementSpacing[index]);
            return newViewSize;
        }
        public Vector2 CalculatePositionOnHorizontalMode(int index, (Vector2, Vector2) posSizeOfElement, (Vector2, Vector2)? posSizeOfPrevElement)
        {
            var (elementOrigPos, elementSize) = posSizeOfElement;
            int indexInAxis = index / ElementCountInOppositeAxis;
            int indexInOppositeAxis = index % ElementCountInOppositeAxis;
            int prevIndexInOppositeAxis = (index - 1) % ElementCountInOppositeAxis;
            int firstElementSpacingMultipler = StartEndWithSpacingInAxis ? 0 : 1;
            float posX = elementOrigPos.x;
            float posY = elementOrigPos.y;
            if (IsHorizontal)
            {
                if (ElementCountInOppositeAxis != 1)
                {
                    if (ScrollStartsFromTop)
                        posY = ViewSize.y - (indexInOppositeAxis + 1) * elementSize.y - ElementSpacing.y * (indexInOppositeAxis + firstElementSpacingMultipler);
                    else
                        posY = indexInOppositeAxis * (elementSize.y + ElementSpacing.y);
                }
                if (ScrollStartsFromLeft)
                    posX = posSizeOfPrevElement != null ? posSizeOfPrevElement.Value.Item1.y - ElementSpacing.y - elementSize.y :
                        ViewSize.y - elementSize.y - (ElementSpacing.y * firstElementSpacingMultipler);
                else
                    posX = posSizeOfPrevElement != null ? posSizeOfPrevElement.Value.Item1.y - ElementSpacing.y - elementSize.y :
                            indexInAxis * elementSize.y + ElementSpacing.y * (firstElementSpacingMultipler + indexInAxis);
            }
            return new(posX, posY);
        }
        public Vector2 CalculatePositionOnVerticalMode(int index, (Vector2, Vector2) posSizeOfElement, (Vector2, Vector2)? posSizeOfPrevElement)
        {
            var (elementOrigPos, elementSize) = posSizeOfElement;
            int indexInAxis = index / ElementCountInOppositeAxis;
            int indexInOppositeAxis = index % ElementCountInOppositeAxis;
            int firstElementSpacingMultipler = StartEndWithSpacingInAxis ? 1 : 0;
            float boundaryOffset = ElementSpacing.y * firstElementSpacingMultipler;
            float posX = elementOrigPos.x;
            float posY = elementOrigPos.y;

            float elementSizeY = elementSize.y;
            float elementSpacingY = ElementSpacing.y;
            float viewSizeEndYPos = ViewSize.y - boundaryOffset;
            float viewSizeStartYPos = boundaryOffset;

            if (ElementCountInOppositeAxis != 1)
            {
                if (ScrollStartsFromLeft)
                    posX = indexInOppositeAxis * (elementSize.x + ElementSpacing.x);
                else
                    posX = (ElementCountInOppositeAxis - 1 - indexInOppositeAxis) * (elementSize.x + ElementSpacing.x);
            }

            if (ScrollStartsFromTop)
            {
                if (posSizeOfPrevElement != null)
                    posY = posSizeOfPrevElement.Value.Item1.y - elementSpacingY - elementSizeY;
                else posY = viewSizeEndYPos - elementSize.y * (indexInAxis + 1) - elementSpacingY * indexInAxis;
            }
            else
            {
                if (posSizeOfPrevElement != null)
                    posY = posSizeOfPrevElement.Value.Item1.y - elementSpacingY - elementSizeY;
                else
                    posY = viewSizeStartYPos + (elementCount - indexInAxis - 1) * (elementSizeY + elementSpacingY);
            }
            return new(posX, posY);
        }
        public void ViewSizeChanged()
        {
            CacheMaxVisibleItemsShownInAxis();
            if (ElementCountInOppositeAxis != 1 && AutomaticallyCalculateElementOppositeSize)
                AdjustElementOppositeAxisSize();
        }
        public void AdjustElementOppositeAxisSize()
        {
            int oppIndex = OppositeIndexToRef;
            var elementCountInoppAxis = ElementCountInOppositeAxis;
            var sizeToRefer = ViewSize[oppIndex] - (elementCountInoppAxis - 1) * ElementSpacing[oppIndex];
            var newElementSize = ElementSize;
            newElementSize[oppIndex] = sizeToRefer / elementCountInoppAxis;
            ElementSize = newElementSize;
        }
        public void CacheMaxVisibleItemsShownInAxis()
        {
            float offSet = _startEndWithSpacingInAxis ? 1 : -1;
            int indexToRef = IndexToRef;
            cachedVisibleItemsShown = Mathf.Max(0, (ViewSize[indexToRef] - ElementSpacing[indexToRef] * offSet) / Mathf.Max(1, (ElementSize[indexToRef] + ElementSpacing[indexToRef])));
        }
        public void SetElementCount(int elementCount)
        {
            if (this.elementCount == elementCount) return;
            this.elementCount = elementCount;
            MarkDirtyForScrollObjects?.Invoke();
        }
        public override float GetMaxScroll()
        {
            float maxScroll = Mathf.Max(elementCount - cachedVisibleItemsShown, 0);
            return maxScroll;
        }
        public override Vector2 SizeOfElement(int index, Vector2 origSizeOfElement)
        {
            float sizeX = ElementCountInOppositeAxis == 1 && !IsHorizontal? origSizeOfElement.x : _elementSize.x;
            float sizeY = ElementCountInOppositeAxis == 1 && IsHorizontal? origSizeOfElement.y : _elementSize.y;
            return new(sizeX, sizeY);
        }
        public override Vector2 NormalPositionOfElement(int index, (Vector2, Vector2) posSizeOfElement, (Vector2, Vector2)? posSizeOfPrevElement)
        {
            if (IsHorizontal)
                return CalculatePositionOnHorizontalMode(index, posSizeOfElement, posSizeOfPrevElement);
            return CalculatePositionOnVerticalMode(index, posSizeOfElement, posSizeOfPrevElement);
        }
        public override Vector2 ScrollOffsetToPosOffset(float scrollOffset)
        {
            Vector2 elementSpacing = new(0, 0), elementSize = new(0, 0);
            elementSpacing[IndexToRef] = ElementSpacing[IndexToRef];
            elementSize[IndexToRef] = ElementSize[IndexToRef];
            return base.ScrollOffsetToPosOffset(scrollOffset) * (elementSpacing + elementSize);
        }
    }
}
