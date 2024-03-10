using System;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    public class EmoteType : ExtEnum<EmoteType>
    {
        public EmoteType(string value, bool register = false) : base(value, register) { }
        public static EmoteType none = new EmoteType("none", true);

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

    public class EmoteHandler : HUD.HudPart
    {
        public static void InitializeBuiltinTypes()
        {
            _ = EmoteType.emoteHappy;
            RainMeadow.Debug($"{ExtEnum<EmoteType>.values.entries.Count} emotes loaded");
        }

        private InputScheme currentInputScheme; // todo
        enum InputScheme
        {
            none,
            kbm,
            controller
        }

        private RoomCamera roomCamera;
        private Creature avatar;
        private EmoteDisplayer displayer;
        private MeadowAvatarCustomization customization;
        private EmoteKeyboardInput kbmInput;
        private EmoteControllerInput controllerInput;
        private Options.ControlSetup.Preset? currentPreset;

        public EmoteHandler(HUD.HUD hud, RoomCamera roomCamera, Creature avatar) : base(hud)
        {
            RainMeadow.Debug($"EmoteHandler created for {avatar}");
            currentInputScheme = InputScheme.none; // todo

            this.roomCamera = roomCamera;
            this.avatar = avatar;
            this.displayer = EmoteDisplayer.map.GetValue(avatar, (c) => throw new KeyNotFoundException());
            this.customization = (MeadowAvatarCustomization)RainMeadow.creatureCustomizations.GetValue(avatar, (c) => throw new KeyNotFoundException());

            if (!Futile.atlasManager.DoesContainAtlas("emotes_common"))
            {
                HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/emotes/emotes_common").name);
            }
            if (!Futile.atlasManager.DoesContainAtlas(customization.EmoteAtlas))
            {
                HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/emotes/" + customization.EmoteAtlas).name);
            }
        }

        private InputScheme SchemeForPreset(Options.ControlSetup.Preset? currentPreset)
        {
            if(currentPreset == Options.ControlSetup.Preset.None || currentPreset == Options.ControlSetup.Preset.KeyboardSinglePlayer)
            {
                return InputScheme.kbm;
            }
            return InputScheme.controller;
        }

        private void InitKeyboardInput()
        {
            this.kbmInput = new EmoteKeyboardInput(hud, customization, this);
            hud.AddPart(this.kbmInput);
        }

        private void InitControllerInput()
        {
            this.controllerInput = new EmoteControllerInput(hud, customization, this);
            hud.AddPart(this.controllerInput);
        }

        public override void Update()
        {
            base.Update();
            var newpreset = hud.rainWorld.options.controls[0].recentPreset;
            var newscheme = SchemeForPreset(newpreset);
            if (currentPreset != newpreset && currentInputScheme != newscheme)
            {

                if (currentInputScheme == InputScheme.kbm)
                {
                    this.kbmInput.slatedForDeletion = true;
                }
                else if (currentInputScheme == InputScheme.controller)
                {
                    this.controllerInput.slatedForDeletion = true;
                }

                if (newscheme == InputScheme.kbm)
                {
                    InitKeyboardInput();
                }
                else if (newscheme == InputScheme.controller)
                {
                    InitControllerInput();
                }
            }
            currentPreset = newpreset;
            currentInputScheme = newscheme;
        }

        public void EmotePressed(EmoteType emoteType)
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
    }
}
