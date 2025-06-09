using System;
using System.Collections.Generic;
using System.Linq;
using Menu;
using UnityEngine;
using HarmonyLib;
using RWCustom;
using RainMeadow.UI.Components.Patched;
using RainMeadow.UI.Interfaces;
using RainMeadow.UI.Components;

namespace RainMeadow
{
    //a scroller just for predetermined buttons, intended for buttons' owner to be ButtonScroller, rn has predetermined height and spacing
    public class ButtonScroller : RectangularMenuObject, Slider.ISliderOwner, IPLEASEUPDATEME
    {
        public static float CalculateHeightBasedOnAmtOfButtons(int amtOfButtonsView, float buttonHeight, float spacing, bool startEndSpacing = false)
        {
            //remember it goes by buttonsize + button spacing not the buttonSpacing + buttonsize. button size plus first as there will be not extra spacing
            //unless....
            // startEndSpacing is true, then it will add spacing to the start and end instead of button height
            return startEndSpacing ? amtOfButtonsView * (buttonHeight + spacing) + spacing : buttonHeight + Mathf.Max(amtOfButtonsView - 1, 0) * (buttonHeight + spacing);
        }
        public float MaxVisibleItemsShown => (UpperBound - LowerBound) / ButtonHeightAndSpacing;
        public float MaxDownScroll => Mathf.Max(0, (buttons.Count - MaxVisibleItemsShown));
        public virtual float DownScrollOffset
        {
            get => desiredScrollOffset;
            set
            {
                desiredScrollOffset = value;
                DirectConstrainScroll();

            }
        }
        public float LowerBound => 0;
        public float UpperBound => size.y;
        public float ButtonHeightAndSpacing => buttonHeight + buttonSpacing;
        public float ScrollOffsetPos => scrollOffset * ButtonHeightAndSpacing;
        public bool CanScrollUp => desiredScrollOffset > 0;
        public bool CanScrollDown => desiredScrollOffset < MaxDownScroll;
        public bool IsHidden { get; set; }
        public ButtonScroller(Menu.Menu menu, MenuObject owner, Vector2 pos, int amtOfButtonsToView, float listSizeX, (float, float) buttonHeightSpacing, bool sliderOnRight = false, Vector2 sliderPosOffset = default, float sliderSizeYOffset = 0, bool startEndWithSpacing = false) : 
            this(menu, owner, pos, new(listSizeX, CalculateHeightBasedOnAmtOfButtons(amtOfButtonsToView, buttonHeightSpacing.Item1, buttonHeightSpacing.Item2, startEndWithSpacing)), sliderOnRight, sliderPosOffset, sliderSizeYOffset)
        {
            this.startEndWithSpacing = startEndWithSpacing;
            buttonHeight = buttonHeightSpacing.Item1;
            buttonSpacing = buttonHeightSpacing.Item2;
        }
        public ButtonScroller(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, bool sliderOnRight = false, Vector2 sliderPosOffset = default, float sliderSizeYOffset = 0) : base(menu, owner, pos, size)
        {
            myContainer = new();
            (owner?.Container ?? menu.container).AddChild(myContainer);
            sliderIsOnRightSide = sliderOnRight;
            desiredScrollOffset = 0;
            //slider sprite xoffset is 15
            scrollSlider = new(menu, this, "Scroller", sliderPosOffset + new Vector2(sliderOnRight? size.x : -32, 0), new Vector2(30, size.y + sliderSizeYOffset), new("BUTTONSCROLLER_SCROLLSLIDER"), true);
            subObjects.Add(scrollSlider);
        }
        public override void RemoveSprites()
        {
            myContainer?.RemoveFromContainer();
            sideButtonLines.Do(x => x.RemoveFromContainer());
            base.RemoveSprites();
            RemoveAllButtons();
        }
        public override void Update()
        {
            base.Update();
            if (!IsHidden && MouseOver && menu.manager.menuesMouseMode) ScrollingUpdate(menu.mouseScrollWheelMovement);
            
            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].Size = new(buttons[i].Size.x, buttonHeight);
                buttons[i].Pos = new(buttons[i].Pos.x, GetIdealYPosWithScroll(i));
                buttons[i].Alpha = GetAmountOfAlphaByCrossingBounds(buttons[i].Pos);
            }
            float currentScrollOffset = GetCurrentScrollOffset();
            scrollOffset = Custom.LerpAndTick(scrollOffset, currentScrollOffset, 0.01f, 0.01f);
            floatScrollSpeed *= Custom.LerpMap(Math.Abs(currentScrollOffset - scrollOffset), 0.25f, 1.5f, 0.45f, 0.99f);
            floatScrollSpeed += Mathf.Clamp(currentScrollOffset - scrollOffset, -2.5f, 2.5f) / 2.5f * 0.15f;
            floatScrollSpeed = Mathf.Clamp(floatScrollSpeed, -maxScrollSpeed, maxScrollSpeed);
            scrollOffset += floatScrollSpeed;

