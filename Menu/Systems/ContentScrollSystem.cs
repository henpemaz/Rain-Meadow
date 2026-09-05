using RainMeadow.UI.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Systems
{
    public class ContentScrollSystem : ScrollSystem
    {
        public event Action? OnContentSizeChanged;
        public float ContentSize
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
        public ContentScrollSystem(Axis scrollingAxis = Axis.Vertical) : base(scrollingAxis)
        {

        }
        public override float GetMaxScroll()
        {
            return  Mathf.Max(ContentSize - ViewSize[IndexToRef], 0);
        }
        public override Vector2 NormalPositionOfElement(int index, ValueTuple<Vector2, Vector2> posSizeOfElement, ValueTuple<Vector2, Vector2>? posSizeOfPrevElement)
        {
            var (elementOrigPos, elementSize) = posSizeOfElement;
            float posX = elementOrigPos.x;
            float posY = elementOrigPos.y;
            if (IsHorizontal)
            {
                    if (ScrollStartsFromLeft)
                    posX = posSizeOfPrevElement != null ? posSizeOfPrevElement.Value.Item1.x + posSizeOfPrevElement.Value.Item2.x : 0;
                else
                    posX = posSizeOfPrevElement != null ? posSizeOfPrevElement.Value.Item1.x - elementSize.x  : (ViewSize.x - elementSize.x);
            }
            else
            {
                if (ScrollStartsFromTop)
                    posY = posSizeOfPrevElement != null ? posSizeOfPrevElement.Value.Item1.y - elementSize.y : ViewSize.y - elementSize.y;
                else
                    posY = posSizeOfPrevElement != null ? posSizeOfPrevElement.Value.Item1.y + posSizeOfPrevElement.Value.Item2.y : 0;
            }
            return new Vector2(posX, posY);
        }
        public override Vector2 SizeOfElement(int index, Vector2 origSizeOfElement)
        {
            return origSizeOfElement;
        }
    }
}
