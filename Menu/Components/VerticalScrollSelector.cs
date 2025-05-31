// maybe merge into ButtonScroller for a single unified scroller?
using System;
using System.Collections.Generic;
using Menu;
using RainMeadow.UI.Components.Patched;
using RWCustom;
using UnityEngine;

namespace RainMeadow.UI.Components;

public class VerticalScrollSelector : RectangularMenuObject, Slider.ISliderOwner
{
    public class SideButton : SimplerSymbolButton
    {
        public MenuLabel label;
        public event Action<SideButton>? OnUpdate;
        public float labelFade, lastLabelFade;
        public new event Action<SideButton>? OnClick;

        public SideButton(Menu.Menu menu, MenuObject owner, Vector2 pos, string symbolName, string text, string description) : base(menu, owner, symbolName, "", pos)
        {
            this.description = description;
            label = new MenuLabel(menu, this, text, new Vector2(34f, -3f), new Vector2(0f, 30f), false);
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
    }

    public List<ButtonScroller.IPartOfButtonScroller> scrollElements = [];
    public VerticalSlider slider;
    public EventfulScrollButton? scrollUpButton, scrollDownButton;
    public FSprite[] rightLines = [];
    public List<SideButton> sideButtons = [];
    public bool sliderPulled;
    public int scrollPos;
    public float floatScrollPos, floatScrollVelocity, sliderValue, sliderValueCap, elementHeight, elementSpacing;
    public int TotalItems => scrollElements.Count;
    public int MaxVisibleElements => (int)(UpperBound / (elementHeight + elementSpacing));
    public int MaximumScrollPos => Math.Max(0, TotalItems - MaxVisibleElements);
    public float UpperBound => size.y;
    public float LowerBound => 0;

    public VerticalScrollSelector(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 elementSize, int amountOfVisibleElements, bool scrollButtons = true, float elementSpacing = 10f, float scrollButtonWidth = 24f)
        : base(menu, owner, pos, new Vector2(elementSize.x, amountOfVisibleElements * (elementSize.y + elementSpacing) + elementSpacing))
    {
        elementHeight = elementSize.y;
        this.elementSpacing = elementSpacing;

        slider = new PatchedVerticalSlider(menu, this, null, new Vector2(-32f, 9f), new Vector2(30f, size.y - 40f), new Slider.SliderID(""), true);

        if (scrollButtons)
        {
            scrollUpButton = new EventfulScrollButton(menu, this, new Vector2(size.x / 2f - scrollButtonWidth / 2f, size.y + 10f), 0, scrollButtonWidth);
            scrollUpButton.OnClick += _ => Scroll(-1);
            scrollDownButton = new EventfulScrollButton(menu, this, new Vector2(size.x / 2f - scrollButtonWidth / 2f, -34f), 2, scrollButtonWidth);
            scrollDownButton.OnClick += _ => Scroll(1);
        }

        myContainer = new FContainer();
        owner.Container.AddChild(myContainer);

        CreateRightLines();
        this.SafeAddSubobjects(slider, scrollUpButton, scrollDownButton);
    }

    public void CreateRightLines()
    {
        for (int i = 0; i < rightLines.Length; i++)
            Container.RemoveChild(rightLines[i]);

        rightLines = new FSprite[sideButtons.Count + 1];
        for (int i = 0; i < rightLines.Length; i++)
        {
            rightLines[i] = new FSprite("pixel")
            {
                anchorX = 0f,
                anchorY = 0f,
                scaleX = 2f
            };
            Container.AddChild(rightLines[i]);
            rightLines[i].MoveToBack();
        }
    }

    public SideButton AddSideButton(string symbolName, string text = "", string description = "")
    {
        SideButton btn = new(menu, this, new Vector2(size.x + 7f, 14f + 30f * sideButtons.Count), symbolName, text, description);
        sideButtons.Add(btn);
        subObjects.Add(btn);

        CreateRightLines();

        return btn;
    }

    public void AddScrollElements(params ButtonScroller.IPartOfButtonScroller[] elements)
    {
        for (int i = 0; i < elements.Length; i++)
        {
            var element = elements[i];

            element.Pos = new Vector2(0, IdealScrollElementYPos(scrollElements.Count));
            scrollElements.Add(element);
            subObjects.Add((MenuObject)element);
        }

        ConstrainScroll();
    }

