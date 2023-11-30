using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using RWCustom;

namespace RainMeadow
{
    public partial class MeadowCustomization
    {
        public class CreatureCustomization
        {
            public MeadowProgression.Skin skin;
            public MeadowProgression.SkinData skinData;
            public Color tint;
            public float tintAmount;

            public CreatureCustomization(MeadowProgression.Skin skin, Color tint, float tintAmount)
            {
                this.skin = skin;
                this.skinData = MeadowProgression.skinData[skin];
                this.tint = new(tint.r, tint.g, tint.b);
                this.tintAmount = tintAmount * skinData.tintFactor;
            }

            internal string GetEmote(EmoteType emote)
            {
                if (emote.value.StartsWith("emote"))
                {
                    return (skinData.emotePrefix ?? MeadowProgression.characterData[skinData.character].emotePrefix) + emote.value;
                }
                return emote.value;
            }

            internal Color EmoteTileColor()
            {
                var color = skinData.emoteTileColor;
                return Color.Lerp(color, tint, tintAmount);
            }

            internal void ModifyBodyColor(ref Color originalBodyColor)
            {
                if (skinData.statsName != null) originalBodyColor = PlayerGraphics.SlugcatColor(skinData.statsName);
                if (skinData.baseColor.HasValue) originalBodyColor = skinData.baseColor.Value;
                originalBodyColor = Color.Lerp(originalBodyColor, tint, tintAmount);
            }

            internal void ModifyEyeColor(ref Color originalEyeColor)
            {
                if (skinData.eyeColor.HasValue) originalEyeColor = skinData.eyeColor.Value;
            }
        }

        public static ConditionalWeakTable<Creature, CreatureCustomization> creatureCustomizations = new();

        internal static void Customize(Creature creature, OnlineCreature oc)
        {
            if (MeadowAvatarSettings.map.TryGetValue(oc.owner, out MeadowAvatarSettings mas))
            {
                var mcc = mas.MakeCustomization();
                MeadowCustomization.creatureCustomizations.Add(creature, mcc); // for easier finding
                if (oc.gameModeData is MeadowCreatureData mcd)
                {
                    EmoteDisplayer.map.Add(creature, new EmoteDisplayer(creature, mcd, mcc));
                }
                else
                {
                    RainMeadow.Error("missing mcd?? " + oc);
                }
            }
            else
            {
                RainMeadow.Error("missing mas?? " + oc);
            }

            if(oc.isMine && !oc.isTransferable) // persona, wish there was a better flag
            {
                // playable creatures
                CreatureController.BindCreature(creature);
            }
        }
    }
}
