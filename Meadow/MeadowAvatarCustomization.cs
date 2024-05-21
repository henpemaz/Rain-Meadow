using UnityEngine;
using static RainMeadow.MeadowProgression;

namespace RainMeadow
{
    /// <summary>
    /// Limited skin-and-tint customization, meadow-specific
    /// </summary>
    public class MeadowAvatarCustomization : ClientSettings.AvatarCustomization
    {
        public MeadowProgression.Skin skin;
        public MeadowProgression.SkinData skinData;
        public MeadowProgression.Character character;
        public MeadowProgression.CharacterData characterData;
        public Color tint;
        public float tintAmount;

        private Color emoteBgColor;
        private Color symbolBgColor;
        private Color emoteColor;
        private Color symbolColor;

        public MeadowAvatarCustomization(MeadowProgression.Skin skin, Color tint, float tintAmount)
        {
            this.skin = skin;
            this.skinData = MeadowProgression.skinData[skin];
            this.character = skinData.character;
            this.characterData = MeadowProgression.characterData[skinData.character];

            this.tint = new(tint.r, tint.g, tint.b);
            this.tintAmount = tintAmount * skinData.tintFactor;

            emoteBgColor = Color.Lerp(skinData.emoteColorOverride ?? characterData.emoteColor, this.tint, this.tintAmount);
            symbolBgColor = Color.white;
            emoteColor = Color.white;
            var v = RWCustom.Custom.RGB2HSL(Color.Lerp(Color.white, this.tint, this.tintAmount));
            // there used to be some hsl maths here
            symbolColor = new HSLColor(v[0], v[1], v[2]).rgb;
        }

        internal string EmoteAtlas => skinData.emoteAtlasOverride ?? characterData.emoteAtlas;
        internal string EmotePrefix => skinData.emotePrefixOverride ?? characterData.emotePrefix;

        internal override void ModifyBodyColor(ref Color originalBodyColor)
        {
            if (skinData.baseColor.HasValue) originalBodyColor = skinData.baseColor.Value;
            originalBodyColor = Color.Lerp(originalBodyColor, tint, tintAmount);
        }

        internal override void ModifyEyeColor(ref Color originalEyeColor)
        {
            if (skinData.eyeColor.HasValue) originalEyeColor = skinData.eyeColor.Value;
        }

        internal string GetEmote(Emote emote)
        {
            return (emote.value.StartsWith("emote") ? EmotePrefix + emote.value : emote.value).ToLowerInvariant();
        }

        internal string GetBackground(Emote emote)
        {
            return (emote.value.StartsWith("emote") ? "emote_background" : "symbols_background");
        }

        internal Color EmoteBackgroundColor(Emote emote)
        {
            return emote.value.StartsWith("emote") ? emoteBgColor : symbolBgColor;
        }

        internal Color EmoteColor(Emote emote)
        {
            return emote.value.StartsWith("emote") ? emoteColor : symbolColor;
        }
    }
}
