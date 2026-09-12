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
        public readonly string IDForTexture;
        public UICamera uiCam;
        public FTexture? insideTexture;
        public FContainer camContainer, maskContainer;
        public Vector2 initialCamPos, camViewSizeOffset, lastCamViewSizeOffset;
        public Vector2 camOffsetSizeAnchor = new(0.5f, 0.5f), lastCamOffsetSizeAnchor = new(0.5f, 0.5f);
        public Vector3 camViewPosOffset;
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
                    anchorX = 0.5f,
                    anchorY = 0.5f
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
            if (camViewSizeOffset != lastCamViewSizeOffset || lastSize != size || camOffsetSizeAnchor != lastCamOffsetSizeAnchor)
            {
                lastCamViewSizeOffset = camViewSizeOffset;
                lastCamOffsetSizeAnchor = camOffsetSizeAnchor;

                uiCam.size = camViewSizeOffset + size;
                uiCam.pos = initialCamPos + camViewSizeOffset * camOffsetSizeAnchor;
            }
            base.Update();
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 screenPos = DrawPos(timeStacker);
            maskContainer.SetPosition(initialCamPos);
            if (insideTexture != null)
            {
                Vector2 camScreenPos = uiCam.ScreenPos, camSize = uiCam.size;
                insideTexture.SetPosition(camScreenPos - initialCamPos + camSize * camOffsetSizeAnchor);
                insideTexture.anchorX = camOffsetSizeAnchor.x;
                insideTexture.anchorY = camOffsetSizeAnchor.y;
            }
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
