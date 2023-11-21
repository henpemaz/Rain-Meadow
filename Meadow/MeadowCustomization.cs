using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public partial class MeadowCustomization
    {
        public class CreatureCustomization
        {
            public MeadowProgression.SkinData skinData;
            public Color tint;
            public float tintAmount;

            public void ModifyBodyColor(ref Color originalBodyColor)
            {
                if (skinData.statsName != null) originalBodyColor = PlayerGraphics.SlugcatColor(skinData.statsName);
                if (skinData.baseColor.HasValue) originalBodyColor = skinData.baseColor.Value;
                originalBodyColor = Color.Lerp(originalBodyColor, tint, tintAmount);
            }

            public void ModifyEyeColor(ref Color originalEyeColor)
            {
                if (skinData.eyeColor.HasValue) originalEyeColor = skinData.eyeColor.Value;
            }
        }

        public static ConditionalWeakTable<Creature, CreatureCustomization> creatureCustomizations = new();

        public static void Customize(Creature creature, OnlinePhysicalObject oe)
        {
            if (OnlineManager.lobby.entities.Values.Select(em => em.entity).FirstOrDefault(e => e is PersonaSettingsEntity settings && settings.target == oe.id) is PersonaSettingsEntity settings)
            {
                settings.ApplyCustomizations(creature, oe);
            }

            if (oe.isMine && !oe.isTransferable) // persona, wish there was a better flag
            {
                CreatureController.BindCreature(creature);
            }
        }
    }
}
