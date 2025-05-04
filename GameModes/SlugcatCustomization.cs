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
        public bool wearingCape { get; set; } = RainMeadow.rainMeadowOptions.WearingCape.Value;

        public Color bodyColor { get => currentColors[0]; set => currentColors[0] = value; }
        public Color eyeColor { get => currentColors[1]; set => currentColors[1] = value; }

        public int playerIndex = 0;
        public SlugcatStats.Name playingAs;
        public string nickname;

        public SlugcatCustomization() { }

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

        public class State : EntityDataState
        {
            [OnlineFieldColorRgb]
            public Color[] customColors;
            [OnlineField(nullable = true)]
            public SlugcatStats.Name playingAs;
            [OnlineField]
            public string nickname;

            [OnlineField]
            public bool wearingCape;

            [OnlineField]
            public int playerIndex;

            public State() { }
            public State(SlugcatCustomization slugcatCustomization) : base()
            {
                customColors = slugcatCustomization.currentColors.ToArray();
                playingAs = slugcatCustomization.playingAs;
                nickname = slugcatCustomization.nickname;
                wearingCape = slugcatCustomization.wearingCape;
                playerIndex = slugcatCustomization.playerIndex;
            }

            public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
            {
                var slugcatCustomization = (SlugcatCustomization)entityData;
                slugcatCustomization.currentColors = customColors.ToList();
                slugcatCustomization.playingAs = playingAs;
                slugcatCustomization.nickname = nickname;
                slugcatCustomization.wearingCape = wearingCape;
                slugcatCustomization.playerIndex =  playerIndex;
            }

            public override Type GetDataType() => typeof(SlugcatCustomization);
        }
    }
}
