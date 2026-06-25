using MoreSlugcats;
using RainMeadow.Generics;
using RWCustom;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Watcher;

using Random = UnityEngine.Random;

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

        public void LoadPearlStrings()
        {
            if (!isActive || !isAvailable) return;
            if (absroom.realizedRoom == null) return;
            RainMeadow.Debug("looking for pearls");
            if (isOwner)
            {
                foreach(var obj in absroom.realizedRoom.updateList)
                {
                    if (obj is HangingPearlString or ScavengerOutpost.PearlString)
                    {
                        OnlinePearlString.InitializePearlString(obj);
                    }
                }
            }

            foreach (var entity in activeEntities.OfType<OnlinePearlString>())
            {
                entity.FindPearlString(this);
            }
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

            LoadPearlStrings();
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
                        var room = rs.absroom.realizedRoom;
                        foreach (FlameJet flameJet in room.updateList.OfType<FlameJet>())
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
