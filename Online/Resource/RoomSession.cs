using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Watcher;

namespace RainMeadow
{
    public partial class RoomSession : OnlineResource
    {
        public AbstractRoom absroom;
        public static ConditionalWeakTable<AbstractRoom, RoomSession> map = new();

        public WorldSession worldSession => super as WorldSession;
        public World World => worldSession.world;

        public RoomSession(WorldSession ws, AbstractRoom absroom) : base(ws)
        {
            this.absroom = absroom;
            map.Add(absroom, this);
        }

        protected override void AvailableImpl()
        {

        }

        protected override void ActivateImpl()
        {
            foreach (var ent in absroom.entities.Concat(absroom.entitiesInDens))
            {
                if (ent is AbstractPhysicalObject apo)
                {
                    worldSession.ApoEnteringWorld(apo);
                    ApoEnteringRoom(apo, apo.pos);
                }
            }

            if (isOwner)
            {
                foreach (var ent in absroom.entities.Concat(absroom.entitiesInDens))
                {
                    if (ent is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe)
                         && !oe.realized && !oe.isMine && oe.isTransferable && !oe.isPending)
                    {
                        oe.Request(); // I am realizing this entity, let me have it
                    }
                }
            }
        }

        protected override void DeactivateImpl()
        {

        }

        protected override void UnavailableImpl()
        {

        }

        public override string Id()
        {
            return super.Id() + "." + absroom.name;
        }

        public override ushort ShortId()
        {
            return (ushort)(absroom.index - absroom.world.firstRoomIndex);
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
            [OnlineFieldHalf]
            float FlameJetTime;
            public RoomState() : base() { }
            public RoomState(RoomSession resource, uint ts) : base(resource, ts)
            {
                if (resource.absroom.realizedRoom != null)
                {
                    FlameJet firstJet = resource.absroom.realizedRoom.updateList.OfType<FlameJet>().FirstOrDefault();
                    if (firstJet != null)
                    {
                        FlameJetTime = firstJet.time;
                    }
                }
            }

            public override void ReadTo(OnlineResource resource)
            {
                base.ReadTo(resource);

                if (resource.isActive)
                {
                    var rs = (RoomSession)resource;

                    if (rs.absroom.realizedRoom != null)
                    {
                        foreach (FlameJet flameJet in rs.absroom.realizedRoom.updateList.OfType<FlameJet>())
                        {
                            flameJet.time = Mathf.Max(FlameJetTime, flameJet.time);
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return "Room " + Id();
        }
    }
}
