using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using RWCustom;

namespace RainMeadow
{
    public partial class StoryCustomization
    {
        public class CreatureCustomization
        {
            public SkinSelection.Skin skin;
            private SkinSelection.SkinData skinData;
            private SkinSelection.CharacterData characterData;
            public Color tint;
            public float tintAmount;

            public CreatureCustomization(SkinSelection.Skin skin, Color tint, float tintAmount)
            {
                this.skin = skin;
                this.skinData = SkinSelection.skinData[skin];
                this.characterData = SkinSelection.characterData[skinData.character];
                this.tint = new(tint.r, tint.g, tint.b);
                this.tintAmount = tintAmount * skinData.tintFactor;
                var v = RWCustom.Custom.RGB2HSL(Color.Lerp(Color.white, this.tint, this.tintAmount));
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


/*        internal static void InitMeadowHud(RoomCamera camera)
        {
            var mgm = OnlineManager.lobby.gameMode as MeadowGameMode;
            camera.hud.AddPart(new HUD.TextPrompt(camera.hud)); // game assumes this never null
            camera.hud.AddPart(new HUD.Map(camera.hud, new HUD.Map.MapData(camera.room.world, camera.room.game.rainWorld))); // game assumes this too :/
            camera.hud.AddPart(new EmoteHandler(camera.hud, mgm.avatar, creatureCustomizations.GetValue(mgm.avatar.realizedCreature, (c) => throw new InvalidProgrammerException("Creature doesn't have customization"))));
        }*/
    }
}
