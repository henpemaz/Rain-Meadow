using HUD;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;
using static RainMeadow.MeadowProgression;

namespace RainMeadow
{
    public class MeadowHud : HudPart
    {
        // common
        private RoomCamera camera;
        public RainWorldGame game;
        private Creature owner;
        private MeadowAvatarCustomization customization;

        // progression
        private FContainer container;
        private TokenSparkIcon emotesIcon;
        private FLabel emotesLabel;
        private TokenSparkIcon skinsIcon;
        private FLabel skinsLabel;
        private TokenSparkIcon characterIcon;
        private FLabel characterLabel;
        private int emoteAnim;
        private int skinAnim;
        private int charAnim;
        private int needed;
        private float visible;
        private Vector2 rootPos;

        // emote
        private EmoteDisplayer displayer;
        private EmoteGridInput kbmInput;
        private EmoteRadialInput controllerInput;

        Vector2 DrawPos(float timeStacker, int index)
        {
            return rootPos + new Vector2(index * 40f, 0);
        }

        public MeadowHud(HUD.HUD hud, RoomCamera camera, Creature owner) : base(hud)
        {
            this.hud = hud;
            this.camera = camera;
            this.game = camera.game;
            this.owner = owner;
            this.displayer = EmoteDisplayer.map.GetValue(owner, (c) => throw new KeyNotFoundException());
            this.customization = (MeadowAvatarCustomization)RainMeadow.creatureCustomizations.GetValue(owner, (c) => throw new KeyNotFoundException());

            if (!Futile.atlasManager.DoesContainAtlas("emotes_common"))
            {
                HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/emotes/emotes_common").name);
            }
            if (!Futile.atlasManager.DoesContainAtlas(customization.EmoteAtlas))
            {
                HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/emotes/" + customization.EmoteAtlas).name);
            }

            this.kbmInput = new EmoteGridInput(hud, customization, this);
            hud.AddPart(this.kbmInput);

            this.controllerInput = new EmoteRadialInput(hud, customization, this);
            hud.AddPart(this.controllerInput);


            rootPos = new Vector2(22f + Mathf.Max(55.01f, hud.rainWorld.options.SafeScreenOffset.x + 22.51f), Mathf.Max(45.01f, hud.rainWorld.options.SafeScreenOffset.y + 22.51f));
            this.container = new FContainer();
            
            emotesIcon = new TokenSparkIcon(container, MeadowProgression.TokenRedColor, DrawPos(1f, 0), 1.5f, 1f);
            emotesLabel = new FLabel(Custom.GetFont(), EmoteCountText);
            emotesLabel.SetPosition(DrawPos(1f, 0) + new Vector2(0, 20f));
            container.AddChild(emotesLabel);

            skinsIcon = new TokenSparkIcon(container, MeadowProgression.TokenBlueColor, DrawPos(1f, 1), 1.5f, 1f);
            skinsLabel = new FLabel(Custom.GetFont(), SkinCountText);
            skinsLabel.SetPosition(DrawPos(1f, 1) + new Vector2(0, 20f));
            container.AddChild(skinsLabel);

            characterIcon = new TokenSparkIcon(container, MeadowProgression.TokenGoldColor, DrawPos(1f, 2), 1.5f, 1f);
            characterLabel = new FLabel(Custom.GetFont(), CharacterCountText);
            characterLabel.SetPosition(DrawPos(1f, 2) + new Vector2(0, 20f));
            container.AddChild(characterLabel);

            hud.fContainers[1].AddChild(container);
        }

        string EmoteCountText => $"{MeadowProgression.progressionData.currentCharacterProgress.emoteUnlockProgress}/{MeadowProgression.emoteProgressTreshold}";
        string SkinCountText => $"{MeadowProgression.progressionData.currentCharacterProgress.skinUnlockProgress}/{MeadowProgression.skinProgressTreshold}";
        string CharacterCountText => $"{MeadowProgression.progressionData.characterUnlockProgress}/{MeadowProgression.characterProgressTreshold}";

        public override void Update()
        {
            base.Update();

            needed = Mathf.Max(needed - 1, 0);
            if (emoteAnim > 0)
            {
                needed = 80;
                emoteAnim--;
                if (emoteAnim == 0)
                {
                    emotesLabel.text = EmoteCountText;
                    this.hud.fadeCircles.Add(new FadeCircle(this.hud, 10f, 10f, 0.82f, 50f, 4f, DrawPos(1f, 0), this.container));
                    this.hud.PlaySound(SoundID.HUD_Food_Meter_Fill_Fade_Circle);
                }
            }
            if (skinAnim > 0)
            {
                needed = 80;
                skinAnim--;
                if (skinAnim == 0)
                {
                    skinsLabel.text = SkinCountText;
                    this.hud.fadeCircles.Add(new FadeCircle(this.hud, 10f, 10f, 0.82f, 50f, 4f, DrawPos(1f, 1), this.container));
                    this.hud.PlaySound(SoundID.HUD_Food_Meter_Fill_Fade_Circle);
                }
            }
            if (charAnim > 0)
            {
                needed = 80;
                charAnim--;
                if (charAnim == 0)
                {
                    characterLabel.text = CharacterCountText;
                    this.hud.fadeCircles.Add(new FadeCircle(this.hud, 10f, 10f, 0.82f, 50f, 4f, DrawPos(1f, 2), this.container));
                    this.hud.PlaySound(SoundID.HUD_Food_Meter_Fill_Fade_Circle);
                }
            }

            visible = Custom.LerpAndTick(visible, needed > 0 ? 1f : 0f, needed > 0 ? 0.025f : 0.01f, 0.01f);
            container.alpha = visible;

            if(visible > 0f)
            {
                container.isVisible = true;
                emotesIcon.Update();
                skinsIcon.Update();
                characterIcon.Update();
            }
            else
            {
                container.isVisible = false; // cut off shaders that still draw at 0 alpha
            }
        }

        public void AnimateEmote() { emoteAnim = 40; }
        public void AnimateSkin() { skinAnim = 40; }
        public void AnimateChar() { charAnim = 40; }

        public override void Draw(float timeStacker)
        {
            InputUpdate();
            base.Draw(timeStacker);

            if (visible > 0f)
            {
                emotesIcon.Draw(timeStacker);
                skinsIcon.Draw(timeStacker);
                characterIcon.Draw(timeStacker);
            }
        }

        private void InputUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                ClearEmotes();
            }
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            container.RemoveFromContainer();
            container.RemoveAllChildren();
            emotesIcon.ClearSprites();
            skinsIcon.ClearSprites();
            characterIcon.ClearSprites();
        }

        internal void NewCharacterUnlocked(MeadowProgression.Character chararcter)
        {
            hud.textPrompt.AddMessage(hud.rainWorld.inGameTranslator.Translate("New character unlocked"), 60, 160, true, true);
        }

        internal void NewEmoteUnlocked(MeadowProgression.Emote emote)
        {
            hud.textPrompt.AddMessage(hud.rainWorld.inGameTranslator.Translate("New emote unlocked"), 60, 160, true, true);
        }

        internal void NewSkinUnlocked(MeadowProgression.Skin skin)
        {
            hud.textPrompt.AddMessage(hud.rainWorld.inGameTranslator.Translate("New skin unlocked"), 60, 160, true, true);
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