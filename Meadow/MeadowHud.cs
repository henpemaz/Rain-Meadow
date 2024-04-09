using HarmonyLib;
using HUD;
using RWCustom;
using System;
using UnityEngine;

namespace RainMeadow
{
    internal class MeadowHud : HudPart
    {
        private RoomCamera self;
        private Creature owner;
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

        Vector2 DrawPos(float timeStacker, int index)
        {
            return rootPos + new Vector2(index * 40f, 0);
        }

        public MeadowHud(HUD.HUD hud, RoomCamera self, Creature owner) : base(hud)
        {
            this.hud = hud;
            this.self = self;
            this.owner = owner;

            rootPos = new Vector2(22f + Mathf.Max(55.01f, hud.rainWorld.options.SafeScreenOffset.x + 22.51f), Mathf.Max(45.01f, hud.rainWorld.options.SafeScreenOffset.y + 22.51f));
            this.container = new FContainer();
            
            emotesIcon = new TokenSparkIcon(container, MeadowProgression.TokenRedColor, DrawPos(1f, 0), 1.5f);
            emotesLabel = new FLabel(Custom.GetFont(), EmoteCountText);
            emotesLabel.SetPosition(DrawPos(1f, 0) + new Vector2(0, 20f));
            container.AddChild(emotesLabel);

            skinsIcon = new TokenSparkIcon(container, MeadowProgression.TokenBlueColor, DrawPos(1f, 1), 1.5f);
            skinsLabel = new FLabel(Custom.GetFont(), SkinCountText);
            skinsLabel.SetPosition(DrawPos(1f, 1) + new Vector2(0, 20f));
            container.AddChild(skinsLabel);

            characterIcon = new TokenSparkIcon(container, MeadowProgression.TokenGoldColor, DrawPos(1f, 2), 1.5f);
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
                needed = 60;
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
                needed = 60;
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
                needed = 60;
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
                container.isVisible = false;
            }
        }

