using System.Collections.Generic;

namespace RainMeadow
{
    public class WorldSession// : OnlineResource
    {
        public Region region;
        public List<RoomSession> rooms;

        public WorldSession(Region region)
        {
            this.region = region;
        }
    }
}
