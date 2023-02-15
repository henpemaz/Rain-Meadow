using System.Collections.Generic;

namespace RainMeadow
{
    public class WorldSession : OnlineResource
    {
        public Region region;
        public List<RoomSession> rooms;

        public WorldSession(Region region, Lobby lobby)
        {
            this.region = region;
            this.super = lobby;
        }

        public override void ReadState(ResourceState newState, ulong ts)
        {
            throw new System.NotImplementedException();
        }

        protected override ResourceState MakeState(ulong ts)
        {
            throw new System.NotImplementedException();
        }

        internal override string Identifier()
        {
            return region.name;
        }
    }
}
