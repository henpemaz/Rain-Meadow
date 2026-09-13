using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components.Patched;
using RainMeadow.UI.Interfaces;
using RainMeadow.UI.Systems;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Rewired.ComponentControls.Effects.RotateAroundAxis;

namespace RainMeadow.UI.Components
{
    /// <summary>
    /// Use this if you need a dynamic more flexible way of scrollsizing<para></para>
    /// If you are using indexing for positioning of ur objects, PLEASE BUTTONSCROLLER EXISTED FOR THIS REASON
    /// </summary>
    public class ScrollableContainer : RectangularMenuObject, IScrollObjectHolder, IPLEASEUPDATEME, Slider.ISliderOwner
    {
        public float scrollSliderCapLerp = 0.02f, scrollSliderCapTick = 0.05f, floatScrollMultipler = 100f;
        public bool scrollableDirty = true, lastScrollableDirty = true, sliderDefaultIsDown, isScrolling;
        public float scrollSliderValueCap, scrollSliderValue, scrollSpeed, desiredScrollPosOffset, floatScrollPosOffset, prevFloatScrollPosOffset;
        public MenuScrollObject? _content;
        public readonly ContentScrollSystem contentSystem;

        public Slider? scrollSlider;
        public UIMask uiMask;
        public FContainer itemMaskContainer;
        public bool MouseOverItemBounds => uiMask.MouseOverTexture;
        public bool IsHidden { get; set; }
        public bool ScrollObjectsDirty => lastScrollableDirty;
        public FContainer ItemContainer => itemMaskContainer;
        public MenuScrollObject? ContentObject
        {
            get => _content;
            set
            {
                if (value == null || _content == value) return;
                if (_content != null)
                {
                    _content.RemovedFromScroller();
                    this.ClearMenuObject(_content.menuObject);
                }
                _content = value;
                this.SafeAddSubobjects(_content.menuObject);
                _content.menuObject.GetScrollObject().AddedIntoScroller(this, 0);
                scrollableDirty = true;
            }
        }
        public ScrollableContainer(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, ContentScrollSystem? scrollingSystem = null, ScrollSystem.Anchor horiVertSliderAnchor = ScrollSystem.Anchor.BottomRight, Vector2 sliderPosOffset = default, float sliderSizeoffset = 0) : base(menu, owner, pos, size)
        {
            (owner?.Container ?? menu.container).AddChild(myContainer = new());
            myContainer.AddChild(itemMaskContainer = new());

            uiMask = new(menu, this, new(0, 0), size, itemMaskContainer);
            contentSystem = scrollingSystem ?? new ContentScrollSystem(ScrollSystem.Axis.Vertical);
            contentSystem.MarkScrollObjectsDirty += MarkScrollObjectsDirty;
            contentSystem.OnViewSizeChanged += ViewSizeChanged;
            contentSystem.OnContentSizeChanged += ContentSizeChanged;

            subObjects.Add(uiMask);
            BuildSliders(horiVertSliderAnchor, sliderPosOffset, sliderSizeoffset);
        }
        public void BuildSliders(ScrollSystem.Anchor horiVertSliderAnchor, Vector2 sliderPosOffset, float sliderSizeOffset)
        {
            if (contentSystem.IsHorizontal)
            {
                bool sliderOnTop = horiVertSliderAnchor is ScrollSystem.Anchor.TopRight or ScrollSystem.Anchor.TopLeft;
                scrollSlider = new HorizontalSlider(menu, this, null, new Vector2(20, sliderOnTop? size.y : -32) + sliderPosOffset, new(size.x - 40 + sliderSizeOffset, 30f), new("Test"), true);
            }
            else
            {
                bool sliderOnRight = horiVertSliderAnchor is ScrollSystem.Anchor.TopRight or ScrollSystem.Anchor.BottomRight;
                scrollSlider = new PatchedVerticalSlider(menu, this, null, new Vector2(sliderOnRight ? size.x - 32 : 0, 10) + sliderPosOffset, new(30, size.y - 40 + sliderSizeOffset), new("Test"), true);
            }
            subObjects.Add(scrollSlider);
        }
        public void ContentSizeChanged() => ConstrainScroll(false);
        public void ViewSizeChanged() => ConstrainScroll(true);
        public void MarkScrollObjectsDirty()
        {
            scrollableDirty = true;
        }
        public void AttachScrollable(Scrollable scrollable, float desiredContentSize)
        {
            scrollable.size = size;
            scrollable.size[contentSystem.IndexToRef] = desiredContentSize;
            ContentObject = scrollable.GetScrollObject();
        }
        public Scrollable CreateAndAttachScrollable(float contentSize)
        {
            Scrollable scrollable = new(menu, this, Vector2.zero, default);
            AttachScrollable(scrollable, contentSize);
            return scrollable;
        }
        public void SetScrollImmediately(float scrollOffset)
        {
            floatScrollPosOffset = desiredScrollPosOffset = scrollOffset;
        }
        public void AddScroll(float scrollAmt)
        {
            scrollAmt *= floatScrollMultipler;
            if (contentSystem.TryAddScrollThroughWheel(scrollAmt * 5, ref desiredScrollPosOffset))
            {
                menu.PlaySound(SoundID.MENU_Scroll_Tick);
                isScrolling = true;
            }
        }
        public void UpdateScrollingSystemComponents()
        {
            contentSystem.ViewSize = size;
            var contentObj = ContentObject;
            if (contentObj != null)
                contentSystem.ContentSize = contentObj.Size[contentSystem.IndexToRef];
        }
        public void UpdateScroll()
        {
            prevFloatScrollPosOffset = floatScrollPosOffset;
            floatScrollPosOffset = Mathf.SmoothDamp(floatScrollPosOffset, desiredScrollPosOffset, ref scrollSpeed, 0.15f * UIelement.frameMulti);
            if (Mathf.Abs(floatScrollPosOffset - desiredScrollPosOffset) < 0.5f)
            {
                floatScrollPosOffset = desiredScrollPosOffset;
                MarkScrollObjectsDirty();
                scrollSpeed = 0;
            }
            if (prevFloatScrollPosOffset != floatScrollPosOffset)
                MarkScrollObjectsDirty();

            var maxScroll = contentSystem.GetMaxScroll();

            scrollSliderValueCap = Custom.LerpAndTick(scrollSliderValueCap, maxScroll, scrollSliderCapLerp, 30);

            if (maxScroll == 0) scrollSliderValue = Custom.LerpAndTick(scrollSliderValue, sliderDefaultIsDown ? 1 : 0, scrollSliderCapLerp, scrollSliderCapTick);
            else scrollSliderValue = Custom.LerpAndTick(scrollSliderValue, Mathf.InverseLerp(0f, scrollSliderValueCap, floatScrollPosOffset), isScrolling ? Mathf.Max(0.9f, scrollSliderCapLerp) : scrollSliderCapLerp, scrollSliderCapTick);

            if (isScrolling && floatScrollPosOffset == desiredScrollPosOffset)
                isScrolling = false;
        }
        public void ConstrainScroll(bool immediatelyApplyConstrainedScroll = false)
        {
            UpdateScrollingSystemComponents();
            desiredScrollPosOffset = Mathf.Clamp(desiredScrollPosOffset, 0, contentSystem.GetMaxScroll());
            if (immediatelyApplyConstrainedScroll)
                floatScrollPosOffset = desiredScrollPosOffset;

        }
        public override void Update()
        {
            uiMask.size = size;
            UpdateScrollingSystemComponents();
            lastScrollableDirty = scrollableDirty;
            scrollableDirty = false;
            base.Update();

            if (!menu.FreezeMenuFunctions && !IsHidden && MouseOver && menu.manager.menuesMouseMode)
                AddScroll(menu.mouseScrollWheelMovement);
            UpdateScroll();
        }
        public override void RemoveSprites()
        {
            contentSystem.MarkScrollObjectsDirty -= MarkScrollObjectsDirty;
            contentSystem.OnViewSizeChanged -= ViewSizeChanged;
            contentSystem.OnContentSizeChanged -= ContentSizeChanged;
            base.RemoveSprites();
        }
        public float ValueOfSlider(Slider slider)
        {
            if (slider == scrollSlider)
                return contentSystem.GetSliderValue(scrollSliderValue);
            return 0;
        }
        public void SliderSetValue(Slider slider, float f)
        {
            if (slider == scrollSlider)
            {
                var val = scrollSliderValue = contentSystem.GetSliderValue(f);
                SetScrollImmediately(Mathf.Lerp(0, scrollSliderValueCap, val));
                return;
            }
        }
        public bool WithinBounds(Vector2 screenPos, Vector2 screenSize)
        {
            return uiMask.WithinBounds(screenPos, screenSize);
        }
        public Vector2 SizeOfObject(Vector2 origSize)
        {
            origSize[contentSystem.IndexToRef] = Mathf.Max(origSize[contentSystem.IndexToRef], contentSystem.ViewSize[contentSystem.IndexToRef]);
            origSize[contentSystem.OppositeIndexToRef] = contentSystem.ViewSize[contentSystem.OppositeIndexToRef];
            return origSize;
        }
        public Vector2 PositionOfObject(int index, Vector2 origPosition)
        {
            Vector2 contentSize = ContentObject == null ? Vector2.zero : ContentObject.Size;
            return contentSystem.PositionOfElementWithScroll(index, (origPosition, contentSize), null, floatScrollPosOffset);
        }
        public float AlphaOfObject(Vector2 posOfContent, Vector2 sizeofContent)
        {
            return 1;
        }
        public class Scrollable : RectangularMenuObject, IOwnMenuScrollObject
        {
            public ScrollableContainer myScrollContainer;
            public Dictionary<WeakReference<PositionedMenuObject>, ScrollSystem.Anchor> subObjectsForcedAnchor = [];
            //this is default positioning of menuObjs, you can remove this implementation of anchoring as this originally was made in case for Slugcat abilities extended ui
            public ScrollSystem.Anchor defaultSubObjectAnchorRelativeToScrollable = ScrollSystem.Anchor.BottomLeft;
            public bool checkSubobjectsOnly = true;
            public Vector2 ScreenPosOffset => Vector2.Max(size - myScrollContainer.size, Vector2.zero);
            public Scrollable(Menu.Menu menu, ScrollableContainer owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
            {
                myScrollContainer = owner;
            }
            public void ForceAnchor(PositionedMenuObject menuobject, ScrollSystem.Anchor? anchor)
            {
                if (anchor == null) //remove
                {
                    var menuObjToRemove = subObjectsForcedAnchor.Keys.FirstOrDefault(x => x.TryGetTarget(out PositionedMenuObject target) && target == menuobject);
                    if (menuObjToRemove != null)
                        subObjectsForcedAnchor.Remove(menuObjToRemove);
                    return;
                }
                var instanceOfMenuObj = subObjectsForcedAnchor.Keys.FirstOrDefault(x => x.TryGetTarget(out PositionedMenuObject target) && target == menuobject);
                instanceOfMenuObj ??= new(menuobject);
                subObjectsForcedAnchor[instanceOfMenuObj] = anchor.Value;
            }
            public override void Update()
            {
                base.Update();
                List<WeakReference<PositionedMenuObject>> toRemove = [];
               foreach (var subObjForcedAnchor in subObjectsForcedAnchor.Keys)
                {
                    if (!subObjForcedAnchor.TryGetTarget(out _))
                        toRemove.Add(subObjForcedAnchor);
                }
               for (int i = 0; i < toRemove.Count; i++)
                subObjectsForcedAnchor.Remove(toRemove[i]);
            }
            public Vector2 GetNewScreenPos(PositionedMenuObject posMenuObj, Vector2 origScreenPos)
            {
                var anchorToFollow = defaultSubObjectAnchorRelativeToScrollable;
                var forcedAnchorObj = subObjectsForcedAnchor.Keys.FirstOrDefault(x => x.TryGetTarget(out PositionedMenuObject target) && target == posMenuObj);
                if (forcedAnchorObj != null)
                    anchorToFollow = subObjectsForcedAnchor[forcedAnchorObj];


                if (anchorToFollow == ScrollSystem.Anchor.BottomLeft || (checkSubobjectsOnly && !subObjects.Contains(posMenuObj)))
                    return origScreenPos;
                var isHori = myScrollContainer.contentSystem.IsHorizontal;
                if (isHori && anchorToFollow is ScrollSystem.Anchor.TopRight or ScrollSystem.Anchor.BottomRight)
                    origScreenPos.x += ScreenPosOffset.x;
                if (!isHori && anchorToFollow is ScrollSystem.Anchor.TopLeft or ScrollSystem.Anchor.TopRight)
                    origScreenPos.y += ScreenPosOffset.y;
                return origScreenPos;
            }
        }
    }
}
