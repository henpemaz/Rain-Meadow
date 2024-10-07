using HUD;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using static RainMeadow.MeadowProgression;

namespace RainMeadow
{
    public class MeadowEmoteHud : HudPart
    {
        // common
        private RoomCamera camera;
        private RainWorldGame game;
        private CreatureController creatureController;
        private Creature owner;
        private MeadowAvatarCustomization customization;

        // emote
        private EmoteDisplayer displayer;
        private Rect gridRect;

        // ui
        private bool lastMouseDown;
        private Vector2 lastMousePos;

        // grid
        private bool gridVisible;
        private int uiNeeded;
        private float uiFade;
        private EmoteGridDisplay gridDisplay;
        private Emote[,] gridEmotes;
        private int gridRows;
        private int gridColumns;
        private Rect gridButtonRect;
        private FContainer gridButtonContainer;
        private IntVector2 gridHover;

        // radial
        private bool radialVisible;
        private EmoteRadialDisplayer radialDisplayer;
        private Emote[][] radialEmotes;
        private int radialPagesCount;
        private float mainWheelRad;
        private float innerWheelRad;
        private Vector2 mainWheelPos;
        private Rect radialButtonRect;
        public int radialSelected;
        private int lastRadialSelected;
        private FLabel centerLabel;
        private FContainer radialButtonContainer;

        // picker
        private bool radialPickerActive;
        private bool lastRadialPickerActive;
        private int currentPage;
        private FSprite knobSprite;
        private Vector2 lastKnobPos;
        private Vector2 knobPos;
        private Vector2 knobVel;
        private Vector2 mouseOrigin;
        private Emote clickedEmote;
        private FSprite draggedEmote;
        private FSprite draggedTile;
        private bool mouseDown;
        private Vector2 mousePos;
        private Player.InputPackage lastPackage;
        private bool dragging;
        private FLabel pageLabel;
        private Player.InputPackage package;
        private float uiLastFade;

        UnityEngine.KeyCode[] alphakeys = new KeyCode[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8 };

        public MeadowEmoteHud(HUD.HUD hud, RoomCamera camera, Creature owner) : base(hud)
        {
            this.camera = camera;
            this.owner = owner;
            this.game = camera.game;
            this.creatureController = CreatureController.creatureControllers.GetValue(owner, (c) => throw new KeyNotFoundException());

            this.displayer = EmoteDisplayer.map.GetValue(owner, (c) => throw new KeyNotFoundException());
            this.customization = (MeadowAvatarCustomization)RainMeadow.creatureCustomizations.GetValue(owner, (c) => throw new KeyNotFoundException());
            var emotes = MeadowProgression.AllAvailableEmotes(customization.character);
            var symbols = MeadowProgression.symbolEmotes;

            // grid
            int iconsPerColumn = 3;
            var emoteColumns = (emotes.Count - 1) / iconsPerColumn + 1;
            var symbolColumns = (symbols.Count - 1) / iconsPerColumn + 1;

            RainMeadow.Debug($"grid: {iconsPerColumn} by {emoteColumns}+{symbolColumns}");
            gridEmotes = new Emote[iconsPerColumn, emoteColumns + symbolColumns];

            for (int i = 0; i < emoteColumns; i++)
            {
                for (int j = 0; j < iconsPerColumn; j++)
                {
                    var n = i * iconsPerColumn + j;
                    if (n < emotes.Count)
                    {
                        gridEmotes[j, i] = emotes[n];
                    }
                    else break;
                }
            }
            for (int i = 0; i < symbolColumns; i++)
            {
                for (int j = 0; j < iconsPerColumn; j++)
                {
                    var n = i * iconsPerColumn + j;
                    if (n < symbols.Count)
                    {
                        gridEmotes[j, emoteColumns + i] = symbols[n];
                    }
                    else break;
                }
            }

            gridRows = iconsPerColumn;
            gridColumns = emoteColumns + symbolColumns;

            var emoteCombinedSize = (EmoteGridDisplay.emotePreviewSize + EmoteGridDisplay.emotePreviewSpacing);
            var left = hud.rainWorld.options.ScreenSize.x / 2f - emoteCombinedSize * (gridColumns / 2f);
            var bottom = Mathf.Max(hud.rainWorld.options.SafeScreenOffset.y, EmoteGridDisplay.emotePreviewSpacing / 2f);
            gridRect = new Rect(left, bottom, emoteCombinedSize * gridColumns, emoteCombinedSize * gridRows);
            this.gridDisplay = new EmoteGridDisplay(hud.fContainers[1], customization, gridEmotes, gridRect.position);

            // a single sprite would have been better
            var gridButtonDots = new FSprite[6];
            var dotsPos = gridRect.position + new Vector2(-30f, 0);
            var dotsSize = new Vector2(30, 20);
            gridButtonRect = new Rect(dotsPos + new Vector2(-6, -8), dotsSize + new Vector2(12, 16));
            gridButtonContainer = new FContainer();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    gridButtonContainer.AddChild(gridButtonDots[i * 2 + j] = new FSprite("Futile_White")
                    {
                        shader = Custom.rainWorld.Shaders["FlatLight"],
                        scale = 12f / 16f,
                        x = dotsPos.x + 2f + i * 8f,
                        y = dotsPos.y + 2f + j * 8f,
                    });
                }
            }
            hud.fContainers[1].AddChild(gridButtonContainer);
            gridVisible = true;

