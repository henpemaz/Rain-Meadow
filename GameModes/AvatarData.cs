using UnityEngine;
using static RainMeadow.OnlineEntity;

namespace RainMeadow
{
    public abstract class AvatarData : EntityData
    {
        internal abstract void ModifyBodyColor(ref Color bodyColor);

        internal abstract void ModifyEyeColor(ref Color eyeColor);

        internal abstract Color GetBodyColor();
    }
}