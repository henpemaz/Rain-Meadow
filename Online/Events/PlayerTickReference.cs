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

        internal bool ChecksOut()
        {
            return !Invalid() && OnlineManager.IsNewerOrEqual(fromPlayer.tick, tick);
        }

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref fromPlayer);
            serializer.Serialize(ref tick);
        }

        public static bool IsNewerOrEqual(PlayerTickReference tick, OnlineResource inResource, PlayerTickReference otherTick, OnlineResource otherResource)
        {
            if (otherTick.fromPlayer != otherResource.owner && tick.fromPlayer != inResource.owner) throw new InvalidProgrammerException("neither");
            if (otherTick.fromPlayer != otherResource.owner) return true;
            if (tick.fromPlayer != inResource.owner) return false;
            return OnlineManager.IsNewerOrEqual(tick.tick, otherTick.tick);
        }

        internal bool Invalid()
        {
            return fromPlayer == null || fromPlayer.hasLeft;
        }
    }
}