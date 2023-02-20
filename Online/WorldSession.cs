using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class WorldSession : OnlineResource
    {
        public Region region;
        private World world;

        public Dictionary<string, RoomSession> roomSessions = new();

        public WorldSession(Region region, Lobby lobby)
        {
            this.region = region;
            this.super = lobby;
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
        }

        protected override void DeactivateImpl()
        {
            this.roomSessions.Clear();
            world = null;
        }

        public override void ReadState(ResourceState newState, ulong ts)
        {
            //throw new System.NotImplementedException();
        }

        protected override ResourceState MakeState(ulong ts)
        {
            return new WorldState(this, ts);
        }

        internal override string Identifier()
        {
            return region.name;
        }

        internal void BindWorld(World world)
        {
            this.world = world;
        }

        public class WorldState : ResourceState
        {
            public WorldState(OnlineResource resource, ulong ts) : base(resource, ts)
            {
            }

            public override ResourceStateType stateType => ResourceStateType.WorldState;
        }
    }
}
