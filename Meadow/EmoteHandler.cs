using RWCustom;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RainMeadow
{
    public class EmoteType : ExtEnum<EmoteType>
    {
        public EmoteType(string value, bool register = false) : base(value, register) { }

        // emotions
        public static EmoteType emoteHello = new EmoteType("emoteHello", true);
        public static EmoteType emoteHappy = new EmoteType("emoteHappy", true);
        public static EmoteType emoteSad = new EmoteType("emoteSad", true);
        public static EmoteType emoteConfused = new EmoteType("emoteConfused", true);
        public static EmoteType emoteGoofy = new EmoteType("emoteGoofy", true);
        public static EmoteType emoteDead = new EmoteType("emoteDead", true);
        public static EmoteType emoteAmazed = new EmoteType("emoteAmazed", true);
        public static EmoteType emoteShrug = new EmoteType("emoteShrug", true);
        public static EmoteType emoteHug = new EmoteType("emoteHug", true);
        public static EmoteType emoteAngry = new EmoteType("emoteAngry", true);
        public static EmoteType emoteWink = new EmoteType("emoteWink", true);
        public static EmoteType emoteMischievous = new EmoteType("emoteMischievous", true);

        // ideas
        public static EmoteType symbolYes = new EmoteType("symbolYes", true);
        public static EmoteType symbolNo = new EmoteType("symbolNo", true);
        public static EmoteType symbolQuestion = new EmoteType("symbolQuestion", true);
        public static EmoteType symbolTime = new EmoteType("symbolTime", true);
        public static EmoteType symbolSurvivor = new EmoteType("symbolSurvivor", true);
        public static EmoteType symbolFriends = new EmoteType("symbolFriends", true);
        public static EmoteType symbolGroup = new EmoteType("symbolGroup", true);
        public static EmoteType symbolKnoledge = new EmoteType("symbolKnoledge", true);
        public static EmoteType symbolTravel = new EmoteType("symbolTravel", true);
        public static EmoteType symbolMartyr = new EmoteType("symbolMartyr", true);

        // things
        public static EmoteType symbolCollectible = new EmoteType("symbolCollectible", true);
        public static EmoteType symbolFood = new EmoteType("symbolFood", true);
        public static EmoteType symbolLight = new EmoteType("symbolLight", true);
        public static EmoteType symbolShelter = new EmoteType("symbolShelter", true);
        public static EmoteType symbolGate = new EmoteType("symbolGate", true);
        public static EmoteType symbolEcho = new EmoteType("symbolEcho", true);
        public static EmoteType symbolPointOfInterest = new EmoteType("symbolPointOfInterest", true);
        public static EmoteType symbolTree = new EmoteType("symbolTree", true);
        public static EmoteType symbolIterator = new EmoteType("symbolIterator", true);

        // verbs
        // todo
    }

    class EmoteHandler : HUD.HudPart
    {
        public static void InitializeBuiltinTypes()
        {
            _ = EmoteType.emoteHappy;
            RainMeadow.Debug($"{ExtEnum<EmoteType>.values.entries.Count} emotes loaded");
        }

        private InputScheme currentInputScheme;

        static KeyCode[] alphaRow = new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0, KeyCode.Minus, KeyCode.Equals };
        static string[] keycodeNames = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };

        static EmoteType[,] keyboardMappingRows = new EmoteType[,]{
            {
                EmoteType.emoteHello,
                EmoteType.emoteHappy,
                EmoteType.emoteSad,
                EmoteType.emoteConfused,
                EmoteType.emoteGoofy,
                EmoteType.emoteDead,
                EmoteType.emoteAmazed,
                EmoteType.emoteShrug,
                EmoteType.emoteHug,
                EmoteType.emoteAngry,
                EmoteType.emoteWink,
                EmoteType.emoteMischievous,
            },{
                EmoteType.symbolYes,
                EmoteType.symbolNo,
                EmoteType.symbolQuestion,
                EmoteType.symbolTime,
                EmoteType.symbolSurvivor,
                EmoteType.symbolFriends,
                EmoteType.symbolGroup,
                EmoteType.symbolKnoledge,
                EmoteType.symbolTravel,
                EmoteType.symbolMartyr,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
            },{
                EmoteType.symbolCollectible,
                EmoteType.symbolFood,
                EmoteType.symbolLight,
                EmoteType.symbolShelter,
                EmoteType.symbolGate,
                EmoteType.symbolEcho,
                EmoteType.symbolPointOfInterest,
                EmoteType.symbolTree,
                EmoteType.symbolIterator,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
            }
        };

        enum InputScheme
        {
            none,
            kbm,
            controller
        }

        private readonly RoomCamera roomCamera;
        private Creature avatar;
        private MeadowAvatarCustomization customization;
        private FSprite[] emoteDisplayers;
        private FSprite[] emoteSeparators;
        private FLabel[] inputLabels;
        private FContainer hotbarContainer; // single element controlling culling instead of 80+
        private int currentKeyboardRow;
        private Room lastRoom;
        private Vector2 corner;
        private int nr;
        private int ne;
        private IntVector2 hoverPos;
        private EmoteDisplayer displayer;
        public const int emotePreviewSize = 40;
        public const int emotePreviewSpacing = 8;
        public const float emotePreviewOpacityActive = 0.8f;
        public const float emotePreviewOpacityInactive = 0.5f;

        public EmoteHandler(HUD.HUD hud, RoomCamera roomCamera, Creature avatar, MeadowAvatarCustomization customization) : base(hud)
        {
            RainMeadow.Debug($"EmoteHandler created for {avatar}");
            currentInputScheme = InputScheme.kbm;
            this.hotbarContainer = new FContainer();
            hud.fContainers[1].AddChild(hotbarContainer);
            this.roomCamera = roomCamera;
            this.avatar = avatar;
            this.displayer = EmoteDisplayer.map.GetValue(avatar, (c) => throw new KeyNotFoundException());
            this.customization = customization;
            hud.AddPart(new EmoteRadialMenu(hud, Enumerable.Range(0, keyboardMappingRows.GetLength(1))
                .Select(x => keyboardMappingRows[0, x])
                .ToArray(), customization, 60f));

            if (!Futile.atlasManager.DoesContainAtlas("emotes_common"))
            {
                HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/emotes/emotes_common").name);
            }
            if (!Futile.atlasManager.DoesContainAtlas(customization.EmoteAtlas))
            {
                HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/emotes/" + customization.EmoteAtlas).name);
            }

            // this is speficic to keyboard preview
            nr = keyboardMappingRows.GetLength(0);
            ne = keyboardMappingRows.GetLength(1);
            this.emoteDisplayers = new FSprite[ne * nr];
            this.inputLabels = new FLabel[ne];
            this.emoteSeparators = new FSprite[nr * (ne - 1)];
            var left = hud.rainWorld.options.ScreenSize.x / 2f - (emotePreviewSize + emotePreviewSpacing) * ((ne - 1) / 2f);
            corner = new(left - (emotePreviewSize + emotePreviewSpacing) / 2f, 0);
            for (int j = 0; j < nr; j++)
            {
                var y = emotePreviewSize / 2f + (emotePreviewSize + emotePreviewSpacing) * j;
                var ylabel = emotePreviewSize + (emotePreviewSize + emotePreviewSpacing) * (currentKeyboardRow);
                var alpha = j == currentKeyboardRow ? emotePreviewOpacityActive : emotePreviewOpacityInactive;
                for (int i = 0; i < ne; i++)
                {
                    float x = left + (emotePreviewSize + emotePreviewSpacing) * i;
                    if (i != 0) hotbarContainer.AddChild(emoteSeparators[j * (ne - 1) + i - 1] = new FSprite("listDivider")
                    {
                        scaleX = 40f / 140f,
                        rotation = 90f,
                        x = x - (emotePreviewSize + emotePreviewSpacing) / 2f,
                        y = y,
                        alpha = alpha / 2f
                    });
                    hotbarContainer.AddChild(emoteDisplayers[j * ne + i] = new FSprite(customization.GetEmote(keyboardMappingRows[j, i]))
                    {
                        scale = emotePreviewSize / EmoteDisplayer.emoteSourceSize,
                        x = x,
                        y = y,
                        alpha = alpha
                    });
                    if (j == currentKeyboardRow)
                    {
                        hotbarContainer.AddChild(inputLabels[i] = new FLabel("font", keycodeNames[i])
                        {
                            x = x,
                            y = ylabel,
                            alpha = alpha
                        });
                    }
                }
            }
        }

        public override void Draw(float timeStacker)
        {
            InputUpdate(); // here because 60fps for key events
            base.Draw(timeStacker);
        }

        public override void Update()
        {
            base.Update();

            if (this.lastRoom != this.roomCamera.room)
            {
                NewRoom(this.roomCamera.room);
            }

            //kb vs controller logic here
        }

        private void NewRoom(Room room)
        {
            this.lastRoom = room;
        }

        public void InputUpdate()
        {
            if (currentInputScheme == InputScheme.kbm)
            {
                for (int i = 0; i < alphaRow.Length; i++)
                {
                    if (Input.GetKeyDown(alphaRow[i]))
                    {
                        EmotePressed(keyboardMappingRows[currentKeyboardRow, i]);
                    }
                }
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    currentKeyboardRow = (currentKeyboardRow + 1) % keyboardMappingRows.GetLength(0);
                    UpdateDisplayers();
                }
                if (Input.GetKeyDown(KeyCode.Backspace))
                {
                    ClearEmotes();
                }
            }

            IntVector2 newHover = IntVector2.FromVector2(((Vector2)Futile.mousePosition - this.corner) / (emotePreviewSize + emotePreviewSpacing));
            if (newHover.x < 0 || newHover.x >= ne || newHover.y < 0 || newHover.y >= nr)
            {
                newHover = new(-1, -1);
            }
            if (newHover != hoverPos)
            {
                hoverPos = newHover;
                UpdateDisplayers();
            }
            if (hoverPos.x != -1 && Input.GetMouseButtonDown(0))
            {
                EmotePressed(keyboardMappingRows[hoverPos.y, hoverPos.x]);
            }
        }

        public void UpdateDisplayers()
        {
            var ylabel = emotePreviewSize + (emotePreviewSize + emotePreviewSpacing) * (currentKeyboardRow);
            for (int i = 0; i < ne; i++)
            {
                inputLabels[i].y = ylabel;
            }

            for (int j = 0; j < nr; j++)
            {
                var alpha = j == currentKeyboardRow ? emotePreviewOpacityActive : emotePreviewOpacityInactive;
                for (int i = 0; i < ne; i++)
                {
                    if (i != 0) emoteSeparators[j * (ne - 1) + i - 1].alpha = alpha / 2f;
                    emoteDisplayers[j * ne + i].alpha = (hoverPos.x == i && hoverPos.y == j) ? 1 : alpha;
                }
            }
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            hotbarContainer.RemoveFromContainer();
            hotbarContainer.RemoveAllChildren();
            hotbarContainer = null;
            emoteDisplayers = null;
            emoteSeparators = null;
            inputLabels = null;
        }

        private void EmotePressed(EmoteType emoteType)
        {
            RainMeadow.Debug(emoteType);
            if (displayer.AddEmoteLocal(emoteType))
            {
                RainMeadow.Debug("emote added");
                // todo play local input sound
                hud.owner.PlayHUDSound(SoundID.MENU_Checkbox_Check);
            }
        }

        private void ClearEmotes()
        {
            displayer.ClearEmotes();
            hud.owner.PlayHUDSound(SoundID.MENU_Checkbox_Uncheck);
        }
    }

    public class EmoteRadialMenu : HUD.HudPart
    {
        private readonly EmoteType[] emotes;
        private readonly MeadowAvatarCustomization customization;
        private FContainer container;
        private TriangleMesh[] meshes;
        private FSprite[] icons;
        private TriangleMesh centerMesh;
        private FSprite knobSprite;
        private Vector2 lastKnobPos;
        private Vector2 knobPos;
        private Vector2 knobVel;
        private int selected;
        private int lastSelected;

        Color colorUnselected = new Color(0f, 0f, 0f, 0.2f);
        Color colorSelected = new Color(1f, 1f, 1f, 0.2f);

        // relative to emote size
        const float innerRadiusFactor = 0.888f;
        const float outterRadiusFactor = 1.975f;
        const float emoteRadiusFactor = 1.32f;
        float innerRadius;
        float outterRadius;
        float emoteRadius;

        public EmoteRadialMenu(HUD.HUD hud, EmoteType[] emotes, MeadowAvatarCustomization customization, float emotesSize) : base(hud)
        {
            this.emotes = emotes;
            this.customization = customization;

            this.container = new FContainer();
            this.meshes = new TriangleMesh[8];
            this.icons = new FSprite[8];
            this.centerMesh = new TriangleMesh("Futile_White", new TriangleMesh.Triangle[] { new(0, 1, 2), new(0, 2, 3), new(0, 3, 4), new(0, 4, 5), new(0, 5, 6), new(0, 6, 7), new(0, 7, 8), new(0, 8, 1), }, false);
            this.container.AddChild(this.centerMesh);

            this.innerRadius = innerRadiusFactor * emotesSize;
            this.outterRadius = outterRadiusFactor * emotesSize;
            this.emoteRadius = emoteRadiusFactor * emotesSize;

            centerMesh.color = colorUnselected;
            for (int i = 0; i < 8; i++)
            {
                Vector2 dira = RWCustom.Custom.RotateAroundOrigo(Vector2.left, (-1f + 2 * i) * (360f / 16f));
                Vector2 dirb = RWCustom.Custom.RotateAroundOrigo(Vector2.left, (1f + 2 * i) * (360f / 16f));
                this.meshes[i] = new TriangleMesh("Futile_White", new TriangleMesh.Triangle[] { new(0, 1, 2), new(2, 3, 0) }, false);

                meshes[i].vertices[0] = dira * innerRadius;
                meshes[i].vertices[1] = dira * outterRadius;
                meshes[i].vertices[2] = dirb * outterRadius;
                meshes[i].vertices[3] = dirb * innerRadius;

                meshes[i].color = colorUnselected;

                this.container.AddChild(meshes[i]);

                icons[i] = new FSprite(customization.GetEmote(emotes[i]));
                icons[i].scale = emotesSize / EmoteDisplayer.emoteSourceSize;
                icons[i].alpha = 0.6f;
                icons[i].SetPosition(RWCustom.Custom.RotateAroundOrigo(Vector2.left * emoteRadius, (i) * (360f / 8f)));
                this.container.AddChild(icons[i]);

                centerMesh.vertices[i + 1] = dira * innerRadius;
            }


            this.knobSprite = new FSprite("Circle20", true);
            knobSprite.alpha = 0.4f;
            this.container.AddChild(this.knobSprite);

            hud.fContainers[1].AddChild(this.container);

            this.container.SetPosition(RWCustom.Custom.rainWorld.screenSize / 2f);
        }

        public override void Update()
        {
            this.lastKnobPos = this.knobPos;
            this.knobPos += this.knobVel;
            this.knobVel *= 0.5f;
            var control = RWCustom.Custom.rainWorld.options.controls[0];
            if (control.gamePad)
            {
                var analogDir = new Vector2(Input.GetAxisRaw("Joy1Axis4"), -Input.GetAxisRaw("Joy1Axis5")); // yes it's hardcoded, no I can't get rewired to work
                this.knobVel += (analogDir - this.knobPos) / 8f;
                this.knobPos += (analogDir - this.knobPos) / 4f;
            }
            else
            {
                var package = RWInput.PlayerInput(0, RWCustom.Custom.rainWorld);
                this.knobVel -= this.knobPos / 6f;
                this.knobVel.x += (float)package.x * 0.3f;
                this.knobVel.y += (float)package.y * 0.3f;
                this.knobPos.x += (float)package.x * 0.3f;
                this.knobPos.y += (float)package.y * 0.3f;
            }
            if (this.knobPos.magnitude > 1f)
            {
                Vector2 b = Vector2.ClampMagnitude(this.knobPos, 1f) - this.knobPos;
                this.knobPos += b;
                this.knobVel += b;
            }

            this.lastSelected = selected;
            if(knobPos.magnitude > 0.5f)
            {
                this.selected = Mathf.FloorToInt((-Custom.Angle(Vector2.left, knobPos) + 382.5f) / 45f) % 8;
            }
            else
            {
                selected = -1;
            }
        }

        public override void Draw(float timeStacker)
        {
            this.knobSprite.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
            Vector2 vector2 = Vector2.Lerp(this.lastKnobPos, this.knobPos, timeStacker);
            this.knobSprite.x = vector2.x * (outterRadius - 18f) + 0.01f;
            this.knobSprite.y = vector2.y * (outterRadius - 18f) + 0.01f;
            if(selected != lastSelected)
            {
                centerMesh.color = colorUnselected;
                for (int i = 0; i < 8; i++)
                {
                    meshes[i].color = colorUnselected;
                }
                if (selected > -1) meshes[selected].color = colorSelected; else centerMesh.color = colorSelected;
            }
        }
    }
}
