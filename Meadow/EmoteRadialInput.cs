using UnityEngine;
using RWCustom;
using System;
using static RainMeadow.MeadowProgression;

namespace RainMeadow
{
    public class EmoteRadialInput : HUD.HudPart
    {
        private float mainWheelSize;
        private float innerWheelSize;
        private Vector2 mainPos;
        EmoteRadialPage[] pages;
        private int currentPage;
        private MeadowAvatarCustomization customization;
        private MeadowHud owner;
        private FContainer mainContainer;
        private FContainer secondaryContainer;

        private FLabel centerLabel;
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
        private Rect buttonRect;
        private bool lastMouseDown;
        private bool visible;
        private Vector2 mouseOrigin;

        public EmoteRadialInput(HUD.HUD hud, MeadowAvatarCustomization customization, MeadowHud owner) : base(hud)
        {
            this.customization = customization;
            this.owner = owner;

            mainContainer = new FContainer();
            secondaryContainer = new FContainer();

            var emotes = MeadowProgression.AllAvailableEmotes(customization.character);
            var symbols = MeadowProgression.symbolEmotes;
            
            // todo add favorites page
            // rounded up div
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

            // bottom left
            var cornerPos = new Vector2(hud.rainWorld.options.ScreenSize.x - Mathf.Max(30.01f, hud.rainWorld.options.SafeScreenOffset.x + 15.51f), Mathf.Max(8.01f, hud.rainWorld.options.SafeScreenOffset.y + 4.51f));
            
            // size math
            var mainEmoteSize = 50f;
            var secondaryEmoteSize = 20f;
            mainWheelSize = 1.9f * mainEmoteSize;
            innerWheelSize = 0.9f * mainEmoteSize;
            mainPos = cornerPos + new Vector2(-mainWheelSize, mainWheelSize);

            this.pages = new[] {
                new EmoteRadialPage(hud, mainContainer, customization, radialMappingPages[0], mainPos, mainEmoteSize),
                new EmoteRadialPage(hud, secondaryContainer, customization, radialMappingPages[npages-1], mainPos + Custom.RotateAroundOrigo(Vector2.right, 45) * (mainWheelSize + 40), secondaryEmoteSize),
                new EmoteRadialPage(hud, secondaryContainer, customization, radialMappingPages[1], mainPos + Custom.RotateAroundOrigo(Vector2.left, -45) * (mainWheelSize + 40), secondaryEmoteSize)
            };

            var dots = new FSprite[8];
            Vector2 buttonPos = cornerPos + new Vector2(-1.44f * mainWheelSize, 0f);
            buttonRect = new Rect(buttonPos + new Vector2(-12, -12), new Vector2(24, 24));
            for (int i = 0; i < dots.Length; i++)
            {
                var pos = buttonPos + Custom.RotateAroundOrigo(Vector2.up, i * 360f / dots.Length) * 6f;
                hud.fContainers[1].AddChild(dots[i] = new FSprite("Futile_White")
                {
                    shader = Custom.rainWorld.Shaders["FlatLight"],
                    scale = 12f / 16f,
                    x = pos.x,
                    y = pos.y,
                });
            }

            visible = true;
            active = false;
            lastActive = active;
            selected = -1;

            this.currentPage = 0;

            this.knobSprite = new FSprite("Circle20", true);
            knobSprite.alpha = 0.4f;
            this.secondaryContainer.AddChild(this.knobSprite);

            this.centerLabel = new FLabel(Custom.GetFont(), "CANCEL");
            centerLabel.SetPosition(mainPos);
            mainContainer.AddChild(centerLabel);

            hud.fContainers[1].AddChild(this.mainContainer);
            hud.fContainers[1].AddChild(this.secondaryContainer);
        }

