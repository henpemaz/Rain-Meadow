using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Interfaces;
using RainMeadow.UI.Systems;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class UIMask : RectangularMenuObject
    {
        public static bool debug = false;
        public readonly string IDForTexture;
        public bool dirty = true;
        public UICamera uiCam;
        public FTexture? insideTexture;
        public FContainer camContainer, maskContainer;
        public FSprite? maskRect, camRect;
        public Vector2 initialCamPos, _camViewSizeOffset, _camViewPosOffset;
        public Vector2 CamViewPosOffset
        {
            get => _camViewPosOffset; set
            {
                if (value == _camViewPosOffset) return;
                _camViewPosOffset = value;
                dirty = true;
            }
        }
        public Vector2 CamViewSizeOffset
        {
            get => _camViewSizeOffset; set
            {
                if (value == _camViewSizeOffset) return;
                _camViewSizeOffset = value;
                dirty = true;
            }
        }
        public bool MouseOverTexture => WithinBounds(menu.mousePosition, default);
        public bool IsHidden { get; set; }
        public UIMask(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, FContainer itemMaskContainer) : base(menu, owner, pos, size)
        {
            maskContainer = itemMaskContainer;
            itemMaskContainer.container.AddChild(camContainer = new());
            camContainer.MoveBehindOtherNode(itemMaskContainer);
            uiCam = new(menu, this, new(0, 0), size);

            IDForTexture = "UIMASKCAM" + uiCam.index;
            initialCamPos = new Vector2(10000f + 10300f * uiCam.index, 10000f);
            uiCam.pos = initialCamPos;

            maskContainer.SetPosition(initialCamPos);

            subObjects.Add(uiCam);
            uiCam.OnCameraRenderTextureMade += SetMaskTexture;

            if (!debug) return;
            maskRect = new("pixel")
            {
                anchorX = 0,
                anchorY = 0,
                color = Color.red,
            };
            camRect = new("pixel")
            {
                anchorX = 0,
                anchorY = 0,
                color = Color.blue,
            };
            Container.AddChild(maskRect);
            Container.AddChild(camRect);
        }
        public bool WithinBounds(Vector2 screenPos, Vector2 size)
        {
            Vector2 startScreenPos = uiCam.ScreenPos - initialCamPos;
            Vector2 localElementPos = screenPos - startScreenPos;
            Vector2 elementEndScreenPos = localElementPos + size;


            return elementEndScreenPos.x >= 0 && elementEndScreenPos.x <= uiCam.size.x
              && elementEndScreenPos.y >= 0 && elementEndScreenPos.y <= uiCam.size.y;
        }
        public void SetMaskTexture(RenderTexture rt)
        {
            if (insideTexture == null)
            {
                insideTexture = new FTexture(rt, IDForTexture)
                {
                    anchorX = 0,
                    anchorY = 0
                };
                camContainer.AddChild(insideTexture);
                return;
            }
            insideTexture.SetTexture(rt);
        }
        public override void RemoveSprites()
        {
            maskContainer.SetPosition(0, 0);
            camContainer.RemoveFromContainer();
            base.RemoveSprites();
        }
        public override void Update()
        {
            if (dirty || lastSize != size)
            {
                uiCam.size = CamViewSizeOffset + size;
                uiCam.pos = initialCamPos + CamViewPosOffset;
            }
            base.Update();
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 screenPos = DrawPos(timeStacker);
            Vector2 camScreenPos = uiCam.ScreenPos, camSize = uiCam.size;
            Vector2 insideTexPos = camScreenPos - initialCamPos;
            maskContainer.SetPosition(initialCamPos);
            if (insideTexture != null)
                insideTexture.SetPosition(insideTexPos);
            if (maskRect == null) return;
            maskRect.alpha = 0.25f;
            maskRect.SetPosition(screenPos);
            maskRect.scaleX = size.x;
            maskRect.scaleY = size.y;
            camRect!.alpha = 0.25f;
            camRect.SetPosition(insideTexPos);
            camRect.scaleX = camSize.x;
            camRect.scaleY = camSize.y;
        }
        public class UICamera : RectangularMenuObject, IPLEASEUPDATEME
        {
            public readonly int index;
            public Camera cam;
            public RenderTexture? cameraRT;
            public FSprite debugRect;
            public Vector3 camPos;
            public bool camDirty = true, _isHidden;
            public event Action<RenderTexture> OnCameraRenderTextureMade;
            public bool IsHidden 
            { get => _isHidden;
                set
                {
                    if (_isHidden == value) return;
                    _isHidden = value;
                    if (value)
                    {
                        DestroyRender();
                        cam.enabled = false;
                        return;
                    }
                    cam.enabled = true;
                    CreateRender();
                }
            }
            public UICamera(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
            {
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
                    index = OpScrollBox._cameras.Count - 1;
                    OpScrollBox._cameras.Add(cam);
                }
                this.index = index;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }
            public void CreateRender()
            {
                int width = Mathf.CeilToInt(size.x);
                int height = Mathf.CeilToInt(size.y);
                cameraRT = new RenderTexture(width, height, 8, RenderTextureFormat.ARGB32)
                {
                    filterMode = FilterMode.Point
                };
                cam.targetTexture = cameraRT;
                OnCameraRenderTextureMade?.Invoke(cameraRT);
            }
            public void DestroyRender()
            {
                if (cameraRT)
                {
                    cameraRT!.Release();
                    UnityEngine.Object.Destroy(cameraRT);
                }
            }
            public void RefreshCamera()
            {
                cam.enabled = true;
                float sizeX = size.x;
                float sizeY = size.y;
                float posXOffset = sizeX * 0.5f;
                float posYOffset = sizeY * 0.5f;
                ;
                cam.aspect = sizeX / sizeY;
                cam.orthographic = true;
                cam.orthographicSize = sizeY / 2f;
                cam.nearClipPlane = 1f;
                cam.farClipPlane = 100f;
                camPos = new Vector3(posXOffset, posYOffset, -50f);
                cam.depth = -1000f;

                int width = Mathf.CeilToInt(sizeX);
                int height = Mathf.CeilToInt(sizeY);
                DestroyRender();
                CreateRender();

            }
            public override void RemoveSprites()
            {
                DestroyRender();
                if (cam)
                    UnityEngine.Object.Destroy(cam.gameObject);
                base.RemoveSprites();
            }
            public override void Update()
            {
                if (lastSize != size)
                    camDirty = true;
                if (camDirty)
                    RefreshCamera();
                base.Update();
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                var screenPos = DrawPos(timeStacker);
                cam?.transform.position = camPos + (Vector3)screenPos;
                if (debugRect == null) return;
                debugRect.SetPosition(screenPos);
                debugRect.scaleX = size.x;
                debugRect.scaleY = size.y;
            }
        }
    }
}
