using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowCustomization
    {
        public class CreatureCustomization
        {
            public MeadowProgression.SkinData skinData;
            public Color tint;
            public float tintAmount;

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

        internal static void Customize(AbstractCreature creature, OnlinePhysicalObject oe)
        {
            if(OnlineManager.lobby.entities.Keys.FirstOrDefault(e=> e is PersonaSettingsEntity settings && settings.target == oe.id) is PersonaSettingsEntity settings)
            {
                settings.ApplyCustomizations(creature, oe);
            }
        }
    }
}
