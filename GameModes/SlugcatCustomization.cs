using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class SlugcatCustomization : AvatarData
    {
        // Error colors, suggests something's gone wrong in StartGame (which should handle setting to either custom or default depending on the checkbox)
        public List<Color> currentColors { get; set; } = [Color.magenta, Color.white];

        public bool globalMute { get; set; } = RainMeadow.rainMeadowOptions.GlobalMute.Value;
        public string? cosmetic { get; set; }
        public string? cosmeticSkin { get; set; }
        public Color customCosmeticColor;

        public Color bodyColor { get => currentColors[0]; set => currentColors[0] = value; }
        public Color eyeColor { get => currentColors[1]; set => currentColors[1] = value; }
        public bool fakePup { get; set; }

        public int playerIndex = 0;
        public SlugcatStats.Name playingAs;
        public string nickname;

        public SlugcatCustomization()
        {
            cosmetic = RainMeadow.rainMeadowOptions.currentlyActiveCosmetic.Value;
            cosmeticSkin = RainMeadow.rainMeadowOptions.currentlyActiveCosmeticSkin.Value;
            customCosmeticColor = RainMeadow.rainMeadowOptions.currentlyActiveCustomCosmeticColor.Value;
        }

        internal override void ModifyBodyColor(ref Color originalBodyColor)
        {
            originalBodyColor = bodyColor.SafeColorRange();
        }

        internal override void ModifyEyeColor(ref Color originalEyeColor)
        {
            originalEyeColor = eyeColor.SafeColorRange();
        }

        internal Color SlugcatColor()
        {
            return bodyColor;
        }

        public Color GetColor(int staticColorIndex)
        {
            if (staticColorIndex >= 0 && staticColorIndex < currentColors.Count)
            {
                return currentColors[staticColorIndex];
            }

            // Indicates something's gone wrong (staticColorIndex is outside the range of available colors)
            return Color.magenta;
        }

        public override EntityDataState MakeState(OnlineEntity onlineEntity, OnlineResource inResource)
        {
            return new State(this);
        }

        public class State : AvatarDataState
        {
            [OnlineFieldColorRgb]
            public Color[] customColors;
            [OnlineField(nullable = true)]
            public SlugcatStats.Name playingAs;
            [OnlineField]
            public string nickname;

            [OnlineField]
            public bool wearingCape;



            [OnlineField(group: "cosmetic", nullable: true)]   
            public string? cosmetic;

            [OnlineField(group: "cosmetic", nullable: true)]   
            public string? cosmeticSkin;

            [OnlineField(group: "cosmetic_color", nullable: true)]
            public string customCosmeticColor;

            [OnlineField]
            public int playerIndex;

            [OnlineField]
            public bool globalMute;

            [OnlineField]
            public bool fakePup { get; set; }

            public State() { }
            public State(SlugcatCustomization slugcatCustomization) : base(slugcatCustomization)
            {
                customColors = slugcatCustomization.currentColors.ToArray();
                playingAs = slugcatCustomization.playingAs;
                nickname = slugcatCustomization.nickname;
                cosmetic = slugcatCustomization.cosmetic;
                cosmeticSkin = slugcatCustomization.cosmeticSkin;
                customCosmeticColor = ColorUtility.ToHtmlStringRGB(slugcatCustomization.customCosmeticColor);
                playerIndex = slugcatCustomization.playerIndex;
                fakePup = slugcatCustomization.fakePup;
                globalMute = slugcatCustomization.globalMute;
            }

            public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
            {
                base.ReadTo(entityData, onlineEntity);
                var slugcatCustomization = (SlugcatCustomization)entityData;
                slugcatCustomization.currentColors = customColors.ToList();
                slugcatCustomization.playingAs = playingAs;
                slugcatCustomization.nickname = nickname;
                slugcatCustomization.globalMute = globalMute;

                bool needsGraphicRefresh = false;
                if (cosmetic != slugcatCustomization.cosmetic)
                {
                    if (CosmeticManager.AvailableCosmetics(onlineEntity.owner.id).Contains(cosmetic))
                    {
                        slugcatCustomization.cosmetic = cosmetic;
                        needsGraphicRefresh = true;
                    }
                }

                if (cosmeticSkin != slugcatCustomization.cosmeticSkin)
                {
                    if (CosmeticManager.AvailableCosmeticSkins(onlineEntity.owner.id).Contains(cosmeticSkin))
                    {
                        slugcatCustomization.cosmeticSkin = cosmeticSkin;
                        needsGraphicRefresh = true;
                    }
                }
                if (ColorUtility.TryParseHtmlString(customCosmeticColor, out var newColor))
                {   
                    if (((Vector4)newColor - (Vector4)slugcatCustomization.customCosmeticColor).sqrMagnitude > 0.01)
                    {
                        slugcatCustomization.customCosmeticColor = newColor;
                        needsGraphicRefresh = true;
                    }
                }

                if (needsGraphicRefresh && onlineEntity is OnlineCreature critter && critter.abstractCreature.realizedCreature is Creature s)
                {
                    CosmeticManager.RefreshGraphicalModule(s);
                }
                

                slugcatCustomization.playerIndex = playerIndex;
                slugcatCustomization.fakePup = fakePup;
            }

            public override Type GetDataType() => typeof(SlugcatCustomization);
        }
    }
}
