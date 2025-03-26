using System;
using System.Linq;
using System.Collections.Generic;
using Menu;
using UnityEngine;
using RWCustom;
using System.Reflection;

namespace RainMeadow
{
    //a scroller just for predetermined buttons
    public class ButtonScroller(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, float buttonHeight = 30) : RectangularMenuObject(menu, owner, pos, size)
    {
        public int ItemCount => buttons.Count;
        public int MaxVisibleItems => (int)(size.y / buttonHeight);
        public int MaxScroll => Math.Max(0, ItemCount - MaxVisibleItems);
        public float LowerBound => 0;
        public float UpperBound => size.y;
        public bool CanScrollUp => scrollPos > 0;
        public bool CanScrollDown => scrollPos < MaxScroll;
        public override void Update()
        {
            base.Update();
            if (MouseOver)
            {
                ScrollingUpdate(InputOverride.GetMenuMouseWheelInput()); //not sure the extent of controller users
            }
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            for (int i = 0; i < buttons?.Count; i++)
            {
                buttons[i].fade = PercentageOverYBound(buttons[i].pos.y);
            }

        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
        }
        public int IndexFromButton(ScrollerButton button)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] == button)
                {
                    return i;
                }
            }
            return -1;
        }
        public void RemoveButton(int index)
        {
            if (buttons.Count > index)
            {
                buttons[index].RemoveSprites();
                subObjects.Remove(buttons[index]);
                buttons.RemoveAt(index);
                ConstrainScroll();
            }
        }
        public void RemoveAllButtons()
        {
            foreach (ScrollerButton button in buttons)
            {
                button.RemoveSprites();
                subObjects.Remove(button);
            }
            buttons.Clear();
        }
        public void AddButtons(params ScrollerButton[] scrollBoxButtons)
        {
            IEnumerable<ScrollerButton> newButtons = scrollBoxButtons.Where(x => x != null);
            buttons.AddRange(newButtons);
            subObjects.AddRange(newButtons.Where(x => !subObjects.Contains(x)));
            ConstrainScroll();
        }
        public float StepsDownOfItem(int itemIndex)
        {
            float num = 0f;
            for (int i = 0; i <= Math.Min(itemIndex, buttons.Count - 1); i++)
            {
                num += (i > 0) ? Mathf.Pow(Custom.SCurve(1f, 0.3f), 0.5f) : 1f;
            }

            return num;
        }
        public float GetIdealButtonYPos(int index)
        {
            if (buttons?.Count > index)
            {
                float num = StepsDownOfItem(index);
                num -=  scrollPos;
                return size.y - num * buttons[index].pos.y;
            }
            return 0;
        }
        public void ScrollingUpdate(float yInput)
        {
            if ((yInput < 0 && CanScrollUp) || (yInput > 0 && CanScrollDown))
            {
                //scrolling up -, scrolling down +
                AddScroll(yInput);
                menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Select_Gamepad_Or_Keyboard);
            }
        }
        public void AddScroll(float addDir)
        {
            scrollPos += addDir;
            ConstrainScroll();
        }
        public void ConstrainScroll()
        {
            scrollPos = Mathf.Clamp(scrollPos, 0, MaxScroll);
        }
        public float PercentageOverYBound(float posY)
        {
            float topPart = posY + buttonHeight;
            return (posY < LowerBound) ? 1 - Math.Min(1, (LowerBound - posY) / buttonHeight) :
                topPart > UpperBound ? 1 - Math.Min(1, (topPart - UpperBound) / buttonHeight) : 1;

        }

        public float buttonHeight = buttonHeight, scrollPos;
        public List<ScrollerButton> buttons = [];
        public Action<ButtonScroller,ScrollerButton, int> onScrollButtonListClick;
        public class ScrollerButton : SimplerButton
        {
            public ScrollerButton(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, displayText, pos, size)
            {
                OnClick += (_) =>
                {

                    if (this.owner is ButtonScroller bSB && bSB.onScrollButtonListClick != null)
                    {
                        int index = bSB.IndexFromButton(this);
                        if (index > -1)
                        {
                            bSB.onScrollButtonListClick(bSB, this, index);
                        }
                        return;
                    }
                    onScrollBoxButtonClick?.Invoke(this);
                };
            }
            public override bool CurrentlySelectableNonMouse => fade >= 1 && base.CurrentlySelectableNonMouse;
            public override bool CurrentlySelectableMouse => fade >= 1 && base.CurrentlySelectableMouse; 
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                UpdateFade(fade);
            }
            public virtual void UpdateFade(float fade)
            {
                menuLabel.label.alpha = fade;
                for (int i = 0; i < roundedRect.sprites.Length; i++)
                {
                    roundedRect.sprites[i].alpha = fade;
                    roundedRect.fillAlpha = fade / 2;
                }
                for (int i = 0; i < selectRect.sprites.Length; i++)
                {
                    selectRect.sprites[i].alpha = fade;
                }
            }

            public float fade = 1;
            public Action<ScrollerButton> onScrollBoxButtonClick;
        }
    }
}
