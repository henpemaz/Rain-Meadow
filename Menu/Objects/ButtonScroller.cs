using Menu;
using RainMeadow.UI.Components.Patched;
using RainMeadow.UI.Interfaces;
using RainMeadow.UI.Systems;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    //a scroller just for predetermined buttons, intended for buttons' owner to be ButtonScroller
    //Could support horizontal but built to be vertical most of the time
    public class ButtonScroller : RectangularMenuObject, Slider.ISliderOwner, IPLEASEUPDATEME, IScrollObjectHolder
    {
        public bool sliderDefaultIsDown, greyOutWhenNoScroll, isScrolling, buttonsDirty, lastButtonsDirty;
        public float desiredScrollOffset, scrollOffset, prevScrollOffset, floatScrollSpeed, scrollSliderValue, scrollSliderValueCap
            , maxScrollSpeed = 1.2f, scrollSliderCapLerp = 0.02f, scrollSliderCapTick = 0.05f;
        public Slider scrollSlider;
        public ScrollSystem.Direction sliderAnchor;
        public EventfulScrollButton? scrollUpButton, scrollDownButton;
        public ObservableCollection<MenuObject> scrollObjects = [];
        public FContainer itemContainer;
        public List<SideButton> sideButtons = [];
        public FSprite[] sideButtonLines = [];
        public readonly GridScrollSystem gridSystem;
        public static float CalculateHeightBasedOnAmtOfButtons(int amtOfButtonsView, float buttonHeight, float spacing, bool startEndSpacing = false)
        {
            //remember it goes by buttonsize + button spacing not the buttonSpacing + buttonsize. button size plus first as there will be not extra spacing
            //unless....
            // startEndSpacing is true, then it will add spacing to the start and end instead of button height
            return startEndSpacing ? amtOfButtonsView * (buttonHeight + spacing) + spacing : buttonHeight + Mathf.Max(amtOfButtonsView - 1, 0) * (buttonHeight + spacing);
        }
        public virtual float MaxDownScroll => gridSystem.GetMaxScroll();
        public virtual float DownScrollOffset
        {
            get => desiredScrollOffset;
            set => desiredScrollOffset = Mathf.Clamp(value, 0, MaxDownScroll);
        }
        public float buttonHeight
        {
            get => gridSystem.ElementSize.y;
            set => gridSystem.ElementSize = new(gridSystem.ElementSize.x, value);
        }
        public float buttonSpacing
        {
            get => gridSystem.ElementSpacing.y;
            set => gridSystem.ElementSpacing = new(gridSystem.ElementSpacing.x, value);
        }
        public float ButtonHeightAndSpacing => buttonHeight + buttonSpacing;
        public bool StartEndWithSpacing
        {
            get => gridSystem.StartEndWithSpacing;
            set => gridSystem.StartEndWithSpacing = value;
        }
        public bool CanScrollUp => DownScrollOffset > 0;
        public bool CanScrollDown => DownScrollOffset < MaxDownScroll;
        public bool CanScroll => !menu.FreezeMenuFunctions;
        public bool ScrollObjectsDirty => lastButtonsDirty;
        public virtual FContainer ItemContainer => itemContainer;
        public TextAnchor textAnchor { set => gridSystem.ScrollPosAnchor = value == TextAnchor.Top ? ScrollSystem.Anchor.TopLeft : ScrollSystem.Anchor.BottomLeft; }
        public bool IsHidden { get; set; }
        public ButtonScroller(Menu.Menu menu, MenuObject owner, Vector2 pos, GridScrollSystem gridScrollSystem, ScrollSystem.Direction sliderAnchor = ScrollSystem.Direction.Left, Vector2 sliderPosOffset = default, float sliderSizeAxisOffset = 0) :
          this(menu, owner, pos, gridScrollSystem.CalculateCustomViewSize(), gridScrollSystem, sliderAnchor, sliderPosOffset, sliderSizeAxisOffset)
        {

        }
        public ButtonScroller(Menu.Menu menu, MenuObject owner, Vector2 pos, int amtOfButtonsToView, float listSizeX, (float, float) buttonHeightSpacing, bool sliderOnRight = false, Vector2 sliderPosOffset = default, float sliderSizeAxisOffset = 0, bool startEndWithSpacing = false) : 
            this(menu, owner, pos, new GridScrollSystem(new(listSizeX, buttonHeightSpacing.Item1), new(0, buttonHeightSpacing.Item2), amtOfButtonsToView, 1, startEndWithSpacing), sliderOnRight? ScrollSystem.Direction.Right : ScrollSystem.Direction.Left, sliderPosOffset, sliderSizeAxisOffset)
        {
            buttonHeight = buttonHeightSpacing.Item1;
            buttonSpacing = buttonHeightSpacing.Item2;
            this.StartEndWithSpacing = startEndWithSpacing;
        }
        public ButtonScroller(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, GridScrollSystem? gridScrollSystem = null, ScrollSystem.Direction sliderAnchor = ScrollSystem.Direction.Left, Vector2 sliderPosOffset = default, float sliderSizeAxisOffset = 0) : base(menu, owner, pos, size)
        {
            (owner?.Container ?? menu.container).AddChild(myContainer = new());
            myContainer.AddChild(itemContainer = new());
            //slider sprite xoffset is 15
            this.sliderAnchor = sliderAnchor;
            if (sliderAnchor is ScrollSystem.Direction.Left or ScrollSystem.Direction.Right)
                scrollSlider = new PatchedVerticalSlider(menu, this, "Scroller", sliderPosOffset + new Vector2(sliderAnchor is ScrollSystem.Direction.Right ? size.x : -32, 0), new Vector2(30, size.y + sliderSizeAxisOffset), new("BUTTONSCROLLER_SCROLLSLIDER"), true);
            else
                scrollSlider = new HorizontalSlider(menu, this, "Scroller", sliderPosOffset + new Vector2(0, sliderAnchor is ScrollSystem.Direction.Top ? size.y : -32), new(size.x + sliderSizeAxisOffset, 30), new("BUTTONSCROLLER_SCROLLSLIDER"), true);
            subObjects.Add(scrollSlider);

            scrollObjects.CollectionChanged += (_, collectionChangedArgs) => OnButtonListChanged(collectionChangedArgs);

            gridSystem = gridScrollSystem ?? 
                new(ScrollSystem.Axis.Vertical)
            {
                    _elementSize = new(size.x, 30),
            };
            gridSystem.MarkScrollObjectsDirty += MarkScrollObjectsDirty;

            UpdateGridSystem();
        }
        public void MarkScrollObjectsDirty() => buttonsDirty = true;
        public virtual void OnButtonListChanged(NotifyCollectionChangedEventArgs args)
        {
            if (args.Action is NotifyCollectionChangedAction.Remove)
            {
                for (int i = args.OldStartingIndex; i < scrollObjects.Count; i++)
                    scrollObjects[i].GetScrollObject().UpdateIndexFromScroller(this, i);
            }
            else if (args.Action is NotifyCollectionChangedAction.Add)
            {
                if (args.NewStartingIndex != scrollObjects.Count - args.NewItems.Count)
                {
                    for (int i = args.NewStartingIndex + args.NewItems.Count; i < scrollObjects.Count; i++)
                        scrollObjects[i].GetScrollObject().UpdateIndexFromScroller(this, i);
                }
            }
            else if (args.Action is NotifyCollectionChangedAction.Move or NotifyCollectionChangedAction.Replace)
            {
                throw new NotImplementedException("ButtonScroller is not designed to move/change elements like that");
                /*int toStart = Mathf.Min(args.NewStartingIndex, args.OldStartingIndex);
                for (int i = toStart; i < scrollObjects.Count; i++)
                    scrollObjects[i].GetScrollObject().UpdateIndexFromScroller(this, i);*/
            }
            gridSystem.SetElementCount(scrollObjects.Count);
        }
        public bool IsAtBoundary(ScrollSystem.Direction boundary)
        {
            if (gridSystem.TryGetScrollNeededForBounds(boundary, out float desiredScrollOffset))
                return desiredScrollOffset == DownScrollOffset;
            return false;
        }
        public void MoveToBoundary(ScrollSystem.Direction boundary)
        {
            if (gridSystem.TryGetScrollNeededForBounds(boundary, out float desiredScrollOffset))
                SetScrollImmediately(desiredScrollOffset);
        }
        public void SetScrollImmediately(float scrollOffset)
        {
            this.scrollOffset = DownScrollOffset = scrollOffset;
        }
        public void UpdateGridSystem()
        {
            gridSystem.ViewSize = size;
            gridSystem.StartEndWithSpacing = StartEndWithSpacing;
        }
        public override void RemoveSprites()
        {
            itemContainer.RemoveFromContainer();
            myContainer.RemoveFromContainer();
            gridSystem.MarkScrollObjectsDirty -= MarkScrollObjectsDirty;
            base.RemoveSprites();
        }
        public override void Update()
        {
            UpdateGridSystem();
            lastButtonsDirty = buttonsDirty;
            buttonsDirty = false;
            base.Update(); 
            if (!IsHidden && CanScroll && MouseOver && menu.manager.menuesMouseMode) ScrollingUpdate(menu.mouseScrollWheelMovement);
                /*for (int i = 0; i < buttons.Count; i++)
                {
                    buttons[i].Size = new(buttons[i].Size.x, buttonHeight);
                    buttons[i].Pos = new(buttons[i].Pos.x, GetIdealYPosWithScroll(i));
                    buttons[i].Alpha = AlphaOfObject(buttons[i].Pos);
                }*/
            
            prevScrollOffset = scrollOffset;
            float currentScrollOffset = GetCurrentScrollOffset();
            scrollOffset = Custom.LerpAndTick(scrollOffset, currentScrollOffset, 0.01f, 0.01f);
            floatScrollSpeed *= Custom.LerpMap(Math.Abs(currentScrollOffset - scrollOffset), 0.25f, 1.5f, 0.45f, 0.99f);
            floatScrollSpeed += Mathf.Clamp(currentScrollOffset - scrollOffset, -2.5f, 2.5f) / 2.5f * 0.15f;
            floatScrollSpeed = Mathf.Clamp(floatScrollSpeed, -maxScrollSpeed, maxScrollSpeed);
            scrollOffset += floatScrollSpeed;

            scrollSliderValueCap = Custom.LerpAndTick(scrollSliderValueCap, MaxDownScroll, scrollSliderCapLerp, scrollObjects.Count / 40f);

            if (MaxDownScroll == 0) scrollSliderValue = Custom.LerpAndTick(scrollSliderValue, sliderDefaultIsDown? 1 : 0, scrollSliderCapLerp, scrollSliderCapTick);
            else scrollSliderValue = Custom.LerpAndTick(scrollSliderValue, Mathf.InverseLerp(0f, scrollSliderValueCap, scrollOffset), isScrolling?  Mathf.Max(0.9f, scrollSliderCapLerp) : scrollSliderCapLerp, scrollSliderCapTick);

            if (isScrolling && scrollOffset == currentScrollOffset) isScrolling = false;

            scrollSlider.buttonBehav.greyedOut = greyOutWhenNoScroll && MaxDownScroll == 0;
            if (scrollDownButton != null) scrollDownButton.buttonBehav.greyedOut = !CanScrollDown;
            if (scrollUpButton != null) scrollUpButton.buttonBehav.greyedOut = !CanScrollUp;

            if (scrollOffset != prevScrollOffset)
                buttonsDirty = true;
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            for (int i = 0; i < sideButtonLines.Length; i++)
            {
                float bottomY = (i != 0) ? (sideButtons[i - 1].DrawY(timeStacker) + sideButtons[i - 1].DrawSize(timeStacker).y + 0.01f) : (DrawY(timeStacker) + scrollSlider.anchorPoint.y),
                    topY = (i != sideButtonLines.Length - 1) ? (sideButtons[i].DrawY(timeStacker) + 0.01f) : (DrawY(timeStacker) + DrawSize(timeStacker).y + (20 - (size.y - scrollSlider.length) + scrollSlider.anchorPoint.y));
                sideButtonLines[i].x = DrawX(timeStacker) + (sliderAnchor is ScrollSystem.Direction.Right? scrollSlider.pos.x - 15 : size.x - (scrollSlider.pos.x + 17));
                sideButtonLines[i].y = bottomY;
                sideButtonLines[i].scaleY = topY - bottomY;
                sideButtonLines[i].color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey);
            }
        }
        public virtual float GetCurrentScrollOffset() => DownScrollOffset;
        public  void SliderSetValue(Slider slider, float f)
        {
            if (slider == scrollSlider)
            {
                scrollSliderValue = gridSystem.GetSliderValue(f);
                SetScrollImmediately(Mathf.Lerp(0f, scrollSliderValueCap, scrollSliderValue));
                buttonsDirty = true;
            }
            /*if (slider?.ID?.value == "BUTTONSCROLLER_SCROLLSLIDER")
            {
                scrollSliderValue = textAnchor == TextAnchor.Top ? 1 - f : f;
                DownScrollOffset = scrollOffset = Mathf.Lerp(0f, scrollSliderValueCap, scrollSliderValue);
                buttonsDirty = true;
            }*/
        }
        public float ValueOfSlider(Slider slider)
        {
            if (slider == scrollSlider)
                return gridSystem.GetSliderValue(scrollSliderValue);
            return 0;
        }
        public void ScrollingUpdate(float yInput)
        {
            float downScroll = DownScrollOffset;
            if (gridSystem.TryAddScrollThroughWheel(yInput, ref downScroll))
            {
                DownScrollOffset = downScroll;
                menu.PlaySound(SoundID.MENU_Scroll_Tick);
                isScrolling = true;
            }
        }
        public void AddScroll(float addDir)
        {
            DownScrollOffset += addDir * gridSystem.ScrollStepDir[gridSystem.IndexToRef];
        }
        public void ConstrainScroll(bool constrainImmediately = false)
        {
            UpdateGridSystem();
            DownScrollOffset = Mathf.Clamp(DownScrollOffset, 0, MaxDownScroll);
            if (constrainImmediately)
                scrollOffset = DownScrollOffset;
        }
        public List<T> GetSpecificButtons<T>() where T : MenuObject
        {
            return [.. scrollObjects.OfType<T>()];
        }
        public void RemoveScrollObject(int index, bool constrainScroll = true) => RemoveScrollObject(scrollObjects.GetValueOrDefault(index), constrainScroll);
        public void RemoveScrollObject(MenuObject? scrollObj, bool constrainScroll = true)
        {
            if (!scrollObjects.Contains(scrollObj)) return;
            scrollObj.GetScrollObject().RemovedFromScroller();
            this.ClearMenuObject(scrollObj);

            scrollObjects.Remove(scrollObj);

            if (constrainScroll) ConstrainScroll();
        }
        public void RemoveAllButtons(bool constrainScroll = true)
        {
            this.ClearMenuObjectIList(scrollObjects);
            scrollObjects.Clear();
            if (constrainScroll) ConstrainScroll();
        }

        [Obsolete]
        /// <summary>
        /// Add scrollButtons first before adding scroll objects when wanted. Slider won't be accessible if scroll buttons were added.
        /// </summary>
        /// <param name="scrollBoxButtons"></param>
        public void AddButtons(params IPartOfButtonScroller[]? scrollBoxButtons)
        {
            if (scrollBoxButtons == null) return;
            AddScrollObjects([..scrollBoxButtons.Where(x => x is MenuObject).Cast<MenuObject>()]);
        }
        public void AddScrollObjects(params MenuObject[]? scrollObjects) => AddScrollObjects(-1, scrollObjects);
        public void AddScrollObjects(int startingIndex, MenuObject[]? scrollObjects)
        {
            if (scrollObjects == null) return;
            int actualStartingIndex = startingIndex == -1? this.scrollObjects.Count : startingIndex;
            int subObjectIndexToInsert = startingIndex == - 1? subObjects.Count : subObjects.IndexOf(scrollObjects[startingIndex]);
            for (int i = 0; i < scrollObjects.Length; i++)
            {
                var obj = scrollObjects[i];
                int indexInsert = actualStartingIndex + i;
                OnAddMenuScrollObject(obj, indexInsert);
                subObjects.Insert(subObjectIndexToInsert + i, obj);
                this.scrollObjects.Insert(actualStartingIndex + i, obj);
            }
        }
        public virtual void OnAddMenuScrollObject(MenuObject scrollObject, int indexAt)
        {
            scrollObject.GetScrollObject().AddedIntoScroller(this, indexAt);
        }
        public virtual Vector2 SizeOfObject(Vector2 origSize) => new(origSize.x, buttonHeight);
        public virtual Vector2 PositionOfObject(int index, Vector2 origPosition = default)
        {
            var prevScrollObj = scrollObjects.GetValueOrDefault(index - 1)?.GetScrollObject();
            (Vector2, Vector2) posSizeOfElement = (origPosition, scrollObjects.GetValueOrDefault(index)?.GetScrollObject()?.Size ?? SizeOfObject(size));
            (Vector2, Vector2)? prevPosSizeOfElement = prevScrollObj == null ? null : (prevScrollObj.LocalPos, prevScrollObj.Size);

            return gridSystem.PositionOfElementWithScroll(index, posSizeOfElement, prevPosSizeOfElement, scrollOffset);
        }
        public virtual float AlphaOfObject(Vector2 elementPos, Vector2 elementSize)
        {
            int indexToRef = gridSystem.IndexToRef;
            float startSize = 0;
            float endSize = size[indexToRef];

            float combinedPosInAxis = elementPos[indexToRef];
            float elementPosWithSizeInAxis = combinedPosInAxis + elementSize[indexToRef];

            float boundaryOffset = (gridSystem.ElementSizeInAxis + gridSystem.ElementSpacingInAxis) / 3;
            return combinedPosInAxis < startSize ? Mathf.InverseLerp(-boundaryOffset, 0, combinedPosInAxis) : 
                elementPosWithSizeInAxis > endSize ? Mathf.InverseLerp(endSize + boundaryOffset, endSize, elementPosWithSizeInAxis) : 1;
            //if button starts crossing the bound, calculate the alpha else alpha = 1
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
        public enum TextAnchor
        {
            Top,
            Bottom
        }
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
        public class ScrollerButton(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, string description = "") : SimplerButton(menu, owner, displayText, pos, size, description), IOwnMenuScrollObject
        {
        }

        [Obsolete]
        public interface IPartOfButtonScroller //allows other derived objects to be part of the button scroller
        {
            public float Alpha { get; set; }
            public Vector2 Pos { get; set; }
            public Vector2 Size { get; set; }
        }
    }
}
