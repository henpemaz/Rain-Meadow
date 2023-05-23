using Mono.Cecil;
using System;
using static RainMeadow.Serializer;

namespace RainMeadow
{
    public class TickReference : ICustomSerializable
    {
        internal OnlinePlayer fromPlayer;
        internal ulong tick;

        public TickReference() { }
        public TickReference(OnlinePlayer fromPlayer, ulong tick)
        {
            this.fromPlayer = fromPlayer;
            this.tick = tick;
        }

        public TickReference(OnlinePlayer player)
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

        public static TickReference NewestOf(TickReference tick, OnlineResource inResource, TickReference otherTick, OnlineResource otherResource)
        {
            if (otherTick.fromPlayer != otherResource.owner && tick.fromPlayer != inResource.owner) return null;
            if (otherTick.fromPlayer != otherResource.owner) return tick;
            if (tick.fromPlayer != inResource.owner) return otherTick;
            return OnlineManager.IsNewerOrEqual(tick.tick, otherTick.tick) ? tick : otherTick;
        }

        internal static TickReference NewestOfMemberships(ResourceMembership membershipA, ResourceMembership membershipB)
        {
            if (membershipA.memberSinceTick.fromPlayer != membershipA.resource.owner && membershipB.memberSinceTick.fromPlayer != membershipB.resource.owner) return null;
            if (membershipA.memberSinceTick.fromPlayer != membershipA.resource.owner) return membershipB.memberSinceTick;
            if (membershipB.memberSinceTick.fromPlayer != membershipB.resource.owner) return membershipA.memberSinceTick;
            return OnlineManager.IsNewerOrEqual(membershipA.memberSinceTick.tick, membershipB.memberSinceTick.tick) ? membershipA.memberSinceTick : membershipB.memberSinceTick;
        }
    }
}