        public override void Update()
        {
            if (active)
            {
                this.lastKnobPos = this.knobPos;
                this.knobPos += this.knobVel;
                this.knobVel *= 0.5f;
            }

            var mouseDown = Input.GetMouseButton(0);
            if (mouseDown && !lastMouseDown && buttonRect.Contains((Vector2)Futile.mousePosition))
            {
                ToggleVisibility();
            }

            if (visible && !active) // mouse input
            {
                Vector2 offset = ((Vector2)Futile.mousePosition - this.mainPos);
                var mag = offset.magnitude;
                if(mag < this.mainWheelSize && mag > innerWheelSize)
                { 
                    this.selected = Mathf.FloorToInt((-Custom.Angle(Vector2.up, offset) + 382.5f) / 45f) % 8;
                    if (selected != lastSelected)
                    {
                        centerLabel.text = "CLEAR"; // could display name here
                        pages[0].SetSelected(selected);
                    }
                }
                else if (mag < this.innerWheelSize)
                {
                    selected = -1;
                    if (selected != lastSelected)
                    {
                        centerLabel.text = "CLEAR";
                        pages[0].SetSelected(selected);
                    }
                }
                else if (mag > this.mainWheelSize)
                {
                    selected = -2;
                    if (selected != lastSelected)
                    {
                        centerLabel.text = "";
                        pages[0].ClearSelection();
                    }
                }
                if (mouseDown && !lastMouseDown) // mouse confirm
                {
                    if (selected > -1)
                    {
                        var selectedEmote = radialMappingPages[currentPage][selected];
                        if (selectedEmote != null)
                        {
                            owner.EmotePressed(selectedEmote);
                        }
                        selected = -1;
                    }
                    else if (selected == -1) // center = CLEAR
                    {
                        owner.ClearEmotes();
                    }
                    pages[0].ClearSelection();
                }
            }

            // directional input
            var controller = Custom.rainWorld.options.controls[0].GetActiveController();
            if (controller is Rewired.Joystick joystick)
            {
                this.active = joystick.GetAxis(4) > 0.5f; // ideally should be button shouldnt it
                // maybe unify this with creaturecontroller getspecialinput
                var analogDir = new Vector2(joystick.GetAxis(2), joystick.GetAxis(3));
                if (active)
                {
                    this.knobVel += (analogDir - this.knobPos) / 8f;
                    this.knobPos += (analogDir - this.knobPos) / 4f;
                }
            }
            else if(controller is Rewired.Keyboard keyboard)
            {
                this.active = keyboard.GetKey(KeyCode.A);
                if (active)
                {
                    var mousePos = (Vector2)Futile.mousePosition;
                    if (!lastActive)
                    {
                        mouseOrigin = mousePos;
                    }
                    var mouseOffset = mousePos - mouseOrigin;
                    if (mouseOffset.magnitude > 10f)
                    {
                        var mouseDir = Vector2.ClampMagnitude(mouseOffset / 50f, 1f);
                        this.knobVel += (mouseDir - this.knobPos) / 2f;
                        this.knobPos += (mouseDir - this.knobPos) / 2f;
                    }
                    else
                    {
                        var package = RWInput.PlayerInput(0);
                        this.knobVel -= this.knobPos / 6f;
                        this.knobVel.x += (float)package.x * 0.4f;
                        this.knobVel.y += (float)package.y * 0.4f;
                        this.knobPos.x += (float)package.x * 0.4f;
                        this.knobPos.y += (float)package.y * 0.4f;
                    }
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
                        owner.EmotePressed(selectedEmote);
                    }
                    selected = -1;
                }
                pages[0].ClearSelection();
                knobPos = Vector2.zero;
                knobVel = Vector2.zero;
            }

            lastMouseDown = mouseDown;
            this.lastSelected = selected;
            lastActive = active;
        }

        private void ToggleVisibility()
        {
            visible = !visible;
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
            mainContainer.isVisible = visible;
            secondaryContainer.isVisible = active && visible;
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
            else if(controller is Rewired.Keyboard keyboard)
            {
                if (keyboard.GetKeyDown(KeyCode.Tab)) FlipPage(keyboard.GetModifierKey(Rewired.ModifierKey.Shift) ? -1 : 1);
            }
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            secondaryContainer.RemoveAllChildren();
            secondaryContainer.RemoveFromContainer();
        }
    }
}
