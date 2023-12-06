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
            private MeadowProgression.SkinData skinData;
            private MeadowProgression.CharacterData characterData;
            public Color tint;
            public float tintAmount;

            private Color emoteBgColor;
            private Color symbolBgColor;
            private Color emoteColor;
            private Color symbolColor;

            public CreatureCustomization(MeadowProgression.Skin skin, Color tint, float tintAmount)
            {
                this.skin = skin;
                this.skinData = MeadowProgression.skinData[skin];
                this.characterData = MeadowProgression.characterData[skinData.character];
                this.tint = new(tint.r, tint.g, tint.b);
                this.tintAmount = tintAmount * skinData.tintFactor;

                emoteBgColor = Color.Lerp(skinData.emoteColorOverride ?? characterData.emoteColor, this.tint, this.tintAmount);
                symbolBgColor = Color.white;
                emoteColor = Color.white;
                var v = RWCustom.Custom.RGB2HSL(Color.Lerp(Color.white, this.tint, this.tintAmount));
                symbolColor = new HSLColor(v[0], v[1], v[2]).rgb;
            }

            internal string EmoteAtlas => skinData.emoteAtlasOverride ?? characterData.emoteAtlas;
            internal string EmotePrefix => skinData.emotePrefixOverride ?? characterData.emotePrefix;

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

            internal string GetEmote(EmoteType emote)
            {
                return (emote.value.StartsWith("emote") ? EmotePrefix + emote.value : emote.value).ToLowerInvariant();
            }

            internal string GetBackground(EmoteType emote)
            {
                return (emote.value.StartsWith("emote") ? "emote_background" : "symbols_background");
            }

            internal Color EmoteBackgroundColor(EmoteType emote)
            {
                return emote.value.StartsWith("emote") ? emoteBgColor : symbolBgColor;
            }

            internal Color EmoteColor(EmoteType emote)
            {
                return emote.value.StartsWith("emote") ? emoteColor : symbolColor;
            }
        }

        public static ConditionalWeakTable<Creature, CreatureCustomization> creatureCustomizations = new();

        internal static void Customize(Creature creature, OnlineCreature oc)
        {
            if (MeadowAvatarSettings.map.TryGetValue(oc.owner, out MeadowAvatarSettings mas))
            {
                RainMeadow.Debug($"Customizing avatar {creature} for {oc.owner}");
                var mcc = MeadowCustomization.creatureCustomizations.GetValue(creature, (c) => mas.MakeCustomization());
                if (oc.gameModeData is MeadowCreatureData mcd)
                {
                    EmoteDisplayer.map.GetValue(creature, (c) => new EmoteDisplayer(creature, oc, mcd, mcc));
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

        internal static void InitMeadowHud(RoomCamera camera)
        {
            var mgm = OnlineManager.lobby.gameMode as MeadowGameMode;
            camera.hud.AddPart(new HUD.TextPrompt(camera.hud)); // game assumes this never null
            camera.hud.AddPart(new HUD.Map(camera.hud, new HUD.Map.MapData(camera.room.world, camera.room.game.rainWorld))); // game assumes this too :/
            camera.hud.AddPart(new EmoteHandler(camera.hud, mgm.avatar, creatureCustomizations.GetValue(mgm.avatar.realizedCreature, (c) => throw new InvalidProgrammerException("Creature doesn't have customization"))));
        }
    }
}
