using UnityEngine;
using static RainMeadow.OnlineEntity;

namespace RainMeadow
{

    // onlinestate for polymorphism
    [OnlineState.DeltaSupport(level = StateHandler.DeltaSupport.None)]
    public abstract class OverlaySkin : OnlineState
    {
        [OnlineField] 
        public int _unused;
        abstract public Texture2D texture { get; }
        abstract public Texture2D glowtexture { get; }
        virtual public bool Available(OnlineEntity entity) => true;
    }

    public class NightSkySkin : OverlaySkin
    {
        override public Texture2D texture => RainMeadow.nightsky;
        override public Texture2D glowtexture => RainMeadow.nightskyGlow;
        override public bool Available(OnlineEntity entity) => MatchmakingManager.currentInstance.IsDev(entity.owner.id);
    }

    public class CoinSkin : OverlaySkin
    {
        override public Texture2D texture => RainMeadow.coin_tile;
        override public Texture2D glowtexture => RainMeadow.nightskyGlow;
    }

    public abstract class AvatarData : EntityData
    {
        internal abstract void ModifyBodyColor(ref Color bodyColor);
        internal abstract void ModifyEyeColor(ref Color eyeColor);

        public OverlaySkin? overlaySkin;
        public static OverlaySkin ConfigureOverlay(OnlineEntity entity)
        {
            if (RainMeadow.rainMeadowOptions.boughtGoldenSkin.Value && SpecialEvents.EventActiveInLobby<SpecialEvents.AprilFools>())
            {
                return new CoinSkin();
            }

            if (RainMeadow.rainMeadowOptions.DevNightskySkin.Value && MatchmakingManager.currentInstance.IsDev(entity.owner.id))
            {
                return new NightSkySkin();
            }

            return null;
        }

        public abstract class AvatarDataState : EntityDataState
        {
            [OnlineField(nullable = true, polymorphic = true)]
            OverlaySkin? overlay;

            public AvatarDataState() { }
            public AvatarDataState(AvatarData avatarData)
            {
                overlay = avatarData.overlaySkin;
            }

            public override void ReadTo(EntityData data, OnlineEntity onlineEntity)
            {
                if (data is AvatarData avatarData)
                {
                    if (avatarData.overlaySkin?.GetType() != overlay?.GetType())
                    {
                        avatarData.overlaySkin = overlay;
                        if (onlineEntity is OnlinePhysicalObject obj)
                        {
                            if (obj.apo.realizedObject is PhysicalObject realobj)
                            {
                                CapeManager.RefreshGraphicalModule(realobj);
                            }
                        }
                    }
                }
            }
        }
    }
}