            uiNeeded = 240;
            uiFade = 1f;

            RainMeadow.Debug($"grid: done");

            // radial

            var hotbarEmotes = MeadowProgression.progressionData.currentCharacterProgress.emoteHotbar;

            // rounded up div
            int emotesPerPage = 8;
            var emotePages = (emotes.Count - 1) / emotesPerPage + 1;
            var symbolPages = (symbols.Count - 1) / emotesPerPage + 1;
            radialPagesCount = 1 + emotePages + symbolPages;
            RainMeadow.Debug($"emotePages: {emotePages}; symbolPages:{symbolPages}");

            radialEmotes = new Emote[radialPagesCount][];
            radialEmotes[0] = new Emote[emotesPerPage];
            for (int n = 0; n < emotesPerPage; n++)
            {
                if (n < hotbarEmotes.Count)
                {
                    radialEmotes[0][n] = hotbarEmotes[n];
                }
                else break;
            }
            for (int i = 0; i < emotePages; i++)
            {
                radialEmotes[1 + i] = new Emote[emotesPerPage];
                for (int j = 0; j < emotesPerPage; j++)
                {
                    var n = i * emotesPerPage + j;
                    if (n < emotes.Count)
                    {
                        radialEmotes[1 + i][j] = emotes[n];
                    }
                    else break;
                }
            }
            for (int i = 0; i < symbolPages; i++)
            {
                radialEmotes[1 + emotePages + i] = new Emote[emotesPerPage];
                for (int j = 0; j < emotesPerPage; j++)
                {
                    var n = i * emotesPerPage + j;
                    if (n < symbols.Count)
                    {
                        radialEmotes[1 + emotePages + i][j] = symbols[n];
                    }
                    else break;
                }
            }

            // size math
            var radialRootPos = new Vector2(hud.rainWorld.options.ScreenSize.x - Mathf.Max(30.01f, hud.rainWorld.options.SafeScreenOffset.x + 15.51f), Mathf.Max(8.01f, hud.rainWorld.options.SafeScreenOffset.y + 4.51f));
            var radialEmoteSize = 50f;
            var secondaryEmoteSize = 20f;
            mainWheelRad = 1.9f * radialEmoteSize;
            innerWheelRad = 0.9f * radialEmoteSize;
            mainWheelPos = radialRootPos + new Vector2(-mainWheelRad, mainWheelRad);

            radialDisplayer = new EmoteRadialDisplayer(hud.fContainers[1], customization, radialEmotes[0], mainWheelPos, radialEmoteSize);

            var radialDots = new FSprite[8];
            Vector2 radialButtonPos = radialRootPos + new Vector2(-1.88f * mainWheelRad, 4f);
            radialButtonRect = new Rect(radialButtonPos + new Vector2(-12, -12), new Vector2(24, 24));
            radialButtonContainer = new FContainer();
            for (int i = 0; i < radialDots.Length; i++)
            {
                var pos = radialButtonPos + Custom.RotateAroundOrigo(Vector2.up, i * 360f / radialDots.Length) * 8f;
                radialButtonContainer.AddChild(radialDots[i] = new FSprite("Futile_White")
                {
                    shader = Custom.rainWorld.Shaders["FlatLight"],
                    scale = 10f / 16f,
                    x = pos.x,
                    y = pos.y,
                });
            }
            hud.fContainers[1].AddChild(radialButtonContainer);