        public void AnimateEmote() { emoteAnim = 40; }
        public void AnimateSkin() { skinAnim = 40; }
        public void AnimateChar() { charAnim = 40; }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);

            if (visible > 0f)
            {
                emotesIcon.Draw(timeStacker);
                skinsIcon.Draw(timeStacker);
                characterIcon.Draw(timeStacker);
            }
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            container.RemoveAllChildren();
            container.RemoveFromContainer();
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

        public class TokenSparkIcon
        {
            private Color TokenColor;
            private Vector2[,] lines;
            //private Vector2 pos;
            //private Vector2[] trail;
            //private float sinCounter;
            private float sinCounter2;
            private FSprite[] sprites;
            private FContainer container;

            public TokenSparkIcon(FContainer hudContainer, Color color, Vector2 pos, float scale)
            {
                TokenColor = color;

                this.lines = new Vector2[4, 4];
                this.lines[0, 2] = new Vector2(-7f, 0f);
                this.lines[1, 2] = new Vector2(0f, 11f);
                this.lines[2, 2] = new Vector2(7f, 0f);
                this.lines[3, 2] = new Vector2(0f, -11f);
                this.sprites = new FSprite[6];
                this.sprites[0] = new FSprite("Futile_White", true);
                this.sprites[0].shader = Custom.rainWorld.Shaders["FlatLight"];
                this.sprites[1] = new FSprite("JetFishEyeA", true);
                this.sprites[1].shader = Custom.rainWorld.Shaders["Hologram"];
                for (int i = 0; i < 4; i++)
                {
                    this.sprites[(2 + i)] = new FSprite("pixel", true);
                    this.sprites[(2 + i)].anchorY = 0f;
                    this.sprites[(2 + i)].shader = Custom.rainWorld.Shaders["Hologram"];
                }

                float num = 0.2f;
                float num2 = 0f;
                float num3 = 1f;
                Color goldColor = this.GoldCol(num);
                this.sprites[1].color = goldColor;
                this.sprites[1].alpha = (1f - num) * Mathf.InverseLerp(0.5f, 0f, num2) * num3 * (1f);
                this.sprites[0].alpha = 0.9f * (1f - num) * Mathf.InverseLerp(0.5f, 0f, num2) * num3;
                this.sprites[0].scale = Mathf.Lerp(20f, 40f, num) / 16f;
                this.sprites[0].color = Color.Lerp(this.TokenColor, goldColor, 0.4f);

                this.container = new FContainer();
                container.SetPosition(pos);
                container.scale = scale; // yipee

                this.sprites.Do(s => container.AddChild(s));
                hudContainer.AddChild(container);
            }

            public void Update()
            {
                this.sinCounter2 += (1f + Mathf.Lerp(-10f, 10f, UnityEngine.Random.value) * 0.2f);
                float num = Mathf.Sin(this.sinCounter2 / 20f);
                num = Mathf.Pow(Mathf.Abs(num), 0.5f) * Mathf.Sign(num);
                var lenLines = this.lines.GetLength(0);
                for (int i = 0; i < lenLines; i++)
                {
                    this.lines[i, 1] = this.lines[i, 0];
                }
                for (int k = 0; k < lenLines; k++)
                {
                    if (Mathf.Pow(UnityEngine.Random.value, 0.1f + 0.2f * 5f) > this.lines[k, 3].x)
                    {
                        this.lines[k, 0] = Vector2.Lerp(this.lines[k, 0], new Vector2(this.lines[k, 2].x * num, this.lines[k, 2].y), Mathf.Pow(UnityEngine.Random.value, 1f + this.lines[k, 3].x * 17f));
                    }
                    if (UnityEngine.Random.value < Mathf.Pow(this.lines[k, 3].x, 0.2f) && UnityEngine.Random.value < Mathf.Pow(0.2f, 0.8f - 0.4f * this.lines[k, 3].x))
                    {
                        this.lines[k, 0] += Custom.RNV() * 17f * this.lines[k, 3].x;
                        this.lines[k, 3].y = Mathf.Max(this.lines[k, 3].y, 0.2f);
                    }
                    this.lines[k, 3].x = Custom.LerpAndTick(this.lines[k, 3].x, this.lines[k, 3].y, 0.01f, 0.033333335f);
                    this.lines[k, 3].y = Mathf.Max(0f, this.lines[k, 3].y - 0.014285714f);
                    if (UnityEngine.Random.value < 1f / Mathf.Lerp(210f, 20f, 0.2f))
                    {
                        this.lines[k, 3].y = Mathf.Max(0.2f, (UnityEngine.Random.value < 0.5f) ? 0.2f : UnityEngine.Random.value);
                    }
                }
            }

            public Color GoldCol(float g)
            {
                return Color.Lerp(this.TokenColor, new Color(1f, 1f, 1f), 0.4f + 0.4f * Mathf.Max(0, Mathf.Pow(g, 0.5f)));
            }

            public void Draw(float timeStacker)
            {
                float num = 0.2f;
                float num2 = 0f;
                float num3 = 1f;
                Color goldColor = this.GoldCol(num);
                
                for (int i = 0; i < 4; i++)
                {
                    Vector2 vector2 = Vector2.Lerp(this.lines[i, 1], this.lines[i, 0], timeStacker);
                    int num4 = (i == 3) ? 0 : (i + 1);
                    Vector2 vector3 = Vector2.Lerp(this.lines[num4, 1], this.lines[num4, 0], timeStacker);
                    float num5 = 1f - (1f - Mathf.Max(this.lines[i, 3].x, this.lines[num4, 3].x)) * (1f - num);
                    num5 = Mathf.Pow(num5, 2f);
                    num5 *= 1f - num2;
                    if (UnityEngine.Random.value < num5)
                    {
                        vector3 = Vector2.Lerp(vector2, vector3, UnityEngine.Random.value);
                    }
                    this.sprites[(2 + i)].x = vector2.x;
                    this.sprites[(2 + i)].y = vector2.y;
                    this.sprites[(2 + i)].scaleY = Vector2.Distance(vector2, vector3);
                    this.sprites[(2 + i)].rotation = Custom.AimFromOneVectorToAnother(vector2, vector3);
                    this.sprites[(2 + i)].alpha = (1f - num5) * num3 * 1f;
                    this.sprites[(2 + i)].color = goldColor;
                }
            }

            public void ClearSprites()
            {
                this.sprites.Do(s => s.RemoveFromContainer());
            }
        }
    }
}