            scrollSliderValueCap = Custom.LerpAndTick(scrollSliderValueCap, MaxDownScroll, scrollSliderCapLerp, buttons.Count / 30);

            if (MaxDownScroll == 0) scrollSliderValue = Custom.LerpAndTick(scrollSliderValue, sliderDefaultIsDown? 1 : 0, scrollSliderCapLerp, scrollSliderCapTick);
            else scrollSliderValue = Custom.LerpAndTick(scrollSliderValue, Mathf.InverseLerp(0f, scrollSliderValueCap, scrollOffset), isScrolling?  Mathf.Max(0.9f, scrollSliderCapLerp) : scrollSliderCapLerp, scrollSliderCapTick);

            if (isScrolling && scrollOffset == currentScrollOffset) isScrolling = false;

            scrollSlider.buttonBehav.greyedOut = greyOutWhenNoScroll && MaxDownScroll == 0;
            if (scrollDownButton != null) scrollDownButton.buttonBehav.greyedOut = !CanScrollDown;
            if (scrollUpButton != null) scrollUpButton.buttonBehav.greyedOut = !CanScrollUp;
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            for (int i = 0; i < sideButtonLines.Length; i++)
            {
                float bottomY = (i != 0) ? (sideButtons[i - 1].DrawY(timeStacker) + sideButtons[i - 1].DrawSize(timeStacker).y + 0.01f) : (DrawY(timeStacker) + scrollSlider.anchorPoint.y),
                    topY = (i != sideButtonLines.Length - 1) ? (sideButtons[i].DrawY(timeStacker) + 0.01f) : (DrawY(timeStacker) + DrawSize(timeStacker).y + (20 - (size.y - scrollSlider.length) + scrollSlider.anchorPoint.y));
                sideButtonLines[i].x = DrawX(timeStacker) + (sliderIsOnRightSide? scrollSlider.pos.x - 15 : size.x - (scrollSlider.pos.x + 17));
                sideButtonLines[i].y = bottomY;
                sideButtonLines[i].scaleY = topY - bottomY;
                sideButtonLines[i].color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey);
            }
        }
        public virtual float GetCurrentScrollOffset() => desiredScrollOffset;
        public void SliderSetValue(Slider slider, float f)
        {
            if (slider?.ID?.value == "BUTTONSCROLLER_SCROLLSLIDER")
            {
                scrollSliderValue = 1 - f;
                DownScrollOffset = scrollOffset = Mathf.Lerp(0f, scrollSliderValueCap, scrollSliderValue);
            }
        }
        public float ValueOfSlider(Slider slider)
        {
            if (slider?.ID?.value == "BUTTONSCROLLER_SCROLLSLIDER") return 1 - scrollSliderValue;
            return 0;
        }
        public void ScrollingUpdate(float yInput)
        {
            if ((yInput < 0 && CanScrollUp) || (yInput > 0 && CanScrollDown))
            {
                //scrolling up -, scrolling down +
                AddScroll(yInput);
                menu.PlaySound(SoundID.MENU_Scroll_Tick);
                isScrolling = true;
            }
        }
        public void AddScroll(float addDir)
        {
            DownScrollOffset += addDir;
        }
        public void ConstrainScroll() => DownScrollOffset = Mathf.Clamp(DownScrollOffset, 0, MaxDownScroll);
        protected void DirectConstrainScroll()  => desiredScrollOffset = Mathf.Clamp(desiredScrollOffset, 0, MaxDownScroll);//for direct scroll clamp, like for DownScrollOffset set method, this doesnt not update slider value
        public List<T> GetSpecificButtons<T>() => [.. buttons.OfType<T>()];
        public void RemoveButton(int index, bool constrainScroll = true) => RemoveButton(buttons.GetValueOrDefault(index), constrainScroll);
        public void RemoveButton(IPartOfButtonScroller? button, bool constrainScroll = true)
        {
            if (button != null)
            {
                if (button is MenuObject menuObj) this.ClearMenuObject(menuObj);
                buttons.Remove(button);
            }
            if (constrainScroll)
                ConstrainScroll();
        }
        public void RemoveAllButtons(bool constrainScroll = true)
        {
            this.ClearMenuObjectIList(buttons.Where(x => x is MenuObject).Cast<MenuObject>());
            buttons.Clear();
            if (constrainScroll)
                ConstrainScroll();
        }
        public void AddScrollObjects(params IPartOfButtonScroller[]? scrollBoxButtons) => AddScrollObjects(scrollBoxButtons, true, true);
        public void AddScrollObjects(IPartOfButtonScroller[]? scrollBoxButtons, bool addToSubobjects, bool bindToSlider)
        {
            if (scrollBoxButtons == null) return;
            List<IPartOfButtonScroller> newButtons = [.. scrollBoxButtons.Where(x => x != null)];
            buttons.AddRange(newButtons);
            foreach (MenuObject menuObj in scrollBoxButtons.OfType<MenuObject>())
            {
                if (addToSubobjects)
                    subObjects.Add(menuObj);
                if (bindToSlider)
                    menuObj.TryBind(scrollSlider, !sliderIsOnRightSide, sliderIsOnRightSide);
            }
        }
        public Vector2 GetIdealNormalPosForButton(int index)
        {
            return new(0, UpperBound - ((startEndWithSpacing? ButtonHeightAndSpacing : buttonHeight) + (index * ButtonHeightAndSpacing)));
        }
        public Vector2 GetIdealPosWithScrollForButton(int index) => new(GetIdealNormalPosForButton(index).x, GetIdealYPosWithScroll(index));
        public virtual float GetIdealYPosWithScroll(int index) => Math.Min(UpperBound + (ButtonHeightAndSpacing / 3), Mathf.Max(LowerBound - (ButtonHeightAndSpacing / 3), GetIdealNormalPosForButton(index).y + ScrollOffsetPos));
        public virtual float GetAmountOfAlphaByCrossingBounds(Vector2 combinedPos)
        {
            //if button starts crossing the bound, calculate the alpha else alpha = 1
            float combinedPosY = combinedPos.y;
            return combinedPosY < LowerBound ? Mathf.InverseLerp(LowerBound - (ButtonHeightAndSpacing / 3), LowerBound, combinedPosY) : combinedPosY + buttonHeight > UpperBound ? Mathf.InverseLerp(UpperBound + (ButtonHeightAndSpacing / 3), UpperBound, combinedPosY + buttonHeight) : 1;
        }
        public void AddScrollUpDownButtons(float scrollButtonWidth = 24, float upButtonYPosOffset = 10, float downButtonYPosOffset = -34f)
        {
            if (scrollUpButton == null)
            {
                scrollUpButton = new(menu, this, new Vector2(size.x / 2f - scrollButtonWidth / 2f, size.y + upButtonYPosOffset), 0, scrollButtonWidth);
                scrollUpButton.OnClick += _ => AddScroll(-1);
            }
            if (scrollDownButton == null)
            {
                scrollDownButton = new(menu, this, new Vector2(scrollUpButton.pos.x, downButtonYPosOffset), 2, scrollButtonWidth);
                scrollDownButton.OnClick += _ => AddScroll(1);
            }
            this.SafeAddSubobjects(scrollUpButton, scrollDownButton);
        }
        public SideButton AddSideButton(string symbolName, string text = "", string description = "", string signal = "")
        {
            SideButton btn = new(menu, this, new Vector2(size.x + 7f, 14f + 30f * sideButtons.Count), symbolName, text, description, signal);
            sideButtons.Add(btn);
            subObjects.Add(btn);

            CreateSideButtonLines();
            return btn;
        }
        public void CreateSideButtonLines()
        {
            for (int i = 0; i < sideButtonLines.Length; i++) Container.RemoveChild(sideButtonLines[i]);
            sideButtonLines = new FSprite[sideButtons.Count + 1];
            for (int i = 0; i < sideButtonLines.Length; i++)
            {
                sideButtonLines[i] = new("pixel")
                {
                    anchorX = 0,
                    anchorY = 0,
                    scaleX = 2
                };
                Container.AddChild(sideButtonLines[i]);
                sideButtonLines[i].MoveToBack();
            }
        }

        public bool sliderDefaultIsDown, greyOutWhenNoScroll, startEndWithSpacing;
        protected bool sliderIsOnRightSide, isScrolling;
        protected float desiredScrollOffset, scrollOffset, floatScrollSpeed, scrollSliderValue, scrollSliderValueCap;
        public float buttonSpacing, buttonHeight = 30, maxScrollSpeed = 1.2f, scrollSliderCapLerp = 0.02f, scrollSliderCapTick = 0.05f;
        public PatchedVerticalSlider scrollSlider;
        public EventfulScrollButton? scrollUpButton, scrollDownButton;
        public List<IPartOfButtonScroller> buttons = [];
        public List<SideButton> sideButtons = [];
        public FSprite[] sideButtonLines = [];
        public class SideButton : SimplerSymbolButton
        {
            public SideButton(Menu.Menu menu, MenuObject owner, Vector2 pos, string symbolName, string text, string description, string signal = "") : base(menu, owner, symbolName, signal, pos)
            {
                this.description = description;
                label = new(menu, this, text, new Vector2(34f, -3f), new Vector2(0f, 30f), false);
                label.label.alignment = FLabelAlignment.Left;
                subObjects.Add(label);
            }
            public override void Update()
            {
                base.Update();
                lastLabelFade = labelFade;
                labelFade = Selected ? Custom.LerpAndTick(labelFade, 0.33f, 0.04f, 1f / 60f) : Custom.LerpAndTick(labelFade, 0f, 0.04f, 1f / 60f);
                OnUpdate?.Invoke(this);
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                label.label.alpha = Mathf.Lerp(lastLabelFade, labelFade, timeStacker);
            }
            public override void Clicked() => OnClick?.Invoke(this);

            public MenuLabel label;
            public event Action<SideButton>? OnUpdate;
            public float labelFade, lastLabelFade;
            public new event Action<SideButton>? OnClick;
        }
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
