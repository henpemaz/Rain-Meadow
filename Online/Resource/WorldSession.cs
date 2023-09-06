using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RainMeadow
{
    public partial class WorldSession : OnlineResource
    {
        public Region region;
        public World world;

        public static ConditionalWeakTable<World, WorldSession> map = new();
        public Dictionary<string, RoomSession> roomSessions = new();

        public override World World => world;

        public WorldSession(Region region, Lobby lobby)
        {
            this.region = region;
            this.super = lobby;
        }

        public void BindWorld(World world)
        {
            this.world = world;
            map.Add(world, this);
        }

        protected override void AvailableImpl()
        {

        }

        protected override void ActivateImpl()
        {
            if (world == null) throw new InvalidOperationException("world not set");
            foreach (var room in world.abstractRooms)
            {
                var rs = new RoomSession(this, room);
                roomSessions.Add(room.name, rs);
                subresources.Add(rs);
            }
            foreach (var item in earlyApos)
            {
                ApoEnteringWorld(item);
            }
            earlyApos.Clear();
        }

        protected override void DeactivateImpl()
        {
            this.roomSessions.Clear();
            world = null;
        }

        protected override ResourceState MakeState(uint ts)
        {
            return new WorldState(this, ts);
        }

        public override string Id()
        {
            return region.name;
        }

        public override ushort ShortId()
        {
            return (ushort)region.regionNumber;
        }

        public override OnlineResource SubresourceFromShortId(ushort shortId)
        {
            return this.subresources[shortId - region.firstRoomIndex];
        }

        public class WorldState : ResourceWithSubresourcesState
        {
            public WorldState() : base() { }
            public WorldState(OnlineResource resource, uint ts) : base(resource, ts) { }
        }

        public override string ToString()
        {
            return "Region " + Id();
        }
    }
}
