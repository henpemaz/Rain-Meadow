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
        public bool startEndWithSpacing;
        public float cachedVisibleItemsShown;
        public int elementCount;
        public Vector2 ElementSpacing
        {
            get => _elementSpacing;
            set
            {
                if (_elementSpacing == value) return;
                _elementSpacing = value;
                cachedVisibleItemsShown = MaxVisibleItemsShown();
            }
        }
        public Vector2 ElementSize
        {
            get => _elementSize;
            set
            {
                if (_elementSize == value) return;
                _elementSize = value;
                cachedVisibleItemsShown = MaxVisibleItemsShown();
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
                MarkDirtyForScrollObjects?.Invoke();
            }
        }
        public override float ContentSize 
        { 
            get
            {
                return 0;
            }
            set
            {
            }
        }
        public GridScrollSystem(Axis scrollingAxis = Axis.Vertical) : base(scrollingAxis)
        {
            OnViewSizeChanged += () => cachedVisibleItemsShown = MaxVisibleItemsShown();
        }
        public float MaxVisibleItemsShown()
        {
            float offSet = startEndWithSpacing ? 1 : -1;
            int indexToRef = IndexToRef;
            return (ViewSize[indexToRef] - ElementSpacing[indexToRef] * offSet) / Mathf.Max(1, (ElementSize[indexToRef] + ElementSpacing[indexToRef]));
        }
        public override float GetMaxScroll()
        {
            return elementCount - cachedVisibleItemsShown;
        }
        public override Vector2 SizeOfElement(int index, Vector2 origSizeOfElement)
        {
            float sizeX = ElementCountInOppositeAxis == 1 && !IsHorizontal? origSizeOfElement.x : _elementSize.x;
            float sizeY = ElementCountInOppositeAxis == 1 && IsHorizontal? origSizeOfElement.y : _elementSize.y;
            return _elementSize;
        }
        public override Vector2 NormalPositionOfElement(int index, (Vector2, Vector2) posSizeOfElement, (Vector2, Vector2)? posSizeOfPrevElement)
        {
            var (elementOrigPos, elementSize) = posSizeOfElement;
            int indexInAxis = index / elementCountInOppositeAxis;
            int indexInOppositeAxis = index % elementCountInOppositeAxis;
            int prevIndexInOppositeAxis = (index - 1) % elementCountInOppositeAxis;
            int firstElementSpacingMultipler = startEndWithSpacing ? 0 : 1;
            float posX = elementOrigPos.x;
            float posY = elementOrigPos.y;
            if (IsHorizontal)
            {
                if (ElementCountInOppositeAxis != 1)
                {
                    if (ScrollStartsFromTop)
                        posY = ViewSize.y - (indexInOppositeAxis + 1) * elementSize.y - _elementSpacing.y * (indexInOppositeAxis + firstElementSpacingMultipler);
                    else
                        posY = indexInOppositeAxis * (elementSize.y + _elementSpacing.y);
                }
                if (ScrollStartsFromLeft)
                    posX = posSizeOfPrevElement != null ? posSizeOfPrevElement.Value.Item1.y - _elementSpacing.y - elementSize.y :
                        ViewSize.y - elementSize.y - (_elementSpacing.y * firstElementSpacingMultipler);
                else
                    posX = posSizeOfPrevElement != null ? posSizeOfPrevElement.Value.Item1.y - _elementSpacing.y - elementSize.y :
                            indexInAxis * elementSize.y + _elementSpacing.y * (firstElementSpacingMultipler + indexInAxis);
            }
            else
            {
                if (ElementCountInOppositeAxis != 1)
                {
                    if (ScrollStartsFromLeft)
                        posX = indexInOppositeAxis * (elementSize.x + _elementSpacing.x);
                    else
                        posX = (elementCountInOppositeAxis - 1 - indexInOppositeAxis) * (elementSize.x + _elementSpacing.x);
                }

                if (ScrollStartsFromTop)
                    posY = posSizeOfPrevElement != null ? posSizeOfPrevElement.Value.Item1.y - _elementSpacing.y - elementSize.y :
                        ViewSize.y - elementSize.y - (_elementSpacing.y * firstElementSpacingMultipler);
                else
                    posY = posSizeOfPrevElement != null ? posSizeOfPrevElement.Value.Item1.y - _elementSpacing.y - elementSize.y :
                            indexInAxis * elementSize.y + _elementSpacing.y * (firstElementSpacingMultipler + indexInAxis);
            }
            RainMeadow.Debug($"Index: {index}, elementPos: {posX}, {posY}");
            return new(posX, posY);
        }
        public override void SetElementCount(int elementCount)
        {
            if (elementCount == this.elementCount) return;
            this.elementCount = elementCount;
        }
    }
}
