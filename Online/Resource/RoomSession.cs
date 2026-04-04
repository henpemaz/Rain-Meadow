using MoreSlugcats;
using RainMeadow.Generics;
using RWCustom;
using System;
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
            if (isOwner && absroom.realizedRoom != null)
            {
                foreach(var obj in absroom.realizedRoom.updateList)
                {
                    if (obj is ScavengerOutpost outpost && outpost.pearlStrings.Count == 0)
                    {
                        //initiate outpost pearl strings
                        Random.State state = Random.state;
                        Random.InitState((outpost.placedObj.data as PlacedObject.ScavengerOutpostData).pearlsSeed);
                        int length = Random.Range(5, 15);
                        for (int i = 0; i < length; i++)
                        {
                            var pearlString = new ScavengerOutpost.PearlString(outpost.room, outpost, 20f + Mathf.Lerp(20f, 150f, Random.value) * Custom.LerpMap(length, 5f, 15f, 1f, 0.1f));
                            outpost.room.AddObject(pearlString);
                            outpost.pearlStrings.Add(pearlString);

                            pearlString.Initiate();
                        }
                        Random.state = state;
                    }
                }
                foreach(var obj in absroom.realizedRoom.roomSettings.placedObjects)
                {
                    if (obj.type == PlacedObject.Type.HangingPearls)
                    {
                        absroom.realizedRoom.AddObject(new HangingPearlString(absroom.realizedRoom, Mathf.Lerp(60f, 180f, 0.5f + Mathf.Sin(obj.pos.x * 10f) / 2f), obj.pos));
                    }
                }
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
            [OnlineField(nullable = true)]
            private DynamicOrderedStates<PlacedObjectRef> placedObjects;
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
                    // Track these on a need by need basis (Severe dupes, etc)
                    placedObjects = new();
                    foreach(var placedObject in resource.absroom.realizedRoom.roomSettings.placedObjects
                        .Where(p => p.type == PlacedObject.Type.HangingPearls || p.type == PlacedObject.Type.ScavengerOutpost))
                    {
                        placedObjects.list.Add(PlacedObjectRef.FromPlacedObject(placedObject, resource.absroom.realizedRoom));
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

                        foreach(var placedObject in placedObjects.list)
                        {
                            var po = placedObject.ToPlacedObject(rs);
                            if (po != null && !PlacedObjectRef.map.TryGetValue(po, out _))
                            {
                                PlacedObjectRef.map.Add(po, placedObject);
                            }
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
