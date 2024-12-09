using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class SlugcatCustomization : AvatarData
    {
        public List<Color> customColors = new() { Color.white, Color.black };
        public Color bodyColor { get => customColors[0]; set => customColors[0] = value; }
        public Color eyeColor { get => customColors[1]; set => customColors[1] = value; }
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

            public State() { }
            public State(SlugcatCustomization slugcatCustomization) : base()
            {
                customColors = slugcatCustomization.customColors.ToArray();
                playingAs = slugcatCustomization.playingAs;
                nickname = slugcatCustomization.nickname;
            }

            public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
            {
                var slugcatCustomization = (SlugcatCustomization)entityData;
                slugcatCustomization.customColors = customColors.ToList();
                slugcatCustomization.playingAs = playingAs;
                slugcatCustomization.nickname = nickname;
            }

            public override Type GetDataType() => typeof(SlugcatCustomization);
        }
    }
}
