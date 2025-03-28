using System;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class ButtonSelector : SimplerButton
    {
        public float StartingPoint => (downwardsList ? -sizeOfList : size.y) + (downwardsList? -size.y : size.y);
        public ButtonSelector(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, int amtOfButtonsView,float spacingOfButton) : this(menu, owner, displayText, pos, size, amtOfButtonsView * size.y, spacingOfButton)
        {
        }
        public ButtonSelector(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, float listSize, float spacingOfButton) : base(menu, owner, displayText, pos, size, "Press the button to open a list to select")
        {
            sizeOfList = listSize;
            buttonSpacing = spacingOfButton;
            OnClick += (_) =>
            {
                OpenCloseList();
            };
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            this.ClearMenuObject(ref scroller);
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (scroller != null)
            {
                scroller.buttonHeight = size.y;
                scroller.buttonSpacing = buttonSpacing;
            }
        }
        public virtual void UpdateUponClosingList()
        {

        }
        public virtual void OpenCloseList()
        {
            if (scroller == null)
            {
                scroller = new(menu, this, new(0, StartingPoint), new(size.x, sizeOfList));
                scroller.AddScrollObjects(populateList?.Invoke(this, scroller));
                subObjects.Add(scroller);
                return;
            }
            this.ClearMenuObject(ref scroller);
            UpdateUponClosingList();
        }

        public bool downwardsList = true;
        public float sizeOfList, buttonSpacing;
        public ButtonScroller? scroller;
        public Func<ButtonSelector,ButtonScroller, ButtonScroller.IPartOfButtonScroller[]>? populateList;
    }
}