using UnityEngine;
using RWCustom;
using System;
using static RainMeadow.MeadowProgression;

namespace RainMeadow
{
    public class EmoteRadialInput : HUD.HudPart
    {
        private Vector2 mainPos;
        EmoteRadialPage[] pages;
        private int currentPage;
        private MeadowAvatarCustomization customization;
        private MeadowHud emoteHandler;
        private FContainer secondaryContainer;

        private FLabel cancelLabel;
        private FSprite knobSprite;
        private bool lastActive;
        private Vector2 lastKnobPos;
        private Vector2 knobPos;
        private Vector2 knobVel;
        public int selected;
        private int lastSelected;
        private bool active;

        Emote[][] radialMappingPages;
        int npages;
        int emotesPerPage = 8;


        public EmoteRadialInput(HUD.HUD hud, MeadowAvatarCustomization customization, MeadowHud emoteHandler) : base(hud)
        {
            this.customization = customization;
            this.emoteHandler = emoteHandler;

            secondaryContainer = new FContainer();

            var emotes = MeadowProgression.AllAvailableEmotes(customization.character);
            var symbols = MeadowProgression.symbolEmotes;
            
            var emotePages = (emotes.Count - 1) / emotesPerPage + 1;
            var symbolPages = (symbols.Count - 1) / emotesPerPage + 1;
            npages = emotePages + symbolPages;
            RainMeadow.Debug($"emotePages: {symbolPages}; symbolPages:{symbolPages}");

            radialMappingPages = new Emote[emotePages + symbolPages][];

            for (int i = 0; i < emotePages; i++)
            {
                radialMappingPages[i] = new Emote[emotesPerPage];
                for (int j = 0; j < emotesPerPage; j++)
                {
                    var n = i * emotesPerPage + j;
                    if (n < emotes.Count)
                    {
                        radialMappingPages[i][j] = emotes[n];
                    }
                    else break;
                }
            }
            for (int i = 0; i < symbolPages; i++)
            {
                radialMappingPages[emotePages + i] = new Emote[emotesPerPage];
                for (int j = 0; j < emotesPerPage; j++)
                {
                    var n = i * emotesPerPage + j;
                    if (n < symbols.Count)
                    {
                        radialMappingPages[emotePages + i][j] = symbols[n];
                    }
                    else break;
                }
            }

            var cornerPos = new Vector2(hud.rainWorld.options.ScreenSize.x - 22f - Mathf.Max(30.01f, hud.rainWorld.options.SafeScreenOffset.x + 30.51f), Mathf.Max(30.01f, hud.rainWorld.options.SafeScreenOffset.y + 30.51f));
            var mainSize = 1.9f * 60f;
            mainPos = cornerPos + new Vector2(-mainSize, mainSize);

            this.pages = new[] {
                new EmoteRadialPage(hud, hud.fContainers[1], customization, radialMappingPages[0], mainPos, 60),
                new EmoteRadialPage(hud, this.secondaryContainer, customization, radialMappingPages[npages-1], mainPos + Custom.RotateAroundOrigo(Vector2.right, 45) * (mainSize + 40), 20),
                new EmoteRadialPage(hud, this.secondaryContainer, customization, radialMappingPages[1], mainPos + Custom.RotateAroundOrigo(Vector2.left, -45) * (mainSize + 40), 20)
            };

            this.currentPage = 0;

            this.knobSprite = new FSprite("Circle20", true);
            knobSprite.alpha = 0.4f;
            this.secondaryContainer.AddChild(this.knobSprite);

            this.cancelLabel = new FLabel(Custom.GetFont(), "CANCEL");
            cancelLabel.SetPosition(mainPos);
            secondaryContainer.AddChild(cancelLabel);

            hud.fContainers[1].AddChild(this.secondaryContainer);
        }

