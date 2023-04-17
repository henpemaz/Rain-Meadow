using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static RainMeadow.RoomSession;

namespace RainMeadow
{
    public partial class WorldSession : OnlineResource
    {
        public Region region;
        public World world;

        internal static ConditionalWeakTable<World, WorldSession> map = new();
        public Dictionary<string, RoomSession> roomSessions = new();

        protected override World World => world;

        public WorldSession(Region region, Lobby lobby)
        {
            this.region = region;
            this.super = lobby;
        }

        internal void BindWorld(World world)
        {
            this.world = world;
            map.Add(world, this);
        }

        protected override void ActivateImpl()
        {
            if(world == null) throw new InvalidOperationException("world not set");
            foreach (var room in world.abstractRooms)
            {
                var rs = new RoomSession(this, room);
                roomSessions.Add(room.name, rs);
                subresources.Add(rs);
            }
            foreach (var item in earlyEntities)
            {
                NewEntityInWorld(item);
            }
            earlyEntities.Clear();
        }

        protected override void DeactivateImpl()
        {
            this.roomSessions.Clear();
            world = null;
        }

        public override void ReadState(ResourceState newState, ulong ts)
        {
            base.ReadState(newState, ts);
            if (newState is WorldState newWorldState)
            {
                // no op
            }
            else
            {
                throw new InvalidCastException("not a WorldState");
            }
        }

        protected override ResourceState MakeState(ulong ts)
        {
            return new WorldState(this, ts);
        }

        internal override string Identifier()
        {
            return region.name;
        }

        public class WorldState : ResourceState
        {
            public WorldState() : base() { }
            public WorldState(OnlineResource resource, ulong ts) : base(resource, ts)
            {

            }

            public override StateType stateType => StateType.WorldState;
        }
    }
}
