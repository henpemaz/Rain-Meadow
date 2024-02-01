using System.Runtime.CompilerServices;

namespace RainMeadow
{
    public partial class RoomSession : OnlineResource
    {
        public AbstractRoom absroom;
        public bool abstractOnDeactivate;
        public static ConditionalWeakTable<AbstractRoom, RoomSession> map = new();

        public WorldSession worldSession => super as WorldSession;
        public override World World => worldSession.world;

        public RoomSession(WorldSession ws, AbstractRoom absroom)
        {
            super = ws;
            this.absroom = absroom;
            map.Add(absroom, this);
        }

        protected override void AvailableImpl()
        {
            if (isOwner)
            {
                foreach (var ent in absroom.entities)
                {
                    if (ent is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe)
                         && !oe.realized && !oe.isMine && oe.isTransferable && !oe.isPending)
                    {
                        oe.Request(); // I am realizing this entity, let me have it
                    }
                }
            }
        }

        protected override void ActivateImpl()
        {
            foreach (var ent in absroom.entities)
            {
                if (ent is AbstractPhysicalObject apo)
                {
                    if (OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(worldSession, apo)) worldSession.ApoEnteringWorld(apo);
                    if (OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(this, apo)) ApoEnteringRoom(apo, apo.pos);
                }
            }
        }

        protected override void DeactivateImpl()
        {
            if (abstractOnDeactivate)
            {
                absroom.Abstractize();
                abstractOnDeactivate = false;
            }
        }
        public override string Id()
        {
            return super.Id() + absroom.name;
        }

        public override ushort ShortId()
        {
            return (ushort)absroom.index;
        }

        public override OnlineResource SubresourceFromShortId(ushort shortId)
        {
            return this.subresources[shortId];
        }

        protected override ResourceState MakeState(uint ts)
        {
            return new RoomState(this, ts);
        }

        public class RoomState : ResourceState
        {
            public RoomState() : base() { }
            public RoomState(RoomSession resource, uint ts) : base(resource, ts) { }
        }

        public override string ToString()
        {
            return "Room " + Id();
        }
    }
}
