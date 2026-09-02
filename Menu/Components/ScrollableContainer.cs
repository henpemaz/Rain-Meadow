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
    public class ScrollableContainer : RectangularMenuObject, IScrollObjectHolder, IPLEASEUPDATEME, Slider.ISliderOwner
    {
        public readonly string IDForTexture;

        public float scrollSliderCapLerp = 0.02f, scrollSliderCapTick = 0.05f, maxScrollSpeed = 1.2f, floatScrollMultipler = 100f;
        public bool scrollableDirty = true, lastScrollableDirty = true, cameraDirty = true, sliderDefaultIsDown, isScrolling;
        public float scrollSliderValueCap, scrollSliderValue, scrollSpeed, desiredScrollPosOffset, floatScrollPosOffset, prevFloatScrollPosOffset;

        public Scrollable? _content;
        public readonly ContentScrollSystem scrollingSystem;

        public Slider? scrollSlider;

        public Camera cam;
        public RenderTexture? cameraRT;
        public FTexture? insideTexture;
        public FContainer itemMaskContainer;
        public FContainer camContainer;
        public Vector2 initialCamPos, camSizeOffset;
        public Vector3 camPosOffset;
        public bool IsHidden { get; set; }
        public bool ScrollObjectsDirty => lastScrollableDirty;
        public FContainer ItemContainer => itemMaskContainer;
        public Scrollable? ContentObject
        {
            get => _content;
            set
            {
                if (value == null || _content == value) return;
                if (_content != null)
                {
                    _content.GetScrollObject().RemovedFromScroller();
                    this.ClearMenuObject(_content);
                }
                _content = value;
                this.SafeAddSubobjects(_content);
                _content.GetScrollObject().AddedIntoScroller(this, 0);
                scrollableDirty = true;
            }
        }
        public ScrollableContainer(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, ContentScrollSystem? scrollingSystem = null, ScrollSystem.Anchor horiVertSliderAnchor = ScrollSystem.Anchor.BottomRight) : base(menu, owner, pos, size)
        {
            (owner?.Container ?? menu.container).AddChild(myContainer = new());
            myContainer.AddChild(itemMaskContainer = new()); 
            myContainer.AddChild(camContainer = new());
            this.scrollingSystem = scrollingSystem ?? new ContentScrollSystem(ScrollSystem.Axis.Vertical);
            this.scrollingSystem.MarkScrollObjectsDirty += MarkScrollObjectsDirty;
            this.scrollingSystem.OnViewSizeChanged += ViewSizeChanged;
            this.scrollingSystem.OnContentSizeChanged += ContentSizeChanged;
            cam = new GameObject().AddComponent<Camera>();
            int index = -1;
            for (int i = 0; i < OpScrollBox._cameras.Count; i++)
            {
                if (OpScrollBox._cameras[i] == null)
                {
                    index = i;
                    OpScrollBox._cameras[i] = cam;
                    break;
                }
            }
            if (index == -1)
            {
                index = OpScrollBox._cameras.Count;
                OpScrollBox._cameras.Add(cam);
            }
            IDForTexture = "Scrollable" + index;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            initialCamPos = new Vector2(10000f + 10300f * index, 10000f);
            BuildSliders(horiVertSliderAnchor);
            itemMaskContainer.SetPosition(initialCamPos);
        }
        public void BuildSliders(ScrollSystem.Anchor horiVertSliderAnchor)
        {
            if (scrollingSystem.IsHorizontal)
            {
                bool sliderOnTop = horiVertSliderAnchor is ScrollSystem.Anchor.TopRight or ScrollSystem.Anchor.TopLeft;
                scrollSlider = new HorizontalSlider(menu, this, null, new Vector2(20, sliderOnTop? size.y : -32), new(size.x - 40, 30f), new("Test"), true);
            }
            else
            {
                bool sliderOnRight = horiVertSliderAnchor is ScrollSystem.Anchor.TopRight or ScrollSystem.Anchor.BottomRight;
                scrollSlider = new PatchedVerticalSlider(menu, this, null, new Vector2(sliderOnRight ? size.x - 32 : 0, 10), new(30, size.y - 40), new("Test"), true);
            }
            subObjects.Add(scrollSlider);
        }
        public void ContentSizeChanged()
        {
            ConstrainScroll(false);
        }
        public void ViewSizeChanged()
        {
            ConstrainScroll(true);
            cameraDirty = true;
        }
        public void MarkScrollObjectsDirty()
        {
            scrollableDirty = true;
        }
        public Scrollable CreateNewContentObject(float contentSize)
        {
            Vector2 sizeofcontent = scrollingSystem.IsHorizontal ? new(contentSize, size.y) : new(size.x, contentSize);
            Scrollable scrollable = new(menu, this, Vector2.zero, sizeofcontent);
            ContentObject = scrollable;
            return scrollable;
        }
        public void DestroyRender()
        {
            if (cameraRT)
            {
                cameraRT.Release();
                UnityEngine.Object.Destroy(cameraRT);
            }
        }
        public void RefreshCamera()
        {
            cam.enabled = true;
            cameraDirty = false;
            float posXOffset = camSizeOffset.x * -0.5f;
            float posYOffset = camSizeOffset.y * -0.5f;
            float sizeX = size.x + camSizeOffset.x;
            float sizeY = size.y + camSizeOffset.y;
                ;
            cam.aspect = sizeX / sizeY;
            cam.orthographic = true;
            cam.orthographicSize = sizeY / 2f;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 100f;
            camPosOffset = new Vector3(posXOffset + sizeX * 0.5f, posYOffset + sizeY * 0.5f, -50f) + (Vector3)initialCamPos;
            cam.depth = -1000f;

            int width = Mathf.CeilToInt(sizeX);
            int height = Mathf.CeilToInt(sizeY);
            DestroyRender();
            cameraRT = new RenderTexture(width, height, 8, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point
            };
            cam.targetTexture = cameraRT;
            if (insideTexture == null)
            {
                insideTexture = new FTexture(cameraRT, IDForTexture)
                {
                    anchorX = 0.5f,
                    anchorY = 0.5f
                };
                camContainer.AddChild(insideTexture);
            }
            else
                insideTexture.SetTexture(cameraRT);

        }
        public void SetScrollImmediately(float scrollOffset)
        {
            floatScrollPosOffset = desiredScrollPosOffset = scrollOffset;
        }
        public void AddScroll(float scrollAmt)
        {
            scrollAmt *= floatScrollMultipler;
            if (scrollingSystem.AddScroll(scrollAmt * 10, ref desiredScrollPosOffset))
            {
                menu.PlaySound(SoundID.MENU_Scroll_Tick);
                isScrolling = true;
            }
        }
        public void UpdateScrollingSystemComponents()
        {
            scrollingSystem.ViewSize = size;
            var contentObj = ContentObject;
            scrollingSystem.SetContentSize(ContentObject?.size ?? default);
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

            var maxScroll = scrollingSystem.GetMaxScroll();

            scrollSliderValueCap = Custom.LerpAndTick(scrollSliderValueCap, maxScroll, scrollSliderCapLerp, 30);

            if (maxScroll == 0) scrollSliderValue = Custom.LerpAndTick(scrollSliderValue, sliderDefaultIsDown ? 1 : 0, scrollSliderCapLerp, scrollSliderCapTick);
            else scrollSliderValue = Custom.LerpAndTick(scrollSliderValue, Mathf.InverseLerp(0f, scrollSliderValueCap, floatScrollPosOffset), isScrolling ? Mathf.Max(0.9f, scrollSliderCapLerp) : scrollSliderCapLerp, scrollSliderCapTick);

            if (isScrolling && floatScrollPosOffset == desiredScrollPosOffset)
                isScrolling = false;
        }
        public void ConstrainScroll(bool immediatelyApplyConstrainedScroll = false)
        {
            desiredScrollPosOffset = Mathf.Clamp(desiredScrollPosOffset, 0, scrollingSystem.GetMaxScroll());
            if (immediatelyApplyConstrainedScroll)
                floatScrollPosOffset = desiredScrollPosOffset;

        }
        public override void Update()
        {
            UpdateScrollingSystemComponents();
            if (cameraDirty)
                RefreshCamera();
            lastScrollableDirty = scrollableDirty;
            scrollableDirty = false;
            base.Update();

            if (!menu.FreezeMenuFunctions && !IsHidden && MouseOver && menu.manager.menuesMouseMode)
                AddScroll(menu.mouseScrollWheelMovement);
            UpdateScroll();
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            var screenPos = DrawPos(timeStacker);
            cam?.transform.position = camPosOffset + (Vector3)screenPos;
            itemMaskContainer.SetPosition(initialCamPos);
            insideTexture?.SetPosition(screenPos + (size * 0.5f));
        }
        public override void RemoveSprites()
        {
            DestroyRender();
            UnityEngine.Object.Destroy(cam?.gameObject);
            scrollingSystem.MarkScrollObjectsDirty -= MarkScrollObjectsDirty;
            scrollingSystem.OnViewSizeChanged -= ViewSizeChanged;
            scrollingSystem.OnContentSizeChanged -= ContentSizeChanged;
            base.RemoveSprites();
        }
        public float ValueOfSlider(Slider slider)
        {
            if (slider == scrollSlider)
                return scrollingSystem.GetSliderValue(scrollSliderValue);
            return 0;
        }
        public void SliderSetValue(Slider slider, float f)
        {
            if (slider == scrollSlider)
            {
                var val = scrollSliderValue = scrollingSystem.GetSliderValue(f);
                SetScrollImmediately(Mathf.Lerp(0, scrollSliderValueCap, val));
                return;
            }
        }
        public Vector2 SizeOfObject(Vector2 origSize)
        {
            return origSize;
        }
        public Vector2 PositionOfObject(int index, Vector2 origPosition)
        {
            Vector2 contentSize = ContentObject == null ? Vector2.zero : ContentObject.size;
            return scrollingSystem.NormalPositionOfElement(index, (origPosition, contentSize), null) + scrollingSystem.ScrollOffsetToDirection(prevFloatScrollPosOffset);
        }
        public float AlphaOfObject(Vector2 posOfContent)
        {
            return 1;
        }
        public class Scrollable : RectangularMenuObject, IOwnMenuScrollObject
        {
            public ScrollableContainer myScrollContainer;
            public Dictionary<WeakReference<PositionedMenuObject>, ScrollSystem.Anchor> subObjectsForcedAnchor = [];
            public ScrollSystem.Anchor defaultSubObjectAnchorRelativeToScrollable = ScrollSystem.Anchor.BottomLeft; //this is default positioning of menuObjs
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
                var isHori = myScrollContainer.scrollingSystem.IsHorizontal;
                if (isHori && anchorToFollow is ScrollSystem.Anchor.TopRight or ScrollSystem.Anchor.BottomRight)
                    origScreenPos.x += ScreenPosOffset.x;
                if (!isHori && anchorToFollow is ScrollSystem.Anchor.TopLeft or ScrollSystem.Anchor.TopRight)
                    origScreenPos.y += ScreenPosOffset.y;
                return origScreenPos;
            }
        }
    }
}
