using Mono.Cecil;
using System;
using static RainMeadow.Serializer;

namespace RainMeadow
{
    public class TickReference : ICustomSerializable
    {
        public ushort fromPlayer;
        public uint tick;

        public TickReference() { }
        public TickReference(OnlinePlayer fromPlayer, uint tick)
        {
            this.fromPlayer = fromPlayer.inLobbyId;
            this.tick = tick;
        }

        public TickReference(OnlinePlayer player)
        {
            this.fromPlayer = player.inLobbyId;
            this.tick = player.tick;
        }

        public bool Invalid()
        {
            return OnlineManager.lobby.PlayerFromId(fromPlayer) == null;
        }

        public bool ChecksOut()
        {
            return !Invalid() && NetIO.IsNewerOrEqual(OnlineManager.lobby.PlayerFromId(fromPlayer).tick, tick);
        }

        public float TimeSinceTick()
        {
            var player = OnlineManager.lobby.PlayerFromId(fromPlayer);
            if(player == null) { RainMeadow.Error("Player not found: " + fromPlayer); return -1; }
            return (player.tick - tick) / (float) OnlineManager.instance.framesPerSecond;
        }

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref fromPlayer);
            serializer.Serialize(ref tick);
        }

        public static TickReference NewestOf(TickReference tick, OnlineResource inResource, TickReference otherTick, OnlineResource otherResource)
        {
            var aInvalid = inResource.supervisor == null || tick.fromPlayer != inResource.supervisor.inLobbyId;
            var bInvalid = otherResource.supervisor == null || otherTick.fromPlayer != otherResource.supervisor.inLobbyId;
            if (aInvalid && bInvalid) return null;
            if (aInvalid) return otherTick;
            if (bInvalid) return tick;
            return NetIO.IsNewerOrEqual(tick.tick, otherTick.tick) ? tick : otherTick;
        }

        public static TickReference NewestOfMemberships(ResourceMembership membershipA, ResourceMembership membershipB)
        {
            var aInvalid = membershipA == null || membershipA.resource.supervisor == null || membershipA.memberSinceTick.fromPlayer != membershipA.resource.supervisor.inLobbyId;
            var bInvalid = membershipB == null || membershipB.resource.supervisor == null || membershipB.memberSinceTick.fromPlayer != membershipB.resource.supervisor.inLobbyId;
            if (aInvalid && bInvalid) return null;
            if (aInvalid) return membershipB.memberSinceTick;
            if (bInvalid) return membershipA.memberSinceTick;
            if (membershipA.resource != membershipB.resource && !membershipA.resource.IsSibling(membershipB.resource)) return null;
            return NetIO.IsNewerOrEqual(membershipA.memberSinceTick.tick, membershipB.memberSinceTick.tick) ? membershipA.memberSinceTick : membershipB.memberSinceTick;
        }

        internal bool IsNewerThan(OnlineResource inResource, TickReference otherTick, OnlineResource otherResource)
        {
            var aInvalid = inResource.supervisor == null || this.fromPlayer != inResource.supervisor.inLobbyId;
            var bInvalid = otherTick == null || otherResource.supervisor == null || otherTick.fromPlayer != otherResource.supervisor.inLobbyId;
            if (aInvalid && bInvalid) return false;
            if (aInvalid) return false;
            if (bInvalid) return true;
            if (inResource.supervisor != otherResource.supervisor) return false;
            return NetIO.IsNewer(this.tick, otherTick.tick) ? true : false;
        }
    }
}