            hud.fContainers[1].AddChild(this.knobSprite = new FSprite("Circle20", true)
            {
                alpha = 0.4f,
                color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey)
            });

            this.centerLabel = new FLabel(Custom.GetFont(), "CANCEL");
            centerLabel.SetPosition(mainWheelPos);
            hud.fContainers[1].AddChild(centerLabel);

            this.pageLabel = new FLabel(Custom.GetFont(), "");
            pageLabel.SetPosition(mainWheelPos + new Vector2(mainWheelRad, mainWheelRad));
            hud.fContainers[1].AddChild(pageLabel);

            radialVisible = true;

            // drag n drop
            hud.fContainers[1].AddChild(draggedTile = new FSprite("Futile_White")
            {
                scale = EmoteGridDisplay.emotePreviewSize / EmoteDisplayer.emoteSourceSize,
                isVisible = false
            });
            hud.fContainers[1].AddChild(draggedEmote = new FSprite("Futile_White")
            {
                scale = EmoteGridDisplay.emotePreviewSize / EmoteDisplayer.emoteSourceSize,
                isVisible = false
            });

            // cursor
            this.cursor = new MeadowMouseCursor(this, hud.fContainers[1]);
            this.hud.AddPart(this.cursor);
        }

        public override void Update()
        {
            base.Update();

            if(game.pauseMenu != null)
            {
                lastRadialPickerActive = false;
                radialPickerActive = false;
                dragging = false;
                return;
            }

            lastRadialSelected = radialSelected;
            lastRadialPickerActive = radialPickerActive;

            lastMouseDown = mouseDown;
            lastMousePos = mousePos;

            lastKnobPos = knobPos;

            mouseDown = Input.GetMouseButton(0);
            mousePos = (Vector2)Futile.mousePosition;

            lastPackage = package;
            package = RWInput.PlayerInput(0);

            uiLastFade = uiFade;

            if(mousePos != lastMousePos || dragging || this.gridHover.x != -1)
            {
                uiNeeded = Mathf.Max(uiNeeded, 80);
            }
            uiNeeded = Mathf.Max(uiNeeded - 1, 0);
            uiFade = Custom.LerpAndTick(uiFade, (uiNeeded > 0) ? 1f : 0f, 0.02f, 0.02f);

            // grid
            if (mouseDown && !lastMouseDown && gridButtonRect.Contains(mousePos))
            {
                if (toggleHidden) { toggleHidden = false; gridVisible = true; }
                else gridVisible = !gridVisible;
            }

            if (!toggleHidden && gridVisible && !radialPickerActive)
            {
                Vector2 offset = (mousePos - this.gridRect.position - new Vector2(0, gridRect.height)) * new Vector2(1, -1) / (EmoteGridDisplay.emotePreviewSize + EmoteGridDisplay.emotePreviewSpacing);
                IntVector2 newHover = new IntVector2(Mathf.FloorToInt(offset.x), Mathf.FloorToInt(offset.y));
                if (newHover.x < 0 || newHover.x >= gridColumns || newHover.y < 0 || newHover.y >= gridRows)
                {
                    newHover = new(-1, -1);
                }
                if (clickedEmote == null && newHover != gridHover)
                {
                    gridHover = newHover;
                    this.gridDisplay.SetSelected(gridHover);
                }
                if (mouseDown && !lastMouseDown && gridHover.x != -1)
                {
                    clickedEmote = gridEmotes[gridHover.y, gridHover.x];
                    mouseOrigin = mousePos;
                }
                else if(!mouseDown && lastMouseDown)
                {
                    if(clickedEmote != null && !dragging && newHover == gridHover) // still here
                    {
                        EmotePressed(clickedEmote);
                    }
                }
            }

            // radial menu
            if (mouseDown && !lastMouseDown && radialButtonRect.Contains((Vector2)Futile.mousePosition))
            {
                if (toggleHidden) { toggleHidden = false; radialVisible = true; }
                else radialVisible = !radialVisible;
            }

            if (!toggleHidden && radialVisible && !radialPickerActive) // mouse input
            {
                Vector2 offset = ((Vector2)Futile.mousePosition - this.mainWheelPos);
                var mag = offset.magnitude;
                if (mag < this.mainWheelRad && mag > innerWheelRad)
                {
                    this.radialSelected = Mathf.FloorToInt((-Custom.Angle(Vector2.up, offset) + 382.5f) / 45f) % 8;
                    if (radialSelected != lastRadialSelected)
                    {
                        centerLabel.text = "CLEAR"; // could display name here
                        radialDisplayer.SetSelected(radialSelected);
                    }
                }
                else if (mag < this.innerWheelRad)
                {
                    radialSelected = -1;
                    if (radialSelected != lastRadialSelected)
                    {
                        centerLabel.text = "CLEAR";
                        radialDisplayer.SetSelected(radialSelected);
                    }
                }
                else if (mag > this.mainWheelRad)
                {
                    radialSelected = -2;
                    if (radialSelected != lastRadialSelected)
                    {
                        centerLabel.text = "";
                        radialDisplayer.ClearSelection();
                    }
                }
                if (mouseDown && !lastMouseDown) // mouse confirm
                {
                    if (radialSelected > -1)
                    {
                        var selectedEmote = radialEmotes[currentPage][radialSelected];
                        if (selectedEmote != null)
                        {
                            EmotePressed(selectedEmote);
                        }
                        radialSelected = -1;
                    }
                    else if (radialSelected == -1) // center = CLEAR
                    {
                        ClearEmotes();
                    }
                    radialDisplayer.ClearSelection();
                }
            }

            // directional selector
            var controller = Custom.rainWorld.options.controls[0].GetActiveController();
            if (controller is Rewired.Joystick joystick)
            {
                this.radialPickerActive = joystick.GetButton(4);
                // maybe unify this with creaturecontroller getspecialinput
                // but creature doesn't update while in shortcuts
                var analogDir = new Vector2(joystick.GetAxis(2), joystick.GetAxis(3));
                if (radialPickerActive)
                {
                    this.knobVel += (analogDir - this.knobPos) / 8f;
                    this.knobPos += (analogDir - this.knobPos) / 4f;
                }
            }
            else if (controller is Rewired.Keyboard keyboard)
            {
                this.radialPickerActive = package.pckp;
                if (radialPickerActive)
                {
                    if (!lastRadialPickerActive)
                    {
                        mouseOrigin = mousePos;
                    }
                    var mouseOffset = mousePos - mouseOrigin;
                    if (mouseOffset.magnitude > 10f)
                    {
                        var mouseDir = Vector2.ClampMagnitude(mouseOffset / 40f, 1f);
                        this.knobVel += (mouseDir - this.knobPos) / 2f;
                        this.knobPos += (mouseDir - this.knobPos) / 2f;
                    }
                    else
                    {
                        creatureController.standStill = true;
                        this.knobVel -= this.knobPos / 6f;
                        this.knobVel.x += (float)package.x * 0.4f;
                        this.knobVel.y += (float)package.y * 0.4f;
                        this.knobPos.x += (float)package.x * 0.4f;
                        this.knobPos.y += (float)package.y * 0.4f;
                    }
                }
            }

            if (radialPickerActive)
            {
                if (!lastRadialPickerActive)
                {
                    centerLabel.text = "CANCEL";
                    FlipPage(-currentPage); // back to zero
                    knobPos = Vector2.zero;
                    knobVel = Vector2.zero;
                    lastKnobPos = Vector2.zero;
                }
                this.knobPos += this.knobVel;
                this.knobVel *= 0.5f;
                if (this.knobPos.magnitude > 1f)
                {
                    Vector2 b = Vector2.ClampMagnitude(this.knobPos, 1f) - this.knobPos;
                    this.knobPos += b;
                    this.knobVel += b;
                }
                if (knobPos.magnitude > 0.5f)
                {
                    this.radialSelected = Mathf.FloorToInt((-Custom.Angle(Vector2.up, knobPos) + 382.5f) / 45f) % 8;
                }
                else
                {
                    radialSelected = -1;
                }

                if (radialSelected != lastRadialSelected || !lastRadialPickerActive)
                {
                    radialDisplayer.SetSelected(radialSelected);
                }

                if (package.jmp && !lastPackage.jmp) FlipPage(-1);
                if (package.thrw && !lastPackage.thrw) FlipPage(1);

                pageLabel.text = $"{currentPage + 1}/{radialPagesCount}";
            }
            else
            {
                pageLabel.text = "";
                // let go to confirm
                if (lastRadialPickerActive)
                {
                    if (radialSelected != -1)
                    {
                        var selectedEmote = radialEmotes[currentPage][radialSelected];
                        if (selectedEmote != null)
                        {
                            EmotePressed(selectedEmote);
                        }
                        radialSelected = -1;
                    }
                    radialDisplayer.ClearSelection();
                    FlipPage(-currentPage); // back to zero
                    knobPos = Vector2.zero;
                    knobVel = Vector2.zero;
                    lastKnobPos = Vector2.zero;
                }
            }

            // drag n drop
            if (clickedEmote != null && !dragging && mouseDown && Vector2.Distance(mousePos, mouseOrigin) > 10f) // move from click pos to start
            {
                dragging = true;
                draggedEmote.SetElementByName(customization.GetEmote(clickedEmote));
                draggedTile.SetElementByName(customization.GetBackground(clickedEmote));
            }
            if(dragging && !mouseDown && clickedEmote != null) // let go to end
            {
                if(radialSelected >= 0)
                {
                    MeadowProgression.progressionData.currentCharacterProgress.emoteHotbar[radialSelected] = clickedEmote;
                    MeadowProgression.AutosaveProgression(true);

                    radialEmotes[0][radialSelected] = clickedEmote;
                    FlipPage(0);
                }
            }
            if (!mouseDown || game.pauseMenu != null)
            {
                dragging = false;
                clickedEmote = null;
            }
            if(mouseDown && clickedEmote != null)
            {
                creatureController.preventMouseInput = true;
            }
        }


        bool toggleHidden;
        bool fullyHidden;
        private MeadowMouseCursor cursor;

        public override void Draw(float timeStacker)
        {

            FrameInput();

            var hideAll = game.pauseMenu != null || fullyHidden;
            base.Draw(timeStacker);
            
            if (hideAll)
            {
                gridDisplay.isVisible = false;
                gridButtonContainer.isVisible = false;

                radialDisplayer.isVisible = false;
                radialButtonContainer.isVisible = false;

                draggedEmote.isVisible = false;
                draggedTile.isVisible = false;

                knobSprite.isVisible = false;
                centerLabel.isVisible = false;
            }
            else
            {
                var gridAlpha = Mathf.Lerp(uiFade, uiLastFade, timeStacker);
                gridDisplay.alpha = gridAlpha;
                gridDisplay.isVisible = (gridVisible && !toggleHidden);
                gridButtonContainer.isVisible = true;
                gridButtonContainer.alpha = (gridVisible && !toggleHidden) ? 0.8f : 0.4f;

                radialDisplayer.isVisible = (radialVisible && !toggleHidden) || radialPickerActive;
                radialButtonContainer.isVisible = true;
                radialButtonContainer.alpha = (radialVisible && !toggleHidden) ? 0.8f : 0.4f;

                draggedEmote.isVisible = dragging;
                draggedTile.isVisible = dragging;
                if (dragging)
                {
                    Vector2 mouseDrawPos = Vector2.Lerp(this.lastMousePos, this.mousePos, timeStacker);
                    draggedEmote.SetPosition(mouseDrawPos);
                    draggedTile.SetPosition(mouseDrawPos);
                }

                knobSprite.isVisible = radialPickerActive;
                Vector2 knobDrawPos = Vector2.Lerp(this.lastKnobPos, this.knobPos, timeStacker);
                this.knobSprite.x = knobDrawPos.x * (mainWheelRad - 18f) + mainWheelPos.x;
                this.knobSprite.y = knobDrawPos.y * (mainWheelRad - 18f) + mainWheelPos.y;

                centerLabel.isVisible = true;
            }
        }

        private void FrameInput()
        {
            // debug
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                toggleHidden = !toggleHidden;
                if (toggleHidden && game.devToolsActive && Input.GetKey(KeyCode.LeftShift)) fullyHidden = true;
                else fullyHidden = false;
            }

            // alpha row
            for (int i = 0; i < alphakeys.Length; i++)
            {
                if (Input.GetKeyDown(alphakeys[i]))
                {
                    var selectedEmote = radialEmotes[currentPage][i];
                    if (selectedEmote != null)
                    {
                        EmotePressed(selectedEmote);
                    }
                }
            }

            // backspace
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                ClearEmotes();
            }
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            gridDisplay.ClearSprites();
            gridButtonContainer.RemoveFromContainer();
            gridButtonContainer.RemoveAllChildren();
            radialDisplayer.ClearSprites();
            radialButtonContainer.RemoveFromContainer();
            radialButtonContainer.RemoveAllChildren();

            knobSprite.RemoveFromContainer();
            draggedEmote.RemoveFromContainer();
            draggedTile.RemoveFromContainer();
            centerLabel.RemoveFromContainer();
        }

        private void FlipPage(int v)
        {
            currentPage = (currentPage + radialPagesCount + v) % radialPagesCount;
            radialDisplayer.SetEmotes(radialEmotes[currentPage], customization);
        }

        public void EmotePressed(Emote emoteType)
        {
            RainMeadow.Debug(emoteType);
            if (displayer.AddEmoteLocal(emoteType))
            {
                RainMeadow.Debug("emote added");
                hud.owner.PlayHUDSound(SoundID.MENU_Checkbox_Check);
            }
        }

        public void ClearEmotes()
        {
            displayer.ClearEmotes();
            hud.owner.PlayHUDSound(SoundID.MENU_Checkbox_Uncheck);
        }

        internal void Refresh()
        {
            if(slatedForDeletion) { return; }
            slatedForDeletion = true;
            hud.AddPart(new MeadowEmoteHud(hud, camera, owner));
        }

        public class MeadowMouseCursor : HUD.HudPart
        {
            public MeadowMouseCursor(MeadowEmoteHud meadowEmoteHud, FContainer fContainer) : base(meadowEmoteHud.hud)
            {
                this.shadow = new FSprite("Futile_White", true);
                this.shadow.shader = meadowEmoteHud.game.manager.rainWorld.Shaders["FlatLight"];
                this.shadow.color = new Color(0f, 0f, 0f);
                this.shadow.scale = 4f;
                fContainer.AddChild(this.shadow);
                this.cursorSprite = new FSprite("Cursor", true);
                this.cursorSprite.anchorX = 0f;
                this.cursorSprite.anchorY = 1f;
                fContainer.AddChild(this.cursorSprite);
                this.meadowEmoteHud = meadowEmoteHud;
            }

            private bool Needed()
            {
                return meadowEmoteHud.uiNeeded > 0 && !UnityEngine.Cursor.visible;
            }

            public override void Update()
            {
                base.Update();
                this.lastFade = this.fade;
                this.fade = Custom.LerpAndTick(this.fade, Needed() ? 1f : 0f, 0.01f, 0.033333335f);
            }

            public override void Draw(float timeStacker)
            {
                base.Draw(timeStacker);
                this.cursorSprite.x = Futile.mousePosition.x + 0.01f;
                this.cursorSprite.y = Futile.mousePosition.y + 0.01f;
                this.shadow.x = Futile.mousePosition.x + 3.01f;
                this.shadow.y = Futile.mousePosition.y - 8.01f;
                float num = Custom.SCurve(Mathf.Lerp(this.lastFade, this.fade, timeStacker), 0.6f);
                this.cursorSprite.alpha = num;
                this.shadow.alpha = Mathf.Pow(num, 3f) * 0.3f;
            }

            public override void ClearSprites()
            {
                base.ClearSprites();
                this.shadow.RemoveFromContainer();
                this.cursorSprite.RemoveFromContainer();
            }

            private FSprite cursorSprite;

            private FSprite shadow;

            private float fade;
            private float lastFade;
            private readonly MeadowEmoteHud meadowEmoteHud;
        }
    }
}