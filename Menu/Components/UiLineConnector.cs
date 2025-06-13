using Menu;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class UiLineConnector : PositionedMenuObject //must be positioned menu object for owner and other obj, meant to be this way. currently only allows horizontal and vertical, not diag
    {
        public UiLineConnector(Menu.Menu menu, PositionedMenuObject owner, PositionedMenuObject secondObj, bool vertical) : base(menu, owner, Vector2.zero)
        {
            this.vertical = vertical;
            secondMenuObj = secondObj;
            lineConnector = new("pixel");
            Container.AddChild(lineConnector);
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            lineConnector.RemoveFromContainer();
        }
        public override void Update()
        {
            base.Update();
            lineConnector.anchorX = vertical? 0.5f : 0;
            lineConnector.anchorY = vertical? 0 : 0.5f;
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 newPos = GetStartPos(timeStacker);
            pos = new Vector2(newPos.x / (vertical ? 2 : 1), newPos.y / (vertical ? 1 : 2)) + posOffset;
            Vector2 linePos = DrawPos(timeStacker), scale = GetPosDifference(timeStacker);
            lineConnector.x = linePos.x;
            lineConnector.y = linePos.y;
            lineConnector.scaleX = vertical? widthThickness : scale.x;
            lineConnector.scaleY = vertical ? scale.y : widthThickness;
        }
        public void MoveLineSpriteBeforeNode(FNode? node)
        {
            if (node == null)
            {
                return;
            }
            lineConnector.MoveBehindOtherNode(node);
        }
        public Vector2 GetStartPos(float timeStacker)
        {
            return GetOwnerSize(timeStacker);
        }
        public Vector2 GetOwnerSize(float timeStacker)
        {
            return owner is RectangularMenuObject rectangular? rectangular.DrawSize(timeStacker) : Vector2.zero;
        }
        public Vector2 GetPosDifference(float timeStacker)
        {
            return secondMenuObj.ScreenPos - (owner is PositionedMenuObject posObj ? posObj.ScreenPos + GetOwnerSize(timeStacker): Vector2.zero);
        }
        public float widthThickness = 2;
        public bool vertical;  //vertical makes starting pos start from top of owner
        public Vector2 posOffset;
        public FSprite lineConnector;
        public PositionedMenuObject secondMenuObj;
    }
}
