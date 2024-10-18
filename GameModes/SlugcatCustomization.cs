using System;
using UnityEngine;

namespace RainMeadow
{
    public class SlugcatCustomization : AvatarData
    {
        public Color bodyColor;
        public Color eyeColor;
        public SlugcatStats.Name playingAs;
        public string nickname;

        public SlugcatCustomization() { }

        internal override void ModifyBodyColor(ref Color originalBodyColor)
        {
            originalBodyColor = new Color(Mathf.Clamp(bodyColor.r, 0.004f, 0.996f), Mathf.Clamp(bodyColor.g, 0.004f, 0.996f), Mathf.Clamp(bodyColor.b, 0.004f, 0.996f));
        }

        internal override void ModifyEyeColor(ref Color originalEyeColor)
        {
            originalEyeColor = new Color(Mathf.Clamp(eyeColor.r, 0.004f, 0.996f), Mathf.Clamp(eyeColor.g, 0.004f, 0.996f), Mathf.Clamp(eyeColor.b, 0.004f, 0.996f));
        }

        internal override Color GetBodyColor()
        {
            return bodyColor;
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
            public Color bodyColor;
            [OnlineFieldColorRgb]
            public Color eyeColor;
            [OnlineField(nullable = true)]
            public SlugcatStats.Name playingAs;
            [OnlineField]
            public string nickname;

            public State() { }
            public State(SlugcatCustomization slugcatCustomization) : base()
            {
                bodyColor = slugcatCustomization.bodyColor;
                eyeColor = slugcatCustomization.eyeColor;
                playingAs = slugcatCustomization.playingAs;
                nickname = slugcatCustomization.nickname;
            }

            public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
            {
                var slugcatCustomization = (SlugcatCustomization)entityData;
                slugcatCustomization.bodyColor = bodyColor;
                slugcatCustomization.eyeColor = eyeColor;
                slugcatCustomization.playingAs = playingAs;
                slugcatCustomization.nickname = nickname;

                if (UnityEngine.Input.GetKey(KeyCode.L))
                {
                    RainMeadow.Debug("color? " + bodyColor);
                }
            }

            public override Type GetDataType() => typeof(SlugcatCustomization);
        }
    }
}
