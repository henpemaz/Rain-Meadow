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
            public MeadowProgression.SkinData skinData;
            public Color tint;
            public float tintAmount;

            internal Color EmoteTileColor()
            {
                var color = skinData.emoteTileColor;
                return Color.Lerp(color, tint, tintAmount);
            }

            internal string GetEmote(EmoteType emote)
            {
                if (emote.value.StartsWith("emote"))
                {
                    return (skinData.emotePrefix ?? MeadowProgression.characterData[skinData.character].emotePrefix) + emote.value;
                }
                return emote.value;
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
            // loose definition of "this is someone's avatar"
            if (OnlineManager.lobby.entities.Values.Select(em => em.entity).FirstOrDefault(e => e is AvatarSettingsEntity settings && settings.target == oc.id) is AvatarSettingsEntity settings)
            {
                settings.ApplyCustomizations(creature, oc);
            }

            if(oc.isMine && !oc.isTransferable) // persona, wish there was a better flag
            {
                // playable creatures
                CreatureController.BindCreature(creature);
            }
        }
    }
}
