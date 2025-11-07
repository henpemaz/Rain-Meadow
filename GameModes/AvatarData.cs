using UnityEngine;
using static RainMeadow.OnlineEntity;

namespace RainMeadow
{
    public abstract class AvatarData : EntityData
    {
        internal abstract void ModifyBodyColor(ref Color bodyColor);

        internal abstract void ModifyEyeColor(ref Color eyeColor);

        private bool nightSkySkin { get; set; } = RainMeadow.rainMeadowOptions.DevNightskySkin.Value;
        internal bool IsNightSkySkin(OnlineEntity onlineEntity)
        {
            if (!nightSkySkin) return false;
            return RainMeadow.IsDev(onlineEntity.owner.id);
        }

        public abstract class AvatarDataState : EntityDataState
        {
            [OnlineField]
            bool nightSkySkin;

            public AvatarDataState() { }
            public AvatarDataState(AvatarData avatarData)
            {
                nightSkySkin = avatarData.nightSkySkin;
            }

            public override void ReadTo(EntityData data, OnlineEntity onlineEntity)
            {
                if (data is AvatarData avatarData)
                {
                    avatarData.nightSkySkin = nightSkySkin;
                }
            }
        }
    }
}