using System;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    //now includes the first button as another button to view
    public class ButtonSelector : SimplerButton
    {
        public float OrigDistanceBetweenButtonYPos => size.y + buttonSpacing;
        public float ListPosOffset => downwardsList ? -(OrigDistanceBetweenButtonYPos + listDownUpYOffset) : OrigDistanceBetweenButtonYPos + listDownUpYOffset;
        public float OrigStart => downwardsList ? -sizeOfList : size.y;
        public float StartingPoint => OrigStart + ListPosOffset;
        public ButtonSelector(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, int amtOfButtonsView, float spacingOfButton) : this(menu, owner, displayText, pos, size, (Mathf.Max(0, amtOfButtonsView - 1) * (size.y + spacingOfButton)), spacingOfButton)
        {
            //if you set it to one or less... why???
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
        public float listDownUpYOffset;
        public float sizeOfList, buttonSpacing;
        public ButtonScroller? scroller;
        public Func<ButtonSelector,ButtonScroller, ButtonScroller.IPartOfButtonScroller[]>? populateList;
    }
}