    public void RemoveScrollElements(params ButtonScroller.IPartOfButtonScroller[] elements)
    {
        for (int i = 0; i < elements.Length; i++)
            scrollElements.Remove(elements[i]);

        ConstrainScroll();
    }

    public void ConstrainScroll() => scrollPos = Mathf.Clamp(scrollPos, 0, MaximumScrollPos);

    public void Scroll(int scrollDir)
    {
        scrollPos += scrollDir;
        ConstrainScroll();
    }

    public float IdealScrollElementYPos(int elementIndex) => UpperBound - (elementHeight + elementSpacing + (elementIndex * (elementSpacing + elementHeight))) + (floatScrollPos * (elementHeight + elementSpacing));

    public float PercentageOverYBound(float y)
    {
        if (y < LowerBound) return 1 - Math.Min(1, (LowerBound - y) / elementHeight);
        float elementUpperBound = y + elementHeight;
        if (elementUpperBound > UpperBound) return 1 - Math.Min(1, (elementUpperBound - UpperBound) / elementHeight);
        return 1;
    }

    public void SliderSetValue(Slider slider, float setValue)
    {
        sliderValue = 1 - setValue;
        sliderPulled = true;
    }

    public float ValueOfSlider(Slider slider) => 1 - sliderValue;

    public override void Update()
    {
        if (MouseOver && menu.manager.menuesMouseMode && menu.mouseScrollWheelMovement != 0)
            Scroll(menu.mouseScrollWheelMovement);

        for (int i = 0; i < scrollElements.Count; i++)
        {
            scrollElements[i].Pos = new(scrollElements[i].Pos.x, IdealScrollElementYPos(i));
            scrollElements[i].Alpha = PercentageOverYBound(scrollElements[i].Pos.y);
        }

        base.Update();

        floatScrollPos = Custom.LerpAndTick(floatScrollPos, scrollPos, 0.01f, 0.01f);
        floatScrollVelocity *= Custom.LerpMap(Math.Abs(scrollPos - floatScrollPos), 0.25f, 1.5f, 0.45f, 0.99f);
        floatScrollVelocity += Mathf.Clamp(scrollPos - floatScrollPos, -2.5f, 2.5f) / 2.5f * 0.15f;
        floatScrollVelocity = Mathf.Clamp(floatScrollVelocity, -1.2f, 1.2f);
        floatScrollPos += floatScrollVelocity;
        sliderValueCap = Custom.LerpAndTick(sliderValueCap, MaximumScrollPos, 0.02f, scrollElements.Count / 40f);

        slider.buttonBehav.greyedOut = MaximumScrollPos == 0;

        if (MaximumScrollPos == 0)
            sliderValue = Custom.LerpAndTick(sliderValue, 0.5f, 0.02f, 0.05f);
        else
        {
            if (sliderPulled)
            {
                floatScrollPos = Mathf.Lerp(0f, sliderValueCap, sliderValue);
                scrollPos = Custom.IntClamp(Mathf.RoundToInt(floatScrollPos), 0, MaximumScrollPos);
                sliderPulled = false;
            }
            else
                sliderValue = Custom.LerpAndTick(sliderValue, Mathf.InverseLerp(0f, sliderValueCap, floatScrollPos), 0.02f, 0.05f);
        }
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        for (int i = 0; i < rightLines.Length; i++)
        {
            float num = (i != 0) ? (sideButtons[i - 1].DrawY(timeStacker) + sideButtons[i - 1].DrawSize(timeStacker).y + 0.01f) : (DrawY(timeStacker) + 9.01f);
            float num2 = (i != rightLines.Length - 1) ? (sideButtons[i].DrawY(timeStacker) + 0.01f) : (DrawY(timeStacker) + DrawSize(timeStacker).y - 10.99f);
            rightLines[i].x = DrawX(timeStacker) + size.x + 15f;
            rightLines[i].y = num;
            rightLines[i].scaleY = num2 - num;
            rightLines[i].color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey);
        }
    }
}