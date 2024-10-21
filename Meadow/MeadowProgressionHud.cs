using HUD;
using RWCustom;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowProgressionHud : HudPart
    {
        private FContainer progressionContainer;
        private TokenSparkIcon emotesIcon;
        private FLabel emotesLabel;
        private TokenSparkIcon skinsIcon;
        private FLabel skinsLabel;
        private TokenSparkIcon characterIcon;
        private FLabel characterLabel;
        private int emoteAnim;
        private int skinAnim;
        private int charAnim;
        private int progressionNeeded;
        private float progressionVisible;
        private Vector2 rootPos;
        private MeadowProgression.Emote newEmote;

        Vector2 DrawPos(float timeStacker, int index)
        {
            return rootPos + new Vector2(index * 40f, 0);
        }

        public MeadowProgressionHud(HUD.HUD hud) : base(hud)
        {
            this.hud = hud;

            rootPos = new Vector2(22f + Mathf.Max(55.01f, hud.rainWorld.options.SafeScreenOffset.x + 22.51f), Mathf.Max(45.01f, hud.rainWorld.options.SafeScreenOffset.y + 22.51f));
            this.progressionContainer = new FContainer();

            emotesIcon = new TokenSparkIcon(progressionContainer, MeadowProgression.TokenRedColor, DrawPos(1f, 0), 1.5f, 1f);
            emotesLabel = new FLabel(Custom.GetFont(), EmoteCountText);
            emotesLabel.SetPosition(DrawPos(1f, 0) + new Vector2(0, 20f));
            progressionContainer.AddChild(emotesLabel);

            skinsIcon = new TokenSparkIcon(progressionContainer, MeadowProgression.TokenBlueColor, DrawPos(1f, 1), 1.5f, 1f);
            skinsLabel = new FLabel(Custom.GetFont(), SkinCountText);
            skinsLabel.SetPosition(DrawPos(1f, 1) + new Vector2(0, 20f));
            progressionContainer.AddChild(skinsLabel);

            characterIcon = new TokenSparkIcon(progressionContainer, MeadowProgression.TokenGoldColor, DrawPos(1f, 2), 1.5f, 1f);
            characterLabel = new FLabel(Custom.GetFont(), CharacterCountText);
            characterLabel.SetPosition(DrawPos(1f, 2) + new Vector2(0, 20f));
            progressionContainer.AddChild(characterLabel);

            hud.fContainers[1].AddChild(progressionContainer);
        }

        string EmoteCountText => $"{MeadowProgression.progressionData.currentCharacterProgress.emoteUnlockProgress}/{MeadowProgression.emoteProgressTreshold}";
        string SkinCountText => $"{MeadowProgression.progressionData.currentCharacterProgress.skinUnlockProgress}/{MeadowProgression.skinProgressTreshold}";
        string CharacterCountText => $"{MeadowProgression.progressionData.characterUnlockProgress}/{MeadowProgression.characterProgressTreshold}";

        public override void Update()
        {
            base.Update();

            progressionNeeded = Mathf.Max(progressionNeeded - 1, 0);
            if (emoteAnim > 0)
            {
                progressionNeeded = 80;
                emoteAnim--;
                if (emoteAnim == 0)
                {
                    emotesLabel.text = EmoteCountText;
                    this.hud.fadeCircles.Add(new FadeCircle(this.hud, 10f, 10f, 0.82f, 50f, 4f, DrawPos(1f, 0), this.progressionContainer));
                    this.hud.PlaySound(SoundID.HUD_Food_Meter_Fill_Fade_Circle);
                    if (newEmote != null)
                    {
                        this.hud.parts.OfType<MeadowEmoteHud>().First().Refresh();
                        newEmote = null;
                    }
                }
            }
            if (skinAnim > 0)
            {
                progressionNeeded = 80;
                skinAnim--;
                if (skinAnim == 0)
                {
                    skinsLabel.text = SkinCountText;
                    this.hud.fadeCircles.Add(new FadeCircle(this.hud, 10f, 10f, 0.82f, 50f, 4f, DrawPos(1f, 1), this.progressionContainer));
                    this.hud.PlaySound(SoundID.HUD_Food_Meter_Fill_Fade_Circle);
                }
            }
            if (charAnim > 0)
            {
                progressionNeeded = 80;
                charAnim--;
                if (charAnim == 0)
                {
                    characterLabel.text = CharacterCountText;
                    this.hud.fadeCircles.Add(new FadeCircle(this.hud, 10f, 10f, 0.82f, 50f, 4f, DrawPos(1f, 2), this.progressionContainer));
                    this.hud.PlaySound(SoundID.HUD_Food_Meter_Fill_Fade_Circle);
                }
            }

            progressionVisible = Custom.LerpAndTick(progressionVisible, progressionNeeded > 0 ? 1f : 0f, progressionNeeded > 0 ? 0.025f : 0.01f, 0.01f);
            progressionContainer.alpha = progressionVisible;

            if (progressionVisible > 0f)
            {
                progressionContainer.isVisible = true;
                emotesIcon.Update();
                skinsIcon.Update();
                characterIcon.Update();
            }
            else
            {
                progressionContainer.isVisible = false; // cut off shaders that still draw at 0 alpha
            }
        }

        public void AnimateEmote() { emoteAnim = 40; }
        public void AnimateSkin() { skinAnim = 40; }
        public void AnimateChar() { charAnim = 40; }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);

            if (progressionVisible > 0f)
            {
                emotesIcon.Draw(timeStacker);
                skinsIcon.Draw(timeStacker);
                characterIcon.Draw(timeStacker);
            }
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            progressionContainer.RemoveFromContainer();
            progressionContainer.RemoveAllChildren();
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
            newEmote = emote;
        }

        internal void NewSkinUnlocked(MeadowProgression.Skin skin)
        {
            hud.textPrompt.AddMessage(hud.rainWorld.inGameTranslator.Translate("New skin unlocked"), 60, 160, true, true);
        }
    }
}