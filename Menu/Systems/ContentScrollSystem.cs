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
        public Vector2 elementSpacing = new(0, 0);
        public ContentScrollSystem(Axis scrollingAxis = Axis.Vertical) : base(scrollingAxis)
        {

        }
        public override Vector2 NormalPositionOfElement(int index, ValueTuple<Vector2, Vector2> posSizeOfElement, ValueTuple<Vector2, Vector2>? posSizeOfPrevElement)
        {
            var (elementOrigPos, elementSize) = posSizeOfElement;
            float posX = elementOrigPos.x;
            float posY = elementOrigPos.y;
            if (IsHorizontal)
            {
                    if (ScrollStartsFromLeft)
                    posX = posSizeOfPrevElement != null ? posSizeOfPrevElement.Value.Item1.x + posSizeOfPrevElement.Value.Item2.x + elementSpacing.x : 0;
                else
                    posX = posSizeOfPrevElement != null ? posSizeOfPrevElement.Value.Item1.x - elementSpacing.x - elementSize.x  : (ViewSize.x - elementSize.x);
            }
            else
            {
                if (ScrollStartsFromTop)
                    posY = posSizeOfPrevElement != null ? posSizeOfPrevElement.Value.Item1.y - elementSpacing.y - elementSize.y : ViewSize.y - elementSize.y;
                else
                    posY = posSizeOfPrevElement != null ? posSizeOfPrevElement.Value.Item1.y + posSizeOfPrevElement.Value.Item2.y + elementSpacing.y : 0;
            }
            return new Vector2(posX, posY);
        }
        public override Vector2 SizeOfElement(int index, Vector2 origSizeOfElement)
        {
            return origSizeOfElement;
        }
    }
}
