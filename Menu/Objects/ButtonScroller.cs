using System;
using System.Collections.Generic;
using System.Linq;
using Menu;
using UnityEngine;
using HarmonyLib;
using RainMeadow.UI.Components;
using RainMeadow.UI.Components.Patched;

namespace RainMeadow
{
    //a scroller just for predetermined buttons, intended for buttons' owner to be ButtonScroller, rn has predetermined height and spacing
    public class ButtonScroller : RectangularMenuObject, Slider.ISliderOwner
    {
        public static float CalculateHeightBasedOnAmtOfButtons(int amtOfButtonsView, float buttonHeight, float spacing)
        {
            //remember it goes by buttonsize + button spacing not the buttonSpacing + buttonsize. button size plus first as there will be not extra spacing
            return buttonHeight + Mathf.Max(amtOfButtonsView - 1) * (buttonHeight + spacing);
        }
        public int ItemCount => buttons.Count;
        public float MaxVisibleItemsShown => (UpperBound - LowerBound) / ButtonHeightAndSpacing;
        public float MaxDownScroll => Mathf.Max(0, (ItemCount - MaxVisibleItemsShown) * ButtonHeightAndSpacing);
        public float DownScrollOffset
        {
            get => scrollOffset;
            set
            {
                scrollOffset = value;
                DirectConstrainScroll();

            }
        }
        public float LowerBound => 0;
        public float UpperBound => size.y;
        public float ScrollingMultipler => Mathf.Lerp(1, 15, Mathf.InverseLerp(30, 120, buttonHeight + buttonSpacing)) * buttons.Count / 2;
        public float ButtonHeightAndSpacing => buttonHeight + buttonSpacing;
        public bool CanScrollUp => scrollOffset > 0;
        public bool CanScrollDown => scrollOffset < MaxDownScroll;
        public ButtonScroller(Menu.Menu menu, MenuObject owner, Vector2 pos, int amtOfButtonsToView, float listSizeX, float heightOfButton, float buttonSpacing, bool sliderOnRight = false, Vector2 sliderPosOffset = default, float sliderSizeYOffset = 0) : this(menu, owner, pos, new(listSizeX, CalculateHeightBasedOnAmtOfButtons(amtOfButtonsToView, heightOfButton, buttonSpacing)), sliderOnRight, sliderPosOffset, sliderSizeYOffset)
        {
            buttonHeight = heightOfButton;
            this.buttonSpacing = buttonSpacing;
        }
        public ButtonScroller(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, bool sliderOnRight = false, Vector2 sliderPosOffset = default, float sliderSizeYOffset = 0) : base(menu, owner, pos, size)
        {
            sliderIsOnRightSide = sliderOnRight;
            scrollOffset = 0;
            scrollSlider = new(menu, this, "Scroller", sliderPosOffset + new Vector2(sliderOnRight? size.x + 30 : -30, 0), new Vector2(20, size.y + sliderSizeYOffset), new("BUTTONSCROLLER_SCROLLSLIDER"), true);
            subObjects.Add(scrollSlider);
            ConstrainScroll();
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            RemoveAllButtons();
        }
        public override void Update()
        {
            base.Update();
            if (MouseOver && menu.manager.menuesMouseMode)
            {
                ScrollingUpdate(menu.mouseScrollWheelMovement * ScrollingMultipler);
            }
            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].Size = new(buttons[i].Size.x, buttonHeight);
                buttons[i].Pos = new(buttons[i].Pos.x, GetIdealPosWithScrollForButton(i).y);
                buttons[i].Alpha = GetAmountOfAlphaByCrossingBounds(buttons[i].Pos);
            }
        }
        public void SliderSetValue(Slider slider, float f)
        {
            if (slider?.ID?.value == "BUTTONSCROLLER_SCROLLSLIDER")
            {
                //scrollSliderValue is the entire scroll avaliblity combined into 0-1, better if could adjust the slider size
                DownScrollOffset = Mathf.Lerp(MaxDownScroll, 0, f);
            }
        }
        public float ValueOfSlider(Slider slider)
        {
            if (slider?.ID?.value == "BUTTONSCROLLER_SCROLLSLIDER")
            {
                return MaxDownScroll > 0 ? Mathf.InverseLerp(MaxDownScroll, 0, scrollOffset) : sliderDefaultIsDown ? 0 : 1;
            }
            return 0;
        }
        public void ScrollingUpdate(float yInput)
        {
            if ((yInput < 0 && CanScrollUp) || (yInput > 0 && CanScrollDown))
            {
                //scrolling up -, scrolling down +
                AddScroll(yInput);
                menu.PlaySound(SoundID.MENU_Scroll_Tick);
            }
        }
        public void AddScroll(float addDir)
        {
            DownScrollOffset += addDir;
        }
        public void ConstrainScroll() //for clamping scroll, also updating slider
        {
            DownScrollOffset = Mathf.Clamp(DownScrollOffset, 0, MaxDownScroll);
        }
        protected void DirectConstrainScroll() //for direct scroll clamp, like for DownScrollOffset set method, this doesnt not update slider value
        {
            scrollOffset = Mathf.Clamp(scrollOffset, 0, MaxDownScroll);
        }
        public List<T> GetSpecificButtons<T>()
        {
            return [.. buttons.OfType<T>()];
        }
        public int IndexFromButton(IPartOfButtonScroller button)
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
        public void RemoveButton(int index, bool constrainScroll = true)
        {
            RemoveButton(buttons.GetValueOrDefault(index), constrainScroll);
        }
        public void RemoveButton(IPartOfButtonScroller? button, bool constrainScroll = true)
        {
            if (button != null)
            {
                if (button is MenuObject menuObj)
                {
                    this.ClearMenuObject(menuObj);
                }
                buttons.Remove(button);
            }
            if (constrainScroll)
            {
                ConstrainScroll();
            }
        }
        public void RemoveAllButtons(bool constrainScroll = true)
        {
            this.ClearMenuObjectIList(buttons.Where(x => x is MenuObject).Cast<MenuObject>());
            buttons.Clear();
            if (constrainScroll)
            {
                ConstrainScroll();
            }
        }
        public void AddScrollObjects(params IPartOfButtonScroller[]? scrollBoxButtons)
        {
            AddScrollObjects(scrollBoxButtons, true, true);
        }
        public void AddScrollObjects(IPartOfButtonScroller[]? scrollBoxButtons, bool addToSubobjects, bool bindToSlider)
        {
            if (scrollBoxButtons != null)
            {
                List<IPartOfButtonScroller> newButtons = [.. scrollBoxButtons.Where(x => x != null)];
                buttons.AddRange(newButtons);
                if (addToSubobjects)
                {
                    subObjects.AddDistinctRange(newButtons.Where(x => x is MenuObject).Cast<MenuObject>());
                }
                if (bindToSlider)
                {
                    buttons.DoIf(x => x is MenuObject, x => (x as MenuObject).TryBind(scrollSlider, !sliderIsOnRightSide, sliderIsOnRightSide));
                }

            }
        }
        public Vector2 GetIdealNormalPosForButton(int index)
        {
            return new(0, UpperBound - (buttonHeight + (index * (buttonSpacing + buttonHeight))));
        }
        public Vector2 GetIdealPosWithScrollForButton(int index)
        {
            Vector2 idealPos = GetIdealNormalPosForButton(index);
            return new(idealPos.x, Math.Min(UpperBound + (ButtonHeightAndSpacing / 3), Mathf.Max(LowerBound - (ButtonHeightAndSpacing / 3), idealPos.y + scrollOffset)));
        }
        public float GetAmountOfAlphaByCrossingBounds(Vector2 combinedPos)
        {
            //if button starts crossing the bound, calculate the alpha else alpha = 1
            float combinedPosY = combinedPos.y;
            return combinedPosY < LowerBound ? Mathf.InverseLerp(LowerBound - (ButtonHeightAndSpacing / 3), LowerBound, combinedPosY) : combinedPosY + buttonHeight > UpperBound ? Mathf.InverseLerp(UpperBound + (ButtonHeightAndSpacing / 3), UpperBound, combinedPosY + buttonHeight) : 1;
        }

        public bool sliderDefaultIsDown;
        private bool sliderIsOnRightSide;
        private float scrollOffset;
        public float buttonSpacing, buttonHeight = 30;
        public PatchedVerticalSlider scrollSlider;
        public List<IPartOfButtonScroller> buttons = [];
        public class ScrollerButton(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, string description = "") : SimplerButton(menu, owner, displayText, pos, size, description), IPartOfButtonScroller
        {
            public float Alpha { get; set; } = 1;
            public Vector2 Pos { get => pos; set => pos = value; }
            public Vector2 Size { get => size; set => size = value; }
            public override void Update()
            {
                base.Update();
                buttonBehav.greyedOut = forceGreyedOut;
            }
            public bool forceGreyedOut;
        }
        public interface IPartOfButtonScroller //allows other derived objects to be part of the button scroller
        {
            public float Alpha { get; set; }
            public Vector2 Pos { get; set; }
            public Vector2 Size { get; set; }
        }
    }
}
