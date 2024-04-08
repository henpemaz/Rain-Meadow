using System;
using System.Collections.Generic;
using UnityEngine;
using static RainMeadow.MeadowProgression;

namespace RainMeadow
{
    public class EmoteHandler : HUD.HudPart
    {
        public static void InitializeBuiltinTypes()
        {
            _ = Emote.emoteHappy;
            RainMeadow.Debug($"{ExtEnum<Emote>.values.entries.Count} emotes loaded");
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
    }
}
