using System;
using static RainMeadow.Serializer;

namespace RainMeadow
{
    public class PlayerTickReference : ICustomSerializable
    {
        internal OnlinePlayer fromPlayer;
        internal ulong tick;

        public PlayerTickReference() { }
        public PlayerTickReference(OnlinePlayer fromPlayer, ulong tick)
        {
            this.fromPlayer = fromPlayer;
            this.tick = tick;
        }

        public PlayerTickReference(OnlinePlayer player)
        {
            this.fromPlayer = player;
            this.tick = player.tick;
        }

        internal bool Invalid()
        {
            return fromPlayer == null || fromPlayer.hasLeft;
        }

        internal bool ChecksOut()
        {
            return !Invalid() && OnlineManager.IsNewerOrEqual(fromPlayer.tick, tick);
        }

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref fromPlayer);
            serializer.Serialize(ref tick);
        }

        public static PlayerTickReference NewestOf(PlayerTickReference tick, OnlineResource inResource, PlayerTickReference otherTick, OnlineResource otherResource)
        {
            if (otherTick.fromPlayer != otherResource.owner && tick.fromPlayer != inResource.owner) return null;
            if (otherTick.fromPlayer != otherResource.owner) return tick;
            if (tick.fromPlayer != inResource.owner) return otherTick;
            return OnlineManager.IsNewerOrEqual(tick.tick, otherTick.tick) ? tick : otherTick;
        }
    }
}