        public override void Update()
        {
            this.lastActive = active;
            if (active)
            {
                this.lastKnobPos = this.knobPos;
                this.knobPos += this.knobVel;
                this.knobVel *= 0.5f;
            }
            
            var controller = Custom.rainWorld.options.controls[0].GetActiveController();
            if (controller is Rewired.Joystick joystick)
            {
                this.active = joystick.GetAxis(4) > 0.5f;
                // maybe unify this with creaturecontroller getspecialinput
                var analogDir = new Vector2(joystick.GetAxis(2), joystick.GetAxis(3));
                if (active)
                {
                    this.knobVel += (analogDir - this.knobPos) / 8f;
                    this.knobPos += (analogDir - this.knobPos) / 4f;
                }
            }
            else
            {
                var keyboard = controller as Rewired.Keyboard;
                this.active = keyboard.GetKey(KeyCode.A);
                if (active)
                {
                    var package = RWInput.PlayerInput(0);
                    this.knobVel -= this.knobPos / 6f;
                    this.knobVel.x += (float)package.x * 0.4f;
                    this.knobVel.y += (float)package.y * 0.4f;
                    this.knobPos.x += (float)package.x * 0.4f;
                    this.knobPos.y += (float)package.y * 0.4f;
                }
            }
            if (active)
            {
                if (this.knobPos.magnitude > 1f)
                {
                    Vector2 b = Vector2.ClampMagnitude(this.knobPos, 1f) - this.knobPos;
                    this.knobPos += b;
                    this.knobVel += b;
                }

                this.lastSelected = selected;
                if (knobPos.magnitude > 0.5f)
                {
                    this.selected = Mathf.FloorToInt((-Custom.Angle(Vector2.up, knobPos) + 382.5f) / 45f) % 8;
                }
                else
                {
                    selected = -1;
                }

                if (selected != lastSelected || !lastActive)
                {
                    pages[0].SetSelected(selected);
                }
            }

            if (!active && lastActive)
            {
                if(selected != -1)
                {
                    var selectedEmote = radialMappingPages[currentPage][selected];
                    if (selectedEmote != null)
                    {
                        emoteHandler.EmotePressed(selectedEmote);
                    }
                    selected = -1;
                }
                pages[0].ClearSelection();
            }
        }

        private void FlipPage(int v)
        {
            currentPage = (currentPage + npages + v) % npages;
            pages[0].SetEmotes(radialMappingPages[currentPage]);
            pages[1].SetEmotes(radialMappingPages[(currentPage + npages - 1) % npages]);
            pages[2].SetEmotes(radialMappingPages[(currentPage + 1) % npages]);
        }

        public override void Draw(float timeStacker)
        {
            secondaryContainer.isVisible = active;
            if (!active) return;
            InputUpdate();
            var outterRadius = 1.975f * 60f;
            this.knobSprite.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
            Vector2 vector2 = Vector2.Lerp(this.lastKnobPos, this.knobPos, timeStacker);
            this.knobSprite.x = vector2.x * (outterRadius - 18f) + 0.01f + mainPos.x;
            this.knobSprite.y = vector2.y * (outterRadius - 18f) + 0.01f + mainPos.y;
        }

        public void InputUpdate()
        {
            // need to be in draw for easy keydown events
            var controller = Custom.rainWorld.options.controls[0].GetActiveController();
            if (controller is Rewired.Joystick joystick)
            {
                if (joystick.GetButtonDown(5)) FlipPage(1);
                if (joystick.GetButtonDown(4)) FlipPage(-1);
            }
            else
            {
                var keyboard = controller as Rewired.Keyboard;
                if (keyboard.GetKeyDown(KeyCode.Tab)) FlipPage(keyboard.GetModifierKey(Rewired.ModifierKey.Shift) ? -1 : 1);
            }
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            secondaryContainer.RemoveAllChildren();
            secondaryContainer.RemoveFromContainer();
        }

        public class EmoteRadialPage
        {
            private Emote[] emotes;
            private MeadowAvatarCustomization customization;
            private TriangleMesh[] meshes;
            private FSprite[] icons;
            private FSprite[] tiles;
            private TriangleMesh centerMesh;
            

            public Color colorUnselected = new Color(0f, 0f, 0f, 0.2f);
            public Color colorSelected = new Color(1f, 1f, 1f, 0.2f);

            // relative to emote size
            const float innerRadiusFactor = 1f;
            const float outterRadiusFactor = 2.076f;
            const float emoteRadiusFactor = 1.42f;
            float innerRadius;
            float outterRadius;
            float emoteRadius;

            public EmoteRadialPage(HUD.HUD hud, FContainer container, MeadowAvatarCustomization customization, Emote[] emotes, Vector2 pos, float emotesSize)
            {
                this.customization = customization;
                this.meshes = new TriangleMesh[8];
                this.icons = new FSprite[8];
                this.tiles = new FSprite[8];
                this.centerMesh = new TriangleMesh("Futile_White", new TriangleMesh.Triangle[] { new(0, 1, 2), new(0, 2, 3), new(0, 3, 4), new(0, 4, 5), new(0, 5, 6), new(0, 6, 7), new(0, 7, 8), new(0, 8, 1), }, false);
                container.AddChild(this.centerMesh);

                this.innerRadius = innerRadiusFactor * emotesSize;
                this.outterRadius = outterRadiusFactor * emotesSize;
                this.emoteRadius = emoteRadiusFactor * emotesSize;

                centerMesh.color = colorUnselected;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 dira = RWCustom.Custom.RotateAroundOrigo(Vector2.up, (-1f + 2 * i) * (360f / 16f));
                    Vector2 dirb = RWCustom.Custom.RotateAroundOrigo(Vector2.up, (1f + 2 * i) * (360f / 16f));
                    this.meshes[i] = new TriangleMesh("Futile_White", new TriangleMesh.Triangle[] { new(0, 1, 2), new(2, 3, 0) }, false);

                    meshes[i].vertices[0] = pos + dira * innerRadius;
                    meshes[i].vertices[1] = pos + dira * outterRadius;
                    meshes[i].vertices[2] = pos + dirb * outterRadius;
                    meshes[i].vertices[3] = pos + dirb * innerRadius;

                    meshes[i].color = colorUnselected;

                    container.AddChild(meshes[i]);

                    tiles[i] = new FSprite("Futile_White");
                    tiles[i].scale = emotesSize / EmoteDisplayer.emoteSourceSize;
                    tiles[i].alpha = 0.6f;
                    tiles[i].SetPosition(pos + RWCustom.Custom.RotateAroundOrigo(Vector2.up * emoteRadius, (i) * (360f / 8f)));
                    container.AddChild(tiles[i]);

                    icons[i] = new FSprite("Futile_White");
                    icons[i].scale = emotesSize / EmoteDisplayer.emoteSourceSize;
                    icons[i].alpha = 0.6f;
                    icons[i].SetPosition(pos + RWCustom.Custom.RotateAroundOrigo(Vector2.up * emoteRadius, (i) * (360f / 8f)));
                    container.AddChild(icons[i]);
                    
                    centerMesh.vertices[i + 1] = pos + dira * innerRadius;
                }
                centerMesh.vertices[0] = pos;

                SetEmotes(emotes);
            }

            public void SetEmotes(Emote[] emotes)
            {
                this.emotes = emotes;
                for (int i = 0; i < icons.Length; i++)
                {
                    if (emotes[i] != null)
                    {
                        icons[i].SetElementByName(customization.GetEmote(emotes[i]));
                        icons[i].alpha = 0.6f;
                        tiles[i].SetElementByName(customization.GetBackground(emotes[i]));
                        tiles[i].alpha = 0.6f;
                    }
                    else
                    {
                        icons[i].alpha = 0f;
                        tiles[i].alpha = 0f;
                    }
                }
            }

            internal void SetSelected(int selected)
            {
                ClearSelection();
                if (selected > -1 && emotes[selected] != null) meshes[selected].color = colorSelected; else centerMesh.color = colorSelected;
            }

            internal void ClearSelection()
            {
                centerMesh.color = colorUnselected;
                for (int i = 0; i < 8; i++)
                {
                    meshes[i].color = colorUnselected;
                }
            }
        }
    }
}
